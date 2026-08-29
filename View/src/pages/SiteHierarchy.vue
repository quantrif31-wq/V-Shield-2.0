<template>
  <div class="page-container site-hierarchy-page animate-in">
    <section class="hero-panel">
      <div class="hero-copy">
        <span class="panel-kicker">Nền tảng doanh nghiệp</span>
        <h1 class="page-title">Phân cấp khu vực &amp; Ánh xạ tài sản</h1>
        <p class="hero-text">
          Sắp xếp vị trí vật lý, vùng an ninh và điểm truy cập trong một không gian làm việc, sau đó kiểm tra cách
          cổng, camera và phương tiện được ánh xạ trên toàn bộ phân cấp.
        </p>
        <div class="hero-actions">
          <select v-model="ieEntity" class="filter-select" aria-label="Đối tượng nhập/xuất dữ liệu">
            <option value="Company">Công ty</option>
            <option value="Site">Khu vực</option>
            <option value="Building">Tòa nhà</option>
            <option value="FacilityFloor">Tầng</option>
            <option value="SecurityZone">Vùng an ninh</option>
          </select>
          <button type="button" class="btn btn-secondary" @click="showImportModal = true">Nhập dữ liệu</button>
          <button type="button" class="btn btn-secondary" @click="showExportModal = true">Xuất dữ liệu</button>
          <button type="button" class="btn btn-primary" :disabled="loading" @click="loadAll">Làm mới dữ liệu</button>
          <button type="button" class="btn btn-secondary" @click="focusStructureTab">Mở giao diện cấu trúc</button>
        </div>
      </div>
      <div class="hero-side">
        <div class="hero-stat">
          <strong>{{ overview.sites }}</strong>
          <span>khu vực đang hoạt động trong phạm vi</span>
        </div>
        <div class="hero-stat">
          <strong>{{ overview.accessPoints }}</strong>
          <span>điểm truy cập đã ánh xạ</span>
        </div>
        <div class="hero-stat highlight">
          <strong>{{ mappedAssetSummary }}</strong>
          <span>tổng quan mức phủ tài sản</span>
        </div>
      </div>
    </section>

    <section class="metric-strip">
      <article v-for="metric in topMetrics" :key="metric.label" class="metric-card">
        <span class="metric-label">{{ metric.label }}</span>
        <strong class="metric-value">{{ metric.value }}</strong>
        <span class="metric-note">{{ metric.note }}</span>
      </article>
    </section>

    <section class="workspace-tabs">
      <button type="button" :class="{ active: tab === 'tree' }" @click="tab = 'tree'">Cấu trúc phân cấp</button>
      <button type="button" :class="{ active: tab === 'assets' }" @click="openAssetTab">Độ phủ tài sản</button>
      <button type="button" :class="{ active: tab === 'spatial' }" @click="openSpatialTab">Bản đồ không gian</button>
    </section>

    <section v-if="tab === 'tree'" class="workspace-shell">
      <aside class="navigator-panel">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Điều hướng</span>
            <h2>Trình khám phá phân cấp</h2>
          </div>
          <div class="toolbar-actions">
            <span class="soft-chip">{{ hierarchy.length }} công ty</span>
            <button type="button" class="btn btn-xs btn-primary" @click="openCreateCompany">+ Công ty</button>
          </div>
        </div>

        <div class="navigator-toolbar">
          <label class="attention-toggle">
            <input v-model="showNeedsAttentionOnly" type="checkbox" />
            <span>Chỉ hiện nút cần cấu hình</span>
          </label>
          <div class="toolbar-actions">
            <button type="button" class="btn btn-xs btn-secondary" @click="expandAll">Mở rộng tất cả</button>
            <button type="button" class="btn btn-xs btn-secondary" @click="collapseAll">Thu gọn tất cả</button>
          </div>
        </div>

        <label class="search-box">
          <span>Tìm nút</span>
          <input v-model="searchQuery" placeholder="Tìm theo tên hoặc mã..." @input="searchNodes" />
        </label>

        <div v-if="searchQuery && !searchResults.length" class="empty-inline">
          Chưa có kết quả phù hợp. Hãy nhập ít nhất 2 ký tự hoặc chuyển sang cây bên dưới.
        </div>

        <div v-if="searchResults.length" class="search-results">
          <button
            v-for="(result, index) in searchResults"
            :key="`${result.entityType}-${index}`"
            type="button"
            class="search-hit"
            @click="selectSearchResult(result)"
          >
            <strong>{{ result.name }}</strong>
            <span class="hit-meta">{{ result.entityType }}</span>
            <span v-if="result.parentName" class="hit-parent">{{ result.parentName }}</span>
          </button>
        </div>

        <div v-if="!searchQuery && hierarchy.length" class="tree-root">
          <div v-for="company in hierarchy" :key="company.companyId" class="tree-branch">
            <div class="node-stack">
              <button
                v-if="company.sites?.length"
                type="button"
                class="expand-btn"
                @click.stop="toggleExpanded('company', company.companyId)"
              >
                {{ isExpanded('company', company.companyId) ? '−' : '+' }}
              </button>
              <button
                type="button"
                class="node-row company-row"
                :class="{ active: isSelected('company', company.companyId), attention: needsAttention(company, 'company') }"
                @click="selectNode('company', company.companyId, company)"
              >
                <span class="node-icon">◎</span>
                <span class="node-main">
                  <strong>{{ company.name }}</strong>
                  <small>{{ company.code }}</small>
                </span>
                <span v-if="needsAttention(company, 'company')" class="node-badge">Cần cấu hình</span>
                <span class="node-count">{{ company.sites?.length || 0 }} khu vực</span>
              </button>
            </div>

            <div v-if="company.sites?.length && isExpanded('company', company.companyId)" class="tree-children">
              <div v-for="site in filteredSites(company.sites || [])" :key="site.siteId" class="tree-branch">
                <div class="node-stack">
                  <button
                    v-if="siteHasVisibleChildren(site)"
                    type="button"
                    class="expand-btn"
                    @click.stop="toggleExpanded('site', site.siteId)"
                  >
                    {{ isExpanded('site', site.siteId) ? '−' : '+' }}
                  </button>
                <button
                  type="button"
                  class="node-row site-row"
                  :class="{ active: isSelected('site', site.siteId), attention: needsAttention(site, 'site') }"
                  @click="selectNode('site', site.siteId, site)"
                >
                  <span class="node-icon">⌂</span>
                  <span class="node-main">
                    <strong>{{ site.name }}</strong>
                    <small>{{ site.code }}</small>
                  </span>
                  <span v-if="needsAttention(site, 'site')" class="node-badge">Cần cấu hình</span>
                  <span class="node-count">{{ (site.buildings?.length || 0) + (site.zones?.length || 0) }} đơn vị</span>
                </button>
                </div>

                <div v-if="isExpanded('site', site.siteId)" class="tree-children">
                  <div v-for="building in filteredBuildings(site.buildings || [])" :key="building.buildingId" class="tree-branch">
                    <div class="node-stack">
                      <button
                        v-if="building.floors?.length"
                        type="button"
                        class="expand-btn"
                        @click.stop="toggleExpanded('building', building.buildingId)"
                      >
                        {{ isExpanded('building', building.buildingId) ? '−' : '+' }}
                      </button>
                    <button
                      type="button"
                      class="node-row building-row"
                      :class="{ active: isSelected('building', building.buildingId), attention: needsAttention(building, 'building') }"
                      @click="selectNode('building', building.buildingId, building)"
                    >
                      <span class="node-icon">▣</span>
                      <span class="node-main">
                        <strong>{{ building.name }}</strong>
                        <small>{{ building.code }}</small>
                      </span>
                      <span v-if="needsAttention(building, 'building')" class="node-badge">Cần cấu hình</span>
                      <span class="node-count">{{ building.floors?.length || 0 }} tầng</span>
                    </button>
                    </div>

                    <div v-if="building.floors?.length && isExpanded('building', building.buildingId)" class="tree-children">
                      <button
                        v-for="floor in filteredFloors(building.floors || [])"
                        :key="floor.facilityFloorId"
                        type="button"
                        class="node-row floor-row"
                        :class="{ active: isSelected('floor', floor.facilityFloorId) }"
                        @click="selectNode('floor', floor.facilityFloorId, floor)"
                      >
                        <span class="node-icon">□</span>
                        <span class="node-main">
                          <strong>{{ floor.name }}</strong>
                          <small>Thứ tự {{ floor.sortOrder ?? 0 }}</small>
                        </span>
                        <span v-if="needsAttention(floor, 'floor')" class="node-badge">Rà soát</span>
                      </button>
                    </div>
                  </div>

                  <div v-for="zone in filteredZones(site.zones || [])" :key="zone.securityZoneId" class="tree-branch">
                    <div class="node-stack">
                      <button
                        v-if="zone.accessPoints?.length"
                        type="button"
                        class="expand-btn"
                        @click.stop="toggleExpanded('zone', zone.securityZoneId)"
                      >
                        {{ isExpanded('zone', zone.securityZoneId) ? '−' : '+' }}
                      </button>
                    <button
                      type="button"
                      class="node-row zone-row"
                      :class="{ active: isSelected('zone', zone.securityZoneId), attention: needsAttention(zone, 'zone') }"
                      @click="selectNode('zone', zone.securityZoneId, zone)"
                    >
                      <span class="node-icon">◈</span>
                      <span class="node-main">
                        <strong>{{ zone.name }}</strong>
                        <small>{{ zone.code }}</small>
                      </span>
                      <span v-if="needsAttention(zone, 'zone')" class="node-badge">Cần cấu hình</span>
                      <span class="node-count">{{ zone.accessPoints?.length || 0 }} điểm truy cập</span>
                    </button>
                    </div>

                    <div v-if="zone.accessPoints?.length && isExpanded('zone', zone.securityZoneId)" class="tree-children">
                      <button
                        v-for="accessPoint in filteredAccessPoints(zone.accessPoints || [])"
                        :key="accessPoint.accessPointId"
                        type="button"
                        class="node-row access-row"
                        :class="{ active: isSelected('accesspoint', accessPoint.accessPointId), attention: needsAttention(accessPoint, 'accesspoint') }"
                        @click="selectNode('accesspoint', accessPoint.accessPointId, accessPoint)"
                      >
                        <span class="node-icon">↘</span>
                        <span class="node-main">
                          <strong>{{ accessPoint.name }}</strong>
                          <small>{{ accessPoint.type }}</small>
                        </span>
                        <span v-if="needsAttention(accessPoint, 'accesspoint')" class="node-badge">Rà soát</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="!hierarchy.length && !searchQuery" class="empty-card">
          Chưa có dữ liệu phân cấp. Hãy dùng công cụ nâng cao để dựng nền tảng khu vực mặc định trước.
        </div>
      </aside>

      <main class="detail-panel-shell">
        <div v-if="selectedNode && selectedData" class="detail-panel">
          <div class="detail-hero">
            <div>
              <div class="detail-type-row">
                <span class="soft-chip success">{{ selectedNodeLabel }}</span>
                <span v-if="selectedData.code" class="soft-chip">{{ selectedData.code }}</span>
                <span v-if="selectedData.isActive !== undefined" class="soft-chip" :class="selectedData.isActive ? 'success' : 'muted'">
                  {{ selectedData.isActive ? 'Hoạt động' : 'Không hoạt động' }}
                </span>
              </div>
              <h2>{{ selectedData.name }}</h2>
              <p class="detail-description">
                {{ selectedNodeDescription }}
              </p>
            </div>
            <div class="detail-hero-actions">
              <button
                type="button"
                class="btn btn-primary btn-sm"
                @click="openEditNode"
              >
                Sửa
              </button>
              <button
                type="button"
                class="btn btn-sm"
                :class="selectedData.isActive === false ? 'btn-secondary' : 'btn-danger'"
                @click="deleteSelectedNode"
              >
                {{ selectedNodeLifecycleAction }}
              </button>
              <button
                v-for="option in childOptions"
                :key="option.type"
                type="button"
                class="btn btn-secondary btn-sm"
                @click="openAddChild(option.type)"
              >
                + {{ option.label }}
              </button>
            </div>
          </div>

          <section class="detail-stat-grid">
            <article v-for="stat in selectedNodeStats" :key="stat.label" class="detail-stat-card">
              <span>{{ stat.label }}</span>
              <strong>{{ stat.value }}</strong>
            </article>
          </section>

          <section class="detail-content-grid">
            <article class="detail-card primary">
              <div class="panel-section-header compact">
                <div>
                  <span class="section-kicker">Tổng quan</span>
                  <h3>Chi tiết nút</h3>
                </div>
              </div>
              <div class="detail-grid">
                <div v-for="(value, key) in detailFields" :key="key" class="detail-field">
                  <span class="detail-label">{{ key }}</span>
                  <strong>{{ value ?? '---' }}</strong>
                </div>
              </div>
            </article>

            <article class="detail-card">
              <div class="panel-section-header compact">
                <div>
                  <span class="section-kicker">Hoạt động</span>
                  <h3>Thay đổi gần đây</h3>
                </div>
              </div>
              <div v-if="historyLoading" class="history-empty">
                Đang tải lịch sử thay đổi...
              </div>
              <div v-else-if="historyCards.length" class="history-list">
                <div v-for="item in historyCards" :key="item.id" class="history-item">
                  <div class="history-item-top">
                    <strong>{{ item.action }}</strong>
                    <span class="soft-chip" :class="item.status === 'Success' ? 'success' : 'muted'">{{ item.status === 'Success' ? 'Thành công' : 'Thất bại' }}</span>
                  </div>
                  <p>{{ item.actor }} • {{ item.timestamp }}</p>
                  <small v-if="item.path">{{ item.path }}</small>
                  <small v-if="item.reason" class="history-reason">{{ item.reason }}</small>
                </div>
              </div>
              <div v-else class="history-empty">
                Chưa có thay đổi nào được ghi nhận cho nút này.
              </div>
            </article>
          </section>
        </div>

        <div v-else class="empty-state-card">
          <span class="section-kicker">Sẵn sàng</span>
          <h2>Chọn một nút để bắt đầu</h2>
          <p>
            Chọn công ty, khu vực, tòa nhà, tầng, vùng hoặc điểm truy cập từ thanh điều hướng để xem chi tiết và tiếp tục cấu trúc hóa môi trường.
          </p>
          <button v-if="hierarchy.length" type="button" class="btn btn-primary" @click="selectNode('company', hierarchy[0].companyId, hierarchy[0])">
            Mở công ty đầu tiên
          </button>
        </div>
      </main>
    </section>

    <section v-else-if="tab === 'assets'" class="asset-workspace">
      <div class="asset-overview-card">
        <div>
          <span class="section-kicker">Độ phủ</span>
          <h2>Trạng thái ánh xạ tài sản</h2>
          <p>
            Rà soát mức độ hoàn chỉnh khi gắn tài sản vận hành vào cấu trúc doanh nghiệp trước khi chạy kiểm tra tự động hóa hoặc quản trị.
          </p>
        </div>
        <button type="button" class="btn btn-secondary btn-sm" @click="toggleAdvancedTools">
          {{ showAdvancedTools ? 'Ẩn công cụ nâng cao' : 'Hiện công cụ nâng cao' }}
        </button>
      </div>

      <div class="asset-status-grid">
        <article v-for="card in assetCards" :key="card.label" class="asset-card">
          <div class="asset-card-top">
            <h3>{{ card.label }}</h3>
            <span class="soft-chip" :class="card.unmapped === 0 ? 'success' : 'warning'">{{ card.coverageLabel }}</span>
          </div>
          <div class="asset-bar">
            <div class="asset-bar-fill mapped" :style="{ width: `${card.percent}%` }"></div>
          </div>
          <div class="asset-numbers">
            <span class="mapped">{{ card.mapped }} đã ánh xạ</span>
            <span class="unmapped">{{ card.unmapped }} chưa ánh xạ</span>
          </div>
          <p class="asset-note">{{ card.note }}</p>
        </article>
      </div>

      <div class="asset-insight-strip">
        <div class="insight-card">
          <strong>{{ mappedAssetSummary }}</strong>
          <span>tổng tài sản đã ánh xạ</span>
        </div>
        <div class="insight-card">
          <strong>{{ overallCoveragePercent }}%</strong>
          <span>tổng độ phủ</span>
        </div>
        <div class="insight-card">
          <strong>{{ riskiestAssetLabel }}</strong>
          <span>cần chú ý nhất</span>
        </div>
      </div>

      <section class="unmapped-section">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Hàng đợi hành động</span>
            <h2>Tài sản chưa ánh xạ</h2>
          </div>
          <div class="toolbar-actions">
            <button
              v-for="option in assetTypeOptions"
              :key="option.value"
              type="button"
              class="btn btn-xs"
              :class="selectedAssetFilter === option.value ? 'btn-primary' : 'btn-secondary'"
              @click="selectedAssetFilter = option.value"
            >
              {{ option.label }}
            </button>
          </div>
        </div>

        <div v-if="!assetMapLoaded" class="empty-card">Đang tải chi tiết tài sản chưa ánh xạ...</div>
        <div v-else-if="!filteredUnmappedAssets.length" class="empty-card">Toàn bộ tài sản trong bộ lọc này đã được ánh xạ.</div>
        <div v-else class="unmapped-grid">
          <article v-for="asset in filteredUnmappedAssets" :key="asset.key" class="unmapped-card">
            <div class="unmapped-top">
              <strong>{{ asset.title }}</strong>
              <span class="soft-chip warning">{{ asset.typeLabel }}</span>
            </div>
            <p class="asset-note">{{ asset.subtitle }}</p>
            <div class="unmapped-meta">
              <span v-for="tag in asset.tags" :key="tag" class="soft-chip muted">{{ tag }}</span>
            </div>
          </article>
      </div>
    </section>

    <SpatialInfrastructureWorkspace
      v-if="tab === 'spatial'"
      :site-options="siteOptions"
      :preferred-site-id="preferredSpatialSiteId"
    />

      <section v-if="showAdvancedTools" class="advanced-tools-panel">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Nâng cao</span>
            <h2>Bổ sung tài sản kế thừa</h2>
          </div>
          <span class="soft-chip warning">Hành động đặc quyền</span>
        </div>
        <p class="advanced-copy">
          Chỉ dùng khi khởi tạo hoặc sửa chữa các triển khai cũ. Công cụ này tạo cấu trúc công ty/khu vực mặc định an toàn và gắn lại tài sản kế thừa vào cấu trúc đó.
        </p>

        <div class="backfill-grid">
          <label>Mã công ty <input v-model="backfillForm.companyCode" placeholder="VSHIELD" /></label>
          <label>Mã khu vực <input v-model="backfillForm.siteCode" placeholder="HQ" /></label>
          <label>Tên công ty <input v-model="backfillForm.companyName" placeholder="V-Shield Company" /></label>
          <label>Tên khu vực <input v-model="backfillForm.siteName" placeholder="Headquarters" /></label>
        </div>

        <div class="advanced-actions">
          <button type="button" class="btn btn-primary btn-sm" :disabled="busy.backfill" @click="runBackfill">
            {{ busy.backfill ? 'Đang bổ sung dữ liệu...' : 'Chạy bổ sung an toàn' }}
          </button>
        </div>

        <div v-if="backfillReport" class="backfill-result">
          <strong>Bổ sung hoàn tất</strong>
          <div class="backfill-stats">
            <span>{{ backfillReport.gatesMapped }} cổng</span>
            <span>{{ backfillReport.cameraDevicesCreated }} camera</span>
            <span>{{ backfillReport.employeesMapped }} nhân sự</span>
            <span>{{ backfillReport.vehiclesMapped }} phương tiện</span>
            <span>{{ backfillReport.accessLogSnapshotsUpdated }} bản ghi nhật ký</span>
          </div>
        </div>
      </section>
    </section>

    <div v-if="showAddModal" class="modal-overlay" @click.self="showAddModal = false">
      <div class="modal-content modern-modal">
        <div class="modal-header">
          <div>
            <span class="section-kicker">Tạo nút con</span>
            <h2>Thêm {{ childLabel }}</h2>
          </div>
          <button type="button" class="btn-close" @click="showAddModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <p class="modal-copy">
            <template v-if="childType === 'company'">
              Tạo một không gian công ty cấp cao nhất để bắt đầu một phân cấp mới.
            </template>
            <template v-else>
              {{ childLabel }} mới sẽ được đính vào dưới <strong>{{ selectedData?.name }}</strong>.
            </template>
          </p>
          <div class="form-grid single">
            <label>Tên <input v-model="addForm.name" required /></label>
            <label v-if="childType !== 'accesspoint'">Mã <input v-model="addForm.code" placeholder="Mã ngắn (không bắt buộc)" /></label>
            <label v-if="childType === 'floor'">Thứ tự sắp xếp <input v-model.number="addForm.sortOrder" type="number" min="0" /></label>
            <label v-if="childType === 'zone'">Cấp an ninh
              <select v-model="addForm.securityLevel">
                <option value="Normal">Bình thường</option>
                <option value="Restricted">Hạn chế</option>
                <option value="HighSecurity">An ninh cao</option>
              </select>
            </label>
            <label v-if="childType === 'zone'" class="checkbox-label">
              <input v-model="addForm.isRestricted" type="checkbox" /> Vùng hạn chế
            </label>
            <label v-if="childType === 'accesspoint'">Loại
              <select v-model="addForm.type">
                <option value="Door">Cửa</option>
                <option value="Gate">Cổng</option>
                <option value="Turnstile">Cổng xoay</option>
              </select>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!addForm.name" @click="saveChild">Tạo</button>
          <button type="button" class="btn btn-secondary" @click="showAddModal = false">Hủy</button>
        </div>
      </div>
    </div>

    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal-content modern-modal">
        <div class="modal-header">
          <div>
            <span class="section-kicker">Sửa nút</span>
            <h2>{{ editTitle }}</h2>
          </div>
          <button type="button" class="btn-close" @click="showEditModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid single">
            <label>Tên <input v-model="editForm.name" required /></label>
            <label v-if="editNodeType !== 'accesspoint'">Mã <input v-model="editForm.code" /></label>
            <label v-if="editNodeType === 'site'">Địa chỉ <input v-model="editForm.address" /></label>
            <label v-if="editNodeType === 'site'">Múi giờ <input v-model="editForm.timeZoneId" /></label>
            <label v-if="editNodeType === 'floor'">Thứ tự sắp xếp <input v-model.number="editForm.sortOrder" type="number" min="0" /></label>
            <label v-if="editNodeType === 'zone'">Cấp an ninh
              <select v-model="editForm.securityLevel">
                <option value="Normal">Bình thường</option>
                <option value="Restricted">Hạn chế</option>
                <option value="HighSecurity">An ninh cao</option>
              </select>
            </label>
            <label v-if="editNodeType === 'zone'" class="checkbox-label">
              <input v-model="editForm.isRestricted" type="checkbox" /> Vùng hạn chế
            </label>
            <label v-if="editNodeType === 'accesspoint'">Loại
              <select v-model="editForm.type">
                <option value="Door">Cửa</option>
                <option value="Gate">Cổng</option>
                <option value="Turnstile">Cổng xoay</option>
              </select>
            </label>
            <label v-if="editNodeType === 'accesspoint'">Chiều
              <select v-model="editForm.directionMode">
                <option value="Bidirectional">Hai chiều</option>
                <option value="EntryOnly">Chỉ vào</option>
                <option value="ExitOnly">Chỉ ra</option>
              </select>
            </label>
            <label class="checkbox-label">
              <input v-model="editForm.isActive" type="checkbox" /> Hoạt động
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!editForm.name" @click="saveEditNode">Lưu</button>
          <button type="button" class="btn btn-secondary" @click="showEditModal = false">Hủy</button>
        </div>
      </div>
    </div>

    <ImportModal v-if="showImportModal" :entity-type="ieEntity" :entity-display-name="ieDisplayName" @close="showImportModal = false" @import-complete="onImportComplete" />
    <ExportModal v-if="showExportModal" :entity-type="ieEntity" :entity-display-name="ieDisplayName" :available-columns="ieColumns" @close="showExportModal = false" />
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import SpatialInfrastructureWorkspace from '../components/site-hierarchy/SpatialInfrastructureWorkspace.vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { getSystemAuditLogs } from '../services/accessLogApi'
import ImportModal from '../components/import-export/ImportModal.vue'
import ExportModal from '../components/import-export/ExportModal.vue'

