<template>
    <div class="page-container parking-fallback-page animate-in">
        <header class="parking-hero">
            <div>
                <span class="panel-kicker">Dự phòng bãi xe</span>
                <h1 class="page-title">Dự phòng gửi xe thủ công</h1>
                <p class="page-subtitle">
                    Dự phòng cho bãi xe khi camera hoặc QR flow bị tê liệt. Mỗi lane yêu cầu xác minh đúng đối
                    tượng, QR động và biển số xe trước khi cho qua.
                </p>
            </div>
            <div class="hero-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadBootstrap">Làm mới</button>
            </div>
        </header>

        <section class="signal-strip">
            <article class="signal-card">
                <span class="signal-label">Làn trực tuyến</span>
                <strong>{{ laneOptions.length }}</strong>
            </article>
            <article class="signal-card">
                <span class="signal-label">Khu vực đỗ xe</span>
                <strong>{{ parkingAreas.length }}</strong>
            </article>
            <article class="signal-card">
                <span class="signal-label">Chế độ thủ công</span>
                <strong>2 làn</strong>
            </article>
        </section>

        <div v-if="loading" class="empty-card">Đang tải lane và parking area...</div>
        <div v-else class="lane-grid">
            <section v-for="lane in lanes" :key="lane.key" class="lane-shell" :class="lane.resultTone || 'tone-idle'">
                <div class="lane-shell-glow"></div>

                <div class="lane-head">
                    <div>
                        <span class="lane-kicker">Làn {{ lane.order }}</span>
                        <h2>{{ lane.title }}</h2>
                    </div>
                    <span class="lane-status-chip" :class="lane.resultTone || 'tone-idle'">
                        {{ lane.resultLabel || 'Chờ' }}
                    </span>
                </div>

                <div class="lane-config">
                    <label class="form-group">
                        <span>Sơ đồ lane</span>
                        <select v-model="lane.selectedLaneId" class="form-control">
                            <option :value="null">-- Chọn lane --</option>
                            <option v-for="opt in laneOptions" :key="opt.laneId" :value="opt.laneId">
                                {{ opt.name }} ({{ opt.direction || 'VÀO' }})
                            </option>
                        </select>
                    </label>

                    <div class="lane-type-toggle">
                        <button
                            type="button"
                            class="type-pill"
                            :class="{ active: lane.subjectType === 'employee' }"
                            @click="switchLaneType(lane, 'employee')"
                        >
                            Nhân viên
                        </button>
                        <button
                            type="button"
                            class="type-pill"
                            :class="{ active: lane.subjectType === 'visitor' }"
                            @click="switchLaneType(lane, 'visitor')"
                        >
                            Khách
                        </button>
                    </div>
                </div>

                <div class="lookup-shell">
                    <label class="form-group">
                        <span>{{ lane.subjectType === 'employee' ? 'Tìm nhân viên' : 'Tìm khách' }}</span>
                        <input
                            v-model="lane.query"
                            type="text"
                            class="form-control"
                            :placeholder="
                                lane.subjectType === 'employee'
                                    ? 'Tên hoặc mã nhân viên...'
                                    : 'Tên khách / host / SDT...'
                            "
                            @input="searchSubjects(lane)"
                        />
                    </label>

                    <div v-if="lane.searching" class="lookup-state">Đang tìm đối tượng...</div>
                    <div v-else-if="lane.query.trim().length >= 2 && !lane.searchResults.length" class="lookup-state muted">
                        Không tìm thấy kết quả phù hợp.
                    </div>

                    <div v-if="lane.searchResults.length" class="search-dropdown">
                        <button
                            v-for="item in lane.searchResults"
                            :key="item.key"
                            type="button"
                            class="search-result"
                            @click="pickSubject(lane, item)"
                        >
                            <div class="search-avatar" :class="item.kind">
                                {{ item.initials }}
                            </div>
                            <div class="search-copy">
                                <strong>{{ item.displayName }}</strong>
                                <span>{{ item.meta }}</span>
                            </div>
                        </button>
                    </div>
                </div>

                <div v-if="lane.subject" class="subject-card" :class="lane.resultTone || 'tone-idle'">
                    <div class="subject-avatar" :class="lane.subject.kind">
                        <img v-if="lane.subject.faceUrl" :src="lane.subject.faceUrl" alt="subject" />
                        <span v-else>{{ lane.subject.initials }}</span>
                    </div>
                    <div class="subject-copy">
                        <strong>{{ lane.subject.displayName }}</strong>
                        <span>{{ lane.subject.idLabel }}: {{ lane.subject.idValue }}</span>
                        <small>{{ lane.subject.meta }}</small>
                    </div>
                    <button type="button" class="subject-clear" @click="clearSubject(lane)">x</button>
                </div>

                <div class="entry-grid">
                    <label class="form-group">
                        <span>Biển số xe</span>
                        <input v-model.trim="lane.plateNumber" type="text" class="form-control" placeholder="VD: 29A-12345" />
                    </label>

                    <label class="form-group">
                        <span>QR động</span>
                        <input
                            v-model.trim="lane.qrPayload"
                            type="text"
                            class="form-control"
                            placeholder="EMP:... hoặc VIS:..."
                            @keyup.enter="verifyLane(lane)"
                        />
                    </label>
                </div>

                <label v-if="lane.subjectType === 'visitor'" class="form-group">
                    <span>Khu vực đỗ xe (nếu cần cấp giấy phép)</span>
                    <select v-model="lane.parkingAreaId" class="form-control">
                        <option :value="null">-- Bỏ qua --</option>
                        <option v-for="area in parkingAreas" :key="area.parkingAreaId || area.id" :value="area.parkingAreaId || area.id">
                            {{ area.name }} ({{ area.availableSpots ?? '?' }} chỗ)
                        </option>
                    </select>
                </label>

                <div class="wave-row">
                    <span v-for="n in 5" :key="`${lane.key}-${n}`" class="wave-bar" :class="{ active: n <= lane.waveLevel }"></span>
                </div>

                <div class="lane-actions">
                    <button class="btn btn-primary" :disabled="lane.busy || !canSubmit(lane)" @click="verifyLane(lane)">
                        {{ lane.busy ? 'Đang xử lý...' : 'Xác minh và cho qua' }}
                    </button>
                    <button class="btn btn-secondary" :disabled="lane.busy" @click="resetLane(lane)">Đặt lại làn</button>
                </div>

                <div v-if="lane.error" class="alert alert-danger lane-alert">{{ lane.error }}</div>
                <div v-if="lane.auditMessage" class="lane-receipt" :class="lane.resultTone || 'tone-idle'">
                    <strong>{{ lane.auditTitle }}</strong>
                    <span>{{ lane.auditMessage }}</span>
                </div>
            </section>
        </div>
    </div>
