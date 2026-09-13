<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.stkRequestPoint.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.stkRequestPoint.subtitle') }}</p>
    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wcsOps.common.refresh') }}</el-button>
    </div>
    <el-table :data="rows" border stripe v-loading="loading">
      <el-table-column prop="code" :label="t('wcsOps.common.code')" min-width="140" />
      <el-table-column prop="pointType" :label="t('wcsOps.common.type')" width="140" />
      <el-table-column prop="aisleCode" :label="t('wcsOps.common.aisle')" width="120" />
      <el-table-column prop="isEnabled" :label="t('wcsOps.common.enabled')" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
            {{ row.isEnabled ? t('wcsOps.common.yes') : t('wcsOps.common.no') }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="t('wcsOps.common.actions')" width="180">
        <template #default="{ row }">
          <el-button v-if="!row.isEnabled" link type="primary" @click="setEnabled(row.id, true)">
            {{ t('wcsOps.common.enable') }}
          </el-button>
          <el-button v-else link type="danger" @click="setEnabled(row.id, false)">
            {{ t('wcsOps.common.disable') }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../../../api/http'
import '../../../../styles/wcs-ops.css'

const { t } = useI18n()
const loading = ref(false)
const rows = ref<any[]>([])

async function load() {
  loading.value = true
  try {
    const res = await http.get<{ status: boolean; data?: any[] }>('/api/Wcs/Stacker/Ops/request-points')
    if (res.status) rows.value = res.data ?? []
  } finally {
    loading.value = false
  }
}

async function setEnabled(id: number, enabled: boolean) {
  const url = enabled
    ? `/api/Wcs/Stacker/Ops/request-point/${id}/enable`
    : `/api/Wcs/Stacker/Ops/request-point/${id}/disable`
  const res = await http.post<{ status: boolean; message?: string }>(url, {})
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.updated'))
    await load()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

onMounted(load)
</script>
