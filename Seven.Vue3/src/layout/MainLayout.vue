<template>
  <el-container class="layout-container" :class="{ 'is-content-fullscreen': isContentFullscreen }">
    <el-aside :width="sidebarWidth" class="aside dual-rail">
      <nav class="icon-rail" :aria-label="primaryNavigationLabel">
        <div class="rail-logo" :title="t('layout.logoTitle')">
          <img src="../assets/icons/logo-master.svg" :alt="t('layout.logoTitle')" class="logo-icon" />
        </div>

        <button
          type="button"
          class="rail-item"
          :class="{ active: activeRailKey === 'home' }"
          :title="t('common.home')"
          :aria-label="t('common.home')"
          :aria-current="activeRailKey === 'home' ? 'page' : undefined"
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
          :aria-current="activeRailKey === String(menu.menu_Id) ? 'page' : undefined"
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
          :aria-current="activeRailKey === String(menu.menu_Id) ? 'page' : undefined"
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
            :aria-label="t('layout.menuFilter')"
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
                :title="menuLabel(leaf.menuName)"
                :aria-label="menuLabel(leaf.menuName)"
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
              :title="menuLabel(node.menu.menuName)"
              :aria-label="menuLabel(node.menu.menuName)"
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
          <div class="header-context">
            <el-breadcrumb class="context-trail" separator="/">
              <el-breadcrumb-item :to="{ path: '/home' }">{{ t('common.home') }}</el-breadcrumb-item>
              <el-breadcrumb-item v-if="currentTitle">{{ currentTitle }}</el-breadcrumb-item>
            </el-breadcrumb>
          </div>
        </div>
        <div class="header-right">
          <div class="header-status" :aria-label="headerStatusLabel">
            <div class="header-meta">
              <span class="meta-item"><el-icon><Clock /></el-icon>{{ currentTime }}</span>
              <span class="meta-item"><el-icon><Connection /></el-icon>{{ onlineLabel }}</span>
            </div>
          </div>
          <div class="header-utilities" :aria-label="headerUtilitiesLabel">
            <AlarmBell v-if="featureStore.alarmEnabled" />
            <LocaleSwitch />
            <ThemeToggle />
          </div>
          <div class="header-operator">
            <el-dropdown trigger="click" @command="handleCommand">
              <button
                type="button"
                class="user-block cursor-pointer"
                :title="operatorMenuLabel"
                :aria-label="operatorMenuLabel"
                aria-haspopup="menu"
              >
                <el-avatar :size="32" class="user-avatar">{{ avatarText }}</el-avatar>
                <span class="user-name">{{ operatorName }}</span>
                <el-icon><ArrowDown /></el-icon>
              </button>
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
        </div>
      </el-header>

      <div class="tabs-bar" :aria-label="tabsBarLabel">
        <div
          v-for="tab in tabsStore.tabs"
          :key="tab.path"
          class="tab-item"
          :class="{ active: tabsStore.activeTab === tab.path }"
          @contextmenu.prevent="openTabMenu($event, tab.path)"
        >
          <button
            type="button"
            class="tab-button"
            :title="tabLabel(tab)"
            :aria-label="tabLabel(tab)"
            :aria-current="tabsStore.activeTab === tab.path ? 'page' : undefined"
            @click="switchTab(tab.path)"
          >
            <span class="tab-label">{{ tabLabel(tab) }}</span>
          </button>
          <button
            v-if="tab.path !== '/home'"
            class="tab-close"
            type="button"
            :title="tabCloseLabel(tab)"
            :aria-label="tabCloseLabel(tab)"
            @click.stop="closeTab(tab.path)"
          >
            <el-icon><Close /></el-icon>
          </button>
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

const primaryNavigationLabel = computed(
  () => secondaryTitle.value || menuLabel(activeRailMenu.value?.menuName || '') || t('common.home'),
)

const headerStatusLabel = computed(() => [currentTime.value, onlineLabel.value].filter(Boolean).join(' · '))

const headerUtilitiesLabel = computed(() =>
  [
    featureStore.alarmEnabled ? t('alarm.bellLabel') : '',
    t('locale.label'),
    t('theme.label'),
  ]
    .filter(Boolean)
    .join(' · '),
)

const operatorName = computed(
  () => userStore.userTrueName || userStore.userName || t('userInfo.userName'),
)

const operatorMenuLabel = computed(() => `${t('userInfo.title')}: ${operatorName.value}`)

const currentTitle = computed(() => {
  if (route.path === '/home') return ''
  const tab = tabsStore.tabs.find((item) => item.path === route.path)
  return tab ? tabLabel(tab) : ''
})

function tabLabel(tab: { title: string; path: string }) {
  if (tab.path === '/home') return t('layout.homeTab')
  return menuLabel(tab.title)
}

const tabsBarLabel = computed(() => {
  const activePath = tabsStore.activeTab || route.path
  const activeTab = tabsStore.tabs.find((tab) => tab.path === activePath)
  return activeTab ? tabLabel(activeTab) : t('layout.homeTab')
})

function tabCloseLabel(tab: { title: string; path: string }) {
  return `${t('layout.tabMenu.close')} ${tabLabel(tab)}`
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
  min-width: 0;
  background: var(--seven-bg-page);
  overflow: hidden;
}

