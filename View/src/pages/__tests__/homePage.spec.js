import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import HomePage from '../HomePage.vue'

vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn() }) }))
vi.mock('../../stores/auth', () => ({ authState: { user: null } }))
vi.mock('../../services/portalApi', () => ({
  portalApi: {
    getOverview: vi.fn(),
    getReviews: vi.fn(),
    getComments: vi.fn(),
    createReview: vi.fn(),
    createComment: vi.fn(),
    reactComment: vi.fn(),
    submitFeedback: vi.fn(),
    subscribeNewsletter: vi.fn(),
  },
}))

const { portalApi } = await import('../../services/portalApi')

function makeStubs(user = null) {
  return {
    global: {
      stubs: {
        RouterLink: { template: '<a><slot /></a>' },
        PortalParticlesCanvas: { template: '<div class="canvas-stub"></div>' },
        PortalAudioToggle: { template: '<button class="audio-stub"></button>' },
        PortalAuthModal: { template: '<div class="modal-stub"></div>' },
      },
    },
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
  portalApi.getOverview.mockResolvedValue({ systemName: 'V-SHIELD 2.0', averageRating: 4.9, totalReviews: 100, version: '2.0.0' })
  portalApi.getReviews.mockResolvedValue([{ id: 'r1', authorName: 'A', rating: 5, content: 'ok', createdAt: '2026-08-01' }])
  portalApi.getComments.mockResolvedValue([{ id: 'c1', authorName: 'B', content: 'nice', badge: 'Operator' }])
})

