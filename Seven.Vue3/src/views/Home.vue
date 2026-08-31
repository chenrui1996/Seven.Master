<template>
  <div class="seven-page home-dashboard">
    <div class="home-toolbar">
      <div class="home-toolbar__text">
        <h2 class="home-toolbar__title">{{ t('home.title') }}</h2>
        <p class="home-toolbar__subtitle">{{ t('home.subtitle') }}</p>
      </div>
      <a
        class="home-toolbar__docs"
        :href="docsUrl"
        target="_blank"
        rel="noopener noreferrer"
      >
        <el-button type="primary" plain :icon="Document">
          {{ t('home.devDocs') }}
        </el-button>
      </a>
    </div>

    <div class="seven-kpi-grid">
      <div v-for="kpi in kpiCards" :key="kpi.label" class="seven-kpi-card" :class="kpi.variant">
        <div class="seven-kpi-label">{{ kpi.label }}</div>
        <div class="seven-kpi-value">{{ kpi.value }}</div>
        <div class="seven-kpi-meta">{{ kpi.meta }}</div>
      </div>
    </div>

    <el-row :gutter="16">
      <el-col :xs="24" :lg="14">
        <div class="seven-panel">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.inventoryTitle') }}</h3>
            <span class="panel-meta">{{ t('home.inventoryMeta') }}</span>
          </div>
          <div class="seven-panel__body">
            <div class="inv-summary">
              <div v-for="item in inventorySummary" :key="item.label" class="inv-stat">
                <div class="inv-stat__value">{{ item.value }}</div>
                <div class="inv-stat__label">{{ item.label }}</div>
              </div>
            </div>
            <el-table :data="inventoryRows" size="small" border stripe max-height="220">
              <el-table-column prop="zone" :label="t('home.invColZone')" min-width="100" />
              <el-table-column prop="sku" :label="t('home.invColSku')" min-width="120" />
              <el-table-column prop="qty" :label="t('home.invColQty')" width="90" align="right" />
              <el-table-column prop="uom" :label="t('home.invColUom')" width="70" />
            </el-table>
          </div>
        </div>

        <div class="seven-panel" style="margin-top: 16px">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.occupancyTitle') }}</h3>
            <span class="panel-meta">{{ occupancyPercent }}%</span>
          </div>
          <div class="seven-panel__body">
            <div ref="occupancyChartRef" class="occupancy-chart" />
          </div>
        </div>
      </el-col>

      <el-col :xs="24" :lg="10">
        <div v-if="featureStore.flags.alarm" class="seven-panel">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.alarmTitle') }}</h3>
            <el-button link type="primary" @click="router.push('/Sys_Alarm')">{{ t('home.viewAll') }}</el-button>
          </div>
          <div class="seven-panel__body alarm-list" v-loading="alarmLoading">
            <div v-if="!alarmRows.length" class="empty-hint">{{ t('home.alarmEmpty') }}</div>
            <div v-for="row in alarmRows" :key="row.alarmId" class="alarm-row">
              <div class="alarm-row__main">
                <el-tag :type="levelTag(row.level)" size="small" effect="plain">L{{ row.level }}</el-tag>
                <span class="alarm-code">{{ row.code }}</span>
                <span class="alarm-msg" :title="row.message">{{ row.message }}</span>
              </div>
              <div class="alarm-row__meta">
                <span>{{ row.deviceName || '-' }}</span>
                <span>{{ formatTime(row.createDate) }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="seven-panel" style="margin-top: 16px">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.transferTitle') }}</h3>
            <el-button link type="primary" @click="router.push('/Business/TransferOrder')">{{ t('home.viewAll') }}</el-button>
          </div>
          <div class="seven-panel__body device-list" v-loading="transferLoading">
            <div v-if="!transferRows.length" class="empty-hint">{{ t('home.transferEmpty') }}</div>
            <div v-for="row in transferRows" :key="row.id" class="device-row">
              <div class="device-info">
                <span class="device-name">{{ row.orderNo }}</span>
                <span class="device-code">{{ row.remark || '-' }}</span>
              </div>
              <el-tag :type="transferStatusType(row.status)" size="small" effect="plain">
                {{ transferStatusLabel(row.status) }}
              </el-tag>
            </div>
          </div>
        </div>

        <div v-if="featureStore.signalREnabled" class="seven-panel notify-panel" style="margin-top: 16px">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.notifyTitle') }}</h3>
          </div>
          <div class="seven-panel__body">
            <el-form label-position="top" size="small" @submit.prevent="sendBroadcast">
              <el-form-item :label="t('home.notifyTitleLabel')">
                <el-input v-model="notifyTitle" maxlength="80" />
              </el-form-item>
              <el-form-item :label="t('home.notifyContentLabel')">
                <el-input v-model="notifyContent" type="textarea" :rows="2" maxlength="500" />
              </el-form-item>
              <el-button type="primary" size="small" :loading="notifySending" @click="sendBroadcast">
                {{ t('home.notifySend') }}
              </el-button>
            </el-form>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
defineOptions({ name: 'Home' })

import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Document } from '@element-plus/icons-vue'
import * as echarts from 'echarts/core'
import { PieChart } from 'echarts/charts'
import { LegendComponent, TooltipComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import type { ECharts } from 'echarts/core'
import { getPageData, default as http } from '../api/http'
import { ElMessage } from 'element-plus'
import { useAlarmStore } from '../stores/alarm'
import { useFeatureStore } from '../stores/features'

echarts.use([PieChart, LegendComponent, TooltipComponent, CanvasRenderer])

const { t, locale } = useI18n()
const router = useRouter()
const alarmStore = useAlarmStore()
const featureStore = useFeatureStore()

/** GitHub 文档入口：快速开始 */
const docsUrl =
  'https://github.com/chenrui1996/Seven.Master/blob/main/doc/01-%E5%BF%AB%E9%80%9F%E5%BC%80%E5%A7%8B.md'

const occupancyChartRef = ref<HTMLElement | null>(null)
let occupancyChart: ECharts | null = null

const alarmLoading = ref(false)
const transferLoading = ref(false)

interface AlarmRow {
  alarmId: number
  code: string
  message: string
  level: number
  deviceName?: string
  createDate?: string
}

interface TransferRow {
  id: number
  orderNo: string
  status: number
  remark?: string
}

const alarmRows = ref<AlarmRow[]>([])
const transferRows = ref<TransferRow[]>([])
const notifyTitle = ref('')
const notifyContent = ref('')
const notifySending = ref(false)

async function sendBroadcast() {
  const title = notifyTitle.value.trim()
  const content = notifyContent.value.trim()
  if (!title || !content) {
    ElMessage.warning(t('home.notifyRequired'))
    return
  }
  notifySending.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>('/api/Notify/broadcast', {
      title,
      content,
    })
    if (res.status) {
      ElMessage.success(res.message || t('home.notifySent'))
      notifyContent.value = ''
    }
  } finally {
    notifySending.value = false
  }
}

