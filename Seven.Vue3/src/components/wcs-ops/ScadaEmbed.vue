<template>
  <div class="scada-embed" data-testid="scada-embed">
    <div class="toolbar">
      <div class="field">
        <label>{{ t('scada.selectView') }}</label>
        <el-select
          v-model="selectedViewId"
          :placeholder="t('scada.selectView')"
          filterable
          class="view-select"
          @change="onViewChange"
        >
          <el-option v-for="v in filteredViews" :key="v.id" :label="v.name" :value="v.id" />
        </el-select>
      </div>
      <el-button-group>
        <el-button size="small" :aria-label="t('scada.zoomOut')" @click="zoomBy(1 / 1.2)">−</el-button>
        <el-button size="small" disabled class="zoom-label">{{ Math.round(scale * 100) }}%</el-button>
        <el-button size="small" :aria-label="t('scada.zoomIn')" @click="zoomBy(1.2)">+</el-button>
      </el-button-group>
      <el-button size="small" type="primary" plain @click="fitToView">{{ t('scada.fit') }}</el-button>
      <el-button size="small" @click="loadStatus">{{ t('scada.refreshMap') }}</el-button>
      <div class="meta" :title="t('scada.panHint')">
        <span class="pulse" :class="pulseClass" aria-hidden="true" />
        <span>{{ occupiedCount }}/{{ nodeCount }} {{ t('scada.legendOccupied') }}</span>
        <span class="sep">·</span>
        <span class="hint">{{ t('scada.panHint') }}</span>
      </div>
    </div>

    <div
      class="legend"
      role="list"
      :aria-label="t('scada.legendTitle')"
    >
      <span class="leg" role="listitem">
        <span class="swatch swatch-bin" aria-hidden="true">{{ t('scada.kindBin') }}</span>
        {{ t('scada.legendEmpty') }}
      </span>
      <span class="leg" role="listitem">
        <span class="swatch swatch-occupy" aria-hidden="true">{{ t('scada.kindOccupy') }}</span>
        {{ t('scada.legendOccupied') }}
      </span>
    </div>

    <div
      ref="viewportEl"
      class="viewport"
      :class="{ grabbing: dragging }"
      @wheel.prevent="onWheel"
      @pointerdown="onPointerDown"
      @pointermove="onPointerMove"
      @pointerup="onPointerUp"
      @pointercancel="onPointerUp"
      @pointerleave="onPointerUp"
    >
      <div v-if="status" class="world" :style="worldStyle">
        <div
          v-for="node in status.nodes"
          :key="node.bindId"
          class="node is-bin"
          :class="{
            'is-occupy': node.isOccupied,
            compact: scale < 0.55,
          }"
          :style="{ left: node.x + 'px', top: node.y + 'px', width: nodeSize + 'px', height: nodeSize + 'px' }"
          :title="nodeTitle(node)"
        >
          <span v-if="scale >= 0.55" class="node-kind" aria-hidden="true">
            {{ node.isOccupied ? t('scada.kindOccupy') : t('scada.kindBin') }}
          </span>
          <span v-if="scale >= 0.7" class="node-code">{{ shortLabel(node) }}</span>
        </div>
      </div>
      <el-empty v-else class="empty" :description="t('scada.emptyEmbed')" :image-size="56" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../api/http'

const props = defineProps<{ packHint?: string }>()
const { t } = useI18n()

interface ScdViewRow {
  id: number
  code: string
  name: string
  width: number
  height: number
}
interface ScdViewStatus {
  viewId: number
  width: number
  height: number
  nodes: { bindId: number; locationCode: string; x: number; y: number; label?: string; isOccupied: boolean }[]
}

const views = ref<ScdViewRow[]>([])
const selectedViewId = ref<number | null>(null)
const status = ref<ScdViewStatus | null>(null)
const viewportEl = ref<HTMLElement | null>(null)
const scale = ref(1)
const offsetX = ref(0)
const offsetY = ref(0)
const dragging = ref(false)
const lastPtr = ref({ x: 0, y: 0 })
const lastRefreshOk = ref(true)
let poll: ReturnType<typeof setInterval> | null = null
let reduceMotion = false

