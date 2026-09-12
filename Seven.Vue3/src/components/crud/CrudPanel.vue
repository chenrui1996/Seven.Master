<template>
  <div class="crud-panel" :class="{ 'crud-panel--nested': depth > 0 }">
    <el-card v-if="showCard" shadow="never" class="crud-main-card">
      <template #header>
        <div class="crud-header-row">
          <div class="crud-header-left">
            <div class="seven-section-title">
              <span class="seven-section-title__bar" aria-hidden="true" />
              <el-icon class="crud-title-icon"><component :is="ActionIcons.model" /></el-icon>
              <span class="toolbar-title">{{ panelTitle }}</span>
            </div>
            <el-form
              v-if="resolvedSearchFields.length"
              :model="searchModel"
              inline
              size="small"
              class="search-bar search-bar--inline"
              role="search"
              :aria-label="panelActionLabel(t('common.search'))"
              @submit.prevent="onSearch"
            >
              <el-form-item v-for="f in resolvedSearchFields" :key="f.prop" :label="searchFieldLabel(f)">
                <el-select
                  v-if="f.kind === 'enum' || f.kind === 'bool'"
                  v-model="searchModel[f.prop]"
                  clearable
                  style="width:140px"
                >
                  <el-option
                    v-for="o in searchFieldOptions(f)"
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
                  style="width:180px"
                />
                <el-input-number
                  v-else-if="f.kind === 'number'"
                  v-model="searchModel[f.prop]"
                  controls-position="right"
                  style="width:140px"
                />
                <el-input
                  v-else
                  v-model="searchModel[f.prop]"
                  clearable
                  style="width:140px"
                  @keyup.enter="onSearch"
                />
              </el-form-item>
            </el-form>
          </div>
          <div
            class="toolbar-actions crud-toolbar"
            role="toolbar"
            :aria-label="panelActionLabel(t(`${i18nKey}.actions`))"
          >
            <div class="crud-toolbar__group crud-toolbar__group--primary" role="group" :aria-label="panelActionLabel(t('common.search'))">
              <el-button size="small" type="primary" :icon="ActionIcons.sync" @click="onSearch">
                {{ t('common.search') }}
              </el-button>
              <el-button size="small" @click="onResetSearch">
                {{ t('common.reset') }}
              </el-button>
            </div>
            <div
              v-if="userStore.hasPermission(`${tableName}.Add`)"
              class="crud-toolbar__group crud-toolbar__group--create"
              role="group"
              :aria-label="panelActionLabel(t('common.add'))"
            >
              <el-button
                size="small"
                class="is-create"
                :icon="ActionIcons.add"
                @click="openForm()"
              >{{ t('common.add') }}</el-button>
            </div>
            <div
              v-if="userStore.hasPermission(`${tableName}.Update`) || (depth === 0 && toolbarButtons.length)"
              class="crud-toolbar__group crud-toolbar__group--primary"
              role="group"
              :aria-label="panelActionLabel(t(`${i18nKey}.actions`))"
            >
              <el-button
                v-if="userStore.hasPermission(`${tableName}.Update`)"
                size="small"
                :icon="ActionIcons.edit"
                :disabled="!singleSelectedRow"
                @click="openForm(singleSelectedRow || undefined)"
              >{{ t(`${i18nKey}.edit`) }}</el-button>
              <template v-if="depth === 0 && extension">
                <el-button
                  v-for="btn in toolbarButtons"
                  :key="btn.key"
                  size="small"
                  :type="btn.type === 'default' ? undefined : (btn.type || undefined)"
                  :icon="btn.icon"
                  :disabled="!!btn.requireSelection && selectedIds.length === 0"
                  @click="onExtButton(btn)"
                >{{ btn.label }}</el-button>
              </template>
            </div>
            <div
              v-if="userStore.hasPermission(`${tableName}.Delete`)"
              class="crud-toolbar__group crud-toolbar__group--destructive"
              role="group"
              :aria-label="panelActionLabel(t('common.delete'))"
            >
              <el-button
                size="small"
                :disabled="selectedIds.length === 0"
                @click="toolbarDelete"
              >{{ t('common.delete') }}</el-button>
            </div>
            <div class="crud-toolbar__group crud-toolbar__group--utility" role="group" :aria-label="panelActionLabel(t('common.more'))">
              <el-dropdown v-if="depth === 0 && hasMoreActions" trigger="click">
                <el-button size="small" class="crud-toolbar__menu" :title="panelActionLabel(t('common.more'))">
                  {{ t('common.more') }}
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item
                      v-if="userStore.hasPermission(`${tableName}.Delete`)"
                      :disabled="selectedIds.length === 0"
                      @click="batchRemove"
                    >{{ t('common.batchDelete') }}</el-dropdown-item>
                    <el-dropdown-item
                      v-if="userStore.hasPermission(`${tableName}.Import`)"
                      @click="triggerImport"
                    >{{ t('common.import') }}</el-dropdown-item>
                    <el-dropdown-item
                      v-if="userStore.hasPermission(`${tableName}.Export`)"
                      @click="doExport"
                    >{{ t('common.export') }}</el-dropdown-item>
                    <el-dropdown-item
                      v-if="userStore.hasPermission(`${tableName}.Import`)"
                      @click="downloadTemplate"
                    >{{ t('common.downloadTemplate') }}</el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
              <el-button
                size="small"
                class="crud-toolbar__icon"
                :icon="ActionIcons.sync"
                circle
                :aria-label="panelActionLabel(t('common.refresh'))"
                :title="panelActionLabel(t('common.refresh'))"
                @click="loadData"
              />
              <el-button
                size="small"
                class="crud-toolbar__icon"
                :icon="ActionIcons.columnSettings"
                circle
                :aria-label="panelActionLabel(t('common.columnSettings'))"
                :title="panelActionLabel(t('common.columnSettings'))"
                @click="openColumnSettings"
              />
            </div>
          </div>
        </div>
      </template>

      <div class="table-wrap" role="region" :aria-label="panelTitle" :aria-busy="loading ? 'true' : 'false'">
        <el-table
          class="crud-table"
          :data="tableData"
          v-loading="loading"
          :element-loading-text="t('common.loading')"
          border
          stripe
          highlight-current-row
          table-layout="auto"
          @sort-change="onSortChange"
          @selection-change="onSelectionChange"
          @current-change="onCurrentChange"
          @row-dblclick="onRowDblClick"
        >
          <el-table-column
            type="selection"
            width="48"
            class-name="crud-table__selection-cell"
            label-class-name="crud-table__selection-head"
          />
          <el-table-column
            v-for="(col, colIdx) in visibleColumns"
            :key="col.prop"
            :prop="col.kind === 'enum' || col.kind === 'bool' || col.kind === 'date' ? undefined : col.prop"
            :label="columnLabel(col.prop)"
            :sortable="col.sortable ? 'custom' : false"
            :align="col.kind === 'number' ? 'right' : undefined"
            :class-name="tableColumnClassName(col, colIdx)"
            :label-class-name="tableHeaderClassName(col, colIdx)"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              <template v-if="col.kind === 'enum'">
                {{ enumLabel(mergedEnumOptionsMap[col.prop], row[col.prop]) }}
              </template>
              <template v-else-if="col.kind === 'bool'">
                {{ row[col.prop] ? t('common.enabled') : t('common.disabled') }}
              </template>
              <template v-else-if="col.kind === 'date'">
                {{ formatDate(row[col.prop]) }}
              </template>
              <span
                v-else-if="colIdx === 0 && userStore.hasPermission(`${tableName}.Update`)"
                class="seven-link-cell"
                role="button"
                tabindex="0"
                :title="displayCellValue(row[col.prop])"
                :aria-label="editRowLabel(row, col.prop)"
                @click.stop="openForm(row)"
                @keydown.enter.prevent="openForm(row)"
                @keydown.space.prevent="openForm(row)"
              >{{ row[col.prop] == null ? '' : String(row[col.prop]) }}</span>
              <template v-else>
                {{ row[col.prop] == null ? '' : String(row[col.prop]) }}
              </template>
            </template>
          </el-table-column>

          <el-table-column
            v-if="entryDetailTables.length"
            :label="t('common.detailTables')"
            :min-width="detailTablesColumnWidth"
            class-name="crud-table__operations-cell"
            label-class-name="crud-table__operations-head"
            fixed="right"
          >
            <template #default="{ row }">
              <div class="row-actions" role="group" :aria-label="rowDetailGroupLabel(row)">
                <el-button
                  v-for="dt in entryDetailTables"
                  :key="'detail-' + dt.key"
                  link
                  type="primary"
                  @click="onDetailEntry(dt, row)"
                >{{ dt.buttonLabel || dt.title }}</el-button>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            :label="t(`${i18nKey}.actions`)"
            :min-width="actionsColumnWidth"
            class-name="crud-table__operations-cell"
            label-class-name="crud-table__operations-head"
            fixed="right"
          >
            <template #default="{ row }">
              <div class="row-actions" role="group" :aria-label="rowActionGroupLabel(row)">
                <el-button
                  v-if="userStore.hasPermission(`${tableName}.Update`)"
                  link
                  type="primary"
                  :icon="ActionIcons.edit"
                  @click="openForm(row)"
                >{{ t(`${i18nKey}.edit`) }}</el-button>
                <template v-if="depth === 0">
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
                    v-if="extension?.hooks?.submitAudit"
                    link
                    type="success"
                    @click="onSubmitAudit(row)"
                  >{{ t('common.submitAudit') }}</el-button>
                  <el-button
                    v-if="featureStore.flags.workFlow && hasAuditStatus(row)"
                    link
                    type="primary"
                    @click="openAudit(row)"
                  >{{ t('sysWorkFlow.viewAudit') }}</el-button>
                </template>
                <el-button
                  v-if="userStore.hasPermission(`${tableName}.Delete`)"
                  link
                  type="danger"
                  :icon="ActionIcons.delete"
                  @click="remove(row)"
                >{{ t('common.delete') }}</el-button>
              </div>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="innerPageSize"
        size="small"
        :page-sizes="[20, 30, 50, 100]"
        :total="total"
        background
        layout="total, sizes, prev, pager, next"
        @change="loadData"
        @size-change="onPageSizeChange"
        class="crud-pagination"
      />
    </el-card>

    <!-- below：主子表同页 -->
    <template v-if="canRenderDetails">
      <template v-for="dt in belowDetailTables" :key="'below-' + dt.key">
        <div class="detail-section">
          <div class="detail-section__head">
            <div class="seven-section-title">
              <span class="seven-section-title__bar" aria-hidden="true" />
              <h3 class="detail-section__title">{{ dt.title }}</h3>
            </div>
            <span v-if="!activeMasterRow" class="detail-hint">{{ t('common.selectRowForDetail') }}</span>
          </div>
          <div class="detail-section__body" :aria-busy="detailBusy(dt) ? 'true' : 'false'">
            <CrudPanel
              v-if="dt.apiRoute && activeMasterRow"
              :key="`${dt.key}-${masterIdOf(dt, activeMasterRow)}`"
              v-bind="nestedPanelProps(dt, activeMasterRow)"
            />
            <el-card v-else class="detail-card" shadow="never">
              <el-table
                v-if="activeMasterRow && !dt.apiRoute"
                class="crud-table crud-table--detail"
                :data="detailState(dt.key).rows"
                v-loading="detailState(dt.key).loading"
                :element-loading-text="t('common.loading')"
                border
                stripe
                table-layout="auto"
                max-height="320"
              >
                <el-table-column
                  v-for="(col, detailColIdx) in dt.columns"
                  :key="col.prop"
                  :prop="col.kind === 'enum' || col.kind === 'bool' || col.kind === 'date' ? undefined : col.prop"
                  :label="detailColLabel(col)"
                  :width="col.width"
                  :class-name="detailTableColumnClassName(col, detailColIdx)"
                  :label-class-name="detailTableHeaderClassName(col, detailColIdx)"
                  show-overflow-tooltip
                >
                  <template v-if="col.kind === 'enum'" #default="{ row }">
                    {{ enumLabel(mergedEnumOptionsMap[col.prop], row[col.prop]) }}
                  </template>
                  <template v-else-if="col.kind === 'bool'" #default="{ row }">
                    {{ row[col.prop] ? t('common.enabled') : t('common.disabled') }}
                  </template>
                  <template v-else-if="col.kind === 'date'" #default="{ row }">
                    {{ formatDate(row[col.prop]) }}
                  </template>
                </el-table-column>
              </el-table>
              <div v-else-if="!activeMasterRow" class="detail-empty">{{ t('common.selectRowForDetail') }}</div>
            </el-card>
          </div>
        </div>
      </template>
    </template>

    <el-dialog
      v-model="dialogVisible"
      :title="Number(form[keyField] ?? 0) > 0 ? t(`${i18nKey}.edit`) : t(`${i18nKey}.add`)"
      width="720px"
      destroy-on-close
      class="crud-edit-dialog"
    >
      <el-form :model="form" label-width="110px" class="crud-edit-form">
        <el-row :gutter="12">
          <el-col v-for="f in formFields" :key="f.prop" :span="12">
            <el-form-item :label="t(`${i18nKey}.${f.prop}`)">
              <el-select
                v-if="f.kind === 'enum' || f.kind === 'bool'"
                v-model="form[f.prop]"
                style="width:100%"
                clearable
              >
                <el-option
                  v-for="o in formFieldOptions(f)"
                  :key="String(o.value)"
                  :label="o.label"
                  :value="o.value"
                />
              </el-select>
              <el-date-picker
                v-else-if="f.kind === 'date'"
                v-model="form[f.prop]"
                type="datetime"
                value-format="YYYY-MM-DDTHH:mm:ss"
                style="width:100%"
                clearable
              />
              <el-input-number
                v-else-if="f.kind === 'number'"
                v-model="form[f.prop]"
                :precision="f.isDecimal ? 2 : 0"
                controls-position="right"
                style="width:100%"
              />
              <FileUploadField
                v-else-if="f.kind === 'upload'"
                v-model="form[f.prop] as string"
              />
              <el-input v-else v-model="form[f.prop]" clearable />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button size="small" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button
            size="small"
            type="primary"
            :icon="ActionIcons.save"
            :loading="saving"
            @click="save"
          >{{ t('common.confirm') }}</el-button>
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
    <input
      v-if="depth === 0"
      ref="importInput"
      type="file"
      accept=".xlsx,.xls"
      style="display:none"
      @change="onImportFile"
    />
    <AuditFlowDrawer
      v-model="auditVisible"
      :table-name="tableName"
      :table-key="auditTableKey"
      @audited="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { downloadFile, downloadGet, getPageData, uploadFile } from '../../api/http'
