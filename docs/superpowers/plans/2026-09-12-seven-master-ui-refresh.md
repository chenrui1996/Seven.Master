# Seven Master UI Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh the Seven Master Vue 3 frontend for desktop operations and monitoring while preserving the existing blue/white ERP identity, API behavior, permissions, routes, generated-page contracts, and i18n keys.

**Architecture:** Make the visual system token-driven first, then update the shared shell and `CrudPanel.vue` so the majority of generated pages improve without page-level duplication. Apply focused composition changes to Login, Home, and non-standard WCS/DeviceComm/SCADA surfaces, with no new navigation architecture or backend contract.

**Tech Stack:** Vue 3.5, TypeScript, Vite, Element Plus, Pinia, vue-i18n, ECharts, existing CSS files and browser smoke checks.

**Spec:** `docs/superpowers/specs/2026-09-12-seven-master-ui-refresh-design.md`

## Global Constraints

- Preserve the existing blue/white ERP identity, dark dual-rail navigation, API contracts, permissions, routes, generated-page conventions, and internationalization keys.
- Desktop widescreen monitoring and data operations are primary; narrow screens remain usable without a new mobile navigation architecture.
- Keep Source Sans 3 for UI text and Fira Code for identifiers, metrics, and operational values.
- Keep the existing blue primary interaction color and coral danger/create semantics; use orange only for execution or in-progress semantics.
- Honor `prefers-reduced-motion`; keep keyboard focus visible on interactive controls.
- Do not add dependencies, change APIs, invent telemetry, or fabricate real-time values.
- Preserve existing loading, error toast, permission, feature-flag, and route behavior.
- The worktree already contains unrelated/uncommitted frontend changes. Inspect diffs before editing and stage only files owned by the current task; never reset or revert existing changes.
- Run only existing validation commands: `npm run build`, VS Code Problems checks, and browser smoke checks through the existing Vite app.

---

## File Map

| File | Responsibility in this plan |
|---|---|
| `Seven.Vue3/src/styles/theme.css` | Semantic light/dark tokens, Element Plus variables, global surfaces, table/form/focus primitives |
| `Seven.Vue3/src/styles/buttons.css` | Button hierarchy, disabled/loading/focus states, toolbar actions |
| `Seven.Vue3/src/styles/dual-rail.css` | Desktop rail, secondary menu, tabs/shell interaction states and responsive overflow protection |
| `Seven.Vue3/src/styles/wcs-ops.css` | Shared WCS operations surface tokens and monitoring panels |
| `Seven.Vue3/src/layout/MainLayout.vue` | Shell markup, accessible labels, page context/system status/operator grouping |
| `Seven.Vue3/src/views/Login.vue` | Login composition, captcha accessibility, focused responsive form |
| `Seven.Vue3/src/views/Home.vue` | Dashboard composition, status hierarchy, stable states, token-based chart colors |
| `Seven.Vue3/src/components/crud/CrudPanel.vue` | Shared generated-page toolbar, table, row actions, master-detail and pagination presentation |
| `Seven.Vue3/src/views/Scada/Floor2d.vue` | Non-CRUD monitoring canvas and polling status presentation |
| `Seven.Vue3/src/views/DeviceComm/Runtime.vue` | Non-CRUD device/runtime monitoring surface, if its layout requires operational treatment |
| `Seven.Vue3/src/views/Wcs/**` | Non-standard WCS operational surfaces only where they do not use the shared CRUD pattern |

No new files are required for the implementation unless an existing non-standard view needs a small local component; prefer existing files and styles.

## Execution Order and Boundaries

Implement tasks in order. Each task must finish its own validation before the next begins. A task commit must include only its owned files and must not include the already-dirty files from unrelated work.

### Task 1: Establish Semantic Operations Design Tokens

**Files:**
- Modify: `Seven.Vue3/src/styles/theme.css`
- Modify: `Seven.Vue3/src/styles/buttons.css`
- Modify: `Seven.Vue3/src/styles/wcs-ops.css`

