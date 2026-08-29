<!-- 通用 CrudPanel：FwAislePolicy -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="FwAislePolicy"
      i18n-key="generated.FwAislePolicy"
      table-name="FwAislePolicy"
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
import pageExtension from '../../../extension/Wcs/FourWay/FwAislePolicy'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../../extension/types'
import CrudPanel from '../../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../../components/crud/mergeExtension'
import type { ColumnDef } from '../../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'layerCode', kind: 'string', sortable: false },
  { prop: 'aisleCode', kind: 'string', sortable: false },
  { prop: 'isAvailable', kind: 'bool', sortable: false },
  { prop: 'allocationWeight', kind: 'number', sortable: false },
  { prop: 'destinationPointCode', kind: 'string', sortable: false },
  { prop: 'minEmptySlots', kind: 'number', sortable: false },
  { prop: 'maxShuttleCount', kind: 'number', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'layerCode', kind: 'string' },
  { prop: 'aisleCode', kind: 'string' },
  { prop: 'isAvailable', kind: 'bool' },
  { prop: 'allocationWeight', kind: 'number' },
  { prop: 'destinationPointCode', kind: 'string' },
  { prop: 'minEmptySlots', kind: 'number' },
  { prop: 'maxShuttleCount', kind: 'number' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  layerCode: '',
  aisleCode: '',
  isAvailable: true,
  allocationWeight: 1,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'layerCode', kind: 'string', operator: 'like' },
  { prop: 'aisleCode', kind: 'string', operator: 'like' }
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