import type {
  CrudHookResult,
  DetailColumnConfig,
  DetailTableConfig,
  FormFieldDef,
  PageActionContext,
  PageExtension,
  RowActionContext,
  RowButton,
  SearchFieldConfig,
  ToolbarButton,
} from '../../extension/types'
import { useUserStore } from '../../stores/user'
import { useDictStore, useTabsStore } from '../../stores'
import { ActionIcons } from '../../constants/actionIcons'
import TableColumnSettings from '../TableColumnSettings.vue'
import FileUploadField from '../FileUploadField.vue'
import AuditFlowDrawer from '../workflow/AuditFlowDrawer.vue'
import { useTableColumns, type ColumnDef } from '../../composables/useTableColumns'
import { useFeatureStore } from '../../stores/features'

defineOptions({ name: 'CrudPanel' })

const props = withDefaults(
  defineProps<{
    apiRoute: string
    i18nKey: string
    tableName: string
    /** 主键字段 camelCase */
    keyField: string
    columns: ColumnDef[]
    formFields: FormFieldDef[]
    formDefaults?: Record<string, unknown>
    enumOptionsMap?: Record<string, { value: number | string; label: string }[]>
    searchFields?: SearchFieldConfig[]
    /** 固定过滤条件，合并进 wheres（equal）；新增时预填 */
    fixedFilter?: Record<string, unknown>
    detailTables?: DetailTableConfig[]
    /** toolbarButtons / rowButtons / hooks；overlay 由外壳渲染 */
    extension?: PageExtension
    depth?: number
    maxDepth?: number
    title?: string
    showCard?: boolean
    pageSize?: number
  }>(),
  {
    formDefaults: () => ({}),
    enumOptionsMap: () => ({}),
    searchFields: () => [],
    detailTables: () => [],
    depth: 0,
    maxDepth: 3,
    showCard: true,
    pageSize: 30,
  },
)

