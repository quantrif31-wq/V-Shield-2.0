<template>
    <div class="page-container enterprise-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">An ninh toàn doanh nghiệp</span>
                <h1 class="page-title">Trung tâm điều hành an ninh doanh nghiệp</h1>
            </div>
            <div class="header-actions">
                <button type="button" class="btn btn-secondary" :disabled="loading" @click="loadOverview">
                    Làm mới
                </button>
                <button type="button" class="btn btn-primary" @click="selectedWorkspace = 'soc'">
                    Mở SOC
                </button>
            </div>
        </div>

        <section class="readiness-band">
            <div class="readiness-score">
                <span>Mục tiêu</span>
                <strong>100%</strong>
            </div>
            <div class="readiness-copy">
                <h2>Bảng điều phối an ninh hợp nhất</h2>
                <p>{{ statusMessage }}</p>
            </div>
            <div class="readiness-actions">
                <span class="status-pill" :class="{ danger: loadError }">
                    {{ loadError ? 'Cần chú ý' : 'Đang hoạt động' }}
                </span>
            </div>
        </section>

        <section class="metric-grid">
            <article v-for="metric in headlineMetrics" :key="metric.label" class="metric-tile">
                <span class="metric-label">{{ metric.label }}</span>
                <strong class="metric-value">{{ metric.value }}</strong>
                <span class="metric-note">{{ metric.note }}</span>
            </article>
        </section>

        <section class="workspace-tabs" aria-label="Các không gian làm việc doanh nghiệp">
            <button
                v-for="workspace in workspaces"
                :key="workspace.id"
                type="button"
                :class="{ active: selectedWorkspace === workspace.id }"
                @click="selectedWorkspace = workspace.id"
            >
                {{ workspace.label }}
            </button>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">{{ activeWorkspace.kicker }}</span>
                        <h2 class="panel-title">{{ activeWorkspace.title }}</h2>
                    </div>
                    <span class="soft-chip">{{ activeWorkspace.badge }}</span>
                </div>

                <div class="workspace-summary">
                    <div v-for="item in activeWorkspace.metrics" :key="item.label" class="workspace-stat">
                        <strong>{{ item.value }}</strong>
                        <span>{{ item.label }}</span>
                    </div>
                </div>

                <div v-if="isAdmin" class="action-strip">
                    <button
                        v-for="action in activeWorkspace.actions"
                        :key="action"
                        type="button"
                        class="btn btn-secondary btn-sm"
                        @click="selectedAction = action"
                    >
                        {{ action }}
                    </button>
                </div>
            </article>

            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Xác minh tăng cường</span>
                        <h2 class="panel-title">Phiên thao tác đặc quyền</h2>
                    </div>
                    <span class="soft-chip" :class="{ success: stepUp.active }">
                        {{ stepUp.active ? 'Đã xác minh' : 'Bắt buộc' }}
                    </span>
                </div>

                <form class="form-grid" @submit.prevent="verifyStepUp">
                    <label>
                        Hành động
                        <select v-model="stepUp.action">
                            <option value="AllPrivilegedActions">Tất cả thao tác đặc quyền</option>
                            <option value="UserAdministration">Quản trị người dùng</option>
                            <option value="AccessPolicyEmergency">Chính sách khẩn cấp</option>
                            <option value="DeviceConfiguration">Cấu hình thiết bị</option>
                            <option value="EvidenceExportApproval">Duyệt xuất chứng cứ</option>
                            <option value="EvidenceRetentionPurge">Dọn xóa lưu giữ chứng cứ</option>
                            <option value="SiteHierarchyBackfill">Bổ sung phân cấp site</option>
                            <option value="ReleaseApproval">Phê duyệt phát hành</option>
                        </select>
                    </label>
                    <label>
                        Mật khẩu
                        <input v-model="stepUp.password" type="password" autocomplete="current-password" />
                    </label>
                    <label>
                        Mã MFA
                        <input v-model="stepUp.mfaCode" inputmode="numeric" autocomplete="one-time-code" />
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.stepUp">
                        Xác minh
                    </button>
                </form>
                <p v-if="stepUp.message" class="inline-message">{{ stepUp.message }}</p>
            </article>
        </section>

        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Kiểm soát vận hành</span>
                        <h2 class="panel-title">Sức khỏe cấu hình</h2>
                    </div>
                    <span class="soft-chip" :class="{ success: configHealth.status === 'Healthy', danger: configHealth.status === 'Blocked' }">
                        {{ configHealthLabel(configHealth.status) || 'Chưa rõ' }}
                    </span>
                </div>
                <div class="finding-list">
                    <div v-for="finding in visibleFindings" :key="finding.key" class="finding-row">
                        <strong>{{ finding.key }}</strong>
                        <span :class="finding.status.toLowerCase()">{{ findingStatusLabel(finding.status) }}</span>
                    </div>
                </div>
            </article>

            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Nền tảng</span>
                        <h2 class="panel-title">Bổ sung tài sản kế thừa</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="backfillDefaultSite">
                    <label>
                        Mã công ty
                        <input v-model="backfillForm.companyCode" required />
                    </label>
                    <label>
                        Mã site
                        <input v-model="backfillForm.siteCode" required />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.backfill">
                        Chạy bổ sung an toàn
                    </button>
                </form>
                <div class="asset-map-summary">
                    <span>{{ assetMap.gates.length }} cổng</span>
                    <span>{{ assetMap.cameras.length }} camera</span>
                    <span>{{ assetMap.vehicles.length }} phương tiện</span>
                </div>
            </article>

        </section>

        <section class="ops-grid two">
            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Định danh</span>
                        <h2 class="panel-title">Nhà cung cấp và nhập liệu HR</h2>
                    </div>
                </div>
                <form class="form-grid" @submit.prevent="saveProvider">
                    <label>
                        Tên nhà cung cấp
                        <input v-model="providerForm.name" required />
                    </label>
                    <label>
                        Địa chỉ xác thực
                        <input v-model="providerForm.authority" required />
                    </label>
                    <label>
                        Mã client
                        <input v-model="providerForm.clientId" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.provider">
                        Lưu nhà cung cấp
                    </button>
                </form>
                <form class="form-grid stacked" @submit.prevent="importUser">
                    <label>
                        ID nhà cung cấp
                        <input v-model.number="importForm.providerId" type="number" min="1" required />
                    </label>
                    <label>
                        Mã định danh ngoài
                        <input v-model="importForm.externalSubject" required />
                    </label>
                    <label>
                        Tên đăng nhập
                        <input v-model="importForm.username" required />
                    </label>
                    <label>
                        Họ và tên
                        <input v-model="importForm.displayName" />
                    </label>
                    <label>
                        Email
                        <input v-model="importForm.email" type="email" />
                    </label>
                    <label>
                        Vòng đời
                        <select v-model="importForm.lifecycleStatus">
                            <option value="Active">Đang hoạt động</option>
                            <option value="Suspended">Tạm dừng</option>
                            <option value="Terminated">Đã nghỉ việc</option>
                            <option value="ContractorActive">Nhà thầu đang hiệu lực</option>
                            <option value="ContractorExpired">Nhà thầu hết hiệu lực</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.importUser">
                        Nhập người dùng
                    </button>
                </form>
            </article>

            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Thiết bị</span>
                        <h2 class="panel-title">Mô phỏng và diễn tập lỗi</h2>
                    </div>
                </div>
                <form class="form-grid" @submit.prevent="createVirtualController">
                    <label>
                        Tên bộ điều khiển
                        <input v-model="deviceForm.name" required />
                    </label>
                    <label>
                        Giao thức
                        <select v-model="deviceForm.protocol">
                            <option>OSDP-Sim</option>
                            <option>ONVIF-Access-Sim</option>
                        </select>
                    </label>
                    <label>
                        Số định danh tối đa
                        <input v-model.number="deviceForm.maxCredentials" type="number" min="1" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.device">
                        Tạo bộ mô phỏng
                    </button>
                </form>
                <form class="form-grid stacked" @submit.prevent="injectFault">
                    <label>
                        ID thiết bị
                        <input v-model.number="faultForm.securityDeviceId" type="number" min="1" required />
                    </label>
                    <label>
                        Loại lỗi
                        <select v-model="faultForm.status">
                            <option value="Tamper">Can thiệp trái phép</option>
                            <option value="Offline">Mất kết nối</option>
                            <option value="RelayFailure">Lỗi relay</option>
                            <option value="BarrierStuck">Barrier kẹt</option>
                        </select>
                    </label>
                    <label>
                        Mức độ
                        <select v-model="faultForm.severity">
                            <option value="Medium">Trung bình</option>
                            <option value="High">Cao</option>
                            <option value="Critical">Nghiêm trọng</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.fault">
                        Gây lỗi mô phỏng
                    </button>
                </form>
            </article>
        </section>

        <section class="ops-grid three">
            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">SOC</span>
                        <h2 class="panel-title">Tiếp nhận cảnh báo</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="createAlarm">
                    <label>
                        Tóm tắt
                        <input v-model="alarmForm.summary" required />
                    </label>
                    <label>
                        Mức độ
                        <select v-model="alarmForm.severity">
                            <option value="Medium">Trung bình</option>
                            <option value="High">Cao</option>
                            <option value="Critical">Nghiêm trọng</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.alarm">
                        Tạo cảnh báo
                    </button>
                </form>
                <div v-if="socIntel.summary" class="soc-intel-summary">
                    <span class="soft-chip" :class="riskChipClass">{{ riskLabel }}</span>
                    <p>{{ socIntel.summary }}</p>
                </div>
            </article>

            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Khôi phục</span>
                        <h2 class="panel-title">Diễn tập sao lưu</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="startBackup">
                    <label>
                        Hồ sơ
                        <select v-model="backupForm.profile">
                            <option value="MediumCompany">Doanh nghiệp vừa</option>
                            <option value="LargeCompany">Doanh nghiệp lớn</option>
                            <option value="Production">Môi trường thực</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.backup">
                        Bắt đầu sao lưu
                    </button>
                </form>
            </article>

            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Phát hành</span>
                        <h2 class="panel-title">Chứng cứ QA</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="createQaRun">
                    <label>
                        Loại kiểm thử
                        <select v-model="qaForm.testType">
                            <option value="E2E">Kiểm thử đầu cuối</option>
                            <option value="LoadStressSoakChaos">Tải, stress, soak, chaos</option>
                            <option value="HardwareSimulator">Mô phỏng phần cứng</option>
                            <option value="Migration">Di trú dữ liệu</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.qa">
                        Ghi nhận lượt QA
                    </button>
                </form>
            </article>
        </section>

        <!-- Ops Workspace: Restore, Security Checks, Outbox -->
        <section v-if="selectedWorkspace === 'ops'" class="ops-grid three">
            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Khôi phục</span>
                        <h2 class="panel-title">Diễn tập khôi phục</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="startRestore">
                    <label>
                        ID lượt sao lưu
                        <input v-model.number="restoreForm.backupRunId" type="number" required />
                    </label>
                    <label>
                        RTO mục tiêu (phút)
                        <input v-model.number="restoreForm.targetRtoMinutes" type="number" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.restore">
                        {{ busy.restore ? 'Đang bắt đầu...' : 'Bắt đầu khôi phục' }}
                    </button>
                </form>
                <div v-if="restoreResult" class="success-card" style="margin-top:8px;">{{ restoreResult }}</div>
            </article>
            <article v-if="isAdmin" class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">An ninh</span>
                        <h2 class="panel-title">Kiểm tra an ninh</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="recordSecurityCheck">
                    <label>
                        Loại kiểm tra
                        <select v-model="securityForm.checkType">
                            <option value="PhysicalPatrol">Tuần tra vật lý</option>
                            <option value="CameraReview">Rà soát camera</option>
                            <option value="DoorAudit">Kiểm tra cửa</option>
                            <option value="PerimeterCheck">Kiểm tra vành đai</option>
                            <option value="ComplianceAudit">Kiểm tra tuân thủ</option>
                        </select>
                    </label>
                    <label>
                        Trạng thái
                        <select v-model="securityForm.status">
                            <option value="Pass">Đạt</option>
                            <option value="Fail">Không đạt</option>
                            <option value="Degraded">Suy giảm</option>
                        </select>
                    </label>
                    <label>
                        Ghi chú
                        <textarea v-model="securityForm.notes" class="form-input" rows="2"></textarea>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.security">
                        {{ busy.security ? 'Đang ghi nhận...' : 'Ghi nhận kiểm tra' }}
                    </button>
                </form>
                <div v-if="securityResult" class="success-card" style="margin-top:8px;">{{ securityResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Hàng chờ gửi</span>
                        <h2 class="panel-title">Outbox và webhook</h2>
                    </div>
                </div>
                <div class="incident-brief-form" style="flex-wrap:wrap;">
                    <select v-model="outboxFilter" class="filter-select" style="flex:1;">
                        <option value="">Tất cả</option>
                        <option value="Pending">Đang chờ</option>
                        <option value="Failed">Lỗi</option>
                        <option value="Delivered">Đã gửi</option>
                    </select>
                    <button class="btn btn-primary btn-sm" :disabled="outboxLoading" @click="loadOutboxEvents">
                        {{ outboxLoading ? 'Đang tải...' : 'Tải' }}
                    </button>
                </div>
                <div v-if="outboxEvents.length === 0" class="empty-card">Chưa có sự kiện outbox.</div>
                <div v-else class="table-container" style="max-height:200px;overflow-y:auto;">
                    <table class="data-table">
                        <thead><tr><th>Loại</th><th>Trạng thái</th><th>Số lần thử lại</th></tr></thead>
                        <tbody>
                            <tr v-for="e in outboxEvents" :key="e.outboxEventId">
                                <td class="table-sub">{{ (e.eventType || '').substring(0, 20) }}</td>
                                <td><span class="soft-chip" :class="e.status === 'Failed' ? 'danger' : e.status === 'Delivered' ? 'success' : 'warn'">{{ outboxStatusLabel(e.status) }}</span></td>
                                <td><span v-if="e.retryCount != null" class="text-muted">{{ e.retryCount }}</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <!-- Ops backup list -->
        <section v-if="selectedWorkspace === 'ops'" class="ops-grid two" style="margin-top:0.5rem;">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Sao lưu</span>
                        <h2 class="panel-title">Các lượt sao lưu gần đây</h2>
                    </div>
                    <button class="btn btn-sm btn-secondary" :disabled="backupLoading" @click="loadBackupRuns">Làm mới</button>
                </div>
                <div v-if="backupLoading" class="empty-card">Đang tải...</div>
                <div v-else-if="backupRuns.length === 0" class="empty-card">Chưa có lượt sao lưu.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Hồ sơ</th><th>Trạng thái</th><th>Bắt đầu</th><th>RPO</th></tr></thead>
                        <tbody>
                            <tr v-for="b in backupRuns" :key="b.backupRunId">
                                <td>{{ b.profile || '—' }}</td>
                                <td><span class="soft-chip" :class="b.status === 'Completed' ? 'success' : b.status === 'Failed' ? 'danger' : 'warn'">{{ runStatusLabel(b.status) }}</span></td>
                                <td class="table-sub">{{ new Date(b.startedAtUtc).toLocaleString() }}</td>
                                <td>{{ b.achievedRpoMinutes || '—' }}m</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Khôi phục</span>
                        <h2 class="panel-title">Các lượt diễn tập khôi phục</h2>
                    </div>
                    <button class="btn btn-sm btn-secondary" :disabled="restoreLoading" @click="loadRestoreDrills">Làm mới</button>
                </div>
                <div v-if="restoreLoading" class="empty-card">Đang tải...</div>
                <div v-else-if="restoreDrills.length === 0" class="empty-card">Chưa có lượt diễn tập khôi phục.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Trạng thái</th><th>RTO mục tiêu</th><th>Bắt đầu</th></tr></thead>
                        <tbody>
                            <tr v-for="r in restoreDrills" :key="r.restoreDrillId">
                                <td>{{ r.restoreDrillId }}</td>
                                <td><span class="soft-chip" :class="r.status === 'Completed' ? 'success' : r.status === 'Failed' ? 'danger' : 'warn'">{{ runStatusLabel(r.status) }}</span></td>
                                <td>{{ r.targetRtoMinutes || '—' }}m</td>
                                <td class="table-sub">{{ new Date(r.startedAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'soc'" class="ops-grid two">
            <!-- SOC content (same as before) -->
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Trí tuệ AI</span>
                        <h2 class="panel-title">Phân tích SOC</h2>
                    </div>
                    <button type="button" class="btn btn-sm btn-secondary" @click="loadSocIntel">Làm mới</button>
                </div>
                <div class="soc-stats-grid">
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.totalToday }}</strong>
                        <span>Cảnh báo hôm nay</span>
                        <span class="soc-change" :class="{ up: socIntel.statistics.changePercent > 0, down: socIntel.statistics.changePercent < 0 }">
                            {{ socIntel.statistics.changePercent > 0 ? '+' : '' }}{{ socIntel.statistics.changePercent }}%
                        </span>
                    </div>
                    <div class="soc-stat">
                        <strong class="text-danger">{{ socIntel.statistics.criticalOpenAlarms }}</strong>
                        <span>Nghiêm trọng đang mở</span>
                    </div>
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.openAlarms }}</strong>
                        <span>Tổng cảnh báo mở</span>
                    </div>
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.avgResolutionHours }}</strong>
                        <span>Giờ xử lý TB</span>
                    </div>
                </div>
                <div v-if="Object.keys(socIntel.statistics.bySeverity).length" class="soc-severity-breakdown">
                    <h4>Phân bố theo mức độ</h4>
                    <div v-for="(count, sev) in socIntel.statistics.bySeverity" :key="sev" class="severity-bar-row">
                        <span>{{ severityLabel(sev) }}</span>
                        <div class="severity-bar-track">
                            <div class="severity-bar-fill" :class="'sev-' + sev.toLowerCase()" :style="{ width: (count / Math.max(...Object.values(socIntel.statistics.bySeverity)) * 100) + '%' }"></div>
                        </div>
                        <span>{{ count }}</span>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Trợ lý AI</span>
                        <h2 class="panel-title">Phân tích sự cố</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="incidentBriefing.incidentId" type="number" min="1" placeholder="ID sự cố" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="incidentBriefing.loading" @click="analyzeIncident">
                        {{ incidentBriefing.loading ? 'Đang phân tích...' : 'Phân tích bằng AI' }}
                    </button>
                </div>
                <div v-if="incidentBriefing.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(incidentBriefing.result.severity)">
                            {{ severityLabel(incidentBriefing.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ incidentBriefing.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ incidentBriefing.result.summary }}</p>
                    <div v-if="incidentBriefing.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phân tích:</strong>
                        <p>{{ incidentBriefing.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="incidentBriefing.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(incidentBriefing.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(incidentBriefing.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'reception'" class="ops-grid two">
            <!-- Reception content -->
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI sàng lọc rủi ro</span>
                        <h2 class="panel-title">Sàng lọc khách</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="visitorScreening.visitId" type="number" min="1" placeholder="ID lượt thăm" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="visitorScreening.loading" @click="screenVisitor">
                        {{ visitorScreening.loading ? 'Đang phân tích...' : 'Phân tích rủi ro' }}
                    </button>
                </div>
                <div v-if="visitorScreening.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(visitorScreening.result.severity)">
                            {{ severityLabel(visitorScreening.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ visitorScreening.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ visitorScreening.result.summary }}</p>
                    <div v-if="visitorScreening.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(visitorScreening.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(visitorScreening.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Thao tác nhanh</span>
                        <h2 class="panel-title">Nhật ký sàng lọc</h2>
                    </div>
                </div>
                <div class="empty-card">Các kết quả phân tích rủi ro khách thăm sẽ hiện ở đây.</div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'gate'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI sàng lọc rủi ro</span>
                        <h2 class="panel-title">Sàng lọc phương tiện</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="vehicleScreening.vehicleId" type="number" min="1" placeholder="ID phương tiện" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="vehicleScreening.loading" @click="screenVehicle">
                        {{ vehicleScreening.loading ? 'Đang phân tích...' : 'Phân tích rủi ro' }}
                    </button>
                </div>
                <div v-if="vehicleScreening.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(vehicleScreening.result.severity)">
                            {{ severityLabel(vehicleScreening.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ vehicleScreening.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ vehicleScreening.result.summary }}</p>
                    <div v-if="vehicleScreening.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(vehicleScreening.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(vehicleScreening.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Sức khỏe làn</span>
                        <h2 class="panel-title">Tóm tắt vận hành</h2>
                    </div>
                    <span class="soft-chip" :class="{ danger: laneHealthSummary.degradedCount > 0, success: laneHealthSummary.degradedCount === 0 }">
                        {{ laneHealthSummary.degradedCount > 0 ? 'Cần theo dõi' : 'Ổn định' }}
                    </span>
                </div>
                <div v-if="laneHealthSummary.total === 0" class="empty-card">Chưa có làn hoạt động được kết nối.</div>
                <div v-else class="finding-list">
                    <div class="finding-row">
                        <strong>Làn ổn định</strong>
                        <span>{{ laneHealthSummary.healthyCount }}</span>
                    </div>
                    <div class="finding-row">
                        <strong>Làn cần chú ý</strong>
                        <span :class="{ fail: laneHealthSummary.degradedCount > 0 }">{{ laneHealthSummary.degradedCount }}</span>
                    </div>
                    <div class="finding-row">
                        <strong>Số barrier đang phủ</strong>
                        <span>{{ laneHealthSummary.barrierCount }}</span>
                    </div>
                    <div v-for="lane in laneHealthFocus" :key="lane.laneId" class="finding-row">
                        <strong>{{ lane.name || `Làn ${lane.laneId}` }}</strong>
                        <span :class="{ fail: lane.isDegraded }">
                            {{ lane.isDegraded ? 'Cần chú ý' : 'Ổn định' }}
                        </span>
                    </div>
                </div>
                <p v-if="laneHealthSummary.degradedCount > 0" class="inline-message">
                    Khu vực cần chú ý: {{ laneHealthSummary.degradedNames.join(', ') }}
                </p>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'auditor'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI phân tích</span>
                        <h2 class="panel-title">Phân tích chứng cứ</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="evidenceAnalysis.evidenceId" type="number" min="1" placeholder="ID chứng cứ" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="evidenceAnalysis.loading" @click="analyzeEvidence">
                        {{ evidenceAnalysis.loading ? 'Đang phân tích...' : 'Phân tích bằng AI' }}
                    </button>
                </div>
                <div v-if="evidenceAnalysis.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(evidenceAnalysis.result.severity)">
                            {{ severityLabel(evidenceAnalysis.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ evidenceAnalysis.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ evidenceAnalysis.result.summary }}</p>
                    <div v-if="evidenceAnalysis.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phân tích:</strong>
                        <p>{{ evidenceAnalysis.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="evidenceAnalysis.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(evidenceAnalysis.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(evidenceAnalysis.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI kiểm tra</span>
                        <h2 class="panel-title">Duyệt yêu cầu xuất</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="evidenceExport.exportId" type="number" min="1" placeholder="ID yêu cầu xuất" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="evidenceExport.loading" @click="reviewExport">
                        {{ evidenceExport.loading ? 'Đang kiểm tra...' : 'Kiểm tra xuất' }}
                    </button>
                </div>
                <div v-if="evidenceExport.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(evidenceExport.result.severity)">
                            {{ severityLabel(evidenceExport.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ evidenceExport.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ evidenceExport.result.summary }}</p>
                    <div v-if="evidenceExport.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(evidenceExport.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(evidenceExport.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'admin'" class="ops-grid two">
            <article class="ops-panel policy-admin-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Quản trị chính sách truy cập</span>
                        <h2 class="panel-title">Quản trị policy truy cập</h2>
                    </div>
                    <span class="soft-chip">{{ overview.policy.policyVersions || 0 }} phiên bản</span>
                </div>

                <div class="workspace-tabs policy-tabs">
                    <button type="button" :class="{ active: policyAdminTab === 'simulate' }" @click="policyAdminTab = 'simulate'">Mô phỏng</button>
                    <button type="button" :class="{ active: policyAdminTab === 'versions' }" @click="policyAdminTab = 'versions'; loadPolicyVersions()">Phiên bản</button>
                    <button type="button" :class="{ active: policyAdminTab === 'rules' }" @click="policyAdminTab = 'rules'; loadPolicyRules()">Luật truy cập</button>
                </div>

                <div v-if="policyAdminMessage" class="inline-message">{{ policyAdminMessage }}</div>

                <div v-if="policyAdminTab === 'simulate'" class="policy-admin-body">
                    <form class="form-grid single" @submit.prevent="simulatePolicy">
                        <label>
                            ID đối tượng
                            <input v-model.number="policyForm.subjectId" type="number" min="1" required />
                        </label>
                        <label>
                            Loại định danh
                            <select v-model="policyForm.credentialType">
                                <option>QR</option>
                                <option>Badge</option>
                                <option>EmergencyOverride</option>
                            </select>
                        </label>
                        <button type="submit" class="btn btn-secondary" :disabled="busy.policy">
                            Mô phỏng quyết định
                        </button>
                    </form>
                    <div v-if="policyResult.reason" class="policy-result-card">
                        <div class="rec-header">
                            <span class="soft-chip" :class="{ danger: policyResult.result === 'Deny', success: policyResult.result === 'Allow' }">
                                {{ policyDecisionLabel(policyResult.result) || 'Chưa có kết quả' }}
                            </span>
                            <small>Chế độ: {{ policyDecisionModeLabel(policyResult.decisionMode) || 'Mô phỏng' }}</small>
                        </div>
                        <p class="brief-summary">{{ policyResult.reason }}</p>
                    </div>
                </div>

                <div v-else-if="policyAdminTab === 'versions'" class="policy-admin-body">
                    <form class="form-grid single" @submit.prevent="createPolicyVersion">
                        <label>
                            Tên phiên bản policy
                            <input v-model.trim="policyVersionForm.name" required />
                        </label>
                        <label>
                            Tóm tắt thay đổi
                            <textarea v-model.trim="policyVersionForm.changeSummary"></textarea>
                        </label>
                        <button type="submit" class="btn btn-secondary" :disabled="policyGovernanceLoading || !policyVersionForm.name">
                            Tạo phiên bản mới
                        </button>
                    </form>

                    <div v-if="policyGovernanceLoading && policyVersions.length === 0" class="empty-card">Đang tải phiên bản policy...</div>
                    <div v-else-if="policyVersions.length === 0" class="empty-card">Chưa có phiên bản policy nào.</div>
                    <div v-else class="version-list">
                        <div v-for="version in policyVersions" :key="version.accessPolicyVersionId" class="version-row">
                            <div class="version-info">
                                <strong>{{ version.name }}</strong>
                                <span class="version-meta">Trạng thái: {{ policyStatusLabel(version.status) }} · {{ version.rules ?? version.ruleCount ?? 0 }} luật</span>
                            </div>
                            <div class="version-badges">
                                <span class="badge" :class="statusClass(version.status)">{{ policyStatusLabel(version.status) }}</span>
                                <button v-if="version.status === 'Draft'" class="btn btn-xs btn-secondary" :disabled="policyGovernanceLoading" @click="submitPolicyVersion(version)">Gửi duyệt</button>
                                <button v-if="version.status === 'PendingApproval'" class="btn btn-xs btn-primary" :disabled="policyGovernanceLoading" @click="approvePolicyVersion(version)">Phê duyệt</button>
                                <button v-if="version.status === 'Approved'" class="btn btn-xs btn-success" :disabled="policyGovernanceLoading" @click="activatePolicyVersion(version)">Kích hoạt</button>
                                <button v-if="version.status === 'Active'" class="btn btn-xs btn-secondary" :disabled="policyGovernanceLoading" @click="retirePolicyVersion(version)">Ngừng hiệu lực</button>
                            </div>
                        </div>
                    </div>
                </div>

                <div v-else class="policy-admin-body">
                    <form class="form-grid" @submit.prevent="createPolicyRule">
                        <label>
                            Loại đối tượng
                            <select v-model="policyRuleForm.subjectType">
                                <option>Employee</option>
                                <option>Visitor</option>
                                <option>Contractor</option>
                            </select>
                        </label>
                        <label>
                            Loại định danh
                            <select v-model="policyRuleForm.credentialType">
                                <option>Any</option>
                                <option>QR</option>
                                <option>Bio</option>
                                <option>Card</option>
                            </select>
                        </label>
                        <label>
                            ID đối tượng
                            <input v-model.number="policyRuleForm.subjectId" type="number" min="1" />
                        </label>
                        <label>
                            ID mức truy cập
                            <input v-model.number="policyRuleForm.accessLevelId" type="number" min="1" required />
                        </label>
                        <label>
                            Mã khu vực
                            <input v-model.number="policyRuleForm.siteId" type="number" min="1" />
                        </label>
                        <label>
                            ID khu vực
                            <input v-model.number="policyRuleForm.securityZoneId" type="number" min="1" />
                        </label>
                        <label>
                            ID điểm truy cập
                            <input v-model.number="policyRuleForm.accessPointId" type="number" min="1" />
                        </label>
                        <label class="checkbox-row">
                            <input v-model="policyRuleForm.allowAccess" type="checkbox" />
                            <span>Cho phép truy cập</span>
                        </label>
                        <button type="submit" class="btn btn-secondary" :disabled="policyGovernanceLoading || !policyRuleForm.accessLevelId">
                            Tạo luật truy cập
                        </button>
                    </form>

                    <div v-if="policyGovernanceLoading && policyRules.length === 0" class="empty-card">Đang tải luật policy...</div>
                    <div v-else-if="policyRules.length === 0" class="empty-card">Chưa có luật truy cập nào.</div>
                    <div v-else class="rule-list">
                        <div v-for="rule in policyRules" :key="rule.accessRuleId" class="rule-row">
                            <div class="rule-info">
                                <strong>{{ rule.allowAccess ? 'Cho phép' : 'Từ chối' }}</strong>
                                <span class="rule-detail">{{ rule.subjectType }}:{{ rule.subjectId || '*' }} · {{ rule.credentialType }}</span>
                            </div>
                            <div class="rule-scope">
                                <span v-if="rule.siteId">Khu vực {{ rule.siteId }}</span>
                                <span v-if="rule.securityZoneId">Khu vực {{ rule.securityZoneId }}</span>
                                <span v-if="rule.accessPointId">Điểm truy cập {{ rule.accessPointId }}</span>
                            </div>
                            <span class="badge" :class="rule.isActive ? 'badge-green' : 'badge-gray'">{{ rule.isActive ? 'Đang hiệu lực' : 'Không hiệu lực' }}</span>
                        </div>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Chính sách</span>
                        <h2 class="panel-title">Phân tích và giải thích policy</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="policySimulation.policyId" type="number" min="1" placeholder="ID phiên bản chính sách" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="policySimulation.loading" @click="simulateAiPolicy">
                        {{ policySimulation.loading ? 'Đang mô phỏng...' : 'Mô phỏng chính sách' }}
                    </button>
                    <button class="btn btn-secondary btn-sm" :disabled="policySimulation.loading" @click="explainAiPolicy">
                        Giải thích
                    </button>
                </div>
                <div v-if="policySimulation.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(policySimulation.result.severity)">
                            {{ severityLabel(policySimulation.result.severity) || 'Không rõ' }}
                        </span>
                        <small>Nhà cung cấp: {{ policySimulation.result.provider || 'Không rõ' }}</small>
                    </div>
                    <p class="brief-summary">{{ policySimulation.result.summary }}</p>
                    <div v-if="policySimulation.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phân tích:</strong>
                        <p>{{ policySimulation.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="policySimulation.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(policySimulation.result.recommendationId)">Phê duyệt</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(policySimulation.result.recommendationId)">Từ chối</button>
                    </div>
                </div>
                <div v-else class="empty-card">
                    Nhập ID phiên bản chính sách để xem mô phỏng hoặc giải thích bằng ngôn ngữ tự nhiên.
                </div>
            </article>
        </section>

        <section class="ops-panel audit-panel">
            <div class="panel-head compact">
                <div>
                    <span class="panel-kicker">Hoạt động</span>
                    <h2 class="panel-title">Các thao tác cục bộ gần nhất</h2>
                </div>
            </div>
            <div v-if="activityLog.length" class="activity-list">
                <div v-for="item in activityLog" :key="item.id" class="activity-row">
                    <span>{{ item.time }}</span>
                    <strong>{{ item.title }}</strong>
                    <p>{{ item.detail }}</p>
                </div>
            </div>
            <div v-else class="empty-card">Chưa có thao tác cục bộ trong phiên này.</div>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { authState } from '../stores/auth'
import { enterpriseApi, socIntelApi } from '../services/enterpriseSecurityApi'
import { enterpriseAiApi } from '../services/enterpriseAiApi'

const loading = ref(false)
const loadError = ref('')
const selectedWorkspace = ref('admin')
const selectedAction = ref('')
const activityLog = ref([])
const isAdmin = computed(() => authState.user?.role === 'Admin')

const overview = reactive({
    foundation: {},
    identity: {},
    policy: {},
    visitorVehicle: {},
    devices: {},
    soc: {},
    evidence: {},
    operations: {},
    release: {},
})
const configHealth = reactive({
    status: '',
    findings: [],
})
const socIntel = reactive({
    summary: '',
    overallRisk: 'thap',
    statistics: { totalToday: 0, totalYesterday: 0, changePercent: 0, openAlarms: 0, criticalOpenAlarms: 0, avgResolutionHours: 0, bySeverity: {}, byType: {}, byHour: {} },
    anomalies: [],
})

const incidentBriefing = reactive({ incidentId: null, loading: false, result: null })
const evidenceAnalysis = reactive({ evidenceId: null, loading: false, result: null })
const evidenceExport = reactive({ exportId: null, loading: false, result: null })
const deviceInsights = reactive({ loading: false, items: [] })
const visitorScreening = reactive({ visitId: null, loading: false, result: null })
const vehicleScreening = reactive({ vehicleId: null, loading: false, result: null })
const policySimulation = reactive({ policyId: null, loading: false, result: null })

const eventFeed = reactive({
    events: [], loading: false,
    filter: { sourceType: '', eventType: '', cameraId: null, subjectId: '', limit: 20 },
})

const nlQuery = reactive({ queryText: '', loading: false, result: null })

const assetMap = reactive({ gates: [], cameras: [], vehicles: [] })
const laneHealth = ref([])

const busy = reactive({
    stepUp: false, provider: false, importUser: false, device: false,
    fault: false, alarm: false, backup: false, qa: false, backfill: false,
    policy: false, ai: false, restore: false, security: false,
})

const stepUp = reactive({ action: 'AllPrivilegedActions', password: '', mfaCode: '', sessionId: null, active: false, message: '' })

const providerForm = reactive({ name: 'Corporate IdP', protocol: 'OIDC', authority: 'https://idp.company.local', clientId: 'v-shield', isEnabled: true })
const importForm = reactive({ providerId: 1, externalSubject: 'employee-001', username: 'employee.001', displayName: 'Employee 001', email: 'employee.001@company.local', phone: '', role: 'LeTan', lifecycleStatus: 'Active', primarySiteId: null })
const deviceForm = reactive({ name: 'Virtual Controller 01', protocol: 'OSDP-Sim', direction: 'Entry', maxCredentials: 50000 })
const faultForm = reactive({ securityDeviceId: null, status: 'Tamper', severity: 'High', message: 'Operator drill' })
const alarmForm = reactive({ summary: 'Manual SOC drill alarm', severity: 'High' })
const backupForm = reactive({ profile: 'MediumCompany' })
const qaForm = reactive({ testType: 'LoadStressSoakChaos' })
const backfillForm = reactive({ companyName: 'V-Shield Company', companyCode: 'VSHIELD', siteName: 'Headquarters', siteCode: 'HQ', timeZoneId: 'Asia/Ho_Chi_Minh' })
const policyForm = reactive({ subjectType: 'Employee', subjectId: 1, siteId: null, securityZoneId: null, accessPointId: null, credentialType: 'QR', allowHolidayAccess: false, evaluatedAtUtc: null })
const policyResult = reactive({ result: '', reason: '', decisionMode: '' })
const policyAdminTab = ref('simulate')
const policyGovernanceLoading = ref(false)
const policyAdminMessage = ref('')
const policyVersions = ref([])
const policyRules = ref([])
const policyVersionForm = reactive({ name: '', changeSummary: '' })
const policyRuleForm = reactive({
    subjectType: 'Employee',
    credentialType: 'Any',
    subjectId: null,
    accessLevelId: null,
    accessGroupId: null,
    siteId: null,
    securityZoneId: null,
    accessPointId: null,
    accessScheduleId: null,
    allowAccess: true,
    isActive: true,
})

// Ops workspace state
const restoreForm = reactive({ backupRunId: null, targetRtoMinutes: 60 })
const restoreResult = ref('')
const securityForm = reactive({ checkType: 'PhysicalPatrol', status: 'Pass', notes: '' })
const securityResult = ref('')
const outboxFilter = ref('')
const outboxLoading = ref(false)
const outboxEvents = ref([])
const backupLoading = ref(false)
const backupRuns = ref([])
const restoreLoading = ref(false)
const restoreDrills = ref([])

const statusMessage = computed(() => {
    if (loadError.value) return loadError.value
    if (loading.value) return 'Đang làm mới dữ liệu an ninh doanh nghiệp.'
    return 'Các màn hình vận hành đang kết nối với API doanh nghiệp và các cổng kiểm soát nội bộ.'
})

const headlineMetrics = computed(() => [
    { label: 'Site', value: overview.foundation.sites || 0, note: `${overview.foundation.accessPoints || 0} điểm truy cập` },
    { label: 'Cảnh báo mở', value: overview.soc.openAlarms || 0, note: `${overview.soc.criticalOpenAlarms || 0} mức nghiêm trọng` },
    { label: 'Thiết bị', value: overview.devices.devices || 0, note: `${overview.devices.offlinePackages || 0} gói offline` },
    { label: 'Chứng cứ', value: overview.evidence.evidenceItems || 0, note: `${overview.evidence.pendingExports || 0} yêu cầu xuất chờ xử lý` },
    { label: 'Outbox', value: overview.operations.pendingOutboxEvents || 0, note: `${overview.operations.failedOutboxEvents || 0} lỗi` },
    { label: 'Cổng phát hành', value: overview.release.pendingRequiredGates || 0, note: `${overview.release.approvedReleaseCandidates || 0} bản phát hành đã duyệt` },
])

const laneHealthSummary = computed(() => {
    const lanes = Array.isArray(laneHealth.value) ? laneHealth.value : []
    const degraded = lanes.filter((lane) => lane?.isDegraded)

    return {
        total: lanes.length,
        healthyCount: Math.max(0, lanes.length - degraded.length),
        degradedCount: degraded.length,
        barrierCount: lanes.reduce((sum, lane) => sum + Number(lane?.barrierCount || 0), 0),
        degradedNames: degraded.map((lane) => lane?.name).filter(Boolean).slice(0, 3),
    }
})

const laneHealthFocus = computed(() =>
    [...(Array.isArray(laneHealth.value) ? laneHealth.value : [])]
        .sort((left, right) => Number(Boolean(right?.isDegraded)) - Number(Boolean(left?.isDegraded)))
        .slice(0, 4)
)

const workspaces = computed(() => [
    {
        id: 'admin', label: 'Quản trị', kicker: 'Quản trị hệ thống', title: 'Nền tảng và định danh',
        badge: `${overview.identity.activeMappings || 0} ánh xạ đang hoạt động`,
        metrics: [
            { label: 'Công ty', value: overview.foundation.companies || 0 },
            { label: 'Nhà cung cấp định danh', value: overview.identity.enabledProviders || 0 },
            { label: 'Người dùng đã nghỉ việc', value: overview.identity.terminatedEmployees || 0 },
        ],
        actions: ['Nhà cung cấp', 'Nhập HR', 'Tái chứng nhận'],
    },
    {
        id: 'soc', label: 'SOC', kicker: 'Trung tâm chỉ huy', title: 'Cảnh báo và sự cố',
        badge: `${overview.soc.openIncidents || 0} sự cố đang mở`,
        metrics: [
            { label: 'Cảnh báo mở', value: overview.soc.openAlarms || 0 },
            { label: 'SOP đang chạy', value: overview.soc.activeSops || 0 },
            { label: 'Tác vụ điều phối', value: overview.soc.openDispatchTasks || 0 },
        ],
        actions: ['Xác nhận', 'Điều phối', 'Bàn giao'],
    },
    {
        id: 'reception', label: 'Lễ tân', kicker: 'Bàn tiếp đón', title: 'Lượt thăm và danh sách theo dõi',
        badge: `${overview.visitorVehicle.watchlistMatches || 0} kết quả trùng khớp`,
        metrics: [
            { label: 'Lượt thăm', value: overview.visitorVehicle.visits || 0 },
            { label: 'Định danh', value: overview.visitorVehicle.visitorCredentials || 0 },
            { label: 'Mục theo dõi', value: overview.visitorVehicle.watchlistEntries || 0 },
        ],
        actions: ['Check-in', 'Biểu mẫu', 'Quá giờ'],
    },
    {
        id: 'gate', label: 'Cổng xe', kicker: 'Làn phương tiện', title: 'Bãi xe và barrier',
        badge: laneHealthSummary.value.degradedCount > 0
            ? `${laneHealthSummary.value.degradedCount} làn cần chú ý`
            : `${laneHealthSummary.value.barrierCount || overview.visitorVehicle.barriers || 0} thanh chắn`,
        metrics: [
            { label: 'Giấy phép đỗ xe', value: overview.visitorVehicle.parkingPermits || 0 },
            { label: 'Làn ổn định', value: laneHealthSummary.value.healthyCount },
            { label: 'Làn cần chú ý', value: laneHealthSummary.value.degradedCount },
            { label: 'Lệnh barrier', value: overview.visitorVehicle.barrierCommands || 0 },
        ],
        actions: ['Duyệt biển số', 'Mở barrier', 'Ngoại lệ'],
    },
    {
        id: 'auditor', label: 'Kiểm soát', kicker: 'Quản trị tuân thủ', title: 'Chứng cứ và tuân thủ',
        badge: `${overview.evidence.activeLegalHolds || 0} lệnh giữ pháp lý`,
        metrics: [
            { label: 'Bộ sưu tập', value: overview.evidence.collections || 0 },
            { label: 'Nhật ký truy cập', value: overview.evidence.accessLogs || 0 },
            { label: 'Báo cáo', value: overview.evidence.complianceReports || 0 },
        ],
        actions: ['Duyệt xuất', 'Lưu giữ', 'Báo cáo'],
    },
    {
        id: 'ops', label: 'Vận hành', kicker: 'Khả năng phục hồi', title: 'Sao lưu, khôi phục và an ninh',
        badge: `${overview.operations.degradedDependencies || 0} phụ thuộc suy giảm`,
        metrics: [
            { label: 'Sao lưu', value: overview.operations.backupRuns || 0 },
            { label: 'Diễn tập khôi phục', value: overview.operations.restoreDrills || 0 },
            { label: 'Kiểm tra an ninh', value: overview.operations.securityChecks || 0 },
        ],
        actions: ['Outbox', 'Sao lưu', 'Khôi phục', 'An ninh'],
    },
])

const activeWorkspace = computed(() =>
    workspaces.value.find((workspace) => workspace.id === selectedWorkspace.value) || workspaces.value[0]
)

const visibleFindings = computed(() =>
    (configHealth.findings || []).filter((finding) => finding.status !== 'Pass').slice(0, 5)
)

const riskLabel = computed(() => {
    switch (socIntel.overallRisk) {
        case 'cao': return 'Rủi ro cao'
        case 'trung_binh': return 'Rủi ro trung bình'
        default: return 'Rủi ro thấp'
    }
})

const riskChipClass = computed(() => {
    switch (socIntel.overallRisk) {
        case 'cao': return 'danger'
        case 'trung_binh': return 'warning'
        default: return 'success'
    }
})

async function loadOverview() {
    loading.value = true
    loadError.value = ''
    try {
        const [foundation, identity, policy, visitorVehicle, devices, soc, evidence, operations, release] = await enterpriseApi.overview()
        Object.assign(overview.foundation, normalizeKeys(foundation.data))
        Object.assign(overview.identity, normalizeKeys(identity.data))
        Object.assign(overview.policy, normalizeKeys(policy.data))
        Object.assign(overview.visitorVehicle, normalizeKeys(visitorVehicle.data))
        Object.assign(overview.devices, normalizeKeys(devices.data))
        Object.assign(overview.soc, normalizeKeys(soc.data))
        Object.assign(overview.evidence, normalizeKeys(evidence.data))
        Object.assign(overview.operations, normalizeKeys(operations.data))
        Object.assign(overview.release, normalizeKeys(release.data))

        const [configResult, assetResult, laneHealthResult] = await Promise.allSettled([
            enterpriseApi.configHealth(),
            enterpriseApi.assetMap(),
            enterpriseApi.getLaneHealth(),
        ])
        if (configResult.status === 'fulfilled') {
            const normalized = normalizeKeys(configResult.value.data)
            configHealth.status = normalized.status || ''
            configHealth.findings = normalized.findings || []
        }
        if (assetResult.status === 'fulfilled') {
            const normalized = normalizeKeys(assetResult.value.data)
            assetMap.gates = normalized.gates || []
            assetMap.cameras = normalized.cameras || []
            assetMap.vehicles = normalized.vehicles || []
        }
        laneHealth.value = laneHealthResult.status === 'fulfilled' ? (laneHealthResult.value.data || []) : []
        loadSocIntel()
    } catch (error) {
        loadError.value = error.response?.data?.message || 'Không thể tải dữ liệu an ninh doanh nghiệp.'
    } finally {
        loading.value = false
    }
}

async function loadSocIntel() {
    try {
        const intel = await socIntelApi.getIntelligence()
        Object.assign(socIntel, normalizeKeys(intel.data))
    } catch {}
}

const sevClass = (sev) => {
    switch ((sev || '').toLowerCase()) {
        case 'critical': return 'danger'
        case 'high': return 'danger'
        case 'medium': return 'warning'
        default: return 'success'
    }
}

const statusLabelMap = { Healthy: 'Hoạt động tốt', Blocked: 'Bị chặn', Degraded: 'Suy giảm', Unknown: 'Chưa rõ', Pass: 'Đạt', Fail: 'Không đạt', Warn: 'Cảnh báo' }
function configHealthLabel(value) { return statusLabelMap[value] || value }
function findingStatusLabel(value) { return statusLabelMap[value] || value }
const severityLabelMap = { Critical: 'Nghiêm trọng', High: 'Cao', Medium: 'Trung bình', Low: 'Thấp', Unknown: 'Không rõ' }
function severityLabel(value) { return severityLabelMap[value] || value }
const runStatusLabelMap = { Completed: 'Hoàn tất', Failed: 'Lỗi', Running: 'Đang chạy', Pending: 'Đang chờ', Delivered: 'Đã gửi', Started: 'Đã khởi chạy', Cancelled: 'Đã hủy' }
function outboxStatusLabel(value) { return runStatusLabelMap[value] || value }
function runStatusLabel(value) { return runStatusLabelMap[value] || value }
const policyStatusLabelMap = { Draft: 'Bản nháp', PendingApproval: 'Chờ phê duyệt', Approved: 'Đã phê duyệt', Active: 'Đang hiệu lực', Retired: 'Đã ngừng' }
function policyStatusLabel(value) { return policyStatusLabelMap[value] || value }
const policyDecisionLabelMap = { Allow: 'Cho phép', Deny: 'Từ chối' }
function policyDecisionLabel(value) { return policyDecisionLabelMap[value] || value }
const policyDecisionModeLabelMap = { Simulation: 'Mô phỏng', Evaluation: 'Đánh giá' }
function policyDecisionModeLabel(value) { return policyDecisionModeLabelMap[value] || value }

async function analyzeIncident() {
    if (!incidentBriefing.incidentId) return
    incidentBriefing.loading = true
    incidentBriefing.result = null
    try {
        const { data } = await enterpriseAiApi.analyzeIncident(incidentBriefing.incidentId)
        incidentBriefing.result = data
        pushActivity('AI phân tích sự cố', `Đã phân tích sự cố #${incidentBriefing.incidentId}`)
    } catch (error) {
        incidentBriefing.result = { severity: 'Medium', summary: 'Không thể phân tích: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null }
    } finally { incidentBriefing.loading = false }
}

async function analyzeEvidence() {
    if (!evidenceAnalysis.evidenceId) return
    evidenceAnalysis.loading = true
    evidenceAnalysis.result = null
    try {
        const { data } = await enterpriseAiApi.analyzeEvidence(evidenceAnalysis.evidenceId)
        evidenceAnalysis.result = data
        pushActivity('AI phân tích chứng cứ', `Đã phân tích chứng cứ #${evidenceAnalysis.evidenceId}`)
    } catch (error) {
        evidenceAnalysis.result = { severity: 'Medium', summary: 'Không thể phân tích: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null }
    } finally { evidenceAnalysis.loading = false }
}

async function reviewExport() {
    if (!evidenceExport.exportId) return
    evidenceExport.loading = true
    evidenceExport.result = null
    try {
        const { data } = await enterpriseAiApi.reviewExportRequest(evidenceExport.exportId)
        evidenceExport.result = data
        pushActivity('AI duyệt yêu cầu xuất', `Đã kiểm tra yêu cầu xuất #${evidenceExport.exportId}`)
    } catch (error) {
        evidenceExport.result = { severity: 'Medium', summary: 'Không thể kiểm tra: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null }
    } finally { evidenceExport.loading = false }
}

async function approveAi(id) { if (!id) return; try { await enterpriseAiApi.reviewRecommendation(id, 'Approved', 'Phê duyệt sau khi xem xét'); pushActivity('Khuyến nghị AI', `Đã phê duyệt #${id}`) } catch {} }
async function rejectAi(id) { if (!id) return; try { await enterpriseAiApi.reviewRecommendation(id, 'Rejected', 'Không đồng ý'); pushActivity('Khuyến nghị AI', `Đã từ chối #${id}`) } catch {} }

async function screenVisitor() {
    if (!visitorScreening.visitId) return
    visitorScreening.loading = true; visitorScreening.result = null
    try { const { data } = await enterpriseAiApi.screenVisitor(visitorScreening.visitId); visitorScreening.result = data; pushActivity('AI sàng lọc khách', `Đã sàng lọc lượt thăm #${visitorScreening.visitId}`) }
    catch (error) { visitorScreening.result = { severity: 'Medium', summary: 'Không thể phân tích: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null } }
    finally { visitorScreening.loading = false }
}

async function screenVehicle() {
    if (!vehicleScreening.vehicleId) return
    vehicleScreening.loading = true; vehicleScreening.result = null
    try { const { data } = await enterpriseAiApi.screenVehicle(vehicleScreening.vehicleId); vehicleScreening.result = data; pushActivity('AI sàng lọc phương tiện', `Đã sàng lọc phương tiện #${vehicleScreening.vehicleId}`) }
    catch (error) { vehicleScreening.result = { severity: 'Medium', summary: 'Không thể phân tích: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null } }
    finally { vehicleScreening.loading = false }
}

async function simulateAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true; policySimulation.result = null
    try { const { data } = await enterpriseAiApi.simulatePolicy(policySimulation.policyId); policySimulation.result = data; pushActivity('AI mô phỏng chính sách', `Đã mô phỏng chính sách #${policySimulation.policyId}`) }
    catch (error) { policySimulation.result = { severity: 'Low', summary: 'Không thể mô phỏng: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null } }
    finally { policySimulation.loading = false }
}

async function explainAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true; policySimulation.result = null
    try { const { data } = await enterpriseAiApi.explainPolicy(policySimulation.policyId); policySimulation.result = data; pushActivity('AI giải thích chính sách', `Đã giải thích chính sách #${policySimulation.policyId}`) }
    catch (error) { policySimulation.result = { severity: 'Low', summary: 'Không thể giải thích: ' + (error.response?.data?.message || error.message), provider: 'Lỗi', recommendationId: null } }
    finally { policySimulation.loading = false }
}

watch(selectedWorkspace, (ws) => {
    if (ws === 'soc') { loadSocIntel() }
    if (ws === 'ops') { loadBackupRuns(); loadRestoreDrills(); loadOutboxEvents() }
    if (ws === 'admin') { refreshPolicyGovernance() }
})

async function verifyStepUp() {
    busy.stepUp = true; stepUp.message = ''
    try {
        const start = await enterpriseApi.stepUpStart(stepUp.action, 'Xác minh tại bảng điều phối')
        const verified = await enterpriseApi.stepUpVerify(start.data.sessionId, stepUp.password, stepUp.mfaCode)
        stepUp.sessionId = verified.data.sessionId; stepUp.active = verified.data.active
        enterpriseApi.setStepUpSession(verified.data.sessionId)
        stepUp.message = 'Đã xác minh đến ' + formatDateTime(verified.data.expiresAtUtc)
        pushActivity('Đã xác minh tăng cường', stepUp.action)
    } catch (error) {
        stepUp.active = false; stepUp.message = error.response?.data?.message || 'Xác minh thất bại.'
    } finally { busy.stepUp = false }
}

async function loadStepUpStatus() {
    try {
        const response = await enterpriseApi.getStepUpStatus(stepUp.action, stepUp.sessionId || undefined)
        stepUp.sessionId = response.data?.sessionId || null
        stepUp.active = !!response.data?.active
        stepUp.message = stepUp.active && response.data?.expiresAtUtc
            ? 'Đã xác minh đến ' + formatDateTime(response.data.expiresAtUtc)
            : ''

        if (stepUp.sessionId) {
            enterpriseApi.setStepUpSession(stepUp.sessionId)
        }
    } catch {
        stepUp.active = false
        stepUp.message = ''
    }
}

async function saveProvider() { await runAction('provider', 'Đã lưu nhà cung cấp', () => enterpriseApi.upsertIdentityProvider(providerForm)) }
async function importUser() { const user = { ...importForm }; const pid = user.providerId; delete user.providerId; await runAction('importUser', 'Đã ghi nhận nhập người dùng', () => enterpriseApi.importIdentityUsers(pid, [user])) }
async function createVirtualController() { await runAction('device', 'Đã tạo bộ điều khiển mô phỏng', () => enterpriseApi.createVirtualController(deviceForm)) }
async function injectFault() { await runAction('fault', 'Đã chèn lỗi mô phỏng', () => enterpriseApi.injectSimulatorFault(faultForm)) }
async function createAlarm() { await runAction('alarm', 'Đã tạo cảnh báo', () => enterpriseApi.createAlarm({ alarmType: 'ManualDrill', severity: alarmForm.severity, summary: alarmForm.summary })) }
async function startBackup() { await runAction('backup', 'Đã khởi chạy sao lưu', () => enterpriseApi.startBackup({ profile: backupForm.profile, targetRpoMinutes: 15, targetRtoMinutes: 60, notes: 'Khởi chạy từ bảng điều phối doanh nghiệp' })) }
async function createQaRun() { await runAction('qa', 'Đã ghi nhận lượt QA', () => enterpriseApi.createQaRun({ testType: qaForm.testType, profile: 'MediumCompany', evidenceReference: '/qa/local-enterprise-console', notes: 'Ghi nhận từ bảng điều phối doanh nghiệp' })) }
async function backfillDefaultSite() { await runAction('backfill', 'Đã hoàn tất bổ sung nền tảng', () => enterpriseApi.backfillDefaultSite(backfillForm)) }

async function simulatePolicy() {
    await runAction('policy', 'Đã hoàn tất mô phỏng chính sách', async () => {
        const response = await enterpriseApi.simulateAccessPolicy({ ...policyForm, evaluatedAtUtc: policyForm.evaluatedAtUtc || new Date().toISOString() })
        policyResult.result = response.data?.result || ''; policyResult.reason = response.data?.reason || ''; policyResult.decisionMode = response.data?.decisionMode || ''
        return response
    })
}

function statusClass(status) {
    const map = {
        Draft: 'badge-gray',
        PendingApproval: 'badge-yellow',
        Approved: 'badge-blue',
        Active: 'badge-green',
        Retired: 'badge-gray',
    }
    return map[status] || 'badge-gray'
}

async function loadPolicyVersions() {
    policyGovernanceLoading.value = true
    try {
        const response = await enterpriseApi.getPolicyVersions()
        policyVersions.value = response.data || []
    } catch {
        policyVersions.value = []
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function loadPolicyRules() {
    policyGovernanceLoading.value = true
    try {
        const response = await enterpriseApi.getAccessRules()
        policyRules.value = response.data || []
    } catch {
        policyRules.value = []
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function refreshPolicyGovernance(activeTab = policyAdminTab.value) {
    if (activeTab === 'versions') {
        await loadPolicyVersions()
        return
    }

    if (activeTab === 'rules') {
        await loadPolicyRules()
        return
    }

    await Promise.all([loadPolicyVersions(), loadPolicyRules()])
}

async function createPolicyVersion() {
    if (!policyVersionForm.name) return
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.createPolicyVersion({
            name: policyVersionForm.name,
            changeSummary: policyVersionForm.changeSummary,
        })
        policyVersionForm.name = ''
        policyVersionForm.changeSummary = ''
        policyAdminMessage.value = 'Đã tạo phiên bản policy mới.'
        await loadPolicyVersions()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể tạo phiên bản policy.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function submitPolicyVersion(version) {
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.submitPolicyVersion(version.accessPolicyVersionId)
        policyAdminMessage.value = `Đã gửi duyệt phiên bản ${version.name}.`
        await loadPolicyVersions()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể gửi duyệt phiên bản.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function approvePolicyVersion(version) {
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.approvePolicyVersion(version.accessPolicyVersionId, { note: 'Approved from enterprise console' })
        policyAdminMessage.value = `Đã phê duyệt phiên bản ${version.name}.`
        await loadPolicyVersions()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể phê duyệt phiên bản.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function activatePolicyVersion(version) {
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.activatePolicyVersion(version.accessPolicyVersionId)
        policyAdminMessage.value = `Đã kích hoạt phiên bản ${version.name}.`
        await loadPolicyVersions()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể kích hoạt phiên bản.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function retirePolicyVersion(version) {
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.retirePolicyVersion(version.accessPolicyVersionId)
        policyAdminMessage.value = `Đã ngừng hiệu lực phiên bản ${version.name}.`
        await loadPolicyVersions()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể ngừng hiệu lực phiên bản.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

async function createPolicyRule() {
    if (!policyRuleForm.accessLevelId) return
    policyGovernanceLoading.value = true
    policyAdminMessage.value = ''
    try {
        await enterpriseApi.createAccessRule({ ...policyRuleForm })
        policyRuleForm.subjectId = null
        policyRuleForm.accessLevelId = null
        policyRuleForm.accessGroupId = null
        policyRuleForm.siteId = null
        policyRuleForm.securityZoneId = null
        policyRuleForm.accessPointId = null
        policyRuleForm.accessScheduleId = null
        policyRuleForm.allowAccess = true
        policyAdminMessage.value = 'Đã tạo luật truy cập mới.'
        await loadPolicyRules()
        await loadOverview()
    } catch (error) {
        policyAdminMessage.value = error.response?.data?.message || 'Không thể tạo luật truy cập.'
    } finally {
        policyGovernanceLoading.value = false
    }
}

// --- Ops workspace actions ---
async function startRestore() {
    if (!restoreForm.backupRunId) return
    busy.restore = true; restoreResult.value = ''
    try {
        await enterpriseApi.startRestore({ backupRunId: restoreForm.backupRunId, targetRtoMinutes: restoreForm.targetRtoMinutes })
        restoreResult.value = 'Đã bắt đầu diễn tập khôi phục.'
        restoreForm.backupRunId = null
        await loadRestoreDrills()
    } catch (e) { restoreResult.value = 'Thất bại: ' + (e.response?.data?.message || e.message) }
    finally { busy.restore = false }
}

async function recordSecurityCheck() {
    busy.security = true; securityResult.value = ''
    try {
        await enterpriseApi.recordSecurityCheck({
            checkType: securityForm.checkType,
            status: securityForm.status,
            notes: securityForm.notes || null,
        })
        securityResult.value = 'Đã ghi nhận lượt kiểm tra an ninh.'
        securityForm.notes = ''
    } catch (e) { securityResult.value = 'Thất bại: ' + (e.response?.data?.message || e.message) }
    finally { busy.security = false }
}

async function loadOutboxEvents() {
    outboxLoading.value = true; outboxEvents.value = []
    try {
        const params = { pageSize: 20 }
        if (outboxFilter.value) params.status = outboxFilter.value
        const res = await enterpriseApi.getOutboxEvents(params)
        outboxEvents.value = res.data?.items || []
    } catch { outboxEvents.value = [] }
    finally { outboxLoading.value = false }
}

async function loadBackupRuns() {
    backupLoading.value = true; backupRuns.value = []
    try {
        const res = await enterpriseApi.getBackupRuns({ pageSize: 10 })
        backupRuns.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { backupRuns.value = [] }
    finally { backupLoading.value = false }
}

async function loadRestoreDrills() {
    restoreLoading.value = true; restoreDrills.value = []
    try {
        const res = await enterpriseApi.getRestoreDrills({ pageSize: 10 })
        restoreDrills.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { restoreDrills.value = [] }
    finally { restoreLoading.value = false }
}

async function runAction(key, title, action) {
    busy[key] = true
    try {
        const response = await action()
        pushActivity(title, response.data?.message || JSON.stringify(response.data).slice(0, 140))
        await loadOverview()
    } catch (error) { pushActivity(title + ' thất bại', error.response?.data?.message || error.message) }
    finally { busy[key] = false }
}

function pushActivity(title, detail) {
    activityLog.value.unshift({ id: `${Date.now()}-${Math.random()}`, title, detail, time: new Date().toLocaleTimeString() })
    activityLog.value = activityLog.value.slice(0, 8)
}

function normalizeKeys(data) {
    return Object.fromEntries(Object.entries(data || {}).map(([key, value]) => [key.charAt(0).toLowerCase() + key.slice(1), value]))
}

function formatDateTime(value) { if (!value) return ''; return new Date(value).toLocaleString() }

onMounted(async () => {
    await loadOverview()
    await loadStepUpStatus()
    if (isAdmin.value) {
        await refreshPolicyGovernance()
    }
})
</script>

<style scoped>
.enterprise-page { display: flex; flex-direction: column; gap: 22px; }
.readiness-band { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; align-items: center; gap: 24px; padding: 22px; border-radius: 18px; border: 1px solid var(--border-soft); background: linear-gradient(135deg, rgba(18, 75, 91, 0.92), rgba(18, 36, 52, 0.96)); color: #f7fcff; box-shadow: var(--shadow-md); }
.readiness-score { width: 112px; height: 112px; border-radius: 999px; display: grid; place-content: center; text-align: center; border: 1px solid rgba(255, 255, 255, 0.24); background: rgba(255, 255, 255, 0.08); }
.readiness-score span { font-size: 0.74rem; text-transform: uppercase; color: rgba(247, 252, 255, 0.72); }
.readiness-score strong { font-size: 2rem; line-height: 1; }
.readiness-copy h2 { margin: 0 0 8px; font-size: 1.35rem; }
.readiness-copy p { margin: 0; color: rgba(247, 252, 255, 0.76); }
.status-pill { display: inline-flex; align-items: center; min-height: 36px; padding: 0 14px; border-radius: 999px; background: rgba(77, 216, 180, 0.16); color: #bbffe8; font-weight: 700; }
.status-pill.danger { background: rgba(236, 91, 91, 0.18); color: #ffd0d0; }
.workspace-tabs { display: flex; flex-wrap: wrap; gap: 10px; }
.workspace-tabs button { min-height: 40px; padding: 0 16px; border-radius: 999px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-secondary); font-weight: 700; transition: transform var(--transition-fast), box-shadow var(--transition-fast), border-color var(--transition-fast), background var(--transition-fast); }
.workspace-tabs button:hover { transform: translateY(-1px); border-color: var(--border-strong); }
.workspace-tabs button.active { color: #05313b; background: #8ceaf4; border-color: #8ceaf4; }
.workspace-summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
.workspace-stat { min-height: 86px; padding: 14px; border-radius: 14px; border: 1px solid var(--border-soft); background: var(--surface-muted); }
.workspace-stat strong { display: block; font-size: 1.45rem; color: var(--text-primary); }
.workspace-stat span { color: var(--text-secondary); font-size: 0.86rem; }
.action-strip { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 18px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.form-grid.stacked { margin-top: 18px; }
.form-grid.single { grid-template-columns: 1fr; }
.checkbox-row { display: flex !important; flex-direction: row !important; align-items: center; gap: 8px; }
.checkbox-row input { width: 18px; min-height: 18px; }
.form-grid label { display: flex; flex-direction: column; gap: 7px; color: var(--text-secondary); font-size: 0.82rem; font-weight: 700; }
.form-grid input, .form-grid select, .form-grid textarea { width: 100%; min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.form-grid textarea { padding: 8px 12px; min-height: 60px; }
.form-grid button { align-self: end; }
.inline-message { margin: 12px 0 0; color: var(--text-secondary); }
.policy-admin-panel { display: flex; flex-direction: column; gap: 14px; }
.policy-tabs { gap: 8px; }
.policy-tabs button { min-height: 36px; padding: 0 14px; }
.policy-admin-body { display: flex; flex-direction: column; gap: 14px; }
.policy-result-card { padding: 14px; border-radius: 14px; background: var(--surface-muted); border: 1px solid var(--border-soft); }
.version-list, .rule-list { display: flex; flex-direction: column; gap: 8px; }
.version-row, .rule-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 12px 14px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface-muted); }
.version-info, .rule-info { display: flex; flex-direction: column; gap: 4px; flex: 1; }
.version-meta, .rule-detail { font-size: 0.8rem; color: var(--text-muted); }
.version-badges { display: flex; flex-wrap: wrap; align-items: center; justify-content: flex-end; gap: 6px; }
.rule-scope { display: flex; flex-wrap: wrap; gap: 6px; font-size: 0.78rem; color: var(--text-secondary); }
.badge { font-size: 0.72rem; padding: 2px 8px; border-radius: 12px; font-weight: 700; }
.badge-green { background: var(--status-success-bg); color: var(--status-success-text); }
.badge-gray { background: var(--status-neutral-bg); color: var(--status-neutral-text); }
.badge-yellow { background: var(--status-warning-bg); color: var(--status-warning-text); }
.badge-blue { background: var(--status-info-bg); color: var(--status-info-text); }
.btn-xs { min-height: 30px; padding: 0 10px; font-size: 0.75rem; border-radius: 9px; }
.btn-success { background: #16a34a; color: #fff; }
.finding-list { display: grid; gap: 8px; }
.finding-row, .asset-map-summary { display: flex; align-items: center; justify-content: space-between; gap: 10px; min-height: 38px; padding: 8px 0; border-top: 1px solid var(--border-soft); }
.finding-row:first-child { border-top: none; }
.finding-row strong { min-width: 0; color: var(--text-primary); font-size: 0.88rem; overflow-wrap: anywhere; }
.finding-row span, .asset-map-summary span { flex: 0 0 auto; color: var(--text-secondary); font-size: 0.82rem; font-weight: 700; }
.finding-row span.fail { color: #d44747; }
.finding-row span.warn { color: #b7791f; }
.asset-map-summary { margin-top: 14px; justify-content: flex-start; flex-wrap: wrap; }
.audit-panel { width: 100%; }
.activity-list { display: grid; gap: 10px; }
.activity-row { display: grid; grid-template-columns: 90px 190px minmax(0, 1fr); gap: 12px; align-items: center; padding: 12px 0; border-top: 1px solid var(--border-soft); }
.activity-row:first-child { border-top: none; }
.activity-row span { color: var(--text-muted); font-size: 0.82rem; }
.activity-row strong { color: var(--text-primary); }
.activity-row p { margin: 0; color: var(--text-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.soc-intel-summary { margin-top: 14px; padding: 12px; border-radius: 12px; background: var(--surface-muted); border: 1px solid var(--border-soft); display: flex; flex-direction: column; gap: 8px; }
.soc-intel-summary p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); }
.soc-stats-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-top: 14px; }
.soc-stat { padding: 14px; border-radius: 12px; background: var(--surface-muted); border: 1px solid var(--border-soft); }
.soc-stat strong { display: block; font-size: 1.5rem; color: var(--text-primary); }
.soc-stat span { font-size: 0.82rem; color: var(--text-secondary); }
.soc-change { font-size: 0.8rem; font-weight: 700; }
.soc-change.up { color: #d44747; }
.soc-change.down { color: #4db480; }
.text-danger { color: #d44747; }
.soc-severity-breakdown { margin-top: 16px; }
.soc-severity-breakdown h4 { margin: 0 0 8px; font-size: 0.82rem; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.04em; }
.severity-bar-row { display: grid; grid-template-columns: 64px minmax(0, 1fr) 32px; align-items: center; gap: 8px; margin-bottom: 6px; font-size: 0.82rem; color: var(--text-secondary); }
.severity-bar-track { height: 18px; border-radius: 999px; background: var(--surface); overflow: hidden; }
.severity-bar-fill { height: 100%; border-radius: 999px; transition: width 0.6s ease; }
.severity-bar-fill.sev-critical { background: #d44747; }
.severity-bar-fill.sev-high { background: #d49b47; }
.severity-bar-fill.sev-medium { background: #47a3d4; }
.severity-bar-fill.sev-low { background: #74b47a; }
.anomaly-list { display: grid; gap: 8px; margin-top: 10px; }
.anomaly-item { padding: 12px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface-muted); }
.anomaly-item.sev-critical { border-left: 3px solid #d44747; }
.anomaly-item.sev-high { border-left: 3px solid #d49b47; }
.anomaly-item strong { display: block; font-size: 0.85rem; color: var(--text-primary); text-transform: capitalize; margin-bottom: 4px; }
.anomaly-item p { margin: 0 0 6px; font-size: 0.82rem; color: var(--text-secondary); }
.anomaly-metric { display: flex; gap: 10px; font-size: 0.78rem; color: var(--text-muted); }
.incident-brief-form { display: flex; gap: 8px; align-items: center; margin-top: 14px; }
.incident-brief-form input, .filter-input { width: 100%; min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.filter-select { min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.ai-brief-result { margin-top: 14px; padding: 14px; border-radius: 14px; background: var(--surface-muted); border: 1px solid var(--border-soft); display: flex; flex-direction: column; gap: 10px; }
.ai-brief-result .brief-summary { margin: 0; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.6; }
.rec-header { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.rec-header small { color: var(--text-muted); font-size: 0.74rem; }
.rec-reasoning { padding: 10px; border-radius: 10px; background: rgba(24, 49, 77, 0.04); }
.rec-reasoning strong { display: block; font-size: 0.78rem; color: var(--text-secondary); margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.04em; }
.rec-reasoning p { margin: 0; font-size: 0.82rem; color: var(--text-secondary); white-space: pre-wrap; }
.rec-actions { display: flex; gap: 8px; }
.success-card { padding: 10px; border-radius: 8px; background: var(--status-success-bg); color: var(--status-success-text); font-size: 0.85rem; }
.empty-card { padding: 40px; text-align: center; color: var(--text-muted); border: 1px dashed var(--border-soft); border-radius: 12px; }
@media (max-width: 900px) {
    .readiness-band { grid-template-columns: 1fr; }
    .readiness-score { width: 92px; height: 92px; }
    .workspace-summary, .form-grid, .activity-row { grid-template-columns: 1fr; }
}
</style>
