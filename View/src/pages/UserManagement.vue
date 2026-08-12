<template>
    <div class="page-container animate-in">
        <!-- Minimalist Header -->
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Quản lý tài khoản</h1>
                <p class="page-subtitle">Thêm, sửa, xóa và phân quyền tài khoản người dùng hệ thống</p>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showImportModal = true">Nhập dữ liệu</button>
                <button class="btn btn-secondary" @click="showExportModal = true">Xuất dữ liệu</button>
                <button class="btn btn-primary" @click="openCreateModal">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px;">
                        <line x1="12" y1="5" x2="12" y2="19" />
                        <line x1="5" y1="12" x2="19" y2="12" />
                    </svg>
                    Thêm tài khoản
                </button>
            </div>
        </header>

        <!-- Stats Overview Row -->
        <div class="bento-grid-mini" style="grid-template-columns: repeat(3, 1fr);">
            <div class="bento-card stat-card">
                <div class="stat-icon-wrapper blue">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>
                </div>
                <div class="stat-details">
                    <div class="stat-val blue">{{ users.length }}</div>
                    <div class="stat-lbl">Tổng tài khoản</div>
                </div>
            </div>
            <div class="bento-card stat-card">
                <div class="stat-icon-wrapper green">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
                </div>
                <div class="stat-details">
                    <div class="stat-val green">{{ activeCount }}</div>
                    <div class="stat-lbl">Đang hoạt động</div>
                </div>
            </div>
            <div class="bento-card stat-card">
                <div class="stat-icon-wrapper red">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>
                </div>
                <div class="stat-details">
                    <div class="stat-val red">{{ inactiveCount }}</div>
                    <div class="stat-lbl">Đã vô hiệu hóa</div>
                </div>
            </div>
        </div>

        <!-- Main Content Box -->
        <div class="bento-card table-section">
            <div class="table-toolbar">
                <div class="search-box">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" /></svg>
                    <input type="text" v-model="searchQuery" placeholder="Tìm kiếm tài khoản..." />
                </div>
                <div class="filter-box" style="display: flex; gap: 12px;">
                    <select class="minimal-select" v-model="filterRole">
                        <option value="">Tất cả vai trò</option>
                        <option value="Admin">Admin</option>
                        <option value="QuanLy">Quản lý</option>
                        <option value="LeTan">Lễ tân</option>
                        <option value="BaoVe">Bảo vệ</option>
                        <option value="NhanSu">Nhân sự</option>
                        <option value="NhanVien">Nhân viên</option>
                    </select>
                    <select class="minimal-select" v-model="filterStatus">
                        <option value="">Tất cả trạng thái</option>
                        <option value="active">Hoạt động</option>
                        <option value="inactive">Vô hiệu hóa</option>
                    </select>
                </div>
            </div>

            <!-- States -->
            <div v-if="loading" class="empty-layout">
                <div class="spinner-lg"></div>
                <p>Đang tải dữ liệu...</p>
            </div>
            <div v-else-if="loadError" class="empty-layout">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" style="width: 48px; height: 48px; color: var(--accent-danger);"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                <p style="color: var(--accent-danger);">{{ loadError }}</p>
                <button class="btn btn-primary" @click="fetchUsers">Thử lại</button>
            </div>
            
            <!-- Sleek Table -->
            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead>
                        <tr>
                            <th style="width: 80px;">ID</th>
                            <th>Tài khoản</th>
                            <th>Họ và tên</th>
                            <th>Vai trò</th>
                            <th>Trạng thái</th>
                            <th>Ngày tạo</th>
                            <th class="text-right">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="user in filteredUsers" :key="user.userId" class="table-row">
                            <td class="text-muted" style="font-family: monospace;">#{{ user.userId }}</td>
                            <td>
                                <div class="user-cell">
                                    <div class="avatar" :style="{ background: getAvatarColor(getInitials(user.fullName || user.username)) }">{{ getInitials(user.fullName || user.username) }}</div>
                                    <div class="user-info">
                                        <span class="user-name">{{ user.username }}</span>
                                    </div>
                                </div>
                            </td>
                            <td><span class="text-primary" style="font-weight: 500;">{{ user.fullName || '-' }}</span></td>
                            <td>
                                <span class="badge-role" :class="getRoleBadgeClass(user.role)">
                                    {{ getRoleLabel(user.role) }}
                                </span>
                            </td>
                            <td>
                                <span class="status-pill minimal" :class="user.isActive ? 'active' : 'inactive'">
                                    <span class="pill-dot"></span>
                                    {{ user.isActive ? 'Hoạt động' : 'Vô hiệu hóa' }}
                                </span>
                            </td>
                            <td class="text-muted" style="font-size: 0.85rem;">{{ formatDate(user.createdAt) }}</td>
                            <td class="text-right">
                                <div class="action-menu">
                                    <button class="icon-btn" title="Phân quyền thao tác" @click="openScopeModal(user)">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 3l7 4v5c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V7l7-4z"/><path d="M9 12l2 2 4-4"/></svg>
                                    </button>
                                    <button class="icon-btn" title="Chỉnh sửa" @click="openEditModal(user)">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" /><path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" /></svg>
                                    </button>
                                    <button class="icon-btn" title="Đặt lại MFA" @click="handleResetMfa(user)" :disabled="!user.mfaEnabled">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v4"/><path d="M12 18v4"/><path d="M4.93 4.93l2.83 2.83"/><path d="M16.24 16.24l2.83 2.83"/><path d="M2 12h4"/><path d="M18 12h4"/><path d="M4.93 19.07l2.83-2.83"/><path d="M16.24 7.76l2.83-2.83"/><circle cx="12" cy="12" r="4"/></svg>
                                    </button>
                                    <button class="icon-btn action-reject" title="Xóa" @click="confirmDelete(user)">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6" /><path d="M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" /><line x1="10" y1="11" x2="10" y2="17" /><line x1="14" y1="11" x2="14" y2="17" /></svg>
                                    </button>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="filteredUsers.length === 0">
                            <td colspan="7" class="empty-layout">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" style="width: 48px; height: 48px; color: var(--text-muted);"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>
                                <p>Không tìm thấy tài khoản nào khớp với bộ lọc</p>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Modern Modal for Create/Edit -->
        <transition name="modal">
            <div v-if="showModal" class="modal-backdrop" @click.self="closeModal">
                <div class="modern-modal" style="max-width: 500px;">
                    <div class="modal-top">
                        <h3>{{ isEditing ? 'Cập nhật tài khoản' : 'Thêm tài khoản mới' }}</h3>
                        <button class="icon-close" @click="closeModal"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
                    </div>

                    <div class="modal-body">
                        <form @submit.prevent="handleSubmit" class="modal-form-grid">
                            <div class="input-pane" v-if="!isEditing" :class="{ 'has-error': fieldErrors.username }">
                                <label>Tên đăng nhập <span class="req">*</span></label>
                                <input v-model="modalForm.username" type="text" class="sleek-input" :class="{ 'input-error': fieldErrors.username }" placeholder="Nhập tên đăng nhập" required maxlength="50" @input="fieldErrors.username = ''" />
                                <p v-if="fieldErrors.username" class="field-error" role="alert">{{ fieldErrors.username }}</p>
                            </div>

                            <div class="input-pane" :class="{ 'has-error': fieldErrors.password }">
                                <label>{{ isEditing ? 'Mật khẩu mới' : 'Mật khẩu' }} <span v-if="!isEditing" class="req">*</span></label>
                                <input v-model="modalForm.password" type="password" class="sleek-input" :class="{ 'input-error': fieldErrors.password }" :placeholder="isEditing ? 'Để trống nếu không đổi' : 'Tối thiểu 6 ký tự'" :required="!isEditing" minlength="6" @input="fieldErrors.password = ''" />
                                <p v-if="fieldErrors.password" class="field-error" role="alert">{{ fieldErrors.password }}</p>
                            </div>

                            <div class="input-pane employee-search-pane" :class="{ 'has-error': fieldErrors.fullName }">
                                <label>Họ và tên</label>
                                <div class="combo-box-wrapper" ref="comboBoxRef">
                                    <div class="combo-input-row">
                                        <svg class="combo-search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" /></svg>
                                        <input
                                            v-model="employeeSearchText"
                                            type="text"
                                            class="sleek-input combo-input"
                                            :class="{ 'input-error': fieldErrors.fullName }"
                                            placeholder="Tìm và chọn nhân viên..."
                                            @focus="showEmployeeDropdown = true"
                                            @input="onEmployeeSearchInput"
                                            autocomplete="off"
                                        />
                                        <button v-if="modalForm.fullName" type="button" class="combo-clear-btn" @click="clearEmployeeSelection" title="Xóa">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                                        </button>
                                    </div>
                                    <p v-if="fieldErrors.fullName" class="field-error" role="alert">{{ fieldErrors.fullName }}</p>
                                    <transition name="dropdown">
                                        <div v-if="showEmployeeDropdown" class="combo-dropdown">
                                            <div v-if="loadingEmployees" class="combo-loading">
                                                <span class="spinner-sm"></span> Đang tải...
                                            </div>
                                            <template v-else>
                                                <div v-if="filteredEmployees.length === 0" class="combo-empty">
                                                    Không tìm thấy nhân viên
                                                </div>
                                                <div
                                                    v-for="emp in filteredEmployees"
                                                    :key="emp.employeeId"
                                                    class="combo-option"
                                                    :class="{ selected: modalForm.fullName === emp.fullName }"
                                                    @mousedown.prevent="selectEmployee(emp)"
                                                >
                                                    <div class="combo-opt-avatar" :style="{ background: getAvatarColor(getInitials(emp.fullName)) }">{{ getInitials(emp.fullName) }}</div>
                                                    <div class="combo-opt-info">
                                                        <span class="combo-opt-name">{{ emp.fullName }}</span>
                                                        <span class="combo-opt-detail">{{ emp.departmentName || 'Chưa xếp phòng' }} - {{ emp.phone || 'N/A' }}</span>
                                                    </div>
                                                </div>
                                            </template>
                                        </div>
                                    </transition>
                                </div>
                            </div>

                            <div class="grid-2">
                                <div class="input-pane">
                                    <label>Vai trò <span class="req">*</span></label>
                                    <select v-model="modalForm.role" class="sleek-select" required>
                                        <option value="Admin">Admin</option>
                                        <option value="QuanLy">Quản lý</option>
                                        <option value="LeTan">Lễ tân</option>
                                        <option value="BaoVe">Bảo vệ</option>
                                        <option value="NhanSu">Nhân sự</option>
                                        <option value="NhanVien">Nhân viên</option>
                                    </select>
                                </div>
                                <div class="input-pane" v-if="isEditing">
                                    <label>Trạng thái</label>
                                    <select v-model="modalForm.isActive" class="sleek-select">
                                        <option :value="true">Đang hoạt động</option>
                                        <option :value="false">Vô hiệu hóa</option>
                                    </select>
                                </div>
                            </div>

                            <!-- Form Error -->
                            <div v-if="modalError" class="error-box">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                                <span>{{ modalError }}</span>
                            </div>

                            <div class="modal-actions mt-4">
                                <button type="button" class="btn btn-secondary" @click="closeModal">Hủy</button>
                                <button type="submit" class="btn btn-primary" :disabled="saving">
                                    <span v-if="saving" class="spinner-sm"></span>
                                    {{ isEditing ? 'Lưu cập nhật' : 'Khởi tạo' }}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </transition>

        <!-- Modern Warning Modal for Delete -->
        <transition name="modal">
            <div v-if="showDeleteModal" class="modal-backdrop" @click.self="showDeleteModal = false">
                <div class="modern-modal mini">
                    <div class="modal-body text-center" style="padding: 32px 24px;">
                        <div class="warning-icon"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg></div>
                        <h3 style="margin: 0 0 10px 0;">Xóa tài khoản này?</h3>
                        <p style="color: var(--text-secondary); font-size: 0.95rem; margin-bottom: 24px;">
                            Tài khoản <strong style="color: var(--text-primary);">{{ deleteTarget?.username }}</strong> sẽ bị xóa vĩnh viễn khỏi hệ thống.
                        </p>
                        
                        <div v-if="modalError" class="error-box text-left" style="margin-bottom: 20px;">
                            <span>{{ modalError }}</span>
                        </div>

                        <div class="modal-actions centered">
                            <button class="btn btn-secondary" @click="showDeleteModal = false">Hủy</button>
                            <button class="btn btn-danger" @click="handleDelete" :disabled="saving">
                                <span v-if="saving" class="spinner-sm"></span> Xóa
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </transition>

        <transition name="modal">
            <div v-if="showScopeModal" class="modal-backdrop" @click.self="closeScopeModal">
                <div class="modern-modal scope-modal-shell">
                    <div class="modal-top">
                        <h3>Phân quyền thao tác: {{ scopeTarget?.fullName || scopeTarget?.username }}</h3>
                        <button class="icon-close" @click="closeScopeModal"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
                    </div>
                    <div class="modal-body scope-modal-body">
                        <p class="scope-note">
                            Vai trò là quyền gốc của tài khoản. Bạn có thể mở rộng, chặn riêng từng chức năng,
                            hoặc giới hạn theo site, cổng, làn, khu vực ở phần bên dưới.
                        </p>
                        <div v-if="scopeError" class="error-box"><span>{{ scopeError }}</span></div>
                        <div v-if="scopeLoading" class="empty-layout" style="padding: 28px;">
                            <div class="spinner-lg"></div>
                            <p>Đang tải thiết lập quyền...</p>
                        </div>
                        <template v-else>
                            <div class="scope-toolbar">
                                <div class="scope-summary">
                                    <span class="badge-role" :class="getRoleBadgeClass(scopeTarget?.role)">{{ getRoleLabel(scopeTarget?.role) }}</span>
                                    <span class="text-muted">{{ permissionOverrides.length }} chức năng</span>
                                    <span class="text-muted">{{ scopeItems.length }} dòng giới hạn</span>
                                    <span class="text-muted">{{ gateAccessItems.length }} cổng</span>
                                </div>
                            </div>

                            <div class="bento-tabs scope-tabs" style="display: flex; gap: 4px; background: var(--bg-surface); padding: 4px; border-radius: 14px; margin-bottom: 18px; max-width: 480px;">
                                <button type="button" class="tab-btn" :class="{ active: scopeActiveTab === 'tasks' }" @click="scopeActiveTab = 'tasks'">Chức năng</button>
                                <button type="button" class="tab-btn" :class="{ active: scopeActiveTab === 'gates' }" @click="scopeActiveTab = 'gates'">Cổng</button>
                            </div>

                            <div v-if="scopeActiveTab === 'tasks'">
                                <div class="scope-block scope-card-block">
                                    <div class="scope-block-head">
                                        <div>
                                            <h4 class="scope-block-title">Quyền theo từng chức năng</h4>
                                            <p class="scope-block-subtitle">Mỗi thẻ là một nhóm trang. Chọn cách áp dụng cho riêng tài khoản này.</p>
                                        </div>
                                    </div>
                                <div class="permission-card-grid">
                                    <article v-for="item in permissionOverrides" :key="item.taskKey" class="permission-card">
                                        <div class="permission-card-top">
                                            <div>
                                                <h5 class="permission-card-title">{{ item.label }}</h5>
                                                <p class="permission-card-routes">{{ item.routes.join(', ') || 'Không có route' }}</p>
                                            </div>
                                            <span class="status-pill minimal" :class="item.defaultAllowed ? 'active' : 'inactive'">
                                                <span class="pill-dot"></span>
                                                {{ item.defaultAllowed ? 'Mặc định: được vào' : 'Mặc định: bị ẩn' }}
                                            </span>
                                        </div>
                                        <div class="mode-switch">
                                            <button
                                                type="button"
                                                class="mode-option"
                                                :class="{ active: item.accessMode === 'inherit' }"
                                                @click="item.accessMode = 'inherit'"
                                            >
                                                Theo vai trò
                                            </button>
                                            <button
                                                type="button"
                                                class="mode-option allow"
                                                :class="{ active: item.accessMode === 'allow' }"
                                                @click="item.accessMode = 'allow'"
                                            >
                                                Cho phép thêm
                                            </button>
                                            <button
                                                type="button"
                                                class="mode-option deny"
                                                :class="{ active: item.accessMode === 'deny' }"
                                                @click="item.accessMode = 'deny'"
                                            >
                                                Chặn riêng
                                            </button>
                                        </div>
                                        <p class="permission-card-caption">{{ describeAccessMode(item.accessMode) }}</p>
                                    </article>
                                </div>
                            </div>

                            <div class="scope-block scope-card-block">
                                <div class="scope-block-head">
                                    <div>
                                        <h4 class="scope-block-title">Giới hạn chi tiết</h4>
                                        <p class="scope-block-subtitle">Chỉ dùng khi cần khóa phạm vi theo địa điểm cụ thể. Mỗi dòng là một luật riêng.</p>
                                    </div>
                                    <button class="btn btn-secondary" @click="addScopeRow">Thêm dòng</button>
                                </div>
                                <div v-if="scopeItems.length === 0" class="scope-empty">
                                    Chưa có dòng giới hạn nào. Nếu không cần giới hạn theo site/cổng/làn/khu vực thì có thể để trống.
                                </div>
                                <div v-else class="scope-list">
                                    <article v-for="(scope, index) in scopeItems" :key="scope.localId" class="scope-entry">
                                        <div class="scope-entry-head">
                                            <strong>Dòng {{ index + 1 }}</strong>
                                            <button class="icon-btn action-reject" @click="removeScopeRow(index)">
                                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                                            </button>
                                        </div>
                                        <div class="scope-entry-grid">
                                            <label class="scope-field">
                                                <span>Chức năng</span>
                                                <select v-model="scope.taskKey" class="sleek-select">
                                                    <option value="">Chọn chức năng</option>
                                                    <option v-for="task in scopeTaskOptions" :key="task.value" :value="task.value">{{ task.label }}</option>
                                                </select>
                                            </label>
                                            <label class="scope-field">
                                                <span>Khu vực</span>
                                                <select v-model="scope.siteId" class="sleek-select">
                                                    <option value="">Tất cả</option>
                                                    <option v-for="site in scopeReference.sites" :key="site.siteId" :value="String(site.siteId)">{{ site.name }}</option>
                                                </select>
                                            </label>
                                            <label class="scope-field">
                                                <span>Cổng</span>
                                                <select v-model="scope.gateId" class="sleek-select">
                                                    <option value="">Tất cả</option>
                                                    <option v-for="gate in scopeReference.gates" :key="gate.gateId" :value="String(gate.gateId)">{{ gate.name }}</option>
                                                </select>
                                            </label>
                                            <label class="scope-field">
                                                <span>Làn</span>
                                                <select v-model="scope.laneId" class="sleek-select">
                                                    <option value="">Tất cả</option>
                                                    <option v-for="lane in scopeReference.lanes" :key="lane.laneId" :value="String(lane.laneId)">{{ lane.name }}</option>
                                                </select>
                                            </label>
                                            <label class="scope-field">
                                                <span>Khu vực</span>
                                                <select v-model="scope.securityZoneId" class="sleek-select">
                                                    <option value="">Tất cả</option>
                                                    <option v-for="zone in scopeReference.zones" :key="zone.securityZoneId" :value="String(zone.securityZoneId)">{{ zone.name }}</option>
                                                </select>
                                            </label>
                                            <label class="scope-field scope-field-wide">
                                                <span>Ghi chú</span>
                                                <input v-model="scope.note" type="text" class="sleek-input" placeholder="Ví dụ: chỉ được xem dữ liệu site A" />
                                            </label>
                                        </div>
                                        <div class="scope-permission-row">
                                            <label class="scope-check">
                                                <input v-model="scope.canView" type="checkbox" />
                                                <span>Cho xem</span>
                                            </label>
                                            <label class="scope-check">
                                                <input v-model="scope.canManage" type="checkbox" />
                                                <span>Cho xử lý</span>
                                            </label>
                                        </div>
                                    </article>
                                </div>
                            </div>
                            </div>

                            <div v-else>
                                <div class="scope-block scope-card-block">
                                    <div class="scope-block-head">
                                        <div>
                                            <h4 class="scope-block-title">Quyền qua cổng riêng</h4>
                                            <p class="scope-block-subtitle">Chọn quyền qua từng cổng truy cập cho riêng tài khoản này. Mặc định tài khoản sẽ kế thừa quyền theo vai trò của mình.</p>
                                        </div>
                                    </div>
                                    <div v-if="gateAccessError" class="error-box"><span>{{ gateAccessError }}</span></div>
                                    <div v-else-if="gateAccessItems.length === 0" class="scope-empty">
                                        Chưa có cổng nào trong hệ thống.
                                    </div>
                                    <div v-else class="permission-card-grid">
                                        <article v-for="gate in gateAccessItems" :key="gate.gateId" class="permission-card">
                                            <div class="permission-card-top">
                                                <div>
                                                    <h5 class="permission-card-title">{{ gate.gateName }}</h5>
                                                    <p class="permission-card-routes">{{ gate.location || 'Chưa ghi vị trí' }}</p>
                                                </div>
                                                <span class="status-pill minimal" :class="gate.effectiveAllowed ? 'active' : 'inactive'">
                                                    <span class="pill-dot"></span>
                                                    {{ gate.effectiveAllowed ? 'Được qua' : 'Không qua' }}
                                                </span>
                                            </div>
                                            <div v-if="isAdminUser" class="zone-lock-note">
                                                Tài khoản Admin luôn được qua mọi cổng.
                                            </div>
                                            <template v-else>
                                                <div class="mode-switch">
                                                    <button type="button" class="mode-option" :class="{ active: gate.accessMode === 'inherit' }" @click="setGateMode(gate, 'inherit')">Theo vai trò</button>
                                                    <button type="button" class="mode-option allow" :class="{ active: gate.accessMode === 'allow' }" @click="setGateMode(gate, 'allow')">Cho phép thêm</button>
                                                    <button type="button" class="mode-option deny" :class="{ active: gate.accessMode === 'deny' }" @click="setGateMode(gate, 'deny')">Chặn riêng</button>
                                                </div>
                                                <p class="permission-card-caption">{{ describeGateMode(gate) }}</p>
                                            </template>
                                        </article>
                                    </div>
                                </div>
                            </div>

                            <div class="modal-actions mt-4">
                                <button class="btn btn-secondary" @click="closeScopeModal">Đóng</button>
                                <button class="btn btn-primary" :disabled="scopeSaving" @click="saveScopes">
                                    <span v-if="scopeSaving" class="spinner-sm"></span>
                                    Lưu thiết lập
                                </button>
                            </div>
                        </template>
                    </div>
                </div>
            </div>
        </transition>

        <ImportModal v-if="showImportModal" entity-type="AppUser" entity-display-name="Tài khoản người dùng" @close="showImportModal = false" @import-complete="onImportComplete" />
        <ExportModal v-if="showExportModal" entity-type="AppUser" entity-display-name="Tài khoản người dùng" :available-columns="['UserId','Username','FullName','Role','IsActive','EmployeeEmail']" @close="showExportModal = false" />

        <StepUpModal
            :visible="stepUpVisible"
            action-label="Thiết lập quyền riêng cho tài khoản"
            action="UserAdministration"
            :action-description="'Lưu thiết lập quyền cho tài khoản ' + (scopeTarget?.username || '')"
            severity="high"
            @cancel="onStepUpCancelled"
            @confirmed="onStepUpConfirmed"
        />
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { getAll, create, update, deleteUser, resetMfa, getOperationalScopeReference, getOperationalScopes, replaceOperationalScopes, getUserGateAccess, replaceUserGateAccess } from '../services/userApi'
import { getAll as getAllEmployees } from '../services/employeeApi'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import ImportModal from '../components/import-export/ImportModal.vue'
import ExportModal from '../components/import-export/ExportModal.vue'
import StepUpModal from '../components/shared/StepUpModal.vue'

