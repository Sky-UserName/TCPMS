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

## 2. 认证和公共接口

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| GET | /api/v1/health | 无 | 返回服务、环境、数据库提供程序和 UTC 时间 |
| POST | /api/v1/auth/login | 无 | 后台账号登录，Body 为 LoginRequest |
| POST | /api/v1/auth/miniapp-login | 无 | 小程序开发登录，Body 为 MiniappLoginRequest |
| GET | /api/v1/map/config | 无 | 返回高德小程序 Key 是否启用和默认附近半径 |

登录请求模型：

- LoginRequest：username、password。
- MiniappLoginRequest：code、devOpenId、nickname、avatarUrl。
- 成功响应 TokenResponse：token、expiresAt、userType、displayName。

开发环境可以传 devOpenId。生产环境必须实现微信 code2Session，不能把开发 openId 当真实身份方案。

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
| GET | /api/v1/admin/stores | admin | 后台门店列表 |
| GET | /api/v1/admin/stores/{id} | admin | 后台门店详情 |
| POST | /api/v1/admin/stores | admin | StoreRequest |
| PUT | /api/v1/admin/stores/{id} | admin | StoreRequest |

核心响应：

- StoreListItem：id、name、address、phone、status、longitude、latitude、distanceKm、hasMapLocation。
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
| POST | /api/v1/orders/{id}/mock-pay | 开发环境模拟支付 |
| POST | /api/v1/orders/{id}/refunds | CreateRefundRequest，创建退款申请 |
| GET | /api/v1/orders/{id}/refunds | 当前用户退款记录 |

请求模型：

- OrderQuoteRequest：storeId、roomTypeId、checkIn、checkOut、quantity、guestCount。
- CreateOrderRequest：以上字段加 guestSnapshotJson。
- CreateRefundRequest：amountCents、reason。

OrderQuote 包含 nights、quantity、guestCount、totalAmountCents、dailyItems。OrderListItem 包含 id、orderNumber、storeId、roomTypeId、storeName、roomTypeName、checkIn、checkOut、quantity、totalAmountCents、status、paymentStatus、createdAt、hasReview。

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

RoomTypeRequest：storeId、name、kind、bedCount、gender、maxGuests、basePriceCents、description、facilitiesJson、imageUrlsJson、isPublished、approvalStatus。

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

库存人工调整必须传 reason；调整不能让总量、锁定量或已售量产生非法负数。

## 8. 后台订单和入住

| 方法 | 路径 | 查询或 Body |
| --- | --- | --- |
| GET | /api/v1/admin/orders | status、storeId |
| GET | /api/v1/admin/orders/{id} | 返回 AdminOrderDetail |
| POST | /api/v1/admin/orders/{id}/confirm | 无 Body |
| POST | /api/v1/admin/orders/{id}/check-in | 可选 CheckInRequest |
| POST | /api/v1/admin/orders/{id}/check-out | 无 Body |
| POST | /api/v1/admin/orders/{id}/cancel | 可选 query reason |

CheckInRequest：resourceCodes、note。resourceCodes 不传时后端按数量自动分配可用房间/床位；传入时会校验编码和 Available 状态。

状态转换：

1. PaidPendingConfirmation -> ConfirmedPendingCheckIn：confirm。
2. ConfirmedPendingCheckIn -> CheckedIn：check-in，资源标记 Occupied。
3. CheckedIn -> Completed：check-out，释放已分配资源。
4. 待支付订单可以由后台 cancel，并释放锁定库存。

AdminOrderDetail 包含订单、门店、房型、金额、状态、支付记录和退款记录。PaymentItem 金额为分；RefundItem 包含退款单号、金额、状态、审核意见和第三方退款号。

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
| GET | /api/v1/admin/stats/trend | from、to、storeId，最多 90 天 |
| GET | /api/v1/admin/stats/store-rank | from、to，最多 90 天 |
| GET | /api/v1/admin/audit-logs | keyword，最多返回最近 200 条 |

AdminUserRequest：username、displayName、password、phoneNumber、storeId、isEnabled、roleIds。

角色代码和权限由后端 RoleCodes 控制。总部角色可跨门店；门店角色只能访问 JWT 中 store_id 对应门店；账号新增和角色绑定只允许系统管理员。