const loading = ref(false)
const tab = ref('tree')
const showImportModal = ref(false)
const showExportModal = ref(false)
const ieEntity = ref('Company')
const ieDisplayName = computed(() => ({ Company: 'Công ty', Site: 'Khu vực', Building: 'Tòa nhà', FacilityFloor: 'Tầng', SecurityZone: 'Vùng an ninh' }[ieEntity.value] || ieEntity.value))
const ieColumns = computed(() => {
  switch (ieEntity.value) {
    case 'Site': return ['SiteId', 'Name', 'Code', 'CompanyCode', 'Address', 'TimeZoneId', 'IsActive']
    case 'Building': return ['BuildingId', 'Name', 'Code', 'SiteCode', 'TotalFloors', 'IsActive']
    case 'FacilityFloor': return ['FacilityFloorId', 'Name', 'Code', 'BuildingCode', 'SortOrder', 'IsActive']
    case 'SecurityZone': return ['SecurityZoneId', 'Name', 'Code', 'SiteCode', 'SecurityLevel', 'IsRestricted', 'IsActive']
    default: return ['CompanyId', 'Name', 'Code', 'IsActive']
  }
})
const hierarchy = ref([])
const overview = reactive({ companies: 0, sites: 0, buildings: 0, floors: 0, zones: 0, accessPoints: 0, doors: 0, lanes: 0 })
const selectedNode = ref(null)
const selectedData = ref(null)
const searchQuery = ref('')
const searchResults = ref([])
const assetStatus = ref(null)
const assetMap = ref(null)
const assetMapLoaded = ref(false)
const historyLoading = ref(false)
const selectedHistory = ref([])
const showAdvancedTools = ref(false)
const showNeedsAttentionOnly = ref(false)
const selectedAssetFilter = ref('all')
const expandedNodes = ref({
  company: new Set(),
  site: new Set(),
  building: new Set(),
  zone: new Set(),
})

