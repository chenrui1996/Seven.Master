<script setup lang="ts">
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '../api/http'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()
const log = ref<string[]>([])

const dest = reactive({
  containerCode: 'TP001',
  sourcePointCode: 'RP_IN_01',
  height: 1,
  weight: 1,
  checkResult: 'OK',
})

const feedback = reactive({
  deviceCode: 'SRM01',
  segmentId: '',
  resultCode: 'OK',
  message: 'done',
})

function push(msg: string) {
  log.value.unshift(`${new Date().toLocaleTimeString()} ${msg}`)
}

async function simulateDestination() {
  try {
    await http.post('/api/Wcs/Triggers/destination-request', dest)
    push('SUDR 语义：destination-request 已发送')
    ElMessage.success('已触发目的地申请')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`失败: ${msg}`)
    ElMessage.error('触发失败（需 API 开启 Stacker/Bus，并登录）')
  }
}

async function simulateFeedback() {
  try {
    await http.post('/api/Wcs/Triggers/segment-feedback', {
      ...feedback,
      segmentId: feedback.segmentId || crypto.randomUUID(),
    })
    push('段反馈已发送')
    ElMessage.success('已触发段反馈')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`失败: ${msg}`)
    ElMessage.error('触发失败')
  }
}
</script>

<template>
  <div class="sim-page">
    <h1>③ 仿真测试</h1>
    <p class="hint">
      模式：{{ store.project.meta.runtimeMode }}。单机走现有 Trigger API；流程/调度请配合 Vue3 单据或后续 Player 编排。
    </p>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-card header="单机 · 目的地申请（SUDR）">
          <el-form label-width="110px">
            <el-form-item label="托盘"><el-input v-model="dest.containerCode" /></el-form-item>
            <el-form-item label="申请点"><el-input v-model="dest.sourcePointCode" /></el-form-item>
            <el-form-item label="CheckResult"><el-input v-model="dest.checkResult" /></el-form-item>
            <el-button type="primary" @click="simulateDestination">发送</el-button>
          </el-form>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card header="单机 · 段反馈">
          <el-form label-width="110px">
            <el-form-item label="设备"><el-input v-model="feedback.deviceCode" /></el-form-item>
            <el-form-item label="结果"><el-input v-model="feedback.resultCode" /></el-form-item>
            <el-button type="primary" @click="simulateFeedback">发送</el-button>
          </el-form>
        </el-card>
      </el-col>
    </el-row>

    <el-card header="运行日志" style="margin-top: 1rem">
      <pre class="log">{{ log.join('\n') || '暂无' }}</pre>
    </el-card>
  </div>
</template>

<style scoped>
.log {
  margin: 0;
  max-height: 240px;
  overflow: auto;
  font-size: 12px;
  background: #0f2744;
  color: #d7e3f4;
  padding: 12px;
  border-radius: 6px;
}
</style>
