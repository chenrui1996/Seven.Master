# Location retain report

- RunId: **LOC-20260913-224343**
- Time: 2026-09-13 22:43:57
- BaseUrl: http://localhost:5000
- Pack: all MaxLocations: 0 (0=all)
- PASS: **6** / FAIL: **3** / SKIP: **1**
- Policy: **KEEP**
- Location total: **7 -> -1** (delta -8)
- Artifacts: D:\Junheinrich\Junheinrich.Master\Seven.Master\testplan\reports\retain-locations\retain-locations-artifacts-20260913-224343.json

## UI search

1. Location search Stk.l (SRM Demo CoordPoint)
2. Location search Fw. / Fw.SW (Singapore slots)
3. Locations Stk.RECV-MAP / Fw.RECV-MAP
4. Aisles Stk.A1..Stk.A4, Fw.A1/Fw.A2
5. Layers Fw.L01 / Fw.L02
- Stock container MAP-LOC-TP-20260913-224343 (if TC-LOC-031 PASS)

## Samples

```json
{
    "Stacker":  [
                    "Stk.l101011",
                    "Stk.l101012",
                    "Stk.l101021",
                    "Stk.l101022",
                    "Stk.l101031"
                ]
}
```

| CaseId | Status | Title | Detail |
|--------|--------|-------|--------|
| TC-LOC-000 | PASS | Login | LOC-20260913-224343 |
| TC-LOC-001 | PASS | Ensure warehouse RETAIN_WH | id=1 |
| TC-LOC-010 | PASS | Stacker zone + aisles | zone=2 aisles=4 |
| TC-LOC-012 | PASS | Depth pair sample in batch | d1=132 d2=131 |
| TC-LOC-011 | PASS | Seed SRM Demo locations | attempted=320 created=263 existed=0 fail=57 |
| TC-LOC-020 | FAIL | FourWay zone/layer/aisle | add /api/WmsLayer Fw.L02 : HTTP 429  |
| TC-LOC-021 | FAIL | Seed Singapore FW locations | add /api/WmsLayer Fw.L02 : HTTP 429  |
| TC-LOC-030 | FAIL | WmsLocation total probe | after=-1 baseline=50 |
| TC-LOC-031 | SKIP | Seed stock on map recv KEEP | inbound create failed:  |
| TC-LOC-040 | PASS | UI checklist written | see report |

## Note

- Earlier retain/coverage scripts only wrote a few locations; this run seeds map fixtures.
- Sources: 20260426-SRM-Demo / 20260619-singapore -> testplan/fixtures/*
