<template>
  <div class="login-page">
    <div class="login-toolbar">
      <LocaleSwitch />
      <ThemeToggle />
    </div>
    <div class="login-bg" aria-hidden="true">
      <div class="login-bg__grid" />
      <div class="login-bg__glow login-bg__glow--orange" />
      <div class="login-bg__glow login-bg__glow--blue" />
    </div>

    <div class="login-shell">
      <section class="login-brand">
        <img src="../assets/icons/logo-master.svg" alt="" class="brand-logo" />
        <h1>{{ t('login.title') }}</h1>
        <p class="brand-tagline">{{ t('login.tagline') }}</p>
        <ul class="feature-list">
          <li><el-icon><Box /></el-icon>{{ t('login.featureCore') }}</li>
          <li><el-icon><Operation /></el-icon>{{ t('login.featureAuth') }}</li>
          <li><el-icon><Monitor /></el-icon>{{ t('login.featureRealtime') }}</li>
        </ul>
        <div class="brand-footer">
          <span class="seven-status-dot" />
          <span>{{ t('login.footer') }}</span>
        </div>
      </section>

      <section class="login-panel">
        <div class="panel-header">
          <h2>{{ t('login.panelTitle') }}</h2>
          <p>{{ t('login.panelSubtitle') }}</p>
        </div>
        <el-form :model="form" class="login-form" @submit.prevent="handleLogin">
          <el-form-item :label="t('login.userName')">
            <el-input
              v-model="form.userName"
              :placeholder="t('login.userNamePlaceholder')"
              size="large"
              :prefix-icon="User"
              autocomplete="username"
            />
          </el-form-item>
          <el-form-item :label="t('login.password')">
            <el-input
              v-model="form.password"
              type="password"
              :placeholder="t('login.passwordPlaceholder')"
              size="large"
              :prefix-icon="Lock"
              show-password
              autocomplete="current-password"
            />
          </el-form-item>
          <el-form-item v-if="featureStore.captchaEnabled" :label="t('login.captcha')">
            <div class="captcha-row">
              <el-input
                v-model="form.verificationCode"
                :placeholder="t('login.captchaPlaceholder')"
                size="large"
                maxlength="6"
                autocomplete="off"
                @keyup.enter="handleLogin"
              />
              <button
                type="button"
                class="captcha-display"
                :title="t('login.captchaRefresh')"
                :disabled="captchaLoading"
                @click="loadCaptcha"
              >
                <span v-if="captchaLoading">…</span>
                <span v-else class="captcha-code">{{ captchaDisplay }}</span>
              </button>
            </div>
          </el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            size="large"
            class="login-btn"
            :icon="ActionIcons.login"
            :loading="loading"
          >
            {{ t('login.submit') }}
          </el-button>
        </el-form>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Box, Lock, Monitor, Operation, User } from '@element-plus/icons-vue'
import { createCaptcha, login } from '../api/http'
import { useUserStore } from '../stores/user'
import { useFeatureStore } from '../stores/features'
import { ActionIcons } from '../constants/actionIcons'
import ThemeToggle from '../components/ThemeToggle.vue'
import LocaleSwitch from '../components/LocaleSwitch.vue'

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()
const featureStore = useFeatureStore()
const loading = ref(false)
const captchaLoading = ref(false)
const captchaDisplay = ref('----')
const form = reactive({
  userName: 'admin',
  password: '123456',
  verificationCode: '',
  uuid: '',
})

async function loadCaptcha() {
  if (!featureStore.captchaEnabled) return
  captchaLoading.value = true
  try {
    const res = await createCaptcha()
    if (res.status && res.data) {
      form.uuid = res.data.key
      captchaDisplay.value = res.data.code
      form.verificationCode = ''
    }
  } catch {
    captchaDisplay.value = '????'
  } finally {
    captchaLoading.value = false
  }
}

onMounted(async () => {
  if (!featureStore.loaded) await featureStore.load()
  void loadCaptcha()
})

