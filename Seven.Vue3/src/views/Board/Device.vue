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

      <el-form v-if="searchFields.length" :model="searchModel" inline class="search-bar" @submit.prevent="onSearch">
        <el-form-item v-for="f in searchFields" :key="f.prop" :label="searchFieldLabel(f)">
          <el-select v-if="f.kind === 'enum' || f.kind === 'bool'" v-model="searchModel[f.prop]" clearable style="width:160px">
            <el-option
              v-for="o in (f.options ?? (f.kind === 'bool' ? boolSearchOptions : (enumOptionsMap[f.prop] ?? [])))"
              :key="String(o.value)"
              :label="o.label"
              :value="o.value"
            />
          </el-select>
          <el-date-picker
            v-else-if="f.kind === 'date'"
            v-model="searchModel[f.prop]"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            clearable
            style="width:200px"
          />
          <el-input-number
            v-else-if="f.kind === 'number'"
            v-model="searchModel[f.prop]"
            controls-position="right"
            style="width:160px"
          />
          <el-input v-else v-model="searchModel[f.prop]" clearable style="width:160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">{{ t('common.search') }}</el-button>
          <el-button @click="onResetSearch">{{ t('common.reset') }}</el-button>
        </el-form-item>
      </el-form>

      <el-table
        :data="tableData"
        v-loading="loading"
        border
        highlight-current-row
        @sort-change="onSortChange"
        @selection-change="onSelectionChange"
        @current-change="onCurrentChange"
      >
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
        <el-table-column :label="t('generated.Device.actions')" :min-width="actionsColumnWidth" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <el-button v-if="userStore.hasPermission('Device.Update')" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">{{ t('generated.Device.edit') }}</el-button>
              <el-button
                v-for="btn in rowButtonsFor(row)"
                :key="btn.key"
                link
                :type="btn.type === 'default' ? 'primary' : (btn.type || 'primary')"
                :icon="btn.icon"
                :disabled="!!btn.disabled?.(row)"
                @click="onRowExtButton(btn, row)"
              >{{ btn.label }}</el-button>
              <el-button
                v-for="dt in dialogDetailTables"
                :key="'detail-' + dt.key"
                link
                type="primary"
                @click="openDetailDialog(dt, row)"
              >{{ dt.buttonLabel || dt.title }}</el-button>
              <el-button v-if="userStore.hasPermission('Device.Delete')" link type="danger" :icon="ActionIcons.delete" @click="remove(row)">{{ t('common.delete') }}</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination v-model:current-page="page" :total="total" @change="loadData" style="margin-top:16px" />
    </el-card>

    <el-card
      v-for="dt in belowDetailTables"
      :key="'below-' + dt.key"
      class="detail-card"
      shadow="never"
    >
      <template #header>
        <div class="detail-header">
          <span class="detail-title">{{ dt.title }}</span>
          <span v-if="!activeMasterRow" class="detail-hint">{{ t('common.selectRowForDetail') }}</span>
        </div>
      </template>
      <el-table :data="detailState(dt.key).rows" v-loading="detailState(dt.key).loading" border max-height="320">
        <el-table-column
          v-for="col in dt.columns"
          :key="col.prop"
          :prop="col.kind === 'enum' || col.kind === 'bool' || col.kind === 'date' ? undefined : col.prop"
          :label="detailColLabel(col)"
          :width="col.width"
        >
          <template v-if="col.kind === 'enum'" #default="{ row }">{{ enumLabel(enumOptionsMap[col.prop], row[col.prop]) }}</template>
          <template v-else-if="col.kind === 'bool'" #default="{ row }">{{ row[col.prop] ? t('common.enabled') : t('common.disabled') }}</template>
          <template v-else-if="col.kind === 'date'" #default="{ row }">{{ formatDate(row[col.prop]) }}</template>
        </el-table-column>
      </el-table>
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

    <el-dialog
      v-model="detailDialogVisible"
      :title="detailDialogCfg?.title || ''"
      width="800px"
      destroy-on-close
    >
      <el-table :data="detailDialogRows" v-loading="detailDialogLoading" border max-height="420">
        <el-table-column
          v-for="col in (detailDialogCfg?.columns ?? [])"
          :key="col.prop"
          :prop="col.kind === 'enum' || col.kind === 'bool' || col.kind === 'date' ? undefined : col.prop"
          :label="detailColLabel(col)"
          :width="col.width"
        >
          <template v-if="col.kind === 'enum'" #default="{ row }">{{ enumLabel(enumOptionsMap[col.prop], row[col.prop]) }}</template>
          <template v-else-if="col.kind === 'bool'" #default="{ row }">{{ row[col.prop] ? t('common.enabled') : t('common.disabled') }}</template>
          <template v-else-if="col.kind === 'date'" #default="{ row }">{{ formatDate(row[col.prop]) }}</template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <TableColumnSettings
      v-model="settingsVisible"
      :columns="settingsColumns"
      :prefs="columnPrefs"
      @save="saveColumnPrefs"
      @reset="resetColumnPrefs"
    />
    <input ref="importInput" type="file" accept=".xlsx,.xls" style="display:none" @change="onImportFile" />
    <component :is="pageExtension.overlay" v-if="pageExtension.overlay" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { downloadFile, downloadGet, getPageData, uploadFile } from '../../api/http'
