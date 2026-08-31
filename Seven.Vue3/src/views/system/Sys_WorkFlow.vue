<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysWorkFlow.defListTitle') }}</span>
          <div class="toolbar-actions">
            <el-button
              v-permission="'Sys_WorkFlow.Delete'"
              size="small"
              type="danger"
              plain
              :icon="ActionIcons.batchDelete"
              :disabled="!selectedIds.length"
              @click="batchRemove"
            >
              {{ t('common.batchDelete') }}
            </el-button>
            <el-button v-permission="'Sys_WorkFlow.Add'" type="primary" :icon="ActionIcons.add" @click="openForm()">
              {{ t('common.add') }}
            </el-button>
          </div>
        </div>
      </template>

      <el-table :data="definitions" v-loading="loading" border @selection-change="onSelectionChange">
        <el-table-column type="selection" width="48" />
        <el-table-column prop="workFlow_Id" :label="t('sysWorkFlow.colId')" width="80" />
        <el-table-column prop="workName" :label="t('sysWorkFlow.colName')" min-width="160" />
        <el-table-column prop="workTable" :label="t('sysWorkFlow.colTable')" min-width="140" />
        <el-table-column prop="workTableKey" :label="t('sysWorkFlow.colTableKey')" width="120" />
        <el-table-column prop="stepCount" :label="t('sysWorkFlow.colStepCount')" width="90" align="right" />
        <el-table-column :label="t('sysWorkFlow.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlow.colActions')" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-permission="'Sys_WorkFlow.Update'" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">
              {{ t('sysWorkFlow.edit') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      :title="form.workFlow_Id ? t('sysWorkFlow.editFlow') : t('sysWorkFlow.addFlow')"
      width="920px"
      destroy-on-close
      top="4vh"
    >
      <el-form :model="form" label-width="110px" class="flow-meta">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item :label="t('sysWorkFlow.colName')" required>
              <el-input v-model="form.workName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('sysWorkFlow.workTableName')">
              <el-input v-model="form.workTableName" :placeholder="t('sysWorkFlow.workTableNameHint')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('sysWorkFlow.colTable')" required>
              <el-input v-model="form.workTable" placeholder="WmsInboundOrder" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('sysWorkFlow.colTableKey')">
              <el-input v-model="form.workTableKey" placeholder="Id" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('sysWorkFlow.weight')">
              <el-input-number v-model="form.weight" :min="0" controls-position="right" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('sysWorkFlow.colStatus')">
              <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('sysWorkFlow.auditingEdit')">
              <el-switch v-model="form.auditingEdit" :active-value="1" :inactive-value="0" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item :label="t('sysWorkFlow.remark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <WorkflowDesigner
        v-model="form.steps"
        @update:node-config="(v) => (form.nodeConfig = v)"
        @update:line-config="(v) => (form.lineConfig = v)"
      />

      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button
            v-permission="['Sys_WorkFlow.Add', 'Sys_WorkFlow.Update']"
            type="primary"
            :icon="ActionIcons.save"
            @click="save"
          >
            {{ t('common.save') }}
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'
import WorkflowDesigner, { type DesignerStep } from '../../components/workflow/WorkflowDesigner.vue'

interface DefinitionRow {
  workFlow_Id: number
  workName: string
  workTable?: string
  workTableKey?: string
  enable?: number
  stepCount?: number
}

const { t } = useI18n()
const loading = ref(false)
const definitions = ref<DefinitionRow[]>([])
const selectedIds = ref<number[]>([])
const dialogVisible = ref(false)

const form = reactive({
  workFlow_Id: 0,
  workName: '',
  workTable: '',
  workTableName: '',
  workTableKey: 'Id',
  weight: 0 as number | undefined,
  enable: 1 as number,
  auditingEdit: 0 as number,
  remark: '',
  nodeConfig: '',
  lineConfig: '',
  steps: [] as DesignerStep[],
})

function newStep(name?: string): DesignerStep {
  return {
    stepId: Math.random().toString(36).slice(2, 10),
    stepName: name || t('sysWorkFlow.defaultStepName'),
    stepOrder: 1,
    stepType: 1,
    stepValue: '',
    stepAttrType: 'node',
    sendMail: 0,
    remark: '',
  }
}

async function loadDefinitions() {
  loading.value = true
  try {
    const res = await http.post('/api/Sys_WorkFlow/listDefinitions', {})
    if (res.status && res.data) {
      definitions.value = res.data as DefinitionRow[]
    }
  } finally {
    loading.value = false
  }
}

function onSelectionChange(rows: DefinitionRow[]) {
  selectedIds.value = rows.map((r) => r.workFlow_Id)
}

async function openForm(row?: DefinitionRow) {
  if (!row) {
    Object.assign(form, {
      workFlow_Id: 0,
      workName: '',
      workTable: '',
      workTableName: '',
      workTableKey: 'Id',
      weight: 0,
      enable: 1,
      auditingEdit: 0,
      remark: '',
      nodeConfig: '',
      lineConfig: '',
      steps: [newStep()],
    })
    dialogVisible.value = true
    return
  }
  const res = await http.post('/api/Sys_WorkFlow/getDefinition', { workFlowId: row.workFlow_Id })
  if (res.status && res.data) {
    const data = res.data as {
      workFlow_Id: number
      workName: string
      workTable?: string
      workTableName?: string
      workTableKey?: string
      weight?: number
      enable?: number
      auditingEdit?: number
      remark?: string
      nodeConfig?: string
      lineConfig?: string
      steps?: {
        stepId?: string
        stepName: string
        stepOrder: number
        stepType?: number
        stepValue?: string
        stepAttrType?: string
        sendMail?: number
        remark?: string
      }[]
    }
    form.workFlow_Id = data.workFlow_Id
    form.workName = data.workName
    form.workTable = data.workTable ?? ''
    form.workTableName = data.workTableName ?? ''
    form.workTableKey = data.workTableKey ?? 'Id'
    form.weight = data.weight ?? 0
    form.enable = data.enable ?? 1
    form.auditingEdit = data.auditingEdit ?? 0
    form.remark = data.remark ?? ''
    form.nodeConfig = data.nodeConfig ?? ''
    form.lineConfig = data.lineConfig ?? ''
    form.steps = (data.steps ?? []).map((s, i) => ({
      stepId: s.stepId || Math.random().toString(36).slice(2, 10),
      stepName: s.stepName,
      stepOrder: s.stepOrder || i + 1,
      stepType: s.stepType ?? 1,
      stepValue: s.stepValue ?? '',
      stepAttrType: s.stepAttrType || 'node',
      auditMethod: (s as { auditMethod?: number }).auditMethod ?? 0,
      filters: (s as { filters?: string }).filters ?? '',
      sendMail: s.sendMail ?? 0,
      remark: s.remark ?? '',
    }))
    if (!form.steps.length) form.steps = [newStep()]
    dialogVisible.value = true
  }
}

async function save() {
  if (!form.workName.trim() || !form.workTable.trim()) {
    ElMessage.warning(t('sysWorkFlow.requiredFields'))
    return
  }
  const payload = {
    workFlow_Id: form.workFlow_Id,
    workName: form.workName.trim(),
    workTable: form.workTable.trim(),
    workTableName: form.workTableName.trim() || undefined,
    workTableKey: form.workTableKey.trim() || 'Id',
    weight: form.weight,
    enable: form.enable,
    auditingEdit: form.auditingEdit,
    remark: form.remark,
    nodeConfig: form.nodeConfig,
    lineConfig: form.lineConfig,
    steps: form.steps.map((s, i) => ({
      stepId: s.stepId,
      stepName: s.stepName,
      stepOrder: i + 1,
      stepType: s.stepType,
      stepValue: s.stepValue,
      stepAttrType: s.stepAttrType || 'node',
      nextStepIds: s.nextStepIds,
      auditMethod: s.auditMethod ?? 0,
      filters: s.filters || undefined,
      sendMail: s.sendMail,
      remark: s.remark,
    })),
  }
  const res = await http.post('/api/Sys_WorkFlow/saveDefinition', payload)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadDefinitions()
  }
}

async function batchRemove() {
  if (!selectedIds.value.length) return
  await ElMessageBox.confirm(t('common.batchDeleteConfirm'), t('common.batchDelete'), { type: 'warning' })
  const res = await http.post('/api/Sys_WorkFlow/delete', { ids: selectedIds.value })
  if (res.status) {
    ElMessage.success(t('common.success'))
    selectedIds.value = []
    await loadDefinitions()
  }
}

onMounted(loadDefinitions)
</script>

<style scoped>
.flow-meta {
  margin-bottom: 8px;
}
</style>
