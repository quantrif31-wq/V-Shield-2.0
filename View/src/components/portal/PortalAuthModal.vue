<script setup>
import { ref } from 'vue'
import { portalApi } from '../../services/portalApi'

const props = defineProps({
  show: Boolean,
  currentUser: Object
})

const emit = defineEmits(['close', 'login-success', 'logout'])

const activeTab = ref('google') // 'google' | 'email'
const emailInput = ref('')
const nameInput = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

async function handleGoogleLoginMock() {
  isSubmitting.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const demoGoogleEmails = [
      'cyber_operator@gmail.com',
      'hikari_security@gmail.com',
      'ren_protocol@gmail.com',
      'vanguard_echo@gmail.com'
    ]
    const randomEmail = emailInput.value.trim() || demoGoogleEmails[Math.floor(Math.random() * demoGoogleEmails.length)]
    const randomName = nameInput.value.trim() || randomEmail.split('@')[0]

    const res = await portalApi.authGoogle({
      googleTokenOrEmail: randomEmail,
      fullName: randomName,
      photoUrl: `https://api.dicebear.com/7.x/adventurer/svg?seed=${encodeURIComponent(randomName)}`
    })

    if (res.success && res.data) {
      successMessage.value = `Chào mừng Operator ${res.data.fullName} đã gia nhập cộng đồng V-Shield!`
      localStorage.setItem('vshield_community_user', JSON.stringify(res.data))
      emit('login-success', res.data)
      setTimeout(() => {
        emit('close')
      }, 1000)
    } else {
      errorMessage.value = res.message || 'Đăng nhập thất bại, vui lòng thử lại!'
    }
  } catch (err) {
    errorMessage.value = 'Lỗi kết nối máy chủ Google OAuth!'
  } finally {
    isSubmitting.value = false
  }
}

function handleLogout() {
  localStorage.removeItem('vshield_community_user')
  emit('logout')
  emit('close')
}
</script>

