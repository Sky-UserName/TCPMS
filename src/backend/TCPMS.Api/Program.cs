using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
builder.Services.Configure<WeChatOptions>(builder.Configuration.GetSection("WeChat"));
builder.Services.AddHttpClient<AmapService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TCPMS/1.0");
});
builder.Services.AddHttpClient<WeChatPayService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
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

// 允许跨域的来源从配置读取，开发环境默认放开；生产环境只允许显式列出的域名，
// 避免 AllowAnyOrigin 把管理后台接口暴露给任意站点。
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .ToArray() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("App", policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        return;
    }

    options.AddPolicy("App", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            // 生产环境没有配置来源时退化为同源访问，宁可后台跨域失败也不放开任意站点。
            policy.WithOrigins("https://localhost");
            return;
        }

        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddSingleton<LoginThrottle>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddHostedService<PaymentReconciliationService>();
builder.Services.AddHostedService<OrderExpiryService>();
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

var webRootDirectory = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootDirectory, "uploads", "avatars"));
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

app.UseStaticFiles();
app.UseCors("App");
app.UseAuthentication();
app.UseAuthorization();

// 初始密码未修改前，只放行改密、登出和健康检查，其余管理接口一律拒绝。
app.Use(async (context, next) =>
{
    var mustChange = context.User.FindFirst("must_change_password")?.Value;
    if (string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var allowed = path.StartsWith("/api/v1/auth/change-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/auth/logout", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/health", StringComparison.OrdinalIgnoreCase);
        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(
                new ApiError("password_change_required", "请先修改初始密码后再继续操作"));
            return;
        }
    }

    await next();
});

await SeedData.InitializeAsync(app.Services);

if (swaggerEnabled)
{
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

var api = app.MapGroup("/api/v1")
    .WithTags("公共接口");

api.MapGet("/merchant/permission-menus", async (
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var menus = await db.PermissionMenus.AsNoTracking()
        .Where(x => x.Status == "normal")
        .OrderBy(x => x.Weight)
        .ThenBy(x => x.Title)
        .Select(x => new PermissionMenuItem(
            x.Key,
            x.Title,
            x.IconKey,
            x.IconUrl,
            x.Route,
            x.Weight,
            x.Status,
            x.CreatedAt,
            x.UpdatedAt))
        .ToListAsync(cancellationToken);
    return Results.Ok(menus);
}).WithTags("用户端");

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
    LoginThrottle throttle,
    HttpRequest httpRequest,
    CancellationToken cancellationToken) =>
{
    var username = (request.Username ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new ApiError("validation_error", "账号和密码不能为空"));
    }

    var clientIp = httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var throttleKey = $"{username}|{clientIp}";
    var accountKey = $"account|{username}";
    if (throttle.IsLocked(throttleKey, out var remaining) || throttle.IsLocked(accountKey, out remaining))
    {
        return Results.Json(
            new ApiError(
                "login_locked",
                $"登录失败次数过多，请在 {(int)Math.Ceiling(remaining.TotalMinutes)} 分钟后重试"),
            statusCode: StatusCodes.Status429TooManyRequests);
    }

    var admin = await db.AdminUsers
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

    var verified = admin is not null &&
        passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password) != PasswordVerificationResult.Failed;
    if (admin is null || !verified || !admin.IsEnabled)
    {
        throttle.RegisterFailure(throttleKey);
        throttle.RegisterFailure(accountKey);
        return Results.Unauthorized();
    }

    throttle.Reset(throttleKey);
    throttle.Reset(accountKey);

    admin.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    var roles = admin.UserRoles
        .Where(x => x.Role is not null)
        .Select(x => x.Role!.Code)
        .ToArray();
    var token = tokenService.CreateAdminToken(admin, roles);
    return Results.Ok(new AdminLoginResponse(
        token.Token,
        token.ExpiresAt,
        "admin",
        admin.DisplayName,
        admin.MustChangePassword));
});

