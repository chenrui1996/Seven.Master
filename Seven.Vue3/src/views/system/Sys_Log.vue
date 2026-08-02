<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysLog.listTitle') }}</span>
        </div>
      </template>

      <el-form inline size="small" class="search-bar" @submit.prevent="onSearch">
        <el-form-item :label="t('sysLog.keyword')">
          <el-input v-model="keyword" clearable style="width: 220px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item :label="t('sysLog.colType')">
          <el-input v-model="logType" clearable style="width: 140px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">{{ t('common.search') }}</el-button>
          <el-button @click="onResetSearch">{{ t('common.reset') }}</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="id" :label="t('sysLog.colId')" width="80" />
        <el-table-column prop="logType" :label="t('sysLog.colType')" width="100" />
        <el-table-column prop="userName" :label="t('sysLog.colUser')" width="120" />
        <el-table-column prop="url" :label="t('sysLog.colUrl')" min-width="200" show-overflow-tooltip />
        <el-table-column prop="ipAddress" :label="t('sysLog.colIp')" width="130" />
        <el-table-column prop="serviceTime" :label="t('sysLog.colDuration')" width="90" align="right" />
        <el-table-column prop="createDate" :label="t('sysLog.colTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.createDate as string) }}</template>
        </el-table-column>
        <el-table-column prop="exceptionInfo" :label="t('sysLog.colException')" min-width="160" show-overflow-tooltip />
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rows"
        :total="total"
        @change="loadData"
        style="margin-top: 16px"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getPageData } from '../../api/http'

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const keyword = ref('')
const logType = ref('')

function formatTime(value?: string) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

function buildWheres() {
  const parts: { name: string; value: string; displayType: string }[] = []
  if (keyword.value.trim()) {
    parts.push({ name: 'userName', value: keyword.value.trim(), displayType: 'like' })
    parts.push({ name: 'url', value: keyword.value.trim(), displayType: 'like' })
  }
  if (logType.value.trim()) {
    parts.push({ name: 'logType', value: logType.value.trim(), displayType: 'equal' })
  }
  return parts.length ? JSON.stringify(parts) : undefined
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_Log/getPageData', {
      page: page.value,
      rows: rows.value,
      wheres: buildWheres(),
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows ?? []
    }
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  loadData()
}

function onResetSearch() {
  keyword.value = ''
  logType.value = ''
  onSearch()
}

onMounted(loadData)
</script>
