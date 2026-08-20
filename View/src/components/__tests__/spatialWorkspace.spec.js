import { defineComponent } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/campusMapApi', () => ({
    getCampusScene3D: vi.fn(),
    getCampusMapRealtime: vi.fn(),
    createCampusSceneObject: vi.fn(),
    updateCampusSceneObject: vi.fn(),
    deleteCampusSceneObject: vi.fn(),
}))
vi.mock('../../services/enterpriseSecurityApi', () => ({
    enterpriseApi: {
        getSiteMaps: vi.fn(),
        getTopology: vi.fn(),
        getMapPlacements: vi.fn(),
        updateSiteMap: vi.fn(),
        createSiteMap: vi.fn(),
        deleteSiteMap: vi.fn(),
        updateMapPlacement: vi.fn(),
        addMapPlacement: vi.fn(),
        deleteMapPlacement: vi.fn(),
    },
}))

const campusMapApi = await import('../../services/campusMapApi')
const { enterpriseApi } = await import('../../services/enterpriseSecurityApi')
const Workspace = (await import('../site-hierarchy/SpatialInfrastructureWorkspace.vue')).default

const canvasStub = defineComponent({
    name: 'Campus3DCanvas',
    setup() {
        return { focusSite: vi.fn(), fitToContent: vi.fn() }
    },
    template: '<div class="stub-canvas"><button class="inspect-btn" @click="$emit(\'inspect-object\', { label: \'Nhà A\', objectType: \'Building\' })">inspect</button></div>',
    emits: ['inspect-object'],
})

const siteOptions = [
    { siteId: 1, name: 'Khu A' },
    { siteId: 2, name: 'Khu B' },
]

const sceneData = {
    sites: [
        {
            siteId: 1,
            objects: [
                { id: 10, label: 'Nhà A', type: 'Building', siteId: 1, posX: 1, posY: 2, posZ: 0, width: 24, length: 16, height: 8, floors: 2, isActive: true, properties: '{"zone":"z1"}' },
                { id: 11, label: 'Cổng B', type: 'GateMarker', siteId: 1, posX: 3, posY: 4, posZ: 0, width: 20, length: 10, height: 3, floors: 1, isActive: false },
            ],
        },
        {
            siteId: 2,
            objects: [
                { id: 12, label: 'Bãi xe C', type: 'ParkingArea', siteId: 2, posX: 0, posY: 0, posZ: 0, width: 40, length: 30, height: 0.5, floors: 1, isActive: true },
            ],
        },
    ],
    gates: [{ gateId: 1, status: 'Normal' }],
}

const realtimeData = {
    gates: [{ gateId: 1, status: 'Active' }],
    recentEvents: [{ id: 1, title: 'Vào' }],
}

beforeEach(() => {
    vi.clearAllMocks()
    campusMapApi.getCampusScene3D.mockResolvedValue({ data: sceneData })
    campusMapApi.getCampusMapRealtime.mockResolvedValue({ data: realtimeData })
    campusMapApi.createCampusSceneObject.mockResolvedValue({ data: { id: 99 } })
    campusMapApi.updateCampusSceneObject.mockResolvedValue({})
    campusMapApi.deleteCampusSceneObject.mockResolvedValue({})
    enterpriseApi.getSiteMaps.mockResolvedValue({
        data: [{ siteMapId: 5, name: 'Tầng trệt', coordinateSystem: 'Normalized', assetReference: 'draw-1', isActive: true }],
    })
    enterpriseApi.getTopology.mockResolvedValue({
        data: [{ securityDeviceId: 20, name: 'Cam 1', deviceType: 'Camera', siteId: 1 }],
    })
    enterpriseApi.getMapPlacements.mockResolvedValue({
        data: [{ mapDevicePlacementId: 30, securityDeviceId: 20, securityDeviceName: 'Cam 1', iconType: 'Device', x: 10, y: 20 }],
    })
    enterpriseApi.createSiteMap.mockResolvedValue({ data: { siteMapId: 6 } })
})

const mountWorkspace = async (opts = {}) => {
    const wrapper = mount(Workspace, {
        props: { siteOptions, ...opts },
        global: { stubs: { Campus3DCanvas: canvasStub, transition: false } },
    })
    await flushPromises()
    return wrapper
}

