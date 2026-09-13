# ApiHttp 测试报告

- 时间: 2026-09-13 19:42:50
- BaseUrl: http://localhost:5000
- 通过: **29** / 失败: **0** / 跳过: **1** / 合计: 30

| CaseId | Status | Title | Detail |
|--------|--------|-------|--------|
| TC-AUTH-API-001 | PASS | Health 探针 | Seven API is running |
| TC-AUTH-API-002 | PASS | Captcha/create 返回 key+code | key=bc551abcf5f140fcb44524a556ff4fd6 code=5138 |
| TC-AUTH-API-003 | PASS | 登录 admin+验证码 | token length=372 |
| TC-AUTH-API-004 | PASS | 错误密码拒绝 | 用户名或密码错误 |
| TC-X-013 | PASS | Features API 与期望包开关 | {"workFlow":true,"quartz":false,"signalR":true,"alarm":true,"messageQueue":false,"outbox":false,"mail":false,"minIO":false,"tenant":false,"captcha":true,"rateLimit":true,"idempotency":true,"dataScope":true,"auditInterceptor":true,"builder":true,"hotStore":false,"deviceComm":false,"simulator":true,"wms":true,"orchestrationBus":true,"wcsPacks":{"stacker":true,"fourWay":true,"boxSort":true}} |
| TC-X-012 | PASS | Dual：双边运维 + 无执行运维(getMenu) | ops folders present; 执行运维 hidden |
| TC-OPS-FW-020 | PASS | 五页菜单可见(API) | FwOps* present |
| TC-OPS-STK-008 | PASS | 运维四页菜单可见(API) | StkOps* present |
| TC-X-040 | PASS | DbSeeder 退役执行运维(enable=0) | rows=1 |
| TC-WMS-041 | PASS | 接口日志菜单在系统管理 | parentId=19 |
| TC-X-041 | PASS | 权限码 Search 可进运维页 | all Search perms present |
| TC-OPS-FW-001 | PASS | meta 与 canAcceptLegs | HTTP 200 |
| TC-OPS-FW-002 | PASS | board 活动集 | HTTP 200 |
| TC-OPS-FW-006 | PASS | 指定货位 map | HTTP 200 |
| TC-OPS-FW-013 | PASS | 联锁 scope=FourWay GET | HTTP 200 |
| TC-OPS-FW-003 | PASS | task-tree 无参返回业务错误 | 未找到任务树 |
| TC-OPS-FW-010 | PASS | 强制完成空 GUID 拒批 | Shuttle 不存在 |
| TC-X-021 | PASS | 包专属模式可写 FourWay | set Manual(1) then restore 0 |
| TC-X-020 | PASS | 仓级急停可开关(经 FourWay control-mode) | toggled globalEStop |
| TC-OPS-STK-001 | PASS | board 返回活动任务 | HTTP 200 |
| TC-OPS-STK-002 | PASS | task-tree 可达 | 未找到任务树 |
| TC-OPS-STK-006 | PASS | 申请点列表 | HTTP 200 |
| TC-OPS-STK-007 | PASS | 联锁 GET Stacker | HTTP 200 |
| TC-OPS-STK-003 | PASS | 强制完成空 GUID 拒批 | DeviceTask 不存在 |
| TC-OPS-STK-006b | SKIP | 申请点停用/启用往返 | no request points in DB |
| TC-WMS-API-001 | PASS | 入库单分页 | HTTP 200 |
| TC-WMS-API-002 | PASS | 出库单分页 | HTTP 200 |
| TC-WMS-API-003 | PASS | 盘点单分页 | HTTP 200 |
| TC-WMS-API-004 | PASS | 库存分页 | HTTP 200 |
| TC-AUTH-API-005 | PASS | Ops 无 Token → 401 | 401 |
