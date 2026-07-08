import type { App, Directive } from 'vue'
import { useUserStore } from '../stores/user'

/** v-permission 按钮权限指令 */
export const permissionDirective: Directive = {
  mounted(el, binding) {
    const userStore = useUserStore()
    const perm = binding.value as string
    if (perm && !userStore.hasPermission(perm)) {
      el.style.display = 'none'
    }
  }
}

/** 注册全局指令 */
export function setupDirectives(app: App) {
  app.directive('permission', permissionDirective)
}