const filteredViews = computed(() => {
  const hint = (props.packHint || '').toLowerCase()
  if (!hint) return views.value
  const matched = views.value.filter(
    (v) =>
      v.code?.toLowerCase().includes(hint) ||
      v.name?.toLowerCase().includes(hint) ||
      (hint === 'fourway' && /fw|four|四向/i.test(`${v.code} ${v.name}`)) ||
      (hint === 'stacker' && /stk|stack|立库|堆垛/i.test(`${v.code} ${v.name}`)),
  )
  return matched.length ? matched : views.value
})

const nodeCount = computed(() => status.value?.nodes?.length ?? 0)
const occupiedCount = computed(() => status.value?.nodes?.filter((n) => n.isOccupied).length ?? 0)
const pulseClass = computed(() => {
  if (!status.value) return 'is-off'
  return lastRefreshOk.value ? 'is-ok' : 'is-stale'
})

/** Approximate cell size from median nearest-neighbor so dense maps stay readable. */
const nodeSize = computed(() => {
  const nodes = status.value?.nodes
  if (!nodes?.length) return 18
  const sample = nodes.slice(0, Math.min(nodes.length, 80))
  const dists: number[] = []
  for (let i = 0; i < sample.length; i++) {
    let best = Infinity
    for (let j = 0; j < sample.length; j++) {
      if (i === j) continue
      const dx = sample[i].x - sample[j].x
      const dy = sample[i].y - sample[j].y
      const d = Math.hypot(dx, dy)
      if (d > 0.5 && d < best) best = d
    }
    if (Number.isFinite(best) && best < Infinity) dists.push(best)
  }
  if (!dists.length) return 18
  dists.sort((a, b) => a - b)
  const median = dists[Math.floor(dists.length / 2)]
  return Math.round(Math.min(36, Math.max(12, median * 0.72)))
})

const worldStyle = computed(() => ({
  width: `${status.value?.width || 0}px`,
  height: `${status.value?.height || 0}px`,
  transform: `translate(${offsetX.value}px, ${offsetY.value}px) scale(${scale.value})`,
  transformOrigin: '0 0',
}))

function shortLabel(node: ScdViewStatus['nodes'][0]) {
  const raw = node.label || node.locationCode || ''
  if (raw.length <= 10) return raw
  return raw.slice(-8)
}

function nodeTitle(node: ScdViewStatus['nodes'][0]) {
  const state = node.isOccupied ? t('scada.legendOccupied') : t('scada.legendEmpty')
  return `${node.locationCode || node.label || ''} · ${state}`
}

function clampScale(v: number) {
  return Math.min(4, Math.max(0.08, v))
}

function zoomBy(factor: number, cx?: number, cy?: number) {
  if (reduceMotion) {
    scale.value = clampScale(scale.value * factor)
    return
  }
  const el = viewportEl.value
  if (!el) {
    scale.value = clampScale(scale.value * factor)
    return
  }
  const rect = el.getBoundingClientRect()
  const pivotX = (cx ?? rect.width / 2) - offsetX.value
  const pivotY = (cy ?? rect.height / 2) - offsetY.value
  const next = clampScale(scale.value * factor)
  const k = next / scale.value
  offsetX.value = (cx ?? rect.width / 2) - pivotX * k
  offsetY.value = (cy ?? rect.height / 2) - pivotY * k
  scale.value = next
}

function fitToView() {
  const el = viewportEl.value
  const st = status.value
  if (!el || !st || st.width <= 0 || st.height <= 0) return
  const pad = 24
  const vw = Math.max(el.clientWidth - pad * 2, 40)
  const vh = Math.max(el.clientHeight - pad * 2, 40)
  const s = clampScale(Math.min(vw / st.width, vh / st.height))
  scale.value = s
  offsetX.value = pad + (vw - st.width * s) / 2
  offsetY.value = pad + (vh - st.height * s) / 2
}

function onWheel(e: WheelEvent) {
  const el = viewportEl.value
  if (!el) return
  const rect = el.getBoundingClientRect()
  const factor = e.deltaY > 0 ? 1 / 1.12 : 1.12
  zoomBy(factor, e.clientX - rect.left, e.clientY - rect.top)
}

function onPointerDown(e: PointerEvent) {
  if (e.button !== 0) return
  dragging.value = true
  lastPtr.value = { x: e.clientX, y: e.clientY }
  ;(e.currentTarget as HTMLElement).setPointerCapture?.(e.pointerId)
}

