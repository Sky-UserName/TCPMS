# 开发、联调与部署

## 1. 环境要求

- Windows 11 或 macOS。
- .NET SDK 10。
- Node.js 18 或更高版本。
- npm。
- HBuilderX 和微信开发者工具。
- 可选：Docker Desktop，用于本地 MySQL。

## 2. 启动后端 API

~~~powershell
cd D:\Code\微信小程序\TCPMS\src\backend\TCPMS.Api
dotnet restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5180
~~~

`Development` 环境使用 InMemory 数据库并写入演示门店、房型、库存、订单和管理员。若未设置环境变量，应用会读取生产默认配置并尝试连接 MySQL。

验证：

~~~powershell
Invoke-WebRequest http://localhost:5180/api/v1/health
Invoke-WebRequest http://localhost:5180/swagger/index.html
Invoke-WebRequest http://localhost:5180/swagger/v1/swagger.json
~~~

打开 http://localhost:5180/swagger 查看、试用和导出接口。前端对接时以 Swagger JSON 中的路径、参数和 schema 为准。

Swagger 当前已启用 JWT Bearer 输入框、中文接口摘要、OperationId、成功响应 schema 和常见错误响应说明。开发环境联调时先调用登录接口，再点击右上角 Authorize 输入 `Bearer <token>`；公开门店、房型和健康检查接口不需要 token。

## 3. 启动管理后台

~~~powershell
cd D:\Code\微信小程序\TCPMS\src\admin
npm install
npm run dev
~~~

默认地址：http://localhost:5173。如 API 不在本机：

~~~powershell
$env:VITE_API_BASE_URL = "https://api.example.com/api/v1"
npm run dev
~~~

## 4. 启动小程序

1. 使用 HBuilderX 打开 src/TCPMS.User。
2. 运行到微信开发者工具。
3. 开发阶段允许“不校验合法域名”。
4. 确认 common/api.js 默认地址或 tcpms.apiBaseUrl 指向后端。
5. 修改页面后重新编译，不要编辑 unpackage/dist。

## 5. 本地 MySQL

~~~powershell
cd D:\Code\微信小程序\TCPMS
docker compose up -d mysql
~~~

切换数据库前配置：

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__Default = "Server=localhost;Port=3306;Database=tcpms;User=tcpms;Password=replace-me;"
dotnet run --project src/backend/TCPMS.Api --urls http://localhost:5180
~~~

生产环境必须替换默认密码、JWT 密钥、数据库连接、微信支付证书和高德 Key。

生产环境可通过 `Swagger__Enabled=false` 关闭公开文档，或在内网/网关后启用后再提供给前端联调人员。

## 6. 常用验证

~~~powershell
dotnet build src/backend/TCPMS.Api/TCPMS.Api.csproj
cd src/admin
npm run build
~~~

前端检查重点：

- 页面顶部文字没有被系统状态栏遮挡。
- 五项底部导航固定且只有一套。
- 列表、订单、消息、收藏和门店无数据时显示统一空状态图。
- 日期、人数、房型弹层关闭和确认后状态正确回填。
- API 不可用时本地回退只用于开发联调。

## 7. 常见问题

| 现象 | 排查 |
| --- | --- |
| /health 404 | 正确地址是 /api/v1/health |
| Swagger 打不开 | 确认 API 进程、端口 5180 和 swagger/v1/swagger.json |
| 小程序请求失败 | 检查 API 基地址、域名校验和后端 CORS |
| 页面使用旧数据 | 清理小程序缓存，检查 tcpms.* 本地存储 |
| 顶部或底部被遮挡 | 检查 ProtoHeader、.proto-page 和重复固定导航 |
| 图片不显示 | 检查 static/prototype 资源路径 |

## 8. 上线前清单

- 接入微信 code2Session、手机号授权和隐私协议。
- 接入微信支付 JSAPI、支付回调、退款和回调幂等。
- 配置 HTTPS 合法域名、对象存储和图片审核。
- 完成 MySQL 迁移、备份、恢复和定时任务。
- 关闭生产环境 mock-pay 和不受控本地演示回退。
- 为 JWT、数据库、地图和支付凭证配置密钥托管。
- 通过 Swagger 导出并评审对外接口，再发布前端。
