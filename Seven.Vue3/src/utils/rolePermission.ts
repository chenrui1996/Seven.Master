/** 权限树节点（Legrand Permission.vue 兼容结构） */
export interface PermissionAction {
  text: string
  value: string
  checked?: boolean
}

export interface PermissionNode {
  id: number
  pid: number
  text: string
  isApp?: boolean
  actions: PermissionAction[]
  children?: PermissionNode[]
  leftCk?: boolean
  lv?: number
}

function normalizeAssignedNodes(raw: unknown): PermissionNode[] {
  if (!Array.isArray(raw)) return []
  return raw.map((item) => {
    const row = item as Record<string, unknown>
    const id = Number(row.id ?? row.Id ?? 0)
    const actionsRaw = (row.actions ?? row.Actions ?? []) as Record<string, unknown>[]
    return {
      id,
      pid: Number(row.pid ?? row.Pid ?? 0),
      text: String(row.text ?? row.Text ?? ''),
      isApp: Boolean(row.isApp ?? row.IsApp),
      actions: actionsRaw.map((a) => ({
        text: String(a.text ?? a.Text ?? ''),
        value: String(a.value ?? a.Value ?? ''),
        checked: true,
      })),
    }
  })
}

/** 扁平列表按 pid 组装树（复用 flat 中同一对象引用） */
export function buildPermissionTree(flat: PermissionNode[]): PermissionNode[] {
  const map = new Map<number, PermissionNode>()
  for (const item of flat) {
    item.children = []
    map.set(item.id, item)
  }

  const roots: PermissionNode[] = []
  for (const item of flat) {
    if (item.pid === 0) {
      item.lv = 1
      roots.push(item)
      continue
    }
    const parent = map.get(item.pid)
    if (parent) {
      item.lv = (parent.lv ?? 1) + 1
      parent.children!.push(item)
    } else {
      item.lv = 1
      roots.push(item)
    }
  }
  return roots
}

/** 将角色已有权限勾选到节点（需在 buildPermissionTree 之前调用） */
export function applyRolePermissions(flat: PermissionNode[], assignedRaw: unknown) {
  const assigned = normalizeAssignedNodes(assignedRaw)
  for (const node of flat) {
    node.leftCk = false
    for (const action of node.actions) action.checked = false
  }

  for (const item of assigned) {
    const source = flat.find((f) => f.id === item.id)
    if (!source) continue
    for (const action of item.actions) {
      const target = source.actions.find(
        (a) => a.value.localeCompare(action.value, undefined, { sensitivity: 'accent' }) === 0,
      )
      if (target) target.checked = true
    }
    source.leftCk = source.actions.length > 0 && source.actions.every((a) => a.checked)
  }
}

/** 菜单全选/取消（含子级） */
export function setNodeActionsChecked(node: PermissionNode, checked: boolean) {
  node.leftCk = checked
  for (const action of node.actions) action.checked = checked
  node.children?.forEach((child) => setNodeActionsChecked(child, checked))
}

/** 向上同步父级菜单全选状态 */
export function syncParentLeftCheck(nodes: PermissionNode[], node: PermissionNode) {
  syncNodeLeftCheck(node)
  const parent = findParentNode(nodes, node.id)
  if (!parent) return
  const actionableChildren = flattenPermissionTree(parent.children ?? []).filter((n) => n.actions.length)
  if (actionableChildren.length) {
    parent.leftCk = actionableChildren.every((c) => c.leftCk)
  }
  syncParentLeftCheck(nodes, parent)
}

/** 单个按钮变更后同步当前节点 */
export function syncNodeLeftCheck(node: PermissionNode) {
  node.leftCk = node.actions.length > 0 && node.actions.every((a) => a.checked)
}

export function findParentNode(nodes: PermissionNode[], childId: number): PermissionNode | undefined {
  for (const node of nodes) {
    if (node.children?.some((c) => c.id === childId)) return node
    const found = node.children ? findParentNode(node.children, childId) : undefined
    if (found) return found
  }
  return undefined
}

/** 收集待保存权限 */
export function collectPermissions(flat: PermissionNode[]) {
  return flat
    .filter((x) => x.actions.some((a) => a.checked))
    .map((x) => ({
      id: x.id,
      actions: x.actions
        .filter((a) => a.checked)
        .map((a) => ({ text: a.text, value: a.value })),
    }))
}

/** 扁平化树 */
export function flattenPermissionTree(nodes: PermissionNode[]): PermissionNode[] {
  const result: PermissionNode[] = []
  const walk = (items: PermissionNode[]) => {
    for (const item of items) {
      result.push(item)
      if (item.children?.length) walk(item.children)
    }
  }
  walk(nodes)
  return result
}
