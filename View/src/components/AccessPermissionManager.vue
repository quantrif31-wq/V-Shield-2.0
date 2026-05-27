<template>
  <div class="page">
    <div class="topbar">
      <h1>Quan ly quyen vao khu vuc gioi han</h1>
      <p>Quan ly quyen cho nhan vien va khach duoc moi (dang ky truoc) theo tung khu vuc.</p>
    </div>

    <section class="panel tabs-panel">
      <button class="tab-btn" :class="{ active: activeTab === 'employee' }" @click="switchTab('employee')">Nhan vien</button>
      <button class="tab-btn" :class="{ active: activeTab === 'visitor' }" @click="switchTab('visitor')">Khach duoc moi</button>
    </section>

    <template v-if="activeTab === 'employee'">
      <section class="panel filters">
        <div class="field">
          <label>Tim nhan vien (Ten hoac ID)</label>
          <div class="combo-box">
            <input
              v-model.trim="employeeFilterKeyword"
              type="text"
              placeholder="Vi du: Nguyen Van A hoac 12"
              @focus="showEmployeeFilterDropdown = true"
              @input="handleEmployeeFilterInput"
            />
            <ul v-if="showEmployeeFilterDropdown && employeeFilterOptions.length" class="combo-menu">
              <li
                v-for="employee in employeeFilterOptions"
                :key="employee.employeeId"
                class="combo-item"
                @mousedown.prevent="selectEmployeeFilter(employee)"
              >
                {{ employee.fullName }} (ID {{ employee.employeeId }})
              </li>
            </ul>
          </div>
        </div>

        <div class="field">
          <label>Loc theo khu vuc duoc phep vao</label>
          <div class="combo-box">
            <input
              v-model.trim="gateFilterKeyword"
              type="text"
              placeholder="Go ten hoac ID khu vuc"
              @focus="showGateFilterDropdown = true"
              @input="handleGateFilterInput"
            />
            <ul v-if="showGateFilterDropdown && gateFilterOptions.length" class="combo-menu">
              <li class="combo-item" @mousedown.prevent="selectAllGateFilter">Tat ca khu vuc</li>
              <li
                v-for="gate in gateFilterOptions"
                :key="gate.gateId"
                class="combo-item"
                @mousedown.prevent="selectGateFilter(gate)"
              >
                {{ gate.gateName }} (ID {{ gate.gateId }})
              </li>
            </ul>
          </div>
        </div>

        <div class="actions">
          <button class="btn btn-subtle" :disabled="loading" @click="resetFilters">Dat lai</button>
        </div>
      </section>

      <section class="panel">
        <div class="panel-head">
          <h2>Bang phan quyen theo nhan vien</h2>
          <span>Tong nhan vien: {{ employees.length }}</span>
        </div>

        <div v-if="errorMessage" class="alert error">{{ errorMessage }}</div>
        <div v-if="successMessage" class="alert success">{{ successMessage }}</div>

        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th>Nhan vien</th>
                <th>Phong ban / Chuc vu</th>
                <th>Khu vuc duoc vao</th>
                <th>Cap quyen nhanh</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && employees.length === 0">
                <td colspan="4" class="empty">Khong co du lieu phu hop voi bo loc hien tai.</td>
              </tr>

              <tr v-for="employee in employees" :key="employee.employeeId">
                <td>
                  <div class="main">{{ employee.fullName }}</div>
                  <div class="sub">ID: {{ employee.employeeId }}</div>
                </td>
                <td>
                  <div class="main">{{ employee.departmentName || 'Chua gan phong ban' }}</div>
                  <div class="sub">{{ employee.positionName || 'Chua gan chuc vu' }}</div>
                </td>
                <td>
                  <div v-if="employee.allowedGates?.length" class="chips">
                    <span v-for="gate in employee.allowedGates" :key="`${employee.employeeId}_${gate.gateId}`" class="chip">
                      {{ gate.gateName }}
                      <button
                        class="chip-remove"
                        :disabled="loadingRowKey === `${employee.employeeId}_${gate.gateId}`"
                        @click="revokeEmployeePermission(employee, gate)"
                      >x</button>
                    </span>
                  </div>
                  <span v-else class="muted">Chua duoc cap khu vuc nao</span>
                </td>
                <td>
                  <div class="grant-box">
                    <div class="combo-box grow">
                      <input
                        v-model.trim="rowGateKeyword[employee.employeeId]"
                        type="text"
                        placeholder="Tim khu vuc theo ten/ID"
                        @focus="openRowGateDropdown(employee.employeeId)"
                        @input="handleRowGateInput(employee)"
                      />
                      <ul v-if="rowGateDropdownOpen[employee.employeeId] && getAssignableGatesFiltered(employee).length" class="combo-menu">
                        <li
                          v-for="gate in getAssignableGatesFiltered(employee)"
                          :key="gate.gateId"
                          class="combo-item"
                          @mousedown.prevent="selectRowGate(employee, gate)"
                        >
                          {{ gate.gateName }} (ID {{ gate.gateId }})
                        </li>
                      </ul>
                    </div>
                    <button class="btn btn-main btn-sm" :disabled="loading || !rowGateSelection[employee.employeeId]" @click="grantEmployeePermission(employee)">Cap quyen</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>

    <template v-else>
      <section class="panel filters">
        <div class="field">
          <label>Tim khach duoc moi (Ten hoac ID)</label>
          <div class="combo-box">
            <input
              v-model.trim="visitorFilterKeyword"
              type="text"
              placeholder="Vi du: Le Thi B hoac 101"
              @focus="showVisitorFilterDropdown = true"
              @input="handleVisitorFilterInput"
            />
            <ul v-if="showVisitorFilterDropdown && visitorFilterOptions.length" class="combo-menu">
              <li
                v-for="visitor in visitorFilterOptions"
                :key="visitor.visitorDetailId"
                class="combo-item"
                @mousedown.prevent="selectVisitorFilter(visitor)"
              >
                {{ visitor.fullName }} (ID {{ visitor.visitorDetailId }})
              </li>
            </ul>
          </div>
        </div>

        <div class="field">
          <label>Loc theo khu vuc duoc phep vao</label>
          <div class="combo-box">
            <input
              v-model.trim="visitorGateFilterKeyword"
              type="text"
              placeholder="Go ten hoac ID khu vuc"
              @focus="showVisitorGateFilterDropdown = true"
              @input="handleVisitorGateFilterInput"
            />
            <ul v-if="showVisitorGateFilterDropdown && visitorGateFilterOptions.length" class="combo-menu">
              <li class="combo-item" @mousedown.prevent="selectAllVisitorGateFilter">Tat ca khu vuc</li>
              <li
                v-for="gate in visitorGateFilterOptions"
                :key="gate.gateId"
                class="combo-item"
                @mousedown.prevent="selectVisitorGateFilter(gate)"
              >
                {{ gate.gateName }} (ID {{ gate.gateId }})
              </li>
            </ul>
          </div>
        </div>

        <div class="actions">
          <button class="btn btn-subtle" :disabled="loadingVisitor" @click="resetVisitorFilters">Dat lai</button>
        </div>
      </section>

      <section class="panel">
        <div class="panel-head">
          <h2>Bang phan quyen theo khach duoc moi</h2>
          <span>Tong khach: {{ visitors.length }}</span>
        </div>

        <div v-if="visitorErrorMessage" class="alert error">{{ visitorErrorMessage }}</div>
        <div v-if="visitorSuccessMessage" class="alert success">{{ visitorSuccessMessage }}</div>

        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th>Khach duoc moi</th>
                <th>Phieu dang ky truoc</th>
                <th>Khu vuc duoc vao</th>
                <th>Cap quyen nhanh</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loadingVisitor && visitors.length === 0">
                <td colspan="4" class="empty">Khong co du lieu phu hop voi bo loc hien tai.</td>
              </tr>

              <tr v-for="visitor in visitors" :key="visitor.visitorDetailId">
                <td>
                  <div class="main">{{ visitor.fullName }}</div>
                  <div class="sub">ID: {{ visitor.visitorDetailId }}</div>
                </td>
                <td>
                  <div class="main">Registration ID: {{ visitor.registrationId }}</div>
                  <div class="sub">Trang thai: {{ visitor.registrationStatus || 'N/A' }}</div>
                </td>
                <td>
                  <div v-if="visitor.allowedGates?.length" class="chips">
                    <span v-for="gate in visitor.allowedGates" :key="`${visitor.visitorDetailId}_${gate.gateId}`" class="chip">
                      {{ gate.gateName }}
                      <button
                        class="chip-remove"
                        :disabled="loadingVisitorRowKey === `${visitor.visitorDetailId}_${gate.gateId}`"
                        @click="revokeVisitorPermission(visitor, gate)"
                      >x</button>
                    </span>
                  </div>
                  <span v-else class="muted">Chua duoc cap khu vuc nao</span>
                </td>
                <td>
                  <div class="grant-box">
                    <div class="combo-box grow">
                      <input
                        v-model.trim="visitorRowGateKeyword[visitor.visitorDetailId]"
                        type="text"
                        placeholder="Tim khu vuc theo ten/ID"
                        @focus="openVisitorRowGateDropdown(visitor.visitorDetailId)"
                        @input="handleVisitorRowGateInput(visitor)"
                      />
                      <ul v-if="visitorRowGateDropdownOpen[visitor.visitorDetailId] && getAssignableVisitorGatesFiltered(visitor).length" class="combo-menu">
                        <li
                          v-for="gate in getAssignableVisitorGatesFiltered(visitor)"
                          :key="gate.gateId"
                          class="combo-item"
                          @mousedown.prevent="selectVisitorRowGate(visitor, gate)"
                        >
                          {{ gate.gateName }} (ID {{ gate.gateId }})
                        </li>
                      </ul>
                    </div>
                    <button class="btn btn-main btn-sm" :disabled="loadingVisitor || !visitorRowGateSelection[visitor.visitorDetailId]" @click="grantVisitorPermission(visitor)">Cap quyen</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import {
  deleteEmployeeAccessPermission,
  deleteVisitorAccessPermission,
  getEmployeePermissionMatrix,
  getVisitorPermissionMatrix,
  setAccessPermission,
} from '../services/accessPermissionApi'

