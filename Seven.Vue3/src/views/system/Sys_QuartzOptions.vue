<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysQuartz.listTitle') }}</span>
          <div class="toolbar-actions">
            <el-button
              v-permission="'Sys_QuartzOptions.Delete'"
              size="small"
              type="danger"
              plain
              :icon="ActionIcons.batchDelete"
              :disabled="!selectedIds.length"
              @click="batchRemove"
            >
              {{ t('common.batchDelete') }}
            </el-button>
            <el-button v-permission="'Sys_QuartzOptions.Add'" type="primary" :icon="ActionIcons.add" @click="openForm()">
              {{ t('common.add') }}
            </el-button>
          </div>
        </div>
      </template>

      <el-table :data="tableData" v-loading="loading" border @selection-change="onSelectionChange">
        <el-table-column type="selection" width="48" />
        <el-table-column prop="id" :label="t('sysQuartz.colId')" width="70" />
        <el-table-column prop="taskName" :label="t('sysQuartz.colName')" min-width="160" />
        <el-table-column prop="groupName" :label="t('sysQuartz.colGroup')" width="120" />
        <el-table-column prop="cronExpression" :label="t('sysQuartz.colCron')" min-width="160" show-overflow-tooltip />
        <el-table-column prop="apiUrl" :label="t('sysQuartz.colApiUrl')" min-width="200" show-overflow-tooltip />
        <el-table-column :label="t('sysQuartz.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
        <el-table-column :label="t('sysQuartz.colActions')" width="340" fixed="right">
          <template #default="{ row }">
            <el-button v-permission="'Sys_QuartzOptions.Update'" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">
              {{ t('sysQuartz.edit') }}
            </el-button>
            <el-button v-permission="'Sys_QuartzOptions.Update'" link type="success" @click="toggleEnable(row, true)">
              {{ t('sysQuartz.enable') }}
            </el-button>
            <el-button v-permission="'Sys_QuartzOptions.Update'" link type="warning" @click="toggleEnable(row, false)">
              {{ t('sysQuartz.disable') }}
            </el-button>
            <el-button v-permission="'Sys_QuartzOptions.Update'" link @click="runNow(row)">
              {{ t('sysQuartz.runNow') }}
            </el-button>
            <el-button link type="info" @click="openLogs(row)">
              {{ t('sysQuartz.viewLogs') }}
            </el-button>
            <el-button v-permission="'Sys_QuartzOptions.Delete'" link type="danger" :icon="ActionIcons.delete" @click="removeOne(row)">
              {{ t('common.delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rowsPerPage"
        :total="total"
        @change="loadData"
        style="margin-top: 16px"
      />
    </el-card>

    <el-dialog v-model="dialogVisible" :title="form.id ? t('sysQuartz.editJob') : t('sysQuartz.addJob')" width="560px">
      <el-form :model="form" label-width="110px">
        <el-form-item :label="t('sysQuartz.colName')" required>
          <el-input v-model="form.taskName" />
        </el-form-item>
        <el-form-item :label="t('sysQuartz.colGroup')">
          <el-input v-model="form.groupName" />
        </el-form-item>
        <el-form-item :label="t('sysQuartz.colCron')" required>
          <el-input v-model="form.cronExpression" placeholder="0 0/5 * * * ?" />
        </el-form-item>
        <el-form-item :label="t('sysQuartz.colApiUrl')">
          <el-input v-model="form.apiUrl" />
        </el-form-item>
        <el-form-item :label="t('sysQuartz.colStatus')">
          <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.save" @click="save">{{ t('common.save') }}</el-button>
        </div>
      </template>
    </el-dialog>

    <el-drawer v-model="logDrawerVisible" :title="logDrawerTitle" size="720px" destroy-on-close>
      <el-table :data="logTableData" v-loading="logLoading" border size="small">
        <el-table-column prop="logId" :label="t('sysQuartz.colLogId')" width="80" />
        <el-table-column :label="t('sysQuartz.colSuccess')" width="72">
          <template #default="{ row }">
            {{ row.success ? t('sysQuartz.logSuccess') : t('sysQuartz.logFailed') }}
          </template>
        </el-table-column>
        <el-table-column prop="elapsedMs" :label="t('sysQuartz.colElapsed')" width="90" align="right" />
        <el-table-column prop="responseContent" :label="t('sysQuartz.colResponse')" min-width="200" show-overflow-tooltip />
        <el-table-column :label="t('sysQuartz.colLogTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.createDate as string) }}</template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="logPage"
        v-model:page-size="logRows"
        :total="logTotal"
        layout="total, prev, pager, next"
        @change="loadLogs"
        style="margin-top: 16px"
      />
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface QuartzRow {
  id: number
  taskName: string
  groupName?: string
  cronExpression: string
  apiUrl?: string
  enable?: number
}

interface QuartzLogRow {
  logId: number
  taskId: number
  success: boolean
  elapsedMs: number
  responseContent?: string
  createDate?: string
}

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<QuartzRow[]>([])
const total = ref(0)
const page = ref(1)
const rowsPerPage = ref(30)
const selectedIds = ref<number[]>([])
const dialogVisible = ref(false)

const logDrawerVisible = ref(false)
const logDrawerTitle = ref('')
const logTaskId = ref(0)
const logLoading = ref(false)
const logTableData = ref<QuartzLogRow[]>([])
const logTotal = ref(0)
const logPage = ref(1)
const logRows = ref(20)

function formatTime(value?: string) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

const form = reactive<QuartzRow>({
  id: 0,
  taskName: '',
  groupName: 'DEFAULT',
  cronExpression: '',
  apiUrl: '',
  enable: 1,
})

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_QuartzOptions/getPageData', {
      page: page.value,
      rows: rowsPerPage.value,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: QuartzRow[] }
      total.value = data.total
      tableData.value = data.rows ?? []
    }
  } finally {
    loading.value = false
  }
}

