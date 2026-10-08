# 交接手册

## 1. 新成员第一天

1. 阅读项目总览和业务规则。
2. 启动后端，打开 Swagger，确认 /api/v1/health、登录和门店接口。
3. 使用 HBuilderX 打开 src/TCPMS.User，运行小程序。
4. 查看 pages.json、common/api.js、common/app-store.js 和 ProtoBottomNav.vue。
5. 再阅读负责页面的 vue 文件及其 API 封装。

## 2. 修改页面前的检查

- 页面是否已注册在 pages.json？
- 是否复用了 ProtoHeader 和 ProtoBottomNav？
- 根节点是否有 .proto-page，是否预留安全区？
- 有数据、无数据、加载中、失败和重复提交状态是否完整？
- 是否通过 common/api.js 调用接口？
- 是否需要同步本地 app-store.js 缓存？
- 页面视觉是否仍使用蓝绿色变量？

## 3. 修改接口前的检查

- 路由是否存在于 /swagger/v1/swagger.json？
- DTO 是否需要兼容旧字段？
- 金额单位是元还是分？
- 日期是本地日期还是带时区时间？
- 写操作是否会改变库存、订单或退款状态？
- 是否需要 JWT、管理员角色或门店范围？
- 是否需要新增审计日志？

最小验证：

~~~powershell
dotnet build src/backend/TCPMS.Api/TCPMS.Api.csproj
dotnet run --project src/backend/TCPMS.Api --urls http://localhost:5180
Invoke-WebRequest http://localhost:5180/swagger/v1/swagger.json
~~~

## 4. 当前已知占位

| 项目 | 现状 |
| --- | --- |
| 数据库 | 开发默认 InMemory；生产需 MySQL 迁移和备份 |
| 登录 | 开发可用 devOpenId；生产需微信登录和手机号授权 |
| 支付 | 仅 mock-pay 联调；生产需微信支付和回调幂等 |
| 图片 | 当前含原型演示资源；生产需对象存储和审核 |
| 地图 | 需要配置高德服务端 Key |
| 消息 | 当前为页面展示入口；订阅消息和推送任务需继续接入 |
| 定时任务 | 订单超时释放、通知重试和监控 Worker 尚未完成 |
| 观测 | 需补充统一日志、traceId、错误告警和指标 |

## 5. 发布流程

1. 后端编译和接口回归通过。
2. Swagger JSON 导出并和前端确认破坏性变更。
3. 管理后台执行 npm run build。
4. 小程序检查真机安全区、导航栏、空状态和支付前后流程。
5. 更新本目录对应文档和实现状态。
6. 配置生产环境变量、数据库迁移、HTTPS 域名和密钥。

## 6. 术语

- store：门店。
- room type：房型或床型可售商品。
- room unit：具体房间或床位资源。
- inventory：按日期记录的总量、锁定量、已售量和可用量。
- mock-pay：仅开发环境使用的模拟支付。
- API fallback：后端不可用时用户端读取本地演示数据的开发机制。

