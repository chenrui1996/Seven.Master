<template>
  <div class="seven-page platform-interface-log">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.orchestrationBus" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.orchestrationBus">Features.OrchestrationBus=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="direction" label="方向" width="80">
        <template #default="{ row }">{{ row.direction === 0 ? 'In' : 'Out' }}</template>
      </el-table-column>
      <el-table-column prop="systemCode" label="系统" width="100" />
      <el-table-column prop="path" label="路径" min-width="160" show-overflow-tooltip />
      <el-table-column prop="orderNo" label="单号" min-width="120" />
      <el-table-column prop="durationMs" label="耗时ms" width="90" />
      <el-table-column prop="success" label="成功" width="80">
        <template #default="{ row }">
          <el-tag :type="row.success ? 'success' : 'danger'">{{ row.success ? '是' : '否' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="errorMessage" label="错误" min-width="160" show-overflow-tooltip />
      <el-table-column prop="createDate" label="时间" min-width="160" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface LogRow {
  id: number
  direction: number
  systemCode: string
  path?: string
  orderNo?: string
  durationMs: number
  success: boolean
  errorMessage?: string
  createDate?: string
}

const featureStore = useFeatureStore()
const rows = ref<LogRow[]>([])
const loading = ref(false)

async function load() {
  if (!featureStore.flags.orchestrationBus) return
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: LogRow[] } }>('/api/InterfaceLog/getPageData', {
      page: 1,
      rows: 100,
      sort: 'Id',
      order: 'desc',
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.platform-interface-log .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