</template>

<script setup>
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { getAll as getEmployees, getProtectedFaceImage } from '../services/employeeApi'
import { getVisitorDirectory } from '../services/guestProfileApi'
import { verifyDynamicQr } from '../services/dynamicQrVerifyApi'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const loading = ref(false)
const laneOptions = ref([])
const parkingAreas = ref([])

const searchTimers = new Map()

const createLaneState = (order) =>
    reactive({
        key: `lane-${order}`,
        order,
        title: order === 1 ? 'Làn A bãi xe' : 'Làn B bãi xe',
        selectedLaneId: null,
        subjectType: 'employee',
        query: '',
        searching: false,
        searchResults: [],
        subject: null,
        qrPayload: '',
        plateNumber: '',
        parkingAreaId: null,
        busy: false,
        error: '',
        resultTone: 'tone-idle',
        resultLabel: '',
        auditTitle: '',
        auditMessage: '',
        waveLevel: 1,
    })

const lanes = reactive([createLaneState(1), createLaneState(2)])

const canSubmit = (lane) =>
    Boolean(lane.selectedLaneId && lane.subject && lane.qrPayload.trim() && lane.plateNumber.trim())

const normalizePlate = (value) => String(value || '').trim().toUpperCase()

const buildInitials = (text) => {
    const parts = String(text || '').trim().split(/\s+/).filter(Boolean)
    if (!parts.length) return '--'
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
    return `${parts[0][0] || ''}${parts[parts.length - 1][0] || ''}`.toUpperCase()
}

