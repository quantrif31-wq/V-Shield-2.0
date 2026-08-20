import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'

import AIChatBot from '../AIChatBot.vue'

let router

beforeEach(async () => {
  Object.defineProperty(window, 'addEventListener', { writable: true, value: vi.fn() })
  Object.defineProperty(window, 'removeEventListener', { writable: true, value: vi.fn() })
  router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/guide', component: { template: '<div>guide</div>' } },
      { path: '/gate-transit-monitor', component: { template: '<div>monitor</div>' } },
      { path: '/dynamic-qr-generator', component: { template: '<div>qr</div>' } },
      { path: '/', component: { template: '<div>home</div>' } },
    ],
  })
})

afterEach(() => {
  vi.restoreAllMocks()
})

function mountChat() {
  return mount(AIChatBot, { global: { plugins: [router] } })
}

describe('AIChatBot.vue', () => {
  it('shows the fab and opens the chat with a welcome message', async () => {
    const wrapper = mountChat()
    expect(wrapper.find('.chat-fab').exists()).toBe(true)
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.vm.chatOpen).toBe(true)
    expect(wrapper.vm.messages.length).toBe(1)
    expect(wrapper.vm.messages[0].role).toBe('ai')
    expect(wrapper.vm.hasInteracted).toBe(true)
  })

  it('does not duplicate the welcome message when reopening', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    await wrapper.find('.chat-close').trigger('click')
    expect(wrapper.vm.chatOpen).toBe(false)
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.vm.messages.length).toBe(1)
  })

  it('answers a guide question through handleGuideResponse', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('huong dan su dung phan mem V-Shield')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Hướng dẫn sử dụng V-Shield')
  })

  it('answers an admin question', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('Tôi là Admin, tôi có thể làm gì?')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Quyền hạn của Admin')
  })

  it('answers a guard question', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('bao ve can lam gi khi truc cong')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Quyền hạn của Bảo vệ')
  })

  it('answers a reception question', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('le tan can dung V-Shield the nao')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Quyền hạn của Lễ tân')
  })

  it('answers a manager question', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('quan ly van hanh')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Quyền hạn của Quản lý')
  })

  it('answers manual operation questions', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('lam the nao khi camera loi')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Vận hành thủ công')
  })

  it('answers qr questions and thanks', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('cach tao qr dong')
    expect(wrapper.vm.messages[wrapper.vm.messages.length - 1].text).toContain('QR động')
    wrapper.vm.handleGuideResponse('cam on ban')
    expect(wrapper.vm.messages[wrapper.vm.messages.length - 1].text).toContain('Không có gì')
  })

  it('falls back to the generic assistant reply for unknown questions', () => {
    const wrapper = mountChat()
    wrapper.vm.handleGuideResponse('thời tiết hôm nay thế nào ?!@#')
    const last = wrapper.vm.messages[wrapper.vm.messages.length - 1]
    expect(last.text).toContain('Tôi có thể giúp gì')
  })

  it('sends a message and routes from a guide link', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.inputText = 'huong dan su dung'
    await wrapper.vm.sendMessage()
    const ai = wrapper.vm.messages.find((m) => m.role === 'ai' && m.text.includes('Hướng dẫn'))
    expect(ai).toBeTruthy()
    const link = wrapper.find('.msg-bubble a')
    await link.trigger('click')
    await new Promise((r) => setTimeout(r, 0))
    expect(wrapper.vm.chatOpen).toBe(false)
    expect(router.currentRoute.value.path).toBe('/guide')
  })

  it('ignores non-link clicks inside the messages area', async () => {
    const wrapper = mountChat()
    wrapper.vm.openChat()
    const event = { target: document.createElement('div'), preventDefault: vi.fn() }
    await wrapper.vm.handleMsgClick(event)
    expect(event.preventDefault).not.toHaveBeenCalled()
  })

  it('does not push a route for external links', async () => {
    const wrapper = mountChat()
    wrapper.vm.openChat()
    const pushSpy = vi.spyOn(router, 'push')
    const anchor = document.createElement('a')
    anchor.setAttribute('href', 'https://external.example/x')
    const event = { target: anchor, preventDefault: vi.fn() }
    await wrapper.vm.handleMsgClick(event)
    expect(event.preventDefault).not.toHaveBeenCalled()
    expect(pushSpy).not.toHaveBeenCalled()
  })

  it('drags the fab and updates its offset', async () => {
    const wrapper = mountChat()
    wrapper.vm.startDrag({ pointerId: 1, clientX: 100, clientY: 100, currentTarget: { setPointerCapture: vi.fn() } })
    expect(wrapper.vm.dragState.active).toBe(true)
    expect(window.addEventListener).toHaveBeenCalled()
    const move = window.addEventListener.mock.calls.find(([name]) => name === 'pointermove')[1]
    move({ pointerId: 1, clientX: 160, clientY: 140 })
    expect(wrapper.vm.dragState.moved).toBe(true)
    expect(wrapper.vm.fabOffset).toEqual({ x: 24, y: 24 })
    const up = window.addEventListener.mock.calls.find(([name]) => name === 'pointerup')[1]
    up({ pointerId: 1 })
    expect(wrapper.vm.dragState.active).toBe(false)
    await wrapper.vm.handleFabClick()
    expect(wrapper.vm.chatOpen).toBe(false)
  })

  it('ignores moves from a different pointer', () => {
    const wrapper = mountChat()
    wrapper.vm.startDrag({ pointerId: 3, clientX: 10, clientY: 10, currentTarget: { setPointerCapture: vi.fn() } })
    const move = window.addEventListener.mock.calls.find(([name]) => name === 'pointermove')[1]
    move({ pointerId: 99, clientX: 200, clientY: 200 })
    expect(wrapper.vm.fabOffset).toEqual({ x: 0, y: 0 })
    const up = window.addEventListener.mock.calls.find(([name]) => name === 'pointerup')[1]
    up({ pointerId: 3 })
  })

  it('clicks without dragging to open the chat', async () => {
    const wrapper = mountChat()
    const fab = wrapper.find('.chat-fab')
    await fab.trigger('pointerdown', { pointerId: 2 })
    wrapper.vm.startDrag({ pointerId: 2, clientX: 10, clientY: 10, currentTarget: { setPointerCapture: vi.fn() } })
    const up = window.addEventListener.mock.calls.find(([name]) => name === 'pointerup')[1]
    up({ pointerId: 2 })
    await fab.trigger('click')
    expect(wrapper.vm.chatOpen).toBe(true)
  })

  it('closes the chat on a route change', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.vm.chatOpen).toBe(true)
    await router.push('/guide')
    await router.isReady()
    expect(wrapper.vm.chatOpen).toBe(false)
  })

  it('cleans up event listeners on unmount', () => {
    const wrapper = mountChat()
    wrapper.unmount()
    expect(window.removeEventListener).toHaveBeenCalled()
  })
})