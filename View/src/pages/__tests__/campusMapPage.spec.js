import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { role: 'Admin' } } }))
vi.mock('../../services/campusMapApi', () => ({
  getCampusScene3D: vi.fn(),
  getCampusMapRealtime: vi.fn(),
}))
vi.mock('../../components/campus-map/CampusMapToolbar.vue', () => ({
  default: { name: 'CampusMapToolbar', template: '<div class="toolbar">TOOLBAR</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'CampusMapToolbar',
  __name: 'CampusMapToolbar',
}))
vi.mock('../../components/campus-map/Campus3DCanvas.vue', () => ({
  default: { name: 'Campus3DCanvas', template: '<div class="canvas3d">3D</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'Campus3DCanvas',
  __name: 'Campus3DCanvas',
}))

const campusMapApi = await import('../../services/campusMapApi')
const CampusMapPage = (await import('../CampusMapPage.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  campusMapApi.getCampusScene3D.mockResolvedValue({ data: {} })
  campusMapApi.getCampusMapRealtime.mockResolvedValue({ data: {} })
})

describe('CampusMapPage', () => {
  it('loads the 3d scene and realtime data on mount', async () => {
    const wrapper = mount(CampusMapPage)
    await flushPromises()
    expect(campusMapApi.getCampusScene3D).toHaveBeenCalled()
    expect(campusMapApi.getCampusMapRealtime).toHaveBeenCalled()
    expect(wrapper.find('.toolbar').exists()).toBe(true)
  })
})
