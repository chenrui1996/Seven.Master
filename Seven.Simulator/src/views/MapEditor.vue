<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '../api/http'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()
const code = ref('')
const x = ref(40)
const y = ref(40)
const deploying = ref(false)
const lastDeploy = ref('')

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

function clearMap() {
  store.project.map.nodes = []
  store.project.map.edges = []
  store.project.map.devices = []
}

async function onImport(file: File) {
  await store.importProject(file)
  ElMessage.success('工程已导入')
  return false
}

async function deploy() {
  if (!store.project.map.nodes.length) {
    ElMessage.warning('请先添加节点')
    return
  }
  deploying.value = true
  try {
    const { data } = await http.post<{
      status: boolean
      message?: string
      data?: { warehouseCode: string; message: string; locationCount: number }
    }>('/api/simulation/deploy', {
      version: store.project.version,
      meta: store.project.meta,
      map: store.project.map,
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
        <el-button>导入工程 JSON</el-button>
      </el-upload>
      <el-button type="success" :loading="deploying" @click="deploy">Deploy</el-button>
      <el-button @click="undeploy">Undeploy</el-button>
    </div>

    <el-alert v-if="lastDeploy" :title="lastDeploy" type="success" show-icon :closable="false" style="margin-bottom: 1rem" />

    <div class="canvas">
      <div
        v-for="n in store.project.map.nodes"
        :key="n.id"
        class="node"
        :style="{ left: n.x + 'px', top: n.y + 'px' }"
      >
        {{ n.code }}
      </div>
      <el-empty v-if="!store.project.map.nodes.length" description="添加节点或导入工程" />
    </div>
  </div>
</template>

<style scoped>
.toolbar { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem; align-items: center; }
.canvas {
  position: relative;
  min-height: 360px;
  background: linear-gradient(180deg, #f7f9fb, #eef2f6);
  border: 1px solid #d5dde6;
  border-radius: 8px;
}
.node {
  position: absolute;
  min-width: 64px;
  padding: 6px 10px;
  background: #0f2744;
  color: #fff;
  border-radius: 4px;
  font-size: 12px;
  text-align: center;
}
</style>
