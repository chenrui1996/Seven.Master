<template>
  <el-dropdown trigger="click" @command="onCommand">
    <button
      type="button"
      class="locale-switch cursor-pointer"
      :aria-label="`${t('locale.label')}: ${localeStore.localeLabel}`"
      :title="`${t('locale.label')}: ${localeStore.localeLabel}`"
    >
      <el-icon :size="18"><Place /></el-icon>
      <span class="locale-code">{{ localeStore.locale }}</span>
    </button>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item
          v-for="item in localeStore.SUPPORT_LOCALES"
          :key="item.code"
          :command="item.code"
          :class="{ 'is-active': localeStore.locale === item.code }"
        >
          {{ t(item.nameKey) }}
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup lang="ts">
import { Place } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import type { AppLocale } from '../locales'
import { useLocaleStore } from '../stores/locale'

const { t } = useI18n()
const localeStore = useLocaleStore()

function onCommand(code: string) {
  localeStore.setLocale(code as AppLocale)
}
</script>

<style scoped>
.locale-switch {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  height: 36px;
  padding: 0 10px;
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
  background: var(--seven-bg-panel);
  color: var(--seven-text);
  font-family: var(--seven-font-mono);
  font-size: var(--seven-text-xs);
  transition: border-color 0.2s ease, background-color 0.2s ease, color 0.2s ease;
}

.locale-switch:hover {
  border-color: var(--seven-accent);
  color: var(--seven-accent);
  background: var(--seven-accent-soft);
}

.locale-switch:focus-visible {
  outline: 2px solid var(--seven-accent);
  outline-offset: 2px;
}

.locale-code {
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

:deep(.el-dropdown-menu__item.is-active) {
  color: var(--seven-accent);
  font-weight: 600;
}
</style>
