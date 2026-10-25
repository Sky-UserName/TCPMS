using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

namespace TCPMS.Api.Services;

/// <summary>
/// 未支付订单超时关单。
/// 下单时会把库存记到 LockedQuantity（锁定），原本只有用户主动取消才会释放；
/// 用户提交订单后直接离开，那几天的库存就会被永久占住。这里按 PaymentExpiresAt
/// 定期把过期订单置为已取消并回滚锁定量。多实例部署时依靠「状态判断 + 行级更新」保证幂等。
/// </summary>
public sealed class OrderExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 50;
    private const string CancellationReason = "支付超时，系统自动取消";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("订单超时关单任务已启动，每 {Seconds} 秒扫描一次过期未支付订单", (int)Interval.TotalSeconds);

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "订单超时关单任务本轮执行失败");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReleaseExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var expired = await db.BookingOrders
            .Where(x => x.Status == OrderStatuses.PendingPayment && x.PaymentExpiresAt <= now)
            .OrderBy(x => x.PaymentExpiresAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var order in expired)
        {
            // 关单时再确认一次状态，避免和用户支付/主动取消并发时重复处理。
            if (order.Status != OrderStatuses.PendingPayment)
            {
                continue;
            }

            var inventories = await db.InventoryDailies
                .Where(x => x.RoomTypeId == order.RoomTypeId &&
                            x.Date >= order.CheckIn &&
                            x.Date < order.CheckOut)
                .ToListAsync(cancellationToken);
            foreach (var inventory in inventories)
            {
                inventory.LockedQuantity = Math.Max(0, inventory.LockedQuantity - order.Quantity);
                inventory.UpdatedAt = now;
            }

            order.Status = OrderStatuses.Cancelled;
            order.CancellationReason = CancellationReason;
            order.UpdatedAt = now;

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorType = "System",
                Action = "order.expire",
                ResourceType = "BookingOrder",
                ResourceId = order.Id.ToString(),
                Summary = $"{order.OrderNumber} 支付超时自动取消，释放锁定库存 {order.Quantity}"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("已自动关闭 {Count} 笔超时未支付订单", expired.Count);
    }
}
