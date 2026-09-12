<template>
  <div class="seven-page scada-floor2d wcs-ops">
    <div class="ops-header">
      <div class="ops-heading">
        <h1 class="ops-title">{{ t('scada.title') }}</h1>
        <p class="ops-sub">{{ t('scada.subtitle', { name: selectedViewName, count: views.length }) }}</p>
      </div>
      <div class="ops-toolbar">
        <el-select v-model="selectedViewId" :placeholder="t('scada.selectView')" style="width: 220px" @change="loadStatus">
          <el-option v-for="v in views" :key="v.id" :label="v.name" :value="v.id" />
        </el-select>
        <el-button type="primary" @click="loadStatus">{{ t('scada.refresh') }}</el-button>
      </div>
    </div>

    <div v-if="status" class="ops-status-grid">
      <div class="ops-status-card ops-status-card--accent">
        <div class="ops-status-card__label">{{ t('scada.selectView') }}</div>
        <div class="ops-status-card__value">{{ selectedViewName }}</div>
        <div class="ops-status-card__meta">{{ status.width }} × {{ status.height }} px</div>
      </div>
      <div class="ops-status-card ops-status-card--info">
        <div class="ops-status-card__label">{{ t('scada.nodes') }}</div>
        <div class="ops-status-card__value">{{ nodeCount }}</div>
        <div class="ops-status-card__meta">{{ t('scada.nodesMeta') }}</div>
      </div>
      <div class="ops-status-card ops-status-card--danger">
        <div class="ops-status-card__label">{{ t('scada.occupied') }}</div>
        <div class="ops-status-card__value">{{ occupiedCount }}</div>
        <div class="ops-status-card__meta">{{ t('scada.occupiedMeta') }}</div>
      </div>
      <div class="ops-status-card ops-status-card--success">
        <div class="ops-status-card__label">{{ t('scada.available') }}</div>
        <div class="ops-status-card__value">{{ availableCount }}</div>
        <div class="ops-status-card__meta">{{ t('scada.availableMeta') }}</div>
      </div>
    </div>

    <div v-if="status" class="ops-panel scada-panel">
      <div class="ops-panel__head">
        <div>
          <h3>{{ selectedViewName }}</h3>
          <p class="ops-panel__sub">{{ t('scada.pollingMeta') }}</p>
        </div>
        <div class="scada-legend" :aria-label="t('scada.legendLabel')">
          <span class="scada-legend__item">
            <span class="scada-swatch scada-swatch--available" aria-hidden="true"></span>
            {{ t('scada.legendAvailable') }}
          </span>
          <span class="scada-legend__item">
            <span class="scada-swatch scada-swatch--occupied" aria-hidden="true"></span>
            {{ t('scada.legendOccupied') }}
          </span>
        </div>
      </div>

      <div class="scada-canvas-wrap">
        <div
          class="canvas"
          :style="{ width: status.width + 'px', height: status.height + 'px' }"
        >
          <div
            v-for="node in status.nodes"
            :key="node.bindId"
            class="node"
            :class="{ occupied: node.isOccupied }"
            :style="{ left: node.x + 'px', top: node.y + 'px' }"
            :title="node.locationCode"
          >
            {{ node.label || node.locationCode }}
          </div>
        </div>
      </div>
    </div>
    <div v-else class="ops-panel">
      <el-empty :description="t('scada.empty')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../api/http'
import '../../styles/wcs-ops.css'

const { t } = useI18n()

interface ScdViewRow {
  id: number
  code: string
  name: string
  width: number
  height: number
}

interface ScdNodeStatus {
  bindId: number
  locationCode: string
  x: number
  y: number
  label?: string
  isOccupied: boolean
}

interface ScdViewStatus {
  viewId: number
  code: string
  name: string
  width: number
  height: number
  nodes: ScdNodeStatus[]
}

const views = ref<ScdViewRow[]>([])
const selectedViewId = ref<number | null>(null)
const status = ref<ScdViewStatus | null>(null)
let pollTimer: ReturnType<typeof setInterval> | null = null

const selectedViewName = computed(
  () => status.value?.name ?? views.value.find((view) => view.id === selectedViewId.value)?.name ?? t('scada.fallbackName'),
)
const nodeCount = computed(() => status.value?.nodes.length ?? 0)
const occupiedCount = computed(() => status.value?.nodes.filter((node) => node.isOccupied).length ?? 0)
const availableCount = computed(() => Math.max(0, nodeCount.value - occupiedCount.value))

async function loadViews() {
  const res = await http.post<{ status: boolean; data?: { rows?: ScdViewRow[] } }>('/api/ScdView/getPageData', {
    page: 1,
    rows: 100,
  })
  if (res.status) {
    views.value = res.data?.rows ?? []
    if (!selectedViewId.value && views.value.length > 0) {
      selectedViewId.value = views.value[0].id
      await loadStatus()
    }
  }
}

async function loadStatus() {
  if (!selectedViewId.value) return
  const res = await http.get<{ status: boolean; data?: ScdViewStatus }>(`/api/ScdView/${selectedViewId.value}/status`)
  if (res.status) status.value = res.data ?? null
}

onMounted(async () => {
  await loadViews()
  pollTimer = setInterval(loadStatus, 5000)
})

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer)
})
</script>

<style scoped>
.scada-floor2d .canvas {
  position: relative;
  border: 1px solid var(--ops-border);
  border-radius: 4px;
  background:
    linear-gradient(180deg, color-mix(in srgb, var(--ops-surface-subtle) 82%, white), var(--ops-surface-subtle));
  overflow: hidden;
}

.scada-floor2d .scada-panel {
  padding: 14px;
}

.scada-floor2d .scada-canvas-wrap {
  width: 100%;
  max-width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  padding-bottom: 4px;
}

.scada-floor2d .scada-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 12px;
  align-items: center;
}

.scada-floor2d .scada-legend__item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: var(--ops-muted);
  font-size: 0.82rem;
}

.scada-floor2d .scada-swatch {
  width: 12px;
  height: 12px;
  border-radius: 999px;
  border: 1px solid color-mix(in srgb, var(--ops-border) 80%, transparent);
  background: var(--ops-success);
}

.scada-floor2d .scada-swatch--occupied {
  background: var(--ops-danger);
}

.scada-floor2d .node {
  position: absolute;
  min-width: 48px;
  padding: 4px 8px;
  font-size: 12px;
  text-align: center;
  border-radius: 4px;
  line-height: 1.35;
  border: 1px solid color-mix(in srgb, var(--ops-success) 56%, var(--ops-border));
  background: var(--ops-success);
  color: var(--seven-btn-on-solid);
  transform: translate(-50%, -50%);
  cursor: default;
  user-select: none;
  box-shadow: 0 8px 16px color-mix(in srgb, var(--ops-success) 18%, transparent);
}
.scada-floor2d .node.occupied {
  border-color: color-mix(in srgb, var(--ops-danger) 56%, var(--ops-border));
  background: var(--ops-danger);
  box-shadow: 0 8px 16px color-mix(in srgb, var(--ops-danger) 18%, transparent);
}

@media (max-width: 640px) {
  .scada-floor2d .node {
    min-width: 42px;
    font-size: 11px;
    padding-inline: 6px;
  }
}
</style>
