<template>
  <div class="crud-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>用户管理</span>
          <el-button v-permission="'Sys_User.Add'" type="primary" @click="dialogVisible = true">新增</el-button>
        </div>
      </template>
      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="user_Id" label="ID" width="80" />
        <el-table-column prop="userName" label="账号" />
        <el-table-column prop="userTrueName" label="姓名" />
        <el-table-column prop="roleName" label="角色" />
        <el-table-column prop="phoneNo" label="手机" />
        <el-table-column label="状态" width="80">
          <template #default="{ row }">{{ row.enable === 1 ? '启用' : '禁用' }}</template>
        </el-table-column>
      </el-table>
      <el-pagination v-model:current-page="page" v-model:page-size="rows" :total="total" @change="loadData" style="margin-top:16px" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getPageData } from '../../api/http'

const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const dialogVisible = ref(false)

/** 加载分页数据 */
async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_User/getPageData', { page: page.value, rows: rows.value })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally {
    loading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.toolbar { display: flex; justify-content: space-between; align-items: center; }
</style>
