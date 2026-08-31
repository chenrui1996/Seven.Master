<script setup lang="ts">

import { computed, ref, watch } from 'vue'

import { ElMessage } from 'element-plus'

import http from '../api/http'

import MapCanvas from '../components/map/MapCanvas.vue'

import {

  DEVICE_CATALOG_LIST,

  formatPortRef,

  nextDeviceCode,

  parsePortRef,

  portLabel,

  type DeviceCatalogType,

} from '../components/map/deviceCatalog'

import { compileMapSpaces } from '../lib/map/spaceCompiler'

import { validateTopology } from '../lib/map/topologyValidator'

import { compileMapForDeploy } from '../lib/project/deployCompile'

import { useProjectStore } from '../stores/project'



const store = useProjectStore()

const selectedNodeId = ref<string | null>(null)

const selectedDeviceId = ref<string | null>(null)

const placementType = ref<DeviceCatalogType | null>(null)

const code = ref('')

const x = ref(40)

const y = ref(40)

const deploying = ref(false)

const lastDeploy = ref('')

const topologyErrors = computed(() => validateTopology(store.project.map))

const canDeploy = computed(() => topologyErrors.value.length === 0)



const edgeFrom = ref('')

const edgeTo = ref('')

const connFrom = ref('')

const connTo = ref('')

const rpCode = ref('')

const rpLocation = ref('')



const portOptions = computed(() => {

  const opts: { value: string; label: string }[] = []

  for (const d of store.project.map.devices) {

    const entry = DEVICE_CATALOG_LIST.find((c) => c.type === d.type)

    if (!entry) continue

    for (const p of entry.ports) {

      opts.push({

        value: formatPortRef(d.id, p.id),

        label: portLabel(d, p.id),

      })

    }

  }

  return opts

})



function addNode() {

  if (!code.value.trim()) {

    ElMessage.warning('请输入库位/节点编码')

    return

  }

  store.project.map.nodes.push({

    id: crypto.randomUUID(),

    code: code.value.trim(),

    x: x.value,

    y: y.value,

  })

  code.value = ''

  x.value += 80

  ElMessage.success('已添加节点（本地工程）')

}



function syncSelectedNode() {

  if (!selectedNodeId.value) return

  const n = store.project.map.nodes.find((node) => node.id === selectedNodeId.value)

  if (!n) return

  const trimmed = code.value.trim()

  if (trimmed) n.code = trimmed

  n.x = x.value

  n.y = y.value

}



watch([code, x, y], () => syncSelectedNode())



function addEdge() {

  if (!edgeFrom.value || !edgeTo.value) {

    ElMessage.warning('请选择起点与终点节点')

    return

  }

  if (edgeFrom.value === edgeTo.value) {

    ElMessage.warning('起点与终点不能相同')

    return

  }

  store.project.map.edges.push({

    id: crypto.randomUUID(),

    from: edgeFrom.value,

    to: edgeTo.value,

  })

  edgeFrom.value = ''

  edgeTo.value = ''

  ElMessage.success('已添加边')

}



function removeEdge(id: string) {

  store.project.map.edges = store.project.map.edges.filter((e) => e.id !== id)

}



function addConnection() {

  if (!connFrom.value || !connTo.value) {

    ElMessage.warning('请选择起点与终点端口')

    return

  }

  if (connFrom.value === connTo.value) {

    ElMessage.warning('起点与终点不能相同')

    return

  }

  const fromParsed = parsePortRef(connFrom.value)

  const toParsed = parsePortRef(connTo.value)

  if (!fromParsed || !toParsed) {

    ElMessage.warning('端口格式无效')

    return

  }

  store.project.map.connections.push({

    id: crypto.randomUUID(),

    from: connFrom.value,

    to: connTo.value,

  })

  connFrom.value = ''

  connTo.value = ''

  ElMessage.success('已添加设备连线')

}



function removeConnection(id: string) {

  store.project.map.connections = store.project.map.connections.filter((c) => c.id !== id)

}



