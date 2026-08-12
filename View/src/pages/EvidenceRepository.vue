<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Bằng chứng</span>
                <h1 class="page-title">Kho lưu trữ bằng chứng</h1>
            </div>
            <div class="header-actions">
                <template v-if="activeRepoTab === 'items'">
                    <button class="btn btn-secondary" @click="showCreateItem = true">+ Bằng chứng</button>
                    <button class="btn btn-secondary" @click="showCollections = true">Bộ sưu tập</button>
                </template>
                <template v-else>
                    <button class="btn btn-secondary" @click="showPolicyComposer = !showPolicyComposer">
                        {{ showPolicyComposer ? 'Đóng biểu mẫu chính sách' : '+ Chính sách lưu giữ' }}
                    </button>
                    <button class="btn btn-secondary" @click="runRetentionDryRun">Chạy thử</button>
                </template>
                <button class="btn btn-primary" @click="refreshCurrentView">Làm mới</button>
            </div>
        </div>
        <div class="repo-tabs">
            <button :class="{ active: activeRepoTab === 'items' }" @click="activeRepoTab = 'items'">Mục bằng chứng</button>
            <button :class="{ active: activeRepoTab === 'governance' }" @click="activeRepoTab = 'governance'">Lưu giữ & Khóa pháp lý</button>
        </div>
        <section v-if="activeRepoTab === 'items'" class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Mục</span><h2 class="panel-title">Mục bằng chứng</h2></div>
                    <div class="filter-row">
                        <select v-model="filters.evidenceType" class="form-select" @change="loadItems">
                            <option value="">Tất cả loại</option>
                            <option value="Document">Tài liệu</option>
                            <option value="Image">Ảnh</option>
                            <option value="Video">Video</option>
                            <option value="Log">Nhật ký</option>
                            <option value="Report">Báo cáo</option>
                        </select>
                        <select v-model="filters.privacyLabel" class="form-select" @change="loadItems">
                            <option value="">Tất cả quyền riêng tư</option>
                            <option value="Internal">Nội bộ</option>
                            <option value="Biometric">Sinh trắc học</option>
                            <option value="PersonalIdentity">Danh tính cá nhân</option>
                            <option value="VehicleIdentity">Danh tính phương tiện</option>
                            <option value="VisitorDocument">Giấy tờ khách</option>
                            <option value="SensitiveSite">Khu vực nhạy cảm</option>
                            <option value="Public">Công khai</option>
                        </select>
                        <select v-model="filters.isLegalHold" class="form-select" @change="loadItems">
                            <option value="">Tất cả khóa</option>
                            <option value="true">Có Khóa pháp lý</option>
                            <option value="false">Không khóa</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="items.length === 0" class="empty-card">Chưa có mục bằng chứng.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Loại</th><th>Nguồn</th><th>Quyền riêng tư</th><th>Lưu giữ</th><th>Hash</th><th>Khóa pháp lý</th><th>Ngày tạo</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="item in items" :key="item.evidenceItemId">
                                <td>{{ item.evidenceItemId }}</td>
                                <td><span class="badge badge-info">{{ item.evidenceType }}</span></td>
                                <td class="table-sub">{{ item.sourceType }}:{{ (item.sourceReference || '').substring(0, 20) }}</td>
                                <td><span class="badge" :class="privacyClass(item.privacyLabel)">{{ item.privacyLabel }}</span></td>
                                <td>{{ item.retentionCategory }}</td>
                                <td class="table-sub">{{ (item.hashSha256 || '').substring(0, 12) }}...</td>
                                <td><span v-if="item.isLegalHold" class="badge badge-danger">Khóa</span><span v-else class="table-sub">—</span></td>
                                <td class="table-sub">{{ new Date(item.createdAtUtc).toLocaleDateString() }}</td>
                                <td><button class="btn btn-secondary btn-sm" @click="viewDetail(item)">Chi tiết</button></td>
                            </tr>
                        </tbody>
                    </table>
                    <div class="pagination-bar">
                        <span>Trang {{ page }}/{{ totalPages }}</span>
                        <div class="page-buttons">
                            <button class="page-btn" :disabled="page <= 1" @click="page--; loadItems()">‹</button>
                            <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadItems()">›</button>
                        </div>
                    </div>
                </div>
            </article>
        </section>
        <section v-else class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Quản trị</span>
                        <h2 class="panel-title">Chính sách lưu giữ</h2>
                    </div>
                    <div class="panel-actions">
                        <span class="soft-chip muted">{{ retentionPolicies.length }} chính sách</span>
                    </div>
                </div>
                <div v-if="showPolicyComposer" class="policy-composer">
                    <div class="form-row two">
                        <div class="form-group">
                            <label>Tên chính sách</label>
                            <input v-model="policyForm.name" class="form-control" placeholder="vd. Lưu giữ video sự cố" />
                        </div>
                        <div class="form-group">
                            <label>Loại bằng chứng</label>
                            <select v-model="policyForm.evidenceType" class="form-control">
                                <option value="Any">Bất kỳ</option>
                                <option value="Document">Tài liệu</option>
                                <option value="Image">Ảnh</option>
                                <option value="Video">Video</option>
                                <option value="Log">Nhật ký</option>
                                <option value="Report">Báo cáo</option>
                            </select>
                        </div>
                    </div>
                    <div class="form-row three">
                        <div class="form-group">
                            <label>Danh mục lưu giữ</label>
                            <input v-model="policyForm.retentionCategory" class="form-control" placeholder="Mặc định, Sự cố, Quyền riêng tư" />
                        </div>
                        <div class="form-group">
                            <label>Số ngày lưu giữ</label>
                            <input v-model.number="policyForm.retentionDays" type="number" min="1" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Chế độ xóa</label>
                            <select v-model="policyForm.purgeMode" class="form-control">
                                <option value="ReviewRequired">Cần rà soát</option>
                                <option value="Auto">Tự động</option>
                            </select>
                        </div>
                    </div>
                    <label class="checkbox-row">
                        <input v-model="policyForm.isActive" type="checkbox" />
                        <span>Kích hoạt ngay</span>
                    </label>
                    <div class="chip-row">
                        <button class="btn btn-sm btn-secondary" @click="resetPolicyForm">Đặt lại</button>
                        <button class="btn btn-sm btn-primary" :disabled="policySaving || !policyForm.name || !policyForm.retentionDays" @click="submitRetentionPolicy">
                            {{ policySaving ? 'Đang lưu...' : 'Tạo chính sách' }}
                        </button>
                    </div>
                </div>
                <div v-if="governanceMessage" class="alert" :class="governanceMessageType === 'success' ? 'alert-success' : 'alert-danger'" style="margin-bottom:12px;">
                    {{ governanceMessage }}
                </div>
                <div v-if="loadingPolicies" class="empty-card">Đang tải...</div>
                <div v-else-if="retentionPolicies.length === 0" class="empty-card">Chưa có chính sách lưu giữ.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Tên</th><th>Loại</th><th>Danh mục</th><th>Số ngày</th><th>Chế độ xóa</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="policy in retentionPolicies" :key="policy.retentionPolicyId">
                                <td>{{ policy.name }}</td>
                                <td>{{ policy.evidenceType }}</td>
                                <td>{{ policy.retentionCategory }}</td>
                                <td>{{ policy.retentionDays }}</td>
                                <td><span class="badge badge-info">{{ policy.purgeMode }}</span></td>
                                <td><span class="badge" :class="policy.isActive ? 'badge-success' : 'badge-secondary'">{{ policy.isActive ? 'Hoạt động' : 'Ngừng' }}</span></td>
                                <td>
                                    <button
                                        class="btn btn-sm"
                                        :class="policy.isActive ? 'btn-warning' : 'btn-success'"
                                        @click="toggleRetentionPolicy(policy, !policy.isActive)"
                                    >
                                        {{ policy.isActive ? 'Ngừng' : 'Kích hoạt' }}
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Bảo vệ</span>
                        <h2 class="panel-title">Khóa pháp lý đang hoạt động</h2>
                    </div>
                    <div class="panel-actions">
                        <span class="soft-chip success">{{ legalHolds.length }} đang khóa</span>
                    </div>
                </div>
                <div class="governance-note">
                    Khóa pháp lý vẫn có thể áp dụng ngay trong chi tiết từng bằng chứng. Khu vực này là nơi theo dõi và gỡ khóa đang hoạt động một cách tập trung.
                </div>
                <div v-if="loadingHolds" class="empty-card">Đang tải...</div>
                <div v-else-if="legalHolds.length === 0" class="empty-card">Không có khóa pháp lý đang hoạt động.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Phạm vi</th><th>Lý do</th><th>Ngày áp dụng</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="hold in legalHolds" :key="hold.legalHoldId">
                                <td>{{ hold.legalHoldId }}</td>
                                <td>{{ formatHoldScope(hold) }}</td>
                                <td class="table-sub">{{ (hold.reason || '').substring(0, 60) || '—' }}</td>
                                <td class="table-sub">{{ new Date(hold.appliedAtUtc).toLocaleString() }}</td>
                                <td><button class="btn btn-warning btn-sm" @click="releaseHoldFromGovernance(hold)">Gỡ khóa</button></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <div v-if="dryRunResult" class="modal-overlay" @click.self="dryRunResult = null">
            <div class="modal-box wide-modal">
                <h3>Chạy thử lưu giữ</h3>
                <pre class="dry-run-output">{{ JSON.stringify(dryRunResult, null, 2) }}</pre>
                <div class="modal-actions">
                    <button class="btn btn-danger" :disabled="purgeBusy" @click="confirmGovernancePurge">Xóa các mục đã liệt kê</button>
                    <button class="btn btn-secondary" @click="dryRunResult = null">Đóng</button>
                </div>
            </div>
        </div>

        <!-- Evidence Detail Drawer -->
        <Teleport to="body">
            <div v-if="detail" class="modal-overlay drawer-overlay" @click.self="closeDetail">
                <div class="modal-panel drawer-panel">
                    <div class="modal-header">
                        <h2>Evidence #{{ detail.evidenceItemId }}</h2>
                        <span class="badge badge-info">{{ detail.evidenceType }}</span>
                        <button class="btn-close" @click="closeDetail">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="drawer-tabs">
                            <button v-for="dt in detailTabs" :key="dt.key" :class="{ active: activeDetailTab === dt.key }" @click="activeDetailTab = dt.key; loadDetailTab(dt.key)">
                                {{ dt.label }}
                            </button>
                        </div>

                        <!-- Overview Tab -->
                        <div v-if="activeDetailTab === 'overview'" class="drawer-tab-content">
                            <div class="detail-grid">
                                <div class="detail-row"><span class="detail-label">Loại</span><span>{{ detail.evidenceType }}</span></div>
                                <div class="detail-row"><span class="detail-label">Nguồn</span><span>{{ detail.sourceType }}:{{ detail.sourceReference || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Lưu trữ</span><span class="table-sub">{{ detail.storageReference }}</span></div>
                                <div class="detail-row"><span class="detail-label">Hash (SHA256)</span><span class="table-sub" style="font-size:11px;word-break:break-all;">{{ detail.hashSha256 }}</span></div>
                                <div class="detail-row"><span class="detail-label">Quyền riêng tư</span><span class="badge" :class="privacyClass(detail.privacyLabel)">{{ detail.privacyLabel }}</span></div>
                                <div class="detail-row"><span class="detail-label">Lưu giữ</span><span>{{ detail.retentionCategory }}</span></div>
                                <div class="detail-row"><span class="detail-label">Khóa pháp lý</span><span>{{ detail.isLegalHold ? 'Có' : 'Không' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Ngày tạo</span><span>{{ new Date(detail.createdAtUtc).toLocaleString() }}</span></div>
                            </div>

                            <div class="chip-row" style="margin-top:12px;flex-wrap:wrap;gap:6px;">
                                <button class="btn btn-sm btn-secondary" @click="verifyHash">Xác minh Hash</button>
                                <button class="btn btn-sm btn-secondary" @click="showCreateExport = true">Yêu cầu xuất</button>
                                <button class="btn btn-sm btn-secondary" @click="showCreateRedaction = true">Yêu cầu che dữ liệu</button>
                                <button v-if="!detail.isLegalHold" class="btn btn-sm btn-danger" @click="applyLegalHold">Áp dụng Khóa pháp lý</button>
                                <button v-if="detail.isLegalHold" class="btn btn-sm btn-warning" @click="releaseLegalHold">Gỡ Khóa</button>
                            </div>
                            <div v-if="hashResult" class="alert" :class="hashResult.valid ? 'alert-success' : 'alert-danger'" style="margin-top:8px;">
                                {{ hashResult.message }}
                            </div>
                            <div v-if="actionSuccess" class="alert alert-success" style="margin-top:8px;">{{ actionSuccess }}</div>
                            <div v-else-if="actionError" class="alert alert-danger" style="margin-top:8px;">{{ actionError }}</div>
                        </div>

                        <!-- Custody Tab -->
                        <div v-if="activeDetailTab === 'custody'" class="drawer-tab-content">
                            <div class="chip-row" style="margin-bottom:8px;">
                                <button class="btn btn-sm btn-secondary" @click="showAddCustody = true">+ Thêm lệnh chuyển</button>
                            </div>
                            <div v-if="detailLoading" class="empty-card">Đang tải...</div>
                            <div v-else-if="custody.length === 0" class="empty-card">Chưa có lệnh chuyển.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>Hành động</th><th>Người thực hiện</th><th>Từ</th><th>Đến</th><th>Ghi chú</th><th>Thời gian</th></tr></thead>
                                    <tbody>
                                        <tr v-for="c in custody" :key="c.chainOfCustodyEntryId">
                                            <td><span class="badge badge-info">{{ c.action }}</span></td>
                                            <td>{{ c.actorUserId || '—' }}</td>
                                            <td class="table-sub">{{ c.fromCustodian || '—' }}</td>
                                            <td class="table-sub">{{ c.toCustodian || '—' }}</td>
                                            <td class="table-sub">{{ c.note || '—' }}</td>
                                            <td class="table-sub">{{ new Date(c.createdAtUtc).toLocaleString() }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>

                            <div v-if="showAddCustody" style="margin-top:12px;">
                                <div class="detail-section-title">Thêm lệnh chuyển</div>
                                <div class="form-row two">
                                    <div class="form-group">
                                        <label>Hành động</label>
                                        <input v-model="custodyForm.action" class="form-control" placeholder="vd. Bàn giao, Rà soát" />
                                    </div>
                                    <div class="form-group">
                                        <label>Người nhận</label>
                                        <input v-model="custodyForm.toCustodian" class="form-control" placeholder="Người/Bộ phận" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label>Ghi chú</label>
                                    <textarea v-model="custodyForm.note" class="form-control" rows="2"></textarea>
                                </div>
                                <div class="chip-row">
                                    <button class="btn btn-sm btn-secondary" @click="showAddCustody = false">Hủy</button>
                                    <button class="btn btn-sm btn-primary" :disabled="custodySaving" @click="submitCustodyEntry">{{ custodySaving ? 'Đang lưu...' : 'Lưu' }}</button>
                                </div>
                            </div>
                        </div>

                        <!-- Access Logs Tab -->
                        <div v-if="activeDetailTab === 'access'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Đang tải...</div>
                            <div v-else-if="accessLogs.length === 0" class="empty-card">Không có nhật ký truy cập.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>Người dùng</th><th>Hành động</th><th>Thời gian</th></tr></thead>
                                    <tbody>
                                        <tr v-for="a in accessLogs" :key="a.evidenceAccessLogId || a.id">
                                            <td>{{ a.userId || a.actorUserId || '—' }}</td>
                                            <td>{{ a.action || a.accessType }}</td>
                                            <td class="table-sub">{{ new Date(a.createdAtUtc || a.accessedAtUtc).toLocaleString() }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Collections Tab -->
                        <div v-if="activeDetailTab === 'collections'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Đang tải...</div>
                            <div v-else-if="evidenceCollections.length === 0" class="empty-card">Chưa thuộc bộ sưu tập nào.</div>
                            <div v-else class="collection-list">
                                <div v-for="col in evidenceCollections" :key="col.evidenceCollectionId" class="collection-card" @click="showCollectionDetail(col)">
                                    <strong>{{ col.name }}</strong>
                                    <div class="text-muted">{{ col.status }} · {{ col.itemCount || 0 }} mục</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Export Request Modal -->
            <div v-if="showCreateExport" class="modal-overlay" @click.self="showCreateExport = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Yêu cầu xuất — #{{ detail?.evidenceItemId }}</h2>
                        <button class="btn-close" @click="showCreateExport = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Người nhận *</label>
                            <input v-model="exportForm.recipient" class="form-control" placeholder="Email hoặc tên" />
                        </div>
                        <div class="form-group">
                            <label>Mục đích *</label>
                            <textarea v-model="exportForm.purpose" class="form-control" rows="2" placeholder="Vì sao cần xuất dữ liệu"></textarea>
                        </div>
                        <div v-if="exportResult" class="alert alert-success">{{ exportResult }}</div>
                        <div v-else-if="exportError" class="alert alert-danger">{{ exportError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateExport = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="exportSaving || !exportForm.recipient || !exportForm.purpose" @click="submitExportRequest">
                            {{ exportSaving ? 'Đang gửi...' : 'Gửi yêu cầu' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Redaction Request Modal -->
            <div v-if="showCreateRedaction" class="modal-overlay" @click.self="showCreateRedaction = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Yêu cầu che dữ liệu — #{{ detail?.evidenceItemId }}</h2>
                        <button class="btn-close" @click="showCreateRedaction = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Nhãn quyền riêng tư</label>
                            <select v-model="redactionForm.privacyLabel" class="form-control">
                                <option value="PersonalIdentity">Danh tính cá nhân</option>
                                <option value="Biometric">Sinh trắc học</option>
                                <option value="VehicleIdentity">Danh tính phương tiện</option>
                                <option value="SensitiveSite">Khu vực nhạy cảm</option>
                                <option value="VisitorDocument">Giấy tờ khách</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Lý do *</label>
                            <textarea v-model="redactionForm.reason" class="form-control" rows="2" placeholder="Vì sao cần che dữ liệu"></textarea>
                        </div>
                        <div v-if="redactResult" class="alert alert-success">{{ redactResult }}</div>
                        <div v-else-if="redactError" class="alert alert-danger">{{ redactError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateRedaction = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="redactSaving || !redactionForm.reason" @click="submitRedactionRequest">
                            {{ redactSaving ? 'Đang gửi...' : 'Gửi yêu cầu' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Create Evidence Item Modal -->
            <div v-if="showCreateItem" class="modal-overlay" @click.self="showCreateItem = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Tạo mục bằng chứng</h2>
                        <button class="btn-close" @click="showCreateItem = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Loại bằng chứng *</label>
                            <select v-model="createForm.evidenceType" class="form-control">
                                <option value="Document">Tài liệu</option>
                                <option value="Image">Ảnh</option>
                                <option value="Video">Video</option>
                                <option value="Log">Nhật ký</option>
                                <option value="Report">Báo cáo</option>
                            </select>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Loại nguồn</label>
                                <input v-model="createForm.sourceType" class="form-control" placeholder="vd. Camera, AccessLog" />
                            </div>
                            <div class="form-group">
                                <label>Tham chiếu nguồn</label>
                                <input v-model="createForm.sourceReference" class="form-control" placeholder="Tham chiếu tùy chọn" />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Tham chiếu lưu trữ</label>
                            <input v-model="createForm.storageReference" class="form-control" placeholder="S3://, /path/to/file" />
                        </div>
                        <div class="form-group">
                            <label>Nhãn quyền riêng tư</label>
                            <select v-model="createForm.privacyLabel" class="form-control">
                                <option value="Internal">Nội bộ</option>
                                <option value="PersonalIdentity">Danh tính cá nhân</option>
                                <option value="Biometric">Sinh trắc học</option>
                                <option value="VehicleIdentity">Danh tính phương tiện</option>
                                <option value="VisitorDocument">Giấy tờ khách</option>
                                <option value="SensitiveSite">Khu vực nhạy cảm</option>
                            </select>
                        </div>
                        <div v-if="createResult" class="alert alert-success">{{ createResult }}</div>
                        <div v-else-if="createError" class="alert alert-danger">{{ createError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateItem = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="createBusy || !createForm.evidenceType" @click="submitCreateItem">
                            {{ createBusy ? 'Đang tạo...' : 'Tạo' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Collections Modal -->
            <div v-if="showCollections" class="modal-overlay" @click.self="showCollections = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Bộ sưu tập bằng chứng</h2>
                        <button class="btn-close" @click="showCollections = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="chip-row" style="margin-bottom:12px;">
                            <button class="btn btn-sm btn-primary" @click="showNewCollection = true">+ Bộ sưu tập mới</button>
                            <button class="btn btn-sm btn-secondary" @click="loadCollections">Làm mới</button>
                        </div>

                        <div v-if="showNewCollection" style="margin-bottom:12px;padding:12px;border:1px solid #e2e8f0;border-radius:8px;">
                            <div class="form-group">
                                <label>Tên bộ sưu tập *</label>
                                <input v-model="collectionForm.name" class="form-control" placeholder="vd. Vụ việc #2024-001" />
                            </div>
                            <div class="form-group">
                                <label>Mô tả</label>
                                <textarea v-model="collectionForm.description" class="form-control" rows="2"></textarea>
                            </div>
                            <div v-if="colResult" class="alert alert-success">{{ colResult }}</div>
                            <div class="chip-row">
                                <button class="btn btn-sm btn-secondary" @click="showNewCollection = false">Hủy</button>
                                <button class="btn btn-sm btn-primary" :disabled="colBusy || !collectionForm.name" @click="createCollection">
                                    {{ colBusy ? 'Đang tạo...' : 'Tạo' }}
                                </button>
                            </div>
                        </div>

                        <div v-if="colLoading" class="empty-card">Đang tải...</div>
                        <div v-else-if="collections.length === 0" class="empty-card">Chưa có bộ sưu tập.</div>
                        <div v-else class="collection-list">
                            <div v-for="col in collections" :key="col.evidenceCollectionId" class="collection-card" @click="showCollectionDetail(col)">
                                <div class="collection-card-header">
                                    <strong>{{ col.name }}</strong>
                                    <span class="soft-chip" :class="col.status === 'Open' ? 'success' : 'muted'">{{ col.status === 'Open' ? 'Đang mở' : col.status }}</span>
                                </div>
                                <div class="text-muted">{{ col.itemCount || 0 }} mục · {{ col.description || '' }}</div>
                            </div>
                        </div>

                        <!-- Collection Detail -->
                        <div v-if="selectedCollection" style="margin-top:16px;">
                            <div class="detail-section-title">{{ selectedCollection.name }}</div>
                            <div class="detail-grid" style="margin-bottom:12px;">
                                <div class="detail-row"><span class="detail-label">Trạng thái</span><span class="soft-chip" :class="selectedCollection.status === 'Open' ? 'success' : 'muted'">{{ selectedCollection.status === 'Open' ? 'Đang mở' : selectedCollection.status }}</span></div>
                                <div class="detail-row"><span class="detail-label">Số mục</span><span>{{ selectedCollection.itemCount || collectionItems.length }}</span></div>
                            </div>
                            <div v-if="colDetailLoading" class="empty-card">Đang tải...</div>
                            <div v-else-if="collectionItems.length === 0" class="text-muted">Bộ sưu tập chưa có mục nào.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>ID bằng chứng</th><th>Loại</th><th>Ngày thêm</th></tr></thead>
                                    <tbody>
                                        <tr v-for="ci in collectionItems" :key="ci.evidenceItemId || ci.id">
                                            <td>{{ ci.evidenceItemId }}</td>
                                            <td>{{ ci.evidenceType || '—' }}</td>
                                            <td class="table-sub">{{ ci.addedAtUtc ? new Date(ci.addedAtUtc).toLocaleString() : '—' }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div v-if="selectedCollection.status === 'Open'" class="chip-row" style="margin-top:8px;">
                                <button class="btn btn-sm btn-secondary" @click="showAddToCollection = !showAddToCollection">+ Thêm mục</button>
                                <button class="btn btn-sm btn-warning" @click="closeCollection">Đóng bộ sưu tập</button>
                            </div>
                            <div v-if="showAddToCollection" style="margin-top:8px;">
                                <div class="chip-row">
                                    <input v-model.number="addToCollectionForm.evidenceItemId" type="number" class="form-control" placeholder="ID bằng chứng" style="width:150px;" />
                                    <button class="btn btn-sm btn-primary" :disabled="!addToCollectionForm.evidenceItemId" @click="addItemToCollection">Thêm</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const items = ref([])
const detail = ref(null)
const custody = ref([])
const loading = ref(true)
const page = ref(1)
const totalPages = ref(1)
const filters = reactive({ evidenceType: '', privacyLabel: '', isLegalHold: '' })
const activeRepoTab = ref('items')

// Detail drawer tabs
const activeDetailTab = ref('overview')
const detailLoading = ref(false)
const custodySaving = ref(false)
const showAddCustody = ref(false)
const custodyForm = ref({ action: 'Transferred', fromCustodian: '', toCustodian: '', note: '' })
const accessLogs = ref([])
const evidenceCollections = ref([])
const hashResult = ref(null)
const actionSuccess = ref('')
const actionError = ref('')

// Export
const showCreateExport = ref(false)
const exportSaving = ref(false)
const exportResult = ref('')
const exportError = ref('')
const exportForm = ref({ recipient: '', purpose: '' })

// Redaction
const showCreateRedaction = ref(false)
const redactSaving = ref(false)
const redactResult = ref('')
const redactError = ref('')
const redactionForm = ref({ privacyLabel: 'PersonalIdentity', reason: '' })

// Create
const showCreateItem = ref(false)
const createBusy = ref(false)
const createResult = ref('')
const createError = ref('')
const createForm = ref({
    evidenceType: 'Document', sourceType: '', sourceReference: '',
    storageReference: '', privacyLabel: 'Internal',
})

// Collections
const showCollections = ref(false)
const colLoading = ref(false)
const colBusy = ref(false)
const colResult = ref('')
const collections = ref([])
const selectedCollection = ref(null)
const colDetailLoading = ref(false)
const collectionItems = ref([])
const showNewCollection = ref(false)
const showAddToCollection = ref(false)
const collectionForm = ref({ name: '', description: '' })
const addToCollectionForm = ref({ evidenceItemId: null })

// Governance
const retentionPolicies = ref([])
const legalHolds = ref([])
const loadingPolicies = ref(false)
const loadingHolds = ref(false)
const governanceMessage = ref('')
const governanceMessageType = ref('success')
const showPolicyComposer = ref(false)
const policySaving = ref(false)
const dryRunResult = ref(null)
const purgeBusy = ref(false)
const policyForm = ref({
    name: '',
    evidenceType: 'Any',
    retentionCategory: 'Default',
    retentionDays: 90,
    purgeMode: 'ReviewRequired',
    isActive: true,
})

const detailTabs = [
    { key: 'overview', label: 'Tổng quan' },
    { key: 'custody', label: 'Chuỗi bàn giao' },
    { key: 'access', label: 'Nhật ký truy cập' },
    { key: 'collections', label: 'Bộ sưu tập' },
]

async function loadItems() {
    loading.value = true
    try {
        const params = { page: page.value, pageSize: 50 }
        if (filters.evidenceType) params.evidenceType = filters.evidenceType
        if (filters.privacyLabel) params.privacyLabel = filters.privacyLabel
        if (filters.isLegalHold) params.isLegalHold = filters.isLegalHold === 'true'
        const res = await enterpriseApi.getEvidenceItems(params)
        items.value = res.data.items || []
        totalPages.value = Math.ceil((res.data.total || 0) / 50) || 1
    } catch { items.value = [] }
    finally { loading.value = false }
}

async function refreshCurrentView() {
    if (activeRepoTab.value === 'governance') {
        await loadGovernance()
        return
    }
    await loadItems()
}

async function loadGovernance() {
    await Promise.all([loadRetentionPolicies(), loadLegalHolds()])
}

async function loadRetentionPolicies() {
    loadingPolicies.value = true
    try {
        const res = await enterpriseApi.getRetentionPolicies()
        retentionPolicies.value = Array.isArray(res.data) ? res.data : []
    } catch {
        retentionPolicies.value = []
    } finally {
        loadingPolicies.value = false
    }
}

async function loadLegalHolds() {
    loadingHolds.value = true
    try {
        const res = await enterpriseApi.getLegalHolds({ status: 'Active' })
        legalHolds.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch {
        legalHolds.value = []
    } finally {
        loadingHolds.value = false
    }
}

function resetPolicyForm() {
    policyForm.value = {
        name: '',
        evidenceType: 'Any',
        retentionCategory: 'Default',
        retentionDays: 90,
        purgeMode: 'ReviewRequired',
        isActive: true,
    }
}

async function submitRetentionPolicy() {
    if (!policyForm.value.name || !policyForm.value.retentionDays) return
    policySaving.value = true
    governanceMessage.value = ''
    governanceMessageType.value = 'success'
    try {
        await enterpriseApi.createRetentionPolicy({
            name: policyForm.value.name,
            evidenceType: policyForm.value.evidenceType,
            retentionCategory: policyForm.value.retentionCategory,
            retentionDays: policyForm.value.retentionDays,
            purgeMode: policyForm.value.purgeMode,
            isActive: policyForm.value.isActive,
        })
        governanceMessage.value = 'Đã tạo retention policy.'
        showPolicyComposer.value = false
        resetPolicyForm()
        await loadRetentionPolicies()
    } catch (e) {
        governanceMessageType.value = 'error'
        governanceMessage.value = e.response?.data?.message || 'Không thể tạo retention policy.'
    } finally {
        policySaving.value = false
    }
}

async function toggleRetentionPolicy(policy, isActive) {
    governanceMessage.value = ''
    governanceMessageType.value = 'success'
    try {
        await enterpriseApi.updateRetentionPolicy(policy.retentionPolicyId, { isActive })
        governanceMessage.value = isActive ? 'Đã kích hoạt retention policy.' : 'Đã tắt retention policy.'
        await loadRetentionPolicies()
    } catch (e) {
        governanceMessageType.value = 'error'
        governanceMessage.value = e.response?.data?.message || 'Không thể cập nhật retention policy.'
    }
}

async function runRetentionDryRun() {
    governanceMessage.value = ''
    governanceMessageType.value = 'success'
    try {
        const res = await enterpriseApi.dryRunRetention({ asOfUtc: new Date().toISOString(), limit: 100 })
        dryRunResult.value = res.data
    } catch {
        alert('Chạy thử thất bại')
    }
}

async function confirmGovernancePurge() {
    if (!confirm('Thao tác này sẽ xóa các mục bằng chứng và yêu cầu xác thực tăng cường MFA. Tiếp tục?')) return
    purgeBusy.value = true
    try {
        const ids = dryRunResult.value?.candidates?.map(candidate => candidate.evidenceItemId) || []
        if (ids.length === 0) {
            governanceMessageType.value = 'error'
            governanceMessage.value = 'Dry run hiện chưa có evidence nào đủ điều kiện purge.'
            dryRunResult.value = null
            return
        }
        await enterpriseApi.purgeEvidence({ evidenceItemIds: ids, reason: 'Retention policy purge' })
        dryRunResult.value = null
        governanceMessageType.value = 'success'
        governanceMessage.value = 'Đã purge các evidence đủ điều kiện theo retention policy.'
        await Promise.all([loadItems(), loadGovernance()])
    } catch {
        alert('Xóa dữ liệu thất bại')
    } finally {
        purgeBusy.value = false
    }
}

function formatHoldScope(hold) {
    if (hold.evidenceItemId) return `Evidence #${hold.evidenceItemId}`
    if (hold.evidenceCollectionId) return `Collection #${hold.evidenceCollectionId}`
    return 'Unknown'
}

async function releaseHoldFromGovernance(hold) {
    const reason = prompt(`Lý do gỡ khóa pháp lý #${hold.legalHoldId}:`)
    if (!reason) return
    governanceMessage.value = ''
    governanceMessageType.value = 'success'
    try {
        await enterpriseApi.releaseLegalHold(hold.legalHoldId, { reason })
        governanceMessage.value = `Đã gỡ legal hold #${hold.legalHoldId}.`
        if (detail.value?.evidenceItemId && hold.evidenceItemId === detail.value.evidenceItemId) {
            detail.value.isLegalHold = false
        }
        await Promise.all([loadItems(), loadLegalHolds()])
    } catch (e) {
        governanceMessageType.value = 'error'
        governanceMessage.value = e.response?.data?.message || 'Không thể gỡ legal hold.'
    }
}

async function viewDetail(item) {
    detail.value = item
    activeDetailTab.value = 'overview'
    custody.value = []
    accessLogs.value = []
    evidenceCollections.value = []
    hashResult.value = null
    actionSuccess.value = ''
    actionError.value = ''
    try {
        const res = await enterpriseApi.getChainOfCustody(item.evidenceItemId)
        custody.value = Array.isArray(res.data) ? res.data : []
    } catch { custody.value = [] }
}

function closeDetail() {
    detail.value = null
}

async function loadDetailTab(tab) {
    if (!detail.value) return
    const itemId = detail.value.evidenceItemId
    detailLoading.value = true
    try {
        if (tab === 'custody') {
            const res = await enterpriseApi.getChainOfCustody(itemId)
            custody.value = Array.isArray(res.data) ? res.data : []
        } else if (tab === 'access') {
            const res = await enterpriseApi.getEvidenceAccessLogs(itemId)
            accessLogs.value = Array.isArray(res.data) ? res.data : []
        } else if (tab === 'collections') {
            const res = await enterpriseApi.getEvidenceCollections({ evidenceItemId: itemId })
            evidenceCollections.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
        }
    } catch (e) {
        console.error(`Failed to load ${tab}`, e)
    } finally {
        detailLoading.value = false
    }
}

async function verifyHash() {
    if (!detail.value) return
    try {
        const res = await enterpriseApi.verifyEvidenceHash(detail.value.evidenceItemId, {})
        hashResult.value = { valid: true, message: res.data?.message || 'Xác minh Hash thành công!' }
    } catch (e) {
        hashResult.value = { valid: false, message: 'Xác minh Hash thất bại' }
    }
}

async function submitExportRequest() {
    if (!detail.value || !exportForm.value.recipient || !exportForm.value.purpose) return
    exportSaving.value = true
    exportResult.value = ''
    exportError.value = ''
    try {
        await enterpriseApi.createExportRequest({
            evidenceItemId: detail.value.evidenceItemId,
            recipient: exportForm.value.recipient,
            purpose: exportForm.value.purpose,
        })
        exportResult.value = 'Yêu cầu xuất đã gửi để phê duyệt!'
        exportForm.value = { recipient: '', purpose: '' }
    } catch (e) {
        exportError.value = e.response?.data?.message || e.message
    } finally {
        exportSaving.value = false
    }
}

async function submitRedactionRequest() {
    if (!detail.value || !redactionForm.value.reason) return
    redactSaving.value = true
    redactResult.value = ''
    redactError.value = ''
    try {
        await enterpriseApi.createRedactionRequest({
            evidenceItemId: detail.value.evidenceItemId,
            privacyLabel: redactionForm.value.privacyLabel,
            reason: redactionForm.value.reason,
        })
        redactResult.value = 'Yêu cầu che dữ liệu đã gửi để phê duyệt!'
        redactionForm.value = { privacyLabel: 'PersonalIdentity', reason: '' }
    } catch (e) {
        redactError.value = e.response?.data?.message || e.message
    } finally {
        redactSaving.value = false
    }
}

async function applyLegalHold() {
    if (!detail.value) return
    actionSuccess.value = ''
    actionError.value = ''
    const reason = prompt('Lý do áp dụng khóa pháp lý:')
    if (!reason) return
    try {
        await enterpriseApi.createLegalHold({
            evidenceItemId: detail.value.evidenceItemId,
            reason,
        })
        detail.value.isLegalHold = true
        actionSuccess.value = 'Đã áp dụng khóa pháp lý!'
    } catch (e) {
        actionError.value = e.response?.data?.message || e.message
    }
}

async function releaseLegalHold() {
    if (!detail.value) return
    actionSuccess.value = ''
    actionError.value = ''
    const reason = prompt('Lý do gỡ khóa:')
    if (!reason) return
    try {
        const holds = await enterpriseApi.getLegalHolds({ evidenceItemId: detail.value.evidenceItemId })
        const holdList = Array.isArray(holds.data) ? holds.data : (holds.data?.items || [])
        const activeHold = holdList.find(h => h.status === 'Active')
        if (activeHold) {
            await enterpriseApi.releaseLegalHold(activeHold.legalHoldId, { reason })
            detail.value.isLegalHold = false
            actionSuccess.value = 'Đã gỡ khóa pháp lý!'
        }
    } catch (e) {
        actionError.value = e.response?.data?.message || e.message
    }
}

async function submitCustodyEntry() {
    if (!detail.value) return
    custodySaving.value = true
    try {
        await enterpriseApi.addCustodyEntry(detail.value.evidenceItemId, {
            action: custodyForm.value.action,
            fromCustodian: custodyForm.value.fromCustodian || null,
            toCustodian: custodyForm.value.toCustodian || null,
            note: custodyForm.value.note || null,
        })
        custodyForm.value = { action: 'Transferred', toCustodian: '', note: '' }
        showAddCustody.value = false
        const res = await enterpriseApi.getChainOfCustody(detail.value.evidenceItemId)
        custody.value = Array.isArray(res.data) ? res.data : []
    } catch { alert('Không thể thêm lệnh chuyển') }
    finally { custodySaving.value = false }
}

// --- Create Evidence ---
async function submitCreateItem() {
    if (!createForm.value.evidenceType) return
    createBusy.value = true
    createResult.value = ''
    createError.value = ''
    try {
        await enterpriseApi.createEvidenceItem({
            evidenceType: createForm.value.evidenceType,
            sourceType: createForm.value.sourceType || null,
            sourceReference: createForm.value.sourceReference || null,
            storageReference: createForm.value.storageReference || null,
            privacyLabel: createForm.value.privacyLabel,
        })
        createResult.value = 'Đã tạo mục bằng chứng!'
        createForm.value = { evidenceType: 'Document', sourceType: '', sourceReference: '', storageReference: '', privacyLabel: 'Internal' }
        showCreateItem.value = false
        await loadItems()
    } catch (e) {
        createError.value = e.response?.data?.message || e.message
    } finally {
        createBusy.value = false
    }
}

// --- Collections ---
async function loadCollections() {
    colLoading.value = true
    try {
        const res = await enterpriseApi.getEvidenceCollections({ pageSize: 50 })
        collections.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { collections.value = [] }
    finally { colLoading.value = false }
}

async function showCollectionDetail(col) {
    selectedCollection.value = col
    colDetailLoading.value = true
    collectionItems.value = []
    try {
        const res = await enterpriseApi.getEvidenceCollectionDetail(col.evidenceCollectionId)
        collectionItems.value = res.data?.items || []
    } catch { collectionItems.value = [] }
    finally { colDetailLoading.value = false }
}

async function createCollection() {
    if (!collectionForm.value.name) return
    colBusy.value = true
    colResult.value = ''
    try {
        await enterpriseApi.createEvidenceCollection({
            name: collectionForm.value.name,
            description: collectionForm.value.description || null,
        })
        collectionForm.value = { name: '', description: '' }
        showNewCollection.value = false
        colResult.value = 'Đã tạo bộ sưu tập!'
        await loadCollections()
    } catch (e) {
        colResult.value = 'Thất bại: ' + (e.response?.data?.message || e.message)
    } finally {
        colBusy.value = false
    }
}

async function addItemToCollection() {
    if (!selectedCollection.value || !addToCollectionForm.value.evidenceItemId) return
    try {
        await enterpriseApi.addEvidenceCollectionItem(selectedCollection.value.evidenceCollectionId, {
            evidenceItemId: addToCollectionForm.value.evidenceItemId,
        })
        addToCollectionForm.value = { evidenceItemId: null }
        showAddToCollection.value = false
        await showCollectionDetail(selectedCollection.value)
    } catch { alert('Không thể thêm mục') }
}

async function closeCollection() {
    if (!selectedCollection.value) return
    if (!confirm(`Đóng bộ sưu tập "${selectedCollection.value.name}"?`)) return
    try {
        await enterpriseApi.closeEvidenceCollection(selectedCollection.value.evidenceCollectionId, {})
        selectedCollection.value.status = 'Closed'
        await loadCollections()
    } catch { alert('Không thể đóng bộ sưu tập') }
}

function privacyClass(l) {
    if (l === 'Biometric' || l === 'PersonalIdentity') return 'badge-danger'
    if (l === 'SensitiveSite') return 'badge-warn'
    if (l === 'VisitorDocument') return 'badge-primary'
    return 'badge-info'
}

onMounted(async () => {
    await Promise.all([loadItems(), loadGovernance()])
})
</script>

<style scoped>
.repo-tabs { display: flex; gap: 8px; margin-bottom: 16px; }
.repo-tabs button { border: 1px solid var(--border-default); background: var(--surface-default); color: var(--text-secondary); border-radius: 999px; padding: 8px 14px; font-size: 13px; font-weight: 600; cursor: pointer; transition: all 0.15s ease; }
.repo-tabs button.active { background: var(--accent-primary); border-color: var(--accent-primary); color: #fff; }
.policy-composer { margin-bottom: 16px; padding: 16px; border: 1px solid var(--border-subtle); border-radius: 14px; background: linear-gradient(180deg, var(--surface-subtle) 0%, var(--surface-hover) 100%); }
.form-row.three { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.checkbox-row { display: inline-flex; align-items: center; gap: 8px; margin-bottom: 12px; color: var(--text-secondary); font-size: 13px; }
.governance-note { margin-bottom: 12px; padding: 12px 14px; border-radius: 12px; background: var(--surface-subtle); border: 1px solid var(--border-subtle); color: var(--text-secondary); font-size: 13px; line-height: 1.5; }
.dry-run-output { max-height: 320px; overflow: auto; background: var(--ink-950); color: var(--text-inverse); border-radius: 12px; padding: 14px; font-size: 12px; }
.drawer-overlay { display: flex; justify-content: flex-end; }
.drawer-panel { width: 540px; max-width: 95vw; height: 100vh; margin: 0; border-radius: 0; overflow-y: auto; background: var(--bg-card-strong); }
.drawer-tabs { display: flex; gap: 4px; margin-bottom: 16px; border-bottom: 1px solid var(--border-subtle); padding-bottom: 8px; }
.drawer-tabs button { padding: 6px 14px; border: none; background: transparent; color: var(--text-secondary); font-size: 13px; border-radius: 8px; cursor: pointer; transition: all 0.15s; }
.drawer-tabs button:hover { background: var(--surface-subtle); }
.drawer-tabs button.active { background: var(--status-info-bg); color: var(--status-info-text); font-weight: 600; }
.drawer-tab-content { min-height: 100px; }
.detail-section-title { font-size: 13px; font-weight: 600; color: var(--text-primary); margin-bottom: 8px; padding-bottom: 4px; border-bottom: 1px solid var(--border-subtle); }
.collection-card { padding: 10px 12px; border: 1px solid var(--border-subtle); border-radius: 8px; margin-bottom: 6px; cursor: pointer; transition: background 0.15s; }
.collection-card:hover { background: var(--surface-subtle); }
.collection-card-header { display: flex; justify-content: space-between; align-items: center; }
</style>
