<template>
  <div class="crud-page seven-page coder-page">
    <div class="split-layout">
      <el-card class="tree-panel">
        <template #header>
          <div class="toolbar">
            <span>{{ t('sysCoder.treeTitle') }}</span>
            <el-button size="small" type="primary" :icon="ActionIcons.add" @click="showAdd = true">{{ t('sysCoder.addConfig') }}</el-button>
          </div>
        </template>
        <el-tree
          :data="treeNodes"
          node-key="id"
          :props="{ label: 'name', children: 'children' }"
          highlight-current
          default-expand-all
          @node-click="onTreeSelect"
        />
      </el-card>
      <div class="main-panel">
        <el-card>
          <template #header>
            <div class="toolbar actions">
              <span>{{ tableInfo.columnCNName || tableInfo.tableName || t('sysCoder.title') }}</span>
              <div class="seven-btn-group">
                <el-button type="primary" :icon="ActionIcons.save" @click="save">{{ t('sysCoder.save') }}</el-button>
                <el-button :icon="ActionIcons.sync" @click="syncTable">{{ t('sysCoder.syncTable') }}</el-button>
                <el-button :icon="ActionIcons.services" @click="createServices">{{ t('sysCoder.createServices') }}</el-button>
                <el-button :icon="ActionIcons.vue" @click="createVue">{{ t('sysCoder.createVue') }}</el-button>
                <el-button type="danger" :icon="ActionIcons.delete" @click="delTree">{{ t('sysCoder.deleteTree') }}</el-button>
              </div>
            </div>
          </template>

          <el-tabs v-model="activeTab">
            <el-tab-pane :label="t('sysCoder.tabBasic')" name="basic">
              <el-form :model="tableInfo" label-width="100px" class="config-form">
                <el-row :gutter="16">
                  <el-col :span="12">
                    <el-form-item :label="t('sysCoder.tableName')">
                      <el-input v-model="tableInfo.tableName" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item :label="t('sysCoder.cnName')">
                      <el-input v-model="tableInfo.columnCNName" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item :label="t('sysCoder.namespace')">
                      <el-select v-model="tableInfo.namespace" style="width:100%">
                        <el-option v-for="ns in namespaces" :key="ns" :label="ns" :value="ns" />
                      </el-select>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item :label="t('sysCoder.folderName')">
                      <el-input v-model="tableInfo.folderName" />
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-form>
            </el-tab-pane>

            <el-tab-pane :label="t('sysCoder.tabColumns')" name="columns">
              <el-table :data="columns" border size="small">
                <el-table-column prop="columnName" :label="t('sysCoder.colName')" width="140" />
                <el-table-column :label="t('sysCoder.colCnName')" min-width="120">
                  <template #default="{ row }">
                    <el-input v-model="row.columnCNName" size="small" />
                  </template>
                </el-table-column>
                <el-table-column prop="columnType" :label="t('sysCoder.colType')" width="100" />
                <el-table-column :label="t('sysCoder.colKey')" width="70">
                  <template #default="{ row }">{{ row.isKey ? 'Y' : '' }}</template>
                </el-table-column>
                <el-table-column :label="t('sysCoder.colEditable')" width="80">
                  <template #default="{ row }">
                    <el-switch v-model="row.editable" size="small" />
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <el-tab-pane :label="t('sysCoder.tabDetails')" name="details">
              <div class="detail-toolbar">
                <el-button :icon="ActionIcons.sync" :disabled="!tableInfo.tableName" @click="scanForeignKeys">
                  {{ t('sysCoder.scanForeignKeys') }}
                </el-button>
                <el-button type="primary" :icon="ActionIcons.save" :disabled="!tableInfo.tableName" @click="saveDetails">
                  {{ t('sysCoder.saveDetails') }}
                </el-button>
                <span class="detail-hint">{{ t('sysCoder.detailHint') }}</span>
              </div>
              <el-table :data="tableDetails" border size="small">
                <el-table-column :label="t('sysCoder.detailEnable')" width="80">
                  <template #default="{ row }">
                    <el-switch v-model="row.enable" size="small" />
                  </template>
                </el-table-column>
                <el-table-column prop="childTable" :label="t('sysCoder.detailChild')" width="140" />
                <el-table-column :label="t('sysCoder.detailCnName')" min-width="120">
                  <template #default="{ row }">
                    <el-input v-model="row.cnName" size="small" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('sysCoder.detailForeignKey')" width="140">
                  <template #default="{ row }">
                    <el-input v-model="row.foreignKey" size="small" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('sysCoder.detailMasterKey')" width="120">
                  <template #default="{ row }">
                    <el-input v-model="row.masterKey" size="small" placeholder="PK" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('sysCoder.detailDisplayMode')" width="140">
                  <template #default="{ row }">
                    <el-select v-model="row.displayMode" size="small" style="width:100%">
                      <el-option label="Below" value="Below" />
                      <el-option label="Dialog" value="Dialog" />
                      <el-option label="Page" value="Page" />
                    </el-select>
                  </template>
                </el-table-column>
                <el-table-column :label="t('sysCoder.detailOrderNo')" width="90">
                  <template #default="{ row }">
                    <el-input-number v-model="row.orderNo" size="small" :controls="false" style="width:100%" />
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>
          </el-tabs>
        </el-card>
      </div>
    </div>

    <el-dialog v-model="showAdd" :title="t('sysCoder.addConfig')" width="480px">
      <el-form :model="addForm" label-width="100px">
        <el-form-item :label="t('sysCoder.tableName')">
          <el-input v-model="addForm.tableName" />
        </el-form-item>
        <el-form-item :label="t('sysCoder.cnName')">
          <el-input v-model="addForm.columnCNName" />
        </el-form-item>
        <el-form-item :label="t('sysCoder.namespace')">
          <el-select v-model="addForm.namespace" style="width:100%">
            <el-option v-for="ns in namespaces" :key="ns" :label="ns" :value="ns" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysCoder.folderName')">
          <el-input v-model="addForm.folderName" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="showAdd = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.confirm" @click="addConfig">{{ t('common.confirm') }}</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'

