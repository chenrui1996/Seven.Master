<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wmsOps.outbound.title') }}</h1>
    <p class="ops-sub">{{ t('wmsOps.outbound.subtitle') }}</p>

    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wmsOps.refresh') }}</el-button>
      <el-button @click="showCreate = true">{{ t('wmsOps.outbound.create') }}</el-button>
    </div>

    <div class="ops-split">
      <div class="ops-panel">
        <el-table :data="rows" stripe border v-loading="loading" highlight-current-row @current-change="onSelect">
          <el-table-column prop="id" label="Id" width="70" />
          <el-table-column prop="orderNo" :label="t('wmsOps.orderNo')" min-width="130">
            <template #default="{ row }"><span class="ops-mono">{{ row.orderNo }}</span></template>
          </el-table-column>
          <el-table-column prop="wcsGroupNo" :label="t('wmsOps.outbound.groupNo')" min-width="110" />
          <el-table-column prop="status" :label="t('wmsOps.status')" width="90" />
          <el-table-column :label="t('wmsOps.actions')" width="200" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="approve(row.id)">{{ t('wmsOps.approve') }}</el-button>
              <el-button link type="warning" @click="ship(row.id)">{{ t('wmsOps.outbound.ship') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <div class="ops-panel" v-if="detail">
        <h3>{{ t('wmsOps.outbound.lines') }} · <span class="ops-mono">{{ detail.orderNo }}</span></h3>
        <el-table :data="detail.lines || []" size="small" border>
          <el-table-column prop="lineNo" :label="t('wmsOps.line')" width="50" />
          <el-table-column prop="wcsPri" label="Pri" width="60" />
          <el-table-column prop="materialCode" :label="t('wmsOps.material')" min-width="100" />
          <el-table-column prop="qty" :label="t('wmsOps.qty')" width="70" />
          <el-table-column prop="fromLocation" label="From" min-width="110" />
          <el-table-column prop="toLocation" label="To" min-width="110" />
          <el-table-column prop="containerCode" :label="t('wmsOps.container')" min-width="100" />
        </el-table>
      </div>
    </div>

    <el-dialog v-model="showCreate" :title="t('wmsOps.outbound.create')" width="640px" destroy-on-close>
      <el-form label-width="110px">
        <el-form-item :label="t('wmsOps.orderNo')"><el-input v-model="form.orderNo" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.outbound.groupNo')">
          <el-input v-model="form.wcsGroupNo" class="ops-mono" :placeholder="t('wmsOps.outbound.groupPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('wmsOps.material')"><el-input v-model="form.materialCode" /></el-form-item>
        <el-form-item :label="t('wmsOps.qty')"><el-input-number v-model="form.qty" :min="0.001" /></el-form-item>
        <el-form-item label="From"><el-input v-model="form.fromLocation" class="ops-mono" /></el-form-item>
        <el-form-item label="To"><el-input v-model="form.toLocation" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.container')"><el-input v-model="form.containerCode" class="ops-mono" /></el-form-item>
        <el-form-item label="WcsPri"><el-input-number v-model="form.wcsPri" :min="0" /></el-form-item>
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

interface OrderRow {
  id: number
  orderNo: string
  wcsGroupNo?: string
  status: number
}

const rows = ref<OrderRow[]>([])
const loading = ref(false)
const creating = ref(false)
const showCreate = ref(false)
const detail = ref<Record<string, unknown> | null>(null)

const form = reactive({
  orderNo: '',
  wcsGroupNo: '',
  orderType: 0,
  materialCode: '',
  qty: 1,
  fromLocation: '',
  toLocation: '',
  containerCode: '',
  wcsPri: 1,
})

async function load() {
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: OrderRow[] } }>('/api/WmsOutboundOrder/getPageData', {
      page: 1,
      rows: 100,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

async function onSelect(row: OrderRow | null) {
  if (!row) {
    detail.value = null
    return
  }
  const res = await http.get<{ status: boolean; data?: Record<string, unknown> }>(`/api/WmsOutboundOrder/${row.id}`)
  if (res.status) detail.value = res.data ?? null
}

async function create() {
  creating.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>('/api/WmsOutboundOrder/add', {
      orderNo: form.orderNo,
      orderType: form.orderType,
      wcsGroupNo: form.wcsGroupNo || null,
      lines: [
        {
          lineNo: 1,
          materialCode: form.materialCode,
          qty: form.qty,
          fromLocation: form.fromLocation || null,
          toLocation: form.toLocation || null,
          containerCode: form.containerCode || null,
          wcsPri: form.wcsPri,
        },
      ],
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

async function approve(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsOutboundOrder/approve/${id}`)
  if (res.status) {
    ElMessage.success(res.message || t('wmsOps.approveSuccess'))
    await load()
  } else ElMessage.error(res.message || t('wmsOps.failed'))
}

async function ship(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsOutboundOrder/ship/${id}`)
  if (res.status) {
    ElMessage.success(res.message || t('wmsOps.outbound.shipSuccess'))
    await load()
  } else ElMessage.error(res.message || t('wmsOps.failed'))
}

onMounted(load)
</script>
