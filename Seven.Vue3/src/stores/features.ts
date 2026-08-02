import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import http from '../api/http'

/** 与后端 FeatureOptions 对齐（camelCase） */
export interface FeatureFlags {
  workFlow: boolean
  quartz: boolean
  signalR: boolean
  alarm: boolean
  messageQueue: boolean
  outbox: boolean
  mail: boolean
  minIO: boolean
  tenant: boolean
  captcha: boolean
  rateLimit: boolean
  idempotency: boolean
  dataScope: boolean
  auditInterceptor: boolean
  builder: boolean
}

const defaults: FeatureFlags = {
  workFlow: false,
  quartz: false,
  signalR: true,
  alarm: true,
  messageQueue: false,
  outbox: false,
  mail: false,
  minIO: false,
  tenant: false,
  captcha: false,
  rateLimit: true,
  idempotency: true,
  dataScope: true,
  auditInterceptor: true,
  builder: true,
}

/** 菜单 TableName → 功能开关 */
const menuFeatureMap: Record<string, keyof FeatureFlags> = {
  Sys_WorkFlow: 'workFlow',
  Sys_WorkFlowTable: 'workFlow',
  Sys_QuartzOptions: 'quartz',
  Sys_QuartzLog: 'quartz',
  Sys_Alarm: 'alarm',
  Sys_TableInfo: 'builder',
}

export const useFeatureStore = defineStore('features', () => {
  const flags = ref<FeatureFlags>({ ...defaults })
  const loaded = ref(false)

  const captchaEnabled = computed(() => flags.value.captcha)
  const signalREnabled = computed(() => flags.value.signalR)
  const alarmEnabled = computed(() => flags.value.alarm && flags.value.signalR)

  async function load() {
    try {
      const res = await http.get<{ status: boolean; data?: Partial<FeatureFlags> }>('/api/config/features')
      if (res.status && res.data) {
        flags.value = { ...defaults, ...res.data }
      }
    } catch {
      flags.value = { ...defaults }
    } finally {
      loaded.value = true
    }
  }

  function isMenuEnabled(tableName?: string | null, url?: string | null) {
    const key = tableName || ''
    const feature = menuFeatureMap[key]
    if (feature) return !!flags.value[feature]
    // 兼容无 TableName 的 URL
    if (url?.includes('WorkFlow')) return flags.value.workFlow
    if (url?.includes('Quartz')) return flags.value.quartz
    if (url?.includes('Alarm')) return flags.value.alarm
    if (url?.includes('coder') || url?.includes('TableInfo')) return flags.value.builder
    return true
  }

  function filterMenus<T extends { tableName?: string; url?: string; children?: T[] }>(menus: T[]): T[] {
    return menus
      .map((m) => {
        const children = m.children ? filterMenus(m.children) : undefined
        return { ...m, children }
      })
      .filter((m) => {
        const selfOk = isMenuEnabled(m.tableName, m.url)
        const hasKids = (m.children?.length ?? 0) > 0
        // 目录节点：有可用子菜单则保留
        if (!m.url && !m.tableName) return hasKids
        return selfOk || hasKids
      })
  }

  return {
    flags,
    loaded,
    captchaEnabled,
    signalREnabled,
    alarmEnabled,
    load,
    isMenuEnabled,
    filterMenus,
  }
})
