import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import HomePage from '../HomePage.vue'

vi.mock('../../services/portalApi', () => ({
  portalApi: {
    getOverview: vi.fn().mockResolvedValue({
      systemName: 'V-SHIELD 2.0',
      averageRating: 4.95,
      totalReviews: 1280,
      version: '2.0.0'
    }),
    getReviews: vi.fn().mockResolvedValue([
      { id: '1', authorName: 'Prof Tung', rating: 5, content: 'Very good' }
    ]),
    getComments: vi.fn().mockResolvedValue([
      { id: '1', authorName: 'Cyber01', content: 'Awesome!' }
    ]),
    createReview: vi.fn().mockResolvedValue({ success: true, data: { id: '2', authorName: 'Tester', rating: 5, content: 'Nice' } }),
    createComment: vi.fn().mockResolvedValue({ success: true, data: { id: '2', authorName: 'User', content: 'Hello' } }),
    reactComment: vi.fn().mockResolvedValue({ success: true }),
    submitFeedback: vi.fn().mockResolvedValue({ success: true }),
    subscribeNewsletter: vi.fn().mockResolvedValue({ success: true })
  }
}))

describe('HomePage.vue', () => {
  it('renders portal hero and navigation properly', () => {
    const wrapper = mount(HomePage, {
      global: {
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
          PortalParticlesCanvas: { template: '<div class="canvas-stub"></div>' },
          PortalAudioToggle: { template: '<button class="audio-stub"></button>' },
          PortalAuthModal: { template: '<div class="modal-stub"></div>' }
        }
      }
    })

    expect(wrapper.text()).toContain('V-SHIELD 2.0')
    expect(wrapper.text()).toContain('HỆ THỐNG AN NINH')
    expect(wrapper.text()).toContain('TÍNH NĂNG')
    expect(wrapper.text()).toContain('LỊCH SỬ & LỘ TRÌNH')
    expect(wrapper.text()).toContain('TẢI APK')
    expect(wrapper.text()).toContain('ĐÁNH GIÁ')
  })
})
