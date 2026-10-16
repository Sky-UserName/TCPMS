# 后台门店权限前端 API 对接记录

更新时间：2026-10-08

本次只完成后台管理端前端页面和本地演示数据，未修改后端接口、数据库或小程序接口。页面当前使用 `localStorage`，便于后续统一替换为 API。

## 已完成的前端页面

- `门店员工`
  - 用户名
  - 性别
  - 手机号
  - 状态：正常 / 隐藏
  - 民宿下拉选择
  - 权限菜单多选
  - 创建时间、更新时间
- `权限菜单`
  - 规则（页面路径）
  - 标题
  - 图标标识
  - 图标上传和预览
  - 权重
  - 状态：正常 / 隐藏
  - 创建时间、更新时间
  - 同步用户端个人首页的 `merchantEntries`

## 后续建议接口

### 门店下拉

沿用或确认现有门店接口：

```http
GET /api/v1/admin/stores
```

返回至少包含：

```json
[
  { "id": "store-001", "name": "民宿名称" }
]
```

### 门店员工

建议增加标准 CRUD：

```http
GET    /api/v1/admin/store-staff
POST   /api/v1/admin/store-staff
PUT    /api/v1/admin/store-staff/{id}
DELETE /api/v1/admin/store-staff/{id}
```

建议字段：

```json
{
  "id": "staff-001",
  "username": "frontdesk",
  "gender": "male",
  "phone": "13800000000",
  "status": "normal",
  "storeId": "store-001",
  "permissions": ["rooms", "orders", "verify"],
  "createdAt": "2026-10-08T10:00:00+08:00",
  "updatedAt": "2026-10-08T10:00:00+08:00"
}
```

权限建议保存菜单 `key`，不要保存标题，避免标题调整后产生脏数据。

### 权限菜单

建议增加标准 CRUD：

```http
GET    /api/v1/admin/permission-menus
POST   /api/v1/admin/permission-menus
PUT    /api/v1/admin/permission-menus/{key}
DELETE /api/v1/admin/permission-menus/{key}
POST   /api/v1/admin/permission-menus/sync
```

建议字段：

```json
{
  "key": "rooms",
  "route": "/pages/merchant/rooms",
  "title": "房间管理",
  "iconKey": "house",
  "iconUrl": "https://cdn.example.com/icons/rooms.png",
  "weight": 2,
  "status": "normal",
  "createdAt": "2026-10-08T10:00:00+08:00",
  "updatedAt": "2026-10-08T10:00:00+08:00"
}
```

### 用户端实时同步

当前小程序个人首页的入口来源是：

```text
src/TCPMS.User/pages/me/index.vue
merchantEntries
```

后续建议由后台和小程序共同读取同一份已发布菜单：

```http
GET /api/v1/merchant/permission-menus
```

接口只返回 `status=normal` 的菜单，并按 `weight` 升序返回。后台点击“同步用户端菜单”时可以调用管理端同步接口；小程序个人首页则直接读取公开菜单接口。若需要避免缓存，可增加 `version` 或使用 `updatedAt` 做增量刷新。

### 图标上传

当前前端使用 `FileReader` 生成 Data URL 并通过权限菜单接口持久化到 `iconUrl`，因此适合开发和小规模配置。生产环境建议再接入对象存储上传接口，例如：

```http
POST /api/v1/admin/media/upload
```

接口返回图片 URL 后保存到权限菜单的 `iconUrl`，小程序端继续使用 `iconKey` 作为本地默认图标，`iconUrl` 作为后台上传图片。

## 服务端持久化状态

权限菜单和门店员工页面已移除本地读写，分别使用 `/api/v1/admin/permission-menus` 和 `/api/v1/admin/store-staff`。浏览器缓存只保留登录 JWT、首次提醒已读标记和页面筛选状态，不再作为业务数据源。

## 会员管理前端调整记录

更新时间：2026-10-08

本次只调整后台会员管理前端，未修改后端 API、数据库或微信登录流程：

- 列表隐藏内部 `ID`。
- 列表保留：用户名（当前使用 `OpenId`）、昵称、手机号、头像、最近登录、状态。
- 列表移除：余额、累计佣金、直属用户、间属用户。
- 移除“添加会员”入口；后台不再调用 `POST /api/v1/admin/members`。
- 用户资料字段在后台编辑弹窗中只读，来源标记为微信授权。
- 用户首次授权自动注册仍由 `POST /api/v1/auth/miniapp-login` 负责。
- 当前保留启用/停用操作，兼容现有 `PUT /api/v1/admin/members/{id}`；由于旧接口仍是完整会员更新请求，前端会在请求中保留原有财务和推荐关系字段，但不在界面展示或编辑。

后续 API 改造建议：

```http
PATCH /api/v1/admin/members/{id}/status
```

