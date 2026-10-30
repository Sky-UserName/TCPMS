# 后端接口契约摘要

本文件把当前后端路由和请求模型按业务整理，方便前端快速定位。完整参数、响应 schema、鉴权按钮和可执行示例请以运行中的 Swagger 为准：

- Swagger UI：http://localhost:5180/swagger
- OpenAPI JSON：http://localhost:5180/swagger/v1/swagger.json

服务前缀：/api/v1。除健康检查、认证、地图配置、公开门店/房型接口外，接口均需要 JWT。Swagger 已为主要成功响应生成 schema，字段约束和业务规则以本文件补充说明。

## 1. 通用约定

### 鉴权

请求头：

~~~http
Authorization: Bearer <JWT>
~~~

- admin 签发给管理后台，包含 user_type=admin。
- miniapp 签发给微信小程序，包含 user_type=miniapp。
- 管理后台接口还会按角色和 store_id 做数据范围校验。

### 金额和日期

- 所有金额字段以 CNY 分为单位，17600 表示 176.00 元。
- DateTime 入参建议使用 YYYY-MM-DD；时间范围采用入住日包含、离店日不包含。
- 订单单次最多 90 晚；查询价格和库存的后台范围最多 31 或 180 天，具体以 Swagger 校验为准。

### 错误

常见 JSON 错误：

~~~json
{
  "code": "validation_error",
  "message": "错误说明",
  "traceId": "请求追踪号"
}
~~~

常见状态码：401 未登录、403 无权限、404 资源不存在、409 状态/库存冲突、400 或 422 参数校验失败、503 地图服务未配置或不可用。

### 静态资源

- `GET /brand/<文件名>`：品牌 logo、登录页图片、首页大图，源文件在 `wwwroot/brand/`，随后端发布包一起部署，缓存 7 天。
- `GET /uploads/<业务目录>/<文件名>`：商家上传的门店、房型、评价、头像图片，源文件在 `wwwroot/uploads/`，部署升级时必须保留该目录。
- 小程序端在 `common/brand-assets.js` 里按 `API 根地址 + /brand/...` 拼接，加载失败自动回退到包内 `/static/...` 同名图片。

## 2. 认证和公共接口

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| GET | /api/v1/health | 无 | 返回服务、环境、数据库提供程序和 UTC 时间 |
| POST | /api/v1/auth/login | 无 | 后台账号登录，返回 token 与 `mustChangePassword`；失败 5 次锁定 10 分钟（429 `login_locked`） |
| POST | /api/v1/auth/change-password | admin | 管理员自助改密，Body 为 `{ currentPassword, newPassword }`，成功换发新 token |
| POST | /api/v1/auth/miniapp-login | 无 | 小程序登录，生产环境传微信 login code，Body 为 MiniappLoginRequest |
| GET | /api/v1/auth/me | miniapp | 查询当前微信用户资料 |
| POST | /api/v1/auth/phone | miniapp | 使用微信手机号授权凭证绑定手机号 |
| POST | /api/v1/auth/avatar | miniapp | 上传当前用户头像并保存到本地文件目录 |
| POST | /api/v1/reviews/images | miniapp | 上传评价图片（multipart `file`），返回 `{ url }`，用于评价提交前替换本机临时路径 |
| GET | /api/v1/map/config | 无 | 返回高德小程序 Key 是否启用和默认附近半径 |

登录请求模型：

- LoginRequest：username、password。
- MiniappLoginRequest：code、devOpenId、nickname、avatarUrl、phoneCode。
- MiniappPhoneRequest：phoneCode。
- 成功响应 TokenResponse：token、expiresAt、userType、displayName、profile。
- profile：id、nickname、phoneNumber、avatarUrl。

开发环境可以传 devOpenId。生产环境由后端调用微信 code2Session 换取 openid，不能把开发 openId 当真实身份方案。

开发种子管理员账号（密码均为 `Admin@123456`）：

