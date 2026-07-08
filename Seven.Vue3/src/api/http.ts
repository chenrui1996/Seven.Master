import axios from 'axios'
import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios'
import { useUserStore } from '../stores/user'
import router from '../router'

/** 统一 API 响应结构 */
export interface ApiResponse<T = unknown> {
  status: boolean
  message?: string
  data?: T
  code?: string
}

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 30000
}) as AxiosInstance & {
  get<T = ApiResponse>(url: string): Promise<T>
  post<T = ApiResponse>(url: string, data?: unknown): Promise<T>
}

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

let isRefreshing = false
let pendingRequests: Array<(token: string) => void> = []

http.interceptors.response.use(
  (response) => response.data as ApiResponse,
  async (error) => {
    const originalRequest = error.config
    if (error.response?.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()
      if (!userStore.refreshToken) {
        userStore.logout()
        router.push('/login')
        return Promise.reject(error)
      }
      if (isRefreshing) {
        return new Promise((resolve) => {
          pendingRequests.push((token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            resolve(http(originalRequest))
          })
        })
      }
      originalRequest._retry = true
      isRefreshing = true
      try {
        const res = await axios.post<ApiResponse>(`${import.meta.env.VITE_API_BASE_URL}/api/Auth/refresh`, {
          refreshToken: userStore.refreshToken
        })
        if (res.data.status && res.data.data) {
          const data = res.data.data as { token: string; refreshToken: string }
          userStore.setToken(data.token, data.refreshToken)
          pendingRequests.forEach((cb) => cb(data.token))
          pendingRequests = []
          originalRequest.headers.Authorization = `Bearer ${data.token}`
          return http(originalRequest)
        }
      } catch {
        userStore.logout()
        router.push('/login')
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  }
)

export default http

export const login = (userName: string, password: string) =>
  http.post('/api/Auth/login', { userName, password })

export const getMenu = () => http.get('/api/Sys_Menu/getMenu')

export const getPageData = (url: string, options: object) =>
  http.post(url, options)

export const getVueDictionary = (dicNos: string[]) =>
  http.post('/api/Sys_Dictionary/getVueDictionary', dicNos)
