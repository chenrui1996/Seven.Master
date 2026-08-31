<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../api/http'
import { isLoopbackHost, type SimPromoteDevice } from '../lib/project/schema'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()
const previewLoading = ref(false)
const promoteLoading = ref(false)
const previewWarnings = ref<string[]>([])

function addDevice() {
  store.project.promote.devices.push({
    code: `DEV${store.project.promote.devices.length + 1}`,
    host: '',
    port: 102,
    protocol: 'S7',
  })
}

function removeDevice(index: number) {
  store.project.promote.devices.splice(index, 1)
}

function validateDevices(): SimPromoteDevice[] | null {
  const devices = store.project.promote.devices
  if (!devices.length) {
    ElMessage.warning('请至少添加一台设备')
    return null
  }
  for (const d of devices) {
    if (!d.code.trim()) {
      ElMessage.warning('设备编码不能为空')
      return null
    }
    if (isLoopbackHost(d.host)) {
      ElMessage.error(`禁止环回地址：${d.code} → ${d.host}（127.0.0.1 / localhost / ::1）`)
      return null
    }
  }
  return devices
}

function onHostBlur(row: SimPromoteDevice) {
  if (isLoopbackHost(row.host)) {
    ElMessage.error(`禁止保存环回地址：${row.host}`)
    row.host = ''
  }
}

async function onPreview() {
  const devices = validateDevices()
  if (!devices) return
  previewLoading.value = true
  previewWarnings.value = []
  try {
    const { data } = await http.post<{
      status: boolean
      message?: string
      data?: { projectName: string; devices: SimPromoteDevice[]; warnings: string[] }
    }>('/api/simulation/promote-preview', {
      projectName: store.project.meta.name,
      devices,
    })
    if (!data.status) {
      ElMessage.error(data.message || '预览失败')
      return
    }
    previewWarnings.value = data.data?.warnings ?? []
    const warnText = previewWarnings.value.length ? previewWarnings.value.join('；') : '无警告'
    await ElMessageBox.alert(
      `设备 ${data.data?.devices.length ?? 0} 台\n警告：${warnText}`,
      'Promote 预览',
      { confirmButtonText: '知道了' },
    )
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '预览失败')
  } finally {
    previewLoading.value = false
  }
}

async function onPromote() {
  const devices = validateDevices()
  if (!devices) return
  try {
    await ElMessageBox.confirm('确认 Promote？将写入 CommConnection 并标记部署为 Promoted。', 'Promote', {
      type: 'warning',
    })
  } catch {
    return
  }
  promoteLoading.value = true
  try {
    const { data } = await http.post<{
      status: boolean
      message?: string
      data?: { status: string; commConnectionCount: number }
    }>('/api/simulation/promote', {
      projectName: store.project.meta.name,
      devices,
    })
    if (!data.status) {
      ElMessage.error(data.message || 'Promote 失败')
      return
    }
    store.project.meta.runtimeMode = 'Production'
    ElMessage.success(data.message || 'Promote 成功')
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : 'Promote 失败')
  } finally {
    promoteLoading.value = false
  }
}

function switchSimulation() {
  store.project.meta.runtimeMode = 'Simulation'
  ElMessage.success('已切回 Simulation（本地标记）')
}
</script>

<template>
  <div class="sim-page">
    <h1>④ 切换生产模式</h1>
    <p class="hint">
      当前：<el-tag>{{ store.project.meta.runtimeMode }}</el-tag>
      。仿真身份下禁止只改 IP 冒充上线；须正式 Promote（拒绝 127.0.0.1 / localhost / ::1）。
    </p>

    <el-space wrap>
      <el-button @click="switchSimulation">切回仿真</el-button>
      <el-button :loading="previewLoading" @click="onPreview">Promote 预览</el-button>
      <el-button type="danger" :loading="promoteLoading" @click="onPromote">Promote</el-button>
      <el-button @click="addDevice">添加设备</el-button>
    </el-space>

    <el-table :data="store.project.promote.devices" style="margin-top: 1rem" border empty-text="请添加设备">
      <el-table-column prop="code" label="设备" width="120">
        <template #default="{ row }">
          <el-input v-model="row.code" />
        </template>
      </el-table-column>
      <el-table-column prop="protocol" label="协议" width="100">
        <template #default="{ row }">
          <el-input v-model="row.protocol" />
        </template>
      </el-table-column>
      <el-table-column label="Host">
        <template #default="{ row }">
          <el-input v-model="row.host" placeholder="真机 IP" @blur="onHostBlur(row)" />
        </template>
      </el-table-column>
      <el-table-column label="Port" width="120">
        <template #default="{ row }">
          <el-input-number v-model="row.port" :min="1" :max="65535" />
        </template>
      </el-table-column>
      <el-table-column label="操作" width="80">
        <template #default="{ $index }">
          <el-button link type="danger" @click="removeDevice($index)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-alert
      v-if="previewWarnings.length"
      :title="previewWarnings.join('；')"
      type="warning"
      show-icon
      :closable="false"
      style="margin-top: 1rem"
    />

    <p class="hint" style="margin-top: 1rem">
      上线后请用 <strong>Seven.Vue3</strong> 做单据/库存/联锁验收；本工具专注工程与联调。
    </p>
  </div>
</template>
