<template>
    <div class="page-container mpc-page animate-in">
        <header class="mpc-hero">
            <div>
                <span class="panel-kicker">Bãi xe</span>
                <h1 class="page-title">Gửi xe thủ công</h1>
                <p class="page-subtitle">
                    Nhập mã khách hoặc mã nhân viên để tự nhận dạng. Xác nhận thủ công sau khi đối chiếu
                    thông tin đăng ký, sau đó chọn xe đang gửi để lấy ra hoặc nhập biển số mới để gửi thêm.
                </p>
            </div>
            <div class="hero-actions">
                <button class="btn btn-secondary" :disabled="busy" @click="resetAll">Làm mới</button>
            </div>
        </header>

        <div class="mpc-layout">
            <section class="mpc-main">
                <div class="mpc-card">
                    <div class="mpc-card-head">
                        <h2>1 · Cổng kiểm soát</h2>
                        <span class="text-muted">Chọn cổng nơi bảo vệ đang trực</span>
                    </div>
                    <select v-model="gateId" class="form-control" :disabled="busy || !!subject">
                        <option value="">-- Chọn cổng --</option>
                        <option v-for="g in gates" :key="g.gateId" :value="g.gateId">{{ g.gateName }}</option>
                    </select>
                </div>

                <div class="mpc-card">
                    <div class="mpc-card-head">
                        <h2>2 · Nhận dạng đối tượng</h2>
                        <span class="text-muted">Mã QR, mã số nhân viên/khách hoặc số căn cước</span>
                    </div>
                    <div class="code-row">
                        <input
                            v-model.trim="code"
                            type="text"
                            class="form-control code-input"
                            placeholder="VD: EMP:5734|TS:...|OTP:... / VIS:... / 5734 / CCCD"
                            :disabled="busy || !!subject"
                            @keyup.enter="lookup"
                        />
                        <button class="btn btn-primary" :disabled="busy || !code || !!subject" @click="lookup">
                            {{ busy ? 'Đang nhận dạng...' : 'Nhận dạng' }}
                        </button>
                    </div>
                    <div v-if="errorMsg" class="alert alert-danger mpc-alert">{{ errorMsg }}</div>
                </div>

                <template v-if="subject">
                    <div class="mpc-card mpc-subject-card" :class="`type-${subject.subjectType}`">
                        <div class="mpc-subject-head">
                            <div class="mpc-photo">
                                <img v-if="subject.faceUrl" :src="subject.faceUrl" alt="subject" />
                                <span v-else>{{ initials }}</span>
                            </div>
                            <div class="mpc-subject-copy">
                                <span class="mpc-type-chip" :class="subject.subjectType">
                                    {{ subject.subjectType === 'employee' ? 'Nhân viên' : 'Khách' }}
                                </span>
                                <h2 class="mpc-name">{{ subject.fullName }}</h2>
                                <div class="mpc-id-line">
                                    {{ subject.subjectType === 'employee' ? 'Mã NV' : 'Mã KH' }}: {{ subject.subjectId }}
                                </div>
                                <div class="mpc-meta">
                                    <template v-if="subject.subjectType === 'employee'">
                                        <span v-if="subject.departmentName">{{ subject.departmentName }}</span>
                                        <span v-if="subject.positionName">{{ subject.positionName }}</span>
                                        <span v-if="subject.phone">SĐT: {{ subject.phone }}</span>
                                    </template>
                                    <template v-else>
                                        <span v-if="subject.idCardNumber">CCCD: {{ subject.idCardNumber }}</span>
                                        <span v-if="subject.guestPhone">SĐT: {{ subject.guestPhone }}</span>
                                        <span v-if="subject.hostEmployeeName">Người mời: {{ subject.hostEmployeeName }}</span>
                                        <span v-if="subject.registrationStatus">Đăng ký: {{ subject.registrationStatus }}</span>
                                    </template>
                                </div>
                            </div>
                            <button class="mpc-cancel" :disabled="busy" @click="resetAll">Hủy</button>
                        </div>
                    </div>

                    <div v-if="subject.parkedVehicles?.length" class="mpc-card">
                        <div class="mpc-card-head">
                            <h2>3 · Xe đang gửi</h2>
                            <span class="text-muted">Chọn xe để xác nhận lấy ra</span>
                        </div>
                        <div class="parked-list">
                            <div v-for="vehicle in subject.parkedVehicles" :key="vehicle.vehicleId" class="parked-row">
                                <div class="parked-plate">{{ vehicle.licensePlate }}</div>
                                <div class="parked-meta">Đang gửi trong bãi</div>
                                <button
                                    class="btn btn-primary parked-action"
                                    :disabled="busy"
                                    @click="checkoutVehicle(vehicle)"
                                >
                                    Lấy xe ra
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="mpc-card">
                        <div class="mpc-card-head">
                            <h2>{{ subject.parkedVehicles?.length ? '4' : '3' }} · Gửi thêm xe mới</h2>
                            <span class="text-muted">Nhập biển số để gửi vào bãi</span>
                        </div>
                        <div class="code-row">
                            <input
                                v-model.trim="plateNumber"
                                type="text"
                                class="form-control code-input"
                                placeholder="VD: 29A-12345"
                                :disabled="busy"
                                @keyup.enter="checkinNewVehicle"
                            />
                            <button class="btn btn-primary" :disabled="busy || !plateNumber" @click="checkinNewVehicle">
                                {{ busy ? 'Đang xử lý...' : 'Gửi xe vào' }}
                            </button>
                        </div>
                        <div v-if="actionMsg" class="mpc-receipt" :class="actionTone">
                            <strong>{{ actionTitle }}</strong>
                            <span>{{ actionMsg }}</span>
                        </div>
                    </div>
                </template>

                <div v-else-if="!busy" class="mpc-empty">
                    <div class="mpc-empty-icon">🚗</div>
                    <p>Chưa có đối tượng nào được nhận dạng. Hãy nhập mã ở bước 2 để bắt đầu.</p>
                </div>
            </section>

            <aside class="mpc-side">
                <div class="mpc-card mpc-side-card">
                    <span class="panel-kicker">Ghi chú thao tác</span>
                    <ul class="mpc-steps">
                        <li>Bảo vệ đối chiếu ảnh và thông tin trên màn hình với người xuất trình mã.</li>
                        <li>Nếu không đúng người, nhấn <strong>Hủy</strong> ngay để chấm dứt phiên.</li>
                        <li>Xe đang gửi dưới tên người này sẽ được liệt kê để lấy ra.</li>
                        <li>Biển số mới nhập vào sẽ được ghi nhận là gửi xe (IN).</li>
                    </ul>
                </div>
            </aside>
        </div>
    </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { getManualSubject, scanGate, scanGuest, getManualGates } from '../services/gateTransitApi'
