<template>
    <div class="login-page">
        <div class="login-theme-toggle">
            <button
                type="button"
                class="theme-toggle-btn"
                :aria-label="isDark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'"
                :title="isDark ? 'Giao diện sáng' : 'Giao diện tối'"
                @click="toggleTheme"
            >
                <svg v-if="isDark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                    <circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.42 1.42M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.42-1.42M17.66 6.34l1.41-1.41"/>
                </svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                    <path d="M21 12.8A9 9 0 1111.2 3 7 7 0 0021 12.8z"/>
                </svg>
            </button>
        </div>

        <div class="login-bg" aria-hidden="true">
            <div class="bg-orb orb-a"></div>
            <div class="bg-orb orb-b"></div>
            <div class="bg-orb orb-c"></div>
            <div class="bg-grid"></div>
        </div>

        <div class="login-shell animate-in">
            <section class="login-story">
                <span class="story-eyebrow">Trung tâm điều phối V-Shield</span>
                <h1>Kiểm soát ra vào rõ ràng và tin cậy.</h1>
                <p class="story-copy">
                    Theo dõi nhân sự, phương tiện, camera và cảnh báo trên cùng một không gian vận hành.
                </p>

                <div class="story-metrics">
                    <article class="metric-card">
                        <strong>24/7</strong>
                        <span>Giám sát liên tục</span>
                    </article>
                    <article class="metric-card">
                        <strong>QR động</strong>
                        <span>Xác thực chống dùng lại</span>
                    </article>
                    <article class="metric-card">
                        <strong>ANPR</strong>
                        <span>Đối soát biển số</span>
                    </article>
                </div>

                <div class="story-panel">
                    <div class="panel-heading">
                        <span class="panel-chip">Luồng vận hành</span>
                        <span class="panel-status">
                            <span class="panel-dot"></span>
                            Sẵn sàng
                        </span>
                    </div>

                    <div class="panel-steps">
                        <div class="panel-step">
                            <strong>01</strong>
                            <div>
                                <h3>Xác thực người vận hành</h3>
                                <p>Đảm bảo đúng vai trò truy cập trước khi vào trung tâm điều phối.</p>
                            </div>
                        </div>
                        <div class="panel-step">
                            <strong>02</strong>
                            <div>
                                <h3>Tổng hợp tín hiệu tại cổng</h3>
                                <p>Luồng nhân sự, phương tiện và camera được gom về một góc nhìn thống nhất.</p>
                            </div>
                        </div>
                        <div class="panel-step">
                            <strong>03</strong>
                            <div>
                                <h3>Ra quyết định nhanh</h3>
                                <p>Nhìn thấy trạng thái, nhật ký và cảnh báo ngay tại điểm cần can thiệp.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <section class="login-card">
                <div class="brand-lockup">
                    <div class="brand-mark">
                        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke="currentColor" stroke-width="1.9" stroke-linejoin="round" />
                            <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.24" />
                            <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round" />
                        </svg>
                    </div>
                    <div>
                        <span class="brand-kicker">Đăng nhập an toàn</span>
                        <h2>Truy cập trung tâm điều phối</h2>
                    </div>
                </div>

                <div class="login-intro">
                    <p>
                        Đăng nhập bằng tài khoản được cấp để theo dõi camera, kiểm tra lịch sử
                        và xử lý cảnh báo ngay tại trung tâm điều phối.
                    </p>
                </div>

                <form class="login-form" novalidate @submit.prevent="handleLogin">
                    <div class="form-group">
                        <label for="username">Tên đăng nhập</label>
                        <div class="input-shell">
                            <svg class="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
                                <circle cx="12" cy="7" r="4" />
                            </svg>
                            <input
                                id="username"
                                v-model="form.username"
                                type="text"
                                placeholder="Nhập tên đăng nhập"
                                autocomplete="username"
                                :disabled="loading"
                                @keydown.enter.prevent="handleLogin"
                            />
                        </div>
                    </div>

                    <div class="form-group">
                        <div class="label-row">
                            <label for="password">Mật khẩu truy cập</label>
                            <span class="field-hint" title="Liên hệ quản trị viên nếu bạn không thể đăng nhập">Không thể đăng nhập?</span>
                        </div>
                        <div class="input-shell">
                            <svg class="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <rect x="3" y="11" width="18" height="10" rx="2" />
                                <path d="M7 11V8a5 5 0 0110 0v3" />
                            </svg>
                            <input
                                id="password"
                                v-model="form.password"
                                :type="showPassword ? 'text' : 'password'"
                                placeholder="Nhập mật khẩu"
                                autocomplete="current-password"
                                :disabled="loading"
                                @keydown.enter.prevent="handleLogin"
                            />
                            <button
                                type="button"
                                class="toggle-password"
                                :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                                @click="showPassword = !showPassword"
                            >
                                <svg v-if="!showPassword" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
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

                    <div v-if="mfaRequired" class="form-group">
                        <div class="label-row">
                            <label for="mfa-code">Mã xác thực 6 số</label>
                            <span class="field-hint">Ứng dụng xác thực</span>
                        </div>
                        <div class="input-shell">
                            <svg class="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <rect x="4" y="4" width="16" height="16" rx="4" />
                                <path d="M9 12h6" />
                                <path d="M12 9v6" />
                            </svg>
                            <input
                                id="mfa-code"
                                ref="mfaInputRef"
                                v-model="form.mfaCode"
                                type="text"
                                inputmode="numeric"
                                maxlength="6"
                                placeholder="Nhập mã đang hiện trên ứng dụng"
                                autocomplete="one-time-code"
                                :disabled="loading"
                                aria-describedby="mfa-help"
                                @input="normalizeMfaCode"
                                @keydown.enter.prevent="handleLogin"
                            />
                        </div>
                        <p id="mfa-help" class="mfa-help">Mở ứng dụng xác thực và nhập mã gồm 6 chữ số đang hiển thị.</p>
                    </div>

                    <div v-if="mfaSetupSecret" class="mfa-setup">
                        <div class="mfa-setup-header">
                            <strong>Thiết lập xác thực hai lớp</strong>
                            <span>Quét QR bằng Authenticator rồi nhập mã 6 số.</span>
                        </div>
                        <div v-if="mfaQrDataUrl" class="mfa-qr-frame">
                            <img :src="mfaQrDataUrl" alt="Mã QR thiết lập MFA" />
                        </div>
                        <div class="mfa-manual-key">
                            <span>Mã thiết lập dự phòng</span>
                            <code>{{ mfaSetupSecret }}</code>
                        </div>
                        <small>{{ mfaSetupUri }}</small>
                    </div>

                    <transition name="slide-error">
                        <div v-if="feedbackMessage" class="login-alert" :class="feedbackType" :role="feedbackType === 'danger' ? 'alert' : 'status'" aria-live="polite">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <circle cx="12" cy="12" r="10" />
                                <path d="M12 8v4" />
                                <path d="M12 16h.01" />
                            </svg>
                            <span>{{ feedbackMessage }}</span>
                        </div>
                    </transition>

                    <BaseButton type="submit" size="large" class="btn-login" :loading="loading" :disabled="loading">
                        {{ mfaRequired ? 'Xác thực và đăng nhập' : 'Vào trung tâm điều phối' }}
                    </BaseButton>
                </form>

                <div v-if="ssoProviders.length" class="sso-divider">
                    <span class="sso-line"></span>
                    <span class="sso-or">Hoặc đăng nhập với SSO</span>
                    <span class="sso-line"></span>
                </div>

                <div v-if="ssoProviders.length" class="sso-buttons">
                    <button
                        v-for="p in ssoProviders"
                        :key="p.externalIdentityProviderId"
                        type="button"
                        class="btn-sso"
                        :disabled="loading"
                        @click="handleSSO(p)"
                    >
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="sso-icon">
                            <path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
                            <circle cx="8.5" cy="7" r="4"/>
                            <path d="M20 8v6"/><path d="M23 11h-6"/>
                        </svg>
                        <span>{{ p.name }}</span>
                    </button>
                </div>

                <div class="login-footer">
                    <div class="footer-item">
                        <strong>Kênh truy cập</strong>
                        <span>Bảo vệ, vận hành, quản trị</span>
                    </div>
                    <div class="footer-item">
                        <strong>Phạm vi</strong>
                        <span>Nhân sự, camera, phương tiện, lịch sử</span>
                    </div>
                </div>
            </section>
        </div>

        <ForcePasswordChange v-if="forcePasswordChange" @changed="handlePasswordChanged" />
    </div>
