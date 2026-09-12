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
      <section class="login-brand" aria-labelledby="login-brand-title">
        <div class="brand-badge">
          <img src="../assets/icons/logo-master.svg" alt="" class="brand-logo" />
        </div>
        <h1 id="login-brand-title">{{ t('login.title') }}</h1>
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
          <div class="panel-brand">
            <div class="brand-badge brand-badge--compact" aria-hidden="true">
              <img src="../assets/icons/logo-master.svg" alt="" class="brand-logo" />
            </div>
            <div class="panel-brand__copy">
              <span class="panel-brand__name">{{ t('login.title') }}</span>
              <span class="panel-brand__tagline">{{ t('login.tagline') }}</span>
            </div>
          </div>
          <div class="seven-section-title">
            <span class="seven-section-title__bar" aria-hidden="true" />
            <h2>{{ t('login.panelTitle') }}</h2>
          </div>
          <p>{{ t('login.panelSubtitle') }}</p>
        </div>
        <el-form :model="form" class="login-form" label-position="top" @submit.prevent="handleLogin">
          <div class="login-form__group">
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
                  :aria-label="t('login.captchaRefresh')"
                  :aria-busy="captchaLoading ? 'true' : 'false'"
                  :aria-disabled="captchaLoading ? 'true' : 'false'"
                  :disabled="captchaLoading"
                  @click="loadCaptcha"
                >
                  <span v-if="captchaLoading">...</span>
                  <span v-else class="captcha-code">{{ captchaDisplay }}</span>
                </button>
              </div>
            </el-form-item>
          </div>
          <div class="login-form__actions" :aria-busy="loading ? 'true' : 'false'">
            <el-button
              type="primary"
              native-type="submit"
              size="large"
              class="login-btn"
              :icon="ActionIcons.login"
              :loading="loading"
              :aria-label="t('login.submit')"
              :aria-busy="loading ? 'true' : 'false'"
              :aria-disabled="loading ? 'true' : 'false'"
              aria-live="polite"
            >
              {{ loading ? `${t('login.submit')}...` : t('login.submit') }}
            </el-button>
          </div>
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
    radial-gradient(circle at top right, var(--seven-accent-soft) 0%, transparent 28%),
    radial-gradient(circle at bottom left, var(--seven-status-info-soft) 0%, transparent 24%),
    linear-gradient(145deg, var(--seven-bg-panel) 0%, var(--seven-bg-page) 46%, var(--seven-bg-subtle) 100%);
}

.login-bg__mesh {
  position: absolute;
  inset: 0;
  opacity: 0.22;
  background-image:
    linear-gradient(var(--seven-border-light) 1px, transparent 1px),
    linear-gradient(90deg, var(--seven-border-light) 1px, transparent 1px);
  background-size: 32px 32px;
}

.login-bg__glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(72px);
  pointer-events: none;
}

.login-bg__glow--a {
  width: 340px;
  height: 340px;
  top: -120px;
  right: 12%;
  background: var(--seven-accent-soft);
}

.login-bg__glow--b {
  width: 240px;
  height: 240px;
  bottom: -70px;
  left: 10%;
  background: var(--seven-status-info-soft);
}

.login-shell {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: minmax(0, 1.05fr) minmax(0, 420px);
  max-width: 920px;
  width: 100%;
  background: var(--seven-bg-panel);
  border: 1px solid var(--seven-border-light);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 16px 48px var(--seven-accent-soft), var(--seven-shadow);
}

.login-brand {
  min-width: 0;
  padding: 44px 40px;
  border-right: 1px solid var(--seven-border-light);
  background:
    linear-gradient(165deg, var(--seven-bg-charcoal) 0%, var(--seven-bg-charcoal-hover) 55%, var(--seven-bg-sidebar) 100%);
  color: var(--seven-panel-title-text);
}

.brand-badge {
  width: 52px;
  height: 52px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 20px;
  background: var(--seven-bg-charcoal-active);
  border: 1px solid var(--seven-rail-border);
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
  color: var(--seven-panel-title-text);
  letter-spacing: 0.01em;
}

.brand-tagline {
  margin: 0 0 28px;
  font-size: 14px;
  line-height: 1.55;
  color: var(--seven-rail-text);
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
  color: var(--seven-rail-text-hover);
  padding: 8px 10px;
  border-radius: 6px;
  background: var(--seven-bg-charcoal-active);
  border: 1px solid var(--seven-rail-border);
}

.feature-list .el-icon {
  color: var(--seven-accent);
  font-size: 18px;
}

