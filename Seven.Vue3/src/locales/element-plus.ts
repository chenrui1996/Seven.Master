import type { AppLocale } from './index'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import en from 'element-plus/es/locale/lang/en'
import ja from 'element-plus/es/locale/lang/ja'

const elementLocaleMap: Record<AppLocale, typeof zhCn> = {
  'zh-CN': zhCn,
  'en-US': en,
  'ja-JP': ja,
}

/** 获取 Element Plus 与当前应用语言匹配的 locale 包 */
export function getElementPlusLocale(locale: AppLocale) {
  return elementLocaleMap[locale] ?? zhCn
}

/** 同步 html lang 属性（SEO 与无障碍） */
export function applyHtmlLang(locale: AppLocale) {
  const map: Record<AppLocale, string> = {
    'zh-CN': 'zh-CN',
    'en-US': 'en',
    'ja-JP': 'ja',
  }
  document.documentElement.lang = map[locale] ?? 'zh-CN'
}