import { getProtectedFaceImage } from '../services/employeeApi'

const gates = ref([])
const gateId = ref('')
const code = ref('')
const plateNumber = ref('')
const busy = ref(false)
const errorMsg = ref('')
const subject = ref(null)
const actionTitle = ref('')
const actionMsg = ref('')
const actionTone = ref('')

const initials = computed(() => {
    if (!subject.value) return ''
    const parts = String(subject.value.fullName || '').trim().split(/\s+/).filter(Boolean)
    if (!parts.length) return '--'
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
    return `${parts[0][0] || ''}${parts[parts.length - 1][0] || ''}`.toUpperCase()
})

const revokeFace = () => {
    if (subject.value?.faceUrl?.startsWith('blob:')) {
        URL.revokeObjectURL(subject.value.faceUrl)
    }
}

const resetAll = () => {
    revokeFace()
    subject.value = null
    code.value = ''
    plateNumber.value = ''
    errorMsg.value = ''
    actionTitle.value = ''
    actionMsg.value = ''
    actionTone.value = ''
}

const fetchFaceImage = async (data) => {
    if (data?.subjectType === 'employee') {
        const url = data.faceImageUrl
        if (url && String(url).startsWith('http')) {
            data.faceUrl = url
            return
        }
        if (data.subjectId) {
            try {
                const blob = await getProtectedFaceImage(data.subjectId)
                data.faceUrl = URL.createObjectURL(blob.data)
                return
            } catch {
                /* no face image */
            }
        }
        data.faceUrl = ''
    } else {
        const url = data.faceImageUrl
        data.faceUrl = url && String(url).startsWith('http') ? url : ''
    }
}

