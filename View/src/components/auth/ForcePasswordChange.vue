<template>
    <div class="fpc">
        <div class="fpc-card">
            <div class="fpc-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <rect x="4" y="10" width="16" height="10" rx="2" stroke="currentColor" stroke-width="1.8" />
                    <path d="M8 10V7a4 4 0 1 1 8 0v3" stroke="currentColor" stroke-width="1.8" />
                    <circle cx="12" cy="15" r="1.4" fill="currentColor" />
                    <path d="M12 15v2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                </svg>
            </div>

            <span class="fpc-kicker">Nâng cấp tài khoản</span>
            <h2 class="fpc-title">Đổi mật khẩu để bảo vệ tài khoản</h2>
            <p class="fpc-copy">
                Bạn vừa kích hoạt xác thực hai lớp (MFA) lần đầu. Hệ thống yêu cầu đặt mật khẩu mới
                trước khi vào trung tâm điều phối. Thao tác này chỉ diễn ra một lần.
            </p>

            <form class="fpc-form" novalidate @submit.prevent="submit">
                <div v-if="feedbackMessage" class="fpc-alert" :class="feedbackType" role="alert" aria-live="polite">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                        <circle cx="12" cy="12" r="10" />
                        <path d="M12 8v4" />
                        <path d="M12 16h.01" />
                    </svg>
                    <span>{{ feedbackMessage }}</span>
                </div>

                <div class="fpc-group">
                    <label for="fpc-current">Mật khẩu hiện tại</label>
                    <div class="fpc-input-shell">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="fpc-input-icon">
                            <rect x="3" y="11" width="18" height="10" rx="2" />
                            <path d="M7 11V8a5 5 0 0110 0v3" />
                        </svg>
                        <input
                            id="fpc-current"
                            v-model="form.currentPassword"
                            :type="showCurrent ? 'text' : 'password'"
                            autocomplete="current-password"
                            placeholder="Nhập mật khẩu bạn đang dùng"
                            :disabled="loading"
                            @keydown.enter.prevent="submit"
                        />
                        <button
                            type="button"
                            class="fpc-eye"
                            :aria-label="showCurrent ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                            @click="showCurrent = !showCurrent"
                        >
                            <svg v-if="!showCurrent" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                                <circle cx="12" cy="12" r="3" />
                            </svg>
                            <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M17.94 17.94A10.94 10.94 0 0112 20c-7 0-11-8-11-8a21.76 21.76 0 015.17-5.94" />
                                <path d="M9.88 9.88A3 3 0 0114.12 14.12" />
                                <path d="M22.54 11.88A21.64 21.64 0 0017 6.12" />
                                <line x1="1" y1="1" x2="23" y2="23" />
                            </svg>
                        </button>
                    </div>
                </div>

                <div class="fpc-group">
                    <label for="fpc-new">Mật khẩu mới</label>
                    <div class="fpc-input-shell">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="fpc-input-icon">
                            <rect x="3" y="11" width="18" height="10" rx="2" />
                            <path d="M7 11V8a5 5 0 0110 0v3" />
                        </svg>
                        <input
                            id="fpc-new"
                            v-model="form.newPassword"
                            :type="showNew ? 'text' : 'password'"
                            autocomplete="new-password"
                            placeholder="Tối thiểu 6 ký tự"
                            :disabled="loading"
                            :class="{ 'has-error': strength === 'weak' && form.newPassword }"
                            @keydown.enter.prevent="submit"
                            @input="onNewInput"
                        />
                        <button
                            type="button"
                            class="fpc-eye"
                            :aria-label="showNew ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                            @click="showNew = !showNew"
                        >
                            <svg v-if="!showNew" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                                <circle cx="12" cy="12" r="3" />
                            </svg>
                            <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M17.94 17.94A10.94 10.94 0 0112 20c-7 0-11-8-11-8a21.76 21.76 0 015.17-5.94" />
                                <path d="M9.88 9.88A3 3 0 0114.12 14.12" />
                                <path d="M22.54 11.88A21.64 21.64 0 0017 6.12" />
                                <line x1="1" y1="1" x2="23" y2="23" />
                            </svg>
                        </button>
                    </div>
                    <div v-if="form.newPassword" class="fpc-meter" :class="strength" aria-hidden="true">
                        <span></span><span></span><span></span>
                    </div>
                </div>

                <div class="fpc-group">
                    <label for="fpc-confirm">Nhập lại mật khẩu mới</label>
                    <div class="fpc-input-shell">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="fpc-input-icon">
                            <rect x="3" y="11" width="18" height="10" rx="2" />
                            <path d="M7 11V8a5 5 0 0110 0v3" />
                        </svg>
                        <input
                            id="fpc-confirm"
                            v-model="form.confirmPassword"
                            :type="showConfirm ? 'text' : 'password'"
                            autocomplete="new-password"
                            placeholder="Nhập lại mật khẩu mới"
                            :disabled="loading"
                            @keydown.enter.prevent="submit"
                        />
                    </div>
                </div>

                <button type="submit" class="fpc-submit" :disabled="loading">
                    <span v-if="loading" class="fpc-spinner" aria-hidden="true"></span>
                    <span v-else class="fpc-shield" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M12 2L3 7v5c0 5 4 8 9 10 5-2 9-5 9-10V7L12 2z" stroke-linejoin="round" />
                            <path d="M9 12l2 2 4-4" stroke-linecap="round" stroke-linejoin="round" />
                        </svg>
                    </span>
                    {{ loading ? 'Đang cập nhật…' : 'Cập nhật và tiếp tục' }}
                </button>
            </form>

            <button type="button" class="fpc-logout" :disabled="loading" @click="handleLogout">
                Đăng xuất và thử lại sau
            </button>
        </div>
    </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { changePassword, logout } from '../../stores/auth'

