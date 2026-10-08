using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TCPMS.Api;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;
using TCPMS.Api.Infrastructure;
using TCPMS.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var swaggerEnabled = builder.Configuration.GetValue(
    "Swagger:Enabled",
    builder.Environment.IsDevelopment());

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AmapOptions>(builder.Configuration.GetSection("Amap"));
builder.Services.AddHttpClient<AmapService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TCPMS/1.0");
});

var databaseProvider = builder.Configuration["Database:Provider"] ?? "MySql";
if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("tcpms-development"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("缺少 ConnectionStrings:Default");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            connectionString,
            ServerVersion.Parse("8.0.36-mysql"),
            mysql => mysql.EnableRetryOnFailure(3)));
}

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("缺少 Jwt 配置");
if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey 至少需要 32 字节");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("user_type", "admin"));
    options.AddPolicy("Miniapp", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("user_type", "miniapp"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TCPMS 民宿预订平台 API",
        Version = "v1",
        Description = "TCPMS（短租民宿管理平台）后端接口。接口覆盖微信小程序端的门店、房型、库存、订单和退款流程，以及管理后台的门店、房型、价格、库存、订单、用户、统计和审计日志能力。金额统一使用分（CNY），时间统一使用 ISO 8601 格式。"
    });

    // 让 Swagger 能够复用项目中的 XML 文档注释，便于前端直接查看字段和类型说明。
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.SupportNonNullableReferenceTypes();
    options.DescribeAllParametersInCamelCase();
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "请输入 JWT：Bearer {token}。管理员接口需要 user_type=admin，微信小程序接口需要 user_type=miniapp。"
    });
    options.OperationFilter<ChineseOperationFilter>();
});

var app = builder.Build();

if (swaggerEnabled)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TCPMS API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "TCPMS API 接口文档";
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableTryItOutByDefault();
    });
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";
        var traceId = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(
            new ApiError("internal_error", "服务器处理请求时发生错误", traceId));
    });
});

app.UseCors("Development");
app.UseAuthentication();
app.UseAuthorization();

await SeedData.InitializeAsync(app.Services);

if (swaggerEnabled)
{
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

var api = app.MapGroup("/api/v1")
    .WithTags("公共接口");

api.MapGet("/health", (IConfiguration configuration) =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "TCPMS.Api",
        environment = app.Environment.EnvironmentName,
        databaseProvider = configuration["Database:Provider"] ?? "MySql",
        utc = DateTime.UtcNow
    });
});

var auth = api.MapGroup("/auth")
    .WithTags("认证");
auth.MapPost("/login", async (
    LoginRequest request,
    AppDbContext db,
    IPasswordHasher<AdminUser> passwordHasher,
    TokenService tokenService,
    CancellationToken cancellationToken) =>
{
    var admin = await db.AdminUsers
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .SingleOrDefaultAsync(x => x.Username == request.Username, cancellationToken);

    if (admin is null || !admin.IsEnabled ||
        passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    admin.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    var roles = admin.UserRoles
        .Where(x => x.Role is not null)
        .Select(x => x.Role!.Code)
        .ToArray();
    var token = tokenService.CreateAdminToken(admin, roles);
    return Results.Ok(new TokenResponse(token.Token, token.ExpiresAt, "admin", admin.DisplayName));
});

auth.MapPost("/miniapp-login", async (
    MiniappLoginRequest request,
    AppDbContext db,
    TokenService tokenService,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!string.IsNullOrWhiteSpace(request.Code) &&
        !string.IsNullOrWhiteSpace(configuration["WeChat:MiniProgramAppId"]))
    {
        return Results.BadRequest(new ApiError(
            "wechat_login_not_configured",
            "生产环境需要实现微信 code2Session 和手机号解密流程"));
    }

    var openId = string.IsNullOrWhiteSpace(request.DevOpenId)
        ? $"dev-{Guid.NewGuid():N}"
        : request.DevOpenId.Trim();
    var user = await db.WxUsers.SingleOrDefaultAsync(x => x.OpenId == openId, cancellationToken);
    if (user is null)
    {
        user = new WxUser
        {
            Id = Guid.NewGuid(),
            OpenId = openId,
            Nickname = request.Nickname ?? "演示用户",
            AvatarUrl = request.AvatarUrl,
            LastLoginAt = DateTime.UtcNow
        };
        db.WxUsers.Add(user);
    }
    else
    {
        user.Nickname = request.Nickname ?? user.Nickname;
        user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        user.LastLoginAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync(cancellationToken);
    var token = tokenService.CreateMiniappToken(user);
    return Results.Ok(new TokenResponse(token.Token, token.ExpiresAt, "miniapp", user.Nickname ?? "微信用户"));
});

api.MapGet("/map/config", (IOptions<AmapOptions> options) =>
{
    var value = options.Value;
    return Results.Ok(new
    {
        amapEnabled = !string.IsNullOrWhiteSpace(value.MiniProgramKey),
        miniProgramKey = value.MiniProgramKey,
        defaultRadiusKm = value.DefaultRadiusKm
    });
});

var stores = api.MapGroup("/stores")
    .WithTags("门店");
stores.MapGet("/", async (
    AppDbContext db,
    string? keyword,
    string? status,
    CancellationToken cancellationToken) =>
{
    var query = db.Stores.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(x => x.Status == status);
    }
    else
    {
        query = query.Where(x => x.Status == StoreStatuses.Active);
    }

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(x =>
            x.Name.Contains(keyword) ||
            (x.Address != null && x.Address.Contains(keyword)));
    }

    var storesData = await query
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.Name)
        .ToListAsync(cancellationToken);
    var data = storesData.Select(x => ToStoreListItem(x, null)).ToList();
    return Results.Ok(data);
});

