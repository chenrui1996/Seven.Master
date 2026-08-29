<template>
  <div class="seven-page devicecomm-runtime">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.deviceComm" @click="refresh">{{ t('deviceComm.refresh') }}</el-button>
      <el-button :disabled="!featureStore.flags.deviceComm" @click="reload">{{ t('deviceComm.reload') }}</el-button>
      <el-tag :type="hubOk ? 'success' : 'info'">
        Hub {{ hubOk ? t('deviceComm.hubConnected') : t('deviceComm.hubDisconnected') }}
      </el-tag>
      <el-tag type="warning" v-if="!featureStore.flags.deviceComm">Features.DeviceComm=false</el-tag>
    </div>

    <el-table :data="statuses" stripe border style="width: 100%; margin-top: 12px">
      <el-table-column prop="commConnectionId" label="Id" width="80" />
      <el-table-column prop="name" :label="t('deviceComm.name')" min-width="120" />
      <el-table-column prop="protocol" :label="t('deviceComm.protocol')" width="100">
        <template #default="{ row }">{{ row.protocol === 2 ? 'ModbusTcp' : 'Step7' }}</template>
      </el-table-column>
      <el-table-column prop="state" :label="t('deviceComm.status')" width="120">
        <template #default="{ row }">
          <el-tag :type="stateType(row.state)">{{ stateLabel(row.state) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="reconnectCount" :label="t('deviceComm.reconnectCount')" width="100" />
      <el-table-column prop="ioFailCount" :label="t('deviceComm.ioFail')" width="90" />
      <el-table-column prop="lastError" :label="t('deviceComm.lastError')" min-width="180" show-overflow-tooltip />
      <el-table-column :label="t('deviceComm.actions')" width="260" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="act('connect', row.commConnectionId)">{{ t('deviceComm.connect') }}</el-button>
          <el-button link @click="act('disconnect', row.commConnectionId)">{{ t('deviceComm.disconnect') }}</el-button>
          <el-button link type="warning" @click="act('reconnect', row.commConnectionId)">{{ t('deviceComm.reconnect') }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <h3 style="margin-top: 24px">{{ t('deviceComm.recentEvents') }}</h3>
    <el-table :data="events" stripe border style="width: 100%">
      <el-table-column prop="timestamp" :label="t('deviceComm.time')" width="200" />
      <el-table-column prop="ruleName" :label="t('deviceComm.rule')" min-width="120" />
      <el-table-column prop="eventName" :label="t('deviceComm.event')" min-width="120" />
      <el-table-column prop="success" :label="t('deviceComm.success')" width="80">
        <template #default="{ row }">{{ row.success ? t('deviceComm.yes') : t('deviceComm.no') }}</template>
      </el-table-column>
      <el-table-column prop="message" :label="t('deviceComm.message')" min-width="160" show-overflow-tooltip />
      <el-table-column :label="t('deviceComm.values')" min-width="220">
        <template #default="{ row }">{{ JSON.stringify(row.values || {}) }}</template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import http from '../../api/http'
import { useUserStore } from '../../stores/user'
import { useFeatureStore } from '../../stores/features'

const { t } = useI18n()

interface StatusRow {
  commConnectionId: number
  name: string
  protocol: number
  state: number
  lastError?: string
  reconnectCount: number
  ioFailCount: number
}

interface RuleEvent {
  commRuleId: number
  ruleName: string
  eventName: string
  success: boolean
  message?: string
  values?: Record<string, unknown>
  timestamp?: string
}

const featureStore = useFeatureStore()
const userStore = useUserStore()
const statuses = ref<StatusRow[]>([])
const events = ref<RuleEvent[]>([])
const hubOk = ref(false)
let connection: HubConnection | null = null

function stateLabel(s: number) {
  return [
    t('deviceComm.stateDisconnected'),
    t('deviceComm.stateConnecting'),
    t('deviceComm.stateConnected'),
    t('deviceComm.stateFault'),
    t('deviceComm.stateReconnecting'),
  ][s] ?? String(s)
}
function stateType(s: number) {
  if (s === 2) return 'success'
  if (s === 3 || s === 4) return 'danger'
  if (s === 1) return 'warning'
  return 'info'
}

async function refresh() {
  if (!featureStore.flags.deviceComm) return
  const res = await http.get<{ status: boolean; data?: StatusRow[] }>('/api/DeviceComm/status')
  if (res.status) statuses.value = res.data ?? []
}

async function reload() {
  const res = await http.post<{ status: boolean; message?: string }>('/api/DeviceComm/reload')
  if (res.status) {
    ElMessage.success(res.message || t('deviceComm.reloaded'))
    await refresh()
  }
}

async function act(kind: 'connect' | 'disconnect' | 'reconnect', id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/DeviceComm/${kind}/${id}`)
  if (res.status) {
    ElMessage.success(res.message || t('deviceComm.done'))
    await refresh()
  }
}

async function startHub() {
  if (!featureStore.flags.deviceComm || !featureStore.flags.signalR || !userStore.token) return
  if (connection) return
  const baseUrl = import.meta.env.VITE_API_BASE_URL as string
  connection = new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hub/devicecomm`, { accessTokenFactory: () => userStore.token })
    .withAutomaticReconnect()
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build()

  connection.on('ConnectionStatus', (s: StatusRow) => {
    const i = statuses.value.findIndex((x) => x.commConnectionId === s.commConnectionId)
    if (i >= 0) statuses.value[i] = { ...statuses.value[i], ...s }
    else statuses.value = [...statuses.value, s]
  })
  connection.on('RuleEvent', (e: RuleEvent) => {
    events.value = [e, ...events.value].slice(0, 50)
  })
  connection.onreconnected(() => (hubOk.value = true))
  connection.onclose(() => (hubOk.value = false))
  try {
    await connection.start()
    hubOk.value = true
  } catch {
    hubOk.value = false
  }
}

async function stopHub() {
  if (connection) {
    await connection.stop()
    connection = null
    hubOk.value = false
  }
}

onMounted(async () => {
  await refresh()
  await startHub()
})
onUnmounted(() => {
  stopHub()
})
</script>

<style scoped>
.toolbar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}
</style>
