<template>
  <div class="crud-page seven-page">
    <div class="seven-page-header">
      <div>
        <h1 class="seven-page-title">{{ t('sysUser.title') }}</h1>
        <p class="seven-page-subtitle">{{ t('sysUser.subtitle') }}</p>
      </div>
    </div>
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysUser.listTitle') }}</span>
          <el-button v-permission="'Sys_User.Add'" type="primary" @click="dialogVisible = true">
            {{ t('common.add') }}
          </el-button>
        </div>
      </template>
      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="user_Id" :label="t('sysUser.colId')" width="80" />
        <el-table-column prop="userName" :label="t('sysUser.colAccount')" />
        <el-table-column prop="userTrueName" :label="t('sysUser.colName')" />
        <el-table-column prop="roleName" :label="t('sysUser.colRole')" />
        <el-table-column prop="phoneNo" :label="t('sysUser.colPhone')" />
        <el-table-column :label="t('sysUser.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rows"
        :total="total"
        @change="loadData"
        style="margin-top:16px"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getPageData } from '../../api/http'

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const dialogVisible = ref(false)

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
