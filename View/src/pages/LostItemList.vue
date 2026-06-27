<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Danh sách đồ thất lạc</h1>
            <button class="btn btn-primary" @click="showForm = true">+ Báo mất đồ</button>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Tất cả báo mất</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadItems">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ xử lý</option>
                            <option value="MatchFound">Đã ghép</option>
                            <option value="Claimed">Đã nhận</option>
                            <option value="Closed">Đã đóng</option>
                        </select>
                    </div>
                </div>
                <table class="data-table">
                    <thead><tr><th>Người báo</th><th>SĐT</th><th>Mô tả</th><th>Nơi mất</th><th>Ngày mất</th><th>Trạng thái</th></tr></thead>
                    <tbody>
                        <tr v-for="item in items" :key="item.lostItemReportId">
                            <td>{{ item.reporterName }}</td>
                            <td>{{ item.reporterPhone }}</td>
                            <td style="max-width:250px" class="text-truncate">{{ item.itemDescription }}</td>
                            <td>{{ item.lastSeenLocation || '---' }}</td>
                            <td>{{ formatDate(item.lostAtUtc) }}</td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                        </tr>
                        <tr v-if="!items.length"><td colspan="6" class="empty-state">Chưa có dữ liệu.</td></tr>
                    </tbody>
                </table>
                <div class="pagination-bar"><span>Tổng: {{ total }}</span></div>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
                <div class="modal-panel">
                    <h3>Báo mất đồ</h3>
                    <div class="form-group">
                        <label>Người báo *</label>
                        <input v-model="form.reporterName" class="form-control" />
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại *</label>
                            <input v-model="form.reporterPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Email</label>
                            <input v-model="form.reporterEmail" class="form-control" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Mô tả đồ vật *</label>
                        <textarea v-model="form.itemDescription" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Nơi mất gần nhất</label>
                            <input v-model="form.lastSeenLocation" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Thời gian mất *</label>
                            <input type="datetime-local" v-model="form.lostAtUtc" class="form-control" />
                        </div>
                    </div>
                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submit" :disabled="submitting">{{ submitting ? 'Đang lưu...' : 'Lưu' }}</button>
                        <button class="btn btn-secondary" @click="showForm = false">Hủy</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const items = ref([])
const total = ref(0)
const filter = ref('')
const showForm = ref(false)
const submitting = ref(false)
const form = reactive({ reporterName: '', reporterPhone: '', reporterEmail: '', itemDescription: '', lastSeenLocation: '', lostAtUtc: '' })

onMounted(loadItems)

async function loadItems() {
    try {
        const res = await lostFoundApi.getLostItems({ status: filter.value || undefined, page: 1, pageSize: 100 })
        items.value = res.data.items || []
        total.value = res.data.total || 0
    } catch (e) { console.error(e) }
}

async function submit() {
    if (!form.reporterName || !form.reporterPhone || !form.itemDescription || !form.lostAtUtc) { alert('Vui lòng điền đầy đủ thông tin.'); return }
    submitting.value = true
    try {
        await lostFoundApi.createLostItem({ reporterName: form.reporterName, reporterPhone: form.reporterPhone, reporterEmail: form.reporterEmail || null, itemDescription: form.itemDescription, lastSeenLocation: form.lastSeenLocation || null, lostAtUtc: new Date(form.lostAtUtc).toISOString(), photoUrl: null })
        showForm.value = false
        form.reporterName = ''; form.reporterPhone = ''; form.reporterEmail = ''; form.itemDescription = ''; form.lastSeenLocation = ''; form.lostAtUtc = ''
        await loadItems()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
    finally { submitting.value = false }
}

function formatDate(d) { return d ? new Date(d).toLocaleDateString('vi-VN') : '' }
function statusClass(s) { const m = { Pending: 'warning', MatchFound: 'primary', Claimed: 'success', Closed: 'secondary' }; return m[s] || 'secondary' }
function statusLabel(s) { const m = { Pending: 'Chờ xử lý', MatchFound: 'Đã ghép', Claimed: 'Đã nhận', Closed: 'Đã đóng' }; return m[s] || s }
</script>

<style scoped>
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 180px; }
</style>