const activeTab = ref('employee')

const loading = ref(false)
const loadingRowKey = ref('')
const errorMessage = ref('')
const successMessage = ref('')
const employees = ref([])
const gates = ref([])
const rowGateSelection = reactive({})
const rowGateKeyword = reactive({})
const rowGateDropdownOpen = reactive({})
const gateFilterKeyword = ref('')
const showGateFilterDropdown = ref(false)
const employeeFilterKeyword = ref('')
const showEmployeeFilterDropdown = ref(false)
let matrixFetchDebounceTimer = null
const filters = reactive({ query: '', gateId: '' })

const loadingVisitor = ref(false)
const loadingVisitorRowKey = ref('')
const visitorErrorMessage = ref('')
const visitorSuccessMessage = ref('')
const visitors = ref([])
const visitorGates = ref([])
const visitorRowGateSelection = reactive({})
const visitorRowGateKeyword = reactive({})
const visitorRowGateDropdownOpen = reactive({})
const visitorGateFilterKeyword = ref('')
const showVisitorGateFilterDropdown = ref(false)
const visitorFilterKeyword = ref('')
const showVisitorFilterDropdown = ref(false)
let visitorMatrixFetchDebounceTimer = null
const visitorFilters = reactive({ query: '', gateId: '' })

const gateFilterOptions = computed(() => filterGatesByKeyword(gates.value, gateFilterKeyword.value, 5))
const employeeFilterOptions = computed(() => filterEmployeesByKeyword(employees.value, employeeFilterKeyword.value, 5))
const visitorGateFilterOptions = computed(() => filterGatesByKeyword(visitorGates.value, visitorGateFilterKeyword.value, 5))
const visitorFilterOptions = computed(() => filterVisitorsByKeyword(visitors.value, visitorFilterKeyword.value, 5))

