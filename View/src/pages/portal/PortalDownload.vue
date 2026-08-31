<script setup>
import { ref, onMounted } from 'vue'
import QRCode from 'qrcode'
import { portalApi } from '../../services/portalApi'

const apkDownloadUrl = ref('https://v-shield.site/downloads/VShield-Mobile-Latest.apk')
const apkVersion = ref('2.0.0')
const apkSize = ref('61.05 MB')
const qrDataUrl = ref('')

const systemRequirements = [
  { spec: 'Hệ điều hành', requirement: 'Android 8.0 (Oreo) trở lên' },
  { spec: 'Kiến trúc CPU', requirement: 'ARM64-v8a / armeabi-v7a' },
  { spec: 'Dung lượng RAM tối thiểu', requirement: '2.0 GB RAM' },
  { spec: 'Quyền truy cập yêu cầu', requirement: 'Camera (Quét QR/FaceID), Microphone (VoIP Call)' },
  { spec: 'Kết nối mạng', requirement: 'Wi-Fi / 4G / 5G (Hỗ trợ quét Offline)' }
]

const installationSteps = [
  {
    step: '01',
    title: 'Tải File APK Trực Tiếp',
    desc: 'Nhấn nút "Tải APK Trực Tiếp" bên dưới hoặc dùng camera điện thoại quét mã QR để tải file cài đặt về thiết bị.'
  },
  {
    step: '02',
    title: 'Cho Phép Cài Đặt Ngoài ChPlay',
    desc: 'Khi mở file APK, nếu hệ thống hiển thị cảnh báo, chọn "Cài đặt từ nguồn không xác định" (Allow from this source).'
  },
  {
    step: '03',
    title: 'Đăng Nhập & Bật Sinh Trắc Học',
    desc: 'Mở ứng dụng V-Shield, nhập thông tin tài khoản nhân viên được cấp để kích hoạt mã QR động và thẻ thông hành cá nhân.'
  }
]