// 管理员自助改密。初始密码（超管设置）必须先改一次才能使用其他管理接口。
auth.MapPost("/change-password", async (
    ChangePasswordRequest request,
    AppDbContext db,
    IPasswordHasher<AdminUser> passwordHasher,
    TokenService tokenService,
    ClaimsPrincipal principal,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(principal.FindFirst("user_type")?.Value, "admin", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Forbid();
    }

    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var newPassword = request.NewPassword ?? string.Empty;
    if (newPassword.Length < 8 || newPassword.Length > 64)
    {
        return Results.BadRequest(new ApiError("weak_password", "新密码长度需在 8 到 64 位之间"));
    }
    if (!newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
    {
        return Results.BadRequest(new ApiError("weak_password", "新密码需同时包含字母和数字"));
    }
    if (string.Equals(newPassword, request.CurrentPassword, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ApiError("password_unchanged", "新密码不能与当前密码相同"));
    }

    var admin = await db.AdminUsers
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    if (admin is null || !admin.IsEnabled)
    {
        return Results.Unauthorized();
    }

    if (passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.CurrentPassword ?? string.Empty)
        == PasswordVerificationResult.Failed)
    {
        return Results.BadRequest(new ApiError("invalid_current_password", "当前密码不正确"));
    }

    admin.PasswordHash = passwordHasher.HashPassword(admin, newPassword);
    admin.MustChangePassword = false;
    admin.PasswordChangedAt = DateTime.UtcNow;
    admin.UpdatedAt = DateTime.UtcNow;
    db.AuditLogs.Add(CreateAudit(principal, "admin_user.change_password", "AdminUser", admin.Id.ToString(), admin.Username));
    await db.SaveChangesAsync(cancellationToken);

    // 旧 token 仍带着 must_change_password，换发一个新 token 供前端替换。
    var roles = admin.UserRoles
        .Where(x => x.Role is not null)
        .Select(x => x.Role!.Code)
        .ToArray();
    var token = tokenService.CreateAdminToken(admin, roles);
    return Results.Ok(new AdminLoginResponse(
        token.Token,
        token.ExpiresAt,
        "admin",
        admin.DisplayName,
        false));
});