/** 库存 Demo 数据（尚无独立库存 API） */
const inventoryRows = computed(() => [
  { zone: 'A-01', sku: 'MAT-1001', qty: 320, uom: t('home.uomBox') },
  { zone: 'A-02', sku: 'MAT-2048', qty: 86, uom: t('home.uomPallet') },
  { zone: 'B-03', sku: 'MAT-3102', qty: 1540, uom: t('home.uomPcs') },
  { zone: 'C-01', sku: 'MAT-4410', qty: 42, uom: t('home.uomPallet') },
])

const occupancyZones = [
  { key: 'zoneA', used: 720, total: 1000 },
  { key: 'zoneB', used: 410, total: 800 },
  { key: 'zoneC', used: 190, total: 600 },
  { key: 'zoneD', used: 55, total: 400 },
]

const occupancyPercent = computed(() => {
  const used = occupancyZones.reduce((s, z) => s + z.used, 0)
  const total = occupancyZones.reduce((s, z) => s + z.total, 0)
  return total ? Math.round((used / total) * 100) : 0
})

const inventorySummary = computed(() => {
  const rows = inventoryRows.value
  const skuCount = new Set(rows.map((r) => r.sku)).size
  const qtySum = rows.reduce((s, r) => s + r.qty, 0)
  return [
    { label: t('home.invSkuCount'), value: String(skuCount) },
    { label: t('home.invQtyTotal'), value: qtySum.toLocaleString() },
    { label: t('home.invZoneCount'), value: String(new Set(rows.map((r) => r.zone)).size) },
  ]
})

const openTransferCount = computed(
  () => transferRows.value.filter((d) => d.status === 0 || d.status === 1).length,
)

const kpiCards = computed(() => [
  {
    label: t('home.kpiInventory'),
    value: inventorySummary.value[1]?.value ?? '0',
    meta: t('home.kpiInventoryMeta'),
    variant: 'seven-kpi-card--info',
  },
  {
    label: t('home.kpiOccupancy'),
    value: `${occupancyPercent.value}%`,
    meta: t('home.kpiOccupancyMeta'),
    variant: 'seven-kpi-card--warning',
  },
  {
    label: t('home.kpiOpenTransfers'),
    value: String(openTransferCount.value),
    meta: t('home.kpiOpenTransfersMeta'),
    variant: 'seven-kpi-card--success',
  },
  {
    label: t('home.kpiAlerts'),
    value: String(alarmStore.activeCount),
    meta: t('home.kpiAlertsMeta'),
    variant: 'seven-kpi-card--danger',
  },
])

function levelTag(level: number) {
  if (level >= 4) return 'danger'
  if (level >= 3) return 'warning'
  if (level >= 2) return 'info'
  return 'success'
}

function transferStatusType(status: number) {
  if (status === 3) return 'success'
  if (status === 1) return 'warning'
  if (status === 4) return 'info'
  return 'primary'
}

function transferStatusLabel(status: number) {
  return t(`generated.TransferOrder.enum_status_${status}`)
}

function formatTime(v?: string) {
  if (!v) return '-'
  return String(v).replace('T', ' ').slice(0, 19)
}

