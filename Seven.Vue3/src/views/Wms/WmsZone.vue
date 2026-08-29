<!-- 通用 CrudPanel：WmsZone -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="WmsZone"
      i18n-key="generated.WmsZone"
      table-name="WmsZone"
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
import pageExtension from '../../extension/Wms/WmsZone'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'warehouseId', kind: 'number', sortable: false },
  { prop: 'packId', kind: 'string', sortable: false },
  { prop: 'code', kind: 'string', sortable: false },
  { prop: 'name', kind: 'string', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'warehouseId', kind: 'number' },
  { prop: 'packId', kind: 'string' },
  { prop: 'code', kind: 'string' },
  { prop: 'name', kind: 'string' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  warehouseId: 0,
  packId: 'stacker',
  code: '',
  name: '',
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'code', kind: 'string', operator: 'like' },
  { prop: 'packId', kind: 'string', operator: 'like' }
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