const { t } = useI18n()
const router = useRouter()
const tabsStore = useTabsStore()
const userStore = useUserStore()
const dictStore = useDictStore()
const featureStore = useFeatureStore()

const loading = ref(false)
const saving = ref(false)
const tableData = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const innerPageSize = ref(props.pageSize)
const sort = ref('')
const order = ref('')
const dialogVisible = ref(false)
const selectedRows = ref<Record<string, unknown>[]>([])
const auditVisible = ref(false)
const auditTableKey = ref('')
const importInput = ref<HTMLInputElement | null>(null)
const activeMasterRow = ref<Record<string, unknown> | null>(null)
const detailDialogVisible = ref(false)
const detailDialogCfg = ref<DetailTableConfig | null>(null)
const detailDialogMaster = ref<Record<string, unknown> | null>(null)
const detailDialogRows = ref<Record<string, unknown>[]>([])
const detailDialogLoading = ref(false)
const detailDataMap = reactive<Record<string, { loading: boolean; rows: Record<string, unknown>[] }>>({})

const {
  columnPrefs,
  visibleColumns,
  settingsVisible,
  openColumnSettings,
  saveColumnPrefs,
  resetColumnPrefs,
} = useTableColumns(`crud-columns:${props.tableName}:d${props.depth}`, props.columns)

