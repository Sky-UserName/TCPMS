# TCPMS 民宿多门店预订与运营系统

TCPMS 是由微信小程序、管理后台和 ASP.NET Core API 组成的民宿预订与运营系统。用户可以浏览门店、定位、选择日期和房型、创建订单、开发支付、申请退款并查看消息；管理人员可以维护门店、房源、价格、库存、订单、退款、账号和审计记录。

当前版本用于开发联调：

- 用户端：uni-app + Vue 3，目录为 src/TCPMS.User。
- 管理后台：Vue 3 + Element Plus + Vite，目录为 src/admin。
- 后端：ASP.NET Core 10 Minimal API，目录为 src/backend/TCPMS.Api。
- 开发数据库：EF Core InMemory；生产可切换 MySQL。
- 主题：蓝绿色小程序 UI，固定底部导航：首页 / 门店 / 订单 / 消息 / 我的。

## 文档入口

从 [docs/README.md](docs/README.md) 开始。主要文档：

- [项目总览](docs/PROJECT_OVERVIEW.md)
- [小程序前端开发指南](docs/FRONTEND_GUIDE.md)
- [开发、联调与部署](docs/DEVELOPMENT.md)
- [后端接口契约摘要](docs/BACKEND_API.md)
- [接口使用说明](docs/API_USAGE.md)
- [业务规则](docs/PRODUCT_RULES.md)
- [交接手册](docs/HANDOFF.md)

## 快速启动

### 后端

~~~powershell
cd src/backend/TCPMS.Api
dotnet restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5180
~~~

`Development` 环境默认使用 EF Core InMemory 数据库并写入演示种子数据。若不设置该环境变量，应用会按 `appsettings.json` 使用 MySQL，并要求数据库账号已经准备好。

- 健康检查：http://localhost:5180/api/v1/health
- Swagger UI：http://localhost:5180/swagger
- OpenAPI JSON：http://localhost:5180/swagger/v1/swagger.json

开发后台账号：admin / Admin@123456。

### 管理后台

~~~powershell
cd src/admin
npm install
npm run dev
~~~

默认地址：http://localhost:5173。API 地址可通过 VITE_API_BASE_URL 设置。

### 微信小程序

使用 HBuilderX 打开 src/TCPMS.User，运行到微信开发者工具。开发工具中需要允许本地请求或配置合法域名；生产环境必须使用 HTTPS 合法域名。

## 生产前必须完成

1. 微信 code2Session、手机号授权和隐私协议。
2. 微信支付 JSAPI、支付回调、退款和幂等。
3. MySQL 迁移、备份恢复和定时任务。
4. 对象存储、图片审核和 HTTPS 域名。
5. 高德服务端 Key、JWT、数据库和支付证书密钥托管。
6. 订单超时释放、订阅消息、错误监控和告警。

详细限制和交接步骤见 [docs/HANDOFF.md](docs/HANDOFF.md)。
