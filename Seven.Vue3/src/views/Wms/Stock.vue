<!-- 通用 CrudPanel：WmsStock -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="WmsStock"
      i18n-key="generated.WmsStock"
      table-name="WmsStock"
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
import pageExtension from '../../extension/Wms/WmsStock'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'locationCode', kind: 'string', sortable: false },
  { prop: 'materialCode', kind: 'string', sortable: false },
  { prop: 'containerCode', kind: 'string', sortable: false },
  { prop: 'lot', kind: 'string', sortable: false },
  { prop: 'qty', kind: 'number', sortable: false },
  { prop: 'availableQty', kind: 'number', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'locationCode', kind: 'string' },
  { prop: 'materialCode', kind: 'string' },
  { prop: 'containerCode', kind: 'string' },
  { prop: 'lot', kind: 'string' },
  { prop: 'qty', kind: 'number' },
  { prop: 'availableQty', kind: 'number' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  locationCode: '',
  materialCode: '',
  qty: 0,
  availableQty: 0,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'locationCode', kind: 'string', operator: 'like' },
  { prop: 'materialCode', kind: 'string', operator: 'like' },
  { prop: 'containerCode', kind: 'string', operator: 'like' }
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
