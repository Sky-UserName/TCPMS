# 小程序前端开发指南

## 1. 技术栈和运行方式

- 框架：uni-app + Vue 3 单文件组件。
- 目标：微信小程序，开发期也可运行 H5 预览。
- 编译入口：src/TCPMS.User/main.js。
- 页面注册：src/TCPMS.User/pages.json。
- 资源目录：src/TCPMS.User/static。
- 微信构建产物：src/TCPMS.User/unpackage/dist/dev/mp-weixin；这是编译输出，不要直接修改。

使用 HBuilderX 打开 src/TCPMS.User。需要查看页面时运行到微信开发者工具；H5 预览只用于快速检查布局和交互。

## 2. 页面路由

| 业务域 | 页面 |
| --- | --- |
| 首页与搜索 | pages/index/index、pages/search/index、pages/city/select（选择城市，含当前定位 / 热门城市 / A-Z 索引） |
| 认证 | pages/auth/login、pages/auth/privacy |
| 门店 | pages/store/list、pages/store/nearby、pages/store/map（门店详情已并入 pages/room/detail） |
| 房型 | pages/room/list、pages/room/detail |
| 预订 | pages/booking/date、pages/booking/confirm、pages/pay/result |
| 订单 | pages/order/list、pages/order/detail、pages/order/refund |
| 评价 | pages/review/list、pages/review/create |
| 个人中心 | pages/me/index、pages/me/guests、pages/me/favorites、pages/me/history |
| 服务与内容 | pages/service/contact、pages/message/list、pages/content/detail、pages/rule/index |

所有页面使用自定义导航栏，顶部必须预留系统状态栏安全区；不要在页面内部再叠加一层固定标题栏。

## 3. 公共组件

### ProtoHeader

统一处理标题、返回按钮、左侧标题、右侧用户按钮和安全区。新页面优先复用，不要复制一套 header CSS。覆盖大图时使用组件提供的 overlay 属性，正文页面使用默认背景。

### ProtoBottomNav

底部导航固定在视口底部，当前只允许五项：首页、门店、订单、消息、我的。页面根节点使用 .proto-page，它会自动预留底部导航高度。详情页如需自己的操作栏，必须额外增加底部安全区，不能覆盖全局导航。

### ProtoIcon

图标资源来自 static/prototype-icons。新增图标前先检查已有资源，不要在页面内手写重复 SVG。

### ProtoEmptyState

所有用户端无数据场景统一使用 ProtoEmptyState 和 static/prototype-empty-data.png。适用范围包括无门店、无订单、无消息、无收藏、无浏览记录和无搜索结果。不要再使用旧的空白白框、单独说明文字或不同插画。

## 4. API 和本地数据

页面只通过 common/api.js 调用后端，不要在页面中直接使用 uni.request。请求层会：

- 从 app-store.js 读取 JWT。
- 自动附加 Authorization: Bearer <token>。
- 过滤空查询参数。
- 将后端 DTO 映射为页面模型。
- 仅本地演示记录使用演示兜底；服务端 GUID 记录不会用样例价格、图片、评分或房型补全。

默认 API 地址是云服务器 `http://39.96.200.62/api/v1`（`common/api.js` 的 `defaultApiBaseUrl`）。构建时可通过 `VITE_API_BASE_URL` 配置；开发者工具中也可按设备覆盖：

~~~js
uni.setStorageSync('tcpms.apiBaseUrl', 'http://39.96.200.62/api/v1')
~~~

本地联调后端时把 `defaultApiBaseUrl` 或上面的存储值改回 `http://localhost:5180/api/v1`（真机不能写 localhost，要用开发机的局域网 IP）。开发者工具需勾选“不校验合法域名”才能请求 HTTP 的 IP 地址；正式版必须换成 HTTPS 域名，并在公众平台加入 request 合法域名。`tcpms.apiBaseUrl` 优先于构建变量，适合开发联调；上线前检查设备存储中没有旧的测试地址。

后端字段与页面字段不一定相同，例如：

- totalAmountCents -> 页面 price（元）。
- status -> status、statusKey、action。
- distanceKm -> distance、unknownDistance。
- 房型 kind -> 页面 type。

修改接口字段时，应同时修改 mapStore、mapRoom 或 mapOrder，并检查订单列表、详情和确认页。

## 5. 本地持久化键

