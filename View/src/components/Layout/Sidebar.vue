<template>
    <aside
        ref="sidebarRootRef"
        class="sidebar"
        :class="{
            collapsed,
            'is-mobile': isMobile,
            'mobile-open': mobileOpen,
        }"
    >
        <div class="sidebar-panel" @click="collapsed && !isMobile && $emit('toggle')">
            <div class="sidebar-top">
                <div class="sidebar-logo">
                    <div class="logo-icon">
                        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path
                                d="M12 2L3 7V17L12 22L21 17V7L12 2Z"
                                stroke="currentColor"
                                stroke-width="1.8"
                                stroke-linejoin="round"
                            />
                            <path
                                d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z"
                                fill="currentColor"
                                opacity="0.28"
                            />
                            <path
                                d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z"
                                stroke="currentColor"
                                stroke-width="1.4"
                                stroke-linejoin="round"
                            />
                        </svg>
                    </div>

                    <transition name="fade">
                        <div v-if="!collapsed" class="logo-copy">
                            <span class="logo-title">V-Shield</span>
                            <span class="logo-subtitle">Security Operations</span>
                        </div>
                    </transition>
                </div>

                <button
                    v-if="isMobile"
                    type="button"
                    class="sidebar-mobile-close"
                    aria-label="Đóng điều hướng"
                    @click="$emit('close-mobile')"
                >
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="18" y1="6" x2="6" y2="18" />
                        <line x1="6" y1="6" x2="18" y2="18" />
                    </svg>
                </button>
            </div>

            <nav class="sidebar-nav">
                <transition name="fade">
                    <div v-if="!collapsed" class="sidebar-search" ref="searchContainerRef">
                        <label class="search-label" for="sidebar-search">Tra cứu nhanh</label>
                        <div class="search-shell">
                            <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="11" cy="11" r="8" />
                                <path d="M21 21l-4.35-4.35" />
                            </svg>
                            <input
                                id="sidebar-search"
                                v-model="searchQuery"
                                type="text"
                                placeholder="Nhân sự, khách thăm..."
                                @input="debouncedSearch"
                                @focus="showDropdown = true"
                            />

                            <transition name="dropdown">
                                <div
                                    v-show="showDropdown && (isSearching || searchResults.length > 0 || noResultsFound)"
                                    class="search-dropdown"
                                >
                                    <div v-if="isSearching" class="dropdown-msg">Đang tra cứu dữ liệu...</div>
                                    <div v-else-if="noResultsFound" class="dropdown-msg">Không có kết quả phù hợp</div>
                                    <div v-else class="dropdown-list">
                                        <button
                                            v-for="res in searchResults"
                                            :key="res.id"
                                            type="button"
                                            class="dropdown-item"
                                            @click="handleSearchResultClick(res)"
                                        >
                                            <div class="result-icon">
                                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                                                    <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
                                                    <circle cx="12" cy="7" r="4" />
                                                </svg>
                                            </div>
                                            <div class="result-info">
                                                <div class="result-name">{{ res.name }}</div>
                                                <div class="result-sub">{{ res.sub }}</div>
                                            </div>
                                            <span class="result-badge">{{ res.badge }}</span>
                                        </button>
                                    </div>
                                </div>
                            </transition>
                        </div>
                    </div>
                </transition>

                <div
                    v-for="group in visibleGroups"
                    :key="group.label"
                    class="nav-group"
                    :class="{ 'is-open': isGroupExpanded(group.label) }"
                    :ref="(el) => setGroupAnchor(group.label, el)"
                    @mouseenter="hoverGroup(group.label)"
                    @mouseleave="leaveGroup(group.label)"
                >
                    <button
                        v-if="!collapsed"
                        type="button"
                        class="nav-label-toggle"
                        @click="toggleNavGroup(group.label)"
                    >
                        <span class="nav-label-text">{{ group.label }}</span>
                        <svg
                            class="nav-label-chevron"
                            :class="{ 'chevron-collapsed': !isGroupExpanded(group.label) }"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                        >
                            <path d="M6 9l6 6 6-6" />
                        </svg>
                    </button>
                    <span v-else class="nav-label sr-only">{{ group.label }}</span>

                    <div
                        class="nav-group-items mobile-inline"
                        :class="{ 'group-collapsed': !isGroupExpanded(group.label) && !collapsed }"
                    >
                        <router-link
                            v-for="item in group.items"
                            :key="item.path"
                            :to="item.path"
                            class="nav-item"
                            :class="{ active: route.path === item.path }"
                            @click="handleSidebarNavClick"
                        >
                            <span class="nav-icon" v-html="item.icon"></span>
                            <transition name="fade">
                                <span v-if="!collapsed" class="nav-copy">
                                    <span class="nav-text">{{ item.label }}</span>
                                    <span class="nav-hint">{{ item.hint }}</span>
                                </span>
                            </transition>
                            <transition name="fade">
                                <span v-if="!collapsed && item.badge" class="nav-badge">{{ item.badge }}</span>
                            </transition>
                        </router-link>
                    </div>
                </div>

            </nav>

            <div
                v-if="showDesktopFlyout"
                ref="flyoutRef"
                class="nav-flyout"
                :style="flyoutStyle"
                @mouseenter="hoverGroup(activeFlyoutLabel)"
                @mouseleave="leaveGroup(activeFlyoutLabel)"
            >
                <router-link
                    v-for="item in activeFlyoutItems"
                    :key="`flyout_${item.path}`"
                    :to="item.path"
                    class="nav-item"
                    :class="{ active: route.path === item.path }"
                    @click="handleSidebarNavClick"
                >
                    <span class="nav-icon" v-html="item.icon"></span>
                    <span class="nav-copy">
                        <span class="nav-text">{{ item.label }}</span>
                        <span class="nav-hint">{{ item.hint }}</span>
                    </span>
                    <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
                </router-link>
            </div>

            <button
                v-if="!isMobile && !collapsed"
                type="button"
                class="sidebar-collapse-btn"
                aria-label="Thu gọn"
                @click="$emit('toggle')"
            >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                    <path d="M15 18l-6-6 6-6" />
                </svg>
            </button>
        </div>
    </aside>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState } from '../../stores/auth'