**Interfaces:**
- Consumes: Existing `--seven-*` variables and Element Plus CSS variable mappings.
- Produces: Stable semantic tokens consumed by the shell, login, dashboard, CRUD, and operations surfaces:
  `--seven-focus-ring`, `--seven-bg-page`, `--seven-bg-panel`, `--seven-bg-subtle`,
  `--seven-border`, `--seven-border-light`, `--seven-text`, `--seven-text-muted`,
  `--seven-primary-dark`, `--seven-accent`, `--seven-success`, `--seven-warning`,
  `--seven-danger`, `--seven-info`, `--seven-execution`, and their dark-mode mappings.

- [ ] **Step 1: Record the current diff and identify token ownership**

  Run:

  ```powershell
  git diff -- Seven.Vue3/src/styles/theme.css Seven.Vue3/src/styles/buttons.css Seven.Vue3/src/styles/wcs-ops.css
  ```

  Expected: The existing user changes are understood and will be preserved while the task changes are layered on top.

- [ ] **Step 2: Add semantic light and dark tokens without changing brand semantics**

  In `theme.css`, keep the existing blue primary and coral danger/create variables, add explicit focus/execution/status surface tokens, and map the same semantic names in `html.dark`. Replace hardcoded status colors in shared CSS with these tokens.

  ```css
  :root {
    --seven-focus-ring: #2563eb;
    --seven-execution: #d97706;
    --seven-execution-soft: rgba(217, 119, 6, 0.12);
    --seven-status-success-soft: rgba(103, 194, 58, 0.12);
    --seven-status-warning-soft: rgba(230, 162, 60, 0.14);
    --seven-status-danger-soft: rgba(245, 108, 108, 0.12);
    --seven-status-info-soft: rgba(144, 147, 153, 0.12);
  }

  html.dark {
    --seven-focus-ring: #79bbff;
    --seven-execution: #f59e0b;
    --seven-execution-soft: rgba(245, 158, 11, 0.18);
    --seven-status-success-soft: rgba(103, 194, 58, 0.2);
    --seven-status-warning-soft: rgba(230, 162, 60, 0.2);
    --seven-status-danger-soft: rgba(245, 108, 108, 0.2);
    --seven-status-info-soft: rgba(163, 166, 173, 0.18);
  }
  ```

  Use the existing primary blue instead of replacing it with a second unrelated primary value.

- [ ] **Step 3: Normalize shared control focus and disabled states**

  In `buttons.css` and the global rules in `theme.css`, make `:focus-visible` use `var(--seven-focus-ring)` with a 2px ring and ensure disabled buttons never receive hover color or pointer affordances. Preserve the existing button sizes and Element Plus variants.

- [ ] **Step 4: Replace hardcoded WCS surface colors with semantic tokens**

  In `wcs-ops.css`, replace hardcoded panel/background/text/status values with `--seven-*` or local `--ops-*` aliases. Keep the existing WCS class names and layout contract.

- [ ] **Step 5: Validate the token task**

  Run:

  ```powershell
  npm run build
  ```

  Expected: Vite production build succeeds with no TypeScript or CSS compilation errors.

