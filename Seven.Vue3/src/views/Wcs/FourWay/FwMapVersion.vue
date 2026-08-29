<!-- 通用 CrudPanel：FwMapVersion -->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="FwMapVersion"
      i18n-key="generated.FwMapVersion"
      table-name="FwMapVersion"
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
import pageExtension from '../../../extension/Wcs/FourWay/FwMapVersion'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../../extension/types'
import CrudPanel from '../../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../../components/crud/mergeExtension'
import type { ColumnDef } from '../../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'id', kind: 'number', sortable: false },
  { prop: 'layerCode', kind: 'string', sortable: false },
  { prop: 'versionName', kind: 'string', sortable: false },
  { prop: 'isActive', kind: 'bool', sortable: false }
]

const formFields: FormFieldDef[] = [
  { prop: 'layerCode', kind: 'string' },
  { prop: 'versionName', kind: 'string' },
  { prop: 'isActive', kind: 'bool' }
]

const formDefaults: Record<string, unknown> = {
  id: 0,
  layerCode: '',
  versionName: '',
  isActive: true,
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'layerCode', kind: 'string', operator: 'like' },
  { prop: 'versionName', kind: 'string', operator: 'like' }
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
