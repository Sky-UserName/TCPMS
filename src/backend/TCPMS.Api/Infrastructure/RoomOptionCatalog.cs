using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

public sealed record RoomCategoryOption(string Name, string Kind, int Count);

public sealed record RoomOptionCatalogData(
    IReadOnlyList<RoomCategoryOption> Categories,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Services,
    IReadOnlyList<string> Facilities,
    IReadOnlyList<string> BathroomFacilities,
    IReadOnlyList<string> NearbyFacilities);

/// <summary>
/// 房间表单需要的选项：房型分类直接来自 roomtypes 房源表，
/// 标签/服务/设施/周边设施来自管理端维护的 adminmanageditems 字典。
/// </summary>
public static class RoomOptionCatalog
{
    /// <summary>
    /// 根据房型分类名称推断房间类型：带「床位/多人间/宿舍…」判为多人间，
    /// 带「独立/单间/大床/房间…」判为独立房间，识别不出来才沿用原有 kind。
    /// </summary>
    public static string ResolveKindFromCategory(string? category, string fallback)
    {
        var name = category?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return fallback;
        }
        if (name.Contains("床位") || name.Contains("多人间") || name.Contains("宿舍") ||
            name.Contains("胶囊") || name.Contains("铺位") || name.Contains("青旅"))
        {
            return RoomTypeKinds.Bed;
        }
        if (name.Contains("独立") || name.Contains("单间") || name.Contains("标间") ||
            name.Contains("大床") || name.Contains("双床") || name.Contains("套房") ||
            name.Contains("家庭房") || name.Contains("房间"))
        {
            return RoomTypeKinds.PrivateRoom;
        }
        return fallback;
    }

    public static async Task<RoomOptionCatalogData> LoadAsync(
        AppDbContext db,
        Guid? storeId,
        CancellationToken cancellationToken)
    {
        var roomQuery = db.RoomTypes.AsNoTracking();
        if (storeId.HasValue)
        {
            roomQuery = roomQuery.Where(x => x.StoreId == storeId.Value);
        }

        var categoryRows = await roomQuery
            .Where(x => x.RoomCategory != null && x.RoomCategory != string.Empty)
            .GroupBy(x => new { x.RoomCategory, x.Kind })
            .Select(group => new
            {
                group.Key.RoomCategory,
                group.Key.Kind,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        // 分类的「类型」以名称为准：历史数据里出现过「独立房间」被记成 Bed 的情况，
        // 若直接沿用库里的 kind，管理端选「独立房间」也会被当成多人间，数量字段跟着错。
        var categories = categoryRows
            .Select(x => new RoomCategoryOption(x.RoomCategory!, ResolveKindFromCategory(x.RoomCategory, x.Kind), x.Count))
            .GroupBy(x => new { x.Name, x.Kind })
            .Select(group => new RoomCategoryOption(group.Key.Name, group.Key.Kind, group.Sum(x => x.Count)))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();

        var items = await db.AdminManagedItems.AsNoTracking()
            .Where(x => x.ResourceType == "tags" ||
                        x.ResourceType == "services" ||
                        x.ResourceType == "facilities" ||
                        x.ResourceType == "nearbyFacilities")
            .ToListAsync(cancellationToken);

        var entries = items
            .Select(item => new ManagedEntry(item.ResourceType, Parse(item.DataJson)))
            .Where(item => item.Data is { ValueKind: JsonValueKind.Object } && IsActive(item.Data.Value))
            .ToList();

        var facilityEntries = entries.Where(x => x.ResourceType == "facilities").ToList();
        var facilityNames = OrderedNames(facilityEntries
            .Where(x => !string.Equals(GetCategory(x.Data), "卫浴设施", StringComparison.OrdinalIgnoreCase)));
        var bathroomNames = OrderedNames(facilityEntries
            .Where(x => string.Equals(GetCategory(x.Data), "卫浴设施", StringComparison.OrdinalIgnoreCase)));

        return new RoomOptionCatalogData(
            categories,
            OrderedNames(entries.Where(x => x.ResourceType == "tags")),
            OrderedNames(entries.Where(x => x.ResourceType == "services")),
            facilityNames,
            bathroomNames,
            OrderedNames(entries.Where(x => x.ResourceType == "nearbyFacilities")));
    }

    private static List<string> OrderedNames(IEnumerable<ManagedEntry> entries)
    {
        return entries
            .OrderBy(x => GetSort(x.Data))
            .ThenBy(x => GetName(x.Data), StringComparer.OrdinalIgnoreCase)
            .Select(x => GetName(x.Data))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsActive(JsonElement data)
    {
        var status = GetString(data, "status");
        return string.IsNullOrWhiteSpace(status) ||
               status.Equals("active", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("enabled", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("normal", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetName(JsonElement? data) => data is null ? null : GetString(data.Value, "name");

    private static string? GetCategory(JsonElement? data) => data is null ? null : GetString(data.Value, "category");

    private static int GetSort(JsonElement? data)
    {
        if (data is null) return 0;
        if (data.Value.TryGetProperty("sort", out var sort) && sort.TryGetInt32(out var value)) return value;
        return 0;
    }

    private static string? GetString(JsonElement data, string property)
    {
        if (!data.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String) return null;
        return value.GetString();
    }

    private sealed record ManagedEntry(string ResourceType, JsonElement? Data);
}
