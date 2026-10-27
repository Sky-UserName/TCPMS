using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

/// <summary>
/// 初始化基础数据。
/// 原则：地区、角色、菜单、字典、订阅消息模板属于配置，必须存在；
/// 门店 / 房型 / 订单 / 用户等业务数据只在空库且 Seed:DemoData 为 true 时写入少量样例，
/// 方便本地把流程走通，生产环境可以用 Seed:DemoData=false 关掉。
/// </summary>
public static class SeedData
{
    // 初始管理员口令优先从配置读取，避免把固定口令写死在代码里。
    private const string DefaultAdminPassword = "Admin@123456";

    private static readonly Guid ShanghaiStoreId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid HangzhouStoreId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static readonly Guid StandardRoomId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid BedRoomId = Guid.Parse("00000000-0000-0000-0000-000000000012");
    private static readonly Guid LakeViewRoomId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    private static readonly Guid PendingRoomId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    private static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-000000000301");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();
        var passwordHasher = new PasswordHasher<AdminUser>();

        await db.Database.EnsureCreatedAsync();
        await SeedRegionsAsync(db);

        var roles = await SeedRolesAsync(db);
        await SeedAdminAsync(db, configuration, passwordHasher, roles);
        await SeedConfigurationAsync(db);
        await db.SaveChangesAsync();

        var seedDemoData = configuration?.GetValue<bool?>("Seed:DemoData") ?? true;
        if (seedDemoData && !await db.Stores.AnyAsync())
        {
            await SeedDemoBusinessAsync(db, passwordHasher, roles);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(AppDbContext db)
    {
        var definitions = new[]
        {
            (Id: "00000000-0000-0000-0000-000000000101", Name: SystemRoles.Admin, Code: RoleCodes.SystemAdmin),
            (Id: "00000000-0000-0000-0000-000000000102", Name: SystemRoles.Operations, Code: RoleCodes.Operations),
            (Id: "00000000-0000-0000-0000-000000000103", Name: SystemRoles.Finance, Code: RoleCodes.Finance),
            (Id: "00000000-0000-0000-0000-000000000104", Name: SystemRoles.StoreAdmin, Code: RoleCodes.StoreAdmin),
            (Id: "00000000-0000-0000-0000-000000000105", Name: SystemRoles.StoreStaff, Code: RoleCodes.StoreStaff)
        };

        var result = new Dictionary<string, Role>();
        foreach (var definition in definitions)
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
            result[definition.Code] = role;
        }

        return result;
    }

    private static async Task SeedAdminAsync(
        AppDbContext db,
        IConfiguration? configuration,
        IPasswordHasher<AdminUser> passwordHasher,
        IReadOnlyDictionary<string, Role> roles)
    {
        var seedPassword = configuration?.GetValue<string>("Seed:AdminPassword");
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            seedPassword = DefaultAdminPassword;
        }