function onPointerMove(e: PointerEvent) {
  if (!dragging.value) return
  offsetX.value += e.clientX - lastPtr.value.x
  offsetY.value += e.clientY - lastPtr.value.y
  lastPtr.value = { x: e.clientX, y: e.clientY }
}

function onPointerUp(e: PointerEvent) {
  dragging.value = false
  try {
    ;(e.currentTarget as HTMLElement).releasePointerCapture?.(e.pointerId)
  } catch {
    /* ignore */
  }
}

async function onViewChange() {
  await loadStatus()
  await nextTick()
  fitToView()
}

async function loadViews() {
  const res = await http.post<{ status: boolean; data?: { rows?: ScdViewRow[] } }>('/api/ScdView/getPageData', {
    page: 1,
    rows: 100,
  })
  if (res.status) {
    const rows = res.data?.rows ?? []
    views.value = [...rows].sort((a, b) => {
      const score = (v: ScdViewRow) => {
        let s = 0
        if ((v.width || 0) > 0 && (v.height || 0) > 0) s += 10
        if (/^MAP-/i.test(v.code || '')) s += 5
        const hint = (props.packHint || '').toLowerCase()
        if (hint && (v.code?.toLowerCase().includes(hint) || v.name?.toLowerCase().includes(hint))) s += 3
        if (hint === 'stacker' && /stk|stack|立库/i.test(`${v.code} ${v.name}`)) s += 4
        if (hint === 'fourway' && /fw|four|四向/i.test(`${v.code} ${v.name}`)) s += 4
        return s
      }
      return score(b) - score(a)
    })
    const list = filteredViews.value
    if (!selectedViewId.value && list.length > 0) {
      selectedViewId.value = list[0].id
      await onViewChange()
    }
  }
}

async function loadStatus() {
  if (!selectedViewId.value) return
  try {
    const res = await http.get<{ status: boolean; data?: ScdViewStatus }>(
      `/api/ScdView/${selectedViewId.value}/status`,
    )
    if (res.status) {
      status.value = res.data ?? null
      lastRefreshOk.value = true
      const st = status.value
      const empty = !st || !st.width || !st.height || !(st.nodes?.length)
      if (empty && filteredViews.value.length > 1) {
        const others = filteredViews.value.filter((v) => v.id !== selectedViewId.value && (v.width || 0) > 0)
        if (others.length) {
          selectedViewId.value = others[0].id
          const again = await http.get<{ status: boolean; data?: ScdViewStatus }>(
            `/api/ScdView/${selectedViewId.value}/status`,
          )
          if (again.status) status.value = again.data ?? null
        }
      }
    } else {
      lastRefreshOk.value = false
    }
  } catch {
    lastRefreshOk.value = false
  }
}

watch(
  () => props.packHint,
  async () => {
    const list = filteredViews.value
    if (list.length && !list.some((v) => v.id === selectedViewId.value)) {
      selectedViewId.value = list[0].id
      await onViewChange()
    }
  },
)

onMounted(() => {
  reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches ?? false
  void loadViews()
  poll = setInterval(() => void loadStatus(), 4000)
  window.addEventListener('resize', fitToView)
})
onUnmounted(() => {
  if (poll) clearInterval(poll)
  window.removeEventListener('resize', fitToView)
})
</script>

<style scoped>
.scada-embed {
  --scada-bg: #ffffff;
  --scada-panel: #fafafa;
  --scada-text: #1e293b;
  --scada-muted: #64748b;
  --scada-border: #e2e8f0;
  --scada-cta: #ea580c;
  --scada-cta-soft: #ffedd5;
  --scada-brand: #ffb900;
  --scada-ok: #15803d;
  --scada-warn: #b45309;
  --scada-bin: #dbeafe;
  --scada-bin-border: #3b82f6;
  --scada-occupy: #94a3b8;
  --scada-occupy-ink: #334155;
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 0;
  height: 100%;
  color: var(--scada-text);
  font-size: 14px;
  line-height: 1.5;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 14px;
  align-items: flex-end;
  padding-bottom: 10px;
  border-bottom: 2px solid var(--scada-brand);
}

.field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 160px;
}

.field label {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--scada-muted);
}

.view-select {
  width: 220px;
}

.toolbar :deep(.el-button) {
  cursor: pointer;
  min-height: 32px;
  transition: background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease;
}

.toolbar :deep(.el-button:focus-visible) {
  outline: 2px solid var(--scada-brand);
  outline-offset: 1px;
}

