import type { App, Directive, DirectiveBinding } from 'vue'
import { watch } from 'vue'
import { useUserStore } from '../stores/user'

function resolveAllowed(perm: unknown): boolean {
  if (perm == null || perm === '') return true
  const userStore = useUserStore()
  if (Array.isArray(perm))
    return perm.some((p) => typeof p === 'string' && userStore.hasPermission(p))
  if (typeof perm === 'string') return userStore.hasPermission(perm)
  return true
}

function applyPermission(el: HTMLElement, binding: DirectiveBinding) {
  const allowed = resolveAllowed(binding.value)
  // Element Plus 按钮会写 display，必须用 important，否则取消授权后仍可见
  if (allowed) {
    el.style.removeProperty('display')
    el.removeAttribute('aria-hidden')
  } else {
    el.style.setProperty('display', 'none', 'important')
    el.setAttribute('aria-hidden', 'true')
  }
}

/** v-permission 按钮权限指令（支持字符串或字符串数组） */
export const permissionDirective: Directive = {
  mounted(el, binding) {
    applyPermission(el as HTMLElement, binding)
    const userStore = useUserStore()
    const stop = watch(
      () => userStore.permissions.slice(),
      () => applyPermission(el as HTMLElement, binding),
      { deep: true },
    )
    ;(el as HTMLElement & { __permStop?: () => void }).__permStop = stop
  },
  updated(el, binding) {
    applyPermission(el as HTMLElement, binding)
  },
  unmounted(el) {
    ;(el as HTMLElement & { __permStop?: () => void }).__permStop?.()
  },
}

/** 注册全局指令 */
export function setupDirectives(app: App) {
  app.directive('permission', permissionDirective)
}
