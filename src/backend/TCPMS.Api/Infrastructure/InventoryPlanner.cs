using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

/// <summary>
/// 每日库存（InventoryDailies）的初始化与同步。
/// 房型只有一个「数量」口径：独立房间按 RoomCount、多人间床位按 BedCount，
/// 两者都会落到 RoomType.DefaultInventory，价格设置日历里的「总数」必须与它保持一致，
/// 否则会出现「房间管理改了数量、价格设置还是老数字」的问题。
///
/// 例外：价格设置里手动改过的日期（PriceCalendars.Source = Manual）属于「日期例外」，
/// 保存房型数量时原样保留，日历里的总数/剩余也按这一天的记录展示。
/// 种子数据自动生成的 Seed / Weekend 价格不算手动设置。
/// </summary>
public static class InventoryPlanner
{
    /// <summary>默认同步未来多少天的每日库存，够价格日历翻半年。</summary>
    public const int DefaultDays = 180;

    /// <summary>
    /// 确保 [start, start+days) 每一天都有一条库存记录，并把「没有单独设置过」的日期
    /// 对齐到房型当前数量。已有锁定/已售的日期不会被压到低于占用量，避免破坏在途订单。
    /// </summary>
    /// <param name="db">数据库上下文。</param>
    /// <param name="room">要同步的房型，数量取它的 DefaultInventory。</param>
    /// <param name="start">同步起始日期（含）。</param>
    /// <param name="days">从起始日期往后同步多少天。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task EnsureDailyInventoryAsync(
        AppDbContext db,
        RoomType room,
        DateTime start,
        int days,
        CancellationToken cancellationToken)
    {
        var from = start.Date;
        var end = from.AddDays(Math.Max(days, 1));
        var existing = await db.InventoryDailies
            .Where(x => x.RoomTypeId == room.Id && x.Date >= from && x.Date < end)
            .ToListAsync(cancellationToken);
        var map = existing.ToDictionary(x => x.Date.Date);
        var quantity = Math.Max(room.DefaultInventory, 0);

        // 价格设置里手动改过单价的日期是明确的例外，哪怕总数刚好等于房型数量也要保留。
        var customDates = (await db.PriceCalendars.AsNoTracking()
                .Where(x => x.RoomTypeId == room.Id &&
                            x.Date >= from &&
                            x.Date < end &&
                            (x.Source == null || (x.Source != "Seed" && x.Source != "Weekend")))
                .Select(x => x.Date)
                .ToListAsync(cancellationToken))
            .Select(x => x.Date)
            .ToHashSet();

        for (var date = from; date < end; date = date.AddDays(1))
        {
            if (!map.TryGetValue(date, out var inventory))
            {
                db.InventoryDailies.Add(new InventoryDaily
                {
                    Id = Guid.NewGuid(),
                    StoreId = room.StoreId,
                    RoomTypeId = room.Id,
                    Date = date,
                    TotalQuantity = quantity,
                    UpdatedAt = DateTime.UtcNow
                });
                continue;
            }

            var occupied = inventory.LockedQuantity + inventory.SoldQuantity;
            if (customDates.Contains(date))
            {
                // 日期例外：只保证不会低于已锁定/已售，其余数字按价格日历原样展示。
                if (inventory.TotalQuantity < occupied)
                {
                    inventory.TotalQuantity = occupied;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
                continue;
            }

            var nextTotal = Math.Max(quantity, occupied);
            if (inventory.TotalQuantity != nextTotal)
            {
                inventory.TotalQuantity = nextTotal;
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
