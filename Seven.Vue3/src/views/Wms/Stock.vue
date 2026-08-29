<template>
  <div class="seven-page wms-stock">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.wms" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.wms">Features.Wms=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="locationCode" label="库位" min-width="120" />
      <el-table-column prop="materialCode" label="物料" min-width="120" />
      <el-table-column prop="containerCode" label="容器" min-width="120" />
      <el-table-column prop="qty" label="数量" width="100" />
      <el-table-column prop="availableQty" label="可用" width="100" />
      <el-table-column prop="lot" label="批次" min-width="100" show-overflow-tooltip />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface StockRow {
  id: number
  locationCode: string
  materialCode: string
  containerCode?: string
  qty: number
  availableQty: number
  lot?: string
}

const featureStore = useFeatureStore()
const rows = ref<StockRow[]>([])
const loading = ref(false)

async function load() {
  if (!featureStore.flags.wms) return
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: StockRow[] } }>('/api/WmsStock/getPageData', {
      page: 1,
      rows: 200,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.wms-stock .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
