<template>
  <el-container class="layout-container" :class="{ 'is-content-fullscreen': isContentFullscreen }">
    <el-aside :width="sidebarWidth" class="aside dual-rail">
      <nav class="icon-rail" aria-label="primary">
        <div class="rail-logo" :title="t('layout.logoTitle')">
          <img src="../assets/icons/logo-master.svg" alt="Seven Master" class="logo-icon" />
        </div>

        <button
          type="button"
          class="rail-item"
          :class="{ active: activeRailKey === 'home' }"
          :title="t('common.home')"
          :aria-label="t('common.home')"
          @click="selectHome"
        >
          <el-icon :size="20"><HomeFilled /></el-icon>
          <span class="rail-label">{{ t('common.home') }}</span>
        </button>

        <button
          v-for="menu in railMenusMain"
          :key="menu.menu_Id"
          type="button"
          class="rail-item"
          :class="{ active: activeRailKey === String(menu.menu_Id) }"
          :title="menuLabel(menu.menuName)"
          :aria-label="menuLabel(menu.menuName)"
          @click="selectRail(menu)"
        >
          <el-icon :size="20">
            <component :is="resolveMenuIcon(menu)" />
          </el-icon>
          <span class="rail-label">{{ menuLabel(menu.menuName) }}</span>
        </button>

        <div class="rail-spacer" aria-hidden="true" />

        <button
          v-for="menu in railMenusBottom"
          :key="menu.menu_Id"
          type="button"
          class="rail-item"
          :class="{ active: activeRailKey === String(menu.menu_Id) }"
          :title="menuLabel(menu.menuName)"
          :aria-label="menuLabel(menu.menuName)"
          @click="selectRail(menu)"
        >
          <el-icon :size="20">
            <component :is="resolveMenuIcon(menu)" />
          </el-icon>
          <span class="rail-label">{{ menuLabel(menu.menuName) }}</span>
        </button>
      </nav>

      <div v-if="showSecondaryPanel" class="secondary-panel">
        <div class="panel-title-bar">
          <el-icon v-if="activeRailMenu" class="panel-title-icon" :size="16">
            <component :is="resolveMenuIcon(activeRailMenu)" />
          </el-icon>
          <span class="panel-title">{{ secondaryTitle }}</span>
        </div>

        <div class="panel-filter">
          <el-input
            v-model="menuFilter"
            clearable
            size="default"
            :placeholder="t('layout.menuFilter')"
          />
        </div>

        <el-scrollbar class="panel-scroll">
          <template v-for="node in filteredSecondaryNodes" :key="node.key">
            <div v-if="node.kind === 'group'" class="menu-group">
              <div class="menu-group-header">
                <el-icon :size="14" class="group-icon">
                  <component :is="resolveMenuIcon(node.folder)" />
                </el-icon>
                <span>{{ node.label }}</span>
              </div>
              <button
                v-for="leaf in node.leaves"
                :key="leaf.menu_Id"
                type="button"
                class="menu-leaf is-nested"
                :class="{ active: isLeafActive(leaf) }"
                :aria-current="isLeafActive(leaf) ? 'page' : undefined"
                @click="onMenuClick(leaf)"
              >
                <el-icon :size="15" class="leaf-icon">
                  <component :is="resolveMenuIcon(leaf)" />
                </el-icon>
                <span class="leaf-text">{{ menuLabel(leaf.menuName) }}</span>
              </button>
            </div>
            <button
              v-else
              type="button"
              class="menu-leaf"
              :class="{ active: isLeafActive(node.menu) }"
              :aria-current="isLeafActive(node.menu) ? 'page' : undefined"
              @click="onMenuClick(node.menu)"
            >
              <el-icon :size="15" class="leaf-icon">
                <component :is="resolveMenuIcon(node.menu)" />
              </el-icon>
              <span class="leaf-text">{{ menuLabel(node.menu.menuName) }}</span>
            </button>
          </template>
          <div v-if="filteredSecondaryNodes.length === 0" class="panel-empty">
            {{ t('layout.menuEmpty') }}
          </div>
        </el-scrollbar>
      </div>
    </el-aside>

    <el-container class="main-wrap">
      <el-header class="header">
        <div class="header-left">
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/home' }">{{ t('common.home') }}</el-breadcrumb-item>
            <el-breadcrumb-item v-if="currentTitle">{{ currentTitle }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <div class="header-meta">
            <span class="meta-item"><el-icon><Clock /></el-icon>{{ currentTime }}</span>
            <span class="meta-item"><el-icon><Connection /></el-icon>{{ onlineLabel }}</span>
          </div>
          <AlarmBell v-if="featureStore.alarmEnabled" />
          <LocaleSwitch />
          <ThemeToggle />
          <el-dropdown trigger="click" @command="handleCommand">
            <div class="user-block cursor-pointer">
              <el-avatar :size="32" class="user-avatar">{{ avatarText }}</el-avatar>
              <span class="user-name">{{ userStore.userTrueName || userStore.userName }}</span>
              <el-icon><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">
                  <el-icon><User /></el-icon>{{ t('userInfo.title') }}
                </el-dropdown-item>
                <el-dropdown-item command="logout" divided>
                  <el-icon><SwitchButton /></el-icon>{{ t('common.logout') }}
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <div class="tabs-bar">
        <div
          v-for="tab in tabsStore.tabs"
          :key="tab.path"
          class="tab-item cursor-pointer"
          :class="{ active: tabsStore.activeTab === tab.path }"
          @click="switchTab(tab.path)"
          @contextmenu.prevent="openTabMenu($event, tab.path)"
        >
          {{ tabLabel(tab) }}
          <el-icon
            v-if="tab.path !== '/home'"
            class="tab-close"
            @click.stop="closeTab(tab.path)"
          ><Close /></el-icon>
        </div>
      </div>

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

      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <keep-alive :include="tabsStore.keepAliveIncludes">
            <component
              :is="Component"
              v-if="Component"
              :key="`${route.fullPath}-${viewKey}`"
            />
          </keep-alive>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  ArrowDown,
  CircleClose,
  Clock,
  Close,
  Connection,
  FolderDelete,
  FullScreen,
  HomeFilled,
  Refresh,
  SwitchButton,
  User,
} from '@element-plus/icons-vue'
import { useUserStore } from '../stores/user'
import { useMenuStore, useTabsStore, type MenuItem } from '../stores'
import { useMenuLabel } from '../composables/useMenuLabel'
import { getMenuIcon } from '../utils/menuIcons'
import ThemeToggle from '../components/ThemeToggle.vue'
import LocaleSwitch from '../components/LocaleSwitch.vue'
import AlarmBell from '../components/AlarmBell.vue'
import { useAlarmHub } from '../composables/useAlarmHub'
import { useMessageHub } from '../composables/useMessageHub'
import { useFeatureStore } from '../stores/features'

