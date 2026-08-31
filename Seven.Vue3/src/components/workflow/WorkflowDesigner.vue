<template>
  <div class="wf-designer">
    <div class="wf-designer__toolbar">
      <el-button size="small" type="primary" @click="addStep">{{ t('sysWorkFlow.addStep') }}</el-button>
      <span class="wf-designer__hint">{{ t('sysWorkFlow.designerHint') }}</span>
    </div>
    <div class="wf-canvas">
      <div class="wf-node wf-node--start">{{ t('sysWorkFlow.nodeStart') }}</div>
      <div class="wf-arrow" />
      <template v-for="(step, index) in modelValue" :key="step.stepId || index">
        <div
          class="wf-node wf-node--step"
          :class="{ 'is-active': selectedIndex === index }"
          @click="selectedIndex = index"
        >
          <div class="wf-node__ord">{{ index + 1 }}</div>
          <div class="wf-node__body">
            <div class="wf-node__title">{{ step.stepName || t('sysWorkFlow.unnamedStep') }}</div>
            <div class="wf-node__meta">{{ stepTypeLabel(step.stepType) }}</div>
          </div>
          <div class="wf-node__actions" @click.stop>
            <el-button link size="small" :disabled="index === 0" @click="move(index, -1)">↑</el-button>
            <el-button link size="small" :disabled="index === modelValue.length - 1" @click="move(index, 1)">↓</el-button>
            <el-button link type="danger" size="small" @click="remove(index)">×</el-button>
          </div>
        </div>
        <div class="wf-arrow" />
      </template>
      <div class="wf-node wf-node--end">{{ t('sysWorkFlow.nodeEnd') }}</div>
    </div>

    <el-card v-if="selected" shadow="never" class="wf-editor">
      <template #header>{{ t('sysWorkFlow.stepEditor') }} #{{ selectedIndex! + 1 }}</template>
      <el-form label-width="100px" size="small">
        <el-form-item :label="t('sysWorkFlow.stepName')">
          <el-input v-model="selected.stepName" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.stepType')">
          <el-select v-model="selected.stepType" style="width: 100%">
            <el-option v-for="opt in stepTypeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.stepValue')">
          <el-select
            v-if="nodeOptions.length && selected.auditMethod === 1"
            v-model="stepValueMulti"
            multiple
            filterable
            style="width: 100%"
            :placeholder="t('sysWorkFlow.countersignHint')"
          >
            <el-option v-for="o in nodeOptions" :key="o.key" :label="`${o.value} (${o.key})`" :value="String(o.key)" />
          </el-select>
          <el-select
            v-else-if="nodeOptions.length"
            v-model="selected.stepValue"
            filterable
            allow-create
            default-first-option
            style="width: 100%"
            :placeholder="t('sysWorkFlow.stepValueHint')"
          >
            <el-option v-for="o in nodeOptions" :key="o.key" :label="`${o.value} (${o.key})`" :value="String(o.key)" />
          </el-select>
          <el-input v-else v-model="selected.stepValue" :placeholder="t('sysWorkFlow.stepValueHint')" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.auditMethod')">
          <el-switch v-model="selected.auditMethod" :active-value="1" :inactive-value="0" />
          <span class="wf-hint">{{ t('sysWorkFlow.auditMethodHint') }}</span>
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.sendMail')">
          <el-switch v-model="selected.sendMail" :active-value="1" :inactive-value="0" />
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.filters')">
          <div class="wf-filters">
            <div v-for="(f, fi) in localFilters" :key="fi" class="wf-filters__row">
              <el-input v-model="f.field" :placeholder="t('sysWorkFlow.filterField')" size="small" style="width:110px" />
              <el-select v-model="f.filterType" size="small" style="width:90px">
                <el-option v-for="op in filterOps" :key="op" :label="op" :value="op" />
              </el-select>
              <el-input v-model="f.value" :placeholder="t('sysWorkFlow.filterValue')" size="small" style="flex:1" />
              <el-button link type="danger" size="small" @click="removeFilter(fi)">×</el-button>
            </div>
            <el-button size="small" @click="addFilter">{{ t('sysWorkFlow.addFilter') }}</el-button>
          </div>
        </el-form-item>
        <el-form-item :label="t('sysWorkFlow.remark')">
          <el-input v-model="selected.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../api/http'