| 用户名 | 角色 | 数据范围 |
| --- | --- | --- |
| admin | system_admin | 全部门店 |
| ops_zhang | operations | 全部门店 |
| finance_li | finance | 全部门店 |
| shanghai_admin | store_admin | 上海人民广场店 |
| hangzhou_staff | store_staff | 杭州西湖店 |

这些账号只用于本地联调，部署到共享环境前必须修改或禁用。

## 3. 门店和地图

| 方法 | 路径 | 鉴权 | 查询或 Body |
| --- | --- | --- | --- |
| GET | /api/v1/stores | 无 | keyword、status |
| GET | /api/v1/stores/nearby | 无 | latitude、longitude、radiusKm |
| GET | /api/v1/stores/{id} | 无 | 门店 GUID |
| POST | /api/v1/stores/{id}/geo/geocode | admin | GeocodeRequest |
| POST | /api/v1/stores/{id}/geo/reverse | admin | ReverseGeocodeRequest |
| POST | /api/v1/admin/geo/geocode | admin | GeocodeRequest（只解析，不写库） |
| POST | /api/v1/admin/geo/reverse | admin | ReverseGeocodeRequest（只解析，不写库） |
| GET | /api/v1/admin/stores | admin | 后台门店列表 |
| GET | /api/v1/admin/stores/{id} | admin | 后台门店详情 |
| POST | /api/v1/admin/stores | admin | StoreRequest |
| PUT | /api/v1/admin/stores/{id} | admin | StoreRequest |

核心响应：

- StoreListItem：id、name、address、phone、status、longitude、latitude、distanceKm、hasMapLocation、featuredRoomId、featuredRoomName、startingPriceCents、tags；`/stores` 与 `/stores/nearby` 额外返回 rooms（在售房型摘要，每店最多 6 条，首页直接用房型卡片渲染），后台列表不返回。
- StoreDetail：store 和已上架 roomTypes。
- StoreRequest：name、code、description、province、city、district、address、phone、longitude、latitude、status、sortOrder。
- GeocodeRequest：address、city。
- ReverseGeocodeRequest：longitude、latitude。

附近查询只对有经纬度且营业中的门店计算距离；radiusKm 必须大于 0 且不超过 500 公里。

## 4. 用户端房型和可用性

| 方法 | 路径 | 鉴权 | 查询或 Body |
| --- | --- | --- | --- |
| GET | /api/v1/room-types | 无 | 可选 storeId |
| GET | /api/v1/room-types/{id}/availability | 无 | checkIn、checkOut |
| GET | /api/v1/room-types/{id}/reviews | 无 | 可选 limit，返回房型/门店评分汇总和公开评价 |

RoomTypeSummary 字段：id、storeId、name、kind、bedCount、gender、maxGuests、basePriceCents、description、isPublished。

AvailabilityItem 字段：date、totalQuantity、lockedQuantity、soldQuantity、availableQuantity、priceCents。

房型列表只返回已上架房型；可用性接口按房型 ID 查询库存，日期不能早于今天，离店日期必须晚于入住日期，人数不能超过 maxGuests 乘以 quantity。

## 5. 用户端订单和退款

订单组需要 miniapp JWT：

| 方法 | 路径 | Body 或说明 |
| --- | --- | --- |
| POST | /api/v1/orders/preview | OrderQuoteRequest，返回 OrderQuote |
| POST | /api/v1/orders/ | CreateOrderRequest，创建待支付订单并锁定库存 |
| GET | /api/v1/orders/me | 当前用户订单列表 |
| GET | /api/v1/orders/{id} | 当前用户订单详情 |
| POST | /api/v1/orders/{id}/cancel | 仅待支付订单可取消 |
| POST | /api/v1/orders/{id}/pay | 创建微信小程序 JSAPI 支付参数，可带 `{ code }`（uni.login 凭证）换成真实 openid |
| POST | /api/v1/orders/{id}/mock-pay | 开发环境模拟支付 |
| POST | /api/v1/orders/{id}/refunds | CreateRefundRequest，创建退款申请 |
| GET | /api/v1/orders/{id}/refunds | 当前用户退款记录 |

