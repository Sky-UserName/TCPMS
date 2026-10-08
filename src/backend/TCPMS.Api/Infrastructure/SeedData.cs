using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = new PasswordHasher<AdminUser>();

        await db.Database.EnsureCreatedAsync();

        var roleDefinitions = new[]
        {
            (Id: "00000000-0000-0000-0000-000000000101", Name: SystemRoles.Admin, Code: RoleCodes.SystemAdmin),
            (Id: "00000000-0000-0000-0000-000000000102", Name: SystemRoles.Operations, Code: RoleCodes.Operations),
            (Id: "00000000-0000-0000-0000-000000000103", Name: SystemRoles.Finance, Code: RoleCodes.Finance),
            (Id: "00000000-0000-0000-0000-000000000104", Name: SystemRoles.StoreAdmin, Code: RoleCodes.StoreAdmin),
            (Id: "00000000-0000-0000-0000-000000000105", Name: SystemRoles.StoreStaff, Code: RoleCodes.StoreStaff)
        };
        var rolesByCode = new Dictionary<string, Role>();
        foreach (var definition in roleDefinitions)
        {
            var role = await db.Roles.SingleOrDefaultAsync(x => x.Code == definition.Code);
            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.Parse(definition.Id),
                    Name = definition.Name,
                    Code = definition.Code
                };
                db.Roles.Add(role);
            }
            rolesByCode[definition.Code] = role;
        }
        var adminRole = rolesByCode[RoleCodes.SystemAdmin];

        var admin = await db.AdminUsers.SingleOrDefaultAsync(x => x.Username == "admin");
        if (admin is null)
        {
            admin = new AdminUser
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
                Username = "admin",
                DisplayName = "系统管理员",
                IsEnabled = true
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123456");
            db.AdminUsers.Add(admin);
            db.AdminUserRoles.Add(new AdminUserRole
            {
                AdminUserId = admin.Id,
                RoleId = adminRole.Id
            });
        }
        else if (!await db.AdminUserRoles.AnyAsync(x => x.AdminUserId == admin.Id && x.RoleId == adminRole.Id))
        {
            db.AdminUserRoles.Add(new AdminUserRole
            {
                AdminUserId = admin.Id,
                RoleId = adminRole.Id
            });
        }

        if (!await db.Stores.AnyAsync())
        {
            var shanghai = new Store
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Code = "SH-PPL",
                Name = "上海人民广场店",
                Status = StoreStatuses.Active,
                Description = "演示门店，靠近人民广场和地铁站。",
                Province = "上海市",
                City = "上海市",
                District = "黄浦区",
                Address = "人民广场演示路 1 号",
                StandardAddress = "上海市黄浦区人民广场演示路1号",
                Phone = "021-60000001",
                Longitude = 121.473701m,
                Latitude = 31.230416m,
                CoordinateSource = "Seed",
                CoordinatesUpdatedAt = DateTime.UtcNow,
                SortOrder = 1
            };
            var hangzhou = new Store
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Code = "HZ-WEST",
                Name = "杭州西湖店",
                Status = StoreStatuses.Active,
                Description = "演示门店，步行可到西湖景区。",
                Province = "浙江省",
                City = "杭州市",
                District = "西湖区",
                Address = "西湖演示路 8 号",
                StandardAddress = "浙江省杭州市西湖区西湖演示路8号",
                Phone = "0571-60000002",
                Longitude = 120.149620m,
                Latitude = 30.242480m,
                CoordinateSource = "Seed",
                CoordinatesUpdatedAt = DateTime.UtcNow,
                SortOrder = 2
            };
            db.Stores.AddRange(shanghai, hangzhou);

            var roomTypes = new[]
            {
                new RoomType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    StoreId = shanghai.Id,
                    Name = "标准大床房",
                    Kind = RoomTypeKinds.PrivateRoom,
                    BedCount = 1,
                    MaxGuests = 2,
                    BasePriceCents = 26800,
                    Description = "独立房间，适合 1-2 位入住。",
                    FacilitiesJson = "[\"独立卫浴\",\"空调\",\"无线网络\"]"
                },
                new RoomType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
                    StoreId = shanghai.Id,
                    Name = "女生四人间床位",
                    Kind = RoomTypeKinds.Bed,
                    BedCount = 4,
                    MaxGuests = 1,
                    BasePriceCents = 8800,
                    Gender = "Female",
                    Description = "女生多人间，按床位预订。",
                    FacilitiesJson = "[\"公共卫浴\",\"储物柜\",\"无线网络\"]"
                },
                new RoomType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000021"),
                    StoreId = hangzhou.Id,
                    Name = "西湖景观双床房",
                    Kind = RoomTypeKinds.PrivateRoom,
                    BedCount = 2,
                    MaxGuests = 2,
                    BasePriceCents = 39800,
                    Description = "双床独立房间，含西湖方向景观。",
                    FacilitiesJson = "[\"独立卫浴\",\"空调\",\"无线网络\",\"景观\"]"
                },
                new RoomType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000022"),
                    StoreId = hangzhou.Id,
                    Name = "待审核庭院房",
                    LongName = "西湖旁待审核庭院大床房",
                    Kind = RoomTypeKinds.PrivateRoom,
                    BedCount = 1,
                    MaxGuests = 2,
                    BasePriceCents = 32800,
                    Description = "开发环境中的待审核房间示例。",
                    FacilitiesJson = "[\"独立卫浴\",\"无线网络\"]",
                    CheckInTime = "当天14:00后",
                    CheckOutTime = "次日12:00前",
                    CancellationRule = "提前24小时可免费取消",
                    JoinRule = "入住须知",
                    DefaultInventory = 2,
                    IsPublished = false,
                    IsDraft = true,
                    ApprovalStatus = "PendingReview",
                    SortOrder = 3
                }
            };
            db.RoomTypes.AddRange(roomTypes);

            foreach (var roomType in roomTypes)
            {
                var resourceCount = roomType.Kind == RoomTypeKinds.Bed ? 12 : 8;
                for (var index = 1; index <= resourceCount; index++)
                {
                    db.RoomUnits.Add(new RoomUnit
                    {
                        Id = Guid.NewGuid(),
                        StoreId = roomType.StoreId,
                        RoomTypeId = roomType.Id,
                        Code = roomType.Kind == RoomTypeKinds.Bed
                            ? $"床位-{index:00}"
                            : $"房间-{index:000}",
                        Kind = roomType.Kind == RoomTypeKinds.Bed ? "Bed" : "Room",
                        Gender = roomType.Gender,
                        Status = "Available"
                    });
                }
            }

            var start = DateTime.Today;
            foreach (var roomType in roomTypes)
            {
                var store = roomType.StoreId == shanghai.Id ? shanghai : hangzhou;
                for (var offset = 0; offset < 120; offset++)
                {
                    var date = start.AddDays(offset);
                    var weekendAdjustment = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday ? 1.2m : 1m;
                    var price = (int)Math.Round(roomType.BasePriceCents * weekendAdjustment);
                    db.PriceCalendars.Add(new PriceCalendar
                    {
                        Id = Guid.NewGuid(),
                        StoreId = store.Id,
                        RoomTypeId = roomType.Id,
                        Date = date,
                        PriceCents = price,
                        Source = weekendAdjustment > 1 ? "Weekend" : "Seed"
                    });
                    db.InventoryDailies.Add(new InventoryDaily
                    {
                        Id = Guid.NewGuid(),
                        StoreId = store.Id,
                        RoomTypeId = roomType.Id,
                        Date = date,
                        TotalQuantity = roomType.DefaultInventory > 0
                            ? roomType.DefaultInventory
                            : roomType.Kind == RoomTypeKinds.Bed ? 12 : 8,
                        LockedQuantity = 0,
                        SoldQuantity = 0
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        if (!await db.RoomUnits.AnyAsync())
        {
            var roomTypes = await db.RoomTypes.AsNoTracking().ToListAsync();
            foreach (var roomType in roomTypes)
            {
                var resourceCount = roomType.Kind == RoomTypeKinds.Bed ? 12 : 8;
                for (var index = 1; index <= resourceCount; index++)
                {
                    db.RoomUnits.Add(new RoomUnit
                    {
                        Id = Guid.NewGuid(),
                        StoreId = roomType.StoreId,
                        RoomTypeId = roomType.Id,
                        Code = roomType.Kind == RoomTypeKinds.Bed
                            ? $"床位-{index:00}"
                            : $"房间-{index:000}",
                        Kind = roomType.Kind == RoomTypeKinds.Bed ? "Bed" : "Room",
                        Gender = roomType.Gender,
                        Status = "Available"
                    });
                }
            }
        }

        var shanghaiStoreId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var hangzhouStoreId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var demoAdminDefinitions = new[]
        {
            (Username: "ops_zhang", DisplayName: "张运营", StoreId: (Guid?)null, RoleCode: RoleCodes.Operations),
            (Username: "finance_li", DisplayName: "李财务", StoreId: (Guid?)null, RoleCode: RoleCodes.Finance),
            (Username: "shanghai_admin", DisplayName: "上海店管理员", StoreId: shanghaiStoreId, RoleCode: RoleCodes.StoreAdmin),
            (Username: "hangzhou_staff", DisplayName: "杭州店员工", StoreId: hangzhouStoreId, RoleCode: RoleCodes.StoreStaff)
        };
        foreach (var definition in demoAdminDefinitions)
        {
            var demoAdmin = await db.AdminUsers.SingleOrDefaultAsync(x => x.Username == definition.Username);
            if (demoAdmin is null)
            {
                demoAdmin = new AdminUser
                {
                    Id = Guid.NewGuid(),
                    Username = definition.Username,
                    DisplayName = definition.DisplayName,
                    StoreId = definition.StoreId,
                    IsEnabled = true
                };
                demoAdmin.PasswordHash = passwordHasher.HashPassword(demoAdmin, "Admin@123456");
                db.AdminUsers.Add(demoAdmin);
            }

            var role = rolesByCode[definition.RoleCode];
            if (!await db.AdminUserRoles.AnyAsync(x => x.AdminUserId == demoAdmin.Id && x.RoleId == role.Id))
            {
                db.AdminUserRoles.Add(new AdminUserRole
                {
                    AdminUserId = demoAdmin.Id,
                    RoleId = role.Id
                });
            }
        }

        await db.SaveChangesAsync();

        if (!await db.BookingOrders.AnyAsync())
        {
            var demoUser = await db.WxUsers.SingleOrDefaultAsync(x => x.OpenId == "dev-user-001");
            if (demoUser is null)
            {
                demoUser = new WxUser
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000301"),
                    OpenId = "dev-user-001",
                    Nickname = "演示用户",
                    PhoneNumber = "13800000000"
                };
                db.WxUsers.Add(demoUser);
            }

            var demoMembers = new[]
            {
                ("user_a2cb01e05252e7dcf9824424", "星星", "15900003334", 0, 0),
                ("user_06cd4af92194276af9678528", "用户69eadf96783be", "18355330761", 0, 0),
                ("user_d2ac82afe2cc926e4ed093b06", "刘", "18652949949", 100, 0),
                ("user_48e0d02f37a34a8e", "墨", "18862515515", 0, 0),
                ("user_2b762c3b7f8b4a80", "月亮", "19952195103", 0, 0),
                ("user_0b7f2a7807d848e0", "Carl", "18952505817", 10, 10),
                ("user_aa7b0c5f8d7a4210", "Jane", "18921915967", 20, 120),
                ("user_12adcae2f3ee49da", "星星", "15929039049", 0, 0),
                ("user_0d8f3ac8ce1a41c7", "健康最重要", "15716770770", 0, 0),
                ("user_817a0b9cc4bf4c6f", "133****0825", "1333709825", 0, 0),
                ("user_3d7fd5f52a1a46a1", "小满", "13600001234", 0, 32),
                ("user_2f6ca7c1f6ea40c7", "阿南", "13700005678", 0, 0),
                ("user_71d8dd8e6b754c83", "漫游者", "13800007890", 0, 80)
            };
            foreach (var member in demoMembers)
            {
                if (!await db.WxUsers.AnyAsync(x => x.OpenId == member.Item1))
                {
                    db.WxUsers.Add(new WxUser
                    {
                        Id = Guid.NewGuid(),
                        OpenId = member.Item1,
                        Nickname = member.Item2,
                        PhoneNumber = member.Item3,
                        BalanceCents = member.Item4,
                        CommissionCents = member.Item5,
                        ParentUserId = member.Item1.Contains("aa7b") ? demoUser.Id : null,
                        CreatedAt = DateTime.UtcNow.AddDays(-member.Item4 - 3),
                        LastLoginAt = DateTime.UtcNow.AddHours(-(member.Item4 + 1))
                    });
                }
            }

            var roomTypes = await db.RoomTypes
                .Where(x => x.Id == Guid.Parse("00000000-0000-0000-0000-000000000011") ||
                            x.Id == Guid.Parse("00000000-0000-0000-0000-000000000012") ||
                            x.Id == Guid.Parse("00000000-0000-0000-0000-000000000021"))
                .ToDictionaryAsync(x => x.Id);
            var today = DateTime.Today;
            await AddDemoOrderAsync(
                db,
                demoUser,
                roomTypes[Guid.Parse("00000000-0000-0000-0000-000000000011")],
                "TCP202610010001",
                today.AddDays(1),
                today.AddDays(3),
                1,
                2,
                53600,
                OrderStatuses.ConfirmedPendingCheckIn,
                "Paid",
                sold: true);
            await AddDemoOrderAsync(
                db,
                demoUser,
                roomTypes[Guid.Parse("00000000-0000-0000-0000-000000000012")],
                "TCP202610010002",
                today.AddDays(4),
                today.AddDays(6),
                1,
                1,
                17600,
                OrderStatuses.PendingPayment,
                "Unpaid",
                sold: false);
            await AddDemoOrderAsync(
                db,
                demoUser,
                roomTypes[Guid.Parse("00000000-0000-0000-0000-000000000021")],
                "TCP202609300003",
                today.AddDays(7),
                today.AddDays(8),
                1,
                2,
                39800,
                OrderStatuses.CheckedIn,
                "Paid",
                sold: true,
                assignedResourceCodes: new[] { "房间-001" });
            await AddDemoOrderAsync(
                db,
                demoUser,
                roomTypes[Guid.Parse("00000000-0000-0000-0000-000000000011")],
                "TCP202609150003",
                today.AddDays(-23),
                today.AddDays(-22),
                1,
                2,
                26800,
                OrderStatuses.Completed,
                "Paid",
                sold: true);
            await AddDemoOrderAsync(
                db,
                demoUser,
                roomTypes[Guid.Parse("00000000-0000-0000-0000-000000000011")],
                "TCP202609280004",
                today.AddDays(9),
                today.AddDays(10),
                1,
                2,
                26800,
                OrderStatuses.Refunding,
                "Paid",
                sold: true,
                refund: true);
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddDemoOrderAsync(
        AppDbContext db,
        WxUser user,
        RoomType roomType,
        string orderNumber,
        DateTime checkIn,
        DateTime checkOut,
        int quantity,
        int guestCount,
        int totalAmountCents,
        string status,
        string paymentStatus,
        bool sold,
        string[]? assignedResourceCodes = null,
        bool refund = false)
    {
        var order = new BookingOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            WxUserId = user.Id,
            StoreId = roomType.StoreId,
            RoomTypeId = roomType.Id,
            CheckIn = checkIn.Date,
            CheckOut = checkOut.Date,
            Quantity = quantity,
            GuestCount = guestCount,
            TotalAmountCents = totalAmountCents,
            Status = status,
            PaymentStatus = paymentStatus,
            GuestSnapshotJson = "[{\"name\":\"张三\",\"identity\":\"410***********1234\",\"phone\":\"138****8888\"}]",
            AssignedResourcesJson = assignedResourceCodes is null ? null : System.Text.Json.JsonSerializer.Serialize(assignedResourceCodes),
            PaymentExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };
        db.BookingOrders.Add(order);

        var inventories = await db.InventoryDailies
            .Where(x => x.RoomTypeId == roomType.Id &&
                        x.Date >= checkIn.Date &&
                        x.Date < checkOut.Date)
            .ToListAsync();
        foreach (var inventory in inventories)
        {
            if (sold)
            {
                inventory.SoldQuantity += quantity;
            }
            else
            {
                inventory.LockedQuantity += quantity;
            }
        }

        if (paymentStatus == "Paid")
        {
            db.PaymentRecords.Add(new PaymentRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TransactionNumber = $"SEED_{orderNumber}",
                AmountCents = totalAmountCents,
                Status = "Paid",
                PaidAt = DateTime.UtcNow.AddHours(-1)
            });
        }

        if (assignedResourceCodes is not null)
        {
            var resourceCodeSet = new HashSet<string>(assignedResourceCodes, StringComparer.OrdinalIgnoreCase);
            var allUnits = await db.RoomUnits
                .Where(x => x.RoomTypeId == roomType.Id)
                .ToListAsync();
            var units = allUnits.Where(x => resourceCodeSet.Contains(x.Code)).ToList();
            foreach (var unit in units)
            {
                unit.Status = "Occupied";
            }
            order.CheckedInAt = DateTime.UtcNow.AddHours(-1);
        }

        if (refund)
        {
            db.RefundRecords.Add(new RefundRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                RefundNumber = $"RF{orderNumber[3..]}",
                AmountCents = totalAmountCents,
                Reason = "行程调整",
                Status = "PendingReview",
                OriginalOrderStatus = OrderStatuses.ConfirmedPendingCheckIn,
                RequestedByUserId = user.Id
            });
        }
    }
}