const busy = reactive({ backfill: false })
const backfillForm = reactive({ companyName: 'V-Shield Company', companyCode: 'VSHIELD', siteName: 'Headquarters', siteCode: 'HQ', timeZoneId: 'Asia/Ho_Chi_Minh' })
const backfillReport = ref(null)

const showAddModal = ref(false)
const childType = ref('site')
const childLabel = ref('Khu vực')
const addForm = reactive({ name: '', code: '', sortOrder: 0, securityLevel: 'Normal', isRestricted: false, type: 'Door' })
const showEditModal = ref(false)
const editNodeType = ref('company')
const editTitle = ref('Sửa nút')
const editForm = reactive({
  name: '',
  code: '',
  address: '',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  sortOrder: 0,
  securityLevel: 'Normal',
  isRestricted: false,
  type: 'Door',
  directionMode: 'Bidirectional',
  isActive: true,
})

const topMetrics = computed(() => ([
  { label: 'Công ty', value: overview.companies, note: 'đơn vị cấp cao nhất' },
  { label: 'Khu vực', value: overview.sites, note: 'cơ sở vật lý' },
  { label: 'Tòa nhà', value: overview.buildings, note: 'cấu trúc đã ánh xạ' },
  { label: 'Vùng', value: overview.zones, note: 'vành đai an ninh' },
  { label: 'Điểm truy cập', value: overview.accessPoints, note: 'cửa, cổng, cổng xoay' },
  { label: 'Cửa / Làn', value: `${overview.doors} / ${overview.lanes}`, note: 'điểm vận hành cuối' },
]))