- [ ] **Step 6: Commit only token files**

  ```powershell
  git add Seven.Vue3/src/styles/theme.css Seven.Vue3/src/styles/buttons.css Seven.Vue3/src/styles/wcs-ops.css
  git commit -m "refactor(ui): establish operations design tokens" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 2: Refine the Desktop Operations Shell

**Files:**
- Modify: `Seven.Vue3/src/layout/MainLayout.vue`
- Modify: `Seven.Vue3/src/styles/dual-rail.css`
- Modify: `Seven.Vue3/src/styles/theme.css` only if a shell token is missing

**Interfaces:**
- Consumes: Existing `useMenuStore`, `useTabsStore`, `useFeatureStore`, `useUserStore`, `LocaleSwitch`, `ThemeToggle`, `AlarmBell`, and `SecondaryNode` behavior.
- Produces: Existing route/menu/tab behavior with clearer page context, system status, operator controls, active states, accessible tab controls, and no page-level horizontal overflow.

- [ ] **Step 1: Add accessible names and state attributes to shell controls**

  In `MainLayout.vue`, keep native buttons for rail items and tab controls. Add or preserve `aria-label`, `aria-current`, and `title` for icon-only controls. Ensure tab close controls have an accessible name derived from the tab label and do not trigger tab activation when closing.

- [ ] **Step 2: Group the header without changing data sources**

  Use wrapper classes around the existing breadcrumb/page context, clock/connection metadata, alarm/theme/locale controls, and operator dropdown. Do not add new stores or status requests. Keep `onlineLabel`, current time, user identity, and feature-gated alarm behavior unchanged.

- [ ] **Step 3: Upgrade rail, secondary menu, tab, and overflow styles**

  In `dual-rail.css`, improve active/hover/focus contrast, give tab close icons a stable hit area, add visible keyboard focus, and ensure the main content can shrink with `min-width: 0` while the secondary menu remains scrollable. Preserve `RAIL_WIDTH`, `PANEL_WIDTH`, menu filtering, and bottom-pinned menu behavior.

- [ ] **Step 4: Add narrow-screen protections**

  At the existing mobile breakpoint, hide nonessential header metadata before reducing the usable content area, allow tabs to scroll inside the tab bar, and keep the page itself free of horizontal overflow. Do not replace the dual-rail navigation with a new drawer.

- [ ] **Step 5: Validate the shell task**

  Run `npm run build`, then use the Vite browser smoke check at a desktop width and a narrow width. Verify home navigation, a nested menu leaf, tab activation/close, theme toggle, locale switch, and operator dropdown.

- [ ] **Step 6: Commit only shell files**

  ```powershell
  git add Seven.Vue3/src/layout/MainLayout.vue Seven.Vue3/src/styles/dual-rail.css
  git commit -m "refactor(ui): refine operations shell" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 3: Redesign the Login Surface Without Changing Authentication

**Files:**
- Modify: `Seven.Vue3/src/views/Login.vue`
- Modify: `Seven.Vue3/src/styles/theme.css` only if a login token is missing

**Interfaces:**
- Consumes: Existing `createCaptcha`, `login`, `useFeatureStore`, `useUserStore`, `LocaleSwitch`, `ThemeToggle`, and `ActionIcons.login`.
- Produces: Existing login flow with restrained enterprise visual hierarchy, explicit labels, accessible captcha refresh, loading/error feedback, light/dark support, and small-screen form focus.

- [ ] **Step 1: Preserve the current login behavior before markup edits**

  Confirm that `loadCaptcha()` still handles feature-disabled captcha, success data, loading state, and the existing error fallback, and that `handleLogin()` still sets tokens/user data, routes to `/home`, shows messages, and refreshes captcha after failed requests.

- [ ] **Step 2: Add explicit accessibility metadata to captcha and form actions**

  Keep Element Plus `el-form-item` labels. Add `aria-label` to the captcha refresh button using the existing `t('login.captchaRefresh')` text, expose its busy/disabled state while loading, and ensure the submit button remains a native form submit control with visible loading feedback.

- [ ] **Step 3: Recompose the brand and form hierarchy**

  Keep the existing two-column structure and translations, but use a restrained brand panel, smaller decorative background treatment, clearer panel header, and stronger form field grouping. Do not add new copy or i18n keys unless the existing keys cannot express the approved design.

- [ ] **Step 4: Make light/dark and responsive styling token-driven**

  Replace hardcoded panel/background/border/status values with existing semantic tokens. At the small breakpoint, hide only the decorative brand content, keep the brand identity available in the form header, make the submit action full width, and prevent captcha controls from overflowing.

