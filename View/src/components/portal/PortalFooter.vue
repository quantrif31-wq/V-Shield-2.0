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
      newsletterToast.value = '✨ Đăng ký nhận bản tin an ninh thành công!'
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
  <footer class="relative z-10 border-t-2 border-amber-500/30 bg-[#07080b] text-slate-400 font-mono">
    <!-- Bottom Hazard Caution Ribbon -->
    <div class="h-1 w-full mecha-hazard-bar opacity-80"></div>

    <div class="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8 lg:py-16">
      <div class="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-8">
        <!-- Col 1: Brand & Mech Lore (4 cols) -->
        <div class="space-y-4 lg:col-span-4">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center border-2 border-amber-400 bg-[#121620] mecha-cut-tr shadow-[0_0_15px_rgba(255,204,0,0.3)]">
              <svg class="h-5 w-5 text-amber-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
                <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.35" />
              </svg>
            </div>
            <span class="text-xl font-black tracking-widest text-slate-100 uppercase">
              V-SHIELD MK-II
            </span>
          </div>
          <p class="font-sans text-xs leading-relaxed text-slate-400">
            Hệ thống kiểm soát an ninh thông minh đa nền tảng kết hợp AI sinh trắc học thời gian thực, rào chắn động lực học và giao thức đồng bộ lai (Hybrid Sync).
          </p>
          <div class="flex items-center gap-2 pt-2">
            <div class="flex items-center gap-1.5 border border-emerald-500/40 bg-[#0c1613] px-2.5 py-1 text-[10px] font-bold text-emerald-400 mecha-cut-tr">
              <span class="h-1.5 w-1.5 bg-emerald-400 animate-ping"></span>
              <span>DEFENSE SYSTEM 99.99% ONLINE</span>
            </div>
            <span class="border border-slate-700 bg-[#121620] px-2.5 py-1 text-[10px] text-amber-400 font-bold mecha-cut-tr">
              MK-II // PROD
            </span>
          </div>
        </div>

        <!-- Col 2: Vũ Khí (2 cols) -->
        <div class="space-y-3 lg:col-span-2">
          <h4 class="text-xs font-black uppercase tracking-widest text-amber-400">
            KHO VŨ KHÍ AI
          </h4>
          <ul class="space-y-2 text-xs font-sans">
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                AI Face ID 60FPS
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                Virtual Smart Barrier
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                Mã QR Động TOTP
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                VoIP Video Call WebRTC
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                Hybrid Sync Offline-First
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/features')" class="hover:text-amber-300 transition">
                UEBA Threat Interceptor
              </button>
            </li>
          </ul>
        </div>

        <!-- Col 3: Điều Hướng (2 cols) -->
        <div class="space-y-3 lg:col-span-2">
          <h4 class="text-xs font-black uppercase tracking-widest text-orange-400">
            KHÁM PHÁ
          </h4>
          <ul class="space-y-2 text-xs font-sans">
            <li>
              <button type="button" @click="navigateTo('/')" class="hover:text-orange-300 transition">
                Trạm Chỉ Huy
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/roadmap')" class="hover:text-orange-300 transition">
                Lộ Trình Nâng Cấp
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/download')" class="hover:text-orange-300 transition">
                Tải Field App (APK)
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/community')" class="hover:text-orange-300 transition">
                Bảng Tin Operator
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/about')" class="hover:text-orange-300 transition">
                Phi Đội Sáng Lập
              </button>
            </li>
            <li>
              <button type="button" @click="navigateTo('/contact')" class="hover:text-orange-300 transition">
                Kênh Hỗ Trợ Tác Chiến
              </button>
            </li>
          </ul>
        </div>

        <!-- Col 4: Newsletter & Liên hệ (4 cols) -->
        <div class="space-y-3 lg:col-span-4">
          <h4 class="text-xs font-black uppercase tracking-widest text-amber-400">
            ĐĂNG KÝ BẢN TIN TÁC CHIẾN
          </h4>
          <p class="font-sans text-xs text-slate-400">
            Nhận thông báo cập nhật bản vá bảo mật và báo cáo phân tích đe dọa an ninh mới nhất.
          </p>

          <form @submit.prevent="subscribeNewsletter" class="space-y-2">
            <div class="relative flex items-center">
              <input
                v-model="newsletterEmail"
                type="email"
                placeholder="pilot@v-shield.site"
                class="w-full border border-slate-700 bg-[#0c0f15] px-3.5 py-2 text-xs text-slate-200 placeholder-slate-600 outline-none transition focus:border-amber-400 mecha-cut-tr"
                :disabled="newsletterSubmitting"
              />
              <button
                type="submit"
                :disabled="newsletterSubmitting"
                class="mecha-btn-hazard absolute right-1 px-3 py-1.5 text-xs font-black text-slate-950 mecha-cut-btn disabled:opacity-50"
              >
                {{ newsletterSubmitting ? '...' : 'GỬI' }}
              </button>
            </div>
            <div v-if="newsletterToast" class="text-[11px] font-bold text-amber-400">
              {{ newsletterToast }}
            </div>
          </form>

          <div class="pt-2 text-[11px] text-slate-500 font-sans">
            <div>GVHD: <strong>ThS. Phan Hoàng Khải</strong></div>
            <div>Đồ án tốt nghiệp Kỹ sư CNTT / ATTT - 2026</div>
          </div>
        </div>
      </div>

      <!-- Bottom Credits -->
      <div class="mt-10 flex flex-col items-center justify-between gap-4 border-t border-slate-800 pt-6 text-xs text-slate-500 sm:flex-row font-mono">
        <div>
          <span>© 2026 V-SHIELD MK-II. ALL TACTICAL DEFENSE RIGHTS RESERVED.</span>
        </div>
        <div class="flex items-center gap-4">
          <router-link to="/login" class="text-amber-400 hover:text-amber-300 font-bold transition">
            [ CỔNG QUẢN TRỊ ]
          </router-link>
          <span>•</span>
          <a href="https://github.com/quantrif31-wq/V-Shield-2.0" target="_blank" rel="noopener" class="text-orange-400 hover:text-orange-300 font-bold transition">
            [ GITHUB REPO ]
          </a>
        </div>
      </div>
    </div>
  </footer>
</template>