export interface DesignerStep {
  stepId: string
  stepName: string
  stepOrder: number
  stepType: number
  stepValue: string
  stepAttrType: string
  nextStepIds?: string
  auditMethod?: number
  filters?: string
  sendMail?: number
  remark?: string
}

interface FilterRow {
  field: string
  filterType: string
  value: string
}

const props = defineProps<{
  modelValue: DesignerStep[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: DesignerStep[]]
  'update:nodeConfig': [value: string]
  'update:lineConfig': [value: string]
}>()

const { t } = useI18n()
const selectedIndex = ref<number | null>(0)
const users = ref<{ key: number; value: string }[]>([])
const roles = ref<{ key: number; value: string }[]>([])
const depts = ref<{ key: number; value: string }[]>([])
const filterOps = ['=', '!=', '>', '>=', '<', '<=', 'in', 'like', 'or']

const stepTypeOptions = computed(() => [
  { value: 1, label: t('sysWorkFlow.stepTypeRole') },
  { value: 2, label: t('sysWorkFlow.stepTypeUser') },
  { value: 3, label: t('sysWorkFlow.stepTypeDept') },
])

const selected = computed(() =>
  selectedIndex.value == null ? null : props.modelValue[selectedIndex.value] ?? null,
)

const nodeOptions = computed(() => {
  const step = selected.value
  if (!step) return []
  if (step.stepType === 2) return users.value
  if (step.stepType === 3) return depts.value
  return roles.value
})

const stepValueMulti = computed({
  get: () =>
    (selected.value?.stepValue || '')
      .split(',')
      .map((x) => x.trim())
      .filter(Boolean),
  set: (vals: string[]) => {
    if (!selected.value) return
    selected.value.stepValue = vals.join(',')
  },
})

const localFilters = ref<FilterRow[]>([])

watch(
  () => selectedIndex.value,
  () => {
    const raw = selected.value?.filters
    if (!raw) {
      localFilters.value = []
      return
    }
    try {
      const arr = JSON.parse(raw) as FilterRow[]
      localFilters.value = Array.isArray(arr) ? arr.map((x) => ({ ...x })) : []
    } catch {
      localFilters.value = []
    }
  },
  { immediate: true },
)

watch(
  localFilters,
  (rows) => {
    if (!selected.value) return
    const cleaned = rows.filter((r) => r.field?.trim())
    selected.value.filters = cleaned.length ? JSON.stringify(cleaned) : ''
  },
  { deep: true },
)

function stepTypeLabel(type: number) {
  return stepTypeOptions.value.find((x) => x.value === type)?.label || String(type)
}

function uid() {
  return Math.random().toString(36).slice(2, 10)
}

function addFilter() {
  localFilters.value.push({ field: '', filterType: '=', value: '' })
}

function removeFilter(index: number) {
  localFilters.value.splice(index, 1)
}

function syncLayout(steps: DesignerStep[]) {
  const nodes = [
    { id: 'start', type: 'start', x: 40, y: 80 },
    ...steps.map((s, i) => ({ id: s.stepId, type: 'node', x: 180 + i * 160, y: 80, stepOrder: i + 1 })),
    { id: 'end', type: 'end', x: 180 + steps.length * 160, y: 80 },
  ]
  const lines: { from: string; to: string }[] = []
  if (steps.length === 0) {
    lines.push({ from: 'start', to: 'end' })
  } else {
    lines.push({ from: 'start', to: steps[0].stepId })
    for (let i = 0; i < steps.length - 1; i++) lines.push({ from: steps[i].stepId, to: steps[i + 1].stepId })
    lines.push({ from: steps[steps.length - 1].stepId, to: 'end' })
  }
  const next = steps.map((s, i) => ({
    ...s,
    stepOrder: i + 1,
    stepAttrType: 'node',
    nextStepIds: i < steps.length - 1 ? steps[i + 1].stepId : 'end',
  }))
  emit('update:modelValue', next)
  emit('update:nodeConfig', JSON.stringify({ nodes }))
  emit('update:lineConfig', JSON.stringify({ lines }))
}

