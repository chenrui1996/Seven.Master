<template>
  <div class="crud-page seven-page role-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysRole.listTitle') }}</span>
          <el-button v-permission="'Sys_Role.Add'" type="primary" :icon="ActionIcons.add" @click="openForm()">
            {{ t('common.add') }}
          </el-button>
        </div>
      </template>
      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="role_Id" :label="t('sysRole.colId')" width="80" />
        <el-table-column prop="roleName" :label="t('sysRole.colName')" />
        <el-table-column prop="orderNo" :label="t('sysRole.colOrder')" width="80" />
        <el-table-column :label="t('sysRole.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
        <el-table-column :label="t('sysRole.colActions')" width="220" fixed="right">
          <template #default="{ row }">
            <el-button v-permission="'Sys_Role.Update'" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">
              {{ t('sysRole.edit') }}
            </el-button>
            <el-button v-permission="'Sys_Role.Update'" link type="warning" :icon="ActionIcons.permission" @click="openPermission(row)">
              {{ t('sysRole.permission') }}
            </el-button>
            <el-button v-permission="'Sys_Role.Delete'" link type="danger" :icon="ActionIcons.delete" @click="remove(row)">
              {{ t('sysRole.delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="rows"
        :total="total"
        @change="loadData"
        style="margin-top:16px"
      />
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      :title="form.role_Id ? t('sysRole.editRole') : t('sysRole.addRole')"
      width="480px"
    >
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('sysRole.colName')" required>
          <el-input v-model="form.roleName" />
        </el-form-item>
        <el-form-item :label="t('sysRole.colOrder')">
          <el-input-number v-model="form.orderNo" :min="0" />
        </el-form-item>
        <el-form-item :label="t('sysRole.colStatus')">
          <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.confirm" @click="save">{{ t('common.confirm') }}</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog
      v-model="permDialogVisible"
      :title="permDialogTitle"
      width="860px"
      class="role-perm-dialog"
      destroy-on-close
    >
      <div v-loading="permLoading" class="perm-panel">
        <div class="perm-panel__header">
          <span>{{ t('sysRole.permissionMenuTitle') }}</span>
          <span v-if="permRoleName" class="perm-panel__role">{{ permRoleName }}</span>
        </div>
        <el-scrollbar class="perm-scroll">
          <el-tree
            :data="permissionTree"
            node-key="id"
            default-expand-all
            :expand-on-click-node="false"
            :indent="18"
          >
            <template #default="{ data }">
              <div class="perm-node">
                <div class="perm-node__menu" :style="{ minWidth: menuLabelWidth(data.lv) }">
                  <el-checkbox
                    v-if="data.actions.length"
                    v-model="data.leftCk"
                    @change="(val: boolean) => onMenuCheckChange(data, val)"
                  >
                    {{ data.text }}{{ data.isApp ? t('sysRole.appSuffix') : '' }}
                  </el-checkbox>
                  <span v-else class="perm-node__folder">{{ data.text }}</span>
                </div>
                <div v-if="data.actions.length" class="perm-node__actions">
                  <el-checkbox
                    v-for="action in data.actions"
                    :key="action.value"
                    v-model="action.checked"
                    @change="() => onActionChange(data)"
                  >
                    {{ actionLabel(action) }}
                  </el-checkbox>
                </div>
              </div>
            </template>
          </el-tree>
          <el-empty v-if="!permLoading && !permissionTree.length" :description="t('sysRole.permissionEmpty')" />
        </el-scrollbar>
      </div>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="permDialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.save" :loading="permSaving" @click="savePermission">
            {{ t('sysRole.permissionSave') }}
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { getPageData } from '../../api/http'
import { ActionIcons } from '../../constants/actionIcons'
import {
  applyRolePermissions,
  buildPermissionTree,
  collectPermissions,
  setNodeActionsChecked,
  syncParentLeftCheck,
  type PermissionAction,
  type PermissionNode,
} from '../../utils/rolePermission'

interface RoleRow {
  role_Id?: number
  roleName?: string
  orderNo?: number
  enable?: number
  parentId?: number
}

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<RoleRow[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const dialogVisible = ref(false)
const permDialogVisible = ref(false)
const permLoading = ref(false)
const permSaving = ref(false)
const permissionTree = ref<PermissionNode[]>([])
const permissionFlat = ref<PermissionNode[]>([])
const permRoleId = ref(0)
const permRoleName = ref('')

const form = reactive<RoleRow>({ role_Id: 0, roleName: '', orderNo: 0, enable: 1, parentId: 0 })

const permDialogTitle = computed(() => t('sysRole.permissionTitle'))

function menuLabelWidth(lv?: number) {
  const level = lv ?? 1
  return `${Math.max(140, 190 - (level - 1) * 16)}px`
}

function actionLabel(action: PermissionAction) {
  const key = `sysRole.actions.${action.value}`
  const translated = t(key)
  return translated === key ? action.text : translated
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_Role/getPageData', { page: page.value, rows: rows.value })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: RoleRow[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally {
    loading.value = false
  }
}

function openForm(row?: RoleRow) {
  if (row) {
    Object.assign(form, { ...row })
  } else {
    Object.assign(form, { role_Id: 0, roleName: '', orderNo: 0, enable: 1, parentId: 0 })
  }
  dialogVisible.value = true
}

async function save() {
  if (!form.roleName) {
    ElMessage.warning(t('sysRole.requiredName'))
    return
  }
  const url = form.role_Id ? '/api/Sys_Role/update' : '/api/Sys_Role/add'
  const res = await http.post(url, form)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadData()
  }
}

async function remove(row: RoleRow) {
  await ElMessageBox.confirm(t('sysRole.deleteConfirm'), t('sysRole.delete'), { type: 'warning' })
  const res = await http.post('/api/Sys_Role/del', [row.role_Id])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  }
}

async function openPermission(row: RoleRow) {
  permRoleId.value = row.role_Id!
  permRoleName.value = row.roleName ?? ''
  permDialogVisible.value = true
  permLoading.value = true
  permissionTree.value = []
  permissionFlat.value = []

  try {
    const [templateRes, assignedRes] = await Promise.all([
      http.post('/api/Sys_Role/getCurrentTreePermission'),
      http.post('/api/Sys_Role/getUserTreePermission', { roleId: row.role_Id }),
    ])

    if (!templateRes.status || !templateRes.data) {
      ElMessage.error(templateRes.message || t('sysRole.permissionLoadFailed'))
      return
    }

    const flat = (templateRes.data as PermissionNode[]).map((item) => ({
      id: Number((item as PermissionNode & { Id?: number }).id ?? (item as PermissionNode & { Id?: number }).Id),
      pid: Number((item as PermissionNode & { Pid?: number }).pid ?? (item as PermissionNode & { Pid?: number }).Pid),
      text: String((item as PermissionNode & { Text?: string }).text ?? (item as PermissionNode & { Text?: string }).Text ?? ''),
      isApp: Boolean((item as PermissionNode & { IsApp?: boolean }).isApp ?? (item as PermissionNode & { IsApp?: boolean }).IsApp),
      actions: ((item.actions ?? (item as PermissionNode & { Actions?: PermissionAction[] }).Actions) ?? []).map((a) => ({
        text: String((a as PermissionAction & { Text?: string }).text ?? (a as PermissionAction & { Text?: string }).Text ?? ''),
        value: String((a as PermissionAction & { Value?: string }).value ?? (a as PermissionAction & { Value?: string }).Value ?? ''),
        checked: false,
      })),
    }))

    if (assignedRes.status && assignedRes.data) {
      applyRolePermissions(flat, assignedRes.data)
    }

    permissionFlat.value = flat
    permissionTree.value = buildPermissionTree(flat)
  } finally {
    permLoading.value = false
  }
}

function onMenuCheckChange(node: PermissionNode, checked: boolean) {
  setNodeActionsChecked(node, checked)
  syncParentLeftCheck(permissionTree.value, node)
}

function onActionChange(node: PermissionNode) {
  syncParentLeftCheck(permissionTree.value, node)
}

async function savePermission() {
  if (!permRoleId.value) return
  permSaving.value = true
  try {
    const permissions = collectPermissions(permissionFlat.value)
    const res = await http.post('/api/Sys_Role/savePermission', {
      roleId: permRoleId.value,
      permissions,
    })
    if (res.status) {
      ElMessage.success(res.message || t('common.success'))
      permDialogVisible.value = false
    }
  } finally {
    permSaving.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.role-page :deep(.role-perm-dialog .el-dialog__body) {
  padding-top: 8px;
}

.perm-panel {
  min-height: 420px;
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
  background: var(--seven-bg-panel);
}

.perm-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--seven-space-2) var(--seven-space-4);
  border-bottom: 1px solid var(--seven-border-light);
  background: var(--seven-bg-subtle);
  font-size: var(--seven-text-sm);
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.perm-panel__role {
  font-weight: 500;
  color: var(--seven-accent);
}

.perm-scroll {
  height: 420px;
  padding: var(--seven-space-3) var(--seven-space-4);
}

.perm-node {
  display: flex;
  align-items: flex-start;
  gap: var(--seven-space-3);
  width: 100%;
  padding: 2px 0 6px;
}

.perm-node__menu {
  flex-shrink: 0;
}

.perm-node__folder {
  font-size: var(--seven-text-sm);
  font-weight: 600;
  color: var(--seven-text);
}

.perm-node__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 12px;
  flex: 1;
  min-width: 0;
  padding-top: 1px;
}

.role-page :deep(.el-tree-node__content) {
  height: auto;
  align-items: flex-start;
  padding-top: 2px;
  padding-bottom: 2px;
}

.role-page :deep(.el-checkbox__label) {
  font-size: var(--seven-text-sm);
}
</style>
