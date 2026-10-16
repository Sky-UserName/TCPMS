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
4. 开发联调可在设备存储中设置 `tcpms.apiBaseUrl`；构建环境也可设置 `VITE_API_BASE_URL`。生产小程序须使用 HTTPS 并配置微信 request 合法域名。
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

生产配置默认开启 Swagger，接口文档地址为 `/swagger`，OpenAPI JSON 地址为 `/swagger/v1/swagger.json`。如需关闭公网访问，将 `Swagger__Enabled` 设为 `false`，或在网关层限制访问来源。

### 5.1 微信支付与高德地图配置

管理后台“系统管理 > 应用中心”会读取服务端配置并显示脱敏状态，密钥不会写入后台前端或小程序包。配置可以写入 `appsettings.Production.json`，也可以使用同名环境变量：

| 配置 | 用途 |
| --- | --- |
| `WeChat__MiniProgramAppId` / `WeChat__MiniProgramSecret` | 微信小程序登录和支付使用的 AppID、AppSecret |
| `WeChat__MerchantId` | 微信支付商户号 |
| `WeChat__MerchantSerialNumber` | 商户 API 证书序列号 |
| `WeChat__ApiV3Key` | 微信支付 API v3 Key，必须是 32 字节 |
| `WeChat__MerchantPrivateKeyPath` | `apiclient_key.pem` 的服务端文件路径；也可直接填写 `MerchantPrivateKeyPem` |
| `WeChat__PlatformCertificatePath` | 微信支付平台证书 PEM 文件路径；也可直接填写 `PlatformCertificatePem` |
| `WeChat__NotifyBaseUrl` | 后端公网基地址，例如 `https://api.example.com`；服务会拼接支付回调路径 |
| `Amap__ServerKey` | 高德 Web 服务 API Key，用于地址解析、逆解析和门店坐标维护 |
| `Amap__MiniProgramKey` | 高德小程序端 Key，供小程序侧地图能力使用 |
| `Amap__DefaultRadiusKm` | 附近门店默认搜索半径，单位为公里 |

微信支付回调地址最终为 `/api/v1/payments/wechat/notify`，必须使用公网 HTTPS，并在微信商户平台配置为与服务端一致的地址。应用中心的“测试商户连接”会调用微信支付商户 API 的证书接口；“测试地址解析”会调用高德地理编码接口。生产环境要求真实微信登录凭证，开发用 `devOpenId` 和 `mock-pay` 不会生效。

## 6. 生成发布包

在项目根目录执行：

~~~powershell
.\scripts\publish.ps1 -ApiBaseUrl "https://api.example.com/api/v1"
~~~

脚本会编译管理后台、发布后端并在 `release/` 下生成目录和 `.zip` 压缩包。`release/` 与压缩包已加入 `.gitignore`，不会提交到 Git。完整部署步骤见 [发布说明](RELEASE.md)。

## 7. 常用验证

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

## 8. 常见问题

| 现象 | 排查 |
| --- | --- |
| /health 404 | 正确地址是 /api/v1/health |
| Swagger 打不开 | 确认 API 进程、端口 5180 和 swagger/v1/swagger.json |
| 小程序请求失败 | 检查 API 基地址、域名校验和后端 CORS |
| 页面使用旧数据 | 清理小程序缓存，检查 tcpms.* 本地存储 |
| 顶部或底部被遮挡 | 检查 ProtoHeader、.proto-page 和重复固定导航 |
| 图片不显示 | 检查 static/prototype 资源路径 |

## 9. 上线前清单

- 在微信公众平台、微信商户平台和高德开放平台填写生产配置，并完成真实支付、退款、回调联调。
- 接入手机号授权和隐私协议。
- 配置 HTTPS 合法域名、对象存储和图片审核。
- 完成 MySQL 迁移、备份、恢复和定时任务。
- 关闭生产环境 mock-pay 和不受控本地演示回退。
- 为 JWT、数据库、地图和支付凭证配置密钥托管。
- 通过 Swagger 导出并评审对外接口，再发布前端。
