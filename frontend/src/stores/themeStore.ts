import { defineStore } from 'pinia'

type ThemePreference = 'system' | 'dark' | 'light'
type ResolvedTheme = 'dark' | 'light'

const THEME_STORAGE_KEY = 'ai-daily-theme'

function readStoredTheme(): ThemePreference {
  if (typeof localStorage === 'undefined') return 'system'

  const value = localStorage.getItem(THEME_STORAGE_KEY)
  return value === 'dark' || value === 'light' || value === 'system' ? value : 'system'
}

function resolveTheme(preference: ThemePreference): ResolvedTheme {
  if (preference !== 'system') return preference

  if (typeof matchMedia === 'undefined') return 'dark'
  return matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'
}

export const useThemeStore = defineStore('themeStore', {
  state: () => ({
    preference: readStoredTheme(),
  }),
  getters: {
    resolvedTheme: (state): ResolvedTheme => resolveTheme(state.preference),
  },
  actions: {
    setPreference(preference: ThemePreference): void {
      this.preference = preference
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(THEME_STORAGE_KEY, preference)
      }
    },
    toggleResolvedTheme(): void {
      this.setPreference(this.resolvedTheme === 'dark' ? 'light' : 'dark')
    },
  },
})
