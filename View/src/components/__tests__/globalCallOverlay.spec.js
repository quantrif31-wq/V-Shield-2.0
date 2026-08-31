import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/callStore', async () => {
  const { reactive, ref } = await vi.importActual('vue')
  const callState = reactive({
    state: 'idle',
    callType: 'audio',
    targetEmployeeId: null,
    targetFullName: '',
    fromEmployeeId: null,
    fromFullName: '',
    conversationId: null,
    offerSdp: null,
    isMuted: false,
    isVideoOff: false,
    isSpeakerMuted: false,
    isPip: false,
    callDuration: 0,
    errorMessage: '',
  })
  const localMediaStream = ref(null)
  const remoteMediaStream = ref(null)
  return {
    callState,
    localMediaStream,
    remoteMediaStream,
    initGlobalCallListener: vi.fn(),
    acceptCall: vi.fn(() => { callState.state = 'connected' }),
    rejectCall: vi.fn(() => { callState.state = 'idle' }),
    endCall: vi.fn(() => { callState.state = 'idle' }),
    toggleMic: vi.fn(() => { callState.isMuted = !callState.isMuted }),
    toggleCamera: vi.fn(() => { callState.isVideoOff = !callState.isVideoOff }),
    togglePip: vi.fn(() => { callState.isPip = !callState.isPip }),
    toggleSpeaker: vi.fn(() => { callState.isSpeakerMuted = !callState.isSpeakerMuted }),
    formatCallDuration: vi.fn((seconds) => `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`),
  }
})

const GlobalCallOverlay = (await import('../Call/GlobalCallOverlay.vue')).default
const callStore = await import('../../stores/callStore')

const norm = (s) => String(s).normalize('NFC')

beforeEach(() => {
  vi.clearAllMocks()
  callStore.callState.state = 'idle'
  callStore.callState.callType = 'audio'
  callStore.callState.targetFullName = ''
  callStore.callState.fromFullName = ''
  callStore.callState.isMuted = false
  callStore.callState.isVideoOff = false
  callStore.callState.isSpeakerMuted = false
  callStore.callState.isPip = false
  callStore.callState.callDuration = 0
  callStore.localMediaStream.value = null
  callStore.remoteMediaStream.value = null
})

afterEach(() => {
  callStore.callState.state = 'idle'
  callStore.localMediaStream.value = null
  callStore.remoteMediaStream.value = null
})

