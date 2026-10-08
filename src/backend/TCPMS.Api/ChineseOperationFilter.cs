using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TCPMS.Api.Contracts;
using TCPMS.Api.Domain;

namespace TCPMS.Api;

/// <summary>
/// 为 Minimal API 补充稳定的中文摘要、说明、OperationId 和认证标记。
/// Minimal API 没有控制器 XML 注释入口，集中处理可以保证 Swagger 文档完整且便于前端检索。
/// </summary>
internal sealed class ChineseOperationFilter : IOperationFilter
{
    private static readonly IReadOnlyDictionary<string, OperationDescription> Descriptions =
        new Dictionary<string, OperationDescription>(StringComparer.OrdinalIgnoreCase)
        {
            ["GET /api/v1/health"] = new("健康检查", "检查 API 服务、运行环境和数据库提供程序是否可用。", "getHealth"),
            ["GET /api/v1/map/config"] = new("获取地图配置", "返回高德地图是否启用、小程序端 Key 和默认附近搜索半径。", "getMapConfig"),
            ["POST /api/v1/auth/login"] = new("管理员登录", "使用管理员账号密码登录，成功后返回用于管理后台接口的 JWT。", "adminLogin"),
            ["POST /api/v1/auth/miniapp-login"] = new("微信小程序登录", "创建或更新小程序用户，并返回用于用户端接口的 JWT。开发环境支持 DevOpenId；生产环境应接入微信 code2Session。", "miniappLogin"),
            ["GET /api/v1/stores"] = new("查询门店列表", "按关键词和营业状态查询门店。未传 status 时只返回营业中的门店。", "listStores"),
            ["GET /api/v1/stores/nearby"] = new("查询附近门店", "根据经纬度和半径计算距离，按距离由近到远返回营业中的门店。", "listNearbyStores"),
            ["GET /api/v1/stores/{id}"] = new("获取门店详情", "返回门店基础资料及已发布房型摘要。", "getStore"),
            ["POST /api/v1/stores/{id}/geo/geocode"] = new("门店地址转坐标", "调用高德地理编码服务，将门店地址转换为经纬度并写回门店资料。需要管理员权限。", "geocodeStore"),
            ["POST /api/v1/stores/{id}/geo/reverse"] = new("门店坐标转地址", "调用高德逆地理编码服务，将经纬度转换为标准地址并写回门店资料。需要管理员权限。", "reverseGeocodeStore"),
            ["GET /api/v1/room-types"] = new("查询房型列表", "查询已发布房型，可按门店 ID 筛选；结果按基础价格升序排列。", "listRoomTypes"),
            ["GET /api/v1/room-types/{id}/availability"] = new("查询房型库存和价格", "返回指定房型在入住日期区间内的每日库存、锁定量、售出量、可用量和价格。", "getRoomTypeAvailability"),
            ["GET /api/v1/room-types/{id}/reviews"] = new("查询房型评价", "返回房型和所属门店的评分汇总，以及住客公开评价列表。", "listRoomTypeReviews"),
            ["POST /api/v1/orders/preview"] = new("预览订单报价", "校验日期、库存和房型后计算入住晚数、每日价格和订单总价，不会创建订单。", "previewOrder"),
            ["POST /api/v1/orders"] = new("创建订单", "锁定对应日期库存并创建待支付订单。需要微信小程序用户 JWT。", "createOrder"),
            ["GET /api/v1/orders/me"] = new("查询我的订单", "返回当前登录小程序用户的订单列表，按创建时间倒序排列。", "listMyOrders"),
            ["GET /api/v1/orders/{id}"] = new("获取订单详情", "返回当前用户拥有的订单及房型、门店摘要。", "getMyOrder"),
            ["POST /api/v1/orders/{id}/cancel"] = new("取消待支付订单", "仅允许取消待支付订单，并释放已锁定库存。", "cancelMyOrder"),
            ["POST /api/v1/orders/{id}/mock-pay"] = new("模拟支付订单", "开发环境模拟支付成功，更新订单支付状态并写入支付记录。", "mockPayOrder"),
            ["GET /api/v1/orders/{id}/review"] = new("查询订单评价", "查询当前用户指定订单已提交的评价。", "getOrderReview"),
            ["POST /api/v1/orders/{id}/review"] = new("提交订单评价", "仅允许已完成订单提交一次评价，同时保存房型评分、门店评分、文字和图片。", "createOrderReview"),
            ["POST /api/v1/orders/{id}/refunds"] = new("申请订单退款", "提交已支付订单的退款申请，申请进入待审核状态。", "createOrderRefund"),
            ["GET /api/v1/orders/{id}/refunds"] = new("查询订单退款记录", "查询当前用户指定订单的退款申请和处理结果。", "listOrderRefunds"),
            ["GET /api/v1/admin/me"] = new("获取当前管理员", "返回管理员身份、角色和门店数据权限范围。", "getAdminMe"),
            ["GET /api/v1/admin/metrics"] = new("获取管理后台指标", "返回门店、订单、今日销售和待审核退款等运营指标。", "getAdminMetrics"),
            ["GET /api/v1/admin/stores"] = new("管理后台查询门店", "返回管理后台可访问范围内的门店列表。", "adminListStores"),
            ["POST /api/v1/admin/stores"] = new("创建门店", "创建门店基础资料，可选填经纬度。需要总部管理员或运营角色。", "adminCreateStore"),
            ["GET /api/v1/admin/stores/{id}"] = new("获取门店管理详情", "返回门店完整资料、坐标和房型数量。", "adminGetStore"),
            ["PUT /api/v1/admin/stores/{id}"] = new("更新门店", "更新门店名称、地址、联系方式、营业状态和排序等资料。", "adminUpdateStore"),
            ["GET /api/v1/admin/room-types"] = new("管理后台查询房型", "查询房型及所属门店，可按门店筛选并选择是否包含未发布房型。", "adminListRoomTypes"),
            ["POST /api/v1/admin/room-types"] = new("创建房型", "为门店创建房型，配置床位数、入住人数、价格、设施和图片。", "adminCreateRoomType"),
            ["PUT /api/v1/admin/room-types/{id}"] = new("更新房型", "更新房型资料、价格、设施、图片及发布审核状态。", "adminUpdateRoomType"),
            ["DELETE /api/v1/admin/room-types/{id}"] = new("删除房型", "删除没有订单和评价关联的房型及其房间、价格和库存数据。", "adminDeleteRoomType"),
            ["POST /api/v1/admin/room-types/{id}/review"] = new("审核房型", "审核用户或后台提交的房型，支持通过和拒绝并填写拒绝原因。", "adminReviewRoomType"),
            ["POST /api/v1/admin/room-types/{id}/publish"] = new("发布或下架房型", "切换房型发布状态，发布后才会出现在用户端房型列表。", "adminPublishRoomType"),
            ["GET /api/v1/admin/room-types/{roomTypeId}/units"] = new("查询房间或床位", "查询房型下的房间/床位资源，可按资源状态筛选。", "adminListRoomUnits"),
            ["POST /api/v1/admin/room-types/{roomTypeId}/units"] = new("创建房间或床位", "为房型新增可分配的房间或床位资源。", "adminCreateRoomUnit"),
            ["PUT /api/v1/admin/room-units/{id}"] = new("更新房间或床位", "更新资源编码、类型、性别限制、状态和备注。", "adminUpdateRoomUnit"),
            ["GET /api/v1/admin/prices"] = new("查询价格日历", "查询指定房型日期范围内的每日价格，范围最长 31 天。", "adminListPrices"),
            ["PUT /api/v1/admin/prices/bulk"] = new("批量设置价格", "按日期范围批量设置房型价格，范围最长 31 天。", "adminBulkUpdatePrices"),
            ["GET /api/v1/admin/inventory"] = new("查询库存日历", "查询指定房型日期范围内的库存、锁定、售出和可用数量。", "adminListInventory"),
            ["POST /api/v1/admin/inventory/adjust"] = new("调整库存", "按日期范围批量调整库存数量并记录调整原因。", "adminAdjustInventory"),
            ["GET /api/v1/admin/orders/{id}"] = new("获取管理订单详情", "返回订单、支付记录和退款记录，供管理后台处理订单。", "adminGetOrder"),
            ["POST /api/v1/admin/orders/{id}/cancel"] = new("管理后台取消订单", "管理员取消订单并释放相应库存。", "adminCancelOrder"),
            ["GET /api/v1/admin/orders"] = new("管理后台查询订单", "按状态和门店筛选订单，最多返回最近 500 条。", "adminListOrders"),
            ["POST /api/v1/admin/orders/{id}/confirm"] = new("确认订单", "将已支付待确认订单推进为待入住状态。", "adminConfirmOrder"),
            ["POST /api/v1/admin/orders/{id}/check-in"] = new("办理入住", "为订单分配房间或床位，并将订单推进为已入住状态。", "adminCheckInOrder"),
            ["POST /api/v1/admin/orders/{id}/check-out"] = new("办理离店", "释放订单占用的房间或床位，并将订单推进为已完成状态。", "adminCheckOutOrder"),
            ["GET /api/v1/admin/refunds"] = new("查询退款记录", "按订单、门店和处理状态查询管理后台可访问范围内的退款申请。", "adminListRefunds"),
            ["POST /api/v1/admin/refunds/{id}/review"] = new("审核退款申请", "批准或驳回退款申请，并更新订单和退款记录状态。", "adminReviewRefund"),
            ["GET /api/v1/admin/roles"] = new("查询角色列表", "返回系统角色及各角色关联的管理员数量。", "adminListRoles"),
            ["GET /api/v1/admin/users"] = new("查询管理员", "查询管理员账号、所属门店、启用状态和角色。", "adminListUsers"),
            ["POST /api/v1/admin/users"] = new("创建管理员", "创建后台管理员账号并配置角色及门店数据范围。", "adminCreateUser"),
            ["PUT /api/v1/admin/users/{id}"] = new("更新管理员", "更新管理员资料、密码、角色、所属门店和启用状态。", "adminUpdateUser"),
            ["GET /api/v1/admin/members"] = new("查询会员", "按关键词分页查询微信小程序会员，返回余额、佣金和两级推荐人数。", "adminListMembers"),
            ["GET /api/v1/admin/stats/trend"] = new("查询经营趋势", "按日期范围统计订单数、已支付订单数、销售额和退款额，范围最长 90 天。", "adminGetTrend"),
            ["GET /api/v1/admin/stats/store-rank"] = new("查询门店排行", "按订单数和销售额返回门店经营排行，范围最长 90 天。", "adminGetStoreRank"),
            ["GET /api/v1/admin/audit-logs"] = new("查询审计日志", "查询管理操作审计记录，可按关键词过滤，最多返回最近 200 条。", "adminListAuditLogs")
        };

