import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getBackupRuns: vi.fn(),
    getRestoreDrills: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const BackupRestoreDrillDashboard = (await import('../BackupRestoreDrillDashboard.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getBackupRuns.mockResolvedValue({ data: [{ backupRunId: 1, profile: 'p1', status: 'Completed', startedAtUtc: '2026-01-01T00:00:00Z', sizeBytes: 2097152 }] })
  enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [{ restoreDrillId: 2, profile: 'p2', status: 'Failed', startedAtUtc: '2026-01-01T00:00:00Z', targetRpoMinutes: 5, targetRtoMinutes: 10, passed: false }] })
})

describe('BackupRestoreDrillDashboard', () => {
  it('loads backups and drills on mount', async () => {
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(enterpriseApi.getBackupRuns).toHaveBeenCalledWith({ limit: 10 })
    expect(enterpriseApi.getRestoreDrills).toHaveBeenCalledWith({ limit: 10 })
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.text()).toContain('p1')
    expect(wrapper.text()).toContain('p2')
  })

  it('renders a refresh action', async () => {
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    await wrapper.find('button.btn').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getBackupRuns).toHaveBeenCalledTimes(2)
  })

  it('falls back to empty arrays for non-array responses', async () => {
    enterpriseApi.getBackupRuns.mockResolvedValue({ data: { not: 'array' } })
    enterpriseApi.getRestoreDrills.mockResolvedValue({ data: null })
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(wrapper.vm.backups).toEqual([])
    expect(wrapper.vm.drills).toEqual([])
  })

  it('handles load error gracefully', async () => {
    enterpriseApi.getBackupRuns.mockRejectedValue({})
    enterpriseApi.getRestoreDrills.mockRejectedValue({})
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(wrapper.vm.backups).toEqual([])
    expect(wrapper.vm.drills).toEqual([])
    expect(wrapper.vm.loading).toBe(false)
  })

  it('shows empty states when no data', async () => {
    enterpriseApi.getBackupRuns.mockResolvedValue({ data: [] })
    enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [] })
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(wrapper.findAll('.empty-card').length).toBe(2)
  })

  it('maps run status labels and falls back to the raw value', () => {
    const wrapper = mount(BackupRestoreDrillDashboard)
    expect(wrapper.vm.runStatusLabel('Completed')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Failed')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Running')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Pending')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Started')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Cancelled')).toBeTruthy()
    expect(wrapper.vm.runStatusLabel('Weird')).toBe('Weird')
  })

  it('renders size in MB and dash when missing', async () => {
    enterpriseApi.getBackupRuns.mockResolvedValue({ data: [
      { backupRunId: 1, status: 'Completed', sizeBytes: 1048576 },
      { backupRunId: 2, status: 'Failed', sizeBytes: 0 },
    ] })
    enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [] })
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(wrapper.text()).toContain('1.00 MB')
  })
})