const selectedNodeLabel = computed(() => {
  const map = {
    company: 'Công ty',
    site: 'Khu vực',
    building: 'Tòa nhà',
    floor: 'Tầng',
    zone: 'Vùng an ninh',
    accesspoint: 'Điểm truy cập',
  }
  return selectedNode.value ? map[selectedNode.value.type] || 'Nút' : 'Nút'
})

const selectedNodeDescription = computed(() => {
  if (!selectedNode.value) return ''
  const map = {
    company: 'Vùng chứa cấp công ty cho toàn bộ khu vực và tài sản vận hành bên dưới.',
    site: 'Không gian cấp khu vực nhóm các tòa nhà, vùng và tài sản vận hành tại chỗ.',
    building: 'Bản ghi tòa nhà dùng để sắp xếp tầng và bối cảnh bố trí vật lý.',
    floor: 'Nút cấp tầng để sắp thứ tự và định hướng cơ sở vật chất trong khu vực.',
    zone: 'Vùng an ninh dùng để nhóm rủi ro, kiểm soát truy cập và điểm ra vào.',
    accesspoint: 'Điểm vận hành cuối nơi quyết định truy cập và ánh xạ thiết bị hội tụ.',
  }
  return map[selectedNode.value.type] || 'Chi tiết nút phân cấp.'
})

const childOptions = computed(() => {
  if (!selectedNode.value) return []
  const map = {
    company: [{ type: 'site', label: 'Khu vực' }],
    site: [{ type: 'building', label: 'Tòa nhà' }, { type: 'zone', label: 'Vùng an ninh' }],
    building: [{ type: 'floor', label: 'Tầng' }],
    zone: [{ type: 'accesspoint', label: 'Điểm truy cập' }],
  }
  return map[selectedNode.value.type] || []
})

const selectedNodeLifecycleAction = computed(() => (selectedData.value?.isActive === false ? 'Khôi phục' : 'Vô hiệu hóa'))

const selectedNodeStats = computed(() => {
  if (!selectedData.value || !selectedNode.value) return []
  const data = selectedData.value
  const type = selectedNode.value.type

  if (type === 'company') {
    return [
      { label: 'Khu vực', value: data.sites?.length || 0 },
      { label: 'Tòa nhà', value: (data.sites || []).reduce((sum, site) => sum + (site.buildings?.length || 0), 0) },
      { label: 'Vùng', value: (data.sites || []).reduce((sum, site) => sum + (site.zones?.length || 0), 0) },
    ]
  }
  if (type === 'site') {
    return [
      { label: 'Tòa nhà', value: data.buildings?.length || 0 },
      { label: 'Vùng', value: data.zones?.length || 0 },
      { label: 'Có địa chỉ', value: data.address ? 'Có' : 'Không' },
    ]
  }
  if (type === 'building') {
    return [
      { label: 'Tầng', value: data.floors?.length || 0 },
      { label: 'Hoạt động', value: data.isActive ? 'Có' : 'Không' },
      { label: 'Mã', value: data.code || '---' },
    ]
  }
  if (type === 'zone') {
    return [
      { label: 'Điểm truy cập', value: data.accessPoints?.length || 0 },
      { label: 'Cấp an ninh', value: data.securityLevel || 'Normal' },
      { label: 'Giới hạn', value: data.isRestricted ? 'Có' : 'Không' },
    ]
  }
  if (type === 'accesspoint') {
    return [
      { label: 'Loại', value: data.type || '---' },
      { label: 'Chiều', value: data.directionMode || '---' },
      { label: 'Hoạt động', value: data.isActive ? 'Có' : 'Không' },
    ]
  }
  return [
    { label: 'Mã', value: data.code || '---' },
    { label: 'Thứ tự sắp xếp', value: data.sortOrder ?? '---' },
    { label: 'Hoạt động', value: data.isActive ? 'Có' : 'Không' },
  ]
})

const historyCards = computed(() =>
  (selectedHistory.value || []).map((item) => ({
    id: item.id,
    action: item.actionType || 'UPDATE',
    actor: item.username || 'Hệ thống',
    status: item.isSuccess ? 'Success' : 'Failed',
    reason: item.failureReason || null,
    path: item.path || null,
    timestamp: formatAuditTimestamp(item.timestampUtc),
  }))
)

const detailFields = computed(() => {
  if (!selectedData.value) return {}
  const data = selectedData.value
  const fields = {}
  fields['Tên'] = data.name
  if (data.code) fields['Mã'] = data.code
  if (data.address) fields['Địa chỉ'] = data.address
  if (data.timeZoneId) fields['Múi giờ'] = data.timeZoneId
  if (data.isActive !== undefined) fields['Trạng thái'] = data.isActive ? 'Hoạt động' : 'Không hoạt động'
  if (data.type) fields['Loại'] = data.type
  if (data.securityLevel) fields['Cấp an ninh'] = data.securityLevel
  if (data.isRestricted !== undefined) fields['Giới hạn'] = data.isRestricted ? 'Có' : 'Không'
  if (data.directionMode) fields['Chiều'] = data.directionMode
  if (data.sortOrder !== undefined) fields['Thứ tự sắp xếp'] = data.sortOrder
  return fields
})

