<template>
  <div class="seven-page bus-transport-order">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.orchestrationBus" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.orchestrationBus">Features.OrchestrationBus=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" min-width="280" show-overflow-tooltip />
      <el-table-column prop="containerCode" label="容器" min-width="120" />
      <el-table-column prop="fromLocationCode" label="起点" min-width="120" />
      <el-table-column prop="toLocationCode" label="终点" min-width="120" />
      <el-table-column prop="status" label="状态" width="100" />
      <el-table-column prop="refType" label="来源类型" width="110" />
      <el-table-column prop="refId" label="来源单号" min-width="120" />
      <el-table-column prop="failReason" label="失败原因" min-width="160" show-overflow-tooltip />
      <el-table-column prop="createDate" label="创建时间" min-width="160" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import http from '../../../api/http'
import { useFeatureStore } from '../../../stores/features'

interface BusOrderRow {
  id: string
  containerCode: string
  fromLocationCode: string
  toLocationCode: string
  status: number
  refType?: string
  refId?: string
  failReason?: string
  createDate?: string
}

const featureStore = useFeatureStore()
const rows = ref<BusOrderRow[]>([])
const loading = ref(false)

async function load() {
  if (!featureStore.flags.orchestrationBus) return
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: BusOrderRow[] } }>('/api/BusTransportOrder/getPageData', {
      page: 1,
      rows: 100,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.bus-transport-order .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
