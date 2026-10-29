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
            ["POST /api/v1/auth/miniapp-login"] = new("微信小程序登录", "创建或更新小程序用户，并返回用于用户端接口的 JWT。开发环境支持 DevOpenId；生产环境使用微信 code2Session。", "miniappLogin"),
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
            ["POST /api/v1/orders/{id}/pay"] = new("创建微信支付订单", "为当前用户的待支付订单创建微信小程序 JSAPI 支付参数。", "createWechatPayment"),
            ["POST /api/v1/orders/{id}/mock-pay"] = new("模拟支付订单", "开发环境模拟支付成功，更新订单支付状态并写入支付记录。", "mockPayOrder"),
            ["POST /api/v1/payments/wechat/notify"] = new("接收微信支付回调", "校验微信支付回调签名并解密通知，成功后幂等更新订单支付状态。", "wechatPaymentNotify"),
            ["GET /api/v1/orders/{id}/review"] = new("查询订单评价", "查询当前用户指定订单已提交的评价。", "getOrderReview"),
            ["POST /api/v1/orders/{id}/review"] = new("提交订单评价", "仅允许已完成订单提交一次评价，同时保存房型评分、门店评分、文字和图片。", "createOrderReview"),
            ["POST /api/v1/orders/{id}/refunds"] = new("申请订单退款", "提交已支付订单的退款申请，申请进入待审核状态。", "createOrderRefund"),
            ["GET /api/v1/orders/{id}/refunds"] = new("查询订单退款记录", "查询当前用户指定订单的退款申请和处理结果。", "listOrderRefunds"),
            ["GET /api/v1/admin/me"] = new("获取当前管理员", "返回管理员身份、角色和门店数据权限范围。", "getAdminMe"),
            ["GET /api/v1/admin/metrics"] = new("获取管理后台指标", "返回门店、订单、今日销售和待审核退款等运营指标。", "getAdminMetrics"),
            ["GET /api/v1/admin/notifications"] = new("获取后台消息提醒", "按当前管理员角色和门店数据范围返回待确认订单、退款、门店、房间、评价和提现审核提醒。", "getAdminNotifications"),
            ["GET /api/v1/admin/integrations"] = new("获取外部服务配置状态", "返回微信支付和高德地图的脱敏配置状态，不返回任何密钥内容。", "getIntegrationStatus"),
            ["POST /api/v1/admin/integrations/{provider}/test"] = new("测试外部服务连接", "测试高德地理编码或微信支付商户 API 连接。", "testIntegration"),
            ["GET /api/v1/admin/stores"] = new("管理后台查询门店", "返回管理后台可访问范围内的门店列表。", "adminListStores"),
            ["POST /api/v1/admin/stores"] = new("创建门店", "创建门店基础资料，可选填经纬度。需要总部管理员或运营角色。", "adminCreateStore"),
            ["GET /api/v1/admin/stores/{id}"] = new("获取门店管理详情", "返回门店完整资料、坐标和房型数量。", "adminGetStore"),
            ["PUT /api/v1/admin/stores/{id}"] = new("更新门店", "更新门店名称、地址、联系方式、营业状态和排序等资料。", "adminUpdateStore"),
            ["POST /api/v1/admin/geo/geocode"] = new("地图选点地址解析", "调用高德地理编码把地址转换为经纬度，供后台地图选点使用，不修改门店资料。", "adminGeocodeAddress"),
            ["POST /api/v1/admin/geo/reverse"] = new("地图选点逆地理解析", "调用高德逆地理编码把经纬度转换为标准地址，供后台地图选点使用，不修改门店资料。", "adminReverseGeocode"),
            ["GET /api/v1/admin/regions"] = new("查询地区列表", "查询中国省、市、区县三级地区种子数据，可按层级、父级和关键词筛选。", "adminListRegions"),
            ["GET /api/v1/admin/regions/tree"] = new("查询地区树", "返回中国省、市、区县三级地区树，供门店地区级联选择器使用。", "adminGetRegionTree"),
            ["GET /api/v1/admin/regions/{code}"] = new("获取地区详情", "根据行政区划代码返回省、市或区县地区详情。", "adminGetRegion"),
            ["POST /api/v1/admin/regions"] = new("创建地区", "创建省、市或区县地区，校验行政区划代码唯一性和父级层级。", "adminCreateRegion"),
            ["PUT /api/v1/admin/regions/{code}"] = new("更新地区", "更新地区名称、上级、显示状态和排序；行政区划代码不可修改。", "adminUpdateRegion"),
            ["DELETE /api/v1/admin/regions/{code}"] = new("删除地区", "删除没有下级地区的省、市或区县。", "adminDeleteRegion"),
            ["GET /api/v1/admin/reviews"] = new("查询评价审核", "按状态、关键词和门店查询住客评价及审核状态。", "adminListReviews"),
            ["PUT /api/v1/admin/reviews/{id}"] = new("审核或编辑评价", "更新评价内容、评分、上架状态和审核备注。", "adminUpdateReview"),
            ["DELETE /api/v1/admin/reviews/{id}"] = new("删除评价", "删除指定住客评价及其审核记录。", "adminDeleteReview"),
            ["GET /api/v1/admin/store-staff"] = new("查询门店员工", "返回当前管理员可访问门店下的员工账号、权限菜单和状态。", "adminListStoreStaff"),
            ["POST /api/v1/admin/store-staff"] = new("创建门店员工", "创建门店员工账号并保存门店归属、性别和权限菜单。", "adminCreateStoreStaff"),
            ["PUT /api/v1/admin/store-staff/{id}"] = new("更新门店员工", "更新员工账号、手机号、门店归属、状态和权限菜单。", "adminUpdateStoreStaff"),
            ["DELETE /api/v1/admin/store-staff/{id}"] = new("删除门店员工", "删除指定门店员工账号，不能删除当前登录账号。", "adminDeleteStoreStaff"),
            ["GET /api/v1/admin/permission-menus"] = new("查询权限菜单", "返回用户端民宿管理入口的标题、路径、图标、权重和启用状态。", "adminListPermissionMenus"),
            ["POST /api/v1/admin/permission-menus"] = new("创建权限菜单", "新增一个用户端民宿管理入口。", "adminCreatePermissionMenu"),
            ["PUT /api/v1/admin/permission-menus/{key}"] = new("更新权限菜单", "更新权限菜单标题、图标、页面路径、权重和状态。", "adminUpdatePermissionMenu"),
            ["POST /api/v1/admin/permission-menus/sync"] = new("同步权限菜单", "根据用户端 merchantEntries 批量同步民宿管理入口，保留服务端图标和运营状态。", "adminSyncPermissionMenus"),
            ["DELETE /api/v1/admin/permission-menus/{key}"] = new("删除权限菜单", "删除指定用户端民宿管理入口。", "adminDeletePermissionMenu"),
            ["GET /api/v1/admin/managed-items/{resourceType}"] = new("查询基础配置", "查询指定配置资源类型的持久化记录。", "adminListManagedItems"),
            ["POST /api/v1/admin/managed-items/{resourceType}"] = new("创建基础配置", "创建指定资源类型的配置记录，数据以 JSON 对象保存。", "adminCreateManagedItem"),
            ["PUT /api/v1/admin/managed-items/{resourceType}/{id}"] = new("更新基础配置", "更新指定配置记录并保留未提交的字段。", "adminUpdateManagedItem"),
            ["DELETE /api/v1/admin/managed-items/{resourceType}/{id}"] = new("删除基础配置", "删除指定资源类型的配置记录。", "adminDeleteManagedItem"),
            ["GET /api/v1/admin/subscription-templates"] = new("查询订阅消息模板", "查询微信小程序订阅消息模板及其参数映射，可按标识、名称或模板 ID 搜索。", "adminListSubscriptionTemplates"),
            ["GET /api/v1/admin/subscription-templates/{id}"] = new("获取订阅消息模板", "返回指定小程序订阅消息模板的字段映射和启用状态。", "adminGetSubscriptionTemplate"),
            ["POST /api/v1/admin/subscription-templates"] = new("创建订阅消息模板", "创建小程序订阅消息模板，配置微信模板 ID 和业务字段映射。", "adminCreateSubscriptionTemplate"),
            ["PUT /api/v1/admin/subscription-templates/{id}"] = new("更新订阅消息模板", "更新订阅消息模板名称、模板 ID、字段映射和启用状态。", "adminUpdateSubscriptionTemplate"),
            ["DELETE /api/v1/admin/subscription-templates/{id}"] = new("删除订阅消息模板", "删除后台维护的小程序订阅消息模板，已有消息日志会保留。", "adminDeleteSubscriptionTemplate"),
            ["GET /api/v1/admin/message-logs"] = new("查询订阅消息日志", "分页查询小程序订阅消息发送记录，可按模板、会员、openid、状态和结果搜索。", "adminListMessageLogs"),
            ["GET /api/v1/admin/message-logs/{id}"] = new("获取订阅消息日志", "返回指定订阅消息日志的模板数据和微信发送结果。", "adminGetMessageLog"),
            ["POST /api/v1/admin/message-logs"] = new("记录订阅消息日志", "写入一次小程序订阅消息发送记录，供实际微信发送服务回写发送结果。", "adminCreateMessageLog"),
            ["PUT /api/v1/admin/message-logs/{id}"] = new("更新订阅消息日志", "更新订阅消息日志的会员、模板数据、发送状态和微信返回结果。", "adminUpdateMessageLog"),
            ["DELETE /api/v1/admin/message-logs/{id}"] = new("删除订阅消息日志", "删除指定的小程序订阅消息发送记录。", "adminDeleteMessageLog"),
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
            ["POST /api/v1/admin/orders/{id}/verify"] = new("核销订单", "将已支付待核销订单标记为已核销并完成订单，记录核销时间。", "adminVerifyOrder"),
            ["GET /api/v1/verification-qr/{code}"] = new("获取订单核销二维码", "根据支付成功后生成的核销码返回二维码图片。", "getOrderVerificationQr"),
            ["POST /api/v1/admin/orders/{id}/cancel"] = new("管理后台取消订单", "管理员取消订单并释放相应库存。", "adminCancelOrder"),
            ["GET /api/v1/admin/orders"] = new("管理后台查询订单", "按状态和门店筛选订单，最多返回最近 500 条。", "adminListOrders"),
            ["POST /api/v1/admin/orders/{id}/check-in"] = new("办理入住", "为订单分配房间或床位，并将订单推进为已入住状态；入住时同步记录核销状态和时间。", "adminCheckInOrder"),
            ["POST /api/v1/admin/orders/{id}/check-out"] = new("办理离店", "释放订单占用的房间或床位，并将订单推进为已完成状态。", "adminCheckOutOrder"),
            ["GET /api/v1/admin/refunds"] = new("查询退款记录", "按订单、门店和处理状态查询管理后台可访问范围内的退款申请。", "adminListRefunds"),
            ["POST /api/v1/admin/refunds/{id}/review"] = new("审核退款申请", "批准或驳回退款申请，并更新订单和退款记录状态。", "adminReviewRefund"),
            ["GET /api/v1/admin/withdrawals"] = new("查询门店提现", "按状态和门店查询提现申请及审核结果。", "adminListWithdrawals"),
            ["POST /api/v1/admin/withdrawals"] = new("创建门店提现", "创建一条门店提现申请，默认进入待审核状态。", "adminCreateWithdrawal"),
            ["PUT /api/v1/admin/withdrawals/{id}"] = new("更新门店提现", "更新提现门店、收款账户和提现金额。", "adminUpdateWithdrawal"),
            ["POST /api/v1/admin/withdrawals/{id}/review"] = new("审核门店提现", "批准或驳回门店提现申请，驳回时必须填写原因。", "adminReviewWithdrawal"),
            ["DELETE /api/v1/admin/withdrawals/{id}"] = new("删除门店提现", "删除指定门店提现记录。", "adminDeleteWithdrawal"),
            ["GET /api/v1/admin/roles"] = new("查询角色列表", "返回系统角色及各角色关联的管理员数量。", "adminListRoles"),
            ["GET /api/v1/admin/users"] = new("查询管理员", "查询管理员账号、所属门店、启用状态和角色。", "adminListUsers"),
            ["POST /api/v1/admin/users"] = new("创建管理员", "创建后台管理员账号并配置角色及门店数据范围。", "adminCreateUser"),
            ["PUT /api/v1/admin/users/{id}"] = new("更新管理员", "更新管理员资料、密码、角色、所属门店和启用状态。", "adminUpdateUser"),
            ["GET /api/v1/admin/members"] = new("查询会员", "按关键词分页查询微信小程序会员，返回余额、佣金和两级推荐人数。", "adminListMembers"),
            ["GET /api/v1/admin/stats/trend"] = new("查询经营趋势", "按日期范围统计订单数、已支付订单数、销售额和退款额，范围最长 90 天。", "adminGetTrend"),
            ["GET /api/v1/admin/stats/store-rank"] = new("查询门店排行", "按订单数和销售额返回门店经营排行，范围最长 90 天。", "adminGetStoreRank"),
            ["GET /api/v1/admin/stats/income"] = new("查询门店收入", "按门店和日期统计订单实收、退款和可结算金额，范围最长 90 天。", "adminGetIncome"),
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
            ["GET /api/v1/admin/notifications"] = typeof(AdminNotificationCenter),
            ["GET /api/v1/admin/stores"] = typeof(List<StoreListItem>),
            ["GET /api/v1/admin/stores/{id}"] = typeof(AdminStoreDetail),
            ["POST /api/v1/admin/stores"] = typeof(StoreListItem),
            ["PUT /api/v1/admin/stores/{id}"] = typeof(StoreListItem),
            ["POST /api/v1/admin/geo/geocode"] = typeof(GeoLookupResponse),
            ["POST /api/v1/admin/geo/reverse"] = typeof(GeoLookupResponse),
            ["GET /api/v1/admin/regions"] = typeof(List<AdminRegionItem>),
            ["GET /api/v1/admin/regions/tree"] = typeof(List<AdminRegionTreeItem>),
            ["GET /api/v1/admin/regions/{code}"] = typeof(AdminRegionItem),
            ["POST /api/v1/admin/regions"] = typeof(AdminRegionItem),
            ["PUT /api/v1/admin/regions/{code}"] = typeof(AdminRegionItem),
            ["DELETE /api/v1/admin/regions/{code}"] = typeof(void),
            ["GET /api/v1/admin/reviews"] = typeof(List<AdminReviewItem>),
            ["PUT /api/v1/admin/reviews/{id}"] = typeof(AdminReviewItem),
            ["DELETE /api/v1/admin/reviews/{id}"] = typeof(void),
            ["GET /api/v1/admin/store-staff"] = typeof(List<StoreStaffItem>),
            ["POST /api/v1/admin/store-staff"] = typeof(StoreStaffItem),
            ["PUT /api/v1/admin/store-staff/{id}"] = typeof(StoreStaffItem),
            ["DELETE /api/v1/admin/store-staff/{id}"] = typeof(void),
            ["GET /api/v1/admin/permission-menus"] = typeof(List<PermissionMenuItem>),
            ["POST /api/v1/admin/permission-menus"] = typeof(PermissionMenuItem),
            ["PUT /api/v1/admin/permission-menus/{key}"] = typeof(PermissionMenuItem),
            ["POST /api/v1/admin/permission-menus/sync"] = typeof(List<PermissionMenuItem>),
            ["DELETE /api/v1/admin/permission-menus/{key}"] = typeof(void),
            ["GET /api/v1/admin/managed-items/{resourceType}"] = typeof(List<Dictionary<string, object>>),
            ["POST /api/v1/admin/managed-items/{resourceType}"] = typeof(Dictionary<string, object>),
            ["PUT /api/v1/admin/managed-items/{resourceType}/{id}"] = typeof(Dictionary<string, object>),
            ["DELETE /api/v1/admin/managed-items/{resourceType}/{id}"] = typeof(void),
            ["GET /api/v1/admin/subscription-templates"] = typeof(List<SubscriptionTemplateItem>),
            ["GET /api/v1/admin/subscription-templates/{id}"] = typeof(SubscriptionTemplateItem),
            ["POST /api/v1/admin/subscription-templates"] = typeof(SubscriptionTemplateItem),
            ["PUT /api/v1/admin/subscription-templates/{id}"] = typeof(SubscriptionTemplateItem),
            ["GET /api/v1/admin/message-logs"] = typeof(MessageLogPage),
            ["GET /api/v1/admin/message-logs/{id}"] = typeof(MessageLogItem),
            ["POST /api/v1/admin/message-logs"] = typeof(MessageLogItem),
            ["PUT /api/v1/admin/message-logs/{id}"] = typeof(MessageLogItem),
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
            ["GET /api/v1/admin/orders"] = typeof(List<AdminOrderListItem>),
            ["POST /api/v1/admin/orders/{id}/verify"] = typeof(void),
            ["GET /api/v1/admin/orders/{id}"] = typeof(AdminOrderDetail),
            ["GET /api/v1/admin/refunds"] = typeof(List<AdminRefundItem>),
            ["POST /api/v1/admin/refunds/{id}/review"] = typeof(AdminRefundItem),
            ["GET /api/v1/admin/withdrawals"] = typeof(List<AdminWithdrawalItem>),
            ["POST /api/v1/admin/withdrawals"] = typeof(AdminWithdrawalItem),
            ["PUT /api/v1/admin/withdrawals/{id}"] = typeof(AdminWithdrawalItem),
            ["POST /api/v1/admin/withdrawals/{id}/review"] = typeof(AdminWithdrawalItem),
            ["DELETE /api/v1/admin/withdrawals/{id}"] = typeof(void),
            ["GET /api/v1/admin/roles"] = typeof(List<RoleItem>),
            ["GET /api/v1/admin/users"] = typeof(List<AdminUserItem>),
            ["POST /api/v1/admin/users"] = typeof(AdminUserItem),
            ["PUT /api/v1/admin/users/{id}"] = typeof(AdminUserItem),
            ["GET /api/v1/admin/members"] = typeof(AdminMemberPage),
            ["GET /api/v1/admin/stats/trend"] = typeof(List<DashboardTrendItem>),
            ["GET /api/v1/admin/stats/store-rank"] = typeof(List<DashboardStoreRankItem>),
            ["GET /api/v1/admin/stats/income"] = typeof(List<AdminIncomeItem>),
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