.main-wrap {
  min-width: 0;
  overflow: hidden;
}

.header {
  height: var(--seven-header-height);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--seven-space-4);
  padding: 0 16px;
  background: var(--seven-bg-panel);
  border-bottom: 1px solid var(--seven-border-light);
  box-shadow: 0 1px 0 color-mix(in srgb, var(--seven-border) 40%, transparent);
}

.header-left {
  flex: 1 1 auto;
  min-width: 0;
}

.header-context {
  display: flex;
  align-items: center;
  min-width: 0;
  padding: 5px 10px;
  border: 1px solid var(--seven-border-light);
  border-radius: 8px;
  background: color-mix(in srgb, var(--seven-bg-subtle) 78%, var(--seven-bg-panel) 22%);
}

.header-left :deep(.el-breadcrumb) {
  min-width: 0;
}

.header-left :deep(.el-breadcrumb__inner) {
  font-family: var(--seven-font-body);
  font-size: 13px;
}

.header-left :deep(.el-breadcrumb__item:last-child .el-breadcrumb__inner) {
  display: inline-block;
  max-width: min(40vw, 360px);
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
  white-space: nowrap;
}

.header-right {
  display: flex;
  align-items: center;
  min-width: 0;
  gap: 8px;
}

.header-status,
.header-utilities,
.header-operator {
  display: flex;
  align-items: center;
  min-width: 0;
  padding: 4px 8px;
  border: 1px solid var(--seven-border-light);
  border-radius: 8px;
  background: color-mix(in srgb, var(--seven-bg-subtle) 72%, var(--seven-bg-panel) 28%);
}

.header-utilities {
  gap: 6px;
}

.header-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
  white-space: nowrap;
}

.user-block {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 0;
  padding: 4px 8px;
  background: transparent;
  border-radius: 6px;
  color: inherit;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.user-block:hover {
  background: var(--seven-accent-soft);
}

.user-block:focus-visible {
  outline: 2px solid var(--seven-focus-ring);
  outline-offset: 2px;
}

.user-avatar {
  background: var(--seven-accent);
  color: #fff;
  font-family: var(--seven-font-mono);
  font-size: 14px;
}

.user-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--seven-text);
  max-width: 168px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tabs-bar {
  display: flex;
  align-items: center;
  gap: var(--seven-space-1);
  min-width: 0;
  height: var(--seven-tabs-height);
  padding: 0 var(--seven-space-3);
  background: var(--seven-bg-subtle);
  border-bottom: 1px solid var(--seven-border-light);
  overflow-x: auto;
  overflow-y: hidden;
  scrollbar-gutter: stable both-edges;
}

.tab-item {
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  gap: 0;
  min-width: 0;
  padding-top: 2px;
}

.tab-button {
  display: flex;
  align-items: center;
  min-width: 0;
  max-width: 220px;
  padding: 5px 8px 5px 10px;
  font-size: 12px;
  color: var(--seven-text-muted);
  background: transparent;
  border-radius: 4px 0 0 0;
  border: 1px solid transparent;
  white-space: nowrap;
  cursor: pointer;
  transition: color 0.2s ease, background-color 0.2s ease, border-color 0.2s ease;
}

.tab-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-button:hover,
.tab-item:hover .tab-close {
  color: var(--seven-text);
  background: var(--seven-bg-panel);
  opacity: 0.85;
}

.tab-button:focus-visible,
.tab-close:focus-visible {
  outline: 2px solid var(--seven-focus-ring);
  outline-offset: 2px;
}

.tab-item.active .tab-button,
.tab-item.active .tab-close {
  color: var(--seven-accent);
  background: var(--seven-bg-panel);
  border-color: var(--seven-border-light);
}

.tab-item.active .tab-button {
  border-bottom: 2px solid var(--seven-accent);
  font-weight: 600;
}

.tab-close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  margin-left: -1px;
  padding: 0;
  font-size: 12px;
  color: var(--seven-text-muted);
  background: transparent;
  border: 1px solid transparent;
  border-radius: 0 4px 0 0;
  cursor: pointer;
  transition: color 0.2s ease, background-color 0.2s ease, border-color 0.2s ease;
}

.tab-close:hover {
  color: var(--seven-danger);
  background: color-mix(in srgb, var(--seven-danger) 10%, var(--seven-bg-panel));
}

.main-content {
  min-width: 0;
  padding: 8px;
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
  .header {
    padding: 0 10px;
  }

  .header-status {
    display: none;
  }

  .header-context,
  .header-utilities,
  .header-operator {
    padding-inline: 6px;
  }

  .user-name {
    display: none;
  }

  .tabs-bar {
    padding: 0 8px;
  }

  .tab-button {
    max-width: 160px;
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
  border: 1px solid var(--seven-border-light, #ebeef5);
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
}
.tab-context-menu li {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  font-size: 13px;
  color: var(--seven-text, #606266);
  cursor: pointer;
}
.tab-context-menu li:hover:not(.disabled) {
  color: var(--seven-accent, #409eff);
  background: var(--seven-accent-soft, rgba(64, 158, 255, 0.12));
}
.tab-context-menu li.disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>