function addStep() {
  const steps = [
    ...props.modelValue,
    {
      stepId: uid(),
      stepName: t('sysWorkFlow.defaultStepName'),
      stepOrder: props.modelValue.length + 1,
      stepType: 1,
      stepValue: '',
      stepAttrType: 'node',
      auditMethod: 0,
      filters: '',
      sendMail: 0,
      remark: '',
    },
  ]
  selectedIndex.value = steps.length - 1
  syncLayout(steps)
}

function remove(index: number) {
  const steps = props.modelValue.filter((_, i) => i !== index)
  selectedIndex.value = steps.length ? Math.min(index, steps.length - 1) : null
  syncLayout(steps)
}

function move(index: number, delta: number) {
  const target = index + delta
  if (target < 0 || target >= props.modelValue.length) return
  const steps = [...props.modelValue]
  const tmp = steps[index]
  steps[index] = steps[target]
  steps[target] = tmp
  selectedIndex.value = target
  syncLayout(steps)
}

function bumpLayout() {
  if (props.modelValue.length) syncLayout([...props.modelValue])
}

defineExpose({ bumpLayout })

onMounted(async () => {
  try {
    const res = await http.get('/api/Sys_WorkFlow/getNodeDic')
    if (res.status && res.data) {
      const data = res.data as {
        users?: { key: number; value: string }[]
        roles?: { key: number; value: string }[]
        dept?: { key: number; value: string }[]
      }
      users.value = data.users ?? []
      roles.value = data.roles ?? []
      depts.value = data.dept ?? []
    }
  } catch {
    /* ignore */
  }
  if (!props.modelValue.length) {
    syncLayout([])
  } else if (selectedIndex.value == null) {
    selectedIndex.value = 0
  }
})
</script>

<style scoped>
.wf-designer {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.wf-designer__toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
}
.wf-designer__hint {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.wf-canvas {
  display: flex;
  align-items: center;
  gap: 0;
  overflow-x: auto;
  padding: 16px 8px;
  background: linear-gradient(180deg, #f7fafc 0%, #eef3f8 100%);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  min-height: 140px;
}
.wf-node {
  flex: 0 0 auto;
  min-width: 120px;
  padding: 10px 12px;
  border-radius: 8px;
  background: #fff;
  border: 1px solid var(--el-border-color);
  box-shadow: 0 1px 2px rgb(0 0 0 / 4%);
  cursor: pointer;
}
.wf-node--start,
.wf-node--end {
  min-width: 72px;
  text-align: center;
  font-weight: 600;
  background: #1f4e79;
  color: #fff;
  border-color: #1f4e79;
  cursor: default;
}
.wf-node--step.is-active {
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--el-color-primary) 25%, transparent);
}
.wf-node__ord {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}
.wf-node__title {
  font-weight: 600;
  font-size: 13px;
}
.wf-node__meta {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}
.wf-node__actions {
  display: flex;
  gap: 2px;
  margin-top: 4px;
}
.wf-arrow {
  width: 28px;
  height: 2px;
  background: #94a3b8;
  position: relative;
  flex: 0 0 28px;
}
.wf-arrow::after {
  content: '';
  position: absolute;
  right: -1px;
  top: -4px;
  border: 5px solid transparent;
  border-left-color: #94a3b8;
}
.wf-editor {
  margin-top: 4px;
}
.wf-hint {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.wf-filters {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.wf-filters__row {
  display: flex;
  gap: 6px;
  align-items: center;
}
</style>
