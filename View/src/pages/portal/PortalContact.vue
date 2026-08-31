<script setup>
import { ref, reactive, inject, onMounted } from 'vue'
import { portalApi } from '../../services/portalApi'

const communityUser = inject('communityUser', ref(null))
const feedbackForm = reactive({
  fullName: '',
  email: '',
  category: 'Feature',
  message: ''
})
const feedbackSubmitting = ref(false)
const feedbackToast = ref('')

const openFaqIndex = ref(null)

const faqs = [
  {
    q: 'V-Shield 2.0 hoạt động thế nào khi trạm cục bộ bị mất kết nối mạng?',
    a: 'Nhờ kiến trúc Offline-First Hybrid Sync, toàn bộ dữ liệu mã lượng tử TOTP và vector khuôn mặt Face ID đã được đồng bộ sẵn vào bộ nhớ đệm cục bộ. Trạm vẫn tự động mở rào chắn bình thường và tự động đồng bộ ngược lên Cloud khi có mạng.'
  },
  {
    q: 'Mã QR động TOTP có khả năng chống chụp màn hình chia sẻ không?',
    a: 'Mã QR được tạo động dựa trên thuật toán HMAC-SHA256 xoay vòng mỗi 30 giây. Mỗi mã quét chỉ có giá trị duy nhất 1 lần (Single-Use Token) và tự động hết hạn, ngăn chặn 100% hành vi chụp màn hình gửi cho người khác.'
  },
  {
    q: 'Hệ thống có hỗ trợ triển khai trên máy chủ nội bộ (On-Premises) không?',
    a: 'Có. Toàn bộ hệ thống V-Shield 2.0 đã được container hóa 100% bằng Docker và Docker Compose, cho phép triển khai nhanh chóng trên cả máy chủ Cloud (VPS) hoặc máy trạm bảo vệ cục bộ chỉ với một câu lệnh.'
  },
  {
    q: 'Kênh thoại VoIP Video Call có được bảo mật chống nghe lén không?',
    a: 'Luồng truyền hình ảnh và âm thanh được mã hóa toàn trình (End-to-End Encryption) qua chuẩn WebRTC DTLS/SRTP cấp doanh nghiệp, đảm bảo tính tuyệt mật giữa người gọi và phòng an ninh.'
  }
]