- [ ] **Step 5: Validate the login task**

  Run `npm run build`. In the browser smoke check, verify `/login` in light and dark themes, keyboard tab order through username/password/captcha/submit, captcha refresh focus, loading state, and a 375px-wide viewport.

- [ ] **Step 6: Commit the login task**

  ```powershell
  git add Seven.Vue3/src/views/Login.vue
  git commit -m "refactor(ui): redesign operations login surface" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 4: Upgrade the Shared CRUD and Master-Detail Surface

**Files:**
- Modify: `Seven.Vue3/src/components/crud/CrudPanel.vue`
- Modify: `Seven.Vue3/src/styles/theme.css`
- Modify: `Seven.Vue3/src/styles/buttons.css` only when a shared table/action rule is required

**Interfaces:**
- Consumes: Existing CRUD props, `userStore.hasPermission`, extension toolbar/row hooks, dictionary loading, pagination, import/export, and master-detail state.
- Produces: Same CRUD API and permission behavior with clearer toolbar priority, wrapped search area, denser readable tables, accessible icon-only actions, stable master-detail sections, and responsive table scrolling.

- [ ] **Step 1: Map all icon-only CRUD actions and permission gates**

  Inspect the template sections for refresh, column settings, row actions, import/export, and detail actions. Keep the existing permission checks and add accessible names/title text to icon-only buttons without exposing actions that are not permitted.

- [ ] **Step 2: Recompose toolbar classes without changing action handlers**

  Keep `onSearch`, `openForm`, `toolbarDelete`, `batchRemove`, `triggerImport`, `doExport`, `downloadTemplate`, `onResetSearch`, `loadData`, and `openColumnSettings` as the handlers. Add semantic wrapper classes so primary actions, creation, destructive actions, and utility actions can be styled independently. Keep the current action order unless a visual grouping requires only wrapper movement.

- [ ] **Step 3: Improve table hierarchy and state contrast**

  In the component-scoped styles and shared theme rules, style headers, numeric columns, hover/current/selected rows, first-column links, operation columns, loading overlays, and pagination. Use status text or existing enum labels in addition to color; do not infer new status values.

- [ ] **Step 4: Normalize master-detail and empty/loading surfaces**

  Preserve same-page detail rendering. Give `.detail-section`, `.detail-section__head`, `.detail-empty`, and nested panels consistent token-based borders, spacing, heading hierarchy, and stable empty-state height. Keep detail API loading and error behavior unchanged.

- [ ] **Step 5: Add responsive containment**

  Keep table horizontal scrolling inside `.table-wrap`, let the toolbar wrap, keep filters usable, and avoid page-level horizontal overflow at narrow widths. Do not shrink operational values below readable sizes.

- [ ] **Step 6: Validate representative generated pages**

  Run `npm run build`, then smoke-test one standard WMS/system CRUD page with search, reset, add/edit/delete permission states, row selection, column settings, pagination, and a master-detail page if available. Confirm generated pages compile without per-view changes.

- [ ] **Step 7: Commit the CRUD task**

  ```powershell
  git add Seven.Vue3/src/components/crud/CrudPanel.vue Seven.Vue3/src/styles/theme.css Seven.Vue3/src/styles/buttons.css
  git commit -m "refactor(ui): upgrade shared CRUD surface" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 5: Refine the Home Monitoring Dashboard

**Files:**
- Modify: `Seven.Vue3/src/views/Home.vue`
- Modify: `Seven.Vue3/src/styles/theme.css` only if dashboard primitives need a shared rule

**Interfaces:**
- Consumes: Existing `kpiCards`, `inventoryRows`, `inventorySummary`, `occupancyZones`, `alarmRows`, `transferRows`, `alarmStore`, `featureStore`, existing API calls, and ECharts setup.
- Produces: Existing dashboard data and actions with clearer monitoring hierarchy, stable loading/empty states, semantic chart colors, and responsive desktop-first layout.

