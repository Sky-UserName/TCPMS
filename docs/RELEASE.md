# TCPMS 发布说明

## 1. 生成发布包

在项目根目录执行：

~~~powershell
.\scripts\publish.ps1 -ApiBaseUrl "https://api.example.com/api/v1"
~~~

`ApiBaseUrl` 会在编译管理后台时写入前端资源。脚本会执行 `npm ci`、编译管理后台、发布 ASP.NET Core 后端，并生成：

~~~text
release/
└─ TCPMS-release-<时间戳>/
   ├─ admin/       管理后台静态文件
   ├─ backend/     ASP.NET Core 发布文件
   └─ RELEASE.md   本说明
~~~

同时会生成同名 `.zip` 压缩包。`release/` 和 `*.zip` 已加入 `.gitignore`，不会提交到 Git。

## 2. 配置后端

发布包中的 `backend/appsettings.Production.json` 默认开启 Swagger，但数据库连接和 JWT 密钥必须通过环境变量或服务器配置注入：

~~~powershell
cd .\backend
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__Default = "Server=127.0.0.1;Port=3306;Database=tcpms;User=tcpms;Password=替换为生产密码;CharSet=utf8mb4"
$env:Jwt__SecretKey = "替换为至少 32 字节的生产密钥"
$env:Seed__AdminPassword = "首次部署时使用的管理员初始口令"
$env:Cors__AllowedOrigins__0 = "https://admin.example.com"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5180"
dotnet .\TCPMS.Api.dll
~~~

`Cors__AllowedOrigins__0/1/...` 是管理后台域名白名单，生产不配置时后端只允许同源访问。`Seed__AdminPassword` 只在首次建库时创建 `admin` 账号时生效，且该账号首次登录会被强制改密。

已有数据库升级到本版本前，先执行 `docs/sql/2026-10-25_admin_password_policy.sql` 补上管理员密码策略字段。

如果使用 IIS、Nginx 或 systemd，请将以上配置转换为对应的应用环境变量或服务配置，不要把真实密码和 JWT 密钥写入 Git。

## 3. Swagger 地址

生产环境默认关闭 Swagger（`Swagger:Enabled=false`），根路径 `/` 也不再跳转。需要临时查看时显式打开：

~~~powershell
$env:Swagger__Enabled = "true"
~~~

打开后：

- Swagger UI：`http://<API域名>/swagger`
- OpenAPI JSON：`http://<API域名>/swagger/v1/swagger.json`

管理后台调用的 API 地址由 `-ApiBaseUrl` 在构建时确定。

## 4. 部署管理后台

将发布包中的 `admin/` 目录部署到 Nginx、IIS、Apache 或其他静态文件服务器，并配置单页应用回退到 `index.html`。管理后台和 API 使用不同域名时，确认后端 CORS、HTTPS 和反向代理配置允许该管理后台域名访问。

发布前建议验证：

~~~powershell
Invoke-WebRequest https://api.example.com/api/v1/health
Invoke-WebRequest https://api.example.com/swagger/v1/swagger.json
~~~