    private static readonly IReadOnlyDictionary<string, Type> ResponseTypes =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["GET /api/v1/health"] = typeof(HealthResponse),
            ["POST /api/v1/auth/login"] = typeof(TokenResponse),
            ["POST /api/v1/auth/miniapp-login"] = typeof(TokenResponse),
            ["GET /api/v1/map/config"] = typeof(MapConfigResponse),
            ["GET /api/v1/stores"] = typeof(List<StoreListItem>),
            ["GET /api/v1/stores/nearby"] = typeof(List<StoreListItem>),
            ["GET /api/v1/stores/{id}"] = typeof(StoreDetail),
            ["POST /api/v1/stores/{id}/geo/geocode"] = typeof(StoreListItem),
            ["POST /api/v1/stores/{id}/geo/reverse"] = typeof(StoreListItem),
            ["GET /api/v1/admin/me"] = typeof(AdminIdentityResponse),
            ["GET /api/v1/admin/metrics"] = typeof(AdminMetrics),
            ["GET /api/v1/admin/stores"] = typeof(List<StoreListItem>),
            ["GET /api/v1/admin/stores/{id}"] = typeof(AdminStoreDetail),
            ["POST /api/v1/admin/stores"] = typeof(StoreListItem),
            ["PUT /api/v1/admin/stores/{id}"] = typeof(StoreListItem),
            ["GET /api/v1/room-types"] = typeof(List<RoomTypeSummary>),
            ["GET /api/v1/room-types/{id}/availability"] = typeof(List<AvailabilityItem>),
            ["GET /api/v1/room-types/{id}/reviews"] = typeof(RoomReviewResponse),
            ["POST /api/v1/orders/preview"] = typeof(OrderQuote),
            ["POST /api/v1/orders"] = typeof(OrderListItem),
            ["GET /api/v1/orders/me"] = typeof(List<OrderListItem>),
            ["GET /api/v1/orders/{id}"] = typeof(OrderListItem),
            ["POST /api/v1/orders/{id}/cancel"] = typeof(OrderListItem),
            ["POST /api/v1/orders/{id}/mock-pay"] = typeof(OrderListItem),
            ["GET /api/v1/orders/{id}/review"] = typeof(ReviewItem),
            ["POST /api/v1/orders/{id}/review"] = typeof(ReviewItem),
            ["POST /api/v1/orders/{id}/refunds"] = typeof(RefundCreatedResponse),
            ["GET /api/v1/orders/{id}/refunds"] = typeof(List<RefundItem>),
            ["GET /api/v1/admin/room-types"] = typeof(List<AdminRoomTypeItem>),
            ["POST /api/v1/admin/room-types"] = typeof(AdminRoomTypeItem),
            ["PUT /api/v1/admin/room-types/{id}"] = typeof(AdminRoomTypeItem),
            ["DELETE /api/v1/admin/room-types/{id}"] = typeof(void),
            ["POST /api/v1/admin/room-types/{id}/review"] = typeof(AdminRoomTypeItem),
            ["POST /api/v1/admin/room-types/{id}/publish"] = typeof(AdminRoomTypeItem),
            ["GET /api/v1/admin/room-types/{roomTypeId}/units"] = typeof(List<RoomUnitItem>),
            ["POST /api/v1/admin/room-types/{roomTypeId}/units"] = typeof(RoomUnitItem),
            ["PUT /api/v1/admin/room-units/{id}"] = typeof(RoomUnitItem),
            ["GET /api/v1/admin/prices"] = typeof(List<PriceCalendarItem>),
            ["PUT /api/v1/admin/prices/bulk"] = typeof(UpdatedCountResponse),
            ["GET /api/v1/admin/inventory"] = typeof(List<InventorySummaryItem>),
            ["POST /api/v1/admin/inventory/adjust"] = typeof(UpdatedCountResponse),
            ["GET /api/v1/admin/orders"] = typeof(List<OrderListItem>),
            ["GET /api/v1/admin/orders/{id}"] = typeof(AdminOrderDetail),
            ["GET /api/v1/admin/refunds"] = typeof(List<AdminRefundItem>),
            ["POST /api/v1/admin/refunds/{id}/review"] = typeof(AdminRefundItem),
            ["GET /api/v1/admin/roles"] = typeof(List<RoleItem>),
            ["GET /api/v1/admin/users"] = typeof(List<AdminUserItem>),
            ["POST /api/v1/admin/users"] = typeof(AdminUserItem),
            ["PUT /api/v1/admin/users/{id}"] = typeof(AdminUserItem),
            ["GET /api/v1/admin/members"] = typeof(AdminMemberPage),
            ["GET /api/v1/admin/stats/trend"] = typeof(List<DashboardTrendItem>),
            ["GET /api/v1/admin/stats/store-rank"] = typeof(List<DashboardStoreRankItem>),
            ["GET /api/v1/admin/audit-logs"] = typeof(List<AuditLog>)
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? "GET";
        // ApiExplorer 会在参数占位符中保留 :guid 等约束，而 OpenAPI 路径会移除约束；
        // 统一成 OpenAPI 形式后，字典中的摘要可以覆盖所有动态资源路由。
        var path = "/" + (context.ApiDescription.RelativePath ?? string.Empty)
            .Trim('/')
            .Replace(":guid", string.Empty, StringComparison.OrdinalIgnoreCase);
        var key = $"{method} {path}";
        if (Descriptions.TryGetValue(key, out var description))
        {
            operation.Summary ??= description.Summary;
            operation.Description ??= description.Description;
            operation.OperationId ??= description.OperationId;
        }
        else
        {
            operation.Summary ??= $"{method} {path}";
            operation.OperationId ??= $"{method.ToLowerInvariant()}_{path.Trim('/').Replace('/', '_').Replace('{', '_').Replace('}', '_')}";
        }

        var requiresAuthorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();
        if (requiresAuthorization)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    }] = Array.Empty<string>()
                }
            ];
        }

        AddResponse(operation, "400", "请求参数错误或业务校验失败");
        if (requiresAuthorization)
        {
            AddResponse(operation, "401", "缺少或无效的 JWT");
            AddResponse(operation, "403", "当前账号无权访问该资源");
        }
        if (path.Contains('{', StringComparison.Ordinal))
        {
            AddResponse(operation, "404", "请求的资源不存在");
        }
        AddResponse(operation, "500", "服务器内部错误，响应中包含 traceId 便于排查");

        if (ResponseTypes.TryGetValue(key, out var responseType))
        {
            var schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository);
            foreach (var statusCode in new[] { "200", "201" })
            {
                if (operation.Responses.TryGetValue(statusCode, out var response))
                {
                    response.Content["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    };
                }
            }
        }
    }

    private static void AddResponse(OpenApiOperation operation, string statusCode, string description)
    {
        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses.Add(statusCode, new OpenApiResponse { Description = description });
        }
    }

    private sealed record OperationDescription(string Summary, string Description, string OperationId);
}
