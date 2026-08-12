<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <h1>Quản lý đồ thất lạc</h1>
                <p class="page-subtitle">Tất cả khâu tiếp nhận, lưu kho, xác minh và trao trả nằm trong một tính năng.</p>
            </div>
        </div>

        <section class="tab-shell">
            <button
                v-for="tab in visibleTabs"
                :key="tab.key"
                class="tab-pill"
                :class="{ active: activeTab === tab.key }"
                @click="selectTab(tab.key)"
            >
                {{ tab.label }}
            </button>
        </section>

        <section v-if="activeTab === 'overview'" class="ops-grid four">
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.pendingLostItems }}</div>
                <div class="summary-label">Báo mất đang mở</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.unclaimedFoundItems }}</div>
                <div class="summary-label">Đồ đang lưu kho</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.pendingClaims }}</div>
                <div class="summary-label">Yêu cầu chờ duyệt</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.occupiedCompartments }}/{{ stats.availableCompartments + stats.occupiedCompartments }}</div>
                <div class="summary-label">Ngăn locker đang dùng</div>
            </article>
        </section>

        <section v-if="activeTab === 'overview'" class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Gợi ý ghép nối tự động</h3>
                    <button class="btn btn-sm" @click="loadSuggestions">Tìm gợi ý</button>
                </div>
                <table class="data-table" v-if="suggestions.length">
                    <thead><tr><th>Đồ mất</th><th>Đồ tìm thấy</th><th>Độ tin cậy</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        <tr v-for="s in suggestions" :key="s.itemMatchId || s.lostItemReportId + '-' + s.foundItemReportId">
                            <td class="text-truncate" style="max-width:180px">{{ s.lostItem?.itemDescription || 'N/A' }}</td>
                            <td class="text-truncate" style="max-width:180px">{{ s.foundItem?.itemDescription || 'N/A' }}</td>
                            <td>{{ (s.confidenceScore * 100).toFixed(0) }}%</td>
                            <td>
                                <button class="btn btn-sm btn-success" @click="confirmSuggestion(s)">Ghép</button>
                                <button class="btn btn-sm btn-danger" @click="rejectSuggestion(s)">Bỏ qua</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-else class="empty-state">Nhấn "Tìm gợi ý" để hệ thống đề xuất ghép nối.</div>
            </article>
        </section>

        <section v-if="activeTab !== 'overview'" class="embedded-page">
            <component :is="activeComponent" />
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState } from '../stores/auth'
import { lostFoundApi } from '../services/enterpriseSecurityApi'
import FoundItemRegistry from './FoundItemRegistry.vue'
import LostItemList from './LostItemList.vue'
import ClaimApproval from './ClaimApproval.vue'
import LockerAccessLogs from './LockerAccessLogs.vue'
import LockerManager from './LockerManager.vue'

const route = useRoute()
const router = useRouter()

const stats = reactive({
    pendingLostItems: 0,
    unclaimedFoundItems: 0,
    suggestedMatches: 0,
    pendingClaims: 0,
    totalCabinets: 0,
    availableCompartments: 0,
    occupiedCompartments: 0
})
const suggestions = ref([])

const allTabs = [
    { key: 'overview', label: 'Tổng quan', roles: ['Admin', 'BaoVe', 'LeTan'] },
    { key: 'found', label: 'Đồ tìm thấy', roles: ['Admin', 'BaoVe', 'LeTan'] },
    { key: 'lost', label: 'Báo mất đồ', roles: ['Admin', 'BaoVe', 'LeTan'] },
    { key: 'claim', label: 'Trao trả', roles: ['Admin', 'BaoVe', 'LeTan'] },
    { key: 'locker', label: 'Nhật ký locker', roles: ['Admin', 'BaoVe'] },
    { key: 'locker-config', label: 'Tủ locker', roles: ['Admin'] }
]

const componentMap = {
    found: FoundItemRegistry,
    lost: LostItemList,
    claim: ClaimApproval,
    locker: LockerAccessLogs,
    'locker-config': LockerManager
}

const userRole = computed(() => authState.user?.role)
const visibleTabs = computed(() => allTabs.filter((tab) => tab.roles.includes(userRole.value)))
const defaultTab = computed(() => visibleTabs.value[0]?.key || 'overview')
const activeTab = ref(resolveTab(route.query.tab))
const activeComponent = computed(() => componentMap[activeTab.value] || null)

onMounted(async () => {
    await loadOverview()
    if (activeTab.value === 'overview') {
        await loadSuggestions()
    }
})

watch(
    () => route.query.tab,
    async (value) => {
        activeTab.value = resolveTab(value)
        if (activeTab.value === 'overview') {
            await loadOverview()
            await loadSuggestions()
        }
    }
)

async function loadOverview() {
    try {
        const res = await lostFoundApi.getOverview()
        Object.assign(stats, res.data)
    } catch (e) {
        console.error('Failed to load overview:', e)
    }
}

async function loadSuggestions() {
    try {
        const res = await lostFoundApi.getMatchSuggestions()
        suggestions.value = res.data || []
    } catch (e) {
        console.error('Failed to load suggestions:', e)
    }
}

async function confirmSuggestion(s) {
    try {
        if (!s.itemMatchId) {
            const res = await lostFoundApi.createMatch({
                lostItemReportId: s.lostItemReportId,
                foundItemReportId: s.foundItemReportId,
                confidenceScore: s.confidenceScore,
                note: s.note
            })
            await lostFoundApi.confirmMatch(res.data.itemMatchId)
        } else {
            await lostFoundApi.confirmMatch(s.itemMatchId)
        }
        await Promise.all([loadOverview(), loadSuggestions()])
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

async function rejectSuggestion(s) {
    try {
        if (s.itemMatchId) {
            await lostFoundApi.rejectMatch(s.itemMatchId)
        }
        await loadSuggestions()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

function selectTab(tabKey) {
    activeTab.value = tabKey
    router.replace({ path: '/lost-found', query: tabKey === 'overview' ? {} : { tab: tabKey } })
}

function resolveTab(rawTab) {
    const requested = typeof rawTab === 'string' ? rawTab : defaultTab.value
    const allowed = visibleTabs.value.some((tab) => tab.key === requested)
    return allowed ? requested : defaultTab.value
}
</script>

<style scoped>
.page-subtitle { margin-top: 0.35rem; color: var(--text-secondary); }
.tab-shell { display: flex; flex-wrap: wrap; gap: 0.75rem; margin: 1rem 0 1.25rem; }
.tab-pill {
    border: 1px solid var(--border-default);
    background: var(--surface-default);
    color: var(--text-primary);
    border-radius: 999px;
    padding: 0.7rem 1rem;
    font-weight: 600;
    transition: background var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast), transform var(--transition-fast);
}
.tab-pill:hover:not(.active) {
    background: var(--surface-hover);
    border-color: var(--border-strong);
    transform: translateY(-1px);
}
.tab-pill.active {
    background: var(--accent-primary);
    color: var(--text-on-interactive);
    border-color: var(--accent-primary);
}
.summary-card { text-align: center; padding: 1.5rem; }
.summary-value { font-size: 2.2rem; font-weight: 700; color: var(--accent-primary); }
.summary-label { font-size: 0.85rem; color: var(--text-secondary); margin-top: 0.25rem; }
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.embedded-page :deep(.page-container) { padding: 0; }
</style>
