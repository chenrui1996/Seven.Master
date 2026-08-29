<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wmsOps.inbound.title') }}</h1>
    <p class="ops-sub">{{ t('wmsOps.inbound.subtitle') }}</p>

    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wmsOps.refresh') }}</el-button>
      <el-button :loading="creating" @click="showCreate = true">{{ t('wmsOps.inbound.create') }}</el-button>
    </div>

    <div class="ops-split">
      <div class="ops-panel">
        <el-table :data="rows" stripe border v-loading="loading" highlight-current-row @current-change="onSelect">
          <el-table-column prop="id" label="Id" width="70" />
          <el-table-column prop="orderNo" :label="t('wmsOps.orderNo')" min-width="140">
            <template #default="{ row }"><span class="ops-mono">{{ row.orderNo }}</span></template>
          </el-table-column>
          <el-table-column prop="orderType" :label="t('wmsOps.type')" width="80" />
          <el-table-column prop="status" :label="t('wmsOps.status')" width="90" />
          <el-table-column prop="createDate" :label="t('wmsOps.createDate')" min-width="160" />
          <el-table-column :label="t('wmsOps.actions')" width="220" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" :loading="busyId === row.id" @click="approve(row.id)">{{ t('wmsOps.approve') }}</el-button>
              <el-button link type="success" @click="openPallet(row)">{{ t('wmsOps.inbound.pallet') }}</el-button>
              <el-button link @click="receive(row.id)">{{ t('wmsOps.inbound.receive') }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <div class="ops-panel" v-if="detail">
        <h3>{{ t('wmsOps.inbound.details') }} · <span class="ops-mono">{{ detail.orderNo }}</span></h3>
        <el-table :data="detail.lines || []" size="small" border>
          <el-table-column prop="lineNo" :label="t('wmsOps.line')" width="50" />
          <el-table-column prop="materialCode" :label="t('wmsOps.material')" min-width="100" />
          <el-table-column prop="qty" :label="t('wmsOps.inbound.planned')" width="70" />
          <el-table-column prop="completedQty" :label="t('wmsOps.inbound.completed')" width="70" />
        </el-table>
        <h3 style="margin-top: 16px">{{ t('wmsOps.inbound.palletDetails') }}</h3>
        <el-table :data="detail.details || []" size="small" border>
          <el-table-column prop="detailNo" label="#" width="50" />
          <el-table-column prop="containerCode" :label="t('wmsOps.container')" min-width="100" />
          <el-table-column prop="receiveLocationCode" :label="t('wmsOps.inbound.receiveLoc')" min-width="110" />
          <el-table-column prop="targetLocationCode" :label="t('wmsOps.inbound.targetLoc')" min-width="110" />
          <el-table-column prop="status" :label="t('wmsOps.status')" width="80" />
        </el-table>
      </div>
    </div>

    <el-dialog v-model="showCreate" :title="t('wmsOps.inbound.create')" width="560px" destroy-on-close>
      <el-form label-width="100px">
        <el-form-item :label="t('wmsOps.orderNo')"><el-input v-model="createForm.orderNo" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.type')">
          <el-input-number v-model="createForm.orderType" :min="0" />
        </el-form-item>
        <el-form-item :label="t('wmsOps.material')"><el-input v-model="createForm.materialCode" /></el-form-item>
        <el-form-item :label="t('wmsOps.qty')"><el-input-number v-model="createForm.qty" :min="0.001" :step="1" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreate = false">{{ t('wmsOps.cancel') }}</el-button>
        <el-button type="primary" :loading="creating" @click="create">{{ t('wmsOps.create') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showPallet" :title="t('wmsOps.inbound.pallet')" width="560px" destroy-on-close>
      <el-form label-width="120px">
        <el-form-item :label="t('wmsOps.inbound.lineNo')"><el-input-number v-model="palletForm.lineNo" :min="1" /></el-form-item>
        <el-form-item :label="t('wmsOps.qty')"><el-input-number v-model="palletForm.qty" :min="0.001" /></el-form-item>
        <el-form-item :label="t('wmsOps.container')"><el-input v-model="palletForm.containerCode" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.inbound.receiveLoc')"><el-input v-model="palletForm.receiveLocationCode" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wmsOps.inbound.targetLoc')">
          <el-input v-model="palletForm.targetLocationCode" class="ops-mono" :placeholder="t('wmsOps.inbound.targetPlaceholder')" />
        </el-form-item>
        <el-form-item label="PackId"><el-input v-model="palletForm.packId" placeholder="stacker / fourway" /></el-form-item>
        <el-form-item :label="t('wmsOps.inbound.allocate')">
          <el-switch v-model="palletForm.allocateTarget" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showPallet = false">{{ t('wmsOps.cancel') }}</el-button>
        <el-button type="primary" :loading="palletBusy" @click="buildPallet">{{ t('wmsOps.inbound.submitPallet') }}</el-button>
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
  orderType: number
  status: number
  createDate?: string
}

const rows = ref<OrderRow[]>([])
const loading = ref(false)
const creating = ref(false)
const busyId = ref<number | null>(null)
const showCreate = ref(false)
const showPallet = ref(false)
const palletBusy = ref(false)
const detail = ref<Record<string, unknown> | null>(null)
const palletOrderId = ref(0)

const createForm = reactive({
  orderNo: '',
  orderType: 0,
  materialCode: '',
  qty: 1,
})

const palletForm = reactive({
  lineNo: 1,
  qty: 1,
  containerCode: '',
  receiveLocationCode: '',
  targetLocationCode: '',
  packId: 'stacker',
  allocateTarget: true,
  height: 0,
  weight: 0,
})

async function load() {
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

async function onSelect(row: OrderRow | null) {
  if (!row) {
    detail.value = null
    return
  }
  const res = await http.get<{ status: boolean; data?: Record<string, unknown> }>(`/api/WmsInboundOrder/${row.id}`)
  if (res.status) detail.value = res.data ?? null
}

async function create() {
  creating.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>('/api/WmsInboundOrder/add', {
      orderNo: createForm.orderNo,
      orderType: createForm.orderType,
      lines: [{ lineNo: 1, materialCode: createForm.materialCode, qty: createForm.qty }],
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
  busyId.value = id
  try {
    const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsInboundOrder/approve/${id}`)
    if (res.status) {
      ElMessage.success(res.message || t('wmsOps.approveSuccess'))
      await load()
    } else ElMessage.error(res.message || t('wmsOps.failed'))
  } finally {
    busyId.value = null
  }
}

async function receive(id: number) {
  const res = await http.post<{ status: boolean; message?: string }>(`/api/WmsInboundOrder/receive/${id}`, {})
  if (res.status) {
    ElMessage.success(res.message || t('wmsOps.inbound.receiveSuccess'))
    await load()
  } else ElMessage.error(res.message || t('wmsOps.failed'))
}

function openPallet(row: OrderRow) {
  palletOrderId.value = row.id
  showPallet.value = true
}

async function buildPallet() {
  palletBusy.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>(
      `/api/WmsInboundOrder/buildPallet/${palletOrderId.value}`,
      { ...palletForm },
    )
    if (res.status) {
      ElMessage.success(res.message || t('wmsOps.inbound.palletSuccess'))
      showPallet.value = false
      await load()
      const cur = rows.value.find((r) => r.id === palletOrderId.value)
      if (cur) await onSelect(cur)
    } else ElMessage.error(res.message || t('wmsOps.failed'))
  } finally {
    palletBusy.value = false
  }
}

onMounted(load)
</script>
