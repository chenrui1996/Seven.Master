import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { ElNotification } from 'element-plus'
import { onUnmounted, watch } from 'vue'
import { useUserStore } from '../stores/user'
import { useAlarmStore, type AlarmItem } from '../stores/alarm'
import { useFeatureStore } from '../stores/features'

/** 建立告警 SignalR 连接（登录后调用） */
export function useAlarmHub() {
  const userStore = useUserStore()
  const alarmStore = useAlarmStore()
  const featureStore = useFeatureStore()
  let connection: HubConnection | null = null

  async function start() {
    if (!featureStore.alarmEnabled) return
    if (!userStore.token || connection) return

    const baseUrl = import.meta.env.VITE_API_BASE_URL as string
    connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hub/alarm`, {
        accessTokenFactory: () => userStore.token,
      })
      .withAutomaticReconnect()
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
      .build()

    connection.on('ReceiveAlarm', (alarm: AlarmItem) => {
      alarmStore.handleReceiveAlarm(alarm)
      ElNotification({
        title: alarm.code,
        message: alarm.message,
        type: alarm.level >= 3 ? 'error' : 'warning',
        duration: 6000,
      })
    })

    connection.on('AlarmUpdated', (alarm: AlarmItem) => {
      alarmStore.handleAlarmUpdated(alarm)
    })

    connection.onreconnected(() => alarmStore.setHubConnected(true))
    connection.onclose(() => alarmStore.setHubConnected(false))

    try {
      await connection.start()
      alarmStore.setHubConnected(true)
      await alarmStore.fetchActiveCount()
    } catch {
      alarmStore.setHubConnected(false)
    }
  }

  async function stop() {
    if (connection) {
      await connection.stop()
      connection = null
      alarmStore.setHubConnected(false)
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

  return { start, stop }
}
