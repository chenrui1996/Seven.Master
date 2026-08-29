<!-- 通用 CrudPanel：WmsLocation -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="WmsLocation"
      i18n-key="generated.WmsLocation"
      table-name="WmsLocation"
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
    <component :is="pageExtension.overlay" v-if="pageExtension.overlay" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, watch } from 'vue'
import { useRoute } from 'vue-router'
import pageExtension from '../../extension/Wms/WmsLocation'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'code', kind: 'string', sortable: false },
  { prop: 'warehouseId', kind: 'number', sortable: false },
  { prop: 'packId', kind: 'string', sortable: false },
  { prop: 'aisle', kind: 'string', sortable: false },
  { prop: 'row', kind: 'string', sortable: false },
  { prop: 'column', kind: 'string', sortable: false },
  { prop: 'layer', kind: 'string', sortable: false },
  { prop: 'depth', kind: 'string', sortable: false },
  { prop: 'isOccupied', kind: 'bool', sortable: false },
  { prop: 'isBooked', kind: 'bool', sortable: false },
  { prop: 'isLocked', kind: 'bool', sortable: false },
  { prop: 'currentContainerCode', kind: 'string', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'code', kind: 'string' },
  { prop: 'warehouseId', kind: 'number' },
  { prop: 'zoneId', kind: 'number' },
  { prop: 'layerId', kind: 'number' },
  { prop: 'aisleId', kind: 'number' },
  { prop: 'packId', kind: 'string' },
  { prop: 'aisle', kind: 'string' },
  { prop: 'row', kind: 'string' },
  { prop: 'column', kind: 'string' },
  { prop: 'layer', kind: 'string' },
  { prop: 'depth', kind: 'string' },
  { prop: 'isHandover', kind: 'bool' },
  { prop: 'isLocked', kind: 'bool' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  code: '',
  warehouseId: 0,
  packId: 'stacker',
  isOccupied: false,
  isBooked: false,
  isLocked: false,
  isHandover: false,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'code', kind: 'string', operator: 'like' },
  { prop: 'packId', kind: 'string', operator: 'like' },
  { prop: 'aisle', kind: 'string', operator: 'like' }
]

const generatedDetailTables: DetailTableConfig[] = []

const queryFilterKeys: string[] = []

const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {}

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
