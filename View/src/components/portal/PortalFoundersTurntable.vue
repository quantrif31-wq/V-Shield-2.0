<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'
import { tacticalVoice } from '../../utils/portalVoiceSynth'
import PortalMechaWarrior3DStage from './PortalMechaWarrior3DStage.vue'

const activeIndex = ref(3) // Default to user (Vũ Tiến Đạt - Purple)
const isAutoRotating = ref(false)
let autoRotateTimer = null

const champions = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    role: 'Trưởng Nhóm & Kiến Trúc Sư Backend Core',
    classId: 'K65-ATTT',
    codename: 'V-SHIELD PRIME // AEGIS COMMAND',
    weaponName: 'EX-01 QUANTUM BROADSWORD (LƯỠI ĐẠI KIẾM LƯỢNG TỬ)',
    weaponType: 'Chỉ huy Điều hành / Điều phối Hệ thống Phân tán',
    weaponDesc: 'Lưỡi kiếm phát quang tích hợp bộ xử lý .NET 8 Clean Architecture & giao thức đồng bộ lai Hybrid Sync, giảm xung đột dữ liệu CRDT với độ trễ dưới 30ms.',
    duties: 'Thiết kế kiến trúc hệ thống phân tán, mã hóa mật mã TOTP HMAC-SHA256, phân quyền đa tầng RBAC và đảm bảo an ninh dữ liệu.',
    color: '#ffcc00',
    stats: { code: 98, defense: 96, sync: 99, power: 97 },
    borderColor: 'border-amber-400',
    textColor: 'text-amber-400',
    weaponIcon: '⚔️'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    role: 'Kỹ Sư AI & Thị Giác Máy Tính (Optical Vision)',
    classId: 'K65-CNTT',
    codename: 'PHANTOM FALCON // OPTICAL RADAR',
    weaponName: 'EX-02 HYPER-VELOCITY PLASMA RAILGUN (SÚNG TRƯỜNG PLASMA)',
    weaponType: 'Trinh sát Tầm xa / Khóa Mục tiêu Sinh trắc học',
    weaponDesc: 'Phát xạ chùm tia năng lượng quang học 60 FPS YOLOv11 + ArcFace, tự động khóa mục tiêu khuôn mặt trong phạm vi ±45° và đối soát biển số ANPR OCR tức thì.',
    duties: 'Tối ưu hóa pipeline suy luận AI, chống giả mạo sinh trắc học 3D Depth Anti-Spoofing và tích hợp camera giám sát luồng RTSP.',
    color: '#ff5500',
    stats: { code: 95, defense: 91, sync: 93, power: 99 },
    borderColor: 'border-orange-500',
    textColor: 'text-orange-400',
    weaponIcon: '🎯'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    role: 'Kỹ Sư DevOps & Hạ Tầng Đám Mây',
    classId: 'K65-KTPM',
    codename: 'DREADNOUGHT VORTEX // CLOUD CORE',
    weaponName: 'EX-03 TITAN HEAVY PARTICLE CANNON (ĐẠI BÁC HẠT NẶNG TITAN)',
    weaponType: 'Phòng vệ Không gian mạng / Hạ tầng Container',
    weaponDesc: 'Tạo trường lực bảo vệ không gian mạng Docker Compose & Caddy TLS đa lớp, nén băng thông Protobuf và duy trì thời gian hoạt động Uptime 99.99%.',
    duties: 'Triển khai hạ tầng VPS, thiết lập pipeline CI/CD tự động, bảo mật mạng nội bộ và giám sát tài nguyên phần cứng thời gian thực.',
    color: '#00f0ff',
    stats: { code: 93, defense: 99, sync: 96, power: 95 },
    borderColor: 'border-cyan-400',
    textColor: 'text-cyan-400',
    weaponIcon: '🛡️'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    role: 'Kỹ Sư Frontend UI/UX & Realtime WebRTC HUD',
    classId: 'K65-CNTT',
    codename: 'SPECTRE STRIKER // CYBER HUD',
    weaponName: 'EX-04 DUAL HOLOGRAPHIC ENERGY DAGGERS (CẶP DAO GĂM HOLOGRAM)',
    weaponType: 'Tác chiến Nhanh / Giao diện Realtime & WebRTC',
    weaponDesc: 'Cặp dao găm laser độ trễ dưới 30ms kết hợp luồng đàm thoại video WebRTC VoIP và giao diện tương tác Mecha Tactical chuẩn game AAA.',
    duties: 'Phát triển toàn bộ hệ thống giao diện Vue 3 Mecha, tích hợp WebGL Three.js và tối ưu hóa trải nghiệm người dùng trên mọi thiết bị.',
    color: '#a855f7',
    stats: { code: 96, defense: 89, sync: 94, power: 96 },
    borderColor: 'border-purple-400',
    textColor: 'text-purple-400',
    weaponIcon: '🗡️'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    role: 'Kỹ Sư Lập Trình Mobile & Thiết Bị IoT Barie',
    classId: 'K65-ATTT',
    codename: 'TEMPEST JUGGERNAUT // GATE GUARDIAN',
    weaponName: 'EX-05 THUNDERSTRIKE POWER HALBERD (KÍCH XUNG KÍCH SẤM SÉT)',
    weaponType: 'Kiểm soát Rào chắn / Thiết bị Ngoại vi Relay',
    weaponDesc: 'Mũi kích phóng xung điện điều khiển đóng mở rào chắn Barie trong 0.6 giây, đồng bộ ứng dụng di động Android APK và mã TOTP thẻ thông hành.',
    duties: 'Lập trình ứng dụng di động Android, giao tiếp phần cứng Relay/RS485 và kiểm thử độ bền bỉ của trạm kiểm soát an ninh tại cổng.',
    color: '#10b981',
    stats: { code: 92, defense: 95, sync: 92, power: 98 },
    borderColor: 'border-emerald-400',
    textColor: 'text-emerald-400',
    weaponIcon: '⚡'
  }
]

