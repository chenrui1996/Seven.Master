# Task I-1 Review: 扩展工程 schema 与 DTO

**Reviewer:** Code review subagent  
**Date:** 2026-08-29  
**Scope:** Spec compliance + code quality (read-only; diff verified)

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance** | ✅ (minor gaps noted below) |
| **Task quality** | **Approved** |

---

## Spec Compliance Checklist

| Requirement | Status | Notes |
|-------------|--------|-------|
| Step 1: `schema.ts` 定义 TS 接口，`project.ts` 引用 | ✅ | 新文件 `Seven.Simulator/src/lib/project/schema.ts`；store 已 refactor |
| Step 2: 扩展 `SimProjectDto` / `SimMapDto` | ✅ | `RequestPoints`、`Scada`、`Promote`（`devices`）、`SimCommsMode` 均已添加 |
| `SimFeaturesDto` 保持兼容 | ✅ | record 未改动；现有构造调用仍有效 |
| Step 3: 最小 JSON + requestPoints 测试 | ✅ | `SimProjectDtoTests.cs` 含 2 个 Fact |
| Step 4: `dotnet test --filter FullyQualifiedName~SimProject` | ✅ (report) | 未重跑；测试代码与断言与 diff 一致 |
| 不修改 Deploy 逻辑 | ✅ | `SimulationDeployService.cs` 不在 diff 中 |
| 无 RCS/LES 程序集引用 | ✅ | 变更文件内无引用 |
| 一期不加 three.js | ✅ | 无 three.js 依赖或代码 |
| 禁止 git commit | ✅ | working tree only |

### Spec §3 字段对齐

| Field | TS | C# | Match |
|-------|----|----|-------|
| `meta.simCommsMode` | `'Trigger' \| 'Gateway'` | `string SimCommsMode = "Trigger"` | ✅ |
| `map.requestPoints` | `{ code, mappedLocationCode }[]` | `SimMapRequestPointDto` | ✅ |
| `scada.views` | `SimScadaView[]`（默认 `[]`） | `SimScadaDto.Views?` | ✅ |
| `promote.devices` | `{ code, host, port, protocol }[]` | `SimPromoteDeviceDto` | ✅ |
| `map` nodes/edges/devices 保留 | ✅ | ✅ | ✅ |
| canvas/layers（可选一期） | 未建模 | 未建模 | ✅ 符合 brief「可选」 |

---

## Findings

### Critical

*None.*

### Important

1. **TS 导入/持久化未合并缺省字段** — `project.ts` 中 `importProject` 仍为 `JSON.parse(text) as SimProject`，Pinia `persist: true` 下旧本地状态或旧版 `.sevenproj.json` 不会自动补齐 `simCommsMode`、`requestPoints`、`scada`、`promote`。后续 UI 绑定这些字段时可能出现 `undefined` 访问。建议在 I-2 前增加 `mergeWithDefaults(parsed)` 或在 persist 迁移中处理（非 I-1 硬性要求，但应跟踪）。

2. **Brief 措辞 vs C# 反序列化语义** — Brief 写「缺省集合用空列表」，实现与既有 `Edges`/`Devices` 一致：缺失时反序列化为 `null`（测试亦断言 `BeNull()`）。向后兼容反序列化已通过；I-2 Deploy 需按 report 所述将 `null` 当空集合处理。非阻塞，但应在 I-2 brief 中显式约定。

### Minor

1. **`SimScadaViewDto` 额外字段** — 增加了 `Name`、`Width`、`Height`，spec §3 示例仅展示空 `views` 数组。属于合理前瞻扩展，与 `promote.devices` 结构一致，无冲突。

2. **测试覆盖偏最小** — 第二则测试对 `scada.views` / `promote.devices` 仅断言空数组，未覆盖含元素的 promote device 或 scada view。满足 brief Step 3 要求，后续可加 round-trip 用例。

3. **`MapEditor.clearMap()` 未清空 `requestPoints`** — 与本次 schema 扩展相关的小遗漏；当前 MapEditor 未编辑 requestPoints，影响有限。

4. **Deploy POST 仍只发送 `{ version, meta, map }`** — 符合 I-1 范围（不扩展 Deploy）；`scada`/`promote` 将在后续任务接入。

---

## Code Quality Notes

**Strengths**

- TS schema 与 C# DTO 命名/结构清晰，camelCase JSON 与 ASP.NET 默认策略一致。
- `defaultSimProject()` 集中默认值，store 瘦身合理；类型 re-export 保持现有 import 路径兼容。
- 新 DTO 以 optional trailing record 参数扩展，不破坏 `SimulationDeployServiceTests.SampleProject()` 等既有构造。
- 测试 JSON 贴近真实工程文件，断言明确。

**Constraints verified**

- 变更文件：`ISimulationDeployService.cs`、`project.ts`、`schema.ts`（新）、`SimProjectDtoTests.cs`（新）。
- 无 Deploy 实现、无 three.js、无 RCS/LES 引用、无 commit。

---

## Recommendation

**Approve Task I-1.** 实现满足 brief 与 spec §3 一期范围；Important 项为后续任务跟进项，不构成本任务返工条件。

**Suggested follow-ups (I-2 prep):**

- Deploy 层：`null` → 空集合归一化。
- 前端：`importProject` / persist 迁移合并 `defaultSimProject()`。
- 可选：补充 promote/scada 非空元素反序列化测试。
