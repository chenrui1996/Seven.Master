<!--
  Device 扩展按钮打开的自定义处理弹窗（手写 Demo）
  由 extension/Board/Device.ts 的 overlay 挂到生成页上
-->
<template>
  <el-dialog
    v-model="deviceCustomState.visible"
    :title="pageTitle"
    width="900px"
    destroy-on-close
    @closed="onClosed"
  >
    <el-alert
      type="info"
      :closable="false"
      show-icon
      style="margin-bottom: 12px"
      :title="hintText"
    />

    <el-empty v-if="!deviceCustomState.rows.length" description="未传入设备数据" />

    <el-table v-else :data="deviceCustomState.rows" border max-height="420">
      <el-table-column prop="deviceId" :label="t('generated.Device.deviceId')" width="90" />
      <el-table-column prop="deviceName" :label="t('generated.Device.deviceName')" min-width="120" />
      <el-table-column prop="deviceCode" :label="t('generated.Device.deviceCode')" min-width="110" />
      <el-table-column :label="t('generated.Device.status')" width="150">
        <template #default="{ row }">
          <el-select v-model="row.status" style="width: 100%">
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </template>
      </el-table-column>
      <el-table-column prop="location" :label="t('generated.Device.location')" min-width="140">
        <template #default="{ row }">
          <el-input v-model="row.location" clearable placeholder="可修改位置后提交" />
        </template>
      </el-table-column>
      <el-table-column label="处理说明" min-width="160">
        <template #default="{ row }">
          <el-input v-model="row._remark" clearable placeholder="仅前端演示，不落库" />
        </template>
      </el-table-column>
    </el-table>

    <template #footer>
      <div class="dialog-footer-actions">
        <el-button @click="closeDeviceCustomDialog">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :icon="ActionIcons.save" :loading="saving" @click="submit">
          {{ t('common.confirm') }}
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../api/http'
import { ActionIcons } from '../constants/actionIcons'
import {
  closeDeviceCustomDialog,
  deviceCustomState,
} from '../extension/Board/deviceCustomShared'

const { t } = useI18n()
const saving = ref(false)

const statusOptions = [
  { value: 0, label: '离线' },
  { value: 1, label: '在线' },
  { value: 2, label: '故障' },
  { value: 3, label: '维护中' },
]

const pageTitle = computed(() =>
  deviceCustomState.mode === 'row' ? '设备行内处理（Demo）' : '设备批量处理（Demo）',
)

const hintText = computed(() =>
  deviceCustomState.mode === 'row'
    ? '由行内按钮打开：展示单行数据，修改后点确定调用 update 提交。'
    : '由工具栏按钮打开：展示勾选行，修改后点确定逐条 update 提交。',
)

function onClosed() {
  closeDeviceCustomDialog()
}

async function submit() {
  if (!deviceCustomState.rows.length) {
    ElMessage.warning('没有可提交的数据')
    return
  }
  saving.value = true
  let ok = 0
  let fail = 0
  try {
    for (const row of deviceCustomState.rows) {
      const id = Number(row.deviceId ?? 0)
      if (!id) {
        fail++
        continue
      }
      const payload = {
        deviceId: id,
        deviceName: row.deviceName ?? '',
        deviceCode: row.deviceCode ?? '',
        status: Number(row.status ?? 0),
        location: row.location ?? '',
      }
      const res = await http.post<{ status: boolean; message?: string }>('/api/Device/update', payload)
      if (res.status) ok++
      else fail++
    }
    if (fail === 0) {
      const hasRemark = deviceCustomState.rows.some((r) => !!r._remark)
      ElMessage.success(`提交成功（${ok} 条）${hasRemark ? '；处理说明仅演示未落库' : ''}`)
      const reload = deviceCustomState.reload
      closeDeviceCustomDialog()
      if (reload) await reload()
    } else {
      ElMessage.warning(`完成：成功 ${ok}，失败 ${fail}`)
    }
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.dialog-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
