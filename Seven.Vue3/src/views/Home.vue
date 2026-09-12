<template>
  <div class="seven-page home-dashboard">
    <header class="home-toolbar">
      <div class="home-toolbar__text">
        <h1 class="home-toolbar__title">{{ t('home.title') }}</h1>
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
    </header>

    <div class="seven-kpi-grid home-kpi-grid">
      <div v-for="kpi in kpiCards" :key="kpi.label" class="seven-kpi-card" :class="kpi.variant">
        <div class="seven-kpi-label">{{ kpi.label }}</div>
        <div class="seven-kpi-value">{{ kpi.value }}</div>
        <div class="seven-kpi-meta">{{ kpi.meta }}</div>
      </div>
    </div>

    <div class="home-layout">
      <section class="home-column home-column--primary">
        <div class="seven-panel">
          <div class="seven-panel__header home-panel__header">
            <div class="home-panel-heading">
              <h2 class="seven-panel__title">{{ t('home.inventoryTitle') }}</h2>
              <p class="panel-meta">{{ t('home.inventoryMeta') }}</p>
            </div>
          </div>
          <div class="seven-panel__body seven-panel__body--stack">
            <div class="inv-summary">
              <div v-for="item in inventorySummary" :key="item.label" class="inv-stat">
                <div class="inv-stat__value">{{ item.value }}</div>
                <div class="inv-stat__label">{{ item.label }}</div>
              </div>
            </div>
            <div class="inventory-table">
              <el-table :data="inventoryRows" size="small" border stripe max-height="220">
                <el-table-column prop="zone" :label="t('home.invColZone')" min-width="100" />
                <el-table-column prop="sku" :label="t('home.invColSku')" min-width="120" />
                <el-table-column prop="qty" :label="t('home.invColQty')" width="90" align="right" />
                <el-table-column prop="uom" :label="t('home.invColUom')" width="70" />
              </el-table>
            </div>
          </div>
        </div>

        <div class="seven-panel">
          <div class="seven-panel__header home-panel__header">
            <div class="home-panel-heading">
              <h2 class="seven-panel__title">{{ t('home.occupancyTitle') }}</h2>
              <p class="panel-meta">
                {{ occupancyPercent }}% ·
                {{ occupancyTotals.used.toLocaleString() }}/{{ occupancyTotals.total.toLocaleString() }}
              </p>
            </div>
          </div>
          <div class="seven-panel__body seven-panel__body--stack">
            <div ref="occupancyChartRef" class="occupancy-chart" />
            <div class="occupancy-breakdown">
              <div class="occupancy-stat occupancy-stat--used">
                <span class="occupancy-stat__label">{{ t('home.occupancyUsed') }}</span>
                <strong class="occupancy-stat__value">{{ occupancyTotals.used.toLocaleString() }}</strong>
              </div>
              <div class="occupancy-stat occupancy-stat--free">
                <span class="occupancy-stat__label">{{ t('home.occupancyFree') }}</span>
                <strong class="occupancy-stat__value">{{ occupancyTotals.free.toLocaleString() }}</strong>
              </div>
            </div>
          </div>
        </div>
      </section>

      <aside class="home-column home-column--secondary">
        <div v-if="featureStore.flags.alarm" class="seven-panel">
          <div class="seven-panel__header seven-panel__header--action">
            <div class="home-panel-heading">
              <h2 class="seven-panel__title">{{ t('home.alarmTitle') }}</h2>
              <p v-if="alarmFreshness" class="panel-meta">{{ alarmFreshness }}</p>
            </div>
            <el-button link type="primary" @click="router.push('/Sys_Alarm')">{{ t('home.viewAll') }}</el-button>
          </div>
          <div class="seven-panel__body seven-panel__body--list">
            <div class="panel-list-shell alarm-list" v-loading="alarmLoading">
              <div v-if="!alarmLoading && !alarmRows.length" class="empty-hint panel-list-shell__state">
                {{ t('home.alarmEmpty') }}
              </div>
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
        </div>

        <div class="seven-panel">
          <div class="seven-panel__header seven-panel__header--action">
            <div class="home-panel-heading">
              <h2 class="seven-panel__title">{{ t('home.transferTitle') }}</h2>
              <p v-if="transferFreshness" class="panel-meta">{{ transferFreshness }}</p>
            </div>
            <el-button link type="primary" @click="router.push('/Business/TransferOrder')">{{ t('home.viewAll') }}</el-button>
          </div>
          <div class="seven-panel__body seven-panel__body--list">
            <div class="panel-list-shell device-list" v-loading="transferLoading">
              <div v-if="!transferLoading && !transferRows.length" class="empty-hint panel-list-shell__state">
                {{ t('home.transferEmpty') }}
              </div>
              <div v-for="row in transferRows" :key="row.id" class="device-row">
                <div class="device-info">
                  <div class="device-row__main">
                    <span class="device-name">{{ row.orderNo }}</span>
                    <el-tag :type="transferStatusType(row.status)" size="small" effect="plain">
                      {{ transferStatusLabel(row.status) }}
                    </el-tag>
                  </div>
                  <div class="device-row__meta">
                    <span class="device-code" :title="row.remark || '-'">{{ row.remark || '-' }}</span>
                    <span v-if="row.createDate" class="device-time">{{ formatTime(row.createDate) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="featureStore.signalREnabled" class="seven-panel notify-panel">
          <div class="seven-panel__header home-panel__header">
            <div class="home-panel-heading">
              <h2 class="seven-panel__title">{{ t('home.notifyTitle') }}</h2>
            </div>
          </div>
          <div class="seven-panel__body seven-panel__body--stack">
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
      </aside>
    </div>
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
let themeObserver: MutationObserver | null = null
let occupancyResizeObserver: ResizeObserver | null = null

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
  createDate?: string
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

const occupancyTotals = computed(() => {
  const used = occupancyZones.reduce((s, z) => s + z.used, 0)
  const total = occupancyZones.reduce((s, z) => s + z.total, 0)
  return {
    used,
    total,
    free: total - used,
  }
})

const occupancyPercent = computed(() => {
  const { used, total } = occupancyTotals.value
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

const latestAlarmTimestamp = computed(() =>
  pickLatestTimestamp(alarmRows.value.map((row) => row.createDate)),
)

const latestTransferTimestamp = computed(() =>
  pickLatestTimestamp(transferRows.value.map((row) => row.createDate)),
)

const alarmFreshness = computed(() =>
  formatTimestampMeta(t('sysAlarm.colTime'), latestAlarmTimestamp.value),
)

const transferFreshness = computed(() =>
  formatTimestampMeta(t('generated.TransferOrder.createDate'), latestTransferTimestamp.value),
)

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

function formatTimestampMeta(label: string, value?: string) {
  if (!value) return ''
  return `${label}: ${formatTime(value)}`
}

function pickLatestTimestamp(values: Array<string | undefined>) {
  let latestValue: string | undefined
  let latestTime = Number.NEGATIVE_INFINITY
  for (const value of values) {
    if (!value) continue
    const parsed = Date.parse(value)
    if (Number.isNaN(parsed)) continue
    if (parsed > latestTime) {
      latestTime = parsed
      latestValue = value
    }
  }
  return latestValue
}

function readCssVar(name: string) {
  if (typeof window === 'undefined') return ''
  const value = window.getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value
}

function prefersReducedMotion() {
  return (
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  )
}

function renderOccupancyChart() {
  if (!occupancyChartRef.value) return
  if (!occupancyChart) {
    occupancyChart = echarts.init(occupancyChartRef.value)
  }
  const { used, free } = occupancyTotals.value
  const palette = {
    used: readCssVar('--seven-accent'),
    free: readCssVar('--seven-info'),
    text: readCssVar('--seven-text-muted'),
    title: readCssVar('--seven-primary-dark'),
    surface: readCssVar('--seven-bg-panel'),
  }
  occupancyChart.setOption({
    animation: !prefersReducedMotion(),
    color: [palette.used, palette.free],
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0, textStyle: { color: palette.text } },
    series: [
      {
        type: 'pie',
        radius: ['42%', '68%'],
        center: ['50%', '46%'],
        avoidLabelOverlap: true,
        itemStyle: { borderRadius: 4, borderColor: palette.surface, borderWidth: 2 },
        label: { formatter: '{b}\n{d}%', color: palette.title },
        labelLine: { lineStyle: { color: palette.text } },
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
        createDate: r.createDate != null ? String(r.createDate) : undefined,
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
  if (occupancyChartRef.value && typeof ResizeObserver !== 'undefined') {
    occupancyResizeObserver = new ResizeObserver(() => {
      occupancyChart?.resize()
    })
    occupancyResizeObserver.observe(occupancyChartRef.value)
  }
  if (typeof MutationObserver !== 'undefined') {
    themeObserver = new MutationObserver(() => {
      renderOccupancyChart()
    })
    themeObserver.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class', 'style'],
    })
  }
  window.addEventListener('resize', onResize)
})

watch(locale, () => {
  renderOccupancyChart()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  themeObserver?.disconnect()
  themeObserver = null
  occupancyResizeObserver?.disconnect()
  occupancyResizeObserver = null
  occupancyChart?.dispose()
  occupancyChart = null
})
</script>

<style scoped>
.home-dashboard {
  max-width: 1400px;
  padding-bottom: 24px;
  min-width: 0;
  overflow-x: hidden;
}

.home-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--seven-border-light);
}

.home-toolbar__text,
.home-column,
.inventory-table,
.device-info {
  min-width: 0;
}

.home-toolbar__title {
  margin: 0;
  font-size: clamp(18px, 1.8vw, 22px);
  font-weight: 700;
  color: var(--seven-primary-dark);
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

.home-toolbar__docs .el-button {
  min-height: 34px;
}

.home-kpi-grid {
  margin-bottom: 16px;
}

.home-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(320px, 1fr);
  gap: 16px;
  align-items: start;
}

.home-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.home-panel__header,
.seven-panel__header--action {
  align-items: flex-start;
  gap: 12px;
}

.home-panel-heading {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.seven-panel__body--stack {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.seven-panel__body--list {
  padding: 0;
}

.panel-meta {
  margin: 0;
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
  line-height: 1.4;
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
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.inv-stat:hover {
  border-color: var(--seven-accent);
  background: var(--seven-accent-soft);
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
  height: 260px;
}

.occupancy-breakdown {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.occupancy-stat {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 12px 14px;
  background: var(--seven-bg-subtle);
  border: 1px solid var(--seven-border-light);
  border-left-width: 3px;
  border-radius: var(--seven-radius);
}

.occupancy-stat--used {
  border-left-color: var(--seven-accent);
}

.occupancy-stat--free {
  border-left-color: var(--seven-info);
}

.occupancy-stat__label {
  font-size: 12px;
  color: var(--seven-text-muted);
}

.occupancy-stat__value {
  font-family: var(--seven-font-mono);
  font-size: 18px;
  font-weight: 700;
  color: var(--seven-primary-dark);
}

.inventory-table {
  overflow-x: auto;
  overscroll-behavior-x: contain;
}

.panel-list-shell {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 188px;
  max-height: 364px;
  padding: 12px;
  overflow: auto;
  overscroll-behavior: contain;
}

.alarm-list,
.device-list {
  min-width: 0;
}

.panel-list-shell__state {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 1;
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
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.alarm-row:hover,
.device-row:hover {
  border-color: var(--seven-accent);
  background: var(--seven-accent-soft);
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
  gap: 8px 12px;
  flex-wrap: wrap;
  margin-top: 6px;
  font-size: 12px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.device-row {
  display: flex;
  align-items: flex-start;
  padding: var(--seven-space-2) var(--seven-space-3);
  background: var(--seven-bg-subtle);
  border-radius: 4px;
  border: 1px solid var(--seven-border-light);
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.device-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
}

.device-row__main {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.device-name {
  min-width: 0;
  font-size: 13px;
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.device-row__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 12px;
  font-family: var(--seven-font-mono);
  font-size: 12px;
  color: var(--seven-text-muted);
}

.device-code {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.device-time {
  flex-shrink: 0;
}

.notify-panel :deep(.el-form-item) {
  margin-bottom: 14px;
}

@media (prefers-reduced-motion: reduce) {
  .seven-kpi-card,
  .seven-panel,
  .inv-stat,
  .occupancy-stat,
  .alarm-row,
  .device-row {
    transition: none;
  }
}

@media (max-width: 1200px) {
  .home-layout {
    grid-template-columns: minmax(0, 1fr);
  }
}

@media (max-width: 720px) {
  .inv-summary,
  .occupancy-breakdown {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .home-dashboard {
    padding-bottom: 16px;
  }

  .home-toolbar {
    align-items: stretch;
    flex-direction: column;
    gap: 12px;
    margin-bottom: 14px;
  }

  .home-toolbar__docs .el-button {
    width: 100%;
  }

  .seven-panel__header--action {
    flex-direction: column;
  }

  .seven-panel__header--action .el-button {
    padding-left: 0;
  }

  .seven-panel__body {
    padding: 12px;
  }

  .seven-panel__body--list {
    padding: 0;
  }

  .panel-list-shell {
    min-height: 160px;
    max-height: none;
    padding: 12px;
  }

  .alarm-row__main {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .alarm-msg {
    flex-basis: 100%;
    white-space: normal;
    word-break: break-word;
  }

  .alarm-row__meta {
    gap: 4px 10px;
  }

  .device-row__main {
    flex-direction: column;
  }

  .device-code {
    white-space: normal;
    word-break: break-word;
  }

  .occupancy-chart {
    height: 220px;
  }
}
</style>
