import { createI18n } from 'vue-i18n'
import zhCN from './lang/zh-CN.json'
import enUS from './lang/en-US.json'
import jaJP from './lang/ja-JP.json'

/** 支持的语言列表（新增语言时在此注册） */
export const SUPPORT_LOCALES = [
  { code: 'zh-CN', elName: 'zh-cn', nameKey: 'locale.zh-CN' },
  { code: 'en-US', elName: 'en', nameKey: 'locale.en-US' },
  { code: 'ja-JP', elName: 'ja', nameKey: 'locale.ja-JP' },
] as const

export type AppLocale = (typeof SUPPORT_LOCALES)[number]['code']

const STORAGE_KEY = 'locale'

/** 读取持久化语言（与 Pinia persist 一致） */
export function getStoredLocale(): AppLocale {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return 'zh-CN'
    const parsed = JSON.parse(raw) as { locale?: string }
    if (SUPPORT_LOCALES.some((l) => l.code === parsed.locale)) {
      return parsed.locale as AppLocale
    }
  } catch {
    /* ignore */
  }
  return 'zh-CN'
}

const i18n = createI18n({
  legacy: false,
  globalInjection: true,
  locale: getStoredLocale(),
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
    'ja-JP': jaJP,
  },
})

export default i18n
