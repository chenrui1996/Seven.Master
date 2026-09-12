# Task 5 Report — Refine the Home Monitoring Dashboard

## Scope
- Worktree: `D:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Vue3\.worktrees\seven-master-ui-refresh`
- Target file changed: `Seven.Vue3/src/views/Home.vue`
- `theme.css` was reviewed but not modified.

## Requirements Coverage
1. **Preserved existing data, APIs, feature gates, routes, computed values, and behaviors**
   - Kept existing inventory demo data, occupancy calculations, alarm loading path, transfer loading path, SignalR/broadcast flow, docs link, and navigation targets.
   - Did not alter API routes, alarm store usage, feature flags, or computed KPI meanings.
2. **Stable status and freshness presentation**
   - Added fixed-height, scroll-contained shells for alarm and transfer sections to prevent layout jumps during loading and empty states.
   - Added freshness metadata only when existing row timestamps are present:
     - alarms use `sysAlarm.colTime`
     - transfers use `generated.TransferOrder.createDate`
   - Reused the existing `formatTime` formatter and omitted freshness when no trustworthy timestamp exists.
3. **Semantic chart colors**
   - Removed hardcoded chart palette usage and now derive occupancy chart colors/text/surface borders from existing CSS tokens (`--seven-accent`, `--seven-info`, `--seven-text-muted`, `--seven-primary-dark`, `--seven-bg-panel`).
   - Preserved the existing occupancy calculation and pie-series structure.
4. **Improved hierarchy and responsive composition**
   - Promoted the dashboard title to an `h1` and section titles to `h2` for clearer hierarchy.
   - Replaced the `el-row`/inline-margin stacking with a two-column desktop-first CSS grid and consistent panel spacing.
   - Added occupancy summary cards below the chart.
   - Kept inventory table horizontally contained and bounded alarm/transfer panels with responsive wrapping on narrow widths.
   - Added `overflow-x: hidden` and min-width guards to prevent page overflow.
5. **Reduced-motion-safe styling and chart resilience**
   - Extended reduced-motion handling to cards/panels/stats/list rows.
   - Added `ResizeObserver` to keep the chart responsive to panel/container changes.
   - Added `MutationObserver` on the root element so theme class changes re-render the chart with the correct semantic colors.

## UX Guidance Applied
- `ui-ux-pro-max` results used:
  - heading hierarchy
  - consistent type scale
  - reduced motion handling
  - chart accessibility guidance to avoid color-only meaning and keep textual summaries visible
- Vue stack search returned no matching guidance after one retry, so layout implementation used repository patterns plus the verified UX guidance above.

## Validation
### Build
- Ran `npm run build` in `Seven.Vue3`
- Result: success (exit code 0)

### Smoke
- Started Vite dev server on `http://127.0.0.1:4173`
- Verified HTTP responses:
  - `200 http://127.0.0.1:4173/`
  - `200 http://127.0.0.1:4173/home`
- Integrated browser smoke was attempted, but browser tooling was not available in this session (`openBrowserPage` failed), so no interactive browser snapshot could be captured.

### Diff / Review
- Ran `git diff --check -- Seven.Vue3/src/views/Home.vue`
- Result: clean
- Self-review focus:
  - no API or feature-gate changes
  - no invented telemetry
  - freshness sourced only from existing timestamps
  - no theme token regressions introduced
  - no extra files staged for commit

## Git
- Staged only `Seven.Vue3/src/views/Home.vue`
- Commit message: `refactor(ui): refine monitoring dashboard`

## Notes
- Existing unrelated user changes in locale files and other worktree artifacts were preserved and left unstaged.

## Review Fix Round 1

Addressed the Task 5 review findings in `Seven.Vue3/src/views/Home.vue` only:

- `pickLatestTimestamp` now ignores invalid/empty candidates and returns no freshness value when every candidate timestamp is unparseable.
- The occupancy ECharts option now disables animation when `window.matchMedia('(prefers-reduced-motion: reduce)')` matches, while preserving normal-motion behavior.

### Verification Commands

#### Source-level verification
```powershell
@'
const fs = require('fs');
const path = 'Seven.Vue3\\src\\views\\Home.vue';
const source = fs.readFileSync(path, 'utf8');
const start = source.indexOf('function pickLatestTimestamp(values: Array<string | undefined>) {');
const end = source.indexOf('function readCssVar', start);
const block = source.slice(start, end);
const body = block.slice(block.indexOf('{') + 1, block.lastIndexOf('}')).replace(/: string \| undefined/g, '');
const pickLatestTimestamp = new Function('values', body);
const invalidOnly = pickLatestTimestamp([undefined, '', 'not-a-date', 'still-not-a-date']);
const latestValid = pickLatestTimestamp(['2024-01-01T00:00:00', 'not-a-date', '2024-01-03T12:00:00']);
if (invalidOnly !== undefined) throw new Error(`invalid-only result=${JSON.stringify(invalidOnly)}`);
if (latestValid !== '2024-01-03T12:00:00') throw new Error(`latest-valid result=${JSON.stringify(latestValid)}`);
if (!/animation\s*:\s*!prefersReducedMotion\(\)/.test(source)) throw new Error('animation toggle missing');
if (!/window\.matchMedia\(['"]\(prefers-reduced-motion: reduce\)['"]\)\.matches/.test(source)) throw new Error('reduced-motion media query missing');
console.log('Home.vue verification OK');
console.log('invalid-only => undefined');
console.log('latest-valid => 2024-01-03T12:00:00');
console.log('animation => !prefersReducedMotion()');
'@ | node -
```

Output:
```text
Home.vue verification OK
invalid-only => undefined
latest-valid => 2024-01-03T12:00:00
animation => !prefersReducedMotion()
```

#### Build verification
```powershell
npm run build
```

Output (tail):
```text
dist/assets/Home-BqVpuerx.js                       454.80 kB │ gzip: 153.74 kB
dist/assets/index-6G5QY7w5.js                      768.91 kB │ gzip: 239.94 kB

[INEFFECTIVE_DYNAMIC_IMPORT] src/router/index.ts is dynamically imported by src/stores/user.ts
but also statically imported by src/api/http.ts, src/main.ts, dynamic import will not move module into another chunk.

[plugin builtin:vite-reporter]
(!) Some chunks are larger than 500 kB after minification. Consider:
- Using dynamic import() to code-split the application
- Use build.rolldownOptions.output.codeSplitting to improve chunking
- Adjust chunk size limit for this warning via build.chunkSizeWarningLimit.
✓ built in 1.30s
```
