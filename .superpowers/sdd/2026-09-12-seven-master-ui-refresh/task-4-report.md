# Task 4 Report — Upgrade the Shared CRUD and Master-Detail Surface

## Scope
Implemented Task 4 only in the allowed shared CRUD surface files:
- `Seven.Vue3/src/components/crud/CrudPanel.vue`
- `Seven.Vue3/src/styles/theme.css`
- `Seven.Vue3/src/styles/buttons.css`

Preserved existing worktree changes outside those files and kept all existing CRUD props, handlers, permission checks, extension hooks, dictionary loading, pagination, import/export, and same-page master-detail behavior intact.

## What changed

### 1. Accessibility and semantic action grouping
- Added `aria-label` and `title` text to the icon-only refresh and column-settings buttons.
- Added keyboard-accessible semantics to the first-column edit link (`role="button"`, `tabindex`, Enter/Space activation) without changing edit behavior.
- Grouped toolbar actions with semantic `role="toolbar"` / `role="group"` wrappers so primary, create, destructive, and utility actions can be styled independently while preserving action order and handlers.
- Added row action group labels for assistive technologies.

### 2. Shared CRUD/table clarity
- Added explicit table classes for selection, primary, numeric, and operations columns.
- Improved header, row, selection, numeric-cell, operations-column, loading-mask, and pagination styling through shared tokens and scoped CRUD styles.
- Enabled contained table overflow inside `.table-wrap` so narrow layouts scroll the table instead of the page.

### 3. Master-detail surface normalization
- Kept same-page detail rendering behavior unchanged.
- Normalized detail section heading/body structure.
- Applied token-based borders, backgrounds, spacing, and stable empty-state height for `.detail-section`, `.detail-section__head`, `.detail-section__body`, and `.detail-empty`.
- Added loading text to detail tables using the existing `common.loading` contract.

## Validation

### Build
Command run:
- `npm run build`

Result:
- Passed successfully.

### Representative generated-page smoke
Checked for an existing generated-page smoke harness or UI automation path by searching the repo for smoke/preview-style test hooks (`smoke`, `playwright`, `cypress`, `storybook`, `preview`-style support beyond Vite preview).

Result:
- No dedicated generated-page smoke harness was present.
- Representative generated CRUD pages with same-page detail were identified (`views/Wms/WmsInboundOrder.vue`, `views/Business/TransferOrder.vue`), but runtime smoke requires authenticated backend menu/permission APIs that are not provided by the repo itself.
- Because the brief explicitly said “if available”, no false smoke claim was made; build verification was completed instead.

## Self-review
- Verified icon-only controls now expose accessible names.
- Verified toolbar semantics/grouping did not alter handler wiring or permission gates.
- Verified row selection/current-row/detail rendering logic stayed intact.
- Verified no new dependencies or i18n keys/contracts were introduced.
- Verified only the Task 4-owned shared CRUD files were prepared for staging/commit.

## Commit
Committed with:
- `refactor(ui): upgrade shared CRUD surface`

### Post-commit verification
- Commit: `54f53a0888122c737fcbd42911102046d3858cda`
- Task 4 files are clean after commit.
- Other pre-existing worktree changes remain uncommitted and preserved.

## Task 4 Review Fixes

### Findings addressed
- Restored built-in row edit/delete actions in the actions column with the original permission and handler behavior, while keeping the accessible action grouping and styling wrappers introduced by Task 4.
- Restored reset availability independently of Delete/Import/Export permissions by moving reset back into the always-visible search/toolbar action group and keeping the existing `onResetSearch` handler and `common.reset` i18n key.
- Restored the pre-task detail-mode contract by separating `below` detail tables from `dialog`/`page` entry detail actions again, preserving same-page-below behavior and removing Task 4's mode coercion from the CRUD surface.

### Validation rerun
Command run:
- `npm run build`

Output:
```text
> seven-vue3@0.0.0 build
> vite build

vite v8.3.0 building client environment for production...
transforming...
2535 modules transformed.
rendering chunks...
computing gzip size...
dist/assets/CrudPanel-ltHXBIWP.js                   34.50 kB | gzip:   9.93 kB
dist/assets/index-B572BCCC.js                      768.91 kB | gzip: 239.94 kB

[INEFFECTIVE_DYNAMIC_IMPORT] src/router/index.ts is dynamically imported by src/stores/user.ts but also statically imported by src/api/http.ts, src/main.ts, dynamic import will not move module into another chunk.

[plugin builtin:vite-reporter]
(!) Some chunks are larger than 500 kB after minification. Consider:
- Using dynamic import() to code-split the application
- Use build.rolldownOptions.output.codeSplitting to improve chunking: https://rolldown.rs/reference/OutputOptions.codeSplitting
- Adjust chunk size limit for this warning via build.chunkSizeWarningLimit.
built in 1.18s
```

### Final verification after restoring unconditional actions column
Command run:
- `npm run build`

Output:
```text
> seven-vue3@0.0.0 build
> vite build

vite v8.3.0 building client environment for production...
transforming...
2535 modules transformed.
rendering chunks...
[INEFFECTIVE_DYNAMIC_IMPORT] src/router/index.ts is dynamically imported by src/stores/user.ts but also statically imported by src/api/http.ts, src/main.ts, dynamic import will not move module into another chunk.

[plugin builtin:vite-reporter]
(!) Some chunks are larger than 500 kB after minification. Consider:
- Using dynamic import() to code-split the application
- Use build.rolldownOptions.output.codeSplitting to improve chunking: https://rolldown.rs/reference/OutputOptions.codeSplitting
- Adjust chunk size limit for this warning via build.chunkSizeWarningLimit.
built in 1.19s
```
