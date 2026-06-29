<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Thông tin cá nhân</h1>
                <p class="page-subtitle">Hồ sơ nhân viên</p>
            </div>
        </header>

        <div v-if="loading" class="empty-layout">
            <div class="spinner-lg"></div>
            <p>Đang tải...</p>
        </div>
        <div v-else-if="error" class="empty-layout">
            <p style="color: var(--accent-danger);">{{ error }}</p>
        </div>
        <template v-else-if="profile">
            <div class="bento-card" style="max-width: 600px;">
                <div class="info-row"><span class="info-label">Họ và tên</span><span>{{ profile.fullName }}</span></div>
                <div class="info-row"><span class="info-label">Phòng ban</span><span>{{ profile.departmentName || '—' }}</span></div>
                <div class="info-row"><span class="info-label">Chức vụ</span><span>{{ profile.positionName || '—' }}</span></div>
                <div class="info-row"><span class="info-label">Email</span><span>{{ profile.email || '—' }}</span></div>
                <div class="info-row"><span class="info-label">Số điện thoại</span><span>{{ profile.phone || '—' }}</span></div>
                <div class="info-row"><span class="info-label">Mã nhân viên</span><span>#{{ profile.employeeId }}</span></div>
            </div>
        </template>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getMyProfile } from '../services/employeeApi'

const profile = ref(null)
const loading = ref(true)
const error = ref(null)

onMounted(async () => {
    try {
        const res = await getMyProfile()
        profile.value = res.data
    } catch (e) {
        error.value = 'Không thể tải thông tin cá nhân.'
    } finally {
        loading.value = false
    }
})
</script>

<style scoped>
.info-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid var(--border-color); }
.info-label { color: var(--text-secondary); font-size: 0.85rem; }
</style>