- [ ] **Step 1: Preserve all existing dashboard data and feature gates**

  Keep alarm, transfer, SignalR, broadcast, inventory demo, occupancy calculations, and route actions unchanged. Do not introduce fabricated timestamps or metrics.

- [ ] **Step 2: Add stable status/freshness presentation only from existing data**

  Where a response provides a timestamp, display it with the existing formatter and translation keys. Keep fixed-height/loading containers for alarm and transfer lists so async responses do not shift the dashboard layout. If no trustworthy timestamp exists, omit freshness text rather than inventing one.

- [ ] **Step 3: Replace chart hardcoded colors with semantic CSS-derived values**

  Use the established primary/info or execution token values for ECharts configuration. Keep the occupancy calculation and pie series unchanged; only change presentation and labels. Ensure dark mode legend/label text remains readable.

- [ ] **Step 4: Improve dashboard panel hierarchy and responsive composition**

  Keep KPI, inventory, occupancy, alarm, transfer, and broadcast sections. Use consistent stack classes instead of inline margins, strengthen section headings and metadata, and keep tables/list cards bounded at desktop and scroll-contained on narrow widths.

- [ ] **Step 5: Validate the dashboard task**

  Run `npm run build`, then smoke-test `/home` at wide desktop, normal desktop, dark mode, narrow width, and reduced-motion preference. Confirm chart resize, alarm/transfer loading/empty states, and existing navigation buttons.

