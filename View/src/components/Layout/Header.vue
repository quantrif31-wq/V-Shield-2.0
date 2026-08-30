<template>
    <header class="app-header" :class="{ collapsed: collapsed && !isMobile }">
        <div class="header-left">
            <button
                type="button"
                class="menu-toggle"
                aria-label="Mở điều hướng"
                @click="$emit('toggle-sidebar')"
            >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="3" y1="6" x2="21" y2="6" />
                    <line x1="3" y1="12" x2="21" y2="12" />
                    <line x1="3" y1="18" x2="21" y2="18" />
                </svg>
            </button>

            <div class="header-intro">
                <span class="header-eyebrow">Trung tâm điều hành an ninh</span>
                <div class="header-copy">
                    <h2>{{ pageTitle }}</h2>
                    <p>{{ pageDescription }}</p>
                </div>
            </div>
        </div>

        <div class="header-right" ref="dropdownRootRef">
            <!-- Active Incoming / In-Call Indicator Pill (Zalo Style) -->
            <div v-if="callState.state === 'incoming'" class="header-call-pill incoming-call-pill">
                <span class="call-pulse-ring"></span>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" class="call-icon-ringing" width="16" height="16">
                    <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16.92z"></path>
                </svg>
                <div class="call-pill-text">
                    <strong>{{ callState.fromFullName }}</strong>
                    <span>Đang gọi...</span>
                </div>
                <div class="call-pill-actions">
                    <button type="button" class="btn-pill-action accept" @click.stop="acceptCall" title="Trả lời">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="13" height="13">
                            <polyline points="20 6 9 17 4 12"></polyline>
                        </svg>
                    </button>
                    <button type="button" class="btn-pill-action decline" @click.stop="rejectCall" title="Từ chối">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="13" height="13">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                </div>
            </div>
            <div v-else-if="callState.state === 'connected'" class="header-call-pill active-call-pill" @click="togglePip" title="Nhấp để mở rộng cuộc gọi">
                <span class="active-dot"></span>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                    <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16.92z"></path>
                </svg>
                <span>{{ formatCallDuration(callState.callDuration) }}</span>
            </div>

            <div class="header-chip status-chip" :class="`status-${highestActiveSeverity}`">
                <span class="chip-dot"></span>
                <span>{{ statusChipLabel }}</span>
            </div>

            <div class="header-chip time-chip">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <circle cx="12" cy="12" r="9" />
                    <path d="M12 7v5l3 2" />
                </svg>
                <div>
                    <strong>{{ currentTime }}</strong>
                    <span>{{ currentDate }}</span>
                </div>
            </div>

            <button
                type="button"
                class="header-action notification-trigger"
                :class="`severity-${highestActiveSeverity}`"
                aria-label="Trung tâm cảnh báo"
                @click="toggleNotifications"
            >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
                    <path d="M13.73 21a2 2 0 01-3.46 0" />
                </svg>
                <span v-if="pendingCount" class="notification-count">{{ pendingCount }}</span>
            </button>

            <button
                type="button"
                class="header-action"
                :aria-label="isDark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang chế độ phòng điều khiển tối'"
                :title="isDark ? 'Giao diện sáng' : 'Chế độ phòng điều khiển'"
                @click="toggleTheme"
            >
                <svg v-if="isDark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                    <circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.42 1.42M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.42-1.42M17.66 6.34l1.41-1.41"/>
                </svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                    <path d="M21 12.8A9 9 0 1111.2 3 7 7 0 0021 12.8z"/>
                </svg>
            </button>

            <div class="header-user-wrap">
                <button type="button" class="header-user" @click="toggleUserMenu">
                    <div class="user-avatar">{{ userInitial }}</div>
                    <div class="user-info">
                        <span class="user-name">{{ authState.user?.fullName || authState.user?.username || 'User' }}</span>
                        <span class="user-role">{{ roleLabel }}</span>
                    </div>
                    <svg class="user-chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M6 9l6 6 6-6" />
                    </svg>
                </button>

                <transition name="dropdown">
                    <div v-if="showUserMenu" class="dropdown user-dropdown">
                        <div class="dropdown-user-info">
                            <div class="user-avatar large">{{ userInitial }}</div>
                            <div>
                                <p class="dropdown-user-name">{{ authState.user?.fullName || authState.user?.username }}</p>
                                <p class="dropdown-user-role">{{ roleLabel }}</p>
                            </div>
                        </div>

                        <div class="dropdown-list simple">
                            <div class="dropdown-summary">
                                <span>Truy cập hiện tại</span>
                                <strong>{{ pageTitle }}</strong>
                            </div>

                            <div class="dropdown-summary preference-summary">
                                <span>Mật độ hiển thị</span>
                                <div class="density-actions" role="group" aria-label="Mật độ hiển thị">
                                    <button type="button" :aria-pressed="density === 'comfortable'" @click="setDensity('comfortable')">Thoải mái</button>
                                    <button type="button" :aria-pressed="density === 'compact'" @click="setDensity('compact')">Gọn</button>
                                </div>
                            </div>

                            <button type="button" class="dropdown-item" @click="handleLogout">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
                                    <polyline points="16 17 21 12 16 7" />
                                    <line x1="21" y1="12" x2="9" y2="12" />
                                </svg>
                                Đăng xuất an toàn
                            </button>
                        </div>
                    </div>
                </transition>
            </div>

            <transition name="dropdown">
                <div v-if="showNotifications" class="dropdown notification-dropdown expanded">
                    <div class="dropdown-header">
                        <div>
                            <span>Trung tâm thông báo & cảnh báo</span>
                            <small>{{ statusSummary }}</small>
                        </div>
                        <button
                            v-if="unreadNotificationCount || unresolvedItems.length"
                            type="button"
                            class="btn-link"
                            @click="markAllRead"
                        >
                            Đánh dấu tất cả đã đọc
                        </button>
                    </div>

                    <div class="notification-tabs">
                        <button
                            type="button"
                            class="tab-btn"
                            :class="{ active: activeFilterTab === 'all' }"
                            @click="activeFilterTab = 'all'"
                        >
                            Tất cả
                            <span class="tab-count">{{ mergedFeed.length }}</span>
                        </button>
                        <button
                            type="button"
                            class="tab-btn"
                            :class="{ active: activeFilterTab === 'security' }"
                            @click="activeFilterTab = 'security'"
                        >
                            An ninh
                            <span v-if="securityCount" class="tab-count badge-danger">{{ securityCount }}</span>
                        </button>
                        <button
                            type="button"
                            class="tab-btn"
                            :class="{ active: activeFilterTab === 'approval' }"
                            @click="activeFilterTab = 'approval'"
                        >
                            Phê duyệt
                            <span v-if="approvalCount" class="tab-count badge-caution">{{ approvalCount }}</span>
                        </button>
                        <button
                            type="button"
                            class="tab-btn"
                            :class="{ active: activeFilterTab === 'chat' }"
                            @click="activeFilterTab = 'chat'"
                        >
                            Trao đổi
                            <span v-if="chatCallCount" class="tab-count badge-success">{{ chatCallCount }}</span>
                        </button>
                    </div>

                    <div class="notification-legend">
                        <span v-for="item in severityLegend" :key="item.key" class="legend-item" :class="`legend-${item.key}`">
                            {{ item.label }}
                        </span>
                    </div>

                    <div class="notification-list">
                        <div
                            v-for="item in filteredFeed"
                            :key="item.source + '-' + item.id"
                            class="notification-item"
                            :class="[
                                `severity-${item.severity}`,
                                {
                                    unread: !item.read,
                                    active: item.isActive,
                                    'requires-ack': item.requiresAck,
                                },
                            ]"
                            @click="handleItemClick(item)"
                        >
                            <div class="notification-rail"></div>

                            <div class="notification-icon" :class="`severity-${item.severity}`">
                                <svg v-if="item.severity === 'critical'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <path d="M12 8v5" />
                                    <path d="M12 16h.01" />
                                    <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
                                </svg>
                                <svg v-else-if="item.severity === 'warning'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
                                    <line x1="12" y1="9" x2="12" y2="13" />
                                    <line x1="12" y1="17" x2="12.01" y2="17" />
                                </svg>
                                <svg v-else-if="item.severity === 'caution'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <path d="M12 8v5" />
                                    <path d="M12 16h.01" />
                                    <circle cx="12" cy="12" r="9" />
                                </svg>
                                <svg v-else-if="item.severity === 'success'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" />
                                </svg>
                                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                    <circle cx="12" cy="12" r="9" />
                                    <path d="M12 8v5" />
                                    <path d="M12 16h.01" />
                                </svg>
                            </div>

                            <div class="notification-content">
                                <div class="notification-topline">
                                    <div>
                                        <p class="notification-title">{{ item.title }}</p>
                                        <p class="notification-message">{{ item.message }}</p>
                                    </div>
                                    <span class="severity-pill" :class="`severity-${item.severity}`">
                                        {{ severityLabel(item.severity) }}
                                    </span>
                                </div>

                                <div class="notification-meta">
                                    <span class="meta-badge">{{ sourceLabel(item) }}</span>
                                    <span v-if="item.locationLabel" class="meta-text">{{ item.locationLabel }}</span>
                                    <span v-if="item.referenceType && item.referenceType !== sourceLabel(item)" class="meta-text">{{ item.referenceType }}</span>
                                    <span class="meta-time">{{ formatTimeAgo(item.time) }}</span>
                                </div>

                                <div class="notification-actions">
                                    <button
                                        v-if="item.requiresAck"
                                        type="button"
                                        class="action-button primary"
                                        :disabled="ackLoading[item.source + '-' + item.id]"
                                        @click.stop="acknowledgeItem(item)"
                                    >
                                        {{ ackLoading[item.source + '-' + item.id] ? 'Đang xác nhận...' : 'Xác nhận xử lý' }}
                                    </button>
                                    <button
                                        type="button"
                                        class="action-button"
                                        @click.stop="openItem(item)"
                                    >
                                        {{ actionButtonLabel(item) }}
                                    </button>
                                    <span v-if="item.source === 'security-alert' && !item.requiresAck" class="action-hint">
                                        {{ item.isActive ? 'Đang hoạt động' : 'Đã ghi nhận' }}
                                    </span>
                                    <span v-else-if="!item.read" class="action-hint">
                                        Chưa đọc
                                    </span>
                                </div>
                            </div>
                        </div>

                        <div v-if="!filteredFeed.length" class="notification-empty">
                            <strong>Không có mục nào trong danh mục này</strong>
                            <span>Các thông báo mới, yêu cầu phê duyệt và trao đổi sẽ xuất hiện tại đây.</span>
                        </div>
                    </div>
                </div>
            </transition>
        </div>
    </header>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState, logout } from '../../stores/auth'
