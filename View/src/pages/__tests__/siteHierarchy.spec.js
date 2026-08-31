import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  api: {},
}))

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: mocks.api,
}))
vi.mock('../../services/accessLogApi', () => ({ getSystemAuditLogs: vi.fn() }))

const accessLogApi = await import('../../services/accessLogApi')
const SiteHierarchy = (await import('../SiteHierarchy.vue')).default

const sharedStubs = {
  SpatialInfrastructureWorkspace: true,
  ImportModal: {
    name: 'ImportModal',
    props: ['entityType', 'entityDisplayName'],
    template: '<div class="import-stub"><button class="stub-close" @click="$emit(\'close\')">đóng</button><button class="stub-done" @click="$emit(\'import-complete\', { ok: 1 })">xong</button></div>',
  },
  ExportModal: {
    name: 'ExportModal',
    props: ['entityType', 'entityDisplayName', 'availableColumns'],
    template: '<div class="export-stub"><button class="stub-close" @click="$emit(\'close\')">đóng</button></div>',
  },
}

const companyData = {
  companyId: 'c1',
  name: 'Công ty A',
  code: 'CA',
  isActive: true,
  sites: [
    {
      siteId: 's1',
      name: 'Khu A',
      code: 'SA',
      address: '123 Đường X',
      timeZoneId: 'Asia/Ho_Chi_Minh',
      isActive: true,
      buildings: [
        {
          buildingId: 'b1',
          name: 'Tòa A',
          code: 'BA',
          isActive: true,
          floors: [
            { facilityFloorId: 'f1', name: 'Tầng 1', code: 'F1', sortOrder: 1, isActive: true },
            { facilityFloorId: 'f2', name: 'Tầng 2', code: '', sortOrder: 2 },
          ],
        },
      ],
      zones: [
        {
          securityZoneId: 'z1',
          name: 'Zone 1',
          code: 'Z1',
          securityLevel: 'HighSecurity',
          isRestricted: true,
          accessPoints: [
            { accessPointId: 'ap1', name: 'Cửa 1', type: 'Door', directionMode: 'Bidirectional', isActive: false },
          ],
        },
        { securityZoneId: 'z2', name: 'Zone 2', code: '', accessPoints: [] },
      ],
    },
    { siteId: 's2', name: 'Khu B', code: 'SB', buildings: [], zones: [] },
  ],
}

const overviewData = { sites: 3, accessPoints: 4, companies: 1, buildings: 1, floors: 2, zones: 2, doors: 1, lanes: 5 }
const backfillStatus = {
  gatesMapped: 1,
  gatesUnmapped: 2,
  totalGates: 3,
  camerasMapped: 0,
  camerasUnmapped: 1,
  totalCameras: 1,
  vehiclesMapped: 2,
  vehiclesUnmapped: 0,
  totalVehicles: 2,
}
const assetMap = {
  gates: [{ gateId: 101, siteId: null, gateName: 'Cổng X', location: 'Sảnh' }],
  cameras: [{ cameraId: 201, siteId: null, cameraName: 'Cam 1', cameraType: 'IP', gateId: null }],
  vehicles: [{ vehicleId: 301, siteId: null, licensePlate: '29A-1', employeeName: 'An' }],
}
const searchResults = [{ entityType: 'Site', siteId: 's1', name: 'Khu A' }]
const auditItems = [
  { id: 1, actionType: 'CREATE', username: 'admin', isSuccess: true, path: '/a', timestampUtc: '2024-01-01T00:00:00Z' },
  { id: 2, actionType: 'DELETE', username: '', isSuccess: false, failureReason: 'nope', timestampUtc: 'not-a-date' },
]

function mockAll() {
  mocks.api.overview = vi.fn().mockResolvedValue([{ data: overviewData }])
  mocks.api.getHierarchy = vi.fn().mockResolvedValue({ data: [companyData] })
  mocks.api.getBackfillStatus = vi.fn().mockResolvedValue({ data: backfillStatus })
  mocks.api.assetMap = vi.fn().mockResolvedValue({ data: assetMap })
  mocks.api.searchHierarchy = vi.fn((type) =>
    Promise.resolve({ data: type === 'site' ? searchResults : [] })
  )
  mocks.api.backfillDefaultSite = vi.fn().mockResolvedValue({ data: { gatesMapped: 1 } })
  for (const name of ['createCompany', 'createSite', 'createBuilding', 'createFloor', 'createZone', 'createAccessPoint']) {
    mocks.api[name] = vi.fn().mockResolvedValue({ data: {} })
  }
  for (const name of ['updateCompany', 'updateSite', 'updateBuilding', 'updateFloor', 'updateZone', 'updateAccessPoint']) {
    mocks.api[name] = vi.fn().mockResolvedValue({ data: {} })
  }
  for (const name of ['deleteCompany', 'deleteSite', 'deleteBuilding', 'deleteFloor', 'deleteZone', 'deleteAccessPoint']) {
    mocks.api[name] = vi.fn().mockResolvedValue({ data: {} })
  }
  for (const name of ['restoreCompany', 'restoreSite', 'restoreBuilding', 'restoreFloor', 'restoreZone', 'restoreAccessPoint']) {
    mocks.api[name] = vi.fn().mockResolvedValue({ data: {} })
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  mockAll()
  accessLogApi.getSystemAuditLogs.mockResolvedValue({ data: { items: auditItems } })
  vi.spyOn(window, 'confirm').mockReturnValue(true)
  vi.spyOn(window, 'alert').mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
})

const mountComponent = async () => {
  const wrapper = mount(SiteHierarchy, { global: { stubs: sharedStubs } })
  await flushPromises()
  return wrapper
}