interface TreeNode {
  id: number
  pId: number
  name: string
  children?: TreeNode[]
}

interface TableColumn {
  columnId?: number
  columnName: string
  columnCNName?: string
  columnType?: string
  isKey?: boolean
  editable?: boolean
  orderNo?: number
  isColumnData?: number
}

interface TableInfo {
  table_Id: number
  parentId?: number
  tableName: string
  columnCNName?: string
  namespace?: string
  folderName?: string
  tableColumns?: TableColumn[]
}

interface TableDetail {
  detailId: number
  parentTable: string
  childTable: string
  foreignKey: string
  masterKey?: string | null
  enable: boolean
  displayMode: string
  orderNo: number
  cnName?: string | null
}

const { t } = useI18n()
const treeFlat = ref<TreeNode[]>([])
const namespaces = ref<string[]>([])
const activeTab = ref('basic')
const tableDetails = ref<TableDetail[]>([])
const tableInfo = reactive<TableInfo>({
  table_Id: 0,
  parentId: 0,
  tableName: '',
  columnCNName: '',
  namespace: 'Seven.Domain.Entities.System',
  folderName: 'System',
  tableColumns: [],
})
const showAdd = ref(false)
const addForm = reactive({
  parentId: 0,
  tableName: '',
  columnCNName: '',
  namespace: 'Seven.Domain.Entities.System',
  folderName: 'System',
  table_Id: 0,
  isTreeLoad: false,
})

const columns = computed(() => tableInfo.tableColumns ?? [])
const treeNodes = computed(() => buildTree(treeFlat.value))

function buildTree(flat: TreeNode[], parentId = 0): TreeNode[] {
  return flat
    .filter((n) => n.pId === parentId)
    .map((n) => {
      const children = buildTree(flat, n.id)
      return children.length ? { ...n, children } : { ...n }
    })
}

async function loadTree() {
  const res = await http.post('/api/Builder/GetTableTree', {})
  if (res.status && res.data) {
    const data = res.data as { list: TreeNode[]; nameSpace: string[] }
    treeFlat.value = data.list ?? []
    namespaces.value = data.nameSpace ?? []
  }
}

async function loadDetails(parentTable: string) {
  if (!parentTable) {
    tableDetails.value = []
    return
  }
  const res = await http.get(`/api/Builder/GetTableDetails?parentTable=${encodeURIComponent(parentTable)}`)
  if (res.status && Array.isArray(res.data)) {
    tableDetails.value = (res.data as TableDetail[]).map((d) => ({
      ...d,
      enable: !!d.enable,
      displayMode: d.displayMode || 'Below',
    }))
  } else {
    tableDetails.value = []
  }
}

