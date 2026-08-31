<script setup>
import { ref } from 'vue'

const activeCategory = ref('all')

const categories = [
  { id: 'all', name: 'TẤT CẢ VŨ KHÍ' },
  { id: 'ai', name: 'AI BIOMETRICS' },
  { id: 'barrier', name: 'KINETIC BARRIER' },
  { id: 'sync', name: 'HYBRID SYNC' },
  { id: 'security', name: 'UEBA THREAT' }
]

const features = [
  {
    id: 1,
    category: 'ai',
    title: 'AI FACE ID 60FPS // BIOMETRICS RADAR',
    code: 'MOD-01-OPTIC',
    desc: 'Hệ thống radar nhận diện khuôn mặt quang học tốc độ cao YOLOv11 + ArcFace, xử lý đa góc độ ±45°, thích ứng ánh sáng ban đêm và kháng giả mạo sinh học Live 3D Anti-Spoofing.',
    specs: [
      { label: 'TỐC ĐỘ PHẢN ỨNG', value: '< 45ms' },
      { label: 'ĐỘ CHÍNH XÁC', value: '99.98%' },
      { label: 'GÓC QUÉT QUANG HỌC', value: '±45° Yaw/Pitch' },
      { label: 'KHÁNG ẢNH GIẢ MẠO', value: 'Live 3D Depth' }
    ]
  },
  {
    id: 2,
    category: 'barrier',
    title: 'VIRTUAL SMART BARRIER & ANPR OCR',
    code: 'MOD-02-KINETIC',
    desc: 'Hạ tầng kiểm soát barie tự động và camera nhận dạng biển số xe quân sự ANPR OCR. Tự động đối soát chéo người lái và thông tin chủ xe nhằm triệt tiêu hành vi đổi biển số gian lận.',
    specs: [
      { label: 'THỜI GIAN KÍCH HOẠT', value: '0.6 Giây' },
      { label: 'ĐỘ CHÍNH XÁC OCR', value: '99.5%' },
      { label: 'KIỂM SOÁT LÀN', value: 'Multi-Lane 1/2' },
      { label: 'CẢNH BÁO TÁC CHIẾN', value: 'Realtime SOC Alert' }
    ]
  },
  {
    id: 3,
    category: 'security',
    title: 'DYNAMIC TOTP QR // QUANTUM PASS',
    code: 'MOD-03-CRYPTO',
    desc: 'Thẻ thông hành điện tử sinh mã QR động xoay vòng 30 giây dựa trên thuật toán HMAC-SHA256 mã hóa phần cứng, ngăn chặn 100% việc sao chép hoặc chụp ảnh màn hình.',
    specs: [
      { label: 'CHU KỲ XOAY MÃ', value: '30s / Token' },
      { label: 'MÃ HÓA KHÓA', value: 'HMAC-SHA256' },
      { label: 'KHÁNG SAO CHÉP', value: '100% Absolute' },
      { label: 'HOẠT ĐỘNG OFFLINE', value: 'Autonomous Local' }
    ]
  },
  {
    id: 4,
    category: 'barrier',
    title: 'VOIP INTERCOM // WEBRTC TACTICAL COMMS',
    code: 'MOD-04-COMMS',
    desc: 'Kênh đàm thoại thoại & video mã hóa hai chiều độ trễ cực thấp giữa bảo vệ tại cổng và người dùng qua app di động. Hỗ trợ xác nhận danh tính và mở barie khẩn cấp từ xa.',
    specs: [
      { label: 'ĐỘ TRỄ TRUYỀN DẪN', value: '< 100ms WebRTC' },
      { label: 'CHẤT LƯỢNG HÌNH ẢNH', value: '1080p 60FPS' },
      { label: 'BẢO MẬT LUỒNG GỌI', value: 'DTLS / SRTP E2EE' },
      { label: 'MỞ CỔNG KHẨN CẤP', value: '1-Tap Override' }
    ]
  },
  {
    id: 5,
    category: 'sync',
    title: 'HYBRID SYNC PROTOCOL // OFFLINE-FIRST',
    code: 'MOD-05-AUTONOMOUS',
    desc: 'Kiến trúc đồng bộ hai chiều phân tán giữa Central Cloud và trạm Local Node. Trạm cục bộ vẫn vận hành độc lập hoàn toàn khi mất mạng và tự động đồng bộ khi có kết nối.',
    specs: [
      { label: 'ĐỘ TRỄ ĐỒNG BỘ', value: 'Sub-30ms' },
      { label: 'XỬ LÝ XUNG ĐỘT', value: 'Vector Clocks CRDT' },
      { label: 'OFFLINE CONTINUITY', value: 'Vô Hạn (Auto-Cache)' },
      { label: 'NÉN DỮ LIỆU', value: 'Protobuf / Gzip' }
    ]
  },
  {
    id: 6,
    category: 'security',
    title: 'UEBA // ANOMALY THREAT INTERCEPTOR',
    code: 'MOD-06-DEFENSE',
    desc: 'Mô hình học máy theo dõi hồ sơ di chuyển để phát hiện các dấu hiệu đột nhập: ra vào ngoài giờ, quẹt thẻ ở 2 vị trí bất khả thi (Impossible Travel) hoặc đi theo sau (Tailgating).',
    specs: [
      { label: 'AI MODEL PHÁT HIỆN', value: 'Isolation Forest' },
      { label: 'CẤP ĐỘ CẢNH BÁO', value: 'Critical / Warning' },
      { label: 'PHẢN ỨNG TỰ ĐỘNG', value: 'Auto-Lockdown' },
      { label: 'NHẬT KÝ KIỂM TOÁN', value: 'SIEM & Audit Trail' }
    ]
  }
]