function renderOccupancyChart() {
  if (!occupancyChartRef.value) return
  if (!occupancyChart) {
    occupancyChart = echarts.init(occupancyChartRef.value)
  }
  const used = occupancyZones.reduce((s, z) => s + z.used, 0)
  const free = occupancyZones.reduce((s, z) => s + (z.total - z.used), 0)
  occupancyChart.setOption({
    color: ['#f97316', '#94a3b8'],
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0, textStyle: { color: '#64748b' } },
    series: [
      {
        type: 'pie',
        radius: ['42%', '68%'],
        center: ['50%', '46%'],
        avoidLabelOverlap: true,
        itemStyle: { borderRadius: 4, borderColor: '#fff', borderWidth: 2 },
        label: { formatter: '{b}\n{d}%' },
        data: [
          { name: t('home.occupancyUsed'), value: used },
          { name: t('home.occupancyFree'), value: free },
        ],
      },
    ],
  })
}

async function loadAlarms() {
  alarmLoading.value = true
  try {
    const wheres = JSON.stringify([{ name: 'status', value: '0', displayType: 'equal' }])
    const res = await getPageData('/api/Sys_Alarm/getPageData', { page: 1, rows: 8, wheres })
    if (res.status && res.data) {
      const data = res.data as { rows: Record<string, unknown>[] }
      alarmRows.value = (data.rows ?? []).map((r) => ({
        alarmId: Number(r.alarm_Id ?? r.alarmId ?? 0),
        code: String(r.code ?? ''),
        message: String(r.message ?? ''),
        level: Number(r.level ?? 0),
        deviceName: r.deviceName != null ? String(r.deviceName) : undefined,
        createDate: r.createDate != null ? String(r.createDate) : undefined,
      }))
    }
  } finally {
    alarmLoading.value = false
  }
}

async function loadTransfers() {
  transferLoading.value = true
  try {
    const res = await getPageData('/api/TransferOrder/getPageData', { page: 1, rows: 12 })
    if (res.status && res.data) {
      const data = res.data as { rows: Record<string, unknown>[] }
      transferRows.value = (data.rows ?? []).map((r) => ({
        id: Number(r.id ?? 0),
        orderNo: String(r.orderNo ?? ''),
        status: Number(r.status ?? 0),
        remark: r.remark != null ? String(r.remark) : undefined,
      }))
    }
  } finally {
    transferLoading.value = false
  }
}

function onResize() {
  occupancyChart?.resize()
}

onMounted(async () => {
  const tasks: Promise<unknown>[] = [loadTransfers()]
  if (featureStore.flags.alarm) {
    tasks.push(alarmStore.fetchActiveCount(), loadAlarms())
  }
  await Promise.all(tasks)
  await nextTick()
  renderOccupancyChart()
  window.addEventListener('resize', onResize)
})

watch(locale, () => {
  renderOccupancyChart()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  occupancyChart?.dispose()
  occupancyChart = null
})
</script>

<style scoped>
.home-dashboard {
  max-width: 1400px;
}

.home-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.home-toolbar__title {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: var(--seven-text);
  line-height: 1.3;
}

.home-toolbar__subtitle {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--seven-text-muted);
}

.home-toolbar__docs {
  flex-shrink: 0;
  text-decoration: none;
}

.panel-meta {
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.inv-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 14px;
}

.inv-stat {
  padding: 12px 14px;
  background: var(--seven-bg-subtle);
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
}

.inv-stat__value {
  font-size: 22px;
  font-weight: 700;
  font-family: var(--seven-font-mono);
  color: var(--seven-primary-dark);
  line-height: 1.2;
}

.inv-stat__label {
  margin-top: 4px;
  font-size: 12px;
  color: var(--seven-text-muted);
}

.occupancy-chart {
  width: 100%;
  height: 280px;
}

.alarm-list,
.device-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 120px;
}

.empty-hint {
  padding: 24px 8px;
  text-align: center;
  color: var(--seven-text-muted);
  font-size: 13px;
}

.alarm-row {
  padding: 10px 12px;
  background: var(--seven-bg-subtle);
  border: 1px solid var(--seven-border-light);
  border-radius: 4px;
}

.alarm-row__main {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.alarm-code {
  font-family: var(--seven-font-mono);
  font-size: 12px;
  font-weight: 600;
  color: var(--seven-primary-dark);
  flex-shrink: 0;
}

.alarm-msg {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  color: var(--seven-primary-dark);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.alarm-row__meta {
  display: flex;
  justify-content: space-between;
  margin-top: 6px;
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.device-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: var(--seven-space-2) var(--seven-space-3);
  background: var(--seven-bg-subtle);
  border-radius: 4px;
  border: 1px solid var(--seven-border-light);
}

.device-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.device-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.device-code {
  font-family: var(--seven-font-mono);
  font-size: 12px;
  color: var(--seven-text-muted);
}

@media (max-width: 640px) {
  .inv-summary {
    grid-template-columns: 1fr;
  }
}
</style>
