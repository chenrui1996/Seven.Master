# 鏍囩椤靛彸閿彍鍗?Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 鍦?`MainLayout` 鏍囩鏍忓鍔犲彸閿彍鍗曪細鍒锋柊銆佸叧闂€佸叧闂叾瀹冦€佸叏閮ㄥ叧闂€佸綋鍓嶉〉鍏ㄥ睆锛岄椤靛缁堜繚鐣欍€?

**Architecture:** 鍦?`MainLayout.vue` 鍐呭祵缁濆瀹氫綅娴眰鑿滃崟锛沗useTabsStore` 澧炲姞 `closeOthers` / `closeAll`锛涘埛鏂扮敤 `viewKey` 鎹?key + 鐭殏 `keep-alive` exclude锛涘叏灞忕敤 class 闅愯棌渚ф爮涓庨《鏍忓苟淇濈暀鏍囩鏍忋€?

**Tech Stack:** Vue 3銆丳inia銆乂ue Router銆乿ue-i18n銆丒lement Plus Icons

## Global Constraints

- 棣栭〉 `/home` 濮嬬粓淇濈暀锛涢椤典笂銆屽叧闂€嶇鐢?
- 鍒锋柊 = 寮哄埗閲嶆寕杞斤紙闈炰粎 router 鍐嶈繘锛?
- 鍏ㄥ睆 = 鍐呭鍖哄叏灞忥紙闅愯棌渚ф爮 + 椤舵爮锛屼繚鐣欐爣绛炬爮锛夛紱Esc 鍙€€鍑猴紱涓嶆寔涔呭寲
- 鑿滃崟浜旈」涓庢埅鍥句竴鑷达紱涓嶅仛鍏抽棴宸?鍙炽€佷笉鍋氭祻瑙堝櫒 Fullscreen API
- 鏂囨閿墠缂€锛歚layout.tabMenu.*`锛坺h-CN / en-US / ja-JP锛?
- 鏈粨搴?`Seven.Vue3` 鏃犲崟鍏冩祴璇曟鏋讹紱鍚?Task 浠ユ墜宸ラ獙鏀朵唬鏇胯嚜鍔ㄥ寲娴嬭瘯
- 鎻愪氦锛氫粎鍦ㄧ敤鎴锋槑纭姹傛椂 `git commit`锛堟湰浠撳簱鐢ㄦ埛瑙勫垯浼樺厛浜庤鍒掗粯璁ゃ€宖requent commits銆嶏級

---

## File Structure

| 鏂囦欢 | 鑱岃矗 |
|------|------|
| `Seven.Vue3/src/locales/lang/zh-CN.json` | `layout.tabMenu.*` 涓枃 |
| `Seven.Vue3/src/locales/lang/en-US.json` | `layout.tabMenu.*` 鑻辨枃 |
| `Seven.Vue3/src/locales/lang/ja-JP.json` | `layout.tabMenu.*` 鏃ユ枃 |
| `Seven.Vue3/src/stores/index.ts` | `closeOthers` / `closeAll`锛涘彲閫?`keepAliveExclude` |
| `Seven.Vue3/src/layout/MainLayout.vue` | 鍙抽敭鑿滃崟銆佸叧闂鑸€佸埛鏂般€佸叏灞?|

鏃犳柊寤烘枃浠躲€?

---

