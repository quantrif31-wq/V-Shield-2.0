<template>
    <div class="profile-page page-container animate-in">
        <header class="profile-page__header">
            <div>
                <p class="eyebrow">TÀI KHOẢN CỦA TÔI</p>
                <h1 class="page-title">Hồ sơ cá nhân</h1>
                <p class="page-subtitle">Thông tin định danh và liên hệ trong hệ thống V‑Shield.</p>
            </div>
            <button class="refresh-button" type="button" :disabled="loading" @click="loadProfile"><span aria-hidden="true">↻</span>Làm mới</button>
        </header>

        <div v-if="loading" class="profile-loading" aria-live="polite">
            <div class="profile-loading__hero skeleton"></div><div class="profile-loading__card skeleton"></div><div class="profile-loading__card skeleton"></div>
        </div>
        <section v-else-if="error" class="profile-error" role="alert">
            <div class="profile-error__icon" aria-hidden="true">!</div><div><h2>Không thể tải hồ sơ</h2><p>{{ error }}</p></div><button class="retry-button" type="button" @click="loadProfile">Thử lại</button>
        </section>
        <template v-else-if="profile">
            <section class="identity-card">
                <div class="identity-card__glow"></div>
                <div class="identity-card__main"><div class="avatar" aria-hidden="true">{{ initials }}</div><div class="identity-card__name"><span class="identity-card__tag">HỒ SƠ ĐÃ XÁC THỰC</span><h2>{{ profile.fullName || 'Chưa cập nhật tên' }}</h2><p>{{ profile.positionName || 'Nhân sự nội bộ' }}<span v-if="profile.departmentName"> · {{ profile.departmentName }}</span></p></div></div>
                <div class="identity-card__id"><span>MÃ NHÂN VIÊN</span><strong>#{{ profile.employeeId }}</strong></div>
            </section>
            <div class="profile-content">
                <section class="detail-card"><div class="card-heading"><div class="card-heading__icon" aria-hidden="true">✦</div><div><h2>Thông tin liên hệ</h2><p>Kênh liên lạc đã đăng ký với hệ thống.</p></div></div><dl class="detail-list"><div class="detail-row"><dt><span class="field-icon" aria-hidden="true">@</span>Email</dt><dd>{{ profile.email || 'Chưa cập nhật' }}</dd></div><div class="detail-row"><dt><span class="field-icon" aria-hidden="true">⌕</span>Số điện thoại</dt><dd>{{ profile.phone || 'Chưa cập nhật' }}</dd></div></dl></section>
                <section class="detail-card"><div class="card-heading"><div class="card-heading__icon card-heading__icon--blue" aria-hidden="true">▦</div><div><h2>Thông tin công việc</h2><p>Vai trò và đơn vị đang công tác.</p></div></div><dl class="detail-list"><div class="detail-row"><dt>Phòng ban</dt><dd>{{ profile.departmentName || 'Chưa phân công' }}</dd></div><div class="detail-row"><dt>Chức vụ</dt><dd>{{ profile.positionName || 'Chưa phân công' }}</dd></div></dl></section>
                <aside class="account-note"><div class="account-note__status"><span></span>Tài khoản đang hoạt động</div><h2>Thông tin được bảo vệ</h2><p>Thông tin hồ sơ được dùng để xác thực thao tác và phân quyền truy cập trong V‑Shield.</p></aside>
            </div>
        </template>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getMyProfile } from '../services/employeeApi'

const profile = ref(null)
const loading = ref(true)
const error = ref(null)

const initials = computed(() => (profile.value?.fullName || 'VS').trim().split(/\s+/).filter(Boolean).slice(-2).map((part) => part[0]).join('').toUpperCase())

async function loadProfile() {
    loading.value = true
    error.value = null
    try {
        const res = await getMyProfile()
        profile.value = res.data
    } catch (e) {
        error.value = 'Không thể tải thông tin cá nhân.'
    } finally {
        loading.value = false
    }
}

onMounted(loadProfile)
</script>