import {
    connectNotificationHub,
    disconnectNotificationHub,
    getNotifications,
    getSeverityRank,
    getUnreadCount,
    markAllNotificationsRead,
    markNotificationRead,
    normalizeNotificationSeverity,
    onNotification,
    onUnreadCountChanged,
} from '../../services/notificationApi'
import { enterpriseApi } from '../../services/enterpriseSecurityApi'
import { refreshSecurityAlerts, securityAlertState } from '../../services/securityAlertBus'
import { socApi } from '../../services/socApi'
import { usePreferences } from '../../composables/usePreferences'
import { callState, acceptCall, rejectCall, togglePip, formatCallDuration } from '../../stores/callStore'

defineProps({
    collapsed: Boolean,
    isMobile: Boolean,
})

defineEmits(['toggle-sidebar'])

const router = useRouter()
const route = useRoute()
const dropdownRootRef = ref(null)
const showNotifications = ref(false)
const showUserMenu = ref(false)
const activeFilterTab = ref('all')
const currentTime = ref('')
const currentDate = ref('')
const dbNotifications = ref([])
const unreadCount = ref(0)
const ackLoading = ref({})
const { density, isDark, setDensity, toggleTheme } = usePreferences()

const severityLegend = [
    { key: 'success', label: 'Trao đổi & Chat' },
    { key: 'info', label: 'Thông tin' },
    { key: 'caution', label: 'Cần chú ý / Phê duyệt' },
    { key: 'warning', label: 'Cần xử lý sớm' },
    { key: 'critical', label: 'Khẩn cấp' },
]

