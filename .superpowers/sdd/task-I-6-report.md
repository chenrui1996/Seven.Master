# Task I-6 Report: Phase I 文档锚点

**Status:** DONE  
**Date:** 2026-08-30  
**Commits:** none (per task constraint)

## Summary

Verified `SimulationController` routes against `doc/21` and `Seven.Simulator/README.md`. Removed stale Phase I delivery placeholders; synced API tables, request bodies, and Phase I gate note.

## Controller routes (source of truth)

`Seven.WebApi/Controllers/Simulator/SimulationController.cs` — `[Route("api/simulation")]`:

| Method | Route | Action |
|--------|-------|--------|
| POST | `projects/validate-features` | ValidateFeatures |
| POST | `deploy` | Deploy |
| POST | `undeploy` | Undeploy |
| POST | `reset` | Reset |
| GET | `deployments` | List |
| POST | `promote-preview` | PromotePreview |
| POST | `promote` | Promote |

Request records: `ResetRequest(projectName)`, `UndeployRequest(projectName, removeLocations?)`, `SimPromoteRequest(projectName, devices[])`.

## Doc changes

### `doc/21-仿真器与联调闭环.md`

- §4: Added **Phase I 完成门禁** paragraph (Deploy → Trigger → Promote preview rejects loopback).
- §5: Annotated controller source; removed「一期交付」/「（一期）」placeholders on reset/promote rows.
- §5: Aligned descriptions with README (reset keeps SIM_ data; promote writes CommConnection).
- §5.1: Added request-body table for undeploy / reset / promote-preview / promote.
- §5: Marked Trigger routes as JWT-required.

### `Seven.Simulator/README.md`

- API section: Added controller source + link to doc/21 §5.
- Synced undeploy wording; added Phase I request-body one-liner.
- Table already matched routes; no structural changes needed.

## Stale text removed

| Location | Before | After |
|----------|--------|-------|
| doc/21 §5 reset | 清运行态（一期交付） | 清运行态，保留 `SIM_` 主数据 |
| doc/21 §5 promote-preview | Promote 预览（一期） | Promote 预览（拒环回地址） |
| doc/21 §5 promote | 切换生产配置（一期） | 写入 CommConnection + 标记 Promoted |

No「待实现/占位」text found in target files for Reset/Promote.

## Verification

- Manual diff: all 7 simulation routes in both docs match controller attributes.
- Frontend callers (`Player.vue`, `Promote.vue`, `MapEditor.vue`) use same paths and bodies documented in §5.1.

## Out of Scope

- Git commit
- Spec/plan doc updates (`2026-08-29-seven-simulator-design.md` still says「Reset 一期可后置」— not in task scope)
- Code changes

## Concerns

None.
