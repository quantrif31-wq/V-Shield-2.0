<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Quản lý Đồ Thất Lạc & Tang Vật</h1>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showFoundForm = true">+ Nhập đồ tìm thấy</button>
                <button class="btn btn-secondary" @click="showLostForm = true">+ Báo mất đồ</button>
            </div>
        </div>

        <section class="ops-grid four">
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.pendingLostItems }}</div>
                <div class="summary-label">Đơn báo mất</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.unclaimedFoundItems }}</div>
                <div class="summary-label">Đồ chưa trả</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.suggestedMatches }}</div>
                <div class="summary-label">Gợi ý ghép</div>
            </article>
            <article class="ops-panel summary-card">
                <div class="summary-value">{{ stats.occupiedCompartments }}/{{ stats.availableCompartments + stats.occupiedCompartments }}</div>
                <div class="summary-label">Tủ locker đã dùng</div>
            </article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Đồ thất lạc gần đây</h3>
                    <router-link to="/lost-items" class="link">Xem tất cả</router-link>
                </div>
                <table class="data-table" v-if="lostItems.length">
                    <thead><tr><th>Người báo</th><th>Mô tả</th><th>Ngày mất</th><th>Trạng thái</th></tr></thead>
                    <tbody>
                        <tr v-for="item in lostItems" :key="item.lostItemReportId">
                            <td>{{ item.reporterName }}</td>
                            <td class="text-truncate" style="max-width:200px">{{ item.itemDescription }}</td>
                            <td>{{ formatDate(item.lostAtUtc) }}</td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                        </tr>
                    </tbody>
                </table>
                <div v-else class="empty-state">Chưa có báo mất nào.</div>
            </article>
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Đồ tìm thấy gần đây</h3>
                    <router-link to="/found-items" class="link">Xem tất cả</router-link>
                </div>
                <table class="data-table" v-if="foundItems.length">
                    <thead><tr><th>Người tìm</th><th>Mô tả</th><th>Nơi tìm</th><th>Trạng thái</th></tr></thead>
                    <tbody>
                        <tr v-for="item in foundItems" :key="item.foundItemReportId">
                            <td>{{ item.foundByName }}</td>
                            <td class="text-truncate" style="max-width:200px">{{ item.itemDescription }}</td>
                            <td>{{ item.foundLocation }}</td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                        </tr>
                    </tbody>
                </table>
                <div v-else class="empty-state">Chưa có đồ tìm thấy nào.</div>
            </article>
        </section>

        <section class="ops-grid one">
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
                <div v-else class="empty-state">Nhấn "Tìm gợi ý" để hệ thống tự động gợi ý ghép nối.</div>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showFoundForm" class="modal-overlay" @click.self="showFoundForm = false">
                <div class="modal-panel">
                    <h3>Nhập đồ tìm thấy</h3>
                    <div class="form-group">
                        <label>Người tìm thấy</label>
                        <input v-model="foundForm.foundByName" class="form-control" />
                    </div>
                    <div class="form-group">
                        <label>Mô tả đồ vật</label>
                        <textarea v-model="foundForm.itemDescription" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Nơi tìm thấy</label>
                            <input v-model="foundForm.foundLocation" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Tủ lưu trữ</label>
                            <select v-model="foundForm.lockerCompartmentId" class="form-control">
                                <option :value="null">-- Chọn ngăn tủ --</option>
                                <option v-for="c in availableCompartments" :key="c.lockerCompartmentId" :value="c.lockerCompartmentId">
                                    {{ c.cabinet?.name || 'Tủ' }} - {{ c.code }}
                                </option>
                            </select>
                        </div>
                    </div>
                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitFoundItem" :disabled="submitting">
                            {{ submitting ? 'Đang lưu...' : 'Lưu' }}
                        </button>
                        <button class="btn btn-secondary" @click="showFoundForm = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showLostForm" class="modal-overlay" @click.self="showLostForm = false">
                <div class="modal-panel">
                    <h3>Báo mất đồ</h3>
                    <div class="form-group">
                        <label>Người báo</label>
                        <input v-model="lostForm.reporterName" class="form-control" />
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại</label>
                            <input v-model="lostForm.reporterPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Email</label>
                            <input v-model="lostForm.reporterEmail" class="form-control" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Mô tả đồ vật</label>
                        <textarea v-model="lostForm.itemDescription" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Nơi mất gần nhất</label>
                            <input v-model="lostForm.lastSeenLocation" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Thời gian mất</label>
                            <input type="datetime-local" v-model="lostForm.lostAtUtc" class="form-control" />
                        </div>
                    </div>
                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitLostItem" :disabled="submitting">
                            {{ submitting ? 'Đang lưu...' : 'Lưu' }}
                        </button>
                        <button class="btn btn-secondary" @click="showLostForm = false">Hủy</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const stats = reactive({
    pendingLostItems: 0, unclaimedFoundItems: 0, suggestedMatches: 0,
    pendingClaims: 0, totalCabinets: 0, availableCompartments: 0, occupiedCompartments: 0
})
const lostItems = ref([])
const foundItems = ref([])
const suggestions = ref([])
const availableCompartments = ref([])
const showFoundForm = ref(false)
const showLostForm = ref(false)
const submitting = ref(false)