const { t, locale } = useI18n()
const { menuLabel } = useMenuLabel()
const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const menuStore = useMenuStore()
const tabsStore = useTabsStore()
const featureStore = useFeatureStore()

useAlarmHub()
const { onlineCount, hubConnected: messageHubConnected } = useMessageHub()

const onlineLabel = computed(() => {
  if (featureStore.signalREnabled && messageHubConnected.value && onlineCount.value > 0) {
    return t('layout.onlineUsers', { count: onlineCount.value })
  }
  return t('layout.systemOnline')
})

const RAIL_WIDTH = 84
const PANEL_WIDTH = 200
const menuFilter = ref('')
const activeRailKey = ref<string>('home')

type SecondaryNode =
  | { kind: 'group'; key: string; label: string; folder: MenuItem; leaves: MenuItem[] }
  | { kind: 'leaf'; key: string; menu: MenuItem }

/** OrderNo ≥ 此值，或运维目录：钉在轨底部 */
const RAIL_BOTTOM_ORDER = 90

function isRailBottomMenu(menu: MenuItem) {
  if ((menu.orderNo ?? 0) >= RAIL_BOTTOM_ORDER) return true
  const table = (menu.tableName || '').toLowerCase()
  const name = menu.menuName || ''
  return table === 'wcsopsfolder' || name === '执行运维'
}

const railMenus = computed(() => menuStore.menus)
const railMenusMain = computed(() => railMenus.value.filter((m) => !isRailBottomMenu(m)))
const railMenusBottom = computed(() => railMenus.value.filter((m) => isRailBottomMenu(m)))

const activeRailMenu = computed(() =>
  railMenus.value.find((m) => String(m.menu_Id) === activeRailKey.value) ?? null,
)

