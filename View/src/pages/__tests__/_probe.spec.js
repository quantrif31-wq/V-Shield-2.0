import { flushPromises, mount } from '@vue/test-utils'
import { it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { role: 'Admin' } } }))
vi.mock('../../services/accessLogApi', () => ({ getExceptions: vi.fn().mockResolvedValue({ data: { items: [] } }) }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getLaneEvents: vi.fn().mockResolvedValue({ data: { items: [] } }),
    getEvidenceItems: vi.fn().mockResolvedValue({ data: { items: [] } }),
    getBarriers: vi.fn().mockResolvedValue({ data: [] }),
    getBarrierCommands: vi.fn().mockResolvedValue({ data: { items: [] } }),
    getCorrelations: vi.fn().mockResolvedValue({ data: { items: [] } }),
    getInterventionRequests: vi.fn().mockResolvedValue({ data: { items: [] } }),
    createInterventionRequest: vi.fn(),
    acceptInterventionRequest: vi.fn(),
    rejectInterventionRequest: vi.fn(),
    executeInterventionRequest: vi.fn(),
    recordLaneEvent: vi.fn(),
  },
}))
const Exceptions = (await import('../Exceptions.vue')).default

it('probe vm identity semantics', async () => {
  const wrapper = mount(Exceptions, { global: { stubs: { ExceptionCaseTimeline: true } } })
  await flushPromises()
  const item = { id: 1, sourceLogId: 9 }
  wrapper.vm.selectCase(item)
  const selected = wrapper.vm.selectedCase
  console.log('selectedCase same?', selected === item)
  console.log('keys:', Object.getOwnPropertyNames(wrapper.vm).slice(0, 30).join(','))
  const item2 = { operationalInterventionRequestId: 3, rejectionReason: 'x' }
  wrapper.vm.selectIntervention(item2)
  console.log('selectedIntervention same?', wrapper.vm.selectedIntervention === item2)
})