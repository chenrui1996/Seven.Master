<script setup lang="ts">
import { reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()

const devices = reactive([
  { code: 'SRM01', host: '192.168.1.10', port: 102, protocol: 'S7' },
  { code: 'CNV01', host: '192.168.1.11', port: 502, protocol: 'Modbus' },
])

function switchProduction() {
  store.project.meta.runtimeMode = 'Production'
  ElMessage.warning('已标记为 Production（本地）。Promote API 将在 Phase S5 接入真机配置。')
}

function switchSimulation() {
  store.project.meta.runtimeMode = 'Simulation'
  ElMessage.success('已切回 Simulation')
}

function promoteHint() {
  ElMessage.info('Promote：去 SIM_ 前缀并写真机地址 — POST /api/simulation/promote（待实现）')
}
</script>

<template>
  <div class="sim-page">
    <h1>④ 切换生产模式</h1>
    <p class="hint">
      当前：<el-tag>{{ store.project.meta.runtimeMode }}</el-tag>
      。仿真身份下禁止只改 IP 冒充上线；须正式 Promote。
    </p>

    <el-space>
      <el-button @click="switchSimulation">切回仿真</el-button>
      <el-button type="warning" @click="switchProduction">标记生产</el-button>
      <el-button type="danger" @click="promoteHint">Promote（占位）</el-button>
    </el-space>

    <el-table :data="devices" style="margin-top: 1rem" border>
      <el-table-column prop="code" label="设备" width="120" />
      <el-table-column prop="protocol" label="协议" width="100" />
      <el-table-column label="Host">
        <template #default="{ row }">
          <el-input v-model="row.host" />
        </template>
      </el-table-column>
      <el-table-column label="Port" width="120">
        <template #default="{ row }">
          <el-input-number v-model="row.port" :min="1" :max="65535" />
        </template>
      </el-table-column>
    </el-table>

    <p class="hint" style="margin-top: 1rem">
      上线后请用 <strong>Seven.Vue3</strong> 做单据/库存/联锁验收；本工具专注工程与联调。
    </p>
  </div>
</template>