async function loadTable(tableId: number, isTree = true) {
  const res = await http.post('/api/Builder/LoadTableInfo', {
    parentId: tableInfo.parentId ?? 0,
    tableName: tableInfo.tableName,
    columnCNName: tableInfo.columnCNName,
    namespace: tableInfo.namespace,
    folderName: tableInfo.folderName,
    table_Id: tableId,
    isTreeLoad: isTree,
  })
  if (res.status && res.data) {
    Object.assign(tableInfo, res.data as TableInfo)
    await loadDetails(tableInfo.tableName)
  }
}

function onTreeSelect(node: TreeNode) {
  loadTable(node.id, true)
}

async function addConfig() {
  addForm.parentId = tableInfo.table_Id || 0
  const res = await http.post('/api/Builder/LoadTableInfo', { ...addForm, table_Id: 0, isTreeLoad: false })
  if (res.status) {
    ElMessage.success(t('common.success'))
    showAdd.value = false
    await loadTree()
    if (res.data) {
      Object.assign(tableInfo, res.data as TableInfo)
      await loadDetails(tableInfo.tableName)
    }
  }
}

async function save() {
  const res = await http.post('/api/Builder/Save', { ...tableInfo })
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadTree()
  } else {
    ElMessage.error(res.message || t('sysCoder.saveFailed'))
  }
}

async function saveDetails() {
  if (!tableInfo.tableName) return
  const res = await http.post('/api/Builder/SaveTableDetails', {
    parentTable: tableInfo.tableName,
    details: tableDetails.value,
  })
  if (res.status) {
    ElMessage.success(t('common.success'))
    if (Array.isArray(res.data)) {
      tableDetails.value = (res.data as TableDetail[]).map((d) => ({
        ...d,
        enable: !!d.enable,
        displayMode: d.displayMode || 'Below',
      }))
    }
  } else {
    ElMessage.error(res.message || t('sysCoder.saveFailed'))
  }
}

async function scanForeignKeys() {
  if (!tableInfo.tableName) return
  const res = await http.post('/api/Builder/ScanForeignKeys', { parentTable: tableInfo.tableName })
  if (res.status) {
    ElMessage.success(res.message || t('common.success'))
    if (Array.isArray(res.data)) {
      tableDetails.value = (res.data as TableDetail[]).map((d) => ({
        ...d,
        enable: !!d.enable,
        displayMode: d.displayMode || 'Below',
      }))
    } else {
      await loadDetails(tableInfo.tableName)
    }
  } else {
    ElMessage.error(res.message || t('sysCoder.scanFailed'))
  }
}

async function syncTable() {
  if (!tableInfo.tableName) return
  const res = await http.post('/api/Builder/syncTable', tableInfo.tableName)
  if (res.status) {
    ElMessage.success(res.message || t('common.success'))
    await loadTable(tableInfo.table_Id, true)
  } else {
    ElMessage.error(res.message || t('sysCoder.syncFailed'))
  }
}

async function createServices() {
  const res = await http.post('/api/Builder/CreateServices', {
    tableName: tableInfo.tableName,
    namespace: tableInfo.namespace,
    folderName: tableInfo.folderName,
  })
  ElMessage.success(res.message || (res.status ? t('common.success') : t('sysCoder.failed')))
}

async function createVue() {
  const res = await http.post('/api/Builder/CreateVuePage', { tableInfo: { ...tableInfo } })
  ElMessage.success(res.message || (res.status ? t('common.success') : t('sysCoder.failed')))
}

async function delTree() {
  if (!tableInfo.table_Id) return
  await ElMessageBox.confirm(t('sysCoder.deleteConfirm'), t('sysCoder.deleteTree'), { type: 'warning' })
  const res = await http.post('/api/Builder/delTree', tableInfo.table_Id)
  if (res.status) {
    ElMessage.success(t('common.success'))
    tableInfo.table_Id = 0
    tableDetails.value = []
    await loadTree()
  } else {
    ElMessage.error(res.message || t('sysCoder.deleteFailed'))
  }
}

onMounted(loadTree)
</script>

<style scoped>
.detail-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.detail-hint {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
