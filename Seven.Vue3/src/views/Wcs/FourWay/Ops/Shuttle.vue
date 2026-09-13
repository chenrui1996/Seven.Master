<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.fwShuttle.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.fwShuttle.subtitle') }}</p>

    <div class="ops-toolbar">
      <el-tag :type="meta?.wcsFree ? 'success' : 'warning'">
        {{ meta?.wcsFree ? t('wcsOps.common.wcsIdle') : t('wcsOps.common.wcsBusy') }}
      </el-tag>
      <el-tag :type="meta?.canAcceptLegs ? 'success' : 'danger'">
        {{ meta?.canAcceptLegs ? t('wcsOps.common.canAccept') : t('wcsOps.common.interlockReject') }}
      </el-tag>
      <el-button type="primary" :loading="loading" @click="refresh">{{ t('wcsOps.common.refresh') }}</el-button>
    </div>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.status') }}</summary>
      <el-descriptions :column="2" size="small" border style="margin-top: 8px">
        <el-descriptions-item :label="t('wcsOps.common.wcsIdle')">
          {{ meta?.wcsFree ? t('wcsOps.common.yes') : t('wcsOps.common.no') }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('wcsOps.common.canAccept')">
          {{ meta?.canAcceptLegs ? t('wcsOps.common.yes') : t('wcsOps.common.no') }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('wcsOps.floor.carsOnLayer')">
          {{ (board?.shuttles || []).length }}
        </el-descriptions-item>
        <el-descriptions-item label="Parking">
          {{ (meta?.parking || []).length }}
        </el-descriptions-item>
      </el-descriptions>
    </details>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.commonFeatures') }}</summary>
      <el-form label-width="96px" style="margin-top: 12px; max-width: 720px">
        <el-form-item label="From"><el-input v-model="point.fromCode" class="ops-mono" /></el-form-item>
        <el-form-item label="To"><el-input v-model="point.toCode" class="ops-mono" /></el-form-item>
        <el-form-item>
          <el-button type="primary" :disabled="!meta?.wcsFree" @click="pointDispatch">
            {{ t('wcsOps.common.pointDispatch') }}
          </el-button>
        </el-form-item>
        <el-form-item label="From"><el-input v-model="charge.fromCode" class="ops-mono" /></el-form-item>
        <el-form-item :label="t('wcsOps.common.chargePoint')">
          <el-input v-model="charge.chargePointCode" class="ops-mono" />
        </el-form-item>
        <el-form-item>
          <el-button :disabled="!meta?.wcsFree" @click="doCharge(false)">{{ t('wcsOps.common.charge') }}</el-button>
          <el-button @click="doCharge(true)">{{ t('wcsOps.common.stopCharge') }}</el-button>
        </el-form-item>
      </el-form>
    </details>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.currentTasks') }}</summary>
      <el-table :data="board?.shuttles || []" size="small" border style="margin-top: 8px" @row-click="selectShuttle">
        <el-table-column prop="containerCode" :label="t('wcsOps.common.container')" min-width="100" />
        <el-table-column prop="fromCode" label="From" min-width="100" />
        <el-table-column prop="toCode" label="To" min-width="100" />
        <el-table-column prop="status" :label="t('wcsOps.common.status')" width="100" />
        <el-table-column :label="t('wcsOps.common.actions')" width="200">
          <template #default="{ row }">
            <el-button link type="danger" @click.stop="force('shuttle', row.id)">
              {{ t('wcsOps.common.forceComplete') }}
            </el-button>
            <el-button link @click.stop="resend(row.id)">{{ t('wcsOps.common.resend') }}</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="tree" class="ops-tree-panel">
        <h4>{{ t('wcsOps.common.taskTree') }}</h4>
        <el-descriptions v-if="tree.shuttle" :column="2" size="small" border>
          <el-descriptions-item label="Shuttle">{{ tree.shuttle.status }}</el-descriptions-item>
          <el-descriptions-item :label="t('wcsOps.common.container')">{{ tree.shuttle.containerCode }}</el-descriptions-item>
          <el-descriptions-item label="From">{{ tree.shuttle.fromCode }}</el-descriptions-item>
          <el-descriptions-item label="To">{{ tree.shuttle.toCode }}</el-descriptions-item>
        </el-descriptions>
        <el-table v-if="tree.paths?.length" :data="tree.paths" size="small" border style="margin-top: 8px">
          <el-table-column prop="seq" label="Seq" width="60" />
          <el-table-column prop="pointCode" :label="t('wcsOps.common.code')" min-width="140" />
          <el-table-column prop="edgeId" label="Edge" width="80" />
        </el-table>
        <el-descriptions v-if="tree.hoist" :column="2" size="small" border style="margin-top: 8px">
          <el-descriptions-item label="Hoist">{{ tree.hoist.status }}</el-descriptions-item>
          <el-descriptions-item :label="t('wcsOps.common.stage')">{{ tree.hoist.stage }}</el-descriptions-item>
        </el-descriptions>
      </div>
    </details>

    <details class="ops-section">
      <summary>{{ t('wcsOps.fwShuttle.manualSummary') }}</summary>
      <p class="ops-sub">{{ t('wcsOps.fwShuttle.manualHint') }}</p>
      <el-button @click="$router.push('/Wcs/FourWay/Trigger')">{{ t('wcsOps.common.openSimTrigger') }}</el-button>
    </details>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../../../api/http'
import '../../../../styles/wcs-ops.css'

const { t } = useI18n()
const meta = ref<any>(null)
const board = ref<any>(null)
const tree = ref<any>(null)
const loading = ref(false)
const point = reactive({ fromCode: '', toCode: '' })
const charge = reactive({ fromCode: '', chargePointCode: '' })

async function refresh() {
  loading.value = true
  try {
    const [m, b] = await Promise.all([
      http.get<{ status: boolean; data?: any }>('/api/Wcs/FourWay/Ops/meta'),
      http.get<{ status: boolean; data?: any }>('/api/Wcs/FourWay/Ops/board'),
    ])
    if (m.status) meta.value = m.data
    if (b.status) board.value = b.data
  } finally {
    loading.value = false
  }
}

async function selectShuttle(row: any) {
  const res = await http.get<{ status: boolean; data?: any }>(
    `/api/Wcs/FourWay/Ops/task-tree?shuttleTaskId=${row.id}`,
  )
  if (res.status) tree.value = res.data
}

async function pointDispatch() {
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/point-dispatch', point)
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.dispatched'))
    await refresh()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

async function doCharge(stop: boolean) {
  const url = stop ? '/api/Wcs/FourWay/Ops/charge/stop' : '/api/Wcs/FourWay/Ops/charge'
  const res = await http.post<{ status: boolean; message?: string }>(url, charge)
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.success'))
    await refresh()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

async function force(targetType: string, id: string) {
  await ElMessageBox.confirm(t('wcsOps.common.confirmForce'), t('wcsOps.common.danger'), { type: 'warning' })
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/force-complete', {
    targetType,
    id,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.done'))
    await refresh()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

async function resend(shuttleTaskId: string) {
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/FourWay/Ops/resend', { shuttleTaskId })
  if (res.status) ElMessage.success(res.message || t('wcsOps.common.resent'))
  else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

onMounted(refresh)
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
  color: var(--ops-text, #303133);
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