.zoom-label {
  min-width: 56px;
  cursor: default !important;
}

.meta {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 32px;
  margin-left: auto;
  font-size: 12px;
  color: var(--scada-muted);
  font-variant-numeric: tabular-nums;
}

.sep {
  opacity: 0.5;
}

.hint {
  color: var(--scada-muted);
}

.pulse {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #94a3b8;
  flex-shrink: 0;
}

.pulse.is-ok {
  background: var(--scada-ok);
  box-shadow: 0 0 0 3px rgba(21, 128, 61, 0.2);
}

.pulse.is-stale {
  background: var(--scada-warn);
}

.pulse.is-off {
  background: #94a3b8;
}

.legend {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 18px;
  align-items: center;
  padding: 8px 12px;
  border: 1px solid var(--scada-border);
  border-radius: 8px;
  background: var(--scada-panel);
  font-size: 12px;
  color: var(--scada-muted);
}

.leg {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.swatch {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border-radius: 3px;
  border: 1px solid var(--scada-border);
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
  color: #334155;
}

.swatch-bin {
  background: var(--scada-bin);
  border-color: var(--scada-bin-border);
  color: #1d4ed8;
}

.swatch-occupy {
  background: var(--scada-occupy);
  border-color: #64748b;
  color: #fff;
  box-shadow: inset 0 0 0 2px rgba(100, 116, 139, 0.45);
}

.viewport {
  position: relative;
  flex: 1 1 auto;
  min-height: 360px;
  height: clamp(360px, 52vh, 720px);
  background:
    linear-gradient(180deg, #ffffff 0%, #f8fafc 100%),
    repeating-linear-gradient(
      0deg,
      transparent,
      transparent 23px,
      rgba(226, 232, 240, 0.55) 23px,
      rgba(226, 232, 240, 0.55) 24px
    ),
    repeating-linear-gradient(
      90deg,
      transparent,
      transparent 23px,
      rgba(226, 232, 240, 0.55) 23px,
      rgba(226, 232, 240, 0.55) 24px
    );
  background-blend-mode: normal, multiply, multiply;
  border: 1px solid var(--scada-border);
  border-radius: 8px;
  overflow: hidden;
  cursor: grab;
  touch-action: none;
  content-visibility: auto;
}

.viewport.grabbing {
  cursor: grabbing;
}

.world {
  position: absolute;
  left: 0;
  top: 0;
  background: rgba(255, 255, 255, 0.72);
  border: 1px dashed #cbd5e1;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.8);
  will-change: transform;
}

.node {
  position: absolute;
  box-sizing: border-box;
  border-radius: 4px;
  border: 1px solid var(--scada-border);
  background: #f1f5f9;
  transform: translate(-50%, -50%);
  overflow: hidden;
  pointer-events: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.node.is-bin {
  background: var(--scada-bin);
  border-color: var(--scada-bin-border);
}

.node.is-occupy {
  background: color-mix(in srgb, var(--scada-bin) 55%, var(--scada-occupy));
  border-color: #64748b;
  box-shadow: inset 0 0 0 2px rgba(100, 116, 139, 0.45);
}

.node.compact {
  border-radius: 2px;
}

.node.compact .node-kind,
.node.compact .node-code {
  display: none;
}

.node-kind {
  position: absolute;
  right: 2px;
  top: 1px;
  font-size: 8px;
  font-weight: 700;
  line-height: 1;
  color: #1d4ed8;
  opacity: 0.9;
}

.node.is-occupy .node-kind {
  color: var(--scada-occupy-ink);
}

.node-code {
  position: absolute;
  left: 2px;
  right: 2px;
  bottom: 2px;
  font-size: 8px;
  line-height: 1.1;
  font-variant-numeric: tabular-nums;
  color: var(--scada-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: center;
}

.empty {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.72);
}

@media (prefers-reduced-motion: reduce) {
  .toolbar :deep(.el-button),
  .node {
    transition: none;
  }
  .world {
    will-change: auto;
  }
}

@media (max-width: 960px) {
  .viewport {
    height: clamp(280px, 46vh, 520px);
    min-height: 280px;
  }
  .hint,
  .sep {
    display: none;
  }
  .meta {
    margin-left: 0;
  }
  .view-select {
    width: 160px;
  }
}
</style>
