import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

/** 菜单项类型 */
export interface MenuItem {
  menu_Id: number
  parentId: number
  menuName: string
  url?: string
  icon?: string
  tableName?: string
  auth?: string
  orderNo?: number
  children?: MenuItem[]
}

/** 菜单 Store */
export const useMenuStore = defineStore('menu', () => {
  const menus = ref<MenuItem[]>([])
  const routesLoaded = ref(false)

  function setMenus(items: MenuItem[]) {
    menus.value = items
  }

  return { menus, routesLoaded, setMenus }
})

export interface DictOption {
  label: string
  value: string | number
}

interface VueDictListItem {
  dicName?: string
  dicValue?: string
  enable?: number
}

interface VueDictEntry {
  key: string
  data: VueDictListItem[]
}

/** 字典 Store */
export const useDictStore = defineStore('dict', () => {
  const dictionaries = ref<Record<string, VueDictListItem[]>>({})

  function setDictionary(key: string, data: VueDictListItem[]) {
    dictionaries.value[key] = data
  }

  async function loadDictionaries(dicNos: string[]) {
    const pending = dicNos.filter((no) => no && !dictionaries.value[no])
    if (!pending.length) return

    const { getVueDictionary } = await import('../api/http')
    const res = await getVueDictionary(pending)
    if (res.status && Array.isArray(res.data)) {
      for (const entry of res.data as VueDictEntry[]) {
        if (entry?.key) dictionaries.value[entry.key] = entry.data ?? []
      }
    }
  }

  function getOptions(dicNo: string): DictOption[] {
    const list = dictionaries.value[dicNo] ?? []
    return list
      .filter((item) => item.enable !== 0)
      .map((item) => ({
        label: String(item.dicName ?? ''),
        value: item.dicValue ?? '',
      }))
  }

  return { dictionaries, setDictionary, loadDictionaries, getOptions }
})

export interface TabItem {
  title: string
  path: string
  /** 与路由页面组件 name 一致，供 keep-alive include */
  componentName?: string
}

/** 标签页 Store */
export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<TabItem[]>([
    { title: '首页', path: '/home', componentName: 'Home' },
  ])
  const activeTab = ref('/home')

  const keepAliveIncludes = computed(() => {
    const names = tabs.value
      .map((t) => t.componentName)
      .filter((n): n is string => !!n)
    return [...new Set(names)]
  })

  function addTab(title: string, path: string, componentName?: string) {
    const existing = tabs.value.find((t) => t.path === path)
    if (existing) {
      if (componentName && !existing.componentName) existing.componentName = componentName
    } else {
      tabs.value.push({ title, path, componentName })
    }
    activeTab.value = path
  }

  function removeTab(path: string) {
    const list = tabs.value.filter((t) => t.path !== path)
    tabs.value = list
    if (activeTab.value === path) {
      activeTab.value = list[list.length - 1]?.path ?? '/home'
    }
  }

  return { tabs, activeTab, keepAliveIncludes, addTab, removeTab }
})