import { getAll as getAllEmployees } from '../../services/employeeApi'
import { getGuestProfiles } from '../../services/guestProfileApi'

const props = defineProps({
    collapsed: Boolean,
    isMobile: Boolean,
    mobileOpen: Boolean,
})

const emit = defineEmits(['toggle', 'close-mobile'])

const router = useRouter()
const route = useRoute()

const hoveredGroup = ref('')
const pinnedGroup = ref('')
const groupAnchors = ref({})
const flyoutViewportTick = ref(0)
const sidebarRootRef = ref(null)
const flyoutRef = ref(null)
let hoverLeaveTimer = null

const getCurrentRouteGroupLabel = () => {
    const foundGroup = visibleGroups.value.find((group) =>
        group.items.some((item) => route.path === item.path)
    )
    if (foundGroup) return foundGroup.label
    return ''
}

const isGroupExpanded = (label) => {
    if (props.collapsed) return true
    const activeLabel = getCurrentRouteGroupLabel()
    return pinnedGroup.value === label || hoveredGroup.value === label || activeLabel === label
}

const toggleNavGroup = (label) => {
    pinnedGroup.value = pinnedGroup.value === label ? '' : label
}

const hoverGroup = (label) => {
    if (hoverLeaveTimer) {
        clearTimeout(hoverLeaveTimer)
        hoverLeaveTimer = null
    }
    hoveredGroup.value = label
}

const leaveGroup = (label) => {
    if (hoverLeaveTimer) clearTimeout(hoverLeaveTimer)
    hoverLeaveTimer = setTimeout(() => {
        if (hoveredGroup.value === label && pinnedGroup.value !== label) {
            hoveredGroup.value = ''
        }
        hoverLeaveTimer = null
    }, 120)
}

const setGroupAnchor = (label, el) => {
    if (el) groupAnchors.value[label] = el
    else delete groupAnchors.value[label]
}

const getGroupItemsByLabel = (label) => {
    if (!label) return []
    const found = visibleGroups.value.find((group) => group.label === label)
    return found?.items || []
}

const activeFlyoutLabel = computed(() => {
    if (props.isMobile || props.collapsed) return ''
    return hoveredGroup.value || pinnedGroup.value
})

const activeFlyoutItems = computed(() => getGroupItemsByLabel(activeFlyoutLabel.value))

const showDesktopFlyout = computed(
    () => !props.isMobile && !props.collapsed && !!activeFlyoutLabel.value && activeFlyoutItems.value.length > 0
)

const flyoutStyle = computed(() => {
    flyoutViewportTick.value
    const label = activeFlyoutLabel.value
    const anchor = groupAnchors.value[label]
    if (!anchor) return {}

    const itemsCount = activeFlyoutItems.value.length
    const viewportPadding = 12
    const estimatedItemHeight = 62
    const estimatedPanelHeight = Math.max(140, itemsCount * estimatedItemHeight + 20)
    const maxPanelHeight = Math.min(window.innerHeight - viewportPadding * 2, 620)
    const clampedPanelHeight = Math.min(estimatedPanelHeight, maxPanelHeight)
    const rect = anchor.getBoundingClientRect()
    const preferredTop = rect.top - 6
    const safeTop = Math.max(
        viewportPadding,
        Math.min(preferredTop, window.innerHeight - clampedPanelHeight - viewportPadding)
    )

    const maxHeightPx = Math.max(180, window.innerHeight - safeTop - viewportPadding)

    return {
        top: `${safeTop}px`,
        left: `${rect.right + 12}px`,
        maxHeight: `${Math.min(maxPanelHeight, maxHeightPx)}px`,
    }
})

const userRole = computed(() => authState.user?.role)
const userTaskKeys = computed(() => authState.user?.operationalTaskKeys || [])
const hasScopedAssignments = computed(() => !!authState.user?.hasOperationalScopeAssignments)
const canAccessNavigationItem = (item) => {
    if (!item.roles) return userRole.value === 'Admin'
    const roleAllowed = item.roles.includes(userRole.value)
    if (!roleAllowed) return false
    if (userRole.value === 'Admin') return true
    if (!item.taskKey || !hasScopedAssignments.value) return true
    return userTaskKeys.value.includes(item.taskKey)
}