describe('SiteHierarchy', () => {
  it('loads the hierarchy on mount with overview, status and asset map', async () => {
    const wrapper = await mountComponent()
    expect(mocks.api.getHierarchy).toHaveBeenCalled()
    expect(mocks.api.overview).toHaveBeenCalled()
    expect(mocks.api.getBackfillStatus).toHaveBeenCalled()
    expect(mocks.api.assetMap).toHaveBeenCalled()
    expect(wrapper.vm.hierarchy).toHaveLength(1)
    expect(wrapper.vm.overview.sites).toBe(3)
    expect(wrapper.vm.selectedNode.type).toBe('company')
    expect(wrapper.vm.assetMapLoaded).toBe(true)
    expect(wrapper.text()).toContain('Công ty A')
  })

  it('computes display name and columns by entity type', async () => {
    const wrapper = await mountComponent()
    const cases = {
      Company: ['CompanyId', 'Name', 'Code', 'IsActive'],
      Site: ['SiteId', 'Name', 'Code', 'CompanyCode', 'Address', 'TimeZoneId', 'IsActive'],
      Building: ['BuildingId', 'Name', 'Code', 'SiteCode', 'TotalFloors', 'IsActive'],
      FacilityFloor: ['FacilityFloorId', 'Name', 'Code', 'BuildingCode', 'SortOrder', 'IsActive'],
      SecurityZone: ['SecurityZoneId', 'Name', 'Code', 'SiteCode', 'SecurityLevel', 'IsRestricted', 'IsActive'],
    }
    for (const [type, cols] of Object.entries(cases)) {
      wrapper.vm.ieEntity = type
      expect(wrapper.vm.ieColumns).toEqual(cols)
    }
    wrapper.vm.ieEntity = 'Company'
    expect(wrapper.vm.ieColumns).toEqual(['CompanyId', 'Name', 'Code', 'IsActive'])
    expect(wrapper.vm.ieDisplayName).toBe('Công ty')
    wrapper.vm.ieEntity = 'Site'
    expect(wrapper.vm.ieDisplayName).toBe('Khu vực')
  })

  it('loads hierarchy refresh when a node is already selected', async () => {
    const wrapper = await mountComponent()
    await wrapper.vm.toggleExpanded('site', 's1')
    await wrapper.vm.toggleExpanded('zone', 'z1')
    await wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
    await flushPromises()
    await wrapper.vm.loadAll()
    await flushPromises()
    expect(wrapper.vm.selectedData.siteId).toBe('s1')
  })

  it('handles hierarchy load error by clearing', async () => {
    mocks.api.getHierarchy.mockRejectedValue(new Error('x'))
    const wrapper = await mountComponent()
    expect(wrapper.vm.hierarchy).toEqual([])
    expect(wrapper.text()).toContain('Chưa có dữ liệu phân cấp')
  })

  it('expands, collapses and toggles nodes; checks isSelected && isExpanded', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.expandAll()
    expect(wrapper.vm.isExpanded('company', 'c1')).toBe(true)
    expect(wrapper.vm.isExpanded('site', 's1')).toBe(true)
    expect(wrapper.vm.isExpanded('building', 'b1')).toBe(true)
    expect(wrapper.vm.isExpanded('zone', 'z1')).toBe(true)
    wrapper.vm.toggleExpanded('site', 's1')
    expect(wrapper.vm.isExpanded('site', 's1')).toBe(false)
    expect(wrapper.vm.isExpanded('zone', 'missing')).toBe(false)
    wrapper.vm.collapseAll()
    expect(wrapper.vm.isExpanded('company', 'c1')).toBe(false)
    expect(wrapper.vm.isSelected('company', 'c1')).toBe(true)
    expect(wrapper.vm.isSelected('site', 's1')).toBe(false)
  })

  it('selects every node type and renders stats/detail/history correctly', async () => {
    const wrapper = await mountComponent()
    const h = wrapper.vm.findNodeRecord('company', 'c1')
    const site = wrapper.vm.findNodeRecord('site', 's1')
    const building = wrapper.vm.findNodeRecord('building', 'b1')
    const floor = wrapper.vm.findNodeRecord('floor', 'f1')
    const zone = wrapper.vm.findNodeRecord('zone', 'z1')
    const ap = wrapper.vm.findNodeRecord('accesspoint', 'ap1')

    wrapper.vm.selectNode('company', 'c1', h)
    await flushPromises()
    expect(wrapper.vm.selectedNodeLabel).toBe('Công ty')
    expect(wrapper.vm.selectedNodeStats).toHaveLength(3)
    expect(wrapper.vm.detailFields['Tên']).toBe('Công ty A')

    wrapper.vm.selectNode('site', 's1', site)
    await flushPromises()
    expect(wrapper.vm.selectedNodeLabel).toBe('Khu vực')
    expect(wrapper.vm.selectedNodeStats.some((s) => s.label === 'Có địa chỉ')).toBe(true)
    expect(wrapper.vm.detailFields['Địa chỉ']).toBe('123 Đường X')
    expect(wrapper.vm.childOptions.map((o) => o.type)).toEqual(['building', 'zone'])

    wrapper.vm.selectNode('building', 'b1', building)
    await flushPromises()
    expect(wrapper.vm.selectedNodeStats[0].value).toBe(2)
    expect(wrapper.vm.childOptions.map((o) => o.type)).toEqual(['floor'])

    wrapper.vm.selectNode('floor', 'f1', floor)
    await flushPromises()
    expect(wrapper.vm.selectedNodeLabel).toBe('Tầng')

    wrapper.vm.selectNode('zone', 'z1', zone)
    await flushPromises()
    expect(wrapper.vm.selectedNodeLabel).toBe('Vùng an ninh')
    expect(wrapper.vm.childOptions.map((o) => o.type)).toEqual(['accesspoint'])
    expect(wrapper.vm.detailFields['Giới hạn']).toBe('Có')

    wrapper.vm.selectNode('accesspoint', 'ap1', ap)
    await flushPromises()
    expect(wrapper.vm.selectedNodeLabel).toBe('Điểm truy cập')
    expect(wrapper.vm.selectedNodeDescription).toContain('Điểm')
    expect(wrapper.vm.childOptions).toEqual([])
    expect(wrapper.vm.selectedNodeLifecycleAction).toBe('Khôi phục')
    expect(wrapper.vm.preferredSpatialSiteId).toBe('s1')
    expect(wrapper.vm.historyCards).toHaveLength(2)
    expect(wrapper.vm.historyCards[0].status).toBe('Success')
    expect(wrapper.vm.historyCards[1].status).toBe('Failed')
    expect(wrapper.vm.historyCards[1].reason).toBe('nope')
  })

  it('handles no-selected-node helpers', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.selectedNode = null
    wrapper.vm.selectedData = null
    expect(wrapper.vm.selectedNodeLabel).toBe('Nút')
    expect(wrapper.vm.selectedNodeDescription).toBe('')
    expect(wrapper.vm.childOptions).toEqual([])
    expect(wrapper.vm.selectedNodeStats).toEqual([])
    expect(wrapper.vm.detailFields).toEqual({})
    expect(wrapper.vm.preferredSpatialSiteId).toBeNull()
  })

  it('loads selected node history only when meta present, clearing otherwise', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.selectedNode = { type: 'unknown', id: 1 }
    wrapper.vm.selectedData = {}
    await wrapper.vm.loadSelectedNodeHistory()
    expect(wrapper.vm.selectedHistory).toEqual([])
    wrapper.vm.selectedNode = null
    await wrapper.vm.loadSelectedNodeHistory()
    expect(wrapper.vm.selectedHistory).toEqual([])
    wrapper.vm.selectedNode = { type: 'company', id: 'c1' }
    mocks.api.getHierarchy.mockResolvedValue({ data: [] })
    wrapper.vm.hierarchy = [companyData]
    await wrapper.vm.loadSelectedNodeHistory()
    expect(accessLogApi.getSystemAuditLogs).toHaveBeenCalled()
  })

  it('handles audit log errors', async () => {
    accessLogApi.getSystemAuditLogs.mockRejectedValue(new Error('x'))
    const wrapper = await mountComponent()
    wrapper.vm.selectedNode = { type: 'site', id: 's1' }
    wrapper.vm.selectedData = { siteId: 's1' }
    await wrapper.vm.loadSelectedNodeHistory()
    expect(wrapper.vm.selectedHistory).toEqual([])
  })

  it('formats audit timestamps', async () => {
    const wrapper = await mountComponent()
    expect(wrapper.vm.formatAuditTimestamp('')).toBe('Không rõ thời gian')
    expect(wrapper.vm.formatAuditTimestamp('bad-date')).toBe('Không rõ thời gian')
    expect(wrapper.vm.formatAuditTimestamp('2024-01-01T00:00:00Z')).toBeTruthy()
  })

  it('searches nodes with proper query length and selects a result', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.searchQuery = 'A'
    await wrapper.vm.searchNodes()
    expect(mocks.api.searchHierarchy).not.toHaveBeenCalled()
    wrapper.vm.searchQuery = 'An'
    await wrapper.vm.searchNodes()
    expect(mocks.api.searchHierarchy).toHaveBeenCalledTimes(5)
    expect(wrapper.vm.searchResults).toHaveLength(1)
    wrapper.vm.selectSearchResult(wrapper.vm.searchResults[0])
    expect(wrapper.vm.selectedNode.type).toBe('site')
    expect(wrapper.vm.searchQuery).toBe('')
    expect(wrapper.vm.searchResults).toEqual([])
  })

  it('handles search hierarchy errors per-type', async () => {
    mocks.api.searchHierarchy.mockRejectedValue(new Error('x'))
    const wrapper = await mountComponent()
    wrapper.vm.searchQuery = 'An'
    await wrapper.vm.searchNodes()
    expect(wrapper.vm.searchResults).toEqual([])
  })

  it('maps select search result with fallback type and record', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.hierarchy = [companyData]
    wrapper.vm.selectSearchResult({ entityType: 'UnknownType', companyId: 'c1', name: 'X' })
    expect(wrapper.vm.selectedNode.type).toBe('site')
  })

  it('opens and loads the asset tab, advanced tools and spatial tab', async () => {
    const wrapper = await mountComponent()
    await wrapper.vm.openAssetTab()
    expect(wrapper.vm.tab).toBe('assets')
    expect(wrapper.vm.assetCards).toHaveLength(3)
    expect(wrapper.vm.mappedAssetSummary).toBe('3/6')
    expect(wrapper.vm.overallCoveragePercent).toBe(50)
    expect(wrapper.vm.riskiestAssetLabel).toBe('Cổng')
    expect(wrapper.vm.unmappedAssets).toHaveLength(3)
    expect(wrapper.vm.filteredUnmappedAssets).toHaveLength(3)
    wrapper.vm.selectedAssetFilter = 'camera'
    expect(wrapper.vm.filteredUnmappedAssets).toHaveLength(1)
    wrapper.vm.selectedAssetFilter = 'all'
    wrapper.vm.toggleAdvancedTools()
    expect(wrapper.vm.showAdvancedTools).toBe(true)
    wrapper.vm.toggleAdvancedTools()
    expect(wrapper.vm.showAdvancedTools).toBe(false)
    wrapper.vm.openSpatialTab()
    expect(wrapper.vm.tab).toBe('spatial')
    wrapper.vm.focusStructureTab()
    expect(wrapper.vm.tab).toBe('tree')
  })

  it('handles asset card and summary when status absent', async () => {
    mockAll()
    mocks.api.getBackfillStatus.mockResolvedValue({ data: null })
    const wrapper = await mountComponent()
    expect(wrapper.vm.assetCards).toEqual([])
    expect(wrapper.vm.mappedAssetSummary).toBe('Chưa tải')
    expect(wrapper.vm.overallCoveragePercent).toBe(0)
    expect(wrapper.vm.riskiestAssetLabel).toBe('Chờ dữ liệu')
  })

  it('loads status and map lazily when opening asset tab first time', async () => {
    mockAll()
    mocks.api.assetMap.mockResolvedValue({ data: null })
    const wrapper = await mountComponent()
    mocks.api.getBackfillStatus.mockClear()
    mocks.api.assetMap.mockClear()
    await wrapper.vm.openAssetTab()
    expect(wrapper.vm.assetMap).toEqual({ gates: [], cameras: [], vehicles: [] })
  })

  it('handles asset map load error', async () => {
    mockAll()
    mocks.api.assetMap.mockRejectedValue(new Error('x'))
    const wrapper = await mountComponent()
    wrapper.vm.assetStatus = null
    wrapper.vm.assetMapLoaded = false
    await wrapper.vm.openAssetTab()
    expect(wrapper.vm.assetMap).toEqual({ gates: [], cameras: [], vehicles: [] })
    expect(wrapper.vm.assetMapLoaded).toBe(true)
  })

  it('returns early from openEditNode without selection', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.selectedNode = null
    wrapper.vm.selectedData = null
    wrapper.vm.openEditNode()
    expect(wrapper.vm.showEditModal).toBe(false)
  })

  it('covers unmapped assets mapping via siteOptions and asset summary', async () => {
    const wrapper = await mountComponent()
    const opts = wrapper.vm.siteOptions
    expect(opts).toHaveLength(2)
    expect(opts[0].siteId).toBe('s1')
    wrapper.vm.selectNode('site', 's1', wrapper.vm.findNodeRecord('site', 's1'))
    expect(wrapper.vm.preferredSpatialSiteId).toBe('s1')
    wrapper.vm.selectNode('building', 'b1', wrapper.vm.findNodeRecord('building', 'b1'))
    expect(wrapper.vm.preferredSpatialSiteId).toBe('s1')
    wrapper.vm.selectNode('floor', 'f1', wrapper.vm.findNodeRecord('floor', 'f1'))
    expect(wrapper.vm.preferredSpatialSiteId).toBe('s1')
    wrapper.vm.selectNode('zone', 'z1', wrapper.vm.findNodeRecord('zone', 'z1'))
    expect(wrapper.vm.preferredSpatialSiteId).toBe('s1')
    wrapper.vm.selectNode('company', 'c1', wrapper.vm.findNodeRecord('company', 'c1'))
    expect(wrapper.vm.preferredSpatialSiteId).toBe(null)
    wrapper.vm.assetMap = null
    expect(wrapper.vm.unmappedAssets).toEqual([])
  })

  it('covers toggle-expanded and open-add-child early returns', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.toggleExpanded('floor', 'f1')
    expect(wrapper.vm.isExpanded('floor', 'f1')).toBeFalsy()
    wrapper.vm.selectedNode = null
    wrapper.vm.selectedData = null
    wrapper.vm.openAddChild('site')
    expect(wrapper.vm.showAddModal).toBe(false)
    wrapper.vm.selectNode('accesspoint', 'ap1', { ...wrapper.vm.findNodeRecord('accesspoint', 'ap1'), isActive: true })
    wrapper.vm.openAddChild('site')
    expect(wrapper.vm.showAddModal).toBe(false)
  })

  it('covers delete default branches for unknown node types', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.selectedNode = { type: 'weird', id: 'w1' }
    wrapper.vm.selectedData = { isActive: true, name: 'X' }
    await wrapper.vm.deleteSelectedNode()
    wrapper.vm.selectedData = { isActive: false, name: 'X' }
    await wrapper.vm.deleteSelectedNode()
    expect(mocks.api.deleteCompany).not.toHaveBeenCalled()
    expect(mocks.api.restoreCompany).not.toHaveBeenCalled()
  })

  it('covers needsAttention unknown type when attention-only is on', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.showNeedsAttentionOnly = true
    expect(wrapper.vm.needsAttention({}, 'unknown')).toBe(false)
  })

  it('runs backfill creating a report', async () => {
    const wrapper = await mountComponent()
    await wrapper.vm.runBackfill()
    expect(mocks.api.backfillDefaultSite).toHaveBeenCalledWith(expect.any(Object))
    expect(wrapper.vm.backfillReport).toEqual({ gatesMapped: 1 })
    expect(wrapper.vm.busy.backfill).toBe(false)
  })

  it('handles backfill errors', async () => {
    mocks.api.backfillDefaultSite.mockRejectedValue(new Error('x'))
    const wrapper = await mountComponent()
    await wrapper.vm.runBackfill()
    expect(window.alert).toHaveBeenCalled()
    expect(wrapper.vm.busy.backfill).toBe(false)
  })

  it('import complete closes modal and reloads', async () => {
    const wrapper = await mountComponent()
    wrapper.vm.onImportComplete({})
    expect(wrapper.vm.showImportModal).toBe(false)
  })

  describe('add child', () => {
    it('opens add modal for company via openCreateCompany', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.openCreateCompany()
      expect(wrapper.vm.childType).toBe('company')
      expect(wrapper.vm.childLabel).toBe('Công ty')
      expect(wrapper.vm.showAddModal).toBe(true)
      wrapper.vm.addForm.name = 'Cty Mới'
      await wrapper.vm.saveChild()
      expect(mocks.api.createCompany).toHaveBeenCalled()
      expect(wrapper.vm.showAddModal).toBe(false)
    })

    it('creates site, building and floor children', async () => {
      const wrapper = await mountComponent()
      const h = wrapper.vm.hierarchy[0]
      const site = h.sites[0]
      const building = site.buildings[0]

      wrapper.vm.selectNode('company', 'c1', h)
      wrapper.vm.openAddChild('site')
      wrapper.vm.addForm.name = 'Site Mới'
      await wrapper.vm.saveChild()
      expect(mocks.api.createSite).toHaveBeenCalled()

      wrapper.vm.selectNode('site', 's1', site)
      wrapper.vm.openAddChild('building')
      wrapper.vm.addForm.name = 'Toà Mới'
      await wrapper.vm.saveChild()
      expect(mocks.api.createBuilding).toHaveBeenCalled()

      wrapper.vm.openAddChild('zone')
      wrapper.vm.addForm.name = 'Zone Mới'
      wrapper.vm.addForm.isRestricted = true
      await wrapper.vm.saveChild()
      expect(mocks.api.createZone).toHaveBeenCalled()

      wrapper.vm.selectNode('building', 'b1', building)
      wrapper.vm.openAddChild('floor')
      wrapper.vm.addForm.name = 'Tầng Mới'
      wrapper.vm.addForm.sortOrder = 3
      await wrapper.vm.saveChild()
      expect(mocks.api.createFloor).toHaveBeenCalled()
    })

    it('creates an accesspoint child under a zone', async () => {
      const wrapper = await mountComponent()
      const zone = wrapper.vm.findNodeRecord('zone', 'z1')
      wrapper.vm.selectNode('zone', 'z1', zone)
      wrapper.vm.openAddChild('accesspoint')
      wrapper.vm.addForm.name = 'Cửa mới'
      await wrapper.vm.saveChild()
      expect(mocks.api.createAccessPoint).toHaveBeenCalledWith(
        expect.objectContaining({ siteId: 's1', securityZoneId: 'z1' })
      )
    })

    it('returns early without a parent/name and alerts on error', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectedNode = null
      wrapper.vm.selectedData = null
      wrapper.vm.childType = 'site'
      await wrapper.vm.saveChild()
      expect(mocks.api.createSite).not.toHaveBeenCalled()

      wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
      wrapper.vm.childType = 'site'
      wrapper.vm.addForm.name = ''
      await wrapper.vm.saveChild()
      expect(mocks.api.createSite).not.toHaveBeenCalled()

      wrapper.vm.addForm.name = 'X'
      mocks.api.createSite.mockRejectedValue({ response: { data: { message: 'fail' } } })
      await wrapper.vm.saveChild()
      expect(window.alert).toHaveBeenCalledWith('fail')
    })
  })

  describe('edit node', () => {
    it('opens and saves edits for each node type', async () => {
      const wrapper = await mountComponent()
      const h = wrapper.vm.hierarchy[0]
      const site = h.sites[0]
      const building = site.buildings[0]
      const floor = building.floors[0]
      const zone = site.zones[0]
      const ap = zone.accessPoints[0]

      const cases = [
        ['company', 'c1', h, 'updateCompany'],
        ['site', 's1', site, 'updateSite'],
        ['building', 'b1', building, 'updateBuilding'],
        ['floor', 'f1', floor, 'updateFloor'],
        ['zone', 'z1', zone, 'updateZone'],
        ['accesspoint', 'ap1', ap, 'updateAccessPoint'],
      ]
      for (const [type, id, data, method] of cases) {
        wrapper.vm.selectNode(type, id, data)
        wrapper.vm.openEditNode()
        expect(wrapper.vm.showEditModal).toBe(true)
        expect(wrapper.vm.editNodeType).toBe(type)
        wrapper.vm.editForm.name = `Sửa ${id}`
        await wrapper.vm.saveEditNode()
        expect(mocks.api[method]).toHaveBeenCalled()
        expect(wrapper.vm.showEditModal).toBe(false)
      }
    })

    it('returns early without node/data/name', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectedNode = null
      wrapper.vm.selectedData = null
      await wrapper.vm.saveEditNode()
      expect(mocks.api.updateCompany).not.toHaveBeenCalled()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
      wrapper.vm.editForm.name = ''
      await wrapper.vm.saveEditNode()
      expect(mocks.api.updateSite).not.toHaveBeenCalled()
    })

    it('alerts on edit error', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
      wrapper.vm.openEditNode()
      wrapper.vm.editForm.name = 'X'
      mocks.api.updateSite.mockRejectedValue(new Error('boom'))
      await wrapper.vm.saveEditNode()
      expect(window.alert).toHaveBeenCalledWith('Không thể lưu thay đổi.')
    })
  })

  describe('delete/restore node', () => {
    it('deactivates every node type', async () => {
      const wrapper = await mountComponent()
      const h = wrapper.vm.findNodeRecord('company', 'c1')
      const site = wrapper.vm.findNodeRecord('site', 's1')
      const building = wrapper.vm.findNodeRecord('building', 'b1')
      const floor = wrapper.vm.findNodeRecord('floor', 'f1')
      const zone = wrapper.vm.findNodeRecord('zone', 'z1')
      const ap = { ...wrapper.vm.findNodeRecord('accesspoint', 'ap1'), isActive: true }

      const cases = [
        ['company', 'c1', h, 'deleteCompany'],
        ['site', 's1', site, 'deleteSite'],
        ['building', 'b1', building, 'deleteBuilding'],
        ['floor', 'f1', floor, 'deleteFloor'],
        ['zone', 'z1', zone, 'deleteZone'],
        ['accesspoint', 'ap1', ap, 'deleteAccessPoint'],
      ]
      for (const [type, id, data, method] of cases) {
        wrapper.vm.selectNode(type, id, data)
        await wrapper.vm.deleteSelectedNode()
        expect(mocks.api[method]).toHaveBeenCalled()
      }
    })

    it('restores every inactive node type', async () => {
      const wrapper = await mountComponent()
      const h = wrapper.vm.findNodeRecord('company', 'c1')
      const site = wrapper.vm.findNodeRecord('site', 's1')
      const building = wrapper.vm.findNodeRecord('building', 'b1')
      const floor = wrapper.vm.findNodeRecord('floor', 'f1')
      const zone = wrapper.vm.findNodeRecord('zone', 'z1')
      const ap = wrapper.vm.findNodeRecord('accesspoint', 'ap1')

      const cases = [
        ['company', 'c1', { ...h, isActive: false }, 'restoreCompany'],
        ['site', 's1', { ...site, isActive: false }, 'restoreSite'],
        ['building', 'b1', { ...building, isActive: false }, 'restoreBuilding'],
        ['floor', 'f1', { ...floor, isActive: false }, 'restoreFloor'],
        ['zone', 'z1', { ...zone, isActive: false }, 'restoreZone'],
        ['accesspoint', 'ap1', ap, 'restoreAccessPoint'],
      ]
      for (const [type, id, data, method] of cases) {
        wrapper.vm.selectNode(type, id, data)
        await wrapper.vm.deleteSelectedNode()
        expect(mocks.api[method]).toHaveBeenCalled()
      }
    })

    it('returns when confirm is cancelled', async () => {
      window.confirm.mockReturnValue(false)
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
      await wrapper.vm.deleteSelectedNode()
      expect(mocks.api.deleteSite).not.toHaveBeenCalled()
    })

    it('returns early without selection and alerts on delete error', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectedNode = null
      wrapper.vm.selectedData = null
      await wrapper.vm.deleteSelectedNode()
      expect(mocks.api.deleteCompany).not.toHaveBeenCalled()

      wrapper.vm.selectNode('site', 's1', wrapper.vm.hierarchy[0].sites[0])
      mocks.api.deleteSite.mockRejectedValue({ response: { data: { message: 'no' } } })
      await wrapper.vm.deleteSelectedNode()
      expect(window.alert).toHaveBeenCalledWith('no')
    })
  })

  describe('needs attention filtering', () => {
    it('evaluates needsAttention for each type', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.needsAttention({ sites: [] }, 'company')).toBe(true)
      expect(wrapper.vm.needsAttention({ buildings: [], zones: [] }, 'site')).toBe(true)
      expect(wrapper.vm.needsAttention({ floors: [] }, 'building')).toBe(true)
      expect(wrapper.vm.needsAttention({ accessPoints: [] }, 'zone')).toBe(true)
      expect(wrapper.vm.needsAttention({ code: '' }, 'floor')).toBe(true)
      expect(wrapper.vm.needsAttention({ isActive: false }, 'accesspoint')).toBe(true)
      expect(wrapper.vm.needsAttention({}, 'nope')).toBe(false)
      wrapper.vm.showNeedsAttentionOnly = true
      expect(wrapper.vm.needsAttention({ isActive: true }, 'accesspoint')).toBe(false)
    })

    it('filters nodes when attention-only is enabled', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.showNeedsAttentionOnly = true
      const site = companyData.sites[0]
      expect(wrapper.vm.filteredSites([site, companyData.sites[1]])).toHaveLength(2)
      expect(wrapper.vm.filteredBuildings(site.buildings)).toHaveLength(1)
      expect(wrapper.vm.filteredFloors(site.buildings[0].floors)).toHaveLength(1)
      expect(wrapper.vm.filteredZones(site.zones)).toHaveLength(2)
      expect(wrapper.vm.filteredAccessPoints(site.zones[0].accessPoints)).toHaveLength(1)
      expect(wrapper.vm.siteHasVisibleChildren(site)).toBe(true)
      wrapper.vm.showNeedsAttentionOnly = false
      expect(wrapper.vm.filteredSites([site])).toHaveLength(1)
      expect(wrapper.vm.siteHasVisibleChildren(site)).toBe(true)
    })

    it('normalizes hierarchy and finds node records', async () => {
      const wrapper = await mountComponent()
      const normalized = wrapper.vm.normalizeHierarchy([companyData])
      expect(normalized[0].sites[0].buildings[0].floors[0].siteId).toBe('s1')
      expect(wrapper.vm.findNodeRecord('company', 'c1').companyId).toBe('c1')
      expect(wrapper.vm.findNodeRecord('site', 's1').siteId).toBe('s1')
      expect(wrapper.vm.findNodeRecord('building', 'b1').buildingId).toBe('b1')
      expect(wrapper.vm.findNodeRecord('floor', 'f1').facilityFloorId).toBe('f1')
      expect(wrapper.vm.findNodeRecord('zone', 'z1').securityZoneId).toBe('z1')
      expect(wrapper.vm.findNodeRecord('accesspoint', 'ap1').accessPointId).toBe('ap1')
      expect(wrapper.vm.findNodeRecord('site', 'missing')).toBeNull()
    })

    it('computes percent and normalizes asset map', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.percent(5, 10)).toBe(50)
      expect(wrapper.vm.percent(5, 0)).toBe(0)
      expect(wrapper.vm.normalizeAssetMap(null)).toEqual({ gates: [], cameras: [], vehicles: [] })
      expect(wrapper.vm.getAuditEntityMeta('zone', 'z1')).toEqual({ entityName: 'SecurityZone', entityId: 'z1' })
      expect(wrapper.vm.getAuditEntityMeta('nope', null)).toEqual({ entityName: null, entityId: null })
    })
  })

  describe('template interactions', () => {
    it('fires hero and tab buttons', async () => {
      const wrapper = await mountComponent()
      const before = mocks.api.getHierarchy.mock.calls.length
      await wrapper.find('.hero-actions button.btn-primary').trigger('click')
      await flushPromises()
      expect(mocks.api.getHierarchy.mock.calls.length).toBeGreaterThan(before)

      const tabs = wrapper.findAll('.workspace-tabs button')
      await tabs[1].trigger('click')
      expect(wrapper.vm.tab).toBe('assets')
      await tabs[2].trigger('click')
      expect(wrapper.vm.tab).toBe('spatial')
      await tabs[0].trigger('click')
      expect(wrapper.vm.tab).toBe('tree')

      await wrapper.find('button.btn-secondary').trigger('click') // focus structure
      expect(wrapper.vm.tab).toBe('tree')
    })

    it('fires import and export buttons and open create company', async () => {
      const wrapper = await mountComponent()
      const buttons = wrapper.findAll('.hero-actions button')
      await buttons[0].trigger('click')
      expect(wrapper.vm.showImportModal).toBe(true)
      await wrapper.find('.import-stub .stub-close').trigger('click')
      expect(wrapper.vm.showImportModal).toBe(false)
      await buttons[0].trigger('click')
      const before = mocks.api.getHierarchy.mock.calls.length
      await wrapper.find('.import-stub .stub-done').trigger('click')
      await flushPromises()
      expect(wrapper.vm.showImportModal).toBe(false)
      expect(mocks.api.getHierarchy.mock.calls.length).toBeGreaterThan(before)

      await buttons[1].trigger('click')
      expect(wrapper.vm.showExportModal).toBe(true)
      await wrapper.find('.export-stub .stub-close').trigger('click')
      expect(wrapper.vm.showExportModal).toBe(false)

      await wrapper.find('.panel-section-header button.btn-primary').trigger('click')
      expect(wrapper.vm.showAddModal).toBe(true)
      expect(wrapper.vm.childType).toBe('company')
    })

    it('fires expand/collapse toggles and selects every node type via the tree', async () => {
      const wrapper = await mountComponent()
      const collapseBtn = wrapper.findAll('.toolbar-actions button').find((b) => b.text().includes('Thu gọn'))
      const expandBtn = wrapper.findAll('.toolbar-actions button').find((b) => b.text().includes('Mở rộng'))
      await expandBtn.trigger('click')
      await wrapper.vm.$nextTick()
      expect(wrapper.vm.isExpanded('company', 'c1')).toBe(true)
      expect(wrapper.vm.isExpanded('site', 's1')).toBe(true)
      expect(wrapper.vm.isExpanded('building', 'b1')).toBe(true)
      expect(wrapper.vm.isExpanded('zone', 'z1')).toBe(true)

      await collapseBtn.trigger('click')
      await wrapper.vm.$nextTick()
      expect(wrapper.vm.isExpanded('company', 'c1')).toBe(false)

      await expandBtn.trigger('click')
      await wrapper.vm.$nextTick()

      await wrapper.find('.company-row').trigger('click')
      expect(wrapper.vm.isSelected('company', 'c1')).toBe(true)

      await wrapper.find('.site-row').trigger('click')
      expect(wrapper.vm.isSelected('site', 's1')).toBe(true)

      await wrapper.find('.building-row').trigger('click')
      expect(wrapper.vm.isSelected('building', 'b1')).toBe(true)

      await wrapper.find('.floor-row').trigger('click')
      expect(wrapper.vm.isSelected('floor', 'f1')).toBe(true)

      await wrapper.find('.zone-row').trigger('click')
      expect(wrapper.vm.isSelected('zone', 'z1')).toBe(true)

      await wrapper.find('.access-row').trigger('click')
      expect(wrapper.vm.isSelected('accesspoint', 'ap1')).toBe(true)
    })

    it('fires each node-type expand button in the tree', async () => {
      const wrapper = await mountComponent()
      await wrapper.findAll('.toolbar-actions button').find((b) => b.text().includes('Thu gọn')).trigger('click')
      await wrapper.vm.$nextTick()
      expect(wrapper.vm.isExpanded('company', 'c1')).toBe(false)

      await wrapper.find('button.expand-btn').trigger('click')
      expect(wrapper.vm.isExpanded('company', 'c1')).toBe(true)
      await wrapper.vm.$nextTick()

      await wrapper.findAll('button.expand-btn')[1].trigger('click')
      expect(wrapper.vm.isExpanded('site', 's1')).toBe(true)
      await wrapper.vm.$nextTick()

      await wrapper.findAll('button.expand-btn')[2].trigger('click')
      expect(wrapper.vm.isExpanded('building', 'b1')).toBe(true)
      await wrapper.vm.$nextTick()

      await wrapper.findAll('button.expand-btn').find((b) => b.text() === '+').trigger('click')
      await wrapper.vm.$nextTick()
      expect(wrapper.vm.isExpanded('zone', 'z1')).toBe(true)
    })

    it('fires add-child modal overlay, close and all form fields', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.findNodeRecord('site', 's1'))
      wrapper.vm.openAddChild('zone')
      await wrapper.vm.$nextTick()
      const body = wrapper.findAll('.modal-body input')[0]
      await body.setValue('Vùng mới')
      const codeInput = wrapper.findAll('.modal-body input')[1]
      await codeInput.setValue('Z9')
      expect(wrapper.vm.addForm.code).toBe('Z9')
      const secSel = wrapper.find('.modal-body select')
      await secSel.setValue('Restricted')
      expect(wrapper.vm.addForm.securityLevel).toBe('Restricted')
      const restrictCheck = wrapper.findAll('.modal-body input[type="checkbox"]')[0]
      await restrictCheck.setChecked(true)
      expect(wrapper.vm.addForm.isRestricted).toBe(true)

      await wrapper.find('.modal-header .btn-close').trigger('click')
      expect(wrapper.vm.showAddModal).toBe(false)

      wrapper.vm.openAddChild('zone')
      await wrapper.vm.$nextTick()
      await wrapper.find('.modal-overlay').trigger('click')
      expect(wrapper.vm.showAddModal).toBe(false)

      wrapper.vm.selectNode('building', 'b1', wrapper.vm.findNodeRecord('building', 'b1'))
      wrapper.vm.openAddChild('floor')
      await wrapper.vm.$nextTick()
      const sortInput = wrapper.find('.modal-body input[type="number"]')
      await sortInput.setValue(3)
      expect(wrapper.vm.addForm.sortOrder).toBe(3)
    })

    it('fires add-child accesspoint type select via DOM', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('zone', 'z1', wrapper.vm.findNodeRecord('zone', 'z1'))
      wrapper.vm.openAddChild('accesspoint')
      await wrapper.vm.$nextTick()
      await wrapper.find('.modal-body select').setValue('Turnstile')
      expect(wrapper.vm.addForm.type).toBe('Turnstile')
    })

    it('fires edit-modal overlay, close and all form fields', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.findNodeRecord('site', 's1'))
      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      const codeInput = wrapper.findAll('.modal-body input')[1]
      await codeInput.setValue('S1X')
      expect(wrapper.vm.editForm.code).toBe('S1X')
      const addrInput = wrapper.findAll('.modal-body input')[2]
      await addrInput.setValue('456 Đường Y')
      expect(wrapper.vm.editForm.address).toBe('456 Đường Y')
      const tzInput = wrapper.findAll('.modal-body input')[3]
      await tzInput.setValue('Asia/Bangkok')
      expect(wrapper.vm.editForm.timeZoneId).toBe('Asia/Bangkok')
      const activeCheck = wrapper.findAll('.modal-body input[type="checkbox"]')[0]
      await activeCheck.setChecked(false)
      expect(wrapper.vm.editForm.isActive).toBe(false)

      await wrapper.find('.modal-header .btn-close').trigger('click')
      expect(wrapper.vm.showEditModal).toBe(false)

      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      await wrapper.find('.modal-overlay').trigger('click')
      expect(wrapper.vm.showEditModal).toBe(false)
    })

    it('fires edit-modal zone and floor fields via DOM', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('zone', 'z1', wrapper.vm.findNodeRecord('zone', 'z1'))
      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      await wrapper.find('.modal-body select').setValue('Normal')
      expect(wrapper.vm.editForm.securityLevel).toBe('Normal')
      const restrictCheck = wrapper.findAll('.modal-body input[type="checkbox"]')[0]
      await restrictCheck.setChecked(false)
      expect(wrapper.vm.editForm.isRestricted).toBe(false)

      wrapper.vm.selectNode('floor', 'f1', wrapper.vm.findNodeRecord('floor', 'f1'))
      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      const sortInput = wrapper.find('.modal-body input[type="number"]')
      await sortInput.setValue(5)
      expect(wrapper.vm.editForm.sortOrder).toBe(5)
    })

    it('fires openAddChild and first-company buttons in the detail panel', async () => {
      const wrapper = await mountComponent()
      const companyBtn = wrapper.findAll('.detail-hero-actions button').find((b) => b.text().includes('Khu vực'))
      await companyBtn.trigger('click')
      expect(wrapper.vm.childType).toBe('site')
      expect(wrapper.vm.showAddModal).toBe(true)

      wrapper.vm.showAddModal = false
      wrapper.vm.selectedNode = null
      wrapper.vm.selectedData = null
      await wrapper.vm.$nextTick()
      const firstCompanyBtn = wrapper.findAll('button').find((b) => b.text().includes('Mở công ty đầu tiên'))
      await firstCompanyBtn.trigger('click')
      expect(wrapper.vm.isSelected('company', 'c1')).toBe(true)
    })

    it('fires advanced tools, backfill inputs and run backfill', async () => {
      const wrapper = await mountComponent()
      await wrapper.findAll('.workspace-tabs button')[1].trigger('click')
      await flushPromises()
      await wrapper.find('.asset-overview-card .btn-secondary').trigger('click')
      expect(wrapper.vm.showAdvancedTools).toBe(true)
      const inputs = wrapper.findAll('.backfill-grid input')
      await inputs[0].setValue('CODE')
      await inputs[1].setValue('HQ2')
      await inputs[2].setValue('Cty')
      await inputs[3].setValue('Khu')
      expect(wrapper.vm.backfillForm.companyCode).toBe('CODE')
      const runBtn = wrapper.findAll('button').find((b) => b.text().includes('Chạy bổ sung an toàn'))
      await runBtn.trigger('click')
      await flushPromises()
      expect(mocks.api.backfillDefaultSite).toHaveBeenCalled()
      expect(wrapper.vm.backfillReport).toEqual({ gatesMapped: 1 })
    })

    it('fires the tasks attention-only checkbox and asset filters', async () => {
      const wrapper = await mountComponent()
      const checkbox = wrapper.find('.attention-toggle input')
      await checkbox.setChecked(true)
      expect(wrapper.vm.showNeedsAttentionOnly).toBe(true)
      await checkbox.setChecked(false)
      expect(wrapper.vm.showNeedsAttentionOnly).toBe(false)
      await wrapper.findAll('.workspace-tabs button')[1].trigger('click')
      await flushPromises()
      const filters = wrapper.findAll('.unmapped-section .toolbar-actions button')
      await filters[1].trigger('click')
      expect(wrapper.vm.selectedAssetFilter).toBe('gate')
      await filters.find((b) => b.text().includes('Camera')).trigger('click')
      expect(wrapper.vm.selectedAssetFilter).toBe('camera')
    })

    it('fires the entity-type select and search input handlers', async () => {
      const wrapper = await mountComponent()
      const selects = wrapper.findAll('.hero-actions select')
      await selects.find((s) => !s.element.multiple && s.classes().includes('filter-select')).setValue('Site')
      expect(wrapper.vm.ieEntity).toBe('Site')
      const search = wrapper.find('.search-box input')
      await search.setValue('Khu')
      await flushPromises()
      expect(mocks.api.searchHierarchy).toHaveBeenCalled()
    })

    it('interacts with the add-child modal for a site', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('company', 'c1', wrapper.vm.findNodeRecord('company', 'c1'))
      wrapper.vm.openAddChild('site')
      expect(wrapper.vm.showAddModal).toBe(true)
      await wrapper.vm.$nextTick()
      const nameInput = wrapper.find('.modal-body input')
      await nameInput.setValue('Khu vực mới')
      const cancelBtn = wrapper.findAll('.modal-footer button').find((b) => b.text().includes('Hủy'))
      await cancelBtn.trigger('click')
      expect(wrapper.vm.showAddModal).toBe(false)

      wrapper.vm.openAddChild('site')
      await wrapper.vm.$nextTick()
      await nameInput.setValue('Khu vực mới')
      await wrapper.find('.modal-footer button.btn-primary').trigger('click')
      await flushPromises()
      expect(mocks.api.createSite).toHaveBeenCalled()
      expect(wrapper.vm.showAddModal).toBe(false)
    })

    it('fires the edit modal save via DOM', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('site', 's1', wrapper.vm.findNodeRecord('site', 's1'))
      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      const nameInput = wrapper.find('.modal-body input')
      await nameInput.setValue('Tên mới')
      await wrapper.findAll('.modal-footer button')[0].trigger('click')
      await flushPromises()
      expect(mocks.api.updateSite).toHaveBeenCalled()
    })

    it('fires the zone-specific add form fields and accesspoint edit fields', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectNode('zone', 'z1', wrapper.vm.findNodeRecord('zone', 'z1'))
      wrapper.vm.openAddChild('accesspoint')
      await wrapper.vm.$nextTick()
      const selects = wrapper.findAll('.modal-body select')
      await selects[0].setValue('Gate')
      expect(wrapper.vm.addForm.type).toBe('Gate')
      const nameInput = wrapper.find('.modal-body input')
      await nameInput.setValue('Cổng mới')
      await wrapper.findAll('.modal-footer button')[0].trigger('click')
      await flushPromises()
      expect(mocks.api.createAccessPoint).toHaveBeenCalled()

      wrapper.vm.selectNode('accesspoint', 'ap1', { ...wrapper.vm.findNodeRecord('accesspoint', 'ap1'), isActive: true })
      wrapper.vm.openEditNode()
      await wrapper.vm.$nextTick()
      const editSelects = wrapper.findAll('.modal-body select')
      await editSelects[0].setValue('Turnstile')
      const dirSelect = editSelects.find((s) => Array.from(s.element.options).some((o) => o.value === 'EntryOnly'))
      await dirSelect.setValue('EntryOnly')
      expect(wrapper.vm.editForm.type).toBe('Turnstile')
      expect(wrapper.vm.editForm.directionMode).toBe('EntryOnly')
      await wrapper.findAll('.modal-footer button')[1].trigger('click')
      expect(wrapper.vm.showEditModal).toBe(false)
    })

    it('fires search input and selects via search results', async () => {
      const wrapper = await mountComponent()
      const input = wrapper.find('.search-box input')
      await input.setValue('An')
      await flushPromises()
      expect(mocks.api.searchHierarchy).toHaveBeenCalled()
      await wrapper.find('.search-hit').trigger('click')
      expect(wrapper.vm.selectedNode.type).toBe('site')
    })

    it('fires asset tab and filter buttons', async () => {
      const wrapper = await mountComponent()
      await wrapper.findAll('.workspace-tabs button')[1].trigger('click')
      await flushPromises()
      await wrapper.find('.asset-overview-card .btn-secondary').trigger('click')
      expect(wrapper.vm.showAdvancedTools).toBe(true)
      const filters = wrapper.findAll('.toolbar-actions .btn-xs')
      await filters[1].trigger('click')
      expect(wrapper.vm.selectedAssetFilter).toBe('gate')
    })

    it('renders zero unmapped state', async () => {
      mockAll()
      mocks.api.assetMap.mockResolvedValue({ data: { gates: [], cameras: [], vehicles: [] } })
      const wrapper = await mountComponent()
      await wrapper.findAll('.workspace-tabs button')[1].trigger('click')
      await flushPromises()
      expect(wrapper.vm.filteredUnmappedAssets).toHaveLength(0)
      expect(wrapper.text()).toContain('Toàn bộ tài sản')
    })
  })
})
