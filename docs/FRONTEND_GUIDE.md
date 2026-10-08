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
| 首页与搜索 | pages/index/index、pages/search/index |
| 认证 | pages/auth/login、pages/auth/privacy |
| 门店 | pages/store/list、pages/store/nearby、pages/store/map、pages/store/detail |
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
- 在开发期 API 不可用时回退到演示数据。

默认 API 地址是 http://localhost:5180/api/v1。开发者工具中可设置：

~~~js
uni.setStorageSync('tcpms.apiBaseUrl', 'http://localhost:5180/api/v1')
~~~

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
