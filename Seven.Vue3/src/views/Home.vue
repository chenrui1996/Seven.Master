<template>
  <div class="home">
    <el-row :gutter="20">
      <el-col :span="8">
        <el-card><el-statistic title="欢迎使用" :value="'Seven.Master'" /></el-card>
      </el-col>
      <el-col :span="8">
        <el-card><el-statistic title="当前用户" :value="userStore.userTrueName" /></el-card>
      </el-col>
      <el-col :span="8">
        <el-card><el-statistic title="权限数量" :value="userStore.permissions.length" /></el-card>
      </el-col>
    </el-row>
    <el-card style="margin-top:20px">
      <template #header>系统说明</template>
      <p>Seven.Master 是从 Legrand.Master 迁移而来的 .NET 8 + Vue 3 企业级后台系统。</p>
      <p>技术栈：EF Core CodeFirst、BCrypt 密码、JWT Refresh Token、延迟双删缓存、SignalR、MinIO。</p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { useUserStore } from '../stores/user'
const userStore = useUserStore()
</script>
