using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

namespace TCPMS.Api;

public static class MiniappOrderEndpoints
{
    public static void Map(RouteGroupBuilder orders)
    {
        orders.MapPost("/{id:guid}/refunds", async (
            Guid id,
            CreateRefundRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var order = await db.BookingOrders.SingleOrDefaultAsync(
                x => x.Id == id && x.WxUserId == userId.Value,
                cancellationToken);
            if (order is null)
            {
                return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            }
            if (order.PaymentStatus is not "Paid" and not "Refunded")
            {
                return Results.BadRequest(new ApiError("order_not_refundable", "未支付订单不能申请退款"));
            }
            if (order.Status is OrderStatuses.Refunding or OrderStatuses.Refunded)
            {
                return Results.Conflict(new ApiError("refund_exists", "该订单已经存在退款处理记录"));
            }
            if (order.Status is not OrderStatuses.PaidPendingConfirmation and
                not OrderStatuses.ConfirmedPendingCheckIn)
            {
                return Results.BadRequest(new ApiError("order_not_refundable", "当前订单状态不允许申请退款"));
            }
            if (request.AmountCents <= 0 || request.AmountCents > order.TotalAmountCents)
            {
                return Results.BadRequest(new ApiError("invalid_refund_amount", "退款金额不合法"));
            }
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest(new ApiError("refund_reason_required", "退款原因不能为空"));
            }

            var hasPending = await db.RefundRecords.AnyAsync(
                x => x.OrderId == order.Id &&
                     (x.Status == "PendingReview" || x.Status == "Approved"),
                cancellationToken);
            if (hasPending)
            {
                return Results.Conflict(new ApiError("refund_exists", "该订单已有退款申请"));
            }

            var originalStatus = order.Status;
            order.Status = OrderStatuses.Refunding;
            order.UpdatedAt = DateTime.UtcNow;
            var refund = new RefundRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                RefundNumber = $"RF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
                AmountCents = request.AmountCents,
                Reason = request.Reason.Trim(),
                Status = "PendingReview",
                OriginalOrderStatus = originalStatus,
                RequestedByUserId = userId.Value
            };
            db.RefundRecords.Add(refund);
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = userId.Value,
                ActorType = "Miniapp",
                Action = "refund.create",
                ResourceType = "RefundRecord",
                ResourceId = refund.Id.ToString(),
                Summary = refund.Reason
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/orders/{order.Id}/refunds/{refund.Id}", new
            {
                refund.Id,
                refund.RefundNumber,
                refund.AmountCents,
                refund.Status,
                refund.CreatedAt
            });
        });

        orders.MapGet("/{id:guid}/refunds", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var ownsOrder = await db.BookingOrders.AnyAsync(
                x => x.Id == id && x.WxUserId == userId.Value,
                cancellationToken);
            if (!ownsOrder)
            {
                return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            }

            var refunds = await db.RefundRecords.AsNoTracking()
                .Where(x => x.OrderId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.RefundNumber,
                    x.AmountCents,
                    x.Reason,
                    x.Status,
                    x.ReviewComment,
                    x.ProviderRefundNumber,
                    x.CreatedAt,
                    x.ReviewedAt,
                    x.CompletedAt
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(refunds);
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