stores.MapGet("/nearby", async (
    AppDbContext db,
    double latitude,
    double longitude,
    double? radiusKm,
    IOptions<AmapOptions> amapOptions,
    CancellationToken cancellationToken) =>
{
    if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
    {
        return Results.BadRequest(new ApiError("invalid_coordinates", "经纬度不合法"));
    }

    var maxDistance = radiusKm ?? amapOptions.Value.DefaultRadiusKm;
    if (maxDistance is <= 0 or > 500)
    {
        return Results.BadRequest(new ApiError("invalid_radius", "附近门店半径必须在 0 到 500 公里之间"));
    }

    var candidates = await db.Stores
        .AsNoTracking()
        .Where(x => x.Status == StoreStatuses.Active && x.Latitude.HasValue && x.Longitude.HasValue)
        .ToListAsync(cancellationToken);

    var data = candidates
        .Select(store =>
        {
            var distance = DistanceKm(latitude, longitude, (double)store.Latitude!.Value, (double)store.Longitude!.Value);
            return (store, distance);
        })
        .Where(x => x.distance <= maxDistance)
        .OrderBy(x => x.distance)
        .ThenBy(x => x.store.SortOrder)
        .Select(x => ToStoreListItem(x.store, Math.Round(x.distance, 2)))
        .ToList();
    return Results.Ok(data);
});

stores.MapGet("/{id:guid}", async (
    Guid id,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var store = await db.Stores
        .AsNoTracking()
        .Include(x => x.RoomTypes)
        .SingleOrDefaultAsync(x => x.Id == id && x.Status == StoreStatuses.Active, cancellationToken);
    if (store is null)
    {
        return Results.NotFound(new ApiError("store_not_found", "门店不存在或已下架"));
    }

    var summary = ToStoreListItem(store, null);
    var roomTypes = store.RoomTypes
        .Where(x => x.IsPublished)
        .OrderBy(x => x.BasePriceCents)
        .Select(ToRoomTypeSummary)
        .ToList();
    return Results.Ok(new StoreDetail(summary, roomTypes));
});

stores.MapPost("/{id:guid}/geo/geocode", async (
    Guid id,
    GeocodeRequest request,
    AppDbContext db,
    AmapService amapService,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var store = await db.Stores.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (store is null)
    {
        return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
    }
    if (!AdminEndpoints.CanManageStoreProfile(principal) ||
        !AdminEndpoints.CanAccessStore(principal, store.Id))
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.Address))
    {
        return Results.BadRequest(new ApiError("address_required", "地址不能为空"));
    }

    try
    {
        var result = await amapService.GeocodeAsync(request.Address, request.City, cancellationToken);
        if (result is null)
        {
            return Results.BadRequest(new ApiError("geocode_empty", "高德未找到匹配地址"));
        }

        ApplyGeoResult(store, result, "AmapGeocode");
        db.AuditLogs.Add(CreateAudit(principal, "store.geo.geocode", "Store", store.Id.ToString(), store.Name));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToStoreListItem(store, null));
    }
    catch (InvalidOperationException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).RequireAuthorization("Admin");

stores.MapPost("/{id:guid}/geo/reverse", async (
    Guid id,
    ReverseGeocodeRequest request,
    AppDbContext db,
    AmapService amapService,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var store = await db.Stores.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (store is null)
    {
        return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
    }
    if (!AdminEndpoints.CanManageStoreProfile(principal) ||
        !AdminEndpoints.CanAccessStore(principal, store.Id))
    {
        return Results.Forbid();
    }

    try
    {
        var result = await amapService.ReverseGeocodeAsync(request.Longitude, request.Latitude, cancellationToken);
        if (result is null)
        {
            return Results.BadRequest(new ApiError("reverse_geocode_empty", "高德未返回地址信息"));
        }

        ApplyGeoResult(store, result, "AmapReverseGeocode");
        db.AuditLogs.Add(CreateAudit(principal, "store.geo.reverse", "Store", store.Id.ToString(), store.Name));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToStoreListItem(store, null));
    }
    catch (InvalidOperationException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).RequireAuthorization("Admin");

var admin = api.MapGroup("/admin")
    .WithTags("管理后台")
    .RequireAuthorization("Admin");
admin.MapGet("/metrics", async (
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var today = DateTime.Today;
    var storesQuery = db.Stores.AsNoTracking().AsQueryable();
    var ordersQuery = db.BookingOrders.AsNoTracking().AsQueryable();
    var refundsQuery = db.RefundRecords.AsNoTracking().AsQueryable();
    if (AdminEndpoints.GetStoreScope(principal) is Guid storeScope)
    {
        storesQuery = storesQuery.Where(x => x.Id == storeScope);
        ordersQuery = ordersQuery.Where(x => x.StoreId == storeScope);
        refundsQuery = refundsQuery.Where(x => x.Order!.StoreId == storeScope);
    }

    var metrics = new AdminMetrics(
        await storesQuery.CountAsync(cancellationToken),
        await storesQuery.CountAsync(x => x.Status == StoreStatuses.Active, cancellationToken),
        await ordersQuery.CountAsync(x =>
            x.Status == OrderStatuses.PaidPendingConfirmation ||
            x.Status == OrderStatuses.ConfirmedPendingCheckIn, cancellationToken),
        await ordersQuery.CountAsync(x => x.CreatedAt >= today, cancellationToken),
        await ordersQuery
            .Where(x => x.CreatedAt >= today && x.PaymentStatus == "Paid")
            .Select(x => (int?)x.TotalAmountCents)
            .SumAsync(cancellationToken) ?? 0,
        await refundsQuery.CountAsync(x => x.Status == "PendingReview", cancellationToken));
    return Results.Ok(metrics);
});

admin.MapGet("/stores", async (
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var query = db.Stores.AsNoTracking().AsQueryable();
    if (AdminEndpoints.GetStoreScope(principal) is Guid storeScope)
    {
        query = query.Where(x => x.Id == storeScope);
    }

    var storesData = await query
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.Name)
        .ToListAsync(cancellationToken);
    var result = storesData.Select(x => ToStoreListItem(x, null)).ToList();
    return Results.Ok(result);
});

