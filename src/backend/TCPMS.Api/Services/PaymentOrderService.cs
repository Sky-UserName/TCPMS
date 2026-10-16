using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

namespace TCPMS.Api.Services;

public static class PaymentOrderService
{
    public static async Task<bool> MarkOrderPaidAsync(
        AppDbContext db,
        BookingOrder order,
        string transactionNumber,
        string? rawCallback,
        CancellationToken cancellationToken)
    {
        var existingPayment = await db.PaymentRecords.SingleOrDefaultAsync(
            item => item.TransactionNumber == transactionNumber,
            cancellationToken);
        if (existingPayment is not null && existingPayment.OrderId != order.Id)
        {
            throw new InvalidOperationException("微信交易号已经关联其他订单");
        }

        if (order.PaymentStatus == "Paid" ||
            order.Status != OrderStatuses.PendingPayment)
        {
            return false;
        }

        var inventories = await db.InventoryDailies
            .Where(x => x.RoomTypeId == order.RoomTypeId &&
                        x.Date >= order.CheckIn &&
                        x.Date < order.CheckOut)
            .ToListAsync(cancellationToken);
        foreach (var inventory in inventories)
        {
            inventory.LockedQuantity = Math.Max(0, inventory.LockedQuantity - order.Quantity);
            inventory.SoldQuantity += order.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        order.Status = OrderStatuses.PaidPendingConfirmation;
        order.PaymentStatus = "Paid";
        order.UpdatedAt = DateTime.UtcNow;
        if (existingPayment is null)
        {
            db.PaymentRecords.Add(new PaymentRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TransactionNumber = transactionNumber,
                Provider = "WeChat",
                AmountCents = order.TotalAmountCents,
                Status = "Paid",
                RawCallback = rawCallback,
                PaidAt = DateTime.UtcNow
            });
        }
        return true;
    }
}
