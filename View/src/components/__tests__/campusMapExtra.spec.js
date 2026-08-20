import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

const CampusMapElement = (await import('../campus-map/CampusMapElement.vue')).default
const CampusMapCanvas = (await import('../campus-map/CampusMapCanvas.vue')).default
const CampusAssetInspector = (await import('../campus-map/CampusAssetInspector.vue')).default

const firePointer = (target, type, opts = {}) => {
    const hasPointer = typeof window.PointerEvent === 'function'
    const Ctor = hasPointer ? window.PointerEvent : window.MouseEvent
    const event = new Ctor(type, {
        bubbles: true,
        cancelable: true,
        clientX: opts.clientX ?? 0,
        clientY: opts.clientY ?? 0,
    })
    target.dispatchEvent(event)
}

const baseItem = (overrides = {}) => ({
    gateId: 1,
    gateName: 'Cổng A',
    location: 'Lô 1',
    status: 'Normal',
    stats: { cameraCount: 2 },
    layout: { x: 10, y: 20, w: 200, h: 100, zIndex: 2, isVisible: true, isLocked: false, color: '#0f766e', icon: 'gate' },
    ...overrides,
})

describe('CampusMapElement', () => {
    it('renders with computed style, label and counts', () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem() } })
        expect(wrapper.text()).toContain('Cổng A')
        expect(wrapper.text()).toContain('Lô 1')
        expect(wrapper.text()).toContain('2 camera')
        expect(wrapper.text()).toContain('Bình thường')
        expect(wrapper.get('.map-element').attributes('style')).toContain('left: 10px')
        expect(wrapper.get('.map-element').attributes('style')).toContain('z-index: 2')
        expect(wrapper.get('.element-icon').text()).toBe('GATE')
    })

    it('maps status label variants', async () => {
        const cases = [
            [{ status: 'Active' }, 'Đang hoạt động'],
            [{ status: 'Warning' }, 'Cảnh báo'],
            [{ status: 'Offline' }, 'Offline'],
            [{ status: 'Unknown' }, 'Unknown'],
        ]
        for (const [over, expected] of cases) {
            const wrapper = mount(CampusMapElement, { props: { item: baseItem(over) } })
            expect(wrapper.text()).toContain(expected)
            wrapper.unmount()
        }
    })

    it('renders camera and door icon glyphs', async () => {
        const cam = mount(CampusMapElement, { props: { item: baseItem({ layout: { ...baseItem().layout, icon: 'Camera A' } }) } })
        expect(cam.get('.element-icon').text()).toBe('CAM')
        const door = mount(CampusMapElement, { props: { item: baseItem({ layout: { ...baseItem().layout, icon: 'door-x' } }) } })
        expect(door.get('.element-icon').text()).toBe('DOOR')
    })

    it('emits select on click', async () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem() } })
        await wrapper.get('.map-element').trigger('click')
        expect(wrapper.emitted('select')).toEqual([[1]])
    })

    it('emits drag-start when editable and unlocked', async () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem(), editable: true } })
        firePointer(wrapper.get('.map-element').element, 'pointerdown', { clientX: 5, clientY: 6 })
        expect(wrapper.emitted('drag-start')).toBeTruthy()
        expect(wrapper.emitted('drag-start')[0][0]).toMatchObject({ gateId: 1 })
    })

    it('does not emit drag-start when locked', async () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem({ layout: { ...baseItem().layout, isLocked: true } }), editable: true } })
        firePointer(wrapper.get('.map-element').element, 'pointerdown', { clientX: 5 })
        expect(wrapper.emitted('drag-start')).toBeFalsy()
        expect(wrapper.find('.resize-handle').exists()).toBe(false)
    })

    it('does not emit drag-start when not editable', async () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem() } })
        firePointer(wrapper.get('.map-element').element, 'pointerdown', { clientX: 5 })
        expect(wrapper.emitted('drag-start')).toBeFalsy()
    })

    it('emits resize-start from the resize handle', async () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem(), editable: true } })
        firePointer(wrapper.get('.resize-handle').element, 'pointerdown', { clientX: 3, clientY: 4 })
        expect(wrapper.emitted('resize-start')).toBeTruthy()
        expect(wrapper.emitted('resize-start')[0][0]).toMatchObject({ gateId: 1 })
    })

    it('renders nothing when element is hidden', () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem({ layout: { ...baseItem().layout, isVisible: false } }) } })
        expect(wrapper.find('.map-element').exists()).toBe(false)
    })

    it('applies selected and locked classes', () => {
        const wrapper = mount(CampusMapElement, { props: { item: baseItem({ layout: { ...baseItem().layout, isLocked: true } }), selected: true } })
        expect(wrapper.get('.map-element').classes()).toContain('selected')
        expect(wrapper.get('.map-element').classes()).toContain('locked')
    })
})