onMounted(async () => {
  try {
    const ov = await portalApi.getOverview()
    if (ov?.apkDownloadUrl) apkDownloadUrl.value = ov.apkDownloadUrl
  } catch {}

  try {
    qrDataUrl.value = await QRCode.toDataURL(apkDownloadUrl.value, {
      margin: 2,
      width: 220,
      color: {
        dark: '#00f0ff',
        light: '#0b1622'
      }
    })
  } catch (err) {
    console.error('QR generation error:', err)
  }
})
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-cyan-500/30 bg-cyan-950/40 px-3 py-1 text-xs font-bold text-cyan-300 font-mono">
          <span>MOBILE ANDROID APPLICATION</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Trung Tâm Tải Ứng Dụng Di Động
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Trải nghiệm tính năng thẻ thông hành QR Code động xoay vòng 30s, đàm thoại video call WebRTC trực tiếp với trạm bảo vệ và nhận thông báo an ninh tức thì trên điện thoại.
        </p>
      </div>

      <!-- Main Download Card Grid -->
      <div class="grid grid-cols-1 gap-8 lg:grid-cols-12 items-center">
        <!-- Left: Download Actions & Specs (7 cols) -->
        <div class="space-y-6 lg:col-span-7">
          <div class="rounded-3xl border border-cyan-500/30 bg-slate-900/80 p-8 shadow-[0_0_40px_rgba(0,240,255,0.15)] backdrop-blur-2xl space-y-6">
            <!-- App Badge & Title -->
            <div class="flex items-center gap-4">
              <div class="flex h-16 w-16 shrink-0 items-center justify-center rounded-2xl border border-cyan-400/50 bg-gradient-to-br from-cyan-900/80 to-slate-900 shadow-[0_0_20px_rgba(0,240,255,0.4)]">
                <svg class="h-9 w-9 text-cyan-300 drop-shadow-[0_0_8px_#00f0ff]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
                  <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.35" />
                </svg>
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-2xl font-black text-slate-100 font-mono">V-Shield Mobile</h2>
                  <span class="rounded bg-gradient-to-r from-pink-500 to-purple-600 px-2 py-0.5 text-xs font-black uppercase text-white shadow-[0_0_10px_rgba(255,42,133,0.5)]">
                    v{{ apkVersion }}
                  </span>
                </div>
                <p class="text-xs text-slate-400 mt-1 font-mono">
                  Bản phát hành chính thức • Dung lượng: {{ apkSize }}
                </p>
              </div>
            </div>

            <!-- Highlights Checklist -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 border-t border-b border-slate-800 py-4 text-xs text-slate-300">
              <div class="flex items-center gap-2">
                <span class="text-cyan-400 font-bold">✓</span>
                <span>QR Động TOTP xoay 30 giây</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-cyan-400 font-bold">✓</span>
                <span>Video Call VoIP với trạm bảo vệ</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-cyan-400 font-bold">✓</span>
                <span>Xem lịch làm việc & chấm công</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-cyan-400 font-bold">✓</span>
                <span>Đăng ký khách & cấp quyền ra vào</span>
              </div>
            </div>

            <!-- Direct Download Buttons -->
            <div class="flex flex-wrap items-center gap-4">
              <a
                :href="apkDownloadUrl"
                download="VShield-Mobile-Latest.apk"
                class="group relative inline-flex items-center gap-2.5 overflow-hidden rounded-xl bg-gradient-to-r from-cyan-500 via-teal-400 to-pink-500 px-6 py-3.5 text-sm font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_30px_rgba(0,240,255,0.4)] transition-all hover:scale-105 hover:shadow-[0_0_40px_rgba(0,240,255,0.7)]"
              >
                <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                  <polyline points="7 10 12 15 17 10"></polyline>
                  <line x1="12" y1="15" x2="12" y2="3"></line>
                </svg>
                <span>Tải APK Trực Tiếp (Android)</span>
              </a>

              <div class="flex items-center gap-2 text-xs font-mono text-emerald-400">
                <span class="h-2 w-2 rounded-full bg-emerald-400 animate-pulse"></span>
                <span>SHA-256 Verified Safe</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Scan QR Code Card (5 cols) -->
        <div class="lg:col-span-5 flex justify-center">
          <div class="w-full max-w-sm rounded-3xl border border-pink-500/30 bg-slate-900/80 p-8 text-center shadow-[0_0_40px_rgba(255,42,133,0.2)] backdrop-blur-2xl space-y-4">
            <h3 class="text-base font-bold text-slate-100 font-mono uppercase tracking-wider">
              Quét Mã Tải Trên Điện Thoại
            </h3>
            <p class="text-xs text-slate-400">
              Mở Camera hoặc ứng dụng quét mã QR trên điện thoại để mở link tải trực tiếp.
            </p>

            <div class="mx-auto flex justify-center p-3 rounded-2xl border border-cyan-500/30 bg-slate-950/80 shadow-[0_0_20px_rgba(0,240,255,0.2)]">
              <img
                v-if="qrDataUrl"
                :src="qrDataUrl"
                alt="QR Code Tải APK"
                class="h-44 w-44 rounded-xl"
              />
              <div v-else class="h-44 w-44 flex items-center justify-center text-xs text-slate-500 font-mono">
                Đang tạo mã QR...
              </div>
            </div>

            <div class="font-mono text-[11px] text-cyan-300 break-all bg-slate-950/60 p-2 rounded-lg border border-slate-800">
              {{ apkDownloadUrl }}
            </div>
          </div>
        </div>
      </div>

      <!-- System Requirements Table -->
      <div class="space-y-6">
        <h3 class="text-xl font-bold text-slate-100 font-mono text-center">
          Yêu Cầu Cấu Hình Hệ Thống
        </h3>

        <div class="mx-auto max-w-3xl overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/60 backdrop-blur-xl">
          <table class="w-full text-left text-xs">
            <thead class="border-b border-slate-800 bg-slate-950/80 font-mono text-cyan-300">
              <tr>
                <th class="px-6 py-3.5 font-bold uppercase">Thông Số</th>
                <th class="px-6 py-3.5 font-bold uppercase">Yêu Cầu Khuyến Nghị</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60 text-slate-300">
              <tr v-for="(req, rIdx) in systemRequirements" :key="rIdx" class="hover:bg-slate-800/30 transition">
                <td class="px-6 py-3.5 font-semibold text-slate-400 font-mono">{{ req.spec }}</td>
                <td class="px-6 py-3.5 font-bold text-slate-200">{{ req.requirement }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Installation Guide Steps -->
      <div class="space-y-6">
        <h3 class="text-xl font-bold text-slate-100 font-mono text-center">
          Hướng Dẫn Cài Đặt 3 Bước
        </h3>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div
            v-for="st in installationSteps"
            :key="st.step"
            class="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 space-y-3"
          >
            <div class="font-mono text-3xl font-black text-cyan-400">
              {{ st.step }}
            </div>
            <h4 class="text-sm font-bold text-slate-100 font-mono">{{ st.title }}</h4>
            <p class="text-xs text-slate-400 leading-relaxed">{{ st.desc }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