const currentChampion = computed(() => champions[activeIndex.value])

function selectChampion(index) {
  activeIndex.value = index
  mechaAudio.playTargetLock()
  mechaAudio.playHeavyImpactDrop()
  tacticalVoice.speakTargetLocked(champions[index].name)
}

function nextChampion() {
  const next = (activeIndex.value + 1) % champions.length
  selectChampion(next)
}

function prevChampion() {
  const prev = (activeIndex.value - 1 + champions.length) % champions.length
  selectChampion(prev)
}

function toggleAutoRotate() {
  isAutoRotating.value = !isAutoRotating.value
  mechaAudio.playClick()
  if (isAutoRotating.value) {
    startAutoRotate()
  } else {
    stopAutoRotate()
  }
}

function startAutoRotate() {
  stopAutoRotate()
  autoRotateTimer = setInterval(() => {
    activeIndex.value = (activeIndex.value + 1) % champions.length
  }, 6000)
}

function stopAutoRotate() {
  if (autoRotateTimer) {
    clearInterval(autoRotateTimer)
    autoRotateTimer = null
  }
}

onMounted(() => {
  if (isAutoRotating.value) {
    startAutoRotate()
  }
})

onUnmounted(() => {
  stopAutoRotate()
})
</script>

<template>
  <div class="relative space-y-8 font-mono">
    
    <!-- Header Controls & Status -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b border-amber-500/20 pb-4">
      <div class="flex items-center gap-3">
        <div class="flex h-3 w-3 items-center justify-center">
          <span class="h-2 w-2 rounded-full animate-ping" :style="{ backgroundColor: currentChampion.color }"></span>
        </div>
        <div class="text-xs font-black uppercase tracking-wider text-slate-300">
          BUỒNG LÁI TÁC CHIẾN MECHA 2.0 // <span :style="{ color: currentChampion.color }">{{ currentChampion.codename }}</span>
        </div>
      </div>

      <!-- Navigation & Auto Rotate Toggles -->
      <div class="flex items-center gap-2">
        <button
          type="button"
          @click="prevChampion"
          class="px-2.5 py-1 text-xs font-bold border border-slate-700 bg-[#07090e] hover:border-amber-400 text-slate-300 hover:text-amber-400 transition-all mecha-cut-tr"
          title="Phi công trước"
        >
          ◀ TRƯỚC
        </button>

        <button
          type="button"
          @click="nextChampion"
          class="px-2.5 py-1 text-xs font-bold border border-slate-700 bg-[#07090e] hover:border-amber-400 text-slate-300 hover:text-amber-400 transition-all mecha-cut-tr"
          title="Phi công kế tiếp"
        >
          TIẾP ▶
        </button>

        <button
          type="button"
          @click="toggleAutoRotate"
          class="px-2.5 py-1 text-[11px] font-bold border transition-all mecha-cut-tr"
          :class="[
            isAutoRotating
              ? 'border-emerald-500 bg-emerald-950/60 text-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
              : 'border-slate-800 bg-[#07090e] text-slate-500 hover:text-slate-300'
          ]"
        >
          <span>{{ isAutoRotating ? '🔄 TỰ XOAY: BẬT' : '⏸️ TỰ XOAY: TẮT' }}</span>
        </button>
      </div>
    </div>

    <!-- ── AAA MECHA COCKPIT & PILOT TURNTABLE STAGE ── -->
    <PortalMechaWarrior3DStage
      :active-index="activeIndex"
      @select-pilot="selectChampion"
    />

    <!-- ── ACTIVE PILOT DOSSIER & TACTICAL ARSENAL CARD ── -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
      
      <!-- Left: Pilot Identity & Core Responsibilities -->
      <div
        class="lg:col-span-7 border-2 bg-[#090c14]/95 p-6 sm:p-8 mecha-cut-corners shadow-[0_0_40px_rgba(0,0,0,0.8)] transition-all duration-500"
        :style="{ borderColor: currentChampion.color, boxShadow: `0 0 35px ${currentChampion.color}25` }"
      >
        <div class="space-y-6">
          <!-- Top Badge & Name -->
          <div class="flex flex-wrap items-start justify-between gap-4 border-b border-slate-800 pb-4">
            <div>
              <div class="flex items-center gap-2 text-xs font-black uppercase tracking-wider" :style="{ color: currentChampion.color }">
                <span>{{ currentChampion.weaponIcon }}</span>
                <span>{{ currentChampion.codename }}</span>
                <span class="text-slate-600">•</span>
                <span class="text-slate-400">{{ currentChampion.classId }}</span>
              </div>
              <h2 class="text-2xl sm:text-3xl font-black text-white mt-1">
                {{ currentChampion.name }}
              </h2>
              <div class="text-sm font-bold text-slate-300 pt-0.5">
                {{ currentChampion.role }}
              </div>
            </div>

            <!-- Power Rating -->
            <div class="text-right">
              <div class="text-[10px] text-slate-500 uppercase">CHỈ SỐ ĐỒNG BỘ</div>
              <div class="text-2xl font-black" :style="{ color: currentChampion.color }">
                {{ currentChampion.stats.sync }}%
              </div>
            </div>
          </div>

          <!-- Responsibilities & Thesis Duties -->
          <div class="space-y-2 font-sans">
            <div class="font-mono text-xs font-black uppercase text-amber-400">
              // NHIỆM VỤ ĐỒ ÁN & TRÁCH NHIỆM KỸ THUẬT:
            </div>
            <p class="text-xs sm:text-sm text-slate-300 leading-relaxed">
              {{ currentChampion.duties }}
            </p>
          </div>

          <!-- Weapon & Tech Arsenal -->
          <div class="space-y-2 border-t border-slate-800 pt-4 font-sans">
            <div class="font-mono text-xs font-black uppercase" :style="{ color: currentChampion.color }">
              // VŨ KHÍ CÔNG NGHỆ CHUYÊN DỤNG: {{ currentChampion.weaponName }}
            </div>
            <p class="text-xs sm:text-sm text-slate-400 leading-relaxed">
              {{ currentChampion.weaponDesc }}
            </p>
          </div>
        </div>
      </div>

      <!-- Right: Pilot Radar Stats & Quick Selector Grid -->
      <div class="lg:col-span-5 flex flex-col justify-between gap-4 border border-slate-800 bg-[#07090e]/95 p-6 mecha-cut-corners">
        <div class="space-y-4">
          <div class="flex items-center justify-between border-b border-slate-800 pb-2">
            <span class="text-xs font-black text-amber-400 uppercase">// CHỈ SỐ TÁC CHIẾN (STATS)</span>
            <span class="text-[10px] text-slate-500 font-mono">PHÂN PHỐI NĂNG LƯỢNG</span>
          </div>

          <!-- 4 Stat Bars -->
          <div class="space-y-3">
            <div>
              <div class="flex justify-between text-xs font-bold text-slate-300 mb-1">
                <span>BACKEND & MẬT MÃ (CODE)</span>
                <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.code }}%</span>
              </div>
              <div class="h-2 w-full bg-slate-900 overflow-hidden rounded-sm">
                <div
                  class="h-full transition-all duration-700"
                  :style="{ width: `${currentChampion.stats.code}%`, backgroundColor: currentChampion.color }"
                ></div>
              </div>
            </div>

            <div>
              <div class="flex justify-between text-xs font-bold text-slate-300 mb-1">
                <span>PHÒNG THỦ & AN NINH (DEFENSE)</span>
                <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.defense }}%</span>
              </div>
              <div class="h-2 w-full bg-slate-900 overflow-hidden rounded-sm">
                <div
                  class="h-full transition-all duration-700"
                  :style="{ width: `${currentChampion.stats.defense}%`, backgroundColor: currentChampion.color }"
                ></div>
              </div>
            </div>

            <div>
              <div class="flex justify-between text-xs font-bold text-slate-300 mb-1">
                <span>ĐỒNG BỘ REALTIME (SYNC)</span>
                <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.sync }}%</span>
              </div>
              <div class="h-2 w-full bg-slate-900 overflow-hidden rounded-sm">
                <div
                  class="h-full transition-all duration-700"
                  :style="{ width: `${currentChampion.stats.sync}%`, backgroundColor: currentChampion.color }"
                ></div>
              </div>
            </div>

            <div>
              <div class="flex justify-between text-xs font-bold text-slate-300 mb-1">
                <span>HỎA LỰC SUY LUẬN (AI POWER)</span>
                <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.power }}%</span>
              </div>
              <div class="h-2 w-full bg-slate-900 overflow-hidden rounded-sm">
                <div
                  class="h-full transition-all duration-700"
                  :style="{ width: `${currentChampion.stats.power}%`, backgroundColor: currentChampion.color }"
                ></div>
              </div>
            </div>
          </div>
        </div>

        <!-- 5 Pilot Selectors -->
        <div class="pt-4 border-t border-slate-800/80">
          <div class="text-[10px] text-slate-500 uppercase mb-2 font-bold">// DANH SÁCH 5 CHIẾN BINH MECHA:</div>
          <div class="grid grid-cols-5 gap-1.5">
            <button
              v-for="(c, idx) in champions"
              :key="c.id"
              type="button"
              @click="selectChampion(idx)"
              class="p-2 text-center transition-all mecha-cut-tr flex flex-col items-center justify-center gap-1"
              :class="[
                idx === activeIndex
                  ? 'border-2 text-white font-black scale-105 shadow-[0_0_15px_rgba(0,0,0,0.8)]'
                  : 'border border-slate-800 bg-[#0a0d14] text-slate-400 hover:text-slate-200 hover:border-slate-700'
              ]"
              :style="{
                borderColor: idx === activeIndex ? c.color : undefined,
                backgroundColor: idx === activeIndex ? '#0f1422' : undefined
              }"
            >
              <span class="text-xs">{{ c.weaponIcon }}</span>
              <span class="text-[9px] font-mono leading-none truncate max-w-full">
                {{ c.name.split(' ').pop() }}
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
