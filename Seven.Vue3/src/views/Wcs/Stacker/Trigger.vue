<template>
  <div class="seven-page stacker-trigger">
    <div class="toolbar">
      <el-tag type="warning" v-if="!simEnabled">仿真未启用（需 Stacker 或 OrchestrationBus）</el-tag>
    </div>

    <el-card header="目的地申请 (SUDR)" style="max-width: 640px; margin-bottom: 16px">
      <el-form label-width="120px">
        <el-form-item label="容器编码">
          <el-input v-model="destForm.containerCode" />
        </el-form-item>
        <el-form-item label="源点位">
          <el-input v-model="destForm.sourcePointCode" />
        </el-form-item>
        <el-form-item label="高度">
          <el-input-number v-model="destForm.height" :min="0" />
        </el-form-item>
        <el-form-item label="重量">
          <el-input-number v-model="destForm.weight" :min="0" />
        </el-form-item>
        <el-form-item label="校验结果">
          <el-input v-model="destForm.checkResult" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :disabled="!simEnabled" @click="submitDest">提交目的地申请</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card header="段反馈 (SUMR/SUPR)" style="max-width: 640px">
      <el-form label-width="120px">
        <el-form-item label="容器编码">
          <el-input v-model="segForm.containerCode" />
        </el-form-item>
        <el-form-item label="段点位">
          <el-input v-model="segForm.segmentPointCode" />
        </el-form-item>
        <el-form-item label="反馈码">
          <el-input v-model="segForm.feedbackCode" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :disabled="!simEnabled" @click="submitSeg">提交段反馈</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '../../../api/http'
import { useFeatureStore } from '../../../stores/features'

const featureStore = useFeatureStore()

const simEnabled = computed(
  () => !!featureStore.flags.wcsPacks.stacker || !!featureStore.flags.orchestrationBus,
)

const destForm = ref({
  containerCode: '',
  sourcePointCode: '',
  height: 0,
  weight: 0,
  checkResult: 'OK',
})

const segForm = ref({
  containerCode: '',
  segmentPointCode: '',
  feedbackCode: 'DONE',
})

async function submitDest() {
  if (!simEnabled.value) return
  const res = await http.post<{ status: boolean; message?: string }>(
    '/api/Wcs/Triggers/destination-request',
    destForm.value,
  )
  if (res.status) ElMessage.success(res.message || '已提交')
  else ElMessage.error(res.message || '提交失败')
}

async function submitSeg() {
  if (!simEnabled.value) return
  const res = await http.post<{ status: boolean; message?: string }>(
    '/api/Wcs/Triggers/segment-feedback',
    segForm.value,
  )
  if (res.status) ElMessage.success(res.message || '已提交')
  else ElMessage.error(res.message || '提交失败')
}
</script>

<style scoped>
.stacker-trigger .toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
</style>
