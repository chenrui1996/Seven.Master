import { defineStore } from 'pinia'
import { ref } from 'vue'

/** 菜单项类型 */
export interface MenuItem {
  menu_Id: number
  parentId: number
  menuName: string
  url?: string
  icon?: string
  tableName?: string
  auth?: string
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

/** 字典 Store */
export const useDictStore = defineStore('dict', () => {
  const dictionaries = ref<Record<string, unknown[]>>({})

  function setDictionary(key: string, data: unknown[]) {
    dictionaries.value[key] = data
  }

  return { dictionaries, setDictionary }
})

/** 标签页 Store */
export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<{ title: string; path: string }[]>([{ title: '首页', path: '/home' }])
  const activeTab = ref('/home')

  function addTab(title: string, path: string) {
    if (!tabs.value.find((t) => t.path === path)) {
      tabs.value.push({ title, path })
    }
    activeTab.value = path
  }

  function removeTab(path: string) {
    tabs.value = tabs.value.filter((t) => t.path !== path)
  }

  return { tabs, activeTab, addTab, removeTab }
})