function filteredFeatures() {
  if (activeCategory.value === 'all') return features
  return features.filter(f => f.category === activeCategory.value)
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// TACTICAL SPECIFICATION BLUEPRINTS</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          VŨ KHÍ & CÔNG NGHỆ TÁC CHIẾN
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Được chế tạo theo tiêu chuẩn phòng thủ cấp quân sự, V-Shield MK-II tự động hóa toàn bộ lá chắn kiểm soát ra vào từ trạm rìa Edge đến đám mây.
        </p>
      </div>

      <!-- Category Filter Tabs -->
      <div class="flex flex-wrap items-center justify-center gap-2">
        <button
          v-for="cat in categories"
          :key="cat.id"
          type="button"
          @click="activeCategory = cat.id; triggerSfx()"
          class="px-4 py-2 text-xs font-black uppercase tracking-wider transition-all mecha-cut-btn"
          :class="[
            activeCategory === cat.id
              ? 'mecha-btn-hazard text-slate-950'
              : 'border border-slate-700 bg-[#0e1117] text-slate-400 hover:border-amber-400 hover:text-amber-300'
          ]"
        >
          {{ cat.name }}
        </button>
      </div>

      <!-- Feature Grid -->
      <div class="grid grid-cols-1 gap-8 md:grid-cols-2">
        <div
          v-for="item in filteredFeatures()"
          :key="item.id"
          class="mecha-hud-bracket border-2 border-amber-500/30 bg-[#0c0f15] p-6 sm:p-8 mecha-cut-corners shadow-[0_0_30px_rgba(255,204,0,0.1)] transition-all hover:border-amber-400 hover:shadow-[0_0_40px_rgba(255,204,0,0.25)] space-y-4"
        >
          <!-- Tag Header -->
          <div class="flex items-center justify-between border-b border-slate-800 pb-3">
            <span class="bg-amber-950 px-2.5 py-0.5 text-[10px] font-black text-amber-400 border border-amber-500/40 mecha-cut-tr">
              {{ item.code }}
            </span>
            <span class="text-xs text-slate-500 font-bold">
              SYS // 0{{ item.id }}
            </span>
          </div>

          <!-- Title -->
          <h3 class="text-lg sm:text-xl font-black text-slate-100">
            {{ item.title }}
          </h3>

          <!-- Description -->
          <p class="font-sans text-xs leading-relaxed text-slate-300">
            {{ item.desc }}
          </p>

          <!-- Tech Specs Breakdown Grid -->
          <div class="grid grid-cols-2 gap-3 border-t border-slate-800 pt-4">
            <div
              v-for="(spec, sIdx) in item.specs"
              :key="sIdx"
              class="border border-slate-800 bg-[#121620] p-2.5 mecha-cut-tr"
            >
              <div class="text-[9px] text-slate-400 font-bold uppercase">{{ spec.label }}</div>
              <div class="mt-0.5 text-xs font-black text-amber-400">{{ spec.value }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
