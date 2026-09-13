# Seven Master Design System

## Brand

- **Style:** Data-Dense ERP / WMS dashboard (Vol-like Element Plus)
- **Primary:** `#409EFF` (query / links / active nav / login CTA)
- **Create CTA:** `#F56C6C` (新建)
- **Page bg:** `#F3F7FB`
- **Panel:** `#FFFFFF`
- **Selected row:** `#E0E8FF`
- **Navigation:** dual-rail (icon rail + secondary panel) — styles in `src/styles/dual-rail.css`
- **Login:** light page + white form card; brand panel uses charcoal/blue rail palette

## Typography

- Body: `Source Sans 3`, system Chinese fallbacks
- Mono: `Fira Code` (KPI / codes)

## Anti-patterns

- No emoji as UI icons
- No gold/orange as primary interaction color
- No ornate gradients on CRUD surfaces
- Master-detail always **same page** (`below`), never force dialog/page navigation for child tables
- Do not duplicate dual-rail rules inside `MainLayout.vue` — edit `dual-rail.css` only

## Components

- CRUD toolbar: title + filters left; 查询(blue) / 新建(red) / 编辑·删除(outline) / utilities right
- Section titles use blue vertical bar (`.seven-section-title`)
- Dual-rail active states: left blue inset bar + soft blue fill