const users = ref([])
const loading = ref(true)
const showImportModal = ref(false)
const showExportModal = ref(false)
const loadError = ref('')
const searchQuery = ref('')
const filterRole = ref('')
const filterStatus = ref('')

// Modal state
const showModal = ref(false)
const isEditing = ref(false)
const editingId = ref(null)
const saving = ref(false)
const modalError = ref('')

const modalForm = reactive({
    username: '',
    password: '',
    fullName: '',
    role: 'LeTan',
    isActive: true,
    employeeId: null
})

const fieldErrors = reactive({
    username: '',
    password: '',
    fullName: '',
})

// Employee combo box state
const employeesList = ref([])
const loadingEmployees = ref(false)
const showEmployeeDropdown = ref(false)
const employeeSearchText = ref('')
const comboBoxRef = ref(null)

const assignedEmployeeIds = computed(() =>
    new Set(
        users.value
            .filter(user => user.employeeId && (!isEditing.value || user.userId !== editingId.value))
            .map(user => user.employeeId)
    )
)

const filteredEmployees = computed(() => {
    const q = employeeSearchText.value.toLowerCase().trim()
    const availableEmployees = employeesList.value.filter(employee =>
        !assignedEmployeeIds.value.has(employee.employeeId)
    )

    if (!q) return availableEmployees

    return availableEmployees.filter(employee =>
        employee.fullName.toLowerCase().includes(q) ||
        (employee.phone && employee.phone.includes(q)) ||
        (employee.email && employee.email.toLowerCase().includes(q))
    )
})