### Task 1: 澧炲姞 `layout.tabMenu` 涓夎鏂囨

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/zh-CN.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/en-US.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/ja-JP.json`

**Interfaces:**
- Consumes: 鏃?
- Produces: i18n keys `layout.tabMenu.refresh|close|closeOthers|closeAll|fullscreen|exitFullscreen`

- [x] **Step 1: 鏇存柊 zh-CN `layout` 瀵硅薄**

灏?`homeTab` 鍚庢敼涓猴細

```json
"homeTab": "棣栭〉",
"tabMenu": {
  "refresh": "鍒锋柊",
  "close": "鍏抽棴",
  "closeOthers": "鍏抽棴鍏跺畠",
  "closeAll": "鍏ㄩ儴鍏抽棴",
  "fullscreen": "褰撳墠椤靛叏灞?,
  "exitFullscreen": "閫€鍑哄叏灞?
}
```

- [x] **Step 2: 鏇存柊 en-US `layout` 瀵硅薄**

```json
"homeTab": "Home",
"tabMenu": {
  "refresh": "Refresh",
  "close": "Close",
  "closeOthers": "Close Others",
  "closeAll": "Close All",
  "fullscreen": "Fullscreen Page",
  "exitFullscreen": "Exit Fullscreen"
}
```

- [x] **Step 3: 鏇存柊 ja-JP `layout` 瀵硅薄**

```json
"homeTab": "銉涖兗銉?,
"tabMenu": {
  "refresh": "鏇存柊",
  "close": "闁夈仒銈?,
  "closeOthers": "浠栥倰闁夈仒銈?,
  "closeAll": "銇欍伖銇﹂枆銇樸倠",
  "fullscreen": "銉氥兗銈搞倰鍏ㄧ敾闈?,
  "exitFullscreen": "鍏ㄧ敾闈倰绲備簡"
}
```

- [x] **Step 4: 鏍￠獙 JSON**

鍦?`Seven.Master/Seven.Vue3` 鐩綍杩愯锛?

```powershell
node -e "JSON.parse(require('fs').readFileSync('src/locales/lang/zh-CN.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/en-US.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/ja-JP.json','utf8')); console.log('ok')"
```

Expected: 鎵撳嵃 `ok`

---

### Task 2: 鎵╁睍 `useTabsStore` 鎵归噺鍏抽棴涓?keep-alive 鎺掗櫎

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/stores/index.ts`

**Interfaces:**
- Consumes: 鐜版湁 `TabItem`銆乣tabs`銆乣activeTab`銆乣removeTab` 妯″紡
- Produces:
  - `closeOthers(path: string): void` 鈥?淇濈暀 `/home` 涓?`path`锛宍activeTab = path`
  - `closeAll(): void` 鈥?浠呬繚鐣?`/home`锛宍activeTab = '/home'`
  - `keepAliveExclude: Ref<string[]>`
  - `setKeepAliveExclude(names: string[]): void`
  - `keepAliveIncludes` 璁＄畻鏃舵帓闄?`keepAliveExclude` 涓殑 name

- [x] **Step 1: 鍦?`useTabsStore` 鍐呭鍔?exclude 涓庢壒閲忓叧闂?*

灏嗘爣绛?store 鏇挎崲涓猴紙淇濈暀鏂囦欢鍐呭叾瀹?store 涓嶅姩锛夛細

```ts
/** 鏍囩椤?Store */
export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<TabItem[]>([
    { title: '棣栭〉', path: '/home', componentName: 'Home' },
  ])
  const activeTab = ref('/home')
  /** 鍒锋柊鏃剁煭鏆傛帓闄わ紝閬垮厤 keep-alive 鍥炲～鏃у疄渚?*/
  const keepAliveExclude = ref<string[]>([])

  const keepAliveIncludes = computed(() => {
    const excluded = new Set(keepAliveExclude.value)
    const names = tabs.value
      .map((t) => t.componentName)
      .filter((n): n is string => !!n && !excluded.has(n))
    return [...new Set(names)]
  })

  function setKeepAliveExclude(names: string[]) {
    keepAliveExclude.value = names
  }

  function addTab(title: string, path: string, componentName?: string) {
    const existing = tabs.value.find((t) => t.path === path)
    if (existing) {
      if (componentName && !existing.componentName) existing.componentName = componentName
    } else {
      tabs.value.push({ title, path, componentName })
    }
    activeTab.value = path
  }

  function removeTab(path: string) {
    if (path === '/home') return
    const list = tabs.value.filter((t) => t.path !== path)
    tabs.value = list
    if (activeTab.value === path) {
      activeTab.value = list[list.length - 1]?.path ?? '/home'
    }
  }

  function closeOthers(path: string) {
    tabs.value = tabs.value.filter((t) => t.path === '/home' || t.path === path)
    activeTab.value = path
  }

  function closeAll() {
    tabs.value = tabs.value.filter((t) => t.path === '/home')
    activeTab.value = '/home'
  }

  return {
    tabs,
    activeTab,
    keepAliveIncludes,
    keepAliveExclude,
    setKeepAliveExclude,
    addTab,
    removeTab,
    closeOthers,
    closeAll,
  }
})
```

- [x] **Step 2: 绫诲瀷妫€鏌?*

鍦?`Seven.Master/Seven.Vue3` 杩愯锛?

```powershell
npx vue-tsc --noEmit
```

Expected: 鏃犱笌 `stores/index.ts` 鐩稿叧鐨勬柊澧為敊璇紙鑻ラ」鐩凡鏈夋棤鍏抽敊璇彲蹇界暐锛屼絾鏈枃浠舵敼鍔ㄦ湰韬笉寰楀紩鍏ユ柊閿欙級

---

### Task 3: MainLayout 鍙抽敭鑿滃崟 + 鍏抽棴绫绘搷浣?

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/layout/MainLayout.vue`