const assetCards = computed(() => {
  const status = assetStatus.value
  if (!status) return []
  return [
    {
      label: 'Cổng',
      mapped: status.gatesMapped || 0,
      unmapped: status.gatesUnmapped || 0,
      total: status.totalGates || 0,
      percent: percent(status.gatesMapped, status.totalGates),
      coverageLabel: `${percent(status.gatesMapped, status.totalGates)}% độ phủ`,
      note: 'Tài sản cổng cần được liên kết trước khi tự động hóa barrier và làn đường.',
    },
    {
      label: 'Camera',
      mapped: status.camerasMapped || 0,
      unmapped: status.camerasUnmapped || 0,
      total: status.totalCameras || 0,
      percent: percent(status.camerasMapped, status.totalCameras),
      coverageLabel: `${percent(status.camerasMapped, status.totalCameras)}% độ phủ`,
      note: 'Ánh xạ camera cải thiện việc đối chiếu sự cố và rà soát cấp khu vực.',
    },
    {
      label: 'Phương tiện',
      mapped: status.vehiclesMapped || 0,
      unmapped: status.vehiclesUnmapped || 0,
      total: status.totalVehicles || 0,
      percent: percent(status.vehiclesMapped, status.totalVehicles),
      coverageLabel: `${percent(status.vehiclesMapped, status.totalVehicles)}% độ phủ`,
      note: 'Ánh xạ phương tiện giữ việc đỗ xe và kiểm soát truy cập gắn đúng khu vực.',
    },
  ]
})

const mappedAssetSummary = computed(() => {
  if (!assetStatus.value) return 'Chưa tải'
  const mapped = (assetStatus.value.gatesMapped || 0) + (assetStatus.value.camerasMapped || 0) + (assetStatus.value.vehiclesMapped || 0)
  const total = (assetStatus.value.totalGates || 0) + (assetStatus.value.totalCameras || 0) + (assetStatus.value.totalVehicles || 0)
  return `${mapped}/${total || 0}`
})

const assetTypeOptions = [
  { label: 'Tất cả', value: 'all' },
  { label: 'Cổng', value: 'gate' },
  { label: 'Camera', value: 'camera' },
  { label: 'Phương tiện', value: 'vehicle' },
]

const overallCoveragePercent = computed(() => {
  if (!assetStatus.value) return 0
  const mapped = (assetStatus.value.gatesMapped || 0) + (assetStatus.value.camerasMapped || 0) + (assetStatus.value.vehiclesMapped || 0)
  const total = (assetStatus.value.totalGates || 0) + (assetStatus.value.totalCameras || 0) + (assetStatus.value.totalVehicles || 0)
  return percent(mapped, total)
})

const riskiestAssetLabel = computed(() => {
  if (!assetCards.value.length) return 'Chờ dữ liệu'
  const sorted = [...assetCards.value].sort((a, b) => b.unmapped - a.unmapped)
  return sorted[0]?.label || 'Chờ dữ liệu'
})

const unmappedAssets = computed(() => {
  if (!assetMap.value) return []
  const gates = (assetMap.value.gates || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `gate-${item.gateId}`,
      type: 'gate',
      typeLabel: 'Cổng',
      title: item.gateName,
      subtitle: item.location || 'Chưa có khu vực hoặc làn được ánh xạ.',
      tags: [`Cổng #${item.gateId}`],
    }))
  const cameras = (assetMap.value.cameras || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `camera-${item.cameraId}`,
      type: 'camera',
      typeLabel: 'Camera',
      title: item.cameraName,
      subtitle: item.cameraType || 'Chưa có khu vực hoặc liên kết thiết bị.',
      tags: [`Camera #${item.cameraId}`, item.gateId ? `Cổng ${item.gateId}` : 'Chưa liên kết cổng'],
    }))
  const vehicles = (assetMap.value.vehicles || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `vehicle-${item.vehicleId}`,
      type: 'vehicle',
      typeLabel: 'Phương tiện',
      title: item.licensePlate,
      subtitle: item.employeeName || 'Chưa gán chủ sở hữu là nhân sự.',
      tags: [`Phương tiện #${item.vehicleId}`, item.parkingStatus || 'Không rõ trạng thái đỗ'],
    }))
  return [...gates, ...cameras, ...vehicles]
})

const filteredUnmappedAssets = computed(() => {
  if (selectedAssetFilter.value === 'all') return unmappedAssets.value
  return unmappedAssets.value.filter((item) => item.type === selectedAssetFilter.value)
})

const siteOptions = computed(() =>
  hierarchy.value.flatMap((company) =>
    (company.sites || []).map((site) => ({
      siteId: site.siteId,
      name: site.name,
      code: site.code,
    }))
  )
)

const preferredSpatialSiteId = computed(() => {
  if (!selectedNode.value || !selectedData.value) return null
  if (selectedNode.value.type === 'site') return selectedData.value.siteId
  if (selectedNode.value.type === 'building') return selectedData.value.siteId
  if (selectedNode.value.type === 'floor') return selectedData.value.siteId
  if (selectedNode.value.type === 'zone') return selectedData.value.siteId
  if (selectedNode.value.type === 'accesspoint') return selectedData.value.siteId
  return null
})

async function loadAll() {
  loading.value = true
  await Promise.all([loadOverview(), loadHierarchy(), loadAssetStatus(), loadAssetMap()])
  loading.value = false
}

function onImportComplete(result) {
  showImportModal.value = false
  loadAll()
}

async function loadOverview() {
  try {
    const res = await enterpriseApi.overview()
    if (res?.length && res[0]?.data) Object.assign(overview, res[0].data)
  } catch {}
}

async function loadHierarchy() {
  try {
    const res = await enterpriseApi.getHierarchy()
    hierarchy.value = normalizeHierarchy(res.data || [])
    if (!selectedNode.value && hierarchy.value.length) {
      selectNode('company', hierarchy.value[0].companyId, hierarchy.value[0])
    } else if (selectedNode.value) {
      const refreshed = findNodeRecord(selectedNode.value.type, selectedNode.value.id)
      if (refreshed) selectedData.value = refreshed
    }
  } catch {
    hierarchy.value = []
  }
}

async function loadAssetStatus() {
  try {
    const res = await enterpriseApi.getBackfillStatus()
    assetStatus.value = res.data
  } catch {}
}

async function loadAssetMap() {
  try {
    const res = await enterpriseApi.assetMap()
    assetMap.value = normalizeAssetMap(res.data)
  } catch {
    assetMap.value = { gates: [], cameras: [], vehicles: [] }
  } finally {
    assetMapLoaded.value = true
  }
}

async function searchNodes() {
  const query = searchQuery.value.trim()
  if (!query || query.length < 2) {
    searchResults.value = []
    return
  }
  const types = ['company', 'site', 'building', 'zone', 'accesspoint']
  const all = await Promise.all(types.map((type) => enterpriseApi.searchHierarchy(type, query).catch(() => ({ data: [] }))))
  searchResults.value = all.flatMap((result) => result.data || []).slice(0, 30)
}

function selectSearchResult(result) {
  const typeMap = { Company: 'company', Site: 'site', Building: 'building', Zone: 'zone', AccessPoint: 'accesspoint' }
  const type = typeMap[result.entityType] || 'site'
  const id = result.companyId || result.siteId || result.buildingId || result.securityZoneId || result.accessPointId
  const record = findNodeRecord(type, id)
  selectNode(type, id, record || result)
  searchQuery.value = ''
  searchResults.value = []
}

function selectNode(type, id, data) {
  selectedNode.value = { type, id }
  selectedData.value = data
  loadSelectedNodeHistory()
}

function isSelected(type, id) {
  return selectedNode.value?.type === type && selectedNode.value?.id === id
}

function focusStructureTab() {
  tab.value = 'tree'
}

async function openAssetTab() {
  tab.value = 'assets'
  if (!assetStatus.value) await loadAssetStatus()
  if (!assetMapLoaded.value) await loadAssetMap()
}