const emit = defineEmits(['changed'])

const router = useRouter()
const loading = ref(false)
const feedbackMessage = ref('')
const feedbackType = ref('danger')
const showCurrent = ref(false)
const showNew = ref(false)
const showConfirm = ref(false)
const strength = ref('')

const form = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })

function setError(message) {
    feedbackType.value = 'danger'
    feedbackMessage.value = message
}

function clearFeedback() {
    feedbackMessage.value = ''
}

function onNewInput() {
    evaluateStrength()
}

function evaluateStrength() {
    const value = form.newPassword
    if (!value) {
        strength.value = ''
        return
    }

    let score = 0
    if (value.length >= 6) score += 1
    if (value.length >= 10) score += 1
    if (/[A-Z]/.test(value) && /[a-z]/.test(value)) score += 1
    if (/\d/.test(value)) score += 1
    if (/[^A-Za-z0-9]/.test(value)) score += 1

    strength.value = score <= 2 ? 'weak' : score <= 4 ? 'medium' : 'strong'
}

async function submit() {
    if (loading.value) return

    clearFeedback()

    if (!form.currentPassword.trim()) {
        setError('Vui lòng nhập mật khẩu hiện tại.')
        return
    }
    if (!form.newPassword) {
        setError('Vui lòng nhập mật khẩu mới.')
        return
    }
    if (form.newPassword.length < 6) {
        setError('Mật khẩu mới phải có ít nhất 6 ký tự.')
        return
    }
    if (form.newPassword === form.currentPassword) {
        setError('Mật khẩu mới phải khác mật khẩu hiện tại.')
        return
    }
    if (form.newPassword !== form.confirmPassword) {
        setError('Nhập lại mật khẩu mới không khớp.')
        return
    }

    loading.value = true
    try {
        await changePassword(form.currentPassword, form.newPassword)
        form.currentPassword = ''
        form.newPassword = ''
        form.confirmPassword = ''
        feedbackType.value = 'success'
        feedbackMessage.value = 'Đổi mật khẩu thành công. Đang đưa bạn vào hệ thống…'
        emit('changed')
    } catch (err) {
        const message =
            err.response?.data?.message ||
            (err.code === 'ERR_NETWORK'
                ? 'Không thể kết nối tới Core Server. Vui lòng kiểm tra lại.'
                : 'Đã xảy ra lỗi khi đổi mật khẩu.')
        setError(message)
    } finally {
        loading.value = false
    }
}

async function handleLogout() {
    if (loading.value) return
    await logout()
    router.replace({ name: 'Login' })
}
</script>

<style scoped>
.fpc {
    position: fixed;
    inset: 0;
    z-index: 1200;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
    background: rgba(16, 32, 51, 0.5);
    backdrop-filter: blur(10px);
    overflow-y: auto;
}

.fpc-card {
    width: min(480px, 100%);
    padding: 30px 32px 24px;
    border-radius: 28px;
    border: 1px solid rgba(255, 255, 255, 0.66);
    background: radial-gradient(circle at top right, rgba(84, 196, 211, 0.12), transparent 46%),
        linear-gradient(180deg, rgba(255, 255, 255, 0.97), rgba(250, 253, 254, 0.96));
    box-shadow: 0 44px 110px rgba(16, 32, 51, 0.28);
    animation: fpc-in 0.34s cubic-bezier(0.22, 1, 0.36, 1);
}

.fpc-icon {
    width: 62px;
    height: 62px;
    display: grid;
    place-items: center;
    border-radius: 20px;
    background: linear-gradient(135deg, var(--teal-500), var(--steel-500));
    color: #fff;
    box-shadow: 0 16px 34px rgba(15, 124, 130, 0.34);
    margin-bottom: 18px;
}

.fpc-icon svg {
    width: 30px;
    height: 30px;
}