admin.MapGet("/stores/{id:guid}", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var store = await db.Stores.AsNoTracking()
        .Include(x => x.RoomTypes)
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (store is null)
    {
        return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
    }
    if (!AdminEndpoints.CanAccessStore(principal, store.Id))
    {
        return Results.Forbid();
    }

    return Results.Ok(new
    {
        id = store.Id,
        name = store.Name,
        code = store.Code,
        description = store.Description,
        province = store.Province,
        city = store.City,
        district = store.District,
        address = store.Address,
        standardAddress = store.StandardAddress,
        administrativeAreaCode = store.AdministrativeAreaCode,
        phone = store.Phone,
        longitude = store.Longitude,
        latitude = store.Latitude,
        amapPoiId = store.AmapPoiId,
        coordinateSource = store.CoordinateSource,
        coordinatesUpdatedAt = store.CoordinatesUpdatedAt,
        hasMapLocation = store.Longitude.HasValue && store.Latitude.HasValue,
        status = store.Status,
        sortOrder = store.SortOrder,
        roomTypeCount = store.RoomTypes.Count,
        ownerUsername = store.OwnerUsername,
        ownerNickname = store.OwnerNickname,
        ownerAvatarUrl = store.OwnerAvatarUrl,
        ownerName = store.OwnerName,
        ownerPhone = store.OwnerPhone,
        idCardFrontUrl = store.IdCardFrontUrl,
        idCardBackUrl = store.IdCardBackUrl,
        propertyCertificateUrl = store.PropertyCertificateUrl,
        businessLicenseUrl = store.BusinessLicenseUrl,
        doorImageUrl = store.DoorImageUrl,
        roomImageUrlsJson = store.RoomImageUrlsJson,
        rating = store.Rating
    });
});

admin.MapPost("/stores", async (
    StoreRequest request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!AdminEndpoints.CanCreateStore(principal))
    {
        return Results.Forbid();
    }
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
    {
        return Results.BadRequest(new ApiError("validation_error", "门店名称和编码不能为空"));
    }
    if (request.Longitude.HasValue != request.Latitude.HasValue)
    {
        return Results.BadRequest(new ApiError("invalid_coordinates", "经纬度必须同时填写"));
    }

    var code = request.Code.Trim().ToUpperInvariant();
    if (await db.Stores.AnyAsync(x => x.Code == code, cancellationToken))
    {
        return Results.Conflict(new ApiError("store_code_exists", "门店编码已存在"));
    }

    var store = new Store
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Code = code,
        Description = request.Description,
        Province = request.Province,
        City = request.City,
        District = request.District,
        Address = request.Address,
        Phone = request.Phone,
        Longitude = request.Longitude,
        Latitude = request.Latitude,
        OwnerUsername = request.OwnerUsername,
        OwnerNickname = request.OwnerNickname,
        OwnerAvatarUrl = request.OwnerAvatarUrl,
        OwnerName = request.OwnerName,
        OwnerPhone = request.OwnerPhone,
        IdCardFrontUrl = request.IdCardFrontUrl,
        IdCardBackUrl = request.IdCardBackUrl,
        PropertyCertificateUrl = request.PropertyCertificateUrl,
        BusinessLicenseUrl = request.BusinessLicenseUrl,
        DoorImageUrl = request.DoorImageUrl,
        RoomImageUrlsJson = request.RoomImageUrlsJson,
        Rating = request.Rating,
        Status = string.IsNullOrWhiteSpace(request.Status) ? StoreStatuses.Draft : request.Status,
        SortOrder = request.SortOrder,
        CoordinateSource = request.Longitude.HasValue && request.Latitude.HasValue ? "Manual" : null,
        CoordinatesUpdatedAt = request.Longitude.HasValue && request.Latitude.HasValue ? DateTime.UtcNow : null
    };
    db.Stores.Add(store);
    db.AuditLogs.Add(CreateAudit(principal, "store.create", "Store", store.Id.ToString(), store.Name));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/stores/{store.Id}", ToStoreListItem(store, null));
});

