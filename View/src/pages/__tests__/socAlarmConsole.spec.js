import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {}, params: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))
vi.mock('../../services/socApi', () => ({
  socApi: {
    overview: vi.fn(),
    getAlarms: vi.fn(),
    getIncidents: vi.fn(),
  },
}))
vi.mock('../EventTimeline.vue', () => ({
  default: { name: 'EventTimeline', template: '<div class="timeline">EVENTS</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'EventTimeline',
  __name: 'EventTimeline',
}))

const socApi = (await import('../../services/socApi')).socApi
const SocAlarmConsole = (await import('../SocAlarmConsole.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('SocAlarmConsole', () => {
  it('loads the SOC overview, alarms and incidents', async () => {
    socApi.overview.mockResolvedValue({ data: { alarms: 5, incidents: 2 } })
    socApi.getAlarms.mockResolvedValue({ data: { items: [{ alarmId: 1, title: 'Cảnh báo A', severity: 'High', state: 'Open' }], total: 1 } })
    socApi.getIncidents.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(SocAlarmConsole)
    await flushPromises()
    expect(socApi.overview).toHaveBeenCalled()
    expect(socApi.getAlarms).toHaveBeenCalled()
    expect(socApi.getIncidents).toHaveBeenCalled()
  })
})