const settingsColumns = computed(() =>
  props.columns.map((c) => ({ prop: c.prop, label: columnLabel(c.prop) })),
)

const resolvedSearchFields = computed(() => props.searchFields ?? [])
const searchModel = reactive<Record<string, unknown>>({})
const boolSearchOptions = computed(() => [
  { value: true, label: t('common.enabled') },
  { value: false, label: t('common.disabled') },
])

const panelTitle = computed(() => props.title || t(`${props.i18nKey}.listTitle`))

const canRenderDetails = computed(() => props.depth < props.maxDepth)

const permittedDetails = computed(() =>
  (props.detailTables ?? []).filter(
    (d) => !d.permission || userStore.hasPermission(d.permission),
  ),
)

const belowDetailTables = computed(() =>
  canRenderDetails.value
    ? permittedDetails.value.filter((d) => d.mode === 'below')
    : [],
)

const entryDetailTables = computed(() =>
  canRenderDetails.value
    ? permittedDetails.value.filter((d) => d.mode === 'dialog' || d.mode === 'page')
    : [],
)

const selectedIds = computed(() =>
  selectedRows.value
    .map((r) => Number(r[props.keyField] ?? 0))
    .filter((id) => id > 0),
)

const singleSelectedRow = computed(() =>
  selectedRows.value.length === 1 ? selectedRows.value[0] : null,
)

const hasMoreActions = computed(
  () =>
    userStore.hasPermission(`${props.tableName}.Delete`) ||
    userStore.hasPermission(`${props.tableName}.Import`) ||
    userStore.hasPermission(`${props.tableName}.Export`),
)

const toolbarButtons = computed(() =>
  (props.extension?.toolbarButtons ?? []).filter(
    (btn) => !btn.permission || userStore.hasPermission(btn.permission),
  ),
)

const rowButtons = computed(() =>
  (props.extension?.rowButtons ?? []).filter(
    (btn) => !btn.permission || userStore.hasPermission(btn.permission),
  ),
)

const actionsColumnWidth = computed(() => {
  const base = 2
  const submitAuditExtra = props.depth === 0 && props.extension?.hooks?.submitAudit ? 1 : 0
  const extra = props.depth === 0 ? rowButtons.value.length + submitAuditExtra : 0
  return Math.max(180, (base + extra) * 88)
})

const detailTablesColumnWidth = computed(() =>
  Math.max(120, entryDetailTables.value.length * 100),
)

function buildEmptyForm(): Record<string, unknown> {
  const base: Record<string, unknown> = { [props.keyField]: 0 }
  if (props.formDefaults && Object.keys(props.formDefaults).length) {
    Object.assign(base, props.formDefaults)
    base[props.keyField] = 0
  } else {
    for (const f of props.formFields) {
      if (f.defaultValue !== undefined) base[f.prop] = f.defaultValue
      else if (f.kind === 'number') base[f.prop] = 0
      else if (f.kind === 'bool') base[f.prop] = false
      else base[f.prop] = ''
    }
  }
  if (props.fixedFilter) Object.assign(base, props.fixedFilter)
  return base
}

const form = reactive<Record<string, unknown>>(buildEmptyForm())

function formatDate(v: unknown) {
  if (v == null || v === '') return ''
  return String(v).replace('T', ' ').slice(0, 19)
}