async function fetchEmployeesList() {
    if (employeesList.value.length > 0) return
    loadingEmployees.value = true
    try {
        const res = await getAllEmployees()
        employeesList.value = res.data
    } catch (e) {
        console.error('Failed to load employees', e)
    } finally {
        loadingEmployees.value = false
    }
}

function onEmployeeSearchInput() {
    showEmployeeDropdown.value = true
    // If user edits text after selecting, clear fullName so they must re-select
    if (modalForm.fullName && employeeSearchText.value !== modalForm.fullName) {
        modalForm.fullName = ''
        modalForm.employeeId = null
    }
}

function selectEmployee(emp) {
    modalForm.fullName = emp.fullName
    modalForm.employeeId = emp.employeeId
    employeeSearchText.value = emp.fullName
    showEmployeeDropdown.value = false
    fieldErrors.fullName = ''
}

function clearEmployeeSelection() {
    modalForm.fullName = ''
    modalForm.employeeId = null
    employeeSearchText.value = ''
    showEmployeeDropdown.value = false
    fieldErrors.fullName = ''
}

// Close dropdown on outside click
function handleClickOutside(e) {
    if (comboBoxRef.value && !comboBoxRef.value.contains(e.target)) {
        showEmployeeDropdown.value = false
    }
}

// Delete modal
const showDeleteModal = ref(false)
const deleteTarget = ref(null)
const showScopeModal = ref(false)
const scopeLoading = ref(false)
const scopeSaving = ref(false)
const stepUpVisible = ref(false)
const scopeError = ref('')
const scopeTarget = ref(null)
const scopeItems = ref([])
const scopeReferenceLoaded = ref(false)
const scopeReference = reactive({
    tasksByRole: {},
    taskCatalog: [],
    sites: [],
    gates: [],
    lanes: [],
    zones: []
})
let scopeRowSeed = 1
const permissionOverrides = ref([])
const scopeActiveTab = ref('tasks')
const gateAccessItems = ref([])
const gateAccessError = ref('')
const gateAccessLoaded = ref(false)

