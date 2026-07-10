import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import i18n, { type AppLocale, SUPPORT_LOCALES } from '../locales'
import { applyHtmlLang, getElementPlusLocale } from '../locales/element-plus'

/** 语言 Store：切换 vue-i18n 与 Element Plus 语言包 */
export const useLocaleStore = defineStore('locale', () => {
  const locale = ref<AppLocale>(i18n.global.locale.value as AppLocale)

  const elementLocale = computed(() => getElementPlusLocale(locale.value))

  const localeLabel = computed(() => i18n.global.t(`locale.${locale.value}`))

  function setLocale(next: AppLocale) {
    locale.value = next
    i18n.global.locale.value = next
    applyHtmlLang(next)
  }

  function init() {
    applyHtmlLang(locale.value)
  }

  return { locale, elementLocale, localeLabel, setLocale, init, SUPPORT_LOCALES }
}, { persist: true })