function normalizeText(input) {
  return String(input || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/\u0111/g, 'd')
    .replace(/\u0110/g, 'd')
    .toLowerCase()
    .trim()
}

function normalizeEmployees(rawItems = []) {
  return rawItems.map((item) => ({
    employeeId: item.employeeId,
    fullName: item.fullName || `Nhan vien #${item.employeeId}`,
    departmentName: item.departmentName || '',
    positionName: item.positionName || '',
    allowedGates: Array.isArray(item.allowedGates) ? item.allowedGates : [],
  }))
}

function normalizeVisitors(rawItems = []) {
  return rawItems.map((item) => ({
    visitorDetailId: item.visitorDetailId,
    fullName: item.fullName || `Khach #${item.visitorDetailId}`,
    registrationId: item.registrationId,
    registrationStatus: item.registrationStatus,
    allowedGates: Array.isArray(item.allowedGates) ? item.allowedGates : [],
  }))
}

function clearMessages() { errorMessage.value = ''; successMessage.value = '' }
function clearVisitorMessages() { visitorErrorMessage.value = ''; visitorSuccessMessage.value = '' }

function filterGatesByKeyword(source, keyword, limit = 5) {
  const q = normalizeText(keyword)
  const all = source || []
  if (!q) return all.slice(0, limit)
  return all.filter((gate) => normalizeText(gate.gateName).includes(q) || String(gate.gateId).includes(q)).slice(0, limit)
}

