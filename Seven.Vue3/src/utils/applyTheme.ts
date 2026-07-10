export type ThemeMode = 'light' | 'dark' | 'system'

/** 读取 Pinia 持久化的主题模式（首屏防闪烁） */
export function getStoredThemeMode(): ThemeMode {
  try {
    const raw = localStorage.getItem('theme')
    if (!raw) return 'system'
    const parsed = JSON.parse(raw) as { mode?: ThemeMode }
    if (parsed.mode === 'light' || parsed.mode === 'dark' || parsed.mode === 'system') {
      return parsed.mode
    }
  } catch {
    /* ignore */
  }
  return 'system'
}

function resolveDark(mode: ThemeMode): boolean {
  if (mode === 'dark') return true
  if (mode === 'light') return false
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

/** 将主题应用到 documentElement（Element Plus 使用 .dark 类） */
export function applyTheme(mode: ThemeMode) {
  const isDark = resolveDark(mode)
  const root = document.documentElement
  root.classList.toggle('dark', isDark)
  root.dataset.sevenTheme = isDark ? 'dark' : 'light'
  root.style.colorScheme = isDark ? 'dark' : 'light'
}

/** Vue 挂载前调用，避免主题闪烁 */
export function applyThemeBeforeMount() {
  applyTheme(getStoredThemeMode())
}
