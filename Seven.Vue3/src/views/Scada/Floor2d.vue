<template>
  <div class="seven-page scada-floor2d">
    <div class="toolbar">
      <el-select v-model="selectedViewId" :placeholder="t('scada.selectView')" style="width: 220px" @change="loadStatus">
        <el-option v-for="v in views" :key="v.id" :label="v.name" :value="v.id" />
      </el-select>
      <el-button type="primary" @click="loadStatus">{{ t('scada.refresh') }}</el-button>
    </div>

    <div
      v-if="status"
      class="canvas"
      :style="{ width: status.width + 'px', height: status.height + 'px' }"
    >
      <div
        v-for="node in status.nodes"
        :key="node.bindId"
        class="node"
        :class="{ occupied: node.isOccupied }"
        :style="{ left: node.x + 'px', top: node.y + 'px' }"
        :title="node.locationCode"
      >
        {{ node.label || node.locationCode }}
      </div>
    </div>
    <el-empty v-else :description="t('scada.empty')" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../api/http'

const { t } = useI18n()

interface ScdViewRow {
  id: number
  code: string
  name: string
  width: number
  height: number
}

interface ScdNodeStatus {
  bindId: number
  locationCode: string
  x: number
  y: number
  label?: string
  isOccupied: boolean
}

interface ScdViewStatus {
  viewId: number
  code: string
  name: string
  width: number
  height: number
  nodes: ScdNodeStatus[]
}

const views = ref<ScdViewRow[]>([])
const selectedViewId = ref<number | null>(null)
const status = ref<ScdViewStatus | null>(null)
let pollTimer: ReturnType<typeof setInterval> | null = null

async function loadViews() {
  const res = await http.post<{ status: boolean; data?: { rows?: ScdViewRow[] } }>('/api/ScdView/getPageData', {
    page: 1,
    rows: 100,
  })
  if (res.status) {
    views.value = res.data?.rows ?? []
    if (!selectedViewId.value && views.value.length > 0) {
      selectedViewId.value = views.value[0].id
      await loadStatus()
    }
  }
}

async function loadStatus() {
  if (!selectedViewId.value) return
  const res = await http.get<{ status: boolean; data?: ScdViewStatus }>(`/api/ScdView/${selectedViewId.value}/status`)
  if (res.status) status.value = res.data ?? null
}

onMounted(async () => {
  await loadViews()
  pollTimer = setInterval(loadStatus, 5000)
})

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer)
})
</script>

<style scoped>
.scada-floor2d .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.scada-floor2d .canvas {
  position: relative;
  border: 1px solid var(--el-border-color);
  background: #f5f7fa;
  overflow: hidden;
}
.scada-floor2d .node {
  position: absolute;
  min-width: 48px;
  padding: 4px 8px;
  font-size: 12px;
  text-align: center;
  border-radius: 4px;
  background: #67c23a;
  color: #fff;
  transform: translate(-50%, -50%);
  cursor: default;
  user-select: none;
}
.scada-floor2d .node.occupied {
  background: #f56c6c;
}
</style>