function connectionLabel(ref: string): string {

  const parsed = parsePortRef(ref)

  if (!parsed) return ref

  const d = store.project.map.devices.find((dev) => dev.id === parsed.deviceId)

  return d ? portLabel(d, parsed.portId) : ref

}



function addRequestPoint() {

  if (!rpCode.value.trim() || !rpLocation.value.trim()) {

    ElMessage.warning('请填写申请点编码与映射库位')

    return

  }

  store.project.map.requestPoints.push({

    code: rpCode.value.trim(),

    mappedLocationCode: rpLocation.value.trim(),

  })

  rpCode.value = ''

  rpLocation.value = ''

  ElMessage.success('已添加申请点')

}



function removeRequestPoint(index: number) {

  store.project.map.requestPoints.splice(index, 1)

}



function nodeLabel(id: string) {

  return store.project.map.nodes.find((n) => n.id === id)?.code ?? id

}



function onSelectNode(id: string | null) {

  selectedNodeId.value = id

  if (id) {

    selectedDeviceId.value = null

    placementType.value = null

    const n = store.project.map.nodes.find((node) => node.id === id)

    if (n) {

      code.value = n.code

      x.value = n.x

      y.value = n.y

    }

  }

}



function onSelectDevice(id: string | null) {

  selectedDeviceId.value = id

  if (id) {

    selectedNodeId.value = null

    placementType.value = null

  }

}



function startPlacement(type: DeviceCatalogType) {

  placementType.value = placementType.value === type ? null : type

  selectedNodeId.value = null

  selectedDeviceId.value = null

}



function onDragStart(type: DeviceCatalogType, e: DragEvent) {

  e.dataTransfer?.setData('application/x-seven-device', type)

  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'copy'

}



function placeDevice(payload: { type: DeviceCatalogType; x: number; y: number }) {
  store.project.map.devices.push({
    id: crypto.randomUUID(),
    code: nextDeviceCode(payload.type, store.project.map.devices),
    type: payload.type,
    x: payload.x,
    y: payload.y,
  })
  placementType.value = null
  ElMessage.success('已放置设备')
}

function removeDevice(id: string) {

  store.project.map.devices = store.project.map.devices.filter((d) => d.id !== id)

  store.project.map.connections = store.project.map.connections.filter((c) => {

    const from = parsePortRef(c.from)

    const to = parsePortRef(c.to)

    return from?.deviceId !== id && to?.deviceId !== id

  })

  if (selectedDeviceId.value === id) selectedDeviceId.value = null

}



function clearMap() {

  store.project.map.nodes = []

  store.project.map.edges = []

  store.project.map.devices = []

  store.project.map.connections = []

  store.project.map.requestPoints = []

  selectedNodeId.value = null

  selectedDeviceId.value = null

  placementType.value = null

}



async function onImport(file: File) {

  await store.importProject(file)

  ElMessage.success('工程已导入')

  return false

}



async function onImportSimProj(file: File) {

  const warnings = await store.importSimProj(file)

  if (warnings.length) {

    ElMessage.warning(warnings.join('；'))

  }

  ElMessage.success('simproj 已适配导入')

  return false

}



async function onImportGrid(file: File) {

  try {

    const parsed = JSON.parse(await file.text()) as {

      packId?: string

      cells?: { code: string; x: number; y: number }[]

    }

    const { data } = await http.post<{

      status: boolean

      message?: string

      data?: {

        map: typeof store.project.map

        warnings: string[]

      }

    }>('/api/simulation/import-grid', {

      packId: parsed.packId ?? store.project.map.packId,

      cells: parsed.cells ?? [],

    })

    if (!data.status || !data.data) {

      ElMessage.error(data.message || '栅格导入失败')

      return false

    }

    store.applyImportedMap(data.data.map)

    if (data.data.warnings.length) {

      ElMessage.warning(data.data.warnings.join('；'))

    }

    ElMessage.success(`栅格导入 ${data.data.map.nodes.length} 个节点`)

  } catch (e: unknown) {

    ElMessage.error(e instanceof Error ? e.message : '栅格 JSON 无效')

  }

  return false

}