import pageExtension from '../../extension/Board/Device'
import type {
  CrudHookResult,
  DetailColumnConfig,
  DetailTableConfig,
  PageActionContext,
  RowActionContext,
  RowButton,
  SearchFieldConfig,
  ToolbarButton,
} from '../../extension/types'
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
const activeMasterRow = ref<Record<string, unknown> | null>(null)
const detailDialogVisible = ref(false)
const detailDialogCfg = ref<DetailTableConfig | null>(null)
const detailDialogRows = ref<Record<string, unknown>[]>([])
const detailDialogLoading = ref(false)
const detailDataMap = reactive<Record<string, { loading: boolean; rows: Record<string, unknown>[] }>>({})

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

const searchFields = computed(() => pageExtension.searchFields ?? [])
const searchModel = reactive<Record<string, unknown>>({})
const boolSearchOptions = [
  { value: true, label: t('common.enabled') },
  { value: false, label: t('common.disabled') },
]

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

const rowButtons = computed(() =>
  (pageExtension.rowButtons ?? []).filter(
    (btn) => !btn.permission || userStore.hasPermission(btn.permission),
  ),
)

const belowDetailTables = computed(() =>
  (pageExtension.detailTables ?? []).filter(
    (d) => d.mode === 'below' && (!d.permission || userStore.hasPermission(d.permission)),
  ),
)

const dialogDetailTables = computed(() =>
  (pageExtension.detailTables ?? []).filter(
    (d) => d.mode === 'dialog' && (!d.permission || userStore.hasPermission(d.permission)),
  ),
)

const actionsColumnWidth = computed(() => {
  const base = 2
  const extra = rowButtons.value.length + dialogDetailTables.value.length
  return Math.max(200, (base + extra) * 88)
})

const emptyForm = () => ({ deviceId: 0, deviceName: '', deviceCode: '', status: 0, location: '' })
const form = reactive<Record<string, unknown>>(emptyForm())

function formatDate(v: unknown) {
  if (v == null || v === '') return ''
  return String(v).replace('T', ' ').slice(0, 19)
}

