<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="CommPoint"
      i18n-key="generated.CommPoint"
      table-name="CommPoint"
      key-field="commPointId"
      :columns="allColumns"
      :form-fields="formFields"
      :form-defaults="formDefaults"
      :enum-options-map="enumOptionsMap"
      :search-fields="[]"
      :extension="pageExtension"
    />
  </div>
</template>

<script setup lang="ts">
import pageExtension from '../../extension/DeviceComm/CommPoint'
import type { FormFieldDef } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import type { ColumnDef } from '../../composables/useTableColumns'

const allColumns: ColumnDef[] = [
  { prop: 'commPointId', kind: 'number', sortable: false },
  { prop: 'commConnectionId', kind: 'number', sortable: false },
  { prop: 'code', kind: 'string', sortable: false },
  { prop: 'name', kind: 'string', sortable: false },
  { prop: 'dataType', kind: 'enum', sortable: false },
  { prop: 'address', kind: 'string', sortable: false },
  { prop: 'dbNumber', kind: 'number', sortable: false },
  { prop: 'byteOffset', kind: 'number', sortable: false },
  { prop: 'modbusArea', kind: 'enum', sortable: false },
  { prop: 'modbusAddress', kind: 'number', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'commConnectionId', kind: 'number' },
  { prop: 'code', kind: 'string' },
  { prop: 'name', kind: 'string' },
  { prop: 'dataType', kind: 'enum' },
  { prop: 'address', kind: 'string' },
  { prop: 'dbNumber', kind: 'number' },
  { prop: 'byteOffset', kind: 'number' },
  { prop: 'bitOffset', kind: 'number' },
  { prop: 'modbusArea', kind: 'enum' },
  { prop: 'modbusAddress', kind: 'number' },
  { prop: 'quantity', kind: 'number' },
  { prop: 'remark', kind: 'string' },
]

const formDefaults: Record<string, unknown> = {
  commPointId: 0,
  commConnectionId: 0,
  code: '',
  name: '',
  dataType: 3,
  address: '',
  quantity: 1,
}

const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {
  dataType: [
    { value: 1, label: 'Bool' },
    { value: 2, label: 'Byte' },
    { value: 3, label: 'Int16' },
    { value: 4, label: 'UInt16' },
    { value: 5, label: 'Int32' },
    { value: 6, label: 'UInt32' },
    { value: 7, label: 'Float' },
    { value: 8, label: 'Double' },
    { value: 9, label: 'String' },
  ],
  modbusArea: [
    { value: 1, label: 'Coil' },
    { value: 2, label: 'DiscreteInput' },
    { value: 3, label: 'HoldingRegister' },
    { value: 4, label: 'InputRegister' },
  ],
}
</script>