const routeMeta = {
    Dashboard: {
        title: 'Tổng quan hệ thống',
        description: 'Nhìn nhanh người, xe, khách và camera ngay khi đăng nhập vào V-Shield.',
    },
    Monitoring: {
        title: 'Giám sát trực tiếp',
        description: 'Theo dõi camera, cổng, biển số và access log gần nhất trong cùng một màn hình.',
    },
    AccessLogs: {
        title: 'Tra cứu vào/ra',
        description: 'Rà soát lịch sử ra vào theo thời gian, cổng, biển số và trạng thái xử lý.',
    },
    SystemAuditLogs: {
        title: 'Nhật ký hệ thống',
        description: 'Theo dõi ai làm gì, dữ liệu trước/sau, kết quả thành công hay thất bại.',
    },
    Exceptions: {
        title: 'Xử lý ngoại lệ',
        description: 'Tập trung các trường hợp bypass, lỗi nhận diện và lý do ngoại lệ cần đối soát.',
    },
    PreRegistration: {
        title: 'Danh sách hẹn trước',
        description: 'Quản lý lượt khách đăng ký trước, duyệt trạng thái và theo dõi lịch hẹn.',
    },
    RegistrationLinks: {
        title: 'Link đăng ký tự động',
        description: 'Tạo và quản lý token đăng ký để khách tự khai báo trước khi đến.',
    },
    GuestProfiles: {
        title: 'Hồ sơ khách',
        description: 'Lưu danh bạ khách quen để tái sử dụng nhanh cho các lần mời tiếp theo.',
    },
    Employees: {
        title: 'Hồ sơ nhân viên',
        description: 'Quản lý nhân sự, phòng ban, chức vụ và dữ liệu nhận diện nội bộ.',
    },
    Vehicles: {
        title: 'Phương tiện nội bộ',
        description: 'Theo dõi xe đăng ký cố định của nhân viên và trạng thái trong bãi.',
    },
    AttendanceRecords: {
        title: 'Bảng chấm công',
        description: 'Theo dõi check-in/check-out, đi trễ và tổng giờ làm theo ngày.',
    },
    AttendanceWorkSchedules: {
        title: 'Lịch làm việc',
        description: 'Phân ca và kiểm soát trạng thái lịch làm việc của nhân viên.',
    },
    AttendanceShifts: {
        title: 'Ca làm việc',
        description: 'Cấu hình khung giờ ca, đi trễ, về sớm và nghỉ giữa ca.',
    },
    LeaveRequests: {
        title: 'Đơn xin nghỉ',
        description: 'Gửi đơn nghỉ, theo dõi phê duyệt và trạng thái xử lý.',
    },
    LeaveApprovals: {
        title: 'Duyệt đơn nghỉ',
        description: 'Quản lý/Admin xử lý đơn nghỉ cho nhân viên theo phòng ban.',
    },
    AttendanceReports: {
        title: 'Báo cáo công',
        description: 'Thống kê ngày công, đi trễ, về sớm và tăng ca theo tháng.',
    },
    DeviceManagement: {
        title: 'Camera & cổng',
        description: 'Khai báo và cấu hình các camera, cổng truy cập đang có trong hệ thống.',
    },
    Biometrics: {
        title: 'Dữ liệu nhận diện',
        description: 'Khu vực legacy không sử dụng trong luồng demo QR động.',
    },
    UserManagement: {
        title: 'Tài khoản & phân quyền',
        description: 'Quản lý người dùng phần mềm, vai trò và trạng thái hoạt động của tài khoản.',
    },
    SystemCatalog: {
        title: 'Danh mục hệ thống',
        description: 'Rà soát các danh mục tĩnh như phòng ban, chức vụ và lý do ngoại lệ.',
    },
    DepartmentPosition: {
        title: 'Cấu trúc tổ chức',
        description: 'Quản trị chi tiết phòng ban và chức vụ dùng trong toàn bộ hệ thống.',
    },
    AboutProject: {
        title: 'Giới thiệu dự án',
        description: 'Thông tin tổng quan về mục tiêu và bối cảnh triển khai V-Shield.',
    },
    Settings: {
        title: 'Cài đặt hệ thống',
        description: 'Khu cấu hình mở rộng của ứng dụng.',
    },
    GateTransitMonitor: {
        title: 'Điều phối thông hành',
        description: 'Vận hành theo từng làn với face, biển số và xác nhận qua cổng.',
    },
    tao_qr_d: {
        title: 'Tạo QR động',
        description: 'Sinh mã QR realtime cho nhân viên từ backend QR động mới.',
    },
    scan_qr_d: {
        title: 'Quét QR động',
        description: 'Giải mã, theo dõi phiên và xác thực QR động tại cổng quét.',
    },
    Login: {
        title: 'Xác thực truy cập',
        description: 'Đăng nhập an toàn để vào trung tâm điều phối V-Shield.',
    },
}

const pageTitle = computed(() => routeMeta[route.name]?.title || 'V-Shield Trung tâm điều hành')
const pageDescription = computed(() => routeMeta[route.name]?.description || 'Điều phối và kiểm soát ra vào tập trung.')

const userInitial = computed(() => {
    const name = authState.user?.fullName || authState.user?.username || 'U'
    return name.charAt(0).toUpperCase()
})

const roleLabel = computed(() => {
    const map = {
        Admin: 'Quản trị viên',
        BaoVe: 'Bảo vệ trực cổng',
        QuanLy: 'Quản lý vận hành',
        LeTan: 'Lễ tân',
    }
    return map[authState.user?.role] || authState.user?.role || 'Tài khoản hệ thống'
})

const normalizedNotifications = computed(() => {
    const chatMap = new Map()
    const result = []

    for (const notification of dbNotifications.value) {
        const isChat = notification.category === 'Chat' ||
            String(notification.referenceType || '').toLowerCase() === 'chat' ||
            String(notification.referenceType || '').toLowerCase() === 'call'

        if (isChat && notification.referenceId) {
            const key = `chat_${notification.referenceId}`
            if (!chatMap.has(key)) {
                chatMap.set(key, notification)
            } else {
                const existing = chatMap.get(key)
                if (new Date(notification.createdAt) > new Date(existing.createdAt)) {
                    chatMap.set(key, notification)
                }
                continue
            }
        }

        const severity = normalizeNotificationSeverity(notification)
        result.push({
            id: notification.id,
            source: 'notification',
            category: notification.category,
            title: notification.title || 'Thông báo hệ thống',
            message: notification.body || notification.title || 'Có một cập nhật mới.',
            severity,
            severityRank: getSeverityRank(severity),
            time: notification.createdAt,
            read: !!notification.isRead,
            requiresAck: false,
            ackKind: null,
            actionUrl: notification.actionUrl,
            referenceType: notification.referenceType,
            referenceId: notification.referenceId,
            locationLabel: notification.locationLabel,
            isActive: !notification.isRead,
        })
    }
    return result
})

const normalizedSecurityAlerts = computed(() =>
    (securityAlertState.items || []).map((item) => {
        const alertId = String(item.id || '')
        const parsedId = parsePrefixedId(alertId)
        const ackKind = alertId.startsWith('alarm-') ? 'alarm' : alertId.startsWith('duress-') ? 'duress' : null
        const severity = mapSecuritySeverity(item.severity, item.kind)

        return {
            id: alertId,
            source: 'security-alert',
            title: item.title || 'Cảnh báo an ninh',
            message: item.message || 'Hệ thống đang cần xử lý một tình huống an ninh.',
            severity,
            severityRank: getSeverityRank(severity),
            time: item.occurredAtUtc || item.generatedAtUtc || new Date().toISOString(),
            read: false,
            requiresAck: ackKind === 'alarm' || ackKind === 'duress',
            ackKind,
            actionUrl: item.route || defaultAlertRoute(item.kind),
            referenceType: item.kind,
            referenceId: parsedId,
            locationLabel: item.locationLabel || item.zoneName || '',
            isActive: true,
        }
    })
)

