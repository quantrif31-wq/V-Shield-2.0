import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

const GuideHero = (await import('../guide/GuideHero.vue')).default
const GuidePageCard = (await import('../guide/GuidePageCard.vue')).default
const GuideStepList = (await import('../guide/GuideStepList.vue')).default

const sampleSteps = [
  { title: 'Mở camera', moTa: 'Bật camera để quan sát.', nhapGi: 'Tên cửa', bamGi: 'Nút Mở', ketQua: 'Cửa mở thành công' },
  { title: 'Xác nhận', moTa: 'Xác nhận hành động.', ketQua: 'Đã lưu' }
]

const samplePage = {
  label: 'Giám sát',
  icon: '📷',
  mucDich: 'Giám sát camera và lưu trữ video trong toàn khu vực.',
  roles: ['Admin', 'Bảo vệ'],
  steps: sampleSteps,
  thanhPhan: [
    { ten: 'Camera', yNghia: 'Nguồn video', ghiChu: 'Bắt buộc' },
    { ten: 'Lưu trữ', yNghia: 'Nơi chứa video', ghiChu: '' }
  ]
}

const longPage = {
  label: 'Giám sát',
  icon: '📷',
  mucDich: 'Giám sát camera và lưu trữ video trong toàn khu vực một cách liên tục, ghi nhận mọi sự kiện bất thường và phục vụ công tác soi xét sau sự việc cũng như hỗ trợ điều tra khi cần thiết.',
  roles: ['Admin', 'Bảo vệ'],
  steps: sampleSteps,
  thanhPhan: [
    { ten: 'Camera', yNghia: 'Nguồn video', ghiChu: 'Bắt buộc' },
    { ten: 'Lưu trữ', yNghia: 'Nơi chứa video', ghiChu: '' }
  ]
}

describe('GuideHero', () => {
  it('renders title and default role chips', () => {
    const wrapper = mount(GuideHero)
    expect(wrapper.text()).toContain('V-Shield Security Platform')
    expect(wrapper.findAll('.role-chip').length).toBe(5)
    expect(wrapper.find('.guide-meta').text()).toContain('Phiên bản 2.0')
  })

  it('marks active role chip and displays total pages', () => {
    const wrapper = mount(GuideHero, { props: { activeRole: 'Admin', totalPages: 42 } })
    expect(wrapper.find('.role-chip.active').text()).toContain('Admin')
    expect(wrapper.text()).toContain('42 trang hướng dẫn')
  })

  it('emits select-role when a chip is clicked', async () => {
    const wrapper = mount(GuideHero)
    await wrapper.findAll('.role-chip')[2].trigger('click')
    expect(wrapper.emitted('select-role')).toBeTruthy()
    expect(wrapper.emitted('select-role')[0][0]).toBe('Bảo vệ')
  })
})

describe('GuideStepList', () => {
  it('renders steps with numbers and inputs/clicks/results', () => {
    const wrapper = mount(GuideStepList, { props: { steps: sampleSteps, color: '#3b82f6' } })
    expect(wrapper.findAll('.step-item').length).toBe(2)
    expect(wrapper.text()).toContain('Mở camera')
    expect(wrapper.text()).toContain('Tên cửa')
    expect(wrapper.text()).toContain('Nút Mở')
    expect(wrapper.text()).toContain('Cửa mở thành công')
  })

  it('renders empty list without errors', () => {
    const wrapper = mount(GuideStepList)
    expect(wrapper.findAll('.step-item').length).toBe(0)
  })
})

describe('GuidePageCard', () => {
  it('renders collapsed card with truncated description and roles', () => {
    const wrapper = mount(GuidePageCard, { props: { page: longPage } })
    expect(wrapper.text()).toContain('Giám sát')
    expect(wrapper.find('.card-desc').text()).toContain('...')
    expect(wrapper.findAll('.mini-role').length).toBe(2)
    expect(wrapper.find('.card-body').exists()).toBe(false)
  })

  it('shows read badge when read', () => {
    const wrapper = mount(GuidePageCard, { props: { page: samplePage, isRead: true } })
    expect(wrapper.classes()).toContain('read')
    expect(wrapper.find('.read-badge').exists()).toBe(true)
  })

  it('renders expanded body with steps and component table', () => {
    const wrapper = mount(GuidePageCard, { props: { page: longPage, isOpen: true } })
    expect(wrapper.find('.card-body').exists()).toBe(true)
    expect(wrapper.findAll('.step-item').length).toBe(2)
    expect(wrapper.findAll('.component-table tbody tr').length).toBe(2)
    expect(wrapper.text()).toContain('Bắt buộc')
  })

  it('emits toggle when header is clicked', async () => {
    const wrapper = mount(GuidePageCard, { props: { page: samplePage } })
    await wrapper.find('.card-header').trigger('click')
    expect(wrapper.emitted('toggle')).toBeTruthy()
  })
})