</template>

<script setup>
import { nextTick, onMounted, onUnmounted, reactive, ref } from 'vue'
import QRCode from 'qrcode'
import { useRoute, useRouter } from 'vue-router'
import { login } from '../stores/auth'
import { identityApi } from '../services/identityApi'
import { usePreferences } from '../composables/usePreferences'
import BaseButton from '../components/ui/BaseButton.vue'
import ForcePasswordChange from '../components/auth/ForcePasswordChange.vue'

const { isDark, toggleTheme } = usePreferences()
const router = useRouter()
const route = useRoute()

const form = reactive({ username: '', password: '', mfaCode: '' })
const loading = ref(false)
const feedbackMessage = ref('')
const feedbackType = ref('danger')
const error = feedbackMessage
const showPassword = ref(false)
const mfaRequired = ref(false)
const mfaSetupSecret = ref('')
const mfaSetupUri = ref('')
const mfaQrDataUrl = ref('')
const mfaInputRef = ref(null)
const forcePasswordChange = ref(false)
let redirectTimer = null
const ssoProviders = ref([])

onMounted(async () => {
    try {
        const res = await identityApi.getProviders()
        ssoProviders.value = (res.data || []).filter(p => p.isEnabled && p.protocol === 'OIDC')
    } catch {}
})