// Computed
const activeCount = computed(() => users.value.filter(u => u.isActive).length)
const inactiveCount = computed(() => users.value.filter(u => !u.isActive).length)

const filteredUsers = computed(() => {
    return users.value.filter(u => {
        const matchSearch = !searchQuery.value ||
            u.username.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
            (u.fullName && u.fullName.toLowerCase().includes(searchQuery.value.toLowerCase()))
        const matchRole = !filterRole.value || u.role === filterRole.value
        const matchStatus = !filterStatus.value ||
            (filterStatus.value === 'active' && u.isActive) ||
            (filterStatus.value === 'inactive' && !u.isActive)
        return matchSearch && matchRole && matchStatus
    })
})

const scopeTaskOptions = computed(() =>
    (scopeReference.taskCatalog || []).map(task => ({ value: task.taskKey, label: task.label }))
)

const isAdminUser = computed(() => scopeTarget.value?.role === 'Admin')

// Fetch users
async function fetchUsers() {
    loading.value = true
    loadError.value = ''
    try {
        const res = await getAll()
        users.value = res.data
    } catch (err) {
        if (err.code === 'ERR_NETWORK') {
            loadError.value = 'Không thể kết nối đến server'
        } else {
            loadError.value = 'Không thể tải danh sách tài khoản'
        }
    } finally {
        loading.value = false
    }
}