请求模型：

- OrderQuoteRequest：storeId、roomTypeId、checkIn、checkOut、quantity、guestCount。
- CreateOrderRequest：以上字段加 guestSnapshotJson。
- CreateRefundRequest：amountCents、reason。

发起支付（`POST /orders/{id}/pay`）可以带请求体 `{ "code": "<uni.login 的 code>" }`：服务端会用它换取当前微信账号的真实 openid 作为 payer，换取失败时回退到登录态里的 openid。开发态固定账号 `dev-user-001` 不是有效 openid，微信会直接以 `PARAM_ERROR 无效的openid` 拒单；前端因此每次发起支付前都会 `uni.login` 拿新鲜 code。

支付错误码：

| HTTP | code | 含义 |
| --- | --- | --- |
| 400 | order_not_payable / order_payment_expired / wechat_openid_missing | 订单不可支付、支付超时、登录态缺 openid |
| 502 | wechat_pay_rejected | 微信明确拒单，message 里带微信原因（如「无效的openid（PARAM_ERROR）」） |
| 503 | wechat_pay_not_configured | 商户号、证书、API v3 Key、回调地址未配齐 |
| 503 | wechat_pay_unavailable | 网络层调用微信支付失败 |

微信支付回调：

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| POST | /api/v1/payments/wechat/notify | 微信支付签名 | 校验平台证书签名、解密 API v3 通知并幂等更新订单 |

管理后台应用配置：

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| GET | /api/v1/admin/integrations | admin | 返回微信支付和高德地图脱敏配置状态 |
| POST | /api/v1/admin/integrations/{provider}/test | admin | `provider` 为 `wechat` 或 `amap`，测试外部服务连接 |

OrderQuote 包含 nights、quantity、guestCount、totalAmountCents、dailyItems。OrderListItem 包含 id、orderNumber、storeId、roomTypeId、storeName、roomTypeName、checkIn、checkOut、quantity、totalAmountCents、status、paymentStatus、createdAt、hasReview、guestSnapshotJson。其中 createdAt 统一按 UTC 序列化（带 Z），小程序端再转本地时间展示；guestSnapshotJson 为下单时提交的入住人快照，订单详情页的实名入住信息直接取这个字段。房型摘要（RoomTypeSummary）额外返回 checkInTime / checkOutTime，供订单详情展示房型自己的入住退房时间。

创建订单时逐日增加 lockedQuantity；mock-pay 将锁定库存转为 soldQuantity，并写入 PaymentRecord。已支付订单不能直接 cancel，需走退款申请。

评价接口：

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| GET | /api/v1/orders/{id}/review | miniapp | 查询当前用户对订单的评价 |
| POST | /api/v1/orders/{id}/review | miniapp | 仅已完成订单可提交一次房型评分、门店评分、文字和最多 9 张图片地址 |

CreateReviewRequest 的评分范围为 1-5，文字最多 500 字；重复评价返回 409。

## 6. 后台房型、房间和床位

| 方法 | 路径 | 查询或 Body |
| --- | --- | --- |
| GET | /api/v1/admin/room-types | storeId、includeUnpublished |
| POST | /api/v1/admin/room-types | RoomTypeRequest |
| PUT | /api/v1/admin/room-types/{id} | RoomTypeRequest |
| POST | /api/v1/admin/room-types/{id}/publish | 查询 published=true 或 false |
| GET | /api/v1/admin/room-types/{roomTypeId}/units | 可选 status |
| POST | /api/v1/admin/room-types/{roomTypeId}/units | RoomUnitRequest |
| PUT | /api/v1/admin/room-units/{id} | RoomUnitRequest |