describe('CampusMapCanvas', () => {
    const items = [
        baseItem({ gateId: 1, layout: { ...baseItem().layout, zIndex: 1 } }),
        baseItem({ gateId: 2, gateName: 'Cổng B', layout: { ...baseItem().layout, x: 900, y: 800, zIndex: 5 } }),
    ]

    it('shows empty state when no items', () => {
        const wrapper = mount(CampusMapCanvas)
        expect(wrapper.text()).toContain('Chưa có cổng nào')
    })

    it('renders sorted elements and sizes the grid', () => {
        const wrapper = mount(CampusMapCanvas, { props: { items } })
        const elements = wrapper.findAllComponents({ name: 'CampusMapElement' })
        expect(elements.length).toBe(2)
        expect(wrapper.get('.canvas-grid').attributes('style')).toContain('width')
        expect(wrapper.get('.canvas-grid').attributes('style')).toContain('height')
    })

    it('emits select from child click', async () => {
        const wrapper = mount(CampusMapCanvas, { props: { items } })
        await wrapper.findAllComponents(CampusMapElement)[0].get('.map-element').trigger('click')
        expect(wrapper.emitted('select')).toEqual([[1]])
    })

    it('handles drag and emits update-layout on pointermove', async () => {
        const wrapper = mount(CampusMapCanvas, { props: { items, editable: true } })
        firePointer(wrapper.findAllComponents(CampusMapElement)[0].get('.map-element').element, 'pointerdown', { clientX: 100, clientY: 100 })
        firePointer(window, 'pointermove', { clientX: 130, clientY: 110 })
        expect(wrapper.emitted('update-layout')).toEqual([[{ gateId: 1, x: 40, y: 30 }]])
        firePointer(window, 'pointerup')
    })

    it('handles resize and clamps to minimum size', async () => {
        const wrapper = mount(CampusMapCanvas, { props: { items, editable: true } })
        firePointer(wrapper.findAllComponents(CampusMapElement)[0].get('.resize-handle').element, 'pointerdown', { clientX: 0, clientY: 0 })
        firePointer(window, 'pointermove', { clientX: -1000, clientY: -1000 })
        expect(wrapper.emitted('update-layout')).toEqual([[{ gateId: 1, w: 120, h: 70 }]])
        firePointer(window, 'pointerup')
    })

    it('unbinds pointer listeners on unmount', async () => {
        const removeSpy = vi.spyOn(window, 'removeEventListener')
        const wrapper = mount(CampusMapCanvas, { props: { items, editable: true } })
        firePointer(wrapper.findAllComponents(CampusMapElement)[0].get('.map-element').element, 'pointerdown', { clientX: 0 })
        wrapper.unmount()
        expect(removeSpy).toHaveBeenCalled()
        removeSpy.mockRestore()
    })

    it('fitToContent scrolls the viewport', async () => {
        const scrollTo = vi.fn()
        Object.defineProperty(Element.prototype, 'scrollTo', { configurable: true, writable: true, value: scrollTo })
        const wrapper = mount(CampusMapCanvas, { props: { items } })
        wrapper.vm.fitToContent()
        expect(scrollTo).toHaveBeenCalled()
        delete Element.prototype.scrollTo
    })
})

describe('CampusAssetInspector', () => {
    it('shows overview summary when no asset selected', () => {
        const wrapper = mount(CampusAssetInspector, {
            props: { summary: { siteCount: 3, objectCount: 12, activeGateCount: 5, warningGateCount: 1, offlineCameraCount: 2 } },
        })
        expect(wrapper.text()).toContain('Toàn cảnh')
        expect(wrapper.text()).toContain('Tổng quan 3D')
        expect(wrapper.text()).toContain('3')
        expect(wrapper.text()).toContain('cảnh báo')
        expect(wrapper.text()).toContain('2')
    })

    it('shows selected Site asset with metrics', () => {
        const wrapper = mount(CampusAssetInspector, {
            props: {
                updatedAt: '2026-01-01T00:00:00Z',
                selectedAsset: {
                    label: 'Khu công nghệ',
                    siteCode: 'TC',
                    siteName: 'Công nghệ',
                    objectType: 'Site',
                    metrics: { buildings: 4, gates: 8 },
                    floors: 3,
                    dimensions: { width: 100, length: 200, height: 12 },
                    properties: { zone: 'A', level: 'B1' },
                },
            },
        })
        expect(wrapper.text()).toContain('Đang xem')
        expect(wrapper.text()).toContain('Khu')
        expect(wrapper.text()).toContain('Khu công nghệ')
        expect(wrapper.text()).toContain('tòa nhà')
        expect(wrapper.text()).toContain('tầng')
        expect(wrapper.text()).toContain('100m x 200m x 12m')
        expect(wrapper.text()).toContain('Vùng')
        expect(wrapper.get('.asset-orb').classes()).toContain('site')
    })

    it('shows gate asset with status coloring and recent access', () => {
        const wrapper = mount(CampusAssetInspector, {
            props: {
                selectedAsset: {
                    label: 'Cổng Bắc',
                    siteCode: 'N',
                    siteName: 'Bắc',
                    objectType: 'GateMarker',
                    gate: { status: 'Warning', cameraCount: 6, offlineCameraCount: 1, recentAccessCount: 42, lastAccessAt: '2026-01-02T10:30:00Z' },
                },
            },
        })
        expect(wrapper.text()).toContain('Cổng / lane')
        expect(wrapper.get('.asset-orb').classes()).toContain('gate')
        expect(wrapper.get('.signal-pill strong').attributes('style')).toContain('rgb(245, 158, 11)')
        expect(wrapper.text()).toContain('6 / 1 mất kết nối')
        expect(wrapper.text()).toContain('42')
    })

    it('falls back to object type and generic orb', () => {
        const wrapper = mount(CampusAssetInspector, {
            props: { selectedAsset: { label: 'X', siteCode: '-', siteName: '-', objectType: 'ParkingArea' } },
        })
        expect(wrapper.text()).toContain('Bãi đỗ xe')
        expect(wrapper.get('.asset-orb').classes()).toContain('generic')
    })
})