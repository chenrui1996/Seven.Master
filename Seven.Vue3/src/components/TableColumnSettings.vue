<template>
  <el-dialog
    :model-value="modelValue"
    :title="t('common.columnSettings')"
    width="520px"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-alert
      :title="t('common.columnSettingsTip')"
      type="success"
      :closable="false"
      show-icon
      style="margin-bottom: 12px"
    />
    <el-table :data="draft" border row-key="prop">
      <el-table-column type="index" label="#" width="56" />
      <el-table-column :label="t('common.columnName')">
        <template #default="{ row }">
          <span
            class="col-drag"
            draggable="true"
            @dragstart="onItemDragStart(row.prop, $event)"
            @dragover.prevent
            @drop="onItemDrop(row.prop, $event)"
          >{{ labelOf(row.prop) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('common.columnVisible')" width="100" align="center">
        <template #default="{ row }">
          <el-checkbox v-model="row.visible" />
        </template>
      </el-table-column>
    </el-table>
    <template #footer>
      <div class="dialog-footer-actions">
        <el-button @click="emit('update:modelValue', false)">{{ t('common.cancel') }}</el-button>
        <el-button :icon="ActionIcons.sync" @click="onReset">{{ t('common.reset') }}</el-button>
        <el-button type="primary" :icon="ActionIcons.save" @click="onSave">{{ t('common.save') }}</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ActionIcons } from '../constants/actionIcons'
import type { ColumnPref } from '../composables/useTableColumns'

const props = defineProps<{
  modelValue: boolean
  columns: { prop: string; label: string }[]
  prefs: ColumnPref[]
}>()

const emit = defineEmits<{
  'update:modelValue': [boolean]
  save: [ColumnPref[]]
  reset: []
}>()

const { t } = useI18n()
const draft = ref<ColumnPref[]>([])
let dragProp = ''

watch(
  () => [props.modelValue, props.prefs] as const,
  ([visible]) => {
    if (visible) {
      draft.value = props.prefs.map((p) => ({ ...p }))
    }
  },
  { immediate: true, deep: true },
)

function labelOf(prop: string) {
  return props.columns.find((c) => c.prop === prop)?.label ?? prop
}

function onItemDragStart(prop: string, e: DragEvent) {
  dragProp = prop
  e.dataTransfer?.setData('text/plain', prop)
}

function onItemDrop(targetProp: string, e: DragEvent) {
  e.preventDefault()
  const from = dragProp || e.dataTransfer?.getData('text/plain')
  if (!from || from === targetProp) return
  const list = [...draft.value]
  const fromIdx = list.findIndex((x) => x.prop === from)
  const toIdx = list.findIndex((x) => x.prop === targetProp)
  if (fromIdx < 0 || toIdx < 0) return
  const [item] = list.splice(fromIdx, 1)
  list.splice(toIdx, 0, item)
  draft.value = list
  dragProp = ''
}

function onSave() {
  emit('save', draft.value.map((p) => ({ ...p })))
}

function onReset() {
  emit('reset')
  draft.value = props.columns.map((c) => ({ prop: c.prop, visible: true }))
}
</script>

<style scoped>
.col-drag {
  cursor: grab;
  user-select: none;
}
.col-drag:active {
  cursor: grabbing;
}
.dialog-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
