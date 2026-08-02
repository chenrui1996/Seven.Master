<template>
  <div class="forbidden-page seven-page">
    <el-result icon="warning" :title="t('forbidden.title')" :sub-title="t('forbidden.subtitle')">
      <template #extra>
        <el-button type="primary" @click="goHome">{{ t('common.home') }}</el-button>
      </template>
    </el-result>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

const { t } = useI18n()
const router = useRouter()

function goHome() {
  router.push('/home')
}
</script>

<style scoped>
.forbidden-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 360px;
}
</style>