function onImportComplete() {
    showImportModal.value = false
    fetchUsers()
}

// Modal handlers
function openCreateModal() {
    isEditing.value = false
    editingId.value = null
    modalError.value = ''
    Object.keys(fieldErrors).forEach((key) => { fieldErrors[key] = '' })
    Object.assign(modalForm, { username: '', password: '', fullName: '', role: 'LeTan', isActive: true, employeeId: null })
    employeeSearchText.value = ''
    showEmployeeDropdown.value = false
    fetchEmployeesList()
    showModal.value = true
}

function openEditModal(user) {
    isEditing.value = true
    editingId.value = user.userId
    modalError.value = ''
    Object.keys(fieldErrors).forEach((key) => { fieldErrors[key] = '' })
    Object.assign(modalForm, { username: user.username, password: '', fullName: user.fullName || '', role: user.role, isActive: user.isActive, employeeId: user.employeeId || null })
    employeeSearchText.value = user.fullName || ''
    showEmployeeDropdown.value = false
    fetchEmployeesList()
    showModal.value = true
}

function closeModal() {
    showModal.value = false
    modalError.value = ''
}

async function handleSubmit() {
    Object.keys(fieldErrors).forEach((key) => { fieldErrors[key] = '' })

    if (!isEditing.value) {
        if (!modalForm.username.trim()) {
            fieldErrors.username = 'Vui lòng nhập tên đăng nhập.'
        } else if (!/^[a-zA-Z0-9_]+$/.test(modalForm.username.trim())) {
            fieldErrors.username = 'Tên đăng nhập chỉ gồm chữ cái, số và dấu gạch dưới.'
        } else if (modalForm.username.trim().length < 3) {
            fieldErrors.username = 'Tên đăng nhập tối thiểu 3 ký tự.'
        }
    }

    if (!isEditing.value && !modalForm.password) {
        fieldErrors.password = 'Vui lòng nhập mật khẩu.'
    } else if (modalForm.password && modalForm.password.length < 6) {
        fieldErrors.password = 'Mật khẩu tối thiểu 6 ký tự.'
    }

    if (!modalForm.employeeId) {
        fieldErrors.fullName = 'Vui lòng chọn nhân viên cho tài khoản.'
    }

    if (Object.values(fieldErrors).some((msg) => msg)) return

    saving.value = true
    modalError.value = ''
    try {
        if (isEditing.value) {
            const data = { fullName: modalForm.fullName || null, role: modalForm.role, isActive: modalForm.isActive, employeeId: modalForm.employeeId || null }
            if (modalForm.password) data.password = modalForm.password
            await update(editingId.value, data)
        } else {
            await create({ username: modalForm.username, password: modalForm.password, fullName: modalForm.fullName || null, role: modalForm.role, employeeId: modalForm.employeeId || null })
        }
        closeModal()
        await fetchUsers()
    } catch (err) {
        modalError.value = err.response?.data?.message || 'Đã xảy ra lỗi, vui lòng thử lại'
    } finally {
        saving.value = false
    }
}

// Delete handlers
function confirmDelete(user) {
    deleteTarget.value = user
    modalError.value = ''
    showDeleteModal.value = true
}

async function handleDelete() {
    saving.value = true
    modalError.value = ''
    try {
        await deleteUser(deleteTarget.value.userId)
        showDeleteModal.value = false
        await fetchUsers()
    } catch (err) {
        modalError.value = err.response?.data?.message || 'Không thể xóa tài khoản'
    } finally {
        saving.value = false
    }
}

// Helpers
function getInitials(name) {
    if (!name) return '?'
    return name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()
}

const getAvatarColor = (str) => {
    let hash = 0; for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash);
    const avColors = [ '#3b82f6', '#ec4899', '#10b981', '#f59e0b', '#8b5cf6', '#06b6d4', '#f43f5e' ];
    return avColors[Math.abs(hash) % avColors.length];
}

function getRoleLabel(role) {
    const map = { Admin: 'Admin', QuanLy: 'Quản lý', LeTan: 'Lễ tân', BaoVe: 'Bảo vệ', NhanSu: 'Nhân sự', NhanVien: 'Nhân viên' }
    return map[role] || role
}

async function handleResetMfa(user) {
    if (!user?.userId || !user.mfaEnabled) return

    const confirmed = window.confirm(`Đặt lại MFA cho tài khoản ${user.username}? Người dùng sẽ phải thiết lập lại ở lần đăng nhập tiếp theo.`)
    if (!confirmed) return

    saving.value = true
    modalError.value = ''

    try {
        await resetMfa(user.userId)
        await fetchUsers()
    } catch (err) {
        modalError.value = err.response?.data?.message || 'Không thể đặt lại MFA cho tài khoản này'
    } finally {
        saving.value = false
    }
}

async function ensureScopeReference() {
    if (scopeReferenceLoaded.value) return
    const res = await getOperationalScopeReference()
    scopeReference.tasksByRole = res.data?.tasksByRole || {}
    scopeReference.taskCatalog = res.data?.taskCatalog || []
    scopeReference.sites = res.data?.sites || []
    scopeReference.gates = res.data?.gates || []
    scopeReference.lanes = res.data?.lanes || []
    scopeReference.zones = res.data?.zones || []
    scopeReferenceLoaded.value = true
}

function mapScopeToUi(item = {}) {
    return {
        localId: `scope_${scopeRowSeed++}`,
        taskKey: item.taskKey || '',
        siteId: item.siteId != null ? String(item.siteId) : '',
        gateId: item.gateId != null ? String(item.gateId) : '',
        laneId: item.laneId != null ? String(item.laneId) : '',
        securityZoneId: item.securityZoneId != null ? String(item.securityZoneId) : '',
        canView: item.canView !== false,
        canManage: item.canManage !== false,
        note: item.note || ''
    }
}

function createPermissionOverride(task) {
    const role = scopeTarget.value?.role
    const defaultTaskKeys = role ? scopeReference.tasksByRole?.[role] || [] : []
    return {
        taskKey: task.taskKey,
        label: task.label,
        defaultAllowed: defaultTaskKeys.includes(task.taskKey),
        accessMode: 'inherit',
        routes: task.routes || []
    }
}

function describeAccessMode(mode) {
    if (mode === 'allow') return 'Tài khoản này sẽ được mở thêm quyền cho chức năng này.'
    if (mode === 'deny') return 'Tài khoản này sẽ bị chặn riêng chức năng này.'
    return 'Đang dùng quyền mặc định của vai trò.'
}

