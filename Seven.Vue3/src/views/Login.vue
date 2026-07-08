<template>
  <div class="login-page">
    <el-card class="login-card">
      <h2>Seven.Master</h2>
      <p class="subtitle">企业级后台管理系统</p>
      <el-form :model="form" @submit.prevent="handleLogin">
        <el-form-item>
          <el-input v-model="form.userName" placeholder="用户名" prefix-icon="User" />
        </el-form-item>
        <el-form-item>
          <el-input v-model="form.password" type="password" placeholder="密码" prefix-icon="Lock" show-password />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="loading" style="width:100%">登录</el-button>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { login } from '../api/http'
import { useUserStore } from '../stores/user'

const router = useRouter()
const userStore = useUserStore()
const loading = ref(false)
const form = reactive({ userName: 'admin', password: '123456' })

/** 登录处理 */
async function handleLogin() {
  loading.value = true
  try {
    const res = await login(form.userName, form.password)
    if (res.status && res.data) {
      const data = res.data as {
        token: string; refreshToken: string; userId: number; userName: string;
        userTrueName: string; roleId: number; permissions: string[]
      }
      userStore.setToken(data.token, data.refreshToken)
      userStore.setUserInfo({
        userId: data.userId,
        userName: data.userName,
        userTrueName: data.userTrueName,
        roleId: data.roleId,
        permissions: data.permissions || []
      })
      ElMessage.success('登录成功')
      router.push('/home')
    } else {
      ElMessage.error(res.message || '登录失败')
    }
  } catch {
    ElMessage.error('登录请求失败')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page { display: flex; justify-content: center; align-items: center; height: 100vh; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
.login-card { width: 400px; padding: 20px; }
h2 { text-align: center; margin: 0; }
.subtitle { text-align: center; color: #999; margin-bottom: 24px; }
</style>
