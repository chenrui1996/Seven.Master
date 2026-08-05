<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="CommConnection"
      i18n-key="generated.CommConnection"
      table-name="CommConnection"
      key-field="commConnectionId"
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
import pageExtension from '../../extension/DeviceComm/CommConnection'
import type { FormFieldDef } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import type { ColumnDef } from '../../composables/useTableColumns'

const allColumns: ColumnDef[] = [
  { prop: 'commConnectionId', kind: 'number', sortable: false },
  { prop: 'name', kind: 'string', sortable: false },
  { prop: 'protocol', kind: 'enum', sortable: false },
  { prop: 'host', kind: 'string', sortable: false },
  { prop: 'port', kind: 'number', sortable: false },
  { prop: 'enabled', kind: 'bool', sortable: false },
  { prop: 'autoConnect', kind: 'bool', sortable: false },
  { prop: 'rack', kind: 'number', sortable: false },
  { prop: 'slot', kind: 'number', sortable: false },
  { prop: 'cpuType', kind: 'string', sortable: false },
  { prop: 'unitId', kind: 'number', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'name', kind: 'string' },
  { prop: 'protocol', kind: 'enum' },
  { prop: 'host', kind: 'string' },
  { prop: 'port', kind: 'number' },
  { prop: 'enabled', kind: 'bool' },
  { prop: 'autoConnect', kind: 'bool' },
  { prop: 'rack', kind: 'number' },
  { prop: 'slot', kind: 'number' },
  { prop: 'cpuType', kind: 'string' },
  { prop: 'unitId', kind: 'number' },
  { prop: 'remark', kind: 'string' },
]

const formDefaults: Record<string, unknown> = {
  commConnectionId: 0,
  name: '',
  protocol: 1,
  host: '127.0.0.1',
  port: 102,
  enabled: true,
  autoConnect: true,
  rack: 0,
  slot: 1,
  cpuType: 'S71200',
  unitId: 1,
  remark: '',
}

const enumOptionsMap: Record<string, { value: number | string; label: string }[]> = {
  protocol: [
    { value: 1, label: 'Step7' },
    { value: 2, label: 'ModbusTcp' },
  ],
}
</script>
