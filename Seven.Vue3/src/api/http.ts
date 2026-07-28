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

function triggerBlobDownload(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

async function blobOrThrow(res: { data: Blob; headers: Record<string, unknown> }, fallbackName: string) {
  const blob = res.data
  const contentType = String(res.headers['content-type'] ?? '')
  if (contentType.includes('application/json')) {
    const text = await blob.text()
    let message = '操作失败'
    try {
      const json = JSON.parse(text) as ApiResponse
      message = json.message || message
    } catch { /* ignore */ }
    throw new Error(message)
  }
  const disposition = String(res.headers['content-disposition'] ?? '')
  const match = /filename\*?=(?:UTF-8'')?["']?([^"';]+)/i.exec(disposition)
  const filename = match ? decodeURIComponent(match[1]) : fallbackName
  triggerBlobDownload(blob, filename)
}

/** POST 下载文件（绕过 JSON 响应拦截器） */
export async function downloadFile(url: string, data?: unknown, filename = 'export.xlsx') {
  const userStore = useUserStore()
  const res = await axios.post(`${import.meta.env.VITE_API_BASE_URL}${url}`, data ?? {}, {
    responseType: 'blob',
    headers: userStore.token ? { Authorization: `Bearer ${userStore.token}` } : {},
  })
  await blobOrThrow(res, filename)
}

/** GET 下载文件 */
export async function downloadGet(url: string, filename = 'template.xlsx') {
  const userStore = useUserStore()
  const res = await axios.get(`${import.meta.env.VITE_API_BASE_URL}${url}`, {
    responseType: 'blob',
    headers: userStore.token ? { Authorization: `Bearer ${userStore.token}` } : {},
  })
  await blobOrThrow(res, filename)
}

/** 上传文件（multipart） */
export const uploadFile = (url: string, file: File, fieldName = 'file') => {
  const form = new FormData()
  form.append(fieldName, file)
  return http.post(url, form)
}