**Interfaces:**
- Consumes: Task 1 鏂囨锛汿ask 2 `closeOthers` / `closeAll`锛涚幇鏈?`removeTab`
- Produces: 鍙抽敭鑿滃崟 UI锛沗openTabMenu` / `closeTabMenu` / `onTabMenuCommand`锛涚粺涓€ `closeTab`锛堝彸鈫掑乏鈫掗椤碉級

- [x] **Step 1: 鎵╁睍 template 鈥?鏍瑰鍣?class銆佹爣绛惧彸閿€佽彍鍗曟诞灞?*

1. 鏍硅妭鐐规敼涓猴細

```vue
<el-container class="layout-container" :class="{ 'is-content-fullscreen': isContentFullscreen }">
```

锛坄isContentFullscreen` 鏈?Task 鍏堝０鏄庝负 `ref(false)`锛孴ask 4 鍐嶆帴绾匡紱鏈浠呭姞 class 缁戝畾锛屼笉褰卞搷甯冨眬銆傦級

2. 姣忎釜 `.tab-item` 澧炲姞 `@contextmenu.prevent="openTabMenu($event, tab.path)"`銆?

3. 鍦?`.tabs-bar` 闂悎鍚庛€乣el-main` 鍓嶅鍔犺彍鍗曪紙Teleport 鍒?body锛夛細

```vue
<Teleport to="body">
  <ul
    v-show="tabMenuVisible"
    class="tab-context-menu"
    :style="{ left: `${tabMenuX}px`, top: `${tabMenuY}px` }"
    @click.stop
  >
    <li @click="onTabMenuCommand('refresh')">
      <el-icon><Refresh /></el-icon>{{ t('layout.tabMenu.refresh') }}
    </li>
    <li
      :class="{ disabled: tabMenuPath === '/home' }"
      @click="onTabMenuCommand('close')"
    >
      <el-icon><Close /></el-icon>{{ t('layout.tabMenu.close') }}
    </li>
    <li @click="onTabMenuCommand('closeOthers')">
      <el-icon><CircleClose /></el-icon>{{ t('layout.tabMenu.closeOthers') }}
    </li>
    <li @click="onTabMenuCommand('closeAll')">
      <el-icon><FolderDelete /></el-icon>{{ t('layout.tabMenu.closeAll') }}
    </li>
    <li @click="onTabMenuCommand('fullscreen')">
      <el-icon><FullScreen /></el-icon>
      {{ isContentFullscreen ? t('layout.tabMenu.exitFullscreen') : t('layout.tabMenu.fullscreen') }}
    </li>
  </ul>
</Teleport>
```

- [x] **Step 2: 鎵╁睍 script 鈥?import銆佺姸鎬併€佸叧闂笌鑿滃崟閫昏緫**

鍦ㄧ幇鏈?icons import 涓鍔狅細`CircleClose`, `FolderDelete`, `FullScreen`, `Refresh`銆?

鍦?`tabsStore` 鏃佸鍔犵姸鎬佷笌鍑芥暟锛堟斁鍦ㄧ幇鏈?`closeTab` 闄勮繎锛?*鏇挎崲**鍘?`closeTab`锛夛細

```ts
const tabMenuVisible = ref(false)
const tabMenuX = ref(0)
const tabMenuY = ref(0)
const tabMenuPath = ref('/home')
const isContentFullscreen = ref(false)
const viewKey = ref(0)

function closeTabMenu() {
  tabMenuVisible.value = false
}