function filterEmployeesByKeyword(source, keyword, limit = 5) {
  const q = normalizeText(keyword)
  const all = source || []
  if (!q) return all.slice(0, limit)
  return all.filter((employee) => normalizeText(employee.fullName).includes(q) || String(employee.employeeId).includes(q)).slice(0, limit)
}

function filterVisitorsByKeyword(source, keyword, limit = 5) {
  const q = normalizeText(keyword)
  const all = source || []
  if (!q) return all.slice(0, limit)
  return all.filter((visitor) => normalizeText(visitor.fullName).includes(q) || String(visitor.visitorDetailId).includes(q)).slice(0, limit)
}

function getAssignableGates(employee) {
  const assigned = new Set((employee.allowedGates || []).map((gate) => Number(gate.gateId)))
  return gates.value.filter((gate) => !assigned.has(Number(gate.gateId)))
}

function getAssignableGatesFiltered(employee) {
  return filterGatesByKeyword(getAssignableGates(employee), rowGateKeyword[employee.employeeId], 5)
}

function getAssignableVisitorGates(visitor) {
  const assigned = new Set((visitor.allowedGates || []).map((gate) => Number(gate.gateId)))
  return visitorGates.value.filter((gate) => !assigned.has(Number(gate.gateId)))
}

function getAssignableVisitorGatesFiltered(visitor) {
  return filterGatesByKeyword(getAssignableVisitorGates(visitor), visitorRowGateKeyword[visitor.visitorDetailId], 5)
}

function switchTab(tab) {
  activeTab.value = tab
}

function handleGateFilterInput() { showGateFilterDropdown.value = true; filters.gateId = ''; scheduleMatrixFetch() }
function handleEmployeeFilterInput() { showEmployeeFilterDropdown.value = true; filters.query = employeeFilterKeyword.value; scheduleMatrixFetch() }
function selectEmployeeFilter(employee) { employeeFilterKeyword.value = `${employee.fullName} (ID ${employee.employeeId})`; filters.query = String(employee.employeeId); showEmployeeFilterDropdown.value = false; scheduleMatrixFetch(0) }
function selectAllGateFilter() { filters.gateId = ''; gateFilterKeyword.value = ''; showGateFilterDropdown.value = false; scheduleMatrixFetch(0) }
function selectGateFilter(gate) { filters.gateId = String(gate.gateId); gateFilterKeyword.value = `${gate.gateName} (ID ${gate.gateId})`; showGateFilterDropdown.value = false; scheduleMatrixFetch(0) }
function openRowGateDropdown(employeeId) { rowGateDropdownOpen[employeeId] = true }
function handleRowGateInput(employee) { rowGateSelection[employee.employeeId] = ''; rowGateDropdownOpen[employee.employeeId] = true }
function selectRowGate(employee, gate) { rowGateSelection[employee.employeeId] = String(gate.gateId); rowGateKeyword[employee.employeeId] = `${gate.gateName} (ID ${gate.gateId})`; rowGateDropdownOpen[employee.employeeId] = false }

