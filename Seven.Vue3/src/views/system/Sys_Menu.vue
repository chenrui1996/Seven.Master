<template>
  <div class="crud-page seven-page menu-page">
    <div class="split-layout">
      <el-card class="tree-panel">
        <template #header>
          <div class="toolbar">
            <span>{{ t('sysMenu.treeTitle') }}</span>
            <div class="seven-btn-group">
              <el-tooltip :content="t('sysMenu.addRoot')" placement="top">
                <el-button v-permission="'Sys_Menu.Add'" size="small" type="primary" :icon="ActionIcons.add" @click="addRoot" />
              </el-tooltip>
              <el-tooltip :content="t('sysMenu.addChild')" placement="top">
                <el-button v-permission="'Sys_Menu.Add'" size="small" :icon="ActionIcons.addChild" @click="addChild" />
              </el-tooltip>
              <el-tooltip :content="t('sysMenu.addBrother')" placement="top">
                <el-button v-permission="'Sys_Menu.Add'" size="small" :icon="ActionIcons.addSibling" @click="addBrother" />
              </el-tooltip>
            </div>
          </div>
        </template>
        <el-tree
          :data="menuTree"
          node-key="menu_Id"
          :props="{ label: 'menuName', children: 'children' }"
          highlight-current
          default-expand-all
          @node-click="onNodeClick"
        />
      </el-card>
      <el-card class="form-panel">
        <template #header>
          <div class="toolbar">
            <span>{{ t('sysMenu.formTitle') }}</span>
            <div class="seven-btn-group">
              <el-button v-permission="'Sys_Menu.Update'" type="primary" :icon="ActionIcons.save" @click="save">
                {{ t('sysMenu.save') }}
              </el-button>
              <el-button v-permission="'Sys_Menu.Delete'" type="danger" :icon="ActionIcons.delete" @click="remove">
                {{ t('sysMenu.delete') }}
              </el-button>
            </div>
          </div>
        </template>
        <el-alert type="warning" :closable="false" show-icon class="menu-tip">
          <template #title>{{ t('sysMenu.tipTitle') }}</template>
          <div>{{ t('sysMenu.tipLine1') }}</div>
          <div>{{ t('sysMenu.tipLine2') }}</div>
        </el-alert>
        <el-form :model="form" label-width="100px" class="menu-form">
          <el-form-item :label="t('sysMenu.colParent')" required>
            <el-input-number v-model="form.parentId" :min="0" controls-position="right" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colName')" required>
            <el-input v-model="form.menuName" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colUrl')">
            <el-input v-model="form.url" placeholder="/Sys_User" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colTable')">
            <el-input v-model="form.tableName" placeholder="Sys_User 或 . /" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colIcon')">
            <el-input v-model="form.icon" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colOrder')">
            <el-input-number v-model="form.orderNo" :min="0" controls-position="right" />
            <span class="order-hint">{{ t('sysMenu.orderHint') }}</span>
          </el-form-item>
          <el-form-item :label="t('sysMenu.colStatus')">
            <el-switch v-model="form.enable" :active-value="1" :inactive-value="0" />
          </el-form-item>
          <el-form-item :label="t('sysMenu.colAuth')">
            <el-checkbox-group v-model="authList">
              <el-checkbox v-for="a in displayAuthOptions" :key="a" :label="a">{{ a }}</el-checkbox>
            </el-checkbox-group>
            <div class="auth-custom">
              <el-input
                v-model="customAuth"
                :placeholder="t('sysMenu.customAuthPlaceholder')"
                clearable
                style="width: 200px"
                @keyup.enter="addCustomAuth"
              />
              <el-button type="primary" plain @click="addCustomAuth">{{ t('sysMenu.addAuth') }}</el-button>
            </div>
            <div class="auth-hint">{{ t('sysMenu.customAuthHint') }}</div>
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { getMenu } from '../../api/http'
import { buildMenuTree } from '../../utils/menuTree'
import { ActionIcons } from '../../constants/actionIcons'
import type { MenuItem } from '../../stores'
import { useMenuStore } from '../../stores'

const { t } = useI18n()
const menuStore = useMenuStore()
const menuFlat = ref<MenuItem[]>([])
const menuTree = ref<MenuItem[]>([])
const authOptions = [
  'Search',
  'Add',
  'Update',
  'Delete',
  'Import',
  'Export',
  'Upload',
  'Audit',
  'Acknowledge',
  'Clear',
  'Raise',
  'BatchCustom',
  'RowCustom',
]
const authList = ref<string[]>([])
const customAuth = ref('')

/** 预设 + 本菜单已保存的自定义动作码 */
const displayAuthOptions = computed(() => {
  const set = new Set<string>([...authOptions, ...authList.value])
  return [...set]
})

function addCustomAuth() {
  const code = customAuth.value.trim()
  if (!code) {
    ElMessage.warning(t('sysMenu.customAuthRequired'))
    return
  }
  if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(code)) {
    ElMessage.warning(t('sysMenu.customAuthInvalid'))
    return
  }
  if (!authList.value.some((a) => a.toLowerCase() === code.toLowerCase())) {
    authList.value = [...authList.value, code]
  }
  customAuth.value = ''
}

const form = reactive({
  menu_Id: 0,
  parentId: 0,
  menuName: '',
  url: '',
  tableName: '',
  icon: '',
  orderNo: 0,
  enable: 1 as number,
  menuType: 0,
  auth: '',
})

