<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.fwInbound.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.fwInbound.subtitle') }}</p>

    <div class="ops-panel" style="max-width: 640px">
      <el-form label-width="110px">
        <el-form-item :label="t('wcsOps.fwInbound.containerCode')">
          <el-input v-model="form.containerCode" class="ops-mono" />
        </el-form-item>
        <el-form-item :label="t('wcsOps.fwInbound.materialName')">
          <el-input v-model="form.materialName" />
        </el-form-item>
        <el-form-item :label="t('wcsOps.fwInbound.quantity')">
          <el-input-number v-model="form.quantity" :min="0.001" />
        </el-form-item>
        <el-form-item :label="t('wcsOps.fwInbound.gateway')">
          <el-select v-model="form.gatewayCode" filterable allow-create style="width: 100%">
            <el-option v-for="g in meta?.gateways || []" :key="g.code" :label="g.code" :value="g.code" />
            <el-option
              v-for="h in meta?.handovers || []"
              :key="'h' + h.code"
              :label="h.code + t('wcsOps.fwInbound.handoverSuffix')"
              :value="h.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('wcsOps.fwInbound.strategy')">
          <el-radio-group v-model="form.strategy">
            <el-radio-button value="auto">{{ t('wcsOps.fwInbound.strategyAuto') }}</el-radio-button>
            <el-radio-button value="layer">{{ t('wcsOps.fwInbound.strategyLayer') }}</el-radio-button>
            <el-radio-button value="location">{{ t('wcsOps.fwInbound.strategyLocation') }}</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="form.strategy === 'layer'" :label="t('wcsOps.fwInbound.layer')">
          <el-select v-model="form.layerCode" style="width: 100%">
            <el-option v-for="l in meta?.layers || []" :key="l.code" :label="l.name || l.code" :value="l.code" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="form.strategy === 'location'" :label="t('wcsOps.fwInbound.location')">
          <el-input v-model="form.locationCode" class="ops-mono" />
          <el-button link type="primary" @click="loadPickable">{{ t('wcsOps.fwInbound.loadPickable') }}</el-button>
        </el-form-item>
        <el-form-item :label="t('wcsOps.fwInbound.syncWms')"><el-switch v-model="form.syncWms" /></el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="busy" :disabled="!meta?.canAcceptLegs" @click="submit">
            {{ t('wcsOps.fwInbound.submit') }}
          </el-button>
          <el-tag v-if="meta && !meta.canAcceptLegs" type="danger" style="margin-left: 8px">
            {{ t('wcsOps.fwInbound.interlockBlocked') }}
          </el-tag>
        </el-form-item>
      </el-form>
    </div>

    <el-dialog v-model="showPick" :title="t('wcsOps.fwInbound.pickableTitle')" width="720px">
      <el-table :data="pickable" size="small" height="360" @row-click="pickLoc">
        <el-table-column prop="code" :label="t('wcsOps.common.code')" min-width="140" />
        <el-table-column prop="pickable" :label="t('wcsOps.fwInbound.pickable')" width="70">
          <template #default="{ row }">
            <el-tag :type="row.pickable ? 'success' : 'info'" size="small">
              {{ row.pickable ? t('wcsOps.common.yes') : t('wcsOps.common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="pickBlockReason" :label="t('wcsOps.fwInbound.reason')" width="100" />
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../../../api/http'
import '../../../../styles/wcs-ops.css'

const { t } = useI18n()
const meta = ref<any>(null)
const busy = ref(false)
const showPick = ref(false)
const pickable = ref<any[]>([])
const form = reactive({
  containerCode: '',
  materialName: '',
  quantity: 1,
  gatewayCode: '',
  strategy: 'auto',
  layerCode: '',
  locationCode: '',
  syncWms: false,
})

async function loadMeta() {
  const res = await http.get<{ status: boolean; data?: any }>('/api/Wcs/FourWay/Ops/meta')
  if (res.status) meta.value = res.data
}

async function loadPickable() {
  const res = await http.get<{ status: boolean; data?: any[] }>(
    `/api/Wcs/FourWay/Ops/inbound/pickable-map?layerCode=${encodeURIComponent(form.layerCode || '')}`,
  )
  if (res.status) {
    pickable.value = res.data ?? []
    showPick.value = true
  }
}

function pickLoc(row: any) {
  if (!row.pickable) {
    ElMessage.warning(row.pickBlockReason || t('wcsOps.fwInbound.notPickable'))
    return
  }
  form.locationCode = row.code
  showPick.value = false
}

async function submit() {
  busy.value = true
  try {
    const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/inbound', { ...form })
    if (res.status) ElMessage.success(res.message || t('wcsOps.common.created'))
    else ElMessage.error(res.message || t('wcsOps.common.failed'))
  } finally {
    busy.value = false
  }
}

onMounted(loadMeta)
</script>