function addScopeRow() {
    scopeItems.value.push(mapScopeToUi())
}

function removeScopeRow(index) {
    scopeItems.value.splice(index, 1)
}

async function openScopeModal(user) {
    scopeTarget.value = user
    scopeError.value = ''
    gateAccessError.value = ''
    scopeActiveTab.value = 'tasks'
    gateAccessLoaded.value = false
    scopeLoading.value = true
    showScopeModal.value = true
    try {
        await ensureScopeReference()
        const res = await getOperationalScopes(user.userId)
        const allScopes = Array.isArray(res.data) ? res.data : []
        permissionOverrides.value = (scopeReference.taskCatalog || []).map(task => createPermissionOverride(task))

        const overrideLookup = new Map(
            allScopes
                .filter(item => !item.siteId && !item.gateId && !item.laneId && !item.securityZoneId)
                .map(item => [item.taskKey, item])
        )

        permissionOverrides.value.forEach(item => {
            const current = overrideLookup.get(item.taskKey)
            if (!current) return
            item.accessMode = current.canView || current.canManage ? 'allow' : 'deny'
        })

        scopeItems.value = allScopes
            .filter(item => item.siteId || item.gateId || item.laneId || item.securityZoneId)
            .map(mapScopeToUi)
    } catch (err) {
        scopeError.value = err.response?.data?.message || 'Không thể tải thiết lập quyền'
        permissionOverrides.value = []
        scopeItems.value = []
    }
    try {
        await loadGateAccess(user.userId)
    } catch (err) {
        gateAccessError.value = err.response?.data?.message || 'Không thể tải quyền qua cổng'
        gateAccessItems.value = []
    } finally {
        scopeLoading.value = false
    }
}

async function loadGateAccess(userId) {
    const res = await getUserGateAccess(userId)
    const data = res.data || {}
    gateAccessItems.value = (data.gates || []).map(gate => ({
        gateId: gate.gateId,
        gateName: gate.gateName,
        location: gate.location,
        defaultAllowed: gate.defaultAllowed,
        accessMode: gate.accessMode,
        effectiveAllowed: gate.effectiveAllowed
    }))
    gateAccessLoaded.value = true
}

function setGateMode(gate, mode) {
    gate.accessMode = mode
    gate.effectiveAllowed = mode === 'allow' ? true : mode === 'deny' ? false : gate.defaultAllowed
}

function describeGateMode(gate) {
    if (gate.accessMode === 'allow') return 'Tài khoản này được mở thêm quyền qua cổng này.'
    if (gate.accessMode === 'deny') return 'Tài khoản này bị chặn riêng cổng này.'
    return gate.defaultAllowed
        ? 'Đang dùng mặc định của vai trò: ĐƯỢC qua cổng.'
        : 'Đang dùng mặc định của vai trò: KHÔNG được qua cổng.'
}

function closeScopeModal() {
    showScopeModal.value = false
    scopeTarget.value = null
    permissionOverrides.value = []
    scopeItems.value = []
    scopeError.value = ''
    gateAccessItems.value = []
    gateAccessError.value = ''
    gateAccessLoaded.value = false
}

function buildGatePayload() {
    return gateAccessItems.value
        .filter(item => item.accessMode !== 'inherit')
        .map(item => ({ gateId: item.gateId, accessMode: item.accessMode }))
}

async function saveScopes() {
    if (!scopeTarget.value) return
    stepUpVisible.value = true
}

function onStepUpCancelled() {
    stepUpVisible.value = false
}

async function onStepUpConfirmed(result) {
    stepUpVisible.value = false
    if (result?.sessionId) {
        enterpriseApi.setStepUpSession(result.sessionId)
    }
    await performSaveScopes()
}

async function performSaveScopes() {
    if (!scopeTarget.value) return
    scopeSaving.value = true
    scopeError.value = ''
    try {
        const overridePayload = permissionOverrides.value
            .filter(item => item.accessMode !== 'inherit')
            .map(item => ({
                taskKey: item.taskKey,
                siteId: null,
                gateId: null,
                laneId: null,
                securityZoneId: null,
                canView: item.accessMode === 'allow',
                canManage: item.accessMode === 'allow',
                note: item.accessMode === 'deny' ? 'Personal deny override' : 'Personal allow override'
            }))

        const scopePayload = scopeItems.value
            .filter(item => item.taskKey)
            .map(item => ({
                taskKey: item.taskKey,
                siteId: item.siteId ? Number(item.siteId) : null,
                gateId: item.gateId ? Number(item.gateId) : null,
                laneId: item.laneId ? Number(item.laneId) : null,
                securityZoneId: item.securityZoneId ? Number(item.securityZoneId) : null,
                canView: !!item.canView,
                canManage: !!item.canManage,
                note: item.note?.trim() || null
            }))

        const payload = [...overridePayload, ...scopePayload]

        await replaceOperationalScopes(scopeTarget.value.userId, payload)
        if (gateAccessLoaded.value) {
            await replaceUserGateAccess(scopeTarget.value.userId, buildGatePayload())
        }
        await fetchUsers()
        closeScopeModal()
    } catch (err) {
        scopeError.value = err.response?.data?.message || 'Không thể lưu thiết lập quyền'
    } finally {
        scopeSaving.value = false
        enterpriseApi.setStepUpSession(null)
    }
}

function getRoleBadgeClass(role) {
    const map = { Admin: 'admin', QuanLy: 'manager', LeTan: 'reception', BaoVe: 'guard', NhanSu: 'staff', NhanVien: 'staff' }
    return map[role] || 'staff'
}

function formatDate(dateStr) {
    if (!dateStr) return '-'
    return new Date(dateStr).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

onMounted(() => {
    fetchUsers()
    ensureScopeReference().catch(() => {})
    document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
/* Common Page Layout */
.bento-header { margin-bottom: 24px; padding: 0 4px; display: flex; justify-content: space-between; align-items: center; }
.bento-header .greeting h1 { font-size: 1.8rem; font-weight: 700; color: var(--text-primary); }
.bento-header .greeting p { color: var(--text-secondary); font-size: 0.95rem; }

/* Grid Mini */
.bento-grid-mini { display: grid; gap: 20px; margin-bottom: 24px; }
.bento-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius-lg); padding: 24px; }
.stat-card { display: flex; align-items: center; gap: 16px; transition: transform var(--transition-normal); }
.stat-card:hover { transform: translateY(-3px); box-shadow: var(--shadow-md); }
.stat-icon-wrapper { width: 56px; height: 56px; border-radius: 14px; display: flex; justify-content: center; align-items: center; }
.stat-icon-wrapper svg { width: 28px; height: 28px; }
.stat-icon-wrapper.blue { background: rgba(16, 121, 196, 0.1); color: var(--accent-primary); }
.stat-icon-wrapper.green { background: rgba(16, 185, 129, 0.1); color: var(--accent-success); }
.stat-icon-wrapper.red { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); }
.stat-val { font-size: 1.8rem; font-weight: 700; color: var(--text-primary); line-height: 1.2; }
.stat-val.blue { color: var(--accent-primary); }
.stat-val.green { color: var(--accent-success); }
.stat-val.red { color: var(--accent-danger); }
.stat-lbl { font-size: 0.9rem; color: var(--text-muted); font-weight: 500;}