RoomTypeRequest：storeId、name、kind、bedCount、gender、maxGuests、basePriceCents、description、facilitiesJson、imageUrlsJson、isPublished、approvalStatus，另有 roomCount、roomCategory、defaultInventory。

房型数量只有一个口径：`kind=PrivateRoom` 时取 `roomCount`（房间数量），`kind=Bed` 时取 `bedCount`（床铺数量），服务端据此写 `defaultInventory`，忽略请求里与之不一致的 `defaultInventory`。新建或修改房型时会把未来 180 天的 `InventoryDailies.TotalQuantity` 同步成这个数量（已锁定/已售的日期按占用量兜底，不会压到低于占用），所以「房间管理」里的数量和「价格设置」日历里的总数天然一致。

例外：在价格设置里手动改过单价的日期（`PriceCalendars.Source` 不是 `Seed` / `Weekend`）属于日期例外，保存房型数量时保留这一天的 `TotalQuantity`，不会被刷回统一数量；日历里的「总数/剩余」也按这一天的记录展示。手动调整库存（`POST /admin/inventory/adjust`）同样会给当天补一条 `Source=Manual` 的价格记录作为标记，保证调过的日期不会被下一次保存房型覆盖。种子数据自动生成的 `Seed` / `Weekend` 价格不算手动设置。

`kind` 也可以由 `roomCategory` 推断：名称含「床位/多人间/宿舍/胶囊/铺位/青旅」判为 Bed，含「独立/单间/标间/大床/双床/套房/家庭房/房间」判为 PrivateRoom（`/admin/room-options` 返回的 categories[].kind 也按这个规则校准）。

RoomUnitRequest：roomTypeId、code、kind、gender、status、note。房间或床位状态通常使用 Available、Occupied 等值，实际枚举以代码和 Swagger 为准。

## 7. 后台价格和库存

| 方法 | 路径 | 查询或 Body |
| --- | --- | --- |
| GET | /api/v1/admin/prices | roomTypeId、from、to |
| PUT | /api/v1/admin/prices/bulk | BulkPriceRequest |
| GET | /api/v1/admin/inventory | roomTypeId、from、to |
| POST | /api/v1/admin/inventory/adjust | InventoryAdjustRequest |

BulkPriceRequest：roomTypeId、from、to、priceCents、source。InventoryAdjustRequest：roomTypeId、from、to、delta、reason。

InventorySummaryItem：id、storeId、roomTypeId、date、totalQuantity、lockedQuantity、soldQuantity、availableQuantity、updatedAt。

库存人工调整必须传 reason；调整不能让总量、锁定量或已售量产生非法负数。调整成功后会给涉及日期补一条 `Source=Manual` 的 `PriceCalendars` 记录（价格取当天已有价格，没有则取房型默认价），用于标记「这一天在价格设置里单独设置过」：

- 管理端价格日历：有 Manual 记录的日期，总数/剩余按该日 `InventoryDailies` 展示；没有 Manual 记录的日期，总数回落到房型数量（历史脏库存不会撑大格子）。
- 保存房型/修改房型数量：`InventoryPlanner` 只同步没有 Manual 记录的日期，日期例外原样保留。
- `PUT /admin/prices/bulk` 只改单价时也会补建缺少的库存行（总数取房型数量），避免价格日历出现只有价格、没有总数的空格。

## 8. 后台订单和入住

| 方法 | 路径 | 查询或 Body |
| --- | --- | --- |
| GET | /api/v1/admin/orders | status、storeId |
| GET | /api/v1/admin/orders/{id} | 返回 AdminOrderDetail |
| POST | /api/v1/admin/orders/{id}/verify | 无 Body，仅标记已支付且已确认待入住订单的核销状态 |
| POST | /api/v1/admin/orders/{id}/confirm | 无 Body |
| POST | /api/v1/admin/orders/{id}/check-in | 可选 CheckInRequest |
| POST | /api/v1/admin/orders/{id}/check-out | 无 Body |
| POST | /api/v1/admin/orders/{id}/cancel | 可选 query reason |

