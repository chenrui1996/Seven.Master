<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysQuartzLog.listTitle') }}</span>
          <el-button size="small" @click="load">{{ t('common.search') }}</el-button>
        </div>
      </template>
      <el-form inline size="small" class="search-bar">
        <el-form-item :label="t('sysQuartzLog.taskId')">
          <el-input-number v-model="taskId" :min="0" controls-position="right" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">{{ t('common.search') }}</el-button>
          <el-button @click="onReset">{{ t('common.reset') }}</el-button>
        </el-form-item>
      </el-form>
      <el-table :data="rows" v-loading="loading" border>
        <el-table-column prop="logId" :label="t('sysQuartz.colLogId')" width="80" />
        <el-table-column prop="taskId" :label="t('sysQuartzLog.taskId')" width="90" />
        <el-table-column :label="t('sysQuartz.colSuccess')" width="80">
          <template #default="{ row }">
            {{ row.success ? t('sysQuartz.logSuccess') : t('sysQuartz.logFailed') }}
          </template>
        </el-table-column>
        <el-table-column prop="elapsedMs" :label="t('sysQuartz.colElapsed')" width="100" align="right" />
        <el-table-column prop="responseContent" :label="t('sysQuartz.colResponse')" min-width="240" show-overflow-tooltip />
        <el-table-column :label="t('sysQuartz.colLogTime')" width="170">
          <template #default="{ row }">{{ formatTime(row.createDate) }}</template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        layout="total, prev, pager, next"
        @change="load"
        style="margin-top: 16px"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getPageData } from '../../api/http'

const { t } = useI18n()
const loading = ref(false)
const rows = ref<Record<string, unknown>[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(30)
const taskId = ref<number | undefined>()

function formatTime(v?: unknown) {
  if (!v) return ''
  return new Date(String(v)).toLocaleString()
}

async function load() {
  loading.value = true
  try {
    const wheres =
      taskId.value && taskId.value > 0
        ? JSON.stringify([{ name: 'taskId', value: String(taskId.value), displayType: 'equal' }])
        : undefined
    const res = await getPageData('/api/Sys_QuartzLog/getPageData', {
      page: page.value,
      rows: pageSize.value,
      wheres,
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: Record<string, unknown>[] }
      total.value = data.total
      rows.value = data.rows ?? []
    }
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  void load()
}

function onReset() {
  taskId.value = undefined
  onSearch()
}

onMounted(load)
</script>