<style scoped>
.profile-page { max-width: 1180px; margin: 0 auto; padding-bottom: 36px; }
.profile-page__header { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; margin-bottom: 24px; }
.eyebrow { margin: 0 0 7px; color: var(--accent-primary, #07838a); font-size: .72rem; font-weight: 800; letter-spacing: .12em; }.page-title { margin: 0; }.page-subtitle { margin: 7px 0 0; color: var(--text-secondary); }
.refresh-button, .retry-button { display: inline-flex; align-items: center; justify-content: center; gap: 8px; border: 1px solid var(--border-color); border-radius: 12px; background: var(--bg-card, #fff); color: var(--text-primary); font: inherit; font-weight: 700; cursor: pointer; transition: .2s ease; }.refresh-button { padding: 10px 15px; }.refresh-button:hover:not(:disabled), .retry-button:hover { border-color: var(--accent-primary, #07838a); color: var(--accent-primary, #07838a); transform: translateY(-1px); }.refresh-button:disabled { opacity: .55; cursor: wait; }
.identity-card { position: relative; isolation: isolate; display: flex; align-items: center; justify-content: space-between; gap: 28px; overflow: hidden; min-height: 190px; padding: 32px 36px; border-radius: 24px; color: #fff; background: linear-gradient(118deg, #063f54 0%, #087d83 56%, #0aa39a 100%); box-shadow: 0 18px 40px rgba(5,91,101,.19); }.identity-card::after { content: ''; position: absolute; z-index: -1; right: -64px; bottom: -146px; width: 370px; height: 370px; border: 1px solid rgba(255,255,255,.2); border-radius: 50%; box-shadow: 0 0 0 45px rgba(255,255,255,.06), 0 0 0 92px rgba(255,255,255,.04); }.identity-card__glow { position: absolute; z-index: -1; top: -80px; right: 20%; width: 210px; height: 210px; border-radius: 50%; background: rgba(130,242,222,.22); filter: blur(16px); }.identity-card__main { display: flex; align-items: center; min-width: 0; gap: 20px; }.avatar { display: grid; flex: 0 0 auto; width: 82px; height: 82px; place-items: center; border: 3px solid rgba(255,255,255,.62); border-radius: 24px; background: rgba(255,255,255,.17); box-shadow: 0 8px 22px rgba(0,0,0,.13); font-size: 1.65rem; font-weight: 800; letter-spacing: .03em; }.identity-card__tag { display: inline-flex; padding: 5px 9px; border-radius: 999px; background: rgba(255,255,255,.16); font-size: .65rem; font-weight: 800; letter-spacing: .08em; }.identity-card__name h2 { margin: 9px 0 5px; overflow-wrap: anywhere; font-size: clamp(1.45rem,2.4vw,2rem); line-height: 1.18; }.identity-card__name p { margin: 0; color: rgba(255,255,255,.8); font-size: .95rem; }.identity-card__id { display: flex; flex: 0 0 auto; flex-direction: column; min-width: 130px; padding: 13px 16px; border: 1px solid rgba(255,255,255,.24); border-radius: 14px; background: rgba(0,29,47,.16); }.identity-card__id span { color: rgba(255,255,255,.7); font-size: .65rem; font-weight: 800; letter-spacing: .09em; }.identity-card__id strong { margin-top: 5px; font-size: 1.1rem; }
.profile-content { display: grid; grid-template-columns: minmax(0,1fr) minmax(0,1fr) minmax(230px,.72fr); gap: 18px; margin-top: 20px; }.detail-card, .account-note, .profile-error { border: 1px solid var(--border-color); border-radius: 18px; background: var(--bg-card, #fff); box-shadow: 0 10px 24px rgba(19,55,74,.06); }.detail-card { padding: 23px; }.card-heading { display: flex; align-items: center; gap: 11px; padding-bottom: 19px; border-bottom: 1px solid var(--border-color); }.card-heading__icon { display: grid; width: 35px; height: 35px; place-items: center; border-radius: 11px; background: #dff6ed; color: #078263; font-weight: 900; }.card-heading__icon--blue { background: #e1effa; color: #23709c; }.card-heading h2, .account-note h2, .profile-error h2 { margin: 0; color: var(--text-primary); font-size: 1rem; }.card-heading p { margin: 3px 0 0; color: var(--text-secondary); font-size: .78rem; }.detail-list { margin: 5px 0 0; }.detail-row { display: flex; flex-direction: column; gap: 5px; padding: 16px 0 12px; border-bottom: 1px solid var(--border-color); }.detail-row:last-child { border-bottom: 0; padding-bottom: 0; }.detail-row dt { display: flex; align-items: center; gap: 7px; color: var(--text-secondary); font-size: .77rem; font-weight: 700; }.detail-row dd { margin: 0; overflow-wrap: anywhere; color: var(--text-primary); font-size: .93rem; font-weight: 700; }.field-icon { display: grid; width: 19px; height: 19px; place-items: center; border-radius: 6px; background: var(--bg-secondary, #edf5f6); color: var(--accent-primary, #07838a); font-size: .77rem; }.account-note { padding: 23px; background: linear-gradient(145deg,#f0fbf9,var(--bg-card,#fff)); }.account-note__status { display: inline-flex; align-items: center; gap: 7px; color: #08785e; font-size: .72rem; font-weight: 800; }.account-note__status span { width: 8px; height: 8px; border-radius: 50%; background: #18b782; box-shadow: 0 0 0 4px rgba(24,183,130,.12); }.account-note h2 { margin-top: 24px; font-size: 1.05rem; }.account-note p { margin: 9px 0 0; color: var(--text-secondary); font-size: .85rem; line-height: 1.65; }
.profile-loading { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }.profile-loading__hero { grid-column: 1 / -1; height: 190px; border-radius: 24px; }.profile-loading__card { height: 220px; border-radius: 18px; }.skeleton { background: linear-gradient(100deg,var(--bg-secondary,#eff4f5) 35%,rgba(255,255,255,.75) 50%,var(--bg-secondary,#eff4f5) 65%); background-size: 200% 100%; animation: shimmer 1.35s infinite; }.profile-error { display: flex; align-items: center; gap: 15px; max-width: 620px; padding: 22px; }.profile-error__icon { display: grid; flex: 0 0 auto; width: 36px; height: 36px; place-items: center; border-radius: 50%; background: #fff0ef; color: #c94d45; font-weight: 900; }.profile-error p { margin: 5px 0 0; color: var(--text-secondary); font-size: .9rem; }.retry-button { margin-left: auto; padding: 9px 13px; } @keyframes shimmer { to { background-position: -200% 0; } }
@media (max-width: 980px) { .profile-content { grid-template-columns: 1fr 1fr; }.account-note { grid-column: 1 / -1; } } @media (max-width: 680px) { .profile-page__header, .identity-card { align-items: flex-start; flex-direction: column; }.profile-page__header { margin-bottom: 18px; }.identity-card { min-height: 0; padding: 25px; gap: 20px; }.identity-card__id { width: 100%; box-sizing: border-box; }.profile-content, .profile-loading { grid-template-columns: 1fr; }.profile-loading__hero { grid-column: auto; }.account-note { grid-column: auto; }.profile-error { align-items: flex-start; flex-wrap: wrap; }.retry-button { margin-left: 51px; } }
</style>
