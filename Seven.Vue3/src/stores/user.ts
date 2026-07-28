import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

/** 用户状态 Store：Token、用户信息、权限 */
export const useUserStore = defineStore('user', () => {
  const token = ref(localStorage.getItem('token') || '')
  const refreshToken = ref(localStorage.getItem('refreshToken') || '')
  const userId = ref<number | null>(null)
  const userName = ref('')
  const userTrueName = ref('')
  const roleId = ref<number | null>(null)
  const permissions = ref<string[]>([])
  /** 本次会话是否已从服务端同步过权限 */
  const permissionsSynced = ref(false)

  const isLoggedIn = computed(() => !!token.value)

  function setToken(access: string, refresh: string) {
    token.value = access
    refreshToken.value = refresh
    localStorage.setItem('token', access)
    localStorage.setItem('refreshToken', refresh)
  }

  function setUserInfo(info: {
    userId: number
    userName: string
    userTrueName: string
    roleId: number
    permissions: string[]
  }) {
    userId.value = info.userId
    userName.value = info.userName
    userTrueName.value = info.userTrueName
    roleId.value = info.roleId
    permissions.value = info.permissions ?? []
    permissionsSynced.value = true
  }

  function setPermissions(list: string[]) {
    permissions.value = list ?? []
    permissionsSynced.value = true
  }

  function logout() {
    token.value = ''
    refreshToken.value = ''
    userId.value = null
    userName.value = ''
    userTrueName.value = ''
    roleId.value = null
    permissions.value = []
    permissionsSynced.value = false
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  }

  /** 忽略大小写匹配权限码，如 Device.Add */
  function hasPermission(perm: string) {
    if (!perm) return true
    const target = perm.trim().toLowerCase()
    return permissions.value.some((p) => p.trim().toLowerCase() === target)
  }

  /** 从后端拉取最新权限（角色授权变更后调用） */
  async function refreshPermissions() {
    if (!token.value) {
      permissions.value = []
      permissionsSynced.value = false
      return [] as string[]
    }
    const { default: http } = await import('../api/http')
    const res = await http.get<{ status: boolean; data?: string[] }>('/api/Auth/permissions')
    if (res.status && Array.isArray(res.data)) {
      permissions.value = res.data
      permissionsSynced.value = true
      return res.data
    }
    permissionsSynced.value = true
    return permissions.value
  }

  return {
    token,
    refreshToken,
    userId,
    userName,
    userTrueName,
    roleId,
    permissions,
    permissionsSynced,
    isLoggedIn,
    setToken,
    setUserInfo,
    setPermissions,
    logout,
    hasPermission,
    refreshPermissions,
  }
}, {
  // 仅持久化登录态；权限每次进入系统从接口刷新，避免改权后仍显示旧按钮
  persist: {
    pick: ['token', 'refreshToken', 'userId', 'userName', 'userTrueName', 'roleId'],
  },
})
