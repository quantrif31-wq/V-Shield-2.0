import { beforeEach, describe, expect, it } from 'vitest'
import { clearAllUserViewPrefs, clearViewPrefs, loadViewPrefs, saveViewPrefs } from '../viewPrefs'

beforeEach(() => {
  localStorage.clear()
  sessionStorage.clear()
  sessionStorage.setItem('v_shield_user', JSON.stringify({ userId: 7 }))
})

describe('viewPrefs', () => {
  it('saves and loads prefs for the current user', () => {
    saveViewPrefs('cameras', { gateId: 3, cameraId: 9 })
    expect(loadViewPrefs('cameras')).toEqual({ gateId: 3, cameraId: 9 })
  })

  it('returns null when nothing is stored', () => {
    expect(loadViewPrefs('nope')).toBeNull()
  })

  it('ignores invalid stored payloads', () => {
    localStorage.setItem('vshield.viewPrefs.v1.7.cameras', JSON.stringify('just-a-string'))
    expect(loadViewPrefs('cameras')).toBeNull()
    localStorage.setItem('vshield.viewPrefs.v1.7.cameras', '{not valid json')
    expect(loadViewPrefs('cameras')).toBeNull()
  })

  it('ignores non-object save payloads', () => {
    saveViewPrefs('cameras', 'oops')
    expect(loadViewPrefs('cameras')).toBeNull()
    saveViewPrefs('cameras', null)
    expect(loadViewPrefs('cameras')).toBeNull()
  })

  it('scopes keys per user', () => {
    saveViewPrefs('cameras', { gateId: 1 })
    sessionStorage.setItem('v_shield_user', JSON.stringify({ username: 'other' }))
    expect(loadViewPrefs('cameras')).toBeNull()
  })

  it('clears a single view key', () => {
    saveViewPrefs('cameras', { gateId: 1 })
    saveViewPrefs('gates', { gateId: 2 })
    clearViewPrefs('cameras')
    expect(loadViewPrefs('cameras')).toBeNull()
    expect(loadViewPrefs('gates')).toEqual({ gateId: 2 })
  })

  it('clears all prefs of the current user only', () => {
    saveViewPrefs('cameras', { a: 1 })
    saveViewPrefs('gates', { b: 2 })
    sessionStorage.setItem('v_shield_user', JSON.stringify({ username: 'other' }))
    saveViewPrefs('cameras', { c: 3 })
    sessionStorage.setItem('v_shield_user', JSON.stringify({ userId: 7 }))
    clearAllUserViewPrefs()
    expect(loadViewPrefs('cameras')).toBeNull()
    expect(loadViewPrefs('gates')).toBeNull()
    sessionStorage.setItem('v_shield_user', JSON.stringify({ username: 'other' }))
    expect(loadViewPrefs('cameras')).toEqual({ c: 3 })
  })

  it('falls back to anonymous when no user is stored', () => {
    sessionStorage.removeItem('v_shield_user')
    saveViewPrefs('cameras', { x: 1 })
    expect(loadViewPrefs('cameras')).toEqual({ x: 1 })
  })

  it('tolerates corrupt user payloads', () => {
    sessionStorage.setItem('v_shield_user', 'not-json')
    saveViewPrefs('cameras', { x: 1 })
    expect(loadViewPrefs('cameras')).toEqual({ x: 1 })
  })
})
