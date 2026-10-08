# 项目总览

## 1. 项目定位

TCPMS 是面向民宿和旅宿连锁业务的多门店预订与运营系统，包含：

- 用户端微信小程序：查找门店、定位、选择房型和日期、提交订单、支付、退款、查看消息。
- 管理后台：维护门店、房型、房间/床位、价格、库存、订单、退款、账号、角色和审计。
- 后端 API：统一承载认证、门店、地图、房型、库存、订单、支付联调和后台运营能力。

项目当前是开发联调版。开发环境默认使用 EF Core InMemory 和演示种子数据；生产环境需要切换 MySQL、微信登录、微信支付、对象存储和 HTTPS。

## 2. 用户和角色

| 端 | 角色 | 主要操作 |
| --- | --- | --- |
| 微信小程序 | 普通用户 | 浏览门店、定位、筛选、选日期、选房型、下单、模拟支付、取消、退款、查看消息 |
| 管理后台 | 门店管理员 | 维护所属门店、房型、资源、库存、订单入住/离店、退款 |
| 管理后台 | 总部管理员 | 维护全量门店、价格、库存、账号角色、统计和审计 |
| 管理后台 | 运营/财务 | 审核房源、处理订单和退款、查看经营数据 |

## 3. 典型用户流程

~~~text
首页
  -> 选择城市、日期、人数床位
  -> 门店列表或地图
  -> 门店详情
  -> 房型列表
  -> 房型详情
  -> 确认订单
  -> 开发支付
  -> 订单列表和订单详情
  -> 取消或申请退款
~~~

消息、个人中心、收藏、浏览记录和联系客服是独立入口；所有主页面共用固定底部导航：首页 / 门店 / 订单 / 消息 / 我的。

## 4. 目录与职责

### 用户端

src/TCPMS.User 是 uni-app + Vue 3 项目：

- App.vue：应用生命周期和全局样式入口。
- pages.json：页面注册、标题和自定义导航配置。
- pages/：按业务域拆分页面。
- components/ProtoHeader.vue：统一顶部安全区、返回和标题。
- components/ProtoBottomNav.vue：固定五项底部导航。
- components/ProtoIcon.vue：从 static/prototype-icons 读取图标。
- components/ProtoEmptyState.vue：统一暂无相关数据展示。
- common/api.js：请求封装、DTO 映射和 API 不可用时的本地回退。
- common/app-store.js：登录、预订、订单、入住人、收藏和浏览记录持久化。
- common/prototype.js：开发演示门店、房型、订单和图片资源。
- common/prototype.scss：蓝绿色视觉变量、卡片、按钮、导航和安全区样式。

### 管理后台

src/admin 使用 Vue 3、Vite 和 Element Plus。后台 API 基地址默认是 http://localhost:5180/api/v1，可通过 VITE_API_BASE_URL 覆盖。

### 后端

src/backend/TCPMS.Api 是 ASP.NET Core 10 Minimal API：

- Program.cs：应用启动、认证、公开门店/房型/订单路由和 Swagger。
- AdminEndpoints.cs：后台房型、价格、库存、退款、账号、统计和审计路由。
- MiniappOrderEndpoints.cs：用户端退款申请和退款查询路由。
- Contracts/：请求、响应和后台 DTO。
- Domain/：实体、枚举和业务状态。
- Infrastructure/：EF Core 上下文和种子数据。
- Services/：JWT TokenService、高德 AmapService 等外部能力适配。

具体路由以 Swagger JSON 和源代码为准。

## 5. 运行时数据流

~~~text
页面组件
  -> common/api.js
  -> Authorization: Bearer <token>
  -> ASP.NET Core /api/v1
  -> EF Core 或外部地图适配
  -> JSON DTO
  -> api.js 映射为页面模型
  -> 页面渲染
~~~

当 API 请求失败时，用户端页面会使用 prototype.js 和 app-store.js 的本地数据继续运行，便于 UI 联调。生产环境必须观察 API 错误并关闭或限制本地回退，避免把演示数据当成真实业务数据。

## 6. 关键技术约定

- 时间：入住日包含、离店日不包含；接口日期使用 ISO 日期字符串。
- 金额：后端以分为单位的整数传输，如 17600 表示 176.00 元。
- 身份：后台和小程序登录均返回 JWT，前端写入 tcpms.apiToken。
- 状态：后端枚举是唯一状态源，用户端通过 common/api.js 映射中文显示。
- 库存：下单逐日锁定，支付后转已售，取消或退款批准后释放。
- 安全：高德服务端 Key、支付证书和数据库密码不进入小程序包。
