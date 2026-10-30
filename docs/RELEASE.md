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

### 2.1 生产环境读哪份配置

`ASPNETCORE_ENVIRONMENT` 决定加载哪些文件，按顺序叠加，后者覆盖前者：

| 顺序 | 文件 | 生产是否加载 | 内容定位 |
| --- | --- | --- | --- |
| 1 | `appsettings.json` | 加载 | 公共项 + 微信商户号/AppId/ApiV3Key 等已有值 |
| 2 | `appsettings.Production.json` | `Production` 时加载 | 把数据库、JWT、高德、CORS 置空，强制从环境变量注入 |
| 3 | 环境变量 | 加载 | 最终覆盖，密码和密钥只放这里 |

`appsettings.Development.json` **在生产完全不加载**。所以启动命令里必须显式带上 `ASPNETCORE_ENVIRONMENT=Production`；写成 `Development` 会加载本机证书路径、放开任意来源 CORS，并开放仅开发可用的模拟支付接口 `/orders/{id}/mock-pay`。

`appsettings.Production.json` 里 `ConnectionStrings:Default` 和 `Jwt:SecretKey` 是空字符串，不注入环境变量会**启动即失败**（`Jwt:SecretKey 至少需要 32 字节`），这是刻意设计，避免误用开发密钥上线。

### 2.2 必须配置的项

| 环境变量 | 说明 | 不配置的后果 |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | 固定 `Production` | 走开发分支：放开 CORS、开模拟支付 |
| `ASPNETCORE_URLS` | 建议 `http://127.0.0.1:5180`，只给 Nginx 反代 | 默认监听 5000，与 Nginx 对不上 |
| `ConnectionStrings__Default` | MySQL 连接串，`CharSet=utf8mb4` 必带 | 启动抛异常 |
| `Jwt__SecretKey` | 至少 32 字节随机串，泄露等于可伪造管理端令牌 | 启动抛异常 |
| `Amap__ServerKey` | 高德 Web 服务 Key，服务端算距离用 | 附近门店/距离排序返回 503 |
| `Amap__MiniProgramKey` | 高德小程序 Key，随门店定位设置下发 | 小程序地图选点不可用 |
| `WeChat__MiniProgramAppId` | 小程序 AppId | 微信登录失败 |
| `WeChat__MiniProgramSecret` | 小程序 AppSecret | 拿不到 openid，登录 400 |
| `WeChat__MerchantId` | 微信支付商户号 | 支付/退款 503 |
| `WeChat__MerchantSerialNumber` | 商户证书序列号 | 支付/退款 503 |
| `WeChat__ApiV3Key` | 必须正好 32 字节 | 支付/退款 503 |
| `WeChat__MerchantPrivateKeyPath` | 服务器上 `apiclient_key.pem` 的绝对路径 | 支付/退款 503 |
| `WeChat__WeChatPayPublicKeyId` | `PUB_KEY_ID_...` | 回调验签失败 |
| `WeChat__WeChatPayPublicKeyPath` | 服务器上 `pub_key.pem` 的绝对路径 | 回调验签失败 |
| `WeChat__NotifyBaseUrl` | 公网 HTTPS 地址，如 `https://api.example.com` | 支付下单抛「回调地址未配置」 |
| `Seed__DemoData` | 生产写 `false`（代码默认已是 false，显式写上更保险） | 空库会自动灌演示门店/房型/订单 |
| `Seed__AdminPassword` | 首次建库时的 `admin` 初始口令 | 退回内置口令，且首次登录强制改密 |

可选：

| 环境变量 | 说明 |
| --- | --- |
| `Cors__AllowedOrigins__0`、`__1`… | 管理后台与 API 不同域名时必填，写管理后台完整域名；同域名反代部署无需配置，不配时后端只允许同源 |
| `Swagger__Enabled` | 默认 `false`，排障时临时设 `true` |
| `Jwt__Issuer` / `Jwt__Audience` | 默认 `TCPMS` / `TCPMS.Client`，管理端和小程序端共用，**已上线的库不要改**，否则所有旧令牌失效 |

微信支付的 AppId、商户号、证书序列号、ApiV3Key 在 `appsettings.json` 里已有当前商户的值，同一商户可沿用；建议仍用环境变量注入，别让密钥留在发布包里。

`NotifyBaseUrl` 只需给到域名，后端会自己拼成 `https://域名/api/v1/payments/wechat/notify`；写全路径或写到 `/api/v1` 也能识别。

### 2.3 环境变量文件（systemd 示例）

`/etc/tcpms/api.env`，`chmod 600`、属主为运行用户：

~~~ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5180
ConnectionStrings__Default=Server=127.0.0.1;Port=3306;Database=tcpms;User=tcpms;Password=替换为生产密码;CharSet=utf8mb4
Jwt__SecretKey=替换为至少32字节的随机串
Amap__ServerKey=替换为高德服务端Key
Amap__MiniProgramKey=替换为高德小程序Key
WeChat__MiniProgramAppId=wx5c2bb8ff3259ff29
WeChat__MiniProgramSecret=替换为小程序Secret
WeChat__MerchantId=1751682085
WeChat__MerchantSerialNumber=替换为证书序列号
WeChat__ApiV3Key=替换为32位APIv3Key
WeChat__MerchantPrivateKeyPath=/opt/tcpms/certs/apiclient_key.pem
WeChat__WeChatPayPublicKeyId=PUB_KEY_ID_替换
WeChat__WeChatPayPublicKeyPath=/opt/tcpms/certs/pub_key.pem
WeChat__NotifyBaseUrl=https://api.example.com
Seed__DemoData=false
Seed__AdminPassword=替换为首次登录口令
Cors__AllowedOrigins__0=https://admin.example.com
~~~