const lookup = async () => {
    if (!code.value || busy.value) return
    busy.value = true
    errorMsg.value = ''
    actionTitle.value = ''
    actionMsg.value = ''
    actionTone.value = ''
    try {
        const res = await getManualSubject(code.value)
        const data = res.data?.data
        if (!res.data?.success || !data) {
            throw new Error(res.data?.message || 'Không nhận dạng được đối tượng.')
        }
        subject.value = { ...data, parkedVehicles: data.parkedVehicles || [] }
        await fetchFaceImage(subject.value)
    } catch (error) {
        errorMsg.value =
            error?.response?.data?.message ||
            error?.response?.data?.data?.message ||
            error?.message ||
            'Không nhận dạng được đối tượng.'
    } finally {
        busy.value = false
    }
}

const refreshSubject = async () => {
    if (!subject.value) return
    try {
        const res = await getManualSubject(String(subject.value.subjectId))
        const data = res.data?.data
        if (res.data?.success && data) {
            subject.value = { ...subject.value, ...data, parkedVehicles: data.parkedVehicles || [] }
        }
    } catch {
        /* keep current data */
    }
}

const confirmGate = () => {
    if (!gateId.value) {
        errorMsg.value = 'Vui lòng chọn cổng kiểm soát ở bước 1.'
        return false
    }
    errorMsg.value = ''
    return true
}

const checkoutVehicle = async (vehicle) => {
    if (!confirmGate()) return
    busy.value = true
    actionTitle.value = ''
    actionMsg.value = ''
    actionTone.value = ''
    try {
        let res
        if (subject.value.subjectType === 'employee') {
            res = await scanGate({
                LicensePlate: vehicle.licensePlate,
                GateId: Number(gateId.value),
                LaneId: null,
                Direction: 'OUT',
                CameraId: null,
                CredentialType: 'MANUAL',
                EmployeeId: Number(subject.value.subjectId),
            })
        } else {
            res = await scanGuest({
                LicensePlate: vehicle.licensePlate,
                GateId: Number(gateId.value),
                LaneId: null,
                CameraId: null,
                CredentialType: 'MANUAL',
                VisitorDetailId: Number(subject.value.subjectId),
                QrPayload: '',
            })
        }
        const ok = Boolean(res.data?.success)
        actionTone.value = ok ? 'tone-ok' : 'tone-err'
        actionTitle.value = ok ? 'Đã lấy xe ra' : 'Không thể lấy xe'
        actionMsg.value = res.data?.message || (ok ? 'Xe đã được xác nhận ra khỏi bãi.' : 'Xử lý thất bại.')
        await refreshSubject()
    } catch (error) {
        actionTone.value = 'tone-err'
        actionTitle.value = 'Lấy xe thất bại'
        actionMsg.value =
            error?.response?.data?.message ||
            error?.response?.data?.data?.message ||
            error?.message ||
            'Không gọi được API.'
    } finally {
        busy.value = false
    }
}

const checkinNewVehicle = async () => {
    if (!plateNumber.value) return
    if (!confirmGate()) return
    busy.value = true
    actionTitle.value = ''
    actionMsg.value = ''
    actionTone.value = ''
    try {
        let res
        if (subject.value.subjectType === 'employee') {
            res = await scanGate({
                LicensePlate: plateNumber.value,
                GateId: Number(gateId.value),
                LaneId: null,
                Direction: 'IN',
                CameraId: null,
                CredentialType: 'MANUAL',
                EmployeeId: Number(subject.value.subjectId),
            })
        } else {
            res = await scanGuest({
                LicensePlate: plateNumber.value,
                GateId: Number(gateId.value),
                LaneId: null,
                CameraId: null,
                CredentialType: 'MANUAL',
                VisitorDetailId: Number(subject.value.subjectId),
                QrPayload: '',
            })
        }
        const ok = Boolean(res.data?.success)
        actionTone.value = ok ? 'tone-ok' : 'tone-err'
        actionTitle.value = ok ? 'Đã gửi xe' : 'Không thể gửi xe'
        actionMsg.value = res.data?.message || (ok ? 'Xe đã được ghi nhận vào bãi.' : 'Xử lý thất bại.')
        if (ok) plateNumber.value = ''
        await refreshSubject()
    } catch (error) {
        actionTone.value = 'tone-err'
        actionTitle.value = 'Gửi xe thất bại'
        actionMsg.value =
            error?.response?.data?.message ||
            error?.response?.data?.data?.message ||
            error?.message ||
            'Không gọi được API.'
    } finally {
        busy.value = false
    }
}

