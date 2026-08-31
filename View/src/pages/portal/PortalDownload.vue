<script setup>
import { ref, onMounted } from 'vue'
import QRCode from 'qrcode'
import { portalApi } from '../../services/portalApi'

const apkDownloadUrl = ref('https://v-shield.site/downloads/VShield-Mobile-Latest.apk')
const apkVersion = ref('2.0.0')
const apkSize = ref('61.05 MB')
const qrDataUrl = ref('')

const systemRequirements = [
  { spec: 'HỆ ĐIỀU HÀNH', requirement: 'Android 8.0 (Oreo) hoặc mới hơn' },
  { spec: 'KIẾN TRÚC CPU', requirement: 'ARM64-v8a / armeabi-v7a' },
  { spec: 'BỘ NHỚ RAM TỐI THIỂU', requirement: '2.0 GB RAM (Khuyến nghị 4GB)' },
  { spec: 'QUYỀN TRUY CẬP YÊU CẦU', requirement: 'Camera (Quét QR/FaceID), Mic (VoIP Comms)' },
  { spec: 'KẾT NỐI MẠNG', requirement: 'Wi-Fi / 4G / 5G (Hỗ trợ quét Offline tự hành)' }
]

const installationSteps = [
  {
    step: '01',
    title: 'TẢI GÓI CÀI ĐẶT APK',
    desc: 'Nhấn nút "TẢI APK FIELD APP" bên dưới hoặc dùng camera điện thoại quét mã QR để bắt đầu nạp gói phần mềm.'
  },
  {
    step: '02',
    title: 'CẤP PHÉP CÀI ĐẶT NGOÀI',
    desc: 'Khi mở file APK trên điện thoại, chọn "Cho phép cài đặt từ nguồn này" (Allow from this source).'
  },
  {
    step: '03',
    title: 'KÍCH HOẠT MÃ LƯỢNG TỬ',
    desc: 'Đăng nhập bằng tài khoản Pilot/Nhân viên được cấp để kích hoạt mã QR xoay vòng 30s và thẻ thông hành.'
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
        dark: '#ffcc00',
        light: '#0c0f15'
      }
    })
  } catch (err) {
    console.error('QR generation error:', err)
  }
})
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// FIELD TERMINAL DEPLOYMENT</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          TRẠM TẢI ỨNG DỤNG FIELD APP (APK)
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Trang bị thẻ thông hành mã lượng tử TOTP xoay 30 giây, đàm thoại video WebRTC trực tiếp tới trạm chỉ huy và nhận cảnh báo an ninh thời gian thực.
        </p>
      </div>

      <!-- Main Download Card Grid -->
      <div class="grid grid-cols-1 gap-8 lg:grid-cols-12 items-center">
        <!-- Left: Download Actions & Specs (7 cols) -->
        <div class="space-y-6 lg:col-span-7">
          <div class="mecha-hud-bracket border-2 border-amber-500/40 bg-[#0c0f15] p-6 sm:p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.15)] space-y-6">
            <!-- App Badge & Title -->
            <div class="flex items-center gap-4">
              <div class="flex h-16 w-16 shrink-0 items-center justify-center border-2 border-amber-400 bg-[#121620] mecha-cut-tr shadow-[0_0_25px_rgba(255,204,0,0.4)]">
                <svg class="h-9 w-9 text-amber-400 drop-shadow-[0_0_8px_#ffcc00]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
                  <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.35" />
                </svg>
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-2xl font-black text-slate-100">V-SHIELD FIELD APP</h2>
                  <span class="bg-amber-400 text-slate-950 px-2 py-0.5 text-xs font-black uppercase mecha-cut-tr">
                    v{{ apkVersion }}
                  </span>
                </div>
                <p class="text-xs text-slate-400 mt-1">
                  Bản phát hành chính thức // Dung lượng: {{ apkSize }}
                </p>
              </div>
            </div>

            <!-- Checklist -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 border-t border-b border-slate-800 py-4 text-xs text-slate-300">
              <div class="flex items-center gap-2">
                <span class="text-amber-400 font-bold">»</span>
                <span>QR Động TOTP xoay 30 giây</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-amber-400 font-bold">»</span>
                <span>Video Call VoIP với trạm chỉ huy</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-amber-400 font-bold">»</span>
                <span>Xem lịch trực & bảng chấm công</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-amber-400 font-bold">»</span>
                <span>Đăng ký khách & cấp quyền ra vào</span>
              </div>
            </div>

            <!-- Direct Download Buttons -->
            <div class="flex flex-wrap items-center gap-4">
              <a
                :href="apkDownloadUrl"
                download="VShield-Mobile-Latest.apk"
                class="mecha-btn-hazard inline-flex items-center gap-2.5 px-6 py-3.5 text-xs font-black uppercase mecha-cut-btn transition-all"
              >
                <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                  <polyline points="7 10 12 15 17 10"></polyline>
                  <line x1="12" y1="15" x2="12" y2="3"></line>
                </svg>
                <span>TẢI APK TRỰC TIẾP (ANDROID)</span>
              </a>

              <div class="flex items-center gap-2 text-xs text-emerald-400 font-bold">
                <span class="h-2 w-2 bg-emerald-400 animate-pulse"></span>
                <span>SHA-256 VERIFIED SAFE</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Scan QR Code Pod (5 cols) -->
        <div class="lg:col-span-5 flex justify-center">
          <div class="mecha-hud-bracket w-full max-w-sm border-2 border-amber-500/40 bg-[#0c0f15] p-8 text-center mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.15)] space-y-4">
            <h3 class="text-sm font-black uppercase text-slate-100 tracking-wider">
              QUÉT MÃ TẢI QUA OPTICAL SCANNER
            </h3>
            <p class="font-sans text-xs text-slate-400">
              Mở Camera hoặc ứng dụng quét mã QR trên điện thoại để mở link tải trực tiếp.
            </p>

            <div class="mx-auto flex justify-center p-3 border-2 border-amber-500/30 bg-[#07080b] mecha-cut-tr">
              <img
                v-if="qrDataUrl"
                :src="qrDataUrl"
                alt="QR Code Tải APK"
                class="h-44 w-44"
              />
              <div v-else class="h-44 w-44 flex items-center justify-center text-xs text-slate-500">
                ĐANG SINH MÃ QUANG HỌC...
              </div>
            </div>

            <div class="text-[10px] text-amber-400 break-all bg-[#07080b] p-2 border border-slate-800">
              {{ apkDownloadUrl }}
            </div>
          </div>
        </div>
      </div>

      <!-- System Requirements Table -->
      <div class="space-y-6">
        <h3 class="text-xl font-black uppercase text-slate-100 text-center">
          THÔNG SỐ YÊU CẦU CẤU HÌNH
        </h3>

        <div class="mx-auto max-w-3xl overflow-hidden border border-slate-800 bg-[#0c0f15] mecha-cut-tr">
          <table class="w-full text-left text-xs">
            <thead class="border-b border-slate-800 bg-[#121620] text-amber-400">
              <tr>
                <th class="px-6 py-3.5 font-black uppercase">THÔNG SỐ</th>
                <th class="px-6 py-3.5 font-black uppercase">TIÊU CHUẨN ĐỀ NGHỊ</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/60 text-slate-300 font-sans">
              <tr v-for="(req, rIdx) in systemRequirements" :key="rIdx" class="hover:bg-slate-800/20 transition">
                <td class="px-6 py-3.5 font-mono font-bold text-amber-400/80">{{ req.spec }}</td>
                <td class="px-6 py-3.5 font-medium text-slate-200">{{ req.requirement }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Installation Guide Steps -->
      <div class="space-y-6">
        <h3 class="text-xl font-black uppercase text-slate-100 text-center">
          QUY TRÌNH NẠP GÓI PHẦN MỀM 3 BƯỚC
        </h3>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div
            v-for="st in installationSteps"
            :key="st.step"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-6 space-y-3 mecha-cut-tr"
          >
            <div class="text-3xl font-black text-amber-400">
              // {{ st.step }}
            </div>
            <h4 class="text-sm font-black text-slate-100">{{ st.title }}</h4>
            <p class="font-sans text-xs text-slate-400 leading-relaxed">{{ st.desc }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
