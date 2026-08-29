<template>
  <div class="seven-page platform-control-mode">
    <div class="toolbar">
      <el-select v-model="scope" placeholder="作用域" style="width: 180px" @change="load">
        <el-option v-for="s in scopes" :key="s" :label="s" :value="s" />
      </el-select>
      <el-button type="primary" :disabled="!featureStore.flags.orchestrationBus" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.orchestrationBus">Features.OrchestrationBus=false</el-tag>
    </div>

    <el-card v-if="state" style="max-width: 480px; margin-top: 16px">
      <template #header>当前状态 — {{ state.scope }}</template>
      <p>模式：<strong>{{ modeLabel(state.mode) }}</strong></p>
      <p>急停：<el-tag :type="state.eStop ? 'danger' : 'success'">{{ state.eStop ? 'ON' : 'OFF' }}</el-tag></p>
      <p>更新时间：{{ state.updatedAt }}</p>

      <div class="actions">
        <span>设置模式：</span>
        <el-button
          v-for="m in modes"
          :key="m.value"
          size="small"
          :type="state.mode === m.value ? 'primary' : 'default'"
          :disabled="!featureStore.flags.orchestrationBus"
          @click="setMode(m.value)"
        >
          {{ m.label }}
        </el-button>
      </div>

      <div class="actions" style="margin-top: 12px">
        <el-button type="danger" :disabled="!featureStore.flags.orchestrationBus || state.eStop" @click="setEStop(true)">
          急停 ON
        </el-button>
        <el-button type="success" :disabled="!featureStore.flags.orchestrationBus || !state.eStop" @click="setEStop(false)">
          急停 OFF
        </el-button>
      </div>
    </el-card>
    <el-empty v-else description="加载中…" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface ControlModeState {
  scope: string
  mode: number
  eStop: boolean
  updatedAt: string
}

const featureStore = useFeatureStore()
const scopes = ['Global', 'Stacker', 'FourWay']
const scope = ref('Global')
const state = ref<ControlModeState | null>(null)

const modes = [
  { value: 0, label: '自动' },
  { value: 1, label: '半自动' },
  { value: 2, label: '手动' },
]

function modeLabel(mode: number) {
  return modes.find((m) => m.value === mode)?.label ?? String(mode)
}

async function load() {
  if (!featureStore.flags.orchestrationBus) return
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
    ElMessage.success(res.message || '模式已更新')
  } else {
    ElMessage.error(res.message || '更新失败')
  }
}

async function setEStop(eStop: boolean) {
  const res = await http.post<{ status: boolean; message?: string; data?: ControlModeState }>(
    `/api/ControlMode/${scope.value}/estop`,
    { eStop },
  )
  if (res.status) {
    state.value = res.data ?? state.value
    ElMessage.success(res.message || '急停状态已更新')
  } else {
    ElMessage.error(res.message || '更新失败')
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
