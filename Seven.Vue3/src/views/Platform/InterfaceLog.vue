<template>
  <div class="seven-page platform-interface-log">
    <div class="toolbar">
      <el-button type="primary" @click="load">{{ t('platform.refresh') }}</el-button>
    </div>

    <el-table :data="rows" stripe border v-loading="loading" style="width: 100%; margin-top: 12px">
      <el-table-column prop="id" label="Id" width="80" />
      <el-table-column prop="direction" :label="t('platform.interfaceLog.direction')" width="80">
        <template #default="{ row }">{{ row.direction === 0 ? 'In' : 'Out' }}</template>
      </el-table-column>
      <el-table-column prop="systemCode" :label="t('platform.interfaceLog.system')" width="100" />
      <el-table-column prop="path" :label="t('platform.interfaceLog.path')" min-width="160" show-overflow-tooltip />
      <el-table-column prop="orderNo" :label="t('platform.interfaceLog.orderNo')" min-width="120" />
      <el-table-column prop="durationMs" :label="t('platform.interfaceLog.durationMs')" width="90" />
      <el-table-column prop="success" :label="t('platform.interfaceLog.success')" width="80">
        <template #default="{ row }">
          <el-tag :type="row.success ? 'success' : 'danger'">
            {{ row.success ? t('platform.interfaceLog.yes') : t('platform.interfaceLog.no') }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="errorMessage" :label="t('platform.interfaceLog.error')" min-width="160" show-overflow-tooltip />
      <el-table-column prop="createDate" :label="t('platform.interfaceLog.time')" min-width="160" />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../api/http'

const { t } = useI18n()

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

const rows = ref<LogRow[]>([])
const loading = ref(false)

async function load() {
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
