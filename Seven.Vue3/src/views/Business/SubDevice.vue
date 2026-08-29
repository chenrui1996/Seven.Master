<!--
  代码由框架生成，重新生成会覆盖本文件。
  业务逻辑请写在：../../extension/Business/SubDevice.ts
-->
<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="SubDevice"
      i18n-key="generated.SubDevice"
      table-name="SubDevice"
      key-field="subDeviceId"
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
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import pageExtension from '../../extension/Business/SubDevice'
import type { DetailTableConfig, FormFieldDef, SearchFieldConfig } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import { mergeDetailTables, mergeSearchFields } from '../../components/crud/mergeExtension'
import type { ColumnDef } from '../../composables/useTableColumns'

const route = useRoute()
const { t } = useI18n()

const allColumns: ColumnDef[] = [
  { prop: 'subDeviceId', kind: 'number', sortable: false },
  { prop: 'deviceId', kind: 'number', sortable: false },
  { prop: 'subDeviceName', kind: 'string', sortable: false },
  { prop: 'subDeviceCode', kind: 'string', sortable: false },
  { prop: 'status', kind: 'enum', sortable: false },
  { prop: 'remark', kind: 'string', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'deviceId', kind: 'number' },
  { prop: 'subDeviceName', kind: 'string' },
  { prop: 'subDeviceCode', kind: 'string' },
  { prop: 'status', kind: 'enum' },
  { prop: 'remark', kind: 'string' },
]

const formDefaults: Record<string, unknown> = {
  subDeviceId: 0,
  deviceId: 0,
  subDeviceName: '',
  subDeviceCode: '',
  status: 0,
  remark: '',
}

const generatedSearchFields: SearchFieldConfig[] = [
  { prop: 'subDeviceName', kind: 'string', operator: 'like', labelKey: 'generated.SubDevice.subDeviceName' },
  { prop: 'subDeviceCode', kind: 'string', operator: 'like', labelKey: 'generated.SubDevice.subDeviceCode' },
]

const generatedDetailTables: DetailTableConfig[] = []

/** Page 模式：主表跳转时携带 ?deviceId= */
const queryFilterKeys: string[] = ['deviceId']

const statusOptions = computed(() => [
  { value: 0, label: t('generated.SubDevice.enum_status_0') },
  { value: 1, label: t('generated.SubDevice.enum_status_1') },
  { value: 2, label: t('generated.SubDevice.enum_status_2') },
  { value: 3, label: t('generated.SubDevice.enum_status_3') },
])

const enumOptionsMap = computed(() => ({
  status: statusOptions.value,
}))

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