        var adminRole = roles[RoleCodes.SystemAdmin];
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
            admin.PasswordHash = passwordHasher.HashPassword(admin, seedPassword);
            // 初始口令一律标记为「必须修改」，登录后不完成改密无法使用管理接口。
            admin.MustChangePassword = true;
            db.AdminUsers.Add(admin);
        }

        if (!await db.AdminUserRoles.AnyAsync(x => x.AdminUserId == admin.Id && x.RoleId == adminRole.Id))
        {
            db.AdminUserRoles.Add(new AdminUserRole { AdminUserId = admin.Id, RoleId = adminRole.Id });
        }
    }

    /// <summary>
    /// 权限菜单、筛选字典、订阅消息模板：属于系统配置，不随业务数据清空。
    /// </summary>
    private static async Task SeedConfigurationAsync(AppDbContext db)
    {
        if (!await db.PermissionMenus.AnyAsync())
        {
            var menuDefinitions = new[]
            {
                ("finance", "财务统计", "wallet", "/pages/merchant/finance"),
                ("rooms", "房间管理", "house", "/pages/merchant/rooms"),
                ("orders", "订单管理", "receipt", "/pages/merchant/orders"),
                ("verify", "扫码核销", "qr", "/pages/merchant/verify"),
                ("records", "核销记录", "document", "/pages/merchant/records"),
                ("staff", "员工管理", "group", "/pages/merchant/staff"),
                ("property", "民宿管理", "store", "/pages/merchant/index"),
                ("publish", "发布房源", "plus", "/pages/merchant/publish"),
                ("reviews", "评价管理", "star", "/pages/merchant/reviews"),
                ("coupon", "优惠券管理", "document", "/pages/merchant/coupons"),
                ("settings", "民宿设置", "settings", "/pages/merchant/settings"),
                ("subscriptions", "订阅消息", "document", "/pages/merchant/messages")
            };
            for (var index = 0; index < menuDefinitions.Length; index++)
            {
                var definition = menuDefinitions[index];
                db.PermissionMenus.Add(new PermissionMenu
                {
                    Key = definition.Item1,
                    Title = definition.Item2,
                    IconKey = definition.Item3,
                    Route = definition.Item4,
                    Weight = index + 1,
                    Status = "normal"
                });
            }
        }

        await SeedManagedItemsAsync(db, "levels", new object[]
        {
            new { name = "无等级", description = "普通民宿", sort = 1, status = "active" },
            new { name = "优质民宿", description = "服务和房源质量稳定", sort = 2, status = "active" }
        });
        await SeedManagedItemsAsync(db, "facilities", new object[]
        {
            new { name = "独立卫浴", category = "卫浴设施", status = "active", sort = 1 },
            new { name = "无线网络", category = "基础设施", status = "active", sort = 2 },
            new { name = "空调", category = "基础设施", status = "active", sort = 3 }
        });
        await SeedManagedItemsAsync(db, "tags", new object[]
        {
            new { name = "亲子友好", color = "#16b79a", usage = 0, status = "active" },
            new { name = "近地铁", color = "#4387d6", usage = 0, status = "active" }
        });
        await SeedManagedItemsAsync(db, "roomTypes", new object[]
        {
            new { name = "大床房", sort = 1, status = "active" },
            new { name = "双床房", sort = 2, status = "active" },
            new { name = "多人间床位", sort = 3, status = "active" }
        });
        await SeedManagedItemsAsync(db, "common", new object[]
        {
            new { name = "默认入住时间", key = "checkin.time", value = "14:00", status = "enabled" },
            new { name = "默认退房时间", key = "checkout.time", value = "12:00", status = "enabled" }
        });
        await SeedManagedItemsAsync(db, "carousel", new object[]
        {
            new { name = "首页首屏", image = "/static/prototype/home-hero.jpg", badge = "官方直营 · 连住特惠", title = "寻一处静谧，归心旅途", subtitle = "阳光实木套房与极简美学空间现已开放", link = "", sort = 1, status = "active" },
            new { name = "城市精选", image = "/static/prototype/store-city.jpg", badge = "城市精选 · 即时房态", title = "住进城市，走近风景", subtitle = "通勤、旅行与周末短住，都有合适的落脚点", link = "", sort = 2, status = "active" },
            new { name = "青年友好", image = "/static/prototype/store-hostel.jpg", badge = "青年友好 · 轻装出发", title = "今晚就住得舒服", subtitle = "整洁床位与安心服务，轻松开启下一段行程", link = "", sort = 3, status = "active" }
        });

        await SeedSubscriptionTemplatesAsync(db);
        await SeedContentAsync(db);
    }

    /// <summary>
    /// 少量样例业务数据：2 家门店、4 个房型（含 1 个待审核）、2 个用户、4 张订单。
    /// </summary>
    private static async Task SeedDemoBusinessAsync(
        AppDbContext db,
        IPasswordHasher<AdminUser> passwordHasher,
        IReadOnlyDictionary<string, Role> roles)
    {
        var shanghai = new Store
        {
            Id = ShanghaiStoreId,
            Code = "SH-PPL",
            Name = "上海人民广场店",
            Status = StoreStatuses.Active,
            Description = "靠近人民广场地铁站，步行 5 分钟到南京路。",
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
            Rating = 4.8m,
            SortOrder = 1,
            // 开发演示账号（dev-user-001）作为该门店归属人，登录后可在小程序看到商家中心。
            OwnerUsername = "dev-user-001",
            OwnerName = "演示店主",
            OwnerPhone = "13800000001"
        };
        var hangzhou = new Store
        {
            Id = HangzhouStoreId,
            Code = "HZ-WEST",
            Name = "杭州西湖店",
            Status = StoreStatuses.Active,
            Description = "步行 10 分钟到西湖断桥，周边餐饮便利。",
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
            Rating = 4.6m,
            SortOrder = 2
        };
        db.Stores.AddRange(shanghai, hangzhou);
        db.AdminManagedItems.Add(new AdminManagedItem
        {
            Id = Guid.NewGuid(),
            ResourceType = $"merchant_coupons:{shanghai.Id}",
            DataJson = JsonSerializer.Serialize(new object[]
            {
                new { Id = "coupon-001", Name = "新客立减券", AmountCents = 2000, Threshold = "满200可用", Stock = 50, Used = 8, Status = "生效中", CreatedAt = DateTime.UtcNow },
                new { Id = "coupon-002", Name = "周末房费券", AmountCents = 5000, Threshold = "满400可用", Stock = 20, Used = 20, Status = "已结束", CreatedAt = DateTime.UtcNow.AddDays(-30) }
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var roomTypes = new[]
        {
            new RoomType
            {
                Id = StandardRoomId,
                StoreId = shanghai.Id,
                Name = "标准大床房",
                Kind = RoomTypeKinds.PrivateRoom,
                BedCount = 1,
                MaxGuests = 2,
                BasePriceCents = 26800,
                Description = "独立房间，1.8m 大床，适合 1-2 位入住。",
                FacilitiesJson = "[\"独立卫浴\",\"空调\",\"无线网络\"]",
                CheckInTime = "当天14:00后",
                CheckOutTime = "次日12:00前",
                CancellationRule = "入住前24小时可免费取消",
                DefaultInventory = 4,
                SortOrder = 1
            },
            new RoomType
            {
                Id = BedRoomId,
                StoreId = shanghai.Id,
                Name = "女生四人间床位",
                Kind = RoomTypeKinds.Bed,
                BedCount = 4,
                MaxGuests = 1,
                BasePriceCents = 8800,
                Gender = "Female",
                Description = "女生多人间，按床位预订。",
                FacilitiesJson = "[\"公共卫浴\",\"储物柜\",\"无线网络\"]",
                CheckInTime = "当天14:00后",
                CheckOutTime = "次日12:00前",
                CancellationRule = "入住前24小时可免费取消",
                DefaultInventory = 6,
                SortOrder = 2
            },
            new RoomType
            {
                Id = LakeViewRoomId,
                StoreId = hangzhou.Id,
                Name = "西湖景观双床房",
                Kind = RoomTypeKinds.PrivateRoom,
                BedCount = 2,
                MaxGuests = 2,
                BasePriceCents = 39800,
                Description = "双床独立房间，可看到西湖方向景观。",
                FacilitiesJson = "[\"独立卫浴\",\"空调\",\"无线网络\",\"景观\"]",
                CheckInTime = "当天14:00后",
                CheckOutTime = "次日12:00前",
                CancellationRule = "入住前48小时可免费取消",
                DefaultInventory = 3,
                SortOrder = 1
            },
            new RoomType
            {
                Id = PendingRoomId,
                StoreId = hangzhou.Id,
                Name = "庭院大床房（待审核）",
                LongName = "西湖旁待审核庭院大床房",
                Kind = RoomTypeKinds.PrivateRoom,
                BedCount = 1,
                MaxGuests = 2,
                BasePriceCents = 32800,
                Description = "用于验证房型审核流程的样例房间。",
                FacilitiesJson = "[\"独立卫浴\",\"无线网络\"]",
                CheckInTime = "当天14:00后",
                CheckOutTime = "次日12:00前",
                CancellationRule = "入住前24小时可免费取消",
                DefaultInventory = 2,
                IsPublished = false,
                IsDraft = true,
                ApprovalStatus = "PendingReview",
                SortOrder = 2
            }
        };
        db.RoomTypes.AddRange(roomTypes);

        foreach (var roomType in roomTypes)
        {
            var resourceCount = roomType.DefaultInventory;
            for (var index = 1; index <= resourceCount; index++)
            {
                db.RoomUnits.Add(new RoomUnit
                {
                    Id = Guid.NewGuid(),
                    StoreId = roomType.StoreId,
                    RoomTypeId = roomType.Id,
                    Code = roomType.Kind == RoomTypeKinds.Bed ? $"床位-{index:00}" : $"房间-{index:000}",
                    Kind = roomType.Kind == RoomTypeKinds.Bed ? "Bed" : "Room",
                    Gender = roomType.Gender,
                    Status = "Available"
                });
            }
        }

        // 未来 60 天的价格与库存：周末上浮 20%，方便验证报价和库存扣减。
        var start = DateTime.Today;
        foreach (var roomType in roomTypes)
        {
            // 与商家后台新建房型保持一致：预置 180 天库存与价格，日历可选范围才不会断层。
            for (var offset = 0; offset < 180; offset++)
            {
                var date = start.AddDays(offset);
                var isWeekend = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
                db.PriceCalendars.Add(new PriceCalendar
                {
                    Id = Guid.NewGuid(),
                    StoreId = roomType.StoreId,
                    RoomTypeId = roomType.Id,
                    Date = date,
                    PriceCents = (int)Math.Round(roomType.BasePriceCents * (isWeekend ? 1.2m : 1m)),
                    Source = isWeekend ? "Weekend" : "Seed"
                });
                db.InventoryDailies.Add(new InventoryDaily
                {
                    Id = Guid.NewGuid(),
                    StoreId = roomType.StoreId,
                    RoomTypeId = roomType.Id,
                    Date = date,
                    TotalQuantity = roomType.DefaultInventory,
                    LockedQuantity = 0,
                    SoldQuantity = 0
                });
            }
        }

        var demoUser = new WxUser
        {
            Id = DemoUserId,
            OpenId = "dev-user-001",
            Nickname = "测试用户",
            PhoneNumber = "13800000000",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-2)
        };
        var secondUser = new WxUser
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000302"),
            OpenId = "dev-user-002",
            Nickname = "体验用户",
            PhoneNumber = "13900000000",
            CreatedAt = DateTime.UtcNow.AddDays(-12),
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
        db.WxUsers.AddRange(demoUser, secondUser);

        // 门店管理员样例，用于验证门店维度的数据隔离。
        var storeAdmin = new AdminUser
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000202"),
            Username = "shanghai_admin",
            DisplayName = "上海店管理员",
            StoreId = shanghai.Id,
            IsEnabled = true
        };
        storeAdmin.PasswordHash = passwordHasher.HashPassword(storeAdmin, DefaultAdminPassword);
        storeAdmin.MustChangePassword = true;
        db.AdminUsers.Add(storeAdmin);
        db.AdminUserRoles.Add(new AdminUserRole { AdminUserId = storeAdmin.Id, RoleId = roles[RoleCodes.StoreAdmin].Id });
        db.StoreStaffProfiles.Add(new StoreStaffProfile
        {
            AdminUserId = storeAdmin.Id,
            Gender = "男",
            PermissionKeysJson = JsonSerializer.Serialize(new[] { "rooms", "orders", "verify", "records" })
        });

        var today = DateTime.Today;
        var orderedRoomTypes = roomTypes.ToDictionary(x => x.Id);

        // 1) 待支付：库存处于锁定状态，支付后转为已售。
        await AddDemoOrderAsync(
            db,
            demoUser,
            orderedRoomTypes[StandardRoomId],
            "TCP202610010001",
            today.AddDays(1),
            today.AddDays(2),
            quantity: 1,
            guestCount: 1,
            totalAmountCents: 26800,
            status: OrderStatuses.PendingPayment,
            paymentStatus: "Unpaid",
            sold: false,
            paymentExpiresAt: DateTime.UtcNow.AddHours(12));

        // 2) 已支付待入住：验证订单详情、取消与退款入口。
        await AddDemoOrderAsync(
            db,
            demoUser,
            orderedRoomTypes[BedRoomId],
            "TCP202610010002",
            today.AddDays(3),
            today.AddDays(5),
            quantity: 1,
            guestCount: 1,
            totalAmountCents: 17600,
            status: OrderStatuses.ConfirmedPendingCheckIn,
            paymentStatus: "Paid",
            sold: true);

        // 3) 已完成：可以写评价、看历史订单。
        await AddDemoOrderAsync(
            db,
            demoUser,
            orderedRoomTypes[StandardRoomId],
            "TCP202609200003",
            today.AddDays(-10),
            today.AddDays(-9),
            quantity: 1,
            guestCount: 1,
            totalAmountCents: 26800,
            status: OrderStatuses.Completed,
            paymentStatus: "Paid",
            sold: true,
            completed: true,
            review: true);

        // 4) 退款审核中：验证后台退款审核流程。
        await AddDemoOrderAsync(
            db,
            demoUser,
            orderedRoomTypes[LakeViewRoomId],
            "TCP202610010004",
            today.AddDays(6),
            today.AddDays(7),
            quantity: 1,
            guestCount: 2,
            totalAmountCents: 39800,
            status: OrderStatuses.Refunding,
            paymentStatus: "Paid",
            sold: true,
            refund: true);
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
        bool refund = false,
        bool review = false,
        bool completed = false,
        DateTime? paymentExpiresAt = null)
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
            PriceSnapshotJson = $"{{\"nightlyPriceCents\":{roomType.BasePriceCents},\"nights\":{Math.Max(1, (checkOut.Date - checkIn.Date).Days)}}}",
            PaymentExpiresAt = paymentExpiresAt ?? DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };
        db.BookingOrders.Add(order);

        var inventories = await db.InventoryDailies
            .Where(x => x.RoomTypeId == roomType.Id && x.Date >= checkIn.Date && x.Date < checkOut.Date)
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

        if (completed)
        {
            order.CheckedInAt = order.CheckIn.AddHours(14);
            order.CheckedOutAt = order.CheckOut.AddHours(-4);
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

        if (review)
        {
            var reviewEntity = new Review
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                WxUserId = user.Id,
                StoreId = roomType.StoreId,
                RoomTypeId = roomType.Id,
                RoomRating = 5,
                StoreRating = 5,
                Content = "位置很方便，房间干净，下次还会来。",
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };
            db.Reviews.Add(reviewEntity);
            db.ReviewModerations.Add(new ReviewModeration
            {
                ReviewId = reviewEntity.Id,
                Status = ReviewStatuses.Published,
                ReviewedAt = DateTime.UtcNow.AddDays(-8)
            });
        }
    }

    /// <summary>
    /// 攻略文章与平台公告属于内容配置，跟随配置种子写入；已存在时不覆盖运营人员后的修改。
    /// </summary>
    private static async Task SeedContentAsync(AppDbContext db)
    {
        if (!await db.AdminManagedItems.AnyAsync(x => x.ResourceType == "articles"))
        {
            var articles = new object[]
            {
                new
                {
                    Id = "autumn-nearby",
                    Title = "早上好！从附近开始一段清爽舒心的秋日漫居旅程",
                    Subtitle = "出行攻略 / 官方精选",
                    Author = "TCPMS 智选生活局",
                    PublishedAt = "2026-09-30",
                    ReadCount = 1420,
                    CoverImage = "/static/prototype/home-hero.jpg",
                    Caption = "上海人民广场店 · 晨光与原木茶室",
                    StoreId = ShanghaiStoreId.ToString(),
                    Sections = new object[]
                    {
                        new { Type = "quote", Text = "秋风渐起，逃离城市喧嚣并不需要太远跋涉。TCPMS 智选旅宿为您甄选城市中心偏中取静的质感住所，无论是独自放空、好友小聚还是青年创客同行，这里都有恰到好处的停留方式。" },
                        new { Type = "heading", Text = "01　从附近开始，给旅途减一点负担" },
                        new { Type = "paragraph", Text = "距离地铁、商圈和公园都不远的门店，适合临时起意的短途出行。抵达后可以把时间留给散步、阅读和一顿慢慢吃完的晚餐，而不是消耗在往返路上。" },
                        new { Type = "heading", Text = "02　选一间适合自己的房间" },
                        new { Type = "paragraph", Text = "喜欢安静，可以选择采光好的独立大床房；和朋友同行，可以看看双床房或多人间床位。先确定入住日期和人数，再根据设施、价格和评价做决定，会比只看一张照片更可靠。" },
                        new { Type = "heading", Text = "03　把入住变成旅途的一部分" },
                        new { Type = "paragraph", Text = "提前保存门店地址，打开导航入口，抵达时直接联系前台。需要发票、寄存行李或调整入住时间，也可以在订单详情里联系官方客服和门店管家。" }
                    },
                    Related = new object[]
                    {
                        new { Title = "一张地图，发现城市里的安静角落", Desc = "附近门店与周末散步路线", Image = "/static/prototype/store-city.jpg", ArticleId = "quiet-corner-map" },
                        new { Title = "入住前要准备的 5 件小事", Desc = "让抵达和离店都更从容", Image = "/static/prototype/store-hostel.jpg", ArticleId = "checkin-checklist" }
                    }
                },
                new
                {
                    Id = "quiet-corner-map",
                    Title = "一张地图，发现城市里的安静角落",
                    Subtitle = "城市漫游 / 附近灵感",
                    Author = "TCPMS 内容团队",
                    PublishedAt = "2026-09-18",
                    ReadCount = 860,
                    CoverImage = "/static/prototype/store-city.jpg",
                    Caption = "从门店出发，步行即可抵达的城市角落",
                    StoreId = HangzhouStoreId.ToString(),
                    Sections = new object[]
                    {
                        new { Type = "paragraph", Text = "把地图缩小一点，只保留门店、咖啡馆、书店和公园。你会发现，真正让一趟短途旅行舒服的，往往不是打卡了多少景点，而是留出了多少不赶时间的空白。" },
                        new { Type = "heading", Text = "01　以门店为圆心，画一个 2 公里生活圈" },
                        new { Type = "paragraph", Text = "步行 20 分钟能到达的地方，通常藏着最日常的城市表情。早餐铺、社区公园和独立书店，比热门商圈更适合慢慢逛。" },
                        new { Type = "heading", Text = "02　把雨天和夜晚也安排进去" },
                        new { Type = "paragraph", Text = "出发前看一眼天气，给室内展馆和夜间散步各留一个备选。这样即使计划被打乱，也不会把时间浪费在临时搜索上。" }
                    },
                    Related = new object[]
                    {
                        new { Title = "早上好！从附近开始一段清爽舒心的秋日漫居旅程", Desc = "出行攻略 / 官方精选", Image = "/static/prototype/home-hero.jpg", ArticleId = "autumn-nearby" },
                        new { Title = "入住前要准备的 5 件小事", Desc = "让抵达和离店都更从容", Image = "/static/prototype/store-hostel.jpg", ArticleId = "checkin-checklist" }
                    }
                },
                new
                {
                    Id = "checkin-checklist",
                    Title = "入住前要准备的 5 件小事",
                    Subtitle = "入住指南 / 新手必读",
                    Author = "TCPMS 客服团队",
                    PublishedAt = "2026-09-06",
                    ReadCount = 2310,
                    CoverImage = "/static/prototype/store-hostel.jpg",
                    Caption = "提前十分钟准备，入住更从容",
                    StoreId = ShanghaiStoreId.ToString(),
                    Sections = new object[]
                    {
                        new { Type = "paragraph", Text = "第一次预订民宿，最容易忽略的不是房间设施，而是入住流程。出发前花十分钟确认下面几件事，能省掉大部分现场沟通。" },
                        new { Type = "heading", Text = "01　确认入住时间与前台位置" },
                        new { Type = "paragraph", Text = "不同门店的入住时间可能不同。订单确认后，先在订单详情里核对入住时间、门店地址和联系电话，晚到要提前告知。" },
                        new { Type = "heading", Text = "02　带好证件，登记入住人信息" },
                        new { Type = "paragraph", Text = "入住人需要提供真实有效的身份证件。多人同行时，提前在订单里补全入住人信息，现场办理会更快。" },
                        new { Type = "heading", Text = "03　保存订单号和门店电话" },
                        new { Type = "paragraph", Text = "订单号是核验和售后最重要的凭证，门店电话则用于临时沟通。建议提前截图保存，避免网络不好时找不到信息。" }
                    },
                    Related = new object[]
                    {
                        new { Title = "早上好！从附近开始一段清爽舒心的秋日漫居旅程", Desc = "出行攻略 / 官方精选", Image = "/static/prototype/home-hero.jpg", ArticleId = "autumn-nearby" },
                        new { Title = "一张地图，发现城市里的安静角落", Desc = "附近门店与周末散步路线", Image = "/static/prototype/store-city.jpg", ArticleId = "quiet-corner-map" }
                    }
                }
            };
            var now = DateTime.UtcNow;
            foreach (var article in articles)
            {
                db.AdminManagedItems.Add(new AdminManagedItem
                {
                    Id = Guid.NewGuid(),
                    ResourceType = "articles",
                    DataJson = JsonSerializer.Serialize(article),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (!await db.AdminManagedItems.AnyAsync(x => x.ResourceType == "notices"))
        {
            db.AdminManagedItems.Add(new AdminManagedItem
            {
                Id = Guid.NewGuid(),
                ResourceType = "notices",
                DataJson = JsonSerializer.Serialize(new
                {
                    Id = "notice-001",
                    Title = "平台公告",
                    Summary = "秋季连住活动已上线，指定门店连住两晚可享房费立减。",
                    PublishedAt = DateTime.UtcNow.AddDays(-2)
                }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await SeedPageContentAsync(db);
    }

    /// <summary>
    /// 服务协议、隐私政策、联系客服等说明页面的默认文案。
    /// 小程序通过 GET /api/v1/pages/{key} 读取，管理端可覆盖 AdminManagedItems 中 pages:{key} 的 JSON。
    /// </summary>
    private static async Task SeedPageContentAsync(AppDbContext db)
    {
        const string updatedAt = "2026年09月30日";

        var rulePage = new PageContentItem
        {
            Key = "rule",
            Title = "规则与政策",
            Subtitle = "TCPMS 智选旅宿服务条款",
            Kicker = "合规与数字隐私标准",
            Badge = "合规与数字隐私标准",
            Summary = "我们致力于守护您的旅途隐私与数据安全，为您提供透明、值得信赖的数字化入住体验。",
            Version = updatedAt,
            UpdatedAt = updatedAt,
            EffectiveAt = "2026年10月01日",
            Tabs =
            [
                new PageTab
                {
                    Key = "service",
                    Label = "服务协议",
                    Kicker = "TCPMS 智选旅宿服务协议",
                    Title = "让每一次入住，都有清晰的规则和可预期的服务",
                    UpdatedAt = $"更新日期：{updatedAt}",
                    Highlights =
                    [
                        new PageHighlight { Icon = "location", Title = "位置权限原则", Tag = "非必需", Desc = "仅用于附近门店排序与推荐，拒绝授权仍可手动切换城市。" },
                        new PageHighlight { Icon = "shield", Title = "身份证件严格脱敏加密", Tag = "AES-256", Desc = "实名信息仅用于入住核验与报备，页面统一脱敏展示。" },
                        new PageHighlight { Icon = "wallet", Title = "直连清算与资金安全", Tag = "官方通道", Desc = "支付由微信官方渠道完成，平台不保存银行卡密码等敏感凭据。" }
                    ],
                    Blocks =
                    [
                        new PageBlock { Type = "paragraph", Text = "欢迎使用 TCPMS 智选旅宿小程序。使用门店浏览、房型预订、支付、入住、退款和客服等服务前，请阅读并理解本协议。完成登录、勾选同意或继续使用服务，视为你已阅读并同意本协议。" },
                        new PageBlock { Type = "heading", Text = "核心隐私与合规摘要" },
                        new PageBlock { Type = "highlights" },
                        new PageBlock { Type = "heading", Text = "一、服务范围" },
                        new PageBlock { Type = "paragraph", Text = "平台提供多门店旅宿信息展示、房型预订、订单管理、支付退款申请、入住指引和官方客服入口。具体服务内容以页面实际展示和订单确认页为准。" },
                        new PageBlock { Type = "heading", Text = "二、预订与订单" },
                        new PageBlock { Type = "paragraph", Text = "你需要提供真实、准确的入住日期、入住人数和入住人信息。订单提交后，房型库存会在指定时间内锁定；未在规定时间完成支付的订单可能自动取消并释放库存。" },
                        new PageBlock { Type = "heading", Text = "三、入住与退订" },
                        new PageBlock { Type = "paragraph", Text = "入住时间、离店时间、身份核验、房间或床位分配以及退改规则由订单详情页和门店政策共同确定。申请退款后，平台会按照规则审核并更新退款状态。" },
                        new PageBlock { Type = "heading", Text = "四、用户行为" },
                        new PageBlock { Type = "paragraph", Text = "不得使用平台从事违法活动，不得冒用他人身份，不得恶意占用库存、重复提交订单或干扰平台正常运行。发生违规时，平台可以采取限制服务、取消订单等措施。" }
                    ]
                },
                new PageTab
                {
                    Key = "privacy",
                    Label = "隐私政策",
                    Kicker = "TCPMS 智选旅宿隐私政策",
                    Title = "我们只为完成旅宿服务而使用必要信息",
                    UpdatedAt = $"更新日期：{updatedAt}",
                    Highlights =
                    [
                        new PageHighlight { Icon = "info", Title = "目的明确", Desc = "只为完成预订与履约使用" },
                        new PageHighlight { Icon = "location", Title = "位置可选", Desc = "拒绝定位仍可正常预订" },
                        new PageHighlight { Icon = "shield", Title = "安全保护", Desc = "敏感信息加密与脱敏" }
                    ],
                    Blocks =
                    [
                        new PageBlock { Type = "highlights", Style = "cards" },
                        new PageBlock { Type = "heading", Text = "一、我们收集的信息" },
                        new PageBlock { Type = "paragraph", Text = "为完成登录、下单、支付、入住核验和售后服务，我们可能处理微信身份、手机号、入住人姓名、证件信息、订单信息和必要的设备及日志信息。" },
                        new PageBlock { Type = "heading", Text = "二、位置信息的使用" },
                        new PageBlock { Type = "paragraph", Text = "获得授权后，位置信息仅用于附近门店排序、距离展示和地图导航入口。拒绝授权不会影响你浏览全部门店、预订房型和查看订单。" },
                        new PageBlock { Type = "heading", Text = "三、敏感信息保护" },
                        new PageBlock { Type = "paragraph", Text = "身份证等敏感信息会进行加密存储和页面脱敏，仅在订单履约、入住核验和相关售后场景中由经过授权的人员访问。" },
                        new PageBlock { Type = "heading", Text = "四、你的权利" },
                        new PageBlock { Type = "paragraph", Text = "你可以通过客服入口查询、更正或申请删除相关个人信息，也可以在微信设置中管理位置、手机号和订阅消息等授权。" }
                    ]
                },
                new PageTab
                {
                    Key = "sdk",
                    Label = "权限与SDK",
                    Kicker = "权限与第三方 SDK 说明",
                    Title = "每一项权限，都有明确的使用场景",
                    UpdatedAt = $"更新日期：{updatedAt}",
                    Permissions =
                    [
                        new PagePermission { Icon = "user", Title = "微信登录与手机号", Desc = "用于账号创建、身份识别和订单联系。", Required = true },
                        new PagePermission { Icon = "location", Title = "位置权限", Desc = "用于附近门店排序、距离展示和导航入口。", Required = false },
                        new PagePermission { Icon = "image", Title = "相册与相机", Desc = "用于后续客服举证、发票或入住材料上传。", Required = false },
                        new PagePermission { Icon = "book", Title = "订阅消息", Desc = "用于发送订单状态、入住提醒和退款结果。", Required = false },
                        new PagePermission { Icon = "map", Title = "高德地图服务", Desc = "用于门店地图、地址解析和坐标能力。", Required = true }
                    ],
                    Blocks =
                    [
                        new PageBlock { Type = "permissions" },
                        new PageBlock { Type = "heading", Text = "第三方服务" },
                        new PageBlock { Type = "paragraph", Text = "地图与地址解析能力由高德开放平台提供，用于门店定位、地址标准化和距离计算。微信登录、支付、订阅消息和官方客服入口由微信相关能力提供。" },
                        new PageBlock { Type = "heading", Text = "授权管理" },
                        new PageBlock { Type = "paragraph", Text = "你可以根据页面提示选择是否授权可选权限。关闭可选权限后，相关功能可能无法提供，但不影响不依赖该权限的其他旅宿服务。" }
                    ]
                }
            ]
        };

        var authPrivacyPage = new PageContentItem
        {
            Key = "auth-privacy",
            Title = "欢迎使用 TCPMS",
            Subtitle = "多门店快捷预订 · 智能客房管理",
            Badge = "安全合规保障",
            Intro = "为了向您提供门店浏览、精准定位、房源预订、微信支付及极速退款等核心服务，我们将依据合法、正当、必要的原则收集与使用您的相关信息。",
            UpdatedAt = updatedAt,
            Points =
            [
                new PageBullet { Index = 1, Title = "地理位置与多门店查找", Body = "仅在您明确授权后获取当前位置，用于智能推荐距离最近的直营门店及显示路线导航；若您拒绝授权，仍可手动切换城市。" },
                new PageBullet { Index = 2, Title = "入住人身份与预订信息", Body = "根据旅馆业实名管理要求，预订整套民宿或青年旅舍床位时需核验真实姓名、联系电话与身份证件号。" },
                new PageBullet { Index = 3, Title = "微信安全支付与退款结算", Body = "平台只记录订单编号与支付流水，不保存银行卡密码或敏感支付凭据。" },
                new PageBullet { Index = 4, Title = "您的权利与授权撤回", Body = "您可以在我的页面查看、更正个人资料或注销账户，撤回权限后我们将停止采集后续信息。" }
            ],
            Blocks =
            [
                new PageBlock { Type = "heading", Text = "位置权限原则" },
                new PageBlock { Type = "paragraph", Text = "定位仅用于附近门店排序，不是下单必填项。用户拒绝授权后仍可手动切换城市并浏览完整房源。" },
                new PageBlock { Type = "heading", Text = "入住人信息" },
                new PageBlock { Type = "paragraph", Text = "预订整套民宿或青年旅舍床位时需要核验真实姓名、联系电话和身份证件号，页面展示时默认脱敏。" },
                new PageBlock { Type = "heading", Text = "支付与退款" },
                new PageBlock { Type = "paragraph", Text = "交易通过微信官方支付完成，平台仅记录订单和支付流水，不保存银行卡密码。" }
            ]
        };

        var serviceContactPage = new PageContentItem
        {
            Key = "service-contact",
            Title = "需要帮助？我们随时在这里",
            Kicker = "TCPMS 智选旅宿",
            Summary = "预订、改期、入住和退款问题，都可以从官方服务入口获得帮助。",
            Badge = "官方微信客服",
            ServiceHours = "工作日 09:00 - 22:00 在线，订单问题会优先处理",
            SupportText = "TCPMS 官方客服：请在微信中搜索“TCPMS智选旅宿”进入客服会话。",
            Tags = ["订单变更", "入住咨询", "退款进度"],
            Notices =
            [
                "官方客服不会索要支付密码、短信验证码或完整身份证号码。",
                "退款、改期等操作以订单详情页展示的规则和状态为准。",
                "如需人工服务，请通过上方客服入口或门店前台电话联系。"
            ]
        };

        await SeedManagedItemsAsync(db, "pages:rule", [rulePage]);
        await SeedManagedItemsAsync(db, "pages:auth-privacy", [authPrivacyPage]);
        await SeedManagedItemsAsync(db, "pages:service-contact", [serviceContactPage]);
    }

    private static async Task SeedManagedItemsAsync(
        AppDbContext db,
        string resourceType,
        IEnumerable<object> items)
    {
        if (await db.AdminManagedItems.AnyAsync(x => x.ResourceType == resourceType))
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            db.AdminManagedItems.Add(new AdminManagedItem
            {
                Id = Guid.NewGuid(),
                ResourceType = resourceType,
                DataJson = JsonSerializer.Serialize(item),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }

    private static async Task SeedSubscriptionTemplatesAsync(AppDbContext db)
    {
        var templateDefinitions = new[]
        {
            new
            {
                Identifier = "miniprogram_order_success",
                Name = "预定民宿成功",
                TemplateId = "demo_miniprogram_order_success",
                Parameters = new[]
                {
                    new SubscriptionTemplateParameter("民宿名称", "thing6", "shop_name"),
                    new SubscriptionTemplateParameter("民宿地址", "thing7", "shop_address"),
                    new SubscriptionTemplateParameter("入离日期", "thing8", "start_end_date"),
                    new SubscriptionTemplateParameter("住宿姓名", "thing9", "username"),
                    new SubscriptionTemplateParameter("房型名称", "thing13", "room_name")
                }
            },
            new
            {
                Identifier = "miniprogram_cancel",
                Name = "预订取消成功通知",
                TemplateId = "demo_miniprogram_cancel",
                Parameters = new[]
                {
                    new SubscriptionTemplateParameter("订单单号", "character_string8", "order_sn"),
                    new SubscriptionTemplateParameter("酒店名称", "thing1", "shop_name"),
                    new SubscriptionTemplateParameter("入住户型", "thing7", "room_name"),
                    new SubscriptionTemplateParameter("入住日期", "date3", "start_date")
                }
            },
            new
            {
                Identifier = "miniprogram_leave_alert",
                Name = "客人退房提醒",
                TemplateId = "demo_miniprogram_leave_alert",
                Parameters = new[]
                {
                    new SubscriptionTemplateParameter("民宿名称", "thing1", "shop_name"),
                    new SubscriptionTemplateParameter("入住人", "name3", "username"),
                    new SubscriptionTemplateParameter("入住日期", "date4", "start_date"),
                    new SubscriptionTemplateParameter("到期日期", "time7", "end_date")
                }
            },
            new
            {
                Identifier = "miniprogram_enter_alert",
                Name = "预订入住提醒",
                TemplateId = "demo_miniprogram_enter_alert",
                Parameters = new[]
                {
                    new SubscriptionTemplateParameter("民宿名称", "thing1", "shop_name"),
                    new SubscriptionTemplateParameter("入住时间", "time5", "start_time"),
                    new SubscriptionTemplateParameter("备注", "thing4", "enter_remark")
                }
            },
            new
            {
                Identifier = "miniprogram_withdraw_success",
                Name = "提现审核通过",
                TemplateId = "demo_miniprogram_withdraw_success",
                Parameters = new[]
                {
                    new SubscriptionTemplateParameter("申请时间", "time1", "createtime"),
                    new SubscriptionTemplateParameter("提现金额", "amount3", "money"),
                    new SubscriptionTemplateParameter("处理时间", "time2", "checktime"),
                    new SubscriptionTemplateParameter("提现状态", "phrase4", "state"),
                    new SubscriptionTemplateParameter("备注", "thing5", "reason")
                }
            }
        };

        var existing = await db.SubscriptionTemplates.ToDictionaryAsync(x => x.Identifier);
        var now = DateTime.UtcNow;
        foreach (var definition in templateDefinitions)
        {
            if (existing.ContainsKey(definition.Identifier))
            {
                continue;
            }

            db.SubscriptionTemplates.Add(new MiniProgramSubscriptionTemplate
            {
                Id = Guid.NewGuid(),
                Type = "MiniProgramSubscriptionMessage",
                Identifier = definition.Identifier,
                Name = definition.Name,
                TemplateId = definition.TemplateId,
                ParametersJson = JsonSerializer.Serialize(definition.Parameters),
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }

    private static async Task SeedRegionsAsync(AppDbContext db)
    {
        var existing = await db.Regions.ToDictionaryAsync(x => x.Code);
        foreach (var seed in RegionSeedData.GetRegions())
        {
            if (!existing.TryGetValue(seed.Code, out var region))
            {
                region = new Region
                {
                    Code = seed.Code,
                    ParentCode = seed.ParentCode,
                    Name = seed.Name,
                    ShortName = ToShortName(seed.Name),
                    FullName = seed.FullName,
                    Level = seed.Level,
                    Initial = seed.Initial,
                    IsEnabled = true,
                    SortOrder = seed.SortOrder
                };
                db.Regions.Add(region);
                existing.Add(region.Code, region);
                continue;
            }

            // Keep editable flags such as hot/hidden intact while allowing a later
            // seed refresh to correct names, hierarchy, initials and ordering.
            region.ParentCode = seed.ParentCode;
            region.Name = seed.Name;
            region.ShortName = ToShortName(seed.Name);
            region.FullName = seed.FullName;
            region.Level = seed.Level;
            region.Initial = seed.Initial;
            region.SortOrder = seed.SortOrder;
            region.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string ToShortName(string name)
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
}