async function handleLogin() {
  loading.value = true
  try {
    const res = await login(
      form.userName,
      form.password,
      form.verificationCode || undefined,
      form.uuid || undefined
    )
    if (res.status && res.data) {
      const data = res.data as {
        token: string; refreshToken: string; userId: number; userName: string
        userTrueName: string; roleId: number; permissions: string[]
      }
      userStore.setToken(data.token, data.refreshToken)
      userStore.setUserInfo({
        userId: data.userId,
        userName: data.userName,
        userTrueName: data.userTrueName,
        roleId: data.roleId,
        permissions: data.permissions || [],
      })
      ElMessage.success(t('login.success'))
      router.push('/home')
    } else {
      ElMessage.error(res.message || t('login.failed'))
      await loadCaptcha()
    }
  } catch {
    ElMessage.error(t('login.requestFailed'))
    await loadCaptcha()
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--seven-space-6);
  overflow: hidden;
}

.login-toolbar {
  position: absolute;
  top: var(--seven-space-4);
  right: var(--seven-space-4);
  z-index: 2;
  display: flex;
  align-items: center;
  gap: var(--seven-space-2);
}

.login-bg {
  position: absolute;
  inset: 0;
  background: #0b1220;
}

.login-bg__grid {
  position: absolute;
  inset: 0;
  background-image: url('../assets/patterns/industrial-grid.svg');
  background-size: cover;
  background-position: center;
  opacity: 0.9;
}

.login-bg__glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  pointer-events: none;
}

.login-bg__glow--orange {
  width: 420px;
  height: 420px;
  top: -80px;
  right: 10%;
  background: rgba(249, 115, 22, 0.18);
}

.login-bg__glow--blue {
  width: 320px;
  height: 320px;
  bottom: -60px;
  left: 8%;
  background: rgba(14, 165, 233, 0.12);
}

.login-shell {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1fr 400px;
  max-width: 960px;
  width: 100%;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 24px 48px rgba(0, 0, 0, 0.4);
}

.login-brand {
  padding: 48px 40px;
  border-right: 1px solid rgba(148, 163, 184, 0.15);
  color: #e2e8f0;
}

.brand-logo {
  width: 48px;
  height: 48px;
  margin-bottom: 20px;
}

.login-brand h1 {
  margin: 0 0 8px;
  font-family: var(--seven-font-mono);
  font-size: 26px;
  font-weight: 700;
  color: #f8fafc;
  letter-spacing: 0.02em;
}

.brand-tagline {
  margin: 0 0 28px;
  font-size: 14px;
  color: #94a3b8;
}

.feature-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.feature-list li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: #cbd5e1;
}

.feature-list .el-icon {
  color: var(--seven-accent);
  font-size: 18px;
}

.brand-footer {
  display: flex;
  align-items: center;
  margin-top: 40px;
  font-size: 12px;
  color: #64748b;
  font-family: var(--seven-font-mono);
}

.login-panel {
  padding: var(--seven-space-8) var(--seven-space-6);
  background: var(--seven-bg-panel);
  transition: background-color 0.2s ease;
}

.panel-header h2 {
  margin: 0 0 6px;
  font-family: var(--seven-font-mono);
  font-size: 22px;
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.panel-header p {
  margin: 0 0 28px;
  font-size: 13px;
  color: var(--seven-text-muted);
}

.login-form :deep(.el-form-item__label) {
  font-size: 13px;
  font-weight: 500;
  color: var(--seven-text);
}

.login-btn {
  width: 100%;
  margin-top: 8px;
  font-weight: 600;
  letter-spacing: 0.04em;
  color: var(--seven-btn-on-solid);
}

.login-btn:not(.is-link):not(.is-text) {
  box-shadow: var(--seven-btn-primary-shadow);
}

.login-btn:not(.is-link):not(.is-text):hover {
  box-shadow: var(--seven-btn-primary-hover-shadow);
}

.captcha-row {
  display: flex;
  gap: var(--seven-space-2);
  width: 100%;
}

.captcha-row :deep(.el-input) {
  flex: 1;
}

.captcha-display {
  flex-shrink: 0;
  width: 112px;
  height: 40px;
  padding: 0;
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-base);
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  cursor: pointer;
  user-select: none;
}

.captcha-display:disabled {
  opacity: 0.6;
  cursor: wait;
}

.captcha-code {
  font-family: var(--seven-font-mono);
  font-size: 22px;
  font-weight: 700;
  letter-spacing: 0.35em;
  color: #f97316;
  text-shadow: 0 0 8px rgba(249, 115, 22, 0.4);
}

@media (max-width: 768px) {
  .login-shell {
    grid-template-columns: 1fr;
    max-width: 420px;
  }

  .login-brand {
    display: none;
  }

  .login-panel {
    padding: 36px 28px;
  }
}
</style>
