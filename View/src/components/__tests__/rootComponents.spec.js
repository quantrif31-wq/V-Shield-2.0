import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

const HelloWorld = (await import('../HelloWorld.vue')).default
const ProjectTeamOverview = (await import('../ProjectTeamOverview.vue')).default
const StreamPreview = (await import('../StreamPreview.vue')).default

describe('HelloWorld', () => {
  it('increments the counter on click', async () => {
    const wrapper = mount(HelloWorld)
    const initial = wrapper.text()
    await wrapper.find('button').trigger('click')
    expect(wrapper.text()).not.toBe(initial)
  })
})

describe('ProjectTeamOverview', () => {
  it('renders the project team', () => {
    const wrapper = mount(ProjectTeamOverview)
    expect(wrapper.text()).toContain('V-Shield')
  })
})

describe('StreamPreview', () => {
  it('renders an empty-state message without a valid url', async () => {
    const wrapper = mount(StreamPreview, { props: { url: '', label: 'Cam A' } })
    await flushPromises()
    expect(wrapper.exists()).toBe(true)
  })

  it('renders for an http stream', async () => {
    const wrapper = mount(StreamPreview, { props: { url: 'http://10.0.0.5/video', label: 'Cam A' } })
    await flushPromises()
    expect(wrapper.exists()).toBe(true)
  })
})
