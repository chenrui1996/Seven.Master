<template>
  <div class="crud-page seven-page">
    <CrudPanel
      api-route="CommRule"
      i18n-key="generated.CommRule"
      table-name="CommRule"
      key-field="commRuleId"
      :columns="allColumns"
      :form-fields="formFields"
      :form-defaults="formDefaults"
      :search-fields="[]"
      :extension="pageExtension"
    />
  </div>
</template>

<script setup lang="ts">
import pageExtension from '../../extension/DeviceComm/CommRule'
import type { FormFieldDef } from '../../extension/types'
import CrudPanel from '../../components/crud/CrudPanel.vue'
import type { ColumnDef } from '../../composables/useTableColumns'

const allColumns: ColumnDef[] = [
  { prop: 'commRuleId', kind: 'number', sortable: false },
  { prop: 'name', kind: 'string', sortable: false },
  { prop: 'commConnectionId', kind: 'number', sortable: false },
  { prop: 'enabled', kind: 'bool', sortable: false },
  { prop: 'scanIntervalMs', kind: 'number', sortable: false },
  { prop: 'eventName', kind: 'string', sortable: false },
  { prop: 'definitionJson', kind: 'string', sortable: false },
]

const formFields: FormFieldDef[] = [
  { prop: 'name', kind: 'string' },
  { prop: 'commConnectionId', kind: 'number' },
  { prop: 'enabled', kind: 'bool' },
  { prop: 'scanIntervalMs', kind: 'number' },
  { prop: 'eventName', kind: 'string' },
  { prop: 'definitionJson', kind: 'string' },
  { prop: 'remark', kind: 'string' },
]

const formDefaults: Record<string, unknown> = {
  commRuleId: 0,
  name: '',
  enabled: true,
  eventName: '',
  definitionJson: JSON.stringify(
    {
      type: 'MonitorThenRead',
      monitor: [{ point: 'ReqFlag', op: 'rising', value: true }],
      read: ['ReqId', 'ReqQty'],
      emit: { eventName: 'InboundRequest' },
    },
    null,
    2,
  ),
}
</script>