.fpc-kicker {
    display: inline-flex;
    align-items: center;
    padding: 6px 12px;
    border-radius: 999px;
    background: rgba(216, 155, 55, 0.14);
    color: var(--warning-500);
    font-size: 0.75rem;
    font-weight: 800;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.fpc-title {
    margin-top: 14px;
    font-family: var(--font-heading);
    font-size: 1.62rem;
    font-weight: 700;
    letter-spacing: -0.025em;
    color: var(--ink-950);
    line-height: 1.15;
}

.fpc-copy {
    margin-top: 10px;
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.6;
}

.fpc-form {
    margin-top: 22px;
    display: grid;
    gap: 16px;
}

.fpc-alert {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    padding: 12px 14px;
    border-radius: 14px;
    font-size: 0.9rem;
    font-weight: 600;
    line-height: 1.45;
}

.fpc-alert svg {
    width: 20px;
    height: 20px;
    flex-shrink: 0;
    margin-top: 1px;
}

.fpc-alert.danger {
    background: rgba(195, 81, 70, 0.1);
    color: var(--danger-500);
}

.fpc-alert.success {
    background: rgba(20, 134, 109, 0.1);
    color: var(--success-500);
}

.fpc-group {
    display: grid;
    gap: 8px;
}

.fpc-group label {
    color: var(--text-secondary);
    font-size: 0.84rem;
    font-weight: 700;
}

.fpc-input-shell {
    position: relative;
    display: flex;
    align-items: center;
}

.fpc-input-icon {
    position: absolute;
    left: 15px;
    width: 18px;
    height: 18px;
    color: var(--text-muted);
    pointer-events: none;
}

.fpc-input-shell input {
    width: 100%;
    min-height: 50px;
    padding: 0 46px 0 46px;
    border: 1px solid rgba(24, 49, 77, 0.12);
    border-radius: 15px;
    background: #eef4f6;
    color: var(--ink-950);
    font-size: 0.98rem;
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast);
}

.fpc-input-shell input:focus {
    border-color: rgba(15, 124, 130, 0.42);
    background: #fff;
    box-shadow: 0 0 0 4px rgba(84, 196, 211, 0.16);
}

.fpc-input-shell input::placeholder {
    color: var(--text-muted);
}

.fpc-eye {
    position: absolute;
    right: 10px;
    width: 36px;
    height: 36px;
    display: grid;
    place-items: center;
    border-radius: 10px;
    color: var(--text-muted);
}

.fpc-eye:hover {
    color: var(--accent-primary);
    background: rgba(15, 124, 130, 0.08);
}

.fpc-eye svg {
    width: 19px;
    height: 19px;
}

.fpc-meter {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 6px;
}

.fpc-meter span {
    height: 5px;
    border-radius: 999px;
    background: rgba(24, 49, 77, 0.1);
}

.fpc-meter.weak span:nth-child(1) {
    background: var(--danger-500);
}

.fpc-meter.medium span:nth-child(1),
.fpc-meter.medium span:nth-child(2) {
    background: var(--warning-500);
}

.fpc-meter.strong span {
    background: var(--success-500);
}

.fpc-submit {
    min-height: 54px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    margin-top: 4px;
    border-radius: 999px;
    background: linear-gradient(135deg, var(--teal-500), var(--steel-500));
    color: #fff;
    font-weight: 700;
    font-size: 1rem;
    box-shadow: 0 16px 32px rgba(15, 124, 130, 0.26);
    transition: transform var(--transition-fast), box-shadow var(--transition-fast), opacity var(--transition-fast);
}

.fpc-submit:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 20px 40px rgba(15, 124, 130, 0.32);
}

.fpc-submit:disabled {
    opacity: 0.65;
    cursor: not-allowed;
}

.fpc-spinner {
    width: 18px;
    height: 18px;
    border: 2px solid currentColor;
    border-right-color: transparent;
    border-radius: 50%;
    animation: fpc-spin 0.7s linear infinite;
}

.fpc-shield {
    display: grid;
    place-items: center;
}

.fpc-shield svg {
    width: 19px;
    height: 19px;
}

.fpc-logout {
    margin: 18px auto 0;
    display: block;
    color: var(--text-muted);
    font-size: 0.88rem;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 3px;
}

.fpc-logout:hover:not(:disabled) {
    color: var(--danger-500);
}

@keyframes fpc-in {
    from {
        opacity: 0;
        transform: translateY(22px) scale(0.97);
    }
    to {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
}

@keyframes fpc-spin {
    to {
        transform: rotate(360deg);
    }
}

@media (prefers-reduced-motion: reduce) {
    .fpc-card,
    .fpc-spinner {
        animation: none;
    }
}

@media (max-width: 520px) {
    .fpc-card {
        padding: 30px 20px 20px;
        border-radius: 22px;
    }
}
</style>