CheckInRequest：resourceCodes、note。resourceCodes 不传时后端按数量自动分配可用房间/床位；传入时会校验编码和 Available 状态。

状态转换：

1. PaidPendingConfirmation -> ConfirmedPendingCheckIn：confirm。
2. ConfirmedPendingCheckIn -> 已核销：verify，仅写入核销状态和时间。
3. ConfirmedPendingCheckIn -> CheckedIn：check-in，资源标记 Occupied，并同步记录核销状态和时间。
4. CheckedIn -> Completed：check-out，释放已分配资源。
5. 待支付订单可以由后台 cancel，并释放锁定库存。

管理端订单列表使用 AdminOrderListItem，提供用户 ID、订单号、商品总价、成功支付金额、订单状态、支付时间、核销状态和时间、退款完成时间、预约起止时间、门店 ID、备注、人数和创建时间。金额单位为分。

AdminOrderDetail 包含订单字段、实际支付金额、支付/核销/退款时间、备注、支付记录和退款记录。PaymentItem 金额为分；RefundItem 包含退款单号、金额、状态、审核意见和第三方退款号。

## 9. 后台退款

| 方法 | 路径 | 查询或 Body |
| --- | --- | --- |
| GET | /api/v1/admin/refunds | status、storeId |
| POST | /api/v1/admin/refunds/{id}/review | ReviewRefundRequest |

ReviewRefundRequest：approved、comment。批准后退款记录写入审核信息，订单恢复或进入已退款状态，已售库存按订单晚数释放；生产环境还需要接入微信退款 API 和回调幂等。

## 10. 后台账号、统计和审计

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | /api/v1/admin/me | 当前管理员、角色和门店范围 |
| GET | /api/v1/admin/roles | 角色和用户数 |
| GET | /api/v1/admin/users | 管理员列表 |
| POST | /api/v1/admin/users | AdminUserRequest，新建管理员 |
| PUT | /api/v1/admin/users/{id} | AdminUserRequest，更新管理员 |
| GET | /api/v1/admin/metrics | 门店、订单、销售和退款指标 |
| GET | /api/v1/admin/stats/income | 按门店和日期统计订单实收、退款和可结算金额 |
| GET | /api/v1/admin/notifications | 按角色和门店范围返回订单、退款、门店、房间、评价和提现审核提醒 |
| GET | /api/v1/admin/stats/trend | from、to、storeId，最多 90 天 |
| GET | /api/v1/admin/stats/store-rank | from、to，最多 90 天 |
| GET | /api/v1/admin/audit-logs | keyword，最多返回最近 200 条 |

AdminUserRequest：username、displayName、password、phoneNumber、storeId、isEnabled、roleIds。

角色代码和权限由后端 RoleCodes 控制。总部角色可跨门店；门店角色只能访问 JWT 中 store_id 对应门店；账号新增和角色绑定只允许系统管理员。

后台消息提醒响应 `AdminNotificationCenter` 包含 `pendingCount`、各类待处理数量、`generatedAt` 和 `items`。通知类型包括：

- `order`：已支付待确认或已确认待入住订单。
- `refund`：待审核退款申请。
- `store`：待审核门店资料。
- `room`：待审核房间资料。
- `review`：待审核住客评价。
- `withdrawal`：待审核门店提现。

