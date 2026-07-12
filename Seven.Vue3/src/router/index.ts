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

/** 动态路由组件映射 */
const viewModules = import.meta.glob('../views/**/*.vue')

/** 将菜单转为路由并注册（递归扁平化后注册叶子路由） */
export function generateRoutes(menus: MenuItem[]): RouteRecordRaw[] {
  const routes: RouteRecordRaw[] = []
  for (const menu of flattenMenus(menus)) {
    if (!menu.url || menu.url.startsWith('http') || menu.url === '#') continue
    const path = menu.url.startsWith('/') ? menu.url.slice(1) : menu.url
    const componentPath = `../views/system/${menu.tableName || path.split('/').pop()}.vue`
    const component = viewModules[componentPath] ?? viewModules[`../views/system/${path}.vue`]
    if (component != null) {
      routes.push({
        path,
        name: menu.tableName || menu.menuName,
        component,
        meta: { title: menu.menuName, permission: menu.tableName }
      })
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
