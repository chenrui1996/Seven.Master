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

    <el-dialog v-model="dialogVisible" :title="form.workFlow_Id ? t('sysWorkFlow.editFlow') : t('sysWorkFlow.addFlow')" width="760px">
      <el-form :model="form" label-width="110px">
        <el-form-item :label="t('sysWorkFlow.colName')" required>
          <el-input v-model="form.workName" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.colTable')" required>
          <el-input v-model="form.workTable" placeholder="Device" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.colTableKey')">
          <el-input v-model="form.workTableKey" placeholder="Id" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.colStatus')">
          <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>

      <div class="steps-toolbar">
        <span>{{ t('sysWorkFlow.stepsTitle') }}</span>
        <el-button size="small" type="primary" :icon="ActionIcons.add" @click="addStep">{{ t('sysWorkFlow.addStep') }}</el-button>
      </div>
      <el-table :data="form.steps" border size="small">
        <el-table-column :label="t('sysWorkFlow.stepOrder')" width="70">
          <template #default="{ $index }">{{ $index + 1 }}</template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlow.stepName')" min-width="140">
          <template #default="{ row }">
            <el-input v-model="row.stepName" size="small" />
          </template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlow.stepType')" width="140">
          <template #default="{ row }">
            <el-select v-model="row.stepType" size="small" style="width: 100%">
              <el-option v-for="opt in stepTypeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlow.stepValue')" min-width="160">
          <template #default="{ row }">
            <el-input v-model="row.stepValue" size="small" :placeholder="t('sysWorkFlow.stepValueHint')" />
          </template>
        </el-table-column>
        <el-table-column :label="t('sysWorkFlow.colActions')" width="80">
          <template #default="{ $index }">
            <el-button link type="danger" :icon="ActionIcons.delete" @click="removeStep($index)" />
          </template>
        </el-table-column>
      </el-table>

      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button v-permission="['Sys_WorkFlow.Add', 'Sys_WorkFlow.Update']" type="primary" :icon="ActionIcons.save" @click="save">{{ t('common.save') }}</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface DefinitionRow {
  workFlow_Id: number
  workName: string
  workTable?: string
  workTableKey?: string
  enable?: number
  stepCount?: number
}

interface StepForm {
  stepName: string
  stepOrder: number
  stepType: number
  stepValue: string
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
  workTableKey: 'Id',
  enable: 1 as number,
  steps: [] as StepForm[],
})

const stepTypeOptions = computed(() => [
  { value: 1, label: t('sysWorkFlow.stepTypeRole') },
  { value: 2, label: t('sysWorkFlow.stepTypeUser') },
  { value: 3, label: t('sysWorkFlow.stepTypeDept') },
])

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

function defaultSteps(): StepForm[] {
  return [{ stepName: t('sysWorkFlow.defaultStepName'), stepOrder: 1, stepType: 1, stepValue: '' }]
}

async function openForm(row?: DefinitionRow) {
  if (!row) {
    Object.assign(form, {
      workFlow_Id: 0,
      workName: '',
      workTable: '',
      workTableKey: 'Id',
      enable: 1,
      steps: defaultSteps(),
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
      workTableKey?: string
      enable?: number
      steps?: { stepName: string; stepOrder: number; stepType?: number; stepValue?: string }[]
    }
    form.workFlow_Id = data.workFlow_Id
    form.workName = data.workName
    form.workTable = data.workTable ?? ''
    form.workTableKey = data.workTableKey ?? 'Id'
    form.enable = data.enable ?? 1
    form.steps = (data.steps ?? []).map((s, i) => ({
      stepName: s.stepName,
      stepOrder: s.stepOrder || i + 1,
      stepType: s.stepType ?? 1,
      stepValue: s.stepValue ?? '',
    }))
    if (!form.steps.length) form.steps = defaultSteps()
    dialogVisible.value = true
  }
}

function addStep() {
  form.steps.push({
    stepName: `${t('sysWorkFlow.defaultStepName')}${form.steps.length + 1}`,
    stepOrder: form.steps.length + 1,
    stepType: 1,
    stepValue: '',
  })
}

function removeStep(index: number) {
  form.steps.splice(index, 1)
  form.steps.forEach((s, i) => { s.stepOrder = i + 1 })
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
    workTableKey: form.workTableKey.trim() || 'Id',
    enable: form.enable,
    steps: form.steps.map((s, i) => ({
      stepName: s.stepName,
      stepOrder: i + 1,
      stepType: s.stepType,
      stepValue: s.stepValue,
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
.steps-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 12px 0 8px;
  font-size: 13px;
  font-weight: 600;
}
</style>