<template>
  <div
    v-if="show"
    class="fixed inset-0 z-50 flex items-center justify-center p-4"
    role="dialog"
    aria-modal="true"
  >
    <!-- Backdrop with blur -->
    <div
      class="fixed inset-0 bg-slate-950/80 backdrop-blur-md transition-opacity"
      @click="emit('close')"
    ></div>

    <!-- Modal Box -->
    <div
      class="relative w-full max-w-md overflow-hidden rounded-3xl border border-cyan-500/40 bg-gradient-to-b from-slate-900/95 via-slate-950/95 to-slate-900/95 p-6 shadow-[0_0_50px_rgba(0,240,255,0.25)] text-slate-100"
    >
      <!-- Top Cyber Neon Line -->
      <div class="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-cyan-400 via-pink-500 to-amber-400"></div>

      <!-- Close Button -->
      <button
        type="button"
        @click="emit('close')"
        class="absolute top-4 right-4 rounded-full p-2 text-slate-400 hover:bg-slate-800 hover:text-white transition-colors"
      >
        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>

      <!-- Header -->
      <div class="text-center mb-6 pt-2">
        <div class="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl border border-cyan-400/40 bg-cyan-500/10 shadow-[0_0_20px_rgba(0,240,255,0.3)]">
          <span class="text-2xl">⚡</span>
        </div>
        <h3 class="text-xl font-extrabold tracking-wide text-transparent bg-clip-text bg-gradient-to-r from-cyan-300 via-white to-pink-300">
          {{ currentUser ? 'HỒ SƠ OPERATOR' : 'GIA NHẬP CỘNG ĐỒNG V-SHIELD' }}
        </h3>
        <p class="mt-1 text-xs text-slate-400">
          {{ currentUser ? 'Tài khoản cộng đồng để bình luận, đánh giá và nhận bản tin' : 'Đăng nhập Google để lưu đánh giá, gửi góp ý & nhận thông báo' }}
        </p>
      </div>

      <!-- Logged in state -->
      <div v-if="currentUser" class="space-y-4">
        <div class="flex items-center gap-4 rounded-2xl border border-cyan-500/30 bg-slate-900/80 p-4 shadow-inner">
          <img
            :src="currentUser.avatarUrl"
            :alt="currentUser.fullName"
            class="h-14 w-14 rounded-full border-2 border-cyan-400 object-cover shadow-[0_0_10px_rgba(0,240,255,0.5)]"
          />
          <div class="overflow-hidden">
            <h4 class="font-bold text-cyan-200 truncate">{{ currentUser.fullName }}</h4>
            <p class="text-xs text-slate-400 truncate">{{ currentUser.email }}</p>
            <span class="inline-block mt-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
              {{ currentUser.role }}
            </span>
          </div>
        </div>

        <button
          type="button"
          @click="handleLogout"
          class="w-full rounded-xl border border-rose-500/40 bg-rose-500/10 py-2.5 text-sm font-semibold text-rose-300 hover:bg-rose-500/20 transition-all"
        >
          Đăng xuất khỏi Cổng Cộng Đồng
        </button>
      </div>

      <!-- Login Form state -->
      <div v-else class="space-y-4">
        <!-- Error & Success Messages -->
        <div v-if="errorMessage" class="rounded-xl border border-rose-500/40 bg-rose-950/40 p-3 text-xs text-rose-300">
          {{ errorMessage }}
        </div>
        <div v-if="successMessage" class="rounded-xl border border-emerald-500/40 bg-emerald-950/40 p-3 text-xs text-emerald-300">
          {{ successMessage }}
        </div>

        <!-- Google One-Click CTA Button -->
        <button
          type="button"
          :disabled="isSubmitting"
          @click="handleGoogleLoginMock"
          class="group relative flex w-full items-center justify-center gap-3 rounded-2xl border border-cyan-400/50 bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 px-4 py-3 text-sm font-bold text-white shadow-[0_0_20px_rgba(0,240,255,0.2)] transition-all hover:border-cyan-300 hover:shadow-[0_0_30px_rgba(0,240,255,0.4)] disabled:opacity-50"
        >
          <svg class="h-5 w-5" viewBox="0 0 24 24">
            <path
              fill="#4285F4"
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
            />
            <path
              fill="#34A853"
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
            />
            <path
              fill="#FBBC05"
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"
            />
            <path
              fill="#EA4335"
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"
            />
          </svg>
          <span>Tiếp tục với Google SSO</span>
        </button>

        <div class="relative my-3 text-center">
          <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-800"></div></div>
          <span class="relative bg-slate-900 px-3 text-[11px] text-slate-500 uppercase tracking-widest">hoặc nhập thông tin</span>
        </div>

        <!-- Custom Name & Email Form -->
        <div class="space-y-3">
          <div>
            <label class="block text-xs font-semibold text-slate-300 mb-1">Tên hiển thị / Bí danh Operator</label>
            <input
              v-model="nameInput"
              type="text"
              placeholder="VD: CyberOperator_01, Hikari..."
              class="w-full rounded-xl border border-slate-700/80 bg-slate-950/70 px-3.5 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400"
            />
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-300 mb-1">Email nhận thư & thông báo</label>
            <input
              v-model="emailInput"
              type="email"
              placeholder="operator@domain.com"
              class="w-full rounded-xl border border-slate-700/80 bg-slate-950/70 px-3.5 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none focus:ring-1 focus:ring-cyan-400"
            />
          </div>

          <button
            type="button"
            :disabled="isSubmitting"
            @click="handleGoogleLoginMock"
            class="w-full rounded-xl bg-gradient-to-r from-cyan-500 via-teal-500 to-cyan-600 py-2.5 text-sm font-bold text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.3)] hover:brightness-110 transition-all disabled:opacity-50"
          >
            {{ isSubmitting ? 'Đang xác thực...' : 'Xác Nhận Đăng Nhập' }}
          </button>
        </div>
      </div>

      <!-- Footer Note -->
      <div class="mt-6 text-center text-[11px] text-slate-500">
        Bạn muốn truy cập bảng điều khiển vận hành nội bộ?
        <router-link to="/login" class="text-cyan-400 hover:underline font-semibold ml-1">
          Đăng nhập Hệ thống Quản trị →
        </router-link>
      </div>
    </div>
  </div>
</template>
