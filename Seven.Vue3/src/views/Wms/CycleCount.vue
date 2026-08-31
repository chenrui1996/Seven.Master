<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wmsOps.cycleCount.title') }}</h1>
    <p class="ops-sub">{{ t('wmsOps.cycleCount.subtitle') }}</p>

    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wmsOps.refresh') }}</el-button>
      <el-button @click="showCreate = true">{{ t('wmsOps.cycleCount.create') }}</el-button>
    </div>

    <div class="ops-split">
      <div class="ops-panel">
        <el-table :data="rows" stripe border v-loading="loading" highlight-current-row @current-change="onSelect">
          <el-table-column prop="id" label="Id" width="70" />
          <el-table-column prop="orderNo" :label="t('wmsOps.orderNo')" min-width="140">
            <template #default="{ row }"><span class="ops-mono">{{ row.orderNo }}</span></template>
          </el-table-column>
          <el-table-column prop="status" :label="t('wmsOps.status')" width="90" />
          <el-table-column :label="t('wmsOps.actions')" width="160" fixed="right">
            <template #default="{ row }">
              <el-button link type="warning" @click="confirm(row.id)">{{ t('wmsOps.cycleCount.confirm') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <div class="ops-panel" v-if="detail">
        <h3>{{ t('wmsOps.cycleCount.lines') }} · <span class="ops-mono">{{ detail.orderNo }}</span></h3>
        <el-table :data="detail.lines || []" size="small" border>
          <el-table-column prop="lineNo" :label="t('wmsOps.line')" width="50" />
          <el-table-column prop="locationCode" :label="t('wmsOps.cycleCount.location')" min-width="120" />
          <el-table-column prop="materialCode" :label="t('wmsOps.material')" min-width="100" />
          <el-table-column prop="bookQty" :label="t('wmsOps.cycleCount.bookQty')" width="80" />
          <el-table-column prop="countQty" :label="t('wmsOps.cycleCount.countQty')" width="80" />
          <el-table-column prop="diffQty" :label="t('wmsOps.cycleCount.diffQty')" width="80" />
          <el-table-column :label="t('wmsOps.cycleCount.record')" width="160">
            <template #default="{ row }">
              <el-input-number v-model="row._input" size="small" :min="0" style="width: 90px" />
              <el-button link type="primary" @click="record(detail.id as number, row.lineNo, row._input)">{{ t('wmsOps.save') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <el-dialog v-model="showCreate" :title="t('wmsOps.cycleCount.createTitle')" width="520px">
      <el-form label-width="100px">
        <el-form-item :label="t('wmsOps.orderNo')"><el-input v-model="form.orderNo" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.cycleCount.location')"><el-input v-model="form.locationCode" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.material')"><el-input v-model="form.materialCode" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreate = false">{{ t('wmsOps.cancel') }}</el-button>
        <el-button type="primary" :loading="creating" @click="create">{{ t('wmsOps.create') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import '../../styles/wcs-ops.css'

const { t } = useI18n()

interface Row {
  id: number
  orderNo: string
  status: number
}

const rows = ref<Row[]>([])
const loading = ref(false)
const creating = ref(false)
const showCreate = ref(false)
const detail = ref<Record<string, unknown> & { id?: number; lines?: Record<string, unknown>[] } | null>(null)

const form = reactive({ orderNo: '', locationCode: '', materialCode: '' })

async function load() {
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: Row[] } }>('/api/WmsCycleCount/getPageData', {
      page: 1,
      rows: 100,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

async function onSelect(row: Row | null) {
  if (!row) {
    detail.value = null
    return
  }
  const res = await http.post<{ status: boolean; data?: Record<string, unknown> }>(`/api/WmsCycleCount/get/${row.id}`)
  if (res.status && res.data) {
    const d = res.data as { lines?: Record<string, unknown>[] }
    d.lines = (d.lines || []).map((l) => ({ ...l, _input: l.countQty ?? l.bookQty ?? 0 }))
    detail.value = { ...d, id: row.id }
  }
}

async function create() {
  creating.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>('/api/WmsCycleCount/add', {
      orderNo: form.orderNo,
      lines: [{ lineNo: 1, locationCode: form.locationCode, materialCode: form.materialCode }],
    })
    if (res.status) {
      ElMessage.success(res.message || t('wmsOps.createSuccess'))
      showCreate.value = false
      await load()
    } else ElMessage.error(res.message || t('wmsOps.failed'))
  } finally {
    creating.value = false
  }
}

async function record(orderId: number, lineNo: number, qty: number) {
  const res = await http.post<{ status: boolean; message?: string }>(
    `/api/WmsCycleCount/record/${orderId}/${lineNo}`,
    qty,
  )
  if (res.status) {
    ElMessage.success(res.message || t('wmsOps.cycleCount.recorded'))
    const row = rows.value.find((r) => r.id === orderId)
    if (row) await onSelect(row)
  } else ElMessage.error(res.message || t('wmsOps.failed'))
}

async function confirm(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsCycleCount/confirm/${id}`)
  if (res.status) {
    ElMessage.success(res.message || t('wmsOps.cycleCount.adjustSuccess'))
    await load()
  } else ElMessage.error(res.message || t('wmsOps.failed'))
}

onMounted(load)
</script>
