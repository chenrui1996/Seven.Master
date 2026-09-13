<template>
  <div class="wcs-ops seven-page">
    <div class="ops-toolbar">
      <el-tag type="info">{{ scope }}</el-tag>
      <el-button type="primary" :loading="loading" @click="load">{{ t('wcsOps.common.refresh') }}</el-button>
    </div>
    <el-card v-if="state" style="max-width: 520px; margin-top: 12px">
      <template #header>{{ t('wcsOps.controlMode.header', { scope: state.scope }) }}</template>
      <p>{{ t('wcsOps.controlMode.mode') }}<strong>{{ modeLabel(state.mode) }}</strong></p>
      <p>
        {{ t('wcsOps.controlMode.packEStop') }}
        <el-tag :type="state.eStop ? 'danger' : 'success'">{{ state.eStop ? 'ON' : 'OFF' }}</el-tag>
      </p>
      <p v-if="global" style="margin-top: 8px">
        {{ t('wcsOps.controlMode.globalEStop') }}
        <el-tag :type="global.eStop ? 'danger' : 'success'">{{ global.eStop ? 'ON' : 'OFF' }}</el-tag>
      </p>
      <div class="actions">
        <el-button
          v-for="m in modes"
          :key="m.value"
          size="small"
          :type="state.mode === m.value ? 'primary' : 'default'"
          @click="setMode(m.value)"
        >
          {{ m.label }}
        </el-button>
      </div>
      <div class="actions" style="margin-top: 12px">
        <el-button type="danger" :disabled="state.eStop" @click="setEStop(false, true)">
          {{ t('wcsOps.controlMode.packOn') }}
        </el-button>
        <el-button type="success" :disabled="!state.eStop" @click="setEStop(false, false)">
          {{ t('wcsOps.controlMode.packOff') }}
        </el-button>
        <el-button type="danger" plain @click="setEStop(true, true)">{{ t('wcsOps.controlMode.globalOn') }}</el-button>
        <el-button type="success" plain @click="setEStop(true, false)">{{ t('wcsOps.controlMode.globalOff') }}</el-button>
      </div>
    </el-card>
    <el-empty v-else :description="t('wcsOps.controlMode.loading')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import '../../styles/wcs-ops.css'

const props = defineProps<{ apiBase: string; scope: string }>()
const { t } = useI18n()

interface ControlModeState {
  scope: string
  mode: number
  eStop: boolean
  updatedAt?: string
}

const loading = ref(false)
const state = ref<ControlModeState | null>(null)
const global = ref<ControlModeState | null>(null)
const modes = computed(() => [
  { value: 0, label: t('wcsOps.controlMode.auto') },
  { value: 1, label: t('wcsOps.controlMode.semi') },
  { value: 2, label: t('wcsOps.controlMode.manual') },
])

function modeLabel(mode: number) {
  return modes.value.find((m) => m.value === mode)?.label ?? String(mode)
}

async function load() {
  loading.value = true
  try {
    const [pack, g] = await Promise.all([
      http.get<{ status: boolean; data?: ControlModeState }>(`${props.apiBase}/control-mode`),
      http.get<{ status: boolean; data?: ControlModeState }>('/api/ControlMode/Global'),
    ])
    if (pack.status) state.value = pack.data ?? null
    if (g.status) global.value = g.data ?? null
  } finally {
    loading.value = false
  }
}

async function setMode(mode: number) {
  const res = await http.post<{ status: boolean; message?: string; data?: ControlModeState }>(
    `${props.apiBase}/control-mode`,
    { mode },
  )
  if (res.status) {
    state.value = res.data ?? state.value
    ElMessage.success(res.message || t('wcsOps.common.updated'))
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

async function setEStop(isGlobal: boolean, on: boolean) {
  const body = isGlobal ? { globalEStop: on } : { eStop: on }
  const res = await http.post<{ status: boolean; message?: string; data?: ControlModeState }>(
    `${props.apiBase}/control-mode`,
    body,
  )
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.updated'))
    await load()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

onMounted(load)
</script>

<style scoped>
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}
</style>
