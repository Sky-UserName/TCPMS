using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

namespace TCPMS.Api;

/// <summary>
/// 小程序用户侧内容与资料接口：收藏、常用入住人、浏览记录、消息中心、攻略文章、门店筛选项。
/// 轻量结构化数据统一存放在 AdminManagedItems 的 JSON 行里，避免为原型阶段新增表结构。
/// </summary>
public static class MiniappAccountEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(RouteGroupBuilder api)
    {
        var me = api.MapGroup("/me")
            .RequireAuthorization("Miniapp")
            .WithTags("用户中心");

        me.MapGet("/summary", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var favorites = await ReadUserListAsync<FavoriteItem>(db, userId.Value, "favorites", cancellationToken);
            var guests = await ReadUserListAsync<GuestItem>(db, userId.Value, "guests", cancellationToken);
            var messages = await BuildMessagesAsync(db, userId.Value, cancellationToken);
            var recent = await ReadUserListAsync<RecentItem>(db, userId.Value, "recent", cancellationToken);

            // 统计仅用于「我的」页面的订单量、入住天数与未读消息，不涉及积分体系。
            var completedOrders = await db.BookingOrders.AsNoTracking()
                .Where(x => x.WxUserId == userId.Value && x.Status == OrderStatuses.Completed)
                .Select(x => new { x.CheckIn, x.CheckOut })
                .ToListAsync(cancellationToken);
            var stayNights = completedOrders.Sum(x => Math.Max(1, (x.CheckOut.Date - x.CheckIn.Date).Days));

            return Results.Ok(new
            {
                favoriteCount = favorites.Count,
                guestCount = guests.Count,
                recentCount = recent.Count,
                unreadMessageCount = messages.Count(x => x.Unread),
                orderCount = completedOrders.Count,
                stayNights
            });
        });

        me.MapGet("/favorites", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var items = await ReadUserListAsync<FavoriteItem>(db, userId.Value, "favorites", cancellationToken);
            return Results.Ok(items.OrderByDescending(x => x.UpdatedAt).ToList());
        });

        me.MapPost("/favorites", async (
            FavoriteItem request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return Results.BadRequest(new ApiError("validation_error", "收藏标识不能为空"));
            }

            var items = await ReadUserListAsync<FavoriteItem>(db, userId.Value, "favorites", cancellationToken);
            items.RemoveAll(x => x.Id == request.Id);
            items.Add(new FavoriteItem
            {
                Id = request.Id.Trim(),
                Type = request.Type,
                Title = request.Title,
                Subtitle = request.Subtitle,
                Image = request.Image,
                Path = request.Path,
                UpdatedAt = DateTime.UtcNow
            });
            await WriteUserListAsync(db, userId.Value, "favorites", items, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { favorited = true, count = items.Count });
        });

        me.MapDelete("/favorites/{id}", async (
            string id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var items = await ReadUserListAsync<FavoriteItem>(db, userId.Value, "favorites", cancellationToken);
            items.RemoveAll(x => x.Id == id);
            await WriteUserListAsync(db, userId.Value, "favorites", items, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { favorited = false, count = items.Count });
        });

        me.MapGet("/guests", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var items = await ReadUserListAsync<GuestItem>(db, userId.Value, "guests", cancellationToken);
            return Results.Ok(items);
        });

        me.MapPut("/guests", async (
            List<GuestItem> request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            if (request.Count > 10)
            {
                return Results.BadRequest(new ApiError("guest_limit", "最多保存10位入住人"));
            }
            foreach (var guest in request)
            {
                if (string.IsNullOrWhiteSpace(guest.Name) || string.IsNullOrWhiteSpace(guest.Identity))
                {
                    return Results.BadRequest(new ApiError("validation_error", "入住人姓名和证件号码不能为空"));
                }
            }

            // 只允许一个默认入住人，避免客户端状态不一致。
            var defaultIndex = request.FindIndex(x => x.IsDefault);
            for (var index = 0; index < request.Count; index++)
            {
                request[index].IsDefault = index == defaultIndex;
                request[index].Tag = request[index].IsDefault ? "默认入住人" : "同行人";
            }
            await WriteUserListAsync(db, userId.Value, "guests", request, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(request);
        });

        me.MapGet("/recent", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var items = await ReadUserListAsync<RecentItem>(db, userId.Value, "recent", cancellationToken);
            return Results.Ok(items.OrderByDescending(x => x.UpdatedAt).Take(20).ToList());
        });

        me.MapPost("/recent", async (
            RecentItem request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Id)) return Results.NoContent();
            var items = await ReadUserListAsync<RecentItem>(db, userId.Value, "recent", cancellationToken);
            items.RemoveAll(x => x.Type == request.Type && x.Id == request.Id);
            items.Add(new RecentItem
            {
                Id = request.Id.Trim(),
                Type = request.Type,
                Title = request.Title,
                Subtitle = request.Subtitle,
                Image = request.Image,
                Path = request.Path,
                UpdatedAt = DateTime.UtcNow
            });
            var trimmed = items.OrderByDescending(x => x.UpdatedAt).Take(20).ToList();
            await WriteUserListAsync(db, userId.Value, "recent", trimmed, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(trimmed);
        });

        me.MapDelete("/recent", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            await WriteUserListAsync(db, userId.Value, "recent", new List<RecentItem>(), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });

        me.MapGet("/messages", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var messages = await BuildMessagesAsync(db, userId.Value, cancellationToken);
            return Results.Ok(messages);
        });

        me.MapPost("/messages/{id}/read", async (
            string id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var readIds = await ReadJsonAsync<List<string>>(
                db,
                $"user_message_reads:{userId.Value}",
                cancellationToken) ?? [];
            if (!readIds.Contains(id))
            {
                readIds.Add(id);
                await WriteJsonAsync(db, $"user_message_reads:{userId.Value}", readIds, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            return Results.Ok(new { id, read = true });
        });

        api.MapGet("/articles", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var articles = await ReadArticlesAsync(db, cancellationToken);
            return Results.Ok(articles
                .OrderByDescending(x => x.PublishedAt)
                .Select(article => new
                {
                    id = article.Id,
                    title = article.Title,
                    subtitle = article.Subtitle,
                    coverImage = article.CoverImage,
                    author = article.Author,
                    publishedAt = article.PublishedAt,
                    readCount = article.ReadCount
                })
                .ToList());
        }).WithTags("内容");

        api.MapGet("/articles/{id}", async (
            string id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var articles = await ReadArticlesAsync(db, cancellationToken);
            var article = articles.FirstOrDefault(x => x.Id == id) ?? articles.FirstOrDefault();
            if (article is null)
            {
                return Results.NotFound(new ApiError("article_not_found", "文章不存在"));
            }

            Store? store = null;
            if (Guid.TryParse(article.StoreId, out var storeId))
            {
                store = await db.Stores.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == storeId, cancellationToken);
            }
            List<RoomType> storeRooms = store is null
                ? new List<RoomType>()
                : await db.RoomTypes.AsNoTracking()
                    .Where(x => x.StoreId == store.Id && x.IsPublished)
                    .OrderBy(x => x.BasePriceCents)
                    .Take(1)
                    .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                id = article.Id,
                title = article.Title,
                subtitle = article.Subtitle,
                author = article.Author,
                publishedAt = article.PublishedAt,
                readCount = article.ReadCount,
                coverImage = article.CoverImage,
                caption = article.Caption,
                sections = article.Sections,
                related = article.Related,
                store = store is null ? null : new
                {
                    id = store.Id,
                    name = store.Name,
                    address = store.Address,
                    image = store.CoverImageUrl ?? store.DoorImageUrl ?? string.Empty,
                    price = storeRooms.Count > 0
                        ? (storeRooms[0].BasePriceCents / 100m).ToString("0.##")
                        : "--",
                    rating = store.Rating
                },
                storeRoomId = storeRooms.Count > 0 ? storeRooms[0].Id : (Guid?)null
            });
        }).WithTags("内容");

        api.MapGet("/carousel", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var rows = await db.AdminManagedItems.AsNoTracking()
                .Where(x => x.ResourceType == "carousel")
                .OrderBy(x => x.UpdatedAt)
                .ThenBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(row =>
                {
                    CarouselConfigItem? config = null;
                    try
                    {
                        config = JsonSerializer.Deserialize<CarouselConfigItem>(row.DataJson, JsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed historical rows; one bad configuration must not blank the home page.
                    }
                    return new { row, config };
                })
                .Where(item => item.config is not null &&
                    string.Equals(item.config.Status, "active", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(item.config.Image))
                .OrderBy(item => item.config!.Sort)
                .ThenBy(item => item.row.UpdatedAt)
                .Select(item => new
                {
                    id = item.row.Id,
                    image = item.config!.Image,
                    badge = item.config.Badge,
                    title = item.config.Title,
                    subtitle = item.config.Subtitle,
                    link = item.config.Link
                })
                .ToList();

            return Results.Ok(items);
        }).WithTags("内容");

        api.MapGet("/pages/{key}", async (
            string key,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var page = await ReadJsonAsync<PageContentItem>(db, $"pages:{key}", cancellationToken);
            if (page is null || string.IsNullOrWhiteSpace(page.Key))
            {
                return Results.NotFound(new ApiError("page_not_found", "页面内容暂未配置"));
            }
            return Results.Ok(page);
        }).WithTags("内容");

        api.MapGet("/stores/filters", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var stores = await db.Stores.AsNoTracking()
                .Where(x => x.Status == StoreStatuses.Active)
                .OrderBy(x => x.SortOrder)
                .Select(x => new { x.City, x.District })
                .ToListAsync(cancellationToken);
            var cities = stores
                .Where(x => !string.IsNullOrWhiteSpace(x.City))
                .GroupBy(x => x.City!)
                .Select(group => new
                {
                    city = group.Key,
                    areas = group
                        .Select(x => x.District)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList()
                })
                .ToList();
            return Results.Ok(new { cities });
        }).WithTags("门店");
    }

    private static async Task<List<MessageItem>> BuildMessagesAsync(
        AppDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var orders = await db.BookingOrders.AsNoTracking()
            .Include(x => x.Store)
            .Include(x => x.RoomType)
            .Where(x => x.WxUserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var notices = await ReadJsonAsync<NoticeItem>(db, "notices", cancellationToken);
        var readIds = await ReadJsonAsync<List<string>>(
            db,
            $"user_message_reads:{userId}",
            cancellationToken) ?? [];

        var messages = new List<MessageItem>();
        if (notices is not null)
        {
            messages.Add(new MessageItem
            {
                Id = $"platform-{notices.Id}",
                Type = "platform",
                Icon = "support",
                Title = "平台公告",
                Summary = notices.Summary,
                Time = FormatCn(notices.PublishedAt, "yyyy.MM.dd HH:mm"),
                CreatedAt = notices.PublishedAt,
                Unread = !readIds.Contains($"platform-{notices.Id}")
            });
        }

        foreach (var order in orders)
        {
            var (title, summary, icon) = order.Status switch
            {
                OrderStatuses.PendingPayment => ("待支付提醒", "订单待支付，超时将自动释放库存", "clock"),
                OrderStatuses.PaidPendingVerification => ("订单消息", "订单已支付，请到店出示核销码", "receipt"),
                OrderStatuses.PaidPendingConfirmation or OrderStatuses.ConfirmedPendingCheckIn => ("订单消息", "订单已支付，请到店出示核销码", "receipt"),
                OrderStatuses.CheckedIn => ("入住提醒", "已办理入住，祝您入住愉快", "clock"),
                OrderStatuses.Completed => ("订单消息", "订单已完成，欢迎评价本次入住", "receipt"),
                OrderStatuses.Refunding => ("退款消息", "退款申请审核中，请留意进度", "wallet"),
                OrderStatuses.Refunded => ("退款消息", "退款已完成，请注意查收", "wallet"),
                OrderStatuses.RefundFailed => ("退款消息", "退款处理失败，请联系客服", "wallet"),
                OrderStatuses.Cancelled => ("订单消息", "订单已取消", "receipt"),
                _ => ("订单消息", "订单状态有更新，请及时查看", "receipt")
            };
            var id = $"order-{order.Id}";
            messages.Add(new MessageItem
            {
                Id = id,
                Type = order.Status == OrderStatuses.PaidPendingVerification ? "reminder" : "order",
                Icon = icon,
                Title = title,
                Summary = $"{order.RoomType?.Name ?? "房型"} · {order.Store?.Name ?? "门店"}：{summary}",
                Time = FormatCn(order.UpdatedAt, "yyyy.MM.dd HH:mm"),
                CreatedAt = order.UpdatedAt,
                Unread = !readIds.Contains(id)
            });
        }

        return messages
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToList();
    }

    private static async Task<List<ArticleItem>> ReadArticlesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var rows = await db.AdminManagedItems.AsNoTracking()
            .Where(x => x.ResourceType == "articles")
            .ToListAsync(cancellationToken);
        var articles = new List<ArticleItem>();
        foreach (var row in rows)
        {
            try
            {
                var article = JsonSerializer.Deserialize<ArticleItem>(row.DataJson, JsonOptions);
                if (article is not null && !string.IsNullOrWhiteSpace(article.Id))
                {
                    articles.Add(article);
                }
            }
            catch (JsonException)
            {
                // 单条脏数据不影响其余文章。
            }
        }
        return articles;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string FormatCn(DateTime value, string format)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return utc.AddHours(8).ToString(format);
    }

    private static async Task<List<T>> ReadUserListAsync<T>(
        AppDbContext db,
        Guid userId,
        string suffix,
        CancellationToken cancellationToken) =>
        await ReadJsonAsync<List<T>>(db, $"user_{suffix}:{userId}", cancellationToken) ?? [];

    private static async Task WriteUserListAsync<T>(
        AppDbContext db,
        Guid userId,
        string suffix,
        List<T> items,
        CancellationToken cancellationToken) =>
        await WriteJsonAsync(db, $"user_{suffix}:{userId}", items, cancellationToken);

    private static async Task<T?> ReadJsonAsync<T>(
        AppDbContext db,
        string resourceType,
        CancellationToken cancellationToken)
    {
        var item = await db.AdminManagedItems.AsNoTracking()
            .Where(x => x.ResourceType == resourceType)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null || string.IsNullOrWhiteSpace(item.DataJson)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(item.DataJson, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static async Task WriteJsonAsync<T>(
        AppDbContext db,
        string resourceType,
        T value,
        CancellationToken cancellationToken)
    {
        var item = await db.AdminManagedItems
            .Where(x => x.ResourceType == resourceType)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        if (item is null)
        {
            db.AdminManagedItems.Add(new AdminManagedItem
            {
                Id = Guid.NewGuid(),
                ResourceType = resourceType,
                DataJson = json,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return;
        }
        item.DataJson = json;
        item.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class FavoriteItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "store";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RecentItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "store";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GuestItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? Tag { get; set; }
}

public sealed class MessageItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "order";
    public string Icon { get; set; } = "receipt";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Unread { get; set; }
}

public sealed class NoticeItem
{
    public string Id { get; set; } = "notice-001";
    public string Title { get; set; } = "平台公告";
    public string Summary { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ArticleItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
    public int ReadCount { get; set; }
    public string CoverImage { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public List<ArticleSection> Sections { get; set; } = [];
    public List<ArticleRelated> Related { get; set; } = [];
}

public sealed class ArticleSection
{
    public string Type { get; set; } = "paragraph";
    public string Text { get; set; } = string.Empty;
}

public sealed class ArticleRelated
{
    public string Title { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string ArticleId { get; set; } = string.Empty;
}

/// <summary>
/// 静态说明类页面（服务协议、隐私政策、联系客服等）的内容结构，
/// 由管理端配置、小程序端渲染，避免把长文案写死在页面代码里。
/// </summary>
public sealed class PageContentItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Kicker { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string EffectiveAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string ServiceHours { get; set; } = string.Empty;
    public string SupportText { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<PageTab> Tabs { get; set; } = [];
    public List<PageHighlight> Highlights { get; set; } = [];
    public List<PageBullet> Points { get; set; } = [];
    public List<PageBlock> Blocks { get; set; } = [];
    public List<string> Notices { get; set; } = [];
}

public sealed class CarouselConfigItem
{
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public int Sort { get; set; }
    public string Status { get; set; } = "active";
}

public sealed class PageTab
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Kicker { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<PageHighlight> Highlights { get; set; } = [];
    public List<PageBlock> Blocks { get; set; } = [];
    public List<PagePermission> Permissions { get; set; } = [];
}

public sealed class PageBlock
{
    public string Type { get; set; } = "paragraph";
    public string Text { get; set; } = string.Empty;

    /// <summary>highlights 块的展示样式：rows（横排条目）或 cards（三列卡片）。</summary>
    public string Style { get; set; } = string.Empty;
}

public sealed class PageHighlight
{
    public string Icon { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

public sealed class PageBullet
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class PagePermission
{
    public string Icon { get; set; } = "user";
    public string Title { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public bool Required { get; set; }
}