.brand-footer {
  display: flex;
  align-items: center;
  gap: var(--seven-space-2);
  margin-top: 36px;
  font-size: 12px;
  color: var(--seven-rail-text);
}

.login-panel {
  min-width: 0;
  padding: 40px 36px;
  background: var(--seven-bg-panel);
  transition: background-color 0.2s ease;
}

.panel-header {
  display: flex;
  flex-direction: column;
  gap: var(--seven-space-4);
  margin-bottom: var(--seven-space-6);
}

.panel-brand {
  display: none;
  align-items: center;
  gap: var(--seven-space-4);
  padding-bottom: var(--seven-space-4);
  border-bottom: 1px solid var(--seven-border-light);
}

.brand-badge--compact {
  width: 40px;
  height: 40px;
  margin-bottom: 0;
  flex-shrink: 0;
}

.brand-badge--compact .brand-logo {
  width: 22px;
  height: 22px;
}

.panel-brand__copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.panel-brand__name {
  font-size: 14px;
  font-weight: 700;
  line-height: 1.3;
  color: var(--seven-primary-dark);
}

.panel-brand__tagline {
  font-size: 12px;
  line-height: 1.4;
  color: var(--seven-text-muted);
}

.panel-header .seven-section-title {
  margin-bottom: 0;
}

.panel-header h2 {
  margin: 0;
  font-family: var(--seven-font-body);
  font-size: 20px;
  font-weight: 600;
  color: var(--seven-primary-dark);
}

.panel-header p {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--seven-text-muted);
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: var(--seven-space-5);
}

.login-form__group {
  padding: var(--seven-space-6);
  border: 1px solid var(--seven-border-light);
  border-radius: 8px;
  background: var(--seven-bg-subtle);
}

.login-form :deep(.el-form-item) {
  margin-bottom: 16px;
}

.login-form :deep(.el-form-item:last-child) {
  margin-bottom: 0;
}

.login-form :deep(.el-form-item__label) {
  font-size: 13px;
  font-weight: 500;
  color: var(--seven-text);
  margin-bottom: 4px;
}

.login-form :deep(.el-input__wrapper) {
  min-height: 44px;
  background: var(--seven-bg-panel);
  box-shadow: 0 0 0 1px var(--seven-border) inset;
}

.login-form :deep(.el-input__wrapper:hover),
.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 1px var(--seven-accent) inset, 0 0 0 3px var(--seven-accent-soft);
}

.login-form__actions {
  display: flex;
  flex-direction: column;
  gap: var(--seven-space-2);
}

.login-btn {
  width: 100%;
  margin-top: 4px;
  height: 44px !important;
  min-height: 44px !important;
  font-weight: 600;
  letter-spacing: 0.04em;
}

.captcha-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 112px;
  align-items: stretch;
  gap: var(--seven-space-2);
  width: 100%;
}

.captcha-row :deep(.el-input) {
  min-width: 0;
}

.captcha-display {
  width: 100%;
  min-height: 44px;
  padding: 0;
  border: 1px solid var(--seven-border);
  border-radius: var(--seven-radius-sm);
  background: var(--seven-bg-subtle);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  touch-action: manipulation;
  user-select: none;
  transition: border-color 0.18s ease, background-color 0.18s ease;
}

.captcha-display:hover {
  border-color: var(--seven-accent);
  background: var(--seven-accent-soft);
}

.captcha-display:focus-visible {
  outline: 2px solid var(--seven-focus-ring);
  outline-offset: 1px;
}

.captcha-display:disabled {
  opacity: 0.68;
  cursor: wait;
}

.captcha-code {
  font-family: var(--seven-font-mono);
  font-size: 18px;
  font-weight: 700;
  letter-spacing: 0.24em;
  color: var(--seven-accent);
}

@media (max-width: 768px) {
  .login-page {
    padding: 56px var(--seven-space-4) var(--seven-space-5);
  }

  .login-toolbar {
    top: var(--seven-space-3);
    right: var(--seven-space-3);
  }

  .login-shell {
    grid-template-columns: 1fr;
    max-width: 420px;
  }

  .login-brand {
    display: none;
  }

  .login-panel {
    padding: 28px 22px 24px;
  }

  .panel-brand {
    display: flex;
  }

  .login-form__group {
    padding: var(--seven-space-5);
  }
}

@media (max-width: 480px) {
  .panel-brand__tagline {
    max-width: 24ch;
  }

  .captcha-row {
    grid-template-columns: minmax(0, 1fr);
  }

  .captcha-display {
    min-height: 44px;
  }
}
</style>
