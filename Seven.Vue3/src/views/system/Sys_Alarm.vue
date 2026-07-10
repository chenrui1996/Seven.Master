<template>
  <div class="crud-page seven-page">
    <div class="seven-page-header">
      <div>
        <h1 class="seven-page-title">{{ t('sysAlarm.title') }}</h1>
        <p class="seven-page-subtitle">{{ t('sysAlarm.subtitle') }}</p>
      </div>
      <div class="header-actions">
        <el-button v-permission="'Sys_Alarm.Raise'" type="warning" @click="openTestDialog">
          {{ t('sysAlarm.testRaise') }}
        </el-button>
      </div>
    </div>

    <el-tabs v-model="activeTab">
      <el-tab-pane :label="t('sysAlarm.tabRecords')" name="records">
        <el-card>
          <template #header>
            <div class="toolbar">
              <span>{{ t('sysAlarm.listTitle') }}</span>
              <div class="toolbar-actions">
                <el-select
                  v-model="statusFilter"
                  clearable
                  :placeholder="t('sysAlarm.filterStatus')"
                  style="width: 140px"
                  @change="loadAlarms"
                >
                  <el-option :label="t('sysAlarm.statusActive')" :value="0" />
                  <el-option :label="t('sysAlarm.statusAck')" :value="1" />
                  <el-option :label="t('sysAlarm.statusCleared')" :value="2" />
                </el-select>
                <el-button
                  v-permission="'Sys_Alarm.Acknowledge'"
                  type="primary"
                  :disabled="!selectedIds.length"
                  @click="acknowledge"
                >
                  {{ t('sysAlarm.acknowledge') }}
                </el-button>
                <el-button
                  v-permission="'Sys_Alarm.Clear'"
                  :disabled="!selectedIds.length"
                  @click="clearAlarms"
                >
                  {{ t('sysAlarm.clear') }}
                </el-button>
              </div>
            </div>
          </template>

          <el-table :data="alarmRows" v-loading="loading" border @selection-change="onSelectionChange">
            <el-table-column type="selection" width="48" />
            <el-table-column prop="alarm_Id" :label="t('sysAlarm.colId')" width="70" />
            <el-table-column prop="code" :label="t('sysAlarm.colCode')" width="100" />
            <el-table-column prop="message" :label="t('sysAlarm.colMessage')" min-width="220" show-overflow-tooltip />
            <el-table-column prop="level" :label="t('sysAlarm.colLevel')" width="90">
              <template #default="{ row }">
                <el-tag :type="levelTag(row.level as number)" size="small">{{ levelLabel(row.level as number) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="category" :label="t('sysAlarm.colCategory')" width="90" />
            <el-table-column prop="deviceName" :label="t('sysAlarm.colDevice')" width="120" />
            <el-table-column prop="status" :label="t('sysAlarm.colStatus')" width="100">
              <template #default="{ row }">{{ statusLabel(row.status as number) }}</template>
            </el-table-column>
            <el-table-column prop="createDate" :label="t('sysAlarm.colTime')" width="170">
              <template #default="{ row }">{{ formatTime(row.createDate as string) }}</template>
            </el-table-column>
          </el-table>
          <el-pagination
            v-model:current-page="page"
            v-model:page-size="rowsPerPage"
            :total="total"
            @change="loadAlarms"
            style="margin-top:16px"
          />
        </el-card>
      </el-tab-pane>

      <el-tab-pane :label="t('sysAlarm.tabCodes')" name="codes">
        <el-card>
          <template #header>
            <div class="toolbar">
              <span>{{ t('sysAlarm.codesTitle') }}</span>
              <el-button type="primary" @click="openCodeForm()">{{ t('common.add') }}</el-button>
            </div>
          </template>
          <el-table :data="codeRows" v-loading="codeLoading" border>
            <el-table-column prop="code" :label="t('sysAlarm.colCode')" width="120" />
            <el-table-column prop="message" :label="t('sysAlarm.colMessage')" min-width="240" show-overflow-tooltip />
            <el-table-column prop="level" :label="t('sysAlarm.colLevel')" width="90">
              <template #default="{ row }">{{ levelLabel(row.level as number) }}</template>
            </el-table-column>
            <el-table-column prop="category" :label="t('sysAlarm.colCategory')" width="100" />
            <el-table-column prop="enable" :label="t('sysAlarm.colEnable')" width="80">
              <template #default="{ row }">
                {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
              </template>
            </el-table-column>
            <el-table-column :label="t('sysAlarm.colActions')" width="100">
              <template #default="{ row }">
                <el-button link type="primary" @click="openCodeForm(row)">{{ t('sysAlarm.edit') }}</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-pagination
            v-model:current-page="codePage"
            v-model:page-size="codeRowsPerPage"
            :total="codeTotal"
            @change="loadCodes"
            style="margin-top:16px"
          />
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="testDialogVisible" :title="t('sysAlarm.testRaise')" width="420px">
      <el-form :model="testForm" label-width="100px">
        <el-form-item :label="t('sysAlarm.colCode')">
          <el-select v-model="testForm.code" filterable style="width: 100%">
            <el-option v-for="c in codeRows" :key="String(c.code)" :label="String(c.code)" :value="String(c.code)" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colDevice')">
          <el-input v-model="testForm.deviceName" placeholder="Stacker-01" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="testDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" @click="raiseTest">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="codeDialogVisible"
      :title="codeForm.alarmCode_Id ? t('sysAlarm.editCode') : t('sysAlarm.addCode')"
      width="520px"
    >
      <el-form :model="codeForm" label-width="100px">
        <el-form-item :label="t('sysAlarm.colCode')">
          <el-input v-model="codeForm.code" :disabled="!!codeForm.alarmCode_Id" />
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colMessage')">
          <el-input
            v-model="codeForm.message"
            type="textarea"
            :rows="3"
            :placeholder="t('sysAlarm.messagePlaceholder')"
          />
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colLevel')">
          <el-select v-model="codeForm.level" style="width: 100%">
            <el-option v-for="lv in levelOptions" :key="lv.value" :label="lv.label" :value="lv.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colCategory')">
          <el-input v-model="codeForm.category" />
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colEnable')">
          <el-switch v-model="codeForm.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
        <el-form-item :label="t('sysAlarm.colRemark')">
          <el-input v-model="codeForm.remark" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="codeDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" @click="saveCode">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { useAlarmStore } from '../../stores/alarm'

interface AlarmCodeRow {
  alarmCode_Id?: number
  code: string
  message: string
  level: number
  category?: string
  enable: number
  remark?: string
}

const { t } = useI18n()
const alarmStore = useAlarmStore()

const activeTab = ref('records')
const loading = ref(false)
const alarmRows = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const rowsPerPage = ref(30)
const statusFilter = ref<number | undefined>(0)
const selectedIds = ref<number[]>([])

const codeLoading = ref(false)
const codeRows = ref<AlarmCodeRow[]>([])
const codeTotal = ref(0)
const codePage = ref(1)
const codeRowsPerPage = ref(30)

const testDialogVisible = ref(false)
const testForm = reactive({ code: 'WCS001', deviceName: 'Stacker-01' })

const codeDialogVisible = ref(false)
const codeForm = reactive<AlarmCodeRow>({
  alarmCode_Id: 0,
  code: '',
  message: '',
  level: 2,
  category: 'WCS',
  enable: 1,
  remark: '',
})

const levelOptions = computed(() => [
  { value: 1, label: t('sysAlarm.levelInfo') },
  { value: 2, label: t('sysAlarm.levelWarning') },
  { value: 3, label: t('sysAlarm.levelError') },
  { value: 4, label: t('sysAlarm.levelCritical') },
])

function levelLabel(level: number) {
  return levelOptions.value.find((x) => x.value === level)?.label ?? String(level)
}

function levelTag(level: number): 'info' | 'warning' | 'danger' {
  if (level >= 4) return 'danger'
  if (level >= 3) return 'danger'
  if (level >= 2) return 'warning'
  return 'info'
}

function statusLabel(status: number) {
  if (status === 1) return t('sysAlarm.statusAck')
  if (status === 2) return t('sysAlarm.statusCleared')
  return t('sysAlarm.statusActive')
}

function formatTime(value?: string) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

function onSelectionChange(rows: Record<string, unknown>[]) {
  selectedIds.value = rows.map((r) => r.alarm_Id as number)
}

async function loadAlarms() {
  loading.value = true
  try {
    const wheres = statusFilter.value !== undefined && statusFilter.value !== null
      ? JSON.stringify([{ name: 'status', value: String(statusFilter.value), displayType: 'equal' }])
      : undefined
    const res = await getPageData('/api/Sys_Alarm/getPageData', {
      page: page.value,
      rows: rowsPerPage.value,
      wheres,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      alarmRows.value = data.rows
    }
  } finally {
    loading.value = false
  }
}

async function loadCodes() {
  codeLoading.value = true
  try {
    const res = await http.post('/api/Sys_Alarm/getAlarmCodes', {
      page: codePage.value,
      rows: codeRowsPerPage.value,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: AlarmCodeRow[] }
      codeTotal.value = data.total
      codeRows.value = data.rows
    }
  } finally {
    codeLoading.value = false
  }
}

async function acknowledge() {
  const res = await http.post('/api/Sys_Alarm/acknowledge', selectedIds.value)
  if (res.status) {
    ElMessage.success(t('common.success'))
    selectedIds.value = []
    await loadAlarms()
    await alarmStore.fetchActiveCount()
  }
}

async function clearAlarms() {
  const res = await http.post('/api/Sys_Alarm/clear', selectedIds.value)
  if (res.status) {
    ElMessage.success(t('common.success'))
    selectedIds.value = []
    await loadAlarms()
    await alarmStore.fetchActiveCount()
  }
}

function openTestDialog() {
  if (codeRows.value.length && !testForm.code) {
    testForm.code = codeRows.value[0].code
  }
  testDialogVisible.value = true
}

async function raiseTest() {
  const res = await http.post('/api/Sys_Alarm/raise', {
    code: testForm.code,
    deviceName: testForm.deviceName,
    source: 'Sys_Alarm.Test',
    params: { DeviceName: testForm.deviceName },
  })
  if (res.status) {
    ElMessage.success(t('sysAlarm.raiseSuccess'))
    testDialogVisible.value = false
    await loadAlarms()
    await alarmStore.fetchActiveCount()
  } else {
    ElMessage.error(res.message || t('sysAlarm.raiseFailed'))
  }
}

function openCodeForm(row?: AlarmCodeRow) {
  if (row) {
    Object.assign(codeForm, row)
  } else {
    Object.assign(codeForm, {
      alarmCode_Id: 0,
      code: '',
      message: '',
      level: 2,
      category: 'WCS',
      enable: 1,
      remark: '',
    })
  }
  codeDialogVisible.value = true
}

async function saveCode() {
  const res = await http.post('/api/Sys_Alarm/saveAlarmCode', { ...codeForm })
  if (res.status) {
    ElMessage.success(t('common.success'))
    codeDialogVisible.value = false
    await loadCodes()
  } else {
    ElMessage.error(res.message || t('sysAlarm.saveFailed'))
  }
}

onMounted(async () => {
  await loadCodes()
  await loadAlarms()
})
</script>

<style scoped>
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.header-actions {
  display: flex;
  gap: 8px;
}
</style>
