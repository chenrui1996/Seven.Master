<script setup lang="ts">
import { ElMessage } from 'element-plus'
import http from '../api/http'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()
const f = store.project.meta.features

function onSave() {
  if (f.wcsPacks.fourWay && !f.hotStore) {
    ElMessage.warning('四向车建议同时开启 HotStore')
  }
  ElMessage.success('Features 已写入工程（本地持久化）')
}

async function validateOnServer() {
  try {
    const { data } = await http.post<{
      status: boolean
      message?: string
      data?: { ok: boolean; errors: string[]; warnings: string[] }
    }>('/api/simulation/projects/validate-features', f)
    if (!data.status) {
      ElMessage.error(data.message || '校验失败')
      return
    }
    const r = data.data
    if (r?.warnings?.length) ElMessage.warning(r.warnings.join('；'))
    ElMessage.success(r?.ok ? '服务端校验通过' : '校验未通过')
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '无法连接 API（需 Features.Simulator=true）')
  }
}

function copySnippet() {
  navigator.clipboard.writeText(store.exportAppsettingsSnippet())
  ElMessage.success('已复制 appsettings 片段')
}
</script>

<template>
  <div class="sim-page">
    <h1>① 选择 Features 并构建</h1>
    <p class="hint">勾选本项目启用的能力组合，导出到 appsettings 或调用服务端校验。</p>

    <el-form label-width="140px" style="max-width: 520px">
      <el-form-item label="工程名称">
        <el-input v-model="store.project.meta.name" />
      </el-form-item>
      <el-form-item label="Wms">
        <el-switch v-model="f.wms" />
      </el-form-item>
      <el-form-item label="OrchestrationBus">
        <el-switch v-model="f.orchestrationBus" />
      </el-form-item>
      <el-form-item label="HotStore">
        <el-switch v-model="f.hotStore" />
      </el-form-item>
      <el-form-item label="DeviceComm">
        <el-switch v-model="f.deviceComm" />
      </el-form-item>
      <el-form-item label="Simulator">
        <el-switch v-model="f.simulator" />
      </el-form-item>
      <el-divider>WCS 包</el-divider>
      <el-form-item label="Stacker">
        <el-switch v-model="f.wcsPacks.stacker" />
      </el-form-item>
      <el-form-item label="FourWay">
        <el-switch v-model="f.wcsPacks.fourWay" />
      </el-form-item>
      <el-form-item label="BoxSort">
        <el-switch v-model="f.wcsPacks.boxSort" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="onSave">保存到工程</el-button>
        <el-button @click="validateOnServer">服务端校验</el-button>
        <el-button @click="copySnippet">复制 appsettings 片段</el-button>
        <el-button @click="store.downloadProject()">导出 .sevenproj.json</el-button>
      </el-form-item>
    </el-form>

    <el-input :model-value="store.exportAppsettingsSnippet()" type="textarea" :rows="14" readonly />
  </div>
</template>
