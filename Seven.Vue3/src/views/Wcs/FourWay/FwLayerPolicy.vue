<!-- 通用 CrudPanel：FwLayerPolicy -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="FwLayerPolicy"
      i18n-key="generated.FwLayerPolicy"
      table-name="FwLayerPolicy"
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
import pageExtension from '../../../extension/Wcs/FourWay/FwLayerPolicy'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../../extension/types'
import CrudPanel from '../../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../../components/crud/mergeExtension'
import type { ColumnDef } from '../../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'warehouseCode', kind: 'string', sortable: false },
  { prop: 'zoneCode', kind: 'string', sortable: false },
  { prop: 'layerCode', kind: 'string', sortable: false },
  { prop: 'isAvailable', kind: 'bool', sortable: false },
  { prop: 'allocationWeight', kind: 'number', sortable: false },
  { prop: 'maxHeight', kind: 'number', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'warehouseCode', kind: 'string' },
  { prop: 'zoneCode', kind: 'string' },
  { prop: 'layerCode', kind: 'string' },
  { prop: 'isAvailable', kind: 'bool' },
  { prop: 'allocationWeight', kind: 'number' },
  { prop: 'maxHeight', kind: 'number' },
  { prop: 'maxWeight', kind: 'number' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  warehouseCode: '',
  zoneCode: '',
  layerCode: '',
  isAvailable: true,
  allocationWeight: 1,
  maxHeight: 9999,
  maxWeight: 99999,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'layerCode', kind: 'string', operator: 'like' },
  { prop: 'warehouseCode', kind: 'string', operator: 'like' }
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
