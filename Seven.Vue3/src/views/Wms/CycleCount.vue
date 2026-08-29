<template>
  <div class="seven-page wms-cyclecount">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.wms" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.wms">Features.Wms=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="orderNo" label="单号" min-width="140" />
      <el-table-column prop="status" label="状态" width="100" />
      <el-table-column prop="createDate" label="创建时间" min-width="160" />
    </el-table>

    <el-empty v-if="!loading && rows.length === 0" description="盘点单列表（占位）" style="margin-top: 24px" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface OrderRow {
  id: number
  orderNo: string
  status: number
  createDate?: string
}

const featureStore = useFeatureStore()
const rows = ref<OrderRow[]>([])
const loading = ref(false)

async function load() {
  if (!featureStore.flags.wms) return
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: OrderRow[] } }>('/api/WmsCycleCount/getPageData', {
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
.wms-cyclecount .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