admin.MapPut("/stores/{id:guid}", async (
    Guid id,
    StoreRequest request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!AdminEndpoints.CanManageStoreProfile(principal))
    {
        return Results.Forbid();
    }

    var store = await db.Stores.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (store is null)
    {
        return Results.NotFound(new ApiError("store_not_found", "门店不存在"));
    }
    if (!AdminEndpoints.CanAccessStore(principal, store.Id))
    {
        return Results.Forbid();
    }
    if (request.Longitude.HasValue != request.Latitude.HasValue)
    {
        return Results.BadRequest(new ApiError("invalid_coordinates", "经纬度必须同时填写"));
    }

    store.Name = request.Name.Trim();
    store.Description = request.Description;
    store.Province = request.Province;
    store.City = request.City;
    store.District = request.District;
    store.Address = request.Address;
    store.Phone = request.Phone;
    store.OwnerUsername = request.OwnerUsername;
    store.OwnerNickname = request.OwnerNickname;
    store.OwnerAvatarUrl = request.OwnerAvatarUrl;
    store.OwnerName = request.OwnerName;
    store.OwnerPhone = request.OwnerPhone;
    store.IdCardFrontUrl = request.IdCardFrontUrl;
    store.IdCardBackUrl = request.IdCardBackUrl;
    store.PropertyCertificateUrl = request.PropertyCertificateUrl;
    store.BusinessLicenseUrl = request.BusinessLicenseUrl;
    store.DoorImageUrl = request.DoorImageUrl;
    store.RoomImageUrlsJson = request.RoomImageUrlsJson;
    store.Rating = request.Rating;
    store.Status = string.IsNullOrWhiteSpace(request.Status) ? store.Status : request.Status;
    store.SortOrder = request.SortOrder;
    if (request.Longitude.HasValue && request.Latitude.HasValue)
    {
        store.Longitude = request.Longitude;
        store.Latitude = request.Latitude;
        store.CoordinateSource = "Manual";
        store.CoordinatesUpdatedAt = DateTime.UtcNow;
    }
    else
    {
        store.Longitude = null;
        store.Latitude = null;
        store.CoordinateSource = null;
        store.CoordinatesUpdatedAt = null;
    }
    store.UpdatedAt = DateTime.UtcNow;
    db.AuditLogs.Add(CreateAudit(principal, "store.update", "Store", store.Id.ToString(), store.Name));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToStoreListItem(store, null));
});

var roomTypes = api.MapGroup("/room-types")
    .WithTags("房型");
roomTypes.MapGet("/", async (
    Guid? storeId,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var query = db.RoomTypes.AsNoTracking().Where(x => x.IsPublished);
    if (storeId.HasValue)
    {
        query = query.Where(x => x.StoreId == storeId.Value);
    }
    var roomTypeData = await query.OrderBy(x => x.BasePriceCents).ToListAsync(cancellationToken);
    var result = roomTypeData.Select(ToRoomTypeSummary).ToList();
    return Results.Ok(result);
});

roomTypes.MapGet("/{id:guid}/availability", async (
    Guid id,
    DateTime checkIn,
    DateTime checkOut,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    if (!TryValidateDateRange(checkIn, checkOut, out var error))
    {
        return Results.BadRequest(new ApiError("invalid_date_range", error!));
    }

    var roomType = await db.RoomTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (roomType is null)
    {
        return Results.NotFound(new ApiError("room_type_not_found", "房型不存在"));
    }

    var daily = await LoadAvailabilityAsync(db, roomType, checkIn, checkOut, cancellationToken);
    return Results.Ok(daily);
});

roomTypes.MapGet("/{id:guid}/reviews", async (
    Guid id,
    int? limit,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var roomType = await db.RoomTypes
        .AsNoTracking()
        .Include(x => x.Store)
        .SingleOrDefaultAsync(x => x.Id == id && x.IsPublished, cancellationToken);
    if (roomType is null || roomType.Store is null)
    {
        return Results.NotFound(new ApiError("room_type_not_found", "房型不存在或已下架"));
    }

    var ratingData = await db.Reviews
        .AsNoTracking()
        .Where(x => x.RoomTypeId == id)
        .Select(x => new { x.RoomRating, x.StoreRating })
        .ToListAsync(cancellationToken);
    var reviewData = await db.Reviews
        .AsNoTracking()
        .Include(x => x.User)
        .Include(x => x.Order)
        .Where(x => x.RoomTypeId == id)
        .OrderByDescending(x => x.CreatedAt)
        .Take(Math.Clamp(limit ?? 50, 1, 50))
        .ToListAsync(cancellationToken);

    var response = new RoomReviewResponse(
        roomType.Id,
        roomType.StoreId,
        roomType.Name,
        roomType.Store.Name,
        ratingData.Count == 0 ? 0 : Math.Round((decimal)ratingData.Average(x => x.RoomRating), 1),
        ratingData.Count == 0 ? 0 : Math.Round((decimal)ratingData.Average(x => x.StoreRating), 1),
        ratingData.Count,
        reviewData.Select(ToReviewItem).ToList());
    return Results.Ok(response);
});

var orders = api.MapGroup("/orders")
    .WithTags("用户订单")
    .RequireAuthorization("Miniapp");
orders.MapPost("/preview", async (
    OrderQuoteRequest request,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var quote = await BuildQuoteAsync(db, request, cancellationToken);
    return quote.Error is not null
        ? Results.BadRequest(new ApiError("quote_error", quote.Error))
        : Results.Ok(quote.Quote);
});

