import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { ElNotification } from 'element-plus'
import { onUnmounted, ref, watch } from 'vue'
import { useUserStore } from '../stores/user'
import { useFeatureStore } from '../stores/features'

export interface SystemNotifyPayload {
  title: string
  content: string
  at?: string
}

/** 首页消息 Hub：在线人数与系统通知 */
export function useMessageHub() {
  const userStore = useUserStore()
  const featureStore = useFeatureStore()
  const onlineCount = ref(0)
  const hubConnected = ref(false)
  let connection: HubConnection | null = null

  async function start() {
    if (!featureStore.signalREnabled) return
    if (!userStore.token || connection) return

    const baseUrl = import.meta.env.VITE_API_BASE_URL as string
    connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hub/message`, {
        accessTokenFactory: () => userStore.token,
      })
      .withAutomaticReconnect()
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
      .build()

    connection.on('OnlineCount', (count: number) => {
      onlineCount.value = typeof count === 'number' ? count : 0
    })

    connection.on('SystemNotify', (payload: SystemNotifyPayload) => {
      ElNotification({
        title: payload.title || '系统通知',
        message: payload.content,
        type: 'info',
        duration: 8000,
      })
    })

    connection.on('ReceiveMessage', (message: string) => {
      ElNotification({
        title: '消息',
        message,
        type: 'info',
        duration: 6000,
      })
    })

    connection.onreconnected(() => {
      hubConnected.value = true
    })
    connection.onclose(() => {
      hubConnected.value = false
    })

    try {
      await connection.start()
      hubConnected.value = true
    } catch {
      hubConnected.value = false
    }
  }

  async function stop() {
    if (connection) {
      await connection.stop()
      connection = null
      hubConnected.value = false
      onlineCount.value = 0
    }
  }

  watch(
    () => userStore.token,
    (token) => {
      if (token) start()
      else stop()
    },
    { immediate: true }
  )

  onUnmounted(() => {
    stop()
  })

  return { onlineCount, hubConnected, start, stop }
}
