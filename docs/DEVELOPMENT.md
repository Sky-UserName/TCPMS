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
4. 开发环境默认请求 `http://localhost:5180/api/v1`，地址由 `src/TCPMS.User/.env.development`（本机）和 `.env.production`（发行）控制；也可以在设备存储里设置 `tcpms.apiBaseUrl` 临时改地址。生产小程序须使用 HTTPS 并配置微信 request 合法域名。
5. 修改页面后重新编译，不要编辑 unpackage/dist。

**微信开发者工具要导入 `src/TCPMS.User/unpackage/dist/dev/mp-weixin`，不是 `src/TCPMS.User`。** 源码目录只有 `pages.json` 和 `manifest.json`，`app.json` 是编译产物；直接导入源码目录会报 `app.json is not found in the project root directory`。

没有 HBuilderX 图形界面（或在 CI 上）时用脚本编译，产物位置相同：

~~~
powershell -ExecutionPolicy Bypass -File scripts/build-miniapp.ps1            # 开发包，接口指向 localhost:5180
powershell -ExecutionPolicy Bypass -File scripts/build-miniapp.ps1 -Mode prod # 生产包，接口读 .env.production
~~~

脚本临时借用 HBuilderX 自带的 uni-app 编译器，编译后自动删除临时生成的 `package.json`、`vite.config.js` 和 `node_modules` 联接，不改变项目结构。HBuilderX 不在 `D:\SoftWare\HBuilderX` 时用 `-HBuilderXHome` 指定。

## 4.1 配置文件（不入库）

`appsettings*.json` 含数据库口令、JWT 密钥、微信 AppSecret / APIv3 Key，已从版本库移除并加入 `.gitignore`。克隆仓库后先复制模板再填值：

~~~powershell
cd src/backend/TCPMS.Api
Copy-Item appsettings.example.json appsettings.json
Copy-Item appsettings.Development.example.json appsettings.Development.json
~~~

占位符 `REPLACE_ME` 必须替换；生产环境建议改用环境变量注入，不落文件：

~~~powershell
$env:Jwt__SecretKey = "<32 位以上随机串>"
$env:ConnectionStrings__Default = "Server=...;Password=...;"
$env:WeChat__ApiV3Key = "..."
~~~

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

Swagger 默认关闭（`appsettings.json` 与生产配置均为 `Swagger:Enabled=false`），本地开发由 `appsettings.Development.json` 打开。需要临时查看时把 `Swagger__Enabled` 设为 `true`，接口文档地址为 `/swagger`，OpenAPI JSON 地址为 `/swagger/v1/swagger.json`。

### 5.1 微信支付与高德地图配置

管理后台“系统管理 > 应用中心”会读取服务端配置并显示脱敏状态，密钥不会写入后台前端或小程序包。配置可以写入 `appsettings.Production.json`，也可以使用同名环境变量：

| 配置 | 用途 |
| --- | --- |
| `WeChat__MiniProgramAppId` / `WeChat__MiniProgramSecret` | 微信小程序登录和支付使用的 AppID、AppSecret |
| `WeChat__MerchantId` | 微信支付商户号 |
| `WeChat__MerchantSerialNumber` | 商户 API 证书序列号 |
| `WeChat__ApiV3Key` | 微信支付 API v3 Key，必须是 32 字节 |
| `WeChat__MerchantPrivateKeyPath` | `apiclient_key.pem` 的服务端文件路径；也可直接填写 `MerchantPrivateKeyPem` |
| `WeChat__PlatformCertificatePath` | 微信支付平台证书 PEM 文件路径；也可直接填写 `PlatformCertificatePem`。2024 年后新入驻商户通常没有平台证书，改用下面的微信支付公钥 |
| `WeChat__WeChatPayPublicKeyPath` | 商户平台下载的 `pub_key.pem` 路径；也可直接填写 `WeChatPayPublicKeyPem` |
| `WeChat__WeChatPayPublicKeyId` | 微信支付公钥 ID，形如 `PUB_KEY_ID_...`；用于校验回调头 `Wechatpay-Serial` |
| `WeChat__NotifyBaseUrl` | 后端公网基地址，例如 `https://api.example.com`；服务会拼接支付回调路径 |
| `Amap__ServerKey` | 高德 Web 服务 API Key，用于地址解析、逆解析和门店坐标维护 |
| `Amap__MiniProgramKey` | 高德小程序端 Key，供小程序侧地图能力使用 |
| `Amap__DefaultRadiusKm` | 附近门店默认搜索半径，单位为公里 |