onMounted(async () => {
    try {
        const res = await getManualGates()
        gates.value = res.data?.data && Array.isArray(res.data.data) ? res.data.data : []
    } catch {
        gates.value = []
    }
})

onBeforeUnmount(revokeFace)
</script>

<style scoped>
.mpc-page {
    width: min(100%, 1440px);
    margin: 0 auto;
    display: grid;
    gap: 18px;
}

.mpc-hero {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    padding: 22px 24px;
    border-radius: 28px;
    background:
        radial-gradient(circle at top left, rgba(34, 197, 94, 0.18), transparent 28%),
        radial-gradient(circle at top right, rgba(56, 189, 248, 0.18), transparent 22%),
        linear-gradient(145deg, #0b1624 0%, #13263b 100%);
    color: var(--text-primary);
    border: 1px solid rgba(125, 211, 252, 0.18);
    box-shadow: 0 26px 54px rgba(7, 18, 31, 0.24);
    color: #f8fafc;
}

.mpc-hero .page-title {
    /* Global unified-ui typography uses !important; this operator hero has a
       deliberately dark surface, so it must explicitly opt into inverse text. */
    color: #f8fafc !important;
}

.mpc-hero .panel-kicker {
    color: #67e8f9 !important;
}

.mpc-hero .page-subtitle {
    max-width: 760px;
    margin-top: 8px;
    color: #cbd5e1 !important;
}

.mpc-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 18px;
    align-items: start;
}

.mpc-main {
    display: grid;
    gap: 18px;
}

.mpc-side {
    display: grid;
    gap: 18px;
}