describe('HomePage.vue', () => {
  it('renders hero, navigation and static sections', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    expect(wrapper.text()).toContain('V-SHIELD 2.0')
    expect(wrapper.text()).toContain('HỆ THỐNG AN NINH')
    expect(wrapper.text()).toContain('TÍNH NĂNG')
    expect(wrapper.text()).toContain('LỊCH SỬ & LỘ TRÌNH')
    expect(wrapper.text()).toContain('ĐÁNH GIÁ')
    expect(wrapper.vm.overview.averageRating).toBe(4.9)
    expect(wrapper.vm.reviews).toHaveLength(1)
    expect(wrapper.vm.comments).toHaveLength(1)
  })

  it('loads community user from localStorage on mount', async () => {
    localStorage.setItem('vshield_community_user', JSON.stringify({ fullName: 'Tester', email: 't@t.com' }))
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    expect(wrapper.vm.communityUser).toEqual({ fullName: 'Tester', email: 't@t.com' })
    expect(wrapper.vm.feedbackForm.email).toBe('t@t.com')
  })

  it('handles invalid local storage user payload', async () => {
    const errSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    localStorage.setItem('vshield_community_user', '{bad json')
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    expect(wrapper.vm.communityUser).toBe(null)
    errSpy.mockRestore()
  })

  it('nextQuote rotates through mascot quotes', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    const len = wrapper.vm.mascotQuotes.length
    wrapper.vm.nextQuote()
    expect(wrapper.vm.currentQuoteIndex).toBe(1 % len)
  })

  it('triggerSfx dispatches a custom event', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    const spy = vi.spyOn(window, 'dispatchEvent')
    wrapper.vm.triggerSfx()
    expect(spy).toHaveBeenCalled()
    spy.mockRestore()
  })

  it('handleLoginSuccess fills forms', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    wrapper.vm.handleLoginSuccess({ fullName: 'UserX', email: 'x@x.com' })
    expect(wrapper.vm.communityUser.fullName).toBe('UserX')
    expect(wrapper.vm.newReview.authorName).toBe('UserX')
    expect(wrapper.vm.newComment.authorName).toBe('UserX')
    expect(wrapper.vm.feedbackForm.fullName).toBe('UserX')
  })

  it('handleLogout clears community user', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    wrapper.vm.communityUser = { fullName: 'U' }
    wrapper.vm.handleLogout()
    expect(wrapper.vm.communityUser).toBe(null)
  })

  it('submitReview validates empty fields', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    await wrapper.vm.submitReview()
    expect(wrapper.vm.reviewToast).toContain('Vui lòng nhập')
  })

  it('submitReview posts and unshifts a new review', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.createReview.mockResolvedValue({ success: true, data: { id: 'r2', authorName: 'New' } })
    wrapper.vm.newReview.authorName = 'New'
    wrapper.vm.newReview.content = 'Hay'
    await wrapper.vm.submitReview()
    expect(portalApi.createReview).toHaveBeenCalled()
    expect(wrapper.vm.reviews[0].id).toBe('r2')
    expect(wrapper.vm.reviewSubmitting).toBe(false)
  })

  it('submitReview handles API failure', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.createReview.mockRejectedValue(new Error('boom'))
    wrapper.vm.newReview.authorName = 'A'
    wrapper.vm.newReview.content = 'B'
    await wrapper.vm.submitReview()
    expect(wrapper.vm.reviewToast).toContain('Không thể gửi')
    expect(wrapper.vm.reviewSubmitting).toBe(false)
  })

  it('submitComment validates empty fields', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    await wrapper.vm.submitComment()
    expect(wrapper.vm.commentToast).toContain('Vui lòng nhập')
  })

  it('submitComment posts and unshifts a comment', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.createComment.mockResolvedValue({ success: true, data: { id: 'c2', content: 'hi' } })
    wrapper.vm.newComment.authorName = 'A'
    wrapper.vm.newComment.content = 'hi'
    await wrapper.vm.submitComment()
    expect(wrapper.vm.comments[0].id).toBe('c2')
    expect(wrapper.vm.commentSubmitting).toBe(false)
  })

  it('submitComment handles API failure', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.createComment.mockRejectedValue(new Error('boom'))
    wrapper.vm.newComment.authorName = 'A'
    wrapper.vm.newComment.content = 'B'
    await wrapper.vm.submitComment()
    expect(wrapper.vm.commentToast).toContain('Không thể đăng')
  })

  it('reactComment increments likes and calls API', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.reactComment.mockResolvedValue({ success: true })
    const comment = { id: 'c1', likesCount: 0 }
    await wrapper.vm.reactComment(comment)
    expect(comment.likesCount).toBe(1)
    expect(portalApi.reactComment).toHaveBeenCalledWith('c1', 'like')
  })

  it('submitFeedback validates empty fields', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    await wrapper.vm.submitFeedback()
    expect(wrapper.vm.feedbackToast).toContain('Vui lòng điền')
  })

  it('submitFeedback posts feedback', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.submitFeedback.mockResolvedValue({ success: true })
    wrapper.vm.feedbackForm.fullName = 'A'
    wrapper.vm.feedbackForm.email = 'a@a.com'
    wrapper.vm.feedbackForm.message = 'msg'
    await wrapper.vm.submitFeedback()
    expect(wrapper.vm.feedbackToast).toContain('Góp ý')
    expect(wrapper.vm.feedbackSubmitting).toBe(false)
  })

  it('submitFeedback handles API failure', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.submitFeedback.mockRejectedValue(new Error('boom'))
    wrapper.vm.feedbackForm.fullName = 'A'
    wrapper.vm.feedbackForm.email = 'a@a.com'
    wrapper.vm.feedbackForm.message = 'msg'
    await wrapper.vm.submitFeedback()
    expect(wrapper.vm.feedbackToast).toContain('Lỗi kết nối')
  })

  it('submitNewsletter validates email', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    await wrapper.vm.submitNewsletter()
    expect(wrapper.vm.newsletterToast).toContain('email hợp lệ')
  })

  it('submitNewsletter subscribes', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.subscribeNewsletter.mockResolvedValue({ success: true })
    wrapper.vm.newsletterEmail = 'a@a.com'
    await wrapper.vm.submitNewsletter()
    expect(wrapper.vm.newsletterToast).toContain('Đăng ký')
    expect(wrapper.vm.newsletterSubmitting).toBe(false)
  })

  it('submitNewsletter handles API failure', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    portalApi.subscribeNewsletter.mockRejectedValue(new Error('boom'))
    wrapper.vm.newsletterEmail = 'a@a.com'
    await wrapper.vm.submitNewsletter()
    expect(wrapper.vm.newsletterToast).toContain('Không thể đăng ký')
  })

  it('scrollToSection scrolls into view when element exists', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    const el = { scrollIntoView: vi.fn() }
    const getSpy = vi.spyOn(document, 'getElementById').mockReturnValue(el)
    wrapper.vm.scrollToSection('features')
    expect(el.scrollIntoView).toHaveBeenCalled()
    getSpy.mockRestore()
  })

  it('scrollToSection is a no-op when element missing', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    const getSpy = vi.spyOn(document, 'getElementById').mockReturnValue(null)
    expect(() => wrapper.vm.scrollToSection('nothing')).not.toThrow()
    getSpy.mockRestore()
  })

  it('downloadApk opens the APK url', async () => {
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => {})
    wrapper.vm.downloadApk()
    expect(openSpy).toHaveBeenCalledWith(expect.stringContaining('apk'), '_blank')
    openSpy.mockRestore()
  })

  it('handles portal api load failure gracefully', async () => {
    const errSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    portalApi.getOverview.mockRejectedValue(new Error('boom'))
    portalApi.getReviews.mockRejectedValue(new Error('boom'))
    portalApi.getComments.mockRejectedValue(new Error('boom'))
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    expect(wrapper.vm.overview.systemName).toBe('V-SHIELD 2.0')
    errSpy.mockRestore()
  })

  it('renders dashboard link when a user is logged in', async () => {
    const auth = await import('../../stores/auth')
    auth.authState.user = { fullName: 'Admin' }
    const wrapper = mount(HomePage, makeStubs(null))
    await flushPromises()
    expect(wrapper.text()).toContain('Vào Dashboard')
    auth.authState.user = null
  })
})