const makeSearchItem = (raw, kind) => {
    if (kind === 'employee') {
        const name = raw.fullName || raw.name || `EMP ${raw.employeeId}`
        return {
            key: `emp-${raw.employeeId}`,
            kind,
            displayName: name,
            initials: buildInitials(name),
            idValue: String(raw.employeeId || ''),
            matchIds: [raw.employeeId].filter(Boolean).map((value) => String(value).trim()),
            idLabel: 'Mã NV',
            meta: [raw.department, raw.employeeCode].filter(Boolean).join(' | ') || 'Nhân viên hệ thống',
            raw,
        }
    }

    const visitorIds = [raw.visitorDetailId, raw.visitorId, raw.guestId]
        .filter(Boolean)
        .map((value) => String(value).trim())
    const guestName = raw.fullName || raw.visitorName || `VIS ${raw.visitorDetailId || raw.guestId || ''}`
    return {
        key: `vis-${raw.visitorDetailId || raw.guestId || guestName}`,
        kind,
        displayName: guestName,
        initials: buildInitials(guestName),
        idValue: visitorIds[0] || '',
        matchIds: visitorIds,
        idLabel: 'Mã KH',
        meta: [raw.guestPhone, raw.hostEmployeeName, raw.companyName].filter(Boolean).join(' | ') || 'Khách được phê duyệt',
        raw,
    }
}

const assignDefaultLanes = () => {
    lanes.forEach((lane, index) => {
        if (!lane.selectedLaneId && laneOptions.value[index]) {
            lane.selectedLaneId = laneOptions.value[index].laneId
        }
    })
}

const loadBootstrap = async () => {
    loading.value = true
    try {
        const [laneRes, areaRes] = await Promise.all([
            enterpriseApi.getLaneHealth(),
            enterpriseApi.getParkingAreas({ pageSize: 100 }),
        ])
        laneOptions.value = Array.isArray(laneRes.data) ? laneRes.data : []
        parkingAreas.value = areaRes.data?.items || areaRes.data || []
        assignDefaultLanes()
    } catch (error) {
        console.error('Failed to load parking fallback bootstrap', error)
    } finally {
        loading.value = false
    }
}

const switchLaneType = (lane, type) => {
    lane.subjectType = type
    lane.query = ''
    lane.searchResults = []
    lane.error = ''
    lane.qrPayload = ''
    lane.auditTitle = ''
    lane.auditMessage = ''
    if (type === 'employee') lane.parkingAreaId = null
    clearSubject(lane)
}

const revokeLaneFace = (lane) => {
    if (lane.subject?.faceUrl?.startsWith('blob:')) {
        URL.revokeObjectURL(lane.subject.faceUrl)
    }
}

const clearSubject = (lane) => {
    revokeLaneFace(lane)
    lane.subject = null
    lane.searchResults = []
    lane.query = ''
}

const resetLane = (lane) => {
    revokeLaneFace(lane)
    lane.subjectType = 'employee'
    lane.query = ''
    lane.searchResults = []
    lane.subject = null
    lane.qrPayload = ''
    lane.plateNumber = ''
    lane.parkingAreaId = null
    lane.busy = false
    lane.error = ''
    lane.resultTone = 'tone-idle'
    lane.resultLabel = ''
    lane.auditTitle = ''
    lane.auditMessage = ''
    lane.waveLevel = 1
}