/* Table Box */
.table-section { padding: 0; overflow: hidden; display: flex; flex-direction: column; min-height: 500px; }
.table-toolbar { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--border-color); }
.search-box { position: relative; width: 320px; display: flex; align-items: center; }
.search-icon { position: absolute; left: 14px; color: var(--text-muted); width: 18px; }
.search-box input { width: 100%; padding: 10px 14px 10px 42px; background: var(--bg-input); border: 1px solid var(--border-color); border-radius: 8px; color: var(--text-primary); outline: none; transition: border 0.2s; }
.search-box input:focus { border-color: var(--accent-primary); box-shadow: 0 0 0 2px rgba(16, 121, 196, 0.2); }
.minimal-select { padding: 10px 14px; background: var(--bg-input); border: 1px solid var(--border-color); border-radius: 8px; color: var(--text-primary); cursor: pointer; outline: none; transition: border-color 0.2s, box-shadow 0.2s; }
.minimal-select:hover { border-color: var(--border-strong); }

/* Table Elements */
.sleek-table-container { flex: 1; overflow-x: auto; }
.sleek-table { width: 100%; border-collapse: collapse; text-align: left; }
.sleek-table th { padding: 16px 24px; font-size: 0.85rem; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid var(--border-color); background: rgba(0,0,0,0.1); }
.sleek-table td { padding: 18px 24px; border-bottom: 1px solid var(--border-color); vertical-align: middle; }
.table-row { transition: background var(--transition-fast); }
.table-row:hover { background: var(--bg-card-hover); cursor: default; }

.user-cell { display: flex; align-items: center; gap: 14px; }
.avatar, .avatar-img { width: 38px; height: 38px; border-radius: 50%; display: flex; justify-content: center; align-items: center; font-weight: 700; color: var(--text-on-interactive); font-size: 0.8rem; object-fit: cover; }
.user-info { display: flex; flex-direction: column; }
.user-name { font-weight: 600; font-size: 0.95rem; color: var(--text-primary); }
.text-primary { color: var(--text-primary); }
.text-muted { color: var(--text-muted); }

.badge-role { display: inline-flex; align-items: center; padding: 4px 10px; border-radius: 6px; font-size: 0.75rem; font-weight: 600; letter-spacing: 0.5px; border: 1px solid transparent; }
.badge-role.admin { background: rgba(168, 85, 247, 0.1); color: #a855f7; border-color: rgba(168, 85, 247, 0.2); }
.badge-role.manager { background: rgba(245, 158, 11, 0.12); color: #d97706; border-color: rgba(245, 158, 11, 0.24); }
.badge-role.reception { background: rgba(14, 165, 233, 0.12); color: #0284c7; border-color: rgba(14, 165, 233, 0.24); }
.badge-role.staff { background: rgba(16, 121, 196, 0.1); color: var(--accent-primary); border-color: rgba(16, 121, 196, 0.2); }
.badge-role.guard { background: rgba(16, 185, 129, 0.1); color: var(--accent-success); border-color: rgba(16, 185, 129, 0.2); }

.status-pill.minimal { padding: 4px 10px; border-radius: 6px; font-size: 0.75rem; border: 1px solid transparent; letter-spacing: 0.5px; display: inline-flex; align-items: center; gap: 6px; font-weight: 600;}
.status-pill.active { background: rgba(16, 185, 129, 0.05); color: var(--accent-success); border-color: rgba(16, 185, 129, 0.2); }
.status-pill.inactive { background: rgba(239, 68, 68, 0.05); color: var(--accent-danger); border-color: rgba(239, 68, 68, 0.2); }
.pill-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

.action-menu { display: flex; gap: 8px; justify-content: flex-end; }
.icon-btn { width: 34px; height: 34px; display: flex; align-items: center; justify-content: center; border-radius: 8px; border: none; background: transparent; color: var(--text-muted); cursor: pointer; transition: all 0.2s; }
.icon-btn svg { width: 18px; }
.icon-btn:hover { background: var(--bg-input); color: var(--text-primary); }
.icon-btn.action-reject:hover { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); }

/* Spinners & Empties */
.empty-layout { padding: 60px; text-align: center; color: var(--text-muted); display: flex; flex-direction: column; align-items: center; gap: 16px; }
.spinner-lg { width: 36px; height: 36px; border: 3px solid var(--border-color); border-top-color: var(--accent-primary); border-radius: 50%; animation: spin 0.8s linear infinite; }
.spinner-sm { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.3); border-top-color: var(--text-on-interactive); border-radius: 50%; animation: spin 0.6s linear infinite; display: inline-block; margin-right: 6px; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Modern Modals */
.modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.6); backdrop-filter: blur(4px); display: flex; justify-content: center; align-items: center; z-index: 1000; padding: 20px;}
.modern-modal { background: var(--bg-card); width: 100%; max-width: 500px; border-radius: var(--border-radius-lg); border: 1px solid var(--border-color); box-shadow: var(--shadow-xl); overflow: hidden; display: flex; flex-direction: column;}
.modern-modal.mini { max-width: 400px; }
.modal-top { display: flex; justify-content: space-between; align-items: center; padding: 24px; border-bottom: 1px solid var(--border-color); }
.modal-top h3 { font-size: 1.25rem; font-weight: 700; color: var(--text-primary); margin: 0;}
.icon-close { background: none; border: none; color: var(--text-muted); cursor: pointer; width: 24px; transition: color 0.2s; }
.icon-close:hover { color: var(--accent-danger); }

.modal-body { padding: 24px; display: flex; flex-direction: column; }
.modal-form-grid { display: flex; flex-direction: column; gap: 20px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

.input-pane { display: flex; flex-direction: column; gap: 8px; }
.input-pane label { font-size: 0.9rem; font-weight: 500; color: var(--text-secondary); }
.req { color: var(--accent-danger); }

.sleek-input, .sleek-select { width: 100%; padding: 12px 16px; background: var(--bg-input); border: 1px solid var(--border-color); border-radius: 8px; color: var(--text-primary); outline: none; transition: border 0.2s; font-size: 0.95rem; }
.sleek-input:focus, .sleek-select:focus { border-color: var(--accent-primary); box-shadow: 0 0 0 3px rgba(16, 121, 196, 0.15); }

.error-box { display: flex; align-items: center; gap: 8px; padding: 12px 16px; background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.2); border-radius: 8px; color: var(--accent-danger); font-size: 0.85rem; margin-top: 10px; }
.error-box svg { width: 18px; height: 18px; flex-shrink: 0; }

