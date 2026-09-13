# Location retain report

- RunId: **LOC-20260913-224321**
- Time: 2026-09-13 22:43:23
- BaseUrl: http://localhost:5000
- Pack: all MaxLocations: 0 (0=all)
- PASS: **3** / FAIL: **5** / SKIP: **1**
- Policy: **KEEP**
- Location total: **7 -> 7** (delta 0)
- Artifacts: D:\Junheinrich\Junheinrich.Master\Seven.Master\testplan\reports\retain-locations\retain-locations-artifacts-20260913-224321.json

## UI search

1. Location search Stk.l (SRM Demo CoordPoint)
2. Location search Fw. / Fw.SW (Singapore slots)
3. Locations Stk.RECV-MAP / Fw.RECV-MAP
4. Aisles Stk.A1..Stk.A4, Fw.A1/Fw.A2
5. Layers Fw.L01 / Fw.L02
- Stock container MAP-LOC-TP-20260913-224321 (if TC-LOC-031 PASS)

## Samples

```json
{

}
```

| CaseId | Status | Title | Detail |
|--------|--------|-------|--------|
| TC-LOC-000 | PASS | Login | LOC-20260913-224321 |
| TC-LOC-001 | PASS | Ensure warehouse RETAIN_WH | id=1 |
| TC-LOC-010 | FAIL | Stacker zone + aisles | Method invocation failed because [System.String] does not contain a method named 'Add'. |
| TC-LOC-011 | FAIL | Seed SRM Demo locations | Method invocation failed because [System.String] does not contain a method named 'Add'. |
| TC-LOC-020 | FAIL | FourWay zone/layer/aisle | Method invocation failed because [System.String] does not contain a method named 'Add'. |
| TC-LOC-021 | FAIL | Seed Singapore FW locations | Method invocation failed because [System.String] does not contain a method named 'Add'. |
| TC-LOC-030 | FAIL | WmsLocation total probe | after=7 baseline=50 |
| TC-LOC-031 | SKIP | Seed stock on map recv KEEP | buildPallet biz:库位不存在: Stk.RECV-MAP |
| TC-LOC-040 | PASS | UI checklist written | see report |

## Note

- Earlier retain/coverage scripts only wrote a few locations; this run seeds map fixtures.
- Sources: 20260426-SRM-Demo / 20260619-singapore -> testplan/fixtures/*