describe('SpatialInfrastructureWorkspace', () => {
    it('loads scene, realtime and stats on mount', async () => {
        const wrapper = await mountWorkspace()
        expect(campusMapApi.getCampusScene3D).toHaveBeenCalled()
        expect(campusMapApi.getCampusMapRealtime).toHaveBeenCalled()
        expect(wrapper.find('.spatial-stat-grid').text()).toContain('2')
        expect(wrapper.findAll('.object-item').length).toBe(3)
        expect(wrapper.text()).toContain('Khu A')
    })

    it('shows workspace error when scene load fails', async () => {
        campusMapApi.getCampusScene3D.mockRejectedValue(new Error('boom'))
        const wrapper = await mountWorkspace()
        expect(wrapper.text()).toContain('Không thể tải dữ liệu hạ tầng không gian.')
    })

    it('filters scene sites by selected site', async () => {
        const wrapper = await mountWorkspace()
        const select = wrapper.get('select.form-select')
        await select.setValue(2)
        await flushPromises()
        expect(wrapper.findAll('.object-item').length).toBe(1)
        expect(wrapper.text()).toContain('Bãi xe C')
    })

    it('filters objects by search text', async () => {
        const wrapper = await mountWorkspace()
        const search = wrapper.findAll('input.form-input')[0]
        await search.setValue('cổng')
        expect(wrapper.findAll('.object-item').length).toBe(1)
        expect(wrapper.text()).toContain('Cổng B')
    })

    it('filters objects by type', async () => {
        const wrapper = await mountWorkspace()
        const selects = wrapper.findAll('select.form-select')
        const typeSelect = selects[1]
        await typeSelect.setValue('GateMarker')
        expect(wrapper.findAll('.object-item').length).toBe(1)
        expect(wrapper.text()).toContain('Cổng B')
    })

    it('selects an object and populates the editor form', async () => {
        const wrapper = await mountWorkspace()
        const item = wrapper.findAll('.object-item').find((b) => b.text().includes('Nhà A'))
        await item.trigger('click')
        expect(wrapper.text()).toContain('Lưu đối tượng')
        expect(wrapper.text()).toContain('Xóa đối tượng')
        const nudge = wrapper.findAll('button').find((b) => b.text().includes('X +2'))
        await nudge.trigger('click')
        expect(wrapper.find(`input[placeholder="Tòa nhà chính, cổng xe tải..."]`).exists()).toBe(true)
    })

    it('validates missing label on save', async () => {
        const wrapper = await mountWorkspace()
        const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Tạo đối tượng'))
        await saveBtn.trigger('click')
        await flushPromises()
        expect(campusMapApi.createCampusSceneObject).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain('Khu vực và nhãn đối tượng là bắt buộc.')
    })

    it('creates a new object after editing label', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.find('input[placeholder="Tòa nhà chính, cổng xe tải..."]').setValue('Nhà mới')
        const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Tạo đối tượng'))
        await saveBtn.trigger('click')
        await flushPromises()
        expect(campusMapApi.createCampusSceneObject).toHaveBeenCalled()
        expect(wrapper.text()).toContain('Đã tạo đối tượng 3D.')
    })

    it('updates an existing object', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.findAll('.object-item').find((b) => b.text().includes('Nhà A')).trigger('click')
        await wrapper.find('input[placeholder="Tòa nhà chính, cổng xe tải..."]').setValue('Nhà A v2')
        const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Lưu đối tượng'))
        await saveBtn.trigger('click')
        await flushPromises()
        expect(campusMapApi.updateCampusSceneObject).toHaveBeenCalledWith(10, expect.objectContaining({ label: 'Nhà A v2' }))
        expect(wrapper.text()).toContain('Đã cập nhật đối tượng 3D.')
    })

    it('deletes an object', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.findAll('.object-item').find((b) => b.text().includes('Nhà A')).trigger('click')
        const delBtn = wrapper.findAll('button').find((b) => b.text().includes('Xóa đối tượng'))
        await delBtn.trigger('click')
        await flushPromises()
        expect(campusMapApi.deleteCampusSceneObject).toHaveBeenCalledWith(10)
        expect(wrapper.text()).toContain('Đã xóa đối tượng 3D.')
    })

    it('selects a map and loads its placements', async () => {
        const wrapper = await mountWorkspace()
        const mapItem = wrapper.findAll('.map-item')[0]
        await mapItem.trigger('click')
        await flushPromises()
        expect(enterpriseApi.getMapPlacements).toHaveBeenCalledWith(5)
        expect(wrapper.text()).toContain('Cam 1')
    })

    it('creates a site map', async () => {
        const wrapper = await mountWorkspace()
        const nameInput = wrapper.find('input[placeholder="Tầng trệt, bản đồ lối đi trong khuôn viên..."]')
        await nameInput.setValue('Tầng 2')
        const createBtn = wrapper.findAll('button').find((b) => b.text().includes('Tạo bản đồ'))
        await createBtn.trigger('click')
        await flushPromises()
        expect(enterpriseApi.createSiteMap).toHaveBeenCalledWith(expect.objectContaining({ name: 'Tầng 2' }))
    })

    it('skips saving a map without a name', async () => {
        const wrapper = await mountWorkspace()
        const createBtn = wrapper.findAll('button').find((b) => b.text().includes('Tạo bản đồ'))
        await createBtn.trigger('click')
        await flushPromises()
        expect(enterpriseApi.createSiteMap).not.toHaveBeenCalled()
    })

    it('adds a placement to the selected map', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.findAll('.map-item')[0].trigger('click')
        await flushPromises()
        const deviceSelect = wrapper.findAll('select.form-select').find((s) =>
            Array.from(s.element.options).some((o) => o.text === 'Cam 1 • Camera')
        )
        await deviceSelect.setValue('20')
        const addBtn = wrapper.findAll('button').find((b) => b.text().includes('Thêm vị trí'))
        await addBtn.trigger('click')
        await flushPromises()
        expect(enterpriseApi.addMapPlacement).toHaveBeenCalledWith(5, expect.objectContaining({ securityDeviceId: 20 }))
    })

    it('focuses the selected site on the canvas', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.get('select.form-select').setValue(1)
        await flushPromises()
        const focusBtn = wrapper.findAll('button').find((b) => b.text().includes('Tập trung khu vực'))
        await focusBtn.trigger('click')
        const canvas = wrapper.findComponent(canvasStub)
        expect(canvas.vm.focusSite).toHaveBeenCalledWith(1)
    })

    it('fits the scene when no site selected', async () => {
        const wrapper = await mountWorkspace()
        const fitBtn = wrapper.findAll('button').find((b) => b.text().includes('Vừa khung'))
        await fitBtn.trigger('click')
        const canvas = wrapper.findComponent(canvasStub)
        expect(canvas.vm.fitToContent).toHaveBeenCalled()
    })

    it('inspects an object from the canvas payload', async () => {
        const wrapper = await mountWorkspace()
        await wrapper.get('.inspect-btn').trigger('click')
        await flushPromises()
        expect(wrapper.findAll('button').find((b) => b.text().includes('Xóa đối tượng')).exists()).toBe(true)
    })
})