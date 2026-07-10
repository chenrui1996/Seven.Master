import { defineStore } from 'pinia'
import { ref } from 'vue'
import http from '../api/http'

/** 告警推送 DTO（与后端 AlarmPushDto 对应） */
export interface AlarmItem {
  alarmId: number
  code: string
  message: string
  level: number
  category?: string
  source?: string
  deviceName?: string
  status: number
  ackUserName?: string
  ackDate?: string
  createDate?: string
}

/** 告警 Store：未读数、最新告警、SignalR 事件处理 */
export const useAlarmStore = defineStore('alarm', () => {
  const activeCount = ref(0)
  const recentAlarms = ref<AlarmItem[]>([])
  const hubConnected = ref(false)

  async function fetchActiveCount() {
    const res = await http.get<{ status: boolean; data?: { count: number } }>('/api/Sys_Alarm/getActiveCount')
    if (res.status && res.data) {
      activeCount.value = res.data.count
    }
  }

  function handleReceiveAlarm(alarm: AlarmItem) {
    if (alarm.status === 0) {
      activeCount.value += 1
    }
    recentAlarms.value = [alarm, ...recentAlarms.value.filter((a) => a.alarmId !== alarm.alarmId)].slice(0, 20)
  }

  function handleAlarmUpdated(alarm: AlarmItem) {
    const idx = recentAlarms.value.findIndex((a) => a.alarmId === alarm.alarmId)
    if (idx >= 0) recentAlarms.value[idx] = alarm
    else if (alarm.status === 0) recentAlarms.value.unshift(alarm)

    if (alarm.status !== 0) {
      fetchActiveCount()
    }
  }

  function setHubConnected(connected: boolean) {
    hubConnected.value = connected
  }

  return {
    activeCount,
    recentAlarms,
    hubConnected,
    fetchActiveCount,
    handleReceiveAlarm,
    handleAlarmUpdated,
    setHubConnected,
  }
})
