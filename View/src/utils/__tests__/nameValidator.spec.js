import { describe, expect, it } from 'vitest'
import { normalizeVietnameseName, validateVietnameseName } from '../nameValidator'

describe('normalizeVietnameseName', () => {
  it('trims, collapses spaces and capitalizes each word', () => {
    expect(normalizeVietnameseName('  nguyễn   văn   an ')).toBe('Nguyễn Văn An')
    expect(normalizeVietnameseName('')).toBe('')
    expect(normalizeVietnameseName(null)).toBe('')
  })
})

describe('validateVietnameseName', () => {
  it('accepts a valid multi-word Vietnamese name', () => {
    const result = validateVietnameseName('Nguyễn Văn An')
    expect(result.isValid).toBe(true)
    expect(result.error).toBe('')
    expect(result.normalizedName).toBe('Nguyễn Văn An')
  })

  it('rejects empty input', () => {
    const result = validateVietnameseName('')
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Vui lòng nhập họ và tên')
  })

  it('rejects names shorter than 4 characters', () => {
    const result = validateVietnameseName('An')
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Họ tên quá ngắn (tối thiểu 4 ký tự)')
  })

  it('rejects names longer than 50 characters', () => {
    const result = validateVietnameseName(`${'Nguyễn Văn '.repeat(6)}An`)
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Họ tên quá dài (tối đa 50 ký tự)')
  })

  it('rejects digits and special characters', () => {
    const result = validateVietnameseName('Nguyễn Văn 123')
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Họ tên không được chứa số hoặc ký tự đặc biệt')
  })

  it('rejects single-word names', () => {
    const result = validateVietnameseName('Nguyễn')
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Vui lòng nhập đầy đủ họ và tên (ít nhất 2 từ)')
  })

  it('rejects words shorter than two characters', () => {
    const result = validateVietnameseName('Nguyễn A')
    expect(result.isValid).toBe(false)
    expect(result.error).toBe('Mỗi từ trong tên phải có ít nhất 2 ký tự')
  })

  it('rejects otherwise malformed names', () => {
    const result = validateVietnameseName('Nguyen_Van_An')
    expect(result.isValid).toBe(false)
  })
})
