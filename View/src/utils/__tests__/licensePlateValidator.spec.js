import { describe, expect, it } from 'vitest'
import { getVehicleTypeLabel, optimizeAndValidatePlate, validatePlateFormat } from '../licensePlateValidator'

describe('optimizeAndValidatePlate', () => {
  it('returns invalid for empty or non-string input', () => {
    expect(optimizeAndValidatePlate('')).toEqual({ rawInput: '', cleanedPlate: '', isValid: false, type: 'Unknown' })
    expect(optimizeAndValidatePlate(null)).toEqual({ rawInput: '', cleanedPlate: '', isValid: false, type: 'Unknown' })
  })

  it('validates a clean car plate', () => {
    const result = optimizeAndValidatePlate('30A-123.45')
    expect(result.isValid).toBe(true)
    expect(result.type).toBe('Car')
    expect(result.cleanedPlate).toBe('30-A 123.45')
  })

  it('validates a clean motorcycle plate', () => {
    const result = optimizeAndValidatePlate('29-A1 1234')
    expect(result.isValid).toBe(true)
    expect(result.type).toBe('Motorcycle')
  })

  it('corrects common OCR letter-to-number errors', () => {
    const result = optimizeAndValidatePlate('3OA-12B.45')
    expect(result.isValid).toBe(true)
    expect(result.type).toBe('Car')
    expect(result.cleanedPlate).toBe('30-A 128.45')
  })

  it('corrects OCR number-to-letter series errors', () => {
    const result = optimizeAndValidatePlate('30I-123.45')
    expect(result.isValid).toBe(true)
    expect(result.cleanedPlate.startsWith('30-I')).toBe(true)
  })

  it('rejects clearly invalid plates', () => {
    expect(optimizeAndValidatePlate('abc').isValid).toBe(false)
    expect(optimizeAndValidatePlate('1234567890xyz').isValid).toBe(false)
  })

  it('handles separator variations', () => {
    expect(optimizeAndValidatePlate('30A-12345').isValid).toBe(true)
    expect(optimizeAndValidatePlate('30A 12345').isValid).toBe(true)
    expect(optimizeAndValidatePlate(' 30A-12345 ').cleanedPlate).toBe('30-A 12345')
  })
})

describe('validatePlateFormat', () => {
  it('validates already-formatted plates', () => {
    expect(validatePlateFormat('30A 123.45')).toEqual({ isValid: true, type: 'Car' })
    expect(validatePlateFormat('29-A1 1234')).toEqual({ isValid: true, type: 'Motorcycle' })
  })

  it('rejects invalid and non-string input', () => {
    expect(validatePlateFormat('not-a-plate').isValid).toBe(false)
    expect(validatePlateFormat('').isValid).toBe(false)
    expect(validatePlateFormat(null).isValid).toBe(false)
  })
})

describe('getVehicleTypeLabel', () => {
  it('maps known types to Vietnamese labels', () => {
    expect(getVehicleTypeLabel('Car')).toBe('Ô tô')
    expect(getVehicleTypeLabel('Motorcycle')).toBe('Xe máy')
    expect(getVehicleTypeLabel('Unknown')).toBe('Không xác định')
    expect(getVehicleTypeLabel('anything')).toBe('Không xác định')
  })
})
