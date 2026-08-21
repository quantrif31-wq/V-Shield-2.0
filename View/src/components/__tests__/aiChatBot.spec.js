import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'

import AIChatBot from '../AIChatBot.vue'

let router

function sseResponse(chunks) {
  let i = 0
  const reader = {
    async read() {
      if (i < chunks.length) {
        return { done: false, value: new TextEncoder().encode(chunks[i++]) }
      }
      return { done: true, value: undefined }
    },
    releaseLock() {}
  }
  return { ok: true, status: 200, body: { getReader: () => reader } }
}

beforeEach(() => {
  sessionStorage.clear()
  router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/gate-transit-monitor', component: { template: '<div>monitor</div>' } },
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

describe('AIChatBot.vue (AI thật)', () => {
  it('shows the fab and opens the chat', async () => {
    const wrapper = mountChat()
    expect(wrapper.find('.chat-fab').exists()).toBe(true)
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.vm.chatOpen).toBe(true)
    expect(wrapper.vm.hasInteracted).toBe(true)
  })

  it('shows the welcome hero when there are no messages', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.find('.chat-welcome').exists()).toBe(true)
    expect(wrapper.vm.messages.length).toBe(0)
  })

  it('sends a message, calls the AI stream endpoint and renders the streamed reply', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(
        sseResponse([
          'data: {"status":"Đang tìm người..."}\n\n',
          'data: {"token":"Xin chào"}\n\n',
          'data: {"token":" **bạn**"}\n\n',
          'data: {"token":" hôm nay."}\n\n',
          'data: {"done":true,"threadId":"thread-1"}\n\n'
        ])
      )
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.inputText = 'xin chào'
    await wrapper.vm.sendMessage()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [url, opts] = fetchMock.mock.calls[0]
    expect(url).toContain('/api/ai-chat/stream')
    expect(opts.method).toBe('POST')
    expect(opts.headers.Authorization).toBe('Bearer ')
    const body = JSON.parse(opts.body)
    expect(body.message).toBe('xin chào')
    expect(body.threadId).toBeUndefined()

    const ai = wrapper.vm.messages.find((m) => m.role === 'ai')
    expect(ai).toBeTruthy()
    expect(ai.text).toContain('Xin chào')
    expect(ai.text).toContain('bạn')
    expect(ai.text).toContain('hôm nay')
    expect(wrapper.vm.streaming).toBe(false)
    expect(wrapper.vm.threadId).toBe('thread-1')
  })

  it('shows an editable email draft composer when the agent emits a draft event', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(
        sseResponse([
          'data: {"status":"Đang soạn email..."}\n\n',
          'data: {"draft":{"id":7,"to":["c@company.local"],"subject":"Đơn xin nghỉ","body":"Kính gửi anh Hùng,\\n\\nEm xin nghỉ 1 ngày."}}\n\n',
          'data: {"done":true,"threadId":"abc-123"}\n\n'
        ])
      )
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.inputText = 'soạn email xin nghỉ'
    await wrapper.vm.sendMessage()

    expect(wrapper.vm.drafts.length).toBe(1)
    expect(wrapper.vm.drafts[0].subject).toBe('Đơn xin nghỉ')
    expect(wrapper.vm.threadId).toBe('abc-123')
    expect(wrapper.find('.draft-card').exists()).toBe(true)
    expect(wrapper.find('.draft-textarea').element.value).toContain('Kính gửi anh Hùng')
  })

  it('sendDraft calls the send endpoint with the draft content', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(JSON.stringify({ success: true, message: 'Đã gửi.' }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mountChat()
    const d = { id: 7, to: ['a@x.vn'], subject: 'S', body: 'B', sending: false, sent: false, refineMsg: '' }
    wrapper.vm.drafts.push(d)
    await wrapper.vm.sendDraft(d)

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [url, opts] = fetchMock.mock.calls[0]
    expect(url).toContain('/api/ai-chat/send-draft')
    expect(JSON.parse(opts.body).draftId).toBe(7)
    expect(d.sent).toBe(true)
  })

  it('renders markdown safely (escapes raw HTML)', () => {
    const wrapper = mountChat()
    const html = wrapper.vm.renderMarkdown('Xin chào **đậm** và `code`\n\n<script>alert(1)</script>')
    expect(html).toContain('<strong>đậm</strong>')
    expect(html).toContain('<code>code</code>')
    expect(html).not.toContain('<script>')
  })

  it('shows an error message when the stream returns an error event', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(sseResponse(['data: {"error":"AI chưa được cấu hình."}\n\n']))
    )
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.inputText = 'hỏi gì đó'
    await wrapper.vm.sendMessage()
    const ai = wrapper.vm.messages.find((m) => m.role === 'ai')
    expect(ai.error).toBe(true)
  })

  it('stopStream aborts the fetch', async () => {
    const abortSpy = { abort: vi.fn() }
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise(() => {})))
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.controller = abortSpy
    wrapper.vm.streaming = true
    wrapper.vm.stopStream()
    expect(abortSpy.abort).toHaveBeenCalled()
  })

  it('clearChat empties messages and stops streaming', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    wrapper.vm.messages.push({ id: '1', role: 'user', text: 'a', ts: 1 })
    wrapper.vm.clearChat()
    expect(wrapper.vm.messages.length).toBe(0)
  })

  it('routes on internal link click and closes the chat', async () => {
    const wrapper = mountChat()
    wrapper.vm.openChat()
    const anchor = document.createElement('a')
    anchor.setAttribute('href', '/gate-transit-monitor')
    const event = { target: anchor, preventDefault: vi.fn() }
    wrapper.vm.handleMsgClick(event)
    expect(event.preventDefault).toHaveBeenCalled()
    await new Promise((r) => setTimeout(r, 0))
    expect(router.currentRoute.value.path).toBe('/gate-transit-monitor')
    expect(wrapper.vm.chatOpen).toBe(false)
  })

  it('does not push a route for external links', async () => {
    const wrapper = mountChat()
    wrapper.vm.openChat()
    const pushSpy = vi.spyOn(router, 'push')
    const anchor = document.createElement('a')
    anchor.setAttribute('href', 'https://external.example/x')
    wrapper.vm.handleMsgClick({ target: anchor, preventDefault: vi.fn() })
    expect(pushSpy).not.toHaveBeenCalled()
  })

  it('drags the fab and updates its style position', () => {
    const wrapper = mountChat()
    wrapper.vm.startDrag({ clientX: 100, clientY: 100, currentTarget: { setPointerCapture: vi.fn() } })
    expect(wrapper.vm.dragState.active).toBe(true)
    wrapper.vm.onMove({ clientX: 200, clientY: 160 })
    expect(wrapper.vm.dragState.active).toBe(false)
    expect(wrapper.vm.fabStyle.left).toContain('px')
    wrapper.vm.onUp()
    expect(wrapper.vm.dragState.active).toBe(false)
  })

  it('closes the chat on a route change', async () => {
    const wrapper = mountChat()
    await wrapper.find('.chat-fab').trigger('click')
    expect(wrapper.vm.chatOpen).toBe(true)
    await router.push('/gate-transit-monitor')
    await router.isReady()
    expect(wrapper.vm.chatOpen).toBe(false)
  })

  it('cleans up event listeners on unmount', () => {
    const removeSpy = vi.fn()
    vi.stubGlobal('removeEventListener', removeSpy)
    const wrapper = mountChat()
    wrapper.unmount()
    expect(removeSpy).toHaveBeenCalled()
  })
})
