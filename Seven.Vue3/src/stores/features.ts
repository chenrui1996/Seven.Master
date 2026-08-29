import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import http from '../api/http'

/** 与后端 FeatureOptions 对齐（camelCase） */
export interface WcsPackFeatureFlags {
  stacker: boolean
  fourWay: boolean
  boxSort: boolean
}

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
  hotStore: boolean
  deviceComm: boolean
  simulator: boolean
  wms: boolean
  orchestrationBus: boolean
  wcsPacks: WcsPackFeatureFlags
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
  hotStore: false,
  deviceComm: false,
  simulator: false,
  wms: false,
  orchestrationBus: false,
  wcsPacks: {
    stacker: false,
    fourWay: false,
    boxSort: false,
  },
}

/** 菜单 TableName → 功能开关（顶层 bool）；嵌套包见 isMenuEnabled 特殊分支 */
const menuFeatureMap: Record<string, keyof FeatureFlags> = {
  Sys_WorkFlow: 'workFlow',
  Sys_WorkFlowTable: 'workFlow',
  Sys_QuartzOptions: 'quartz',
  Sys_QuartzLog: 'quartz',
  Sys_Alarm: 'alarm',
  Sys_TableInfo: 'builder',
  CommConnection: 'deviceComm',
  CommPoint: 'deviceComm',
  CommRule: 'deviceComm',
  DeviceComm: 'deviceComm',
  DeviceCommFolder: 'deviceComm',
  WmsFolder: 'wms',
  WmsLocation: 'wms',
  WmsStock: 'wms',
  WmsInboundOrder: 'wms',
  WmsOutboundOrder: 'wms',
  WmsCycleCount: 'wms',
  ScadaFolder: 'wms',
  WcsFolder: 'orchestrationBus',
  WcsOpsFolder: 'orchestrationBus',
  BusTransportOrder: 'orchestrationBus',
  CtlMode: 'orchestrationBus',
  IfcApiLog: 'orchestrationBus',
}

export const useFeatureStore = defineStore('features', () => {
  const flags = ref<FeatureFlags>({ ...defaults })
  const loaded = ref(false)

  const captchaEnabled = computed(() => flags.value.captcha)
  const signalREnabled = computed(() => flags.value.signalR)
  const alarmEnabled = computed(() => flags.value.alarm && flags.value.signalR)

  async function load() {
    try {
      const res = await http.get<{ status: boolean; data?: Partial<FeatureFlags> & { wcsPacks?: Partial<WcsPackFeatureFlags> } }>('/api/config/features')
      if (res.status && res.data) {
        flags.value = {
          ...defaults,
          ...res.data,
          wcsPacks: { ...defaults.wcsPacks, ...res.data.wcsPacks },
        }
      }
    } catch {
      flags.value = { ...defaults }
    } finally {
      loaded.value = true
    }
  }

  function isMenuEnabled(tableName?: string | null, url?: string | null) {
    const key = tableName || ''
    if (key === 'StackerTrigger' || url?.includes('/Wcs/Stacker'))
      return !!flags.value.wcsPacks.stacker || !!flags.value.orchestrationBus
    const feature = menuFeatureMap[key]
    if (feature) return !!flags.value[feature]
    if (url?.includes('WorkFlow')) return flags.value.workFlow
    if (url?.includes('Quartz')) return flags.value.quartz
    if (url?.includes('Alarm')) return flags.value.alarm
    if (url?.includes('coder') || url?.includes('TableInfo')) return flags.value.builder
    if (url?.includes('DeviceComm') || url?.includes('CommConnection') || url?.includes('CommPoint') || url?.includes('CommRule'))
      return flags.value.deviceComm
    if (url?.includes('/Wms/') || url?.includes('/Scada/')) return flags.value.wms
    if (url?.includes('/Wcs/') || url?.includes('/Platform/')) return flags.value.orchestrationBus || flags.value.wms
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