状态接口只接收 `isEnabled`，前端即可移除对余额、佣金和推荐关系字段的兼容传参。会员删除也建议改为隐藏/停用，避免删除微信授权用户记录。微信手机号授权还需要补充 `phoneCode` 等参数，由后端完成手机号解密并写入会员资料。

## 后台导航与页面头部调整记录

更新时间：2026-10-09

本次同步调整后台管理端前端和后端 API：

- 移除“民宿管理”下的“通用配置”菜单入口及前端页面映射。
- 保留“门店管理”“门店权限”“基础配置”“财务管理”“系统管理”等现有功能目录。
- 消息提醒页与消息日志页拆分，右上角铃铛、首次进入弹窗和控制台待办统一进入消息提醒页。
- 评价、提现和收入页面分别接入 `/admin/reviews`、`/admin/withdrawals` 和 `/admin/stats/income`。
- 等级、房型分类、服务设施和民宿标签等配置页接入 `/admin/managed-items/{resourceType}`。
- 移除民宿管理相关页面顶部的英文小标题和中文页面大标题。
- 保留每个页面原有的功能介绍文案、刷新/新增按钮、筛选、表格和业务操作。
- 工作台首页标题保持不变。
- 订单详情弹窗中的业务标题保持不变，避免影响详情识别。

后续如改动后端菜单、页面权限或页面标题接口，需要同步补充本文件的变更日期、接口路径和字段说明。

## 门店、房间与价格设置前端改造记录

更新时间：2026-10-09

本次需求先只改后台管理端前端，未修改后端接口、数据库或小程序接口。门店表单的图片上传先使用浏览器本地预览，房间、价格和地区选择先沿用现有接口能力或前端演示数据；后续接入正式 API 时需要按下面的字段约定替换。

### 当前后端已有接口

门店接口已经存在，当前路由前缀为 `/api/v1`：

```http
GET  /api/v1/admin/stores
GET  /api/v1/admin/stores/{id}
POST /api/v1/admin/stores
PUT  /api/v1/admin/stores/{id}
```

门店详情和保存请求目前支持以下位置、账号及资质字段：

```json
{
  "name": "门店名称",
  "code": "STORE-001",
  "description": "门店介绍",
  "province": "省/自治区/直辖市",
  "city": "城市",
  "district": "区/县",
  "address": "详细地址",
  "phone": "门店电话",
  "longitude": 121.4737,
  "latitude": 31.2304,
  "status": "Draft",
  "sortOrder": 0,
  "ownerUsername": "房东账号",
  "ownerNickname": "房东昵称",
  "ownerAvatarUrl": "https://...",
  "ownerName": "房东姓名",
  "ownerPhone": "13800000000",
  "idCardFrontUrl": "https://...",
  "idCardBackUrl": "https://...",
  "propertyCertificateUrl": "https://...",
  "businessLicenseUrl": "https://...",
  "doorImageUrl": "https://...",
  "roomImageUrlsJson": "[\"https://...\"]",
  "rating": 5
}
```

现有门店接口没有独立删除路由；前端删除或下架交互应先使用状态字段（`Offline` / `Disabled`），后续如确实需要物理删除再补充 `DELETE /api/v1/admin/stores/{id}`。

地图能力已经存在：

```http
POST /api/v1/stores/{id}/geo/geocode
POST /api/v1/stores/{id}/geo/reverse
```

这两个接口由高德服务解析并回写省、市、区、标准地址、行政区划编码、经纬度和 POI 信息。门店编辑页的“地图选择”可以继续复用该能力。

### 地区三级联动现状与后续接口

地区管理后端已在 2026-10-09 完成，当前使用 `Region` 实体和地区表提供真实三级树。接口清单、字段和种子范围见本文末尾的“地区管理种子与管理接口”章节。

正式部署前仍需确认数据库迁移策略（开发环境使用 InMemory；已有 MySQL 数据库需要执行对应建表/迁移），并保留下面的接口约定作为兼容参考：

```http
GET    /api/v1/admin/regions?parentCode=
GET    /api/v1/admin/regions/tree
POST   /api/v1/admin/regions
PUT    /api/v1/admin/regions/{code}
DELETE /api/v1/admin/regions/{code}
```

建议返回字段：

```json
{
  "code": "310101",
  "parentCode": "310000",
  "name": "黄浦区",
  "level": 3,
  "status": "normal",
  "sortOrder": 1,
  "children": []
}
```

门店表单保存时建议同时提交 `provinceCode`、`cityCode`、`districtCode`（或统一的 `administrativeAreaCode`）和展示名称；仅保存名称会导致地区重名和后续筛选不稳定。当前门店页面继续兼容已有名称字段，后续接入编码字段时应复用 `Region.Code`。

### 房间列表、审核与价格接口

当前后端已经具备房间和价格能力，路由前缀为 `/api/v1/admin`：