function displayCellValue(v: unknown) {
  return v == null ? '' : String(v)
}

function enumLabel(
  options: { value: number | string; label: string }[] | null | undefined,
  v: unknown,
) {
  const hit = (options ?? []).find((o) => o.value === v || o.value === Number(v))
  return hit?.label ?? (v == null ? '' : String(v))
}

function columnLabel(prop: string) {
  return t(`${props.i18nKey}.${prop}`)
}

function panelActionLabel(action: string) {
  return `${panelTitle.value} ${action}`
}

function editRowLabel(row: Record<string, unknown>, prop: string) {
  const value = displayCellValue(row[prop]) || columnLabel(prop)
  return `${t(`${props.i18nKey}.edit`)} ${value}`
}

function rowActionGroupLabel(row: Record<string, unknown>) {
  const actionLabel = t(`${props.i18nKey}.actions`)
  const rowKey = displayCellValue(row[props.keyField])
  return rowKey ? `${actionLabel} ${rowKey}` : actionLabel
}

function rowDetailGroupLabel(row: Record<string, unknown>) {
  const detailLabel = t('common.detailTables')
  const rowKey = displayCellValue(row[props.keyField])
  return rowKey ? `${detailLabel} ${rowKey}` : detailLabel
}

function tableColumnClassName(col: ColumnDef, colIdx: number) {
  return [
    'crud-table__cell',
    col.kind === 'number' ? 'crud-table__cell--numeric' : '',
    colIdx === 0 ? 'crud-table__cell--primary' : '',
  ].filter(Boolean).join(' ')
}

function tableHeaderClassName(col: ColumnDef, colIdx: number) {
  return [
    'crud-table__head',
    col.kind === 'number' ? 'crud-table__head--numeric' : '',
    colIdx === 0 ? 'crud-table__head--primary' : '',
  ].filter(Boolean).join(' ')
}

function detailTableColumnClassName(col: DetailColumnConfig, colIdx: number) {
  return [
    'crud-table__cell',
    col.kind === 'number' ? 'crud-table__cell--numeric' : '',
    colIdx === 0 ? 'crud-table__cell--primary' : '',
  ].filter(Boolean).join(' ')
}

function detailTableHeaderClassName(col: DetailColumnConfig, colIdx: number) {
  return [
    'crud-table__head',
    col.kind === 'number' ? 'crud-table__head--numeric' : '',
    colIdx === 0 ? 'crud-table__head--primary' : '',
  ].filter(Boolean).join(' ')
}

function searchFieldLabel(f: SearchFieldConfig) {
  if (f.label) return f.label
  if (f.labelKey) return t(f.labelKey)
  return t(`${props.i18nKey}.${f.prop}`)
}

function detailColLabel(col: DetailColumnConfig) {
  if (col.label) return col.label
  if (col.labelKey) return t(col.labelKey)
  return col.prop
}

function fieldDictionaryKey(f: { dicNo?: string; dataSource?: string }) {
  return f.dicNo || f.dataSource
}

function collectDicNosFromDetailTables(tables: DetailTableConfig[] | undefined, out: Set<string>) {
  for (const dt of tables ?? []) {
    for (const f of [...(dt.searchFields ?? []), ...(dt.formFields ?? [])]) {
      const key = fieldDictionaryKey(f)
      if (key) out.add(key)
    }
    for (const c of dt.columns) {
      if (c.kind === 'enum') {
        const key = fieldDictionaryKey(c)
        if (key) out.add(key)
      }
    }
    collectDicNosFromDetailTables(dt.children, out)
  }
}

function collectDicNos(): string[] {
  const set = new Set<string>()
  for (const f of [...resolvedSearchFields.value, ...props.formFields]) {
    const key = fieldDictionaryKey(f)
    if (key) set.add(key)
  }
  for (const c of props.columns) {
    if (c.kind === 'enum') {
      const key = fieldDictionaryKey(c)
      if (key) set.add(key)
    }
  }
  collectDicNosFromDetailTables(props.detailTables, set)
  return [...set]
}

async function ensureDictionariesLoaded() {
  const nos = collectDicNos()
  if (nos.length) await dictStore.loadDictionaries(nos)
}

const mergedEnumOptionsMap = computed(() => {
  const map: Record<string, { value: number | string; label: string }[]> = {
    ...props.enumOptionsMap,
  }
  const bindField = (f: { prop: string; dicNo?: string; dataSource?: string }) => {
    const no = fieldDictionaryKey(f)
    if (!no || map[f.prop]?.length) return
    map[f.prop] = dictStore.getOptions(no)
  }
  for (const f of [...resolvedSearchFields.value, ...props.formFields]) bindField(f)
  for (const c of props.columns) {
    if (c.kind === 'enum') bindField(c)
  }
  return map
})

function searchFieldOptions(f: SearchFieldConfig) {
  if (f.options?.length) return f.options
  if (f.kind === 'bool') return boolSearchOptions.value
  return mergedEnumOptionsMap.value[f.prop] ?? []
}

function formFieldOptions(f: FormFieldDef) {
  if (f.options?.length) return f.options
  if (f.kind === 'bool') return boolSearchOptions.value
  return mergedEnumOptionsMap.value[f.prop] ?? []
}

function detailState(key: string) {
  if (!detailDataMap[key]) detailDataMap[key] = { loading: false, rows: [] }
  return detailDataMap[key]
}