const unresolvedItems = computed(() =>
    [
        ...normalizedNotifications.value.filter((item) => !item.read),
        ...normalizedSecurityAlerts.value.filter((item) => item.isActive),
    ]
)

const highestActiveSeverity = computed(() => {
    if (!unresolvedItems.value.length) {
        return 'neutral'
    }

    const highest = unresolvedItems.value.reduce((max, item) => {
        if (!max || item.severityRank > max.severityRank) {
            return item
        }
        return max
    }, null)

    return highest?.severity || 'neutral'
})

const pendingCount = computed(() => unresolvedItems.value.length)
const unreadNotificationCount = computed(() => normalizedNotifications.value.filter((item) => !item.read).length)

const mergedFeed = computed(() =>
    [
        ...normalizedSecurityAlerts.value,
        ...normalizedNotifications.value,
    ].sort((left, right) => {
        if (right.severityRank !== left.severityRank) {
            return right.severityRank - left.severityRank
        }

        if (Number(right.isActive) !== Number(left.isActive)) {
            return Number(right.isActive) - Number(left.isActive)
        }

        if (Number(!right.read) !== Number(!left.read)) {
            return Number(!right.read) - Number(!left.read)
        }

        return new Date(right.time).getTime() - new Date(left.time).getTime()
    })
)

const securityCount = computed(() =>
    mergedFeed.value.filter(
        (item) =>
            item.source === 'security-alert' ||
            item.severity === 'critical' ||
            item.severity === 'warning' ||
            String(item.referenceType || '').toLowerCase().includes('alarm')
    ).length
)

const approvalCount = computed(() =>
    mergedFeed.value.filter(
        (item) =>
            item.severity === 'caution' ||
            String(item.referenceType || '').toLowerCase().includes('approval') ||
            String(item.referenceType || '').toLowerCase().includes('leave') ||
            String(item.referenceType || '').toLowerCase().includes('vehicle') ||
            String(item.referenceType || '').toLowerCase().includes('claim') ||
            String(item.referenceType || '').toLowerCase().includes('evidence') ||
            String(item.referenceType || '').toLowerCase().includes('intervention')
    ).length
)

const chatCallCount = computed(() =>
    mergedFeed.value.filter(
        (item) =>
            item.category === 'Chat' ||
            item.severity === 'success' ||
            String(item.referenceType || '').toLowerCase() === 'chat' ||
            String(item.referenceType || '').toLowerCase() === 'call'
    ).length
)

const filteredFeed = computed(() => {
    if (activeFilterTab.value === 'security') {
        return mergedFeed.value.filter(
            (item) =>
                item.source === 'security-alert' ||
                item.severity === 'critical' ||
                item.severity === 'warning' ||
                String(item.referenceType || '').toLowerCase().includes('alarm')
        )
    }
    if (activeFilterTab.value === 'approval') {
        return mergedFeed.value.filter(
            (item) =>
                item.severity === 'caution' ||
                String(item.referenceType || '').toLowerCase().includes('approval') ||
                String(item.referenceType || '').toLowerCase().includes('leave') ||
                String(item.referenceType || '').toLowerCase().includes('vehicle') ||
                String(item.referenceType || '').toLowerCase().includes('claim') ||
                String(item.referenceType || '').toLowerCase().includes('evidence') ||
                String(item.referenceType || '').toLowerCase().includes('intervention')
        )
    }
    if (activeFilterTab.value === 'chat') {
        return mergedFeed.value.filter(
            (item) =>
                item.category === 'Chat' ||
                item.severity === 'success' ||
                String(item.referenceType || '').toLowerCase() === 'chat' ||
                String(item.referenceType || '').toLowerCase() === 'call'
        )
    }
    return mergedFeed.value
})

const statusChipLabel = computed(() => {
    const labels = {
        neutral: 'Ổn định',
        success: 'Có trao đổi mới',
        info: 'Có cập nhật hệ thống',
        caution: 'Có việc cần theo dõi',
        warning: 'Cần xử lý sớm',
        critical: 'Cảnh báo khẩn cấp',
    }
    return labels[highestActiveSeverity.value] || labels.neutral
})

const statusSummary = computed(() => {
    if (!mergedFeed.value.length) {
        return 'Không có mục nào đang chờ xử lý.'
    }

    const securityC = securityCount.value
    const notificationC = unreadNotificationCount.value
    return `${pendingCount.value} mục chưa xử lý, gồm ${securityC} an ninh/cảnh báo và ${notificationC} thông báo mới.`
})

function parsePrefixedId(value) {
    const raw = String(value || '').split('-').slice(1).join('-')
    const num = Number(raw)
    return Number.isFinite(num) ? num : raw
}

function mapSecuritySeverity(severity, kind) {
    const severityKey = String(severity || '').toLowerCase()
    const kindKey = String(kind || '').toLowerCase()

    if (severityKey === 'critical' || kindKey.includes('duress') || kindKey.includes('emergency') || kindKey.includes('intrusion')) {
        return 'critical'
    }

    if (severityKey === 'high') return 'warning'
    if (severityKey === 'medium') return 'caution'
    if (severityKey === 'low') return 'info'
    return severityKey === 'warning' ? 'warning' : 'info'
}

function defaultAlertRoute(kind) {
    const key = String(kind || '').toLowerCase()
    if (key.includes('emergency')) return '/policy-engine'
    return '/soc-console'
}

function sourceLabel(item) {
    if (item.source === 'security-alert') {
        if (item.ackKind === 'duress') return 'Báo động uy hiếp'
        if (item.ackKind === 'alarm') return 'Cảnh báo SOC'
        return 'Điều phối an ninh'
    }

    const refType = String(item.referenceType || '').toLowerCase()
    const cat = String(item.category || '').toLowerCase()
    if (refType.includes('call') || cat.includes('call')) return 'Cuộc gọi'
    if (refType.includes('chat') || cat.includes('chat')) return 'Tin nhắn'
    if (refType.includes('leave')) return 'Đơn nghỉ phép'
    if (refType.includes('vehicle')) return 'Xe nội bộ'
    if (refType.includes('lostfound') || refType.includes('claim')) return 'Thất lạc & Tìm thấy'
    if (refType.includes('evidence')) return 'Hồ sơ chứng cứ'
    if (refType.includes('intervention')) return 'Tác nghiệp an ninh'
    if (refType.includes('alarm')) return 'Cảnh báo hệ thống'

    const severity = item.severity
    if (severity === 'success') return 'Trao đổi'
    if (severity === 'caution') return 'Phê duyệt'
    if (severity === 'warning') return 'Cần xử lý'
    if (severity === 'critical') return 'Khẩn cấp'
    return 'Thông báo'
}

