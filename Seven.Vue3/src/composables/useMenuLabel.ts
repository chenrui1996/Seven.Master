import { useI18n } from 'vue-i18n'

/** 翻译后端返回的中文菜单名（menu.{name} 键，无则回退原文） */
export function useMenuLabel() {
  const { t, te } = useI18n()

  function menuLabel(name?: string): string {
    if (!name) return ''
    const key = `menu.${name}`
    return te(key) ? t(key) : name
  }

  return { menuLabel }
}
