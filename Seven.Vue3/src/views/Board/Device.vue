<!--
  代码由框架生成，重新生成会覆盖本文件。
  业务逻辑请写在：../../extension/Board/Device.ts
-->
<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('generated.Device.listTitle') }}</span>
          <div class="toolbar-actions">
            <el-button
              v-for="btn in toolbarButtons"
              :key="btn.key"
              :type="btn.type === 'default' ? undefined : (btn.type || undefined)"
              :icon="btn.icon"
              :disabled="!!btn.requireSelection && selectedIds.length === 0"
              @click="onExtButton(btn)"
            >{{ btn.label }}</el-button>
            <el-button
              v-if="userStore.hasPermission('Device.Delete')"
              type="danger"
              plain
              :icon="ActionIcons.batchDelete"
              :disabled="selectedIds.length === 0"
              @click="batchRemove"
            >{{ t('common.batchDelete') }}</el-button>
            <el-button
              v-if="userStore.hasPermission('Device.Import')"
              :icon="ActionIcons.import"
              @click="triggerImport"
            >{{ t('common.import') }}</el-button>
            <el-button
              v-if="userStore.hasPermission('Device.Export')"
              :icon="ActionIcons.export"
              @click="doExport"
            >{{ t('common.export') }}</el-button>
            <el-button
              v-if="userStore.hasPermission('Device.Import')"
              link
              type="primary"
              @click="downloadTemplate"
            >{{ t('common.downloadTemplate') }}</el-button>
            <el-button
              v-if="userStore.hasPermission('Device.Add')"
              type="primary"
              :icon="ActionIcons.add"
              @click="openForm()"
            >{{ t('common.add') }}</el-button>
            <el-button :icon="ActionIcons.columnSettings" @click="openColumnSettings" />
          </div>
        </div>
      </template>
      <el-table :data="tableData" v-loading="loading" border @sort-change="onSortChange" @selection-change="onSelectionChange">
        <el-table-column type="selection" width="48" />
        <el-table-column
          v-for="col in visibleColumns"
          :key="col.prop"
          :prop="col.kind === 'enum' || col.kind === 'bool' || col.kind === 'date' ? undefined : col.prop"
          :label="t('generated.Device.' + col.prop)"
          :sortable="col.sortable ? 'custom' : false"
          :align="col.kind === 'number' ? 'right' : undefined"
        >
          <template v-if="col.kind === 'enum'" #default="{ row }">{{ enumLabel(enumOptionsMap[col.prop], row[col.prop]) }}</template>
          <template v-else-if="col.kind === 'bool'" #default="{ row }">{{ row[col.prop] ? t('common.enabled') : t('common.disabled') }}</template>
          <template v-else-if="col.kind === 'date'" #default="{ row }">{{ formatDate(row[col.prop]) }}</template>
        </el-table-column>
        <el-table-column :label="t('generated.Device.actions')" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-if="userStore.hasPermission('Device.Update')" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">{{ t('generated.Device.edit') }}</el-button>
            <el-button v-if="userStore.hasPermission('Device.Delete')" link type="danger" :icon="ActionIcons.delete" @click="remove(row)">{{ t('common.delete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination v-model:current-page="page" :total="total" @change="loadData" style="margin-top:16px" />
    </el-card>
    <el-dialog v-model="dialogVisible" :title="form.deviceId ? t('generated.Device.edit') : t('generated.Device.add')" width="520px">
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('generated.Device.deviceName')"><el-input v-model="form.deviceName" clearable /></el-form-item>
        <el-form-item :label="t('generated.Device.deviceCode')"><el-input v-model="form.deviceCode" clearable /></el-form-item>
        <el-form-item :label="t('generated.Device.status')">
          <el-select v-model="form.status" style="width:100%" clearable>
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('generated.Device.location')"><el-input v-model="form.location" clearable /></el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.save" @click="save">{{ t('common.confirm') }}</el-button>
        </div>
      </template>
    </el-dialog>
    <TableColumnSettings
      v-model="settingsVisible"
      :columns="settingsColumns"
      :prefs="columnPrefs"
      @save="saveColumnPrefs"
      @reset="resetColumnPrefs"
    />
    <input ref="importInput" type="file" accept=".xlsx,.xls" style="display:none" @change="onImportFile" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { downloadFile, downloadGet, getPageData, uploadFile } from '../../api/http'
import pageExtension from '../../extension/Board/Device'
import type { PageActionContext, ToolbarButton } from '../../extension/types'
import { useUserStore } from '../../stores/user'
import { ActionIcons } from '../../constants/actionIcons'
import TableColumnSettings from '../../components/TableColumnSettings.vue'
import { useTableColumns, type ColumnDef } from '../../composables/useTableColumns'

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()
const loading = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const sort = ref('')
const order = ref('')
const dialogVisible = ref(false)
const selectedRows = ref<Record<string, unknown>[]>([])
const importInput = ref<HTMLInputElement | null>(null)

const allColumns: ColumnDef[] = [
  { prop: 'deviceId', kind: 'number', sortable: false },
  { prop: 'deviceName', kind: 'string', sortable: false },
  { prop: 'deviceCode', kind: 'string', sortable: false },
  { prop: 'status', kind: 'enum', sortable: false },
  { prop: 'location', kind: 'string', sortable: false },
]

const {
  columnPrefs,
  visibleColumns,
  settingsVisible,
  openColumnSettings,
  saveColumnPrefs,
  resetColumnPrefs,
} = useTableColumns('crud-columns:Device', allColumns)

const settingsColumns = computed(() =>
  allColumns.map((c) => ({ prop: c.prop, label: t('generated.Device.' + c.prop) })),
)

const selectedIds = computed(() =>
  selectedRows.value
    .map((r) => Number(r.deviceId ?? 0))
    .filter((id) => id > 0),
)

const toolbarButtons = computed(() =>
  (pageExtension.toolbarButtons ?? []).filter(
    (btn) => !btn.permission || userStore.hasPermission(btn.permission),
  ),
)

const emptyForm = () => ({ deviceId: 0, deviceName: '', deviceCode: '', status: 0, location: '' })
const form = reactive<Record<string, unknown>>(emptyForm())

function formatDate(v: unknown) {
  if (v == null || v === '') return ''
  return String(v).replace('T', ' ').slice(0, 19)
}

function enumLabel(options: { value: number; label: string }[] | null | undefined, v: unknown) {
  const hit = (options ?? []).find((o) => o.value === Number(v))
  return hit?.label ?? (v == null ? '' : String(v))
}

const statusOptions = [
  { value: 0, label: '离线' },
  { value: 1, label: '在线' },
  { value: 2, label: '故障' },
  { value: 3, label: '维护中' },
]

const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {
  status: statusOptions,
}

function onSelectionChange(rows: Record<string, unknown>[]) {
  selectedRows.value = rows
}

function buildActionContext(): PageActionContext {
  return {
    selectedRows: selectedRows.value,
    selectedIds: selectedIds.value,
    reload: loadData,
    http,
    router,
    t: (key: string) => t(key),
  }
}

async function onExtButton(btn: ToolbarButton) {
  if (btn.requireSelection && selectedIds.value.length === 0) {
    ElMessage.warning(t('common.selectRequired'))
    return
  }
  await btn.onClick(buildActionContext())
}

async function loadData() {
  loading.value = true
  selectedRows.value = []
  try {
    const res = await getPageData('/api/Device/getPageData', {
      page: page.value,
      rows: 30,
      sort: sort.value || undefined,
      order: order.value || undefined,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally { loading.value = false }
}

function onSortChange(payload: { prop: string; order: string | null }) {
  sort.value = payload.prop || ''
  order.value = payload.order === 'ascending' ? 'asc' : payload.order === 'descending' ? 'desc' : ''
  loadData()
}

function openForm(row?: Record<string, unknown>) {
  Object.assign(form, emptyForm(), row ?? {})
  if (!row) form.deviceId = 0
  dialogVisible.value = true
}

async function save() {
  const key = Number(form.deviceId ?? 0)
  const url = key > 0 ? '/api/Device/update' : '/api/Device/add'
  const payload = { ...form, deviceId: key }
  const res = await http.post(url, payload)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadData()
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

async function remove(row: Record<string, unknown>) {
  try {
    await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), { type: 'warning' })
  } catch {
    return
  }
  const id = Number(row.deviceId ?? 0)
  if (!id) {
    ElMessage.error(t('common.operationFailed'))
    return
  }
  const res = await http.post('/api/Device/del', [id])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

async function batchRemove() {
  if (selectedIds.value.length === 0) {
    ElMessage.warning(t('common.selectRequired'))
    return
  }
  try {
    await ElMessageBox.confirm(t('common.batchDeleteConfirm'), t('common.batchDelete'), { type: 'warning' })
  } catch {
    return
  }
  const res = await http.post('/api/Device/del', selectedIds.value)
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

function triggerImport() {
  importInput.value?.click()
}

async function onImportFile(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  try {
    const res = await uploadFile('/api/Device/import', file)
    if (res.status) {
      ElMessage.success(res.message || t('common.importSuccess'))
      await loadData()
    } else {
      ElMessage.error(res.message || t('common.operationFailed'))
    }
  } catch {
    ElMessage.error(t('common.operationFailed'))
  }
}

async function doExport() {
  try {
    await downloadFile(
      '/api/Device/export',
      { page: 1, rows: 10000, sort: sort.value || undefined, order: order.value || undefined },
      'Device.xlsx',
    )
  } catch {
    ElMessage.error(t('common.operationFailed'))
  }
}

async function downloadTemplate() {
  try {
    await downloadGet('/api/Device/exportTemplate', 'Device_template.xlsx')
  } catch {
    ElMessage.error(t('common.operationFailed'))
  }
}

onMounted(loadData)
</script>

<style scoped>
.toolbar { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.toolbar-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
</style>