.modal-actions { display: flex; justify-content: flex-end; gap: 12px; }
.modal-actions.centered { justify-content: center; }
.warning-icon svg { width: 48px; height: 48px; color: var(--accent-danger); margin-bottom: 16px; }
.scope-modal-shell { max-width: 1120px; max-height: min(90vh, 920px); }
.scope-modal-body { overflow-y: auto; }
.scope-note { color: var(--text-secondary); margin: 0 0 16px; line-height: 1.65; }
.scope-toolbar { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 18px; }
.scope-summary { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.scope-block { margin-bottom: 18px; }
.scope-card-block { border: 1px solid var(--border-color); border-radius: 18px; padding: 18px; background: linear-gradient(180deg, rgba(255,255,255,0.02), rgba(0,0,0,0.04)); }
.scope-block-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 14px; }
.scope-block-title { margin: 0; color: var(--text-primary); font-size: 1rem; }
.scope-block-subtitle { margin: 6px 0 0; color: var(--text-muted); font-size: 0.88rem; line-height: 1.5; }
.permission-card-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 14px; }
.permission-card { border: 1px solid var(--border-color); border-radius: 16px; padding: 16px; background: var(--bg-card); display: flex; flex-direction: column; gap: 14px; min-width: 0; }
.permission-card-top { display: flex; flex-direction: column; gap: 10px; }
.permission-card-title { margin: 0; font-size: 0.98rem; color: var(--text-primary); }
.permission-card-routes { margin: 6px 0 0; color: var(--text-muted); font-size: 0.82rem; line-height: 1.5; word-break: break-word; }
.permission-card-caption { margin: 0; color: var(--text-secondary); font-size: 0.84rem; line-height: 1.45; }
.mode-switch { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; }
.mode-option { border: 1px solid var(--border-color); background: var(--bg-input); color: var(--text-secondary); border-radius: 12px; padding: 10px 8px; font-size: 0.82rem; font-weight: 600; cursor: pointer; transition: all 0.18s ease; }
.mode-option:hover { border-color: var(--accent-primary); color: var(--text-primary); }
.mode-option.active { background: rgba(16, 121, 196, 0.14); border-color: rgba(16, 121, 196, 0.34); color: var(--accent-primary); }
.mode-option.allow.active { background: rgba(16, 185, 129, 0.12); border-color: rgba(16, 185, 129, 0.28); color: var(--accent-success); }
.mode-option.deny.active { background: rgba(239, 68, 68, 0.1); border-color: rgba(239, 68, 68, 0.24); color: var(--accent-danger); }
.scope-empty { border: 1px dashed var(--border-color); border-radius: 14px; padding: 20px; color: var(--text-muted); text-align: center; line-height: 1.6; }
.scope-list { display: flex; flex-direction: column; gap: 14px; }
.scope-entry { border: 1px solid var(--border-color); border-radius: 16px; padding: 16px; background: var(--bg-card); }
.scope-entry-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 12px; }
.scope-entry-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.scope-field { display: flex; flex-direction: column; gap: 8px; min-width: 0; }
.scope-field span { color: var(--text-secondary); font-size: 0.85rem; font-weight: 600; }
.scope-field-wide { grid-column: 1 / -1; }
.scope-permission-row { display: flex; gap: 20px; flex-wrap: wrap; margin-top: 14px; padding-top: 14px; border-top: 1px solid var(--border-color); }
.scope-check { display: inline-flex; align-items: center; gap: 8px; color: var(--text-secondary); font-size: 0.9rem; }
.scope-check input { width: 16px; height: 16px; accent-color: var(--accent-primary); }
.scope-tabs .tab-btn {
    flex: 1; padding: 10px 16px; border-radius: 12px; border: none; background: transparent;
    color: var(--text-secondary); font-size: 0.9rem; font-weight: 500; cursor: pointer; transition: all 0.2s;
}
.scope-tabs .tab-btn.active {
    background: var(--bg-surface-raised); color: var(--text-primary); box-shadow: 0 2px 8px rgba(0,0,0,0.12);
}
.scope-tabs .tab-btn:hover { color: var(--text-primary); }
.zone-lock-note {
    border: 1px solid rgba(168, 85, 247, 0.2); background: rgba(168, 85, 247, 0.08);
    color: #a855f7; border-radius: 12px; padding: 10px 12px; font-size: 0.84rem; line-height: 1.5;
}

.text-right { text-align: right; }
.text-center { text-align: center; }
.text-left { text-align: left; }
.mt-4 { margin-top: 24px; }

.modal-enter-active, .modal-leave-active { transition: all 0.3s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; transform: scale(0.95); }

@media (max-width: 1200px) { .bento-grid-mini { grid-template-columns: repeat(2, 1fr); } }
/* Employee Combo Box */
.employee-search-pane { position: relative; }
.combo-box-wrapper { position: relative; }
.combo-input-row { position: relative; display: flex; align-items: center; }
.combo-search-icon { position: absolute; left: 14px; width: 16px; height: 16px; color: var(--text-muted); pointer-events: none; z-index: 1; }
.combo-input { padding-left: 40px !important; padding-right: 36px !important; }
.combo-clear-btn { position: absolute; right: 10px; background: none; border: none; cursor: pointer; color: var(--text-muted); display: flex; align-items: center; justify-content: center; width: 22px; height: 22px; border-radius: 50%; transition: all 0.2s; }
.combo-clear-btn:hover { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); }
.combo-clear-btn svg { width: 14px; height: 14px; }

.combo-dropdown { position: absolute; top: calc(100% + 4px); left: 0; right: 0; background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 10px; box-shadow: var(--shadow-xl); max-height: 220px; overflow-y: auto; z-index: 1001; }
.combo-dropdown::-webkit-scrollbar { width: 6px; }
.combo-dropdown::-webkit-scrollbar-track { background: transparent; }
.combo-dropdown::-webkit-scrollbar-thumb { background: var(--border-color); border-radius: 3px; }

.combo-option { display: flex; align-items: center; gap: 12px; padding: 10px 14px; cursor: pointer; transition: background 0.15s; }
.combo-option:hover { background: var(--bg-card-hover); }
.combo-option.selected { background: rgba(16, 121, 196, 0.08); }
.combo-option:first-child { border-radius: 10px 10px 0 0; }
.combo-option:last-child { border-radius: 0 0 10px 10px; }

.combo-opt-avatar { width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; font-weight: 700; color: var(--text-on-interactive); flex-shrink: 0; }
.combo-opt-info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.combo-opt-name { font-size: 0.9rem; font-weight: 600; color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.combo-opt-detail { font-size: 0.78rem; color: var(--text-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.combo-loading, .combo-empty { padding: 16px; text-align: center; color: var(--text-muted); font-size: 0.88rem; display: flex; align-items: center; justify-content: center; gap: 8px; }

.dropdown-enter-active { transition: all 0.2s ease; }
.dropdown-leave-active { transition: all 0.15s ease; }
.dropdown-enter-from { opacity: 0; transform: translateY(-6px); }
.dropdown-leave-to { opacity: 0; transform: translateY(-4px); }

@media (max-width: 768px) {
    .bento-grid-mini { grid-template-columns: 1fr; }
    .grid-2 { grid-template-columns: 1fr; }
    .table-toolbar { flex-direction: column; gap: 16px; align-items: stretch;}
    .search-box { width: 100%; }
    .scope-block-head { flex-direction: column; }
    .scope-entry-grid { grid-template-columns: 1fr; }
    .mode-switch { grid-template-columns: 1fr; }
}
</style>

