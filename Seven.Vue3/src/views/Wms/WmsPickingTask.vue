<!-- 通用 CrudPanel：WmsPickingTask -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="WmsPickingTask"
      i18n-key="generated.WmsPickingTask"
      table-name="WmsPickingTask"
      key-field="id"
      :columns="allColumns"
      :form-fields="formFields"
      :form-defaults="formDefaults"
      :enum-options-map="enumOptionsMap"
      :search-fields="mergedSearchFields"
      :fixed-filter="fixedFilter"
      :detail-tables="mergedDetailTables"
      :extension="pageExtension"
      :depth="0"
      :max-depth="3"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, watch } from 'vue'
import { useRoute } from 'vue-router'
import pageExtension from '../../extension/Wms/WmsPickingTask'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'taskNo', kind: 'string', sortable: false },
  { prop: 'outboundOrderId', kind: 'number', sortable: false },
  { prop: 'lineId', kind: 'number', sortable: false },
  { prop: 'materialCode', kind: 'string', sortable: false },
  { prop: 'bookQty', kind: 'number', sortable: false },
  { prop: 'pickQty', kind: 'number', sortable: false },
  { prop: 'fromLocation', kind: 'string', sortable: false },
  { prop: 'toLocation', kind: 'string', sortable: false },
  { prop: 'containerCode', kind: 'string', sortable: false },
  { prop: 'wcsPri', kind: 'number', sortable: false },
  { prop: 'status', kind: 'enum', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'taskNo', kind: 'string' },
  { prop: 'materialCode', kind: 'string' },
  { prop: 'bookQty', kind: 'number', isDecimal: true },
  { prop: 'fromLocation', kind: 'string' },
  { prop: 'toLocation', kind: 'string' },
  { prop: 'containerCode', kind: 'string' },
  { prop: 'wcsPri', kind: 'number' },
  { prop: 'status', kind: 'enum' },
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  taskNo: '',
  bookQty: 0,
  pickQty: 0,
  wcsPri: 1,
  status: 10,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'taskNo', kind: 'string', operator: 'like' },
  { prop: 'materialCode', kind: 'string', operator: 'like' },
  { prop: 'status', kind: 'enum', operator: 'equal' },
]

const generatedDetailTables: DetailTableConfig[] = []
const queryFilterKeys: string[] = ['outboundOrderId', 'lineId']
const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {
  status: [
    { value: 10, label: '已预约' },
    { value: 20, label: '已确认' },
    { value: 30, label: '运输中' },
    { value: 40, label: '已完成' },
    { value: 90, label: '已取消' },
  ],
}

const mergedSearchFields = computed(() =>
  mergeSearchFields(generatedSearchFields, pageExtension.searchFields),
)
const mergedDetailTables = computed(() =>
  mergeDetailTables(generatedDetailTables, pageExtension.detailTables),
)
const fixedFilter = reactive<Record<string, unknown>>({})

function applyQueryFilters() {
  for (const key of Object.keys(fixedFilter)) delete fixedFilter[key]
  for (const key of queryFilterKeys) {
    const raw = route.query[key]
    const val = Array.isArray(raw) ? raw[0] : raw
    if (val == null || val === '') continue
    const num = Number(val)
    fixedFilter[key] = Number.isFinite(num) && String(num) === String(val) ? num : val
  }
}

onMounted(applyQueryFilters)
watch(() => route.query, applyQueryFilters, { deep: true })
</script>