const navGroups = ref([
    {
        label: 'Tổng quan',
        items: [
            {
                path: '/dashboard',
                label: 'Dashboard',
                hint: 'Toàn cảnh khi đăng nhập',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>',
                roles: ['Admin', 'QuanLy'],
            },
        ],
    },
    {
        label: 'Tác nghiệp',
        items: [
            {
                path: '/monitoring',
                label: 'Giám sát trực tiếp',
                hint: 'Camera, biển số, access log',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/><circle cx="12" cy="13" r="4"/></svg>',
                badge: 'Live',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'monitoring',
            },
            {
                path: '/camera-archive/0',
                label: 'Lưu trữ camera',
                hint: 'Xem lại bản ghi hình cũ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z"/><path d="M12 11v5"/><path d="M9 14l3 3 3-3"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/gate-transit-monitor',
                label: 'Điều phối thông hành',
                hint: 'QR + biển số theo từng làn',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 7h18"/><path d="M6 7v10"/><path d="M18 7v10"/><path d="M9 11h6"/><path d="M9 15h6"/><path d="M12 7v10"/></svg>',
                badge: '2 làn',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'gate-transit',
            },
            {
                path: '/campus-map',
                label: 'Bản đồ khuôn viên',
                hint: 'Realtime Gate + camera + AccessLog',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 6l6-3 6 3 6-3v15l-6 3-6-3-6 3z"/><path d="M9 3v15"/><path d="M15 6v15"/></svg>',
                roles: ['Admin', 'LeTan'],
                taskKey: 'reception',
            },
            {
                path: '/soc-console',
                label: 'SOC Alarm Console',
                hint: 'Alarm queue & incident command',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="M12 8v4"/><path d="M12 16h.01"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/incident-map',
                label: 'Bản đồ sự cố',
                hint: 'Định vị alarm trên bản đồ + chỉ đường',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="10" r="3"/><path d="M12 21s-8-4-8-10a8 8 0 0116 0c0 6-8 10-8 10z"/></svg>',
                roles: ['Admin', 'BaoVe', 'QuanLy'],
            },
            {
                path: '/qr-access-monitor',
                label: 'Quét xác nhận vào cổng',
                hint: 'Quét QR để xác nhận cho phép vào',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h6v6H4z"/><path d="M14 4h6v6h-6z"/><path d="M4 14h6v6H4z"/><path d="M14 16h2"/><path d="M18 16h2"/><path d="M14 20h6"/><path d="M9 9l2 2 4-4"/></svg>',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'qr-access',
            },
            {
                path: '/dynamic-qr-generator',
                label: 'Tạo QR động',
                hint: 'Sinh mã QR realtime cho nhân viên',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h6v6H4z"/><path d="M14 4h6v6h-6z"/><path d="M4 14h6v6H4z"/><path d="M15 15h2"/><path d="M19 15v5"/><path d="M14 19h5"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/event-timeline',
                label: 'Event Timeline',
                hint: 'Chuỗi sự kiện an ninh',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 3"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="M2 12h2"/><path d="M20 12h2"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/barrier-panel',
                label: 'Barrier Control',
                hint: 'Điều khiển barrier & đỗ xe',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 12H3"/><path d="M12 3v18"/><path d="M7 8l5-5 5 5"/><path d="M7 16l5 5 5-5"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/lane-dashboard',
                label: 'Lane Dashboard',
                hint: 'Sức khỏe làn & barrier',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="4" width="16" height="16" rx="2"/><path d="M4 12h16"/><path d="M12 4v16"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/reception',
                label: 'Lễ tân',
                hint: 'Hỗ trợ khách, tra cứu xe, đồ thất lạc',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/><circle cx="12" cy="10" r="2"/></svg>',
                roles: ['Admin', 'LeTan'],
                taskKey: 'reception',
            },
            {
                path: '/kiosk',
                label: 'Vào cổng thủ công',
                hint: 'QR tê liệt, nhập tay',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'qr-access',
            },
            {
                path: '/parking-kiosk',
                label: 'Gui xe thu cong',
                hint: '2 lan du phong khi camera, QR parking loi',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 16l2-6a2 2 0 0 1 1.9-1.4h10.2A2 2 0 0 1 19 10l2 6"/><path d="M5 16v3"/><path d="M19 16v3"/><path d="M3 16h18"/><circle cx="7.5" cy="16.5" r="1.5"/><circle cx="16.5" cy="16.5" r="1.5"/><path d="M9 8V5"/><path d="M15 8V5"/></svg>',
                badge: '2 lan',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'parking',
            },
            {
                path: '/host-visitor',
                label: 'Mời khách',
                hint: 'Tạo lời mời cho khách',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M22 12h-6"/><path d="M19 9v6"/></svg>',
                roles: ['Admin', 'LeTan'],
                taskKey: 'guest-support',
            },
            {
                path: '/watchlist',
                label: 'Watchlist',
                hint: 'Rà soát đối sánh',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><path d="M12 8v4l3 3"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/ai-review-queue',
                label: 'AI Review Queue',
                hint: 'Chất lượng & đánh giá AI',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M9 13l2 2 4-4"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/video-search',
                label: 'Video Search',
                hint: 'Tìm kiếm video & bookmark',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="10" cy="10" r="8"/><path d="M16 16l5 5"/><rect x="3" y="12" width="14" height="4" rx="1"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
        ],
    },
    {
        label: 'Phê duyệt & Kiểm soát',
        items: [
            {
                path: '/exceptions',
                label: 'Xử lý ngoại lệ',
                hint: 'Bypass và lỗi nhận diện',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><path d="M12 9v4"/><path d="M12 17h.01"/></svg>',
                roles: ['Admin', 'BaoVe', 'QuanLy'],
            },
            {
                path: '/enterprise-security',
                label: 'Enterprise Console',
                hint: 'Workspaces & setup',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/identity-management',
                label: 'Identity Management',
                hint: 'IdP, SSO & offboarding',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><path d="M20 8v6"/><path d="M23 11h-6"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/access-permission-manager',
                label: 'Quyền khu vực giới hạn',
                hint: 'Phân quyền ra vào theo khu vực',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l7 4v6c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-4z"/><path d="M9 12l2 2 4-4"/></svg>',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'restricted-zone',
            },
            {
                path: '/pre-registrations',
                label: 'Danh sách hẹn trước',
                hint: 'Duyệt và theo dõi đăng ký',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 4h2a2 2 0 012 2v14a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h2"/><rect x="8" y="2" width="8" height="4" rx="1.5"/><path d="M9 14l2 2 4-4"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/export-approval-queue',
                label: 'Export Approval',
                hint: 'Phê duyệt xuất evidence',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/redaction-queue',
                label: 'Redaction Queue',
                hint: 'Xóa thông tin nhạy cảm',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 5H9l-7 7 7 7h11a2 2 0 002-2V7a2 2 0 00-2-2z"/><line x1="18" y1="9" x2="12" y2="15"/><line x1="12" y1="9" x2="18" y2="15"/></svg>',
                roles: ['Admin'],
            },
        ],
    },
    {
        label: 'Danh mục',
        items: [
            {
                path: '/employees',
                label: 'Hồ sơ nhân viên',
                hint: 'Nhân sự, phòng ban, chức vụ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>',
                badge: '0',
                roles: ['Admin', 'NhanSu'],
            },
            {
                path: '/vehicles',
                label: 'Phương tiện nội bộ',
                hint: 'Xe đăng ký cố định',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="5" width="16" height="11" rx="2"/><path d="M17 8h4l2 3v5h-6V8z"/><circle cx="5.5" cy="18" r="2.5"/><circle cx="18.5" cy="18" r="2.5"/></svg>',
                roles: ['Admin', 'BaoVe'],
                taskKey: 'parking',
            },
            {
                path: '/guest-profiles',
                label: 'Hồ sơ khách',
                hint: 'Danh bạ khách quen',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M20 8v6"/><path d="M17 11h6"/></svg>',
                roles: ['Admin', 'LeTan'],
                taskKey: 'guest-support',
            },
            {
                path: '/contractors',
                label: 'Nhà thầu',
                hint: 'Quản lý hợp đồng & truy cập',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/registration-links',
                label: 'Link đăng ký tự động',
                hint: 'Token và URL gửi khách',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/site-hierarchy',
                label: 'Site Hierarchy',
                hint: 'Tree, assets & backfill',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>',
                roles: ['Admin', 'QuanLy'],
                taskKey: 'metadata',
            },
            {
                path: '/system-catalog',
                label: 'Danh mục hệ thống',
                hint: 'Phòng ban, chức vụ, ngoại lệ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 6h16"/><path d="M4 12h16"/><path d="M4 18h16"/></svg>',
                roles: ['Admin', 'QuanLy'],
                taskKey: 'metadata',
            },
        ],
    },
    {
        label: 'Tra cứu & Báo cáo',
        items: [
            {
                path: '/access-logs',
                label: 'Tra cứu vào/ra',
                hint: 'Lịch sử theo thời gian',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 8v4l3 3"/><circle cx="12" cy="12" r="9"/></svg>',
                roles: ['Admin', 'BaoVe', 'QuanLy'],
            },
            {
                path: '/ueba',
                label: 'UEBA',
                hint: 'Phân tích hành vi & bất thường',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 3"/><path d="M5 3l4 4"/></svg>',
                roles: ['Admin', 'BaoVe', 'QuanLy'],
            },
            {
                path: '/system-audit-logs',
                label: 'Nhật ký hệ thống',
                hint: 'Ai làm gì, trước/sau, kết quả',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M8 6h13"/><path d="M8 12h13"/><path d="M8 18h13"/><path d="M3 6h.01"/><path d="M3 12h.01"/><path d="M3 18h.01"/></svg>',
                roles: ['Admin', 'QuanLy'],
            },
            {
                path: '/evidence-repository',
                label: 'Evidence Repository',
                hint: 'Quản lý vật chứng & custody',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M16 13H8"/><path d="M16 17H8"/><path d="M10 9H8"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/compliance-reports',
                label: 'Compliance Reports',
                hint: 'Báo cáo tuân thủ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2"/><rect x="9" y="3" width="6" height="4" rx="1"/><path d="M9 14l2 2 4-4"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/correlation-view',
                label: 'Correlation',
                hint: 'Tương quan đa tín hiệu',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/><circle cx="12" cy="12" r="2"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            // --- Lost & Found ---
            {
                path: '/lost-found',
                label: 'Đồ thất lạc',
                hint: 'Quản lý đồ thất lạc & locker',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4.35-4.35"/><path d="M8 11h6"/><path d="M11 8v6"/></svg>',
                roles: ['Admin', 'BaoVe', 'LeTan'],
                taskKey: 'lost-found',
            },
            {
                path: '/found-items',
                label: 'Đồ tìm thấy',
                hint: 'Danh sách đồ tìm thấy',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 6L9 17l-5-5"/><circle cx="12" cy="12" r="9"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/lost-items',
                label: 'Báo mất đồ',
                hint: 'Danh sách báo mất',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4.35-4.35"/></svg>',
                roles: ['Admin', 'BaoVe'],
            },
            {
                path: '/locker-manager',
                label: 'Tủ locker',
                hint: 'Quản lý tủ chứa đồ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18"/><path d="M9 9h6"/><path d="M9 15h6"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/claim-approval',
                label: 'Duyệt trả đồ',
                hint: 'Xử lý yêu cầu nhận lại',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 6L9 17l-5-5"/></svg>',
                roles: ['Admin'],
            },
        ],
    },
    {
        label: 'Chấm công',
        items: [
            {
                path: '/attendance/records',
                label: 'Bảng chấm công',
                hint: 'Check-in/check-out, trễ/sớm',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/attendance/work-schedules',
                label: 'Lịch làm việc',
                hint: 'Lên lịch ca cho nhân viên',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="4" width="18" height="17" rx="2"/><path d="M8 2v4"/><path d="M16 2v4"/><path d="M3 10h18"/></svg>',
                roles: ['Admin', 'QuanLy'],
                taskKey: 'metadata',
            },
            {
                path: '/attendance/shifts',
                label: 'Ca làm việc',
                hint: 'Cấu hình ca và thời gian',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 6v6l4 2"/><circle cx="12" cy="12" r="9"/></svg>',
                roles: ['Admin', 'QuanLy'],
                taskKey: 'metadata',
            },
            {
                path: '/attendance/leave-requests',
                label: 'Đơn xin nghỉ',
                hint: 'Gửi và theo dõi đơn nghỉ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 6L9 17l-5-5"/></svg>',
                roles: ['Admin', 'NhanVien'],
            },
            {
                path: '/attendance/leave-approvals',
                label: 'Duyệt đơn nghỉ',
                hint: 'Xử lý đơn chờ duyệt',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z"/><path d="M9 12l2 2 4-4"/></svg>',
                roles: ['Admin', 'QuanLy', 'NhanSu'],
                taskKey: 'approvals',
            },
            {
                path: '/attendance/reports',
                label: 'Báo cáo công',
                hint: 'Thống kê theo ngày/tháng',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 3v18h18"/><path d="M7 15l4-4 3 3 4-6"/></svg>',
                roles: ['Admin', 'QuanLy'],
                taskKey: 'reports',
            },
        ],
    },
    {
        label: 'Nhân viên',
        items: [
            {
                path: '/chat',
                label: 'Liên lạc nội bộ',
                hint: 'Chat và gọi điện nội bộ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/><path d="M8 9h8"/><path d="M8 13h6"/></svg>',
                roles: ['Admin', 'NhanVien', 'NhanSu', 'QuanLy', 'BaoVe', 'LeTan'],
            },
            {
                path: '/my-dynamic-qr',
                label: 'QR cá nhân',
                hint: 'Mã QR động ra vào cổng',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h6v6H4z"/><path d="M14 4h6v6h-6z"/><path d="M4 14h6v6H4z"/><path d="M15 15h2"/><path d="M19 15v5"/><path d="M14 19h5"/></svg>',
                roles: ['NhanVien'],
            },
            {
                path: '/my-vehicles',
                label: 'Xe của tôi',
                hint: 'Xe đang gửi trong bãi',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="5" width="16" height="11" rx="2"/><path d="M17 8h4l2 3v5h-6V8z"/><circle cx="5.5" cy="18" r="2.5"/><circle cx="18.5" cy="18" r="2.5"/></svg>',
                roles: ['NhanVien'],
            },
            {
                path: '/my-schedule',
                label: 'Lịch làm việc',
                hint: 'Ca trực và lịch cá nhân',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="4" width="18" height="17" rx="2"/><path d="M8 2v4"/><path d="M16 2v4"/><path d="M3 10h18"/></svg>',
                roles: ['NhanVien'],
            },
            {
                path: '/attendance/leave-requests',
                label: 'Đơn xin nghỉ',
                hint: 'Gửi và theo dõi đơn nghỉ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 6L9 17l-5-5"/></svg>',
                roles: ['NhanVien'],
            },
            {
                path: '/vehicle-transfer',
                label: 'Chuyển nhượng xe',
                hint: 'Ủy quyền xe cho người khác',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>',
                roles: ['NhanVien'],
            },
            {
                path: '/profile',
                label: 'Thông tin cá nhân',
                hint: 'Hồ sơ và đổi mật khẩu',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
                roles: ['NhanVien', 'NhanSu'],
            },
        ],
    },
    {
        label: 'Nhân sự',
        items: [
            {
                path: '/employees',
                label: 'Hồ sơ nhân viên',
                hint: 'Nhân sự, phòng ban, chức vụ',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>',
                roles: ['NhanSu'],
            },
            {
                path: '/attendance/leave-approvals',
                label: 'Duyệt đơn nghỉ',
                hint: 'Xử lý đơn chờ duyệt',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z"/><path d="M9 12l2 2 4-4"/></svg>',
                roles: ['NhanSu'],
                taskKey: 'approvals',
            },
            {
                path: '/users',
                label: 'Tài khoản & phân quyền',
                hint: 'Khóa/mở tài khoản người dùng',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 15a3 3 0 100-6 3 3 0 000 6z"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></svg>',
                roles: ['NhanSu'],
            },
        ],
    },
    {
        label: 'Thiết bị & Hệ thống',
        items: [
            {
                path: '/device-management',
                label: 'Camera & cổng',
                hint: 'Cấu hình thiết bị truy cập',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 7h16v10H4z"/><path d="M9 7V4h6v3"/><path d="M8 17h8"/><path d="M7 21h10"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/device-topology',
                label: 'Device Topology',
                hint: 'Sơ đồ thiết bị enterprise',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M12 9V6"/><path d="M12 18v-3"/><path d="M9 12H6"/><path d="M18 12h-3"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/device-health',
                label: 'Device Health',
                hint: 'Sức khỏe & AI diagnosis',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/provisioning-wizard',
                label: 'Provisioning',
                hint: 'Cấp phát thiết bị mới',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z"/><path d="M9 12l2 2 4-4"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/offline-packages',
                label: 'Offline Packages',
                hint: 'Policy cho offline resilience',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 002 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/simulator-panel',
                label: 'Simulator',
                hint: 'Virtual controller & fault injection',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>',
                roles: ['Admin'],
            },
            {
                path: '/users',
                label: 'Tài khoản & phân quyền',
                hint: 'Người dùng phần mềm',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 15a3 3 0 100-6 3 3 0 000 6z"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></svg>',
                roles: ['Admin', 'NhanSu'],
            },
            {
                path: '/settings?tab=camera',
                label: 'Quản trị camera',
                hint: 'Quản lý tất cả camera giám sát',
                icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h6v6H4z"/><path d="M14 4h6v6h-6z"/><path d="M4 14h6v6H4z"/><path d="M15 15h2"/><path d="M19 15v5"/><path d="M14 19h5"/></svg>',
                roles: ['Admin'],
            },
        ],
    },
])
const HIDDEN_NAV_PATHS = new Set([
    '/event-timeline',
    '/lane-dashboard',
    '/found-items',
    '/lost-items',
    '/claim-approval',
    '/locker-manager',
])

const visibleGroups = computed(() =>
    navGroups.value
        .map((group) => ({
            ...group,
            items: group.items.filter(
                (item) => canAccessNavigationItem(item) && !HIDDEN_NAV_PATHS.has(item.path)
            ),
        }))
        .filter((group) => group.items.length > 0)
)

onMounted(async () => {
    document.addEventListener('click', handleSearchOutsideClick)
    document.addEventListener('mousedown', handleNavOutsideClick)
    window.addEventListener('resize', refreshFlyoutPosition, { passive: true })
    window.addEventListener('scroll', refreshFlyoutPosition, { passive: true })

    try {
        const employeesRes = await getAllEmployees()
        const employeesItem = navGroups.value
            .flatMap((group) => group.items)
            .find((item) => item.path === '/employees')
        if (employeesItem) {
            employeesItem.badge = String(employeesRes.data.length)
        }
    } catch (error) {
        console.error('Lỗi khi tải badge điều hướng:', error)
    }
})

onUnmounted(() => {
    document.removeEventListener('click', handleSearchOutsideClick)
    document.removeEventListener('mousedown', handleNavOutsideClick)
    window.removeEventListener('resize', refreshFlyoutPosition)
    window.removeEventListener('scroll', refreshFlyoutPosition)
    if (hoverLeaveTimer) {
        clearTimeout(hoverLeaveTimer)
        hoverLeaveTimer = null
    }
})

watch(
    () => route.fullPath,
    () => {
        showDropdown.value = false
        hoveredGroup.value = ''
        pinnedGroup.value = ''
        refreshFlyoutPosition()
        if (props.isMobile) {
            emit('close-mobile')
        }
    }
)

const searchQuery = ref('')
const showDropdown = ref(false)
const isSearching = ref(false)
const searchResults = ref([])
const noResultsFound = ref(false)
const searchContainerRef = ref(null)

let quickSearchDebounceTimer = null

const debouncedSearch = () => {
    if (quickSearchDebounceTimer) clearTimeout(quickSearchDebounceTimer)

    if (!searchQuery.value.trim()) {
        searchResults.value = []
        isSearching.value = false
        noResultsFound.value = false
        showDropdown.value = false
        return
    }

    showDropdown.value = true
    isSearching.value = true
    noResultsFound.value = false

    quickSearchDebounceTimer = setTimeout(async () => {
        try {
            const keyword = searchQuery.value.trim()
            const [employeesRes, guestsRes] = await Promise.all([
                getAllEmployees({ search: keyword }),
                getGuestProfiles({ query: keyword, page: 1, pageSize: 6 }),
            ])

            const results = []

            if (employeesRes.data?.length) {
                employeesRes.data.forEach((employee) => {
                    results.push({
                        id: `emp_${employee.employeeId}`,
                        type: 'employee',
                        name: employee.fullName,
                        sub: employee.departmentName || 'Chưa gán phòng ban',
                        badge: 'Nhân sự',
                    })
                })
            }

            if (guestsRes.data?.items?.length) {
                guestsRes.data.items.forEach((guest) => {
                    results.push({
                        id: `guest_${guest.guestId}`,
                        type: 'guest',
                        name: guest.fullName,
                        sub: guest.phone || guest.defaultLicensePlate || 'Hồ sơ khách',
                        badge: 'Khách',
                    })
                })
            }

            searchResults.value = results
            noResultsFound.value = results.length === 0
        } catch (error) {
            console.error('Search error:', error)
            searchResults.value = []
            noResultsFound.value = true
        } finally {
            isSearching.value = false
        }
    }, 320)
}

const handleSearchResultClick = (result) => {
    if (result.type === 'employee') {
        router.push({ path: '/employees', query: { search: result.name } })
    } else if (result.type === 'guest') {
        router.push({ path: '/guest-profiles', query: { search: result.name } })
    }

    showDropdown.value = false
    searchQuery.value = ''
    searchResults.value = []

    if (props.isMobile) {
        emit('close-mobile')
    }
}

const handleSearchOutsideClick = (event) => {
    if (searchContainerRef.value && !searchContainerRef.value.contains(event.target)) {
        showDropdown.value = false
    }
}

const handleSidebarNavClick = () => {
    hoveredGroup.value = ''
    pinnedGroup.value = ''
    if (props.isMobile) {
        emit('close-mobile')
    }
}

const handleNavOutsideClick = (event) => {
    if (props.isMobile || props.collapsed) return
    const target = event.target
    const inSidebar = sidebarRootRef.value?.contains(target)
    const inFlyout = flyoutRef.value?.contains(target)

    if (!inSidebar && !inFlyout) {
        hoveredGroup.value = ''
        pinnedGroup.value = ''
    }
}

const refreshFlyoutPosition = () => {
    flyoutViewportTick.value += 1
}
</script>

<style scoped>
.sidebar {
    position: fixed;
    inset: 0 auto 0 0;
    width: var(--sidebar-width);
    z-index: 90;
    padding: 18px 0 18px 18px;
    transition: transform var(--transition-slow), width var(--transition-slow);
}

.sidebar-panel {
    position: relative;
    height: 100%;
    display: flex;
    flex-direction: column;
    background: linear-gradient(180deg, var(--bg-sidebar) 0%, var(--bg-sidebar-raised) 100%);
    border: 1px solid var(--sidebar-border);
    border-radius: 28px;
    box-shadow: 0 30px 60px rgba(7, 16, 27, 0.34);
    overflow: visible;
    overflow-y: auto;
}

.sidebar.collapsed {
    width: 52px;
    height: 52px;
    inset: 18px auto auto 18px;
    padding: 0;
    overflow: visible;
    cursor: pointer;
}

.sidebar.collapsed .sidebar-panel {
    width: 52px;
    height: 52px;
    border-radius: 50%;
    overflow: hidden;
    justify-content: center;
    align-items: center;
    box-shadow: 0 4px 16px rgba(3, 8, 14, 0.45), 0 0 0 1px rgba(84, 196, 211, 0.15);
    transition: all var(--transition-slow);
}

.sidebar.collapsed .sidebar-panel:hover {
    box-shadow: 0 6px 24px rgba(3, 8, 14, 0.55), 0 0 0 1.5px rgba(84, 196, 211, 0.3);
    transform: scale(1.06);
}

.sidebar.collapsed .sidebar-top {
    padding: 0;
    height: 52px;
    justify-content: center;
}

.sidebar.collapsed .sidebar-logo .logo-copy,
.sidebar.collapsed .sidebar-nav,
.sidebar.collapsed .sidebar-collapse-btn {
    display: none;
}

.sidebar-top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 22px 18px 16px;
}

.sidebar-logo {
    display: flex;
    align-items: center;
    gap: 14px;
    min-width: 0;
}

.logo-icon {
    width: 42px;
    height: 42px;
    flex-shrink: 0;
    border-radius: 14px;
    background: linear-gradient(135deg, rgba(84, 196, 211, 0.18), rgba(43, 109, 138, 0.28));
    color: #d7fbff;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 12px 28px rgba(84, 196, 211, 0.18);
}

.logo-icon svg {
    width: 26px;
    height: 26px;
}

.logo-copy {
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.logo-title {
    font-family: var(--font-heading);
    font-size: 1.18rem;
    font-weight: 700;
    color: var(--sidebar-text);
    line-height: 1.1;
}

.logo-subtitle {
    color: var(--sidebar-text-muted);
    font-size: 0.78rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.sidebar-mobile-close {
    width: 40px;
    height: 40px;
    flex-shrink: 0;
    display: none;
    align-items: center;
    justify-content: center;
    border-radius: 12px;
    color: var(--sidebar-text);
    background: rgba(255, 255, 255, 0.06);
    border: 1px solid rgba(255, 255, 255, 0.08);
}

.sidebar-mobile-close svg {
    width: 18px;
    height: 18px;
}

.sidebar-nav {
    flex: 1;
    padding: 0 12px 16px;
    overflow: visible;
}

.sidebar-search {
    margin-bottom: 18px;
}

.search-label {
    display: block;
    margin-bottom: 10px;
    color: var(--sidebar-text-muted);
    font-size: 0.74rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.search-shell {
    position: relative;
}

.search-icon {
    position: absolute;
    left: 14px;
    top: 50%;
    transform: translateY(-50%);
    width: 16px;
    height: 16px;
    color: var(--sidebar-text-muted);
}

.search-shell input {
    width: 100%;
    min-height: 46px;
    padding: 0 16px 0 40px;
    border-radius: 16px;
    border: 1px solid rgba(255, 255, 255, 0.08);
    background: rgba(255, 255, 255, 0.05);
    color: var(--sidebar-text);
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast);
}

.search-shell input::placeholder {
    color: rgba(188, 209, 218, 0.64);
}

.search-shell input:focus {
    border-color: rgba(84, 196, 211, 0.38);
    background: rgba(255, 255, 255, 0.07);
    box-shadow: 0 0 0 4px rgba(84, 196, 211, 0.12);
}

.nav-group + .nav-group {
    margin-top: 18px;
}

.nav-group {
    position: relative;
}

.nav-label {
    display: block;
    padding: 0 12px 10px;
    color: var(--sidebar-text-muted);
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
}

.nav-label-toggle {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    color: var(--sidebar-text-muted);
    font-size: 0.7rem;
    font-weight: 800;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    background: none;
    border: 1px solid transparent;
    border-radius: 12px;
    cursor: pointer;
    transition: color var(--transition-fast), background var(--transition-fast), border-color var(--transition-fast);
}

.nav-label-toggle:hover {
    color: var(--sidebar-text);
    background: rgba(255, 255, 255, 0.06);
}

.nav-group.is-open .nav-label-toggle {
    color: #d6f5ff;
    background: rgba(84, 196, 211, 0.12);
    border-color: rgba(84, 196, 211, 0.25);
}

.nav-label-text {
    pointer-events: none;
}

.nav-label-chevron {
    width: 14px;
    height: 14px;
    flex-shrink: 0;
    transition: transform 0.25s ease;
}

.nav-label-chevron.chevron-collapsed {
    transform: rotate(-90deg);
}

.nav-group-items {
    position: absolute;
    top: 0;
    left: calc(100% + 12px);
    width: min(320px, 62vw);
    max-height: min(72vh, 620px);
    overflow: auto;
    padding: 10px;
    border-radius: 16px;
    border: 1px solid rgba(84, 196, 211, 0.22);
    background: rgba(11, 25, 39, 0.97);
    box-shadow: 0 18px 40px rgba(3, 8, 14, 0.45);
    backdrop-filter: blur(8px);
    transition: transform 0.2s ease, opacity 0.2s ease;
    opacity: 1;
    transform: translateX(0);
    margin-top: 0;
    z-index: 65;
}

.nav-group-items.group-collapsed {
    opacity: 0;
    transform: translateX(-8px);
    pointer-events: none;
}

.nav-group-items.mobile-inline {
    display: none;
}

.nav-flyout {
    position: fixed;
    width: min(320px, 62vw);
    max-height: min(72vh, 620px);
    overflow-x: hidden;
    overflow-y: auto;
    overscroll-behavior: contain;
    padding: 10px;
    border-radius: 16px;
    border: 1px solid rgba(84, 196, 211, 0.22);
    background: rgba(11, 25, 39, 0.97);
    box-shadow: 0 18px 40px rgba(3, 8, 14, 0.45);
    backdrop-filter: blur(8px);
    z-index: 120;
}

.nav-item {
    position: relative;
    display: flex;
    align-items: center;
    gap: 12px;
    min-height: 54px;
    margin: 6px 0;
    padding: 10px 12px;
    border-radius: 18px;
    color: var(--sidebar-text-muted);
    transition: background var(--transition-fast), border-color var(--transition-fast), transform var(--transition-fast), color var(--transition-fast);
    border: 1px solid transparent;
}

.nav-item:hover {
    transform: translateY(-1px);
    background: var(--sidebar-hover);
    color: var(--sidebar-text);
}

.nav-item.active {
    background: var(--sidebar-active);
    border-color: var(--sidebar-active-border);
    color: var(--sidebar-text);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.06);
}

.nav-item.active::before {
    content: '';
    position: absolute;
    left: 7px;
    top: 10px;
    bottom: 10px;
    width: 3px;
    border-radius: 999px;
    background: #8ceaf4;
}

.nav-icon {
    width: 22px;
    height: 22px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.nav-icon :deep(svg) {
    width: 100%;
    height: 100%;
}

.nav-copy {
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.nav-text {
    color: currentColor;
    font-size: 0.92rem;
    font-weight: 600;
}

.nav-hint {
    color: rgba(188, 209, 218, 0.68);
    font-size: 0.74rem;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.nav-badge {
    margin-left: auto;
    padding: 4px 10px;
    border-radius: 999px;
    background: rgba(84, 196, 211, 0.15);
    color: #b6f6ff;
    font-size: 0.72rem;
    font-weight: 700;
    flex-shrink: 0;
}

.sidebar-collapse-btn {
    position: absolute;
    top: 50%;
    right: -17px;
    transform: translateY(-50%);
    z-index: 110;
    width: 34px;
    height: 34px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--bg-sidebar-raised);
    border: 1px solid var(--sidebar-border);
    color: var(--sidebar-text-muted);
    cursor: pointer;
    box-shadow: 0 4px 14px rgba(3, 8, 14, 0.4), 0 0 0 1px rgba(84, 196, 211, 0.08);
    transition: background var(--transition-fast), color var(--transition-fast), box-shadow var(--transition-fast), transform var(--transition-fast);
}

.sidebar-collapse-btn:hover {
    background: var(--bg-sidebar);
    color: #d6f5ff;
    box-shadow: 0 6px 20px rgba(3, 8, 14, 0.5), 0 0 0 1px rgba(84, 196, 211, 0.2);
    transform: translateY(-50%) scale(1.08);
}

.sidebar-collapse-btn svg {
    width: 16px;
    height: 16px;
    transition: transform var(--transition-slow);
}

.sidebar-collapse-btn.collapsed svg {
    transform: rotate(180deg);
}

.search-dropdown {
    position: absolute;
    top: calc(100% + 8px);
    left: 0;
    right: 0;
    z-index: 10;
    border-radius: 18px;
    overflow: hidden;
    border: 1px solid rgba(255, 255, 255, 0.08);
    background: rgba(11, 25, 39, 0.98);
    box-shadow: 0 18px 40px rgba(3, 8, 14, 0.45);
}

.dropdown-msg {
    padding: 14px 16px;
    color: var(--sidebar-text-muted);
    font-size: 0.84rem;
    text-align: center;
}

.dropdown-list {
    display: flex;
    flex-direction: column;
}

.dropdown-item {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 14px;
    text-align: left;
    color: var(--sidebar-text);
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    transition: background var(--transition-fast);
}

.dropdown-item:last-child {
    border-bottom: none;
}

.dropdown-item:hover {
    background: rgba(84, 196, 211, 0.08);
}

.result-icon {
    width: 38px;
    height: 38px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 12px;
    color: #c2f8ff;
    background: rgba(84, 196, 211, 0.12);
}

.result-icon svg {
    width: 18px;
    height: 18px;
}

.result-info {
    min-width: 0;
    flex: 1;
}

.result-name {
    color: var(--sidebar-text);
    font-size: 0.88rem;
    font-weight: 600;
}

.result-sub {
    color: var(--sidebar-text-muted);
    font-size: 0.76rem;
    margin-top: 3px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.result-badge {
    padding: 4px 9px;
    border-radius: 999px;
    background: rgba(93, 227, 199, 0.12);
    color: #9ff4e2;
    font-size: 0.7rem;
    font-weight: 700;
}

.fade-enter-active,
.fade-leave-active,
.dropdown-enter-active,
.dropdown-leave-active {
    transition: all 0.18s ease;
}

.fade-enter-from,
.fade-leave-to,
.dropdown-enter-from,
.dropdown-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}

.sr-only {
    opacity: 0;
    pointer-events: none;
}

@media (max-width: 1023px) {
    .sidebar {
        width: min(320px, calc(100vw - 24px));
        padding: 12px;
        transform: translateX(calc(-100% - 16px));
    }

    .sidebar.mobile-open {
        transform: translateX(0);
    }

    .sidebar-mobile-close {
        display: inline-flex;
    }

    .sidebar-panel {
        overflow: hidden;
    }

    .nav-group-items {
        position: static;
        width: 100%;
        max-height: none;
        padding: 0;
        border: none;
        border-radius: 0;
        background: transparent;
        box-shadow: none;
        backdrop-filter: none;
        margin-top: 8px;
        opacity: 1 !important;
        transform: none !important;
        pointer-events: auto !important;
    }

    .nav-group-items.mobile-inline {
        display: block;
    }

    .nav-flyout {
        display: none;
    }

    .nav-group-items.group-collapsed {
        max-height: 0;
        overflow: hidden;
    }

    .sidebar-collapse-btn {
        display: none;
    }
}
</style>


