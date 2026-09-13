<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.fwHoist.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.fwHoist.subtitle') }}</p>
    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wcsOps.common.refresh') }}</el-button>
    </div>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.status') }}</summary>
      <el-table :data="metaDevices" size="small" border style="margin-top: 8px">
        <el-table-column prop="code" :label="t('wcsOps.common.code')" min-width="120" />
        <el-table-column prop="currentLayer" :label="t('wcsOps.floor.layer')" width="100" />
        <el-table-column prop="isAvailable" :label="t('wcsOps.common.status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isAvailable ? 'success' : 'info'" size="small">
              {{ row.isAvailable ? t('wcsOps.common.yes') : t('wcsOps.common.no') }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </details>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.commonFeatures') }}</summary>
      <el-form label-width="110px" style="margin-top: 12px; max-width: 560px">
        <el-form-item :label="t('wcsOps.fwHoist.targetLayer')">
          <el-select v-model="layerCmd.toLayer" style="width: 100%">
            <el-option v-for="l in layers" :key="l.code" :label="l.name || l.code" :value="l.code" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" disabled :title="t('wcsOps.fwHoist.layerCmdHint')">
            {{ t('wcsOps.fwHoist.dispatchLayer') }}
          </el-button>
          <span class="ops-sub" style="margin-left: 8px">{{ t('wcsOps.fwHoist.layerCmdHint') }}</span>
        </el-form-item>
      </el-form>
    </details>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.fwHoist.currentHoist') }}</summary>
      <el-table :data="board?.hoists || []" size="small" border style="margin-top: 8px" @row-click="open">
        <el-table-column prop="containerCode" :label="t('wcsOps.common.container')" min-width="120" />
        <el-table-column prop="status" :label="t('wcsOps.common.status')" width="100" />
        <el-table-column prop="stage" :label="t('wcsOps.common.stage')" width="100" />
        <el-table-column :label="t('wcsOps.common.actions')" width="140">
          <template #default="{ row }">
            <el-button link type="danger" @click.stop="force(row.id)">{{ t('wcsOps.common.forceComplete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="tree" class="ops-tree-panel">
        <h4>{{ t('wcsOps.common.taskTree') }}</h4>
        <el-descriptions v-if="tree.hoist" :column="2" size="small" border>
          <el-descriptions-item label="Hoist">{{ tree.hoist.status }}</el-descriptions-item>
          <el-descriptions-item :label="t('wcsOps.common.stage')">{{ tree.hoist.stage }}</el-descriptions-item>
        </el-descriptions>
        <el-table v-if="tree.hoistExecs?.length" :data="tree.hoistExecs" size="small" border style="margin-top: 8px">
          <el-table-column prop="status" :label="t('wcsOps.common.status')" width="100" />
          <el-table-column prop="fromLayerCode" label="FromZ" width="90" />
          <el-table-column prop="toLayerCode" label="ToZ" width="90" />
          <el-table-column prop="srcAddress" label="Src" min-width="100" />
          <el-table-column prop="desAddress" label="Des" min-width="100" />
        </el-table>
        <el-table v-if="tree.paths?.length" :data="tree.paths" size="small" border style="margin-top: 8px">
          <el-table-column prop="seq" label="Seq" width="60" />
          <el-table-column prop="pointCode" :label="t('wcsOps.common.code')" min-width="140" />
        </el-table>
      </div>
    </details>

    <details class="ops-section">
      <summary>{{ t('wcsOps.fwHoist.manualSummary') }}</summary>
      <el-button @click="$router.push('/Wcs/FourWay/Trigger')">{{ t('wcsOps.common.openSimTrigger') }}</el-button>
    </details>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../../../api/http'
import '../../../../styles/wcs-ops.css'

const { t } = useI18n()
const loading = ref(false)
const board = ref<any>(null)
const tree = ref<any>(null)
const meta = ref<any>(null)
const layerCmd = reactive({ toLayer: '' })

const layers = computed(() => meta.value?.layers || [])
const metaDevices = computed(() => meta.value?.hoistDevices || [])

async function load() {
  loading.value = true
  try {
    const [b, m] = await Promise.all([
      http.get<{ status: boolean; data?: any }>('/api/Wcs/FourWay/Ops/board'),
      http.get<{ status: boolean; data?: any }>('/api/Wcs/FourWay/Ops/meta'),
    ])
    if (b.status) board.value = b.data
    if (m.status) {
      meta.value = m.data
      if (!layerCmd.toLayer && m.data?.layers?.[0]?.code) layerCmd.toLayer = m.data.layers[0].code
    }
  } finally {
    loading.value = false
  }
}

async function open(row: any) {
  const res = await http.get<{ status: boolean; data?: any }>(
    `/api/Wcs/FourWay/Ops/task-tree?hoistTaskId=${row.id}`,
  )
  if (res.status) tree.value = res.data
}

async function force(id: string) {
  await ElMessageBox.confirm(t('wcsOps.common.confirmForceHoist'), t('wcsOps.common.danger'), { type: 'warning' })
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/force-complete', {
    targetType: 'hoist',
    id,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.done'))
    await load()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

onMounted(load)
</script>

<style scoped>
.ops-section {
  margin-top: 12px;
  padding: 8px 12px;
  border: 1px solid var(--ops-border, #ebeef5);
  border-radius: 4px;
  background: #fff;
}
.ops-section summary {
  cursor: pointer;
  font-weight: 600;
  min-height: 32px;
}
.ops-tree-panel {
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px solid var(--ops-border, #ebeef5);
}
.ops-tree-panel h4 {
  margin: 0 0 8px;
  font-size: 0.9rem;
}
</style>