function toggleAdvancedTools() {
  showAdvancedTools.value = !showAdvancedTools.value
}

function toggleExpanded(type, id) {
  const current = expandedNodes.value[type]
  if (!current) return
  if (current.has(id)) current.delete(id)
  else current.add(id)
}

function isExpanded(type, id) {
  return expandedNodes.value[type]?.has(id)
}

function expandAll() {
  expandedNodes.value.company = new Set(hierarchy.value.map((company) => company.companyId))
  expandedNodes.value.site = new Set(hierarchy.value.flatMap((company) => (company.sites || []).map((site) => site.siteId)))
  expandedNodes.value.building = new Set(hierarchy.value.flatMap((company) => (company.sites || []).flatMap((site) => (site.buildings || []).map((building) => building.buildingId))))
  expandedNodes.value.zone = new Set(hierarchy.value.flatMap((company) => (company.sites || []).flatMap((site) => (site.zones || []).map((zone) => zone.securityZoneId))))
}

function collapseAll() {
  expandedNodes.value.company = new Set()
  expandedNodes.value.site = new Set()
  expandedNodes.value.building = new Set()
  expandedNodes.value.zone = new Set()
}

function openAddChild(nextType) {
  if (!selectedNode.value) return
  const option = childOptions.value.find((item) => item.type === nextType) || childOptions.value[0]
  if (!option) return
  childType.value = option.type
  childLabel.value = option.label
  addForm.name = ''
  addForm.code = ''
  addForm.sortOrder = 0
  addForm.securityLevel = 'Normal'
  addForm.isRestricted = false
  addForm.type = 'Door'
  showAddModal.value = true
}

function openSpatialTab() {
  tab.value = 'spatial'
}

function openCreateCompany() {
  selectedNode.value = null
  selectedData.value = null
  childType.value = 'company'
  childLabel.value = 'Công ty'
  addForm.name = ''
  addForm.code = ''
  addForm.sortOrder = 0
  addForm.securityLevel = 'Normal'
  addForm.isRestricted = false
  addForm.type = 'Door'
  showAddModal.value = true
}

function openEditNode() {
  if (!selectedNode.value || !selectedData.value) return
  editNodeType.value = selectedNode.value.type
  editTitle.value = `Sửa ${selectedNodeLabel.value}`
  editForm.name = selectedData.value.name || ''
  editForm.code = selectedData.value.code || ''
  editForm.address = selectedData.value.address || ''
  editForm.timeZoneId = selectedData.value.timeZoneId || 'Asia/Ho_Chi_Minh'
  editForm.sortOrder = selectedData.value.sortOrder ?? 0
  editForm.securityLevel = selectedData.value.securityLevel || 'Normal'
  editForm.isRestricted = !!selectedData.value.isRestricted
  editForm.type = selectedData.value.type || 'Door'
  editForm.directionMode = selectedData.value.directionMode || 'Bidirectional'
  editForm.isActive = selectedData.value.isActive !== false
  showEditModal.value = true
}

async function saveChild() {
  const parent = selectedNode.value
  const parentData = selectedData.value
  if (childType.value !== 'company' && (!parent || !parentData)) return
  if (!addForm.name) return

  try {
    switch (childType.value) {
      case 'company':
        await enterpriseApi.createCompany({
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'site':
        await enterpriseApi.createSite({
          companyId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'building':
        await enterpriseApi.createBuilding({
          siteId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'floor':
        await enterpriseApi.createFloor({
          buildingId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
          sortOrder: addForm.sortOrder,
        })
        break
      case 'zone':
        await enterpriseApi.createZone({
          siteId: parent.id,
          buildingId: null,
          facilityFloorId: null,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
          securityLevel: addForm.securityLevel,
          isRestricted: addForm.isRestricted,
        })
        break
      case 'accesspoint':
        await enterpriseApi.createAccessPoint({
          siteId: parentData.siteId,
          securityZoneId: parent.id,
          name: addForm.name,
          type: addForm.type,
          directionMode: 'Bidirectional',
        })
        break
    }
    showAddModal.value = false
    await loadHierarchy()
  } catch (err) {
    alert(err.response?.data?.message || 'Không thể tạo mới.')
  }
}

async function saveEditNode() {
  if (!selectedNode.value || !selectedData.value || !editForm.name) return

  try {
    switch (editNodeType.value) {
      case 'company':
        await enterpriseApi.updateCompany(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          isActive: editForm.isActive,
        })
        break
      case 'site':
        await enterpriseApi.updateSite(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          address: editForm.address || null,
          timeZoneId: editForm.timeZoneId || 'Asia/Ho_Chi_Minh',
          isActive: editForm.isActive,
        })
        break
      case 'building':
        await enterpriseApi.updateBuilding(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          isActive: editForm.isActive,
        })
        break
      case 'floor':
        await enterpriseApi.updateFloor(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          sortOrder: editForm.sortOrder,
          isActive: editForm.isActive,
        })
        break
      case 'zone':
        await enterpriseApi.updateZone(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          siteId: selectedData.value.siteId,
          buildingId: selectedData.value.buildingId || null,
          facilityFloorId: selectedData.value.facilityFloorId || null,
          securityLevel: editForm.securityLevel,
          isRestricted: editForm.isRestricted,
          isActive: editForm.isActive,
        })
        break
      case 'accesspoint':
        await enterpriseApi.updateAccessPoint(selectedNode.value.id, {
          name: editForm.name,
          siteId: selectedData.value.siteId,
          securityZoneId: selectedData.value.securityZoneId || null,
          type: editForm.type,
          directionMode: editForm.directionMode,
          isActive: editForm.isActive,
        })
        break
    }
    showEditModal.value = false
    await loadHierarchy()
    await loadSelectedNodeHistory()
  } catch (err) {
    alert(err.response?.data?.message || 'Không thể lưu thay đổi.')
  }
}

async function deleteSelectedNode() {
  if (!selectedNode.value || !selectedData.value) return
  const actionLabel = selectedData.value.isActive === false ? 'restore' : 'deactivate'
  const ok = confirm(`${actionLabel === 'restore' ? 'Khôi phục' : 'Vô hiệu hóa'} ${selectedNodeLabel.value.toLowerCase()} "${selectedData.value.name}"?`)
  if (!ok) return

  try {
    if (selectedData.value.isActive === false) {
      switch (selectedNode.value.type) {
        case 'company':
          await enterpriseApi.restoreCompany(selectedNode.value.id)
          break
        case 'site':
          await enterpriseApi.restoreSite(selectedNode.value.id)
          break
        case 'building':
          await enterpriseApi.restoreBuilding(selectedNode.value.id)
          break
        case 'floor':
          await enterpriseApi.restoreFloor(selectedNode.value.id)
          break
        case 'zone':
          await enterpriseApi.restoreZone(selectedNode.value.id)
          break
        case 'accesspoint':
          await enterpriseApi.restoreAccessPoint(selectedNode.value.id)
          break
        default:
          return
      }
    } else {
      switch (selectedNode.value.type) {
        case 'company':
          await enterpriseApi.deleteCompany(selectedNode.value.id)
          break
        case 'site':
          await enterpriseApi.deleteSite(selectedNode.value.id)
          break
        case 'building':
          await enterpriseApi.deleteBuilding(selectedNode.value.id)
          break
        case 'floor':
          await enterpriseApi.deleteFloor(selectedNode.value.id)
          break
        case 'zone':
          await enterpriseApi.deleteZone(selectedNode.value.id)
          break
        case 'accesspoint':
          await enterpriseApi.deleteAccessPoint(selectedNode.value.id)
          break
        default:
          return
      }
    }
    await loadHierarchy()
    await loadSelectedNodeHistory()
  } catch (err) {
    alert(err.response?.data?.message || `Không thể ${actionLabel === 'restore' ? 'khôi phục' : 'vô hiệu hóa'} nút`)
  }
}

async function runBackfill() {
  busy.backfill = true
  try {
    const res = await enterpriseApi.backfillDefaultSite(backfillForm)
    backfillReport.value = res.data
    await loadAll()
  } catch (err) {
    alert(err.response?.data?.message || 'Bổ sung dữ liệu thất bại.')
  } finally {
    busy.backfill = false
  }
}

function normalizeHierarchy(companies) {
  const normalized = (companies || []).map((company) => ({
    ...company,
    sites: (company.sites || []).map((site) => ({
      ...site,
      companyId: company.companyId,
      companyName: company.name,
      buildings: (site.buildings || []).map((building) => ({
        ...building,
        siteId: site.siteId,
        siteName: site.name,
        floors: (building.floors || []).map((floor) => ({
          ...floor,
          buildingId: building.buildingId,
          buildingName: building.name,
          siteId: site.siteId,
          siteName: site.name,
        })),
      })),
      zones: (site.zones || []).map((zone) => ({
        ...zone,
        siteId: site.siteId,
        siteName: site.name,
        accessPoints: (zone.accessPoints || []).map((accessPoint) => ({
          ...accessPoint,
          siteId: site.siteId,
          siteName: site.name,
          securityZoneId: zone.securityZoneId,
          securityZoneName: zone.name,
        })),
      })),
    })),
  }))
  if (!expandedNodes.value.company.size && normalized.length) {
    expandedNodes.value.company = new Set(normalized.map((company) => company.companyId))
  }
  return normalized
}

function findNodeRecord(type, id) {
  for (const company of hierarchy.value) {
    if (type === 'company' && company.companyId === id) return company
    for (const site of company.sites || []) {
      if (type === 'site' && site.siteId === id) return site
      for (const building of site.buildings || []) {
        if (type === 'building' && building.buildingId === id) return building
        for (const floor of building.floors || []) {
          if (type === 'floor' && floor.facilityFloorId === id) return floor
        }
      }
      for (const zone of site.zones || []) {
        if (type === 'zone' && zone.securityZoneId === id) return zone
        for (const accessPoint of zone.accessPoints || []) {
          if (type === 'accesspoint' && accessPoint.accessPointId === id) return accessPoint
        }
      }
    }
  }
  return null
}

function percent(value, total) {
  if (!total) return 0
  return Math.round((value / total) * 100)
}

function normalizeAssetMap(data) {
  return {
    gates: data?.gates || [],
    cameras: data?.cameras || [],
    vehicles: data?.vehicles || [],
  }
}

function getAuditEntityMeta(type, id) {
  const entityNames = {
    company: 'Company',
    site: 'Site',
    building: 'Building',
    floor: 'FacilityFloor',
    zone: 'SecurityZone',
    accesspoint: 'AccessPoint',
  }
  return {
    entityName: entityNames[type] || null,
    entityId: id != null ? String(id) : null,
  }
}

async function loadSelectedNodeHistory() {
  if (!selectedNode.value?.type || selectedNode.value?.id == null) {
    selectedHistory.value = []
    return
  }

  const { entityName, entityId } = getAuditEntityMeta(selectedNode.value.type, selectedNode.value.id)
  if (!entityName || !entityId) {
    selectedHistory.value = []
    return
  }

  historyLoading.value = true
  try {
    const res = await getSystemAuditLogs({
      page: 1,
      pageSize: 8,
      entityName,
      entityId,
    })
    selectedHistory.value = res.data?.items || []
  } catch {
    selectedHistory.value = []
  } finally {
    historyLoading.value = false
  }
}

function formatAuditTimestamp(value) {
  if (!value) return 'Không rõ thời gian'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Không rõ thời gian'
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function needsAttention(node, type) {
  if (!showNeedsAttentionOnly.value) {
    if (type === 'company') return !node.sites?.length
    if (type === 'site') return !node.buildings?.length || !node.zones?.length
    if (type === 'building') return !node.floors?.length
    if (type === 'zone') return !node.accessPoints?.length
    if (type === 'floor') return !node.code
    if (type === 'accesspoint') return node.isActive === false
    return false
  }
  if (type === 'company') return !node.sites?.length || filteredSites(node.sites || []).length > 0
  if (type === 'site') return !node.buildings?.length || !node.zones?.length
  if (type === 'building') return !node.floors?.length
  if (type === 'zone') return !node.accessPoints?.length
  if (type === 'floor') return !node.code
  if (type === 'accesspoint') return node.isActive === false
  return false
}

function filteredSites(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'site') || filteredBuildings(item.buildings || []).length > 0 || filteredZones(item.zones || []).length > 0) : items
}

