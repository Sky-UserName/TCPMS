using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

/// <summary>
/// 门店级预定须知（入住离店须知 + 取消退改通知）。
/// 复用 merchant_settings:{storeId} 配置，避免和商家设置页产生两份数据；
/// 没有配置时回退到该门店第一个房型的旧字段，保证升级后详情不空白。
/// </summary>
public static class StoreBookingNoticeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<StoreBookingNoticeDto> LoadAsync(
        AppDbContext db,
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var key = SettingsKey(storeId);
        var item = await db.AdminManagedItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ResourceType == key, cancellationToken);
        if (item is not null)
        {
            var stored = ParseSettings(item.DataJson);
            if (stored is not null)
            {
                var checkInNotice = string.IsNullOrWhiteSpace(stored.CheckInNotice)
                    ? ComposeCheckIn(stored.CheckIn, stored.CheckOut)
                    : stored.CheckInNotice;
                var cancelNotice = string.IsNullOrWhiteSpace(stored.CancelNotice)
                    ? ComposeCancel(stored.CancelRule, stored.JoinRule)
                    : stored.CancelNotice;
                if (!string.IsNullOrWhiteSpace(checkInNotice) || !string.IsNullOrWhiteSpace(cancelNotice))
                {
                    return Normalize(new StoreBookingNoticeDto(checkInNotice ?? string.Empty, cancelNotice ?? string.Empty));
                }
            }
        }

        var room = await db.RoomTypes.AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .OrderByDescending(x => x.IsPublished)
            .ThenBy(x => x.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
        if (room is not null)
        {
            return Normalize(new StoreBookingNoticeDto(
                ComposeCheckIn(room.CheckInTime, room.CheckOutTime),
                ComposeCancel(room.CancellationRule, room.JoinRule)));
        }

        return new StoreBookingNoticeDto(string.Empty, string.Empty);
    }

    public static async Task<StoreBookingNoticeDto> SaveAsync(
        AppDbContext db,
        Guid storeId,
        StoreBookingNoticeDto notice,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(notice);
        var key = SettingsKey(storeId);
        var item = await db.AdminManagedItems
            .SingleOrDefaultAsync(x => x.ResourceType == key, cancellationToken);
        if (item is null)
        {
            item = new AdminManagedItem
            {
                Id = Guid.NewGuid(),
                ResourceType = key,
                CreatedAt = DateTime.UtcNow
            };
            db.AdminManagedItems.Add(item);
        }

        var existing = ParseSettings(item.DataJson);
        var merged = new StoredSettings(
            normalized.CheckInNotice,
            normalized.CancelNotice,
            existing?.CheckIn,
            existing?.CheckOut,
            existing?.CancelRule,
            existing?.JoinRule,
            existing?.AutoAccept ?? true,
            existing?.ShowPhone ?? true);
        item.DataJson = JsonSerializer.Serialize(merged, JsonOptions);
        item.UpdatedAt = DateTime.UtcNow;
        return normalized;
    }

    public static StoreBookingNoticeDto Normalize(StoreBookingNoticeDto notice) => new(
        Clean(notice.CheckInNotice, 2000),
        Clean(notice.CancelNotice, 2000));

    public static bool Same(StoreBookingNoticeDto left, StoreBookingNoticeDto right) =>
        string.Equals(left.CheckInNotice, right.CheckInNotice, StringComparison.Ordinal) &&
        string.Equals(left.CancelNotice, right.CancelNotice, StringComparison.Ordinal);

    private static string SettingsKey(Guid storeId) => $"merchant_settings:{storeId}";

    private static string ComposeCheckIn(string? checkIn, string? checkOut)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(checkIn)) lines.Add($"入住时间：{checkIn.Trim()}");
        if (!string.IsNullOrWhiteSpace(checkOut)) lines.Add($"退房时间：{checkOut.Trim()}");
        return string.Join("\n", lines);
    }

    private static string ComposeCancel(string? cancelRule, string? joinRule)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(cancelRule)) lines.Add($"退订规则：{cancelRule.Trim()}");
        if (!string.IsNullOrWhiteSpace(joinRule)) lines.Add($"加入规则：{joinRule.Trim()}");
        return string.Join("\n", lines);
    }

    private static StoredSettings? ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<StoredSettings>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Clean(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private sealed record StoredSettings(
        string? CheckInNotice,
        string? CancelNotice,
        string? CheckIn,
        string? CheckOut,
        string? CancelRule,
        string? JoinRule,
        bool AutoAccept,
        bool ShowPhone);
}
