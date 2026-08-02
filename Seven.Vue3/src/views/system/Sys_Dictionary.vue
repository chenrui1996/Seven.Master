<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysDict.listTitle') }}</span>
          <div class="toolbar-actions">
            <el-button
              v-permission="'Sys_Dictionary.Delete'"
              size="small"
              type="danger"
              plain
              :icon="ActionIcons.batchDelete"
              :disabled="!selectedIds.length"
              @click="batchRemove"
            >
              {{ t('common.batchDelete') }}
            </el-button>
            <el-button v-permission="'Sys_Dictionary.Add'" type="primary" :icon="ActionIcons.add" @click="openForm()">
              {{ t('common.add') }}
            </el-button>
          </div>
        </div>
      </template>

      <el-form inline size="small" class="search-bar" @submit.prevent="loadData">
        <el-form-item :label="t('sysDict.colNo')">
          <el-input v-model="keyword" clearable style="width: 180px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">{{ t('common.search') }}</el-button>
          <el-button @click="onResetSearch">{{ t('common.reset') }}</el-button>
        </el-form-item>
      </el-form>

      <el-table
        :data="tableData"
        v-loading="loading"
        border
        row-key="dic_ID"
        @selection-change="onSelectionChange"
        @expand-change="onExpandChange"
      >
        <el-table-column type="selection" width="48" />
        <el-table-column type="expand">
          <template #default="{ row }">
            <div v-if="row.dictionaryLists?.length" class="dict-expand">
              <div class="dict-expand-title">{{ t('sysDict.itemsTitle') }}</div>
              <el-table :data="row.dictionaryLists" size="small" border>
                <el-table-column prop="dicName" :label="t('sysDict.itemLabel')" />
                <el-table-column prop="dicValue" :label="t('sysDict.itemValue')" />
                <el-table-column prop="orderNo" :label="t('sysDict.colOrder')" width="80" />
                <el-table-column :label="t('sysDict.colStatus')" width="80">
                  <template #default="{ row: item }">
                    {{ item.enable === 1 ? t('common.enabled') : t('common.disabled') }}
                  </template>
                </el-table-column>
              </el-table>
            </div>
            <el-empty v-else :description="t('sysDict.noItems')" :image-size="48" />
          </template>
        </el-table-column>
        <el-table-column prop="dic_ID" :label="t('sysDict.colId')" width="80" />
        <el-table-column prop="dicNo" :label="t('sysDict.colNo')" min-width="140" />
        <el-table-column prop="dicName" :label="t('sysDict.colName')" min-width="160" />
        <el-table-column prop="orderNo" :label="t('sysDict.colOrder')" width="80" />
        <el-table-column :label="t('sysDict.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
        <el-table-column :label="t('sysDict.colActions')" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-permission="'Sys_Dictionary.Update'" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">
              {{ t('sysDict.edit') }}
            </el-button>
            <el-button v-permission="'Sys_Dictionary.Delete'" link type="danger" :icon="ActionIcons.delete" @click="removeOne(row)">
              {{ t('common.delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rows"
        :total="total"
        @change="loadData"
        style="margin-top: 16px"
      />
    </el-card>

    <el-dialog v-model="dialogVisible" :title="form.dic_ID ? t('sysDict.editDict') : t('sysDict.addDict')" width="640px">
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('sysDict.colNo')" required>
          <el-input v-model="form.dicNo" :disabled="!!form.dic_ID" />
        </el-form-item>
        <el-form-item :label="t('sysDict.colName')" required>
          <el-input v-model="form.dicName" />
        </el-form-item>
        <el-form-item :label="t('sysDict.colOrder')">
          <el-input-number v-model="form.orderNo" :min="0" />
        </el-form-item>
        <el-form-item :label="t('sysDict.colStatus')">
          <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.save" @click="save">{{ t('common.confirm') }}</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface DictListItem {
  dicList_ID?: number
  dic_ID?: number
  dicName: string
  dicValue: string
  orderNo?: number
  enable?: number
}

interface DictRow {
  dic_ID: number
  dicNo: string
  dicName: string
  parentId?: number
  orderNo?: number
  enable?: number
  dictionaryLists?: DictListItem[]
}

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<DictRow[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const keyword = ref('')
const selectedIds = ref<number[]>([])
const dialogVisible = ref(false)

const form = reactive({
  dic_ID: 0,
  dicNo: '',
  dicName: '',
  parentId: 0,
  orderNo: 0,
  enable: 1 as number,
})

function buildWheres() {
  if (!keyword.value.trim()) return undefined
  return JSON.stringify([
    { name: 'dicNo', value: keyword.value.trim(), displayType: 'like' },
    { name: 'dicName', value: keyword.value.trim(), displayType: 'like' },
  ])
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_Dictionary/getPageData', {
      page: page.value,
      rows: rows.value,
      wheres: buildWheres(),
    })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: DictRow[] }
      total.value = data.total
      tableData.value = data.rows ?? []
    }
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  loadData()
}

function onResetSearch() {
  keyword.value = ''
  onSearch()
}

function onSelectionChange(rowsSel: DictRow[]) {
  selectedIds.value = rowsSel.map((r) => r.dic_ID)
}

function onExpandChange(_row: DictRow, _expanded: DictRow[]) {
  /* 明细已在 getPageData Include 中 */
}

function openForm(row?: DictRow) {
  if (row) {
    Object.assign(form, {
      dic_ID: row.dic_ID,
      dicNo: row.dicNo,
      dicName: row.dicName,
      parentId: row.parentId ?? 0,
      orderNo: row.orderNo ?? 0,
      enable: row.enable ?? 1,
    })
  } else {
    Object.assign(form, { dic_ID: 0, dicNo: '', dicName: '', parentId: 0, orderNo: 0, enable: 1 })
  }
  dialogVisible.value = true
}

async function save() {
  if (!form.dicNo || !form.dicName) {
    ElMessage.warning(t('sysDict.requiredFields'))
    return
  }
  const url = form.dic_ID ? '/api/Sys_Dictionary/update' : '/api/Sys_Dictionary/add'
  const res = await http.post(url, { ...form })
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadData()
  }
}

async function removeOne(row: DictRow) {
  await ElMessageBox.confirm(t('common.deleteConfirm'), t('common.delete'), { type: 'warning' })
  const res = await http.post('/api/Sys_Dictionary/del', [row.dic_ID])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  }
}

async function batchRemove() {
  if (!selectedIds.value.length) return
  await ElMessageBox.confirm(t('common.batchDeleteConfirm'), t('common.batchDelete'), { type: 'warning' })
  const res = await http.post('/api/Sys_Dictionary/del', selectedIds.value)
  if (res.status) {
    ElMessage.success(t('common.success'))
    selectedIds.value = []
    await loadData()
  }
}

onMounted(loadData)
</script>

<style scoped>
.dict-expand {
  padding: 8px 48px 12px;
}
.dict-expand-title {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
  color: var(--seven-text-muted);
}
</style>