async function handleSSO(provider) {
    if (loading.value) return
    const redirectUri = window.location.origin + '/login'
    try {
        const res = await identityApi.oidcChallenge(
            provider.externalIdentityProviderId,
            redirectUri,
            null
        )
        window.location.href = res.data.challengeUrl
    } catch (err) {
        setFeedback(err.response?.data?.message || 'SSO initiation failed')
    }
}

function setFeedback(message, type = 'danger') {
    feedbackMessage.value = message
    feedbackType.value = type
}

function normalizeMfaCode(event) {
    const normalized = String(event.target.value || '').replace(/\D/g, '').slice(0, 6)
    if (form.mfaCode !== normalized) form.mfaCode = normalized
}

async function updateMfaQr(uri) {
    mfaQrDataUrl.value = ''
    if (!uri) {
        return
    }

    try {
        mfaQrDataUrl.value = await QRCode.toDataURL(uri, {
            errorCorrectionLevel: 'M',
            margin: 1,
            width: 220,
            color: {
                dark: '#102b3c',
                light: '#ffffff'
            }
        })
    } catch {
        mfaQrDataUrl.value = ''
    }
}

async function handleLogin() {
    if (loading.value) {
        return
    }

    if (redirectTimer) {
        clearTimeout(redirectTimer)
        redirectTimer = null
    }

    feedbackMessage.value = ''
    feedbackType.value = 'danger'
    if (!form.username.trim() || !form.password.trim()) {
        error.value = 'Vui lòng điền đầy đủ thông tin xác thực.'
        return
    }

    if (mfaRequired.value && !form.mfaCode.trim()) {
        error.value = 'Vui lòng nhập mã xác thực 6 số.'
        return
    }

    loading.value = true
    try {
        const result = await login(form.username, form.password, form.mfaCode || null)
        if (result?.requiresMfa) {
            mfaRequired.value = true
            mfaSetupSecret.value = result.mfaSetupSecret || ''
            mfaSetupUri.value = result.mfaSetupUri || ''
            await updateMfaQr(mfaSetupUri.value)
            feedbackType.value = 'danger'
            feedbackMessage.value = result.message || 'Tài khoản cần mã xác thực hai lớp.'
            await nextTick()
            mfaInputRef.value?.focus()
            return
        }

        feedbackType.value = 'success'
        feedbackMessage.value = 'Đăng nhập thành công. Đang chuyển vào trung tâm điều phối...'
        if (result?.requiresPasswordChange) {
            forcePasswordChange.value = true
            feedbackType.value = 'success'
            feedbackMessage.value = 'Bạn vừa thiết lập xác thực hai lớp lần đầu. Hãy đặt mật khẩu mới để bảo vệ tài khoản trước khi vào hệ thống.'
            return
        }
        redirectTimer = setTimeout(() => {
            const requestedPath = String(route.query.redirect || '')
            router.push(requestedPath.startsWith('/') && !requestedPath.startsWith('//') ? requestedPath : '/')
        }, 900)
    } catch (err) {
        if (err.response?.status === 401) {
            if (mfaRequired.value) {
                form.mfaCode = ''
                error.value = 'Mã xác thực không đúng hoặc đã hết hạn. Vui lòng thử mã mới.'
                await nextTick()
                mfaInputRef.value?.focus()
                return
            }
            error.value = 'Tên đăng nhập hoặc mật khẩu không đúng.'
            return
        } else if (err.code === 'ERR_NETWORK') {
            error.value = 'Không thể kết nối tới Core Server. Vui lòng kiểm tra API.'
        } else {
            error.value = err.response?.data?.message || 'Đã xảy ra lỗi hệ thống khi xác thực.'
        }
    } finally {
        loading.value = false
    }
}