function detailBusy(dt: DetailTableConfig) {
  return !!activeMasterRow.value && !dt.apiRoute && detailState(dt.key).loading
}

function buildWheresJson() {
  const list: { name: string; value: string; displayType: string }[] = []
  if (props.fixedFilter) {
    for (const [name, value] of Object.entries(props.fixedFilter)) {
      if (value === undefined || value === null || value === '') continue
      list.push({ name, value: String(value), displayType: 'equal' })
    }
  }
  for (const f of resolvedSearchFields.value) {
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
  const rowId = Number(row[props.keyField] ?? 0)
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

async function onSubmitAudit(row: Record<string, unknown>) {
  await props.extension?.hooks?.submitAudit?.({ row, tableName: props.tableName })
}

function hasAuditStatus(row: Record<string, unknown>) {
  return row.auditStatus !== undefined && row.auditStatus !== null
}

function openAudit(row: Record<string, unknown>) {
  const key = row[props.keyField]
  if (key == null || key === '') {
    ElMessage.warning(t('sysWorkFlow.noBizKey'))
    return
  }
  auditTableKey.value = String(key)
  auditVisible.value = true
}

function guessKeyField(dt: DetailTableConfig): string {
  if (dt.keyField) return dt.keyField
  if (!dt.apiRoute) return 'id'
  const camel = dt.apiRoute.charAt(0).toLowerCase() + dt.apiRoute.slice(1)
  return camel.endsWith('Id') ? camel : `${camel}Id`
}

function mapDetailColumns(dt: DetailTableConfig): ColumnDef[] {
  return dt.columns.map((c) => ({
    prop: c.prop,
    kind: (c.kind === 'number' || c.kind === 'enum' || c.kind === 'bool' || c.kind === 'date'
      ? c.kind
      : 'string') as ColumnDef['kind'],
    sortable: !!c.sortable,
  }))
}

function buildNestedFormDefaults(dt: DetailTableConfig): Record<string, unknown> {
  const key = guessKeyField(dt)
  const o: Record<string, unknown> = { [key]: 0 }
  for (const f of dt.formFields ?? []) {
    if (f.defaultValue !== undefined) o[f.prop] = f.defaultValue
    else if (f.kind === 'number') o[f.prop] = 0
    else if (f.kind === 'bool') o[f.prop] = false
    else o[f.prop] = ''
  }
  return o
}

function buildNestedEnumMap(
  dt: DetailTableConfig,
): Record<string, { value: number | string; label: string }[]> {
  const map: Record<string, { value: number | string; label: string }[]> = {
    ...mergedEnumOptionsMap.value,
  }
  for (const f of [...(dt.formFields ?? []), ...(dt.searchFields ?? [])]) {
    if (f.options?.length) map[f.prop] = f.options
    else {
      const no = fieldDictionaryKey(f)
      if (no) map[f.prop] = dictStore.getOptions(no)
    }
  }
  for (const c of dt.columns) {
    if (c.kind === 'enum') {
      const no = fieldDictionaryKey(c)
      if (no && !map[c.prop]?.length) map[c.prop] = dictStore.getOptions(no)
    }
  }
  return map
}

function masterIdOf(dt: DetailTableConfig, master: Record<string, unknown>) {
  const masterKey = dt.masterKey || props.keyField
  return master[masterKey]
}

function nestedFixedFilter(dt: DetailTableConfig, master: Record<string, unknown>) {
  if (!dt.foreignKey) return {}
  const id = masterIdOf(dt, master)
  return { [dt.foreignKey]: id }
}

function nestedPanelProps(dt: DetailTableConfig, master: Record<string, unknown>) {
  const route = dt.apiRoute!
  const nextDepth = props.depth + 1
  const children = nextDepth < props.maxDepth ? (dt.children ?? []) : []
  return {
    apiRoute: route,
    i18nKey: `generated.${route}`,
    tableName: route,
    keyField: guessKeyField(dt),
    columns: mapDetailColumns(dt),
    formFields: dt.formFields ?? [],
    formDefaults: buildNestedFormDefaults(dt),
    enumOptionsMap: buildNestedEnumMap(dt),
    searchFields: dt.searchFields ?? [],
    fixedFilter: nestedFixedFilter(dt, master),
    detailTables: children,
    depth: nextDepth,
    maxDepth: props.maxDepth,
    title: dt.title,
    showCard: true,
    pageSize: dt.pageSize ?? 30,
  }
}

async function loadDetailRows(cfg: DetailTableConfig, master: Record<string, unknown>) {
  if (cfg.load) return await cfg.load(master, buildActionContext())
  if (!cfg.apiRoute || !cfg.foreignKey) return []
  const masterId = masterIdOf(cfg, master)
  if (masterId == null || masterId === '') return []
  const wheres = JSON.stringify([
    { name: cfg.foreignKey, value: String(masterId), displayType: 'equal' },
  ])
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
    if (dt.apiRoute) continue
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

function onDetailEntry(cfg: DetailTableConfig, row: Record<string, unknown>) {
  if (cfg.mode === 'page') {
    openDetailPage(cfg, row)
    return
  }
  void openDetailDialog(cfg, row)
}

function openDetailPage(cfg: DetailTableConfig, row: Record<string, unknown>) {
  if (!cfg.apiRoute || !cfg.foreignKey) {
    ElMessage.warning(t('common.operationFailed'))
    return
  }
  const id = masterIdOf(cfg, row)
  if (id == null || id === '') {
    ElMessage.warning(t('common.operationFailed'))
    return
  }
  const path = `/${cfg.apiRoute}?${cfg.foreignKey}=${encodeURIComponent(String(id))}`
  tabsStore.addTab(cfg.title, path, cfg.apiRoute)
  void router.push(path)
}

async function openDetailDialog(cfg: DetailTableConfig, row: Record<string, unknown>) {
  detailDialogCfg.value = cfg
  detailDialogMaster.value = row
  detailDialogVisible.value = true
  detailDialogRows.value = []
  if (cfg.apiRoute) return
  detailDialogLoading.value = true
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
  for (const dt of belowDetailTables.value) {
    if (!dt.apiRoute) detailState(dt.key).rows = []
  }
  try {
    const res = await getPageData(`/api/${props.apiRoute}/getPageData`, {
      page: page.value,
      rows: innerPageSize.value,
      sort: sort.value || undefined,
      order: order.value || undefined,
      wheres: buildWheresJson(),
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  void loadData()
}

function onResetSearch() {
  for (const f of resolvedSearchFields.value) searchModel[f.prop] = undefined
  page.value = 1
  void loadData()
}

function onSortChange(payload: { prop: string; order: string | null }) {
  sort.value = payload.prop || ''
  order.value =
    payload.order === 'ascending' ? 'asc' : payload.order === 'descending' ? 'desc' : ''
  void loadData()
}

async function openForm(row?: Record<string, unknown>) {
  const mode = row ? 'edit' : 'add'
  Object.assign(form, buildEmptyForm(), row ?? {})
  if (!row) {
    form[props.keyField] = 0
    if (props.fixedFilter) Object.assign(form, props.fixedFilter)
  }
  const ok = await runHook(
    props.extension?.hooks?.beforeOpenForm?.({ mode, row, form }),
  )
  if (!ok) return
  dialogVisible.value = true
}

function onRowDblClick(row: Record<string, unknown>) {
  if (userStore.hasPermission(`${props.tableName}.Update`)) {
    void openForm(row)
  }
}

async function save() {
  const key = Number(form[props.keyField] ?? 0)
  const mode = key > 0 ? 'edit' : 'add'
  const ok = await runHook(props.extension?.hooks?.beforeSave?.({ mode, form }))
  if (!ok) return
  const url =
    mode === 'edit'
      ? `/api/${props.apiRoute}/update`
      : `/api/${props.apiRoute}/add`
  const payload = { ...form, [props.keyField]: key }
  if (props.fixedFilter) Object.assign(payload, props.fixedFilter)
  saving.value = true
  try {
    const res = await http.post(url, payload)
    if (res.status) {
      ElMessage.success(t('common.success'))
      dialogVisible.value = false
      await props.extension?.hooks?.afterSave?.({ mode, form })
      await loadData()
    } else {
      ElMessage.error(res.message || t('common.operationFailed'))
    }
  } finally {
    saving.value = false
  }
}

function onPageSizeChange() {
  page.value = 1
  void loadData()
}

async function toolbarDelete() {
  if (selectedRows.value.length === 0) {
    ElMessage.warning(t('common.selectRequired'))
    return
  }
  if (selectedRows.value.length === 1) {
    await remove(selectedRows.value[0])
    return
  }
  await batchRemove()
}

async function remove(row: Record<string, unknown>) {
  const id = Number(row[props.keyField] ?? 0)
  if (!id) {
    ElMessage.error(t('common.operationFailed'))
    return
  }
  const hookOk = await runHook(
    props.extension?.hooks?.beforeDelete?.({ ids: [id], rows: [row] }),
  )
  if (!hookOk) return
  try {
    await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), {
      type: 'warning',
    })
  } catch {
    return
  }
  const res = await http.post(`/api/${props.apiRoute}/del`, [id])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await props.extension?.hooks?.afterDelete?.({ ids: [id] })
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
  const hookOk = await runHook(
    props.extension?.hooks?.beforeDelete?.({ ids, rows }),
  )
  if (!hookOk) return
  try {
    await ElMessageBox.confirm(
      t('common.batchDeleteConfirm'),
      t('common.batchDelete'),
      { type: 'warning' },
    )
  } catch {
    return
  }
  const res = await http.post(`/api/${props.apiRoute}/del`, ids)
  if (res.status) {
    ElMessage.success(t('common.success'))
    await props.extension?.hooks?.afterDelete?.({ ids })
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
    const res = await uploadFile(`/api/${props.apiRoute}/import`, file)
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
  const loading = ElMessage({ message: t('common.loading'), duration: 0 })
  try {
    await downloadFile(
      `/api/${props.apiRoute}/export`,
      {
        page: 1,
        rows: 10000,
        sort: sort.value || undefined,
        order: order.value || undefined,
        wheres: buildWheresJson(),
      },
      `${props.tableName}.xlsx`,
    )
    ElMessage.success(t('common.success'))
  } catch {
    ElMessage.error(t('common.operationFailed'))
  } finally {
    loading.close()
  }
}

async function downloadTemplate() {
  try {
    await downloadGet(
      `/api/${props.apiRoute}/exportTemplate`,
      `${props.tableName}_template.xlsx`,
    )
    ElMessage.success(t('common.success'))
  } catch {
    ElMessage.error(t('common.operationFailed'))
  }
}

watch(
  () => [props.columns, props.searchFields, props.formFields, props.detailTables],
  () => {
    void ensureDictionariesLoaded()
  },
  { deep: true, immediate: true },
)

watch(
  () => props.fixedFilter,
  () => {
    page.value = 1
    void loadData()
  },
  { deep: true },
)

onMounted(loadData)
</script>

<style scoped>
.crud-panel,
.crud-main-card {
  min-width: 0;
  max-width: 100%;
}

.crud-main-card :deep(.el-card__header) {
  padding: 8px 12px;
}
.crud-header-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}
.crud-header-left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 16px;
  min-width: 0;
  flex: 1;
}
.crud-title-icon {
  color: var(--seven-accent);
  font-size: 15px;
}
.search-bar--inline {
  margin-bottom: 0;
  min-width: min(100%, 720px);
}
.search-bar--inline :deep(.el-form-item) {
  margin-bottom: 0;
  margin-right: 8px;
}
.search-bar--inline :deep(.el-form-item__content) {
  max-width: 100%;
}
.table-wrap {
  width: 100%;
  max-width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  border: 1px solid var(--seven-table-border, var(--seven-border-light));
  border-radius: var(--seven-radius);
  background: var(--seven-bg-panel);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.5);
}
.table-wrap :deep(.crud-table) {
  min-width: 100%;
}
.table-wrap :deep(.el-table__inner-wrapper) {
  min-width: fit-content;
}
.crud-panel :deep(.el-loading-mask) {
  background: var(--seven-loading-surface, rgba(255, 255, 255, 0.76));
  backdrop-filter: blur(1px);
}
.crud-panel :deep(.el-loading-spinner .el-loading-text) {
  margin-top: 10px;
  color: var(--seven-primary-dark);
  font-size: var(--seven-text-xs);
  letter-spacing: 0.02em;
}
.crud-panel :deep(.crud-table__head .cell) {
  color: var(--seven-table-header-text, var(--seven-primary-dark));
}
.crud-panel :deep(.crud-table__head--numeric .cell),
.crud-panel :deep(.crud-table__cell--numeric .cell) {
  font-family: var(--seven-font-mono);
  font-variant-numeric: tabular-nums;
}
.crud-panel :deep(.crud-table__head--primary .cell),
.crud-panel :deep(.crud-table__cell--primary .cell) {
  font-weight: 600;
}
.crud-panel :deep(.crud-table__selection-head),
.crud-panel :deep(.crud-table__selection-cell) {
  background: var(--seven-table-selection-surface, var(--seven-bg-subtle));
}
.crud-panel :deep(.crud-table__operations-head),
.crud-panel :deep(.crud-table__operations-cell) {
  border-left: 1px solid var(--seven-table-border-strong, var(--seven-border));
}
.crud-panel :deep(.crud-table__operations-head .cell) {
  font-weight: 700;
}
.crud-panel :deep(.el-table__body tr.current-row > td.crud-table__selection-cell),
.crud-panel :deep(.el-table__body tr.current-row > td.crud-table__operations-cell) {
  background-color: var(--seven-table-row-current, var(--seven-row-selected)) !important;
}
.row-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  min-height: var(--seven-table-row-height);
}
.crud-pagination {
  display: flex;
  justify-content: flex-end;
  gap: 8px 12px;
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid var(--seven-border-light);
  justify-content: flex-end;
}
.detail-section {
  margin-top: 12px;
  background: var(--seven-detail-surface, var(--seven-bg-panel));
  border: 1px solid var(--seven-detail-border, var(--seven-border-light));
  border-radius: var(--seven-radius);
  padding: 10px 12px 12px;
  box-shadow: var(--seven-shadow);
}
.detail-section__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--seven-detail-border, var(--seven-border-light));
}
.detail-section__title {
  margin: 0;
  font-size: inherit;
  font-weight: inherit;
}
.detail-section__body {
  min-height: var(--seven-empty-min-height, 136px);
}
.detail-card {
  margin-top: 0;
  border: none;
  box-shadow: none;
  background: transparent;
}
.detail-card :deep(.el-card__body) {
  padding: 0;
}
.detail-hint {
  color: var(--seven-text-muted);
  font-size: 12px;
}
.detail-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: var(--seven-empty-min-height, 136px);
  padding: 24px;
  text-align: center;
  color: var(--seven-text-muted);
  font-size: 13px;
  background: var(--seven-empty-surface, var(--seven-bg-subtle));
  border: 1px dashed var(--seven-detail-border, var(--seven-border));
  border-radius: calc(var(--seven-radius) - 1px);
  overflow-wrap: anywhere;
}
.crud-panel--nested {
  margin-top: 0;
}
.seven-link-cell {
  display: inline-flex;
  align-items: center;
  max-width: 100%;
  overflow-wrap: anywhere;
}
.crud-edit-form :deep(.el-form-item) {
  margin-bottom: 12px;
}
.dialog-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 6px;
}

@media (max-width: 960px) {
  .crud-header-left {
    align-items: stretch;
  }

  .search-bar--inline {
    min-width: 100%;
  }
}

@media (max-width: 768px) {
  .crud-pagination {
    justify-content: flex-start;
  }

  .row-actions {
    gap: 6px;
  }
}
</style>
