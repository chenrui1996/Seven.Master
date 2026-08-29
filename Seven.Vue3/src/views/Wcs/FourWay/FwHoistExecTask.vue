<!-- 通用 CrudPanel：FwHoistExecTask -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="FwHoistExecTask"
      i18n-key="generated.FwHoistExecTask"
      table-name="FwHoistExecTask"
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
import pageExtension from '../../../extension/Wcs/FourWay/FwHoistExecTask'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../../extension/types'
import CrudPanel from '../../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../../components/crud/mergeExtension'
import type { ColumnDef } from '../../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'string', sortable: false },
  { prop: 'hoistTaskId', kind: 'string', sortable: false },
  { prop: 'status', kind: 'number', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'status', kind: 'number' }
]

const formDefaults: Record<string, unknown> = {
  id: '',
  status: 0,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'status', kind: 'string', operator: 'like' }
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
