import type { App, Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '../stores/user'

function applyPermission(el: HTMLElement, binding: DirectiveBinding) {
  const userStore = useUserStore()
  const perm = binding.value as string
  if (perm && !userStore.hasPermission(perm)) {
    el.style.display = 'none'
  } else {
    el.style.display = ''
  }
}

/** v-permission 按钮权限指令 */
export const permissionDirective: Directive = {
  mounted(el, binding) {
    applyPermission(el as HTMLElement, binding)
  },
  updated(el, binding) {
    applyPermission(el as HTMLElement, binding)
  },
}

/** 注册全局指令 */
export function setupDirectives(app: App) {
  app.directive('permission', permissionDirective)
}