const routePreviewLoading = ref(false)

const routePreview = ref<{ code: string; nodeIds: string[] }[]>([])

const routePreviewWarnings = ref<string[]>([])



async function previewRouteGroups() {

  routePreviewLoading.value = true

  try {

    const { data } = await http.post<{

      status: boolean

      message?: string

      data?: {

        suggested: { code: string; nodeIds: string[] }[]

        warnings: string[]

      }

    }>('/api/simulation/route-groups/preview', {

      packId: store.project.map.packId,

      nodes: store.project.map.nodes,

      edges: store.project.map.edges,

    })

    if (!data.status || !data.data) {

      ElMessage.error(data.message || '路径组预览失败')

      return

    }

    routePreview.value = data.data.suggested

    routePreviewWarnings.value = data.data.warnings

    if (data.data.warnings.length) {

      ElMessage.warning(data.data.warnings.join('；'))

    } else {

      ElMessage.success(`建议 ${data.data.suggested.length} 个路径组`)

    }

  } catch (e: unknown) {

    ElMessage.error(e instanceof Error ? e.message : '路径组预览失败')

  } finally {

    routePreviewLoading.value = false

  }

}



async function deploy() {

  if (!canDeploy.value) return

  deploying.value = true

  try {

    const compiledMap = compileMapForDeploy(compileMapSpaces(store.project.map))

    const { data } = await http.post<{

      status: boolean

      message?: string

      data?: { warehouseCode: string; message: string; locationCount: number }

    }>('/api/simulation/deploy', {

      version: store.project.version,

      meta: store.project.meta,

      map: compiledMap,

    })

    if (!data.status) {

      ElMessage.error(data.message || 'Deploy 失败')

      return

    }

    const info = data.data

    lastDeploy.value = info

      ? `${info.message}（仓 ${info.warehouseCode}，节点 ${info.locationCount}）`

      : data.message || 'Deploy 成功'

    ElMessage.success(lastDeploy.value)

  } catch (e: unknown) {

    const msg = e instanceof Error ? e.message : String(e)

    ElMessage.error(`Deploy 失败: ${msg}（需 Features.Simulator=true，且已执行 EF 迁移）`)

  } finally {

    deploying.value = false

  }

}



async function undeploy() {

  try {

    const { data } = await http.post<{ status: boolean; message?: string }>('/api/simulation/undeploy', {

      projectName: store.project.meta.name,

      removeLocations: false,

    })

    if (!data.status) {

      ElMessage.error(data.message || 'Undeploy 失败')

      return

    }

    ElMessage.success(data.message || '已 Undeploy')

    lastDeploy.value = ''

  } catch (e: unknown) {

    ElMessage.error(e instanceof Error ? e.message : 'Undeploy 失败')

  }

}

</script>