## 11. 后台扩展管理

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET/PUT/DELETE | /api/v1/admin/reviews、/api/v1/admin/reviews/{id} | 评价查询、编辑（星级/内容/图片，门店与用户房间只读）、上架/隐藏和删除 |
| POST | /api/v1/admin/uploads/review-image | 上传评价图片，返回 `{ url }`，供管理端编辑评价时替换图片 |
| GET/POST/PUT/DELETE | /api/v1/admin/store-staff、/api/v1/admin/store-staff/{id} | 门店员工账号、门店归属和权限菜单；`username` 取自会员昵称，`displayName` 为员工名称，`wxUserId` 关联所选会员 |
| GET/POST/PUT/DELETE | /api/v1/admin/permission-menus、/api/v1/admin/permission-menus/{key} | 用户端民宿管理入口维护 |
| POST | /api/v1/admin/permission-menus/sync | 从用户端 merchantEntries 同步权限菜单 |
| GET/POST/PUT/DELETE | /api/v1/admin/managed-items/{resourceType} | 装修、等级、设施、标签等配置型页面的持久化 JSON 记录 |
| GET/POST/PUT/DELETE | /api/v1/admin/withdrawals、/api/v1/admin/withdrawals/{id} | 门店提现申请和记录 |
| POST | /api/v1/admin/withdrawals/{id}/review | 批准或驳回提现 |

评价、提现审核完成后，`/api/v1/admin/notifications` 会返回最新待办数量；管理后台右上角消息中心和控制台待办共用该接口。

## 12. 小程序商家中心

商家身份不单独建账号表：后端用 `Store.OwnerUsername`（微信 OpenId）或 `Store.OwnerPhone`（已绑定手机号）匹配当前登录的 miniapp 用户。未匹配到门店时 `/merchant/profile` 返回 `enabled=false`，其余商家接口返回 403 `merchant_not_bound`。

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | /api/v1/merchant/profile | 门店名称、房型数、待处理订单数、入驻状态 |
| GET | /api/v1/merchant/rooms | 当前门店房型、价格、近 14 天可售库存、上下架状态 |
| GET | /api/v1/merchant/rooms/{id} | 房型详情，用于发布页回填；`images` 返回全部图片，`image` 为第一张（封面） |
| POST | /api/v1/merchant/rooms | 新建房型，同时生成 180 天库存和房间资源；`imageUrls` 传图片数组（最多 9 张），兼容旧字段 `imageUrl` |
| PUT | /api/v1/merchant/rooms/{id} | 更新房型、库存和上下架状态；传 `imageUrls` 时整组覆盖（可传空数组清空） |
| POST | /api/v1/merchant/rooms/upload | multipart `file`，上传房型图片，返回 `{ url, fileName, size }`，用于发布页多图上传 |
| POST | /api/v1/merchant/rooms/{id}/publish?published=true\|false | 上架或下架 |
| GET | /api/v1/merchant/orders | 本店订单，支持 status=pending/stay/done/cancel |
| POST | /api/v1/merchant/orders/{id}/confirm | 已支付待确认 → 已确认待入住 |
| POST | /api/v1/merchant/orders/{id}/check-in | 办理入住并占用房间资源 |
| POST | /api/v1/merchant/orders/{id}/check-out | 办理退房，释放资源并结算库存 |
| GET | /api/v1/merchant/verify-records | 本店已核销订单，返回 `orderNumber`、`roomName`、`storeName`、`guest`、`verifiedAt`、`time` |
| POST | /api/v1/merchant/verify | body { code }，按核销码或订单号核销，订单置为已完成并返回核销详情 |
| GET | /api/v1/merchant/finance | 今日/7 天/30 天/全部统计和钱包余额 |
| POST | /api/v1/merchant/withdrawals | body { amountCents, account }，提交提现申请 |
| GET | /api/v1/merchant/reviews | 本店评价（用户、房间、星级、内容、图片、审核状态），支持 status=pending/published/hidden |
| POST | /api/v1/merchant/reviews/{id}/moderate | body { status: published\|hidden, comment? }，通过后用户端可见，不通过置为隐藏 |
| GET/POST/PUT | /api/v1/merchant/coupons、/coupons/{id} | 优惠券列表、创建、启停 |
| GET/POST | /api/v1/merchant/staff | 店主和门店员工，新增员工会创建 AdminUser（初始密码需首次登录修改） |
| GET | /api/v1/merchant/staff/options | 添加员工弹层的可选项：当前账号名下门店（默认选中当前民宿）和启用中的权限菜单 |
| GET | /api/v1/merchant/members/search?keyword=1380 | 按手机号或昵称片段搜索会员，关键词少于 4 位返回空数组，用于添加员工时选人回填 |
| GET/PUT | /api/v1/merchant/settings | 民宿名称、电话、入离时间、退订规则 |
| GET/PUT | /api/v1/merchant/message-settings | 订阅消息偏好 |

