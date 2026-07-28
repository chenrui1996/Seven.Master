import { computed, ref, type Ref } from 'vue'

/** 生成页列定义（与 Builder ColumnDefs 一致） */
export type ColumnDef = {
  prop: string
  kind: 'string' | 'number' | 'enum' | 'bool' | 'date' | 'key'
  sortable?: boolean
}

/** 列显示偏好（顺序 + 是否显示） */
export type ColumnPref = {
  prop: string
  visible: boolean
}

function defaultPrefs(columns: ColumnDef[]): ColumnPref[] {
  return columns.map((c) => ({ prop: c.prop, visible: true }))
}

function loadPrefs(storageKey: string, columns: ColumnDef[]): ColumnPref[] {
  try {
    const raw = localStorage.getItem(storageKey)
    if (!raw) return defaultPrefs(columns)
    const parsed = JSON.parse(raw) as ColumnPref[]
    if (!Array.isArray(parsed) || parsed.length === 0) return defaultPrefs(columns)

    const byProp = new Map(columns.map((c) => [c.prop, c]))
    const seen = new Set<string>()
    const result: ColumnPref[] = []

    for (const p of parsed) {
      if (!p?.prop || !byProp.has(p.prop) || seen.has(p.prop)) continue
      seen.add(p.prop)
      result.push({ prop: p.prop, visible: p.visible !== false })
    }
    for (const c of columns) {
      if (!seen.has(c.prop)) result.push({ prop: c.prop, visible: true })
    }
    return result
  } catch {
    return defaultPrefs(columns)
  }
}

/**
 * 表格列显隐与排序（偏好存 localStorage）
 */
export function useTableColumns(storageKey: string, allColumns: ColumnDef[]) {
  const columnPrefs: Ref<ColumnPref[]> = ref(loadPrefs(storageKey, allColumns))
  const settingsVisible = ref(false)

  const visibleColumns = computed(() => {
    const map = new Map(allColumns.map((c) => [c.prop, c]))
    return columnPrefs.value
      .filter((p) => p.visible)
      .map((p) => map.get(p.prop))
      .filter((c): c is ColumnDef => !!c)
  })

  function openColumnSettings() {
    columnPrefs.value = loadPrefs(storageKey, allColumns)
    settingsVisible.value = true
  }

  function saveColumnPrefs(prefs: ColumnPref[]) {
    columnPrefs.value = prefs
    localStorage.setItem(storageKey, JSON.stringify(prefs))
    settingsVisible.value = false
  }

  function resetColumnPrefs() {
    const prefs = defaultPrefs(allColumns)
    columnPrefs.value = prefs
    localStorage.removeItem(storageKey)
  }

  return {
    allColumns,
    columnPrefs,
    visibleColumns,
    settingsVisible,
    openColumnSettings,
    saveColumnPrefs,
    resetColumnPrefs,
  }
}
