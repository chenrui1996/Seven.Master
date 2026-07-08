<template>
  <el-container class="layout-container">
    <el-aside width="220px" class="aside">
      <div class="logo">Seven.Master</div>
      <el-menu :default-active="route.path" router background-color="#001529" text-color="#fff" active-text-color="#409eff">
        <el-menu-item index="/home">
          <el-icon><HomeFilled /></el-icon>
          <span>首页</span>
        </el-menu-item>
        <template v-for="menu in menuStore.menus" :key="menu.menu_Id">
          <el-sub-menu v-if="menu.children?.length" :index="String(menu.menu_Id)">
            <template #title>
              <el-icon><Setting /></el-icon>
              <span>{{ menu.menuName }}</span>
            </template>
            <el-menu-item v-for="child in menu.children" :key="child.menu_Id" :index="child.url || ''" @click="onMenuClick(child)">
              {{ child.menuName }}
            </el-menu-item>
          </el-sub-menu>
          <el-menu-item v-else-if="menu.url" :index="menu.url" @click="onMenuClick(menu)">
            <span>{{ menu.menuName }}</span>
          </el-menu-item>
        </template>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <span>{{ userStore.userTrueName || userStore.userName }}</span>
        <el-button type="danger" link @click="handleLogout">退出</el-button>
      </el-header>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { HomeFilled, Setting } from '@element-plus/icons-vue'
import { useUserStore } from '../stores/user'
import { useMenuStore, useTabsStore, type MenuItem } from '../stores'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const menuStore = useMenuStore()
const tabsStore = useTabsStore()

/** 菜单点击：添加标签页 */
function onMenuClick(menu: MenuItem) {
  if (menu.url) tabsStore.addTab(menu.menuName, menu.url)
}

/** 退出登录 */
function handleLogout() {
  userStore.logout()
  menuStore.routesLoaded = false
  router.push('/login')
}
</script>

<style scoped>
.layout-container { height: 100vh; }
.aside { background: #001529; }
.logo { color: #fff; font-size: 18px; font-weight: bold; padding: 16px; text-align: center; }
.header { display: flex; align-items: center; justify-content: flex-end; gap: 16px; border-bottom: 1px solid #eee; }
</style>
