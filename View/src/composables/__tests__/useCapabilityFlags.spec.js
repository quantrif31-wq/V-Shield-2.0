import { beforeEach, describe, expect, it } from 'vitest'
import { authState } from '../../stores/auth'
import { capabilityFlags, useCapabilityFlags } from '../useCapabilityFlags'

beforeEach(() => {
  authState.user = null
})

describe('useCapabilityFlags', () => {
  it('returns the shared capability flags object', () => {
    expect(useCapabilityFlags()).toBe(capabilityFlags)
  })

  it('grants admin the broadest permissions', () => {
    authState.user = { role: 'Admin' }
    expect(capabilityFlags.canOperateLane.value).toBe(true)
    expect(capabilityFlags.canManagePolicy.value).toBe(true)
    expect(capabilityFlags.canManageFoundation.value).toBe(true)
    expect(capabilityFlags.canManageVisitor.value).toBe(true)
    expect(capabilityFlags.canViewReports.value).toBe(true)
    expect(capabilityFlags.canActEmergency.value).toBe(true)
    expect(capabilityFlags.canExportEvidence.value).toBe(true)
  })

  it('grants BaoVe lane and soc visibility only', () => {
    authState.user = { role: 'BaoVe' }
    expect(capabilityFlags.canOperateLane.value).toBe(true)
    expect(capabilityFlags.canViewSoc.value).toBe(true)
    expect(capabilityFlags.canViewOperations.value).toBe(true)
    expect(capabilityFlags.canManagePolicy.value).toBe(false)
    expect(capabilityFlags.canManageVisitor.value).toBe(false)
    expect(capabilityFlags.canViewReports.value).toBe(false)
  })

  it('grants QuanLy foundation and reports access', () => {
    authState.user = { role: 'QuanLy' }
    expect(capabilityFlags.canManageFoundation.value).toBe(true)
    expect(capabilityFlags.canViewReports.value).toBe(true)
    expect(capabilityFlags.canOperateLane.value).toBe(false)
    expect(capabilityFlags.canManagePolicy.value).toBe(false)
  })

  it('grants LeTan visitor management', () => {
    authState.user = { role: 'LeTan' }
    expect(capabilityFlags.canManageVisitor.value).toBe(true)
    expect(capabilityFlags.canViewVisitor.value).toBe(true)
    expect(capabilityFlags.canOperateLane.value).toBe(false)
    expect(capabilityFlags.canViewReports.value).toBe(false)
  })

  it('denies everything to unknown roles', () => {
    authState.user = { role: 'NhanVien' }
    expect(capabilityFlags.canOperateLane.value).toBe(false)
    expect(capabilityFlags.canManageFoundation.value).toBe(false)
    expect(capabilityFlags.canViewSoc.value).toBe(false)
    expect(capabilityFlags.canViewReports.value).toBe(false)
  })
})
