<template>
  <div class="crud-page seven-page user-info-page">
    <el-card>
      <template #header>{{ t('userInfo.title') }}</template>
      <el-form :model="form" label-width="110px" style="max-width: 520px">
        <el-form-item :label="t('userInfo.userName')">
          <el-input v-model="form.userName" disabled />
        </el-form-item>
        <el-form-item :label="t('userInfo.userTrueName')">
          <el-input v-model="form.userTrueName" />
        </el-form-item>
        <el-form-item :label="t('userInfo.phone')">
          <el-input v-model="form.phoneNo" />
        </el-form-item>
        <el-form-item :label="t('userInfo.email')">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item :label="t('userInfo.gender')">
          <el-radio-group v-model="form.gender">
            <el-radio :value="1">{{ t('userInfo.male') }}</el-radio>
            <el-radio :value="0">{{ t('userInfo.female') }}</el-radio>
            <el-radio :value="2">{{ t('userInfo.unknown') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('userInfo.avatar')">
          <FileUploadField v-model="form.headImageUrl" accept=".png,.jpg,.jpeg,.webp" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="saving" @click="saveProfile">{{ t('common.save') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card style="margin-top: 16px">
      <template #header>{{ t('userInfo.changePwd') }}</template>
      <el-form :model="pwd" label-width="110px" style="max-width: 520px">
        <el-form-item :label="t('userInfo.oldPwd')">
          <el-input v-model="pwd.oldPwd" type="password" show-password />
        </el-form-item>
        <el-form-item :label="t('userInfo.newPwd')">
          <el-input v-model="pwd.newPwd" type="password" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="warning" :loading="pwdSaving" @click="savePwd">{{ t('userInfo.changePwd') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import http from '../../api/http'
import FileUploadField from '../../components/FileUploadField.vue'
import { useUserStore } from '../../stores/user'

const { t } = useI18n()
const userStore = useUserStore()
const saving = ref(false)
const pwdSaving = ref(false)

const form = reactive({
  userName: '',
  userTrueName: '',
  phoneNo: '',
  email: '',
  gender: 2 as number,
  headImageUrl: '',
})

const pwd = reactive({ oldPwd: '', newPwd: '' })

async function load() {
  const res = await http.get('/api/Sys_User/getCurrentProfile')
  if (res.status && res.data) {
    const d = res.data as Record<string, unknown>
    form.userName = String(d.userName ?? '')
    form.userTrueName = String(d.userTrueName ?? '')
    form.phoneNo = String(d.phoneNo ?? '')
    form.email = String(d.email ?? '')
    form.gender = Number(d.gender ?? 2)
    form.headImageUrl = String(d.headImageUrl ?? '')
  }
}

async function saveProfile() {
  saving.value = true
  try {
    const res = await http.post('/api/Sys_User/updateCurrentProfile', {
      userTrueName: form.userTrueName,
      phoneNo: form.phoneNo,
      email: form.email,
      gender: form.gender,
      headImageUrl: form.headImageUrl,
    })
    if (res.status) {
      ElMessage.success(t('common.success'))
      userStore.setUserInfo({
        userId: Number(userStore.userId ?? 0),
        userName: userStore.userName,
        userTrueName: form.userTrueName,
        roleId: Number(userStore.roleId ?? 0),
        permissions: userStore.permissions,
      })
    } else {
      ElMessage.error(res.message || t('common.operationFailed'))
    }
  } finally {
    saving.value = false
  }
}

async function savePwd() {
  if (!pwd.oldPwd || !pwd.newPwd) {
    ElMessage.warning(t('userInfo.pwdRequired'))
    return
  }
  pwdSaving.value = true
  try {
    const res = await http.post('/api/Sys_User/modifyMyPwd', { oldPwd: pwd.oldPwd, newPwd: pwd.newPwd })
    if (res.status) {
      ElMessage.success(t('common.success'))
      pwd.oldPwd = ''
      pwd.newPwd = ''
    } else {
      ElMessage.error(res.message || t('common.operationFailed'))
    }
  } finally {
    pwdSaving.value = false
  }
}

onMounted(load)
</script>