- [ ] **Step 6: Commit the dashboard task**

  ```powershell
  git add Seven.Vue3/src/views/Home.vue
  git commit -m "refactor(ui): refine monitoring dashboard" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 6: Apply Focused Monitoring Treatment to Non-CRUD Operational Views

**Files:**
- Inspect and modify only as needed: `Seven.Vue3/src/views/Scada/Floor2d.vue`
- Inspect and modify only as needed: `Seven.Vue3/src/views/DeviceComm/Runtime.vue`
- Inspect and modify only as needed: `Seven.Vue3/src/views/DeviceComm/CommConnection.vue`
- Inspect and modify only as needed: `Seven.Vue3/src/views/DeviceComm/CommPoint.vue`
- Inspect and modify only as needed: `Seven.Vue3/src/views/DeviceComm/CommRule.vue`
- Inspect and modify only as needed: `Seven.Vue3/src/views/Wcs/**`
- Modify: `Seven.Vue3/src/styles/wcs-ops.css` when a shared operations rule is appropriate

**Interfaces:**
- Consumes: Existing view-specific API calls, polling timers, WCS operation classes, and existing i18n labels.
- Produces: Improved monitoring/status hierarchy without changing polling intervals, API routes, node coordinates, status semantics, or CRUD-generated page contracts.

- [ ] **Step 1: Identify non-CRUD surfaces before editing**

  Search for `<style`, `setInterval`, canvas/map classes, `wcs-ops`, and layout wrappers in the listed directories. Treat generated CRUD views as already covered by Task 4 and do not duplicate shared CRUD styles.

- [ ] **Step 2: Upgrade SCADA status composition without altering polling**

  In `Floor2d.vue`, keep `loadViews`, `loadStatus`, the 5-second timer, selected view behavior, and node coordinates. Add a compact status header/legend using existing translations or existing text, use semantic tokens instead of hardcoded canvas/node colors, and keep the canvas horizontally contained on narrow screens.

- [ ] **Step 3: Upgrade DeviceComm/WCS status surfaces only where needed**

  For each non-CRUD view, preserve its current handlers and API contracts. Add consistent status panels, operational headings, focus states, and reduced-motion-safe transitions using `wcs-ops.css` or local scoped styles. Do not add new telemetry or change refresh/polling behavior.

- [ ] **Step 4: Validate representative operational pages**

  Run `npm run build`, then smoke-test one SCADA page and one DeviceComm/WCS page in light/dark modes and at desktop/narrow widths. Confirm polling still runs, status text remains visible, and no page-level overflow appears.

- [ ] **Step 5: Commit only the operational view changes**

  ```powershell
  git add Seven.Vue3/src/views/Scada Seven.Vue3/src/views/DeviceComm Seven.Vue3/src/views/Wcs Seven.Vue3/src/styles/wcs-ops.css
  git commit -m "refactor(ui): refine operational monitoring views" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

### Task 7: Run Full UI Refresh Verification and Review

**Files:**
- Inspect: all files modified by Tasks 1–6
- Modify: only if verification finds a regression directly caused by this refresh

**Interfaces:**
- Consumes: Completed token, shell, login, CRUD, dashboard, and operational view changes.
- Produces: Verified build and browser behavior with a clean, reviewable diff that leaves unrelated existing work untouched.

- [ ] **Step 1: Check the final worktree and diff boundaries**

  Run:

  ```powershell
  git status --short
  git diff --check
  git diff --stat
  ```

  Confirm that unrelated pre-existing files are not staged in the refresh commits and that there are no whitespace errors.

- [ ] **Step 2: Run the production build**

  ```powershell
  npm run build
  ```

  Expected: Vite exits with code 0 and emits the production bundle.

- [ ] **Step 3: Run editor diagnostics on all changed Vue files**

  Use the VS Code Problems check for:
  `MainLayout.vue`, `Login.vue`, `Home.vue`, `CrudPanel.vue`,
  `Floor2d.vue`, and any changed DeviceComm/WCS views.

  Expected: no new TypeScript, template, or CSS diagnostics attributable to the refresh.

- [ ] **Step 4: Perform browser smoke checks**

  Start the existing app with:

  ```powershell
  npm run dev -- --host 127.0.0.1
  ```

  Verify:

  - `/login`: light/dark mode, keyboard focus, captcha refresh, submit loading, 375px layout.
  - `/home`: KPI/panels/chart/list loading and empty states, wide and narrow layouts, dark mode.
  - One generated CRUD page: filters, search/reset, table selection, toolbar actions, pagination, detail area.
  - One SCADA/DeviceComm/WCS page: status rendering, polling/refresh, light/dark and narrow layout.
  - Shell: rail active state, secondary menu filtering, tabs, tab close, operator dropdown, no page-level horizontal overflow.
  - Reduced-motion preference: no essential information is hidden behind animation and focus remains visible.

- [ ] **Step 5: Review acceptance criteria against the approved spec**

  Confirm the final diff preserves API calls, route paths, permission checks, feature flags, i18n keys, and generated-page contracts. Record any intentionally deferred visual issue rather than making unrelated fixes.

- [ ] **Step 6: Stop background services and report evidence**

  Stop the detached Vite process using its specific process/session ID. Report the build result, diagnostics result, browser routes checked, viewport/theme checks, and any known limitations.

## Spec Coverage Review

- Foundation tokens and light/dark parity: Task 1.
- Shell context/status/operator grouping, rail/tab states, keyboard focus, and responsive containment: Task 2.
- Login hierarchy, labels, captcha accessibility, loading/error preservation, and responsive behavior: Task 3.
- CRUD toolbar priority, table states, accessible actions, master-detail, and contained scrolling: Task 4.
- Dashboard hierarchy, stable states, chart tokens, freshness honesty, and responsive behavior: Task 5.
- SCADA/DeviceComm/WCS operational surfaces and polling/status preservation: Task 6.
- Build, Problems, browser, theme, viewport, focus, and reduced-motion verification: Task 7.

## Plan Self-Review

- No new dependency or test runner is required.
- No task changes API routes, stores, permissions, route paths, or i18n contracts.
- Every task has explicit files, interfaces, implementation steps, validation, and commit boundaries.
- The plan avoids page-by-page rewrites of generated CRUD views.
- The plan contains no unfinished implementation steps.