orders.MapPost("/", async (
    CreateOrderRequest request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!TryValidateDateRange(request.CheckIn, request.CheckOut, out var dateError))
    {
        return Results.BadRequest(new ApiError("invalid_date_range", dateError!));
    }
    if (request.Quantity <= 0 || request.GuestCount <= 0)
    {
        return Results.BadRequest(new ApiError("invalid_quantity", "预订数量和入住人数必须大于 0"));
    }

    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var roomType = await db.RoomTypes.SingleOrDefaultAsync(
        x => x.Id == request.RoomTypeId && x.StoreId == request.StoreId && x.IsPublished,
        cancellationToken);
    if (roomType is null)
    {
        return Results.NotFound(new ApiError("room_type_not_found", "房型不存在或已下架"));
    }

    var nights = GetNights(request.CheckIn, request.CheckOut);
    await using var transaction = db.Database.IsRelational()
        ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
        : null;

    var inventories = await db.InventoryDailies
        .Where(x => x.RoomTypeId == request.RoomTypeId &&
                    x.Date >= request.CheckIn.Date &&
                    x.Date < request.CheckOut.Date)
        .OrderBy(x => x.Date)
        .ToListAsync(cancellationToken);
    if (inventories.Count != nights)
    {
        return Results.Conflict(new ApiError("inventory_missing", "所选日期尚未配置库存"));
    }
    if (inventories.Any(x => x.AvailableQuantity < request.Quantity))
    {
        return Results.Conflict(new ApiError("inventory_insufficient", "所选日期库存不足"));
    }

    var prices = await db.PriceCalendars
        .Where(x => x.RoomTypeId == request.RoomTypeId &&
                    x.Date >= request.CheckIn.Date &&
                    x.Date < request.CheckOut.Date)
        .ToDictionaryAsync(x => x.Date.Date, cancellationToken);
    var dailyPrices = inventories.Select(x =>
        prices.TryGetValue(x.Date.Date, out var price) ? price.PriceCents : roomType.BasePriceCents).ToArray();
    var total = dailyPrices.Sum() * request.Quantity;

    foreach (var inventory in inventories)
    {
        inventory.LockedQuantity += request.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;
    }

    var order = new BookingOrder
    {
        Id = Guid.NewGuid(),
        OrderNumber = $"TCP{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
        WxUserId = userId.Value,
        StoreId = request.StoreId,
        RoomTypeId = request.RoomTypeId,
        CheckIn = request.CheckIn.Date,
        CheckOut = request.CheckOut.Date,
        Quantity = request.Quantity,
        GuestCount = request.GuestCount,
        TotalAmountCents = total,
        Status = OrderStatuses.PendingPayment,
        PaymentStatus = "Unpaid",
        GuestSnapshotJson = request.GuestSnapshotJson,
        PriceSnapshotJson = JsonSerializer.Serialize(new
        {
            dailyPrices,
            currency = "CNY",
            nights,
            quantity = request.Quantity
        }),
        PaymentExpiresAt = DateTime.UtcNow.AddMinutes(15)
    };
    db.BookingOrders.Add(order);
    db.AuditLogs.Add(CreateAudit(principal, "order.create", "BookingOrder", order.Id.ToString(), order.OrderNumber));
    await db.SaveChangesAsync(cancellationToken);
    if (transaction is not null)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    return Results.Created($"/api/v1/orders/{order.Id}", ToOrderListItem(order, roomType.Name, null));
});

orders.MapGet("/me", async (
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var orderData = await db.BookingOrders.AsNoTracking()
        .Include(x => x.Store)
        .Include(x => x.RoomType)
        .Where(x => x.WxUserId == userId.Value)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync(cancellationToken);
    var reviewOrderIds = (await db.Reviews.AsNoTracking()
            .Where(x => x.WxUserId == userId.Value)
            .Select(x => x.OrderId)
            .ToListAsync(cancellationToken))
        .ToHashSet();
    var data = orderData
        .Select(x => ToOrderListItem(x, x.RoomType?.Name, x.Store?.Name, reviewOrderIds.Contains(x.Id)))
        .ToList();
    return Results.Ok(data);
});

orders.MapGet("/{id:guid}", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    var order = await db.BookingOrders.AsNoTracking()
        .Include(x => x.Store)
        .Include(x => x.RoomType)
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (order is null || (userId.HasValue && order.WxUserId != userId.Value))
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    var hasReview = await db.Reviews.AsNoTracking().AnyAsync(x => x.OrderId == id, cancellationToken);
    return Results.Ok(ToOrderListItem(order, order.RoomType?.Name ?? string.Empty, order.Store?.Name, hasReview));
});

orders.MapPost("/{id:guid}/cancel", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    var order = await db.BookingOrders.SingleOrDefaultAsync(
        x => x.Id == id && x.WxUserId == userId,
        cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (order.Status != OrderStatuses.PendingPayment)
    {
        return Results.BadRequest(new ApiError("order_not_cancelable", "已支付订单请提交退款申请，不能直接取消"));
    }

    var inventories = await db.InventoryDailies
        .Where(x => x.RoomTypeId == order.RoomTypeId &&
                    x.Date >= order.CheckIn &&
                    x.Date < order.CheckOut)
        .ToListAsync(cancellationToken);
    foreach (var inventory in inventories)
    {
        inventory.LockedQuantity = Math.Max(0, inventory.LockedQuantity - order.Quantity);
    }
    order.Status = OrderStatuses.Cancelled;
    order.CancellationReason = "用户取消";
    order.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToOrderListItem(order, null, null));
});