systemd 单元里用 `EnvironmentFile=/etc/tcpms/api.env`、`WorkingDirectory=/opt/tcpms/backend`、`ExecStart=/usr/bin/dotnet /opt/tcpms/backend/TCPMS.Api.dll`。证书文件放 `/opt/tcpms/certs/`，`chmod 600`，运行用户必须可读。

### 2.4 数据库准备

首次部署先建库建账号（`utf8mb4`），后端启动时 `EnsureCreated` 自动建表并写入角色、菜单、字典、订阅消息模板等基础数据；空库且 `Seed:DemoData=false` 时不会灌业务演示数据。

已有数据库升级到本版本前，先执行 `docs/sql/2026-10-25_admin_password_policy.sql` 补上管理员密码策略字段。`EnsureCreated` 只建新表、不改已有表结构，后续加字段要靠 `docs/sql/` 下的脚本。

### 2.5 上传目录要固化

上传图片落在 `wwwroot/uploads/`（品牌图在 `wwwroot/brand/`，随包发布）。解压新版本前先把旧的 `wwwroot/uploads` 挪过去，或把 `wwwroot/uploads` 做成指向固定目录的软链接，否则所有门店、房型、评价、头像图片会 404。

### 2.6 Nginx 反代

~~~nginx
server {
    listen 443 ssl http2;
    server_name api.example.com;
    ssl_certificate     /etc/nginx/ssl/api.example.com.pem;
    ssl_certificate_key /etc/nginx/ssl/api.example.com.key;

    client_max_body_size 20m;

    location / {
        proxy_pass http://127.0.0.1:5180;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
}
~~~

后端没有启用 `UseHttpsRedirection`，Nginx 终止 TLS 后走内网 HTTP 不会出现重定向循环。管理后台如果也想同域部署，加一个 `root /opt/tcpms/admin; try_files $uri $uri/ /index.html;` 的 server 块并删掉 `Cors__AllowedOrigins__*`（同源即可）。

### 2.7 上线自检

~~~bash
curl https://api.example.com/api/v1/health
# {"status":"ok","service":"TCPMS.Api","environment":"Production","databaseProvider":"MySql",...}
curl -I https://api.example.com/brand/brand-mark.jpg   # 200
curl -I https://api.example.com/swagger/index.html     # 404（生产默认关闭）
~~~

`environment` 必须显示 `Production`。接着登录管理后台，用 `/api/v1/admin/integrations` 一次性核对高德和微信支付配置有没有漏项，`missing` 必须是空数组：

~~~bash
curl -H "Authorization: Bearer <管理端登录拿到的token>" \
  https://api.example.com/api/v1/admin/integrations
~~~

~~~json
{
  "environment": "Production",
  "wechatPay": {
    "configured": true,
    "notifyUrl": "https://api.example.com/api/v1/payments/wechat/notify",
    "hasApiV3Key": true,
    "hasMerchantPrivateKey": true,
    "hasWeChatPayPublicKey": true,
    "missing": []
  },
  "amap": { "configured": true, "hasServerKey": true, "hasMiniProgramKey": true }
}
~~~

最后走一遍业务闭环：改密 → 门店 → 房型 → 价格日历 → 下单一笔小额支付 → 退款，确认 `/api/v1/payments/wechat/notify` 能被微信回调到（该地址必须公网可访问、HTTPS 且不带端口）。

## 3. 品牌图与商家上传图

品牌 logo、登录页图片和首页大图放在后端的 `wwwroot/brand/`，由 `dotnet publish` 一起打进发布包，访问路径为 `https://<API域名>/brand/...`；商家上传的图片落在 `wwwroot/uploads/`。

- 前端只引用 URL，不再依赖小程序包里的图片文件，重新打包/重新发版不会丢 logo。
- 升级部署时务必保留服务器上的 `wwwroot/uploads/` 目录，否则已上传的门店、房型、评价图片会 404。
- 小程序包里仍保留同名文件作兜底：后端图片加载失败时 `<image>` 自动切回包内图片。
- 检查：`Invoke-WebRequest https://api.example.com/brand/brand-mark.jpg` 应返回 200。

## 4. Swagger 地址

生产环境默认关闭 Swagger（`Swagger:Enabled=false`），根路径 `/` 也不再跳转。需要临时查看时显式打开：

~~~powershell
$env:Swagger__Enabled = "true"
~~~

打开后：

- Swagger UI：`http://<API域名>/swagger`
- OpenAPI JSON：`http://<API域名>/swagger/v1/swagger.json`

管理后台调用的 API 地址由 `-ApiBaseUrl` 在构建时确定。

## 5. 部署管理后台

将发布包中的 `admin/` 目录部署到 Nginx、IIS、Apache 或其他静态文件服务器，并配置单页应用回退到 `index.html`。管理后台和 API 使用不同域名时，确认后端 CORS、HTTPS 和反向代理配置允许该管理后台域名访问。

发布前建议验证：

~~~powershell
Invoke-WebRequest https://api.example.com/api/v1/health
Invoke-WebRequest https://api.example.com/swagger/v1/swagger.json
~~~
