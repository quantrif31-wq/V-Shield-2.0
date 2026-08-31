import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi } from 'vitest'
import PortalHome from '../portal/PortalHome.vue'
import PortalFeatures from '../portal/PortalFeatures.vue'
import PortalRoadmap from '../portal/PortalRoadmap.vue'
import PortalDownload from '../portal/PortalDownload.vue'
import PortalCommunity from '../portal/PortalCommunity.vue'
import PortalAbout from '../portal/PortalAbout.vue'
import PortalContact from '../portal/PortalContact.vue'

vi.mock('../../services/portalApi', () => ({
  portalApi: {
    getOverview: vi.fn().mockResolvedValue({
      systemName: 'V-SHIELD 2.0',
      tagline: 'Test Tagline',
      averageRating: 5.0
    }),
    getReviews: vi.fn().mockResolvedValue([
      { id: '1', authorName: 'Tester', rating: 5, content: 'Great system', platform: 'Web Cloud', likesCount: 1 }
    ]),
    getComments: vi.fn().mockResolvedValue([
      { id: '1', authorName: 'Operator', content: 'Test Comment', likesCount: 2, replies: [] }
    ]),
    createReview: vi.fn().mockResolvedValue({ success: true, data: { id: 'new-1' } }),
    createComment: vi.fn().mockResolvedValue({ success: true, data: { id: 'cmt-new' } }),
    submitFeedback: vi.fn().mockResolvedValue({ success: true }),
    subscribeNewsletter: vi.fn().mockResolvedValue({ success: true })
  }
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => ({ path: '/' })
}))

describe('Multi-Page Portal Sub-Views', () => {
  it('mounts PortalHome correctly', async () => {
    const wrapper = mount(PortalHome, { global: { stubs: { 'router-link': true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('V-SHIELD')
  })

  it('mounts PortalFeatures correctly', async () => {
    const wrapper = mount(PortalFeatures)
    await flushPromises()
    expect(wrapper.text()).toContain('VŨ KHÍ & CÔNG NGHỆ')
  })

  it('mounts PortalRoadmap correctly', async () => {
    const wrapper = mount(PortalRoadmap)
    await flushPromises()
    expect(wrapper.text()).toContain('LỊCH SỬ NÂNG CẤP')
  })

  it('mounts PortalDownload correctly', async () => {
    const wrapper = mount(PortalDownload)
    await flushPromises()
    expect(wrapper.text()).toContain('TRẠM TẢI ỨNG DỤNG')
  })

  it('mounts PortalCommunity correctly', async () => {
    const wrapper = mount(PortalCommunity, {
      global: {
        provide: {
          communityUser: { value: { fullName: 'Test User' } },
          openAuthModal: vi.fn()
        }
      }
    })
    await flushPromises()
    expect(wrapper.text()).toContain('NHẬT KÝ ĐÁNH GIÁ')
  })

  it('mounts PortalAbout correctly', async () => {
    const wrapper = mount(PortalAbout)
    await flushPromises()
    expect(wrapper.text()).toContain('ThS. Phan Hoàng Khải')
  })

  it('mounts PortalContact correctly', async () => {
    const wrapper = mount(PortalContact, {
      global: {
        provide: {
          communityUser: { value: null }
        }
      }
    })
    await flushPromises()
    expect(wrapper.text()).toContain('KÊNH TIẾP NHẬN TÁC CHIẾN')
  })
})
