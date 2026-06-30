<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Xe của tôi</h1>
                <p class="page-subtitle">Danh sách xe đang gửi trong bãi</p>
            </div>
        </header>

        <div class="bento-card">
            <div v-if="loading" class="empty-layout">
                <div class="spinner-lg"></div>
                <p>Đang tải dữ liệu...</p>
            </div>
            <div v-else-if="error" class="empty-layout">
                <p style="color: var(--accent-danger);">{{ error }}</p>
                <button class="btn btn-primary" @click="fetchVehicles">Thử lại</button>
            </div>
            <div v-else-if="vehicles.length === 0" class="empty-layout">
                <p>Bạn chưa gửi xe nào trong bãi.</p>
            </div>
            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead>
                        <tr>
                            <th>Biển số</th>
                            <th>Loại xe</th>
                            <th>Trạng thái</th>
                            <th>Mô tả</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="v in vehicles" :key="v.vehicleId" class="table-row">
                            <td><strong>{{ v.licensePlate }}</strong></td>
                            <td>{{ v.vehicleTypeName || '—' }}</td>
                            <td>
                                <span class="status-pill minimal" :class="v.parkingStatus === 'IN' ? 'active' : 'inactive'">
                                    <span class="pill-dot"></span>
                                    {{ v.parkingStatus === 'IN' ? 'Trong bãi' : 'Đã ra' }}
                                </span>
                            </td>
                            <td class="text-muted">{{ v.description || '—' }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { authState } from '../stores/auth'
import { getByEmployeeId } from '../services/vehicleApi'

const vehicles = ref([])
const loading = ref(true)
const error = ref(null)

const fetchVehicles = async () => {
    loading.value = true
    error.value = null
    try {
        const res = await getByEmployeeId(authState.user?.employeeId)
        vehicles.value = res.data || []
    } catch (e) {
        error.value = 'Không thể tải danh sách xe.'
    } finally {
        loading.value = false
    }
}

onMounted(fetchVehicles)
</script>
