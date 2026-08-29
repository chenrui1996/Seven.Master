<template>
  <div class="seven-page wms-inbound">
    <div class="toolbar">
      <el-button type="primary" :disabled="!featureStore.flags.wms" @click="load">刷新</el-button>
      <el-tag type="warning" v-if="!featureStore.flags.wms">Features.Wms=false</el-tag>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="orderNo" label="单号" min-width="140" />
      <el-table-column prop="orderType" label="类型" width="90" />
      <el-table-column prop="status" label="状态" width="100" />
      <el-table-column prop="createDate" label="创建时间" min-width="160" />
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" :disabled="!featureStore.flags.wms" @click="approve(row.id)">审核</el-button>
          <el-button link type="success" :disabled="!featureStore.flags.wms" @click="receive(row.id)">收货</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import { useFeatureStore } from '../../stores/features'

interface OrderRow {
  id: number
  orderNo: string
  orderType: number
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
    const res = await http.post<{ status: boolean; data?: { rows?: OrderRow[] } }>('/api/WmsInboundOrder/getPageData', {
      page: 1,
      rows: 100,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

async function approve(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsInboundOrder/approve/${id}`)
  if (res.status) {
    ElMessage.success(res.message || '审核成功')
    await load()
  } else {
    ElMessage.error(res.message || '审核失败')
  }
}

async function receive(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsInboundOrder/receive/${id}`, {})
  if (res.status) {
    ElMessage.success(res.message || '收货成功')
    await load()
  } else {
    ElMessage.error(res.message || '收货失败')
  }
}

onMounted(load)
</script>

<style scoped>
.wms-inbound .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
