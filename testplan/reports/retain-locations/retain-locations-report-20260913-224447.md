# Location retain report

- RunId: **LOC-20260913-224447**
- Time: 2026-09-13 22:44:58
- BaseUrl: http://localhost:5000
- Pack: all MaxLocations: 0 (0=all)
- PASS: **9** / FAIL: **0** / SKIP: **1**
- Policy: **KEEP**
- Location total: **271 -> 353** (delta 82)
- Artifacts: D:\Junheinrich\Junheinrich.Master\Seven.Master\testplan\reports\retain-locations\retain-locations-artifacts-20260913-224447.json

## UI search

1. Location search Stk.l (SRM Demo CoordPoint)
2. Location search Fw. / Fw.SW (Singapore slots)
3. Locations Stk.RECV-MAP / Fw.RECV-MAP
4. Aisles Stk.A1..Stk.A4, Fw.A1/Fw.A2
5. Layers Fw.L01 / Fw.L02
- Stock container MAP-LOC-TP-20260913-224447 (if TC-LOC-031 PASS)

## Samples

```json
{
    "Stacker":  [
                    "Stk.l101011",
                    "Stk.l101012",
                    "Stk.l101021",
                    "Stk.l101022",
                    "Stk.l101031"
                ],
    "FourWay":  [
                    "Fw.SW1_1Z001A0505_0505_0504_01.S1",
                    "Fw.SW1_1Z001A0505_0505_0504_01.S2",
                    "Fw.SW1_1Z001A0405_0405_0404_01.S1",
                    "Fw.SW1_1Z001A0405_0405_0404_01.S2",
                    "Fw.SW1_1Z001A0304_0304_0305_01.S1"
                ]
}
```

| CaseId | Status | Title | Detail |
|--------|--------|-------|--------|
| TC-LOC-000 | PASS | Login | LOC-20260913-224447 |
| TC-LOC-001 | PASS | Ensure warehouse RETAIN_WH | id=1 |
| TC-LOC-010 | PASS | Stacker zone + aisles | zone=2 aisles=4 |
| TC-LOC-012 | PASS | Depth pair sample in batch | d1=160 d2=160 |
| TC-LOC-011 | PASS | Seed SRM Demo locations | attempted=320 created=57 existed=263 fail=0 |
| TC-LOC-020 | PASS | FourWay zone/layer/aisle | layers=2 |
| TC-LOC-021 | PASS | Seed Singapore FW locations | attempted=24 created=24 existed=0 fail=0 |
| TC-LOC-030 | PASS | WmsLocation total probe | before=271 after=353 delta=82 |
| TC-LOC-031 | SKIP | Seed stock on map recv KEEP | receive biz:仅已审核或执行中的入库单可收货组盘 |
| TC-LOC-040 | PASS | UI checklist written | see report |

## Note

- Earlier retain/coverage scripts only wrote a few locations; this run seeds map fixtures.
- Sources: 20260426-SRM-Demo / 20260619-singapore -> testplan/fixtures/*
