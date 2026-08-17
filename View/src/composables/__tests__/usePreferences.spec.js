import { beforeEach, describe, expect, it } from 'vitest'
import { usePreferences } from '../usePreferences'

beforeEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
  document.documentElement.removeAttribute('data-density')
})

describe('usePreferences', () => {
  it('defaults to light and comfortable theme/density', () => {
    const { theme, density, isDark } = usePreferences()
    expect(theme.value).toBe('light')
    expect(density.value).toBe('comfortable')
    expect(isDark.value).toBe(false)
  })

  it('setTheme persists and applies the dark theme', () => {
    const { setTheme, theme, isDark } = usePreferences()
    setTheme('dark')
    expect(theme.value).toBe('dark')
    expect(isDark.value).toBe(true)
    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(localStorage.getItem('vshield-theme')).toBe('dark')
  })

  it('setTheme normalizes unknown values back to light', () => {
    const { setTheme, theme } = usePreferences()
    setTheme('neon')
    expect(theme.value).toBe('light')
    expect(localStorage.getItem('vshield-theme')).toBe('light')
  })

  it('toggleTheme flips between light and dark', () => {
    const { toggleTheme, theme } = usePreferences()
    toggleTheme()
    expect(theme.value).toBe('dark')
    toggleTheme()
    expect(theme.value).toBe('light')
  })

  it('setDensity persists compact density', () => {
    const { setDensity, density } = usePreferences()
    setDensity('compact')
    expect(density.value).toBe('compact')
    expect(document.documentElement.dataset.density).toBe('compact')
    expect(localStorage.getItem('vshield-density')).toBe('compact')
  })

  it('setDensity normalizes unknown values back to comfortable', () => {
    const { setDensity, density } = usePreferences()
    setDensity('huge')
    expect(density.value).toBe('comfortable')
  })
})