const foundForm = reactive({ foundByName: '', itemDescription: '', foundLocation: '', lockerCompartmentId: null })
const lostForm = reactive({ reporterName: '', reporterPhone: '', reporterEmail: '', itemDescription: '', lastSeenLocation: '', lostAtUtc: '' })

onMounted(async () => {
    await loadData()
})

async function loadData() {
    try {
        const [overviewRes, lostRes, foundRes, compRes] = await Promise.all([
            lostFoundApi.getOverview(),
            lostFoundApi.getLostItems({ page: 1, pageSize: 5 }),
            lostFoundApi.getFoundItems({ page: 1, pageSize: 5 }),
            lostFoundApi.getAvailableCompartments()
        ])
        Object.assign(stats, overviewRes.data)
        lostItems.value = lostRes.data.items || []
        foundItems.value = foundRes.data.items || []
        availableCompartments.value = compRes.data || []
    } catch (e) {
        console.error('Failed to load data:', e)
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
    if (!s.itemMatchId) {
        const res = await lostFoundApi.createMatch({ lostItemReportId: s.lostItemReportId, foundItemReportId: s.foundItemReportId, confidenceScore: s.confidenceScore, note: s.note })
        await lostFoundApi.confirmMatch(res.data.itemMatchId)
    } else {
        await lostFoundApi.confirmMatch(s.itemMatchId)
    }
    await loadSuggestions()
    await loadData()
}

async function rejectSuggestion(s) {
    if (s.itemMatchId) {
        await lostFoundApi.rejectMatch(s.itemMatchId)
    }
    await loadSuggestions()
}

async function submitFoundItem() {
    submitting.value = true
    try {
        await lostFoundApi.createFoundItem({
            foundByName: foundForm.foundByName,
            itemDescription: foundForm.itemDescription,
            foundLocation: foundForm.foundLocation,
            foundAtUtc: new Date().toISOString(),
            lockerCompartmentId: foundForm.lockerCompartmentId,
            photoBase64: null,
            photoUrl: null,
            storageLocation: null
        })
        showFoundForm.value = false
        foundForm.foundByName = ''; foundForm.itemDescription = ''; foundForm.foundLocation = ''; foundForm.lockerCompartmentId = null
        await loadData()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submitting.value = false
    }
}

async function submitLostItem() {
    submitting.value = true
    try {
        await lostFoundApi.createLostItem({
            reporterName: lostForm.reporterName,
            reporterPhone: lostForm.reporterPhone,
            reporterEmail: lostForm.reporterEmail || null,
            itemDescription: lostForm.itemDescription,
            lastSeenLocation: lostForm.lastSeenLocation || null,
            lostAtUtc: new Date(lostForm.lostAtUtc).toISOString(),
            photoUrl: null
        })
        showLostForm.value = false
        lostForm.reporterName = ''; lostForm.reporterPhone = ''; lostForm.reporterEmail = ''
        lostForm.itemDescription = ''; lostForm.lastSeenLocation = ''; lostForm.lostAtUtc = ''
        await loadData()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submitting.value = false
    }
}

function formatDate(d) { return d ? new Date(d).toLocaleDateString('vi-VN') : '' }
function statusClass(s) {
    const map = { Pending: 'warning', Unclaimed: 'info', MatchPending: 'info', MatchFound: 'primary', Claimed: 'success', Returned: 'success', Closed: 'secondary', ClaimPending: 'warning' }
    return map[s] || 'secondary'
}
function statusLabel(s) {
    const map = { Pending: 'Chờ xử lý', Unclaimed: 'Chưa trả', MatchPending: 'Chờ ghép', MatchFound: 'Đã ghép', Claimed: 'Đã nhận', Returned: 'Đã trả', Closed: 'Đã đóng', ClaimPending: 'Chờ duyệt' }
    return map[s] || s
}
</script>

<style scoped>
.summary-card { text-align: center; padding: 1.5rem; }
.summary-value { font-size: 2.2rem; font-weight: 700; color: var(--primary); }
.summary-label { font-size: 0.85rem; color: var(--text-secondary); margin-top: 0.25rem; }
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