function onSelectionChange(rows: QuartzRow[]) {
  selectedIds.value = rows.map((r) => r.id)
}

function openForm(row?: QuartzRow) {
  if (row) {
    Object.assign(form, { ...row })
  } else {
    Object.assign(form, {
      id: 0,
      taskName: '',
      groupName: 'DEFAULT',
      cronExpression: '0 0/5 * * * ?',
      apiUrl: '',
      enable: 1,
    })
  }
  dialogVisible.value = true
}

async function save() {
  if (!form.taskName.trim() || !form.cronExpression.trim()) {
    ElMessage.warning(t('sysQuartz.requiredFields'))
    return
  }
  const res = await http.post('/api/Sys_QuartzOptions/save', { ...form })
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadData()
  }
}

async function toggleEnable(row: QuartzRow, enable: boolean) {
  const url = enable ? '/api/Sys_QuartzOptions/enable' : '/api/Sys_QuartzOptions/disable'
  const res = await http.post(url, { id: row.id })
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  }
}

async function runNow(row: QuartzRow) {
  const res = await http.post('/api/Sys_QuartzOptions/runNow', { id: row.id })
  if (res.status) {
    ElMessage.success(t('sysQuartz.runSuccess'))
  }
}

function openLogs(row: QuartzRow) {
  logTaskId.value = row.id
  logDrawerTitle.value = `${t('sysQuartz.logDrawerTitle')} — ${row.taskName}`
  logPage.value = 1
  logDrawerVisible.value = true
  loadLogs()
}

async function loadLogs() {
  if (!logTaskId.value) return
  logLoading.value = true
  try {
    const wheres = JSON.stringify([
      { name: 'taskId', value: String(logTaskId.value), displayType: 'equal' },
    ])
    const res = await getPageData('/api/Sys_QuartzLog/getPageData', {
      page: logPage.value,
      rows: logRows.value,
      wheres,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: QuartzLogRow[] }
      logTotal.value = data.total
      logTableData.value = data.rows ?? []
    }
  } finally {
    logLoading.value = false
  }
}

async function removeOne(row: QuartzRow) {
  await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), { type: 'warning' })
  const res = await http.post('/api/Sys_QuartzOptions/delete', { ids: [row.id] })
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  }
}

async function batchRemove() {
  if (!selectedIds.value.length) return
  await ElMessageBox.confirm(t('common.batchDeleteConfirm'), t('common.batchDelete'), { type: 'warning' })
  const res = await http.post('/api/Sys_QuartzOptions/delete', { ids: selectedIds.value })
  if (res.status) {
    ElMessage.success(t('common.success'))
    selectedIds.value = []
    await loadData()
  }
}

onMounted(loadData)
</script>
