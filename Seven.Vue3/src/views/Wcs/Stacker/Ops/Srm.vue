<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.stkSrm.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.stkSrm.subtitle') }}</p>
    <div class="ops-toolbar">
      <el-tag :type="board?.summary?.wcsFree ? 'success' : 'warning'">
        {{ board?.summary?.wcsFree ? t('wcsOps.common.wcsIdle') : t('wcsOps.common.wcsBusy') }}
      </el-tag>
      <el-button :loading="loading" @click="load">{{ t('wcsOps.common.refresh') }}</el-button>
    </div>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.commonFeatures') }}</summary>
      <p class="ops-sub">{{ t('wcsOps.stkSrm.commonHint') }}</p>
    </details>

    <details class="ops-section" open>
      <summary>{{ t('wcsOps.common.currentTasks') }}</summary>
      <el-table :data="board?.devices || []" size="small" border style="margin-top: 8px" @row-click="selectDevice">
        <el-table-column prop="seq" label="#" width="50" />
        <el-table-column prop="containerCode" :label="t('wcsOps.common.container')" min-width="100" />
        <el-table-column prop="exeStackCode" label="Exe" width="80" />
        <el-table-column prop="status" :label="t('wcsOps.common.status')" width="100" />
        <el-table-column :label="t('wcsOps.common.actions')" width="200">
          <template #default="{ row }">
            <el-button link type="danger" @click.stop="force('device', row.id)">
              {{ t('wcsOps.common.forceComplete') }}
            </el-button>
            <el-button link @click.stop="resend(row.id)">{{ t('wcsOps.common.resend') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div style="margin-top: 8px">
        <el-button size="small" @click="forceBatchPutAway">{{ t('wcsOps.common.forceSelectedPutAway') }}</el-button>
      </div>
      <el-table
        :data="board?.putAways || []"
        size="small"
        border
        style="margin-top: 8px"
        highlight-current-row
        @current-change="onPutAway"
      >
        <el-table-column prop="containerCode" :label="t('wcsOps.common.putAwayContainer')" min-width="100" />
        <el-table-column prop="status" :label="t('wcsOps.common.status')" width="110" />
      </el-table>
      <pre v-if="tree" class="ops-mono" style="margin-top: 8px; max-height: 240px; overflow: auto">{{
        JSON.stringify(tree, null, 2)
      }}</pre>
    </details>

    <details class="ops-section">
      <summary>{{ t('wcsOps.stkSrm.manualSummary') }}</summary>
      <el-button @click="$router.push('/Wcs/Stacker/Trigger')">{{ t('wcsOps.common.openStackerTrigger') }}</el-button>
    </details>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../../../api/http'
import '../../../../styles/wcs-ops.css'

const { t } = useI18n()
const loading = ref(false)
const board = ref<any>(null)
const tree = ref<any>(null)
const currentPutAway = ref<any>(null)

async function load() {
  loading.value = true
  try {
    const res = await http.get<{ status: boolean; data?: any }>('/api/Wcs/Stacker/Ops/board')
    if (res.status) board.value = res.data
  } finally {
    loading.value = false
  }
}

async function selectDevice(row: any) {
  const res = await http.get<{ status: boolean; data?: any }>(
    `/api/Wcs/Stacker/Ops/task-tree?deviceTaskId=${row.id}`,
  )
  if (res.status) tree.value = res.data
}

function onPutAway(row: any) {
  currentPutAway.value = row
  if (row) void selectByPutAway(row.id)
}

async function selectByPutAway(id: string) {
  const res = await http.get<{ status: boolean; data?: any }>(`/api/Wcs/Stacker/Ops/task-tree?putAwayId=${id}`)
  if (res.status) tree.value = res.data
}

async function force(targetType: string, id: string) {
  await ElMessageBox.confirm(t('wcsOps.common.confirmForce'), t('wcsOps.common.danger'), { type: 'warning' })
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/Stacker/Ops/force-complete', {
    targetType,
    id,
  })
  if (res.status) {
    ElMessage.success(res.message || t('wcsOps.common.done'))
    await load()
  } else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

async function forceBatchPutAway() {
  if (!currentPutAway.value) {
    ElMessage.warning(t('wcsOps.common.selectPutAwayFirst'))
    return
  }
  await force('putAway', currentPutAway.value.id)
}

async function resend(deviceTaskId: string) {
  const res = await http.post<{ status: boolean; message?: string }>('/api/Wcs/Stacker/Ops/resend', { deviceTaskId })
  if (res.status) ElMessage.success(res.message || t('wcsOps.common.resent'))
  else ElMessage.error(res.message || t('wcsOps.common.failed'))
}

onMounted(load)
</script>

<style scoped>
.ops-section {
  margin-top: 12px;
  padding: 8px 12px;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
}
.ops-section summary {
  cursor: pointer;
  font-weight: 600;
}
</style>
