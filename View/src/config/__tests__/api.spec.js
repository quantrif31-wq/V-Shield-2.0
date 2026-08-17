import { describe, expect, it } from 'vitest'
import { API_BASE_URL, API_ORIGIN, PLATE_API_BASE_URL, PLATE_API_ORIGIN } from '../api'

describe('config/api', () => {
  it('derives API urls from the fallback ports', () => {
    expect(API_BASE_URL.endsWith(':5107/api')).toBe(true)
    expect(API_ORIGIN.endsWith(':5107')).toBe(true)
    expect(API_ORIGIN.endsWith('/api')).toBe(false)
    expect(PLATE_API_BASE_URL.endsWith(':5002/api')).toBe(true)
    expect(PLATE_API_ORIGIN.endsWith(':5002')).toBe(true)
    expect(PLATE_API_ORIGIN.endsWith('/api')).toBe(false)
  })
})