function actionButtonLabel(item) {
    if (item.source === 'security-alert') return 'Mở điều phối'
    const refType = String(item.referenceType || '').toLowerCase()
    const cat = String(item.category || '').toLowerCase()
    if (refType.includes('chat') || cat.includes('chat')) return 'Mở trò chuyện'
    if (refType.includes('call') || cat.includes('call')) return 'Gọi lại / Nhắn tin'
    if (refType.includes('leave')) return 'Xem đơn nghỉ'
    if (refType.includes('vehicle')) return 'Xem xe'
    if (refType.includes('lostfound') || refType.includes('claim')) return 'Xem đồ thất lạc'
    if (refType.includes('evidence')) return 'Xem chứng cứ'
    if (refType.includes('intervention')) return 'Xem tác nghiệp'
    if (refType.includes('alarm')) return 'Xem cảnh báo'
    return 'Xem chi tiết'
}

function severityLabel(severity) {
    const labels = {
        success: 'Trao đổi',
        info: 'Thông tin',
        caution: 'Phê duyệt',
        warning: 'Cảnh báo',
        critical: 'Khẩn cấp',
        neutral: 'Bình thường',
    }
    return labels[severity] || labels.neutral
}

function formatTimeAgo(dateStr) {
    if (!dateStr) return ''
    const diff = Date.now() - new Date(dateStr).getTime()
    const mins = Math.floor(diff / 60000)
    if (mins < 1) return 'Vừa xong'
    if (mins < 60) return `${mins} phút trước`
    const hrs = Math.floor(mins / 60)
    if (hrs < 24) return `${hrs} giờ trước`
    const days = Math.floor(hrs / 24)
    if (days < 7) return `${days} ngày trước`
    return new Date(dateStr).toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    })
}

function updateTime() {
    const now = new Date()
    currentTime.value = now.toLocaleTimeString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
    })
    currentDate.value = now.toLocaleDateString('vi-VN', {
        weekday: 'long',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    })
}

async function loadNotifications() {
    try {
        const response = await getNotifications(0, 50)
        dbNotifications.value = response.data?.data || []
    } catch {
        dbNotifications.value = []
    }
}

async function loadUnreadCount() {
    try {
        const response = await getUnreadCount()
        unreadCount.value = response.data?.count || 0
    } catch {
        unreadCount.value = 0
    }
}

function closeDropdowns() {
    showNotifications.value = false
    showUserMenu.value = false
}

function toggleNotifications() {
    showNotifications.value = !showNotifications.value
    showUserMenu.value = false
}

function toggleUserMenu() {
    showUserMenu.value = !showUserMenu.value
    showNotifications.value = false
}

async function markAllRead() {
    try {
        await markAllNotificationsRead()
    } catch {}
    dbNotifications.value = dbNotifications.value.map((item) => ({
        ...item,
        isRead: true,
    }))
    unreadCount.value = 0
}

async function handleItemClick(item) {
    if (item.source === 'security-alert') {
        await openItem(item)
        return
    }

    if (!item.read) {
        try {
            await markNotificationRead(item.id)
            const refId = item.referenceId
            if (refId && (item.category === 'Chat' || String(item.referenceType || '').toLowerCase() === 'chat')) {
                dbNotifications.value = dbNotifications.value.map((entry) => {
                    if (entry.referenceId === refId && (entry.category === 'Chat' || String(entry.referenceType || '').toLowerCase() === 'chat')) {
                        return { ...entry, isRead: true }
                    }
                    return entry
                })
            } else {
                const target = dbNotifications.value.find((entry) => entry.id === item.id)
                if (target) {
                    target.isRead = true
                }
            }
            unreadCount.value = Math.max(0, unreadCount.value - 1)
        } catch {}
    }

    await openItem(item)
}

async function openItem(item) {
    showNotifications.value = false
    if (item.actionUrl) {
        await router.push(item.actionUrl)
        return
    }
    const refType = String(item.referenceType || '').toLowerCase()
    const cat = String(item.category || '').toLowerCase()
    if (refType.includes('chat') || cat.includes('chat') || refType.includes('call') || cat.includes('call')) {
        await router.push('/chat')
        return
    }
    if (refType.includes('leave')) {
        const isApprover = ['Admin', 'QuanLy'].includes(authState.user?.role)
        await router.push(isApprover ? '/leave-approvals' : '/leave-requests')
    } else if (refType.includes('vehicle')) {
        await router.push('/vehicles')
    } else if (refType.includes('lostfound') || refType.includes('claim')) {
        await router.push('/lost-found')
    } else if (refType.includes('evidence')) {
        await router.push('/evidence-repository')
    } else if (refType.includes('alarm') || refType.includes('intervention')) {
        await router.push('/soc-console')
    } else {
        await router.push('/dashboard')
    }
}

async function acknowledgeItem(item) {
    const key = `${item.source}-${item.id}`
    ackLoading.value = {
        ...ackLoading.value,
        [key]: true,
    }

    try {
        if (item.ackKind === 'alarm') {
            await socApi.acknowledgeAlarm(item.referenceId)
        } else if (item.ackKind === 'duress') {
            await enterpriseApi.acknowledgeDuressEvent(item.referenceId)
        }

        await refreshSecurityAlerts()
    } finally {
        ackLoading.value = {
            ...ackLoading.value,
            [key]: false,
        }
    }
}

async function handleLogout() {
    await logout()
    router.push('/login')
}

function handleDocumentClick(event) {
    if (dropdownRootRef.value && !dropdownRootRef.value.contains(event.target)) {
        closeDropdowns()
    }
}

let timer = null
let removeNotificationSubscription = null
let removeUnreadSubscription = null

onMounted(async () => {
    updateTime()
    timer = window.setInterval(updateTime, 1000)
    document.addEventListener('click', handleDocumentClick)

    await Promise.all([loadNotifications(), loadUnreadCount(), refreshSecurityAlerts()])

    try {
        const token = sessionStorage.getItem('v_shield_token') || localStorage.getItem('v_shield_token')
        if (token) {
            await connectNotificationHub(token)
            removeNotificationSubscription = onNotification((notification) => {
                dbNotifications.value = [notification, ...dbNotifications.value.filter(n => n.id !== notification.id)].slice(0, 100)
                unreadCount.value += 1
            })
            removeUnreadSubscription = onUnreadCountChanged((count) => {
                unreadCount.value = Number(count) || 0
            })
        }
    } catch {}
})

