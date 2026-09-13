import type { DetailTableConfig, SearchFieldConfig } from '../../extension/types'

/**
 * 合并生成配置与扩展 detailTables：同 key 以扩展为准；仅扩展有的 key 追加到末尾。
 * 展示模式统一为 below（主子表同页）。
 */
export function mergeDetailTables(
  generated: DetailTableConfig[],
  extension?: DetailTableConfig[],
): DetailTableConfig[] {
  if (!extension?.length) {
    return generated.map((d) => (d.mode === 'below' ? d : { ...d, mode: 'below' as const }))
  }
  const byKey = new Map<string, DetailTableConfig>()
  for (const d of generated) byKey.set(d.key, d)
  for (const d of extension) byKey.set(d.key, d)

  const result: DetailTableConfig[] = []
  const seen = new Set<string>()
  for (const d of generated) {
    const merged = byKey.get(d.key)
    if (merged) {
      result.push(merged.mode === 'below' ? merged : { ...merged, mode: 'below' })
      seen.add(d.key)
    }
  }
  for (const d of extension) {
    if (!seen.has(d.key)) {
      result.push(d.mode === 'below' ? d : { ...d, mode: 'below' })
      seen.add(d.key)
    }
  }
  return result
}

/**
 * 合并生成配置与扩展 searchFields：同 prop 以扩展为准；仅扩展有的 prop 追加到末尾。
 */
export function mergeSearchFields(
  generated: SearchFieldConfig[],
  extension?: SearchFieldConfig[],
): SearchFieldConfig[] {
  if (!extension?.length) return generated.slice()
  const byProp = new Map<string, SearchFieldConfig>()
  for (const f of generated) byProp.set(f.prop, f)
  for (const f of extension) byProp.set(f.prop, f)

  const result: SearchFieldConfig[] = []
  const seen = new Set<string>()
  for (const f of generated) {
    const merged = byProp.get(f.prop)
    if (merged) {
      result.push(merged)
      seen.add(f.prop)
    }
  }
  for (const f of extension) {
    if (!seen.has(f.prop)) {
      result.push(f)
      seen.add(f.prop)
    }
  }
  return result
}
