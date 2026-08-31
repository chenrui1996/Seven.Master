<template>
  <el-drawer v-model="visible" :title="t('sysWorkFlow.auditProgress')" size="480px" destroy-on-close @closed="onClosed">
    <div v-loading="loading">
      <template v-if="payload?.hasFlow">
        <el-descriptions :column="1" border size="small" class="mb12">
          <el-descriptions-item :label="t('sysWorkFlowTable.colStatus')">
            {{ statusLabel(payload.auditStatus) }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('sysWorkFlowTable.colId')">
            {{ payload.workFlowTableId }}
          </el-descriptions-item>
        </el-descriptions>

        <el-timeline>
          <el-timeline-item
            v-for="step in payload.list || []"
            :key="step.workStepFlow_Id"
            :type="timelineType(step)"
            :hollow="!step.isCurrent"
          >
            <div class="step-title">
              {{ step.stepName }}
              <el-tag v-if="step.isCurrent" size="small" type="warning">{{ t('sysWorkFlow.currentStep') }}</el-tag>
            </div>
            <div class="step-meta">{{ statusLabel(step.auditStatus) }}</div>
            <div v-if="step.auditor" class="step-meta">{{ step.auditor }} · {{ formatDate(step.auditDate) }}</div>
            <div v-if="step.remark" class="step-remark">{{ step.remark }}</div>
          </el-timeline-item>
        </el-timeline>

        <el-divider />
        <div class="log-title">{{ t('sysWorkFlow.auditLog') }}</div>
        <el-table :data="payload.log || []" size="small" border>
          <el-table-column prop="auditUser" :label="t('sysWorkFlow.auditor')" width="100" />
          <el-table-column :label="t('sysWorkFlowTable.colStatus')" width="90">
            <template #default="{ row }">{{ statusLabel(row.auditStatus) }}</template>
          </el-table-column>
          <el-table-column prop="remark" :label="t('sysWorkFlow.remark')" min-width="120" />
          <el-table-column :label="t('sysWorkFlow.auditTime')" width="160">
            <template #default="{ row }">{{ formatDate(row.createDate) }}</template>
          </el-table-column>
        </el-table>

        <div v-if="canAudit" class="audit-actions">
          <el-input v-model="remark" type="textarea" :rows="2" :placeholder="t('sysWorkFlow.auditRemark')" />
          <div class="audit-btns">
            <el-button type="success" @click="doAudit(1)">{{ t('sysWorkFlow.approve') }}</el-button>
            <el-button type="warning" @click="doAudit(4)">{{ t('sysWorkFlow.return') }}</el-button>
            <el-button type="danger" @click="doAudit(3)">{{ t('sysWorkFlow.reject') }}</el-button>
          </div>
        </div>
      </template>
      <el-empty v-else :description="t('sysWorkFlow.noFlowInstance')" />
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import { useUserStore } from '../../stores/user'

interface StepItem {
  workStepFlow_Id: number
  stepName: string
  auditStatus?: number | null
  auditor?: string
  auditDate?: string
  remark?: string
  isCurrent?: boolean
}

interface StepsPayload {
  hasFlow: boolean
  workFlowTableId?: number
  auditStatus?: number
  list?: StepItem[]
  log?: { auditUser?: string; auditStatus: number; remark?: string; createDate?: string }[]
}

const props = defineProps<{
  modelValue: boolean
  tableName: string
  tableKey: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  audited: []
}>()

const { t } = useI18n()
const userStore = useUserStore()
const loading = ref(false)
const payload = ref<StepsPayload | null>(null)
const remark = ref('')

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v),
})

const canAudit = computed(
  () =>
    !!payload.value?.hasFlow &&
    userStore.hasPermission('Sys_WorkFlow.Audit') &&
    (payload.value?.auditStatus === 0 || payload.value?.auditStatus === 2),
)

function statusLabel(status?: number | null) {
  const map: Record<number, string> = {
    0: t('sysWorkFlow.statusPending'),
    1: t('sysWorkFlow.statusApproved'),
    2: t('sysWorkFlow.statusInProgress'),
    3: t('sysWorkFlow.statusRejected'),
    4: t('sysWorkFlow.statusReturned'),
  }
  return status == null ? '-' : map[status] ?? String(status)
}

function timelineType(step: StepItem) {
  if (step.isCurrent) return 'primary'
  if (step.auditStatus === 1) return 'success'
  if (step.auditStatus === 3 || step.auditStatus === 4) return 'danger'
  return 'info'
}

function formatDate(v?: string) {
  if (!v) return ''
  return String(v).replace('T', ' ').slice(0, 19)
}

async function load() {
  if (!props.tableName || !props.tableKey) {
    payload.value = { hasFlow: false }
    return
  }
  loading.value = true
  try {
    const res = await http.post('/api/Sys_WorkFlow/getSteps', {
      tableName: props.tableName,
      ids: [props.tableKey],
    })
    payload.value = (res.data as StepsPayload) || { hasFlow: false }
  } finally {
    loading.value = false
  }
}

async function doAudit(auditStatus: number) {
  if (!payload.value?.workFlowTableId) return
  const res = await http.post('/api/Sys_WorkFlow/audit', {
    workFlowTableId: payload.value.workFlowTableId,
    auditStatus,
    remark: remark.value,
  })
  if (res.status) {
    ElMessage.success(res.message || t('common.success'))
    emit('audited')
    await load()
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

function onClosed() {
  payload.value = null
  remark.value = ''
}

watch(
  () => [props.modelValue, props.tableName, props.tableKey] as const,
  ([open]) => {
    if (open) void load()
  },
)
</script>

<style scoped>
.mb12 {
  margin-bottom: 12px;
}
.step-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
}
.step-meta {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}
.step-remark {
  margin-top: 4px;
  font-size: 13px;
}
.log-title {
  font-weight: 600;
  margin-bottom: 8px;
}
.audit-actions {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.audit-btns {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
</style>
