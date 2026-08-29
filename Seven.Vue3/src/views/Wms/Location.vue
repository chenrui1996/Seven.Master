<template>
  <div class="seven-page wms-location">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.wms" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.wms">Features.Wms=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="code" label="库位编码" min-width="120" />
      <el-table-column prop="warehouseId" label="仓库Id" width="90" />
      <el-table-column prop="aisle" label="巷道" width="80" />
      <el-table-column prop="row" label="排" width="70" />
      <el-table-column prop="column" label="列" width="70" />
      <el-table-column prop="layer" label="层" width="70" />
      <el-table-column prop="isOccupied" label="占用" width="80">
        <template #default="{ row }">{{ row.isOccupied ? '是' : '否' }}</template>
      </el-table-column>
      <el-table-column prop="isLocked" label="锁定" width="80">
        <template #default="{ row }">{{ row.isLocked ? '是' : '否' }}</template>
      </el-table-column>
      <el-table-column prop="isHandover" label="交接位" width="90">
        <template #default="{ row }">{{ row.isHandover ? '是' : '否' }}</template>
      </el-table-column>
      <el-table-column prop="currentContainerCode" label="当前容器" min-width="120" show-overflow-tooltip />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface LocationRow {
  id: number
  code: string
  warehouseId: number
  aisle?: string
  row?: string
  column?: string
  layer?: string
  isOccupied: boolean
  isLocked: boolean
  isHandover: boolean
  currentContainerCode?: string
}

const featureStore = useFeatureStore()
const rows = ref<LocationRow[]>([])
const loading = ref(false)

async function load() {
  if (!featureStore.flags.wms) return
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: LocationRow[] } }>('/api/WmsLocation/getPageData', {
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
.wms-location .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