```http
GET    /api/v1/admin/room-types?storeId={id}&includeUnpublished=true
POST   /api/v1/admin/room-types
PUT    /api/v1/admin/room-types/{id}
DELETE /api/v1/admin/room-types/{id}
POST   /api/v1/admin/room-types/{id}/review
POST   /api/v1/admin/room-types/{id}/publish?published=true
GET    /api/v1/admin/prices?roomTypeId={id}&from=YYYY-MM-DD&to=YYYY-MM-DD
PUT    /api/v1/admin/prices/bulk
GET    /api/v1/admin/inventory?roomTypeId={id}&from=YYYY-MM-DD&to=YYYY-MM-DD
POST   /api/v1/admin/inventory/adjust
```

房间审核拒绝时必须提交 `rejectionReason`；价格接口使用分为单位的 `PriceCents`，前端显示元并在请求时乘以 100。现有价格查询和库存查询的最大范围为 31 天，批量价格最多 180 天，前端月历切换应按月请求。

建议后续补充门店房间专用查询参数或确认前端继续使用：

```http
GET /api/v1/admin/room-types?storeId={storeId}&includeUnpublished=true
```

这样门店列表的“房间”按钮可以直接带入 `storeId`，只展示当前门店发布或待审核的房间。

### 节日标注

当前后端没有节日/法定节假日接口或节日数据表，现有价格日历只能返回日期和价格来源。前端可以先按周末显示基础颜色，但附件要求的“节日名称和红色标注”需要后续增加：

```http
GET /api/v1/admin/holidays?from=YYYY-MM-DD&to=YYYY-MM-DD
```

建议返回：

```json
[
  {
    "date": "2026-10-01",
    "name": "国庆节",
    "type": "legal",
    "isHoliday": true
  }
]
```

价格日历收到节日数据后，节日名称、日期数字和卡片边框使用红色；普通周末可以使用较浅的提示色，不应与节日混淆。

### 图片上传与预览

当前门店和房间请求只保存图片 URL 字符串，没有后端上传接口。前端可以把本地文件转为 Data URL 做即时预览，但不能把 Data URL 作为长期生产存储方案。

建议后续统一接入：

```http
POST /api/v1/admin/media/upload
```

返回至少包含：

```json
{
  "url": "https://cdn.example.com/uploads/...",
  "thumbnailUrl": "https://cdn.example.com/uploads/.../thumb",
  "width": 1200,
  "height": 800,
  "size": 123456,
  "contentType": "image/jpeg"
}
```

接入后，门店资质图片使用单 URL 字段，房间图片使用 URL 数组序列化到 `ImageUrlsJson`；前端仍保留上传后立即显示预览、查看大图和删除单张图片的交互。

### 本次前端改造对接注意事项

- 门店列表的“房间”按钮必须携带当前 `storeId`，不能打开未筛选的全量房间列表。
- 门店图片列和编辑表单都显示图片预览，不只显示 URL。
- 地区三级联动页面优先请求真实接口 `GET /admin/regions/tree`，失败后兼容 `GET /admin/regions`，并使用过滤后的本地省市区种子作为离线回退。
- 审核状态沿用 `Draft`、`PendingReview`、`Approved`、`Rejected`、`Offline`；门店状态沿用 `Draft`、`PendingReview`、`Active`、`Offline`、`Disabled`。
- 本次不更改后端 API；后端后续每次改动都要在本文件追加日期、路径、请求字段、响应字段和状态映射。

## 地区管理种子与管理接口

更新时间：2026-10-09

本次已将地区管理从演示列表改为真实的中国大陆省、市、区县三级数据，并同步接入门店地区级联选择器。

### 后端数据与初始化

- 新增 `Region` 实体和 `Regions` 数据集，行政区划代码以字符串保存，避免前导零丢失。
- 应用启动时由 `SeedData.InitializeAsync` 幂等写入 `RegionSeedData`。
- 种子数据范围为中国大陆 31 个省级、342 个市级、3272 个区县（共 3645 条）；明确排除港澳台及 9 位街道、乡镇节点。
- 前端离线回退数据使用 `china-area-data@5.0.1`，与后端种子统一按 6 位区县编码过滤。
- 地区名称、简称、全称、层级、首字母、排序、热门标记、上下架状态均作为字段保存。
- 父子关系使用 `ParentCode` 和自关联约束；有下级地区时不允许删除上级地区。

### 管理接口

```http
GET    /api/v1/admin/regions?level=&parentCode=&keyword=&includeDisabled=
GET    /api/v1/admin/regions/tree?includeDisabled=
GET    /api/v1/admin/regions/{code}?includeDisabled=
POST   /api/v1/admin/regions
PUT    /api/v1/admin/regions/{code}
DELETE /api/v1/admin/regions/{code}
```

写接口只允许系统管理员和运营审核角色使用。创建/更新请求字段如下：

