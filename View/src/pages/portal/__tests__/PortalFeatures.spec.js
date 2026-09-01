import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalFeatures from '../PortalFeatures.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: { playClick: vi.fn() }
}))

import { mechaAudio } from '../../../utils/portalAudio'

describe('PortalFeatures', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('mounts and shows all features by default', () => {
    const wrapper = mount(PortalFeatures)
    expect(wrapper.vm.features.length).toBe(6)
    expect(wrapper.findAll('.mecha-hud-bracket').length).toBeGreaterThanOrEqual(6)
    expect(wrapper.text()).toContain('AI FACE ID')
  })

  it('filters features by AI category', async () => {
    const wrapper = mount(PortalFeatures)
    const aiBtn = wrapper.findAll('button').find(b => b.text().includes('AI SINH TRẮC'))
    await aiBtn.trigger('click')
    expect(wrapper.vm.activeCategory).toBe('ai')
    const filtered = wrapper.vm.filteredFeatures()
    expect(filtered.length).toBe(1)
    expect(mechaAudio.playClick).toHaveBeenCalled()
  })

  it('switches back to all features', async () => {
    const wrapper = mount(PortalFeatures)
    const allBtn = wrapper.findAll('button').find(b => b.text().includes('TẤT CẢ GIẢI PHÁP'))
    await allBtn.trigger('click')
    expect(wrapper.vm.activeCategory).toBe('all')
    expect(wrapper.vm.filteredFeatures().length).toBe(6)
  })

  it('filters by barrier and sync and security categories', async () => {
    const wrapper = mount(PortalFeatures)
    for (const [label, count] of [['KIỂM SOÁT RA VÀO', 2], ['ĐỒNG BỘ HYBRID', 1], ['BẢO MẬT & UEBA', 2]]) {
      const btn = wrapper.findAll('button').find(b => b.text().includes(label))
      await btn.trigger('click')
      expect(wrapper.vm.filteredFeatures().length).toBe(count)
    }
  })
})
