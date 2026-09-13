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

type MenuLike = {
  tableName?: string | null
  TableName?: string | null
  url?: string | null
  Url?: string | null
  children?: MenuLike[]
}

function pickTableName(m: MenuLike): string {
  return String(m.tableName ?? m.TableName ?? '').trim()
}

function pickUrl(m: MenuLike): string {
  return String(m.url ?? m.Url ?? '').trim()
}

/** 菜单 TableName → 顶层开关；嵌套包见 isMenuEnabled */
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
  WmsWarehouse: 'wms',
  WmsZone: 'wms',
  WmsLayer: 'wms',
  WmsAisle: 'wms',
  WmsContainer: 'wms',
  WmsContainerType: 'wms',
  WmsHandoverLink: 'wms',
  WmsStockLedger: 'wms',
  WmsInboundOrder: 'wms',
  WmsOutboundOrder: 'wms',
  WmsCycleCount: 'wms',
  WmsPickingTask: 'wms',
  WmsMasterFolder: 'wms',
  WmsStockFolder: 'wms',
  WmsOrderFolder: 'wms',
  WmsInboundOrderOps: 'wms',
  WmsOutboundOrderOps: 'wms',
  ScadaFolder: 'wms',
  ScadaFloor2d: 'wms',
  BusTransportOrder: 'orchestrationBus',
  CtlMode: 'orchestrationBus',
  IfcApiLog: 'orchestrationBus',
  StkOpsFolder: 'orchestrationBus',
  FwOpsFolder: 'orchestrationBus',
}

export const useFeatureStore = defineStore('features', () => {
  const flags = ref<FeatureFlags>({ ...defaults, wcsPacks: { ...defaults.wcsPacks } })
  const loaded = ref(false)

  const captchaEnabled = computed(() => flags.value.captcha)
  const signalREnabled = computed(() => flags.value.signalR)
  const alarmEnabled = computed(() => flags.value.alarm && flags.value.signalR)

  async function load() {
    try {
      const res = await http.get<{
        status: boolean
        data?: Partial<FeatureFlags> & { wcsPacks?: Partial<WcsPackFeatureFlags> }
      }>('/api/config/features')
      if (res.status && res.data) {
        const packs = res.data.wcsPacks ?? (res.data as { WcsPacks?: Partial<WcsPackFeatureFlags> }).WcsPacks
        flags.value = {
          ...defaults,
          ...res.data,
          wms: !!(res.data.wms ?? (res.data as { Wms?: boolean }).Wms),
          orchestrationBus: !!(
            res.data.orchestrationBus ?? (res.data as { OrchestrationBus?: boolean }).OrchestrationBus
          ),
          simulator: !!(res.data.simulator ?? (res.data as { Simulator?: boolean }).Simulator),
          deviceComm: !!(res.data.deviceComm ?? (res.data as { DeviceComm?: boolean }).DeviceComm),
          wcsPacks: {
            ...defaults.wcsPacks,
            stacker: !!(packs?.stacker ?? (packs as { Stacker?: boolean } | undefined)?.Stacker),
            fourWay: !!(packs?.fourWay ?? (packs as { FourWay?: boolean } | undefined)?.FourWay),
            boxSort: !!(packs?.boxSort ?? (packs as { BoxSort?: boolean } | undefined)?.BoxSort),
          },
        }
      } else {
        flags.value = { ...defaults, wcsPacks: { ...defaults.wcsPacks } }
      }
    } catch {
      flags.value = { ...defaults, wcsPacks: { ...defaults.wcsPacks } }
    } finally {
      loaded.value = true
    }
  }

  function anyWcsPack() {
    const p = flags.value.wcsPacks
    return !!(p.stacker || p.fourWay || p.boxSort)
  }

  function isMenuEnabled(tableName?: string | null, url?: string | null) {
    const key = (tableName || '').trim()
    const path = (url || '').trim()

    if (key === 'StackerTrigger' || key.startsWith('Stk') || path.includes('/Wcs/Stacker'))
      return !!flags.value.wcsPacks.stacker
    if (
      key === 'FourWayTrigger' ||
      key === 'FourWayFolder' ||
      key.startsWith('Fw') ||
      path.includes('/Wcs/FourWay')
    )
      return !!flags.value.wcsPacks.fourWay
    if (key === 'WcsFolder')
      return !!flags.value.orchestrationBus || !!flags.value.wcsPacks.stacker
    // 已取消「执行运维」：恒隐藏
    if (key === 'WcsOpsFolder' || key === 'CtlMode' || key === 'ScadaFolder' || key === 'ScadaFloor2d')
      return false
    if (key === 'IfcApiLog' || path.includes('/Platform/InterfaceLog'))
      return !!flags.value.orchestrationBus || !!flags.value.wms

    const feature = menuFeatureMap[key]
    if (feature) {
      const v = flags.value[feature]
      return typeof v === 'boolean' ? v : true
    }

    if (path.includes('WorkFlow')) return flags.value.workFlow
    if (path.includes('Quartz')) return flags.value.quartz
    if (path.includes('Alarm')) return flags.value.alarm
    if (path.includes('coder') || path.includes('TableInfo')) return flags.value.builder
    if (
      path.includes('DeviceComm') ||
      path.includes('CommConnection') ||
      path.includes('CommPoint') ||
      path.includes('CommRule')
    )
      return flags.value.deviceComm
    if (path.includes('/Wms/') || path.includes('/Scada/')) return flags.value.wms
    if (path.includes('/Wcs/Bus') || path.includes('/Platform/')) return flags.value.orchestrationBus
    if (path.includes('/Wcs/')) return anyWcsPack() || flags.value.orchestrationBus
    return true
  }

  /**
   * 递归过滤：叶子按开关；无 url 的目录仅在仍有子节点时保留。
   * 不再用「自身关闭但有子节点则保留」——避免未映射子项把已关闭模块撑出来。
   */
  function filterMenus<T extends MenuLike>(menus: T[]): T[] {
    return menus
      .map((m) => {
        const children = m.children ? filterMenus(m.children) : undefined
        return { ...m, children } as T
      })
      .filter((m) => {
        const url = pickUrl(m)
        const table = pickTableName(m)
        const hasKids = (m.children?.length ?? 0) > 0
        if (!url) return hasKids
        return isMenuEnabled(table, url)
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
