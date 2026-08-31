# Task I-4: Promote / promote-preview API

**Request shape:** `{ projectName, devices: [{ code, host, port, protocol }], apply?: bool }`  
Or separate endpoints: `promote-preview` and `promote`.

**Rules:**
- Reject host `127.0.0.1` / `localhost` / `::1` (case-insensitive)
- Preview: return device list that would be applied + warnings
- Apply: write CommConnection if DeviceComm entities exist OR persist promote snapshot on Sim_Deployment (remark/status Promoted) + do NOT rename SIM_ warehouse
- No git commit

Add DTOs to Application/Simulator. Tests: reject loopback; preview returns devices; apply marks deployment Promoted.

Inspect CommConnection entity under Domain/Entities/DeviceComm. Prefer upsert by name/code if straightforward; otherwise store JSON on Sim_Deployment (check entity fields).

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