onUnmounted(() => {
    if (timer) {
        window.clearInterval(timer)
    }
    document.removeEventListener('click', handleDocumentClick)
    removeNotificationSubscription?.()
    removeUnreadSubscription?.()
    disconnectNotificationHub()
})

watch(
    () => route.fullPath,
    () => {
        closeDropdowns()
    }
)

watch(
    () => unreadCount.value,
    (count) => {
        if (count !== unreadNotificationCount.value) {
            unreadCount.value = unreadNotificationCount.value
        }
    }
)

</script>

<style scoped>
.preference-summary {
    display: grid;
    gap: var(--space-2);
}

.density-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: var(--space-1);
    padding: var(--space-1);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-control);
    background: var(--surface-subtle);
}

.density-actions button {
    min-height: 34px;
    border-radius: calc(var(--radius-control) - 3px);
    color: var(--text-secondary);
    font-size: var(--type-caption-size);
    font-weight: 700;
}

.density-actions button[aria-pressed='true'] {
    background: var(--surface-default);
    color: var(--text-primary);
    box-shadow: var(--shadow-xs);
}
.app-header {
    position: fixed;
    top: 16px;
    right: 18px;
    left: calc(var(--sidebar-width) + 18px);
    height: var(--header-height);
    z-index: 85;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 18px;
    padding: 16px 22px;
    border-radius: 26px;
    border: 1px solid var(--border-subtle);
    background: var(--bg-header);
    backdrop-filter: var(--glass-blur);
    box-shadow: var(--shadow-sm);
    transition: left var(--transition-slow);
}

.app-header.collapsed {
    left: 86px;
}

.header-left {
    min-width: 0;
    display: flex;
    align-items: center;
    gap: 16px;
}

.menu-toggle {
    width: 44px;
    height: 44px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 14px;
    border: 1px solid var(--border-color);
    background: var(--surface-default);
    color: var(--text-secondary);
    flex-shrink: 0;
    cursor: pointer;
    transition: background var(--transition-fast), border-color var(--transition-fast);
}

.menu-toggle:hover {
    background: var(--surface-hover);
    border-color: var(--border-focus);
}

.menu-toggle svg {
    width: 20px;
    height: 20px;
}

.header-intro {
    min-width: 0;
}

.header-eyebrow {
    display: inline-block;
    margin-bottom: 4px;
    color: var(--text-muted);
    font-size: 0.74rem;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
}

.header-copy h2 {
    font-family: var(--font-heading);
    font-size: 1.28rem;
    font-weight: 700;
    line-height: 1.05;
    color: var(--text-primary);
}

.header-copy p {
    margin-top: 4px;
    max-width: 52ch;
    color: var(--text-secondary);
    font-size: 0.9rem;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.header-right {
    position: relative;
    display: flex;
    align-items: center;
    gap: 12px;
    flex-shrink: 0;
}

.header-chip {
    min-height: 48px;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 0 16px;
    border-radius: 999px;
    border: 1px solid var(--border-color);
    background: var(--surface-default);
    color: var(--text-secondary);
}

.status-chip {
    font-weight: 700;
}

.status-chip.status-neutral,
.status-chip.status-info {
    color: var(--accent-primary);
    border-color: rgba(15, 124, 130, 0.18);
    background: rgba(15, 124, 130, 0.08);
}

.status-chip.status-success {
    color: var(--accent-success);
    border-color: rgba(20, 134, 109, 0.16);
    background: rgba(20, 134, 109, 0.08);
}

.status-chip.status-caution {
    color: #af7a13;
    border-color: rgba(234, 185, 52, 0.24);
    background: rgba(234, 185, 52, 0.12);
}

.status-chip.status-warning {
    color: #c56d1f;
    border-color: rgba(221, 136, 39, 0.26);
    background: rgba(221, 136, 39, 0.12);
}

.status-chip.status-critical {
    color: #c43131;
    border-color: rgba(196, 49, 49, 0.24);
    background: rgba(196, 49, 49, 0.12);
}

.chip-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 0 5px rgba(20, 134, 109, 0.12);
}

.time-chip svg {
    width: 18px;
    height: 18px;
    color: var(--text-muted);
    flex-shrink: 0;
}

.time-chip div {
    display: flex;
    flex-direction: column;
}

.time-chip strong {
    color: var(--text-primary);
    font-weight: 700;
    line-height: 1;
}

.time-chip span {
    margin-top: 3px;
    color: var(--text-muted);
    font-size: 0.74rem;
}

.header-action {
    position: relative;
    width: 46px;
    height: 46px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 16px;
    border: 1px solid var(--border-color);
    background: var(--surface-default);
    color: var(--text-secondary);
    transition: border-color var(--transition-fast), color var(--transition-fast), transform var(--transition-fast), background var(--transition-fast), box-shadow var(--transition-fast);
}

.header-action:hover {
    transform: translateY(-1px);
    border-color: var(--border-color-hover);
    background: var(--surface-hover);
}

.header-action svg {
    width: 20px;
    height: 20px;
}

.notification-trigger.severity-neutral {
    color: var(--text-secondary);
}

.notification-trigger.severity-success {
    color: var(--accent-success);
    border-color: rgba(20, 134, 109, 0.22);
    background: rgba(20, 134, 109, 0.08);
}

.notification-trigger.severity-info {
    color: var(--accent-primary);
    border-color: rgba(15, 124, 130, 0.22);
    background: rgba(15, 124, 130, 0.08);
}

.notification-trigger.severity-caution {
    color: #af7a13;
    border-color: rgba(234, 185, 52, 0.24);
    background: rgba(234, 185, 52, 0.12);
}

.notification-trigger.severity-warning {
    color: #c56d1f;
    border-color: rgba(221, 136, 39, 0.24);
    background: rgba(221, 136, 39, 0.12);
}

.notification-trigger.severity-critical {
    color: #c43131;
    border-color: rgba(196, 49, 49, 0.32);
    background: rgba(196, 49, 49, 0.12);
    box-shadow: 0 12px 28px rgba(196, 49, 49, 0.18);
}

.notification-count {
    position: absolute;
    top: -4px;
    right: -3px;
    min-width: 20px;
    height: 20px;
    padding: 0 5px;
    border-radius: 999px;
    background: var(--accent-danger);
    color: #fff;
    font-size: 0.68rem;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 10px 20px rgba(195, 81, 70, 0.24);
}

.header-user-wrap {
    position: relative;
}