function handleVisitorGateFilterInput() { showVisitorGateFilterDropdown.value = true; visitorFilters.gateId = ''; scheduleVisitorMatrixFetch() }
function handleVisitorFilterInput() { showVisitorFilterDropdown.value = true; visitorFilters.query = visitorFilterKeyword.value; scheduleVisitorMatrixFetch() }
function selectVisitorFilter(visitor) { visitorFilterKeyword.value = `${visitor.fullName} (ID ${visitor.visitorDetailId})`; visitorFilters.query = String(visitor.visitorDetailId); showVisitorFilterDropdown.value = false; scheduleVisitorMatrixFetch(0) }
function selectAllVisitorGateFilter() { visitorFilters.gateId = ''; visitorGateFilterKeyword.value = ''; showVisitorGateFilterDropdown.value = false; scheduleVisitorMatrixFetch(0) }
function selectVisitorGateFilter(gate) { visitorFilters.gateId = String(gate.gateId); visitorGateFilterKeyword.value = `${gate.gateName} (ID ${gate.gateId})`; showVisitorGateFilterDropdown.value = false; scheduleVisitorMatrixFetch(0) }
function openVisitorRowGateDropdown(visitorDetailId) { visitorRowGateDropdownOpen[visitorDetailId] = true }
function handleVisitorRowGateInput(visitor) { visitorRowGateSelection[visitor.visitorDetailId] = ''; visitorRowGateDropdownOpen[visitor.visitorDetailId] = true }
function selectVisitorRowGate(visitor, gate) { visitorRowGateSelection[visitor.visitorDetailId] = String(gate.gateId); visitorRowGateKeyword[visitor.visitorDetailId] = `${gate.gateName} (ID ${gate.gateId})`; visitorRowGateDropdownOpen[visitor.visitorDetailId] = false }

function scheduleMatrixFetch(delay = 220) {
  if (matrixFetchDebounceTimer) clearTimeout(matrixFetchDebounceTimer)
  matrixFetchDebounceTimer = setTimeout(() => fetchEmployeeMatrix(), delay)
}

function scheduleVisitorMatrixFetch(delay = 220) {
  if (visitorMatrixFetchDebounceTimer) clearTimeout(visitorMatrixFetchDebounceTimer)
  visitorMatrixFetchDebounceTimer = setTimeout(() => fetchVisitorMatrix(), delay)
}

function closeAllComboboxes() {
  showGateFilterDropdown.value = false
  showEmployeeFilterDropdown.value = false
  showVisitorGateFilterDropdown.value = false
  showVisitorFilterDropdown.value = false
  Object.keys(rowGateDropdownOpen).forEach((key) => { rowGateDropdownOpen[key] = false })
  Object.keys(visitorRowGateDropdownOpen).forEach((key) => { visitorRowGateDropdownOpen[key] = false })
}

function handleDocumentClick(event) {
  const target = event.target
  if (!(target instanceof Element)) return
  if (target.closest('.combo-box')) return
  closeAllComboboxes()
}

async function fetchEmployeeMatrix() {
  loading.value = true
  clearMessages()
  try {
    const params = {}
    if (filters.query) params.query = filters.query
    if (filters.gateId) params.gateId = Number(filters.gateId)
    const { data } = await getEmployeePermissionMatrix(params)
    employees.value = normalizeEmployees(data?.employees || [])
    gates.value = Array.isArray(data?.gates) ? data.gates : []
    employees.value.forEach((employee) => {
      if (!rowGateSelection[employee.employeeId]) rowGateSelection[employee.employeeId] = ''
      if (!rowGateKeyword[employee.employeeId]) rowGateKeyword[employee.employeeId] = ''
      if (!rowGateDropdownOpen[employee.employeeId]) rowGateDropdownOpen[employee.employeeId] = false
    })
  } catch (error) {
    errorMessage.value = error?.response?.data?.message || 'Khong tai duoc du lieu phan quyen.'
  } finally {
    loading.value = false
  }
}

