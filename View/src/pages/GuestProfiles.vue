<template>
  <div class="page-container ops-page animate-in">
    <div class="page-header-bar">
      <div>
        <span class="panel-kicker">VISITOR DIRECTORY</span>
        <h1 class="page-title">Quan ly khach</h1>
      </div>
    </div>

    <section class="ops-panel">
      <div class="toolbar-shell">
        <div class="search-bar">
          <input v-model="query" type="text" placeholder="Tim theo ID, ten, CCCD, host..." />
        </div>
      </div>

      <div v-if="isLoading" class="empty-card">Dang tai du lieu khach...</div>
      <div v-else-if="rows.length === 0" class="empty-card">Chua co du lieu khach.</div>
      <div v-else class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID khach</th>
              <th>Ten khach</th>
              <th>CCCD</th>
              <th>Host phu trach</th>
              <th>Lien he</th>
              <th>Trang thai</th>
              <th>Thao tac</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in rows" :key="item.visitorDetailId">
              <td>{{ item.visitorDetailId }}</td>
              <td class="table-main">{{ item.fullName }}</td>
              <td>{{ item.idCardNumber || '-' }}</td>
              <td>{{ item.hostEmployeeName || '-' }}</td>
              <td>{{ item.guestPhone || '-' }}</td>
              <td>{{ item.registrationStatus || '-' }}</td>
              <td>
                <div class="panel-actions">
                  <button class="btn btn-secondary btn-sm" @click="openModal(item)">Sua</button>
                  <button class="btn btn-danger btn-sm" @click="handleDelete(item)">Xoa</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="!isLoading && total > 0" class="pagination-bar">
        <span>Hien thi {{ rows.length }} / {{ total }}</span>
      </div>
    </section>

    <transition name="modal">
      <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
        <div class="modal">
          <div class="modal-header">
            <h3 class="modal-title">Cap nhat thong tin khach</h3>
            <button class="modal-close" @click="closeModal">x</button>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Ten khach</label>
              <input v-model="form.fullName" type="text" />
            </div>
            <div class="form-group">
              <label>CCCD</label>
              <input v-model="form.idCardNumber" type="text" />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Host phu trach</label>
              <select v-model="form.hostEmployeeId">
                <option :value="null">Khong gan host</option>
                <option v-for="emp in employees" :key="emp.employeeId" :value="emp.employeeId">
                  {{ emp.fullName }} (ID {{ emp.employeeId }})
                </option>
              </select>
            </div>
          </div>

          <div v-if="formError" class="empty-card error-card">{{ formError }}</div>

          <div class="modal-footer">
            <button class="btn btn-secondary" @click="closeModal">Huy</button>
            <button class="btn btn-primary" :disabled="isSaving" @click="handleSave">
              {{ isSaving ? 'Dang luu...' : 'Luu thay doi' }}
            </button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { getAll as getEmployees } from '../services/employeeApi'
import { deleteVisitorDirectoryItem, getVisitorDirectory, updateVisitorDirectoryItem } from '../services/guestProfileApi'

const isLoading = ref(true)
const isSaving = ref(false)
const query = ref('')
const rows = ref([])
const total = ref(0)
const showModal = ref(false)
const formError = ref('')
const employees = ref([])
const editingId = ref(null)

const form = reactive({
  fullName: '',
  idCardNumber: '',
  hostEmployeeId: null
})

const fetchRows = async () => {
  isLoading.value = true
  try {
    const { data } = await getVisitorDirectory({ query: query.value || undefined, page: 1, pageSize: 100 })
    rows.value = data?.items || []
    total.value = data?.total || 0
  } finally {
    isLoading.value = false
  }
}

const fetchEmployees = async () => {
  const { data } = await getEmployees({ search: '' })
  employees.value = Array.isArray(data) ? data : (data?.items || [])
}

const openModal = (item) => {
  editingId.value = item.visitorDetailId
  form.fullName = item.fullName || ''
  form.idCardNumber = item.idCardNumber || ''
  form.hostEmployeeId = item.hostEmployeeId || null
  formError.value = ''
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  editingId.value = null
  form.fullName = ''
  form.idCardNumber = ''
  form.hostEmployeeId = null
  formError.value = ''
}

const handleSave = async () => {
  formError.value = ''
  if (!editingId.value) return
  if (!form.fullName.trim()) {
    formError.value = 'Ten khach la bat buoc.'
    return
  }

  isSaving.value = true
  try {
    await updateVisitorDirectoryItem(editingId.value, {
      fullName: form.fullName.trim(),
      idCardNumber: form.idCardNumber || null,
      hostEmployeeId: form.hostEmployeeId || null
    })
    await fetchRows()
    closeModal()
  } catch (e) {
    formError.value = e?.response?.data?.message || 'Khong the cap nhat khach.'
  } finally {
    isSaving.value = false
  }
}

const handleDelete = async (item) => {
  const ok = window.confirm(`Xoa khach "${item.fullName}" (ID ${item.visitorDetailId})?`)
  if (!ok) return
  await deleteVisitorDirectoryItem(item.visitorDetailId)
  await fetchRows()
}

let timer = null
watch(query, () => {
  clearTimeout(timer)
  timer = setTimeout(fetchRows, 250)
})

onMounted(async () => {
  await Promise.all([fetchRows(), fetchEmployees()])
})
</script>