function enumLabel(options: { value: number | string; label: string }[] | null | undefined, v: unknown) {
  const hit = (options ?? []).find((o) => o.value === v || o.value === Number(v))
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


function searchFieldLabel(f: SearchFieldConfig) {
  if (f.label) return f.label
  if (f.labelKey) return t(f.labelKey)
  return t('generated.Device.' + f.prop)
}

function detailColLabel(col: DetailColumnConfig) {
  if (col.label) return col.label
  if (col.labelKey) return t(col.labelKey)
  return col.prop
}

function detailState(key: string) {
  if (!detailDataMap[key]) detailDataMap[key] = { loading: false, rows: [] }
  return detailDataMap[key]
}

function buildWheresJson() {
  const list: { name: string; value: string; displayType: string }[] = []
  for (const f of searchFields.value) {
    const raw = searchModel[f.prop]
    if (raw === undefined || raw === null || raw === '') continue
    const operator = f.operator ?? (f.kind === 'string' || !f.kind ? 'like' : 'equal')
    list.push({ name: f.prop, value: String(raw), displayType: operator })
  }
  return list.length ? JSON.stringify(list) : undefined
}

async function runHook(result: CrudHookResult): Promise<boolean> {
  return (await result) !== false
}

function onSelectionChange(rows: Record<string, unknown>[]) {
  selectedRows.value = rows
  if (belowDetailTables.value.length) {
    activeMasterRow.value = rows.length ? rows[rows.length - 1] : null
    void loadBelowDetails()
  }
}

function onCurrentChange(row: Record<string, unknown> | undefined) {
  if (!belowDetailTables.value.length) return
  if (row) {
    activeMasterRow.value = row
    void loadBelowDetails()
  }
}

function rowButtonsFor(row: Record<string, unknown>) {
  return rowButtons.value.filter((btn) => !btn.visible || btn.visible(row))
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

function buildRowActionContext(row: Record<string, unknown>): RowActionContext {
  const rowId = Number(row.deviceId ?? 0)
  return {
    ...buildActionContext(),
    row,
    rowId,
    selectedRows: [row],
    selectedIds: rowId > 0 ? [rowId] : [],
  }
}

async function onExtButton(btn: ToolbarButton) {
  if (btn.requireSelection && selectedIds.value.length === 0) {
    ElMessage.warning(t('common.selectRequired'))
    return
  }
  await btn.onClick(buildActionContext())
}

async function onRowExtButton(btn: RowButton, row: Record<string, unknown>) {
  await btn.onClick(buildRowActionContext(row))
}

async function loadDetailRows(cfg: DetailTableConfig, master: Record<string, unknown>) {
  if (cfg.load) return await cfg.load(master, buildActionContext())
  if (!cfg.apiRoute || !cfg.foreignKey) return []
  const masterKey = cfg.masterKey || 'deviceId'
  const masterId = master[masterKey]
  if (masterId == null || masterId === '') return []
  const wheres = JSON.stringify([{ name: cfg.foreignKey, value: String(masterId), displayType: 'equal' }])
  const res = await getPageData(`/api/${cfg.apiRoute}/getPageData`, {
    page: 1,
    rows: cfg.pageSize ?? 100,
    wheres,
  })
  if (res.status && res.data) {
    return (res.data as { rows: Record<string, unknown>[] }).rows ?? []
  }
  return []
}

async function loadBelowDetails() {
  const master = activeMasterRow.value
  for (const dt of belowDetailTables.value) {
    const state = detailState(dt.key)
    if (!master) {
      state.rows = []
      continue
    }
    state.loading = true
    try {
      state.rows = await loadDetailRows(dt, master)
    } finally {
      state.loading = false
    }
  }
}

async function openDetailDialog(cfg: DetailTableConfig, row: Record<string, unknown>) {
  detailDialogCfg.value = cfg
  detailDialogVisible.value = true
  detailDialogLoading.value = true
  detailDialogRows.value = []
  try {
    detailDialogRows.value = await loadDetailRows(cfg, row)
  } finally {
    detailDialogLoading.value = false
  }
}

async function loadData() {
  loading.value = true
  selectedRows.value = []
  activeMasterRow.value = null
  for (const dt of belowDetailTables.value) detailState(dt.key).rows = []
  try {
    const res = await getPageData('/api/Device/getPageData', {
      page: page.value,
      rows: 30,
      sort: sort.value || undefined,
      order: order.value || undefined,
      wheres: buildWheresJson(),
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally { loading.value = false }
}

function onSearch() {
  page.value = 1
  void loadData()
}

function onResetSearch() {
  for (const f of searchFields.value) searchModel[f.prop] = undefined
  page.value = 1
  void loadData()
}

function onSortChange(payload: { prop: string; order: string | null }) {
  sort.value = payload.prop || ''
  order.value = payload.order === 'ascending' ? 'asc' : payload.order === 'descending' ? 'desc' : ''
  loadData()
}

async function openForm(row?: Record<string, unknown>) {
  const mode = row ? 'edit' : 'add'
  Object.assign(form, emptyForm(), row ?? {})
  if (!row) form.deviceId = 0
  const ok = await runHook(pageExtension.hooks?.beforeOpenForm?.({ mode, row, form }))
  if (!ok) return
  dialogVisible.value = true
}

async function save() {
  const key = Number(form.deviceId ?? 0)
  const mode = key > 0 ? 'edit' : 'add'
  const ok = await runHook(pageExtension.hooks?.beforeSave?.({ mode, form }))
  if (!ok) return
  const url = mode === 'edit' ? '/api/Device/update' : '/api/Device/add'
  const payload = { ...form, deviceId: key }
  const res = await http.post(url, payload)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await pageExtension.hooks?.afterSave?.({ mode, form })
    await loadData()
  } else {
    ElMessage.error(res.message || t('common.operationFailed'))
  }
}

async function remove(row: Record<string, unknown>) {
  const id = Number(row.deviceId ?? 0)
  if (!id) {
    ElMessage.error(t('common.operationFailed'))
    return
  }
  const hookOk = await runHook(pageExtension.hooks?.beforeDelete?.({ ids: [id], rows: [row] }))
  if (!hookOk) return
  try {
    await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), { type: 'warning' })
  } catch {
    return
  }
  const res = await http.post('/api/Device/del', [id])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await pageExtension.hooks?.afterDelete?.({ ids: [id] })
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
  const ids = [...selectedIds.value]
  const rows = [...selectedRows.value]
  const hookOk = await runHook(pageExtension.hooks?.beforeDelete?.({ ids, rows }))
  if (!hookOk) return
  try {
    await ElMessageBox.confirm(t('common.batchDeleteConfirm'), t('common.batchDelete'), { type: 'warning' })
  } catch {
    return
  }
  const res = await http.post('/api/Device/del', ids)
  if (res.status) {
    ElMessage.success(t('common.success'))
    await pageExtension.hooks?.afterDelete?.({ ids })
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
      { page: 1, rows: 10000, sort: sort.value || undefined, order: order.value || undefined, wheres: buildWheresJson() },
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
.row-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 2px 4px;
}
.search-bar { margin-bottom: 12px; }
.detail-card { margin-top: 12px; }
.detail-header { display: flex; align-items: center; gap: 12px; }
.detail-title {
  font-weight: 600;
  padding-left: 8px;
  border-left: 3px solid var(--el-color-primary);
}
.detail-hint { color: var(--el-text-color-secondary); font-size: 13px; }
</style>
