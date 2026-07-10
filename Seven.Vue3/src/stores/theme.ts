import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { applyTheme, type ThemeMode } from '../utils/applyTheme'
import i18n from '../locales'

/** 主题模式 Store：浅色 / 深色 / 跟随系统 */
export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>('system')

  const isDark = computed(() => {
    if (mode.value === 'dark') return true
    if (mode.value === 'light') return false
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  })

  const modeLabel = computed(() => {
    const { t } = i18n.global
    const map: Record<ThemeMode, string> = {
      light: t('theme.light'),
      dark: t('theme.dark'),
      system: t('theme.system'),
    }
    return map[mode.value]
  })

  function setMode(next: ThemeMode) {
    mode.value = next
    applyTheme(next)
  }

  /** 应用当前模式并监听系统主题变化 */
  function init() {
    applyTheme(mode.value)

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (mode.value === 'system') applyTheme('system')
    })
  }

  return { mode, isDark, modeLabel, setMode, init }
}, { persist: true })