watch(authList, (v) => { form.auth = v.join(',') })

function resetForm(partial: Partial<typeof form> = {}) {
  Object.assign(form, {
    menu_Id: 0,
    parentId: 0,
    menuName: '',
    url: '',
    tableName: '',
    icon: '',
    orderNo: 0,
    enable: 1,
    menuType: 0,
    auth: 'Search',
    ...partial,
  })
  authList.value = (form.auth ?? '').split(',').filter(Boolean)
  if (authList.value.length === 0) authList.value = ['Search']
}

function normalizeMenuRow(row: Record<string, unknown>): MenuItem {
  return {
    menu_Id: Number(row.menu_Id ?? row.Menu_Id ?? 0),
    parentId: Number(row.parentId ?? row.ParentId ?? 0),
    menuName: String(row.menuName ?? row.MenuName ?? ''),
    url: String(row.url ?? row.Url ?? ''),
    tableName: String(row.tableName ?? row.TableName ?? ''),
    icon: String(row.icon ?? row.Icon ?? ''),
    orderNo: Number(row.orderNo ?? row.OrderNo ?? 0),
    enable: Number(row.enable ?? row.Enable ?? 1),
    auth: String(row.auth ?? row.Auth ?? ''),
  }
}

async function loadTree() {
  const res = await http.get('/api/Sys_Menu/getMenuList')
  if (!res.status || !res.data) return
  const flat = (res.data as Record<string, unknown>[]).map(normalizeMenuRow)
  menuFlat.value = flat
  menuTree.value = buildMenuTree(flat)
}

/** 同步左侧导航菜单顺序（不重建动态路由） */
async function refreshSidebarMenus() {
  try {
    const res = await getMenu()
    if (res.status && res.data) {
      const flat = (res.data as unknown as Record<string, unknown>[]).map(normalizeMenuRow)
      menuStore.setMenus(buildMenuTree(flat))
    }
  } catch {
    /* 忽略：侧栏刷新失败不影响管理页 */
  }
}

async function loadTreeItem(menuId: number) {
  const res = await http.post(`/api/Sys_Menu/getTreeItem?menuId=${menuId}`)
  if (!res.status || !res.data) {
    ElMessage.error(res.message || t('common.operationFailed'))
    return
  }
  const row = res.data as Record<string, unknown>
  Object.assign(form, {
    menu_Id: Number(row.menu_Id ?? row.Menu_Id ?? 0),
    parentId: Number(row.parentId ?? row.ParentId ?? 0),
    menuName: String(row.menuName ?? row.MenuName ?? ''),
    url: String(row.url ?? row.Url ?? ''),
    tableName: String(row.tableName ?? row.TableName ?? ''),
    icon: String(row.icon ?? row.Icon ?? ''),
    orderNo: Number(row.orderNo ?? row.OrderNo ?? 0),
    enable: Number(row.enable ?? row.Enable ?? 1),
    menuType: Number(row.menuType ?? row.MenuType ?? 0),
    auth: String(row.auth ?? row.Auth ?? ''),
  })
  authList.value = (form.auth ?? '').split(',').filter(Boolean)
}

function onNodeClick(data: MenuItem) {
  if (data.menu_Id) loadTreeItem(data.menu_Id)
}

function isSelected() {
  if (!form.menu_Id) {
    ElMessage.warning(t('sysMenu.selectFirst'))
    return false
  }
  return true
}

function addRoot() {
  resetForm({ parentId: 0, auth: 'Search' })
}

function addChild() {
  if (!isSelected()) return
  resetForm({ parentId: form.menu_Id, auth: 'Search,Add,Update,Delete' })
}

function addBrother() {
  if (!isSelected()) return
  resetForm({ parentId: form.parentId, auth: 'Search,Add,Update,Delete' })
}

async function save() {
  if (!form.menuName) {
    ElMessage.warning(t('sysMenu.requiredName'))
    return
  }
  const res = await http.post('/api/Sys_Menu/save', { ...form })
  if (!res.status) {
    ElMessage.error(res.message || t('common.operationFailed'))
    return
  }
  ElMessage.success(t('common.success'))
  const saved = res.data as Record<string, unknown> | undefined
  if (saved) {
    form.menu_Id = Number(saved.menu_Id ?? saved.Menu_Id ?? form.menu_Id)
  }
  await loadTree()
  await refreshSidebarMenus()
}

async function remove() {
  if (!form.menu_Id) {
    ElMessage.warning(t('sysMenu.selectFirst'))
    return
  }
  await ElMessageBox.confirm(
    t('sysMenu.deleteConfirmNamed', { name: form.menuName }),
    t('sysMenu.delete'),
    { type: 'warning' },
  )
  const res = await http.post(`/api/Sys_Menu/del?menuId=${form.menu_Id}`)
  if (!res.status) {
    ElMessage.error(res.message || t('common.operationFailed'))
    return
  }
  ElMessage.success(res.message || t('common.success'))
  resetForm()
  await loadTree()
  await refreshSidebarMenus()
}

onMounted(loadTree)
</script>

<style scoped>
.menu-tip {
  margin-bottom: 12px;
}
.menu-form {
  max-width: 560px;
}
.auth-custom {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}
.auth-hint {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
}
.order-hint {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