async function handlePasswordChanged() {
    const requestedPath = String(route.query.redirect || '')
    router.push(requestedPath.startsWith('/') && !requestedPath.startsWith('//') ? requestedPath : '/')
}

onUnmounted(() => {
    if (redirectTimer) {
        clearTimeout(redirectTimer)
    }
})
</script>

<style scoped>
.login-page {
    min-height: 100vh;
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
    overflow-x: hidden;
    overflow-y: auto;
}

.login-bg {
    position: absolute;
    inset: 0;
    pointer-events: none;
}

.bg-orb {
    position: absolute;
    border-radius: 999px;
    filter: blur(90px);
    opacity: 0.35;
}

.orb-a {
    width: 440px;
    height: 440px;
    top: -120px;
    left: -120px;
    background: rgba(84, 196, 211, 0.34);
}

.orb-b {
    width: 380px;
    height: 380px;
    right: -60px;
    top: 18%;
    background: rgba(15, 124, 130, 0.22);
}

.orb-c {
    width: 320px;
    height: 320px;
    bottom: -80px;
    left: 38%;
    background: rgba(216, 155, 55, 0.16);
}

.bg-grid {
    position: absolute;
    inset: 0;
    background-image:
        linear-gradient(color-mix(in srgb, var(--text-primary) 4%, transparent) 1px, transparent 1px),
        linear-gradient(90deg, color-mix(in srgb, var(--text-primary) 4%, transparent) 1px, transparent 1px);
    background-size: 44px 44px;
}

.login-shell {
    position: relative;
    z-index: 1;
    width: min(1080px, 100%);
    display: grid;
    grid-template-columns: minmax(390px, 460px) minmax(0, 1fr);
    gap: 16px;
    align-items: center;
}

.login-story,
.login-card {
    border: 1px solid var(--border-subtle);
    background: color-mix(in srgb, var(--surface-default) 92%, transparent);
    backdrop-filter: var(--glass-blur);
    box-shadow: var(--shadow-lg);
}

.login-story {
    order: 2;
    padding: 24px 26px;
    border-radius: var(--radius-panel);
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    gap: 16px;
}

