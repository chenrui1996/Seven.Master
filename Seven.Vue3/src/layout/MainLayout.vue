<template>
  <el-container class="layout-container" :class="{ 'is-content-fullscreen': isContentFullscreen }">
    <el-aside :width="sidebarWidth" class="aside">
      <div class="logo-block">
        <img src="../assets/icons/logo-master.svg" alt="Seven Master" class="logo-icon" />
        <div class="logo-text">
          <span class="logo-title">{{ t('layout.logoTitle') }}</span>
          <span class="logo-sub">{{ t('layout.logoSub') }}</span>
        </div>
      </div>

      <div class="aside-status">
        <span class="seven-status-dot" />
        <span>{{ t('layout.systemRunning') }}</span>
      </div>

      <el-scrollbar class="menu-scroll">
        <el-menu
          :default-active="route.path"
          router
          class="industrial-menu"
          background-color="transparent"
          text-color="#94a3b8"
          active-text-color="#f97316"
        >
          <el-menu-item index="/home" @click="onHomeClick">
            <el-icon><HomeFilled /></el-icon>
            <span>{{ t('common.home') }}</span>
          </el-menu-item>

          <template v-for="menu in menuStore.menus" :key="menu.menu_Id">
            <el-sub-menu v-if="menu.children?.length" :index="String(menu.menu_Id)">
              <template #title>
                <el-icon><component :is="getMenuIcon(menu.menuName)" /></el-icon>
                <span>{{ menuLabel(menu.menuName) }}</span>
              </template>
              <el-menu-item
                v-for="child in menu.children"
                :key="child.menu_Id"
                :index="child.url || ''"
                @click="onMenuClick(child)"
              >
                <el-icon><component :is="getMenuIcon(child.menuName)" /></el-icon>
                <span>{{ menuLabel(child.menuName) }}</span>
              </el-menu-item>
            </el-sub-menu>
            <el-menu-item v-else-if="menu.url" :index="menu.url" @click="onMenuClick(menu)">
              <el-icon><component :is="getMenuIcon(menu.menuName)" /></el-icon>
              <span>{{ menuLabel(menu.menuName) }}</span>
            </el-menu-item>
          </template>
        </el-menu>
      </el-scrollbar>
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
                <el-dropdown-item command="logout">
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

const sidebarWidth = '240px'
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

function onHomeClick() {
  tabsStore.activeTab = '/home'
}

function onMenuClick(menu: MenuItem) {
  if (menu.url) {
    const componentName = menu.tableName || (router.resolve(menu.url).name as string | undefined)
    tabsStore.addTab(menu.menuName, menu.url, componentName)
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
}

watch(locale, updateTime)

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

.aside {
  background: var(--seven-bg-sidebar);
  border-right: 1px solid rgba(249, 115, 22, 0.25);
  display: flex;
  flex-direction: column;
  background-image:
    linear-gradient(rgba(148, 163, 184, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(148, 163, 184, 0.04) 1px, transparent 1px);
  background-size: 24px 24px;
}

.logo-block {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 12px 8px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
}

.logo-icon {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.logo-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.logo-title {
  font-family: var(--seven-font-mono);
  font-size: 15px;
  font-weight: 700;
  color: #f8fafc;
  letter-spacing: 0.04em;
}

.logo-sub {
  font-size: 11px;
  color: #64748b;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.aside-status {
  display: flex;
  align-items: center;
  padding: 8px 14px;
  font-size: 11px;
  color: #94a3b8;
  font-family: var(--seven-font-mono);
}

.menu-scroll {
  flex: 1;
}

.industrial-menu {
  border-right: none;
  padding: 6px;
}

.industrial-menu :deep(.el-menu-item),
.industrial-menu :deep(.el-sub-menu__title) {
  border-radius: var(--seven-radius-sm);
  margin-bottom: 1px;
  height: 38px;
  font-size: 13px;
  transition: background-color 0.2s ease, color 0.2s ease;
}

.industrial-menu :deep(.el-menu-item:hover),
.industrial-menu :deep(.el-sub-menu__title:hover) {
  background: var(--seven-bg-sidebar-hover) !important;
}

.industrial-menu :deep(.el-menu-item.is-active) {
  background: rgba(249, 115, 22, 0.12) !important;
  border-left: 3px solid var(--seven-accent);
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

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-4px);
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
