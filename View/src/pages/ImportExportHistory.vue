<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Lịch sử Import / Export</h1>
                <p class="page-subtitle">Theo dõi các thao tác nhập xuất dữ liệu</p>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="fetchHistory">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px;">
                        <polyline points="23 4 23 10 17 10" />
                        <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" />
                    </svg>
                    Làm mới
                </button>
            </div>
        </header>

        <div class="bento-card table-section">
            <div class="table-toolbar">
                <div class="filter-box" style="display: flex; gap: 0.75rem; flex-wrap: wrap;">
                    <select v-model="filterOperation" class="minimal-select">
                        <option value="">Tất cả thao tác</option>
                        <option value="Import">Import</option>
                        <option value="Export">Export</option>
                    </select>
                    <select v-model="filterEntity" class="minimal-select">
                        <option value="">Tất cả entity</option>
                        <option value="Employee">Nhân viên</option>
                        <option value="Vehicle">Phương tiện</option>
                        <option value="Department">Phòng ban</option>
                        <option value="Position">Chức vụ</option>
                        <option value="AppUser">Người dùng</option>
                    </select>
                </div>
            </div>

            <div v-if="loading" class="empty-layout">
                <div class="spinner-lg"></div>
                <p>Đang tải...</p>
            </div>

            <div v-else-if="history.length === 0" class="empty-layout">
                <p>Chưa có lịch sử import/export nào.</p>
            </div>

            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead>
                        <tr>
                            <th>Thao tác</th>
                            <th>Entity</th>
                            <th>File</th>
                            <th>Trạng thái</th>
                            <th class="text-right">Dòng</th>
                            <th>Người thực hiện</th>
                            <th>Thời gian</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in paginatedHistory" :key="item.id">
                            <td>
                                <span class="badge" :class="item.operationType === 'Import' ? 'badge-import' : 'badge-export'">
                                    {{ item.operationType === 'Import' ? '📥 Import' : '📤 Export' }}
                                </span>
                            </td>
                            <td>{{ entityDisplayName(item.entityType) }}</td>
                            <td class="file-cell">
                                <span class="file-name">{{ item.fileName }}</span>
                                <span class="file-size">{{ formatSize(item.fileSize) }}</span>
                            </td>
                            <td>
                                <span class="status-badge" :class="statusClass(item.status)">
                                    {{ statusLabel(item.status) }}
                                </span>
                            </td>
                            <td class="text-right stats-cell">
                                <span class="stat-ok">✅ {{ item.successCount }}</span>
                                <span v-if="item.errorCount > 0" class="stat-err">❌ {{ item.errorCount }}</span>
                            </td>
                            <td>{{ item.performedByName || '—' }}</td>
                            <td>{{ formatDate(item.performedAt) }}</td>
                            <td class="text-right">
                                <button v-if="item.operationType === 'Export'" class="btn btn-sm btn-secondary" @click="downloadFile(item.id, item.fileName)">
                                    Tải về
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>

                <div v-if="totalPages > 1" class="pagination">
                    <button class="btn btn-sm btn-secondary" :disabled="page <= 1" @click="page--">Trước</button>
                    <span class="page-info">Trang {{ page }} / {{ totalPages }}</span>
                    <button class="btn btn-sm btn-secondary" :disabled="page >= totalPages" @click="page++">Sau</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import * as importExportApi from '../services/importExportApi'

const history = ref([])
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const filterOperation = ref('')
const filterEntity = ref('')

const totalPages = computed(() => Math.max(1, Math.ceil(history.value.length / pageSize) + 1))

const paginatedHistory = computed(() => history.value.slice((page.value - 1) * pageSize, page.value * pageSize))

onMounted(() => fetchHistory())

watch([filterOperation, filterEntity], () => {
    page.value = 1
    fetchHistory()
})

async function fetchHistory() {
    loading.value = true
    try {
        const res = await importExportApi.getHistory({
            operationType: filterOperation.value || undefined,
            entityType: filterEntity.value || undefined,
        })
        history.value = res.data
    } catch {
        history.value = []
    } finally {
        loading.value = false
    }
}

function entityDisplayName(type) {
    const map = { Employee: 'Nhân viên', Vehicle: 'Phương tiện', Department: 'Phòng ban', Position: 'Chức vụ', AppUser: 'Người dùng' }
    return map[type] || type
}

function statusClass(status) {
    return { Completed: 'status-ok', PartialSuccess: 'status-warn', Failed: 'status-err', Processing: 'status-proc', Pending: 'status-proc' }[status] || ''
}

function statusLabel(status) {
    return { Completed: '✅ Hoàn tất', PartialSuccess: '⚠️ Có lỗi', Failed: '❌ Thất bại', Processing: '⏳ Đang xử lý', Pending: '⏳ Chờ xử lý' }[status] || status
}

function formatDate(utcStr) {
    if (!utcStr) return '—'
    return new Date(utcStr).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function formatSize(bytes) {
    if (!bytes) return '—'
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

async function downloadFile(id, fileName) {
    try {
        const res = await importExportApi.downloadResult(id)
        const url = window.URL.createObjectURL(new Blob([res.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', fileName)
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        window.URL.revokeObjectURL(url)
    } catch (err) {
        console.error('Download failed', err)
    }
}
</script>

<style scoped>
.file-cell { display: flex; flex-direction: column; gap: 2px; }
.file-name { font-size: 0.8rem; color: var(--text-primary, #f3f4f6); max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.file-size { font-size: 0.7rem; color: var(--text-secondary, #9ca3af); }
.stats-cell { display: flex; gap: 0.5rem; justify-content: flex-end; font-size: 0.8rem; }
.stat-ok { color: #22c55e; }
.stat-err { color: #ef4444; }
.badge { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.75rem; font-weight: 500; }
.badge-import { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
.badge-export { background: rgba(168, 85, 247, 0.15); color: #a78bfa; }
.status-badge { font-size: 0.8rem; }
.status-ok { color: #22c55e; }
.status-warn { color: #eab308; }
.status-err { color: #ef4444; }
.status-proc { color: #60a5fa; }
.pagination { display: flex; align-items: center; justify-content: center; gap: 1rem; padding: 1rem; }
.page-info { font-size: 0.85rem; color: var(--text-secondary, #9ca3af); }
</style>