.story-eyebrow {
    display: inline-flex;
    align-items: center;
    align-self: flex-start;
    padding: 8px 14px;
    border-radius: 999px;
    background: rgba(15, 124, 130, 0.12);
    color: var(--accent-primary);
    font-size: 0.78rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.login-story h1 {
    font-family: var(--font-heading);
    font-size: clamp(2.25rem, 3vw, 3rem);
    font-weight: 700;
    line-height: 1.08;
    letter-spacing: -0.025em;
    color: var(--text-primary);
    max-width: 15ch;
}

.story-copy {
    max-width: 44ch;
    color: var(--text-secondary);
    font-size: 0.96rem;
}

.story-metrics {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.story-metrics .metric-card:last-child {
    display: none;
}

.metric-card {
    padding: 16px;
    border-radius: 20px;
    border: 1px solid var(--border-subtle);
    background: var(--surface-subtle);
    transition: transform var(--transition-fast), border-color var(--transition-fast);
}

.metric-card strong {
    display: block;
    font-family: var(--font-heading);
    font-size: 1.32rem;
    font-weight: 700;
    color: var(--text-primary);
}

.metric-card span {
    display: block;
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.84rem;
}

.story-panel {
    padding: 18px 20px;
    border-radius: 24px;
    background: linear-gradient(135deg, rgba(16, 32, 51, 0.96), rgba(24, 49, 77, 0.92));
    color: var(--text-inverse);
    overflow: hidden;
    position: relative;
}

.story-panel::before {
    content: '';
    position: absolute;
    inset: auto -60px -60px auto;
    width: 220px;
    height: 220px;
    border-radius: 50%;
    background: radial-gradient(circle, rgba(84, 196, 211, 0.24), transparent 70%);
}

.panel-heading {
    position: relative;
    z-index: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 14px;
}

.panel-chip {
    display: inline-flex;
    align-items: center;
    padding: 7px 12px;
    border-radius: 999px;
    background: rgba(84, 196, 211, 0.12);
    color: #b8f7ff;
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.panel-status {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    color: #d7fbff;
    font-size: 0.8rem;
    font-weight: 700;
}

.panel-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: #5de3c7;
    box-shadow: 0 0 0 5px rgba(93, 227, 199, 0.12);
}

.panel-steps {
    position: relative;
    z-index: 1;
    display: grid;
    gap: 10px;
}

.panel-step {
    display: grid;
    grid-template-columns: 44px minmax(0, 1fr);
    gap: 12px;
    padding: 10px 0;
    border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.panel-steps .panel-step:last-child {
    display: none;
}

.panel-step:first-child {
    border-top: none;
    padding-top: 0;
}

.panel-step strong {
    width: 40px;
    height: 40px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 14px;
    background: rgba(84, 196, 211, 0.1);
    color: #c6fbff;
    font-family: var(--font-heading);
    font-weight: 700;
}

.panel-step h3 {
    font-size: 0.93rem;
    font-weight: 700;
    color: #f3fdff;
}

.panel-step p {
    margin-top: 4px;
    color: rgba(222, 241, 246, 0.78);
    font-size: 0.82rem;
}

.login-card {
    order: 1;
    border-radius: var(--radius-panel);
    padding: 26px;
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    gap: 16px;
}

.brand-lockup {
    display: flex;
    align-items: center;
    gap: 16px;
}

.brand-mark {
    width: 58px;
    height: 58px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 20px;
    background: linear-gradient(135deg, rgba(84, 196, 211, 0.18), rgba(43, 109, 138, 0.16));
    color: var(--accent-primary);
}

.brand-mark svg {
    width: 30px;
    height: 30px;
}

.brand-kicker {
    display: block;
    margin-bottom: 4px;
    color: var(--text-muted);
    font-size: 0.72rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
}

.brand-lockup h2 {
    font-family: var(--font-heading);
    font-size: 1.48rem;
    font-weight: 700;
    line-height: 1.08;
    color: var(--text-primary);
}

.login-intro {
    display: grid;
    gap: 8px;
    padding: 12px 14px;
    border-radius: var(--radius-card);
    background: var(--surface-subtle);
    border: 1px solid var(--border-subtle);
}

.login-intro p {
    color: var(--text-secondary);
    font-size: 0.9rem;
    line-height: 1.55;
}

.login-badges {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.login-badges span {
    display: inline-flex;
    align-items: center;
    padding: 7px 11px;
    border-radius: 999px;
    background: var(--surface-subtle);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    font-size: 0.76rem;
    font-weight: 700;
}

.login-form {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.label-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.field-hint {
    color: var(--text-muted);
    font-size: 0.75rem;
}

.mfa-help {
    margin-top: 6px;
    color: var(--text-muted);
    font-size: var(--type-caption-size);
    line-height: var(--type-caption-line);
}

.input-shell {
    position: relative;
}

.input-shell input {
    width: 100%;
    min-height: 52px;
    padding: 0 48px 0 46px;
    border-radius: var(--radius-control);
    border: 1px solid var(--border-default);
    background: var(--bg-input);
    color: var(--text-primary);
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast);
}

.input-shell input::placeholder {
    color: var(--text-muted);
}

.input-shell input:focus {
    border-color: var(--border-focus);
    box-shadow: 0 0 0 4px rgba(84, 196, 211, 0.18);
    background: var(--surface-default);
}

.input-icon {
    position: absolute;
    left: 16px;
    top: 50%;
    transform: translateY(-50%);
    width: 18px;
    height: 18px;
    color: var(--text-muted);
}

.toggle-password {
    position: absolute;
    top: 50%;
    right: 10px;
    transform: translateY(-50%);
    width: 34px;
    height: 34px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 12px;
    color: var(--text-muted);
}

.toggle-password:hover {
    color: var(--accent-primary);
    background: rgba(15, 124, 130, 0.08);
}

.toggle-password svg {
    width: 18px;
    height: 18px;
}

.mfa-setup {
    display: grid;
    justify-items: center;
    gap: 12px;
    padding: 14px;
    border: 1px solid rgba(15, 124, 130, 0.18);
    border-radius: 8px;
    background: rgba(84, 196, 211, 0.08);
    color: var(--text-primary);
}

.mfa-setup-header {
    display: grid;
    gap: 4px;
    width: 100%;
    text-align: center;
}

.mfa-setup-header span,
.mfa-manual-key span {
    color: var(--text-muted);
    font-size: 0.82rem;
}

.mfa-qr-frame {
    display: grid;
    place-items: center;
    width: min(100%, 244px);
    padding: 12px;
    border: 1px solid rgba(16, 43, 60, 0.12);
    border-radius: 8px;
    background: #fff;
    box-shadow: 0 14px 30px rgba(16, 43, 60, 0.12);
}

.mfa-qr-frame img {
    display: block;
    width: min(100%, 220px);
    height: auto;
}

.mfa-manual-key {
    display: grid;
    gap: 6px;
    width: 100%;
}

.mfa-setup code,
.mfa-setup small {
    display: block;
    max-width: 100%;
    overflow-wrap: anywhere;
    word-break: break-word;
}

.mfa-setup code {
    padding: 8px;
    border-radius: 6px;
    background: var(--surface-default);
    color: var(--text-primary);
    border: 1px solid var(--border-subtle);
    font-size: 0.88rem;
}

.mfa-setup small {
    color: var(--text-muted);
    font-size: 0.78rem;
}

.login-alert {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    padding: 14px 16px;
    border-radius: var(--radius-control);
    font-size: 0.88rem;
    line-height: 1.45;
}

.login-alert.danger {
    border: 1px solid rgba(195, 81, 70, 0.18);
    background: rgba(195, 81, 70, 0.08);
    color: var(--accent-danger);
}

.login-alert.success {
    border: 1px solid rgba(42, 132, 97, 0.2);
    background: rgba(42, 132, 97, 0.1);
    color: #1d7a58;
}

.login-alert svg {
    width: 18px;
    height: 18px;
    flex-shrink: 0;
    margin-top: 1px;
}

.slide-error-enter-active,
.slide-error-leave-active {
    transition: all 0.22s ease;
}

.slide-error-enter-from,
.slide-error-leave-to {
    opacity: 0;
    transform: translateY(-8px);
}

.btn-login {
    min-height: 54px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    border-radius: var(--radius-control);
    background: var(--accent-gradient);
    color: #fff;
    font-weight: 700;
    box-shadow: var(--shadow-sm);
    transition: transform var(--transition-fast), box-shadow var(--transition-fast), filter var(--transition-fast);
}

.btn-login:hover:not(:disabled) {
    box-shadow: var(--shadow-md);
}

.btn-login:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}

.spinner {
    width: 20px;
    height: 20px;
    border: 3px solid rgba(255, 255, 255, 0.24);
    border-top-color: #fff;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}

.login-footer {
    margin-top: auto;
    padding-top: 4px;
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.footer-item {
    padding: 14px 16px;
    border-radius: 18px;
    background: var(--surface-subtle);
    border: 1px solid var(--border-subtle);
}

.footer-item strong {
    display: block;
    color: var(--text-primary);
    font-size: 0.86rem;
    font-weight: 700;
}

.footer-item span {
    display: block;
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.8rem;
    line-height: 1.5;
}

.sso-divider {
    display: flex;
    align-items: center;
    gap: 12px;
    margin: 6px 0;
}
.sso-line { flex: 1; height: 1px; background: var(--border-soft); }
.sso-or { font-size: 0.78rem; color: var(--text-muted); white-space: nowrap; text-transform: uppercase; letter-spacing: 0.04em; }

.sso-buttons {
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.btn-sso {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    min-height: 46px;
    border-radius: 16px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-primary);
    font-weight: 600;
    font-size: 0.9rem;
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
    cursor: pointer;
}
.btn-sso:hover:not(:disabled) {
    border-color: var(--primary);
    box-shadow: 0 0 0 3px rgba(84, 196, 211, 0.15);
}
.btn-sso:disabled { opacity: 0.6; cursor: not-allowed; }
.sso-icon { width: 18px; height: 18px; color: var(--text-muted); }

.login-theme-toggle {
    position: absolute;
    top: 20px;
    right: 20px;
    z-index: 10;
}

.theme-toggle-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 42px;
    height: 42px;
    border-radius: var(--radius-control);
    background: color-mix(in srgb, var(--surface-default) 90%, transparent);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    box-shadow: var(--shadow-sm);
    cursor: pointer;
    backdrop-filter: var(--glass-blur);
    transition: all var(--transition-fast);
}

.theme-toggle-btn:hover {
    color: var(--accent-primary);
    border-color: var(--border-focus);
    transform: translateY(-1px);
    box-shadow: var(--shadow-md);
}

.theme-toggle-btn svg {
    width: 20px;
    height: 20px;
}

@media (max-width: 900px) {
    .login-shell {
        grid-template-columns: 1fr;
        width: min(480px, 100%);
    }

    .login-story {
        display: none;
    }
}

@media (max-width: 768px) {
    .login-page {
        padding: 16px;
    }

    .login-story,
    .login-card {
        padding: 20px;
        border-radius: var(--radius-panel);
    }

    .story-metrics {
        grid-template-columns: 1fr;
    }

    .login-footer {
        display: none;
    }

    .label-row {
        align-items: flex-start;
        flex-direction: column;
        gap: 4px;
    }
}

@media (max-height: 760px) and (min-width: 901px) {
    .story-panel,
    .login-footer {
        display: none;
    }

    .login-card,
    .login-story {
        padding: 22px;
    }
}
</style>
