<template>
  <el-card header="角色管理">
    <el-table :data="tableData" v-loading="loading" border>
      <el-table-column prop="role_Id" label="ID" width="80" />
      <el-table-column prop="roleName" label="角色名称" />
      <el-table-column prop="orderNo" label="排序" width="80" />
    </el-table>
    <el-pagination v-model:current-page="page" :total="total" @change="loadData" style="margin-top:16px" />
  </el-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getPageData } from '../../api/http'

const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_Role/getPageData', { page: page.value, rows: 30 })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally { loading.value = false }
}

onMounted(loadData)
</script>
