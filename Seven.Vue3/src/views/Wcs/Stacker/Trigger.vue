<template>
  <div class="wcs-ops seven-page">
    <div class="ops-header">
      <div class="ops-heading">
        <h1 class="ops-title">{{ t('wcsOps.stackerTrigger.title') }}</h1>
        <p class="ops-sub">{{ t('wcsOps.stackerTrigger.subtitle') }}</p>
      </div>
    </div>

    <div class="ops-status-grid">
      <div class="ops-status-card" :class="destBusy ? 'ops-status-card--warning' : 'ops-status-card--success'">
        <div class="ops-status-card__label">{{ t('wcsOps.stackerTrigger.destTitle') }}</div>
        <div class="ops-status-card__value ops-mono">{{ destForm.sourcePointCode || t('wcsOps.stackerTrigger.sourcePlaceholder') }}</div>
        <div class="ops-status-card__meta">{{ t(destBusy ? 'wcsOps.stackerTrigger.submittingDestMeta' : 'wcsOps.stackerTrigger.readyDestMeta') }}</div>
      </div>
      <div class="ops-status-card" :class="segBusy ? 'ops-status-card--warning' : 'ops-status-card--success'">
        <div class="ops-status-card__label">{{ t('wcsOps.stackerTrigger.segTitle') }}</div>
        <div class="ops-status-card__value ops-mono">{{ segForm.segmentPointCode || '—' }}</div>
        <div class="ops-status-card__meta">{{ t(segBusy ? 'wcsOps.stackerTrigger.submittingSegMeta' : 'wcsOps.stackerTrigger.readySegMeta') }}</div>
      </div>
    </div>

    <div class="ops-grid">
      <div class="ops-panel">
        <h3>{{ t('wcsOps.stackerTrigger.destTitle') }}</h3>
        <el-form label-width="120px">
          <el-form-item :label="t('wcsOps.stackerTrigger.container')">
            <el-input v-model="destForm.containerCode" class="ops-mono" />
          </el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.sourcePoint')">
            <el-input
              v-model="destForm.sourcePointCode"
              class="ops-mono"
              :placeholder="t('wcsOps.stackerTrigger.sourcePlaceholder')"
            />
          </el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.height')"><el-input-number v-model="destForm.height" :min="0" /></el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.weight')"><el-input-number v-model="destForm.weight" :min="0" /></el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.checkResult')">
            <el-select v-model="destForm.checkResult" style="width: 160px">
              <el-option label="OK" value="OK" />
              <el-option label="NG" value="NG" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="destBusy" @click="submitDest">{{ t('wcsOps.stackerTrigger.submitDest') }}</el-button>
          </el-form-item>
        </el-form>
      </div>

      <div class="ops-panel">
        <h3>{{ t('wcsOps.stackerTrigger.segTitle') }}</h3>
        <el-form label-width="120px">
          <el-form-item :label="t('wcsOps.stackerTrigger.container')">
            <el-input v-model="segForm.containerCode" class="ops-mono" />
          </el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.segmentPoint')">
            <el-input v-model="segForm.segmentPointCode" class="ops-mono" />
          </el-form-item>
          <el-form-item :label="t('wcsOps.stackerTrigger.feedback')">
            <el-select v-model="segForm.feedbackCode" style="width: 160px">
              <el-option label="DONE / OK" value="DONE" />
              <el-option label="NG" value="NG" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="segBusy" @click="submitSeg">{{ t('wcsOps.stackerTrigger.submitSeg') }}</el-button>
          </el-form-item>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../../api/http'
import '../../../styles/wcs-ops.css'

const { t } = useI18n()

const destBusy = ref(false)
const segBusy = ref(false)

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
  destBusy.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>(
      '/api/Wcs/Triggers/destination-request',
      destForm.value,
    )
    if (res.status) ElMessage.success(res.message || t('wcsOps.stackerTrigger.submitted'))
    else ElMessage.error(res.message || t('wcsOps.stackerTrigger.failed'))
  } finally {
    destBusy.value = false
  }
}

async function submitSeg() {
  segBusy.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>(
      '/api/Wcs/Triggers/segment-feedback',
      segForm.value,
    )
    if (res.status) ElMessage.success(res.message || t('wcsOps.stackerTrigger.submitted'))
    else ElMessage.error(res.message || t('wcsOps.stackerTrigger.failed'))
  } finally {
    segBusy.value = false
  }
}
</script>
