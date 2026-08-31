<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('formDesign.listTitle') }}</span>
          <el-button
            v-permission="['FormDesignOptions.Add', 'FormDesignOptions.Update']"
            type="primary"
            :icon="ActionIcons.add"
            @click="openForm()"
          >{{ t('common.add') }}</el-button>
        </div>
      </template>
      <el-table :data="rows" v-loading="loading" border>
        <el-table-column prop="formId" :label="t('formDesign.colId')" width="80" />
        <el-table-column prop="title" :label="t('formDesign.colTitle')" min-width="180" />
        <el-table-column :label="t('formDesign.colStatus')" width="90">
          <template #default="{ row }">{{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}</template>
        </el-table-column>
        <el-table-column :label="t('formDesign.colActions')" width="280" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openForm(row)">{{ t('common.edit') }}</el-button>
            <el-button link @click="openCollect(row)">{{ t('formDesign.preview') }}</el-button>
            <el-button link @click="openCollections(row)">{{ t('formDesign.collections') }}</el-button>
            <el-button link type="danger" @click="remove(row)">{{ t('common.delete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="form.formId ? t('formDesign.edit') : t('formDesign.add')" width="820px" destroy-on-close>
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('formDesign.colTitle')" required>
          <el-input v-model="form.title" />
        </el-form-item>
        <el-form-item :label="t('formDesign.colStatus')">
          <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <div class="fields-toolbar">
        <span>{{ t('formDesign.fields') }}</span>
        <el-button size="small" type="primary" @click="addField">{{ t('formDesign.addField') }}</el-button>
      </div>
      <el-table :data="fields" border size="small">
        <el-table-column :label="t('formDesign.fieldProp')" min-width="120">
          <template #default="{ row }"><el-input v-model="row.prop" size="small" /></template>
        </el-table-column>
        <el-table-column :label="t('formDesign.fieldLabel')" min-width="120">
          <template #default="{ row }"><el-input v-model="row.label" size="small" /></template>
        </el-table-column>
        <el-table-column :label="t('formDesign.fieldKind')" width="130">
          <template #default="{ row }">
            <el-select v-model="row.kind" size="small" style="width:100%">
              <el-option label="string" value="string" />
              <el-option label="number" value="number" />
              <el-option label="date" value="date" />
              <el-option label="bool" value="bool" />
              <el-option label="textarea" value="textarea" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column :label="t('formDesign.fieldRequired')" width="80">
          <template #default="{ row }"><el-switch v-model="row.required" /></template>
        </el-table-column>
        <el-table-column width="60">
          <template #default="{ $index }">
            <el-button link type="danger" @click="fields.splice($index, 1)">×</el-button>
          </template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" @click="save">{{ t('common.save') }}</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="collectVisible" :title="collectTitle" size="480px">
      <el-form v-if="collectSchema.length" label-width="100px">
        <el-form-item v-for="f in collectSchema" :key="f.prop" :label="f.label" :required="f.required">
          <el-input-number v-if="f.kind === 'number'" v-model="collectModel[f.prop]" style="width:100%" />
          <el-switch v-else-if="f.kind === 'bool'" v-model="collectModel[f.prop]" />
          <el-date-picker v-else-if="f.kind === 'date'" v-model="collectModel[f.prop]" type="datetime" style="width:100%" />
          <el-input v-else-if="f.kind === 'textarea'" v-model="collectModel[f.prop]" type="textarea" :rows="3" />
          <el-input v-else v-model="collectModel[f.prop]" />
        </el-form-item>
        <el-button type="primary" @click="submitCollect">{{ t('formDesign.submit') }}</el-button>
      </el-form>
    </el-drawer>

    <el-drawer v-model="collectionVisible" :title="t('formDesign.collections')" size="640px">
      <el-table :data="collectionRows" v-loading="collectionLoading" border size="small">
        <el-table-column prop="formCollectionId" label="ID" width="70" />
        <el-table-column prop="submitter" :label="t('formDesign.submitter')" width="120" />
        <el-table-column prop="formData" :label="t('formDesign.formData')" min-width="220" show-overflow-tooltip />
        <el-table-column prop="createDate" :label="t('formDesign.createDate')" width="170" />
      </el-table>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface FormRow {
  formId: number
  title: string
  enable?: number
  formOptions?: string
}

interface FieldDef {
  prop: string
  label: string
  kind: string
  required?: boolean
}

const { t } = useI18n()
const loading = ref(false)
const rows = ref<FormRow[]>([])
const dialogVisible = ref(false)
const fields = ref<FieldDef[]>([])
const form = reactive({ formId: 0, title: '', enable: 1 as number, formOptions: '' })

const collectVisible = ref(false)
const collectTitle = ref('')
const collectFormId = ref(0)
const collectSchema = ref<FieldDef[]>([])
const collectModel = reactive<Record<string, unknown>>({})

const collectionVisible = ref(false)
const collectionLoading = ref(false)
const collectionRows = ref<Record<string, unknown>[]>([])

async function load() {
  loading.value = true
  try {
    const res = await getPageData('/api/FormDesignOptions/getPageData', { page: 1, rows: 100 })
    if (res.status && res.data) {
      rows.value = ((res.data as { rows: FormRow[] }).rows) ?? []
    }
  } finally {
    loading.value = false
  }
}

function parseFields(json?: string): FieldDef[] {
  if (!json) return []
  try {
    const o = JSON.parse(json) as { fields?: FieldDef[] }
    return o.fields ?? []
  } catch {
    return []
  }
}

function openForm(row?: FormRow) {
  if (!row) {
    Object.assign(form, { formId: 0, title: '', enable: 1, formOptions: '' })
    fields.value = [{ prop: 'name', label: t('formDesign.sampleName'), kind: 'string', required: true }]
  } else {
    Object.assign(form, { formId: row.formId, title: row.title, enable: row.enable ?? 1, formOptions: row.formOptions ?? '' })
    fields.value = parseFields(row.formOptions)
  }
  dialogVisible.value = true
}

function addField() {
  fields.value.push({ prop: `field${fields.value.length + 1}`, label: t('formDesign.newField'), kind: 'string' })
}

async function save() {
  if (!form.title.trim()) {
    ElMessage.warning(t('formDesign.titleRequired'))
    return
  }
  const payload = {
    formId: form.formId,
    title: form.title.trim(),
    enable: form.enable,
    formOptions: JSON.stringify({ fields: fields.value }),
  }
  const res = await http.post('/api/FormDesignOptions/save', payload)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await load()
  }
}

async function remove(row: FormRow) {
  await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), { type: 'warning' })
  const res = await http.post('/api/FormDesignOptions/del', [row.formId])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await load()
  }
}

function openCollect(row: FormRow) {
  collectFormId.value = row.formId
  collectTitle.value = row.title
  collectSchema.value = parseFields(row.formOptions)
  for (const k of Object.keys(collectModel)) delete collectModel[k]
  for (const f of collectSchema.value) {
    collectModel[f.prop] = f.kind === 'bool' ? false : f.kind === 'number' ? 0 : ''
  }
  collectVisible.value = true
}

async function submitCollect() {
  const res = await http.post('/api/FormDesignOptions/submit', {
    formId: collectFormId.value,
    formData: JSON.stringify(collectModel),
  })
  if (res.status) {
    ElMessage.success(t('common.success'))
    collectVisible.value = false
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

async function openCollections(row: FormRow) {
  collectionVisible.value = true
  collectionLoading.value = true
  try {
    const res = await http.post('/api/FormDesignOptions/getCollections', {
      page: 1,
      rows: 50,
      wheres: JSON.stringify([{ name: 'formId', value: String(row.formId) }]),
    })
    if (res.status && res.data) {
      collectionRows.value = ((res.data as { rows: Record<string, unknown>[] }).rows) ?? []
    }
  } finally {
    collectionLoading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.fields-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 8px 0;
  font-weight: 600;
}
</style>
