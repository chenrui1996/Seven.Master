<template>
  <el-dropdown trigger="click" @command="onCommand">
    <button
      type="button"
      class="theme-toggle cursor-pointer"
      :aria-label="`${t('theme.label')}: ${themeStore.modeLabel}`"
      :title="`${t('theme.label')}: ${themeStore.modeLabel}`"
    >
      <el-icon :size="18">
        <Sunny v-if="themeStore.isDark" />
        <Moon v-else />
      </el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item command="light" :class="{ 'is-active': themeStore.mode === 'light' }">
          <el-icon><Sunny /></el-icon>{{ t('theme.light') }}
        </el-dropdown-item>
        <el-dropdown-item command="dark" :class="{ 'is-active': themeStore.mode === 'dark' }">
          <el-icon><Moon /></el-icon>{{ t('theme.dark') }}
        </el-dropdown-item>
        <el-dropdown-item command="system" :class="{ 'is-active': themeStore.mode === 'system' }">
          <el-icon><Monitor /></el-icon>{{ t('theme.system') }}
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup lang="ts">
import { Monitor, Moon, Sunny } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { useThemeStore } from '../stores/theme'
import type { ThemeMode } from '../utils/applyTheme'

const { t } = useI18n()
const themeStore = useThemeStore()

function onCommand(cmd: string) {
  themeStore.setMode(cmd as ThemeMode)
}
</script>

<style scoped>
.theme-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
  background: var(--seven-bg-panel);
  color: var(--seven-text);
  transition: border-color 0.2s ease, background-color 0.2s ease, color 0.2s ease;
}

.theme-toggle:hover {
  border-color: var(--seven-accent);
  color: var(--seven-accent);
  background: var(--seven-accent-soft);
}

.theme-toggle:focus-visible {
  outline: 2px solid var(--seven-accent);
  outline-offset: 2px;
}

:deep(.el-dropdown-menu__item.is-active) {
  color: var(--seven-accent);
  font-weight: 600;
}

:deep(.el-dropdown-menu__item) {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
