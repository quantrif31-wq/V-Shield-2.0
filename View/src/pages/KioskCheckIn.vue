<template>
    <div class="page-container animate-in" style="max-width: 640px; margin: 0 auto; padding-top: 2rem;">
        <div class="card" style="padding: 2rem; text-align: center;">
            <div style="font-size: 3rem; margin-bottom: 1rem;">🛡️</div>
            <h1 class="page-title" style="font-size: 1.8rem;">Visitor Self Check-in</h1>
            <p class="text-muted" style="margin-bottom: 2rem;">Enter your details to check in</p>

            <div v-if="step === 'lookup'" class="form-group" style="max-width: 400px; margin: 0 auto;">
                <label>Search your name or phone</label>
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="searchTerm" type="text" class="form-control" placeholder="Type your name or phone..." @input="searchVisits" />
                </div>
                <div v-if="searchResults.length > 0" class="card" style="margin-top: 1rem; text-align: left;">
                    <div v-for="v in searchResults" :key="v.visitId" class="kiosk-result" @click="selectVisit(v)">
                        <strong>{{ v.visitorName }}</strong>
                        <div class="text-muted">{{ v.visitorPhone }} — {{ v.status }}</div>
                    </div>
                </div>
                <div v-else-if="searchTerm && !searching" class="text-muted" style="margin-top: 1rem;">
                    No upcoming visits found.
                </div>
            </div>

            <div v-if="step === 'confirm'" class="card" style="margin-top: 1rem; text-align: left; padding: 1.5rem;">
                <h3>Confirm Check-in</h3>
                <div class="detail-grid">
                    <div class="detail-row"><span class="detail-label">Name</span><span>{{ selectedVisit.visitorName }}</span></div>
                    <div class="detail-row"><span class="detail-label">Host</span><span>{{ selectedVisit.hostEmployee?.fullName || '—' }}</span></div>
                    <div class="detail-row"><span class="detail-label">Time</span><span>{{ formatTime(selectedVisit.expectedInUtc) }} — {{ formatTime(selectedVisit.expectedOutUtc) }}</span></div>
                </div>

                <div v-if="selectedVisit.ndaRequired && !ndaAccepted" class="alert alert-info" style="margin-top: 1rem;">
                    <label class="checkbox-label">
                        <input v-model="ndaAccepted" type="checkbox" /> I accept the NDA
                    </label>
                </div>

                <div class="form-group" style="margin-top: 1rem;">
                    <label>ID Document (optional)</label>
                    <div class="form-row two">
                        <input v-model="idDocType" type="text" class="form-control" placeholder="Type" />
                        <input v-model="idDocRef" type="text" class="form-control" placeholder="Number" />
                    </div>
                </div>

                <div v-if="kioskError" class="alert alert-danger">{{ kioskError }}</div>

                <div class="chip-row" style="margin-top: 1rem; justify-content: center;">
                    <button class="btn btn-secondary" @click="step = 'lookup'; searchTerm = ''; searchResults = []">Back</button>
                    <button class="btn btn-primary" :disabled="saving || (selectedVisit.ndaRequired && !ndaAccepted)" @click="submitKioskCheckin">
                        {{ saving ? 'Checking in...' : 'Check in' }}
                    </button>
                </div>
            </div>

            <div v-if="step === 'done'" style="padding: 2rem;">
                <div style="font-size: 4rem; color: #22c55e;">✓</div>
                <h2>Check-in Successful!</h2>
                <p>Welcome, {{ checkedInName }}. Please proceed to your host.</p>
                <button class="btn btn-primary" style="margin-top: 1rem;" @click="reset">Check in another visitor</button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const step = ref('lookup')
const searchTerm = ref('')
const searchResults = ref([])
const searching = ref(false)
const selectedVisit = ref(null)
const ndaAccepted = ref(false)
const idDocType = ref('')
const idDocRef = ref('')
const saving = ref(false)
const kioskError = ref('')
const checkedInName = ref('')

function formatTime(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

async function searchVisits() {
    if (!searchTerm.value || searchTerm.value.length < 2) {
        searchResults.value = []
        return
    }
    searching.value = true
    try {
        const res = await enterpriseApi.getVisits({
            search: searchTerm.value,
            status: 'Approved',
            pageSize: 10,
        })
        searchResults.value = res.data?.items || []
    } catch (e) {
        console.error('Search failed', e)
    } finally {
        searching.value = false
    }
}

function selectVisit(v) {
    selectedVisit.value = v
    ndaAccepted.value = false
    idDocType.value = ''
    idDocRef.value = ''
    kioskError.value = ''
    step.value = 'confirm'
}

async function submitKioskCheckin() {
    saving.value = true
    kioskError.value = ''
    try {
        if (selectedVisit.value.ndaRequired && ndaAccepted.value) {
            await enterpriseApi.acceptForm(selectedVisit.value.visitId, {
                templateId: null,
                acceptedByName: selectedVisit.value.visitorName,
            })
        }
        await enterpriseApi.checkInVisit(selectedVisit.value.visitId, {
            idDocumentType: idDocType.value || null,
            idDocumentReference: idDocRef.value || null,
            verificationStatus: 'Verified',
        })
        checkedInName.value = selectedVisit.value.visitorName
        step.value = 'done'
    } catch (e) {
        kioskError.value = e.response?.data?.message || e.message
    } finally {
        saving.value = false
    }
}

function reset() {
    step.value = 'lookup'
    searchTerm.value = ''
    searchResults.value = []
    selectedVisit.value = null
    ndaAccepted.value = false
    checkedInName.value = ''
}
</script>

<style scoped>
.kiosk-result {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--border);
    cursor: pointer;
    transition: background 0.15s;
}
.kiosk-result:hover { background: var(--surface-hover); }
.kiosk-result:last-child { border-bottom: none; }
</style>