const showSecondaryPanel = computed(
  () =>
    activeRailKey.value !== 'home' &&
    (activeRailMenu.value?.children?.length ?? 0) > 0,
)

const secondaryTitle = computed(() => {
  if (!activeRailMenu.value) return ''
  return menuLabel(activeRailMenu.value.menuName)
})

const sidebarWidth = computed(() =>
  showSecondaryPanel.value ? `${RAIL_WIDTH + PANEL_WIDTH}px` : `${RAIL_WIDTH}px`,
)

function resolveMenuIcon(menu: MenuItem) {
  return getMenuIcon(menu.menuName, menu.icon)
}

function isFolder(menu: MenuItem) {
  return !menu.url && (menu.children?.length ?? 0) > 0
}

function isLeaf(menu: MenuItem) {
  return !!menu.url
}

function buildSecondaryNodes(menu: MenuItem | null): SecondaryNode[] {
  if (!menu?.children?.length) return []
  const nodes: SecondaryNode[] = []
  for (const child of menu.children) {
    if (isFolder(child)) {
      const leaves = (child.children ?? []).filter(isLeaf)
      if (leaves.length || menuFilter.value) {
        nodes.push({
          kind: 'group',
          key: `g-${child.menu_Id}`,
          label: menuLabel(child.menuName),
          folder: child,
          leaves,
        })
      }
    } else if (isLeaf(child)) {
      nodes.push({ kind: 'leaf', key: `l-${child.menu_Id}`, menu: child })
    }
  }
  return nodes
}

const secondaryNodes = computed(() => buildSecondaryNodes(activeRailMenu.value))

const filteredSecondaryNodes = computed(() => {
  const q = menuFilter.value.trim().toLowerCase()
  if (!q) return secondaryNodes.value
  const out: SecondaryNode[] = []
  for (const node of secondaryNodes.value) {
    if (node.kind === 'leaf') {
      if (menuLabel(node.menu.menuName).toLowerCase().includes(q)) out.push(node)
      continue
    }
    const leaves = node.leaves.filter((l) =>
      menuLabel(l.menuName).toLowerCase().includes(q),
    )
    if (leaves.length || node.label.toLowerCase().includes(q)) {
      out.push({ ...node, leaves: leaves.length ? leaves : node.leaves })
    }
  }
  return out
})

function isLeafActive(menu: MenuItem) {
  return !!menu.url && (route.path === menu.url || route.path.startsWith(`${menu.url}/`))
}

function findRailKeyForPath(path: string): string {
  if (path === '/home') return 'home'
  for (const top of railMenus.value) {
    if (top.url && (path === top.url || path.startsWith(`${top.url}/`))) {
      return String(top.menu_Id)
    }
    const stack = [...(top.children ?? [])]
    while (stack.length) {
      const cur = stack.pop()!
      if (cur.url && (path === cur.url || path.startsWith(`${cur.url}/`))) {
        return String(top.menu_Id)
      }
      if (cur.children?.length) stack.push(...cur.children)
    }
  }
  return activeRailKey.value
}

function selectHome() {
  activeRailKey.value = 'home'
  menuFilter.value = ''
  tabsStore.activeTab = '/home'
  if (route.path !== '/home') router.push('/home')
}

function selectRail(menu: MenuItem) {
  activeRailKey.value = String(menu.menu_Id)
  menuFilter.value = ''
  if (menu.url && !menu.children?.length) {
    onMenuClick(menu)
  }
}

const currentTime = ref('')

const avatarText = computed(() => {
  const name = userStore.userTrueName || userStore.userName || 'U'
  return name.charAt(0).toUpperCase()
})

const currentTitle = computed(() => {
  if (route.path === '/home') return ''
  const tab = tabsStore.tabs.find((item) => item.path === route.path)
  return tab ? tabLabel(tab) : ''
})

function tabLabel(tab: { title: string; path: string }) {
  if (tab.path === '/home') return t('layout.homeTab')
  return menuLabel(tab.title)
}

function updateTime() {
  currentTime.value = new Date().toLocaleString(locale.value, { hour12: false })
}

function onMenuClick(menu: MenuItem) {
  if (!menu.url) return
  const componentName = menu.tableName || (router.resolve(menu.url).name as string | undefined)
  tabsStore.addTab(menu.menuName, menu.url, componentName)
  if (route.path !== menu.url) {
    void router.push(menu.url)
  }
}

