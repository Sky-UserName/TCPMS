using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;

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
        MapStatistics(admin);
        MapAuditLogs(admin);
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

            db.AuditLogs.Add(CreateAudit(
                principal,
                "price.bulk_update",
                "RoomType",
                roomType.Id.ToString(),
                $"{roomType.Name}: {range.From:yyyy-MM-dd} - {range.To.AddDays(-1):yyyy-MM-dd}"));
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
                refund.Status = "Refunded";
                refund.ProviderRefundNumber = $"MOCK_RF_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
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

    private static void MapStatistics(RouteGroupBuilder admin)
    {
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
            user.LastLoginAt,
            user.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => x.Role!.Name)
                .ToArray());
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