.mpc-card {
    padding: 20px;
    border-radius: 24px;
    background:
        radial-gradient(circle at top, rgba(255, 255, 255, 0.06), transparent 24%),
        linear-gradient(180deg, #08111c 0%, #102033 100%);
    border: 1px solid rgba(125, 211, 252, 0.16);
    box-shadow: 0 22px 42px rgba(2, 8, 23, 0.2);
    display: grid;
    gap: 14px;
    color: #e2e8f0;
}

.mpc-card-head {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 12px;
}

.mpc-card-head h2 {
    margin: 0;
    color: #f8fafc;
    font-size: 1.02rem;
}

.mpc-card .text-muted,
.mpc-card .panel-kicker {
    color: #a9c4d5;
}

.code-row {
    display: flex;
    gap: 10px;
}

.code-input {
    flex: 1;
    font-family: var(--font-mono, monospace);
}

.mpc-alert {
    margin: 0;
}

.mpc-subject-card {
    border: 1px solid rgba(125, 211, 252, 0.28);
}

.mpc-subject-card.type-employee {
    border-color: rgba(14, 165, 233, 0.38);
}

.mpc-subject-card.type-visitor {
    border-color: rgba(34, 197, 94, 0.38);
}

.mpc-subject-head {
    display: flex;
    gap: 16px;
    align-items: flex-start;
}

.mpc-photo {
    width: 132px;
    height: 132px;
    border-radius: 24px;
    flex-shrink: 0;
    display: grid;
    place-items: center;
    font-weight: 900;
    font-size: 2.6rem;
    color: var(--text-primary);
    overflow: hidden;
    border: 2px solid rgba(125, 211, 252, 0.24);
}

.mpc-subject-card.type-employee .mpc-photo {
    background: linear-gradient(135deg, #0ea5e9, #1d4ed8);
}

.mpc-subject-card.type-visitor .mpc-photo {
    background: linear-gradient(135deg, #22c55e, #15803d);
}

.mpc-photo img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.mpc-subject-copy {
    flex: 1;
    min-width: 0;
    display: grid;
    gap: 6px;
}

.mpc-type-chip {
    justify-self: start;
    padding: 4px 12px;
    border-radius: 999px;
    font-size: 0.74rem;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.06em;
}

.mpc-type-chip.employee {
    background: rgba(14, 165, 233, 0.2);
    color: #7dd3fc;
}

.mpc-type-chip.visitor {
    background: rgba(34, 197, 94, 0.2);
    color: #86efac;
}

.mpc-name {
    margin: 0;
    color: #f8fafc;
    font-size: 1.35rem;
}

.mpc-id-line {
    color: #cbd5e1;
    font-size: 0.92rem;
    font-weight: 700;
}

.mpc-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 6px 14px;
    color: #a9c4d5;
    font-size: 0.84rem;
}

.mpc-cancel {
    width: 44px;
    height: 44px;
    border-radius: 999px;
    border: 1px solid var(--border-subtle);
    background: rgba(15, 23, 42, 0.74);
    color: var(--text-secondary);
    font-weight: 800;
    transition: border-color 0.18s ease, background 0.18s ease, color 0.18s ease;
}

.mpc-cancel:hover:not(:disabled) {
    border-color: var(--border-danger);
    background: rgba(239, 68, 68, 0.14);
    color: var(--status-danger-text);
}

.parked-list {
    display: grid;
    gap: 10px;
}

.parked-row {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    gap: 14px;
    align-items: center;
    padding: 14px 16px;
    border-radius: 18px;
    background: rgba(15, 23, 42, 0.62);
    border: 1px solid var(--border-subtle);
}

.parked-plate {
    font-family: var(--font-mono, monospace);
    font-weight: 900;
    font-size: 1.05rem;
    color: #f8fafc;
    padding: 8px 14px;
    border-radius: 12px;
    background: rgba(56, 189, 248, 0.12);
    border: 1px solid rgba(56, 189, 248, 0.24);
}

.parked-meta {
    color: #a9c4d5;
    font-size: 0.82rem;
}

.parked-action {
    white-space: nowrap;
}

.mpc-receipt {
    display: grid;
    gap: 4px;
    padding: 14px;
    border-radius: 18px;
    background: rgba(15, 23, 42, 0.72);
    border: 1px solid var(--border-subtle);
}

.mpc-receipt.tone-ok {
    border-color: rgba(34, 197, 94, 0.38);
}

.mpc-receipt.tone-err {
    border-color: rgba(239, 68, 68, 0.42);
}

.mpc-receipt strong {
    color: #f8fafc;
}

.mpc-receipt span {
    color: #cbd5e1;
    font-size: 0.88rem;
}

.mpc-empty {
    padding: 40px 24px;
    border-radius: 24px;
    background: rgba(15, 23, 42, 0.5);
    border: 1px dashed var(--border-subtle);
    display: grid;
    justify-items: center;
    gap: 10px;
    text-align: center;
    color: #cbd5e1;
}

.mpc-empty-icon {
    font-size: 2.4rem;
}

.mpc-side-card {
    position: sticky;
    top: 20px;
}

.mpc-steps {
    margin: 0;
    padding-left: 18px;
    display: grid;
    gap: 10px;
    color: #d4e2eb;
    font-size: 0.88rem;
    line-height: 1.45;
}

@media (max-width: 1000px) {
    .mpc-layout {
        grid-template-columns: 1fr;
    }

    .mpc-side-card {
        position: static;
    }
}

@media (max-width: 640px) {
    .mpc-hero {
        display: grid;
    }

    .mpc-subject-head {
        flex-direction: column;
    }

    .mpc-photo {
        width: 100%;
        height: 220px;
        border-radius: 20px;
    }

    .code-row,
    .parked-row {
        grid-template-columns: 1fr;
    }

    .parked-action {
        width: 100%;
    }
}
</style>
