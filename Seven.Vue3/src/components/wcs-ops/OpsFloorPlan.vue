<template>
  <div class="sfp-page" :class="{ 'is-calib': calibOn }" data-testid="ops-floorplan">
    <header class="sfp-header">
      <div>
        <h2 class="sfp-title">{{ title }}</h2>
        <p class="sfp-subtitle">{{ subtitle }}</p>
      </div>
      <div class="sfp-toolbar" role="group" :aria-label="t('wcsOps.floor.toolbarAria')">
        <div class="sfp-field">
          <label for="sfpMap">{{ t('wcsOps.floor.map') }}</label>
          <select id="sfpMap" v-model="selectedMap" :aria-label="t('wcsOps.floor.map')">
            <option v-for="m in maps" :key="m.code" :value="m.code">{{ m.name }}</option>
          </select>
        </div>
        <div class="sfp-field">
          <label for="sfpLayer">{{ t('wcsOps.floor.layer') }}</label>
          <select id="sfpLayer" v-model="selectedLayer" :aria-label="t('wcsOps.floor.layer')">
            <option v-for="z in layers" :key="z.code" :value="z.code">{{ z.label }}</option>
          </select>
        </div>
        <button
          type="button"
          class="sfp-calib-btn"
          :class="{ 'is-on': calibOn }"
          :aria-pressed="calibOn"
          :title="t('wcsOps.floor.calibTitle')"
          @click="calibOn = !calibOn"
        >
          <svg class="sfp-calib-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 7h16M4 12h10M4 17h7" />
            <circle cx="18" cy="17" r="3" />
          </svg>
          <span>{{ t('wcsOps.floor.calib') }}</span>
        </button>
        <button
          type="button"
          class="sfp-calib-btn"
          :title="t('wcsOps.floor.refreshTitle')"
          :aria-label="t('wcsOps.floor.refresh')"
          :disabled="loading"
          @click="refreshAll"
        >
          <svg class="sfp-calib-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 12a9 9 0 1 1-2.64-6.36" />
            <polyline points="21 3 21 9 15 9" />
          </svg>
          <span>{{ t('wcsOps.floor.refresh') }}</span>
        </button>
        <div class="sfp-meta" aria-live="polite">
          <span class="sfp-pulse" :class="pulseClass" aria-hidden="true" />
          <span>{{ refreshHint }}</span>
        </div>
      </div>
    </header>

    <div class="sfp-legend" :aria-label="t('wcsOps.floor.legend')">
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-bin" aria-hidden="true">{{ t('wcsOps.floor.markBin') }}</i>{{ t('wcsOps.floor.bin') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-corridor" aria-hidden="true">{{ t('wcsOps.floor.markCorridor') }}</i>{{ t('wcsOps.floor.corridor') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-device" aria-hidden="true">{{ t('wcsOps.floor.markDevice') }}</i>{{ t('wcsOps.floor.device') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-gateway" aria-hidden="true">{{ t('wcsOps.floor.markGateway') }}</i>{{ t('wcsOps.floor.gateway') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-car" aria-hidden="true">{{ t('wcsOps.floor.markCar') }}</i>{{ t('wcsOps.floor.carEmpty') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-car-load" aria-hidden="true">{{ t('wcsOps.floor.markLoad') }}</i>{{ t('wcsOps.floor.carLoad') }}</span>
      <span class="sfp-leg"><i class="sfp-swatch sfp-swatch-path" aria-hidden="true" /><span>{{ t('wcsOps.floor.path') }}</span></span>
      <span class="sfp-leg sfp-leg-hint">{{ t('wcsOps.floor.hint') }}</span>
    </div>

    <div class="sfp-stats" aria-live="polite">
      <span class="sfp-stat">{{ t('wcsOps.floor.bin') }} <strong>{{ stats.bin }}</strong></span>
      <span class="sfp-stat">{{ t('wcsOps.floor.corridor') }} <strong>{{ stats.corridor }}</strong></span>
      <span class="sfp-stat">{{ t('wcsOps.floor.device') }} <strong>{{ stats.device }}</strong></span>
      <span class="sfp-stat">{{ t('wcsOps.floor.gateway') }} <strong>{{ stats.gateway }}</strong></span>
      <span class="sfp-stat">{{ t('wcsOps.floor.carsOnLayer') }} <strong>{{ cars.length }}</strong></span>
      <span class="sfp-stat">{{ t('wcsOps.floor.edges') }} <strong>{{ stats.edge }}</strong></span>
    </div>

    <div class="sfp-body">
      <div class="sfp-canvas-wrap">
        <div v-if="!cells.length" class="sfp-empty">{{ t('wcsOps.floor.empty') }}</div>
        <div v-else class="sfp-canvas-stack">
          <div
            class="sfp-canvas"
            role="grid"
            :aria-label="t('wcsOps.floor.canvasAria')"
            :style="canvasStyle"
          >
            <div class="sfp-corner" />
            <div v-for="x in xs" :key="'ax-' + x" class="sfp-axis-x">X{{ x }}</div>
            <template v-for="y in ys" :key="'row-' + y">
              <div class="sfp-axis-y">Y{{ y }}</div>
              <button
                v-for="x in xs"
                :key="y + '-' + x"
                type="button"
                class="sfp-cell"
                :class="cellClass(cellAt(x, y))"
                :data-xy="`${x},${y}`"
                :data-code="cellAt(x, y)?.code || ''"
                :data-kind="cellAt(x, y)?.kind || 'empty'"
                :data-car="carAt(x, y)?.deviceNo || undefined"
                :aria-label="cellAria(cellAt(x, y), x, y)"
                @click="onCellClick(cellAt(x, y), x, y)"
              >
                <template v-if="cellAt(x, y)">
                  <span class="sfp-cell-kind" aria-hidden="true">{{ kindMark(cellAt(x, y)!.kind) }}</span>
                  <span class="sfp-cell-code">{{ shortCode(cellAt(x, y)!.code) }}</span>
                  <span v-if="calibOn" class="sfp-cell-qr">{{ cellAt(x, y)!.code }}</span>
                  <span class="sfp-cell-xy">{{ x }},{{ y }}</span>
                  <span
                    v-if="carAt(x, y)"
                    class="sfp-car"
                    :class="{ 'is-load': carAt(x, y)!.hasPallet, 'is-err': !!carAt(x, y)!.errorMessage }"
                  >
                    {{ carAt(x, y)!.deviceNo }}
                    <span v-if="carAt(x, y)!.hasPallet" class="sfp-car-load-mark">{{ t('wcsOps.floor.markLoad') }}</span>
                  </span>
                </template>
              </button>
            </template>
          </div>
          <svg class="sfp-path-layer" :viewBox="pathViewBox" aria-hidden="true">
            <path
              v-for="(p, i) in pathPolylines"
              :key="'path-' + i"
              :d="p.d"
              fill="none"
              stroke="#ea580c"
              stroke-width="4"
              stroke-linecap="round"
              stroke-linejoin="round"
              marker-end="url(#sfpArrow)"
            />
            <defs>
              <marker id="sfpArrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                <path d="M0,0 L6,3 L0,6 Z" fill="#ea580c" />
              </marker>
            </defs>
          </svg>
        </div>
        <div v-if="tip" class="sfp-tip is-visible" role="dialog" :aria-label="t('wcsOps.floor.tipAria')">
          <div class="sfp-tip-head">
            <button type="button" class="sfp-tip-title" @click="copyTip">{{ tip.code }}</button>
            <button type="button" class="sfp-tip-close" :aria-label="t('common.close')" @click="tip = null">×</button>
          </div>
          <div>{{ t('wcsOps.floor.kind') }}：{{ tip.kind }}</div>
          <div>XY：{{ tip.x }},{{ tip.y }} · Z={{ tip.z }}</div>
          <div>{{ t('wcsOps.floor.occupy') }}：{{ tip.occupy ? t('wcsOps.floor.yes') : t('wcsOps.floor.no') }}</div>
          <div v-if="tip.locked">{{ t('wcsOps.floor.locked') }}</div>
        </div>
      </div>

      <aside class="sfp-side sfp-side-wide" :aria-label="t('wcsOps.floor.sideAria')">
        <h3 class="sfp-side-title">{{ t('wcsOps.floor.carsTitle') }}</h3>
        <ul class="sfp-car-list">
          <li
            v-for="c in cars"
            :key="c.deviceNo"
            class="sfp-car-item"
            :class="{ 'is-active': selectedCar === c.deviceNo }"
            tabindex="0"
            @click="selectCar(c.deviceNo)"
            @keydown.enter="selectCar(c.deviceNo)"
          >
            <div class="sfp-car-name">{{ c.deviceNo }}</div>
            <div class="sfp-car-line">
              {{ c.locationCode || '—' }} · X={{ c.x ?? '—' }} Y={{ c.y ?? '—' }} ·
              {{ c.workStatusText }} ·
              {{ c.hasPallet ? t('wcsOps.floor.carLoad') : t('wcsOps.floor.carEmpty') }} ·
              {{ c.hasTask ? t('wcsOps.floor.hasTask') : t('wcsOps.floor.idle') }} ·
              {{ c.power ?? 100 }}%
            </div>
            <div v-if="c.errorMessage" class="sfp-car-err">{{ c.errorMessage }}</div>
          </li>
          <li v-if="!cars.length" class="sfp-car-line" style="padding: 8px">{{ t('wcsOps.floor.noCars') }}</li>
        </ul>

        <div v-if="selectedCarRow" class="sfp-car-ops">
          <h4 class="sfp-side-title">{{ t('wcsOps.floor.selectedCar') }}</h4>
          <div class="sfp-car-ops-info">
            {{ selectedCarRow.locationCode || '—' }} · X={{ selectedCarRow.x ?? '—' }} Y={{ selectedCarRow.y ?? '—' }} ·
            {{ selectedCarRow.workStatusText }}
          </div>
          <div v-if="selectedCarRow.hasTask" class="sfp-car-ops-btns">
            <button type="button" class="sfp-calib-btn is-on" @click="openTaskDetail">
              {{ t('wcsOps.floor.taskDetail') }}
            </button>
          </div>
          <div v-else class="sfp-car-ops-btns">
            <button type="button" class="sfp-calib-btn is-on" @click="openPointDlg">
              {{ t('wcsOps.floor.pointDispatch') }}
            </button>
            <button type="button" class="sfp-calib-btn" @click="doCharge(false)">{{ t('wcsOps.floor.charge') }}</button>
            <button type="button" class="sfp-calib-btn" @click="doCharge(true)">{{ t('wcsOps.floor.endCharge') }}</button>
          </div>
          <p v-if="opsMsg" class="ops-inline-msg">{{ opsMsg }}</p>
        </div>
      </aside>
    </div>

    <details class="sfp-task-board" :open="taskBoardOpen">
      <summary @click.prevent="taskBoardOpen = !taskBoardOpen">
        {{ t('wcsOps.floor.taskBoard') }}
      </summary>
      <div class="sfp-task-cols">
        <div v-for="col in taskColumns" :key="col.key" class="sfp-task-col">
          <h4>{{ col.title }} ({{ col.rows.length }})</h4>
          <ul class="sfp-task-list">
            <li
              v-for="row in col.rows"
              :key="row.id"
              :class="{ 'is-exec': col.key === 'exec', 'is-exception': col.key === 'exception' }"
              @click="onTaskClick(row)"
            >
              <div class="sfp-task-row-main">
                <span class="sfp-task-entity">{{ row.kind }}</span>
                {{ row.containerCode || row.id }}
              </div>
              <div class="sfp-task-detail">{{ row.fromCode || '—' }} → {{ row.toCode || '—' }} · {{ row.status }}</div>
            </li>
            <li v-if="!col.rows.length" class="sfp-task-detail" style="cursor: default">{{ t('wcsOps.floor.noTasks') }}</li>
          </ul>
        </div>
      </div>
    </details>

    <!-- 指定点调度：不遮挡地图（左下浮层） -->
    <div v-if="pointDlgOpen" class="sfp-dlg" role="dialog" :aria-label="t('wcsOps.floor.pointDispatch')">
      <div class="sfp-dlg-panel">
        <header class="sfp-dlg-head">
          <strong>{{ t('wcsOps.floor.pointDispatch') }}</strong>
          <button type="button" class="sfp-calib-btn" @click="pointDlgOpen = false">×</button>
        </header>
        <p class="sfp-dlg-hint">{{ t('wcsOps.floor.pointHint') }}</p>
        <div class="sfp-dlg-grid">
          <label class="sfp-dlg-field">From<input v-model="pointForm.fromCode" class="ops-mono" /></label>
          <label class="sfp-dlg-field">To<input v-model="pointForm.toCode" class="ops-mono" /></label>
        </div>
        <div class="sfp-dlg-actions">
          <button type="button" class="sfp-calib-btn" @click="pointDlgOpen = false">{{ t('common.cancel') }}</button>
          <button type="button" class="sfp-calib-btn is-on" @click="submitPointDispatch">{{ t('common.confirm') }}</button>
        </div>
      </div>
    </div>

    <div v-if="taskDrawer" class="ops-drawer" @click.self="taskDrawer = null">
      <div class="ops-drawer-panel">
        <header class="ops-drawer-head">
          <h3>{{ t('wcsOps.floor.taskDetail') }}</h3>
          <button type="button" class="sfp-calib-btn" @click="taskDrawer = null">{{ t('common.close') }}</button>
        </header>
        <div class="ops-drawer-body">
          <pre class="ops-mono" style="white-space: pre-wrap; font-size: 12px">{{ JSON.stringify(taskDrawer, null, 2) }}</pre>
          <div v-if="taskDrawer?.shuttle?.id" style="margin-top: 12px">
            <button type="button" class="sfp-calib-btn is-on" @click="forceComplete('shuttle', taskDrawer.shuttle.id)">
              {{ t('wcsOps.common.forceComplete') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../api/http'
import '../../styles/ops-floorplan.css'

const props = defineProps<{
  pack: 'fourway' | 'stacker'
  title: string
  subtitle: string
}>()

const { t } = useI18n()

type UiKind = 'bin' | 'corridor' | 'device' | 'gateway' | 'other'
type FloorCell = {
  code: string
  x: number
  y: number
  z: number
  kind: UiKind
  occupy: boolean
  locked: boolean
  px: number
  py: number
}
type FloorCar = {
  deviceNo: string
  locationCode?: string
  x?: number
  y?: number
  workStatusText: string
  hasPallet: boolean
  hasTask: boolean
  power?: number
  errorMessage?: string
  path?: { x: number; y: number }[]
}
type TaskRow = { id: string; kind: string; containerCode?: string; fromCode?: string; toCode?: string; status: string }

const loading = ref(false)
const lastOk = ref(false)
const calibOn = ref(false)
const taskBoardOpen = ref(false)
const pointDlgOpen = ref(false)
const pointPickMode = ref(false)
const opsMsg = ref('')
const taskDrawer = ref<any>(null)
const pointForm = reactive({ fromCode: '', toCode: '' })
const selectedMap = ref('')
const selectedLayer = ref('')
const selectedCar = ref<string | null>(null)
const tip = ref<(FloorCell & { z: number }) | null>(null)
const fixture = ref<any>(null)
const occupyMap = ref<Record<string, { occupy: boolean; locked: boolean; handover: boolean }>>({})
const board = ref<any>(null)
const pathCodes = ref<string[]>([])
let timer: ReturnType<typeof setInterval> | null = null

const maps = computed(() => {
  if (props.pack === 'fourway') {
    return (fixture.value?.maps || []).map((m: any) => ({
      code: m.code,
      name: /L01/i.test(m.layerCode || '') ? 'SIM' : String(m.layerCode || m.code),
      layerCode: m.layerCode,
      nodes: m.nodes || [],
    }))
  }
  const aisles = fixture.value?.aisles || []
  return aisles.map((a: any) => ({
    code: a.aisleCode,
    name: a.aisleCode,
    layerCode: '',
    nodes: a.sampleLocations || [],
  }))
})

const layers = computed(() => {
  if (props.pack === 'fourway') {
    const fromFix = (fixture.value?.layers || []).map((l: any) => ({
      code: l.code,
      label: `Z=${String(l.code).replace(/^Fw\.L0?/i, '') || l.name}`,
    }))
    if (fromFix.length) return fromFix
  }
  // stacker: depth as "layer" selector (浅/深)
  const set = new Set<number>()
  for (const n of maps.value.find((m: any) => m.code === selectedMap.value)?.nodes || []) {
    const depth = Number(n.depth || 1)
    if (Number.isFinite(depth)) set.add(depth)
  }
  const arr = [...set].sort((a, b) => a - b)
  return (arr.length ? arr : [1]).map((z) => ({ code: String(z), label: `Depth=${z}` }))
})

function mapSimKind(raw: string): UiKind {
  const k = (raw || '').toLowerCase()
  if (k === 'location' || k === 'bin') return 'bin'
  if (k.includes('track') || k === 'corridor') return 'corridor'
  if (k.includes('conveyor') || k.includes('lift') || k.includes('hoist') || k === 'device' || k.includes('srm')) return 'device'
  if (k.includes('gateway') || k.includes('handover')) return 'gateway'
  return 'other'
}

function parseLogical(code: string): { x: number; y: number; z: number } | null {
  const c = code || ''
  let m = c.match(/_1Z001(\d{2})(\d{2})(\d{2})$/i)
  if (m) return { y: +m[1], x: +m[2], z: +m[3] }
  m = c.match(/A(\d{2})(\d{2})_/i)
  if (m) return { x: +m[1], y: +m[2], z: 1 }
  return null
}

const cells = computed<FloorCell[]>(() => {
  const map = maps.value.find((m: any) => m.code === selectedMap.value)
  if (!map) return []
  const rawNodes: any[] = map.nodes || []
  const parsed: FloorCell[] = []

  if (props.pack === 'fourway') {
    const layerZ = Number(String(selectedLayer.value).replace(/\D/g, '')) || 1
    // Pixel-rank grid: keep every fixture node (S1/S2/track) so the board is not collapsed by shared Axxxx codes
    const xsPix = [...new Set(rawNodes.map((n) => Number(n.x) || 0))].sort((a, b) => a - b)
    const ysPix = [...new Set(rawNodes.map((n) => Number(n.y) || 0))].sort((a, b) => a - b)
    const xRank = new Map(xsPix.map((v, i) => [v, i + 1]))
    const yRank = new Map(ysPix.map((v, i) => [v, i + 1]))

    for (const n of rawNodes) {
      const code = String(n.code || '')
      const px = Number(n.x) || 0
      const py = Number(n.y) || 0
      const logical = parseLogical(code)
      const occ = occupyMap.value[code] || occupyMap.value[code.replace(/^Fw\./, '')]
      let kind = mapSimKind(String(n.kind || ''))
      if (occ?.handover) kind = 'gateway'
      parsed.push({
        code,
        x: xRank.get(px) ?? 1,
        y: yRank.get(py) ?? 1,
        z: logical?.z ?? layerZ,
        kind,
        occupy: !!occ?.occupy,
        locked: !!occ?.locked,
        px,
        py,
      })
    }
  } else {
    const depthFilter = Number(selectedLayer.value) || 0
    for (const n of rawNodes) {
      const depth = Number(n.depth) || 1
      // selectedLayer for stacker: 1=shallow, 2=deep; 0/unset show depth 1 preferred via dedupe
      if (depthFilter > 0 && depth !== depthFilter && layers.value.length > 1) {
        // when user picks Z=depth, filter; else keep all and dedupe prefers shorter/bin
      }
      if (depthFilter > 0 && depth !== depthFilter) continue
      const code = String(n.code || '')
      const x = Number(n.column) || 1
      const y = Number(n.layer) || 1
      const occ = occupyMap.value[code]
      parsed.push({
        code,
        x,
        y,
        z: depth,
        kind: 'bin',
        occupy: !!occ?.occupy,
        locked: !!occ?.locked,
        px: Number(n.x) || 0,
        py: Number(n.y) || 0,
      })
    }
  }

  // Deduplicate same XY: prefer bin > device > corridor > other
  const rank: Record<UiKind, number> = { bin: 4, gateway: 3, device: 2, corridor: 1, other: 0 }
  const best = new Map<string, FloorCell>()
  for (const c of parsed) {
    const key = `${c.x},${c.y}`
    const prev = best.get(key)
    if (!prev || rank[c.kind] > rank[prev.kind] || (rank[c.kind] === rank[prev.kind] && c.code.length < prev.code.length)) {
      best.set(key, c)
    }
  }
  return [...best.values()]
})

const xs = computed(() => {
  const set = new Set(cells.value.map((c) => c.x))
  return [...set].sort((a, b) => a - b)
})
const ys = computed(() => {
  const set = new Set(cells.value.map((c) => c.y))
  return [...set].sort((a, b) => a - b)
})

const cellIndex = computed(() => {
  const m = new Map<string, FloorCell>()
  for (const c of cells.value) m.set(`${c.x},${c.y}`, c)
  return m
})

function cellAt(x: number, y: number) {
  return cellIndex.value.get(`${x},${y}`)
}

const cars = computed<FloorCar[]>(() => {
  const list: FloorCar[] = []
  const byCode = new Map(cells.value.map((c) => [c.code, c]))
  // Active shuttle tasks as cars
  for (const s of board.value?.shuttles || []) {
    const code = s.fromCode || s.toCode
    const cell = code ? byCode.get(code) || cells.value.find((c) => c.code.includes(String(code).slice(-8))) : undefined
    const no = String(s.shuttleNo || s.deviceNo || list.length + 1)
    list.push({
      deviceNo: no.length > 4 ? String(list.length + 1) : no,
      locationCode: code,
      x: cell?.x,
      y: cell?.y,
      workStatusText: s.status || t('wcsOps.floor.working'),
      hasPallet: !!s.containerCode,
      hasTask: true,
      power: 100,
      path: pathCodes.value
        .map((pc) => byCode.get(pc) || cells.value.find((c) => c.code === pc))
        .filter(Boolean)
        .map((c) => ({ x: c!.x, y: c!.y })),
    })
  }
  // Parking ledger weak cars when idle
  if (!list.length) {
    for (const p of board.value?.summary?.parking || []) {
      /* ignore */
    }
  }
  // Demo car on main track if still empty (visual parity with screenshot when no live tasks)
  if (!list.length) {
    const track = cells.value.find((c) => /RW1_1Z001050601/i.test(c.code)) || cells.value.find((c) => c.kind === 'corridor')
    if (track) {
      list.push({
        deviceNo: '1',
        locationCode: track.code,
        x: track.x,
        y: track.y,
        workStatusText: t('wcsOps.floor.idle'),
        hasPallet: false,
        hasTask: false,
        power: 100,
      })
    }
  }
  return list
})

function carAt(x: number, y: number) {
  return cars.value.find((c) => c.x === x && c.y === y)
}

const selectedCarRow = computed(() => cars.value.find((c) => c.deviceNo === selectedCar.value) || null)

const stats = computed(() => {
  const s = { bin: 0, corridor: 0, device: 0, gateway: 0, edge: 0 }
  for (const c of cells.value) {
    if (c.kind === 'bin') s.bin++
    else if (c.kind === 'corridor') s.corridor++
    else if (c.kind === 'device') s.device++
    else if (c.kind === 'gateway') s.gateway++
  }
  s.edge = Math.max(0, cells.value.length)
  return s
})

const canvasStyle = computed(() => ({
  gridTemplateColumns: `var(--sfp-cell-size) repeat(${xs.value.length}, var(--sfp-cell-size))`,
}))

const pathViewBox = computed(() => {
  const cols = xs.value.length + 1
  const rows = ys.value.length + 1
  const size = 48
  const gap = 3
  return `0 0 ${cols * (size + gap)} ${rows * (size + gap)}`
})

const pathPolylines = computed(() => {
  const car = selectedCarRow.value
  const pts = car?.path?.length ? car.path : []
  if (pts.length < 2) return [] as { d: string }[]
  const size = 48
  const gap = 3
  const cell = size + gap
  const toPx = (p: { x: number; y: number }) => {
    const ix = xs.value.indexOf(p.x)
    const iy = ys.value.indexOf(p.y)
    const cx = cell + ix * cell + size / 2
    const cy = 28 + iy * cell + size / 2
    return { cx, cy }
  }
  const parts: string[] = []
  pts.forEach((p, i) => {
    const { cx, cy } = toPx(p)
    parts.push(`${i === 0 ? 'M' : 'L'}${cx} ${cy}`)
  })
  return [{ d: parts.join(' ') }]
})

const pulseClass = computed(() => {
  if (loading.value) return 'is-stale'
  return lastOk.value ? 'is-ok' : 'is-off'
})

const refreshHint = computed(() => {
  const z = selectedLayer.value || '—'
  const now = new Date()
  const ts = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`
  return `${z} · ${t('wcsOps.floor.carsOnLayer')} ${cars.value.length} · ${t('wcsOps.floor.edges')} ${stats.value.edge} · ${ts}`
})

const taskColumns = computed(() => {
  const all: TaskRow[] = []
  const push = (rows: any[], kind: string) => {
    for (const r of rows || []) {
      all.push({
        id: String(r.id),
        kind,
        containerCode: r.containerCode,
        fromCode: r.fromCode || r.fromPointCode,
        toCode: r.toCode || r.destinationPointCode,
        status: String(r.status || ''),
      })
    }
  }
  push(board.value?.putAways, 'putAway')
  push(board.value?.retrievals, 'retrieval')
  push(board.value?.shuttles, 'shuttle')
  push(board.value?.hoists, 'hoist')
  push(board.value?.devices, 'device')

  const isFail = (s: string) => /fail|error|exception|异常/i.test(s)
  const isPend = (s: string) => /pend|wait|creat|new|待/i.test(s)
  const isAssign = (s: string) => /assign|alloc|dispatch|已分配/i.test(s)
  return [
    {
      key: 'exec',
      title: t('wcsOps.floor.taskExec'),
      rows: all.filter((r) => !isFail(r.status) && !isPend(r.status) && !isAssign(r.status)),
    },
    {
      key: 'assigned',
      title: t('wcsOps.floor.taskAssigned'),
      rows: all.filter((r) => isAssign(r.status)),
    },
    {
      key: 'pending',
      title: t('wcsOps.floor.taskPending'),
      rows: all.filter((r) => isPend(r.status)),
    },
    {
      key: 'exception',
      title: t('wcsOps.floor.taskException'),
      rows: all.filter((r) => isFail(r.status)),
    },
  ]
})

function kindMark(kind: UiKind) {
  if (kind === 'bin') return t('wcsOps.floor.markBin')
  if (kind === 'corridor') return t('wcsOps.floor.markCorridor')
  if (kind === 'device') return t('wcsOps.floor.markDevice')
  if (kind === 'gateway') return t('wcsOps.floor.markGateway')
  return '·'
}

function shortCode(code: string) {
  if (!code) return ''
  if (code.length <= 12) return code
  return code.slice(0, 10) + '…'
}

function cellClass(cell?: FloorCell) {
  if (!cell) return { 'is-empty': true }
  return {
    [`is-${cell.kind}`]: true,
    'is-occupy': cell.occupy && !cell.locked,
    'is-lock': cell.locked,
    'is-focus-car': !!(cell && carAt(cell.x, cell.y) && selectedCar.value === carAt(cell.x, cell.y)?.deviceNo),
  }
}

function cellAria(cell: FloorCell | undefined, x: number, y: number) {
  if (!cell) return `X${x} Y${y}`
  return `${cell.code} X${x} Y${y}`
}

function onCellClick(cell: FloorCell | undefined, x: number, y: number) {
  const car = carAt(x, y)
  if (car && !pointPickMode.value) {
    selectCar(car.deviceNo)
    return
  }
  if (!cell) return
  if (pointPickMode.value && pointDlgOpen.value) {
    pointForm.toCode = cell.code
    pointPickMode.value = false
    ElMessage.success(t('wcsOps.floor.pointPicked'))
    return
  }
  tip.value = { ...cell, z: cell.z }
}

function selectCar(no: string) {
  selectedCar.value = no
  tip.value = null
}

async function copyTip() {
  if (!tip.value) return
  try {
    await navigator.clipboard.writeText(tip.value.code)
    ElMessage.success(t('wcsOps.floor.copied'))
  } catch {
    /* ignore */
  }
}

function openPointDlg() {
  pointForm.fromCode = selectedCarRow.value?.locationCode || ''
  pointForm.toCode = ''
  pointDlgOpen.value = true
  pointPickMode.value = true
  opsMsg.value = t('wcsOps.floor.pointHint')
}

async function submitPointDispatch() {
  if (props.pack !== 'fourway') return
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/point-dispatch', {
    fromCode: pointForm.fromCode,
    toCode: pointForm.toCode,
    layerCode: selectedLayer.value,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.dispatched'))
    pointDlgOpen.value = false
    await refreshAll()
  } else {
    ElMessage.error(res.message || t('wcsOps.common.failed'))
    opsMsg.value = res.message || t('wcsOps.common.failed')
  }
}

async function doCharge(stop: boolean) {
  if (props.pack !== 'fourway') return
  const from = selectedCarRow.value?.locationCode || ''
  const url = stop ? '/api/Wcs/FourWay/Ops/charge/stop' : '/api/Wcs/FourWay/Ops/charge'
  const res = await http.post<{ status: boolean; message?: string }>(url, {
    fromCode: from,
    chargePointCode: from,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.success'))
    opsMsg.value = ''
    await refreshAll()
  } else {
    ElMessage.error(res.message || t('wcsOps.common.failed'))
    opsMsg.value = res.message || ''
  }
}

async function openTaskDetail() {
  taskBoardOpen.value = true
  const shuttle = (board.value?.shuttles || [])[0]
  if (!shuttle?.id) return
  const res = await http.get<{ status: boolean; data?: any }>(
    `/api/Wcs/FourWay/Ops/task-tree?shuttleTaskId=${shuttle.id}`,
  )
  if (res.status) taskDrawer.value = res.data
}

async function forceComplete(targetType: string, id: string) {
  await ElMessageBox.confirm(t('wcsOps.common.confirmForce'), t('wcsOps.common.danger'), { type: 'warning' })
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/force-complete', {
    targetType,
    id,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.done'))
    taskDrawer.value = null
    await refreshAll()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

function onTaskClick(row: TaskRow) {
  void loadPathForTask(row)
}

async function loadPathForTask(row: TaskRow) {
  pathCodes.value = []
  try {
    const base = props.pack === 'fourway' ? '/api/Wcs/FourWay/Ops/task-tree' : '/api/Wcs/Stacker/Ops/task-tree'
    const q =
      row.kind === 'putAway'
        ? `putAwayId=${row.id}`
        : row.kind === 'retrieval'
          ? `retrievalId=${row.id}`
          : row.kind === 'shuttle'
            ? `shuttleTaskId=${row.id}`
            : row.kind === 'device'
              ? `deviceTaskId=${row.id}`
              : ''
    if (!q) return
    const res = await http.get<{ status: boolean; data?: any }>(`${base}?${q}`)
    if (res.status) {
      const paths = res.data?.paths || res.data?.shuttle?.paths || []
      pathCodes.value = (paths as any[]).map((p) => p.pointCode || p.nodeCode || p.code).filter(Boolean)
      if (row.fromCode || row.toCode) {
        const car = cars.value.find((c) => c.hasTask) || cars.value[0]
        if (car) selectedCar.value = car.deviceNo
      }
    }
  } catch {
    /* ignore */
  }
}

async function loadFixture() {
  const url =
    props.pack === 'fourway' ? '/fixtures/singapore-fourway-map.json' : '/fixtures/srm-demo-stacker-map.json'
  const res = await fetch(url)
  fixture.value = await res.json()
  if (maps.value.length && !selectedMap.value) selectedMap.value = maps.value[0].code
  if (layers.value.length && !selectedLayer.value) selectedLayer.value = layers.value[0].code
}

async function loadOccupy() {
  const pack = props.pack
  const res = await http.post<{ status: boolean; data?: { rows?: any[] } }>('/api/WmsLocation/getPageData', {
    page: 1,
    rows: 2000,
    whwere: `PackId='${pack}'`,
  })
  const map: Record<string, { occupy: boolean; locked: boolean; handover: boolean }> = {}
  if (res.status) {
    for (const r of res.data?.rows || []) {
      map[String(r.code)] = {
        occupy: !!r.isOccupied,
        locked: !!r.isLocked,
        handover: !!r.isHandover,
      }
    }
  }
  // FourWay pickable-map also covers layer-filtered codes
  if (pack === 'fourway' && selectedLayer.value) {
    try {
      const pm = await http.get<{ status: boolean; data?: any[] }>(
        `/api/Wcs/FourWay/Ops/inbound/pickable-map?layerCode=${encodeURIComponent(selectedLayer.value)}`,
      )
      if (pm.status) {
        for (const r of pm.data || []) {
          const code = String(r.code)
          map[code] = {
            occupy: r.pickBlockReason === 'occupied' || !!map[code]?.occupy,
            locked: r.pickBlockReason === 'locked' || !!map[code]?.locked,
            handover: r.pickBlockReason === 'handover' || !!map[code]?.handover,
          }
        }
      }
    } catch {
      /* ignore */
    }
  }
  occupyMap.value = map
}

async function loadBoard() {
  const url = props.pack === 'fourway' ? '/api/Wcs/FourWay/Ops/board' : '/api/Wcs/Stacker/Ops/board'
  const res = await http.get<{ status: boolean; data?: any }>(url)
  if (res.status) board.value = res.data
}

async function refreshAll() {
  loading.value = true
  try {
    if (!fixture.value) await loadFixture()
    await Promise.all([loadOccupy(), loadBoard()])
    lastOk.value = true
    if (!selectedCar.value && cars.value.length) selectedCar.value = cars.value[0].deviceNo
  } catch {
    lastOk.value = false
  } finally {
    loading.value = false
  }
}

watch(selectedMap, async () => {
  const m = maps.value.find((x: any) => x.code === selectedMap.value)
  if (m?.layerCode) selectedLayer.value = m.layerCode
  tip.value = null
  await refreshAll()
})

watch(selectedLayer, async (z) => {
  tip.value = null
  if (props.pack === 'fourway') {
    const m = maps.value.find((x: any) => x.layerCode === z)
    if (m && m.code !== selectedMap.value) {
      selectedMap.value = m.code
      return
    }
  }
  await loadOccupy()
})

onMounted(async () => {
  await refreshAll()
  timer = setInterval(() => void refreshAll(), 4000)
})
onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>