```json
{
  "code": "310000",
  "parentCode": null,
  "name": "上海市",
  "shortName": "上海",
  "fullName": "上海市",
  "level": "Province",
  "initial": "S",
  "isHot": false,
  "isEnabled": true,
  "sortOrder": 1
}
```

接口会校验 6 位数字编码、父级存在性、三级层级关系、编码前缀和唯一性。修改时编码不可变；删除有子级的地区返回 `409 region_has_children`。新增、修改、删除均写入审计日志。

### 后台页面

- 新增 `RegionManagerView.vue`，提供全部/上架/下架筛选、层级筛选、关键词搜索、分页、层级钻取、当前位置导航、添加/编辑/删除和 CSV 导出。
- 列表展示编码、简称、名称、全称、级别、首字母、热门状态、上下架状态、上级地区和操作。
- `/admin/regions` 接口不可用时，页面使用同一依赖包生成的本地省市区种子数据；本地备用数据同样过滤 9 位街道/乡镇。
- 门店编辑页的三级地区选项优先读取 `/api/v1/admin/regions/tree`，失败时回退到同一份前端种子数据。

### 后续 API 对接注意事项

- 门店保存仍以现有 `province`、`city`、`district` 展示字段为主；接入正式地区编码字段时，应同时提交省、市、区县编码。
- 后台地区种子更新后，需要同步更新前端依赖版本或备用数据校验，确保层级数量和编码规则一致。

## 订阅消息模板与消息日志

更新时间：2026-10-10

本次新增小程序订阅消息模板配置和发送日志管理，后台页面对应“订阅消息”和“消息日志”两个菜单。当前开发环境使用 InMemory 数据库，应用重启后会重新生成演示模板和日志种子；生产环境接入持久化数据库后不应依赖演示模板 ID 发送真实消息。

### 模板接口

```http
GET    /api/v1/admin/subscription-templates?keyword=&includeDisabled=
GET    /api/v1/admin/subscription-templates/{id}
POST   /api/v1/admin/subscription-templates
PUT    /api/v1/admin/subscription-templates/{id}
DELETE /api/v1/admin/subscription-templates/{id}
```

模板请求字段：

```json
{
  "type": "miniProgram",
  "identifier": "miniprogram_order_success",
  "name": "预定民宿成功",
  "templateId": "微信公众平台模板ID",
  "isEnabled": true,
  "parameters": [
    {
      "description": "民宿名称",
      "field": "thing6",
      "mappingField": "shop_name"
    }
  ]
}
```

响应字段包含 `id`、`type`、`identifier`、`name`、`templateId`、`parameters`、`isEnabled`、`createdAt` 和 `updatedAt`。参数映射由后台保存为 JSON，页面支持动态添加、删除和上下排序。标识唯一，最多 30 个参数，模板 ID 和名称不能为空。

### 消息日志接口

```http
GET    /api/v1/admin/message-logs?keyword=&subscriptionTemplateId=&memberId=&sendStatus=&page=1&pageSize=20
GET    /api/v1/admin/message-logs/{id}
POST   /api/v1/admin/message-logs
PUT    /api/v1/admin/message-logs/{id}
DELETE /api/v1/admin/message-logs/{id}
```

日志创建请求支持 `subscriptionTemplateId`、`templateName`、`templateId`、`memberId`、`openId`、`template`、`sendResult` 和 `sendStatus`。列表返回分页对象：

```json
{
  "items": [
    {
      "id": "guid",
      "subscriptionTemplateId": "guid",
      "templateName": "预定民宿成功",
      "templateId": "微信公众平台模板ID",
      "memberId": "guid",
      "openId": "openid",
      "template": "{\"thing6\":\"好人民宿\"}",
      "sendResult": "{\"errcode\":0,\"errmsg\":\"ok\"}",
      "sendStatus": "Success",
      "createdAt": "2026-10-09T15:38:23Z",
      "updatedAt": "2026-10-09T15:38:23Z"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20
}
```

删除模板时日志会保留，关联模板 ID 置空；删除日志和模板配置均写入审计日志。模板和日志页面以服务端接口为唯一业务数据源，接口异常时直接提示错误，不再伪装写入本地演示数据。

### 种子与真实发送说明

- 开发环境默认生成 5 个模板和 5 条日志，模板 ID 使用 `demo_` 前缀的占位值，不代表可直接调用微信接口。
- `sendStatus` 支持 `Pending`、`Sent`、`Failed`、`InsufficientQuota`、`Unknown` 等状态，前端分别显示待发送、发送成功、发送失败、次数不足或未知。
- 当前接口只负责后台配置和日志读写；微信订阅消息的 access token、用户订阅授权、模板字段校验和真实发送仍需在消息发送服务中接入，并在发送成功/失败后写入 `POST /api/v1/admin/message-logs`。
