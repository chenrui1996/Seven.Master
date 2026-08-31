<template>
  <div class="seven-page platform-control-mode">
    <div class="toolbar">
      <el-select v-model="scope" :placeholder="t('platform.controlMode.scopePlaceholder')" style="width: 180px" @change="load">
        <el-option v-for="s in scopes" :key="s" :label="s" :value="s" />
      </el-select>
      <el-button type="primary" @click="load">{{ t('platform.refresh') }}</el-button>
    </div>

    <el-card v-if="state" style="max-width: 480px; margin-top: 16px">
      <template #header>{{ t('platform.controlMode.currentState', { scope: state.scope }) }}</template>
      <p>{{ t('platform.controlMode.mode') }}<strong>{{ modeLabel(state.mode) }}</strong></p>
      <p>
        {{ t('platform.controlMode.eStop') }}
        <el-tag :type="state.eStop ? 'danger' : 'success'">{{ state.eStop ? 'ON' : 'OFF' }}</el-tag>
      </p>
      <p>{{ t('platform.controlMode.updatedAt') }}{{ state.updatedAt }}</p>

      <div class="actions">
        <span>{{ t('platform.controlMode.setMode') }}</span>
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
        <el-button type="danger" :disabled="state.eStop" @click="setEStop(true)">
          {{ t('platform.controlMode.eStopOn') }}
        </el-button>
        <el-button type="success" :disabled="!state.eStop" @click="setEStop(false)">
          {{ t('platform.controlMode.eStopOff') }}
        </el-button>
      </div>
    </el-card>
    <el-empty v-else :description="t('platform.controlMode.loading')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../api/http'

const { t } = useI18n()

interface ControlModeState {
  scope: string
  mode: number
  eStop: boolean
  updatedAt: string
}

const scopes = ['Global', 'Stacker', 'FourWay']
const scope = ref('Global')
const state = ref<ControlModeState | null>(null)

const modes = computed(() => [
  { value: 0, label: t('platform.controlMode.auto') },
  { value: 1, label: t('platform.controlMode.semi') },
  { value: 2, label: t('platform.controlMode.manual') },
])

function modeLabel(mode: number) {
  return modes.value.find((m) => m.value === mode)?.label ?? String(mode)
}

async function load() {
  const res = await http.get<{ status: boolean; data?: ControlModeState }>(`/api/ControlMode/${scope.value}`)
  if (res.status) state.value = res.data ?? null
}

async function setMode(mode: number) {
  const res = await http.post<{ status: boolean; message?: string; data?: ControlModeState }>(
    `/api/ControlMode/${scope.value}/mode`,
    { mode },
  )
  if (res.status) {
    state.value = res.data ?? state.value
    ElMessage.success(res.message || t('platform.controlMode.modeUpdated'))
  } else {
    ElMessage.error(res.message || t('platform.controlMode.updateFailed'))
  }
}

async function setEStop(eStop: boolean) {
  const res = await http.post<{ status: boolean; message?: string; data?: ControlModeState }>(
    `/api/ControlMode/${scope.value}/estop`,
    { eStop },
  )
  if (res.status) {
    state.value = res.data ?? state.value
    ElMessage.success(res.message || t('platform.controlMode.eStopUpdated'))
  } else {
    ElMessage.error(res.message || t('platform.controlMode.updateFailed'))
  }
}

onMounted(load)
</script>

<style scoped>
.platform-control-mode .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
.platform-control-mode .actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