function switchTab(path: string) {
  tabsStore.activeTab = path
  router.push(path)
}

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

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && isContentFullscreen.value) {
    isContentFullscreen.value = false
  }
}

function handleLogout() {
  userStore.logout()
  menuStore.routesLoaded = false
  menuStore.setMenus([])
  router.push('/login')
}

function handleCommand(cmd: string) {
  if (cmd === 'logout') handleLogout()
  else if (cmd === 'profile') {
    tabsStore.addTab(t('userInfo.title'), '/UserInfo', 'UserInfo')
    router.push('/UserInfo')
  }
}

watch(locale, updateTime)

watch(
  () => route.path,
  (path) => {
    activeRailKey.value = findRailKeyForPath(path)
  },
  { immediate: true },
)

watch(
  () => menuStore.menus,
  () => {
    activeRailKey.value = findRailKeyForPath(route.path)
  },
)

onMounted(() => {
  updateTime()
  timer = setInterval(updateTime, 1000)
  document.addEventListener('click', closeTabMenu)
  document.addEventListener('keydown', onKeydown)
})

let timer: ReturnType<typeof setInterval> | undefined

onUnmounted(() => {
  if (timer) clearInterval(timer)
  document.removeEventListener('click', closeTabMenu)
  document.removeEventListener('keydown', onKeydown)
})
</script>

<style scoped>
.layout-container {
  height: 100vh;
  background: var(--seven-bg-page);
}

.aside.dual-rail {
  display: flex;
  flex-direction: row;
  padding: 0;
  background: var(--seven-bg-charcoal);
  border-right: 1px solid var(--seven-rail-border);
  overflow: hidden;
}

.icon-rail {
  width: var(--seven-rail-width, 84px);
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 8px 5px 10px;
  background: var(--seven-bg-charcoal);
  border-right: 1px solid rgba(148, 163, 184, 0.1);
}

.rail-spacer {
  flex: 1 1 auto;
  min-height: 8px;
  width: 100%;
}

.rail-logo {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 40px;
  margin-bottom: 4px;
  padding-bottom: 6px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
}

.logo-icon {
  width: 26px;
  height: 26px;
}

.rail-item {
  width: 100%;
  min-height: 52px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 3px;
  padding: 6px 3px 5px;
  border: 1px solid transparent;
  border-radius: var(--seven-radius-sm);
  background: transparent;
  color: var(--seven-rail-text);
  cursor: pointer;
  transition: background-color 0.2s ease, color 0.2s ease, border-color 0.2s ease;
}

.rail-item :deep(.el-icon) {
  font-size: 20px;
}

.rail-item:hover {
  background: var(--seven-bg-charcoal-hover);
  color: var(--seven-rail-text-hover);
}

.rail-item:focus-visible {
  outline: 2px solid var(--seven-accent-gold);
  outline-offset: 1px;
}

.rail-item.active {
  color: var(--seven-accent-gold);
  background: var(--seven-bg-charcoal-active);
  border-color: var(--seven-accent-gold-strong);
  box-shadow: inset 0 0 0 1px rgba(201, 162, 39, 0.2);
}

.rail-label {
  max-width: 74px;
  font-size: 11px;
  line-height: 1.2;
  font-weight: 500;
  text-align: center;
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  overflow: hidden;
  word-break: break-all;
}

.secondary-panel {
  width: var(--seven-secondary-menu-width, 200px);
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  background: var(--seven-bg-secondary-menu);
  border-right: 1px solid var(--seven-border-light);
}

.panel-title-bar {
  min-height: 40px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  background: var(--seven-panel-title-bg);
  color: var(--seven-panel-title-text);
  border-bottom: 2px solid var(--seven-accent-gold);
}

.panel-title-icon {
  flex-shrink: 0;
  color: var(--seven-accent-gold);
}