function toggleFaq(idx) {
  openFaqIndex.value = openFaqIndex.value === idx ? null : idx
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

onMounted(() => {
  if (communityUser.value) {
    feedbackForm.fullName = communityUser.value.fullName
    feedbackForm.email = communityUser.value.email
  }
})

async function submitFeedback() {
  if (!feedbackForm.fullName.trim() || !feedbackForm.email.trim() || !feedbackForm.message.trim()) {
    feedbackToast.value = 'Vui lòng điền đầy đủ các trường thông tin!'
    return
  }
  triggerSfx()
  feedbackSubmitting.value = true
  try {
    const res = await portalApi.submitFeedback(feedbackForm)
    if (res.success) {
      feedbackToast.value = '✨ Ý kiến đóng góp của bạn đã được gửi thành công!'
      feedbackForm.message = ''
      setTimeout(() => { feedbackToast.value = '' }, 5000)
    }
  } catch {
    feedbackToast.value = 'Không thể gửi đóng góp lúc này, vui lòng thử lại.'
  } finally {
    feedbackSubmitting.value = false
  }
}
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// SUPPORT & FEEDBACK CHANNEL</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          LIÊN HỆ & ĐÓNG GÓP Ý KIẾN
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Chúng tôi tiếp nhận mọi báo cáo sự cố, đề xuất nâng cấp tính năng và phản hồi kỹ thuật từ hội đồng phản biện, chuyên gia và người dùng.
        </p>
      </div>

      <!-- Main Contact Grid -->
      <div class="grid grid-cols-1 gap-10 lg:grid-cols-12 items-start">
        <!-- Left: Dispatch Form (7 cols) -->
        <div class="mecha-hud-bracket border-2 border-amber-500/40 bg-[#0c0f15] p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.15)] space-y-6 lg:col-span-7">
          <h3 class="text-base font-black text-slate-100 uppercase">
            HÒM THƯ ĐÓNG GÓP Ý KIẾN
          </h3>

          <form @submit.prevent="submitFeedback" class="space-y-4 font-sans">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 font-mono">
              <div>
                <label class="block text-xs font-bold text-slate-400 mb-1">HỌ VÀ TÊN</label>
                <input
                  v-model="feedbackForm.fullName"
                  type="text"
                  placeholder="Nguyễn Văn A"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2.5 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
                />
              </div>

              <div>
                <label class="block text-xs font-bold text-slate-400 mb-1">ĐỊA CHỈ EMAIL</label>
                <input
                  v-model="feedbackForm.email"
                  type="email"
                  placeholder="contact@domain.com"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2.5 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
                />
              </div>
            </div>

            <div class="font-mono">
              <label class="block text-xs font-bold text-slate-400 mb-1">CHỦ ĐỀ ĐÓNG GÓP</label>
              <select
                v-model="feedbackForm.category"
                class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2.5 text-xs text-amber-400 outline-none focus:border-amber-400 mecha-cut-tr"
              >
                <option value="Feature">💡 Đề xuất nâng cấp tính năng mới</option>
                <option value="Bug">🐛 Báo cáo lỗi / Sự cố vận hành</option>
                <option value="UI/UX">🎨 Góp ý trải nghiệm giao diện</option>
                <option value="Partnership">🤝 Hợp tác nghiên cứu & triển khai</option>
                <option value="Other">📝 Ý kiến đóng góp khác</option>
              </select>
            </div>

            <div class="font-mono">
              <label class="block text-xs font-bold text-slate-400 mb-1">NỘI DUNG CHI TIẾT</label>
              <textarea
                v-model="feedbackForm.message"
                rows="4"
                placeholder="Mô tả cụ thể thông tin đóng góp để nâng cấp hệ thống V-Shield 2.0..."
                class="w-full border border-slate-700 bg-[#07080b] p-3.5 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
              ></textarea>
            </div>

            <div class="flex items-center justify-between pt-2 font-mono">
              <div v-if="feedbackToast" class="text-xs font-bold text-amber-400">
                {{ feedbackToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="feedbackSubmitting"
                class="mecha-btn-hazard px-6 py-3 text-xs font-black uppercase mecha-cut-btn disabled:opacity-50"
              >
                {{ feedbackSubmitting ? 'ĐANG GỬI...' : 'GỬI ĐÓNG GÓP' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Right: Official Info & Thesis Lab (5 cols) -->
        <div class="space-y-6 lg:col-span-5">
          <div class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-8 space-y-6 mecha-cut-tr">
            <h3 class="text-sm font-black text-slate-100 uppercase">
              THÔNG TIN PHÒNG NGHIÊN CỨU DỰ ÁN
            </h3>

            <div class="space-y-4 text-xs">
              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center border border-amber-400/40 bg-[#121620] text-amber-400">
                  <span>📍</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200">PHÒNG LAB NGHIÊN CỨU AN NINH</div>
                  <div class="font-sans text-slate-400 mt-0.5">Khoa Công Nghệ Thông Tin & An Toàn Thông Tin</div>
                </div>
              </div>

              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center border border-orange-400/40 bg-[#121620] text-orange-400">
                  <span>✉️</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200">EMAIL LIÊN HỆ ĐỀ TÀI</div>
                  <div class="text-slate-400 mt-0.5">contact@v-shield.site</div>
                </div>
              </div>

              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center border border-amber-400/40 bg-[#121620] text-amber-400">
                  <span>🌐</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200">CỔNG THÔNG TIN CLOUD</div>
                  <div class="text-slate-400 mt-0.5">https://v-shield.site</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- FAQ Section -->
      <div class="space-y-6">
        <h3 class="text-2xl font-black uppercase text-slate-100 text-center">
          CÂU HỎI THƯỜNG GẶP (FAQ)
        </h3>

        <div class="mx-auto max-w-3xl space-y-3">
          <div
            v-for="(faq, fIdx) in faqs"
            :key="fIdx"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] mecha-cut-tr transition hover:border-amber-500/40"
          >
            <button
              type="button"
              @click="toggleFaq(fIdx); triggerSfx()"
              class="flex w-full items-center justify-between p-4 sm:p-5 text-left text-xs sm:text-sm font-bold text-slate-200"
            >
              <span>{{ faq.q }}</span>
              <span class="ml-4 text-amber-400 text-base font-black">{{ openFaqIndex === fIdx ? '−' : '+' }}</span>
            </button>
            <div
              v-if="openFaqIndex === fIdx"
              class="border-t border-slate-800 p-4 sm:p-5 pt-3 font-sans text-xs text-slate-300 leading-relaxed bg-[#07080b]"
            >
              {{ faq.a }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