| 键 | 内容 |
| --- | --- |
| tcpms.auth | 是否已登录 |
| tcpms.apiToken | JWT |
| tcpms.profile | 用户资料 |
| tcpms.guests | 常用入住人 |
| tcpms.orders | 本地订单缓存 |
| tcpms.reviews | 本地评价缓存；评价提交后同步订单的 hasReview 状态 |
| tcpms.booking | 当前预订条件 |
| tcpms.favorites / tcpms.favoriteMeta | 收藏 ID 和展示信息 |
| tcpms.recent | 最近浏览记录 |

新增键时在 app-store.js 的 storageKeys 中集中定义，避免页面散落字符串。

## 6. 页面实现约定

1. script setup 中先加载状态，再加载数据；加载失败时展示统一空状态或错误提示。
2. 页面根节点保留 .proto-page，内容不要直接固定到底部。
3. 颜色、间距、圆角和阴影优先使用 common/prototype.scss 的变量和公共类。
4. 颜色保持蓝绿色主题，避免橙色主按钮和突兀的白色空框。
5. 日期选择使用入住日包含、离店日不包含；展示文本和传给 API 的 ISO 日期分开。
6. 列表处理加载中、成功有数据、成功无数据、请求失败和刷新五种状态。
7. 写操作完成后同步更新本地订单缓存，并防止重复点击。
8. 图片必须设置稳定宽高或 aspect-ratio，避免加载后页面跳动。
9. 房型详情页的评分模块进入评价列表；已完成订单显示“评价”入口，提交后显示“查看评价”。
10. 订单填写页（pages/booking/confirm）的入住信息默认留空，不做任何预填；入住人只能从 `pages/me/guests?mode=select&selected=id1,id2` 选择后回传（`navigateTo` 的 `events.guestsSelected`，并用 `uni.$emit('booking:guests-selected')` 兜底）。选中结果只保存在页面状态，不写入 `tcpms.booking`，避免下一次下单带上上一次的入住人。
11. 入住人数量与出行人数 `booking.guestCount` 解耦：不加 `count` 上限，选择页可任意多选（最多受常用入住人 10 条限制），订单页只校验「至少 1 位」，不足或超出都不做截断；`guests` 全量写入 `guestSnapshotJson`，`guestCount` 仍按出行人数走高，用于报价承载校验。
12. 订单报价（`POST /orders/preview`）的入参必须在 `common/api.js` 里消毒后再发：日期统一成 `YYYY-MM-DDT00:00:00`，`quantity`/`guestCount` 用正整数兜底（脏数据会直接 400 绑定失败，而不是返回业务错误码）。报价 400 的处理顺序是「能修的先修、修不了给出路」：人数超承载 → 按 `maxGuests × 间数` 收敛后重试一次；日期过期 → 重置到明天重试一次；库存不足 → 文案里带上当前间数，并提供「修改入住日期」「改为 1 间」两个入口。`booking.maxGuests` 只在从房型详情页进入时才有值，订单页会用门店详情兜底补一次。
13. 房型详情页（pages/room/detail）展示库存：按当前入住日期调 `/room-types/{id}/availability`，取区间内每日 `availableQuantity` 的**最小值**（连住时最紧的那天决定上限）。文案**任何状态都必须带数字**，否则满房时会被误判成「没查到库存」：`>3` → 「可订 N 间」、`1~3` → 「仅剩 N 间」、`0` → 「已满房 · 可订 0 间」。同一响应里的 `totalQuantity` 也取区间最小值，在房型信息底部右侧显示「共 M 间」；其他房型卡片的库存胶囊用 `stock.short`（满房为「已满房 · 0 间」，避免挤压布局）。同一 `roomId + 日期` 只请求一次（`inventoryCache`），日期变化才重查；查询失败或房型未配置库存时只是不展示库存文案，不阻塞页面。另有 `OrderExpiryService` 每分钟扫描过期未支付订单并回滚 `LockedQuantity`，所以待支付单会短暂把可售量压到 0，最长约 16 分钟。
14. 订单列表（`pages/order/list`）是 tabBar 页，`switchTab` 不能带查询参数。个人中心点「全部/待支付/可使用/已使用/取消退款」时，先用 `uni.setStorageSync('tcpms.orderListTab', key)` 写入一次性分类再跳转；订单页在 `onShow` 里读取并立即清空，只对合法 key 生效（`applyRequestedTab`），否则保持原分类。页面顶部在非「待支付」分类下会额外显示「你有 N 笔待支付订单」提醒条（`pendingOrders`），点「去支付」直接跳 `/pages/pay/result?orderId=`。待支付超时会被后端 `OrderExpiryService` 自动取消并转到「取消/退款」，所以「待支付」只保留仍在 15 分钟支付窗口内的订单。