商家端轻量配置（优惠券、评价回复、设置、订阅偏好）存放在 `AdminManagedItems` 的 JSON 行中，键名带门店 ID，避免为原型阶段新增表结构。

`POST /merchant/staff` 请求体：`{ name, phone, gender, storeId, permissions, memberId }`。`name`、`phone` 必填，手机号需为 11 位；`gender` 支持男/女/未知（兼容微信的 Male/Female）；`storeId` 省略时默认当前民宿，且必须是当前账号名下门店；`permissions` 只保留权限菜单中启用中的 key；`memberId` 为搜索选中的会员 ID，会写入 `AdminUser.WxUserId`。同一门店下手机号或会员重复返回 409。

## 13. 小程序用户内容

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | /api/v1/me/summary | 收藏数、常用入住人数、浏览记录数、未读消息数、已完成订单数、入住天数 |
| GET/POST/DELETE | /api/v1/me/favorites、/favorites/{id} | 收藏列表、新增、取消 |
| GET/PUT | /api/v1/me/guests | 常用入住人，最多 10 位 |
| GET/POST/DELETE | /api/v1/me/recent | 浏览记录，最多 20 条 |
| GET | /api/v1/me/messages | 平台公告 + 最近订单状态消息 |
| POST | /api/v1/me/messages/{id}/read | 标记消息已读 |
| GET | /api/v1/articles、/articles/{id} | 攻略列表与详情 |
| GET | /api/v1/stores/filters | 已上线门店覆盖的城市和区域 |
| GET | /api/v1/pages/{key} | 说明类页面内容：`rule`（服务协议/隐私政策/权限SDK）、`auth-privacy`（授权前隐私要点）、`service-contact`（客服页文案） |

收藏、入住人、浏览记录、已读状态按用户 ID 存放在 `AdminManagedItems` JSON 行中；攻略、平台公告和说明页文案是全局内容配置，首次启动由 SeedData 写入。

`/me/summary` 的 `points` 默认按「已完成订单实付 1 元累计 1 分」计算，运营可通过写入 `AdminManagedItems` 的 `user_points:{userId}` 覆盖；`stayNights` 为已完成订单的入住晚数合计。

`/pages/{key}` 的返回结构支持多种内容形态，页面按需取用：

| 字段 | 说明 |
| --- | --- |
| title / subtitle / kicker / summary / badge / intro | 页面标题、副标题、导语、角标与高亮说明 |
| version / effectiveAt / updatedAt | 版本、生效日期、更新日期文案 |
| serviceHours / supportText / hotline / tags / notices | 客服类页面的服务时段、复制文案、热线、标签与服务说明 |
| tabs[] | 多标签页内容（服务协议/隐私政策/权限SDK），每项含 `key`、`label`、`kicker`、`title`、`updatedAt`、`highlights[]`、`blocks[]`、`permissions[]` |
| points[] | 有序要点（`index`、`title`、`body`） |
| blocks[] | 正文块，`type` 为 `heading` / `paragraph` / `highlights` / `permissions`；`highlights` 可用 `style`（`rows` / `cards`）控制排版 |
| permissions[] | 权限与 SDK 条目（`icon`、`title`、`desc`、`required`） |

未配置的 `key` 返回 404 `page_not_found`，小程序端展示「内容加载失败 + 重试」，不再回退到写死的旧文案。
