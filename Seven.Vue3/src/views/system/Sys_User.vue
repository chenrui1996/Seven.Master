<template>
  <div class="crud-page seven-page">
    <el-card>
      <template #header>
        <div class="toolbar">
          <span>{{ t('sysUser.listTitle') }}</span>
          <el-button v-permission="'Sys_User.Add'" type="primary" :icon="ActionIcons.add" @click="openForm()">
            {{ t('common.add') }}
          </el-button>
        </div>
      </template>
      <el-table :data="tableData" v-loading="loading" border>
        <el-table-column prop="user_Id" :label="t('sysUser.colId')" width="80" />
        <el-table-column prop="userName" :label="t('sysUser.colAccount')" />
        <el-table-column prop="userTrueName" :label="t('sysUser.colName')" />
        <el-table-column prop="roleName" :label="t('sysUser.colRole')" />
        <el-table-column prop="phoneNo" :label="t('sysUser.colPhone')" />
        <el-table-column :label="t('sysUser.colStatus')" width="80">
          <template #default="{ row }">
            {{ row.enable === 1 ? t('common.enabled') : t('common.disabled') }}
          </template>
        </el-table-column>
        <el-table-column :label="t('sysUser.colActions')" width="200" fixed="right">
          <template #default="{ row }">
            <el-button v-permission="'Sys_User.Update'" link type="primary" :icon="ActionIcons.edit" @click="openForm(row)">
              {{ t('sysUser.edit') }}
            </el-button>
            <el-button v-permission="'Sys_User.Update'" link type="warning" :icon="ActionIcons.resetPwd" @click="openPwdForm(row)">
              {{ t('sysUser.resetPwd') }}
            </el-button>
            <el-button v-permission="'Sys_User.Delete'" link type="danger" :icon="ActionIcons.delete" @click="remove(row)">
              {{ t('sysUser.delete') }}
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
      :title="form.user_Id ? t('sysUser.editUser') : t('sysUser.addUser')"
      width="520px"
    >
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('sysUser.colAccount')" required>
          <el-input v-model="form.userName" :disabled="!!form.user_Id" />
        </el-form-item>
        <el-form-item v-if="!form.user_Id" :label="t('sysUser.password')" required>
          <el-input v-model="form.passwordHash" type="password" show-password />
        </el-form-item>
        <el-form-item :label="t('sysUser.colName')" required>
          <el-input v-model="form.userTrueName" />
        </el-form-item>
        <el-form-item :label="t('sysUser.colRole')" required>
          <el-select v-model="form.role_Id" style="width:100%" @change="onRoleChange">
            <el-option
              v-for="r in roleOptions"
              :key="r.role_Id"
              :label="r.roleName || ''"
              :value="r.role_Id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysUser.colPhone')">
          <el-input v-model="form.phoneNo" />
        </el-form-item>
        <el-form-item :label="t('sysUser.colStatus')">
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

    <el-dialog v-model="pwdDialogVisible" :title="t('sysUser.resetPwd')" width="420px">
      <el-form :model="pwdForm" label-width="100px">
        <el-form-item :label="t('sysUser.oldPwd')" required>
          <el-input v-model="pwdForm.oldPwd" type="password" show-password />
        </el-form-item>
        <el-form-item :label="t('sysUser.newPwd')" required>
          <el-input v-model="pwdForm.newPwd" type="password" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-actions">
          <el-button :icon="ActionIcons.cancel" @click="pwdDialogVisible = false">{{ t('common.cancel') }}</el-button>
          <el-button type="primary" :icon="ActionIcons.save" @click="savePwd">{{ t('common.confirm') }}</el-button>
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

interface UserRow {
  user_Id?: number
  userName: string
  userTrueName: string
  role_Id: number
  roleName?: string
  phoneNo?: string
  enable: number
  passwordHash?: string
}

interface RoleOption {
  role_Id: number
  roleName?: string
}

const { t } = useI18n()
const loading = ref(false)
const tableData = ref<UserRow[]>([])
const roleOptions = ref<RoleOption[]>([])
const total = ref(0)
const page = ref(1)
const rows = ref(30)
const dialogVisible = ref(false)
const pwdDialogVisible = ref(false)

const form = reactive<UserRow>({
  user_Id: 0,
  userName: '',
  userTrueName: '',
  role_Id: 0,
  roleName: '',
  phoneNo: '',
  enable: 1,
  passwordHash: '123456',
})

const pwdForm = reactive({ userId: 0, oldPwd: '', newPwd: '' })

async function loadRoles() {
  const res = await getPageData('/api/Sys_Role/getPageData', { page: 1, rows: 200 })
  if (res.status && res.data) {
    roleOptions.value = (res.data as { rows: RoleOption[] }).rows
  }
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPageData('/api/Sys_User/getPageData', { page: page.value, rows: rows.value })
    if (res.status && res.data) {
      const data = res.data as { total: number; rows: UserRow[] }
      total.value = data.total
      tableData.value = data.rows
    }
  } finally {
    loading.value = false
  }
}

function onRoleChange(roleId: number) {
  const role = roleOptions.value.find((r) => r.role_Id === roleId)
  form.roleName = role?.roleName
}

function openForm(row?: UserRow) {
  if (row) {
    Object.assign(form, { ...row, passwordHash: '' })
  } else {
    Object.assign(form, {
      user_Id: 0,
      userName: '',
      userTrueName: '',
      role_Id: roleOptions.value[0]?.role_Id ?? 0,
      roleName: roleOptions.value[0]?.roleName,
      phoneNo: '',
      enable: 1,
      passwordHash: '123456',
    })
  }
  dialogVisible.value = true
}

function openPwdForm(row: UserRow) {
  pwdForm.userId = row.user_Id!
  pwdForm.oldPwd = ''
  pwdForm.newPwd = ''
  pwdDialogVisible.value = true
}

async function save() {
  if (!form.userName || !form.userTrueName || !form.role_Id) {
    ElMessage.warning(t('sysUser.requiredFields'))
    return
  }
  const payload = { ...form, passwordHash: form.passwordHash || '123456' }
  const url = form.user_Id ? '/api/Sys_User/update' : '/api/Sys_User/add'
  const res = await http.post(url, payload)
  if (res.status) {
    ElMessage.success(t('common.success'))
    dialogVisible.value = false
    await loadData()
  } else {
    ElMessage.error(res.message || t('sysUser.saveFailed'))
  }
}

async function savePwd() {
  const res = await http.post('/api/Sys_User/modifyPwd', {
    userId: pwdForm.userId,
    oldPwd: pwdForm.oldPwd,
    newPwd: pwdForm.newPwd,
  })
  if (res.status) {
    ElMessage.success(t('common.success'))
    pwdDialogVisible.value = false
  } else {
    ElMessage.error(res.message || t('sysUser.saveFailed'))
  }
}

async function remove(row: UserRow) {
  await ElMessageBox.confirm(t('sysUser.deleteConfirm'), t('sysUser.delete'), { type: 'warning' })
  const res = await http.post('/api/Sys_User/del', [row.user_Id])
  if (res.status) {
    ElMessage.success(t('common.success'))
    await loadData()
  }
}

onMounted(async () => {
  await loadRoles()
  await loadData()
})
</script>