.header-user {
    display: inline-flex;
    align-items: center;
    gap: 12px;
    min-height: 50px;
    padding: 0 14px 0 8px;
    border-radius: 18px;
    border: 1px solid var(--border-color);
    background: var(--surface-default);
    color: var(--text-primary);
    transition: border-color var(--transition-fast), transform var(--transition-fast), background var(--transition-fast);
}

.header-user:hover {
    transform: translateY(-1px);
    border-color: var(--border-color-hover);
    background: var(--surface-hover);
}

.user-avatar {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--accent-gradient);
    color: #fff;
    font-weight: 700;
    box-shadow: 0 14px 28px rgba(15, 124, 130, 0.18);
}

.user-avatar.large {
    width: 48px;
    height: 48px;
    font-size: 1rem;
}

.user-info {
    display: flex;
    flex-direction: column;
    text-align: left;
}

.user-name {
    font-size: 0.9rem;
    font-weight: 700;
}

.user-role {
    color: var(--text-muted);
    font-size: 0.74rem;
}

.user-chevron {
    width: 16px;
    height: 16px;
    color: var(--text-muted);
}

.dropdown {
    position: absolute;
    top: calc(100% + 12px);
    right: 0;
    width: min(380px, calc(100vw - 32px));
    border-radius: 22px;
    border: 1px solid var(--border-subtle);
    background: var(--surface-default);
    color: var(--text-primary);
    box-shadow: var(--shadow-overlay);
    overflow: hidden;
    backdrop-filter: var(--glass-blur);
}

.notification-dropdown {
    right: 72px;
}

.notification-dropdown.expanded {
    width: min(680px, calc(100vw - 36px));
}

.dropdown-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    padding: 18px 20px 16px;
    border-bottom: 1px solid var(--border-subtle);
}

.dropdown-header span {
    display: block;
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-primary);
}

.dropdown-header small {
    display: block;
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.76rem;
    line-height: 1.45;
}

.btn-link {
    color: var(--accent-primary);
    font-size: 0.78rem;
    font-weight: 700;
}

.notification-tabs {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 10px 16px;
    background: var(--surface-subtle);
    border-bottom: 1px solid var(--border-subtle);
    overflow-x: auto;
    scrollbar-width: none;
}

.notification-tabs::-webkit-scrollbar {
    display: none;
}

.tab-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 12px;
    border-radius: 10px;
    border: 1px solid transparent;
    background: transparent;
    color: var(--text-secondary);
    font-size: 0.78rem;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
    transition: all var(--transition-fast);
}

.tab-btn:hover {
    color: var(--text-primary);
    background: var(--surface-hover);
}

.tab-btn.active {
    color: var(--accent-primary);
    background: var(--surface-default);
    border-color: var(--border-subtle);
    box-shadow: var(--shadow-xs);
}

.tab-count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 18px;
    height: 18px;
    padding: 0 5px;
    border-radius: 999px;
    font-size: 0.68rem;
    font-weight: 700;
    background: var(--surface-selected);
    color: var(--text-primary);
}

.tab-count.badge-danger {
    background: rgba(196, 49, 49, 0.15);
    color: #c43131;
}

.tab-count.badge-caution {
    background: rgba(234, 185, 52, 0.2);
    color: #af7a13;
}

.tab-count.badge-success {
    background: rgba(20, 134, 109, 0.16);
    color: var(--accent-success);
}

.notification-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    padding: 12px 20px 0;
}

.legend-item,
.severity-pill,
.meta-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 28px;
    padding: 0 10px;
    border-radius: 999px;
    font-size: 0.72rem;
    font-weight: 700;
}

.legend-success,
.severity-pill.severity-success {
    color: var(--accent-success);
    background: rgba(20, 134, 109, 0.12);
}

.legend-info,
.severity-pill.severity-info {
    color: var(--accent-primary);
    background: rgba(15, 124, 130, 0.1);
}

.legend-caution,
.severity-pill.severity-caution {
    color: #af7a13;
    background: rgba(234, 185, 52, 0.16);
}

.legend-warning,
.severity-pill.severity-warning {
    color: #c56d1f;
    background: rgba(221, 136, 39, 0.14);
}

.legend-critical,
.severity-pill.severity-critical {
    color: #c43131;
    background: rgba(196, 49, 49, 0.14);
}

.dropdown-user-info {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 20px;
    border-bottom: 1px solid var(--border-subtle);
}

.dropdown-user-name {
    color: var(--text-primary);
    font-size: 0.96rem;
    font-weight: 700;
}

.dropdown-user-role {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.78rem;
}

.dropdown-list.simple {
    padding: 14px;
}

.dropdown-summary {
    padding: 4px 8px 14px;
    color: var(--text-muted);
    font-size: 0.76rem;
}

.dropdown-summary strong {
    display: block;
    margin-top: 6px;
    color: var(--text-primary);
    font-family: var(--font-heading);
    font-size: 1rem;
}

.dropdown-item {
    width: 100%;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    min-height: 44px;
    padding: 0 14px;
    border-radius: 14px;
    color: var(--text-secondary);
    transition: background var(--transition-fast), color var(--transition-fast), transform var(--transition-fast);
}

.dropdown-item:hover {
    transform: translateY(-1px);
    background: rgba(195, 81, 70, 0.08);
    color: var(--accent-danger);
}

.dropdown-item svg {
    width: 18px;
    height: 18px;
}

.notification-list {
    max-height: min(68vh, 640px);
    overflow-y: auto;
    padding: 14px;
}

.notification-item {
    position: relative;
    display: flex;
    align-items: flex-start;
    gap: 14px;
    padding: 16px 16px 16px 18px;
    border-radius: 18px;
    border: 1px solid var(--border-subtle);
    background: var(--surface-default);
    transition: transform var(--transition-fast), background var(--transition-fast), border-color var(--transition-fast), box-shadow var(--transition-fast);
    cursor: pointer;
}

.notification-item + .notification-item {
    margin-top: 10px;
}

.notification-item:hover {
    transform: translateY(-1px);
    background: var(--surface-hover);
    border-color: var(--border-default);
    box-shadow: var(--shadow-xs);
}

.notification-item.unread {
    box-shadow: inset 0 0 0 1px rgba(15, 124, 130, 0.16);
}

.notification-item.active {
    border-color: var(--border-focus);
    background: var(--surface-selected);
}

.notification-rail {
    position: absolute;
    left: 0;
    top: 10px;
    bottom: 10px;
    width: 4px;
    border-radius: 999px;
    background: rgba(15, 124, 130, 0.18);
}

.notification-item.severity-success .notification-rail {
    background: rgba(20, 134, 109, 0.65);
}

.notification-item.severity-info .notification-rail {
    background: rgba(15, 124, 130, 0.58);
}

