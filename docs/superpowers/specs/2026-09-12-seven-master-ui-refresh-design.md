# Seven Master UI Refresh Design

## Status

Approved in conversation on 2026-09-12. Implementation has not started.

## Goal

Refresh the Seven Master Vue 3 frontend for desktop operations and monitoring
workflows while preserving the existing blue/white ERP identity, dark dual-rail
navigation, API contracts, permissions, routes, generated-page conventions, and
internationalization keys.

The refresh is intentionally system-first: most of the 68 views are generated
CRUD pages backed by `CrudPanel.vue`, so shared tokens and primitives should
carry the improvement instead of duplicating page-specific CSS.

## Product and UX Direction

- Product: industrial logistics / WMS / WCS operations console.
- Primary context: desktop widescreen monitoring and data operations.
- Secondary context: narrow screens remain usable, but do not receive a new
  mobile navigation architecture.
- Style: Minimalism / Swiss enterprise UI with dense, high-contrast data
  surfaces.
- Motion: subtle transitions only; honor `prefers-reduced-motion`.
- Brand: keep the existing blue primary interaction color, coral danger/create
  semantics, and charcoal dual-rail navigation. Use orange only for execution
  or in-progress semantics, not as a new primary brand color.
- Typography: keep Source Sans 3 for UI text and Fira Code for identifiers,
  metrics, and operational values.

## Scope

### Foundation

Update the shared design tokens and primitives in:

- `Seven.Vue3/src/styles/theme.css`
- `Seven.Vue3/src/styles/buttons.css`
- `Seven.Vue3/src/styles/dual-rail.css`
- `Seven.Vue3/src/styles/wcs-ops.css`

The system must define semantic light/dark tokens for surfaces, borders,
primary text, muted text, focus rings, status colors, spacing, density, and
elevation. Shared controls must retain visible keyboard focus and consistent
disabled/loading states.

### Shell

Refine:

- `Seven.Vue3/src/layout/MainLayout.vue`
- `Seven.Vue3/src/styles/dual-rail.css`

The shell is divided into page context, system status, and operator controls.
The active rail module and active leaf remain visually related. Tabs gain
clearer active/hover/close states, sufficiently large close hit areas, and
keyboard focus. Desktop dual-rail behavior remains intact; narrow widths must
avoid horizontal page overflow.

### Login

Refine:

- `Seven.Vue3/src/views/Login.vue`
- Relevant shared tokens and button rules.

The login view retains its two-column desktop structure and collapses to a
focused form on small screens. Reduce decorative mesh/glow treatment in favor
of a restrained brand panel and clear form hierarchy. Username, password, and
captcha keep explicit labels. Captcha refresh remains a native button with an
accessible name, visible focus, and loading/disabled feedback. Existing login
success/error behavior and captcha refresh behavior are unchanged.

### Dashboard

Refine:

- `Seven.Vue3/src/views/Home.vue`
- Relevant chart/status tokens.

Keep the existing KPI, inventory, occupancy, alarms, transfer orders, and
broadcast features. Improve monitoring hierarchy, stable loading/empty heights,
status semantics, token-based chart colors, responsive spacing, and data
freshness wording only when backed by existing response data. Do not invent
real-time values or change dashboard API behavior.

### CRUD and Operational Views

Refine:

- `Seven.Vue3/src/components/crud/CrudPanel.vue`
- `Seven.Vue3/src/views/Wcs/**`
- `Seven.Vue3/src/views/DeviceComm/**`
- `Seven.Vue3/src/views/Scada/**`

`CrudPanel.vue` remains the sole shared upgrade point for generated pages.
Toolbar priority is search/reset, create, edit/delete, then secondary
import/export/column actions. Search fields may wrap naturally. Tables gain
clearer header hierarchy, numeric alignment, selected/current-row contrast,
status treatment, and accessible icon-only controls. Master-detail remains on
the same page and receives the same spacing, loading, and empty-state rules.

Non-standard WCS, device, and SCADA views should receive only focused
monitoring/status improvements where their layouts differ from CRUD. Do not
copy CRUD styles into independent operational surfaces.

## Architecture and Data Flow

No API, route, permission, store, or i18n contract changes are planned.

1. `theme.css` owns semantic visual tokens and Element Plus variable mapping.
2. `buttons.css`, `dual-rail.css`, and `wcs-ops.css` consume those tokens.
3. `MainLayout.vue` supplies shell structure and state from existing stores.
4. `CrudPanel.vue` supplies shared generated-page behavior and presentation.
5. Individual views add only domain-specific composition and data mapping.

Existing loading flags, `ElMessage` error feedback, permission checks, feature
flags, and route navigation remain the source of truth. Any directly related
shared-component bug discovered during the refresh must be surfaced or fixed
explicitly; no broad catch-and-ignore behavior may be introduced.

## Accessibility and Responsive Requirements

- Every icon-only action has an accessible name and tooltip where appropriate.
- Keyboard focus is visible on buttons, menu leaves, tabs, captcha refresh, and
  dialog/form controls.
- Form labels remain visible and associated through Element Plus semantics.
- Status is communicated with text or icon in addition to color.
- Normal text targets at least 4.5:1 contrast in light and dark themes.
- Desktop controls keep stable layout bounds during hover/active transitions.
- Narrow screens must not create page-level horizontal scrolling; data tables
  may scroll within their own bounded wrapper.
- Reduced-motion users receive final states without nonessential transitions.

## Validation Plan

Run the existing checks only:

1. `npm run build`
2. VS Code Problems check for changed Vue/CSS files.
3. Browser smoke test using the existing Vite app:
   - login view in light and dark themes;
   - home dashboard;
   - one generated CRUD view;
   - one WCS, DeviceComm, or SCADA view;
   - desktop widescreen and narrow viewport;
   - keyboard focus on primary controls;
   - reduced-motion preference.

No new test runner or dependency is required by this design.

## Non-Goals

- No API or backend changes.
- No replacement of Element Plus.
- No new mobile navigation system.
- No fabricated telemetry, metrics, or real-time state.
- No page-by-page rewrite of all generated views.
- No unrelated refactor of stores, router, or generated extension contracts.

## Acceptance Criteria

- Login, shell, dashboard, CRUD, and operational surfaces share the same
  semantic token system in light and dark modes.
- Existing permissions, routes, feature flags, API calls, and login behavior
  continue to work.
- Generated CRUD views improve without requiring per-view duplication.
- Desktop monitoring density and hierarchy are visibly improved.
- Narrow screens remain usable without page-level horizontal overflow.
- Build succeeds and browser smoke checks show no blocking layout or focus
  regressions.
