using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;
using TCPMS.Api.Services;

namespace TCPMS.Api;

public static class AdminEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        admin.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var storeId = GetStoreScope(principal);
            return Results.Ok(new
            {
                id = GetUserId(principal),
                username = principal.Identity?.Name,
                displayName = principal.FindFirstValue("display_name"),
                roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray(),
                storeId
            });
        });

        MapRoomTypes(admin);
        MapRoomUnits(admin);
        MapPrices(admin);
        MapInventory(admin);
        MapOrders(admin);
        MapRefunds(admin);
        MapUsersAndRoles(admin);
        MapMembers(admin);
        MapReviews(admin);
        MapStoreStaff(admin);
        MapPermissionMenus(admin);
        MapSubscriptionMessages(admin);
        MapUploads(admin);
        MapManagedItems(admin);
        MapWithdrawals(admin);
        MapRegions(admin);
        MapStatistics(admin);
        MapNotifications(admin);
        MapIntegrations(admin);
        MapGeo(admin);
        MapAuditLogs(admin);
    }

    private static void MapGeo(RouteGroupBuilder admin)
    {
        // 地图选点只需要解析结果，等管理员保存门店时再写库，避免取消编辑也改了门店资料。
        admin.MapPost("/geo/geocode", async (
            GeocodeRequest request,
            AmapService amapService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreProfile(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return Results.BadRequest(new ApiError("address_required", "地址不能为空"));
            }

            try
            {
                var result = await amapService.GeocodeAsync(request.Address.Trim(), request.City, cancellationToken);
                if (result is null)
                {
                    return Results.BadRequest(new ApiError("geocode_empty", "高德未找到匹配地址"));
                }

                return Results.Ok(ToGeoLookupResponse(result));
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ApiError("amap_unavailable", ex.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        admin.MapPost("/geo/reverse", async (
            ReverseGeocodeRequest request,
            AmapService amapService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreProfile(principal))
            {
                return Results.Forbid();
            }

            try
            {
                var result = await amapService.ReverseGeocodeAsync(request.Longitude, request.Latitude, cancellationToken);
                if (result is null)
                {
                    return Results.BadRequest(new ApiError("reverse_geocode_empty", "高德未解析出该坐标的地址"));
                }

                return Results.Ok(ToGeoLookupResponse(result));
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ApiError("amap_unavailable", ex.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });
    }

    private static GeoLookupResponse ToGeoLookupResponse(GeoResult result) =>
        new(result.Longitude, result.Latitude, result.FormattedAddress, result.Province, result.City, result.District);

    private static void MapUploads(RouteGroupBuilder admin)
    {
        admin.MapPost("/uploads/carousel", async (
            ClaimsPrincipal principal,
            IWebHostEnvironment environment,
            HttpRequest request,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            IFormFile? file;
            try
            {
                var form = await request.ReadFormAsync(cancellationToken);
                file = form.Files.GetFile("file");
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new ApiError("carousel_image_invalid", "图片上传内容无效"));
            }

            if (file is null || file.Length <= 0)
            {
                return Results.BadRequest(new ApiError("carousel_image_required", "请选择轮播图文件"));
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                return Results.BadRequest(new ApiError("carousel_image_too_large", "轮播图不能超过 10MB"));
            }

            var extension = await DetectImageExtensionAsync(file, cancellationToken);
            if (extension is null)
            {
                return Results.BadRequest(new ApiError("carousel_image_type_invalid", "轮播图必须是 JPG、PNG 或 WEBP 图片"));
            }

            var directoryPath = StoragePaths.Uploads(environment, "carousel");
            Directory.CreateDirectory(directoryPath);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
            var targetPath = Path.Combine(directoryPath, fileName);
            await using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var url = $"{request.Scheme}://{request.Host}/uploads/carousel/{fileName}";
            return Results.Ok(new { url, fileName, size = file.Length });
        });
    }

    private static void MapIntegrations(RouteGroupBuilder admin)
    {
        admin.MapGet("/integrations", (
            AmapService amapService,
            WeChatPayService weChatPayService,
            IHostEnvironment environment) =>
        {
            var weChat = weChatPayService.GetConfigurationStatus();
            return Results.Ok(new
            {
                environment = environment.EnvironmentName,
                wechatPay = new
                {
                    configured = weChat.PaymentConfigured,
                    miniProgramConfigured = weChat.MiniProgramConfigured,
                    appId = weChat.AppId,
                    merchantId = weChat.MerchantId,
                    merchantSerialNumber = weChat.MerchantSerialNumber,
                    notifyUrl = weChat.NotifyUrl,
                    hasApiV3Key = weChat.HasApiV3Key,
                    hasMerchantPrivateKey = weChat.HasMerchantPrivateKey,
                    hasPlatformCertificate = weChat.HasPlatformCertificate,
                    hasWeChatPayPublicKey = weChat.HasWeChatPayPublicKey,
                    missing = weChat.Missing
                },
                amap = new
                {
                    configured = amapService.IsConfigured,
                    hasServerKey = amapService.HasServerKey,
                    hasMiniProgramKey = amapService.HasMiniProgramKey,
                    defaultRadiusKm = amapService.DefaultRadiusKm
                }
            });
        });

        admin.MapPost("/integrations/{provider}/test", async (
            string provider,
            string? address,
            AmapService amapService,
            WeChatPayService weChatPayService,
            CancellationToken cancellationToken) =>
        {
            if (string.Equals(provider, "amap", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var result = await amapService.GeocodeAsync(
                        string.IsNullOrWhiteSpace(address) ? "上海市黄浦区人民广场" : address.Trim(),
                        "上海",
                        cancellationToken);
                    return result is null
                        ? Results.BadRequest(new ApiError("amap_test_empty", "高德未返回匹配地址"))
                        : Results.Ok(new
                        {
                            provider = "amap",
                            message = "高德地理编码测试成功",
                            result.FormattedAddress,
                            result.Longitude,
                            result.Latitude
                        });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Json(
                        new ApiError("amap_test_failed", ex.Message),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (HttpRequestException ex)
                {
                    return Results.Json(
                        new ApiError("amap_test_failed", ex.Message),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            }

            if (string.Equals(provider, "wechat", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await weChatPayService.TestConnectionAsync(cancellationToken);
                    return Results.Ok(new
                    {
                        provider = "wechat",
                        message = "微信支付商户 API 连接成功"
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Json(
                        new ApiError("wechat_test_failed", ex.Message),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (HttpRequestException ex)
                {
                    return Results.Json(
                        new ApiError("wechat_test_failed", ex.Message),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (CryptographicException ex)
                {
                    return Results.Json(
                        new ApiError("wechat_test_failed", $"应答验签失败：{ex.Message}"),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            }

            return Results.NotFound(new ApiError("integration_not_found", "不支持的应用类型"));
        });
    }

    private static void MapRegions(RouteGroupBuilder admin)
    {
        admin.MapGet("/regions", async (
            string? level,
            string? parentCode,
            string? keyword,
            bool? includeDisabled,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var query = db.Regions.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(x => x.Level == level);
            }
            if (!string.IsNullOrWhiteSpace(parentCode))
            {
                query = query.Where(x => x.ParentCode == parentCode);
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var value = keyword.Trim();
                query = query.Where(x =>
                    x.Code.Contains(value) ||
                    x.Name.Contains(value) ||
                    x.ShortName.Contains(value) ||
                    x.FullName.Contains(value));
            }
            if (includeDisabled != true)
            {
                query = query.Where(x => x.IsEnabled);
            }

            var items = await query
                .OrderBy(x => x.Level == RegionLevels.Province ? 0 :
                    x.Level == RegionLevels.City ? 1 : 2)
                .ThenBy(x => x.ParentCode)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(ToAdminRegionItem).ToList());
        });

        admin.MapGet("/regions/tree", async (
            bool? includeDisabled,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var query = db.Regions.AsNoTracking().AsQueryable();
            if (includeDisabled != true)
            {
                query = query.Where(x => x.IsEnabled);
            }

            var regions = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
            return Results.Ok(BuildRegionTree(regions, null));
        });

        admin.MapGet("/regions/{code}", async (
            string code,
            bool? includeDisabled,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var region = await db.Regions.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (region is null || (includeDisabled != true && !region.IsEnabled))
            {
                return Results.NotFound(new ApiError("region_not_found", "地区不存在"));
            }

            return Results.Ok(ToAdminRegionItem(region));
        });

        admin.MapPost("/regions", async (
            RegionRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageRegions(principal))
            {
                return Results.Forbid();
            }

            var validation = await ValidateRegionRequestAsync(
                db,
                request,
                null,
                cancellationToken);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }

            var code = NormalizeRegionCode(request.Code)!;
            var level = NormalizeRegionLevel(request.Level)!;
            var name = request.Name!.Trim();
            var region = new Region
            {
                Code = code,
                ParentCode = NormalizeRegionParentCode(request.ParentCode),
                Name = name,
                ShortName = string.IsNullOrWhiteSpace(request.ShortName)
                    ? ToRegionShortName(name)
                    : request.ShortName.Trim(),
                FullName = string.IsNullOrWhiteSpace(request.FullName)
                    ? name
                    : request.FullName.Trim(),
                Level = level,
                Initial = NormalizeRegionInitial(request.Initial),
                IsHot = request.IsHot,
                IsEnabled = request.IsEnabled,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Regions.Add(region);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "region.create",
                "Region",
                region.Code,
                region.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/admin/regions/{region.Code}",
                ToAdminRegionItem(region));
        });

        admin.MapPut("/regions/{code}", async (
            string code,
            RegionRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageRegions(principal))
            {
                return Results.Forbid();
            }

            var normalizedCode = NormalizeRegionCode(code);
            if (normalizedCode is null)
            {
                return Results.BadRequest(new ApiError("validation_error", "地区代码必须为 6 位数字"));
            }

            var region = await db.Regions
                .SingleOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
            if (region is null)
            {
                return Results.NotFound(new ApiError("region_not_found", "地区不存在"));
            }

            // Administrative codes are stable identifiers. Keep the route code as
            // the source of truth and prevent accidental primary-key changes.
            var effectiveRequest = request with
            {
                Code = string.IsNullOrWhiteSpace(request.Code)
                    ? region.Code
                    : request.Code
            };
            if (!string.Equals(
                    NormalizeRegionCode(effectiveRequest.Code),
                    region.Code,
                    StringComparison.Ordinal))
            {
                return Results.BadRequest(new ApiError(
                    "region_code_immutable",
                    "地区代码不可修改"));
            }

            var validation = await ValidateRegionRequestAsync(
                db,
                effectiveRequest,
                region.Code,
                cancellationToken);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }

            var level = NormalizeRegionLevel(effectiveRequest.Level)!;
            var name = effectiveRequest.Name!.Trim();
            region.ParentCode = NormalizeRegionParentCode(effectiveRequest.ParentCode);
            region.Name = name;
            region.ShortName = string.IsNullOrWhiteSpace(effectiveRequest.ShortName)
                ? ToRegionShortName(name)
                : effectiveRequest.ShortName.Trim();
            region.FullName = string.IsNullOrWhiteSpace(effectiveRequest.FullName)
                ? name
                : effectiveRequest.FullName.Trim();
            region.Level = level;
            region.Initial = NormalizeRegionInitial(effectiveRequest.Initial);
            region.IsHot = effectiveRequest.IsHot;
            region.IsEnabled = effectiveRequest.IsEnabled;
            region.SortOrder = effectiveRequest.SortOrder;
            region.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                "region.update",
                "Region",
                region.Code,
                region.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminRegionItem(region));
        });

        admin.MapDelete("/regions/{code}", async (
            string code,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageRegions(principal))
            {
                return Results.Forbid();
            }

            var normalizedCode = NormalizeRegionCode(code);
            if (normalizedCode is null)
            {
                return Results.BadRequest(new ApiError("validation_error", "地区代码必须为 6 位数字"));
            }

            var region = await db.Regions
                .SingleOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
            if (region is null)
            {
                return Results.NotFound(new ApiError("region_not_found", "地区不存在"));
            }

            if (await db.Regions.AnyAsync(
                    x => x.ParentCode == region.Code,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError(
                    "region_has_children",
                    "该地区包含下级地区，请先处理下级地区"));
            }

            db.Regions.Remove(region);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "region.delete",
                "Region",
                region.Code,
                region.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapRoomTypes(RouteGroupBuilder admin)
    {
        admin.MapGet("/room-types", async (
            Guid? storeId,
            bool? includeUnpublished,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var query = db.RoomTypes
                .AsNoTracking()
                .Include(x => x.Store)
                .AsQueryable();

            var scope = GetStoreScope(principal);
            if (scope.HasValue)
            {
                query = query.Where(x => x.StoreId == scope.Value);
            }
            else if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            if (includeUnpublished != true)
            {
                query = query.Where(x => x.IsPublished);
            }

            var items = await query
                .OrderBy(x => x.Store!.SortOrder)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.BasePriceCents)
                .ToListAsync(cancellationToken);

            var roomTypeIds = items.Select(x => x.Id).ToList();
            var resourceCounts = await db.RoomUnits.AsNoTracking()
                .Where(x => roomTypeIds.Contains(x.RoomTypeId))
                .GroupBy(x => x.RoomTypeId)
                .Select(group => new { RoomTypeId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count, cancellationToken);
            var bookingCounts = await db.BookingOrders.AsNoTracking()
                .Where(x => roomTypeIds.Contains(x.RoomTypeId))
                .GroupBy(x => x.RoomTypeId)
                .Select(group => new { RoomTypeId = group.Key, Count = group.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count, cancellationToken);
            return Results.Ok(items
                .Select(x => ToAdminRoomTypeItem(
                    x,
                    resourceCounts.TryGetValue(x.Id, out var count) ? count : 0,
                    bookingCounts.TryGetValue(x.Id, out var bookings) ? bookings : 0))
                .ToList());
        });

        admin.MapPost("/room-types", async (
            RoomTypeRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var validation = ValidateRoomType(request);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }

            var store = await db.Stores.SingleOrDefaultAsync(x => x.Id == request.StoreId, cancellationToken);
            if (store is null)
            {
                return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
            }
            if (!CanAccessStore(principal, store.Id))
            {
                return Results.Forbid();
            }
            if (await db.RoomTypes.AnyAsync(
                    x => x.StoreId == request.StoreId && x.Name == request.Name.Trim(),
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("room_type_exists", "该门店已存在同名房型"));
            }

            var roomType = new RoomType
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                Name = request.Name.Trim(),
                LongName = request.LongName,
                Kind = request.Kind,
                RoomCategory = request.RoomCategory,
                BedCount = request.BedCount,
                Gender = request.Gender,
                MaxGuests = request.MaxGuests,
                ChildCapacity = Math.Max(request.ChildCapacity, 0),
                BasePriceCents = request.BasePriceCents,
                Description = request.Description,
                FacilitiesJson = request.FacilitiesJson,
                TagsJson = request.TagsJson,
                ServicesJson = request.ServicesJson,
                BathroomFacilitiesJson = request.BathroomFacilitiesJson,
                NearbyFacilitiesJson = request.NearbyFacilitiesJson,
                ImageUrlsJson = request.ImageUrlsJson,
                CheckInTime = request.CheckInTime,
                CheckOutTime = request.CheckOutTime,
                CancellationRule = request.CancellationRule,
                JoinRule = request.JoinRule,
                Area = request.Area,
                Orientation = request.Orientation,
                RoomCount = Math.Max(request.RoomCount, 1),
                Layout = request.Layout,
                DefaultInventory = Math.Max(request.DefaultInventory, 0),
                SortOrder = request.SortOrder,
                IsDraft = request.IsDraft,
                RejectionReason = request.RejectionReason,
                IsPublished = request.IsPublished,
                ApprovalStatus = request.ApprovalStatus ?? "Draft"
            };
            db.RoomTypes.Add(roomType);
            db.AuditLogs.Add(CreateAudit(principal, "room_type.create", "RoomType", roomType.Id.ToString(), roomType.Name));
            await db.SaveChangesAsync(cancellationToken);

            roomType.Store = store;
            return Results.Created($"/api/v1/admin/room-types/{roomType.Id}", ToAdminRoomTypeItem(roomType));
        });

        admin.MapPut("/room-types/{id:guid}", async (
            Guid id,
            RoomTypeRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var roomType = await db.RoomTypes
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var validation = ValidateRoomType(request);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }
            if (request.StoreId != roomType.StoreId &&
                !IsHeadquarters(principal))
            {
                return Results.Forbid();
            }

            roomType.StoreId = request.StoreId;
            roomType.Name = request.Name.Trim();
            roomType.LongName = request.LongName;
            roomType.Kind = request.Kind;
            roomType.RoomCategory = request.RoomCategory;
            roomType.BedCount = request.BedCount;
            roomType.Gender = request.Gender;
            roomType.MaxGuests = request.MaxGuests;
            roomType.ChildCapacity = Math.Max(request.ChildCapacity, 0);
            roomType.BasePriceCents = request.BasePriceCents;
            roomType.Description = request.Description;
            roomType.FacilitiesJson = request.FacilitiesJson;
            roomType.TagsJson = request.TagsJson;
            roomType.ServicesJson = request.ServicesJson;
            roomType.BathroomFacilitiesJson = request.BathroomFacilitiesJson;
            roomType.NearbyFacilitiesJson = request.NearbyFacilitiesJson;
            roomType.ImageUrlsJson = request.ImageUrlsJson;
            roomType.CheckInTime = request.CheckInTime;
            roomType.CheckOutTime = request.CheckOutTime;
            roomType.CancellationRule = request.CancellationRule;
            roomType.JoinRule = request.JoinRule;
            roomType.Area = request.Area;
            roomType.Orientation = request.Orientation;
            roomType.RoomCount = Math.Max(request.RoomCount, 1);
            roomType.Layout = request.Layout;
            roomType.DefaultInventory = Math.Max(request.DefaultInventory, 0);
            roomType.SortOrder = request.SortOrder;
            roomType.IsDraft = request.IsDraft;
            roomType.RejectionReason = request.RejectionReason;
            roomType.IsPublished = request.IsPublished;
            roomType.ApprovalStatus = request.ApprovalStatus ?? roomType.ApprovalStatus;
            roomType.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "room_type.update", "RoomType", id.ToString(), roomType.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminRoomTypeItem(roomType));
        });

        admin.MapDelete("/room-types/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var roomType = await db.RoomTypes
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }
            if (await db.BookingOrders.AnyAsync(x => x.RoomTypeId == id, cancellationToken) ||
                await db.Reviews.AnyAsync(x => x.RoomTypeId == id, cancellationToken))
            {
                return Results.Conflict(new ApiError("room_type_in_use", "该房型已有订单或评价，不能删除；请先下架"));
            }

            db.RoomUnits.RemoveRange(db.RoomUnits.Where(x => x.RoomTypeId == id));
            db.PriceCalendars.RemoveRange(db.PriceCalendars.Where(x => x.RoomTypeId == id));
            db.InventoryDailies.RemoveRange(db.InventoryDailies.Where(x => x.RoomTypeId == id));
            db.RoomTypes.Remove(roomType);
            db.AuditLogs.Add(CreateAudit(principal, "room_type.delete", "RoomType", id.ToString(), roomType.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });

        admin.MapPost("/room-types/{id:guid}/review", async (
            Guid id,
            RoomTypeReviewRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var roomType = await db.RoomTypes
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }
            if (!request.Approved && string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return Results.BadRequest(new ApiError("rejection_reason_required", "拒绝房型时必须填写原因"));
            }

            roomType.ApprovalStatus = request.Approved ? "Approved" : "Rejected";
            roomType.IsPublished = request.Approved;
            roomType.IsDraft = false;
            roomType.RejectionReason = request.Approved ? null : request.RejectionReason?.Trim();
            roomType.ReviewedAt = DateTime.UtcNow;
            roomType.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                request.Approved ? "room_type.approve" : "room_type.reject",
                "RoomType",
                id.ToString(),
                roomType.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminRoomTypeItem(roomType));
        });

        admin.MapPost("/room-types/{id:guid}/publish", async (
            Guid id,
            bool published,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var roomType = await db.RoomTypes
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }
            if (published && roomType.ApprovalStatus != "Approved")
            {
                return Results.Conflict(new ApiError("room_type_not_approved", "房间审核通过后才能上架"));
            }

            roomType.IsPublished = published;
            roomType.ApprovalStatus = published ? "Approved" : "Offline";
            roomType.IsDraft = false;
            roomType.RejectionReason = null;
            roomType.ReviewedAt = published ? DateTime.UtcNow : roomType.ReviewedAt;
            roomType.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                published ? "room_type.publish" : "room_type.unpublish",
                "RoomType",
                id.ToString(),
                roomType.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminRoomTypeItem(roomType));
        });
    }

    private static void MapRoomUnits(RouteGroupBuilder admin)
    {
        admin.MapGet("/room-types/{roomTypeId:guid}/units", async (
            Guid roomTypeId,
            string? status,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var roomType = await db.RoomTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == roomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var query = db.RoomUnits.AsNoTracking()
                .Where(x => x.RoomTypeId == roomTypeId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }
            var units = await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
            return Results.Ok(units.Select(ToRoomUnitItem).ToList());
        });

        admin.MapPost("/room-types/{roomTypeId:guid}/units", async (
            Guid roomTypeId,
            RoomUnitRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var roomType = await db.RoomTypes.SingleOrDefaultAsync(x => x.Id == roomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest(new ApiError("validation_error", "房间或床位编码不能为空"));
            }
            if (await db.RoomUnits.AnyAsync(
                    x => x.RoomTypeId == roomTypeId && x.Code == request.Code.Trim(),
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("room_unit_exists", "该房间或床位编码已存在"));
            }

            var unit = new RoomUnit
            {
                Id = Guid.NewGuid(),
                StoreId = roomType.StoreId,
                RoomTypeId = roomTypeId,
                Code = request.Code.Trim(),
                Kind = string.IsNullOrWhiteSpace(request.Kind) ? "Room" : request.Kind,
                Gender = request.Gender,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Available" : request.Status,
                Note = request.Note
            };
            db.RoomUnits.Add(unit);
            db.AuditLogs.Add(CreateAudit(principal, "room_unit.create", "RoomUnit", unit.Id.ToString(), unit.Code));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/room-types/{roomTypeId}/units/{unit.Id}", ToRoomUnitItem(unit));
        });

        admin.MapPut("/room-units/{id:guid}", async (
            Guid id,
            RoomUnitRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageCatalog(principal))
            {
                return Results.Forbid();
            }

            var unit = await db.RoomUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (unit is null)
            {
                return Results.NotFound(new ApiError("room_unit_not_found", "房间或床位不存在"));
            }
            if (!CanAccessStore(principal, unit.StoreId))
            {
                return Results.Forbid();
            }

            unit.Code = request.Code.Trim();
            unit.Kind = request.Kind;
            unit.Gender = request.Gender;
            unit.Status = request.Status;
            unit.Note = request.Note;
            unit.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "room_unit.update", "RoomUnit", id.ToString(), unit.Code));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToRoomUnitItem(unit));
        });
    }

    private static void MapPrices(RouteGroupBuilder admin)
    {
        admin.MapGet("/prices", async (
            Guid roomTypeId,
            DateTime? from,
            DateTime? to,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var roomType = await db.RoomTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == roomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var range = NormalizeRange(from, to, 31);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }
            var items = await db.PriceCalendars.AsNoTracking()
                .Where(x => x.RoomTypeId == roomTypeId &&
                            x.Date >= range.From &&
                            x.Date < range.To)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(x => new PriceCalendarItem(
                x.Id, x.RoomTypeId, x.Date, x.PriceCents, x.Source)).ToList());
        });

        admin.MapPut("/prices/bulk", async (
            BulkPriceRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManagePrices(principal))
            {
                return Results.Forbid();
            }
            if (request.PriceCents <= 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "价格必须大于 0"));
            }
            if (request.TotalQuantity is < 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "库存不能小于 0"));
            }

            var range = NormalizeRange(request.From, request.To, 180);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }

            var roomType = await db.RoomTypes.SingleOrDefaultAsync(x => x.Id == request.RoomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var existing = await db.PriceCalendars
                .Where(x => x.RoomTypeId == request.RoomTypeId &&
                            x.Date >= range.From &&
                            x.Date < range.To)
                .ToDictionaryAsync(x => x.Date.Date, cancellationToken);
            var source = string.IsNullOrWhiteSpace(request.Source) ? "Manual" : request.Source.Trim();
            for (var date = range.From; date < range.To; date = date.AddDays(1))
            {
                if (existing.TryGetValue(date.Date, out var item))
                {
                    item.PriceCents = request.PriceCents;
                    item.Source = source;
                }
                else
                {
                    db.PriceCalendars.Add(new PriceCalendar
                    {
                        Id = Guid.NewGuid(),
                        StoreId = roomType.StoreId,
                        RoomTypeId = roomType.Id,
                        Date = date.Date,
                        PriceCents = request.PriceCents,
                        Source = source
                    });
                }
            }

            if (request.TotalQuantity is int totalQuantity)
            {
                var inventoryItems = await db.InventoryDailies
                    .Where(x => x.RoomTypeId == request.RoomTypeId &&
                                x.Date >= range.From &&
                                x.Date < range.To)
                    .ToDictionaryAsync(x => x.Date.Date, cancellationToken);

                for (var date = range.From; date < range.To; date = date.AddDays(1))
                {
                    if (!inventoryItems.TryGetValue(date.Date, out var inventory))
                    {
                        inventory = new InventoryDaily
                        {
                            Id = Guid.NewGuid(),
                            StoreId = roomType.StoreId,
                            RoomTypeId = roomType.Id,
                            Date = date.Date,
                            TotalQuantity = totalQuantity
                        };
                        db.InventoryDailies.Add(inventory);
                    }

                    if (totalQuantity < inventory.LockedQuantity + inventory.SoldQuantity)
                    {
                        return Results.Conflict(new ApiError(
                            "inventory_below_occupied",
                            $"{date:yyyy-MM-dd} 的库存不能低于锁定和已售数量"));
                    }

                    inventory.TotalQuantity = totalQuantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
            }

            db.AuditLogs.Add(CreateAudit(
                principal,
                "price.bulk_update",
                "RoomType",
                roomType.Id.ToString(),
                $"{roomType.Name}: {range.From:yyyy-MM-dd} - {range.To.AddDays(-1):yyyy-MM-dd}" +
                (request.TotalQuantity is int quantity ? $"，每日库存 {quantity}" : string.Empty)));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { updated = (int)(range.To - range.From).TotalDays });
        });
    }

    private static void MapInventory(RouteGroupBuilder admin)
    {
        admin.MapGet("/inventory", async (
            Guid roomTypeId,
            DateTime? from,
            DateTime? to,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var roomType = await db.RoomTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == roomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var range = NormalizeRange(from, to, 31);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }
            var items = await db.InventoryDailies.AsNoTracking()
                .Where(x => x.RoomTypeId == roomTypeId &&
                            x.Date >= range.From &&
                            x.Date < range.To)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(ToInventorySummary).ToList());
        });

        admin.MapPost("/inventory/adjust", async (
            InventoryAdjustRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanAdjustInventory(principal))
            {
                return Results.Forbid();
            }
            if (request.Delta == 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "库存调整数量不能为 0"));
            }
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest(new ApiError("validation_error", "库存调整原因不能为空"));
            }

            var range = NormalizeRange(request.From, request.To, 90);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }
            var roomType = await db.RoomTypes.SingleOrDefaultAsync(x => x.Id == request.RoomTypeId, cancellationToken);
            if (roomType is null)
            {
                return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
            }
            if (!CanAccessStore(principal, roomType.StoreId))
            {
                return Results.Forbid();
            }

            var items = await db.InventoryDailies
                .Where(x => x.RoomTypeId == roomType.Id &&
                            x.Date >= range.From &&
                            x.Date < range.To)
                .ToDictionaryAsync(x => x.Date.Date, cancellationToken);
            var updated = 0;
            for (var date = range.From; date < range.To; date = date.AddDays(1))
            {
                if (!items.TryGetValue(date.Date, out var inventory))
                {
                    inventory = new InventoryDaily
                    {
                        Id = Guid.NewGuid(),
                        StoreId = roomType.StoreId,
                        RoomTypeId = roomType.Id,
                        Date = date.Date,
                        TotalQuantity = roomType.DefaultInventory > 0
                            ? roomType.DefaultInventory
                            : roomType.Kind == RoomTypeKinds.Bed
                                ? Math.Max(roomType.BedCount, 1)
                                : 8
                    };
                    db.InventoryDailies.Add(inventory);
                }

                var nextTotal = inventory.TotalQuantity + request.Delta;
                if (nextTotal < inventory.LockedQuantity + inventory.SoldQuantity)
                {
                    return Results.Conflict(new ApiError(
                        "inventory_below_occupied",
                        $"{date:yyyy-MM-dd} 的调整后库存不能低于锁定和已售数量"));
                }
                inventory.TotalQuantity = nextTotal;
                inventory.UpdatedAt = DateTime.UtcNow;
                updated++;
            }

            db.AuditLogs.Add(CreateAudit(
                principal,
                "inventory.adjust",
                "RoomType",
                roomType.Id.ToString(),
                $"{request.Reason}: {request.Delta} x {updated}"));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { updated });
        });
    }

    private static void MapOrders(RouteGroupBuilder admin)
    {
        admin.MapGet("/orders/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var order = await db.BookingOrders.AsNoTracking()
                .Include(x => x.Store)
                .Include(x => x.RoomType)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            }
            if (!CanAccessStore(principal, order.StoreId))
            {
                return Results.Forbid();
            }
            return Results.Ok(await ToAdminOrderDetailAsync(db, order, cancellationToken));
        });

        admin.MapPost("/orders/{id:guid}/cancel", async (
            Guid id,
            string? reason,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageOrders(principal))
            {
                return Results.Forbid();
            }

            var order = await db.BookingOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
            }
            if (!CanAccessStore(principal, order.StoreId))
            {
                return Results.Forbid();
            }
            if (order.Status != OrderStatuses.PendingPayment)
            {
                return Results.BadRequest(new ApiError("order_not_cancelable", "已支付订单请走退款流程"));
            }

            await ReleaseInventoryAsync(db, order, false, cancellationToken);
            order.Status = OrderStatuses.Cancelled;
            order.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "后台取消" : reason.Trim();
            order.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "order.cancel", "BookingOrder", id.ToString(), order.CancellationReason));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok();
        });
    }

    private static void MapRefunds(RouteGroupBuilder admin)
    {
        admin.MapGet("/refunds", async (
            string? status,
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var query = db.RefundRecords.AsNoTracking()
                .Include(x => x.Order)
                .ThenInclude(x => x!.Store)
                .Include(x => x.Order)
                .ThenInclude(x => x!.RoomType)
                .AsQueryable();
            var scope = GetStoreScope(principal);
            if (scope.HasValue)
            {
                query = query.Where(x => x.Order!.StoreId == scope.Value);
            }
            else if (storeId.HasValue)
            {
                query = query.Where(x => x.Order!.StoreId == storeId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(ToAdminRefundItem).ToList());
        });

        admin.MapPost("/refunds/{id:guid}/review", async (
            Guid id,
            ReviewRefundRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            WeChatPayService weChatPayService,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageRefunds(principal))
            {
                return Results.Forbid();
            }

            var refund = await db.RefundRecords
                .Include(x => x.Order)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (refund?.Order is null)
            {
                return Results.NotFound(new ApiError("refund_not_found", "退款申请不存在"));
            }
            if (!CanAccessStore(principal, refund.Order.StoreId))
            {
                return Results.Forbid();
            }
            if (refund.Status != "PendingReview")
            {
                return Results.BadRequest(new ApiError("refund_already_processed", "退款申请已经处理"));
            }

            refund.ReviewedByAdminId = GetUserId(principal);
            refund.ReviewedAt = DateTime.UtcNow;
            refund.ReviewComment = request.Comment;
            if (!request.Approved)
            {
                refund.Status = "Rejected";
                refund.Order.Status = refund.OriginalOrderStatus ?? OrderStatuses.PaidPendingConfirmation;
            }
            else
            {
                var latestPayment = await db.PaymentRecords
                    .Where(x => x.OrderId == refund.Order.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                var isRealWeChatPayment =
                    latestPayment?.Provider == "WeChat" &&
                    !latestPayment.TransactionNumber.StartsWith("MOCK", StringComparison.OrdinalIgnoreCase) &&
                    !latestPayment.TransactionNumber.StartsWith("SEED_", StringComparison.OrdinalIgnoreCase);
                try
                {
                    refund.ProviderRefundNumber = isRealWeChatPayment
                        ? await weChatPayService.CreateRefundAsync(
                            refund.Order,
                            refund,
                            cancellationToken)
                        : $"MOCK_RF_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                }
                catch (InvalidOperationException)
                {
                    refund.Status = "RefundFailed";
                    refund.ReviewComment = string.IsNullOrWhiteSpace(request.Comment)
                        ? "微信退款配置不完整"
                        : $"{request.Comment.Trim()}；微信退款配置不完整";
                    refund.ReviewedByAdminId = GetUserId(principal);
                    refund.ReviewedAt = DateTime.UtcNow;
                    refund.Order.Status = OrderStatuses.RefundFailed;
                    refund.Order.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
                catch (HttpRequestException)
                {
                    refund.Status = "RefundFailed";
                    refund.ReviewComment = string.IsNullOrWhiteSpace(request.Comment)
                        ? "微信退款接口调用失败"
                        : $"{request.Comment.Trim()}；微信退款接口调用失败";
                    refund.ReviewedByAdminId = GetUserId(principal);
                    refund.ReviewedAt = DateTime.UtcNow;
                    refund.Order.Status = OrderStatuses.RefundFailed;
                    refund.Order.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
                refund.Status = "Refunded";
                refund.CompletedAt = DateTime.UtcNow;
                refund.Order.Status = OrderStatuses.Refunded;
                refund.Order.PaymentStatus = "Refunded";
                await ReleaseInventoryAsync(db, refund.Order, true, cancellationToken);
            }

            refund.Order.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                request.Approved ? "refund.approve" : "refund.reject",
                "RefundRecord",
                id.ToString(),
                request.Comment));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminRefundItem(refund));
        });
    }

    private static void MapUsersAndRoles(RouteGroupBuilder admin)
    {
        admin.MapGet("/roles", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var roles = await db.Roles.AsNoTracking()
                .Include(x => x.UserRoles)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
            return Results.Ok(roles.Select(x => new RoleItem(x.Id, x.Name, x.Code, x.UserRoles.Count)).ToList());
        });

        admin.MapGet("/users", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var query = db.AdminUsers.AsNoTracking().AsQueryable();
            if (GetStoreScope(principal) is Guid storeScope)
            {
                query = query.Where(x => x.StoreId == storeScope);
            }

            var users = await query
                .Include(x => x.Store)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .OrderBy(x => x.Username)
                .ToListAsync(cancellationToken);
            return Results.Ok(users.Select(ToAdminUserItem).ToList());
        });

        admin.MapPost("/users", async (
            AdminUserRequest request,
            AppDbContext db,
            IPasswordHasher<AdminUser> passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!IsSystemAdmin(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.DisplayName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new ApiError("validation_error", "账号、姓名和初始密码不能为空"));
            }
            if (await db.AdminUsers.AnyAsync(x => x.Username == request.Username.Trim(), cancellationToken))
            {
                return Results.Conflict(new ApiError("admin_user_exists", "管理员账号已存在"));
            }

            var user = new AdminUser
            {
                Id = Guid.NewGuid(),
                Username = request.Username.Trim(),
                DisplayName = request.DisplayName.Trim(),
                PhoneNumber = request.PhoneNumber,
                StoreId = request.StoreId,
                IsEnabled = request.IsEnabled
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
            // 超管设置的初始密码，要求本人首次登录后自行修改。
            user.MustChangePassword = true;
            db.AdminUsers.Add(user);

            var roleIds = request.RoleIds ?? Array.Empty<Guid>();
            var roles = await db.Roles.Where(x => roleIds.Contains(x.Id)).ToListAsync(cancellationToken);
            foreach (var role in roles)
            {
                db.AdminUserRoles.Add(new AdminUserRole
                {
                    AdminUserId = user.Id,
                    RoleId = role.Id
                });
            }

            db.AuditLogs.Add(CreateAudit(principal, "admin_user.create", "AdminUser", user.Id.ToString(), user.Username));
            await db.SaveChangesAsync(cancellationToken);
            user.UserRoles = roles.Select(x => new AdminUserRole { AdminUserId = user.Id, RoleId = x.Id, Role = x }).ToList();
            return Results.Created($"/api/v1/admin/users/{user.Id}", ToAdminUserItem(user));
        });

        admin.MapPut("/users/{id:guid}", async (
            Guid id,
            AdminUserRequest request,
            AppDbContext db,
            IPasswordHasher<AdminUser> passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!IsSystemAdmin(principal))
            {
                return Results.Forbid();
            }

            var user = await db.AdminUsers
                .Include(x => x.Store)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new ApiError("admin_user_not_found", "管理员不存在"));
            }

            user.DisplayName = request.DisplayName.Trim();
            user.PhoneNumber = request.PhoneNumber;
            user.StoreId = request.StoreId;
            user.IsEnabled = request.IsEnabled;
            user.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
                // 超管重置密码后同样要求本人改密。
                user.MustChangePassword = true;
                user.PasswordChangedAt = null;
            }

            db.AdminUserRoles.RemoveRange(user.UserRoles);
            user.UserRoles.Clear();
            var roleIds = request.RoleIds ?? Array.Empty<Guid>();
            var roles = await db.Roles.Where(x => roleIds.Contains(x.Id)).ToListAsync(cancellationToken);
            foreach (var role in roles)
            {
                var link = new AdminUserRole { AdminUserId = user.Id, RoleId = role.Id, Role = role };
                db.AdminUserRoles.Add(link);
                user.UserRoles.Add(link);
            }

            db.AuditLogs.Add(CreateAudit(principal, "admin_user.update", "AdminUser", id.ToString(), user.Username));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminUserItem(user));
        });
    }

    private static void MapMembers(RouteGroupBuilder admin)
    {
        admin.MapGet("/members", async (
            string? keyword,
            int? page,
            int? pageSize,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var currentPageSize = Math.Clamp(pageSize ?? 10, 1, 100);
            var query = db.WxUsers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var text = keyword.Trim();
                query = query.Where(x =>
                    x.OpenId.Contains(text) ||
                    (x.Nickname != null && x.Nickname.Contains(text)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(text)));
            }

            var total = await query.CountAsync(cancellationToken);
            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(x => x.Id).ToList();
            var directLinks = await db.WxUsers.AsNoTracking()
                .Where(x => x.ParentUserId.HasValue && userIds.Contains(x.ParentUserId.Value))
                .Select(x => new
                {
                    x.Id,
                    ParentUserId = x.ParentUserId!.Value
                })
                .ToListAsync(cancellationToken);
            var directCounts = directLinks
                .GroupBy(x => x.ParentUserId)
                .ToDictionary(x => x.Key, x => x.Count());

            var directIds = directLinks.Select(x => x.Id).ToList();
            var indirectLinks = await db.WxUsers.AsNoTracking()
                .Where(x => x.ParentUserId.HasValue && directIds.Contains(x.ParentUserId.Value))
                .Select(x => new
                {
                    DirectUserId = x.ParentUserId!.Value
                })
                .ToListAsync(cancellationToken);
            var directParentLookup = directLinks.ToDictionary(x => x.Id, x => x.ParentUserId);
            var indirectCounts = indirectLinks
                .Where(x => directParentLookup.ContainsKey(x.DirectUserId))
                .GroupBy(x => directParentLookup[x.DirectUserId])
                .ToDictionary(x => x.Key, x => x.Count());

            var items = users
                .Select(user => ToAdminMemberItem(
                    user,
                    directCounts.TryGetValue(user.Id, out var directCount) ? directCount : 0,
                    indirectCounts.TryGetValue(user.Id, out var indirectCount) ? indirectCount : 0))
                .ToList();

            return Results.Ok(new AdminMemberPage(items, total, currentPage, currentPageSize));
        });

        admin.MapPost("/members", async (
            AdminMemberRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!IsSystemAdmin(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.OpenId))
            {
                return Results.BadRequest(new ApiError("validation_error", "用户名或 OpenID 不能为空"));
            }
            if (await db.WxUsers.AnyAsync(x => x.OpenId == request.OpenId.Trim(), cancellationToken))
            {
                return Results.Conflict(new ApiError("member_exists", "该会员标识已存在"));
            }
            if (request.ParentUserId.HasValue &&
                !await db.WxUsers.AnyAsync(x => x.Id == request.ParentUserId.Value, cancellationToken))
            {
                return Results.BadRequest(new ApiError("parent_member_not_found", "直属推荐人不存在"));
            }

            var user = new WxUser
            {
                Id = Guid.NewGuid(),
                OpenId = request.OpenId.Trim(),
                Nickname = request.Nickname?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                AvatarUrl = request.AvatarUrl?.Trim(),
                Gender = request.Gender?.Trim(),
                BalanceCents = Math.Max(0, request.BalanceCents),
                CommissionCents = Math.Max(0, request.CommissionCents),
                ParentUserId = request.ParentUserId,
                IsEnabled = request.IsEnabled
            };
            db.WxUsers.Add(user);
            db.AuditLogs.Add(CreateAudit(principal, "member.create", "WxUser", user.Id.ToString(), user.OpenId));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/members/{user.Id}", ToAdminMemberItem(user, 0, 0));
        });

        admin.MapPut("/members/{id:guid}", async (
            Guid id,
            AdminMemberRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!IsSystemAdmin(principal))
            {
                return Results.Forbid();
            }
            var user = await db.WxUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new ApiError("member_not_found", "会员不存在"));
            }
            if (string.IsNullOrWhiteSpace(request.OpenId))
            {
                return Results.BadRequest(new ApiError("validation_error", "用户名或 OpenID 不能为空"));
            }
            if (await db.WxUsers.AnyAsync(x => x.Id != id && x.OpenId == request.OpenId.Trim(), cancellationToken))
            {
                return Results.Conflict(new ApiError("member_exists", "该会员标识已存在"));
            }
            user.OpenId = request.OpenId.Trim();
            user.Nickname = request.Nickname?.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.AvatarUrl = request.AvatarUrl?.Trim();
            user.Gender = request.Gender?.Trim();
            user.BalanceCents = Math.Max(0, request.BalanceCents);
            user.CommissionCents = Math.Max(0, request.CommissionCents);
            user.ParentUserId = request.ParentUserId == id ? null : request.ParentUserId;
            user.IsEnabled = request.IsEnabled;
            user.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "member.update", "WxUser", id.ToString(), user.OpenId));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminMemberItem(user, 0, 0));
        });

        admin.MapDelete("/members/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!IsSystemAdmin(principal))
            {
                return Results.Forbid();
            }
            var user = await db.WxUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new ApiError("member_not_found", "会员不存在"));
            }
            var relatedOrders = await db.BookingOrders.Where(x => x.WxUserId == id).ToListAsync(cancellationToken);
            foreach (var order in relatedOrders)
            {
                order.WxUserId = Guid.Empty;
            }
            db.WxUsers.Remove(user);
            db.AuditLogs.Add(CreateAudit(principal, "member.delete", "WxUser", id.ToString(), user.OpenId));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapReviews(RouteGroupBuilder admin)
    {
        admin.MapGet("/reviews", async (
            string? status,
            string? keyword,
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageReviews(principal))
            {
                return Results.Forbid();
            }

            var normalizedStatus = NormalizeReviewStatus(status);
            if (!string.IsNullOrWhiteSpace(status) && normalizedStatus is null)
            {
                return Results.BadRequest(new ApiError("invalid_review_status", "评价状态不正确"));
            }

            var query = db.Reviews
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Store)
                .Include(x => x.RoomType)
                .Include(x => x.Moderation)
                .AsQueryable();

            if (GetStoreScope(principal) is Guid storeScope)
            {
                storeId = storeScope;
            }
            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }
            if (normalizedStatus is not null)
            {
                query = normalizedStatus switch
                {
                    ReviewStatuses.Pending => query.Where(x =>
                        x.Moderation != null &&
                        x.Moderation.Status == ReviewStatuses.Pending),
                    ReviewStatuses.Hidden => query.Where(x =>
                        x.Moderation != null &&
                        x.Moderation.Status == ReviewStatuses.Hidden),
                    _ => query.Where(x =>
                        x.Moderation == null ||
                        x.Moderation.Status == ReviewStatuses.Published)
                };
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var text = keyword.Trim();
                query = query.Where(x =>
                    x.Content.Contains(text) ||
                    (x.User != null && x.User.Nickname != null && x.User.Nickname.Contains(text)) ||
                    (x.Store != null && x.Store.Name.Contains(text)) ||
                    (x.RoomType != null && x.RoomType.Name.Contains(text)));
            }

            var reviews = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);
            return Results.Ok(reviews.Select(ToAdminReviewItem).ToList());
        });

        admin.MapPut("/reviews/{id:guid}", async (
            Guid id,
            AdminReviewRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageReviews(principal))
            {
                return Results.Forbid();
            }

            var review = await db.Reviews
                .Include(x => x.User)
                .Include(x => x.Store)
                .Include(x => x.RoomType)
                .Include(x => x.Moderation)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (review is null)
            {
                return Results.NotFound(new ApiError("review_not_found", "评价不存在"));
            }
            if (!CanAccessStore(principal, review.StoreId))
            {
                return Results.Forbid();
            }
            if (request.Content is not null && request.Content.Trim().Length > 500)
            {
                return Results.BadRequest(new ApiError("review_content_too_long", "评价内容最多 500 个字"));
            }
            if (request.Score is < 1 or > 5)
            {
                return Results.BadRequest(new ApiError("invalid_rating", "评价星级必须在 1 到 5 分之间"));
            }

            var nextStatus = NormalizeReviewStatus(request.Status);
            if (!string.IsNullOrWhiteSpace(request.Status) && nextStatus is null)
            {
                return Results.BadRequest(new ApiError("invalid_review_status", "评价状态不正确"));
            }

            if (request.Content is not null)
            {
                review.Content = request.Content.Trim();
            }
            if (request.Score is int score)
            {
                review.RoomRating = score;
                review.StoreRating = score;
            }

            var moderation = review.Moderation;
            if (moderation is null)
            {
                moderation = new ReviewModeration
                {
                    ReviewId = review.Id,
                    Status = nextStatus ?? ReviewStatuses.Published,
                    Review = review
                };
                db.ReviewModerations.Add(moderation);
                review.Moderation = moderation;
            }
            if (nextStatus is not null)
            {
                moderation.Status = nextStatus;
                moderation.ReviewedAt = nextStatus == ReviewStatuses.Pending
                    ? null
                    : DateTime.UtcNow;
                moderation.ReviewedByAdminId = nextStatus == ReviewStatuses.Pending
                    ? null
                    : GetUserId(principal);
            }
            if (request.ReviewComment is not null)
            {
                moderation.ReviewComment = request.ReviewComment.Trim();
            }
            moderation.UpdatedAt = DateTime.UtcNow;

            db.AuditLogs.Add(CreateAudit(
                principal,
                nextStatus == ReviewStatuses.Published ? "review.publish" :
                nextStatus == ReviewStatuses.Hidden ? "review.hide" : "review.update",
                "Review",
                review.Id.ToString(),
                review.Content));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminReviewItem(review));
        });

        admin.MapDelete("/reviews/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageReviews(principal))
            {
                return Results.Forbid();
            }

            var review = await db.Reviews
                .Include(x => x.Moderation)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (review is null)
            {
                return Results.NotFound(new ApiError("review_not_found", "评价不存在"));
            }
            if (!CanAccessStore(principal, review.StoreId))
            {
                return Results.Forbid();
            }

            db.Reviews.Remove(review);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "review.delete",
                "Review",
                review.Id.ToString(),
                review.Content));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapStoreStaff(RouteGroupBuilder admin)
    {
        admin.MapGet("/store-staff", async (
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreStaff(principal))
            {
                return Results.Forbid();
            }

            var query = db.AdminUsers
                .AsNoTracking()
                .Include(x => x.Store)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Include(x => x.StoreStaffProfile)
                .Where(x => x.UserRoles.Any(link =>
                    link.Role != null &&
                    (link.Role.Code == RoleCodes.StoreAdmin ||
                     link.Role.Code == RoleCodes.StoreStaff)));

            if (GetStoreScope(principal) is Guid storeScope)
            {
                storeId = storeScope;
            }
            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            var users = await query
                .OrderBy(x => x.Username)
                .ToListAsync(cancellationToken);
            return Results.Ok(users.Select(ToStoreStaffItem).ToList());
        });

        admin.MapPost("/store-staff", async (
            StoreStaffRequest request,
            AppDbContext db,
            IPasswordHasher<AdminUser> passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreStaff(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                request.StoreId is null)
            {
                return Results.BadRequest(new ApiError("validation_error", "用户名、手机号和门店不能为空"));
            }
            if (!CanAccessStore(principal, request.StoreId.Value))
            {
                return Results.Forbid();
            }
            if (await db.AdminUsers.AnyAsync(x => x.Username == request.Username.Trim(), cancellationToken))
            {
                return Results.Conflict(new ApiError("admin_user_exists", "员工账号已存在"));
            }
            if (!await db.Stores.AnyAsync(x => x.Id == request.StoreId.Value, cancellationToken))
            {
                return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
            }

            var user = new AdminUser
            {
                Id = Guid.NewGuid(),
                Username = request.Username.Trim(),
                DisplayName = request.Username.Trim(),
                PhoneNumber = request.Phone?.Trim(),
                StoreId = request.StoreId,
                IsEnabled = NormalizeStaffStatus(request.Status) == "normal"
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "Admin@123456");
            db.AdminUsers.Add(user);

            var role = await db.Roles.SingleAsync(x => x.Code == RoleCodes.StoreStaff, cancellationToken);
            db.AdminUserRoles.Add(new AdminUserRole
            {
                AdminUserId = user.Id,
                RoleId = role.Id,
                AdminUser = user,
                Role = role
            });
            var profile = new StoreStaffProfile
            {
                AdminUserId = user.Id,
                AdminUser = user,
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? "未知" : request.Gender.Trim(),
                PermissionKeysJson = JsonSerializer.Serialize(request.Permissions ?? Array.Empty<string>())
            };
            db.StoreStaffProfiles.Add(profile);
            db.AuditLogs.Add(CreateAudit(principal, "store_staff.create", "AdminUser", user.Id.ToString(), user.Username));
            await db.SaveChangesAsync(cancellationToken);
            user.Store = await db.Stores.SingleAsync(x => x.Id == user.StoreId, cancellationToken);
            user.UserRoles = new List<AdminUserRole> { new() { Role = role } };
            user.StoreStaffProfile = profile;
            return Results.Created($"/api/v1/admin/store-staff/{user.Id}", ToStoreStaffItem(user));
        });

        admin.MapPut("/store-staff/{id:guid}", async (
            Guid id,
            StoreStaffRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreStaff(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Username) ||
                request.StoreId is null ||
                string.IsNullOrWhiteSpace(request.Phone))
            {
                return Results.BadRequest(new ApiError("validation_error", "用户名、手机号和门店不能为空"));
            }

            var user = await db.AdminUsers
                .Include(x => x.Store)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Include(x => x.StoreStaffProfile)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null ||
                !user.UserRoles.Any(link =>
                    link.Role?.Code is RoleCodes.StoreAdmin or RoleCodes.StoreStaff))
            {
                return Results.NotFound(new ApiError("store_staff_not_found", "员工账号不存在"));
            }
            if (!CanAccessStore(principal, user.StoreId ?? Guid.Empty) ||
                !CanAccessStore(principal, request.StoreId.Value))
            {
                return Results.Forbid();
            }
            if (!await db.Stores.AnyAsync(x => x.Id == request.StoreId.Value, cancellationToken))
            {
                return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
            }
            var username = request.Username.Trim();
            if (await db.AdminUsers.AnyAsync(
                    x => x.Id != id && x.Username == username,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("admin_user_exists", "员工账号已存在"));
            }

            user.Username = username;
            user.DisplayName = username;
            user.PhoneNumber = request.Phone?.Trim();
            user.StoreId = request.StoreId;
            user.IsEnabled = NormalizeStaffStatus(request.Status) == "normal";
            user.UpdatedAt = DateTime.UtcNow;
            var profile = user.StoreStaffProfile;
            if (profile is null)
            {
                profile = new StoreStaffProfile
                {
                    AdminUserId = user.Id,
                    AdminUser = user
                };
                db.StoreStaffProfiles.Add(profile);
                user.StoreStaffProfile = profile;
            }
            profile.Gender = string.IsNullOrWhiteSpace(request.Gender) ? "未知" : request.Gender.Trim();
            profile.PermissionKeysJson = JsonSerializer.Serialize(request.Permissions ?? Array.Empty<string>());
            profile.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "store_staff.update", "AdminUser", id.ToString(), user.Username));
            await db.SaveChangesAsync(cancellationToken);
            user.Store = await db.Stores.SingleAsync(x => x.Id == user.StoreId, cancellationToken);
            return Results.Ok(ToStoreStaffItem(user));
        });

        admin.MapDelete("/store-staff/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageStoreStaff(principal))
            {
                return Results.Forbid();
            }
            if (GetUserId(principal) == id)
            {
                return Results.BadRequest(new ApiError("cannot_delete_current_user", "不能删除当前登录账号"));
            }

            var user = await db.AdminUsers
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null ||
                !user.UserRoles.Any(link =>
                    link.Role?.Code is RoleCodes.StoreAdmin or RoleCodes.StoreStaff))
            {
                return Results.NotFound(new ApiError("store_staff_not_found", "员工账号不存在"));
            }
            if (!CanAccessStore(principal, user.StoreId ?? Guid.Empty))
            {
                return Results.Forbid();
            }

            db.AdminUsers.Remove(user);
            db.AuditLogs.Add(CreateAudit(principal, "store_staff.delete", "AdminUser", id.ToString(), user.Username));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapPermissionMenus(RouteGroupBuilder admin)
    {
        admin.MapGet("/permission-menus", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var menus = await db.PermissionMenus.AsNoTracking()
                .OrderBy(x => x.Weight)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);
            return Results.Ok(menus.Select(ToPermissionMenuItem).ToList());
        });

        admin.MapPost("/permission-menus", async (
            PermissionMenuRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            var validation = ValidatePermissionMenu(request);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }
            if (await db.PermissionMenus.AnyAsync(x => x.Key == request.Key.Trim(), cancellationToken))
            {
                return Results.Conflict(new ApiError("permission_menu_exists", "权限菜单标识已存在"));
            }

            var menu = new PermissionMenu
            {
                Key = request.Key.Trim(),
                Title = request.Title.Trim(),
                IconKey = request.IconKey?.Trim() ?? string.Empty,
                IconUrl = request.IconUrl,
                Route = request.Route.Trim(),
                Weight = Math.Max(0, request.Weight),
                Status = NormalizePermissionMenuStatus(request.Status)
            };
            db.PermissionMenus.Add(menu);
            db.AuditLogs.Add(CreateAudit(principal, "permission_menu.create", "PermissionMenu", menu.Key, menu.Title));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/permission-menus/{menu.Key}", ToPermissionMenuItem(menu));
        });

        admin.MapPut("/permission-menus/{key}", async (
            string key,
            PermissionMenuRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            var normalizedKey = key.Trim();
            var validation = ValidatePermissionMenu(request with { Key = normalizedKey });
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }
            var menu = await db.PermissionMenus.SingleOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);
            if (menu is null)
            {
                return Results.NotFound(new ApiError("permission_menu_not_found", "权限菜单不存在"));
            }

            menu.Title = request.Title.Trim();
            menu.IconKey = request.IconKey?.Trim() ?? string.Empty;
            menu.IconUrl = request.IconUrl;
            menu.Route = request.Route.Trim();
            menu.Weight = Math.Max(0, request.Weight);
            menu.Status = NormalizePermissionMenuStatus(request.Status);
            menu.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "permission_menu.update", "PermissionMenu", menu.Key, menu.Title));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToPermissionMenuItem(menu));
        });

        admin.MapPost("/permission-menus/sync", async (
            PermissionMenuSyncRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            if (request.Items is null || request.Items.Count == 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "同步菜单不能为空"));
            }

            var existing = await db.PermissionMenus.ToDictionaryAsync(x => x.Key, cancellationToken);
            var now = DateTime.UtcNow;
            var weight = 0;
            foreach (var requestItem in request.Items)
            {
                var validation = ValidatePermissionMenu(requestItem);
                if (validation is not null)
                {
                    return Results.BadRequest(new ApiError("validation_error", validation));
                }

                weight++;
                var normalizedKey = requestItem.Key.Trim();
                if (!existing.TryGetValue(normalizedKey, out var menu))
                {
                    menu = new PermissionMenu
                    {
                        Key = normalizedKey,
                        CreatedAt = now
                    };
                    db.PermissionMenus.Add(menu);
                    existing[normalizedKey] = menu;
                }

                menu.Title = requestItem.Title.Trim();
                menu.IconKey = requestItem.IconKey?.Trim() ?? string.Empty;
                menu.Route = requestItem.Route.Trim();
                menu.Weight = menu.Weight <= 0 ? weight : menu.Weight;
                menu.Status = string.IsNullOrWhiteSpace(menu.Status)
                    ? "normal"
                    : NormalizePermissionMenuStatus(menu.Status);
                menu.UpdatedAt = now;
            }

            db.AuditLogs.Add(CreateAudit(
                principal,
                "permission_menu.sync",
                "PermissionMenu",
                "merchant",
                $"{request.Items.Count} 个菜单"));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(existing.Values
                .OrderBy(x => x.Weight)
                .ThenBy(x => x.Title)
                .Select(ToPermissionMenuItem)
                .ToList());
        });

        admin.MapDelete("/permission-menus/{key}", async (
            string key,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            var menu = await db.PermissionMenus.SingleOrDefaultAsync(x => x.Key == key.Trim(), cancellationToken);
            if (menu is null)
            {
                return Results.NotFound(new ApiError("permission_menu_not_found", "权限菜单不存在"));
            }

            db.PermissionMenus.Remove(menu);
            db.AuditLogs.Add(CreateAudit(principal, "permission_menu.delete", "PermissionMenu", menu.Key, menu.Title));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapSubscriptionMessages(RouteGroupBuilder admin)
    {
        admin.MapGet("/subscription-templates", async (
            string? keyword,
            bool? includeDisabled,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var query = db.SubscriptionTemplates.AsNoTracking().AsQueryable();
            if (includeDisabled != true)
            {
                query = query.Where(x => x.IsEnabled);
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var text = keyword.Trim();
                query = query.Where(x =>
                    x.Identifier.Contains(text) ||
                    x.Name.Contains(text) ||
                    x.TemplateId.Contains(text));
            }

            var items = await query
                .OrderBy(x => x.UpdatedAt)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(ToSubscriptionTemplateItem).ToList());
        });

        admin.MapGet("/subscription-templates/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.SubscriptionTemplates.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            return item is null
                ? Results.NotFound(new ApiError("subscription_template_not_found", "订阅消息模板不存在"))
                : Results.Ok(ToSubscriptionTemplateItem(item));
        });

        admin.MapPost("/subscription-templates", async (
            SubscriptionTemplateRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var validation = ValidateSubscriptionTemplate(request);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }

            var identifier = request.Identifier.Trim();
            if (await db.SubscriptionTemplates.AnyAsync(x => x.Identifier == identifier, cancellationToken))
            {
                return Results.Conflict(new ApiError("subscription_template_exists", "订阅消息模板标识已存在"));
            }

            var now = DateTime.UtcNow;
            var item = new MiniProgramSubscriptionTemplate
            {
                Id = Guid.NewGuid(),
                Type = NormalizeSubscriptionTemplateType(request.Type),
                Identifier = identifier,
                Name = request.Name.Trim(),
                TemplateId = request.TemplateId.Trim(),
                ParametersJson = SerializeSubscriptionParameters(request.Parameters),
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.SubscriptionTemplates.Add(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "subscription_template.create",
                "MiniProgramSubscriptionTemplate",
                item.Id.ToString(),
                item.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/admin/subscription-templates/{item.Id}",
                ToSubscriptionTemplateItem(item));
        });

        admin.MapPut("/subscription-templates/{id:guid}", async (
            Guid id,
            SubscriptionTemplateRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.SubscriptionTemplates
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("subscription_template_not_found", "订阅消息模板不存在"));
            }

            var validation = ValidateSubscriptionTemplate(request);
            if (validation is not null)
            {
                return Results.BadRequest(new ApiError("validation_error", validation));
            }

            var identifier = request.Identifier.Trim();
            if (await db.SubscriptionTemplates.AnyAsync(
                    x => x.Id != id && x.Identifier == identifier,
                    cancellationToken))
            {
                return Results.Conflict(new ApiError("subscription_template_exists", "订阅消息模板标识已存在"));
            }

            item.Type = NormalizeSubscriptionTemplateType(request.Type);
            item.Identifier = identifier;
            item.Name = request.Name.Trim();
            item.TemplateId = request.TemplateId.Trim();
            item.ParametersJson = SerializeSubscriptionParameters(request.Parameters);
            item.IsEnabled = request.IsEnabled;
            item.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                "subscription_template.update",
                "MiniProgramSubscriptionTemplate",
                item.Id.ToString(),
                item.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToSubscriptionTemplateItem(item));
        });

        admin.MapDelete("/subscription-templates/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.SubscriptionTemplates
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("subscription_template_not_found", "订阅消息模板不存在"));
            }

            db.SubscriptionTemplates.Remove(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "subscription_template.delete",
                "MiniProgramSubscriptionTemplate",
                item.Id.ToString(),
                item.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });

        admin.MapGet("/message-logs", async (
            string? keyword,
            Guid? subscriptionTemplateId,
            Guid? memberId,
            string? sendStatus,
            int? page,
            int? pageSize,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var currentPage = Math.Max(page ?? 1, 1);
            var currentPageSize = Math.Clamp(pageSize ?? 20, 1, 100);
            var query = db.MessageLogs.AsNoTracking().AsQueryable();
            if (subscriptionTemplateId.HasValue)
            {
                query = query.Where(x => x.SubscriptionTemplateId == subscriptionTemplateId);
            }
            if (memberId.HasValue)
            {
                query = query.Where(x => x.MemberId == memberId);
            }
            if (!string.IsNullOrWhiteSpace(sendStatus))
            {
                query = query.Where(x => x.SendStatus == sendStatus.Trim());
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var text = keyword.Trim();
                query = query.Where(x =>
                    x.TemplateName.Contains(text) ||
                    x.TemplateId.Contains(text) ||
                    (x.OpenId != null && x.OpenId.Contains(text)) ||
                    (x.SendResultJson != null && x.SendResultJson.Contains(text)));
            }

            var total = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync(cancellationToken);
            return Results.Ok(new MessageLogPage(
                logs.Select(ToMessageLogItem).ToList(),
                total,
                currentPage,
                currentPageSize));
        });

        admin.MapGet("/message-logs/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.MessageLogs.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            return item is null
                ? Results.NotFound(new ApiError("message_log_not_found", "消息日志不存在"))
                : Results.Ok(ToMessageLogItem(item));
        });

        admin.MapPost("/message-logs", async (
            MessageLogRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var template = request.SubscriptionTemplateId.HasValue
                ? await db.SubscriptionTemplates.SingleOrDefaultAsync(
                    x => x.Id == request.SubscriptionTemplateId.Value,
                    cancellationToken)
                : null;
            if (request.SubscriptionTemplateId.HasValue && template is null)
            {
                return Results.BadRequest(new ApiError("subscription_template_not_found", "关联的订阅消息模板不存在"));
            }
            if (string.IsNullOrWhiteSpace(request.TemplateId) &&
                string.IsNullOrWhiteSpace(template?.TemplateId))
            {
                return Results.BadRequest(new ApiError("validation_error", "模板 ID 不能为空"));
            }
            if (request.MemberId.HasValue &&
                !await db.WxUsers.AnyAsync(x => x.Id == request.MemberId.Value, cancellationToken))
            {
                return Results.BadRequest(new ApiError("member_not_found", "关联会员不存在"));
            }

            var now = DateTime.UtcNow;
            var item = new MiniProgramMessageLog
            {
                Id = Guid.NewGuid(),
                SubscriptionTemplateId = template?.Id ?? request.SubscriptionTemplateId,
                TemplateName = string.IsNullOrWhiteSpace(request.TemplateName)
                    ? template?.Name ?? "未命名模板"
                    : request.TemplateName.Trim(),
                TemplateId = string.IsNullOrWhiteSpace(request.TemplateId)
                    ? template!.TemplateId
                    : request.TemplateId.Trim(),
                MemberId = request.MemberId,
                OpenId = request.OpenId?.Trim(),
                TemplateDataJson = ToRawJson(request.Template),
                SendResultJson = ToRawJson(request.SendResult),
                SendStatus = NormalizeMessageSendStatus(request.SendStatus),
                CreatedAt = now,
                UpdatedAt = now
            };
            if (item.MemberId.HasValue && string.IsNullOrWhiteSpace(item.OpenId))
            {
                item.OpenId = await db.WxUsers
                    .Where(x => x.Id == item.MemberId.Value)
                    .Select(x => x.OpenId)
                    .SingleOrDefaultAsync(cancellationToken);
            }

            db.MessageLogs.Add(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "message_log.create",
                "MiniProgramMessageLog",
                item.Id.ToString(),
                item.TemplateName));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/admin/message-logs/{item.Id}",
                ToMessageLogItem(item));
        });

        admin.MapPut("/message-logs/{id:guid}", async (
            Guid id,
            MessageLogRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.MessageLogs
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("message_log_not_found", "消息日志不存在"));
            }

            var template = request.SubscriptionTemplateId.HasValue
                ? await db.SubscriptionTemplates.SingleOrDefaultAsync(
                    x => x.Id == request.SubscriptionTemplateId.Value,
                    cancellationToken)
                : null;
            if (request.SubscriptionTemplateId.HasValue && template is null)
            {
                return Results.BadRequest(new ApiError("subscription_template_not_found", "关联的订阅消息模板不存在"));
            }
            if (string.IsNullOrWhiteSpace(request.TemplateId) &&
                string.IsNullOrWhiteSpace(template?.TemplateId) &&
                string.IsNullOrWhiteSpace(item.TemplateId))
            {
                return Results.BadRequest(new ApiError("validation_error", "模板 ID 不能为空"));
            }
            if (request.MemberId.HasValue &&
                !await db.WxUsers.AnyAsync(x => x.Id == request.MemberId.Value, cancellationToken))
            {
                return Results.BadRequest(new ApiError("member_not_found", "关联会员不存在"));
            }

            item.SubscriptionTemplateId = template?.Id ?? request.SubscriptionTemplateId ?? item.SubscriptionTemplateId;
            item.TemplateName = string.IsNullOrWhiteSpace(request.TemplateName)
                ? template?.Name ?? item.TemplateName
                : request.TemplateName.Trim();
            item.TemplateId = string.IsNullOrWhiteSpace(request.TemplateId)
                ? template?.TemplateId ?? item.TemplateId
                : request.TemplateId.Trim();
            item.MemberId = request.MemberId ?? item.MemberId;
            item.OpenId = string.IsNullOrWhiteSpace(request.OpenId)
                ? item.OpenId
                : request.OpenId.Trim();
            item.TemplateDataJson = request.Template.HasValue
                ? ToRawJson(request.Template)
                : item.TemplateDataJson;
            item.SendResultJson = request.SendResult.HasValue
                ? ToRawJson(request.SendResult)
                : item.SendResultJson;
            item.SendStatus = request.SendStatus is null
                ? item.SendStatus
                : NormalizeMessageSendStatus(request.SendStatus);
            if (item.MemberId.HasValue && string.IsNullOrWhiteSpace(item.OpenId))
            {
                item.OpenId = await db.WxUsers
                    .Where(x => x.Id == item.MemberId.Value)
                    .Select(x => x.OpenId)
                    .SingleOrDefaultAsync(cancellationToken);
            }
            item.UpdatedAt = DateTime.UtcNow;

            db.AuditLogs.Add(CreateAudit(
                principal,
                "message_log.update",
                "MiniProgramMessageLog",
                item.Id.ToString(),
                item.TemplateName));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToMessageLogItem(item));
        });

        admin.MapDelete("/message-logs/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.MessageLogs
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("message_log_not_found", "消息日志不存在"));
            }

            db.MessageLogs.Remove(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "message_log.delete",
                "MiniProgramMessageLog",
                item.Id.ToString(),
                item.TemplateName));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapManagedItems(RouteGroupBuilder admin)
    {
        admin.MapGet("/managed-items/{resourceType}", async (
            string resourceType,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            if (!IsValidResourceType(resourceType))
            {
                return Results.BadRequest(new ApiError("invalid_resource_type", "配置资源类型不正确"));
            }

            var items = await db.AdminManagedItems.AsNoTracking()
                .Where(x => x.ResourceType == resourceType)
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);
            return Results.Ok(items.Select(ToManagedItem).ToList());
        });

        admin.MapPost("/managed-items/{resourceType}", async (
            string resourceType,
            ManagedItemRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            if (!IsValidResourceType(resourceType) || request.Data.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new ApiError("validation_error", "配置数据必须是对象"));
            }

            var now = DateTime.UtcNow;
            var item = new AdminManagedItem
            {
                Id = Guid.NewGuid(),
                ResourceType = resourceType,
                DataJson = request.Data.GetRawText(),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.AdminManagedItems.Add(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "managed_item.create",
                resourceType,
                item.Id.ToString(),
                ReadManagedItemSummary(request.Data)));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/managed-items/{resourceType}/{item.Id}", ToManagedItem(item));
        });

        admin.MapPut("/managed-items/{resourceType}/{id:guid}", async (
            string resourceType,
            Guid id,
            ManagedItemRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }
            if (!IsValidResourceType(resourceType) || request.Data.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new ApiError("validation_error", "配置数据必须是对象"));
            }

            var item = await db.AdminManagedItems
                .SingleOrDefaultAsync(x => x.Id == id && x.ResourceType == resourceType, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("managed_item_not_found", "配置记录不存在"));
            }

            var data = DeserializeManagedData(item.DataJson);
            foreach (var property in request.Data.EnumerateObject())
            {
                data[property.Name] = property.Value.Clone();
            }
            item.DataJson = JsonSerializer.Serialize(data);
            item.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                "managed_item.update",
                resourceType,
                item.Id.ToString(),
                ReadManagedItemSummary(request.Data)));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToManagedItem(item));
        });

        admin.MapDelete("/managed-items/{resourceType}/{id:guid}", async (
            string resourceType,
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageConfiguration(principal))
            {
                return Results.Forbid();
            }

            var item = await db.AdminManagedItems
                .SingleOrDefaultAsync(x => x.Id == id && x.ResourceType == resourceType, cancellationToken);
            if (item is null)
            {
                return Results.NotFound(new ApiError("managed_item_not_found", "配置记录不存在"));
            }

            db.AdminManagedItems.Remove(item);
            db.AuditLogs.Add(CreateAudit(
                principal,
                "managed_item.delete",
                resourceType,
                item.Id.ToString(),
                ReadManagedItemSummary(item.DataJson)));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapWithdrawals(RouteGroupBuilder admin)
    {
        admin.MapGet("/withdrawals", async (
            string? status,
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageWithdrawals(principal))
            {
                return Results.Forbid();
            }

            var normalizedStatus = NormalizeWithdrawalStatus(status);
            if (!string.IsNullOrWhiteSpace(status) && normalizedStatus is null)
            {
                return Results.BadRequest(new ApiError("invalid_withdrawal_status", "提现状态不正确"));
            }
            if (GetStoreScope(principal) is Guid storeScope)
            {
                storeId = storeScope;
            }

            var query = db.WithdrawalRecords
                .AsNoTracking()
                .Include(x => x.Store)
                .AsQueryable();
            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }
            if (normalizedStatus is not null)
            {
                query = query.Where(x => x.Status == normalizedStatus);
            }

            var records = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);
            return Results.Ok(records.Select(ToAdminWithdrawalItem).ToList());
        });

        admin.MapPost("/withdrawals", async (
            WithdrawalRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageWithdrawals(principal))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Account) || request.AmountCents <= 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "收款账户和提现金额必须填写"));
            }

            var store = await ResolveStoreAsync(db, request.StoreId, request.Store, cancellationToken);
            if (store is null)
            {
                return Results.NotFound(new ApiError("store_not_found", "提现门店不存在"));
            }
            if (!CanAccessStore(principal, store.Id))
            {
                return Results.Forbid();
            }

            var record = new WithdrawalRecord
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Store = store,
                Account = request.Account.Trim(),
                AmountCents = request.AmountCents,
                Status = WithdrawalStatuses.PendingReview
            };
            db.WithdrawalRecords.Add(record);
            db.AuditLogs.Add(CreateAudit(principal, "withdrawal.create", "WithdrawalRecord", record.Id.ToString(), store.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/withdrawals/{record.Id}", ToAdminWithdrawalItem(record));
        });

        admin.MapPut("/withdrawals/{id:guid}", async (
            Guid id,
            WithdrawalRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageWithdrawals(principal))
            {
                return Results.Forbid();
            }
            var record = await db.WithdrawalRecords
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (record is null)
            {
                return Results.NotFound(new ApiError("withdrawal_not_found", "提现记录不存在"));
            }
            if (!CanAccessStore(principal, record.StoreId))
            {
                return Results.Forbid();
            }
            if (string.IsNullOrWhiteSpace(request.Account) || request.AmountCents <= 0)
            {
                return Results.BadRequest(new ApiError("validation_error", "收款账户和提现金额必须填写"));
            }

            if (request.StoreId.HasValue || !string.IsNullOrWhiteSpace(request.Store))
            {
                var nextStore = await ResolveStoreAsync(db, request.StoreId, request.Store, cancellationToken);
                if (nextStore is null)
                {
                    return Results.NotFound(new ApiError("store_not_found", "提现门店不存在"));
                }
                if (!CanAccessStore(principal, nextStore.Id))
                {
                    return Results.Forbid();
                }
                record.StoreId = nextStore.Id;
                record.Store = nextStore;
            }
            record.Account = request.Account.Trim();
            record.AmountCents = request.AmountCents;
            record.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(principal, "withdrawal.update", "WithdrawalRecord", id.ToString(), record.Store?.Name));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminWithdrawalItem(record));
        });

        admin.MapPost("/withdrawals/{id:guid}/review", async (
            Guid id,
            ReviewWithdrawalRequest request,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageWithdrawals(principal))
            {
                return Results.Forbid();
            }
            var record = await db.WithdrawalRecords
                .Include(x => x.Store)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (record is null)
            {
                return Results.NotFound(new ApiError("withdrawal_not_found", "提现记录不存在"));
            }
            if (!CanAccessStore(principal, record.StoreId))
            {
                return Results.Forbid();
            }
            if (record.Status != WithdrawalStatuses.PendingReview)
            {
                return Results.BadRequest(new ApiError("withdrawal_already_processed", "提现申请已经处理"));
            }
            if (!request.Approved && string.IsNullOrWhiteSpace(request.Comment))
            {
                return Results.BadRequest(new ApiError("review_comment_required", "驳回提现时必须填写原因"));
            }

            record.Status = request.Approved
                ? WithdrawalStatuses.Approved
                : WithdrawalStatuses.Rejected;
            record.ReviewedByAdminId = GetUserId(principal);
            record.ReviewedAt = DateTime.UtcNow;
            record.ReviewComment = request.Comment?.Trim();
            record.UpdatedAt = DateTime.UtcNow;
            db.AuditLogs.Add(CreateAudit(
                principal,
                request.Approved ? "withdrawal.approve" : "withdrawal.reject",
                "WithdrawalRecord",
                id.ToString(),
                record.ReviewComment));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminWithdrawalItem(record));
        });

        admin.MapDelete("/withdrawals/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!CanManageWithdrawals(principal))
            {
                return Results.Forbid();
            }
            var record = await db.WithdrawalRecords.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (record is null)
            {
                return Results.NotFound(new ApiError("withdrawal_not_found", "提现记录不存在"));
            }
            if (!CanAccessStore(principal, record.StoreId))
            {
                return Results.Forbid();
            }

            db.WithdrawalRecords.Remove(record);
            db.AuditLogs.Add(CreateAudit(principal, "withdrawal.delete", "WithdrawalRecord", id.ToString(), null));
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapStatistics(RouteGroupBuilder admin)
    {
        admin.MapGet("/stats/income", async (
            DateTime? from,
            DateTime? to,
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var range = NormalizeRange(from, to, 90);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }
            if (GetStoreScope(principal) is Guid scope)
            {
                storeId = scope;
            }

            var ordersQuery = db.BookingOrders
                .AsNoTracking()
                .Include(x => x.Store)
                .Where(x => x.CreatedAt >= range.From && x.CreatedAt < range.To &&
                            (x.PaymentStatus == "Paid" || x.PaymentStatus == "Refunded"));
            var refundsQuery = db.RefundRecords
                .AsNoTracking()
                .Include(x => x.Order)
                .ThenInclude(x => x!.Store)
                .Where(x => x.CreatedAt >= range.From &&
                            x.CreatedAt < range.To &&
                            x.Status == "Refunded");
            if (storeId.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.StoreId == storeId.Value);
                refundsQuery = refundsQuery.Where(x => x.Order!.StoreId == storeId.Value);
            }

            var orders = await ordersQuery.ToListAsync(cancellationToken);
            var refunds = await refundsQuery.ToListAsync(cancellationToken);
            var orderGroups = orders
                .GroupBy(x => new
                {
                    x.StoreId,
                    StoreName = x.Store?.Name ?? "未知门店",
                    Date = x.CreatedAt.Date
                })
                .ToDictionary(
                    x => (x.Key.StoreId, x.Key.Date),
                    x => new
                    {
                        x.Key.StoreName,
                        Orders = x.Count(),
                        Income = x.Sum(order => order.TotalAmountCents)
                    });
            var refundGroups = refunds
                .GroupBy(x => new
                {
                    StoreId = x.Order!.StoreId,
                    StoreName = x.Order.Store?.Name ?? "未知门店",
                    Date = x.CreatedAt.Date
                })
                .ToDictionary(
                    x => (x.Key.StoreId, x.Key.Date),
                    x => new
                    {
                        x.Key.StoreName,
                        Refund = x.Sum(refund => refund.AmountCents)
                    });

            var keys = orderGroups.Keys
                .Concat(refundGroups.Keys)
                .Distinct()
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.StoreId)
                .ToList();
            var items = keys.Select(key =>
            {
                orderGroups.TryGetValue(key, out var orderGroup);
                refundGroups.TryGetValue(key, out var refundGroup);
                var income = orderGroup?.Income ?? 0;
                var refund = refundGroup?.Refund ?? 0;
                return new AdminIncomeItem(
                    $"{key.StoreId:N}:{key.Date:yyyy-MM-dd}",
                    key.StoreId,
                    orderGroup?.StoreName ?? refundGroup?.StoreName ?? "未知门店",
                    orderGroup?.Orders ?? 0,
                    income,
                    refund,
                    Math.Max(0, income - refund),
                    key.Date);
            }).ToList();
            return Results.Ok(items);
        });

        admin.MapGet("/stats/trend", async (
            DateTime? from,
            DateTime? to,
            Guid? storeId,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var range = NormalizeRange(from, to, 90);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }
            if (GetStoreScope(principal) is Guid scope)
            {
                storeId = scope;
            }

            var ordersQuery = db.BookingOrders.AsNoTracking()
                .Where(x => x.CreatedAt >= range.From && x.CreatedAt < range.To);
            if (storeId.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.StoreId == storeId.Value);
            }
            var orders = await ordersQuery.ToListAsync(cancellationToken);

            var refundsQuery = db.RefundRecords.AsNoTracking()
                .Where(x => x.CreatedAt >= range.From && x.CreatedAt < range.To && x.Status == "Refunded");
            if (storeId.HasValue)
            {
                refundsQuery = refundsQuery.Where(x => x.Order!.StoreId == storeId.Value);
            }
            var refunds = await refundsQuery
                .Include(x => x.Order)
                .ToListAsync(cancellationToken);

            var items = Enumerable.Range(0, Math.Max(1, (int)(range.To - range.From).TotalDays))
                .Select(offset =>
                {
                    var date = range.From.AddDays(offset).Date;
                    var dayOrders = orders.Where(x => x.CreatedAt.Date == date).ToList();
                    var dayRefunds = refunds.Where(x => x.CreatedAt.Date == date).ToList();
                    return new DashboardTrendItem(
                        date,
                        dayOrders.Count,
                        dayOrders.Count(x => x.PaymentStatus == "Paid" || x.PaymentStatus == "Refunded"),
                        dayOrders.Where(x => x.PaymentStatus is "Paid" or "Refunded").Sum(x => x.TotalAmountCents),
                        dayRefunds.Sum(x => x.AmountCents));
                })
                .ToList();
            return Results.Ok(items);
        });

        admin.MapGet("/stats/store-rank", async (
            DateTime? from,
            DateTime? to,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var range = NormalizeRange(from, to, 90);
            if (range.Error is not null)
            {
                return Results.BadRequest(new ApiError("invalid_date_range", range.Error));
            }

            var query = db.BookingOrders.AsNoTracking()
                .Include(x => x.Store)
                .Where(x => x.CreatedAt >= range.From && x.CreatedAt < range.To);
            if (GetStoreScope(principal) is Guid scope)
            {
                query = query.Where(x => x.StoreId == scope);
            }
            var orders = await query.ToListAsync(cancellationToken);
            var items = orders
                .GroupBy(x => new { x.StoreId, StoreName = x.Store!.Name })
                .Select(group => new DashboardStoreRankItem(
                    group.Key.StoreId,
                    group.Key.StoreName,
                    group.Count(),
                    group.Where(x => x.PaymentStatus is "Paid" or "Refunded").Sum(x => x.TotalAmountCents)))
                .OrderByDescending(x => x.SalesCents)
                .ThenByDescending(x => x.OrderCount)
                .ToList();
            return Results.Ok(items);
        });
    }

    private static void MapNotifications(RouteGroupBuilder admin)
    {
        admin.MapGet("/notifications", async (
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var storeScope = GetStoreScope(principal);
            var items = new List<AdminNotificationItem>();
            var pendingOrderCount = 0;
            var pendingRefundCount = 0;
            var pendingStoreReviewCount = 0;
            var pendingRoomReviewCount = 0;
            var pendingReviewCount = 0;
            var pendingWithdrawalCount = 0;

            if (CanManageOrders(principal))
            {
                var ordersQuery = db.BookingOrders
                    .AsNoTracking()
                    .Include(x => x.Store)
                    .Include(x => x.RoomType)
                    .Where(x =>
                        x.Status == OrderStatuses.PaidPendingConfirmation ||
                        x.Status == OrderStatuses.ConfirmedPendingCheckIn);
                if (storeScope is Guid orderStoreScope)
                {
                    ordersQuery = ordersQuery.Where(x => x.StoreId == orderStoreScope);
                }

                pendingOrderCount = await ordersQuery.CountAsync(cancellationToken);
                var orders = await ordersQuery
                    .OrderByDescending(x => x.UpdatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(orders.Select(order =>
                {
                    var waitingForConfirmation = order.Status == OrderStatuses.PaidPendingConfirmation;
                    return new AdminNotificationItem(
                        $"order:{order.Id}",
                        "order",
                        waitingForConfirmation ? "订单待确认" : "订单待入住",
                        $"{order.Store?.Name ?? "未知门店"} · {order.RoomType?.Name ?? "未知房型"} · {order.OrderNumber}",
                        "financialOrders",
                        waitingForConfirmation ? "teal" : "blue",
                        order.UpdatedAt,
                        order.Id,
                        order.OrderNumber,
                        order.Store?.Name);
                }));
            }

            if (CanManageRefunds(principal))
            {
                var refundsQuery = db.RefundRecords
                    .AsNoTracking()
                    .Include(x => x.Order)
                    .ThenInclude(x => x!.Store)
                    .Include(x => x.Order)
                    .ThenInclude(x => x!.RoomType)
                    .Where(x => x.Status == "PendingReview");
                if (storeScope is Guid refundStoreScope)
                {
                    refundsQuery = refundsQuery.Where(x => x.Order!.StoreId == refundStoreScope);
                }

                pendingRefundCount = await refundsQuery.CountAsync(cancellationToken);
                var refunds = await refundsQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(refunds.Select(refund => new AdminNotificationItem(
                    $"refund:{refund.Id}",
                    "refund",
                    "退款申请待审核",
                    $"{refund.Order?.Store?.Name ?? "未知门店"} · {refund.Order?.OrderNumber ?? "未知订单"} · ¥{refund.AmountCents / 100.0m:F2}",
                    "refunds",
                    "orange",
                    refund.CreatedAt,
                    refund.Id,
                    refund.RefundNumber,
                    refund.Order?.Store?.Name)));
            }

            if (CanManageStoreProfile(principal))
            {
                var storesQuery = db.Stores
                    .AsNoTracking()
                    .Where(x => x.Status == StoreStatuses.PendingReview);
                if (storeScope is Guid storeReviewScope)
                {
                    storesQuery = storesQuery.Where(x => x.Id == storeReviewScope);
                }

                pendingStoreReviewCount = await storesQuery.CountAsync(cancellationToken);
                var stores = await storesQuery
                    .OrderByDescending(x => x.UpdatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(stores.Select(store => new AdminNotificationItem(
                    $"store:{store.Id}",
                    "store",
                    "门店资料待审核",
                    $"{store.Name} · 请检查门店资料和资质后决定是否通过",
                    "stores",
                    "red",
                    store.UpdatedAt,
                    store.Id,
                    store.Code,
                    store.Name)));
            }

            if (CanManageCatalog(principal))
            {
                var roomTypesQuery = db.RoomTypes
                    .AsNoTracking()
                    .Include(x => x.Store)
                    .Where(x => x.ApprovalStatus == "PendingReview");
                if (storeScope is Guid roomStoreScope)
                {
                    roomTypesQuery = roomTypesQuery.Where(x => x.StoreId == roomStoreScope);
                }

                pendingRoomReviewCount = await roomTypesQuery.CountAsync(cancellationToken);
                var roomTypes = await roomTypesQuery
                    .OrderByDescending(x => x.UpdatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(roomTypes.Select(roomType => new AdminNotificationItem(
                    $"room:{roomType.Id}",
                    "room",
                    "房间资料待审核",
                    $"{roomType.Store?.Name ?? "未知门店"} · {roomType.Name} · 请检查房源资料",
                    "roomManagement",
                    "blue",
                    roomType.UpdatedAt,
                    roomType.Id,
                    roomType.Name,
                    roomType.Store?.Name)));
            }

            if (CanManageReviews(principal))
            {
                var reviewsQuery = db.Reviews
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Store)
                    .Include(x => x.RoomType)
                    .Include(x => x.Moderation)
                    .Where(x => x.Moderation != null &&
                                x.Moderation.Status == ReviewStatuses.Pending);
                if (storeScope is Guid reviewStoreScope)
                {
                    reviewsQuery = reviewsQuery.Where(x => x.StoreId == reviewStoreScope);
                }

                pendingReviewCount = await reviewsQuery.CountAsync(cancellationToken);
                var reviews = await reviewsQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(reviews.Select(review => new AdminNotificationItem(
                    $"review:{review.Id}",
                    "review",
                    "评价待审核",
                    $"{review.Store?.Name ?? "未知门店"} · {review.User?.Nickname ?? "微信用户"} · 请检查评价内容",
                    "reviews",
                    "purple",
                    review.CreatedAt,
                    review.Id,
                    review.RoomType?.Name,
                    review.Store?.Name)));
            }

            if (CanManageWithdrawals(principal))
            {
                var withdrawalsQuery = db.WithdrawalRecords
                    .AsNoTracking()
                    .Include(x => x.Store)
                    .Where(x => x.Status == WithdrawalStatuses.PendingReview);
                if (storeScope is Guid withdrawalStoreScope)
                {
                    withdrawalsQuery = withdrawalsQuery.Where(x => x.StoreId == withdrawalStoreScope);
                }

                pendingWithdrawalCount = await withdrawalsQuery.CountAsync(cancellationToken);
                var withdrawals = await withdrawalsQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);
                items.AddRange(withdrawals.Select(withdrawal => new AdminNotificationItem(
                    $"withdrawal:{withdrawal.Id}",
                    "withdrawal",
                    "提现申请待审核",
                    $"{withdrawal.Store?.Name ?? "未知门店"} · ¥{withdrawal.AmountCents / 100.0m:F2} · {withdrawal.Account}",
                    "withdrawals",
                    "orange",
                    withdrawal.CreatedAt,
                    withdrawal.Id,
                    null,
                    withdrawal.Store?.Name)));
            }

            var pendingCount = pendingOrderCount +
                               pendingRefundCount +
                               pendingStoreReviewCount +
                               pendingRoomReviewCount +
                               pendingReviewCount +
                               pendingWithdrawalCount;
            var orderedItems = items
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Type)
                .Take(100)
                .ToList();
            return Results.Ok(new AdminNotificationCenter(
                pendingCount,
                pendingOrderCount,
                pendingRefundCount,
                pendingStoreReviewCount,
                pendingRoomReviewCount,
                pendingReviewCount,
                pendingWithdrawalCount,
                DateTime.UtcNow,
                orderedItems));
        });
    }

    private static void MapAuditLogs(RouteGroupBuilder admin)
    {
        admin.MapGet("/audit-logs", async (
            string? keyword,
            AppDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            var query = db.AuditLogs.AsNoTracking().AsQueryable();
            if (GetStoreScope(principal) is Guid storeId)
            {
                var scopedActorIds = await db.AdminUsers.AsNoTracking()
                    .Where(x => x.StoreId == storeId)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
                var scopedResourceIds = await db.RoomTypes.AsNoTracking()
                    .Where(x => x.StoreId == storeId)
                    .Select(x => x.Id.ToString())
                    .Concat(db.RoomUnits.AsNoTracking()
                        .Where(x => x.StoreId == storeId)
                        .Select(x => x.Id.ToString()))
                    .Concat(db.BookingOrders.AsNoTracking()
                        .Where(x => x.StoreId == storeId)
                        .Select(x => x.Id.ToString()))
                    .ToListAsync(cancellationToken);
                scopedResourceIds.Add(storeId.ToString());
                query = query.Where(x =>
                    (x.ActorId.HasValue && scopedActorIds.Contains(x.ActorId.Value)) ||
                    (x.ResourceId != null && scopedResourceIds.Contains(x.ResourceId)));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var text = keyword.Trim();
                query = query.Where(x =>
                    x.Action.Contains(text) ||
                    x.ResourceType.Contains(text) ||
                    (x.ResourceId != null && x.ResourceId.Contains(text)) ||
                    (x.Summary != null && x.Summary.Contains(text)));
            }

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);
            return Results.Ok(logs);
        });
    }

    private static AdminReviewItem ToAdminReviewItem(Review review)
    {
        var images = ParseReviewImageUrls(review.ImageUrlsJson);
        var status = review.Moderation?.Status switch
        {
            ReviewStatuses.Pending => "pending",
            ReviewStatuses.Hidden => "hidden",
            _ => "published"
        };
        return new AdminReviewItem(
            review.Id,
            review.OrderId,
            review.StoreId,
            review.RoomTypeId,
            review.User?.Nickname ?? "微信用户",
            review.Store?.Name ?? "未知门店",
            review.RoomType?.Name ?? "未知房型",
            images.FirstOrDefault(),
            images,
            review.Content,
            review.RoomRating,
            status,
            review.CreatedAt,
            review.Moderation?.ReviewedAt,
            review.Moderation?.ReviewComment);
    }

    private static SubscriptionTemplateItem ToSubscriptionTemplateItem(
        MiniProgramSubscriptionTemplate template)
    {
        return new SubscriptionTemplateItem(
            template.Id,
            template.Type,
            template.Identifier,
            template.Name,
            template.TemplateId,
            DeserializeSubscriptionParameters(template.ParametersJson),
            template.IsEnabled,
            template.CreatedAt,
            template.UpdatedAt);
    }

    private static MessageLogItem ToMessageLogItem(MiniProgramMessageLog log)
    {
        return new MessageLogItem(
            log.Id,
            log.SubscriptionTemplateId,
            log.TemplateName,
            log.TemplateId,
            log.MemberId,
            log.OpenId,
            log.TemplateDataJson,
            log.SendResultJson,
            log.SendStatus,
            log.CreatedAt,
            log.UpdatedAt);
    }

    private static StoreStaffItem ToStoreStaffItem(AdminUser user)
    {
        return new StoreStaffItem(
            user.Id,
            user.Username,
            user.DisplayName,
            user.StoreStaffProfile?.Gender ?? "未知",
            user.PhoneNumber,
            user.IsEnabled ? "normal" : "hidden",
            user.StoreId,
            user.Store?.Name,
            ParsePermissionKeys(user.StoreStaffProfile?.PermissionKeysJson),
            user.CreatedAt,
            user.UpdatedAt);
    }

    private static PermissionMenuItem ToPermissionMenuItem(PermissionMenu menu)
    {
        return new PermissionMenuItem(
            menu.Key,
            menu.Title,
            menu.IconKey,
            menu.IconUrl,
            menu.Route,
            menu.Weight,
            menu.Status,
            menu.CreatedAt,
            menu.UpdatedAt);
    }

    private static IReadOnlyList<SubscriptionTemplateParameter> DeserializeSubscriptionParameters(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SubscriptionTemplateParameter>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SubscriptionTemplateParameter>>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new List<SubscriptionTemplateParameter>();
        }
        catch (JsonException)
        {
            return Array.Empty<SubscriptionTemplateParameter>();
        }
    }

    private static string SerializeSubscriptionParameters(
        IReadOnlyList<SubscriptionTemplateParameter>? parameters)
    {
        return JsonSerializer.Serialize(
            parameters ?? Array.Empty<SubscriptionTemplateParameter>());
    }

    private static string? ToRawJson(JsonElement? value)
    {
        if (!value.HasValue ||
            value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }
        return value.Value.GetRawText();
    }

    private static IDictionary<string, object?> ToManagedItem(AdminManagedItem item)
    {
        Dictionary<string, object?> data;
        try
        {
            data = JsonSerializer.Deserialize<Dictionary<string, object?>>(item.DataJson)
                ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            data = new Dictionary<string, object?>();
        }

        data["id"] = item.Id;
        data["createdAt"] = item.CreatedAt;
        data["updatedAt"] = item.UpdatedAt;
        return data;
    }

    private static AdminWithdrawalItem ToAdminWithdrawalItem(WithdrawalRecord record)
    {
        return new AdminWithdrawalItem(
            record.Id,
            record.StoreId,
            record.Store?.Name ?? "未知门店",
            record.Account,
            record.AmountCents,
            record.Status switch
            {
                WithdrawalStatuses.PendingReview => "pending",
                WithdrawalStatuses.Approved => "approved",
                WithdrawalStatuses.Rejected => "rejected",
                WithdrawalStatuses.Completed => "completed",
                _ => record.Status
            },
            record.CreatedAt,
            record.ReviewedAt,
            record.ReviewComment);
    }

    private static async Task<Store?> ResolveStoreAsync(
        AppDbContext db,
        Guid? storeId,
        string? storeName,
        CancellationToken cancellationToken)
    {
        if (storeId is Guid id)
        {
            return await db.Stores.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(storeName))
        {
            return null;
        }

        var value = storeName.Trim();
        return await db.Stores
            .SingleOrDefaultAsync(x => x.Name == value || x.Code == value, cancellationToken);
    }

    private static Dictionary<string, JsonElement> DeserializeManagedData(string dataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(dataJson)
                ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string ReadManagedItemSummary(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "name", "title", "key", "slug" })
            {
                if (data.TryGetProperty(key, out var value) &&
                    value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString()!;
                }
            }
        }
        return data.GetRawText();
    }

    private static string ReadManagedItemSummary(string dataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(dataJson);
            return ReadManagedItemSummary(document.RootElement);
        }
        catch (JsonException)
        {
            return dataJson;
        }
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

    private static IReadOnlyList<string> ParseReviewImageUrls(string? json)
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

    private static string? ValidatePermissionMenu(PermissionMenuRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key) ||
            request.Key.Trim().Length > 80 ||
            !request.Key.Trim().All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
        {
            return "菜单标识只能包含字母、数字、短横线和下划线";
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "菜单标题不能为空";
        }
        if (string.IsNullOrWhiteSpace(request.Route) ||
            !request.Route.TrimStart().StartsWith("/", StringComparison.Ordinal))
        {
            return "页面路径必须以 / 开头";
        }
        if (request.Status is not ("normal" or "hidden"))
        {
            return "菜单状态只能是 normal 或 hidden";
        }
        return null;
    }

    private static string? ValidateSubscriptionTemplate(
        SubscriptionTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) ||
            request.Identifier.Trim().Length > 100 ||
            !request.Identifier.Trim().All(character =>
                char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
        {
            return "模板标识不能为空，且只能包含字母、数字、短横线、下划线和点号";
        }
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 100)
        {
            return "模板名称不能为空且不能超过 100 个字符";
        }
        if (string.IsNullOrWhiteSpace(request.TemplateId) ||
            request.TemplateId.Trim().Length > 200)
        {
            return "微信模板 ID 不能为空且不能超过 200 个字符";
        }
        if (request.Parameters is not null && request.Parameters.Count > 30)
        {
            return "模板参数不能超过 30 个";
        }
        foreach (var parameter in request.Parameters ?? Array.Empty<SubscriptionTemplateParameter>())
        {
            if (string.IsNullOrWhiteSpace(parameter.Description) ||
                parameter.Description.Trim().Length > 100)
            {
                return "参数字段说明不能为空且不能超过 100 个字符";
            }
            if (string.IsNullOrWhiteSpace(parameter.Field) ||
                parameter.Field.Trim().Length > 100)
            {
                return "参数字段不能为空且不能超过 100 个字符";
            }
            if (string.IsNullOrWhiteSpace(parameter.MappingField) ||
                parameter.MappingField.Trim().Length > 100)
            {
                return "参数映射库字段不能为空且不能超过 100 个字符";
            }
        }
        return null;
    }

    private static string NormalizeSubscriptionTemplateType(string? type)
    {
        var normalized = type?.Trim();
        return normalized?.ToLowerInvariant() switch
        {
            null or "" or "miniprogram" or "miniprogramsubscriptionmessage" or "subscribe" or "subscription" or "订阅消息"
                => "MiniProgramSubscriptionMessage",
            _ => normalized
        };
    }

    private static string NormalizeMessageSendStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "success" or "ok" or "sent" or "发送成功" => "Success",
            "failed" or "failure" or "error" or "发送失败" => "Failed",
            "pending" or "queued" or "待发送" => "Pending",
            "insufficientquota" or "quota" or "次数不足" => "InsufficientQuota",
            _ => "Unknown"
        };
    }

    private static string NormalizePermissionMenuStatus(string? status) =>
        string.Equals(status?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase)
            ? "hidden"
            : "normal";

    private static string NormalizeStaffStatus(string? status) =>
        string.Equals(status?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase)
            ? "hidden"
            : "normal";

    private static string? NormalizeReviewStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }
        return status.Trim().ToLowerInvariant() switch
        {
            "pending" or "pendingreview" => ReviewStatuses.Pending,
            "published" or "approved" => ReviewStatuses.Published,
            "hidden" => ReviewStatuses.Hidden,
            _ => null
        };
    }

    private static string? NormalizeWithdrawalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }
        return status.Trim().ToLowerInvariant() switch
        {
            "pending" or "pendingreview" => WithdrawalStatuses.PendingReview,
            "approved" => WithdrawalStatuses.Approved,
            "rejected" => WithdrawalStatuses.Rejected,
            "completed" or "settled" => WithdrawalStatuses.Completed,
            _ => null
        };
    }

    private static bool IsValidResourceType(string resourceType) =>
        !string.IsNullOrWhiteSpace(resourceType) &&
        resourceType.Length <= 80 &&
        resourceType.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');

    private static string? ValidateRoomType(RoomTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 50)
        {
            return "房型名称不能为空且不能超过 50 个字符";
        }
        if (request.Kind is not RoomTypeKinds.PrivateRoom and not RoomTypeKinds.Bed)
        {
            return "房型类型必须是 PrivateRoom 或 Bed";
        }
        if (request.BedCount <= 0 || request.MaxGuests <= 0 || request.BasePriceCents <= 0)
        {
            return "床位数、可住人数和基础价格必须大于 0";
        }
        return null;
    }

    private static (DateTime From, DateTime To, string? Error) NormalizeRange(
        DateTime? from,
        DateTime? to,
        int maxDays)
    {
        var start = (from ?? DateTime.Today).Date;
        var end = (to ?? start.AddDays(maxDays)).Date;
        if (end <= start)
        {
            return (start, end, "结束日期必须晚于开始日期");
        }
        if ((end - start).TotalDays > maxDays)
        {
            return (start, end, $"日期范围不能超过 {maxDays} 天");
        }
        return (start, end, null);
    }

    private static async Task<AdminOrderDetail> ToAdminOrderDetailAsync(
        AppDbContext db,
        BookingOrder order,
        CancellationToken cancellationToken)
    {
        var payments = await db.PaymentRecords.AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PaymentItem(
                x.Id,
                x.TransactionNumber,
                x.Provider,
                x.AmountCents,
                x.Status,
                x.CreatedAt,
                x.PaidAt))
            .ToListAsync(cancellationToken);
        var refunds = await db.RefundRecords.AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RefundItem(
                x.Id,
                x.RefundNumber,
                x.AmountCents,
                x.Reason,
                x.Status,
                x.ReviewComment,
                x.ProviderRefundNumber,
                x.CreatedAt,
                x.ReviewedAt,
                x.CompletedAt))
            .ToListAsync(cancellationToken);
        return new AdminOrderDetail(
            order.Id,
            order.OrderNumber,
            order.WxUserId,
            order.StoreId,
            order.Store?.Name ?? string.Empty,
            order.RoomTypeId,
            order.RoomType?.Name ?? string.Empty,
            order.CheckIn,
            order.CheckOut,
            order.Quantity,
            order.GuestCount,
            order.TotalAmountCents,
            order.Currency,
            order.Status,
            order.PaymentStatus,
            order.GuestSnapshotJson,
            order.AssignedResourcesJson,
            order.CheckedInAt,
            order.CheckedOutAt,
            order.CreatedAt,
            order.PaymentExpiresAt,
            payments,
            refunds);
    }

    private static async Task ReleaseInventoryAsync(
        AppDbContext db,
        BookingOrder order,
        bool sold,
        CancellationToken cancellationToken)
    {
        var inventories = await db.InventoryDailies
            .Where(x => x.RoomTypeId == order.RoomTypeId &&
                        x.Date >= order.CheckIn &&
                        x.Date < order.CheckOut)
            .ToListAsync(cancellationToken);
        foreach (var inventory in inventories)
        {
            if (sold)
            {
                inventory.SoldQuantity = Math.Max(0, inventory.SoldQuantity - order.Quantity);
            }
            else
            {
                inventory.LockedQuantity = Math.Max(0, inventory.LockedQuantity - order.Quantity);
            }
            inventory.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static AdminRoomTypeItem ToAdminRoomTypeItem(RoomType roomType, int resourceCount = 0, int totalBookings = 0)
    {
        return new AdminRoomTypeItem(
            roomType.Id,
            roomType.StoreId,
            roomType.Store?.Name ?? string.Empty,
            roomType.Name,
            roomType.Kind,
            roomType.BedCount,
            roomType.Gender,
            roomType.MaxGuests,
            roomType.BasePriceCents,
            roomType.Description,
            roomType.FacilitiesJson,
            roomType.ImageUrlsJson,
            roomType.IsPublished,
            roomType.ApprovalStatus,
            resourceCount,
            roomType.LongName,
            roomType.RoomCategory,
            roomType.TagsJson,
            roomType.ServicesJson,
            roomType.BathroomFacilitiesJson,
            roomType.NearbyFacilitiesJson,
            roomType.CheckInTime,
            roomType.CheckOutTime,
            roomType.CancellationRule,
            roomType.JoinRule,
            roomType.Area,
            roomType.Orientation,
            roomType.RoomCount,
            roomType.Layout,
            roomType.DefaultInventory,
            roomType.SortOrder,
            roomType.IsDraft,
            roomType.RejectionReason,
            roomType.ReviewedAt,
            roomType.CreatedAt,
            roomType.UpdatedAt,
            totalBookings,
            roomType.ChildCapacity);
    }

    private static RoomUnitItem ToRoomUnitItem(RoomUnit unit)
    {
        return new RoomUnitItem(
            unit.Id,
            unit.StoreId,
            unit.RoomTypeId,
            unit.Code,
            unit.Kind,
            unit.Gender,
            unit.Status,
            unit.Note);
    }

    private static InventorySummaryItem ToInventorySummary(InventoryDaily item)
    {
        return new InventorySummaryItem(
            item.Id,
            item.StoreId,
            item.RoomTypeId,
            item.Date,
            item.TotalQuantity,
            item.LockedQuantity,
            item.SoldQuantity,
            item.AvailableQuantity,
            item.UpdatedAt);
    }

    private static AdminRefundItem ToAdminRefundItem(RefundRecord refund)
    {
        return new AdminRefundItem(
            refund.Id,
            refund.OrderId,
            refund.Order?.OrderNumber ?? string.Empty,
            refund.Order?.Store?.Name ?? string.Empty,
            refund.Order?.RoomType?.Name ?? string.Empty,
            refund.AmountCents,
            refund.Reason,
            refund.Status,
            refund.ReviewComment,
            refund.CreatedAt,
            refund.ReviewedAt,
            refund.ProviderRefundNumber);
    }

    private static AdminUserItem ToAdminUserItem(AdminUser user)
    {
        return new AdminUserItem(
            user.Id,
            user.Username,
            user.DisplayName,
            user.PhoneNumber,
            user.StoreId,
            user.Store?.Name,
            user.IsEnabled,
            user.MustChangePassword,
            user.LastLoginAt,
            user.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => x.Role!.Name)
                .ToArray());
    }

    private static AdminRegionItem ToAdminRegionItem(Region region)
    {
        return new AdminRegionItem(
            region.Code,
            region.ParentCode,
            region.Name,
            region.ShortName,
            region.FullName,
            region.Level,
            region.Initial,
            region.IsHot,
            region.IsEnabled,
            region.SortOrder);
    }

    private static async Task<string?> ValidateRegionRequestAsync(
        AppDbContext db,
        RegionRequest request,
        string? currentCode,
        CancellationToken cancellationToken)
    {
        var code = NormalizeRegionCode(request.Code);
        if (code is null)
        {
            return "地区代码必须为 6 位数字";
        }

        var level = NormalizeRegionLevel(request.Level);
        if (level is null)
        {
            return "地区层级只能是省级、市级或区县级";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "地区名称不能为空";
        }

        if (!string.IsNullOrWhiteSpace(request.ParentCode) &&
            NormalizeRegionParentCode(request.ParentCode) is null)
        {
            return "上级地区代码必须为 6 位数字";
        }

        if (currentCode is null &&
            await db.Regions.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return "地区代码已存在";
        }

        var parentCode = NormalizeRegionParentCode(request.ParentCode);
        if (level == RegionLevels.Province)
        {
            if (parentCode is not null)
            {
                return "省级地区不能设置上级地区";
            }
        }
        else
        {
            if (parentCode is null)
            {
                return "市级或区县级地区必须设置上级地区";
            }

            if (string.Equals(parentCode, code, StringComparison.Ordinal))
            {
                return "上级地区不能是自身";
            }

            var parent = await db.Regions
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Code == parentCode, cancellationToken);
            if (parent is null)
            {
                return "上级地区不存在";
            }

            var expectedParentLevel = level == RegionLevels.City
                ? RegionLevels.Province
                : RegionLevels.City;
            if (parent.Level != expectedParentLevel)
            {
                return level == RegionLevels.City
                    ? "市级地区的上级必须是省级地区"
                    : "区县级地区的上级必须是市级地区";
            }

            var expectedPrefixLength = level == RegionLevels.City ? 2 : 4;
            if (!code.StartsWith(parent.Code[..expectedPrefixLength], StringComparison.Ordinal))
            {
                return "地区代码与上级地区不匹配";
            }

        }

        return null;
    }

    private static string? NormalizeRegionCode(string? value)
    {
        var code = value?.Trim();
        return code is { Length: 6 } && code.All(char.IsDigit)
            ? code
            : null;
    }

    private static string? NormalizeRegionParentCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeRegionCode(value);
    }

    private static string? NormalizeRegionLevel(string? value)
    {
        var level = value?.Trim();
        return level switch
        {
            RegionLevels.Province or "省" or "省/直辖市" => RegionLevels.Province,
            RegionLevels.City or "市" or "城市" => RegionLevels.City,
            RegionLevels.District or "区" or "县" or "区县" => RegionLevels.District,
            _ => null
        };
    }

    private static string? NormalizeRegionInitial(string? value)
    {
        var initial = value?.Trim();
        return string.IsNullOrWhiteSpace(initial)
            ? null
            : initial[..1].ToUpperInvariant();
    }

    private static string ToRegionShortName(string name)
    {
        var suffixes = new[]
        {
            "特别行政区",
            "维吾尔自治区",
            "壮族自治区",
            "回族自治区",
            "自治区",
            "自治州",
            "自治县",
            "省",
            "市辖区",
            "市",
            "区",
            "县",
            "旗"
        };

        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal) &&
                name.Length > suffix.Length)
            {
                return name[..^suffix.Length];
            }
        }

        return name;
    }

    private static IReadOnlyList<AdminRegionTreeItem> BuildRegionTree(
        IReadOnlyList<Region> regions,
        string? parentCode)
    {
        return regions
            .Where(x => x.ParentCode == parentCode)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new AdminRegionTreeItem(
                x.Code,
                x.ParentCode,
                x.Name,
                x.ShortName,
                x.FullName,
                x.Level,
                x.Initial,
                x.IsHot,
                x.IsEnabled,
                x.SortOrder,
                BuildRegionTree(regions, x.Code)))
            .ToList();
    }

    private static AdminMemberItem ToAdminMemberItem(
        WxUser user,
        int directUserCount,
        int indirectUserCount)
    {
        return new AdminMemberItem(
            user.Id,
            user.OpenId,
            user.Nickname,
            user.PhoneNumber,
            user.AvatarUrl,
            user.Gender,
            user.BalanceCents,
            user.CommissionCents,
            directUserCount,
            indirectUserCount,
            user.LastLoginAt,
            user.CreatedAt,
            user.IsEnabled);
    }

    private static AuditLog CreateAudit(
        ClaimsPrincipal principal,
        string action,
        string resourceType,
        string resourceId,
        string? summary)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = GetUserId(principal),
            ActorType = principal.FindFirstValue("user_type") ?? "Admin",
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Summary = summary
        };
    }

    private static async Task<string?> DetectImageExtensionAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }
        if (read >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ".png";
        }
        if (read >= 12 &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
        {
            return ".webp";
        }

        return null;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static Guid? GetStoreScope(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("store_id");
        return Guid.TryParse(raw, out var storeId) ? storeId : null;
    }

    private static bool IsSystemAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin);

    public static bool IsHeadquarters(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.Finance);

    public static bool CanAccessStore(ClaimsPrincipal principal, Guid storeId)
    {
        return IsHeadquarters(principal) ||
               (GetStoreScope(principal) is Guid scope && scope == storeId);
    }

    public static bool CanCreateStore(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations);

    public static bool CanManageStoreProfile(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManageRegions(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations);

    private static bool CanManageConfiguration(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManageReviews(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManageStoreStaff(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManageWithdrawals(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.Finance) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManageCatalog(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanManagePrices(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin);

    private static bool CanAdjustInventory(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin) ||
        principal.IsInRole(RoleCodes.StoreStaff);

    public static bool CanManageOrders(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.StoreAdmin) ||
        principal.IsInRole(RoleCodes.StoreStaff);

    private static bool CanManageRefunds(ClaimsPrincipal principal) =>
        principal.IsInRole(RoleCodes.SystemAdmin) ||
        principal.IsInRole(RoleCodes.Operations) ||
        principal.IsInRole(RoleCodes.Finance) ||
        principal.IsInRole(RoleCodes.StoreAdmin);
}
