<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysWorkFlowTable.listTitle') }}</span>
          <el-radio-group v-model="scope" @change="onScopeChange">
            <el-radio-button value="todo">{{ t('sysWorkFlowTable.tabTodo') }}</el-radio-button>
            <el-radio-button value="done">{{ t('sysWorkFlowTable.tabDone') }}</el-radio-button>
            <el-radio-button value="all">{{ t('sysWorkFlowTable.tabAll') }}</el-radio-button>
          </el-radio-group>
        </div>
      </template>

      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="workFlowTable_Id" :label="t('sysWorkFlowTable.colId')" width="90" />
        <el-table-column prop="workFlow_Id" :label="t('sysWorkFlowTable.colFlowId')" width="90" />
        <el-table-column prop="workTable" :label="t('sysWorkFlow.colTable')" min-width="120" />
        <el-table-column prop="workTableKey" :label="t('sysWorkFlowTable.colBizKey')" width="120" />
        <el-table-column :label="t('sysWorkFlowTable.colStatus')" width="110">
          <template #default="{ row }">{{ auditStatusLabel(row.auditStatus as number) }}</template>
        </el-table-column>
        <el-table-column prop="createDate" :label="t('sysWorkFlowTable.colTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.createDate as string) }}</template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlowTable.colActions')" width="260" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="scope === 'todo' && canAudit(row)"
              v-permission="'Sys_WorkFlowTable.Audit'"
              link
              type="primary"
              :icon="ActionIcons.confirm"
              @click="openAudit(row, 1)"
            >
              {{ t('sysWorkFlowTable.approve') }}
            </el-button>
            <el-button
              v-if="scope === 'todo' && canAudit(row)"
              v-permission="'Sys_WorkFlowTable.Audit'"
              link
              type="warning"
              @click="openAudit(row, 4)"
            >
              {{ t('sysWorkFlowTable.reject') }}
            </el-button>
            <el-button
              v-if="scope === 'todo' && canAudit(row)"
              v-permission="'Sys_WorkFlowTable.Audit'"
              link
              type="danger"
              @click="openAudit(row, 3)"
            >
              {{ t('sysWorkFlowTable.refuse') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rows"
        :total="total"
        @change="loadData"
        style="margin-top: 16px"
      />
    </el-card>

    <el-dialog
      v-model="auditVisible"
      :title="auditDialogTitle"
      width="480px"
    >
      <el-form :model="auditForm" label-width="100px">
        <el-form-item :label="t('sysWorkFlowTable.remark')">
          <el-input v-model="auditForm.remark" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="auditVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.confirm" @click="submitAudit">{{ t('common.confirm') }}</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const scope = ref<'todo' | 'done' | 'all'>('todo')

const auditVisible = ref(false)
const auditForm = reactive({
  workFlowTableId: 0,
  auditStatus: 1 as number,
  remark: '',
})

function formatTime(value?: string) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

function auditStatusLabel(status: number) {
  if (status === 1) return t('sysWorkFlowTable.statusApproved')
  if (status === 2) return t('sysWorkFlowTable.statusInProgress')
  if (status === 3) return t('sysWorkFlowTable.statusRejected')
  if (status === 4) return t('sysWorkFlowTable.statusReturned')
  return t('sysWorkFlowTable.statusPending')
}

function canAudit(row: Record<string, unknown>) {
  const status = row.auditStatus as number
  if (status !== 0 && status !== 2) return false
  return row.currentStepId != null && row.currentStepId !== ''
}

const auditDialogTitle = computed(() => {
  if (auditForm.auditStatus === 1) return t('sysWorkFlowTable.approve')
  if (auditForm.auditStatus === 3) return t('sysWorkFlowTable.refuse')
  return t('sysWorkFlowTable.reject')
})

function buildWheres() {
  if (scope.value === 'all') return undefined
  return JSON.stringify([{ name: 'filter', value: scope.value, displayType: 'equal' }])
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_WorkFlowTable/getPageData', {
      page: page.value,
      rows: rows.value,
      wheres: buildWheres(),
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows ?? []
    }
  } finally {
    loading.value = false
  }
}

function onScopeChange() {
  page.value = 1
  loadData()
}

function openAudit(row: Record<string, unknown>, auditStatus: number) {
  auditForm.workFlowTableId = row.workFlowTable_Id as number
  auditForm.auditStatus = auditStatus
  auditForm.remark = ''
  auditVisible.value = true
}

async function submitAudit() {
  const res = await http.post('/api/Sys_WorkFlow/audit', {
    workFlowTableId: auditForm.workFlowTableId,
    auditStatus: auditForm.auditStatus,
    remark: auditForm.remark,
  })
  if (res.status) {
    ElMessage.success(t('common.success'))
    auditVisible.value = false
    await loadData()
  }
}

onMounted(loadData)
</script>
