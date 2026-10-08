# TCPMS 项目文档

本文档目录以当前仓库代码为准，面向前端、后端、测试、运营和后续接手人员。旧版原型说明已清理；原型图片只作为视觉参考，不作为接口或业务契约。

## 文档导航

| 目标 | 文档 |
| --- | --- |
| 快速理解项目 | [项目总览](PROJECT_OVERVIEW.md) |
| 开发小程序页面 | [前端开发指南](FRONTEND_GUIDE.md) |
| 本地启动和发布 | [开发与部署](DEVELOPMENT.md) |
| 对接后端接口 | [接口使用说明](API_USAGE.md) |
| 查看完整接口契约 | [后端接口契约](BACKEND_API.md) |
| 了解业务状态 | [业务规则](PRODUCT_RULES.md) |
| 交接与排障 | [交接手册](HANDOFF.md) |

## 真实来源优先级

出现不一致时按以下顺序判断：

1. 运行中的后端 OpenAPI JSON：/swagger/v1/swagger.json。
2. src/backend/TCPMS.Api 的实际路由、DTO 和状态转换代码。
3. src/TCPMS.User 与 src/admin 的实际调用代码。
4. 本目录说明文档。

接口文档必须以运行时 Swagger 为真源。文档中的路由示例只用于帮助理解，不替代 Swagger 中的参数和响应模型。

## 项目组成

~~~text
TCPMS/
├─ src/TCPMS.User/       uni-app + Vue 3 微信小程序
├─ src/admin/             Vue 3 + Element Plus 管理后台
├─ src/backend/           ASP.NET Core 10 API
├─ docs/                  当前项目文档
└─ docker-compose.yml     本地 MySQL
~~~

## 本地入口

- API 健康检查：http://localhost:5180/api/v1/health
- Swagger UI：http://localhost:5180/swagger
- OpenAPI JSON：http://localhost:5180/swagger/v1/swagger.json
- 管理后台：http://localhost:5173
- 小程序：使用 HBuilderX 打开 src/TCPMS.User，运行到微信开发者工具。

## 文档维护规则

- 新增接口时，先更新 DTO、路由和 XML 注释，再验证 Swagger JSON，最后补充本目录说明。
- 修改页面路由、公共组件或本地存储键时，同步更新前端开发指南。
- 记录已知限制和上线前事项，不要用“已完成”掩盖开发占位。
- 日期、金额、状态名称和权限边界应写出具体值。