const searchSubjects = (lane) => {
    lane.error = ''
    const keyword = lane.query.trim()
    if (searchTimers.has(lane.key)) clearTimeout(searchTimers.get(lane.key))

    if (keyword.length < 2) {
        lane.searching = false
        lane.searchResults = []
        return
    }

    lane.searching = true
    const timer = setTimeout(async () => {
        try {
            if (lane.subjectType === 'employee') {
                const response = await getEmployees({ name: keyword, pageSize: 8 })
                const items = response.data?.items || response.data || []
                lane.searchResults = items.filter(Boolean).map((item) => makeSearchItem(item, 'employee'))
            } else {
                const response = await getVisitorDirectory({ query: keyword, pageSize: 8, registrationStatus: 'Approved' })
                const items = response.data?.items || []
                lane.searchResults = items.filter(Boolean).map((item) => makeSearchItem(item, 'visitor'))
            }
        } catch (error) {
            console.error('Search failed', error)
            lane.searchResults = []
        } finally {
            lane.searching = false
        }
    }, 260)

    searchTimers.set(lane.key, timer)
}

const pickSubject = async (lane, item) => {
    revokeLaneFace(lane)
    lane.subject = {
        kind: item.kind,
        displayName: item.displayName,
        initials: item.initials,
        idValue: item.idValue,
        idLabel: item.idLabel,
        meta: item.meta,
        raw: item.raw,
        faceUrl: '',
        visitId: item.raw.visitId || item.raw.latestVisitId || null,
        matchIds: item.matchIds?.length ? item.matchIds : [item.idValue].filter(Boolean),
    }
    lane.query = item.displayName
    lane.searchResults = []
    lane.error = ''

    if (item.kind === 'employee' && item.raw?.employeeId && item.raw?.faceImageUrl && !String(item.raw.faceImageUrl).startsWith('http')) {
        try {
            const blob = await getProtectedFaceImage(item.raw.employeeId)
            lane.subject.faceUrl = URL.createObjectURL(blob.data)
        } catch {
            lane.subject.faceUrl = ''
        }
    }
}

const extractVerifiedSubject = (verification) => {
    const data = verification?.data || {}
    const type = String(data?.type || '').toUpperCase()

    if (type === 'STATIC' || String(data?.visitorDetailId || data?.guestId || '').trim()) {
        return {
            kind: 'visitor',
            id: String(data?.visitorDetailId || data?.visitorId || data?.guestId || '').trim(),
        }
    }

    return {
        kind: 'employee',
        id: String(data?.employeeId || '').trim(),
    }
}

const logLaneEvent = async (lane, eventType, note) => {
    await enterpriseApi.recordLaneEvent({
        laneId: lane.selectedLaneId,
        eventType,
        direction: 'IN',
        plateText: normalizePlate(lane.plateNumber),
        note,
    })
}

const maybeCreateVisitorPermit = async (lane) => {
    if (lane.subjectType !== 'visitor' || !lane.parkingAreaId || !lane.subject?.visitId) return null

    return enterpriseApi.createParkingPermit({
        visitId: lane.subject.visitId,
        parkingAreaId: lane.parkingAreaId,
        validFromUtc: new Date().toISOString(),
        validToUtc: new Date(Date.now() + 24 * 3600000).toISOString(),
        plateNumber: normalizePlate(lane.plateNumber),
    })
}