.notification-item.severity-caution .notification-rail {
    background: rgba(234, 185, 52, 0.78);
}

.notification-item.severity-warning .notification-rail {
    background: rgba(221, 136, 39, 0.82);
}

.notification-item.severity-critical .notification-rail {
    background: rgba(196, 49, 49, 0.92);
}

.notification-icon {
    width: 42px;
    height: 42px;
    border-radius: 14px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}

.notification-icon.severity-success {
    color: var(--accent-success);
    background: rgba(20, 134, 109, 0.12);
}

.notification-icon.severity-info {
    color: var(--accent-primary);
    background: rgba(15, 124, 130, 0.1);
}

.notification-icon.severity-caution {
    color: #af7a13;
    background: rgba(234, 185, 52, 0.16);
}

.notification-icon.severity-warning {
    color: #c56d1f;
    background: rgba(221, 136, 39, 0.14);
}

.notification-icon.severity-critical {
    color: #c43131;
    background: rgba(196, 49, 49, 0.14);
}

.notification-icon svg {
    width: 18px;
    height: 18px;
}

.notification-content {
    min-width: 0;
    flex: 1;
}

.notification-topline {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
}

.notification-title {
    color: var(--text-primary);
    font-size: 0.92rem;
    font-weight: 700;
    line-height: 1.35;
}

.notification-message {
    margin-top: 4px;
    color: var(--text-secondary);
    font-size: 0.84rem;
    line-height: 1.55;
}

.notification-meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
    margin-top: 10px;
}

.meta-badge {
    color: var(--text-primary);
    background: rgba(24, 49, 77, 0.08);
}

.meta-text,
.meta-time {
    color: var(--text-muted);
    font-size: 0.76rem;
}

.notification-actions {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 10px;
    margin-top: 14px;
}

.action-button {
    min-height: 36px;
    padding: 0 14px;
    border-radius: 12px;
    border: 1px solid rgba(24, 49, 77, 0.12);
    background: rgba(255, 255, 255, 0.9);
    color: var(--text-primary);
    font-size: 0.8rem;
    font-weight: 700;
    transition: transform var(--transition-fast), border-color var(--transition-fast), background var(--transition-fast);
}

.action-button:hover:not(:disabled) {
    transform: translateY(-1px);
    border-color: rgba(15, 124, 130, 0.2);
    background: rgba(240, 248, 250, 0.96);
}

.action-button.primary {
    color: #fff;
    border-color: transparent;
    background: linear-gradient(135deg, #c43131, #e26a3f);
    box-shadow: 0 12px 24px rgba(196, 49, 49, 0.2);
}

.action-button:disabled {
    opacity: 0.65;
    cursor: wait;
}

.action-hint {
    color: var(--text-muted);
    font-size: 0.76rem;
    font-weight: 600;
}

.notification-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    min-height: 220px;
    padding: 24px;
    color: var(--text-muted);
    text-align: center;
}

.notification-empty strong {
    color: var(--text-primary);
    font-family: var(--font-heading);
}

.dropdown-enter-active,
.dropdown-leave-active {
    transition: all 0.18s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
    opacity: 0;
    transform: translateY(-6px);
}

@media (max-width: 1280px) {
    .status-chip {
        display: none;
    }

    .header-copy p {
        max-width: 38ch;
    }
}

@media (max-width: 1023px) {
    .app-header {
        top: 12px;
        left: 12px;
        right: 12px;
        height: var(--header-height);
        padding: 14px 16px;
    }

    .time-chip {
        display: none;
    }

    .header-copy p {
        display: none;
    }
}

@media (max-width: 768px) {
    .header-copy h2 {
        font-size: 1.08rem;
    }

    .header-eyebrow {
        display: none;
    }

    .header-user {
        padding-right: 10px;
    }

    .user-info,
    .user-chevron {
        display: none;
    }

    .notification-dropdown,
    .notification-dropdown.expanded {
        right: 0;
        width: min(100vw - 24px, 540px);
    }

    .notification-topline {
        flex-direction: column;
    }

    .severity-pill {
        align-self: flex-start;
    }
}

@media (max-width: 560px) {
    .notification-list {
        max-height: min(72vh, 560px);
        padding: 12px;
    }

    .notification-item {
        padding: 14px 14px 14px 16px;
    }

    .notification-actions {
        flex-direction: column;
        align-items: stretch;
    }

    .action-button {
        width: 100%;
        justify-content: center;
    }
}

/* ========================================= */
/* HEADER CALL PILLS (Zalo-Style Ringing)   */
/* ========================================= */
.header-call-pill {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    height: 38px;
    padding: 0 12px;
    border-radius: 999px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
    position: relative;
    user-select: none;
}

.incoming-call-pill {
    background: #10b981;
    color: #ffffff;
    box-shadow: 0 4px 14px rgba(16, 185, 129, 0.4);
    animation: pill-glow 1.5s infinite;
}

.call-pulse-ring {
    position: absolute;
    inset: -3px;
    border-radius: 999px;
    border: 2px solid #10b981;
    opacity: 0.8;
    animation: ring-pulse 1.8s ease-out infinite;
}

.call-icon-ringing {
    animation: call-vibrate 0.6s ease-in-out infinite;
}

.call-pill-text {
    display: flex;
    flex-direction: column;
    line-height: 1.1;
    max-width: 110px;
}

.call-pill-text strong {
    font-size: 12px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.call-pill-text span {
    font-size: 10px;
    opacity: 0.9;
}

.call-pill-actions {
    display: flex;
    align-items: center;
    gap: 4px;
    margin-left: 4px;
}

.btn-pill-action {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    border: none;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: transform 0.15s;
}

.btn-pill-action:hover {
    transform: scale(1.15);
}

.btn-pill-action.accept {
    background: #ffffff;
    color: #10b981;
}

.btn-pill-action.decline {
    background: #ef4444;
    color: #ffffff;
}

.active-call-pill {
    background: rgba(15, 124, 130, 0.15);
    border: 1px solid var(--border-focus, #0f7c82);
    color: var(--accent-primary, #0f7c82);
}

.active-call-pill:hover {
    background: rgba(15, 124, 130, 0.25);
    transform: scale(1.03);
}

.active-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: #10b981;
    box-shadow: 0 0 8px #10b981;
}

@keyframes call-vibrate {
    0%, 100% { transform: rotate(0deg); }
    20%, 60% { transform: rotate(-15deg); }
    40%, 80% { transform: rotate(15deg); }
}

@keyframes pill-glow {
    0%, 100% { box-shadow: 0 4px 14px rgba(16, 185, 129, 0.4); }
    50% { box-shadow: 0 4px 22px rgba(16, 185, 129, 0.8); }
}
</style>