.panel-title {
  font-family: var(--seven-font-mono);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.03em;
  color: var(--seven-panel-title-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.panel-filter {
  padding: 8px 10px;
  background: var(--seven-bg-secondary-menu);
  border-bottom: 1px solid var(--seven-border-light);
}

.panel-scroll {
  flex: 1;
  padding: 8px 6px 12px;
}

.menu-group {
  margin: 0 0 8px;
  padding: 6px 0 4px;
  border: 1px solid var(--seven-border-light);
  border-left: 3px solid var(--seven-accent-gold);
  background: var(--seven-bg-group-soft);
  border-radius: var(--seven-radius-sm);
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}

.menu-group-header {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0 8px 4px;
  padding-bottom: 5px;
  border-bottom: 1px solid var(--seven-border-light);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.02em;
  color: var(--seven-group-title);
}

.group-icon {
  color: var(--seven-accent-gold-strong);
  flex-shrink: 0;
}

.menu-leaf {
  width: calc(100% - 6px);
  min-height: 36px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 8px;
  margin: 0 3px 1px;
  border: none;
  border-radius: var(--seven-radius-sm);
  background: transparent;
  color: var(--seven-primary-dark);
  text-align: left;
  cursor: pointer;
  transition: background-color 0.2s ease, color 0.2s ease;
}

.menu-leaf.is-nested {
  padding-left: 12px;
}

.menu-leaf:hover {
  background: rgba(15, 23, 42, 0.05);
  color: var(--seven-primary-dark);
}

.menu-leaf:hover .leaf-icon {
  color: var(--seven-accent-gold-strong);
}

.menu-leaf:focus-visible {
  outline: 2px solid var(--seven-accent-gold);
  outline-offset: 1px;
}

.menu-leaf.active {
  background: var(--seven-accent-gold-soft);
  color: var(--seven-leaf-active-text);
  font-weight: 600;
  box-shadow: inset 2px 0 0 var(--seven-accent-gold);
}

.leaf-icon {
  flex-shrink: 0;
  color: var(--seven-leaf-icon);
  transition: color 0.2s ease;
}

.menu-leaf.active .leaf-icon {
  color: var(--seven-accent-gold-strong);
}

.leaf-text {
  flex: 1;
  min-width: 0;
  font-size: 12.5px;
  line-height: 1.35;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.panel-empty {
  padding: 14px 8px;
  font-size: 12px;
  color: var(--seven-text-muted);
  text-align: center;
}

@media (prefers-reduced-motion: reduce) {
  .rail-item,
  .menu-leaf,
  .leaf-icon {
    transition: none;
  }
}

.main-wrap {
  min-width: 0;
}

.header {
  height: var(--seven-header-height);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  background: var(--seven-bg-panel);
  border-bottom: 1px solid var(--seven-border-light);
  box-shadow: 0 1px 0 rgba(15, 23, 42, 0.04);
}

.header-left :deep(.el-breadcrumb__inner) {
  font-family: var(--seven-font-mono);
  font-size: 13px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-meta {
  display: flex;
  gap: 16px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.user-block {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background-color 0.2s ease;
}

.user-block:hover {
  background: var(--seven-accent-soft);
}

.user-avatar {
  background: var(--seven-primary);
  color: #fff;
  font-family: var(--seven-font-mono);
  font-size: 14px;
}

.user-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--seven-text);
}

.tabs-bar {
  display: flex;
  align-items: center;
  gap: var(--seven-space-1);
  height: var(--seven-tabs-height);
  padding: 0 var(--seven-space-3);
  background: var(--seven-bg-subtle);
  border-bottom: 1px solid var(--seven-border-light);
  overflow-x: auto;
}

.tab-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  font-size: 12px;
  color: var(--seven-text-muted);
  background: transparent;
  border-radius: 4px 4px 0 0;
  border: 1px solid transparent;
  white-space: nowrap;
  transition: color 0.2s ease, background-color 0.2s ease, border-color 0.2s ease;
}

.tab-item:hover {
  color: var(--seven-text);
  background: var(--seven-bg-panel);
  opacity: 0.85;
}

.tab-item.active {
  color: var(--seven-accent);
  background: var(--seven-bg-panel);
  border-color: var(--seven-border-light);
  border-bottom-color: var(--seven-bg-panel);
  font-weight: 600;
}

.tab-close {
  font-size: 12px;
  border-radius: 2px;
  transition: color 0.2s ease;
}

.tab-close:hover {
  color: var(--seven-danger);
}

.main-content {
  padding: var(--seven-space-3) var(--seven-space-4) var(--seven-space-5);
  background: var(--seven-bg-page);
  overflow: auto;
}

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

@media (max-width: 768px) {
  .header-meta {
    display: none;
  }

  .rail-label {
    display: none;
  }
}
</style>

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