orders.MapPost("/{id:guid}/mock-pay", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    var order = await db.BookingOrders.SingleOrDefaultAsync(
        x => x.Id == id && x.WxUserId == userId,
        cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (order.Status != OrderStatuses.PendingPayment)
    {
        return Results.BadRequest(new ApiError("order_not_payable", "订单当前不可支付"));
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
    }
    order.Status = OrderStatuses.PaidPendingConfirmation;
    order.PaymentStatus = "Paid";
    order.UpdatedAt = DateTime.UtcNow;
    db.PaymentRecords.Add(new PaymentRecord
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        TransactionNumber = $"MOCK{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        AmountCents = order.TotalAmountCents,
        Status = "Paid",
        PaidAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToOrderListItem(order, null, null));
});

orders.MapGet("/{id:guid}/review", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    var review = await db.Reviews
        .AsNoTracking()
        .Include(x => x.User)
        .Include(x => x.Order)
        .SingleOrDefaultAsync(x => x.OrderId == id && x.WxUserId == userId, cancellationToken);
    return review is null
        ? Results.NotFound(new ApiError("review_not_found", "该订单还没有评价"))
        : Results.Ok(ToReviewItem(review));
});

orders.MapPost("/{id:guid}/review", async (
    Guid id,
    CreateReviewRequest request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }
    if (request.RoomRating is < 1 or > 5 || request.StoreRating is < 1 or > 5)
    {
        return Results.BadRequest(new ApiError("invalid_rating", "房型评分和门店评分必须在 1 到 5 分之间"));
    }

    var content = request.Content?.Trim() ?? string.Empty;
    if (content.Length > 500)
    {
        return Results.BadRequest(new ApiError("review_content_too_long", "评价内容最多 500 个字"));
    }
    var imageUrls = (request.ImageUrls ?? Array.Empty<string>())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .Take(9)
        .ToArray();
    if (imageUrls.Any(x => x.Length > 500))
    {
        return Results.BadRequest(new ApiError("review_image_url_too_long", "评价图片地址过长"));
    }

    var order = await db.BookingOrders
        .Include(x => x.Store)
        .Include(x => x.RoomType)
        .SingleOrDefaultAsync(x => x.Id == id && x.WxUserId == userId.Value, cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (order.Status != OrderStatuses.Completed)
    {
        return Results.BadRequest(new ApiError("order_not_reviewable", "订单完成后才可以评价"));
    }
    if (await db.Reviews.AnyAsync(x => x.OrderId == id, cancellationToken))
    {
        return Results.Conflict(new ApiError("review_exists", "该订单已经评价过了"));
    }

    var review = new Review
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        WxUserId = userId.Value,
        StoreId = order.StoreId,
        RoomTypeId = order.RoomTypeId,
        RoomRating = request.RoomRating,
        StoreRating = request.StoreRating,
        Content = content,
        ImageUrlsJson = imageUrls.Length == 0 ? null : JsonSerializer.Serialize(imageUrls),
        Order = order,
        User = await db.WxUsers.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken)
    };
    db.Reviews.Add(review);
    db.AuditLogs.Add(CreateAudit(principal, "review.create", "Review", review.Id.ToString(), order.OrderNumber));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/orders/{id}/review", ToReviewItem(review));
});

var adminOrders = admin.MapGroup("/orders")
    .WithTags("管理后台订单");
adminOrders.MapGet("/", async (
    AppDbContext db,
    string? status,
    Guid? storeId,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    var query = db.BookingOrders.AsNoTracking()
        .Include(x => x.Store)
        .Include(x => x.RoomType)
        .AsQueryable();
    if (AdminEndpoints.GetStoreScope(principal) is Guid storeScope)
    {
        storeId = storeScope;
    }
    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(x => x.Status == status);
    }
    if (storeId.HasValue)
    {
        query = query.Where(x => x.StoreId == storeId.Value);
    }
    var orderData = await query.OrderByDescending(x => x.CreatedAt)
        .Take(500)
        .ToListAsync(cancellationToken);
    var result = orderData.Select(x => ToOrderListItem(x, x.RoomType?.Name, x.Store?.Name)).ToList();
    return Results.Ok(result);
});

