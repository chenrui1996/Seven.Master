<template>
  <div class="login-page">
    <div class="login-toolbar">
      <LocaleSwitch />
      <ThemeToggle />
    </div>
    <div class="login-bg" aria-hidden="true">
      <div class="login-bg__mesh" />
      <div class="login-bg__glow login-bg__glow--a" />
      <div class="login-bg__glow login-bg__glow--b" />
    </div>

    <div class="login-shell">
      <section class="login-brand">
        <div class="brand-badge">
          <img src="../assets/icons/logo-master.svg" alt="" class="brand-logo" />
        </div>
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
          <div class="seven-section-title">
            <span class="seven-section-title__bar" aria-hidden="true" />
            <h2>{{ t('login.panelTitle') }}</h2>
          </div>
          <p>{{ t('login.panelSubtitle') }}</p>
        </div>
        <el-form :model="form" class="login-form" label-position="top" @submit.prevent="handleLogin">
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
  background: var(--seven-bg-page);
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
  background:
    radial-gradient(circle at 82% 12%, rgba(64, 158, 255, 0.2) 0%, transparent 24%),
    radial-gradient(circle at 12% 86%, rgba(64, 158, 255, 0.1) 0%, transparent 26%),
    linear-gradient(135deg, #eef7ff 0%, var(--seven-bg-page) 48%, #e9f2fb 100%);
}

html.dark .login-bg {
  background:
    radial-gradient(circle at 82% 12%, rgba(64, 158, 255, 0.2) 0%, transparent 24%),
    radial-gradient(circle at 12% 86%, rgba(64, 158, 255, 0.12) 0%, transparent 26%),
    linear-gradient(135deg, #0e1a29 0%, var(--seven-bg-page) 48%, #111d2b 100%);
}

.login-bg__mesh {
  position: absolute;
  inset: 0;
  opacity: 0.36;
  background-image:
    linear-gradient(rgba(64, 158, 255, 0.1) 1px, transparent 1px),
    linear-gradient(90deg, rgba(64, 158, 255, 0.1) 1px, transparent 1px),
    linear-gradient(135deg, transparent 48%, rgba(64, 158, 255, 0.08) 49%, transparent 50%);
  background-size: 36px 36px, 36px 36px, 180px 180px;
}

.login-bg::before {
  content: '';
  position: absolute;
  width: 68vw;
  height: 68vw;
  min-width: 520px;
  min-height: 520px;
  max-width: 980px;
  max-height: 980px;
  top: 50%;
  left: 50%;
  border: 1px solid rgba(64, 158, 255, 0.14);
  border-radius: 50%;
  transform: translate(-50%, -50%);
  box-shadow:
    0 0 0 28px rgba(64, 158, 255, 0.04),
    0 0 0 56px rgba(64, 158, 255, 0.025);
  pointer-events: none;
}

.login-bg__glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(72px);
  pointer-events: none;
}

.login-bg__glow--a {
  width: 380px;
  height: 380px;
  top: -100px;
  right: 12%;
  background: rgba(64, 158, 255, 0.18);
}

.login-bg__glow--b {
  width: 300px;
  height: 300px;
  bottom: -80px;
  left: 10%;
  background: rgba(51, 126, 204, 0.12);
}

.login-shell {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1.05fr 420px;
  max-width: 920px;
  width: 100%;
  background: var(--seven-bg-panel);
  border: 1px solid var(--seven-border-light);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 12px 40px rgba(64, 158, 255, 0.1), var(--seven-shadow);
}

.login-brand {
  padding: 44px 40px;
  border-right: 1px solid var(--seven-border-light);
  background:
    linear-gradient(165deg, #1d1e2c 0%, #24263a 55%, #1a2740 100%);
  color: #e8eef7;
}

.brand-badge {
  width: 52px;
  height: 52px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 20px;
  background: rgba(64, 158, 255, 0.16);
  border: 1px solid rgba(64, 158, 255, 0.35);
}

.brand-logo {
  width: 30px;
  height: 30px;
}

.login-brand h1 {
  margin: 0 0 8px;
  font-family: var(--seven-font-body);
  font-size: 24px;
  font-weight: 700;
  color: #f8fafc;
  letter-spacing: 0.01em;
}

.brand-tagline {
  margin: 0 0 28px;
  font-size: 14px;
  line-height: 1.55;
  color: #a8b4c8;
}

.feature-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.feature-list li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: #d0d7e4;
  padding: 8px 10px;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.feature-list .el-icon {
  color: var(--seven-accent);
  font-size: 18px;
}

.brand-footer {
  display: flex;
  align-items: center;
  margin-top: 36px;
  font-size: 12px;
  color: #8b97ab;
}

.login-panel {
  padding: 40px 36px;
  background: var(--seven-bg-panel);
  transition: background-color 0.2s ease;
}

.panel-header {
  margin-bottom: 8px;
}

.panel-header .seven-section-title {
  margin-bottom: 6px;
}

.panel-header h2 {
  margin: 0;
  font-family: var(--seven-font-body);
  font-size: 20px;
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.panel-header p {
  margin: 0 0 22px;
  font-size: 13px;
  color: var(--seven-text-muted);
}

.login-form :deep(.el-form-item) {
  margin-bottom: 16px;
}

.login-form :deep(.el-form-item__label) {
  font-size: 13px;
  font-weight: 500;
  color: var(--seven-text);
  margin-bottom: 4px;
}

.login-form :deep(.el-input__wrapper) {
  min-height: 40px;
  box-shadow: 0 0 0 1px var(--seven-border) inset;
}

.login-form :deep(.el-input__wrapper:hover),
.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 1px var(--seven-accent) inset;
}

.login-btn {
  width: 100%;
  margin-top: 4px;
  height: 40px !important;
  min-height: 40px !important;
  font-weight: 600;
  letter-spacing: 0.04em;
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
  border: 1px solid var(--seven-border);
  border-radius: var(--seven-radius-sm);
  background: var(--seven-bg-subtle);
  cursor: pointer;
  user-select: none;
  transition: border-color 0.18s ease, background-color 0.18s ease;
}

.captcha-display:hover {
  border-color: var(--seven-accent);
  background: var(--seven-accent-soft);
}

.captcha-display:focus-visible {
  outline: 2px solid var(--seven-accent);
  outline-offset: 1px;
}

.captcha-display:disabled {
  opacity: 0.6;
  cursor: wait;
}

.captcha-code {
  font-family: var(--seven-font-mono);
  font-size: 20px;
  font-weight: 700;
  letter-spacing: 0.28em;
  color: var(--seven-accent);
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
    padding: 32px 24px;
  }
}
</style>
