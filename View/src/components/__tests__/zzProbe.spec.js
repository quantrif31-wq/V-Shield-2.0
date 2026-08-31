import { describe, it, expect } from 'vitest'

describe('jsdom srcObject probe', () => {
  it('checks identity of srcObject assignment', () => {
    const el = document.createElement('video')
    const stream = {}
    el.srcObject = stream
    console.log('PROBE srcObject === stream:', el.srcObject === stream)
    console.log('PROBE typeof srcObject:', typeof el.srcObject, '| getter?', Object.getOwnPropertyDescriptor(HTMLMediaElement.prototype, 'srcObject'))
    expect(true).toBe(true)
  })
})