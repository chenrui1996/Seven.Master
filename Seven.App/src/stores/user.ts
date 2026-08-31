import { defineStore } from 'pinia'
import { login as loginApi } from '../api/pda'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: (uni.getStorageSync('token') as string) || '',
    userName: (uni.getStorageSync('userName') as string) || '',
  }),
  actions: {
    async login(userName: string, password: string) {
      const res = await loginApi(userName, password)
      if (!res.status) throw new Error(res.message || '登录失败')
      const data = res.data as Record<string, unknown> | undefined
      const token =
        (data?.token as string) ||
        (data?.accessToken as string) ||
        (data?.Token as string) ||
        ''
      if (!token) throw new Error('登录响应无 Token')
      this.token = token
      this.userName = userName
      uni.setStorageSync('token', token)
      uni.setStorageSync('userName', userName)
      if (data?.refreshToken) uni.setStorageSync('refreshToken', data.refreshToken)
    },
    logout() {
      this.token = ''
      this.userName = ''
      uni.removeStorageSync('token')
      uni.removeStorageSync('userName')
      uni.removeStorageSync('refreshToken')
      uni.reLaunch({ url: '/pages/login/index' })
    },
  },
})
