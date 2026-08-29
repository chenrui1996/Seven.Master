export interface ApiResponse<T = unknown> {
  status: boolean
  message?: string
  data?: T
}

const BASE = import.meta.env.VITE_API_BASE_URL || ''

function getToken(): string {
  return uni.getStorageSync('token') || ''
}

export function request<T = unknown>(options: {
  url: string
  method?: 'GET' | 'POST'
  data?: unknown
}): Promise<ApiResponse<T>> {
  return new Promise((resolve, reject) => {
    uni.request({
      url: `${BASE}${options.url}`,
      method: options.method || 'GET',
      data: options.data as UniApp.RequestOptions['data'],
      header: {
        'Content-Type': 'application/json',
        Authorization: getToken() ? `Bearer ${getToken()}` : '',
      },
      success: (res) => {
        const body = res.data as ApiResponse<T>
        if (res.statusCode === 401) {
          uni.removeStorageSync('token')
          uni.reLaunch({ url: '/pages/login/index' })
          reject(new Error('未登录'))
          return
        }
        if (body && body.status === false) {
          uni.showToast({ title: body.message || '请求失败', icon: 'none' })
        }
        resolve(body)
      },
      fail: (err) => {
        uni.showToast({ title: '网络异常', icon: 'none' })
        reject(err)
      },
    })
  })
}
