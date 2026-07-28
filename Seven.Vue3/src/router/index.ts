import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useUserStore } from '../stores/user'
import { useMenuStore, type MenuItem } from '../stores'
import { buildMenuTree, flattenMenus } from '../utils/menuTree'

/** 静态路由 */
const staticRoutes: RouteRecordRaw[] = [
  { path: '/login', name: 'Login', component: () => import('../views/Login.vue'), meta: { public: true } },
  {
    path: '/',
    name: 'Layout',
    component: () => import('../layout/MainLayout.vue'),
    redirect: '/home',
    children: [
      { path: 'home', name: 'Home', component: () => import('../views/Home.vue'), meta: { title: '首页' } }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes: staticRoutes
})

/** 动态路由组件映射（含各业务子目录，如 views/Board/Device.vue） */
const viewModules = import.meta.glob('../views/**/*.vue')

/** 按菜单 Url / TableName 解析页面组件，支持 system 以外文件夹 */
function resolveViewComponent(menu: MenuItem, routePath: string) {
  const fileName = (menu.tableName || routePath.split('/').pop() || '').trim()
  if (!fileName) return undefined

  const candidates = [
    // Url 即相对路径：/Board/Device → views/Board/Device.vue
    `../views/${routePath}.vue`,
    // 兼容历史：一律放在 system
    `../views/system/${fileName}.vue`,
    `../views/system/${routePath}.vue`,
  ]

  for (const key of candidates) {
    if (viewModules[key]) return viewModules[key]
  }

  // 回退：任意子目录下同名 vue（忽略大小写），如 Board/Device.vue
  const needle = `/${fileName}.vue`.toLowerCase()
  const matched = Object.keys(viewModules).find((k) => k.toLowerCase().endsWith(needle))
  return matched ? viewModules[matched] : undefined
}

/** 将菜单转为路由并注册（递归扁平化后注册叶子路由） */
export function generateRoutes(menus: MenuItem[]): RouteRecordRaw[] {
  const routes: RouteRecordRaw[] = []
  for (const menu of flattenMenus(menus)) {
    if (!menu.url || menu.url.startsWith('http') || menu.url === '#') continue
    const path = menu.url.startsWith('/') ? menu.url.slice(1) : menu.url
    const component = resolveViewComponent(menu, path)
    if (component != null) {
      routes.push({
        path,
        name: menu.tableName || menu.menuName,
        component,
        meta: { title: menu.menuName, permission: menu.tableName },
      })
    } else if (import.meta.env.DEV) {
      console.warn(`[router] 未找到菜单页面组件: url=${menu.url}, tableName=${menu.tableName}`)
    }
  }
  return routes
}

/** 路由守卫 */
router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()
  const menuStore = useMenuStore()

  if (to.meta.public) return next()

  if (!userStore.isLoggedIn) return next('/login')

  // 进入系统时刷新权限，避免本地缓存导致已取消授权的按钮仍显示
  if (!userStore.permissionsSynced) {
    try {
      await userStore.refreshPermissions()
    } catch {
      /* 忽略：接口失败时沿用当前 permissions */
    }
  }

  if (!menuStore.routesLoaded) {
    const { getMenu } = await import('../api/http')
    try {
      const res = await getMenu()
      if (res.status && res.data) {
        const flat = res.data as unknown as MenuItem[]
        const tree = buildMenuTree(flat)
        menuStore.setMenus(tree)
        const dynamicRoutes = generateRoutes(tree)
        dynamicRoutes.forEach((r) => router.addRoute('Layout', r))
        menuStore.routesLoaded = true
        return next({ ...to, replace: true })
      }
    } catch {
      return next('/login')
    }
  }

  next()
})

export default router