function filteredBuildings(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'building') || filteredFloors(item.floors || []).length > 0) : items
}

function filteredFloors(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'floor')) : items
}

function filteredZones(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'zone') || filteredAccessPoints(item.accessPoints || []).length > 0) : items
}

function filteredAccessPoints(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'accesspoint')) : items
}

function siteHasVisibleChildren(site) {
  return filteredBuildings(site.buildings || []).length > 0 || filteredZones(site.zones || []).length > 0
}

onMounted(loadAll)
</script>

<style scoped>
.site-hierarchy-page {
  max-width: 1380px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.hero-panel {
  display: grid;
  grid-template-columns: minmax(0, 1.6fr) minmax(260px, 0.8fr);
  gap: 18px;
  padding: 24px 26px;
  border-radius: 28px;
  background:
    radial-gradient(circle at top right, rgba(15, 118, 110, 0.16), transparent 30%),
    linear-gradient(135deg, #f8fbfd 0%, #eef7f6 52%, #f6f8fb 100%);
  border: 1px solid var(--border-default);
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.06);
}

.hero-copy {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.hero-text {
  max-width: 760px;
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.65;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.hero-side {
  display: grid;
  gap: 12px;
  align-content: start;
}

.hero-stat {
  padding: 16px 18px;
  border-radius: 18px;
  background: var(--surface-default);
  border: 1px solid var(--border-default);
}

.hero-stat strong {
  display: block;
  font-size: 1.5rem;
  color: var(--text-primary);
}

.hero-stat span {
  display: block;
  margin-top: 4px;
  color: var(--text-muted);
  font-size: 0.88rem;
}

.hero-stat.highlight {
  background: linear-gradient(135deg, #0f766e 0%, #0f766e 14%, #155e75 100%);
}

.hero-stat.highlight strong,
.hero-stat.highlight span {
  color: var(--text-inverse);
}

.metric-strip {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 14px;
}

.metric-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.05);
}

.metric-label {
  display: block;
  color: var(--text-muted);
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.metric-value {
  display: block;
  margin-top: 8px;
  font-size: 1.35rem;
  color: var(--text-primary);
}

.metric-note {
  display: block;
  margin-top: 6px;
  color: var(--text-disabled);
  font-size: 0.8rem;
}

.workspace-shell {
  display: grid;
  grid-template-columns: minmax(320px, 380px) minmax(0, 1fr);
  gap: 18px;
}

.navigator-panel,
.detail-panel,
.empty-state-card,
.asset-overview-card,
.advanced-tools-panel {
  border-radius: 24px;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.05);
}

.navigator-panel {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 720px;
}

.navigator-toolbar,
.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  justify-content: space-between;
  flex-wrap: wrap;
}

.attention-toggle {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: var(--text-secondary);
  font-size: 0.84rem;
}

.attention-toggle input {
  width: 16px;
  height: 16px;
}

.panel-section-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 12px;
}

.panel-section-header h2,
.panel-section-header h3 {
  margin: 2px 0 0;
  font-size: 1.05rem;
  color: var(--text-primary);
}

.panel-section-header.compact {
  margin-bottom: 14px;
}

.section-kicker {
  color: var(--accent-primary);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.search-box {
  display: grid;
  gap: 8px;
  color: var(--text-secondary);
  font-size: 0.88rem;
}

.search-box input,
.backfill-grid input,
.form-grid input,
.form-grid select {
  width: 100%;
  min-height: 42px;
  padding: 0 14px;
  border-radius: 14px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-subtle);
  color: var(--text-primary);
}

