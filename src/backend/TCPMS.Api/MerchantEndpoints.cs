using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;
using TCPMS.Api.Services;

namespace TCPMS.Api;

/// <summary>
/// 小程序商家中心接口。商家身份通过门店归属人字段（OwnerUsername / OwnerPhone）与登录微信用户匹配，
/// 不额外引入商家账号表；房型、订单、评价、提现等核心数据仍落在原有业务表中。
/// </summary>
public static class MerchantEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly string[] PaidStatuses =
    [
        OrderStatuses.PaidPendingVerification,
        OrderStatuses.PaidPendingConfirmation,
        OrderStatuses.ConfirmedPendingCheckIn,
        OrderStatuses.CheckedIn
    ];

    private static readonly string[] CancelledStatuses =
    [
        OrderStatuses.Cancelled,
        OrderStatuses.Refunding,
        OrderStatuses.Refunded,
        OrderStatuses.RefundFailed
    ];

    public static void Map(RouteGroupBuilder api)
    {
        var merchant = api.MapGroup("/merchant")
            .RequireAuthorization("Miniapp")
            .WithTags("商家中心");

        merchant.MapGet("/profile", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null)
            {
                return Results.Unauthorized();
            }
            if (ctx.Store is null)
            {
                return Results.Ok(new
                {
                    enabled = false,
                    status = "unapplied",
                    storeId = string.Empty,
                    storeName = string.Empty,
                    role = string.Empty,
                    roomCount = 0,
                    pendingOrderCount = 0
                });
            }

            var status = ctx.Store.Status == StoreStatuses.Active
                ? "approved"
                : ctx.Store.Status == StoreStatuses.PendingReview
                    ? "pending"
                    : "rejected";

            var roomCount = await db.RoomTypes.CountAsync(x => x.StoreId == ctx.Store.Id, cancellationToken);
            var pendingOrderCount = await db.BookingOrders.CountAsync(
                x => x.StoreId == ctx.Store.Id &&
                     x.Status == OrderStatuses.PaidPendingVerification,
                cancellationToken);
            return Results.Ok(new
            {
                enabled = ctx.Store.Status == StoreStatuses.Active,
                status,
                storeId = ctx.Store.Id,
                storeName = ctx.Store.Name,
                role = "店主",
                roomCount,
                pendingOrderCount,
                city = ctx.Store.City,
                address = ctx.Store.Address,
                coverImage = ctx.Store.CoverImageUrl ?? ctx.Store.DoorImageUrl ?? string.Empty,
                ownerName = string.IsNullOrWhiteSpace(ctx.Store.OwnerName) ? "店主" : ctx.Store.OwnerName
            });
        });

        merchant.MapGet("/application", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null)
            {
                return Results.Ok(new
                {
                    exists = false,
                    status = "unapplied",
                    name = string.Empty,
                    ownerName = ctx.User.Nickname ?? string.Empty,
                    ownerPhone = ctx.User.PhoneNumber ?? string.Empty,
                    province = string.Empty,
                    city = string.Empty,
                    district = string.Empty,
                    address = string.Empty,
                    standardAddress = string.Empty,
                    longitude = (decimal?)null,
                    latitude = (decimal?)null,
                    description = string.Empty,
                    idCardFrontUrl = string.Empty,
                    idCardBackUrl = string.Empty,
                    propertyCertificateUrl = string.Empty,
                    businessLicenseUrl = string.Empty,
                    doorImageUrl = string.Empty,
                    roomImageUrls = Array.Empty<string>()
                });
            }

            return Results.Ok(ToApplicationResponse(ctx.Store));
        });

        merchant.MapPost("/application", async (
            MerchantApplicationRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();

            var validationError = ValidateApplicationRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validationError));
            }

            var openId = principal.FindFirstValue("openid") ?? ctx.User.OpenId;
            var store = ctx.Store;
            if (store is null)
            {
                store = new Store
                {
                    Id = Guid.NewGuid(),
                    Code = $"WX-{ctx.User.Id:N}",
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow
                };
                db.Stores.Add(store);
            }

            store.Name = request.Name.Trim();
            store.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            store.Province = Clean(request.Province);
            store.City = Clean(request.City);
            store.District = Clean(request.District);
            store.Address = Clean(request.Address);
            store.StandardAddress = Clean(request.StandardAddress);
            store.Phone = request.OwnerPhone.Trim();
            store.Longitude = request.Longitude;
            store.Latitude = request.Latitude;
            store.CoordinateSource = request.Longitude.HasValue && request.Latitude.HasValue ? "Miniapp" : null;
            store.CoordinatesUpdatedAt = request.Longitude.HasValue && request.Latitude.HasValue ? DateTime.UtcNow : null;
            store.OwnerUsername = openId;
            store.OwnerNickname = ctx.User.Nickname;
            store.OwnerAvatarUrl = ctx.User.AvatarUrl;
            store.OwnerName = request.OwnerName.Trim();
            store.OwnerPhone = request.OwnerPhone.Trim();
            store.IdCardFrontUrl = Clean(request.IdCardFrontUrl);
            store.IdCardBackUrl = Clean(request.IdCardBackUrl);
            store.PropertyCertificateUrl = Clean(request.PropertyCertificateUrl);
            store.BusinessLicenseUrl = Clean(request.BusinessLicenseUrl);
            store.DoorImageUrl = Clean(request.DoorImageUrl);
            store.RoomImageUrlsJson = NormalizeImageList(request.RoomImageUrlsJson);
            store.Status = StoreStatuses.PendingReview;
            store.UpdatedAt = DateTime.UtcNow;

            db.AuditLogs.Add(CreateAudit(principal, "merchant.application.submit", "Store", store.Id.ToString(), store.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToApplicationResponse(store));
        });

        merchant.MapGet("/booking-notice", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();
            var notice = await StoreBookingNoticeStore.LoadAsync(db, ctx.Store.Id, cancellationToken);
            return Results.Ok(new
            {
                status = ToStoreStatus(ctx.Store.Status),
                checkInNotice = notice.CheckInNotice,
                cancelNotice = notice.CancelNotice
            });
        });

        merchant.MapPut("/booking-notice", async (
            MerchantBookingNoticeRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var input = StoreBookingNoticeStore.Normalize(new StoreBookingNoticeDto(
                request.CheckInNotice ?? string.Empty,
                request.CancelNotice ?? string.Empty));
            var current = await StoreBookingNoticeStore.LoadAsync(db, ctx.Store.Id, cancellationToken);
            var changed = !StoreBookingNoticeStore.Same(current, input);
            if (changed)
            {
                await StoreBookingNoticeStore.SaveAsync(db, ctx.Store.Id, input, cancellationToken);
                ctx.Store.Status = StoreStatuses.PendingReview;
                ctx.Store.UpdatedAt = DateTime.UtcNow;
                db.AuditLogs.Add(CreateAudit(principal, "merchant.booking_notice.update", "Store", ctx.Store.Id.ToString(), ctx.Store.Name));
                await db.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(new
            {
                status = changed ? "pending" : ToStoreStatus(ctx.Store.Status),
                changed,
                checkInNotice = input.CheckInNotice,
                cancelNotice = input.CancelNotice
            });
        });

        merchant.MapPost("/application/upload", async (
            ClaimsPrincipal principal,
            IWebHostEnvironment environment,
            HttpRequest request,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue) return Results.Unauthorized();

            IFormFile? file;
            try
            {
                var form = await request.ReadFormAsync(cancellationToken);
                file = form.Files.GetFile("file");
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new ApiError("application_image_invalid", "图片上传内容无效"));
            }

            if (file is null || file.Length <= 0)
            {
                return Results.BadRequest(new ApiError("application_image_required", "请选择图片"));
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                return Results.BadRequest(new ApiError("application_image_too_large", "图片不能超过 10MB"));
            }

            var extension = await DetectImageExtensionAsync(file, cancellationToken);
            if (extension is null)
            {
                return Results.BadRequest(new ApiError("application_image_type_invalid", "图片必须是 JPG、PNG 或 WEBP 格式"));
            }

            var directoryPath = StoragePaths.Uploads(environment, "merchant-applications", userId.Value.ToString("N"));
            Directory.CreateDirectory(directoryPath);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
            var targetPath = Path.Combine(directoryPath, fileName);
            await using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return Results.Ok(new
            {
                url = $"{request.Scheme}://{request.Host}/uploads/merchant-applications/{userId.Value:N}/{fileName}",
                fileName,
                size = file.Length
            });
        });

        merchant.MapPost("/rooms/upload", async (
            ClaimsPrincipal principal,
            IWebHostEnvironment environment,
            HttpRequest request,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue) return Results.Unauthorized();

            IFormFile? file;
            try
            {
                var form = await request.ReadFormAsync(cancellationToken);
                file = form.Files.GetFile("file");
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new ApiError("room_image_invalid", "图片上传内容无效"));
            }

            if (file is null || file.Length <= 0)
            {
                return Results.BadRequest(new ApiError("room_image_required", "请选择图片"));
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                return Results.BadRequest(new ApiError("room_image_too_large", "图片不能超过 10MB"));
            }

            var extension = await DetectImageExtensionAsync(file, cancellationToken);
            if (extension is null)
            {
                return Results.BadRequest(new ApiError("room_image_type_invalid", "图片必须是 JPG、PNG 或 WEBP 格式"));
            }

            var directoryPath = StoragePaths.Uploads(environment, "merchant-rooms", userId.Value.ToString("N"));
            Directory.CreateDirectory(directoryPath);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
            var targetPath = Path.Combine(directoryPath, fileName);
            await using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return Results.Ok(new
            {
                url = $"{request.Scheme}://{request.Host}/uploads/merchant-rooms/{userId.Value:N}/{fileName}",
                fileName,
                size = file.Length
            });
        });

        merchant.MapGet("/room-options", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            var options = await RoomOptionCatalog.LoadAsync(db, ctx.Store?.Id, cancellationToken);
            return Results.Ok(options);
        });

        MapRooms(merchant);
        MapOrders(merchant);
        MapVerify(merchant);
        MapFinance(merchant);
        MapReviews(merchant);
        MapCoupons(merchant);
        MapStaff(merchant);
        MapSettings(merchant);
    }

    private static void MapRooms(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/rooms", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var rooms = await db.RoomTypes.AsNoTracking()
                .Where(x => x.StoreId == ctx.Store.Id)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            var stocks = await LoadStockAsync(db, rooms.Select(x => x.Id).ToList(), cancellationToken);
            return Results.Ok(rooms.Select(room => ToMerchantRoom(room, stocks)).ToList());
        });

        merchant.MapGet("/rooms/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var room = await db.RoomTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.StoreId == ctx.Store.Id, cancellationToken);
            if (room is null) return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            var stocks = await LoadStockAsync(db, [room.Id], cancellationToken);
            return Results.Ok(ToMerchantRoom(room, stocks));
        });

        merchant.MapPost("/rooms", async (
            MerchantRoomRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var error = ValidateRoomRequest(request);
            if (error is not null) return Results.BadRequest(new ApiError("validation_error", error));
            var name = request.Name!.Trim();
            if (await db.RoomTypes.AnyAsync(
                    x => x.StoreId == ctx.Store.Id && x.Name == name,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("room_type_exists", "已存在同名房型"));
            }

            var kind = NormalizeRoomKind(request.Kind, request.Type);
            var bedCount = kind == RoomTypeKinds.Bed
                ? Math.Max(request.Beds ?? request.Stock ?? 1, 1)
                : 1;
            var roomCount = kind == RoomTypeKinds.Bed
                ? 1
                : Math.Max(request.Rooms ?? 1, 1);
            var defaultInventory = kind == RoomTypeKinds.Bed
                ? bedCount
                : Math.Max(request.Stock ?? 1, 1);

            var room = new RoomType
            {
                Id = Guid.NewGuid(),
                StoreId = ctx.Store.Id,
                Name = name,
                LongName = request.Title?.Trim(),
                Kind = kind,
                RoomCategory = request.Type,
                Gender = kind == RoomTypeKinds.Bed ? request.Gender : null,
                BedCount = bedCount,
                MaxGuests = Math.Max(request.MaxGuests ?? 2, 1),
                BasePriceCents = request.PriceCents,
                Description = request.Description,
                FacilitiesJson = ToJson(request.Facilities),
                TagsJson = ToJson(request.Tags),
                ServicesJson = ToJson(request.Services),
                BathroomFacilitiesJson = ToJson(request.BathroomFacilities),
                NearbyFacilitiesJson = ToJson(request.NearbyFacilities),
                ImageUrlsJson = ToJson(NormalizeRoomImages(request.ImageUrls, request.ImageUrl)),
                CheckInTime = request.CheckIn,
                CheckOutTime = request.CheckOut,
                CancellationRule = request.CancelRule,
                Area = request.Area,
                Orientation = request.Orientation,
                Layout = request.Layout,
                RoomCount = roomCount,
                DefaultInventory = defaultInventory,
                IsDraft = !request.Published,
                IsPublished = request.Published,
                ApprovalStatus = request.Published ? "Approved" : "Draft",
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.RoomTypes.Add(room);
            await EnsureInventoryAsync(db, room, DateTime.Today, 180, cancellationToken);
            await EnsureRoomUnitsAsync(db, room, cancellationToken);
            db.AuditLogs.Add(CreateAudit(principal, "merchant.room.create", "RoomType", room.Id.ToString(), room.Name));
            await db.SaveChangesAsync(cancellationToken);
            var stocks = await LoadStockAsync(db, [room.Id], cancellationToken);
            return Results.Created($"/api/v1/merchant/rooms/{room.Id}", ToMerchantRoom(room, stocks));
        });

        merchant.MapPut("/rooms/{id:guid}", async (
            Guid id,
            MerchantRoomRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var room = await db.RoomTypes.SingleOrDefaultAsync(
                x => x.Id == id && x.StoreId == ctx.Store.Id,
                cancellationToken);
            if (room is null) return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            var error = ValidateRoomRequest(request);
            if (error is not null) return Results.BadRequest(new ApiError("validation_error", error));
            var name = request.Name!.Trim();
            if (await db.RoomTypes.AnyAsync(
                    x => x.StoreId == ctx.Store.Id && x.Name == name && x.Id != id,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("room_type_exists", "已存在同名房型"));
            }

            var kind = NormalizeRoomKind(request.Kind, request.Type);
            var bedCount = kind == RoomTypeKinds.Bed
                ? Math.Max(request.Beds ?? request.Stock ?? room.BedCount, 1)
                : 1;
            var roomCount = kind == RoomTypeKinds.Bed
                ? 1
                : Math.Max(request.Rooms ?? room.RoomCount, 1);
            var defaultInventory = kind == RoomTypeKinds.Bed
                ? bedCount
                : Math.Max(request.Stock ?? room.DefaultInventory, 1);

            room.Name = name;
            room.LongName = request.Title?.Trim();
            room.RoomCategory = request.Type;
            room.Kind = kind;
            room.Gender = kind == RoomTypeKinds.Bed ? request.Gender : null;
            room.BedCount = bedCount;
            room.MaxGuests = Math.Max(request.MaxGuests ?? room.MaxGuests, 1);
            room.BasePriceCents = request.PriceCents;
            room.Description = request.Description;
            room.FacilitiesJson = ToJson(request.Facilities);
            room.TagsJson = ToJson(request.Tags);
            room.ServicesJson = ToJson(request.Services);
            room.BathroomFacilitiesJson = ToJson(request.BathroomFacilities);
            room.NearbyFacilitiesJson = ToJson(request.NearbyFacilities);
            // 新版本按 imageUrls 全量覆盖（允许清空）；旧版本只传 imageUrl 时保持原行为。
            if (request.ImageUrls is not null)
            {
                room.ImageUrlsJson = ToJson(NormalizeRoomImages(request.ImageUrls, null));
            }
            else if (!string.IsNullOrWhiteSpace(request.ImageUrl) && IsRemoteImage(request.ImageUrl))
            {
                room.ImageUrlsJson = ToJson([request.ImageUrl.Trim()]);
            }
            room.CheckInTime = request.CheckIn ?? room.CheckInTime;
            room.CheckOutTime = request.CheckOut ?? room.CheckOutTime;
            room.CancellationRule = request.CancelRule ?? room.CancellationRule;
            room.Area = request.Area;
            room.Orientation = request.Orientation;
            room.Layout = request.Layout;
            room.RoomCount = roomCount;
            room.DefaultInventory = defaultInventory;
            room.IsDraft = !request.Published;
            room.IsPublished = request.Published;
            room.ApprovalStatus = request.Published ? "Approved" : "Draft";
            room.UpdatedAt = DateTime.UtcNow;
            await EnsureInventoryAsync(db, room, DateTime.Today, 180, cancellationToken);
            await EnsureRoomUnitsAsync(db, room, cancellationToken);
            db.AuditLogs.Add(CreateAudit(principal, "merchant.room.update", "RoomType", room.Id.ToString(), room.Name));
            await db.SaveChangesAsync(cancellationToken);
            var stocks = await LoadStockAsync(db, [room.Id], cancellationToken);
            return Results.Ok(ToMerchantRoom(room, stocks));
        });

        merchant.MapPost("/rooms/{id:guid}/publish", async (
            Guid id,
            bool published,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var room = await db.RoomTypes.SingleOrDefaultAsync(
                x => x.Id == id && x.StoreId == ctx.Store.Id,
                cancellationToken);
            if (room is null) return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));

            room.IsPublished = published;
            room.IsDraft = false;
            room.ApprovalStatus = published ? "Approved" : "Offline";
            room.UpdatedAt = DateTime.UtcNow;
            if (published)
            {
                await EnsureInventoryAsync(db, room, DateTime.Today, 180, cancellationToken);
            }
            db.AuditLogs.Add(CreateAudit(
                principal,
                published ? "merchant.room.publish" : "merchant.room.unpublish",
                "RoomType",
                room.Id.ToString(),
                room.Name));
            await db.SaveChangesAsync(cancellationToken);
            var stocks = await LoadStockAsync(db, [room.Id], cancellationToken);
            return Results.Ok(ToMerchantRoom(room, stocks));
        });
    }

    private static void MapOrders(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/orders", async (
            string? status,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var query = db.BookingOrders.AsNoTracking()
                .Include(x => x.RoomType)
                .Where(x => x.StoreId == ctx.Store.Id);
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statuses = status switch
                {
                    "pending" => new[] { OrderStatuses.PaidPendingVerification },
                    "stay" => new[] { OrderStatuses.CheckedIn },
                    "done" => new[] { OrderStatuses.Completed },
                    "cancel" => CancelledStatuses,
                    _ => Array.Empty<string>()
                };
                if (statuses.Length > 0)
                {
                    query = query.Where(x => statuses.Contains(x.Status));
                }
            }

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);
            return Results.Ok(orders.Select(ToMerchantOrder).ToList());
        });

        merchant.MapPost("/orders/{id:guid}/check-in", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var order = await db.BookingOrders
                .Include(x => x.RoomType)
                .SingleOrDefaultAsync(x => x.Id == id && x.StoreId == ctx.Store.Id, cancellationToken);
            if (order is null) return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            if (order.Status != OrderStatuses.ConfirmedPendingCheckIn)
            {
                return Results.Conflict(new ApiError("order_not_checkinable", "当前订单不允许办理入住"));
            }

            var units = await db.RoomUnits
                .Where(x => x.RoomTypeId == order.RoomTypeId && x.Status == "Available")
                .OrderBy(x => x.Code)
                .Take(order.Quantity)
                .ToListAsync(cancellationToken);
            if (units.Count < order.Quantity)
            {
                return Results.Conflict(new ApiError("resource_insufficient", "可用房间或床位不足，请先释放资源"));
            }
            foreach (var unit in units)
            {
                unit.Status = "Occupied";
                unit.UpdatedAt = DateTime.UtcNow;
            }

            order.Status = OrderStatuses.CheckedIn;
            order.CheckedInAt = DateTime.UtcNow;
            order.VerificationStatus = OrderVerificationStatuses.Verified;
            order.VerifiedAt = order.CheckedInAt;
            order.AssignedResourcesJson = JsonSerializer.Serialize(units.Select(x => x.Code).ToArray());
            order.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "merchant.order.check_in", "BookingOrder", order.Id.ToString(), order.OrderNumber));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToMerchantOrder(order));
        });

        merchant.MapPost("/orders/{id:guid}/check-out", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var order = await db.BookingOrders
                .Include(x => x.RoomType)
                .SingleOrDefaultAsync(x => x.Id == id && x.StoreId == ctx.Store.Id, cancellationToken);
            if (order is null) return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            if (order.Status != OrderStatuses.CheckedIn)
            {
                return Results.Conflict(new ApiError("order_not_checkoutable", "只有入住中的订单可以办理退房"));
            }

            var assigned = ParseStringArray(order.AssignedResourcesJson);
            if (assigned.Count > 0)
            {
                var units = await db.RoomUnits
                    .Where(x => x.RoomTypeId == order.RoomTypeId && assigned.Contains(x.Code))
                    .ToListAsync(cancellationToken);
                foreach (var unit in units)
                {
                    unit.Status = "Available";
                    unit.UpdatedAt = DateTime.UtcNow;
                }
            }

            var inventories = await db.InventoryDailies
                .Where(x => x.RoomTypeId == order.RoomTypeId &&
                            x.Date >= order.CheckIn.Date &&
                            x.Date < order.CheckOut.Date)
                .ToListAsync(cancellationToken);
            foreach (var inventory in inventories)
            {
                var locked = Math.Min(inventory.LockedQuantity, order.Quantity);
                inventory.LockedQuantity -= locked;
                inventory.SoldQuantity += locked;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            order.Status = OrderStatuses.Completed;
            order.CheckedOutAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "merchant.order.check_out", "BookingOrder", order.Id.ToString(), order.OrderNumber));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToMerchantOrder(order));
        });

        // 商家在待核销阶段直接取消：已支付的订单走微信原路退款，未支付订单只释放库存。
        merchant.MapPost("/orders/{id:guid}/cancel", async (
            Guid id,
            string? reason,
            AppDbContext db,
            ClaimsPrincipal principal,
            WeChatPayService weChatPayService,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var order = await db.BookingOrders
                .Include(x => x.RoomType)
                .SingleOrDefaultAsync(x => x.Id == id && x.StoreId == ctx.Store.Id, cancellationToken);
            if (order is null) return Results.NotFound(new ApiError("order_not_found", "订单不存在"));

            if (order.Status is OrderStatuses.Completed or OrderStatuses.CheckedIn)
            {
                return Results.Conflict(new ApiError("order_not_cancelable", "已核销的订单不能取消"));
            }
            if (order.Status is not (OrderStatuses.PendingPayment or
                OrderStatuses.PaidPendingVerification or
                OrderStatuses.PaidPendingConfirmation or
                OrderStatuses.ConfirmedPendingCheckIn))
            {
                return Results.Conflict(new ApiError("order_not_cancelable", "当前订单状态不能取消"));
            }

            var now = DateTime.UtcNow;
            var cancelReason = string.IsNullOrWhiteSpace(reason) ? "商家取消订单" : reason.Trim();
            if (order.PaymentStatus != "Paid")
            {
                await AdminEndpoints.ReleaseInventoryAsync(db, order, false, cancellationToken);
                order.Status = OrderStatuses.Cancelled;
                order.CancellationReason = cancelReason;
                order.UpdatedAt = now;
                db.AuditLogs.Add(CreateAudit(
                    principal,
                    "merchant.order.cancel",
                    "BookingOrder",
                    order.Id.ToString(),
                    cancelReason));
                await db.SaveChangesAsync(cancellationToken);
                return Results.Ok(ToMerchantOrder(order));
            }

            var paidPayments = await db.PaymentRecords.AsNoTracking()
                .Where(x => x.OrderId == order.Id && x.Status == "Paid")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            var refundAmount = paidPayments.Sum(x => x.AmountCents);
            if (refundAmount <= 0)
            {
                refundAmount = order.TotalAmountCents;
            }
            if (refundAmount <= 0)
            {
                return Results.Conflict(new ApiError("refund_amount_invalid", "订单没有可退回的支付金额"));
            }
            var hasRefund = await db.RefundRecords.AnyAsync(
                x => x.OrderId == order.Id && x.Status != "Rejected",
                cancellationToken);
            if (hasRefund)
            {
                return Results.Conflict(new ApiError("refund_exists", "该订单已有退款记录，请勿重复操作"));
            }

            var refund = new RefundRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                RefundNumber = $"RF{now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
                AmountCents = refundAmount,
                Reason = cancelReason,
                Status = "Refunded",
                OriginalOrderStatus = order.Status,
                RequestedByUserId = GetUserId(principal),
                ReviewedAt = now,
                CompletedAt = now,
                ReviewComment = "商家取消订单，原路退回"
            };

            var latestPayment = paidPayments.FirstOrDefault();
            var isRealWeChatPayment =
                latestPayment?.Provider == "WeChat" &&
                !latestPayment.TransactionNumber.StartsWith("MOCK", StringComparison.OrdinalIgnoreCase) &&
                !latestPayment.TransactionNumber.StartsWith("SEED_", StringComparison.OrdinalIgnoreCase);
            try
            {
                refund.ProviderRefundNumber = isRealWeChatPayment
                    ? await weChatPayService.CreateRefundAsync(order, refund, cancellationToken)
                    : $"MOCK_RF_{now:yyyyMMddHHmmssfff}";
            }
            catch (InvalidOperationException)
            {
                return Results.Json(
                    new ApiError("refund_config_incomplete", "微信退款配置不完整，暂时无法原路退回"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (WeChatPayApiException ex)
            {
                return Results.Json(
                    new ApiError("refund_rejected", $"微信退款未受理：{ex.Message}"),
                    statusCode: StatusCodes.Status502BadGateway);
            }
            catch (HttpRequestException)
            {
                return Results.Json(
                    new ApiError("refund_unavailable", "微信退款接口暂时不可用，请稍后重试"),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            db.RefundRecords.Add(refund);
            order.Status = OrderStatuses.Refunded;
            order.PaymentStatus = "Refunded";
            order.VerificationStatus = OrderVerificationStatuses.Unverified;
            order.VerifiedAt = null;
            order.CancellationReason = cancelReason;
            order.UpdatedAt = now;
            await AdminEndpoints.ReleaseInventoryAsync(db, order, true, cancellationToken);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "merchant.order.cancel_refund",
                "BookingOrder",
                order.Id.ToString(),
                $"{cancelReason}；退款 {(refundAmount / 100m):0.00} 元"));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToMerchantOrder(order));
        });
    }

    private static void MapVerify(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/verify-records", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var orders = await db.BookingOrders.AsNoTracking()
                .Include(x => x.RoomType)
                .Where(x => x.StoreId == ctx.Store.Id && x.VerificationStatus == OrderVerificationStatuses.Verified)
                .OrderByDescending(x => x.VerifiedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            return Results.Ok(orders.Select(order => new
            {
                id = order.Id,
                orderNumber = order.OrderNumber,
                code = order.OrderNumber,
                guest = BuildGuestText(order),
                roomName = order.RoomType?.Name ?? "已预订房型",
                storeName = ctx.Store.Name,
                status = "已核销",
                verifiedAt = order.VerifiedAt,
                time = FormatCn(order.VerifiedAt ?? order.UpdatedAt)
            }).ToList());
        });

        merchant.MapPost("/verify", async (
            MerchantVerifyRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var code = (request.Code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return Results.BadRequest(new ApiError("validation_error", "请输入订单号或核销码"));
            }

            var order = await db.BookingOrders
                .Include(x => x.RoomType)
                .SingleOrDefaultAsync(x =>
                    (x.VerificationCode == code || x.OrderNumber == code) &&
                    x.StoreId == ctx.Store.Id,
                    cancellationToken);
            if (order is null)
            {
                return Results.NotFound(new ApiError("order_not_found", "未找到该门店对应的订单"));
            }
            if (order.Status is OrderStatuses.CheckedIn or OrderStatuses.Completed)
            {
                return Results.Conflict(new ApiError("order_already_verified", "该订单已核销"));
            }
            if (order.PaymentStatus != "Paid" ||
                order.Status is not (OrderStatuses.PaidPendingVerification or
                    OrderStatuses.PaidPendingConfirmation or
                    OrderStatuses.ConfirmedPendingCheckIn))
            {
                return Results.Conflict(new ApiError("order_not_verifiable", "订单未支付、已核销或当前状态不能核销"));
            }

            var verifiedAt = DateTime.UtcNow;
            order.Status = OrderStatuses.Completed;
            order.CheckedInAt = verifiedAt;
            order.VerificationStatus = OrderVerificationStatuses.Verified;
            order.VerifiedAt = verifiedAt;
            order.UpdatedAt = verifiedAt;
            db.AuditLogs.Add(CreateAudit(principal, "merchant.order.verify", "BookingOrder", order.Id.ToString(), order.OrderNumber));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                id = order.Id,
                orderNumber = order.OrderNumber,
                code = order.OrderNumber,
                guest = BuildGuestText(order),
                roomName = order.RoomType?.Name ?? "已预订房型",
                storeName = ctx.Store.Name,
                status = "已核销",
                verifiedAt = order.VerifiedAt,
                time = FormatCn(order.VerifiedAt)
            });
        });
    }

    private static void MapFinance(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/finance", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var orders = await db.BookingOrders.AsNoTracking()
                .Where(x => x.StoreId == ctx.Store.Id)
                .ToListAsync(cancellationToken);
            var withdrawals = await db.WithdrawalRecords.AsNoTracking()
                .Where(x => x.StoreId == ctx.Store.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var completedCents = orders
                .Where(x => x.Status == OrderStatuses.Completed)
                .Sum(x => x.TotalAmountCents);
            var withdrawnCents = withdrawals
                .Where(x => x.Status is WithdrawalStatuses.Approved or WithdrawalStatuses.Completed)
                .Sum(x => x.AmountCents);
            var frozenCents = orders
                .Where(x => PaidStatuses.Contains(x.Status))
                .Sum(x => x.TotalAmountCents);

            return Results.Ok(new
            {
                today = BuildFinanceMetric(orders, now.Date),
                days7 = BuildFinanceMetric(orders, now.Date.AddDays(-6)),
                days30 = BuildFinanceMetric(orders, now.Date.AddDays(-29)),
                all = BuildFinanceMetric(orders, null),
                wallet = new
                {
                    frozenCents,
                    availableCents = Math.Max(0, completedCents - withdrawnCents),
                    withdrawnCents,
                    cancelledCents = orders
                        .Where(x => CancelledStatuses.Contains(x.Status))
                        .Sum(x => x.TotalAmountCents)
                },
                withdrawals = withdrawals.Take(20).Select(x => new
                {
                    id = x.Id,
                    amountCents = x.AmountCents,
                    status = x.Status switch
                    {
                        WithdrawalStatuses.PendingReview => "审核中",
                        WithdrawalStatuses.Approved => "已通过",
                        WithdrawalStatuses.Completed => "已到账",
                        _ => "已驳回"
                    },
                    createdAt = FormatCn(x.CreatedAt)
                }).ToList()
            });
        });

        merchant.MapPost("/withdrawals", async (
            MerchantWithdrawalRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var orders = await db.BookingOrders.AsNoTracking()
                .Where(x => x.StoreId == ctx.Store.Id && x.Status == OrderStatuses.Completed)
                .ToListAsync(cancellationToken);
            var withdrawals = await db.WithdrawalRecords.AsNoTracking()
                .Where(x => x.StoreId == ctx.Store.Id &&
                            (x.Status == WithdrawalStatuses.Approved || x.Status == WithdrawalStatuses.Completed))
                .ToListAsync(cancellationToken);
            var available = orders.Sum(x => x.TotalAmountCents) - withdrawals.Sum(x => x.AmountCents);
            if (request.AmountCents <= 0 || request.AmountCents > available)
            {
                return Results.BadRequest(new ApiError("withdrawal_amount_invalid", "提现金额超过可提现余额"));
            }

            var record = new WithdrawalRecord
            {
                Id = Guid.NewGuid(),
                StoreId = ctx.Store.Id,
                Account = string.IsNullOrWhiteSpace(request.Account) ? ctx.Store.Name : request.Account.Trim(),
                AmountCents = request.AmountCents,
                Status = WithdrawalStatuses.PendingReview,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.WithdrawalRecords.Add(record);
            db.AuditLogs.Add(CreateAudit(principal, "merchant.withdrawal.create", "WithdrawalRecord", record.Id.ToString(), record.AmountCents.ToString()));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                id = record.Id,
                amountCents = record.AmountCents,
                status = "审核中",
                createdAt = FormatCn(record.CreatedAt)
            });
        });
    }

    private static void MapReviews(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/reviews", async (
            string? status,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var normalizedStatus = NormalizeReviewStatus(status);
            if (!string.IsNullOrWhiteSpace(status) && normalizedStatus is null)
            {
                return Results.BadRequest(new ApiError("invalid_review_status", "评价状态不正确"));
            }

            var query = db.Reviews.AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Store)
                .Include(x => x.RoomType)
                .Include(x => x.Moderation)
                .Where(x => x.StoreId == ctx.Store.Id);

            if (normalizedStatus is not null)
            {
                query = normalizedStatus switch
                {
                    ReviewStatuses.Pending => query.Where(x =>
                        x.Moderation == null || x.Moderation.Status == ReviewStatuses.Pending),
                    ReviewStatuses.Hidden => query.Where(x =>
                        x.Moderation != null && x.Moderation.Status == ReviewStatuses.Hidden),
                    _ => query.Where(x =>
                        x.Moderation != null && x.Moderation.Status == ReviewStatuses.Published)
                };
            }

            var reviews = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);
            return Results.Ok(reviews.Select(review =>
            {
                var reviewStatus = ToMerchantReviewStatus(review.Moderation?.Status);
                return new
                {
                    id = review.Id,
                    orderId = review.OrderId,
                    guest = string.IsNullOrWhiteSpace(review.User?.Nickname) ? "微信用户" : review.User!.Nickname,
                    avatar = review.User?.AvatarUrl ?? string.Empty,
                    store = review.Store?.Name ?? ctx.Store.Name,
                    room = review.RoomType?.Name ?? "未知房型",
                    roomRating = review.RoomRating,
                    storeRating = review.StoreRating,
                    rating = review.RoomRating,
                    content = review.Content,
                    images = ParseReviewImages(review.ImageUrlsJson),
                    status = reviewStatus,
                    statusLabel = reviewStatus switch
                    {
                        "pending" => "待审核",
                        "published" => "已通过",
                        _ => "未通过"
                    },
                    reviewComment = review.Moderation?.ReviewComment ?? string.Empty,
                    date = FormatCn(review.CreatedAt, "yyyy-MM-dd"),
                    createdAt = review.CreatedAt
                };
            }).ToList());
        });

        merchant.MapPost("/reviews/{id:guid}/moderate", async (
            Guid id,
            MerchantReviewModerateRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var nextStatus = NormalizeReviewStatus(request.Status);
            if (nextStatus is not (ReviewStatuses.Published or ReviewStatuses.Hidden))
            {
                return Results.BadRequest(new ApiError("invalid_review_status", "审核结果只能是已通过或未通过"));
            }

            var review = await db.Reviews
                .Include(x => x.Moderation)
                .SingleOrDefaultAsync(
                    x => x.Id == id && x.StoreId == ctx.Store.Id,
                    cancellationToken);
            if (review is null) return Results.NotFound(new ApiError("review_not_found", "评价不存在"));

            var moderation = review.Moderation;
            if (moderation is null)
            {
                moderation = new ReviewModeration
                {
                    ReviewId = review.Id,
                    Review = review
                };
                db.ReviewModerations.Add(moderation);
                review.Moderation = moderation;
            }

            moderation.Status = nextStatus;
            moderation.ReviewedAt = DateTime.UtcNow;
            moderation.UpdatedAt = DateTime.UtcNow;
            var comment = (request.Comment ?? string.Empty).Trim();
            if (comment.Length > 0)
            {
                moderation.ReviewComment = comment.Length > 200 ? comment[..200] : comment;
            }

            db.AuditLogs.Add(CreateAudit(
                principal,
                nextStatus == ReviewStatuses.Published ? "merchant.review.approve" : "merchant.review.reject",
                "Review",
                review.Id.ToString(),
                review.Content));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { id = review.Id, status = ToMerchantReviewStatus(nextStatus) });
        });
    }

    private static void MapCoupons(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/coupons", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();
            var coupons = await ReadJsonAsync<List<MerchantCouponItem>>(
                db,
                $"merchant_coupons:{ctx.Store.Id}",
                cancellationToken) ?? [];
            return Results.Ok(coupons.OrderByDescending(x => x.CreatedAt).Select(ToCouponResponse).ToList());
        });

        merchant.MapPost("/coupons", async (
            MerchantCouponRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var coupons = await ReadJsonAsync<List<MerchantCouponItem>>(
                db,
                $"merchant_coupons:{ctx.Store.Id}",
                cancellationToken) ?? [];
            var item = new MerchantCouponItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(request.Name) ? $"优惠券{coupons.Count + 1}" : request.Name.Trim(),
                AmountCents = request.AmountCents > 0 ? request.AmountCents : 2000,
                Threshold = string.IsNullOrWhiteSpace(request.Threshold) ? "满200可用" : request.Threshold.Trim(),
                Stock = Math.Max(request.Stock ?? 20, 1),
                Used = 0,
                Status = "生效中",
                CreatedAt = DateTime.UtcNow
            };
            coupons.Add(item);
            await WriteJsonAsync(db, $"merchant_coupons:{ctx.Store.Id}", coupons, cancellationToken);
            db.AuditLogs.Add(CreateAudit(principal, "merchant.coupon.create", "Coupon", item.Id, item.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToCouponResponse(item));
        });

        merchant.MapPut("/coupons/{id}", async (
            string id,
            MerchantCouponUpdateRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var coupons = await ReadJsonAsync<List<MerchantCouponItem>>(
                db,
                $"merchant_coupons:{ctx.Store.Id}",
                cancellationToken) ?? [];
            var item = coupons.FirstOrDefault(x => x.Id == id);
            if (item is null) return Results.NotFound(new ApiError("coupon_not_found", "优惠券不存在"));
            if (!string.IsNullOrWhiteSpace(request.Status)) item.Status = request.Status.Trim();
            if (!string.IsNullOrWhiteSpace(request.Name)) item.Name = request.Name.Trim();
            if (request.AmountCents > 0) item.AmountCents = request.AmountCents.Value;
            if (request.Stock.HasValue) item.Stock = Math.Max(request.Stock.Value, 0);
            await WriteJsonAsync(db, $"merchant_coupons:{ctx.Store.Id}", coupons, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToCouponResponse(item));
        });
    }

    private static void MapStaff(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/staff", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var admins = await db.AdminUsers.AsNoTracking()
                .Include(x => x.StoreStaffProfile)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Where(x => x.StoreId == ctx.Store.Id)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            var items = new List<object>
            {
                new
                {
                    id = "owner",
                    name = string.IsNullOrWhiteSpace(ctx.Store.OwnerName) ? "店主" : ctx.Store.OwnerName,
                    username = ctx.Store.OwnerUsername ?? "owner",
                    phone = MaskPhone(ctx.Store.OwnerPhone ?? ctx.Store.Phone),
                    rawPhone = ctx.Store.OwnerPhone ?? ctx.Store.Phone ?? string.Empty,
                    gender = "未知",
                    role = "店主",
                    status = "正常",
                    storeId = ctx.Store.Id,
                    storeName = ctx.Store.Name,
                    permissions = Array.Empty<string>(),
                    memberId = (Guid?)null,
                    isOwner = true
                }
            };
            items.AddRange(admins.Select(admin => new
            {
                id = admin.Id,
                name = string.IsNullOrWhiteSpace(admin.DisplayName) ? admin.Username : admin.DisplayName,
                username = admin.Username,
                phone = MaskPhone(admin.PhoneNumber),
                rawPhone = admin.PhoneNumber ?? string.Empty,
                gender = admin.StoreStaffProfile?.Gender ?? "未知",
                role = admin.UserRoles.Any(x => x.Role?.Code == RoleCodes.StoreAdmin) ? "店长" : "前台",
                status = admin.IsEnabled ? "正常" : "已停用",
                storeId = admin.StoreId,
                storeName = ctx.Store.Name,
                permissions = ParsePermissionKeys(admin.StoreStaffProfile?.PermissionKeysJson),
                memberId = admin.WxUserId,
                isOwner = false
            }));
            return Results.Ok(items);
        });

        // 添加员工弹层需要的可选项：当前账号名下的门店（默认选中当前民宿）和启用中的权限菜单。
        merchant.MapGet("/staff/options", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var openId = principal.FindFirstValue("openid") ?? ctx.User.OpenId;
            var ownerPhone = ctx.User.PhoneNumber;
            var stores = await db.Stores.AsNoTracking()
                .Where(x => x.Status != StoreStatuses.Disabled &&
                            (x.OwnerUsername == openId ||
                             (!string.IsNullOrWhiteSpace(ownerPhone) && x.OwnerPhone == ownerPhone)))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new { id = x.Id, name = x.Name })
                .ToListAsync(cancellationToken);
            if (!stores.Any(x => x.id == ctx.Store.Id))
            {
                stores.Insert(0, new { id = ctx.Store.Id, name = ctx.Store.Name });
            }

            var permissions = await db.PermissionMenus.AsNoTracking()
                .Where(x => x.Status == "normal")
                .OrderBy(x => x.Weight)
                .ThenBy(x => x.Title)
                .Select(x => new { key = x.Key, title = x.Title, route = x.Route, iconKey = x.IconKey })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                currentStoreId = ctx.Store.Id,
                currentStoreName = ctx.Store.Name,
                stores,
                permissions
            });
        });

        // 输入手机号时按 4 位以上片段搜索会员：手机号优先，同时兼容昵称搜索。
        merchant.MapGet("/members/search", async (
            string? keyword,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var text = (keyword ?? string.Empty).Trim();
            // 与管理端约定一致：手机号输入满 4 位才发起搜索。
            if (text.Length < 4)
            {
                return Results.Ok(Array.Empty<object>());
            }

            var members = await db.WxUsers.AsNoTracking()
                .Where(x => x.IsEnabled &&
                            ((x.PhoneNumber != null && x.PhoneNumber.Contains(text)) ||
                             (x.Nickname != null && x.Nickname.Contains(text))))
                .OrderByDescending(x => x.LastLoginAt ?? x.CreatedAt)
                .Take(10)
                .Select(x => new
                {
                    id = x.Id,
                    nickname = x.Nickname,
                    phone = x.PhoneNumber,
                    gender = x.Gender,
                    avatar = x.AvatarUrl
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(members);
        });

        merchant.MapPost("/staff", async (
            MerchantStaffRequest request,
            AppDbContext db,
            IPasswordHasher<AdminUser> passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(new ApiError("validation_error", "员工名称不能为空"));
            }
            var phoneText = (request.Phone ?? string.Empty).Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneText, @"^1[3-9]\d{9}$"))
            {
                return Results.BadRequest(new ApiError("validation_error", "请填写正确的11位手机号"));
            }

            var storeId = request.StoreId ?? ctx.Store.Id;
            var ownerOpenId = principal.FindFirstValue("openid") ?? ctx.User.OpenId;
            var ownerPhone = ctx.User.PhoneNumber;
            var store = await db.Stores.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == storeId && x.Status != StoreStatuses.Disabled,
                    cancellationToken);
            if (store is null)
            {
                return Results.NotFound(new ApiError("store_not_found", "民宿不存在"));
            }
            if (store.OwnerUsername != ownerOpenId &&
                (string.IsNullOrWhiteSpace(ownerPhone) || store.OwnerPhone != ownerPhone))
            {
                return Results.Forbid();
            }

            if (await db.AdminUsers.AnyAsync(
                    x => x.StoreId == storeId && x.PhoneNumber == phoneText,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("staff_phone_exists", "该手机号已经是这家民宿的员工"));
            }
            if (request.MemberId is Guid memberId)
            {
                if (!await db.WxUsers.AnyAsync(x => x.Id == memberId, cancellationToken))
                {
                    return Results.NotFound(new ApiError("member_not_found", "所选会员不存在，请重新选择"));
                }
                if (await db.AdminUsers.AnyAsync(
                        x => x.WxUserId == memberId && x.StoreId == storeId,
                        cancellationToken))
                {
                    return Results.Conflict(new ApiError("staff_member_exists", "该会员已经是这家民宿的员工"));
                }
            }

            var validKeys = await db.PermissionMenus.AsNoTracking()
                .Where(x => x.Status == "normal")
                .Select(x => x.Key)
                .ToListAsync(cancellationToken);
            var permissionKeys = (request.Permissions ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key) && validKeys.Contains(key))
                .Distinct()
                .ToArray();

            var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.StoreStaff, cancellationToken);
            var username = $"staff_{Guid.NewGuid():N}"[..14];
            while (await db.AdminUsers.AnyAsync(x => x.Username == username, cancellationToken))
            {
                username = $"staff_{Guid.NewGuid():N}"[..14];
            }

            var admin = new AdminUser
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = name,
                PhoneNumber = phoneText,
                WxUserId = request.MemberId,
                StoreId = storeId,
                IsEnabled = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123456");
            db.AdminUsers.Add(admin);
            db.AdminUserRoles.Add(new AdminUserRole { AdminUserId = admin.Id, RoleId = role.Id });
            db.StoreStaffProfiles.Add(new StoreStaffProfile
            {
                AdminUserId = admin.Id,
                Gender = NormalizeStaffGender(request.Gender),
                PermissionKeysJson = JsonSerializer.Serialize(permissionKeys),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.AuditLogs.Add(CreateAudit(principal, "merchant.staff.create", "AdminUser", admin.Id.ToString(), name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                id = admin.Id,
                name = admin.DisplayName,
                username = admin.Username,
                phone = MaskPhone(admin.PhoneNumber),
                rawPhone = admin.PhoneNumber ?? string.Empty,
                gender = NormalizeStaffGender(request.Gender),
                role = "前台",
                status = "正常",
                storeId = store.Id,
                storeName = store.Name,
                permissions = permissionKeys,
                memberId = admin.WxUserId,
                isOwner = false
            });
        });
    }

    private static string NormalizeStaffGender(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "男" or "male" => "男",
            "女" or "female" => "女",
            _ => "未知"
        };
    }

    private static IReadOnlyList<string> ParsePermissionKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void MapSettings(RouteGroupBuilder merchant)
    {
        merchant.MapGet("/settings", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();
            var extra = await ReadJsonAsync<MerchantSettingsItem>(
                db,
                $"merchant_settings:{ctx.Store.Id}",
                cancellationToken) ?? new MerchantSettingsItem();
            return Results.Ok(new
            {
                storeName = ctx.Store.Name,
                phone = ctx.Store.Phone ?? string.Empty,
                checkIn = string.IsNullOrWhiteSpace(extra.CheckIn) ? "14:00" : extra.CheckIn,
                checkOut = string.IsNullOrWhiteSpace(extra.CheckOut) ? "12:00" : extra.CheckOut,
                cancelRule = extra.CancelRule,
                joinRule = extra.JoinRule,
                checkInNotice = extra.CheckInNotice,
                cancelNotice = extra.CancelNotice,
                autoAccept = extra.AutoAccept,
                showPhone = extra.ShowPhone
            });
        });

        merchant.MapPut("/settings", async (
            MerchantSettingsRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();

            var name = (request.StoreName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(new ApiError("validation_error", "民宿名称不能为空"));
            }
            var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            var profileChanged = !string.Equals(ctx.Store.Name, name, StringComparison.Ordinal) ||
                                 !string.Equals(ctx.Store.Phone, phone, StringComparison.Ordinal);
            ctx.Store.Name = name;
            ctx.Store.Phone = phone;
            if (profileChanged)
            {
                ctx.Store.Status = StoreStatuses.PendingReview;
            }
            ctx.Store.UpdatedAt = DateTime.UtcNow;

            var existing = await ReadJsonAsync<MerchantSettingsItem>(
                db,
                $"merchant_settings:{ctx.Store.Id}",
                cancellationToken);
            if (existing is null)
            {
                var notice = await StoreBookingNoticeStore.LoadAsync(db, ctx.Store.Id, cancellationToken);
                existing = new MerchantSettingsItem
                {
                    CheckInNotice = notice.CheckInNotice,
                    CancelNotice = notice.CancelNotice
                };
            }
            var extra = new MerchantSettingsItem
            {
                CheckIn = string.IsNullOrWhiteSpace(request.CheckIn) ? existing.CheckIn : request.CheckIn,
                CheckOut = string.IsNullOrWhiteSpace(request.CheckOut) ? existing.CheckOut : request.CheckOut,
                CancelRule = request.CancelRule ?? existing.CancelRule,
                JoinRule = existing.JoinRule,
                CheckInNotice = existing.CheckInNotice,
                CancelNotice = existing.CancelNotice,
                AutoAccept = request.AutoAccept,
                ShowPhone = request.ShowPhone
            };
            await WriteJsonAsync(db, $"merchant_settings:{ctx.Store.Id}", extra, cancellationToken);
            db.AuditLogs.Add(CreateAudit(principal, "merchant.settings.update", "Store", ctx.Store.Id.ToString(), name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                storeName = ctx.Store.Name,
                phone = ctx.Store.Phone ?? string.Empty,
                checkIn = extra.CheckIn,
                checkOut = extra.CheckOut,
                cancelRule = extra.CancelRule,
                joinRule = extra.JoinRule,
                checkInNotice = extra.CheckInNotice,
                cancelNotice = extra.CancelNotice,
                autoAccept = extra.AutoAccept,
                showPhone = extra.ShowPhone
            });
        });

        merchant.MapGet("/message-settings", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();
            var settings = await ReadJsonAsync<List<MerchantMessageSettingItem>>(
                db,
                $"merchant_message_settings:{ctx.Store.Id}",
                cancellationToken) ?? DefaultMessageSettings();
            return Results.Ok(settings);
        });

        merchant.MapPut("/message-settings", async (
            List<MerchantMessageSettingItem> settings,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var ctx = await ResolveMerchantAsync(db, principal, cancellationToken);
            if (ctx.User is null) return Results.Unauthorized();
            if (ctx.Store is null) return MerchantNotBound();
            await WriteJsonAsync(db, $"merchant_message_settings:{ctx.Store.Id}", settings, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(settings);
        });
    }

    // ---------- 辅助方法 ----------

    private static object ToApplicationResponse(Store store)
    {
        var status = store.Status == StoreStatuses.PendingReview
            ? "pending"
            : store.Status == StoreStatuses.Active
                ? "approved"
                : "rejected";
        return new
        {
            exists = true,
            status,
            name = store.Name,
            ownerName = store.OwnerName ?? string.Empty,
            ownerPhone = store.OwnerPhone ?? store.Phone ?? string.Empty,
            province = store.Province ?? string.Empty,
            city = store.City ?? string.Empty,
            district = store.District ?? string.Empty,
            address = store.Address ?? string.Empty,
            standardAddress = store.StandardAddress ?? string.Empty,
            longitude = store.Longitude,
            latitude = store.Latitude,
            description = store.Description ?? string.Empty,
            idCardFrontUrl = store.IdCardFrontUrl ?? string.Empty,
            idCardBackUrl = store.IdCardBackUrl ?? string.Empty,
            propertyCertificateUrl = store.PropertyCertificateUrl ?? string.Empty,
            businessLicenseUrl = store.BusinessLicenseUrl ?? string.Empty,
            doorImageUrl = store.DoorImageUrl ?? string.Empty,
            roomImageUrls = ParseStringArray(store.RoomImageUrlsJson)
        };
    }

    private static string ToStoreStatus(string status) =>
        status == StoreStatuses.Active
            ? "approved"
            : status == StoreStatuses.PendingReview
                ? "pending"
                : "rejected";

    private static string? ValidateApplicationRequest(MerchantApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "请填写民宿名称";
        if (request.Name.Trim().Length > 80) return "民宿名称不能超过80个字";
        if (string.IsNullOrWhiteSpace(request.OwnerName)) return "请填写房东姓名";
        if (request.OwnerName.Trim().Length > 40) return "房东姓名不能超过40个字";
        if (string.IsNullOrWhiteSpace(request.OwnerPhone)) return "请填写房东电话";
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.OwnerPhone.Trim(), @"^1[3-9]\d{9}$")) return "请输入正确的手机号码";
        if (string.IsNullOrWhiteSpace(request.Address)) return "请选择民宿位置并填写详细地址";
        if (string.IsNullOrWhiteSpace(request.IdCardFrontUrl) || string.IsNullOrWhiteSpace(request.IdCardBackUrl)) return "请上传身份证正反面";
        if (string.IsNullOrWhiteSpace(request.PropertyCertificateUrl)) return "请上传房产证或承包协议";
        if (string.IsNullOrWhiteSpace(request.BusinessLicenseUrl)) return "请上传营业执照";
        if (string.IsNullOrWhiteSpace(request.DoorImageUrl)) return "请上传门头照片";
        if (ParseStringArray(request.RoomImageUrlsJson).Count == 0) return "请至少上传一张房间照片";
        if (request.Longitude.HasValue != request.Latitude.HasValue) return "定位坐标不完整，请重新选择位置";
        return null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeImageList(string? value)
    {
        var list = ParseStringArray(value);
        return list.Count == 0 ? "[]" : JsonSerializer.Serialize(list.Take(12).ToList(), JsonOptions);
    }

    private static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) return ".png";
        if (read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P') return ".webp";
        return null;
    }

    private sealed record MerchantContext(WxUser? User, Store? Store);

    private static async Task<MerchantContext> ResolveMerchantAsync(
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null) return new MerchantContext(null, null);
        var user = await db.WxUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null) return new MerchantContext(null, null);

        var openId = principal.FindFirstValue("openid") ?? user.OpenId;
        var phone = user.PhoneNumber;
        var store = await db.Stores
            .Where(x => x.Status != StoreStatuses.Disabled &&
                        (x.OwnerUsername == openId ||
                         (!string.IsNullOrWhiteSpace(phone) && x.OwnerPhone == phone)))
            .OrderBy(x => x.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
        return new MerchantContext(user, store);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static IResult MerchantNotBound() =>
        Results.Json(
            new ApiError("merchant_not_bound", "当前微信账号尚未绑定门店，请先在管理端设置门店归属人"),
            statusCode: StatusCodes.Status403Forbidden);

    private static string NormalizeRoomKind(string? kind, string? category)
    {
        if (string.Equals(kind, RoomTypeKinds.Bed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(kind, "多人间床位", StringComparison.OrdinalIgnoreCase))
        {
            return RoomTypeKinds.Bed;
        }
        if (string.Equals(kind, RoomTypeKinds.PrivateRoom, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(kind, "独立房间", StringComparison.OrdinalIgnoreCase))
        {
            return RoomTypeKinds.PrivateRoom;
        }
        return category?.Contains("床位") == true ? RoomTypeKinds.Bed : RoomTypeKinds.PrivateRoom;
    }

    private static string? ValidateRoomRequest(MerchantRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "请填写房间名称";
        if (request.PriceCents <= 0) return "请填写正确的房价";
        if (request.Stock is <= 0) return "库存必须大于 0";
        if (request.MaxGuests is <= 0) return "可住人数必须大于 0";
        return null;
    }

    private static async Task<Dictionary<Guid, int>> LoadStockAsync(
        AppDbContext db,
        List<Guid> roomTypeIds,
        CancellationToken cancellationToken)
    {
        if (roomTypeIds.Count == 0) return [];
        var start = DateTime.Today;
        var end = start.AddDays(14);
        var rows = await db.InventoryDailies.AsNoTracking()
            .Where(x => roomTypeIds.Contains(x.RoomTypeId) && x.Date >= start && x.Date < end)
            .GroupBy(x => x.RoomTypeId)
            .Select(group => new
            {
                RoomTypeId = group.Key,
                Stock = group.Min(x => x.TotalQuantity - x.LockedQuantity - x.SoldQuantity)
            })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.RoomTypeId, x => Math.Max(x.Stock, 0));
    }

    private static object ToMerchantRoom(RoomType room, Dictionary<Guid, int> stocks)
    {
        var status = room.IsDraft ? "草稿" : room.IsPublished ? "已上架" : "已下架";
        var key = room.IsDraft ? "draft" : room.IsPublished ? "online" : "offline";
        var image = ParseStringArray(room.ImageUrlsJson).FirstOrDefault() ?? string.Empty;
        var images = ParseStringArray(room.ImageUrlsJson);
        var stock = stocks.TryGetValue(room.Id, out var value) ? value : room.DefaultInventory;
        var capacity = room.Kind == RoomTypeKinds.Bed
            ? $"可住{room.MaxGuests}人 · {room.BedCount}张床位"
            : $"可住{room.MaxGuests}人 · {room.BedCount}张床";
        return new
        {
            id = room.Id,
            name = room.Name,
            title = room.LongName ?? room.Name,
            type = room.RoomCategory ?? (room.Kind == RoomTypeKinds.Bed ? "多人床位" : "独立房间"),
            kind = room.Kind,
            roomCategory = room.RoomCategory ?? string.Empty,
            gender = room.Gender,
            status,
            key,
            price = (room.BasePriceCents / 100m).ToString("0.##"),
            priceCents = room.BasePriceCents,
            stock = stock.ToString(),
            capacity,
            area = room.Area.HasValue ? $"{room.Area:0.#}㎡" : "待完善",
            image,
            images,
            roomCount = room.RoomCount,
            bedCount = room.BedCount,
            maxGuests = room.MaxGuests,
            orientation = room.Orientation,
            layout = room.Layout,
            checkIn = room.CheckInTime,
            checkOut = room.CheckOutTime,
            cancelRule = room.CancellationRule,
            description = room.Description,
            tags = ParseStringArray(room.TagsJson),
            services = ParseStringArray(room.ServicesJson),
            facilities = ParseStringArray(room.FacilitiesJson),
            bathroomFacilities = ParseStringArray(room.BathroomFacilitiesJson),
            nearbyFacilities = ParseStringArray(room.NearbyFacilitiesJson),
            updatedAt = FormatCn(room.UpdatedAt, "yyyy-MM-dd HH:mm")
        };
    }

    private static object ToMerchantOrder(BookingOrder order)
    {
        var (status, statusKey) = order.Status switch
        {
            OrderStatuses.PendingPayment => ("待支付", "pay"),
            OrderStatuses.PaidPendingVerification => ("待核销", "pending"),
            OrderStatuses.PaidPendingConfirmation or OrderStatuses.ConfirmedPendingCheckIn => ("待核销", "pending"),
            // 入住中已停用：核销即完成，历史 CheckedIn 数据按已完成展示，避免页面再出现旧状态。
            OrderStatuses.CheckedIn or OrderStatuses.Completed => ("已完成", "done"),
            OrderStatuses.Refunding => ("退款审核中", "cancel"),
            OrderStatuses.Refunded => ("已退款", "cancel"),
            OrderStatuses.RefundFailed => ("退款失败", "cancel"),
            OrderStatuses.Cancelled => ("已取消", "cancel"),
            _ => (order.Status, "pending")
        };
        var images = ParseStringArray(order.RoomType?.ImageUrlsJson);
        var coverImage = images.FirstOrDefault() ?? string.Empty;
        var image = string.IsNullOrWhiteSpace(coverImage)
            ? string.Empty
            : coverImage.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                ? $"/room-images/{order.RoomTypeId}/0"
                : coverImage;
        return new
        {
            id = order.OrderNumber,
            orderId = order.Id,
            roomName = order.RoomType?.Name ?? "已预订房型",
            guest = BuildGuestText(order),
            date = $"{order.CheckIn:MM月dd日} - {order.CheckOut:MM月dd日}",
            amount = (order.TotalAmountCents / 100m).ToString("0.00"),
            amountCents = order.TotalAmountCents,
            status,
            statusKey,
            paymentStatus = order.PaymentStatus,
            image,
            createdAt = FormatCn(order.CreatedAt, "yyyy-MM-dd HH:mm"),
            checkIn = order.CheckIn.ToString("yyyy-MM-dd"),
            checkOut = order.CheckOut.ToString("yyyy-MM-dd"),
            quantity = order.Quantity,
            guestCount = order.GuestCount
        };
    }

    private static string BuildGuestText(BookingOrder order)
    {
        var guests = ParseGuestNames(order.GuestSnapshotJson);
        var count = Math.Max(order.GuestCount, order.Quantity);
        if (guests.Count == 0) return $"已登记{count}人";
        return count > 1 ? $"{guests[0]} 等{count}人" : guests[0];
    }

    private static List<string> ParseGuestNames(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson)) return [];
        try
        {
            using var document = JsonDocument.Parse(snapshotJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return [];
            return document.RootElement.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.Object &&
                                item.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static object BuildFinanceMetric(List<BookingOrder> orders, DateTime? start)
    {
        var filtered = start.HasValue
            ? orders.Where(x => x.CreatedAt >= start.Value).ToList()
            : orders;
        var pending = filtered.Where(x => PaidStatuses.Contains(x.Status)).ToList();
        var done = filtered.Where(x => x.Status == OrderStatuses.Completed).ToList();
        var cancelled = filtered.Where(x => CancelledStatuses.Contains(x.Status)).ToList();
        return new
        {
            pendingCount = pending.Count,
            doneCount = done.Count,
            cancelCount = cancelled.Count,
            pendingAmountCents = pending.Sum(x => x.TotalAmountCents),
            doneAmountCents = done.Sum(x => x.TotalAmountCents),
            cancelAmountCents = cancelled.Sum(x => x.TotalAmountCents),
            totalAmountCents = filtered.Sum(x => x.TotalAmountCents)
        };
    }

    // 每日库存的初始化/同步统一放在 InventoryPlanner，管理端和商家端共用同一套口径。
    private static Task EnsureInventoryAsync(
        AppDbContext db,
        RoomType room,
        DateTime start,
        int days,
        CancellationToken cancellationToken)
        => InventoryPlanner.EnsureDailyInventoryAsync(db, room, start, days, cancellationToken);

    private static async Task EnsureRoomUnitsAsync(
        AppDbContext db,
        RoomType room,
        CancellationToken cancellationToken)
    {
        var existing = await db.RoomUnits
            .Where(x => x.RoomTypeId == room.Id)
            .ToListAsync(cancellationToken);
        var target = Math.Max(room.DefaultInventory, 1);
        if (existing.Count >= target) return;
        var prefix = room.Kind == RoomTypeKinds.Bed ? "床位" : "房间";
        for (var index = existing.Count + 1; index <= target; index++)
        {
            db.RoomUnits.Add(new RoomUnit
            {
                Id = Guid.NewGuid(),
                StoreId = room.StoreId,
                RoomTypeId = room.Id,
                Code = $"{prefix}-{index:000}",
                Kind = room.Kind == RoomTypeKinds.Bed ? "Bed" : "Room",
                Gender = room.Gender,
                Status = "Available",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static object ToCouponResponse(MerchantCouponItem item) => new
    {
        id = item.Id,
        name = item.Name,
        amount = (item.AmountCents / 100m).ToString("0.##"),
        amountCents = item.AmountCents,
        threshold = item.Threshold,
        stock = item.Stock,
        used = item.Used,
        status = item.Status
    };

    private static List<MerchantMessageSettingItem> DefaultMessageSettings() =>
    [
        new() { Key = "order", Title = "订单状态提醒", Desc = "新订单、入住和退款状态变化", Enabled = true },
        new() { Key = "review", Title = "评价提醒", Desc = "收到新评价时通知", Enabled = true },
        new() { Key = "finance", Title = "财务结算提醒", Desc = "提现和结算状态变化", Enabled = false }
    ];

    private static string? NormalizeImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        return IsRemoteImage(imageUrl) ? imageUrl.Trim() : null;
    }

    /// <summary>
    /// 房型图片统一入口：优先使用 imageUrls（最多 9 张、去重、忽略非法地址），
    /// 旧版单图字段 imageUrl 作为兜底，避免历史客户端保存失败。
    /// </summary>
    private static List<string> NormalizeRoomImages(IReadOnlyList<string>? imageUrls, string? imageUrl)
    {
        var result = new List<string>();
        if (imageUrls is not null)
        {
            foreach (var item in imageUrls)
            {
                var normalized = NormalizeImage(item);
                if (normalized is not null && !result.Contains(normalized))
                {
                    result.Add(normalized);
                }
                if (result.Count >= 9) break;
            }
        }
        if (result.Count == 0 && NormalizeImage(imageUrl) is { } fallback)
        {
            result.Add(fallback);
        }
        return result;
    }

    private static bool IsRemoteImage(string value)
    {
        var trimmed = value.Trim();
        // 微信 chooseImage 返回的 http://tmp/、wxfile:// 只是本机临时文件，
        // 重启或换设备后必失效，且开发者工具会因 __tmp__ 临时服务触发 CORS，禁止入库。
        if (trimmed.StartsWith("http://tmp/", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("wxfile://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("/__tmp__/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/static/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ToJson(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? JsonSerializer.Serialize(values) : null;

    private static string? NormalizeReviewStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "pending" or "待审核" => ReviewStatuses.Pending,
            "published" or "approved" or "已通过" or "上架" => ReviewStatuses.Published,
            "hidden" or "rejected" or "未通过" or "隐藏" => ReviewStatuses.Hidden,
            _ => null
        };
    }

    private static string ToMerchantReviewStatus(string? value) => value switch
    {
        ReviewStatuses.Pending => "pending",
        ReviewStatuses.Hidden => "hidden",
        _ => "published"
    };

    private static List<string> ParseReviewImages(string? json) =>
        ParseStringArray(json)
            .Where(IsRenderableImage)
            .ToList();

    private static bool IsRenderableImage(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0) return false;
        if (trimmed.StartsWith("http://tmp/", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("wxfile://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/static/", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "待补充";
        var value = phone.Trim();
        if (value.Length >= 7) return $"{value[..3]}****{value[^4..]}";
        return value;
    }

    private static string FormatCn(DateTime? value, string format = "yyyy-MM-dd HH:mm")
    {
        if (!value.HasValue) return string.Empty;
        var utc = value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
        return utc.AddHours(8).ToString(format);
    }

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

    private static AuditLog CreateAudit(
        ClaimsPrincipal principal,
        string action,
        string resourceType,
        string resourceId,
        string? summary = null) => new()
    {
        Id = Guid.NewGuid(),
        ActorId = GetUserId(principal),
        ActorType = "Merchant",
        Action = action,
        ResourceType = resourceType,
        ResourceId = resourceId,
        Summary = summary,
        CreatedAt = DateTime.UtcNow
    };

    private sealed class MerchantCouponItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AmountCents { get; set; }
        public string Threshold { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int Used { get; set; }
        public string Status { get; set; } = "生效中";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    private sealed class MerchantSettingsItem
    {
        public string? CheckIn { get; set; } = "14:00";
        public string? CheckOut { get; set; } = "12:00";
        public string? CancelRule { get; set; } = "入住前 24 小时可免费取消";
        public string? JoinRule { get; set; }
        public string? CheckInNotice { get; set; }
        public string? CancelNotice { get; set; }
        public bool AutoAccept { get; set; } = true;
        public bool ShowPhone { get; set; } = true;
    }

    public sealed class MerchantMessageSettingItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }
}

public sealed record MerchantBookingNoticeRequest(
    string? CheckInNotice,
    string? CancelNotice);

public sealed record MerchantRoomRequest(
    string? Name,
    string? Title,
    string? Type,
    int PriceCents,
    int? Stock,
    decimal? Area,
    string? Orientation,
    string? Layout,
    int? Rooms,
    int? Beds,
    int? MaxGuests,
    string? CheckIn,
    string? CheckOut,
    string? CancelRule,
    string? Description,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Services,
    IReadOnlyList<string>? Facilities,
    IReadOnlyList<string>? BathroomFacilities,
    IReadOnlyList<string>? NearbyFacilities,
    IReadOnlyList<string>? ImageUrls,
    string? ImageUrl,
    bool Published,
    string? Kind = null,
    string? Gender = null);

public sealed record MerchantVerifyRequest(string? Code);

public sealed record MerchantWithdrawalRequest(int AmountCents, string? Account);

public sealed record MerchantReviewModerateRequest(string? Status, string? Comment = null);

public sealed record MerchantCouponRequest(string? Name, int AmountCents, string? Threshold, int? Stock);

public sealed record MerchantCouponUpdateRequest(string? Status, string? Name, int? AmountCents, int? Stock);

public sealed record MerchantStaffRequest(
    string? Name,
    string? Phone,
    string? Gender = null,
    Guid? StoreId = null,
    IReadOnlyList<string>? Permissions = null,
    Guid? MemberId = null);

public sealed record MerchantSettingsRequest(
    string? StoreName,
    string? Phone,
    string? CheckIn,
    string? CheckOut,
    string? CancelRule,
    bool AutoAccept,
    bool ShowPhone);