async function fetchVisitorMatrix() {
  loadingVisitor.value = true
  clearVisitorMessages()
  try {
    const params = {}
    if (visitorFilters.query) params.query = visitorFilters.query
    if (visitorFilters.gateId) params.gateId = Number(visitorFilters.gateId)
    const { data } = await getVisitorPermissionMatrix(params)
    visitors.value = normalizeVisitors(data?.visitors || [])
    visitorGates.value = Array.isArray(data?.gates) ? data.gates : []
    visitors.value.forEach((visitor) => {
      if (!visitorRowGateSelection[visitor.visitorDetailId]) visitorRowGateSelection[visitor.visitorDetailId] = ''
      if (!visitorRowGateKeyword[visitor.visitorDetailId]) visitorRowGateKeyword[visitor.visitorDetailId] = ''
      if (!visitorRowGateDropdownOpen[visitor.visitorDetailId]) visitorRowGateDropdownOpen[visitor.visitorDetailId] = false
    })
  } catch (error) {
    visitorErrorMessage.value = error?.response?.data?.message || 'Khong tai duoc du lieu phan quyen khach.'
  } finally {
    loadingVisitor.value = false
  }
}

async function grantEmployeePermission(employee) {
  const selectedGateId = Number(rowGateSelection[employee.employeeId] || 0)
  if (!selectedGateId) return
  loading.value = true
  clearMessages()
  try {
    await setAccessPermission({ EmployeeId: employee.employeeId, GateId: selectedGateId, IsAllowed: true })
    successMessage.value = `Da cap quyen cho nhan vien ID ${employee.employeeId}.`
    await fetchEmployeeMatrix()
    rowGateSelection[employee.employeeId] = ''
    rowGateKeyword[employee.employeeId] = ''
  } catch (error) {
    errorMessage.value = error?.response?.data?.message || 'Cap quyen that bai.'
  } finally {
    loading.value = false
  }
}

async function revokeEmployeePermission(employee, gate) {
  const rowKey = `${employee.employeeId}_${gate.gateId}`
  loadingRowKey.value = rowKey
  clearMessages()
  try {
    await deleteEmployeeAccessPermission(employee.employeeId, gate.gateId)
    successMessage.value = `Da xoa quyen khu vuc cho nhan vien ID ${employee.employeeId}.`
    await fetchEmployeeMatrix()
  } catch (error) {
    errorMessage.value = error?.response?.data?.message || 'Xoa quyen that bai.'
  } finally {
    loadingRowKey.value = ''
  }
}

async function grantVisitorPermission(visitor) {
  const selectedGateId = Number(visitorRowGateSelection[visitor.visitorDetailId] || 0)
  if (!selectedGateId) return
  loadingVisitor.value = true
  clearVisitorMessages()
  try {
    await setAccessPermission({ VisitorDetailId: visitor.visitorDetailId, GateId: selectedGateId, IsAllowed: true })
    visitorSuccessMessage.value = `Da cap quyen cho khach ID ${visitor.visitorDetailId}.`
    await fetchVisitorMatrix()
    visitorRowGateSelection[visitor.visitorDetailId] = ''
    visitorRowGateKeyword[visitor.visitorDetailId] = ''
  } catch (error) {
    visitorErrorMessage.value = error?.response?.data?.message || 'Cap quyen that bai.'
  } finally {
    loadingVisitor.value = false
  }
}

async function revokeVisitorPermission(visitor, gate) {
  const rowKey = `${visitor.visitorDetailId}_${gate.gateId}`
  loadingVisitorRowKey.value = rowKey
  clearVisitorMessages()
  try {
    await deleteVisitorAccessPermission(visitor.visitorDetailId, gate.gateId)
    visitorSuccessMessage.value = `Da xoa quyen khu vuc cho khach ID ${visitor.visitorDetailId}.`
    await fetchVisitorMatrix()
  } catch (error) {
    visitorErrorMessage.value = error?.response?.data?.message || 'Xoa quyen that bai.'
  } finally {
    loadingVisitorRowKey.value = ''
  }
}

function resetFilters() {
  filters.query = ''
  filters.gateId = ''
  gateFilterKeyword.value = ''
  employeeFilterKeyword.value = ''
  scheduleMatrixFetch(0)
}

function resetVisitorFilters() {
  visitorFilters.query = ''
  visitorFilters.gateId = ''
  visitorGateFilterKeyword.value = ''
  visitorFilterKeyword.value = ''
  scheduleVisitorMatrixFetch(0)
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  fetchEmployeeMatrix()
  fetchVisitorMatrix()
})

onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick)
  if (matrixFetchDebounceTimer) clearTimeout(matrixFetchDebounceTimer)
  if (visitorMatrixFetchDebounceTimer) clearTimeout(visitorMatrixFetchDebounceTimer)
})
</script>

<style scoped>
.page { min-height: 100vh; padding: 20px; background: #f3f6fb; color: #0f172a; }
.topbar h1 { margin: 0; font-size: 28px; font-weight: 800; }
.topbar p { margin: 8px 0 0; color: #475569; }
.panel { background: #fff; border: 1px solid #dbe3ef; border-radius: 16px; padding: 16px; margin-top: 16px; }
.tabs-panel { display: flex; gap: 10px; align-items: center; }
.tab-btn { height: 38px; border: 1px solid #cbd5e1; border-radius: 999px; padding: 0 14px; background: #f8fafc; color: #334155; font-weight: 700; cursor: pointer; }
.tab-btn.active { background: #2563eb; border-color: #2563eb; color: #fff; }
.filters { display: grid; grid-template-columns: 1fr 1fr auto; gap: 12px; align-items: end; }
.field label { display: block; margin-bottom: 6px; font-size: 13px; font-weight: 700; color: #334155; }
.field input { width: 100%; height: 40px; border: 1px solid #cbd5e1; border-radius: 10px; padding: 0 10px; background: #fff; }
.actions { display: flex; gap: 8px; }
.panel-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.panel-head h2 { margin: 0; font-size: 20px; font-weight: 800; }
.alert { border-radius: 10px; padding: 10px 12px; margin-bottom: 10px; font-size: 14px; }
.alert.error { background: #fef2f2; color: #991b1b; border: 1px solid #fecaca; }
.alert.success { background: #ecfdf5; color: #166534; border: 1px solid #bbf7d0; }
.table-wrap { overflow-x: auto; overflow-y: visible; position: relative; }
.table { width: 100%; border-collapse: collapse; min-width: 980px; }
.table th, .table td { border-bottom: 1px solid #e2e8f0; padding: 12px 10px; vertical-align: top; overflow: visible; }
.table th { text-align: left; font-size: 12px; text-transform: uppercase; letter-spacing: .04em; color: #64748b; }
.main { font-weight: 700; }
.sub { color: #64748b; font-size: 13px; margin-top: 2px; }
.muted { color: #94a3b8; font-size: 13px; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; }
.chip { display: inline-flex; align-items: center; gap: 8px; background: #e0f2fe; color: #075985; border: 1px solid #bae6fd; padding: 4px 8px; border-radius: 999px; font-size: 13px; }
.chip-remove { border: none; background: transparent; color: #0c4a6e; font-weight: 700; cursor: pointer; }
.grant-box { display: flex; gap: 8px; align-items: flex-start; }
.grow { flex: 1; min-width: 260px; }
.empty { text-align: center; color: #64748b; padding: 20px 0; }
.btn { height: 40px; border: none; border-radius: 10px; padding: 0 14px; font-weight: 700; cursor: pointer; }
.btn-main { background: #2563eb; color: #fff; }
.btn-subtle { background: #e2e8f0; color: #0f172a; }
.btn-sm { height: 38px; }
.btn:disabled { opacity: .6; cursor: not-allowed; }
.combo-box { position: relative; }
.grant-box .combo-box input { width: 100%; height: 40px; border: 1px solid #cbd5e1; border-radius: 10px; padding: 0 10px; background: #fff; }
.combo-menu { position: absolute; z-index: 20; top: calc(100% + 6px); left: 0; right: 0; max-height: 240px; overflow-y: auto; background-color: #ffffff; border: 1px solid #94a3b8; border-radius: 10px; box-shadow: 0 14px 30px rgba(15, 23, 42, 0.22); padding: 4px; margin: 0; list-style: none; }
.combo-item { padding: 9px 10px; border-radius: 8px; cursor: pointer; font-size: 14px; color: #0f172a; background: #ffffff; }
.combo-item:hover { background: #e2e8f0; }
@media (max-width: 1100px) { .filters { grid-template-columns: 1fr; } }
</style>
