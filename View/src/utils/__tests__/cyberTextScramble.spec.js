import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TextScramble } from '../cyberTextScramble'

function makeEl(innerText) {
  let html = innerText || ''
  return {
    innerText,
    textContent: innerText,
    get innerHTML() { return html },
    set innerHTML(v) { html = v },
  }
}

beforeEach(() => {
  vi.stubGlobal('requestAnimationFrame', (cb) => setTimeout(cb, 0))
  vi.stubGlobal('cancelAnimationFrame', () => {})
  vi.spyOn(Math, 'random').mockReturnValue(0.9)
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('TextScramble', () => {
  it('binds update to the instance and stores the element', () => {
    const ts = new TextScramble(makeEl('hi'))
    expect(ts.el).toBeTruthy()
    expect(ts.update).toBeInstanceOf(Function)
    expect(ts.chars).toContain('#')
  })

  it('resolves immediately when there is no element', async () => {
    const ts = new TextScramble(null)
    await expect(ts.setText('new text')).resolves.toBeUndefined()
  })

  it('scrambles from old text to the new text and resolves', async () => {
    const el = makeEl('hello')
    const ts = new TextScramble(el)
    await ts.setText('world')
    expect(ts.queue).toHaveLength(5)
    expect(el.innerHTML).toBe('world')
  })

  it('grows output when the new text is longer than the old', async () => {
    const el = makeEl('ab')
    const ts = new TextScramble(el)
    await ts.setText('xylophone')
    expect(ts.queue).toHaveLength(9)
    expect(el.innerHTML).toBe('xylophone')
  })

  it('shrinks output when the new text is shorter than the old', async () => {
    const el = makeEl('toolong')
    const ts = new TextScramble(el)
    await ts.setText('abc')
    expect(ts.queue).toHaveLength(7)
    expect(el.innerHTML).toBe('abc')
  })

  it('uses textContent as a fallback for the old text', async () => {
    const el = makeEl('')
    el.textContent = 'zzz'
    const ts = new TextScramble(el)
    await ts.setText('abc')
    expect(ts.queue).toHaveLength(3)
    expect(el.innerHTML).toBe('abc')
  })

  it('randomChar returns a single character from the charset', () => {
    const ts = new TextScramble(makeEl('x'))
    expect(ts.chars).toContain(ts.randomChar())
  })
})