.search-results {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.search-hit {
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  text-align: left;
  border: 1px solid var(--border-subtle);
  border-radius: 14px;
  background: linear-gradient(180deg, #fcfefe 0%, #f5fbfb 100%);
  cursor: pointer;
  transition: transform var(--transition-fast), box-shadow var(--transition-fast), border-color var(--transition-fast);
}

.search-hit:hover {
  border-color: var(--border-color-hover);
  box-shadow: var(--shadow-sm);
  transform: translateY(-1px);
}

.search-hit strong {
  color: var(--text-primary);
}

.hit-meta,
.hit-parent,
.empty-inline {
  color: var(--text-muted);
  font-size: 0.82rem;
}

.tree-root {
  display: flex;
  flex-direction: column;
  gap: 10px;
  overflow: auto;
  padding-right: 4px;
}

.tree-branch {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.node-stack {
  display: flex;
  align-items: stretch;
  gap: 8px;
}

.expand-btn {
  width: 32px;
  border: 1px solid var(--border-subtle);
  border-radius: 12px;
  background: var(--surface-default);
  color: var(--accent-primary);
  font-weight: 700;
  cursor: pointer;
  flex-shrink: 0;
  transition: border-color var(--transition-fast), background var(--transition-fast), transform var(--transition-fast);
}

.expand-btn:hover {
  border-color: var(--border-color-hover);
  background: var(--surface-hover);
  transform: translateY(-1px);
}

.tree-children {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-left: 18px;
  border-left: 1px dashed var(--border-subtle);
  margin-left: 10px;
}

.node-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 11px 12px;
  border-radius: 16px;
  border: 1px solid transparent;
  background: var(--surface-subtle);
  color: var(--text-primary);
  cursor: pointer;
  transition: all 0.18s ease;
}

.node-row:hover {
  border-color: rgba(15, 118, 110, 0.16);
  background: var(--surface-hover);
  transform: translateX(2px);
}

.node-row.active {
  background: linear-gradient(135deg, rgba(15, 118, 110, 0.13), rgba(21, 94, 117, 0.08));
  border-color: rgba(15, 118, 110, 0.28);
}

.node-row.attention {
  border-color: rgba(245, 158, 11, 0.32);
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.08), rgba(251, 191, 36, 0.04));
}

.node-icon {
  display: inline-flex;
  width: 28px;
  height: 28px;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: var(--surface-default);
  color: var(--accent-primary);
  flex-shrink: 0;
}

.node-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.node-main strong {
  font-size: 0.92rem;
}

.node-main small,
.node-count {
  color: var(--text-muted);
  font-size: 0.78rem;
}

.node-count {
  margin-left: auto;
  white-space: nowrap;
}

.node-badge {
  padding: 3px 8px;
  border-radius: 999px;
  background: var(--status-warning-bg);
  color: var(--accent-warning);
  font-size: 0.72rem;
  font-weight: 700;
}

.detail-panel-shell {
  min-height: 720px;
}

.detail-panel,
.empty-state-card {
  height: 100%;
  padding: 22px;
}

.detail-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.detail-hero {
  display: flex;
  justify-content: space-between;
  align-items: start;
  gap: 18px;
  padding: 20px;
  border-radius: 22px;
  background: linear-gradient(135deg, #f8fbff 0%, #f5faf9 100%);
  border: 1px solid var(--border-subtle);
}

.detail-type-row,
.detail-hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.detail-hero h2 {
  margin: 10px 0 6px;
  font-size: 1.55rem;
  color: var(--text-primary);
}

.detail-description,
.modal-copy,
.advanced-copy,
.empty-state-card p,
.asset-overview-card p,
.asset-note {
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.6;
}

.detail-stat-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.detail-stat-card,
.insight-card {
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-default);
}

.detail-stat-card span,
.insight-card span {
  display: block;
  color: var(--text-muted);
  font-size: 0.82rem;
}

.detail-stat-card strong,
.insight-card strong {
  display: block;
  margin-top: 8px;
  color: var(--text-primary);
  font-size: 1.2rem;
}

.detail-content-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.25fr) minmax(280px, 0.75fr);
  gap: 16px;
}

.detail-card {
  padding: 20px;
  border-radius: 22px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-default);
}

.detail-card.primary {
  background: linear-gradient(180deg, #ffffff 0%, #fbfdff 100%);
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.detail-field {
  padding: 14px;
  border-radius: 16px;
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
}

.detail-label {
  display: block;
  color: var(--text-muted);
  font-size: 0.8rem;
  margin-bottom: 6px;
}

.action-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.action-tip {
  display: grid;
  gap: 6px;
  padding: 14px;
  border-radius: 16px;
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
}

.action-tip strong {
  color: var(--text-primary);
}

.action-tip span {
  color: var(--text-muted);
  line-height: 1.55;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-item {
  display: grid;
  gap: 6px;
  padding: 14px;
  border-radius: 16px;
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
}

.history-item-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.history-item p,
.history-item small,
.history-empty {
  margin: 0;
  color: var(--text-muted);
  line-height: 1.55;
}

.history-reason {
  color: var(--accent-warning);
}

.empty-state-card {
  display: grid;
  align-content: center;
  justify-items: start;
  gap: 10px;
  background: linear-gradient(135deg, #ffffff 0%, #f8fbfd 100%);
}

.empty-state-card h2 {
  margin: 0;
  color: var(--text-primary);
}

.asset-workspace {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.asset-overview-card,
.advanced-tools-panel {
  padding: 22px;
}

.asset-overview-card {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: start;
}

.asset-overview-card h2 {
  margin: 4px 0 8px;
  color: var(--text-primary);
}

.asset-status-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.asset-card {
  padding: 20px;
  border-radius: 22px;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  box-shadow: 0 16px 40px rgba(15, 23, 42, 0.05);
}

.asset-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 14px;
}

.asset-card h3 {
  margin: 0;
  color: var(--text-primary);
}

.asset-bar {
  height: 10px;
  border-radius: 999px;
  background: #e2e8f0;
  overflow: hidden;
}

.asset-bar-fill {
  height: 100%;
  border-radius: 999px;
  transition: width 0.35s ease;
}

.asset-bar-fill.mapped {
  background: linear-gradient(90deg, #0f766e 0%, #22c55e 100%);
}

.asset-numbers {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  margin-top: 10px;
  color: var(--text-muted);
  font-size: 0.84rem;
}

.asset-numbers .mapped {
  color: var(--accent-primary);
}

.asset-insight-strip {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.unmapped-section {
  padding: 22px;
  border-radius: 24px;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.05);
}

.unmapped-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  margin-top: 16px;
}

.unmapped-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid var(--border-subtle);
  background: linear-gradient(180deg, #fffdf8 0%, #ffffff 100%);
}

.unmapped-top {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  margin-bottom: 8px;
}

.unmapped-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}

.backfill-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
  margin-top: 16px;
}

.backfill-grid label,
.form-grid label {
  display: grid;
  gap: 8px;
  color: var(--text-secondary);
  font-size: 0.88rem;
}

.advanced-actions {
  margin-top: 16px;
}

.backfill-result {
  margin-top: 16px;
  padding: 16px;
  border-radius: 18px;
  background: var(--status-success-bg);
  border: 1px solid var(--status-success-border);
}

.backfill-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 8px;
  color: var(--text-secondary);
  font-size: 0.84rem;
}

.modern-modal {
  max-width: 560px;
  border-radius: 24px;
}

.soft-chip.warning {
  background: var(--status-warning-bg);
  color: var(--accent-warning);
}

.checkbox-label {
  display: flex !important;
  flex-direction: row !important;
  align-items: center;
  gap: 10px;
}

.checkbox-label input[type="checkbox"] {
  width: 18px;
  height: 18px;
}

@media (max-width: 1180px) {
  .metric-strip,
  .asset-status-grid,
  .asset-insight-strip,
  .detail-stat-grid,
  .unmapped-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .detail-content-grid,
  .hero-panel,
  .workspace-shell {
    grid-template-columns: 1fr;
  }

  .navigator-panel,
  .detail-panel-shell {
    min-height: auto;
  }
}

@media (max-width: 760px) {
  .site-hierarchy-page {
    gap: 14px;
  }

  .hero-panel,
  .asset-overview-card,
  .detail-hero {
    padding: 18px;
  }

  .metric-strip,
  .asset-status-grid,
  .asset-insight-strip,
  .detail-stat-grid,
  .detail-grid,
  .backfill-grid,
  .unmapped-grid {
    grid-template-columns: 1fr;
  }

  .detail-hero,
  .asset-overview-card {
    flex-direction: column;
  }

  .workspace-shell {
    gap: 14px;
  }

  .navigator-panel,
  .detail-panel,
  .empty-state-card,
  .advanced-tools-panel {
    padding: 16px;
  }
}
</style>
