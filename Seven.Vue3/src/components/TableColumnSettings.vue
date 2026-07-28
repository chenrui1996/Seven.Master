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
    <div class="col-settings-table">
      <div class="col-settings-head">
        <span class="col-idx">#</span>
        <span class="col-name">{{ t('common.columnName') }}</span>
        <span class="col-vis">{{ t('common.columnVisible') }}</span>
      </div>
      <div
        v-for="(row, index) in draft"
        :key="row.prop"
        class="col-settings-row"
        :class="{ dragging: dragIndex === index, 'drag-over': overIndex === index && dragIndex !== index }"
        draggable="true"
        @dragstart="onDragStart(index, $event)"
        @dragenter.prevent="onDragEnter(index)"
        @dragover.prevent="onDragOver(index, $event)"
        @dragleave="onDragLeave(index)"
        @drop.prevent="onDrop(index)"
        @dragend="onDragEnd"
      >
        <span class="col-idx">{{ index + 1 }}</span>
        <span class="col-name">
          <span class="col-drag-handle" aria-hidden="true">⋮⋮</span>
          {{ labelOf(row.prop) }}
        </span>
        <span class="col-vis" @click.stop @mousedown.stop>
          <el-checkbox v-model="row.visible" />
        </span>
      </div>
    </div>
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
const dragIndex = ref(-1)
const overIndex = ref(-1)

watch(
  () => [props.modelValue, props.prefs] as const,
  ([visible]) => {
    if (visible) {
      draft.value = props.prefs.map((p) => ({ ...p }))
      dragIndex.value = -1
      overIndex.value = -1
    }
  },
  { immediate: true, deep: true },
)

function labelOf(prop: string) {
  return props.columns.find((c) => c.prop === prop)?.label ?? prop
}

function onDragStart(index: number, e: DragEvent) {
  dragIndex.value = index
  overIndex.value = index
  e.dataTransfer!.effectAllowed = 'move'
  e.dataTransfer!.setData('text/plain', String(index))
  // 部分浏览器需异步加 class，避免拖影消失
  requestAnimationFrame(() => {
    ;(e.target as HTMLElement | null)?.classList.add('is-dragging')
  })
}

function onDragEnter(index: number) {
  if (dragIndex.value < 0 || index === dragIndex.value) return
  overIndex.value = index
}

function onDragOver(index: number, e: DragEvent) {
  e.dataTransfer!.dropEffect = 'move'
  if (dragIndex.value < 0 || index === dragIndex.value) return
  overIndex.value = index
}

function onDragLeave(index: number) {
  if (overIndex.value === index) overIndex.value = -1
}

function onDrop(index: number) {
  const from = dragIndex.value
  if (from < 0 || from === index) {
    onDragEnd()
    return
  }
  const list = [...draft.value]
  const [item] = list.splice(from, 1)
  list.splice(index, 0, item)
  draft.value = list
  onDragEnd()
}

function onDragEnd() {
  dragIndex.value = -1
  overIndex.value = -1
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
.col-settings-table {
  border: 1px solid var(--el-border-color);
  border-radius: 4px;
  overflow: hidden;
}
.col-settings-head,
.col-settings-row {
  display: grid;
  grid-template-columns: 48px 1fr 96px;
  align-items: center;
  min-height: 40px;
}
.col-settings-head {
  background: var(--el-fill-color-light);
  font-weight: 600;
  color: var(--el-text-color-regular);
  border-bottom: 1px solid var(--el-border-color);
}
.col-settings-row {
  border-bottom: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
  cursor: grab;
  user-select: none;
  transition: background 0.12s ease;
}
.col-settings-row:last-child {
  border-bottom: none;
}
.col-settings-row:active {
  cursor: grabbing;
}
.col-settings-row.dragging {
  opacity: 0.45;
}
.col-settings-row.drag-over {
  background: var(--el-color-primary-light-9);
  box-shadow: inset 0 2px 0 0 var(--el-color-primary);
}
.col-idx,
.col-name,
.col-vis {
  padding: 8px 10px;
}
.col-vis {
  display: flex;
  justify-content: center;
  cursor: default;
}
.col-drag-handle {
  display: inline-block;
  margin-right: 8px;
  letter-spacing: -2px;
  color: var(--el-text-color-placeholder);
  font-size: 12px;
}
.dialog-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
