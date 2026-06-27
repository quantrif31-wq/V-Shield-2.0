<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Danh sách đồ tìm thấy</h1>
            <button class="btn btn-primary" @click="showForm = true">+ Nhập đồ tìm thấy</button>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Tất cả đồ tìm thấy</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadItems">
                            <option value="">Tất cả</option>
                            <option value="Unclaimed">Chưa trả</option>
                            <option value="MatchPending">Chờ ghép</option>
                            <option value="ClaimPending">Chờ duyệt</option>
                            <option value="Returned">Đã trả</option>
                        </select>
                    </div>
                </div>
                <table class="data-table">
                    <thead><tr><th>Người tìm</th><th>Mô tả</th><th>Nơi tìm</th><th>Ngày</th><th>Vị trí lưu</th><th>Trạng thái</th></tr></thead>
                    <tbody>
                        <tr v-for="item in items" :key="item.foundItemReportId">
                            <td>{{ item.foundByName }}</td>
                            <td style="max-width:250px" class="text-truncate">{{ item.itemDescription }}</td>
                            <td>{{ item.foundLocation }}</td>
                            <td>{{ formatDate(item.foundAtUtc) }}</td>
                            <td>{{ item.storageLocation || (item.lockerCompartment ? item.lockerCompartment.code : '') || '---' }}</td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                        </tr>
                        <tr v-if="!items.length"><td colspan="6" class="empty-state">Chưa có dữ liệu.</td></tr>
                    </tbody>
                </table>
                <div class="pagination-bar">
                    <span>Tổng: {{ total }}</span>
                </div>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
                <div class="modal-panel">
                    <h3>Nhập đồ tìm thấy</h3>
                    <div class="form-group">
                        <label>Người tìm thấy *</label>
                        <input v-model="form.foundByName" class="form-control" />
                    </div>
                    <div class="form-group">
                        <label>Mô tả đồ vật *</label>
                        <textarea v-model="form.itemDescription" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Nơi tìm thấy *</label>
                            <input v-model="form.foundLocation" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Vị trí lưu trữ</label>
                            <input v-model="form.storageLocation" class="form-control" placeholder="VD: Tủ A, ngăn B2" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Ngăn tủ locker</label>
                        <select v-model="form.lockerCompartmentId" class="form-control">
                            <option :value="null">-- Chọn --</option>
                            <option v-for="c in compartments" :key="c.lockerCompartmentId" :value="c.lockerCompartmentId">
                                {{ cabinetName(c) }} - {{ c.code }}
                            </option>
                        </select>
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
const compartments = ref([])

const form = reactive({ foundByName: '', itemDescription: '', foundLocation: '', storageLocation: '', lockerCompartmentId: null })

onMounted(async () => {
    await Promise.all([loadItems(), loadCompartments()])
})

async function loadItems() {
    try {
        const res = await lostFoundApi.getFoundItems({ status: filter.value || undefined, page: 1, pageSize: 100 })
        items.value = res.data.items || []
        total.value = res.data.total || 0
    } catch (e) { console.error(e) }
}

async function loadCompartments() {
    try {
        const res = await lostFoundApi.getAvailableCompartments()
        compartments.value = res.data || []
    } catch (e) { /* ignore */ }
}

function cabinetName(c) { return c.cabinet?.name || `Tủ #${c.lockerCabinetId}` }

async function submit() {
    if (!form.foundByName || !form.itemDescription || !form.foundLocation) { alert('Vui lòng điền đầy đủ thông tin.'); return }
    submitting.value = true
    try {
        await lostFoundApi.createFoundItem({
            foundByName: form.foundByName, itemDescription: form.itemDescription,
            foundLocation: form.foundLocation, foundAtUtc: new Date().toISOString(),
            storageLocation: form.storageLocation || null, lockerCompartmentId: form.lockerCompartmentId,
            photoBase64: null, photoUrl: null
        })
        showForm.value = false
        form.foundByName = ''; form.itemDescription = ''; form.foundLocation = ''; form.storageLocation = ''; form.lockerCompartmentId = null
        await loadItems()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
    finally { submitting.value = false }
}

function formatDate(d) { return d ? new Date(d).toLocaleDateString('vi-VN') : '' }
function statusClass(s) { const m = { Unclaimed: 'info', MatchPending: 'primary', ClaimPending: 'warning', Returned: 'success' }; return m[s] || 'secondary' }
function statusLabel(s) { const m = { Unclaimed: 'Chưa trả', MatchPending: 'Chờ ghép', ClaimPending: 'Chờ duyệt', Returned: 'Đã trả' }; return m[s] || s }
</script>

<style scoped>
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 180px; }
</style>