const verifyLane = async (lane) => {
    if (!canSubmit(lane)) return

    lane.busy = true
    lane.error = ''
    lane.resultTone = 'tone-scanning'
    lane.resultLabel = 'Đang xác minh'
    lane.waveLevel = 3
    lane.auditTitle = ''
    lane.auditMessage = ''

    try {
        const verification = await verifyDynamicQr(lane.qrPayload.trim(), `manual-parking-${lane.key}`)
        if (!verification?.success) throw new Error(verification?.message || 'Xác minh QR thất bại.')

        const verified = extractVerifiedSubject(verification)
        const expectedKind = lane.subjectType === 'visitor' ? 'visitor' : 'employee'
        const expectedIds = Array.isArray(lane.subject?.matchIds)
            ? lane.subject.matchIds.map((value) => String(value).trim()).filter(Boolean)
            : [String(lane.subject?.idValue || '').trim()].filter(Boolean)
        const primaryExpectedId = expectedIds[0] || ''

        if (verified.kind !== expectedKind || !verified.id || !expectedIds.includes(verified.id)) {
            await logLaneEvent(
                lane,
                'MANUAL_PARKING_DENY',
                `[parking-fallback] mismatch; expected=${expectedKind}:${expectedIds.join(',')}; actual=${verified.kind}:${verified.id}`
            )
            lane.resultTone = 'tone-deny'
            lane.resultLabel = 'Từ chối'
            lane.waveLevel = 5
            lane.auditTitle = 'QR không khớp'
            lane.auditMessage = 'QR động không khớp đối tượng đã chọn. Sự kiện làn đã được ghi nhận.'
            return
        }

        await logLaneEvent(
            lane,
            'MANUAL_PARKING_ALLOW',
            `[parking-fallback] allow; subject=${expectedKind}:${primaryExpectedId}; plate=${normalizePlate(lane.plateNumber)}`
        )

        await maybeCreateVisitorPermit(lane).catch((error) => {
            console.warn('Parking permit issue failed', error)
        })

        lane.resultTone = 'tone-allow'
        lane.resultLabel = 'Đã cho qua'
        lane.waveLevel = 4
        lane.auditTitle = 'Vé gửi xe thủ công'
        lane.auditMessage =
            lane.subjectType === 'visitor' && lane.parkingAreaId
                ? 'Đã xác minh QR, ghi lane event và cố gắng cấp parking permit cho khách.'
                : 'Đã xác minh QR và ghi lane event cho thao tác gửi xe thủ công.'
    } catch (error) {
        const message = error?.response?.data?.message || error?.message || 'Không thể xác minh QR.'
        lane.error = message
        lane.resultTone = 'tone-deny'
        lane.resultLabel = 'Từ chối'
        lane.waveLevel = 5
        lane.auditTitle = 'Gửi xe thủ công bị chặn'
        lane.auditMessage = 'Xác minh thất bại. Sự kiện làn từ chối đã được ghi nhận nếu backend sẵn sàng.'
        try {
            await logLaneEvent(lane, 'MANUAL_PARKING_DENY', `[parking-fallback] error=${message}`)
        } catch (logError) {
            console.warn('Failed to record denied lane event', logError)
        }
    } finally {
        lane.busy = false
    }
}

onMounted(loadBootstrap)

onBeforeUnmount(() => {
    lanes.forEach((lane) => revokeLaneFace(lane))
    searchTimers.forEach((timer) => clearTimeout(timer))
    searchTimers.clear()
})
</script>

<style scoped>
.parking-fallback-page {
    display: grid;
    gap: 18px;
}

.parking-hero {
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
}

.page-subtitle {
    max-width: 760px;
    margin-top: 8px;
    color: var(--text-secondary);
}

.signal-strip {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 14px;
}

.signal-card {
    padding: 16px 18px;
    border-radius: 20px;
    background: linear-gradient(180deg, rgba(15, 23, 42, 0.94), rgba(15, 23, 42, 0.82));
    border: 1px solid var(--border-subtle);
    display: grid;
    gap: 8px;
}

.signal-card strong {
    font-size: 1.5rem;
    color: var(--text-primary);
}

.signal-label {
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.75rem;
}

.lane-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 18px;
}