微信支付回调地址最终为 `/api/v1/payments/wechat/notify`，必须使用公网 HTTPS，并在微信商户平台配置为与服务端一致的地址。应用中心的“测试商户连接”会用一个不存在的订单号试调用微信查单接口：返回 404 `ORDER_NOT_EXIST` 说明商户号、证书序列号、私钥签名全部有效；返回 401 `SIGN_ERROR` 说明签名有误。之所以不再调用 `/v3/certificates`，是因为 2024 年后新入驻商户属于“微信支付公钥”模式，该接口会返回 `RESOURCE_NOT_EXISTS`。

服务端同时会校验微信应答的 `Wechatpay-Signature`，避免伪造的“查单成功”响应。小程序支付成功后还会调用 `GET /api/v1/orders/{id}/pay-status` 主动查单，后台另有一个每 60 秒运行一次的兜底任务，把回调丢失但微信侧已支付的订单补记为已支付。生产环境要求真实微信登录凭证，开发用 `devOpenId` 和 `mock-pay` 不会生效。

#### 小程序端地图

小程序里的地图底图由微信原生 `map` 组件渲染（腾讯瓦片），高德只提供逆地理编码、POI 检索这类数据能力，没有“把高德地图嵌进小程序”的渲染方案。因此两个 Key 的类型不能混用：

- `Amap__MiniProgramKey` 必须是“微信小程序”类型，并在高德控制台绑定小程序 AppID（`wx5c2bb8ff3259ff29`）。它会通过 `GET /api/v1/map/config` 下发给小程序端，属于公开信息。
- `Amap__ServerKey` 必须是“Web 服务”类型，只存在服务端，用于门店地址解析和坐标维护。

小程序端实现位于 `src/TCPMS.User/pages/store/map.vue` 和 `common/amap.js`。发布前还需要完成三件事：

1. 微信公众平台 > 开发管理 > 服务器域名，把 `https://restapi.amap.com` 加入 request 合法域名。开发者工具或真机调试时可以勾选“不校验合法域名”跳过。
2. 微信公众平台 > 设置 > 服务内容声明 > 用户隐私保护指引，声明“位置信息”。未声明时 `wx.getLocation` 会直接失败，页面只能显示门店但无法定位。
3. `manifest.json` 的 `mp-weixin` 节点已声明 `permission.scope.userLocation` 和 `requiredPrivateInfos: ["getLocation"]`，缺失会导致定位接口无响应。

若高德 Key 类型错误或未配置，地图仍可浏览门店标记，只是城市名和地点搜索不可用；控制台返回的 `info` 会显示在搜索面板里便于定位问题。

房型详情页 `pages/room/detail.vue` 的“周边位置”卡片同样用原生 `map` 渲染：默认定位门店坐标（后端没存坐标时用 `v3/geocode/geo` 按地址兜底反查），再用 `v3/place/around` 拉取 1.2km 内的餐饮、购物、风景、地铁、公交、医疗 POI，作为地图标记和横向标签；点标签或标记会把视野移过去。地图本身支持拖动和双指缩放，点地址行会调起 `wx.openLocation` 打开全屏地图并可直接导航。

### 5.2 没有公网域名时怎么联调

回调地址只是下单参数，微信不会校验可达性，因此本地可以先填占位地址：

- `appsettings.Development.json` 里 `WeChat:NotifyBaseUrl` 已设为 `https://example.com`，本地可以正常拉起支付，回调收不到，但 `pay-status` 查询与兜底任务会把订单补成已支付。
- 真机预览时手机访问不到 `localhost`：后端需监听 `0.0.0.0`（开发配置已改为 `http://0.0.0.0:5180`），小程序端用 `VITE_API_BASE_URL=http://<开发机局域网IP>:5180/api/v1` 构建，并在开发者工具里勾选“不校验合法域名”。
- 域名下来后，只需把 `WeChat:NotifyBaseUrl` 改成真实域名、在小程序后台把域名加入 request 合法域名，其余代码不用动。

### 5.3 本地样例数据与联调账号

开发环境默认使用 InMemory 数据库，**每次重启后端都会重新初始化数据**，不需要手工清库。种子逻辑分两类：

- 配置数据（始终存在）：地区、角色、权限菜单、筛选项字典、订阅消息模板。
- 样例业务数据（空库且 `Seed:DemoData` 为 true 时写入）：2 家门店、4 个房型（含 1 个待审核）、2 个用户、4 张订单（待支付 / 已支付待入住 / 已完成带评价 / 退款审核中）、1 个门店管理员。

本地联调账号：

