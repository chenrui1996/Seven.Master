<!--
  代码由框架生成，重新生成会覆盖本文件。
  业务逻辑请写在：../../extension/Business/Device.ts
-->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="Device"
      i18n-key="generated.Device"
      table-name="Device"
      key-field="deviceId"
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
import pageExtension from '../../extension/Business/Device'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()

const allColumns: ColumnDef[] = [
  { prop: 'deviceId', kind: 'number', sortable: false },
  { prop: 'deviceName', kind: 'string', sortable: false },
  { prop: 'deviceCode', kind: 'string', sortable: false },
  { prop: 'status', kind: 'enum', sortable: false },
  { prop: 'location', kind: 'string', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'deviceName', kind: 'string' },
  { prop: 'deviceCode', kind: 'string' },
  { prop: 'status', kind: 'enum' },
  { prop: 'location', kind: 'string' },
]

const formDefaults: Record<string, unknown> = {
  deviceId: 0,
  deviceName: '',
  deviceCode: '',
  status: 0,
  location: '',
}

const generatedSearchFields: SearchFieldConfig[] = []

const generatedDetailTables: DetailTableConfig[] = []

const queryFilterKeys: string[] = []

const statusOptions = [
  { value: 0, label: '离线' },
  { value: 1, label: '在线' },
  { value: 2, label: '故障' },
  { value: 3, label: '维护中' },
]

const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {
  status: statusOptions,
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
