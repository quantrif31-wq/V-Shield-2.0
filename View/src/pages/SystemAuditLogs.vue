<template>
  <div class="page-container animate-in">
    <h1 class="page-title">Nhật ký hệ thống</h1>
    <div class="toolbar-shell" style="display:grid;grid-template-columns:1fr 180px 180px auto;gap:10px;">
      <input v-model="filters.query" type="text" placeholder="Tìm user, endpoint, bảng, lỗi..." />
      <select v-model="filters.actionType">
        <option value="">Tất cả hành động</option>
        <option value="REQUEST">REQUEST</option>
        <option value="CREATE">CREATE</option>
        <option value="UPDATE">UPDATE</option>
        <option value="DELETE">DELETE</option>
      </select>
      <select v-model="filters.isSuccess">
        <option value="">Tất cả kết quả</option>
        <option value="true">Thành công</option>
        <option value="false">Thất bại</option>
      </select>
      <button class="btn btn-secondary" @click="resetFilters">Đặt lại</button>
    </div>
    <div v-if="loading" class="empty-card">Đang tải nhật ký...</div>
    <div v-else-if="errorText" class="empty-card" style="color:#b42318;">{{ errorText }}</div>
    <div v-else class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Thời gian</th><th>User</th><th>Hành động</th><th>Đối tượng</th><th>Kết quả</th><th>Lý do lỗi</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in items" :key="row.id">
            <td>{{ fmt(row.timestampUtc) }}</td>
            <td>{{ row.username || row.userId || '-' }}</td>
            <td>{{ row.actionType }} {{ row.httpMethod || '' }}</td>
            <td>{{ row.entityName || row.path || '-' }}</td>
            <td :style="{color: row.isSuccess ? '#14804a' : '#c0392b'}">{{ row.isSuccess ? 'Thành công' : 'Thất bại' }}</td>
            <td>{{ row.failureReason || '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { getSystemAuditLogs } from '../services/accessLogApi'

const loading = ref(false)
const items = ref([])
const errorText = ref('')
const filters = reactive({ query: '', actionType: '', isSuccess: '' })

function fmt(v){ return v ? new Date(v).toLocaleString('vi-VN') : '-' }

async function fetchData() {
  loading.value = true
  errorText.value = ''
  try {
    const params = {
      query: filters.query || undefined,
      actionType: filters.actionType || undefined,
      isSuccess: filters.isSuccess === '' ? undefined : filters.isSuccess === 'true',
      page: 1,
      pageSize: 100
    }
    const { data } = await getSystemAuditLogs(params)
    items.value = data?.items || []
    if (data?.warning) errorText.value = data.warning
  } catch (e) {
    items.value = []
    errorText.value = 'Không tải được nhật ký hệ thống. Vui lòng kiểm tra API và thử lại.'
  } finally {
    loading.value = false
  }
}
function resetFilters(){ filters.query=''; filters.actionType=''; filters.isSuccess=''; fetchData() }
let t=null
watch(() => [filters.query, filters.actionType, filters.isSuccess], () => { clearTimeout(t); t=setTimeout(fetchData, 220) })
onMounted(fetchData)
</script>
