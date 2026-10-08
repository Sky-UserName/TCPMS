# 接口使用说明

## 1. 运行时真源

后端接口文档由 ASP.NET Core Swagger 运行时生成：

- Swagger UI：http://localhost:5180/swagger
- OpenAPI JSON：http://localhost:5180/swagger/v1/swagger.json

本文件只解释对接流程和模块边界。参数、响应字段、枚举和状态码变化，都必须以当前 OpenAPI JSON 为准。
完整路由、DTO、权限和状态摘要见 [后端接口契约摘要](BACKEND_API.md)。

接口前缀为 /api/v1。开发环境服务地址为 http://localhost:5180。

## 2. 认证

后台登录：POST /api/v1/auth/login。开发种子账号为 admin / Admin@123456。

小程序开发登录：POST /api/v1/auth/miniapp-login。开发环境可使用 devOpenId，生产环境必须接入微信 code2Session 和手机号授权。

将登录响应 token 放入请求头：

~~~http
Authorization: Bearer <token>
~~~

common/api.js 会自动读取 tcpms.apiToken 并附加该请求头。

完整的接口路径、参数、响应模型和可执行请求示例请打开 [后端接口契约](BACKEND_API.md) 或在 Swagger 中展开对应操作；本页只保留前端联调顺序和通用约定。

## 3. 当前路由分组

| 分组 | 路径前缀 | 说明 |
| --- | --- | --- |
| 健康检查 | /api/v1/health | 服务状态和数据库提供程序 |
| 认证 | /api/v1/auth | 后台登录和小程序开发登录 |
| 地图 | /api/v1/map | 公开地图配置 |
| 门店 | /api/v1/stores | 列表、附近门店、详情和地理解析 |
| 房型 | /api/v1/room-types | 用户端房型列表和日期可用性 |
| 订单 | /api/v1/orders | 预览、创建、用户订单、取消、开发支付 |
| 退款 | /api/v1/orders/{id}/refunds | 用户申请退款和查询退款 |
| 后台概览 | /api/v1/admin/metrics | 经营指标 |
| 后台门店 | /api/v1/admin/stores | 门店维护 |
| 后台房源 | /api/v1/admin/room-types | 房型和房间/床位 |
| 后台价格库存 | /api/v1/admin/prices、/api/v1/admin/inventory | 价格日历和库存调整 |
| 后台订单 | /api/v1/admin/orders | 查询、确认、入住、离店、取消 |
| 后台退款 | /api/v1/admin/refunds | 退款审核 |
| 后台账号 | /api/v1/admin/users、/api/v1/admin/roles | 角色和管理员 |
| 后台统计审计 | /api/v1/admin/stats、/api/v1/admin/audit-logs | 趋势、门店排行和日志 |

以上分组来自当前后端路由；具体操作、参数和 schema 请在 Swagger 中展开。

## 4. 推荐联调顺序

1. GET /api/v1/health 确认服务。
2. 调用登录接口取得 JWT。
3. 调用门店列表和房型可用性。
4. 调用订单预览，再创建订单。
5. 开发环境调用 mock-pay 验证锁定库存转已售。
6. 调用订单列表、详情、取消和退款。
7. 后台验证确认、入住、离店和退款审核。

## 5. 日期、金额和幂等

- 日期范围：入住日包含、离店日不包含；使用 YYYY-MM-DD。
- 金额：字段名带 Cents 时单位是分，前端展示前转换为元。
- 数量：房间、床位、入住人数使用正整数。
- 写操作：前端防重复提交；生产支付、退款和回调必须使用业务流水号幂等。

## 6. 错误处理

前端同时检查 HTTP 状态码和 JSON 响应，不要只根据 message 判断成功：

- 401：清除 token，跳转登录。
- 403：提示没有权限，不重试。
- 404：展示资源不存在或统一空状态。
- 409：提示库存、状态或重复提交冲突。
- 400：展示字段校验或业务规则信息；当前 Minimal API 统一使用 400 返回这类错误。
- 5xx：保留 traceId，提示稍后重试并记录日志。

## 7. 新增接口的前端流程

1. 在 Swagger 中按标签、HTTP 方法和路径确认 operationId、参数和响应。
2. 在 common/api.js 或后台 API 封装中新增函数。
3. 页面只调用封装函数，不直接拼接 URL。
4. 添加加载、空数据、失败、成功和重复提交状态。
5. 判断 API 不可用时是否允许本地回退；生产敏感操作不得静默回退。
