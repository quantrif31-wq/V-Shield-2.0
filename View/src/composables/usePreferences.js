import { computed, ref } from 'vue'

const THEME_KEY = 'vshield-theme'
const DENSITY_KEY = 'vshield-density'

const theme = ref(document.documentElement.dataset.theme || 'light')
const density = ref(document.documentElement.dataset.density || 'comfortable')

function applyPreference(name, value) {
  document.documentElement.dataset[name] = value
}

export function usePreferences() {
  function setTheme(value) {
    const next = value === 'dark' ? 'dark' : 'light'
    theme.value = next
    applyPreference('theme', next)
    localStorage.setItem(THEME_KEY, next)
  }

  function toggleTheme() {
    setTheme(theme.value === 'dark' ? 'light' : 'dark')
  }

  function setDensity(value) {
    const next = value === 'compact' ? 'compact' : 'comfortable'
    density.value = next
    applyPreference('density', next)
    localStorage.setItem(DENSITY_KEY, next)
  }

  return {
    theme: computed(() => theme.value),
    density: computed(() => density.value),
    isDark: computed(() => theme.value === 'dark'),
    toggleTheme,
    setTheme,
    setDensity,
  }
}