describe('GlobalCallOverlay', () => {
  it('renders nothing when call state is idle', async () => {
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(callStore.initGlobalCallListener).toHaveBeenCalled()
    expect(wrapper.find('.global-call-root').exists()).toBe(false)
  })

  it('shows the incoming banner for an audio call', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.callType = 'audio'
    callStore.callState.fromFullName = 'Nguyễn Văn A'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.zalo-incoming-card').exists()).toBe(true)
    expect(norm(wrapper.text())).toContain(norm('Cuộc gọi đến...'))
    expect(norm(wrapper.text())).toContain(norm('Nguyễn Văn A'))
    expect(norm(wrapper.text())).toContain(norm('Cuộc gọi Thoại HD'))
    expect(wrapper.find('.banner-avatar').text()).toBe('N')
  })

  it('shows the incoming banner for a video call', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.callType = 'video'
    callStore.callState.fromFullName = 'Trần B'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(norm(wrapper.text())).toContain(norm('Cuộc gọi Video Face-to-Face'))
  })

  it('falls back to V avatar when caller name is empty', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.fromFullName = ''
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.banner-avatar').text()).toBe('V')
  })

  it('accepts the incoming call via the answer button', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.fromFullName = 'Nguyễn Văn A'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    await wrapper.find('.btn-answer').trigger('click')
    expect(callStore.acceptCall).toHaveBeenCalled()
    expect(callStore.callState.state).toBe('connected')
    expect(wrapper.find('.global-call-root').exists()).toBe(true)
  })

  it('rejects the incoming call via the decline button', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.fromFullName = 'Nguyễn Văn A'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    await wrapper.find('.btn-decline').trigger('click')
    expect(callStore.rejectCall).toHaveBeenCalled()
    expect(callStore.callState.state).toBe('idle')
  })

  it('dismisses the incoming banner and resets it on the next incoming call', async () => {
    callStore.callState.state = 'incoming'
    callStore.callState.fromFullName = 'Nguyễn Văn A'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.zalo-incoming-card').exists()).toBe(true)
    await wrapper.find('.btn-ignore').trigger('click')
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isIncomingBannerDismissed).toBe(true)
    expect(wrapper.find('.zalo-incoming-card').exists()).toBe(false)

    callStore.callState.state = 'idle'
    await wrapper.vm.$nextTick()
    callStore.callState.state = 'incoming'
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isIncomingBannerDismissed).toBe(false)
    expect(wrapper.find('.zalo-incoming-card').exists()).toBe(true)
  })

  it('shows the calling overlay for an audio call and cancels it', async () => {
    callStore.callState.state = 'calling'
    callStore.callState.callType = 'audio'
    callStore.callState.targetFullName = 'Anh Tùng'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.outgoing-call-box').exists()).toBe(true)
    expect(norm(wrapper.text())).toContain(norm('Đang đổ chuông...'))
    expect(wrapper.find('.caller-name').text()).toBe('Anh Tùng')
    expect(wrapper.find('.outgoing-video-preview').exists()).toBe(false)
    await wrapper.find('.action-btn').trigger('click')
    expect(callStore.endCall).toHaveBeenCalled()
    expect(callStore.callState.state).toBe('idle')
  })

  it('shows the outgoing video preview when calling a video call', async () => {
    callStore.callState.state = 'calling'
    callStore.callState.callType = 'video'
    callStore.callState.targetFullName = 'Anh Tùng'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.outgoing-video-preview').exists()).toBe(true)
  })

  it('shows the connected voice stage with a formatted duration', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'audio'
    callStore.callState.targetFullName = 'Anh Tùng'
    callStore.callState.callDuration = 125
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.face-to-face-modal').exists()).toBe(true)
    expect(wrapper.find('.face-to-face-modal').classes()).toContain('audio-only-modal')
    expect(norm(wrapper.text())).toContain(norm('Đang trong cuộc gọi thoại HD'))
    expect(wrapper.text()).toContain('02:05')
    expect(callStore.formatCallDuration).toHaveBeenCalled()
  })

  it('toggles mic and speaker in a connected call and reflects active states', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'audio'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    const buttons = wrapper.findAll('.ctrl-btn')
    await buttons.at(0).trigger('click')
    await wrapper.vm.$nextTick()
    expect(callStore.toggleMic).toHaveBeenCalled()
    expect(callStore.callState.isMuted).toBe(true)
    expect(wrapper.findAll('.ctrl-btn').at(0).classes()).toContain('is-active')
    await wrapper.findAll('.ctrl-btn').at(2).trigger('click')
    await wrapper.vm.$nextTick()
    expect(callStore.callState.isSpeakerMuted).toBe(true)
    expect(wrapper.findAll('.ctrl-btn').at(2).classes()).toContain('is-active')
  })

  it('toggles the camera in a connected video call', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'video'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.local-pip-screen').exists()).toBe(true)
    await wrapper.findAll('.ctrl-btn').at(1).trigger('click')
    await wrapper.vm.$nextTick()
    expect(callStore.callState.isVideoOff).toBe(true)
    expect(norm(wrapper.text())).toContain(norm('Camera đã tắt'))
    expect(wrapper.find('.camera-off-placeholder').exists()).toBe(true)
    await wrapper.findAll('.ctrl-btn').at(1).trigger('click')
    await wrapper.vm.$nextTick()
    expect(callStore.callState.isVideoOff).toBe(false)
    expect(wrapper.find('.local-pip-screen').exists()).toBe(true)
  })

  it('minimizes to pip bubble and expands back to the full window', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'audio'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    await wrapper.find('.icon-btn-ghost').trigger('click')
    expect(callStore.togglePip).toHaveBeenCalled()
    expect(callStore.callState.isPip).toBe(true)
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.floating-pip-bubble').exists()).toBe(true)
    expect(wrapper.find('.pip-audio-preview').exists()).toBe(true)
    expect(wrapper.find('.face-to-face-modal').exists()).toBe(false)
    await wrapper.find('.pip-btn-expand').trigger('click')
    expect(callStore.callState.isPip).toBe(false)
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.face-to-face-modal').exists()).toBe(true)
    expect(wrapper.find('.floating-pip-bubble').exists()).toBe(false)
  })

  it('ends the call from the pip bubble buttons', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'video'
    callStore.callState.isPip = true
    callStore.callState.callDuration = 7
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    expect(wrapper.find('.pip-video-preview').exists()).toBe(true)
    expect(wrapper.find('.pip-info-badge').text()).toContain('00:07')
    await wrapper.find('.pip-btn-end').trigger('click')
    expect(callStore.endCall).toHaveBeenCalled()
    expect(callStore.callState.state).toBe('idle')
  })

  it('assigns local stream to outgoing preview and local pip video', async () => {
    callStore.callState.state = 'calling'
    callStore.callState.callType = 'video'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    const stream = {}
    callStore.localMediaStream.value = stream
    await flushPromises()
    expect(wrapper.find('.outgoing-video-preview video').element.srcObject).toBe(stream)

    callStore.callState.state = 'connected'
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.local-pip-screen').element.srcObject).toBe(stream)
  })

  it('assigns remote stream to connected video, audio and pip refs', async () => {
    callStore.callState.state = 'connected'
    callStore.callState.callType = 'video'
    const wrapper = mount(GlobalCallOverlay)
    await flushPromises()
    const stream = { fake: true }
    callStore.remoteMediaStream.value = stream
    await flushPromises()
    expect(wrapper.find('.remote-video-screen').element.srcObject).toBe(stream)
    expect(wrapper.find('audio').element.srcObject).toBe(stream)

    callStore.callState.isPip = true
    await flushPromises()
    expect(wrapper.find('.pip-video-preview').element.srcObject).toBe(stream)

    callStore.callState.isPip = false
    await flushPromises()
    expect(wrapper.find('.remote-video-screen').element.srcObject).toBe(stream)
    expect(wrapper.find('.local-pip-screen').element.srcObject).toBe(stream)
  })
})