auth.MapPost("/miniapp-login", async (
    MiniappLoginRequest request,
    AppDbContext db,
    TokenService tokenService,
    IHostEnvironment environment,
    WeChatPayService weChatPayService,
    HttpRequest httpRequest,
    CancellationToken cancellationToken) =>
{
    WeChatCode2Session? weChatSession = null;
    if (!string.IsNullOrWhiteSpace(request.Code))
    {
        if (!weChatPayService.IsMiniProgramConfigured)
        {
            return Results.BadRequest(new ApiError(
                "wechat_login_not_configured",
                "微信小程序 AppID 和 Secret 尚未配置"));
        }
        try
        {
            weChatSession = await weChatPayService.ExchangeCodeAsync(
                request.Code.Trim(),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ApiError("wechat_login_failed", ex.Message));
        }
    }

    if (weChatSession is null &&
        !environment.IsDevelopment())
    {
        return Results.BadRequest(new ApiError(
            "wechat_code_required",
            "生产环境必须使用微信登录凭证"));
    }

    var openId = weChatSession?.OpenId
        ?? (string.IsNullOrWhiteSpace(request.DevOpenId)
            ? $"dev-{Guid.NewGuid():N}"
            : request.DevOpenId.Trim());
    var user = await db.WxUsers.SingleOrDefaultAsync(x => x.OpenId == openId, cancellationToken);
    if (user is not null && !user.IsEnabled)
    {
        return Results.BadRequest(new ApiError("user_disabled", "当前用户已被停用"));
    }

    string? phoneNumber = null;
    if (!string.IsNullOrWhiteSpace(request.PhoneCode))
    {
        if (weChatSession is null)
        {
            return Results.BadRequest(new ApiError("wechat_code_required", "手机号授权必须先使用真实微信登录凭证"));
        }
        try
        {
            phoneNumber = await weChatPayService.ExchangePhoneNumberAsync(
                request.PhoneCode.Trim(),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new ApiError("wechat_phone_failed", ex.Message, httpRequest.HttpContext.TraceIdentifier),
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    var nickname = NormalizeNickname(request.Nickname);
    var avatarUrl = NormalizeAvatarUrl(request.AvatarUrl);
    if (user is null)
    {
        user = new WxUser
        {
            Id = Guid.NewGuid(),
            OpenId = openId,
            Nickname = nickname ?? "微信用户",
            AvatarUrl = avatarUrl,
            PhoneNumber = phoneNumber,
            UnionId = weChatSession?.UnionId,
            LastLoginAt = DateTime.UtcNow
        };
        db.WxUsers.Add(user);
    }
    else
    {
        user.Nickname = nickname ?? user.Nickname;
        user.AvatarUrl = avatarUrl ?? user.AvatarUrl;
        user.PhoneNumber = phoneNumber ?? user.PhoneNumber;
        user.UnionId = weChatSession?.UnionId ?? user.UnionId;
        user.UpdatedAt = DateTime.UtcNow;
        user.LastLoginAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync(cancellationToken);
    var token = tokenService.CreateMiniappToken(user);
    return Results.Ok(new TokenResponse(
        token.Token,
        token.ExpiresAt,
        "miniapp",
        user.Nickname ?? "微信用户",
        ToMiniappProfile(user, httpRequest)));
});

auth.MapGet("/me", async (
    ClaimsPrincipal principal,
    AppDbContext db,
    HttpRequest request,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (!userId.HasValue)
    {
        return Results.Unauthorized();
    }

    var user = await db.WxUsers.AsNoTracking().SingleOrDefaultAsync(
        x => x.Id == userId.Value,
        cancellationToken);
    return user is null
        ? Results.NotFound(new ApiError("user_not_found", "用户不存在"))
        : Results.Ok(ToMiniappProfile(user, request));
}).RequireAuthorization("Miniapp");

auth.MapPost("/phone", async (
    MiniappPhoneRequest request,
    ClaimsPrincipal principal,
    AppDbContext db,
    WeChatPayService weChatPayService,
    HttpRequest httpRequest,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (!userId.HasValue)
    {
        return Results.Unauthorized();
    }
    if (string.IsNullOrWhiteSpace(request.PhoneCode))
    {
        return Results.BadRequest(new ApiError("phone_code_required", "手机号授权凭证不能为空"));
    }

    var user = await db.WxUsers.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    if (user is null)
    {
        return Results.NotFound(new ApiError("user_not_found", "用户不存在"));
    }

    try
    {
        user.PhoneNumber = await weChatPayService.ExchangePhoneNumberAsync(
            request.PhoneCode.Trim(),
            cancellationToken);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToMiniappProfile(user, httpRequest));
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(
            new ApiError("wechat_phone_failed", ex.Message, httpRequest.HttpContext.TraceIdentifier),
            statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireAuthorization("Miniapp");

auth.MapPost("/avatar", async (
    ClaimsPrincipal principal,
    AppDbContext db,
    IWebHostEnvironment environment,
    HttpRequest request,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (!userId.HasValue)
    {
        return Results.Unauthorized();
    }

    var user = await db.WxUsers.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    if (user is null)
    {
        return Results.NotFound(new ApiError("user_not_found", "用户不存在"));
    }

    IFormFile? file;
    try
    {
        var form = await request.ReadFormAsync(cancellationToken);
        file = form.Files.GetFile("file");
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest(new ApiError("avatar_invalid", "头像上传内容无效"));
    }

    if (file is null || file.Length <= 0)
    {
        return Results.BadRequest(new ApiError("avatar_required", "请选择头像文件"));
    }
    if (file.Length > 5 * 1024 * 1024)
    {
        return Results.BadRequest(new ApiError("avatar_too_large", "头像不能超过 5MB"));
    }

    var extension = await DetectImageExtensionAsync(file, cancellationToken);
    if (extension is null)
    {
        return Results.BadRequest(new ApiError("avatar_type_invalid", "头像必须是 JPG、PNG 或 WEBP 图片"));
    }

    var avatarDirectoryPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "avatars");
    Directory.CreateDirectory(avatarDirectoryPath);
    var fileName = $"{user.Id:N}-{Guid.NewGuid():N}{extension}";
    var targetPath = Path.Combine(avatarDirectoryPath, fileName);
    await using (var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    {
        await file.CopyToAsync(stream, cancellationToken);
    }

    var oldAvatarUrl = user.AvatarUrl;
    user.AvatarUrl = $"/uploads/avatars/{fileName}";
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);

    DeleteStoredAvatar(oldAvatarUrl, environment.ContentRootPath);
    return Results.Ok(ToMiniappProfile(user, request));
}).RequireAuthorization("Miniapp");

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

api.MapPost("/payments/wechat/notify", async (
    HttpRequest request,
    AppDbContext db,
    WeChatPayService weChatPayService,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync(cancellationToken);
    var headers = request.Headers.ToDictionary(
        item => item.Key,
        item => (string?)item.Value.ToString(),
        StringComparer.OrdinalIgnoreCase);

    WeChatPaymentNotification notification;
    try
    {
        notification = weChatPayService.VerifyAndDecryptNotification(headers, body);
    }
    catch (CryptographicException)
    {
        return Results.Json(
            new { code = "FAIL", message = "签名校验失败" },
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (InvalidOperationException)
    {
        return Results.Json(
            new { code = "FAIL", message = "支付回调配置不完整" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!string.Equals(notification.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new { code = "SUCCESS", message = "已接收非成功状态通知" });
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(
        item => item.OrderNumber == notification.OutTradeNumber,
        cancellationToken);
    if (order is null)
    {
        return Results.Json(
            new { code = "FAIL", message = "订单不存在" },
            statusCode: StatusCodes.Status404NotFound);
    }
    if (notification.TotalAmountCents != order.TotalAmountCents)
    {
        return Results.Json(
            new { code = "FAIL", message = "支付金额校验失败" },
            statusCode: StatusCodes.Status400BadRequest);
    }

    try
    {
        await PaymentOrderService.MarkOrderPaidAsync(
            db,
            order,
            notification.TransactionId,
            body,
            cancellationToken);
    }
    catch (InvalidOperationException)
    {
        return Results.Json(
            new { code = "FAIL", message = "微信交易号已关联其他订单" },
            statusCode: StatusCodes.Status409Conflict);
    }
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { code = "SUCCESS", message = "成功" });
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
        .Include(x => x.RoomTypes)
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.Name)
        .ToListAsync(cancellationToken);
    var reviewCounts = await LoadPublishedReviewCountsAsync(
        db,
        storesData.Select(x => x.Id).ToArray(),
        cancellationToken);
    var data = storesData
        .Select(x => ToStoreListItem(x, null, reviewCounts.GetValueOrDefault(x.Id)))
        .ToList();
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
        .Include(x => x.RoomTypes)
        .Where(x => x.Status == StoreStatuses.Active && x.Latitude.HasValue && x.Longitude.HasValue)
        .ToListAsync(cancellationToken);
    var reviewCounts = await LoadPublishedReviewCountsAsync(
        db,
        candidates.Select(x => x.Id).ToArray(),
        cancellationToken);

    var data = candidates
        .Select(store =>
        {
            var distance = DistanceKm(latitude, longitude, (double)store.Latitude!.Value, (double)store.Longitude!.Value);
            return (store, distance);
        })
        .Where(x => x.distance <= maxDistance)
        .OrderBy(x => x.distance)
        .ThenBy(x => x.store.SortOrder)
        .Select(x => ToStoreListItem(x.store, Math.Round(x.distance, 2), reviewCounts.GetValueOrDefault(x.store.Id)))
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

    var reviewCount = await db.Reviews
        .AsNoTracking()
        .CountAsync(x => x.StoreId == store.Id &&
                         (x.Moderation == null || x.Moderation.Status == ReviewStatuses.Published),
            cancellationToken);
    var summary = ToStoreListItem(store, null, reviewCount);
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
        .Where(x => x.RoomTypeId == id &&
                    (x.Moderation == null ||
                     x.Moderation.Status == ReviewStatuses.Published))
        .Select(x => new { x.RoomRating, x.StoreRating })
        .ToListAsync(cancellationToken);
    var reviewData = await db.Reviews
        .AsNoTracking()
        .Include(x => x.User)
        .Include(x => x.Order)
        .Where(x => x.RoomTypeId == id &&
                    (x.Moderation == null ||
                     x.Moderation.Status == ReviewStatuses.Published))
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

    // 房型下架之外，门店停业/未上线也不能继续接单。
    var storeSellable = await db.Stores.AnyAsync(
        x => x.Id == request.StoreId && x.Status == StoreStatuses.Active,
        cancellationToken);
    if (!storeSellable)
    {
        return Results.Conflict(new ApiError("store_not_available", "门店暂停营业，暂不可预订"));
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
        OrderNumber = CreateOrderNumber(),
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

orders.MapPost("/{id:guid}/pay", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    WeChatPayService weChatPayService,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(
        item => item.Id == id && item.WxUserId == userId.Value,
        cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }
    if (order.Status != OrderStatuses.PendingPayment)
    {
        return Results.BadRequest(new ApiError("order_not_payable", "订单当前不可支付"));
    }
    if (order.PaymentExpiresAt <= DateTime.UtcNow)
    {
        return Results.BadRequest(new ApiError("order_payment_expired", "订单支付时间已过，请重新下单"));
    }

    var openId = principal.FindFirst("openid")?.Value;
    if (string.IsNullOrWhiteSpace(openId))
    {
        return Results.BadRequest(new ApiError("wechat_openid_missing", "当前登录态缺少微信用户标识，请重新登录"));
    }

    try
    {
        var payment = await weChatPayService.CreateJsapiPaymentAsync(
            order,
            openId,
            cancellationToken);
        return Results.Ok(payment);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(
            new ApiError("wechat_pay_not_configured", ex.Message),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException)
    {
        return Results.Json(
            new ApiError("wechat_pay_unavailable", "微信支付接口暂时不可用"),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

orders.MapGet("/{id:guid}/pay-status", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    WeChatPayService weChatPayService,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var order = await db.BookingOrders.SingleOrDefaultAsync(
        item => item.Id == id && item.WxUserId == userId.Value,
        cancellationToken);
    if (order is null)
    {
        return Results.NotFound(new ApiError("order_not_found", "订单不存在"));
    }

    // 已支付或非待支付订单直接返回，避免重复处理。
    if (order.PaymentStatus == "Paid" || order.Status != OrderStatuses.PendingPayment)
    {
        return Results.Ok(ToOrderListItem(order, null, null));
    }

    try
    {
        // 回调未到达时主动向微信查询，避免本地开发环境没有公网 HTTPS 回调导致订单卡在待支付。
        var result = await weChatPayService.QueryOrderAsync(order.OrderNumber, cancellationToken);
        if (result is not null &&
            string.Equals(result.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
            result.TotalAmountCents == order.TotalAmountCents)
        {
            await PaymentOrderService.MarkOrderPaidAsync(
                db,
                order,
                result.TransactionId,
                null,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(
            new ApiError("wechat_pay_not_configured", ex.Message),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException)
    {
        return Results.Json(
            new ApiError("wechat_pay_unavailable", "微信支付接口暂时不可用"),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(ToOrderListItem(order, null, null));
});

orders.MapPost("/{id:guid}/mock-pay", async (
    Guid id,
    AppDbContext db,
    ClaimsPrincipal principal,
    IHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (!environment.IsDevelopment())
    {
        return Results.StatusCode(StatusCodes.Status404NotFound);
    }

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

    await PaymentOrderService.MarkOrderPaidAsync(
        db,
        order,
        $"MOCK{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        null,
        cancellationToken);
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
    db.ReviewModerations.Add(new ReviewModeration
    {
        ReviewId = review.Id,
        Status = ReviewStatuses.Pending,
        UpdatedAt = DateTime.UtcNow
    });
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

static StoreListItem ToStoreListItem(Store store, double? distanceKm, int reviewCount = 0)
{
    var featuredRoom = store.RoomTypes
        .Where(x => x.IsPublished)
        .OrderBy(x => x.BasePriceCents)
        .ThenBy(x => x.SortOrder)
        .FirstOrDefault();
    var tags = store.RoomTypes
        .Where(x => x.IsPublished)
        .SelectMany(x => ParseImageUrls(x.TagsJson))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(8)
        .ToArray();

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
        store.Rating,
        store.City,
        store.CoverImageUrl,
        store.DoorImageUrl,
        store.RoomImageUrlsJson,
        featuredRoom?.BasePriceCents,
        featuredRoom?.Id,
        featuredRoom?.Name,
        reviewCount,
        tags,
        store.Description);
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
        roomType.IsPublished,
        roomType.ImageUrlsJson,
        roomType.FacilitiesJson,
        roomType.TagsJson,
        roomType.ServicesJson,
        roomType.Area,
        roomType.RoomCount);
}

static async Task<Dictionary<Guid, int>> LoadPublishedReviewCountsAsync(
    AppDbContext db,
    IReadOnlyCollection<Guid> storeIds,
    CancellationToken cancellationToken)
{
    if (storeIds.Count == 0)
    {
        return new Dictionary<Guid, int>();
    }

    return await db.Reviews
        .AsNoTracking()
        .Where(x => storeIds.Contains(x.StoreId) &&
                    (x.Moderation == null || x.Moderation.Status == ReviewStatuses.Published))
        .GroupBy(x => x.StoreId)
        .Select(group => new { StoreId = group.Key, Count = group.Count() })
        .ToDictionaryAsync(x => x.StoreId, x => x.Count, cancellationToken);
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

static string? NormalizeNickname(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var nickname = value.Trim();
    return nickname.Length > 32 ? nickname[..32] : nickname;
}

// 小程序 chooseAvatar 返回的是临时路径（wxfile:// 或 http://tmp/...），
// 这类地址换设备或重启后即失效，不能作为用户头像入库。
static string? NormalizeAvatarUrl(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var avatarUrl = value.Trim();
    if (avatarUrl.StartsWith("wxfile://", StringComparison.OrdinalIgnoreCase) ||
        avatarUrl.StartsWith("http://tmp/", StringComparison.OrdinalIgnoreCase) ||
        avatarUrl.StartsWith("https://tmp/", StringComparison.OrdinalIgnoreCase) ||
        avatarUrl.Contains("/tmp/", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var isSiteRelative = avatarUrl.StartsWith("/", StringComparison.Ordinal);
    var isAbsolute = avatarUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     avatarUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    if (!isSiteRelative && !isAbsolute)
    {
        return null;
    }

    return avatarUrl.Length > 500 ? avatarUrl[..500] : avatarUrl;
}

// 订单号：时间戳 + 6 位随机数。原来只有 4 位，同一秒内并发较高时容易撞唯一索引。
static string CreateOrderNumber() =>
    $"TCP{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100000, 1000000)}";

static MiniappProfileResponse ToMiniappProfile(WxUser user, HttpRequest? request){
    var avatarUrl = user.AvatarUrl;
    if (request is not null &&
        !string.IsNullOrWhiteSpace(avatarUrl) &&
        avatarUrl.StartsWith("/", StringComparison.Ordinal))
    {
        avatarUrl = $"{request.Scheme}://{request.Host}{avatarUrl}";
    }

    return new MiniappProfileResponse(
        user.Id,
        user.Nickname,
        user.PhoneNumber,
        avatarUrl);
}

static async Task<string?> DetectImageExtensionAsync(
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

static void DeleteStoredAvatar(string? avatarUrl, string contentRootPath)
{
    if (string.IsNullOrWhiteSpace(avatarUrl))
    {
        return;
    }

    if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out var absoluteUri))
    {
        avatarUrl = absoluteUri.AbsolutePath;
    }
    if (!avatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var avatarRoot = Path.GetFullPath(Path.Combine(
        contentRootPath,
        "wwwroot",
        "uploads",
        "avatars"));
    var candidate = Path.GetFullPath(Path.Combine(
        contentRootPath,
        "wwwroot",
        avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
    var rootWithSeparator = avatarRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    try
    {
        if (File.Exists(candidate))
        {
            File.Delete(candidate);
        }
    }
    catch (IOException)
    {
        // A stale avatar file should not make a successful profile update fail.
    }
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
