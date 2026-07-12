import type { MenuItem } from '../stores'

/** 将扁平菜单列表构建为树形结构（OrderNo 越大越靠前，与 Legrand 一致） */
export function buildMenuTree(flat: MenuItem[], parentId = 0): MenuItem[] {
  return flat
    .filter((m) => (m.parentId ?? 0) === parentId)
    .sort((a, b) => (b.orderNo ?? 0) - (a.orderNo ?? 0))
    .map((m) => {
      const children = buildMenuTree(flat, m.menu_Id)
      return children.length ? { ...m, children } : { ...m }
    })
}

/** 扁平化菜单树（用于动态路由注册） */
export function flattenMenus(menus: MenuItem[]): MenuItem[] {
  const result: MenuItem[] = []
  for (const m of menus) {
    result.push(m)
    if (m.children?.length) result.push(...flattenMenus(m.children))
  }
  return result
}

/** 将部门/菜单 API 数据转为 el-tree 节点 */
export function toTreeNodes<T extends { parentId?: number }>(
  flat: T[],
  parentId = 0,
  getId: (item: T) => number,
  getLabel: (item: T) => string,
  getChildren?: (item: T) => T[] | undefined
): { id: number; label: string; raw: T; children?: ReturnType<typeof toTreeNodes> }[] {
  return flat
    .filter((item) => (item.parentId ?? 0) === parentId)
    .map((item) => {
      const children = toTreeNodes(flat, getId(item), getId, getLabel)
      return {
        id: getId(item),
        label: getLabel(item),
        raw: item,
        children: children.length ? children : undefined,
      }
    })
}