<template>

  <div class="sim-page">

    <h1>② 导入 / 绘制地图</h1>

    <p class="hint">

      当前包：{{ store.project.map.packId }}。设计态保存在工程文件；Deploy 写入 Wms_Location 与包种子数据。

    </p>



    <template v-if="topologyErrors.length">

      <el-alert

        v-for="err in topologyErrors"

        :key="err.code"

        :title="err.message"

        type="error"

        show-icon

        :closable="false"

        class="topology-alert"

      />

    </template>



    <div class="toolbar">

      <el-select v-model="store.project.map.packId" style="width: 160px">

        <el-option label="stacker" value="stacker" />

        <el-option label="fourway" value="fourway" />

      </el-select>

      <el-input v-model="code" placeholder="节点编码" style="width: 140px" />

      <el-input-number v-model="x" :step="20" />

      <el-input-number v-model="y" :step="20" />

      <el-button type="primary" @click="addNode">添加节点</el-button>

      <el-button @click="clearMap">清空</el-button>

      <el-upload :show-file-list="false" :before-upload="onImport" accept=".json">

        <el-button>导入 .sevenproj.json</el-button>

      </el-upload>

      <el-upload :show-file-list="false" :before-upload="onImportSimProj" accept=".json">

        <el-button>导入 .simproj.json</el-button>

      </el-upload>

      <el-upload :show-file-list="false" :before-upload="onImportGrid" accept=".json">

        <el-button>导入栅格 JSON</el-button>

      </el-upload>

      <el-button :loading="routePreviewLoading" @click="previewRouteGroups">路径组预览</el-button>

      <el-button type="success" :loading="deploying" :disabled="!canDeploy" @click="deploy">Deploy</el-button>

      <el-button @click="undeploy">Undeploy</el-button>

    </div>



    <el-card header="设备库" shadow="never" style="margin-bottom: 1rem">

      <p class="hint" style="margin-top: 0">

        点击设备进入放置模式（再点画布放置），或拖拽到画布。Deploy 前会将连线编译为边/申请点（见 schema 注释）。

      </p>

      <div class="device-palette">

        <div

          v-for="entry in DEVICE_CATALOG_LIST"

          :key="entry.type"

          class="palette-item"

          :class="{ active: placementType === entry.type }"

          draggable="true"

          :title="entry.label"

          @click="startPlacement(entry.type)"

          @dragstart="onDragStart(entry.type, $event)"

        >

          <span class="swatch" :style="{ background: entry.fill, borderColor: entry.stroke }" />

          <span>{{ entry.label }}</span>

        </div>

      </div>

      <el-table

        v-if="store.project.map.devices.length"

        :data="store.project.map.devices"

        size="small"

        border

        style="margin-top: 0.75rem"

        empty-text="暂无设备"

      >

        <el-table-column prop="code" label="编码" width="100" />

        <el-table-column prop="type" label="类型" width="120" />

        <el-table-column label="坐标" width="120">

          <template #default="{ row }">{{ row.x }}, {{ row.y }}</template>

        </el-table-column>

        <el-table-column label="操作" width="80">

          <template #default="{ row }">

            <el-button link type="danger" @click="removeDevice(row.id)">删除</el-button>

          </template>

        </el-table-column>

      </el-table>

    </el-card>



    <MapCanvas

      :nodes="store.project.map.nodes"

      :edges="store.project.map.edges"

      :devices="store.project.map.devices"

      :connections="store.project.map.connections"

      :selected-node-id="selectedNodeId"

      :selected-device-id="selectedDeviceId"

      :placement-type="placementType"

      style="margin-bottom: 1rem"

      @select-node="onSelectNode"

      @select-device="onSelectDevice"

      @place-device="placeDevice"

    />



    <el-row :gutter="16" style="margin-bottom: 1rem">

      <el-col :span="8">

        <el-card header="边（拓扑）" shadow="never">

          <div class="toolbar">

            <el-select v-model="edgeFrom" placeholder="起点" style="width: 120px" clearable>

              <el-option

                v-for="n in store.project.map.nodes"

                :key="n.id"

                :label="n.code"

                :value="n.id"

              />

            </el-select>

            <span>→</span>

            <el-select v-model="edgeTo" placeholder="终点" style="width: 120px" clearable>

              <el-option

                v-for="n in store.project.map.nodes"

                :key="n.id"

                :label="n.code"

                :value="n.id"

              />

            </el-select>

            <el-button @click="addEdge">添加</el-button>

          </div>

          <el-table :data="store.project.map.edges" size="small" border empty-text="暂无边">

            <el-table-column label="起点" width="100">

              <template #default="{ row }">{{ nodeLabel(row.from) }}</template>

            </el-table-column>

            <el-table-column label="终点" width="100">

              <template #default="{ row }">{{ nodeLabel(row.to) }}</template>

            </el-table-column>

            <el-table-column label="操作" width="70">

              <template #default="{ row }">

                <el-button link type="danger" @click="removeEdge(row.id)">删除</el-button>

              </template>

            </el-table-column>

          </el-table>

        </el-card>

      </el-col>

      <el-col :span="8">

        <el-card header="设备连线" shadow="never">

          <div class="toolbar">

            <el-select v-model="connFrom" placeholder="起点端口" style="width: 140px" clearable>

              <el-option v-for="o in portOptions" :key="o.value" :label="o.label" :value="o.value" />

            </el-select>

            <span>→</span>

            <el-select v-model="connTo" placeholder="终点端口" style="width: 140px" clearable>

              <el-option v-for="o in portOptions" :key="o.value" :label="o.label" :value="o.value" />

            </el-select>

            <el-button @click="addConnection">添加</el-button>

          </div>

          <el-table :data="store.project.map.connections" size="small" border empty-text="暂无连线">

            <el-table-column label="起点" min-width="100">

              <template #default="{ row }">{{ connectionLabel(row.from) }}</template>

            </el-table-column>

            <el-table-column label="终点" min-width="100">

              <template #default="{ row }">{{ connectionLabel(row.to) }}</template>

            </el-table-column>

            <el-table-column label="操作" width="70">

              <template #default="{ row }">

                <el-button link type="danger" @click="removeConnection(row.id)">删除</el-button>

              </template>

            </el-table-column>

          </el-table>

        </el-card>

      </el-col>

      <el-col :span="8">

        <el-card header="申请点" shadow="never">

          <div class="toolbar">

            <el-input v-model="rpCode" placeholder="申请点编码" style="width: 120px" />

            <el-input v-model="rpLocation" placeholder="映射库位" style="width: 120px" />

            <el-button @click="addRequestPoint">添加</el-button>

          </div>

          <el-table :data="store.project.map.requestPoints" size="small" border empty-text="暂无申请点">

            <el-table-column prop="code" label="编码" width="120" />

            <el-table-column prop="mappedLocationCode" label="映射库位" />

            <el-table-column label="操作" width="70">

              <template #default="{ $index }">

                <el-button link type="danger" @click="removeRequestPoint($index)">删除</el-button>

              </template>

            </el-table-column>

          </el-table>

        </el-card>

      </el-col>

    </el-row>



    <el-alert v-if="lastDeploy" :title="lastDeploy" type="success" show-icon :closable="false" style="margin-bottom: 1rem" />



    <el-card v-if="routePreview.length" header="路径组预览（启发式）" shadow="never" style="margin-bottom: 1rem">

      <el-alert

        v-for="(w, i) in routePreviewWarnings"

        :key="i"

        :title="w"

        type="warning"

        show-icon

        :closable="false"

        style="margin-bottom: 0.5rem"

      />

      <el-table :data="routePreview" size="small" border>

        <el-table-column prop="code" label="组编码" width="120" />

        <el-table-column label="节点">

          <template #default="{ row }">

            {{ row.nodeIds.map((id: string) => nodeLabel(id)).join(' → ') }}

          </template>

        </el-table-column>

      </el-table>

    </el-card>

  </div>

</template>



<style scoped>

.toolbar { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem; align-items: center; }

.hint { color: #666; font-size: 0.9rem; margin-bottom: 0.75rem; }

.topology-alert { margin-bottom: 0.5rem; }

.device-palette { display: flex; flex-wrap: wrap; gap: 0.5rem; }

.palette-item {

  display: flex;

  align-items: center;

  gap: 0.35rem;

  padding: 0.35rem 0.65rem;

  border: 1px solid #d5dde6;

  border-radius: 6px;

  cursor: grab;

  user-select: none;

  font-size: 0.9rem;

}

.palette-item.active {

  border-color: #e76f51;

  background: #fff5f3;

}

.swatch {

  width: 14px;

  height: 14px;

  border-radius: 3px;

  border: 2px solid;

  flex-shrink: 0;

}

</style>

