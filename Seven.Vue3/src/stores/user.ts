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
    permissions.value = info.permissions
  }

  function logout() {
    token.value = ''
    refreshToken.value = ''
    userId.value = null
    userName.value = ''
    permissions.value = []
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  }

  function hasPermission(perm: string) {
    return permissions.value.includes(perm)
  }

  return { token, refreshToken, userId, userName, userTrueName, roleId, permissions, isLoggedIn, setToken, setUserInfo, logout, hasPermission }
}, { persist: true })
