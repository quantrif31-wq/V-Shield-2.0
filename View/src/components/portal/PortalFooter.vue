<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { portalApi } from '../../services/portalApi'

const router = useRouter()
const newsletterEmail = ref('')
const newsletterSubmitting = ref(false)
const newsletterToast = ref('')

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

function navigateTo(path) {
  triggerSfx()
  router.push(path)
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

async function subscribeNewsletter() {
  if (!newsletterEmail.value.trim() || !newsletterEmail.value.includes('@')) {
    newsletterToast.value = 'Vui lòng nhập địa chỉ email hợp lệ!'
    return
  }
  triggerSfx()
  newsletterSubmitting.value = true
  try {
    const res = await portalApi.subscribeNewsletter({ email: newsletterEmail.value })
    if (res.success) {
      newsletterToast.value = '✨ Đăng ký nhận tin tức thành công!'
      newsletterEmail.value = ''
      setTimeout(() => { newsletterToast.value = '' }, 4000)
    }
  } catch {
    newsletterToast.value = 'Không thể đăng ký lúc này, vui lòng thử lại sau.'
  } finally {
    newsletterSubmitting.value = false
  }
}
</script>

<template>
  <footer class="relative z-10 border-t border-cyan-500/20 bg-slate-950/95 text-slate-400">
    <!-- Top Cyber Accent Line -->
    <div class="h-0.5 w-full bg-gradient-to-r from-transparent via-cyan-500 via-pink-500 to-transparent opacity-60"></div>

    <div class="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8 lg:py-16">
      <div class="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-8">
        <!-- Col 1: Brand & Lore (4 cols) -->
        <div class="space-y-4 lg:col-span-4">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-400/40 bg-cyan-950/60 shadow-[0_0_15px_rgba(0,240,255,0.3)]">
              <svg class="h-5 w-5 text-cyan-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
                <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.35" />
              </svg>
            </div>
            <span class="text-xl font-black tracking-wider text-transparent bg-clip-text bg-gradient-to-r from-cyan-300 via-teal-200 to-pink-400 font-mono">
              V-SHIELD 2.0
            </span>
          </div>
          <p class="text-xs leading-relaxed text-slate-400">
            Hệ thống kiểm soát an ninh thông minh đa nền tảng kết hợp AI sinh trắc học thời gian thực, rào chắn thông minh và giao thức đồng bộ lai (Hybrid Sync).
          </p>
          <div class="flex items-center gap-2 pt-2">
            <div class="flex items-center gap-1.5 rounded-full border border-emerald-500/30 bg-emerald-950/40 px-2.5 py-1 text-[10px] font-bold text-emerald-400">
              <span class="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-ping"></span>
              <span>HỆ THỐNG TRỰC TUYẾN 99.99%</span>
            </div>
            <span class="rounded-full border border-cyan-500/30 bg-cyan-950/40 px-2.5 py-1 text-[10px] font-mono text-cyan-300">
              v2.0.0-PROD
            </span>
          </div>
        </div>

        <!-- Col 2: Hệ Sinh Thái (2 cols) -->
        <div class="space-y-3 lg:col-span-2">
          <h4 class="text-xs font-black uppercase tracking-widest text-cyan-300 font-mono">
            Hệ Sinh Thái
          </h4>
          <ul class="space-y-2 text-xs">
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                AI Face ID 60FPS
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                Virtual Smart Barrier
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                QR Động TOTP Chống Gian Lận
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                VoIP Video Call WebRTC
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                Hybrid Sync Offline-First
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-cyan-300 transition">
                UEBA Giám Sát Hành Vi
              </button>
            </li>
          </ul>
        </div>

        <!-- Col 3: Điều Hướng (2 cols) -->
        <div class="space-y-3 lg:col-span-2">
          <h4 class="text-xs font-black uppercase tracking-widest text-pink-400 font-mono">
            Khám Phá
          </h4>
          <ul class="space-y-2 text-xs">
            <li>
              <button type="button" @click="navigateTo('/')" class="hover:text-pink-300 transition">
                Trang Chủ (Overview)
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/roadmap')" class="hover:text-pink-300 transition">
                Lịch Sử & Lộ Trình
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/download')" class="hover:text-pink-300 transition">
                Tải APK Android
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/community')" class="hover:text-pink-300 transition">
                Đánh Giá & Cộng Đồng
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/about')" class="hover:text-pink-300 transition">
                Về Nhóm Sáng Lập
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/contact')" class="hover:text-pink-300 transition">
                Góp Ý & Hỗ Trợ
              </button>
            </li>
          </ul>
        </div>

        <!-- Col 4: Newsletter & Liên hệ (4 cols) -->
        <div class="space-y-3 lg:col-span-4">
          <h4 class="text-xs font-black uppercase tracking-widest text-cyan-300 font-mono">
            Đăng Ký Nhận Bản Tin
          </h4>
          <p class="text-xs text-slate-400">
            Nhận thông báo cập nhật bản vá bảo mật, thông số kỹ thuật mới nhất và bản tin đồ án.
          </p>

          <form @submit.prevent="subscribeNewsletter" class="space-y-2">
            <div class="relative flex items-center">
              <input
                v-model="newsletterEmail"
                type="email"
                placeholder="operator@v-shield.site"
                class="w-full rounded-xl border border-slate-700 bg-slate-900/90 px-3.5 py-2 text-xs text-slate-200 placeholder-slate-500 outline-none transition focus:border-cyan-400 focus:shadow-[0_0_15px_rgba(0,240,255,0.3)]"
                :disabled="newsletterSubmitting"
              />
              <button
                type="submit"
                :disabled="newsletterSubmitting"
                class="absolute right-1 rounded-lg bg-gradient-to-r from-cyan-500 to-pink-500 px-3 py-1.5 text-xs font-bold text-slate-950 transition hover:opacity-90 disabled:opacity-50"
              >
                {{ newsletterSubmitting ? '...' : 'Gửi' }}
              </button>
            </div>
            <div v-if="newsletterToast" class="text-[11px] font-semibold text-cyan-300">
              {{ newsletterToast }}
            </div>
          </form>

          <div class="pt-2 text-[11px] text-slate-500">
            <div>GVHD: <strong>ThS. Phan Hoàng Khải</strong></div>
            <div>Đồ án tốt nghiệp Kỹ sư CNTT / ATTT - 2026</div>
          </div>
        </div>
      </div>

      <!-- Bottom Credits -->
      <div class="mt-10 flex flex-col items-center justify-between gap-4 border-t border-slate-800/80 pt-6 text-xs text-slate-500 sm:flex-row">
        <div class="flex items-center gap-2">
          <span>© 2026 V-Shield 2.0. Toàn bộ bản quyền thuộc về Nhóm Phát Triển Đồ Án.</span>
        </div>
        <div class="flex items-center gap-4">
          <router-link to="/login" class="text-cyan-400/80 hover:text-cyan-300 font-semibold transition">
            Cổng Quản Trị Hệ Thống
          </router-link>
          <span>•</span>
          <a href="https://github.com/quantrif31-wq/V-Shield-2.0" target="_blank" rel="noopener" class="text-pink-400/80 hover:text-pink-300 font-semibold transition">
            GitHub Repository
          </a>
        </div>
      </div>
    </div>
  </footer>
</template>