function openTabMenu(e: MouseEvent, path: string) {
  tabMenuPath.value = path
  const menuW = 160
  const menuH = 180
  tabMenuX.value = Math.min(e.clientX, window.innerWidth - menuW - 8)
  tabMenuY.value = Math.min(e.clientY, window.innerHeight - menuH - 8)
  tabMenuVisible.value = true
}

function nextPathAfterClose(path: string): string {
  const list = tabsStore.tabs
  const idx = list.findIndex((t) => t.path === path)
  if (idx < 0) return tabsStore.activeTab
  return list[idx + 1]?.path ?? list[idx - 1]?.path ?? '/home'
}

function closeTab(path: string) {
  if (path === '/home') return
  const wasActive = tabsStore.activeTab === path || route.path === path
  const next = wasActive ? nextPathAfterClose(path) : tabsStore.activeTab
  tabsStore.removeTab(path)
  if (wasActive) {
    tabsStore.activeTab = next
    if (route.path !== next) router.push(next)
  }
}

function closeOtherTabs(path: string) {
  tabsStore.closeOthers(path)
  if (route.path !== path) router.push(path)
}

function closeAllTabs() {
  tabsStore.closeAll()
  if (route.path !== '/home') router.push('/home')
}

async function refreshTab(path: string) {
  if (tabsStore.activeTab !== path || route.path !== path) {
    switchTab(path)
    await router.isReady()
  }
  const tab = tabsStore.tabs.find((t) => t.path === path)
  const name = tab?.componentName
  if (name) tabsStore.setKeepAliveExclude([name])
  viewKey.value += 1
  await Promise.resolve()
  tabsStore.setKeepAliveExclude([])
}

function toggleContentFullscreen() {
  isContentFullscreen.value = !isContentFullscreen.value
}

function onTabMenuCommand(cmd: string) {
  const path = tabMenuPath.value
  closeTabMenu()
  if (cmd === 'refresh') {
    void refreshTab(path)
    return
  }
  if (cmd === 'close') {
    if (path === '/home') return
    closeTab(path)
    return
  }
  if (cmd === 'closeOthers') {
    closeOtherTabs(path)
    return
  }
  if (cmd === 'closeAll') {
    closeAllTabs()
    return
  }
  if (cmd === 'fullscreen') {
    if (tabsStore.activeTab !== path) switchTab(path)
    toggleContentFullscreen()
  }
}
```

灏?`router-view` 鍐?component 鐨?key 鏀逛负锛?

```vue
<component
  :is="Component"
  v-if="Component"
  :key="`${route.fullPath}-${viewKey}`"