adminOrders.MapPost("/{id:guid}/confirm", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!AdminEndpoints.CanManageOrders(principal))
    {
        return Results.Forbid();
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (!AdminEndpoints.CanAccessStore(principal, order.StoreId))
    {
        return Results.Forbid();
    }
    if (order.Status != OrderStatuses.PaidPendingConfirmation)
    {
        return Results.BadRequest(new ApiError("order_not_confirmable", "当前订单不允许确认"));
    }
    order.Status = OrderStatuses.ConfirmedPendingCheckIn;
    order.UpdatedAt = DateTime.UtcNow;
    db.AuditLogs.Add(CreateAudit(principal, "order.confirm", "BookingOrder", order.Id.ToString(), order.OrderNumber));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

adminOrders.MapPost("/{id:guid}/check-in", async (
    Guid id,
    CheckInRequest? request,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!AdminEndpoints.CanManageOrders(principal))
    {
        return Results.Forbid();
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (!AdminEndpoints.CanAccessStore(principal, order.StoreId))
    {
        return Results.Forbid();
    }
    if (order.Status != OrderStatuses.ConfirmedPendingCheckIn)
    {
        return Results.BadRequest(new ApiError("order_not_checkinable", "当前订单不允许办理入住"));
    }

    var unitsQuery = db.RoomUnits
        .Where(x => x.RoomTypeId == order.RoomTypeId && x.Status == "Available")
        .OrderBy(x => x.Code);
    List<RoomUnit> units;
    if (request?.ResourceCodes is { Count: > 0 })
    {
        var requestedCodes = request.ResourceCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        units = await db.RoomUnits
            .Where(x => x.RoomTypeId == order.RoomTypeId && requestedCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);
        if (units.Count != requestedCodes.Count || units.Any(x => x.Status != "Available"))
        {
            return Results.Conflict(new ApiError("resource_unavailable", "选择的房间或床位已被占用，请刷新后重试"));
        }
    }
    else
    {
        units = await unitsQuery.Take(order.Quantity).ToListAsync(cancellationToken);
    }

    if (units.Count != order.Quantity)
    {
        return Results.Conflict(new ApiError("resource_insufficient", "没有足够的可用房间或床位"));
    }
    foreach (var unit in units)
    {
        unit.Status = "Occupied";
        unit.UpdatedAt = DateTime.UtcNow;
    }

    order.Status = OrderStatuses.CheckedIn;
    order.AssignedResourcesJson = JsonSerializer.Serialize(units.Select(x => x.Code).ToArray());
    order.CheckedInAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    db.AuditLogs.Add(CreateAudit(
        principal,
        "order.check_in",
        "BookingOrder",
        order.Id.ToString(),
        request?.Note ?? order.OrderNumber));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

adminOrders.MapPost("/{id:guid}/check-out", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!AdminEndpoints.CanManageOrders(principal))
    {
        return Results.Forbid();
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (!AdminEndpoints.CanAccessStore(principal, order.StoreId))
    {
        return Results.Forbid();
    }
    if (order.Status != OrderStatuses.CheckedIn)
    {
        return Results.BadRequest(new ApiError("order_not_checkoutable", "当前订单不允许办理离店"));
    }

    var assignedCodes = string.IsNullOrWhiteSpace(order.AssignedResourcesJson)
        ? new List<string>()
        : JsonSerializer.Deserialize<string[]>(order.AssignedResourcesJson)?.ToList() ?? new List<string>();
    if (assignedCodes.Count > 0)
    {
        var units = await db.RoomUnits
            .Where(x => x.RoomTypeId == order.RoomTypeId && assignedCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);
        foreach (var unit in units)
        {
            if (unit.Status == "Occupied")
            {
                unit.Status = "Available";
                unit.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    order.Status = OrderStatuses.Completed;
    order.CheckedOutAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    db.AuditLogs.Add(CreateAudit(principal, "order.check_out", "BookingOrder", order.Id.ToString(), order.OrderNumber));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

AdminEndpoints.Map(admin);
MiniappOrderEndpoints.Map(orders);

static StoreListItem ToStoreListItem(Store store, double? distanceKm)
{
    return new StoreListItem(
        store.Id,
        store.Name,
        store.StandardAddress ?? store.Address,
        store.Phone,
        store.Status,
        store.Longitude,
        store.Latitude,
        distanceKm,
        store.Longitude.HasValue && store.Latitude.HasValue,
        store.OwnerName,
        store.OwnerPhone,
        store.OwnerAvatarUrl,
        store.Rating);
}

static RoomTypeSummary ToRoomTypeSummary(RoomType roomType)
{
    return new RoomTypeSummary(
        roomType.Id,
        roomType.StoreId,
        roomType.Name,
        roomType.Kind,
        roomType.BedCount,
        roomType.Gender,
        roomType.MaxGuests,
        roomType.BasePriceCents,
        roomType.Description,
        roomType.IsPublished);
}

static OrderListItem ToOrderListItem(
    BookingOrder order,
    string? roomTypeName,
    string? storeName,
    bool hasReview = false)
{
    return new OrderListItem(
        order.Id,
        order.OrderNumber,
        storeName ?? order.Store?.Name ?? string.Empty,
        roomTypeName ?? order.RoomType?.Name ?? string.Empty,
        order.CheckIn,
        order.CheckOut,
        order.Quantity,
        order.TotalAmountCents,
        order.Status,
        order.PaymentStatus,
        order.CreatedAt,
        order.StoreId,
        order.RoomTypeId,
        hasReview);
}

static ReviewItem ToReviewItem(Review review)
{
    return new ReviewItem(
        review.Id,
        review.OrderId,
        review.User?.Nickname ?? "微信用户",
        review.User?.AvatarUrl,
        review.CreatedAt,
        review.Order?.CheckIn,
        review.RoomRating,
        review.StoreRating,
        review.Content,
        ParseImageUrls(review.ImageUrlsJson));
}

static IReadOnlyList<string> ParseImageUrls(string? imageUrlsJson)
{
    if (string.IsNullOrWhiteSpace(imageUrlsJson))
    {
        return Array.Empty<string>();
    }
    try
    {
        return JsonSerializer.Deserialize<string[]>(imageUrlsJson) ?? Array.Empty<string>();
    }
    catch (JsonException)
    {
        return Array.Empty<string>();
    }
}

static AuditLog CreateAudit(
    ClaimsPrincipal principal,
    string action,
    string resourceType,
    string resourceId,
    string? summary)
{
    var actorId = GetUserId(principal);
    return new AuditLog
    {
        Id = Guid.NewGuid(),
        ActorId = actorId,
        ActorType = principal.FindFirstValue("user_type") ?? "System",
        Action = action,
        ResourceType = resourceType,
        ResourceId = resourceId,
        Summary = summary
    };
}

static Guid? GetUserId(ClaimsPrincipal principal)
{
    var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(raw, out var id) ? id : null;
}

static void ApplyGeoResult(Store store, GeoResult result, string source)
{
    store.Longitude = result.Longitude;
    store.Latitude = result.Latitude;
    store.StandardAddress = result.FormattedAddress ?? store.StandardAddress;
    store.Province = result.Province ?? store.Province;
    store.City = result.City ?? store.City;
    store.District = result.District ?? store.District;
    store.AdministrativeAreaCode = result.AdministrativeAreaCode;
    store.AmapPoiId = result.PoiId;
    store.CoordinateSource = source;
    store.CoordinatesUpdatedAt = DateTime.UtcNow;
    store.UpdatedAt = DateTime.UtcNow;
}

static bool TryValidateDateRange(DateTime checkIn, DateTime checkOut, out string? error)
{
    checkIn = checkIn.Date;
    checkOut = checkOut.Date;
    if (checkIn < DateTime.Today)
    {
        error = "入住日期不能早于今天";
        return false;
    }
    if (checkOut <= checkIn)
    {
        error = "入住日期必须早于离店日期";
        return false;
    }
    if ((checkOut - checkIn).TotalDays > 90)
    {
        error = "单次预订最多 90 晚";
        return false;
    }
    error = null;
    return true;
}

static int GetNights(DateTime checkIn, DateTime checkOut)
{
    return (int)(checkOut.Date - checkIn.Date).TotalDays;
}

static double DistanceKm(double latitude1, double longitude1, double latitude2, double longitude2)
{
    const double earthRadiusKm = 6371;
    var dLat = DegreesToRadians(latitude2 - latitude1);
    var dLon = DegreesToRadians(longitude2 - longitude1);
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(latitude1)) *
            Math.Cos(DegreesToRadians(latitude2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}

static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

static async Task<List<AvailabilityItem>> LoadAvailabilityAsync(
    AppDbContext db,
    RoomType roomType,
    DateTime checkIn,
    DateTime checkOut,
    CancellationToken cancellationToken)
{
    var inventories = await db.InventoryDailies.AsNoTracking()
        .Where(x => x.RoomTypeId == roomType.Id &&
                    x.Date >= checkIn.Date &&
                    x.Date < checkOut.Date)
        .OrderBy(x => x.Date)
        .ToListAsync(cancellationToken);
    var prices = await db.PriceCalendars.AsNoTracking()
        .Where(x => x.RoomTypeId == roomType.Id &&
                    x.Date >= checkIn.Date &&
                    x.Date < checkOut.Date)
        .ToDictionaryAsync(x => x.Date.Date, cancellationToken);
    return inventories.Select(item => new AvailabilityItem(
        item.Date,
        item.TotalQuantity,
        item.LockedQuantity,
        item.SoldQuantity,
        item.AvailableQuantity,
        prices.TryGetValue(item.Date.Date, out var price) ? price.PriceCents : roomType.BasePriceCents)).ToList();
}

static async Task<(OrderQuote? Quote, string? Error)> BuildQuoteAsync(
    AppDbContext db,
    OrderQuoteRequest request,
    CancellationToken cancellationToken)
{
    if (!TryValidateDateRange(request.CheckIn, request.CheckOut, out var dateError))
    {
        return (null, dateError);
    }
    if (request.Quantity <= 0 || request.GuestCount <= 0)
    {
        return (null, "预订数量和入住人数必须大于 0");
    }

    var roomType = await db.RoomTypes.AsNoTracking().SingleOrDefaultAsync(
        x => x.Id == request.RoomTypeId &&
             x.StoreId == request.StoreId &&
             x.IsPublished,
        cancellationToken);
    if (roomType is null)
    {
        return (null, "房型不存在或已下架");
    }
    if (request.GuestCount > roomType.MaxGuests * request.Quantity)
    {
        return (null, "入住人数超过房型可承载人数");
    }

    var dailyItems = await LoadAvailabilityAsync(db, roomType, request.CheckIn, request.CheckOut, cancellationToken);
    if (dailyItems.Count != GetNights(request.CheckIn, request.CheckOut))
    {
        return (null, "所选日期尚未配置完整库存");
    }
    if (dailyItems.Any(x => x.AvailableQuantity < request.Quantity))
    {
        return (null, "所选日期库存不足");
    }
    var total = dailyItems.Sum(x => x.PriceCents) * request.Quantity;
    return (new OrderQuote(
        request.StoreId,
        request.RoomTypeId,
        request.CheckIn.Date,
        request.CheckOut.Date,
        dailyItems.Count,
        request.Quantity,
        request.GuestCount,
        total,
        dailyItems), null);
}

app.Run();
