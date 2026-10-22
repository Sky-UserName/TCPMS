using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

namespace TCPMS.Api.Services;

/// <summary>
/// 支付结果兜底核对。
/// 微信支付回调需要公网 HTTPS 地址；本地开发阶段、或线上回调偶发丢失时，订单会一直停在「待支付」。
/// 该任务定时主动向微信查单，把已支付订单补记为已支付（幂等，与回调共用同一套落库逻辑）。
/// </summary>
public sealed class PaymentReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    // 微信 JSAPI 预支付单有效期默认 2 小时，超过这个窗口再查单没有意义。
    private static readonly TimeSpan Lookback = TimeSpan.FromHours(2);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsPaymentConfigured())
        {
            logger.LogInformation("微信支付未配置，跳过支付状态兜底任务");
            return;
        }

        logger.LogInformation(
            "支付状态兜底任务已启动，每 {Seconds} 秒核对一次近 {Hours} 小时的待支付订单",
            (int)Interval.TotalSeconds,
            (int)Lookback.TotalHours);

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "支付状态兜底任务本轮执行失败");
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

    private bool IsPaymentConfigured()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<WeChatPayService>().IsPaymentConfigured;
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weChatPayService = scope.ServiceProvider.GetRequiredService<WeChatPayService>();

        var since = DateTime.UtcNow - Lookback;
        var pending = await db.BookingOrders
            .Where(order => order.Status == OrderStatuses.PendingPayment &&
                            order.PaymentStatus != "Paid" &&
                            order.CreatedAt >= since)
            .OrderByDescending(order => order.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var order in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WeChatPayOrderQuery? result;
            try
            {
                result = await weChatPayService.QueryOrderAsync(order.OrderNumber, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                logger.LogDebug(ex, "微信查单失败，跳过订单 {OrderNumber}", order.OrderNumber);
                continue;
            }

            if (result is null ||
                !string.Equals(result.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (result.TotalAmountCents != order.TotalAmountCents)
            {
                logger.LogWarning(
                    "订单 {OrderNumber} 微信支付金额 {Paid}Cents 与订单金额 {Expected}Cents 不一致，已跳过",
                    order.OrderNumber,
                    result.TotalAmountCents,
                    order.TotalAmountCents);
                continue;
            }

            var changed = await PaymentOrderService.MarkOrderPaidAsync(
                db,
                order,
                result.TransactionId,
                null,
                cancellationToken);
            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "订单 {OrderNumber} 通过微信查单补记为已支付，交易号 {TransactionId}",
                    order.OrderNumber,
                    result.TransactionId);
            }
        }
    }
}