/>
```

鍦?`onMounted` 澧炲姞锛?

```ts
document.addEventListener('click', closeTabMenu)
```

鍦?`onUnmounted` 澧炲姞锛?

```ts
document.removeEventListener('click', closeTabMenu)
```

- [x] **Step 3: 澧炲姞鑿滃崟鏍峰紡锛坰coped 澶栭渶 `:global` 鎴栧幓鎺?scoped 闄愬埗锛?*

鍥?Teleport 鍒?`body`锛屽湪 `<style scoped>` **涔嬪**鍙﹀姞涓€鍧楅潪 scoped锛?

```vue
<style>
.tab-context-menu {
  position: fixed;
  z-index: 4000;
  margin: 0;
  padding: 4px 0;
  min-width: 148px;
  list-style: none;
  background: var(--seven-bg-panel, #fff);
  border: 1px solid var(--seven-border-light, #e2e8f0);
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
}
.tab-context-menu li {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  font-size: 13px;
  color: var(--seven-text, #0f172a);
  cursor: pointer;
}
.tab-context-menu li:hover:not(.disabled) {
  color: var(--seven-accent, #f97316);
  background: rgba(249, 115, 22, 0.08);
}
.tab-context-menu li.disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>
```

- [x] **Step 4: 鎵嬪伐楠屾敹鍏抽棴绫?*

1. 鎵撳紑 鈮? 涓笟鍔℃爣绛?+ 棣栭〉
2. 鍙抽敭闈為椤碉細鍏抽棴 鈫?璇ユ爣绛炬秷澶憋紝鑻ヤ负褰撳墠椤靛垯璺冲埌鐩搁偦椤?
3. 鍙抽敭鏌愪笟鍔￠〉锛氬叧闂叾瀹?鈫?浠呭墿棣栭〉 + 璇ラ〉
4. 鍏ㄩ儴鍏抽棴 鈫?浠呴椤?
5. 鍙抽敭棣栭〉锛氥€屽叧闂€嶄负 disabled锛岀偣鏃犳晥鏋?

Expected: 琛屼负绗﹀悎涓婂垪锛涜彍鍗曟牱寮忔帴杩戞埅鍥?

---

### Task 4: 鍐呭鍖哄叏灞?+ Esc + 鍒锋柊楠屾敹

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/layout/MainLayout.vue`

**Interfaces:**
- Consumes: Task 3 宸叉湁鐨?`isContentFullscreen`銆乣refreshTab`銆乣toggleContentFullscreen`
- Produces: 鍏ㄥ睆 CSS锛汦sc 鐩戝惉

- [x] **Step 1: 鍦?scoped 鏍峰紡涓鍔犲叏灞忚鍒?*

```css
.layout-container.is-content-fullscreen .aside,
.layout-container.is-content-fullscreen .header {
  display: none;
}

.layout-container.is-content-fullscreen .main-wrap {
  width: 100%;
}

.layout-container.is-content-fullscreen .main-content {
  height: calc(100vh - var(--seven-tabs-height));
}
```

- [x] **Step 2: 缁戝畾 Esc**

```ts
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && isContentFullscreen.value) {
    isContentFullscreen.value = false
  }
}
```

`onMounted`锛歚document.addEventListener('keydown', onKeydown)`  
`onUnmounted`锛歚document.removeEventListener('keydown', onKeydown)`

- [x] **Step 3: 鎵嬪伐楠屾敹鍏ㄥ睆涓庡埛鏂?*

1. 鍙抽敭褰撳墠涓氬姟椤?鈫?褰撳墠椤靛叏灞忥細渚ф爮涓庨《鏍忔秷澶憋紝鏍囩鏍忎粛鍦紱鑿滃崟椤瑰彉涓恒€岄€€鍑哄叏灞忋€?
2. Esc 鎴栧啀鐐广€岄€€鍑哄叏灞忋€嶁啋 甯冨眬鎭㈠
3. 鍦ㄥ甫绛涢€?琛ㄥ崟鑽夌鐨勯〉鍙抽敭銆屽埛鏂般€嶁啋 鐘舵€侀噸缃紙缁勪欢閲嶆寕杞斤級
4. 鍙抽敭闈炲綋鍓嶉〉銆屽埛鏂般€嶁啋 鍏堝垏鎹㈠啀鍒锋柊

Expected: 鍏ㄥ睆涓庡埛鏂扮鍚堣璁￠獙鏀堕」

---

## Spec Coverage Checklist

| Spec 瑕佹眰 | Task |
|-----------|------|
| 鍙抽敭浜旈」鑿滃崟 + 鏍峰紡 | Task 1 + 3 |
| 鍏抽棴 / 鍏抽棴鍏跺畠 / 鍏ㄩ儴鍏抽棴 + 棣栭〉淇濈暀 | Task 2 + 3 |
| 寮哄埗鍒锋柊锛坘ey + keep-alive exclude锛?| Task 2 + 3 |
| 鍐呭鍖哄叏灞?+ Esc + 淇濈暀鏍囩鏍?| Task 3 + 4 |
| i18n 涓夎 | Task 1 |
| 涓嶅仛鍏抽棴宸?鍙炽€佹祻瑙堝櫒鍏ㄥ睆銆佺嫭绔嬬粍浠?| 鍏ㄥ眬绾︽潫 |

## Self-Review Notes

- 鏃?TBD/鍗犱綅锛涚鍚嶄笌 Task 闂翠竴鑷达紙`setKeepAliveExclude`銆乣closeOthers`銆乣closeAll`锛?
- `removeTab` 澧炲姞棣栭〉淇濇姢锛屼笌銆岄椤靛缁堜繚鐣欍€嶄竴鑷?
- 鎻愪氦姝ラ鎸変粨搴撹鍒欑渷鐣ワ紝鐢辩敤鎴峰彟琛岃姹?