| 角色 | 账号 | 密码 |
| --- | --- | --- |
| 总部管理员 | `admin` | `Admin@123456`（首次登录强制改密） |
| 门店管理员 | `shanghai_admin` | 同上 |
| 小程序用户 | 无需输入 | 开发环境默认用 `dev-user-001` 登录 |

小程序本地联调要点：

- 接口地址：开发构建读 `.env.development` → `http://localhost:5180/api/v1`，发行构建读 `.env.production`。识别不出构建环境时按开发处理，避免误连线上。
- 开发态默认跳过微信 code 换 openId，直接用 `dev-user-001` 登录，保证样例订单可复现。需要验证真实微信登录时，在开发者工具控制台执行 `uni.setStorageSync('tcpms.devUseWechatLogin', true)`。
- 微信支付未配置时，支付页会自动走 `/orders/{id}/mock-pay`（仅后端 Development 环境存在），把订单真实标记为已支付。该行为由 `VITE_ALLOW_MOCK_PAY` 控制，开发默认开启，生产包恒为关闭。
- 后端重启后样例订单会重新生成，小程序本地缓存里的旧订单需要在开发者工具里「清缓存 > 清除数据」清掉；开发环境的「我的」页面也有「清除本地缓存」入口，效果等同。

如果本地改用 MySQL（`Database:Provider=MySql`），需要手工清库时执行：

~~~powershell
mysql -h 127.0.0.1 -u <user> -p <database> < docs/sql/2026-10-25_reset_local_data.sql
~~~

脚本只保留 `Regions`，其余表全部清空；重启后端会自动补齐角色、管理员和样例数据。生产库不要执行。

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

## 10. 生产环境安全默认值

这一版把几处「开发方便、上线危险」的默认行为改掉了，部署时注意对应配置：

| 项 | 现在的行为 | 需要做什么 |
| --- | --- | --- |
| 首次登录 | 种子管理员和超管重置过密码的账号带 `MustChangePassword`，不改密无法调用管理接口（返回 403 `password_change_required`） | 无，登录页会自动进入改密步骤 |
| 初始管理员口令 | 可用 `Seed:AdminPassword` 覆盖，未配置时用内置默认值 | 生产建议显式配置，避免沿用默认口令 |
| 登录失败 | 同一账号或同一 IP 10 分钟内失败 5 次锁定 10 分钟，返回 429 `login_locked` | 无 |
| 未支付订单 | 每分钟扫描一次，超过 `PaymentExpiresAt` 的订单自动取消并回滚锁定库存 | 无，多实例部署时依赖状态判断保证幂等 |
| CORS | 开发放开；生产只允许 `Cors:AllowedOrigins` 中的来源，未配置则只允许同源 | 生产必须配置管理后台域名 |
| Swagger | 生产默认关闭（`Swagger:Enabled=false`） | 需要临时查看时用环境变量打开 |
| 演示数据 | 小程序 `VITE_ALLOW_DEMO` 控制，生产构建默认关闭，接口失败展示空态 | 需要演示时打包加 `VITE_ALLOW_DEMO=true` |
| 管理后台接口地址 | 默认同源 `/api/v1`，开发由 vite 代理到 5180 | 分域名部署时用 `VITE_API_BASE_URL` 覆盖 |

数据库字段变更（`MustChangePassword`、`PasswordChangedAt`）需要先执行 `docs/sql/2026-10-25_admin_password_policy.sql`，因为项目用的是 `EnsureCreated`，已有库不会自动加列。

## 数据访问约定（手动事务）

Pomelo 在 `Program.cs` 里开了 `EnableRetryOnFailure(3)`，执行策略**不允许**业务代码直接 `BeginTransactionAsync`，否则一进事务就抛：

```
The configured execution strategy 'MySqlRetryingExecutionStrategy' does not support user-initiated transactions.
```

创建订单曾因此稳定返回 500。需要手动事务时统一按下面的写法（参考 `POST /api/v1/orders`）：

```csharp
var strategy = db.Database.CreateExecutionStrategy();
return await strategy.ExecuteAsync(async () =>
{
    db.ChangeTracker.Clear();               // 重试会重跑委托，先清干净跟踪状态
    await using var tx = db.Database.IsRelational()
        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
        : null;
    var roomType = await db.RoomTypes.SingleOrDefaultAsync(...); // 事务内重新读取，别复用外层实体
    // ... 校验库存、改锁定数量、Add 订单、SaveChanges
    await tx.CommitAsync(cancellationToken);
    return Results.Created(...);
});
```

要点：所有查询、写入、提交都放在委托内；重试可能重复执行，因此业务幂等依赖唯一索引（如 `OrderNumber`）兜底。