.lane-shell {
    position: relative;
    overflow: hidden;
    padding: 20px;
    border-radius: 28px;
    background:
        radial-gradient(circle at top, rgba(255, 255, 255, 0.08), transparent 24%),
        linear-gradient(180deg, #08111c 0%, #102033 100%);
    border: 1px solid rgba(125, 211, 252, 0.16);
    box-shadow: 0 22px 42px rgba(2, 8, 23, 0.2);
}

.lane-shell-glow {
    position: absolute;
    inset: auto -30% -45% auto;
    width: 220px;
    height: 220px;
    border-radius: 999px;
    background: radial-gradient(circle, rgba(56, 189, 248, 0.2), transparent 65%);
    pointer-events: none;
}

.lane-head {
    position: relative;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
}

.lane-head h2 {
    margin: 4px 0 0;
    color: var(--text-primary);
    font-size: 1.2rem;
}

.lane-kicker {
    color: #7dd3fc;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.74rem;
    font-weight: 700;
}

.lane-status-chip {
    padding: 7px 12px;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 700;
    border: 1px solid var(--border-default);
    background: rgba(15, 23, 42, 0.72);
    color: var(--text-secondary);
    transition: border-color 0.25s ease, background 0.25s ease, color 0.25s ease;
}

.lane-config {
    position: relative;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 14px;
    margin-top: 18px;
}

.lane-type-toggle {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    align-self: end;
}

.type-pill {
    min-width: 106px;
    padding: 11px 14px;
    border-radius: 999px;
    border: 1px solid var(--border-default);
    background: rgba(15, 23, 42, 0.72);
    color: var(--text-secondary);
    font-weight: 700;
    transition: border-color 0.18s ease, background 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.type-pill:not(.active):hover {
    border-color: rgba(125, 211, 252, 0.36);
    background: rgba(15, 23, 42, 0.92);
    color: var(--text-primary);
    transform: translateY(-1px);
}

.type-pill.active {
    background: rgba(14, 165, 233, 0.2);
    border-color: rgba(56, 189, 248, 0.36);
    color: var(--text-primary);
}

.lookup-shell {
    position: relative;
    margin-top: 16px;
}

.lookup-state {
    margin-top: 8px;
    color: var(--text-secondary);
    font-size: 0.82rem;
}

.lookup-state.muted {
    color: var(--text-muted);
}

.search-dropdown {
    margin-top: 10px;
    display: grid;
    gap: 8px;
}

.search-result {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 11px 12px;
    border-radius: 16px;
    border: 1px solid var(--border-subtle);
    background: rgba(15, 23, 42, 0.62);
    color: var(--text-secondary);
    text-align: left;
    transition: border-color 0.18s ease, transform 0.18s ease, background 0.18s ease;
}

.search-result:hover {
    border-color: rgba(125, 211, 252, 0.28);
    transform: translateY(-1px);
}

.search-avatar,
.subject-avatar {
    width: 46px;
    height: 46px;
    border-radius: 16px;
    display: grid;
    place-items: center;
    font-weight: 800;
    color: var(--text-primary);
    flex-shrink: 0;
}

.search-avatar.employee,
.subject-avatar.employee {
    background: linear-gradient(135deg, #0ea5e9, #1d4ed8);
}

.search-avatar.visitor,
.subject-avatar.visitor {
    background: linear-gradient(135deg, #22c55e, #15803d);
}

.subject-avatar img {
    width: 100%;
    height: 100%;
    border-radius: inherit;
    object-fit: cover;
}

.search-copy,
.subject-copy {
    display: grid;
    gap: 3px;
    min-width: 0;
}

.search-copy strong,
.subject-copy strong {
    color: var(--text-primary);
}

.search-copy span,
.subject-copy span,
.subject-copy small {
    color: var(--text-muted);
    font-size: 0.8rem;
}

.subject-card {
    margin-top: 14px;
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    gap: 12px;
    align-items: center;
    padding: 14px;
    border-radius: 22px;
    background: rgba(15, 23, 42, 0.62);
    border: 1px solid var(--border-subtle);
}

.subject-clear {
    width: 34px;
    height: 34px;
    border-radius: 999px;
    border: 1px solid var(--border-subtle);
    background: rgba(15, 23, 42, 0.74);
    color: var(--text-secondary);
    transition: border-color 0.18s ease, background 0.18s ease, color 0.18s ease, transform 0.18s ease;
}

.subject-clear:hover {
    border-color: var(--border-danger);
    background: rgba(239, 68, 68, 0.14);
    color: var(--status-danger-text);
    transform: translateY(-1px);
}

.entry-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 14px;
    margin-top: 16px;
}

.form-group {
    display: grid;
    gap: 8px;
}

.form-group span {
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 700;
}

.form-control {
    min-height: 46px;
    border-radius: 16px;
    border: 1px solid var(--border-subtle);
    background: rgba(15, 23, 42, 0.82);
    color: var(--text-primary);
    padding: 0 14px;
}

.form-control::placeholder {
    color: var(--text-disabled);
}

.wave-row {
    display: flex;
    align-items: flex-end;
    gap: 5px;
    height: 22px;
    margin-top: 18px;
}

.wave-bar {
    width: 10px;
    height: 7px;
    border-radius: 999px;
    background: rgba(71, 85, 105, 0.78);
    transition: height 0.2s ease, background 0.2s ease, box-shadow 0.2s ease;
}

.wave-bar:nth-child(2) {
    height: 10px;
}

.wave-bar:nth-child(3) {
    height: 13px;
}

.wave-bar:nth-child(4) {
    height: 17px;
}

.wave-bar:nth-child(5) {
    height: 22px;
}

.wave-bar.active {
    background: linear-gradient(180deg, #7dd3fc 0%, #38bdf8 100%);
    box-shadow: 0 0 14px rgba(56, 189, 248, 0.34);
}

.lane-actions {
    display: flex;
    gap: 10px;
    margin-top: 18px;
}

.lane-actions .btn {
    flex: 1;
}

.lane-alert {
    margin-top: 14px;
}

.lane-receipt {
    margin-top: 14px;
    display: grid;
    gap: 6px;
    padding: 14px;
    border-radius: 18px;
    background: rgba(15, 23, 42, 0.72);
    border: 1px solid var(--border-subtle);
}

.lane-receipt strong {
    color: var(--text-primary);
}

.lane-receipt span {
    color: var(--text-secondary);
    font-size: 0.88rem;
}

.tone-idle {
    --tone-accent: rgba(56, 189, 248, 0.28);
}

.tone-scanning {
    --tone-accent: rgba(14, 165, 233, 0.38);
}

.tone-allow {
    --tone-accent: rgba(34, 197, 94, 0.38);
}

.tone-deny {
    --tone-accent: rgba(239, 68, 68, 0.42);
}

.lane-shell.tone-scanning,
.lane-shell.tone-allow,
.lane-shell.tone-deny,
.subject-card.tone-scanning,
.subject-card.tone-allow,
.subject-card.tone-deny,
.lane-status-chip.tone-scanning,
.lane-status-chip.tone-allow,
.lane-status-chip.tone-deny,
.lane-receipt.tone-scanning,
.lane-receipt.tone-allow,
.lane-receipt.tone-deny {
    border-color: var(--tone-accent);
}

.hero-actions,
.lane-actions,
.lane-type-toggle {
    flex-wrap: wrap;
}

@media (max-width: 1100px) {
    .lane-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 820px) {
    .parking-hero,
    .lane-config,
    .entry-grid,
    .signal-strip {
        grid-template-columns: 1fr;
    }

    .parking-hero {
        display: grid;
    }

    .lane-config {
        display: grid;
    }

    .entry-grid {
        grid-template-columns: 1fr;
    }

    .signal-strip {
        display: grid;
    }
}
</style>
