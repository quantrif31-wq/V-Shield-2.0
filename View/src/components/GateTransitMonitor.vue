<template>
  <div class="page">
    <div class="topbar" :class="{ compact: topbarCompact }">
      <div class="topbar-main">
        <span class="topbar-eyebrow">GIÁM SÁT VẬN HÀNH</span>
        <h1>Điều phối cổng ra vào</h1>
        <p v-show="!topbarCompact" class="topbar-desc">
          Theo dõi camera QR, biển số và xử lý quyết định thông hành theo từng làn.
        </p>
      </div>
      <div class="topbar-actions">
        <button
          type="button"
          class="topbar-settings-btn"
          :aria-expanded="opsDrawerOpen"
          @click="openOpsDrawer"
        >
          Cài đặt
        </button>
        <button
          type="button"
          class="topbar-toggle"
          :aria-expanded="!topbarCompact"
          @click="topbarCompact = !topbarCompact"
        >
          {{ topbarCompact ? "Hiện mô tả" : "Ẩn mô tả" }}
        </button>
      </div>
    </div>

    <div class="gate-layout">
      <div class="cam-wall" aria-label="Bốn luồng camera">
        <template v-for="lane in lanes" :key="lane.id + '-lane-cams'">
          <div class="cam-cell">
            <div class="cam-block cam-block--hero">
              <div class="cam-head">
                <div class="cam-head-titles">
                  <span class="cam-lane-tag">{{ lane.name }}</span>
                  <span class="cam-kind">Camera QR</span>
                </div>
                <span class="mini-status" :class="lane.qr.previewHealthy ? 'ok' : 'wait'">
                  {{
                    !lane.qr.previewRunning
                      ? "Đang tắt"
                      : lane.qr.lockedSnapshot
                        ? "Ảnh đã chụp"
                        : (lane.qr.previewHealthy ? "Đang trực tuyến" : "Đang kết nối")
                  }}
                </span>
              </div>

              <div class="cam-preview" :class="`state-${cameraVisualState('qr', lane)}`">
                <iframe
                  v-if="lane.qr.previewRunning && lane.qr.directCameraUrl"
                  :key="lane.qr.directCameraKey"
                  :src="lane.qr.directCameraUrl"
                  class="preview-image"
                  style="border: none;"
                ></iframe>
                <div v-else class="cam-off">
                  <span class="cam-off-dot"></span>
                  Camera QR đang tắt
                </div>
                <div class="cam-overlay">
                  <div
                    v-if="lane.qr.overlayBox"
                    class="bbox-box"
                    :style="boundingStyle(lane.qr.overlayBox)"
                  ></div>
                  <div
                    v-if="lane.qr.overlayText"
                    class="overlay-tag"
                    :style="labelStyle(lane.qr.overlayBox)"
                  >
                    Mã QR: {{ shortText(lane.qr.overlayText, 72) }}
                  </div>
                </div>
                <div class="cam-preview-toolbar">
                  <button
                    type="button"
                    class="cam-refresh-btn"
                    :disabled="lane.loading || !lane.qr.cameraIp.trim()"
                    :aria-label="
                      lane.loading ? 'Đang xử lý' : lane.qr.cameraRunning ? 'Đọc lại QR' : 'Đọc QR'
                    "
                    @click.stop="retryQr(lane)"
                  >
                    <svg
                      v-if="lane.loading"
                      class="cam-refresh-icon cam-refresh-icon--spin"
                      viewBox="0 0 24 24"
                      width="22"
                      height="22"
                      aria-hidden="true"
                    >
                      <circle
                        cx="12"
                        cy="12"
                        r="9"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2.5"
                        stroke-dasharray="44"
                        stroke-linecap="round"
                      />
                    </svg>
                    <svg
                      v-else
                      class="cam-refresh-icon"
                      :class="{ 'cam-refresh-icon--rerun': lane.qr.cameraRunning }"
                      viewBox="0 0 24 24"
                      width="22"
                      height="22"
                      aria-hidden="true"
                    >
                      <path
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2.2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        d="M21 11.5a8.38 8.38 0 0 0-.9-3.8 8.5 8.5 0 0 0-7.6-4.7 8.5 8.5 0 0 0-7.7 4.7M3 12.5a8.38 8.38 0 0 0 .9 3.8 8.5 8.5 0 0 0 7.6 4.7 8.5 8.5 0 0 0 7.7-4.7M21 3v5h-5M3 21v-5h5"
                      />
                    </svg>
                  </button>
                </div>
                <canvas :ref="el => setQrCanvasRef(lane.id, el)" style="display:none;"></canvas>
              </div>

              <div class="quick-result">
                <div class="result-pill" :class="`state-${cameraVisualState('qr', lane)}`">
                  {{ cameraVisualText("qr", lane) }}
                </div>
                <div
                  v-if="lane.qr.backendPhase === 'candidate_found' && lane.qr.backendLastCandidate"
                  class="result-hint result-hint--seen"
                >
                  Python đã thấy mã:
                  {{ shortText(lane.qr.backendLastCandidate, 44) }}
                  <span v-if="lane.qr.backendLastSource">
                    · nguồn {{ lane.qr.backendLastSource }}
                  </span>
                </div>
                <div
                  v-else-if="lane.qr.backendPhase === 'connecting'"
                  class="result-hint result-hint--waiting"
                >
                  Đang kết nối camera QR...
                </div>
                <div
                  v-else-if="lane.qr.backendPhase === 'scanning'"
                  class="result-hint result-hint--waiting"
                >
                  Python đang quét, chưa khóa được mã.
                </div>
                <div
                  v-else-if="lane.qr.backendPhase === 'locked' && lane.qr.verifying"
                  class="result-hint result-hint--seen"
                >
                  Đã khóa QR, đang xác thực...
                </div>
              </div>
            </div>
          </div>

          <div class="cam-cell">
            <div class="cam-block cam-block--hero">
              <div class="cam-head">
                <div class="cam-head-titles">
                  <span class="cam-lane-tag">{{ lane.name }}</span>
                  <span class="cam-kind">Camera biển số</span>
                </div>
                <span class="mini-status" :class="platePreviewStatusClass(lane.plate)">
                  {{ platePreviewStatusText(lane.plate) }}
                </span>
              </div>

              <div class="cam-preview" :class="`state-${cameraVisualState('plate', lane)}`">
                <iframe
                  v-if="lane.plate.previewRunning && lane.plate.directCameraUrl"
                  :key="lane.plate.directCameraKey"
                  :src="lane.plate.directCameraUrl"
                  class="preview-image"
                  style="border: none;"
                ></iframe>
                <div v-else class="cam-off">
                  <span class="cam-off-dot"></span>
                  Camera biển số đang tắt
                </div>
                <div class="cam-overlay">
                  <div
                    v-if="lane.plate.overlayBox"
                    class="bbox-box"
                    :style="boundingStyle(lane.plate.overlayBox)"
                  ></div>
                  <div
                    v-if="lane.plate.overlayText"
                    class="overlay-tag"
                    :style="labelStyle(lane.plate.overlayBox)"
                  >
                    Biển số: {{ lane.plate.overlayText }}
                  </div>
                </div>
                <div class="cam-preview-toolbar">
                  <button
                    type="button"
                    class="cam-refresh-btn"
                    :disabled="lane.loading || !lane.plate.cameraIp.trim()"
                    :aria-label="
                      lane.loading
                        ? 'Đang xử lý'
                        : lane.plate.cameraRunning
                          ? 'Đọc lại biển số'
                          : 'Đọc biển số'
                    "
                    @click.stop="retryPlate(lane)"
                  >
                    <svg
                      v-if="lane.loading"
                      class="cam-refresh-icon cam-refresh-icon--spin"
                      viewBox="0 0 24 24"
                      width="22"
                      height="22"
                      aria-hidden="true"
                    >
                      <circle
                        cx="12"
                        cy="12"
                        r="9"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2.5"
                        stroke-dasharray="44"
                        stroke-linecap="round"
                      />
                    </svg>
                    <svg
                      v-else
                      class="cam-refresh-icon"
                      :class="{ 'cam-refresh-icon--rerun': lane.plate.cameraRunning }"
                      viewBox="0 0 24 24"
                      width="22"
                      height="22"
                      aria-hidden="true"
                    >
                      <path
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2.2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        d="M21 11.5a8.38 8.38 0 0 0-.9-3.8 8.5 8.5 0 0 0-7.6-4.7 8.5 8.5 0 0 0-7.7 4.7M3 12.5a8.38 8.38 0 0 0 .9 3.8 8.5 8.5 0 0 0 7.6 4.7 8.5 8.5 0 0 0 7.7-4.7M21 3v5h-5M3 21v-5h5"
                      />
                    </svg>
                  </button>
                </div>
              </div>

              <div class="quick-result">
                <div class="result-pill" :class="`state-${cameraVisualState('plate', lane)}`">
                  {{ cameraVisualText("plate", lane) }}
                </div>
              </div>
            </div>
          </div>
        </template>
      </div>
    </div>

    <div class="ops-dock" role="toolbar" aria-label="Điều khiển nhanh">
      <div class="ops-dock-grid">
        <div class="ops-dock-center">
          <div
            v-for="lane in lanes"
            :key="lane.id + '-dock-actions'"
            class="lane-action-group"
          >
            <div class="lane-action-title">{{ lane.name }}</div>
            <div class="lane-action-btns" role="group" :aria-label="`Thao tác ${lane.name}`">
              <button
                class="btn btn-dock btn-main"
                :disabled="lane.loading || !lane.qr.cameraIp.trim() || !lane.plate.cameraIp.trim()"
                @click="readAllLane(lane)"
              >
                {{
                  lane.loading ? "Đang xử lý..." : laneAnyRunning(lane) ? "Quét lại cả hai" : "Quét cả hai"
                }}
              </button>

              <button
                class="btn btn-dock btn-confirm"
                :disabled="
                  lane.loading ||
                    (!lane.qr.employeeId && !lane.qr.guestId) ||
                    !lane.plate.confirmedPlate
                "
                @click="confirmLane(lane)"
              >
                Xác nhận
              </button>
              <button
                class="btn btn-dock btn-decision"
                :disabled="
                  lane.loading ||
                    (!lane.qr.employeeId && !lane.qr.guestId && !lane.plate.confirmedPlate)
                "
                @click="openDecisionDrawer(lane)"
              >
                Quyết định
              </button>
            </div>
          </div>
        </div>

      </div>
    </div>

    <div v-show="opsDrawerOpen" class="ops-drawer-root" :aria-hidden="!opsDrawerOpen">
      <div class="ops-drawer-backdrop" @click="closeOpsDrawer"></div>
      <aside class="ops-drawer-panel" role="dialog" aria-modal="true" aria-label="Cài đặt cổng" @click.stop>
        <header class="ops-drawer-head">
          <h2 class="ops-drawer-title">Cài đặt</h2>
          <button type="button" class="ops-drawer-close" aria-label="Đóng" @click="closeOpsDrawer">✕</button>
        </header>

        <div class="ops-drawer-tabs" role="tablist">
          <button
            v-for="lane in lanes"
            :key="lane.id + '-drawer-tab'"
            type="button"
            class="ops-drawer-tab"
            :class="{ active: opsActiveLaneId === lane.id }"
            role="tab"
            :aria-selected="opsActiveLaneId === lane.id"
            @click="opsActiveLaneId = lane.id"
          >
            {{ lane.name }}
          </button>
        </div>

        <div class="ops-drawer-body">
          <div class="drawer-settings-panel">
            <h3 class="drawer-settings-title">Dịch vụ Python</h3>
            <p class="drawer-settings-meta">
              <span>QR: {{ qrApiBaseLabel }}</span>
              <span class="drawer-settings-meta-sep">•</span>
              <span>Biển: {{ plateApiBaseLabel }}</span>
            </p>
            <p class="drawer-settings-hint">
              Bật/tắt backend tương ứng. URL stream lấy theo tab làn đang chọn (QR / Plate). Dịch vụ QR là một
              process dùng chung cho cả hai làn.
            </p>

            <div class="settings-toggle-row">
              <div class="settings-toggle-text">
                <span class="settings-toggle-name">Python đọc QR</span>
                <span class="settings-toggle-desc">Bật/tắt endpoint /qr (camera + worker)</span>
              </div>
              <button
                type="button"
                class="toggle-switch"
                :class="toggleSwitchClass('python_qr', qrPythonServiceOn)"
                role="switch"
                :aria-checked="qrPythonServiceOn"
                :disabled="runtimeIsBusy('python_qr') || toggleIsPending('python_qr')"
                @click="onToggleQrPython"
              >
                <span class="toggle-switch-knob" aria-hidden="true"></span>
              </button>
              <button
                type="button"
                class="auto-start-btn"
                :disabled="runtimeIsBusy('python_qr') || !runtimeEnabled('python_qr')"
                @click="toggleRuntimeAutoStart('python_qr')"
              >
                Tự khởi động: {{ runtimeAutoStart('python_qr') ? 'BẬT' : 'TẮT' }}
              </button>
            </div>

            <div class="settings-toggle-row">
              <div class="settings-toggle-text">
                <span class="settings-toggle-name">Python biển số</span>
                <span class="settings-toggle-desc">API biển số (bật/tắt camera)</span>
              </div>
              <button
                type="button"
                class="toggle-switch"
                :class="toggleSwitchClass('python_plate', platePythonServiceOn)"
                role="switch"
                :aria-checked="platePythonServiceOn"
                :disabled="runtimeIsBusy('python_plate') || toggleIsPending('python_plate')"
                @click="onTogglePlatePython"
              >
                <span class="toggle-switch-knob" aria-hidden="true"></span>
              </button>
              <button
                type="button"
                class="auto-start-btn"
                :disabled="runtimeIsBusy('python_plate') || !runtimeEnabled('python_plate')"
                @click="toggleRuntimeAutoStart('python_plate')"
              >
                Tự khởi động: {{ runtimeAutoStart('python_plate') ? 'BẬT' : 'TẮT' }}
              </button>
            </div>
<div class="settings-toggle-row">
              <div class="settings-toggle-text">
                <span class="settings-toggle-name">Cổng gateway stream go2rtc</span>
                <span class="settings-toggle-desc">Dịch vụ stream WebRTC cho camera</span>
              </div>
              <button
                type="button"
                class="toggle-switch"
                :class="toggleSwitchClass('go2rtc', runtimeRunning('go2rtc'))"
                role="switch"
                :aria-checked="runtimeRunning('go2rtc')"
                :disabled="runtimeIsBusy('go2rtc') || toggleIsPending('go2rtc')"
                @click="toggleRuntime('go2rtc')"
              >
                <span class="toggle-switch-knob" aria-hidden="true"></span>
              </button>
              <button
                type="button"
                class="auto-start-btn"
                :disabled="runtimeIsBusy('go2rtc') || !runtimeEnabled('go2rtc')"
                @click="toggleRuntimeAutoStart('go2rtc')"
              >
                Tự khởi động: {{ runtimeAutoStart('go2rtc') ? 'BẬT' : 'TẮT' }}
              </button>
            </div>

            <div class="settings-toggle-row">
              <div class="settings-toggle-text">
                <span class="settings-toggle-name">Tunnel cloudflared</span>
                <span class="settings-toggle-desc">Tunnel công khai để publish stream</span>
              </div>
              <button
                type="button"
                class="toggle-switch"
                :class="toggleSwitchClass('cloudflared', runtimeRunning('cloudflared'))"
                role="switch"
                :aria-checked="runtimeRunning('cloudflared')"
                :disabled="runtimeIsBusy('cloudflared') || toggleIsPending('cloudflared')"
                @click="toggleRuntime('cloudflared')"
              >
                <span class="toggle-switch-knob" aria-hidden="true"></span>
              </button>
              <button
                type="button"
                class="auto-start-btn"
                :disabled="runtimeIsBusy('cloudflared') || !runtimeEnabled('cloudflared')"
                @click="toggleRuntimeAutoStart('cloudflared')"
              >
                Tự khởi động: {{ runtimeAutoStart('cloudflared') ? 'BẬT' : 'TẮT' }}
              </button>
            </div>
          </div>

          <section
            class="lane-controls lane-controls--drawer"
            :class="{ ready: isLaneReady(activeOpsLane) }"
          >
            <div class="lane-head">
              <div>
                <h2>{{ activeOpsLane.name }}</h2>
                <p>{{ activeOpsLane.desc }}</p>
              </div>

              <div class="lane-final-status" :class="isLaneReady(activeOpsLane) ? 'ok' : 'wait'">
                {{ isLaneReady(activeOpsLane) ? "Sẵn sàng xác nhận" : "Đang xử lý" }}
              </div>
            </div>

            <div class="ip-row">
              <div class="ip-box">
                <label>URL Camera QR</label>
                <div class="search-box">
                  <input
                    v-model="cameraSearch[activeOpsLane.id + '-qr']"
                    placeholder="Tìm camera QR..."
                    :disabled="activeOpsLane.loading"
                  />

                  <div class="dropdown" v-if="cameraSearch[activeOpsLane.id + '-qr']">
                    <div
                      v-for="cam in filterCameras(cameraSearch[activeOpsLane.id + '-qr'])"
                      :key="cam.cameraId"
                      @click="selectCamera(cam, activeOpsLane, 'qr')"
                      class="dropdown-item"
                    >
                      {{ cam.cameraName }} (ID: {{ cam.cameraId }})
                    </div>
                  </div>
                </div>
              </div>

              <div class="ip-box">
                <label>URL Camera Biển số</label>
                <div class="search-box">
                  <input
                    v-model="cameraSearch[activeOpsLane.id + '-plate']"
                    placeholder="Tìm camera biển số..."
                    :disabled="activeOpsLane.loading"
                  />

                  <div class="dropdown" v-if="cameraSearch[activeOpsLane.id + '-plate']">
                    <div
                      v-for="cam in filterCameras(cameraSearch[activeOpsLane.id + '-plate'])"
                      :key="cam.cameraId"
                      @click="selectCamera(cam, activeOpsLane, 'plate')"
                      class="dropdown-item"
                    >
                      {{ cam.cameraName }} (ID: {{ cam.cameraId }})
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="summary-bar">
              <div class="summary-item">
                <span class="label">Người dùng</span>
                <span class="value strong">{{ activeOpsLane.qr.employeeId || activeOpsLane.qr.guestId || "-----" }}</span>
              </div>

              <div class="summary-item">
                <span class="label">QR</span>
                <span class="value" :class="qrStateClass(activeOpsLane.qr)">
                  {{ qrStateText(activeOpsLane.qr) }}
                </span>
              </div>

              <div class="summary-item">
                <span class="label">Biển số</span>
                <span class="value strong plate">{{
                  activeOpsLane.plate.confirmedPlate || activeOpsLane.plate.lastRawPlate || "-----"
                }}</span>
              </div>
            </div>

            <div class="drawer-secondary-actions">
                <button
                type="button"
                class="btn btn-drawer-secondary btn-preview"
                :disabled="activeOpsLane.loading"
                @click="previewLane(activeOpsLane)"
              >
                {{ activeOpsLane.loading ? "Đang xử lý..." : "Preview" }}
              </button>
              <button
                type="button"
                class="btn btn-drawer-secondary btn-off"
                :disabled="activeOpsLane.loading"
                @click="stopLane(activeOpsLane)"
              >
                {{ activeOpsLane.loading ? "Đang xử lý..." : "Tắt" }}
              </button>
            </div>
          </section>
        </div>
      </aside>
    </div>
  </div>

  <!-- Decision Drawer -->
  <DecisionDrawer
    :visible="decisionDrawerVisible"
    :lane-name="decisionLane?.name || ''"
    :subject-name="(decisionLane ? (decisionLane.qr.employeeName || 'Khách') : '')"
    :subject-id="(decisionLane ? (decisionLane.qr.guestId || decisionLane.qr.employeeId || '') : '')"
    :subject-type="(decisionLane ? (decisionLane.qr.guestId ? 'GUEST' : 'EMPLOYEE') : '')"
    :plate-number="(decisionLane ? (decisionLane.plate.confirmedPlate || decisionLane.plate.lastRawPlate || '') : '')"
    :qr-payload="(decisionLane ? (decisionLane.qr.qrPayload || decisionLane.qr.activeSessionPayload || '') : '')"
    :warnings="decisionWarnings"
    :can-allow="getActionPermissions(decisionLane)['allow']"
    :can-deny="getActionPermissions(decisionLane)['deny']"
    :can-manual="getActionPermissions(decisionLane)['manual']"
    :can-unified-emergency="getActionPermissions(decisionLane)['unifiedEmergency']"
    :can-escalate="getActionPermissions(decisionLane)['escalate']"
    :loading="false"
    @close="closeDecisionDrawer"
    @action="handleDecisionAction"
  />

  <!-- Step-Up Modal -->
  <StepUpModal
    :visible="stepUpVisible"
    action-label="Kích hoạt trạng thái khẩn cấp"
    action-description="Hành động này sẽ ghi đè tất cả quy tắc access policy. Chỉ Admin mới được thực hiện."
    severity="critical"
    :require-mfa="true"
    @cancel="onStepUpCancelled"
    @confirmed="onStepUpConfirmed"
  />

  <!-- Audit Receipt Toast -->
  <AuditReceiptToast
    :visible="auditToast.visible"
    :type="auditToast.type"
    :title="auditToast.title"
    :message="auditToast.message"
    :receipt-id="auditToast.receiptId"
    :timestamp="auditToast.timestamp"
    @dismiss="dismissAuditToast"
  />
</template>

<script>
import jsQR from "jsqr"
import * as plateLane1Api from "../services/plateCameraApi"
import * as plateLane2Api from "../services/plateCameraApi"
import axios from "axios"
import { scanGate, scanGuest } from "../services/gateTransitApi"
import { verifyDynamicQr } from "../services/dynamicQrVerifyApi"
import { getCameras, startPythonQrProcess, stopPythonQrProcess, startPythonPlateProcess, stopPythonPlateProcess, startPythonSimulatedCameraProcess, stopPythonSimulatedCameraProcess, getPythonProcessStatus } from "../services/cameraRuntimeApi"
import { getRuntimeServices, updateRuntimeService, startRuntimeService, stopRuntimeService } from "../services/runtimeServiceApi"
import {
  startQrScanner,
  resetQrSession,
  stopQrScanner,
  getQrScanResult,
  scanQrOnce,
  QR_API_BASE_URL,
  QR_API_BASE_URL_LANE2
} from "../services/dynamicQrScannerApi"
import { PLATE_API_BASE_URL } from "../config/api"
import { normalizeCameraUrl } from "../utils/cameraNetwork"
import { enterpriseApi, zoneAuthorityApi } from "../services/enterpriseSecurityApi"
import { authState, hasRole } from "../stores/auth"
import DecisionDrawer from "./shared/DecisionDrawer.vue"
import StepUpModal from "./shared/StepUpModal.vue"
import AuditReceiptToast from "./shared/AuditReceiptToast.vue"
function createQrModule(defaultScannerDevice) {
  return {
    cameraIp: "",
    currentIp: "",
    cameraRunning: false,
    previewRunning: false,
    pollingBusy: false,

    previewHealthy: false,
    imgBusy: false,
    decodeBusy: false,
    verifying: false,
    scanKickoffBusy: false,
    controlSessionId: 0,
    lastAutoScanAt: 0,

    directCameraUrl: "",
    directCameraKey: 0,
    viewUrl: "", // 🔥 thêm dòng này

    scannerDevice: defaultScannerDevice,

    qrPayload: "",
    manualPayload: "",
    verifyMessage: "",
    verifyData: null,

    employeeId: "",
    employeeName: "",
    guestId: "",
    personType: "",

    activeSessionPayload: "",
    activeSessionVerified: false,
    activeSessionVerifyState: "",
    activeSessionVerifyMessage: "",
    lastSeenAt: null,

    lastDecodedText: "",
    lastDecodedAt: 0,
    lastUpdate: "",
    message: "",

    previewTimer: null,
    sessionTimer: null,
    destroyed: false,

    previewIntervalMs: 350,
    absenceThresholdMs: 1500,
    decodeMaxWidth: 640,
    stablePayload: "",
    stablePayloadCount: 0,
    stablePayloadRequiredCount: 2,
    stablePayloadWindowMs: 1200,
    lastStablePayloadAt: 0,
    lastVerifyAttemptPayload: "",
    lastVerifyAttemptAt: 0,
    verifyCooldownMs: 1800,
    autoScanCooldownMs: 1800,

    // Flag set when a backend scan request is active (scan enabled on Python)
    scanRequested: false,
    backendPhase: "idle",
    backendConnected: false,
    backendFrameReady: false,
    backendLastCandidate: "",
    backendLastSource: "",
    backendLastDecodeAt: 0,
    backendCandidateSeenCount: 0,
    backendLockedPayload: "",
    backendLockedAt: 0,
    lastVerifiedPayload: "",

    frameWidth: 0,
    frameHeight: 0,
    overlayText: "",
    overlayBox: null,

    alert: false,
    sessionLocked: false,
    lockedSnapshot: ""
  }
}

function createPlateModule() {
  return {
    cameraIp: "",
    currentIp: "",
    cameraRunning: false,
    previewRunning: false,

    sessionId: 0,
    lastAppliedSessionId: 0,
    lastLockedImageSessionId: 0,

    confirmedPlate: "",
    lastRawPlate: "",
    scanLocked: false,
    scanActive: false,

    lockedSnapshot: "",
    lockedPlateCrop: "",

    message: "",
    fps: 0,
    ocrRunning: false,
    stableCount: 0,
    movingFast: false,
    lastUpdate: "",

    directCameraUrl: "",
    directCameraKey: 0,
    viewUrl: "", // 🔥 thêm dòng này
    previewHealthy: false,
    overlayText: "",
    overlayBox: null,

    resultTimer: null,
    busyResult: false,
    isFetchingLockedImages: false,
    destroyed: false
  }
}

export default {
  name: "VShieldGateMinimalQr",
  components: { DecisionDrawer, StepUpModal, AuditReceiptToast },

  data() {
    return {
      qrCanvasRefs: {},
      cameras: [],
  cameraSearch: {},
      lanes: [
        {
  id: "lane1",
  laneId: 131,
  name: "Làn 1",
  desc: "QR trên / Biển dưới",
  gateId: 1177,
  direction: "IN",
  cameraId: null,
  loading: false,
  plateApi: plateLane1Api,
  qr: createQrModule("WEB_SCANNER_GATE_01"),
  plate: createPlateModule()
},
{
  id: "lane2",
  laneId: 132,
  name: "Làn 2",
  desc: "QR trên / Biển dưới",
  gateId: 1177,
  direction: "OUT",
  cameraId: null,
  loading: false,
  plateApi: plateLane2Api,
  qr: createQrModule("WEB_SCANNER_GATE_02"),
  plate: createPlateModule()
}
      ],
      opsDrawerOpen: false,
      opsActiveLaneId: "lane1",
      topbarCompact: true,
      settingsQrBusy: false,
      settingsPlateBusy: false,
      settingsCameraSimulatorBusy: false,
      runtimeServices: [],
      runtimeBusy: {},
      uiTogglePending: {},
      // Decision drawer state
      decisionDrawerVisible: false,
      decisionLaneId: null,
      // Step-up modal state
      stepUpVisible: false,
      stepUpAction: null,
      stepUpLaneId: null,
      // Audit receipt toast state
      auditToast: {
        visible: false,
        type: 'success',
        title: '',
        message: '',
        receiptId: '',
        timestamp: '',
      },
      userZoneIds: [],
    }
  },

  computed: {
    runtimeMap() {
      return this.runtimeServices.reduce((acc, item) => {
        acc[item.name] = item
        return acc
      }, {})
    },

    activeOpsLane() {
      return this.lanes.find((l) => l.id === this.opsActiveLaneId) || this.lanes[0]
    },

    qrPythonServiceOn() {
      const state = this.runtimeMap.python_qr
      return state ? !!state.running : this.lanes.some((l) => l.qr.cameraRunning)
    },

    platePythonServiceOn() {
      const state = this.runtimeMap.python_plate
      return state ? !!state.running : this.lanes.some((l) => l.plate.cameraRunning)
    },

    qrApiBaseLabel() {
      return this.getLaneQrApiBase(this.activeOpsLane)
    },

    plateApiBaseLabel() {
      try {
        return plateLane1Api.getResolvedPlateApiBaseUrl() || ""
      } catch {
        return ""
      }
    },

    currentRole() {
      return authState.user?.role || 'BaoVe'
    },
    isAdmin() {
      return this.currentRole === 'Admin'
    },
    isBaoVe() {
      return this.currentRole === 'BaoVe'
    },
    isQuanLy() {
      return this.currentRole === 'QuanLy'
    },

    decisionLane() {
      if (!this.decisionLaneId) return null
      return this.lanes.find((l) => l.id === this.decisionLaneId) || null
    },

    decisionWarnings() {
      const lane = this.decisionLane
      if (!lane) return []
      const warnings = []
      // Anti-passback check
      if (lane.plate.confirmedPlate) {
        warnings.push({
          severity: 'warn',
          text: 'Cảnh báo: Biển số đã được quét gần đây. Kiểm tra anti-passback.',
          icon: '&#9888;'
        })
      }
      // QR alert
      if (lane.qr.alert) {
        warnings.push({
          severity: 'critical',
          text: 'QR không hợp lệ hoặc đã hết hạn. Vui lòng kiểm tra lại.',
          icon: '&#9940;'
        })
      }
      // General alert
      if (lane.qr.message && lane.qr.message.includes('hết hạn')) {
        warnings.push({
          severity: 'warn',
          text: 'Phiên QR đã hết hạn. Cần quét lại.',
          icon: '&#9888;'
        })
      }
      return warnings
    },
  },

  async mounted() {
  document.body.classList.add("gate-transit-compact")
  await this.loadCameraList()
  await this.fetchUserZones()

  for (const lane of this.lanes) {
    lane.qr.destroyed = false
    lane.plate.destroyed = false
    await this.loadStatusPlate(lane)
    if (lane.plate.cameraRunning) this.startPlateLoop(lane)
  }
},

  beforeUnmount() {
    document.body.classList.remove("gate-transit-compact")
    for (const lane of this.lanes) {
      lane.qr.destroyed = true
      lane.plate.destroyed = true

      this.stopQrLoops(lane)
      this.stopPlateLoop(lane)

      this.resetQrPreview(lane.id, lane.qr)
      this.resetPreview(lane.plate)
    }
  },

  activated() {
    document.body.classList.add("gate-transit-compact")
    for (const lane of this.lanes) {
      lane.qr.destroyed = false
      lane.plate.destroyed = false

      if (lane.qr.cameraRunning) {
        if (lane.qr.viewUrl && !lane.qr.previewRunning) {
  this.mountPreview(lane.qr, lane.qr.viewUrl)
}
        this.startQrPreviewLoop(lane)
        this.startQrSessionLoop(lane)
      }

      if (lane.plate.cameraRunning) {
        if (lane.plate.viewUrl && !lane.plate.previewRunning) {
  this.mountPreview(lane.plate, lane.plate.viewUrl)
}
        this.startPlateLoop(lane)
      }
    }
  },

  deactivated() {
    document.body.classList.remove("gate-transit-compact")
    for (const lane of this.lanes) {
      this.stopQrLoops(lane)
      this.stopPlateLoop(lane)
    }
  },

  methods: {
    preferMainQrStream(url) {
      const raw = String(url || "").trim()
      if (!raw) return ""
      return raw.replace(/([?&]subtype=)1\b/i, "$10")
    },

    extractGo2RtcStreamName(url) {
      const raw = String(url || "").trim()
      if (!raw) return ""

      try {
        const parsed = new URL(raw, window.location.origin)
        return String(parsed.searchParams.get("src") || "").trim()
      } catch {
        const match = raw.match(/[?&]src=([^&#]+)/i)
        return match?.[1] ? decodeURIComponent(match[1]).trim() : ""
      }
    },

    getEffectiveQrStream(lane) {
      const directStream = this.preferMainQrStream(
        lane?.qr?.currentIp || lane?.qr?.cameraIp || ""
      )
      if (
        directStream &&
        !/^go2rtc:/i.test(directStream) &&
        !/stream\.html\?src=/i.test(directStream)
      ) {
        return directStream
      }

      const go2rtcStreamName = this.extractGo2RtcStreamName(lane?.qr?.viewUrl || "")
      if (go2rtcStreamName) {
        return `go2rtc:${go2rtcStreamName}`
      }

      return this.preferMainQrStream(
        lane?.qr?.cameraIp || lane?.qr?.currentIp || lane?.qr?.viewUrl || ""
      )
    },

    getLaneQrApiBase(lane) {
      return lane?.id === "lane2" ? QR_API_BASE_URL_LANE2 : QR_API_BASE_URL
    },

    async startLaneQrScanner(lane, rtsp) {
      return startQrScanner(rtsp, this.getLaneQrApiBase(lane))
    },

    async scanLaneQrOnce(lane) {
      return scanQrOnce(this.getLaneQrApiBase(lane))
    },

    async resetLaneQrSession(lane) {
      return resetQrSession(this.getLaneQrApiBase(lane))
    },

    async stopLaneQrScanner(lane) {
      return stopQrScanner(this.getLaneQrApiBase(lane))
    },

    async getLaneQrScanResult(lane) {
      return getQrScanResult(this.getLaneQrApiBase(lane))
    },

    setQrCanvasRef(laneId, el) {
      if (el) this.qrCanvasRefs[laneId] = el
    },

    openOpsDrawer() {
      this.opsDrawerOpen = true
      this.$nextTick(() => {
        this.refreshDrawerServiceState()
      })
    },

    closeOpsDrawer() {
      this.opsDrawerOpen = false
    },

    async refreshDrawerServiceState() {
      try {
        this.runtimeServices = await getRuntimeServices()
      } catch (e) {
        console.warn("getRuntimeServices", e)
      }

      try {
        const pythonStatus = await getPythonProcessStatus()
        // NOTE: do NOT set lane.{qr,plate}.cameraRunning directly from python process status.
        // cameraRunning means the camera capture/session is active; we will rely on per-module
        // status endpoints (loadStatusPlate / getQrScanResult) to populate actual cameraRunning.
      } catch {
        // fallback: do not force cameraRunning here
      }

      for (const lane of this.lanes) {
        try {
          await this.loadStatusPlate(lane)
        } catch (e) {
          console.warn("loadStatusPlate", e)
        }
      }
    },

    runtimeState(name) {
      return this.runtimeMap[name] || null
    },

    runtimeAutoStart(name) {
      return !!this.runtimeState(name)?.autoStart
    },

    runtimeRunning(name) {
      return !!this.runtimeState(name)?.running
    },

    runtimeEnabled(name) {
      const state = this.runtimeState(name)
      return state ? !!state.enabled : true
    },

    runtimeIsBusy(name) {
      return !!this.runtimeBusy[name]
    },

    toggleIsPending(name) {
      return !!this.uiTogglePending[name]
    },

    toggleSwitchClass(name, isOn) {
      return {
        on: !!isOn,
        pending: this.toggleIsPending(name)
      }
    },

    setTogglePending(name, value) {
      this.uiTogglePending = { ...this.uiTogglePending, [name]: !!value }
    },

    buildQrIdentityOverlay(data = {}) {
      const personType = String(data?.type || "").toUpperCase()
      if (personType === "STATIC") {
        const guestId = String(
          data?.visitorDetailId ||
          data?.visitorId ||
          data?.guestId ||
          ""
        ).trim()
        const fullName = String(data?.fullName || data?.visitorName || "").trim()
        const parts = []
        if (guestId) parts.push(`ID: ${guestId}`)
        if (fullName) parts.push(`TEN: ${fullName}`)
        return parts.join(" | ")
      }

      const employeeId = String(data?.employeeId || "").trim()
      const employeeName = String(data?.employeeName || "").trim()
      const parts = []
      if (employeeId) parts.push(`ID: ${employeeId}`)
      if (employeeName) parts.push(`TEN: ${employeeName}`)
      return parts.join(" | ")
    },

    async toggleRuntime(name) {
      if (this.runtimeIsBusy(name)) return
      this.setTogglePending(name, true)
      this.runtimeBusy = { ...this.runtimeBusy, [name]: true }
      try {
        const isRunning = this.runtimeRunning(name)
        if (isRunning) {
          await stopRuntimeService(name)
        } else {
          await startRuntimeService(name)
        }
      } catch (e) {
        console.error("toggleRuntime", name, e)
        alert(e?.response?.data?.message || e?.message || "Không thể bật/tắt runtime service.")
      } finally {
        this.runtimeBusy = { ...this.runtimeBusy, [name]: false }
        this.setTogglePending(name, false)
        await this.refreshDrawerServiceState()
      }
    },

    async toggleRuntimeAutoStart(name) {
      if (this.runtimeIsBusy(name)) return
      this.runtimeBusy = { ...this.runtimeBusy, [name]: true }
      try {
        await updateRuntimeService(name, { autoStart: !this.runtimeAutoStart(name) })
      } catch (e) {
        console.error("toggleRuntimeAutoStart", name, e)
        alert(e?.response?.data?.message || e?.message || "Không thể cập nhật AutoStart.")
      } finally {
        this.runtimeBusy = { ...this.runtimeBusy, [name]: false }
        await this.refreshDrawerServiceState()
      }
    },

    onToggleQrPython() {
      if (this.settingsQrBusy) return
      const wantOn = !this.qrPythonServiceOn
      this.applyQrPythonService(wantOn)
    },

    onTogglePlatePython() {
      if (this.settingsPlateBusy) return
      const wantOn = !this.platePythonServiceOn
      this.applyPlatePythonService(wantOn)
    },

    async applyQrPythonService(wantOn) {
      if (this.settingsQrBusy) return
      this.setTogglePending("python_qr", true)

      if (!wantOn) {
        this.settingsQrBusy = true
        try {
          for (const lane of this.lanes) {
            try {
              await this.stopLaneQrScanner(lane)
            } catch (e) {
              console.warn(`stopQrScanner ${lane.id}`, e)
            }
          }

          try {
            await stopPythonQrProcess()
          } catch (e) {
            console.warn("stopPythonQrProcess", e)
          }

          for (const lane of this.lanes) {
            if (lane.qr.resultTimer) {
              clearInterval(lane.qr.resultTimer)
              lane.qr.resultTimer = null
            }
            this.stopQrLoops(lane)
            this.hardResetQr(lane.qr)
            this.resetPreview(lane.qr)
            lane.qr.cameraRunning = false
          }
        } finally {
          this.settingsQrBusy = false
          this.setTogglePending("python_qr", false)
          await this.refreshDrawerServiceState()
        }
        return
      }

      const lane = this.activeOpsLane
      const ip = String(lane.qr.cameraIp || "").trim()
      if (!ip) {
        alert("Chọn URL/stream camera QR ở tab làn hiện tại trước khi bật dịch vụ Python.")
        this.setTogglePending("python_qr", false)
        return
      }

      this.settingsQrBusy = true
      try {
        // Bật tiến trình Python rồi mở camera để preview, chưa quét ngay.
        await startPythonQrProcess()
        // Chờ tiến trình khởi động
        await new Promise((r) => setTimeout(r, 2000))

        // Đợi QR API sẵn sàng rồi chỉ mở camera, reset trạng thái để không tự quét
        try {
          const startAt = Date.now()
          let qrReady = false
          while (Date.now() - startAt < 8000) {
            try {
              await this.getLaneQrScanResult(lane)
              qrReady = true
              break
            } catch (e) {
              await new Promise((r) => setTimeout(r, 500))
            }
          }

          if (qrReady) {
            try {
              await this.startLaneQrScanner(lane, ip).catch(() => {})
              await this.resetLaneQrSession(lane).catch(() => {})
            } catch (e) {
              // ignore
            }
          }
        } catch (e) {
          // ignore top-level errors
        }

        const ln = this.activeOpsLane
        if (ln.qr.viewUrl && !ln.qr.previewRunning) {
          this.mountPreview(ln.qr, ln.qr.viewUrl)
        }
        ln.qr.scanRequested = false
        ln.qr.message = "Preview ready (chưa quét)"
      } catch (e) {
        console.error("applyQrPythonService", e)
        alert(e?.message || "Không bật được dịch vụ QR Python.")
      } finally {
        this.settingsQrBusy = false
        this.setTogglePending("python_qr", false)
        await this.refreshDrawerServiceState()
      }
    },

    async applyPlatePythonService(wantOn) {
      if (this.settingsPlateBusy) return
      this.setTogglePending("python_plate", true)

      if (!wantOn) {
        this.settingsPlateBusy = true
        try {
          try {
            await this.lanes[0].plateApi.turnOffCamera()
          } catch (e) {
            console.warn("turnOffCamera", e)
          }

          try {
            await stopPythonPlateProcess()
          } catch (e) {
            console.warn("stopPythonPlateProcess", e)
          }

          for (const lane of this.lanes) {
            this.stopPlateLoop(lane)
            this.hardResetPlate(lane.plate)
            this.resetPreview(lane.plate)
            lane.plate.cameraRunning = false
          }
        } finally {
          this.settingsPlateBusy = false
          this.setTogglePending("python_plate", false)
          await this.refreshDrawerServiceState()
        }
        return
      }

      const preferredLane = this.activeOpsLane
      const lane =
        [preferredLane, ...this.lanes.filter((x) => x.id !== preferredLane.id)].find((ln) =>
          String(ln?.plate?.currentIp || ln?.plate?.cameraIp || ln?.plate?.viewUrl || "").trim()
        ) || preferredLane

      const ip = String(lane.plate.currentIp || lane.plate.cameraIp || lane.plate.viewUrl || "").trim()
      if (!ip) {
        alert("Chọn URL/stream camera biển số ở tab làn hiện tại trước khi bật dịch vụ Python.")
        this.setTogglePending("python_plate", false)
        return
      }

      this.settingsPlateBusy = true
      try {
        // Bật tiến trình Python cho plate và mở camera ngay để có preview,
        // nhưng chưa thực hiện quét cho đến khi người dùng bấm nút Đọc.
        await startPythonPlateProcess()
        await this.waitForPlateApiReady(45000)
        this.releaseOtherPlateLanes(lane)
        await lane.plateApi.turnOnCamera(ip)

        if (lane.plate.viewUrl && !lane.plate.previewRunning) {
          this.mountPreview(lane.plate, lane.plate.viewUrl)
        }
        lane.plate.currentIp = ip
        lane.plate.cameraRunning = true
        lane.plate.scanActive = false
        lane.plate.message = "Preview ready (chưa quét)"
        if (!lane.plate.resultTimer) this.startPlateLoop(lane)
      } catch (e) {
        console.error("applyPlatePythonService", e)
        alert(e?.message || "Không bật được dịch vụ biển số.")
      } finally {
        this.settingsPlateBusy = false
        this.setTogglePending("python_plate", false)
        await this.refreshDrawerServiceState()
      }
    },

    async waitForPlateApiReady(timeoutMs = 45000) {
      const startedAt = Date.now()
      let lastError = null

      while (Date.now() - startedAt < timeoutMs) {
        try {
          const base = plateLane1Api.getResolvedPlateApiBaseUrl() || PLATE_API_BASE_URL
          await axios.get(`${String(base).replace(/\/+$/, "")}/health`, { timeout: 2000 })
          return
        } catch (e) {
          lastError = e
          await new Promise((r) => setTimeout(r, 1200))
        }
      }

      throw new Error(
        lastError?.message ||
          "Python biển số khởi động chậm hoặc thất bại (quá thời gian chờ health)."
      )
    },

    startQrPolling(lane) {
      if (lane.qr.resultTimer) return

      lane.qr.resultTimer = setInterval(async () => {
        const qr = lane.qr
        if (!qr.cameraRunning || qr.pollingBusy) return

        const pollingSessionId = qr.controlSessionId
        qr.pollingBusy = true

        try {
          const res = await this.getLaneQrScanResult(lane).catch(() => null)
          if (qr.controlSessionId !== pollingSessionId || !res) return

          qr.scanRequested = !!res.scan_enabled
          qr.backendPhase = String(res.phase || "idle")
          qr.backendConnected = !!res.connected
          qr.backendFrameReady = !!res.frame_ready
          qr.backendLastCandidate = String(res.candidate_payload || "").trim()
          qr.backendLastSource = String(res.candidate_source || "").trim()
          qr.backendCandidateSeenCount = Number(res.candidate_seen_count || 0)
          qr.backendLockedPayload = String(res.locked_payload || res.qr || "").trim()
          qr.backendLockedAt = Number(res.locked_at || 0)
          qr.backendLastDecodeAt = Number(res.locked_at || 0)

          if (!qr.sessionLocked && !qr.verifying && !qr.scanRequested) {
            const phase = String(qr.backendPhase || "idle")
            const canKickoff =
              phase === "idle" ||
              phase === "verified" ||
              phase === "expired" ||
              phase === "invalid"
            if (canKickoff && Date.now() - qr.lastAutoScanAt >= qr.autoScanCooldownMs) {
              await this.kickoffQrScan(lane).catch(() => {})
            }
          }

          qr.sessionLocked =
            qr.backendPhase === "locked" ||
            qr.activeSessionVerifyState === "success"

          if (qr.backendPhase === "connecting") {
            qr.message = "Đang kết nối camera QR..."
            return
          }

          if (qr.backendPhase === "scanning") {
            qr.message = "Đang quét QR..."
            return
          }

          if (qr.backendPhase === "candidate_found") {
            qr.message = "Đã thấy mã, đang ổn định khung hình..."
            qr.overlayText = qr.backendLastCandidate || qr.overlayText
            return
          }

          if (qr.backendPhase === "locked") {
            const servicePayload = String(qr.backendLockedPayload || "").trim()
            if (!servicePayload) return
            if (qr.lastVerifiedPayload === servicePayload || qr.verifying) return

            qr.qrPayload = servicePayload
            qr.sessionLocked = true
            qr.activeSessionPayload = servicePayload
            qr.scanRequested = false
            qr.message = "Đã khóa QR, đang xác thực..."

            const result = await this.doVerifyQr(lane, servicePayload)
            if (result?.success) {
              qr.lastVerifiedPayload = servicePayload
              qr.activeSessionPayload = servicePayload
              qr.activeSessionVerified = true
              qr.activeSessionVerifyState = "success"
              qr.activeSessionVerifyMessage =
                result?.message || "Xác thực QR thành công."
              qr.verifyMessage = ""
              qr.sessionLocked = true
              if (result?.data?.type === "STATIC") {
                qr.guestId = String(
                  result?.data?.visitorDetailId ||
                    result?.data?.visitorId ||
                    result?.data?.guestId ||
                    ""
                )
                qr.employeeId = ""
                qr.employeeName = result.data.fullName || result.data.visitorName || ""
              } else {
                qr.employeeId = String(result.data.employeeId || "")
                qr.employeeName = result.data.employeeName || ""
                qr.guestId = ""
              }

              const identityOverlay = this.buildQrIdentityOverlay(result?.data)
              if (identityOverlay) qr.overlayText = identityOverlay
              qr.alert = false
              qr.backendPhase = "verified"
              qr.message = result?.message || "Xác thực QR thành công."
            } else {
              qr.alert = true
              qr.activeSessionVerified = false
              qr.sessionLocked = false
              qr.activeSessionPayload = ""
              qr.activeSessionVerifyState = "failed"
              qr.activeSessionVerifyMessage = result?.message || "Xác thực QR thất bại."
              qr.verifyMessage = result?.message || "Xác thực QR thất bại."
              qr.backendPhase = "scanning"
              if (qr.activeSessionVerifyState === "expired" || qr.activeSessionVerifyState === "invalid") {
                await this.kickoffQrScan(lane).catch(() => {})
              }
            }
          }
        } finally {
          if (qr.controlSessionId === pollingSessionId) {
            qr.pollingBusy = false
          }
        }
      }, 300)
    },

        isLaneReady(lane) {
      return (
        lane.qr.sessionLocked &&
        lane.plate.scanLocked &&
        (!!lane.qr.employeeId || !!lane.qr.guestId) &&
        !!lane.plate.confirmedPlate &&
        !lane.qr.alert
      )
    },

    laneAnyRunning(lane) {
      return lane.qr.cameraRunning || lane.plate.cameraRunning
    },

    qrStateText(qr) {
      if (!qr.cameraRunning) return "CHỜ"
      if (!qr.activeSessionPayload) return "ĐANG QUÉT"
      if (qr.activeSessionVerifyState === "waiting") return "ĐANG XÁC THỰC"
      if (qr.activeSessionVerifyState === "success") return "ĐÃ NHẬN DIỆN"
      if (qr.activeSessionVerifyState === "expired") return "HẾT HẠN"
      if (qr.activeSessionVerifyState === "invalid") return "KHÔNG HỢP LỆ"
      if (qr.activeSessionVerifyState === "failed") return "THẤT BẠI"
      if (qr.activeSessionVerifyState === "system_error") return "LỖI HỆ THỐNG"
      return "Đang xử lý"
    },

    qrStateClass(qr) {
      if (qr.alert) return "danger-text"
      if (qr.sessionLocked && (qr.employeeId || qr.guestId)) return "ok-text"
      return "warn-text"
    },

    hasInvalidHint(message) {
      const normalized = String(message || "").toLowerCase()
      return (
        normalized.includes("không hợp lệ") ||
        normalized.includes("thất bại") ||
        normalized.includes("hết hạn") ||
        normalized.includes("timeout") ||
        normalized.includes("quá thời gian")
      )
    },

    qrBackendPhase(qr) {
      if (qr.activeSessionVerifyState === "success") return "verified"
      return String(qr.backendPhase || "idle")
    },

    cameraVisualState(type, lane) {
      if (type === "qr") {
        const qr = lane.qr
        const phase = this.qrBackendPhase(qr)
        if (!qr.cameraRunning) return "idle"
        if (qr.alert || this.hasInvalidHint(qr.verifyMessage || qr.message)) return "invalid"
        if (phase === "verified" && (qr.employeeId || qr.guestId)) return "valid"
        if (phase === "connecting" || phase === "scanning" || phase === "candidate_found" || phase === "locked" || qr.verifying) return "scanning"
        return "idle"
      }

      const plate = lane.plate
      if (!plate.cameraRunning) return "idle"
      if (this.hasInvalidHint(plate.message)) return "invalid"
      if (plate.scanLocked && !!plate.confirmedPlate) return "valid"
      if (plate.scanActive) return "scanning"
      return "idle"
    },

    cameraVisualText(type, lane) {
      if (type === "qr") {
        const phase = this.qrBackendPhase(lane.qr)
        if (this.cameraVisualState(type, lane) === "invalid") return "LỖI / QUÁ THỜI GIAN"
        if (phase === "verified") return "VALID"
        if (phase === "locked") return "VERIFYING"
        if (phase === "candidate_found") return "SEEN"
        if (phase === "connecting") return "CONNECTING"
        if (phase === "scanning") return "SCANNING"
        return "IDLE"
      }

      const state = this.cameraVisualState(type, lane)
      if (state === "valid") return "VALID"
      if (state === "invalid") return "LỖI / QUÁ THỜI GIAN"
      if (state === "scanning") return "SCANNING"
      return "IDLE"
    },

    normalizeBox(raw) {
      if (!raw || typeof raw !== "object") return null
      if (Array.isArray(raw)) {
        if (!raw.length) return null
        return this.normalizeBox(raw[0])
      }
      const toNumericBox = (x, y, width, height) => ({
        x: Number(x),
        y: Number(y),
        width: Number(width),
        height: Number(height)
      })
      const hasFiniteBox = (box) =>
        Number.isFinite(box.x) &&
        Number.isFinite(box.y) &&
        Number.isFinite(box.width) &&
        Number.isFinite(box.height) &&
        box.width >= 0 &&
        box.height >= 0

      const hasNewShape =
        Number.isFinite(Number(raw.x)) &&
        Number.isFinite(Number(raw.y)) &&
        Number.isFinite(Number(raw.width)) &&
        Number.isFinite(Number(raw.height))

      if (hasNewShape) {
        const box = toNumericBox(raw.x, raw.y, raw.width, raw.height)
        if (!hasFiniteBox(box)) return null
        if (box.x <= 1 && box.y <= 1 && box.width <= 1 && box.height <= 1) {
          return { ...box, x: box.x * 100, y: box.y * 100, width: box.width * 100, height: box.height * 100, unit: "%" }
        }
        if (box.x <= 100 && box.y <= 100 && box.width <= 100 && box.height <= 100) {
          return { ...box, unit: "%" }
        }
        return { ...box, unit: "px" }
      }

      const hasLegacyShape =
        Number.isFinite(Number(raw.x1)) &&
        Number.isFinite(Number(raw.y1)) &&
        Number.isFinite(Number(raw.x2)) &&
        Number.isFinite(Number(raw.y2))

      if (!hasLegacyShape) return null

      const x1 = Number(raw.x1)
      const y1 = Number(raw.y1)
      const x2 = Number(raw.x2)
      const y2 = Number(raw.y2)
      const legacy = {
        x: Math.min(x1, x2),
        y: Math.min(y1, y2),
        width: Math.abs(x2 - x1),
        height: Math.abs(y2 - y1)
      }
      if (!hasFiniteBox(legacy)) return null
      return legacy.x <= 100 && legacy.y <= 100 && legacy.width <= 100 && legacy.height <= 100
        ? { ...legacy, unit: "%" }
        : { ...legacy, unit: "px" }
    },

    boundingStyle(box) {
      if (!box) return {}
      const unit = box.unit === "px" ? "px" : "%"
      return {
        left: `${box.x}${unit}`,
        top: `${box.y}${unit}`,
        width: `${box.width}${unit}`,
        height: `${box.height}${unit}`
      }
    },

    labelStyle(box) {
      const unit = box?.unit === "px" ? "px" : "%"
      const left = Number(box?.x ?? 1)
      const top = Number(box?.y ?? 1)
      if (unit === "px") {
        return {
          left: `${Math.max(4, left)}px`,
          top: `${Math.max(4, top - 22)}px`
        }
      }
      return {
        left: `${Math.max(1, Math.min(80, left))}%`,
        top: `${Math.max(1, top - 6)}%`
      }
    },

    shortText(value, max = 60) {
      const text = String(value || "").trim()
      if (!text) return "-----"
      return text.length <= max ? text : text.slice(0, max) + "..."
    },

    formatDate(value) {
      if (!value) return ""
      return new Date(value).toLocaleString()
    },

    nowText() {
      return new Date().toLocaleString()
    },

    buildDirectCameraUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return ""

      if (raw.startsWith("data:image/")) return raw

      const normalized = normalizeCameraUrl(raw)
      if (/^https?:\/\//i.test(normalized) || normalized.startsWith("/")) {
        try {
          const parsed = new URL(normalized, window.location.origin)
          if (parsed.pathname.endsWith("/stream.html")) {
            // Ưu tiên MSE khi chạy qua reverse proxy; WebRTC vẫn là fallback.
            parsed.searchParams.set("mode", "mse,webrtc")
            return parsed.toString()
          }
        } catch {
          // Giữ nguyên URL nếu đây không phải URL chuẩn.
        }
        return normalized
      }

      // Prevent relative path fallback to Vue route (causing iframe overlay over content).
      const base = String(QR_API_BASE_URL || "").replace(/\/+$/, "")
      const path = raw.replace(/^\/+/, "")
      return `${base}/${path}`
    },

    async attachQrVideoPreview(laneId, qr) {
      await this.$nextTick()
      const video = this.qrVideoRefs[laneId]
      if (!video) return

      this.destroyQrHls(qr)

      try {
        video.pause()
      } catch {
        // ignore
      }

      video.removeAttribute("src")
      video.load()

      qr.previewHealthy = false
      video.src = qr.videoPreviewUrl

      try {
        await video.play()
      } catch {
        // ignore autoplay rejection
      }
    },

    async attachQrHlsPreview(laneId, qr) {
      await this.$nextTick()
      const video = this.qrVideoRefs[laneId]
      if (!video) return

      this.destroyQrHls(qr)

      try {
        video.pause()
      } catch {
        // ignore
      }

      video.removeAttribute("src")
      video.load()

      qr.previewHealthy = false

      if (video.canPlayType("application/vnd.apple.mpegurl")) {
        video.src = qr.videoPreviewUrl
        try {
          await video.play()
        } catch {
          // ignore autoplay rejection
        }
        return
      }

      try {
        const Hls = await this.ensureHlsLibrary()
        if (!Hls?.isSupported?.()) {
          throw new Error("Trình duyệt này không hỗ trợ HLS.")
        }

        qr.hlsInstance = new Hls({
          enableWorker: true,
          lowLatencyMode: true
        })

        qr.hlsInstance.on(Hls.Events.ERROR, (_, data) => {
          if (data?.fatal) {
            this.onQrVideoPreviewError({ qr })
          }
        })

        qr.hlsInstance.loadSource(qr.videoPreviewUrl)
        qr.hlsInstance.attachMedia(video)
        qr.hlsInstance.on(Hls.Events.MANIFEST_PARSED, async () => {
          try {
            await video.play()
          } catch {
            // ignore autoplay rejection
          }
        })
      } catch (error) {
        console.error("attachQrHlsPreview error:", error)
        this.onQrVideoPreviewError({ qr })
      }
    },

    destroyQrHls(qr) {
      if (qr?.hlsInstance) {
        qr.hlsInstance.destroy()
        qr.hlsInstance = null
      }
    },

    destroyQrPreviewMedia(laneId, qr) {
      this.destroyQrHls(qr)
      const video = this.qrVideoRefs[laneId]
      if (video) {
        try {
          video.pause()
        } catch {
          // ignore
        }

        video.removeAttribute("src")
        video.load()
      }
    },

    async enableQrPreview(qr, url, laneId) {
      const cleanUrl = String(url || "").trim()
      if (!cleanUrl) return

      qr.previewMode = resolveQrPreviewMode(cleanUrl)
      qr.previewHealthy = false
      qr.previewRunning = true
      qr.directCameraKey += 1

      if (qr.previewMode === "image") {
        this.destroyQrPreviewMedia(laneId, qr)
        qr.videoPreviewUrl = ""
        qr.directCameraUrl = this.buildDirectCameraUrl(cleanUrl)
        return
      }

      qr.directCameraUrl = ""
      qr.videoPreviewUrl = cleanUrl

      if (qr.previewMode === "video") {
        await this.attachQrVideoPreview(laneId, qr)
        return
      }

      if (qr.previewMode === "hls") {
        await this.attachQrHlsPreview(laneId, qr)
      }
    },

    isImagePreviewableUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return false
      if (raw.startsWith("data:image/")) return true
      if (/^rtsp:\/\//i.test(raw)) return false
      if (/\.mp4(\?|$)/i.test(raw)) return false
      if (/\.m3u8(\?|$)/i.test(raw)) return false
      return /^https?:\/\//i.test(raw) || raw.startsWith("/")
    },

    mountPreview(module, url) {
      const cleanUrl = String(url || "").trim()
      if (!cleanUrl) return
      module.directCameraUrl = this.buildDirectCameraUrl(cleanUrl)
      module.directCameraKey += 1
      module.previewHealthy = false
      module.previewRunning = true
    },

    enablePlatePreview(module, url) {
      module.previewRunning = true

      if (this.isImagePreviewableUrl(url)) {
        this.mountPreview(module, url)
        return
      }

      module.directCameraUrl = ""
      module.directCameraKey += 1
      module.previewHealthy = !!(module.lockedSnapshot || module.lockedPlateCrop)
    },

    platePreviewDisplayUrl(plate) {
      if (!plate.previewRunning) return ""
      return plate.lockedSnapshot || plate.lockedPlateCrop || plate.directCameraUrl || ""
    },

    platePreviewKey(plate) {
      if (plate.lockedSnapshot || plate.lockedPlateCrop) {
        return `plate-capture-${plate.sessionId}-${plate.lastLockedImageSessionId}`
      }

      return plate.directCameraKey
    },

    platePreviewStatusText(plate) {
      if (!plate.previewRunning) return "Đang tắt"
      if (plate.lockedSnapshot || plate.lockedPlateCrop) return "Ảnh đã chụp"
      return plate.previewHealthy ? "Đang trực tuyến" : "Chờ hình ảnh"
    },

    platePreviewStatusClass(plate) {
      if (!plate.previewRunning) return "wait"
      if (plate.lockedSnapshot || plate.lockedPlateCrop) return "ok"
      return plate.previewHealthy ? "ok" : "wait"
    },

    refreshDirectPreview(module) {
      if (!module.previewRunning || !module.viewUrl) return
      if (module.imgBusy) return
      module.imgBusy = true
      module.directCameraUrl = this.buildDirectCameraUrl(module.viewUrl)
    },

    resetPreview(module) {
      module.directCameraUrl = ""
      module.directCameraKey += 1
      module.previewHealthy = false
      module.previewRunning = false
      module.imgBusy = false
      module.decodeBusy = false
      module.frameWidth = 0
      module.frameHeight = 0
    },

    resetQrPreview(laneId, qr) {
      this.destroyQrPreviewMedia(laneId, qr)
      qr.directCameraUrl = ""
      qr.videoPreviewUrl = ""
      qr.previewMode = "empty"
      qr.directCameraKey += 1
      qr.previewHealthy = false
      qr.previewRunning = false
      qr.imgBusy = false
      qr.decodeBusy = false
      qr.frameWidth = 0
      qr.frameHeight = 0
    },

    onPreviewLoaded(module) {
      module.previewHealthy = true
    },

    onPreviewError(module) {
      module.previewHealthy = false
    },

    onQrPreviewError(lane) {
      lane.qr.previewHealthy = false
      lane.qr.imgBusy = false
    },

    async onQrPreviewLoaded(lane) {
      lane.qr.previewHealthy = true
      lane.qr.imgBusy = false

      if (lane.qr.decodeBusy || lane.qr.verifying) return
      // Nếu chỉ đang preview (chưa bật chế độ quét trên Python) thì không tự decode
      if (!lane.qr.cameraRunning) return
      await this.captureAndDecodeQr(lane)
    },

    async onQrVideoPreviewLoaded(lane) {
      lane.qr.previewHealthy = true
      if (lane.qr.decodeBusy || lane.qr.verifying) return
      // Nếu chỉ đang preview (chưa bật chế độ quét trên Python) thì không tự decode
      if (!lane.qr.cameraRunning) return
      await this.captureAndDecodeQr(lane)
    },

    onQrVideoPreviewError(lane) {
      lane.qr.previewHealthy = false
    },

    clearQrState(qr) {
      qr.qrPayload = ""
      qr.manualPayload = ""
      qr.verifyMessage = ""
      qr.verifyData = null
      qr.employeeId = ""
      qr.employeeName = ""
      qr.guestId = ""
      qr.personType = ""

      qr.activeSessionPayload = ""
      qr.activeSessionVerified = false
      qr.activeSessionVerifyState = ""
      qr.activeSessionVerifyMessage = ""
      qr.lastSeenAt = null

      qr.lastDecodedText = ""
      qr.lastDecodedAt = 0
      qr.stablePayload = ""
      qr.stablePayloadCount = 0
      qr.lastStablePayloadAt = 0
      qr.backendPhase = "idle"
      qr.backendConnected = false
      qr.backendFrameReady = false
      qr.backendLastCandidate = ""
      qr.backendLastSource = ""
      qr.backendLastDecodeAt = 0
      qr.backendCandidateSeenCount = 0
      qr.backendLockedPayload = ""
      qr.backendLockedAt = 0
      qr.lastVerifiedPayload = ""
      qr.lastUpdate = ""
      qr.message = ""
      qr.alert = false
      qr.sessionLocked = false
      qr.lockedSnapshot = ""
      qr.overlayText = ""
      qr.overlayBox = null
      qr.scanRequested = false
      qr.scanKickoffBusy = false
    },

    clearPlateState(plate) {
      plate.confirmedPlate = ""
      plate.lastRawPlate = ""
      plate.scanLocked = false
      plate.scanActive = false
      plate.lockedSnapshot = ""
      plate.lockedPlateCrop = ""
      plate.message = ""
      plate.fps = 0
      plate.ocrRunning = false
      plate.stableCount = 0
      plate.movingFast = false
      plate.lastUpdate = ""
      plate.lastLockedImageSessionId = 0
      plate.overlayText = ""
      plate.overlayBox = null
    },

    hardResetQr(qr) {
      qr.cameraRunning = false
      qr.currentIp = ""
      qr.controlSessionId = (qr.controlSessionId || 0) + 1
      qr.pollingBusy = false
      qr.verifying = false
      qr.decodeBusy = false
      this.clearQrState(qr)
    },

    hardResetPlate(plate) {
      plate.cameraRunning = false
      plate.currentIp = ""
      plate.sessionId = 0
      plate.lastAppliedSessionId = 0
      this.clearPlateState(plate)
    },

    releaseOtherPlateLanes(activeLane) {
      for (const lane of this.lanes) {
        if (lane.id === activeLane.id) continue
        this.stopPlateLoop(lane)
        lane.plate.cameraRunning = false
        lane.plate.sessionId = 0
        lane.plate.lastAppliedSessionId = 0
        lane.plate.lastLockedImageSessionId = 0
        this.clearPlateState(lane.plate)
      }
    },

    startQrPreviewLoop(lane) {
      this.stopQrPreviewLoop(lane)
      lane.qr.previewTimer = setInterval(() => {
        if (lane.qr.destroyed) return
        if (!lane.qr.cameraRunning) return
        if (lane.qr.previewMode === "image") {
          this.refreshDirectPreview(lane.qr)
          return
        }

        if (lane.qr.previewMode === "video" || lane.qr.previewMode === "hls") {
          this.captureAndDecodeQr(lane)
        }
      }, lane.qr.previewIntervalMs)
    },

    stopQrPreviewLoop(lane) {
      if (lane.qr.previewTimer) {
        clearInterval(lane.qr.previewTimer)
        lane.qr.previewTimer = null
      }
    },

    startQrSessionLoop(lane) {
      this.stopQrSessionLoop(lane)
      lane.qr.sessionTimer = setInterval(() => {
        if (lane.qr.destroyed) return
        if (!lane.qr.cameraRunning) return
        this.checkQrSessionExpiry(lane)
      }, 200)
    },

    stopQrSessionLoop(lane) {
      if (lane.qr.sessionTimer) {
        clearInterval(lane.qr.sessionTimer)
        lane.qr.sessionTimer = null
      }
    },

    stopQrLoops(lane) {
      this.stopQrPreviewLoop(lane)
      this.stopQrSessionLoop(lane)
    },

    async kickoffQrScan(lane) {
      const qr = lane?.qr
      if (!qr || !qr.cameraRunning || qr.scanKickoffBusy) return

      const kickoffSessionId = qr.controlSessionId
      qr.scanKickoffBusy = true
      qr.scanRequested = false

      try {
        await this.resetLaneQrSession(lane).catch(() => {})
        if (qr.controlSessionId !== kickoffSessionId) return

        await new Promise((r) => setTimeout(r, 100))
        if (qr.controlSessionId !== kickoffSessionId) return

        await this.scanLaneQrOnce(lane).catch(() => {})
        if (qr.controlSessionId !== kickoffSessionId) return

        qr.scanRequested = true
        qr.lastAutoScanAt = Date.now()
      } finally {
        if (qr.controlSessionId === kickoffSessionId) {
          qr.scanKickoffBusy = false
        }
      }
    },

    async restartQrSession(lane, message = "Đang quét lại QR...") {
      const qr = lane?.qr
      if (!qr?.cameraIp?.trim()) {
        throw new Error("Vui lòng nhập URL QR")
      }

      const nextSessionId = (qr.controlSessionId || 0) + 1
      qr.controlSessionId = nextSessionId

      this.stopQrLoops(lane)
      if (qr.resultTimer) {
        clearInterval(qr.resultTimer)
        qr.resultTimer = null
      }
      qr.pollingBusy = false
      qr.verifying = false
      qr.decodeBusy = false
      this.clearQrState(qr)
      qr.message = "Đang khởi động lại bộ quét QR..."

      try {
        await this.stopLaneQrScanner(lane).catch(() => {})
        if (qr.controlSessionId !== nextSessionId) return

        await new Promise((r) => setTimeout(r, 180))
        if (qr.controlSessionId !== nextSessionId) return

        qr.cameraIp = this.getEffectiveQrStream(lane)
        await this.startLaneQrScanner(lane, qr.cameraIp)
        if (qr.controlSessionId !== nextSessionId) return

        await new Promise((r) => setTimeout(r, 800))
        if (qr.controlSessionId !== nextSessionId) return

        qr.cameraRunning = true
        qr.sessionLocked = false
        qr.message = message

        this.startQrPreviewLoop(lane)
        this.startQrSessionLoop(lane)
        this.startQrPolling(lane)
        await this.kickoffQrScan(lane)
      } catch (e) {
        if (qr.controlSessionId === nextSessionId) {
          this.hardResetQr(qr)
        }
        throw e
      }
    },

    checkQrSessionExpiry(lane) {
      const qr = lane.qr
      if (qr.sessionLocked) return
      if (!qr.activeSessionPayload || !qr.lastSeenAt) return

      const now = Date.now()
      const diff = now - qr.lastSeenAt

      if (diff >= qr.absenceThresholdMs) {
        this.clearQrState(qr)
        qr.message = "Phiên cũ đã hết hạn; đang chờ mã mới."
        qr.lastUpdate = this.nowText()
      }
    },

    async captureAndDecodeQr(lane) {
      return
      const qr = lane.qr
      const canvas = this.qrCanvasRefs[lane.id]
      const mode = qr.previewMode

      if (!canvas) return
      if (qr.decodeBusy) return
      if (qr.cameraRunning) return

      let source = null
      let sourceWidth = 0
      let sourceHeight = 0

      if (mode === "image") {
        const img = this.qrImageRefs[lane.id]
        if (!img || !img.complete) return
        source = img
        sourceWidth = img.naturalWidth || img.width
        sourceHeight = img.naturalHeight || img.height
      } else if (mode === "video" || mode === "hls") {
        const video = this.qrVideoRefs[lane.id]
        if (!video || video.readyState < 2) return
        source = video
        sourceWidth = video.videoWidth || video.clientWidth
        sourceHeight = video.videoHeight || video.clientHeight
      } else {
        return
      }

      qr.decodeBusy = true

      try {
        if (!sourceWidth || !sourceHeight) return

        qr.frameWidth = sourceWidth
        qr.frameHeight = sourceHeight

        let targetWidth = sourceWidth
        let targetHeight = sourceHeight

        if (sourceWidth > qr.decodeMaxWidth) {
          const ratio = qr.decodeMaxWidth / sourceWidth
          targetWidth = Math.round(sourceWidth * ratio)
          targetHeight = Math.round(sourceHeight * ratio)
        }

        canvas.width = targetWidth
        canvas.height = targetHeight

        const ctx = canvas.getContext("2d", { willReadFrequently: true })
        ctx.clearRect(0, 0, targetWidth, targetHeight)
        ctx.drawImage(source, 0, 0, targetWidth, targetHeight)

        const imageData = ctx.getImageData(0, 0, targetWidth, targetHeight)
        const code = jsQR(imageData.data, targetWidth, targetHeight, {
          inversionAttempts: "attemptBoth"
        })

        if (!code?.data) return

        const decodedText = String(code.data || "").trim()
        if (!decodedText) return

        const now = Date.now()
        const location = code.location || {}
        const points = [
          location.topLeftCorner,
          location.topRightCorner,
          location.bottomLeftCorner,
          location.bottomRightCorner
        ].filter(Boolean)

        if (points.length >= 2) {
          const xs = points.map((p) => Number(p.x || 0))
          const ys = points.map((p) => Number(p.y || 0))
          const minX = Math.max(0, Math.min(...xs))
          const minY = Math.max(0, Math.min(...ys))
          const maxX = Math.min(targetWidth, Math.max(...xs))
          const maxY = Math.min(targetHeight, Math.max(...ys))
          qr.overlayBox = this.normalizeBox({
            x: (minX / targetWidth) * 100,
            y: (minY / targetHeight) * 100,
            width: ((maxX - minX) / targetWidth) * 100,
            height: ((maxY - minY) / targetHeight) * 100
          })
        } else {
          qr.overlayBox = null
        }
        qr.overlayText = decodedText

        qr.qrPayload = decodedText
        qr.manualPayload = decodedText
        qr.lastDecodedText = decodedText
        qr.lastDecodedAt = now

        if (
          qr.stablePayload === decodedText &&
          now - qr.lastStablePayloadAt <= qr.stablePayloadWindowMs
        ) {
          qr.stablePayloadCount += 1
        } else {
          qr.stablePayload = decodedText
          qr.stablePayloadCount = 1
        }
        qr.lastStablePayloadAt = now

        if (qr.stablePayloadCount < qr.stablePayloadRequiredCount) {
          qr.activeSessionVerifyState = "waiting"
          qr.activeSessionVerifyMessage = "Da thay QR, giu yen them mot nhip de xac thuc."
          qr.message = "Đã thấy QR, đang chờ khung hình ổn định..."
          qr.lastSeenAt = now
          return
        }

        if (qr.activeSessionPayload && decodedText === qr.activeSessionPayload) {
          qr.lastSeenAt = now
          return
        }

        if (
          qr.lastVerifyAttemptPayload === decodedText &&
          now - qr.lastVerifyAttemptAt < qr.verifyCooldownMs
        ) {
          qr.lastSeenAt = now
          qr.activeSessionVerifyState = "waiting"
          qr.activeSessionVerifyMessage = "QR vừa được gửi xác thực, đang chờ nhịp kế tiếp."
          qr.message = "QR vừa được gửi xác thực, đang chờ phản hồi ổn định..."
          return
        }

        qr.activeSessionPayload = decodedText
        qr.activeSessionVerified = false
        qr.activeSessionVerifyState = "waiting"
        qr.activeSessionVerifyMessage = "Đã phát hiện mã mới, đang xác thực..."
        qr.lastSeenAt = now
        qr.lastUpdate = this.nowText()
        qr.message = "Đang xác thực QR..."

        const result = await this.doVerifyQr(lane, decodedText)

        if (result?.success) {
          qr.activeSessionVerified = true
          qr.activeSessionVerifyState = "success"
          qr.activeSessionVerifyMessage = result.message || "Xác thực QR thành công."
          qr.sessionLocked = true
          qr.lockedSnapshot = canvas.toDataURL("image/jpeg", 0.92)
          qr.alert = false
                    qr.personType = result?.data?.type || ""

          if (qr.personType === "STATIC") {
  qr.guestId = String(
    result?.data?.visitorDetailId ||
    result?.data?.visitorId ||
    result?.data?.guestId ||
    ""
  )
  qr.employeeId = ""
  qr.employeeName = result?.data?.fullName || result?.data?.visitorName || ""
} else {
            qr.employeeId = result?.data?.employeeId ? String(result.data.employeeId) : ""
            qr.employeeName = result?.data?.employeeName || ""
            qr.guestId = ""
          }
          const identityOverlay = this.buildQrIdentityOverlay(result?.data)
          if (identityOverlay) {
            qr.overlayText = identityOverlay
          }
          qr.message = result.message || "QR hợp lệ"
          return
        }

        const message = String(result?.message || "")
        qr.sessionLocked = false
        qr.employeeId = ""
        qr.employeeName = ""
        qr.guestId = ""
        qr.personType = ""

        if (message.includes("hết hạn") || message.includes("chưa hiệu lực")) {
          qr.activeSessionVerifyState = "expired"
        } else if (message.includes("không hợp lệ")) {
          qr.activeSessionVerifyState = "invalid"
        } else {
          qr.activeSessionVerifyState = "failed"
        }

        qr.activeSessionVerifyMessage = message || "Xác thực thất bại."
        qr.verifyMessage = message || "Xác thực thất bại."
        qr.alert = true
        qr.message = qr.verifyMessage
      } catch (e) {
        console.warn("Decode QR frame error:", e)
        qr.verifyMessage = "Không đọc được frame từ IP camera. Kiểm tra CORS, mixed content hoặc URL stream."
        qr.activeSessionVerifyState = "system_error"
        qr.activeSessionVerifyMessage = qr.verifyMessage
        qr.alert = true
        qr.sessionLocked = false
        qr.employeeId = ""
        qr.employeeName = ""
        qr.guestId = ""
        qr.personType = ""
        qr.message = qr.verifyMessage
      } finally {
        qr.decodeBusy = false
      }
    },

    async doVerifyQr(lane, payload) {
      const qr = lane.qr
      qr.verifying = true

      try {
        const safePayload = String(payload || "").trim()
        qr.lastVerifyAttemptPayload = safePayload
        qr.lastVerifyAttemptAt = Date.now()
        if (!safePayload) {
          return {
            success: false,
            message: "QR rỗng hoặc không đọc được.",
            data: null
          }
        }

        // 🔥 phân loại QR
let result = null

if (safePayload.startsWith("EMP:")) {
  // QR động
  result = await verifyDynamicQr(safePayload, qr.scannerDevice)
}
else if (safePayload.startsWith("VIS:")) {
  // QR khách mời (động) -> backend tự verify theo counter + OTP
  result = await verifyDynamicQr(safePayload, qr.scannerDevice)
}
else {
  return {
    success: false,
    message: "QR không đúng định dạng",
    data: null
  }
}

        qr.verifyMessage = result?.message || ""
        qr.verifyData = result?.data || null
        qr.lastUpdate = this.nowText()

        return {
          success: !!result?.success,
          message: result?.message || "",
          data: result?.data || null
        }
      } catch (error) {
        let message =
          error?.response?.data?.message ||
          error?.message ||
          "Xác thực thất bại."

        if (Number(error?.response?.status || 0) === 429) {
          message = "QR đang được gửi xác thực quá nhanh. Hệ thống đang tự giảm nhịp, vui lòng giữ yên mã thêm một chút."
        }

        qr.verifyMessage = message
        qr.verifyData = null
        qr.lastUpdate = this.nowText()

        return {
          success: false,
          message,
          data: null
        }
      } finally {
        qr.verifying = false
      }
    },

    stopPlateLoop(lane) {
      const plate = lane.plate
      if (plate.resultTimer) {
        clearInterval(plate.resultTimer)
        plate.resultTimer = null
      }
      plate.busyResult = false
    },

    startPlateLoop(lane) {
      this.stopPlateLoop(lane)

      lane.plate.resultTimer = setInterval(async () => {
        if (lane.plate.destroyed) return
        if (!lane.plate.cameraRunning) return
        if (lane.plate.busyResult) return

        lane.plate.busyResult = true
        try {
          await this.refreshPlate(lane)
        } finally {
          lane.plate.busyResult = false
        }
      }, 500)
    },

    async loadStatusPlate(lane) {
      try {
        const res = await lane.plateApi.getCameraStatus()
        await this.applyPlateRealtimeState(lane, res, false)

        if (lane.plate.viewUrl) {
  this.mountPreview(lane.plate, lane.plate.viewUrl)
}
      } catch (e) {
        console.error("loadStatusPlate error:", e)
      }
    },

    async refreshPlate(lane) {
      try {
        const res = await lane.plateApi.getCameraResult()
        await this.applyPlateRealtimeState(lane, res, true)
      } catch (e) {
        console.warn("refreshPlate error:", e)
      }
    },

    async fetchPlateLockedImages(lane, force = false) {
      const plate = lane.plate
      if (plate.destroyed) return
      if (!plate.cameraRunning) return

      if (!plate.scanLocked) {
        plate.lockedSnapshot = ""
        plate.lockedPlateCrop = ""
        plate.lastLockedImageSessionId = 0
        return
      }

      if (plate.isFetchingLockedImages) return
      if (!force && plate.lastLockedImageSessionId === plate.sessionId) return

      plate.isFetchingLockedImages = true
      try {
        const res = await lane.plateApi.getLockedImages()
        const responseSessionId = Number(res?.session_id || 0)

        if (responseSessionId !== plate.sessionId) return

        if (res?.scan_locked) {
          plate.lockedSnapshot = res.locked_snapshot || ""
          plate.lockedPlateCrop = res.locked_plate_crop || ""
          plate.lastLockedImageSessionId = responseSessionId
          if (plate.previewRunning && (plate.lockedSnapshot || plate.lockedPlateCrop)) {
            plate.previewHealthy = true
          }
        } else {
          plate.lockedSnapshot = ""
          plate.lockedPlateCrop = ""
          plate.lastLockedImageSessionId = 0
        }
      } catch (e) {
        console.warn("fetchPlateLockedImages error:", e)
      } finally {
        plate.isFetchingLockedImages = false
      }
    },

    async applyPlateRealtimeState(lane, res, allowTurnOffReset = true) {
      if (!res || lane.plate.destroyed) return

      const plate = lane.plate
      const incomingSessionId = Number(res.session_id || 0)
      const incomingIp = String(res.ip || "").trim()

      const laneSessionId = Number(plate.sessionId || 0)
      const laneCurrentIp = String(plate.currentIp || plate.cameraIp || "").trim()
      const hasLaneSession = laneSessionId > 0
      const hasIncomingSession = incomingSessionId > 0

      if (hasLaneSession && hasIncomingSession && laneSessionId !== incomingSessionId) {
        if (plate.cameraRunning && allowTurnOffReset) {
          this.stopPlateLoop(lane)
          this.hardResetPlate(plate)
        }
        return
      }

      if (!hasLaneSession && !plate.cameraRunning) {
        return
      }

      if (!hasLaneSession && incomingIp && laneCurrentIp && incomingIp !== laneCurrentIp) {
        return
      }

      if (incomingSessionId > 0) {
        if (plate.lastAppliedSessionId > 0 && incomingSessionId < plate.lastAppliedSessionId) {
          return
        }

        if (incomingSessionId > plate.lastAppliedSessionId) {
          plate.lastAppliedSessionId = incomingSessionId
          plate.sessionId = incomingSessionId
          plate.lastLockedImageSessionId = 0
        } else if (!plate.sessionId) {
          plate.sessionId = incomingSessionId
        }
      }

      const incomingCameraEnabled = !!res.camera_enabled

      plate.cameraRunning = incomingCameraEnabled
      plate.currentIp = res.ip || plate.currentIp
      plate.confirmedPlate = res.confirmed_plate || ""
      plate.lastRawPlate = res.last_raw_plate || ""
      plate.scanLocked = !!res.scan_locked
      plate.scanActive = !!res.scan_active
      plate.fps = Number(res.fps || 0)
      plate.ocrRunning = !!res.ocr_running
      plate.stableCount = Number(res.stable_count || 0)
      plate.movingFast = !!res.moving_fast
      plate.message = res.message || ""
      plate.lastUpdate = res.last_update || ""
      plate.overlayText = plate.confirmedPlate || plate.lastRawPlate || ""
      plate.overlayBox = this.normalizeBox(res.bounding_box || res.bbox || null)

      if (!plate.scanLocked) {
        plate.lockedSnapshot = ""
        plate.lockedPlateCrop = ""
        plate.lastLockedImageSessionId = 0
      }

      if (!incomingCameraEnabled && allowTurnOffReset) {
        this.stopPlateLoop(lane)
        this.hardResetPlate(plate)
        return
      }

      if (plate.scanLocked) {
        await this.fetchPlateLockedImages(lane, false)
      }
    },

    async previewLane(lane) {
  if (!lane.qr.cameraIp.trim() && !lane.plate.cameraIp.trim()) {
    alert("Vui lòng nhập ít nhất 1 URL camera")
    return
  }

  // 🔥 chống spam click
  if (lane.loading) return

  try {
    lane.loading = true
    

    // ===== QR =====
    if (lane.qr.viewUrl) {
      if (lane.qr.previewRunning) {
        // 🔥 STEP 1: tắt nhẹ
        this.resetPreview(lane.qr)

        // 🔥 STEP 2: chờ camera release
        await new Promise(r => setTimeout(r, 300))
      }

      // 🔥 STEP 3: mở lại
      this.mountPreview(lane.qr, lane.qr.viewUrl)
      lane.qr.message = "Đã tải lại preview QR"
      // quét đã hoàn tất → tắt flag quét
      lane.qr.scanRequested = false
    }

    // ===== PLATE =====
    if (lane.plate.viewUrl) {
      if (lane.plate.previewRunning) {
        this.resetPreview(lane.plate)

        // 🔥 delay cực quan trọng
        await new Promise(r => setTimeout(r, 300))
      }

      this.mountPreview(lane.plate, lane.plate.viewUrl)
      lane.plate.message = "Đã tải lại preview Plate"
    }

  } catch (e) {
    console.error("previewLane error:", e)
    alert(e?.message || "Lỗi mở preview")
  } finally {
    lane.loading = false
  }
},

    async readAllLane(lane) {
      if (!lane.qr.cameraIp.trim() || !lane.plate.cameraIp.trim()) {
        alert("Vui lòng nhập đủ URL QR và biển số")
        return
      }

      try {
        lane.loading = true
        await this.restartQrSession(lane, "Đang quét QR từ Python...")

        if (!lane.plate.cameraRunning) {
          this.releaseOtherPlateLanes(lane)
          this.stopPlateLoop(lane)
          const resPlate = await lane.plateApi.turnOnCamera(lane.plate.currentIp)
          if (!resPlate?.success) {
            alert(resPlate?.message || "Không thể khởi tạo trình nhận diện biển số")
            return
          }
          lane.plate.cameraRunning = true
          lane.plate.sessionId = Number(resPlate.session_id || 0)
          lane.plate.lastAppliedSessionId = lane.plate.sessionId
          lane.plate.message = resPlate.message || "Khởi tạo trình nhận diện biển số thành công"
        } else {
          this.releaseOtherPlateLanes(lane)
          const resPlate = await lane.plateApi.resetCameraState()
          lane.plate.message = resPlate?.message || "Da reset Plate"

          const newSessionId = Number(resPlate?.session_id || 0)
          if (newSessionId > 0) {
            lane.plate.sessionId = newSessionId
            lane.plate.lastAppliedSessionId = newSessionId
          }
        }

        await this.refreshPlate(lane)
        if (!lane.plate.resultTimer) this.startPlateLoop(lane)
      } catch (e) {
        console.error("readAllLane error:", e)
        alert(e?.message || "Loi doc ca 2")
      } finally {
        lane.loading = false
      }
    },

    async retryQr(lane) {
  if (!lane.qr.cameraIp.trim()) {
    alert("Vui lòng nhập URL QR")
    return
  }

  try {
    lane.loading = true
    await this.restartQrSession(lane, "Đang quét lại QR...")
  } catch (e) {
    console.error("retryQr error:", e)
    alert(e?.message || "Loi doc lai QR")
  } finally {
    lane.loading = false
  }
},

    async retryPlate(lane) {
      if (!lane.plate.cameraIp.trim()) {
        alert("Vui lòng nhập URL Plate")
        return
      }

      try {
        lane.loading = true

        
        

        this.clearPlateState(lane.plate)

        if (!lane.plate.cameraRunning) {
          this.releaseOtherPlateLanes(lane)
          this.stopPlateLoop(lane)
          const res = await lane.plateApi.turnOnCamera(lane.plate.currentIp)
          if (!res?.success) {
            alert(res?.message || "Không thể khởi tạo Plate")
            return
          }
          lane.plate.cameraRunning = true
          lane.plate.sessionId = Number(res.session_id || 0)
          lane.plate.lastAppliedSessionId = lane.plate.sessionId
          lane.plate.message = res.message || "Khởi tạo Plate thành công"
        } else {
          this.releaseOtherPlateLanes(lane)
          const res = await lane.plateApi.resetCameraState()
          lane.plate.message = res?.message || "Đã reset Plate"

          const newSessionId = Number(res?.session_id || 0)
          if (newSessionId > 0) {
            lane.plate.sessionId = newSessionId
            lane.plate.lastAppliedSessionId = newSessionId
          }
        }

        await this.refreshPlate(lane)
        if (!lane.plate.resultTimer) this.startPlateLoop(lane)
      } catch (e) {
        console.error("retryPlate error:", e)
        alert(e?.message || "Lỗi đọc lại biển số")
      } finally {
        lane.loading = false
      }
    },

    async stopLane(lane) {
  try {
    lane.loading = true

    // 🔥 1. tắt Python scan
    try {
      await this.stopLaneQrScanner(lane)
    } catch (e) {
      console.warn("stopQrScanner warning:", e)
    }

    // 🔥 2. dừng polling QR
    if (lane.qr.resultTimer) {
      clearInterval(lane.qr.resultTimer)
      lane.qr.resultTimer = null
    }

    // 🔥 3. reset QR frontend
    this.stopQrLoops(lane)
    this.hardResetQr(lane.qr)
    this.resetPreview(lane.qr)

    // ===== PLATE giữ nguyên =====
    this.stopPlateLoop(lane)

    try {
      const resPlate = await lane.plateApi.turnOffCamera()
      lane.plate.message = resPlate?.message || "Đã tắt Plate"
    } catch (e) {
      console.warn("turnOff plate warning:", e)
    }

    this.hardResetPlate(lane.plate)
    this.resetPreview(lane.plate)

    } catch (e) {
    console.error("stopLane error:", e)
    alert(e?.message || "Lỗi khi tắt")
  } finally {
    lane.loading = false
  }
},

        async confirmLane(lane) {
  const licensePlate = String(lane.plate.confirmedPlate || "").trim()
  const isGuest = !!lane.qr.guestId
  const employeeId = Number(lane.qr.employeeId || 0)
  const visitorDetailId = Number(lane.qr.guestId || 0)

  if (!licensePlate) {
    alert(`${lane.name}: chưa có biển số`)
    return
  }

  if (!isGuest && !employeeId) {
    alert(`${lane.name}: chưa có Employee ID`)
    return
  }

  if (isGuest && !visitorDetailId) {
    alert(`${lane.name}: chưa có VisitorDetailId`)
    return
  }

  try {
    lane.loading = true

    const payload = {
      LicensePlate: licensePlate,
      GateId: lane.gateId,
      LaneId: lane.laneId,
      Direction: lane.direction,
      CameraId: lane.cameraId,
      CredentialType: "QR",
      PlateSnapshotBase64: lane.plate.lockedSnapshot || null,
      PlateCropBase64: lane.plate.lockedPlateCrop || null,
      QrSnapshotBase64: lane.qr.lockedSnapshot || null
    }

    if (isGuest) {
  payload.VisitorDetailId = Number(lane.qr.guestId || 0)
  payload.QrPayload = lane.qr.qrPayload || lane.qr.activeSessionPayload || ""
} else {
      payload.EmployeeId = employeeId
    }

    const res = isGuest
      ? await scanGuest(payload)
      : await scanGate(payload)

    const data = res.data

    if (data?.success) {
      alert(`${lane.name}: ${data.message}`)
    } else {
      alert(`${lane.name}: ${data?.message || "Xử lý thất bại"}`)
    }
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.message ||
      "Không gọi được API Gate"

    alert(`${lane.name}: ${message}`)
  } finally {
    lane.loading = false
  }
},
    // ================= DECISION DRAWER ACTIONS =================

    openDecisionDrawer(lane) {
      this.decisionLaneId = lane.id
      this.decisionDrawerVisible = true
    },

    closeDecisionDrawer() {
      this.decisionDrawerVisible = false
      this.decisionLaneId = null
    },

    buildDecisionSubjectInfo(lane) {
      if (!lane) return { name: '', id: '', type: '', plate: '', qr: '' }
      const isGuest = !!lane.qr.guestId
      return {
        name: lane.qr.employeeName || lane.qr.employeeName || 'Khách',
        id: isGuest ? lane.qr.guestId : lane.qr.employeeId,
        type: isGuest ? 'GUEST' : 'EMPLOYEE',
        plate: lane.plate.confirmedPlate || lane.plate.lastRawPlate || '',
        qr: lane.qr.qrPayload || lane.qr.activeSessionPayload || '',
      }
    },

    canBaoVeAction(actionType) {
      // BaoVe permissions per the plan's role matrix
      const baoVeAllowed = ['allow', 'deny', 'manual', 'override', 'duress', 'escalate']
      return baoVeAllowed.includes(actionType)
    },

    canAdminAction(actionType) {
      // Admin can do everything
      return true
    },

    async fetchUserZones() {
      if (this.currentRole === 'Admin') return
      try {
        const res = await zoneAuthorityApi.getMyZones()
        this.userZoneIds = (res.data || []).map(z => z.securityZoneId)
      } catch {
        this.userZoneIds = []
      }
    },

    getActionPermissions(lane) {
      const role = this.currentRole
      const isReady = lane && ((!!lane.qr.employeeId || !!lane.qr.guestId) || !!lane.plate.confirmedPlate)
      const roleOk = role === 'Admin' || role === 'BaoVe'
      let zoneOk = true
      if (role === 'BaoVe' && lane && (lane.siteId || lane.securityZoneId)) {
        if (lane.securityZoneId) {
          zoneOk = this.userZoneIds.includes(lane.securityZoneId)
        }
      }
      const permissions = {
        allow: isReady && roleOk,
        deny: isReady && roleOk,
        manual: roleOk,
        unifiedEmergency: isReady && roleOk && zoneOk,
        escalate: isReady && roleOk,
      }
      return permissions
    },

    async handleDecisionAction(action) {
      if (!this.decisionLane) return
      const lane = this.decisionLane
      const { type, reason, responsibility, details } = action

      try {
        switch (type) {
          case 'allow':
            await this.executeAllow(lane, reason)
            break
          case 'deny':
            await this.executeDeny(lane, reason)
            break
          case 'manual':
            await this.executeManual(lane, reason, details)
            break
          case 'override':
            await this.executeOverride(lane, reason, responsibility)
            break
          case 'duress':
            await this.executeDuress(lane, reason, responsibility)
            break
          case 'escalate':
            await this.executeEscalate(lane, reason)
            break
          case 'unified_emergency':
            await this.executeUnifiedEmergency(lane, reason, details, responsibility, action._duress)
            break
          case 'emergency':
            await this.executeEmergency(lane, reason, details, responsibility)
            break
        }
      } catch (e) {
        this.showAuditToast('danger', 'Thất bại', e?.response?.data?.message || e?.message || 'Xử lý thất bại', '')
      } finally {
        this.closeDecisionDrawer()
      }
    },

    async executeAllow(lane, reason) {
      const licensePlate = String(lane.plate.confirmedPlate || '').trim()
      const isGuest = !!lane.qr.guestId
      const employeeId = Number(lane.qr.employeeId || 0)
      const visitorDetailId = Number(lane.qr.guestId || 0)

      if (!licensePlate) throw new Error('Chưa có biển số')
      if (!isGuest && !employeeId) throw new Error('Chưa có Employee ID')

      const payload = {
        LicensePlate: licensePlate,
        GateId: lane.gateId || null,
        CameraId: lane.cameraId || null,
      }
      if (isGuest) {
        payload.VisitorDetailId = visitorDetailId
        payload.QrPayload = lane.qr.qrPayload || lane.qr.activeSessionPayload || ''
      } else {
        payload.EmployeeId = employeeId
      }

      const res = isGuest ? await scanGuest(payload) : await scanGate(payload)

      // Record lane event
      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'ACCESS_GRANTED',
          direction: 'IN',
          plateText: licensePlate,
          note: reason || 'Cho qua bình thường',
        })
      } catch (e) {
        console.warn('recordLaneEvent failed:', e)
      }

      const receiptId = res.data?.receiptId || `RCP-${Date.now()}`
      this.showAuditToast('success', 'Cho qua thành công', `Xe ${licensePlate} đã được cho qua.`, receiptId)
    },

    async executeDeny(lane, reason) {
      // Record lane event as denied
      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'ACCESS_DENIED',
          direction: 'IN',
          plateText: lane.plate.confirmedPlate || '',
          note: reason || 'Từ chối',
        })
      } catch (e) {
        console.warn('recordLaneEvent failed:', e)
      }

      this.clearQrState(lane.qr)
      this.clearPlateState(lane.plate)
      this.showAuditToast('warning', 'Đã từ chối', `Từ chối: ${reason || 'Không có lý do'}.`, `RCP-${Date.now()}`)
    },

    async executeManual(lane, reason, details = {}) {
      const plate = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()
      const subjectName = String(details?.subjectName || lane.qr.employeeName || '').trim()
      const subjectId = String(details?.subjectId || lane.qr.employeeId || lane.qr.guestId || '').trim()
      if (!subjectName && !plate) throw new Error('Cần nhập họ tên/đơn vị hoặc biển số để vận hành thủ công.')

      const response = await enterpriseApi.recordLaneEvent({
        laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
        eventType: 'MANUAL_PASS',
        direction: 'IN',
        plateText: plate,
        note: `[manual] ${subjectName}; subjectId=${subjectId}; reason=${reason}`,
      })

      this.showAuditToast('success', 'Đã cho qua thủ công', `${subjectName || plate} đã được cho qua và ghi nhận trách nhiệm.`, `MAN-${response.data?.laneEventId || Date.now()}`)
    },

    async executeOverride(lane, reason, responsibility) {
      // Override with responsibility - requires step-up for risk mitigation
      const licensePlate = String(lane.plate.confirmedPlate || '').trim()
      const isGuest = !!lane.qr.guestId
      const employeeId = Number(lane.qr.employeeId || 0)

      if (!licensePlate) throw new Error('Chưa có biển số')

      const payload = {
        LicensePlate: licensePlate,
        GateId: lane.gateId || null,
        CameraId: lane.cameraId || null,
        Responsibility: true,
        Reason: reason || 'Override không có lý do',
      }
      if (isGuest) {
        payload.VisitorDetailId = Number(lane.qr.guestId || 0)
        payload.QrPayload = lane.qr.qrPayload || lane.qr.activeSessionPayload || ''
      } else {
        payload.EmployeeId = employeeId
      }

      const res = isGuest ? await scanGuest(payload) : await scanGate(payload)

      // Record lane event
      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'OVERRIDE',
          direction: 'IN',
          plateText: licensePlate,
          note: `Override - ${reason} - Responsibility: ${responsibility}`,
        })
      } catch (e) {
        console.warn('recordLaneEvent failed:', e)
      }

      const receiptId = res.data?.receiptId || `RCP-OVR-${Date.now()}`
      this.showAuditToast('warning', 'Override thành công', `Cho qua có trách nhiệm: ${licensePlate}.`, receiptId)
    },

    async executeDuress(lane, reason, responsibility) {
      // Record duress event
      try {
        const res = await enterpriseApi.recordDuressEvent({
          userId: authState.user?.userId || null,
          employeeId: authState.user?.employeeId || null,
          accessPointId: null,
          siteId: lane.siteId || null,
          securityZoneId: lane.securityZoneId || null,
          credentialType: 'DynamicQR',
          description: `${lane.name || 'Gate'}: ${reason || 'Ghi nhận duress'}`,
        })
        const receiptId = res.data?.duressEventId || `DRS-${Date.now()}`
        this.showAuditToast('danger', 'ĐÃ GHI NHẬN DURESS', 'Tín hiệu ép buộc đã được gửi đến trung tâm giám sát. Hỗ trợ sẽ được điều động.', receiptId)
      } catch (e) {
        console.error('recordDuressEvent failed:', e)
        // Fallback: still show alert with local receipt
        this.showAuditToast('danger', 'DURESS (Offline)', 'Không thể gửi tín hiệu đến server. Đã lưu local. Vui lòng báo ngay quản lý.', `DRS-LOCAL-${Date.now()}`)
      }
    },

    async executeEscalate(lane, reason) {
      // Create an intervention request via Phase G API
      try {
        const isGuest = !!lane.qr.guestId
        const res = await enterpriseApi.createInterventionRequest({
          interventionType: 'other',
          reason: reason || 'Yêu cầu can thiệp từ lane',
          siteId: lane.siteId || null,
          securityZoneId: lane.securityZoneId || null,
          laneId: String(lane.id || ''),
          laneName: String(lane.name || ''),
          subjectName: String(lane.qr.employeeName || 'Khách'),
          subjectId: String(isGuest ? lane.qr.guestId : lane.qr.employeeId || ''),
          subjectType: isGuest ? 'GUEST' : 'EMPLOYEE',
          plateNumber: String(lane.plate.confirmedPlate || lane.plate.lastRawPlate || ''),
          qrPayload: String(lane.qr.qrPayload || lane.qr.activeSessionPayload || ''),
          priority: 'medium',
          expiresInMinutes: 240,
        })

        // Also record lane event for audit trail
        try {
          await enterpriseApi.recordLaneEvent({
            laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
            eventType: 'ESCALATION_REQUEST',
            direction: 'IN',
            plateText: lane.plate.confirmedPlate || '',
            note: `Yêu cầu can thiệp #${res.data?.operationalInterventionRequestId}: ${reason || 'Không có lý do'}`,
          })
        } catch (e) {
          console.warn('recordLaneEvent failed:', e)
        }

        const requestId = res.data?.operationalInterventionRequestId || ''
        this.showAuditToast('info', 'Đã gửi yêu cầu can thiệp',
          `Yêu cầu #${requestId} đã được gửi đến Admin. Lý do: ${reason}`,
          `IR-${requestId || Date.now()}`)
      } catch (e) {
        console.error('createInterventionRequest failed:', e)
        this.showAuditToast('danger', 'Gửi yêu cầu thất bại',
          e?.response?.data?.message || e?.message || 'Không thể gửi yêu cầu can thiệp.',
          'ERR-' + Date.now())
      }
    },

    async executeUnifiedEmergency(lane, reason, details = {}, responsibility, isDuress = false) {
      if (!responsibility) throw new Error('Phải xác nhận trách nhiệm trước khi cấp quyền khẩn cấp.')
      const subjectName = String(details?.subjectName || lane.qr.employeeName || lane.qr.guestName || 'Đối tượng khẩn cấp').trim()
      const plateNumber = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()

      try {
        const response = await enterpriseApi.createEmergencyPass({
          subjectType: lane.qr.guestId ? 'Guest' : 'Person',
          subjectId: String(details?.subjectId || lane.qr.employeeId || lane.qr.guestId || ''),
          subjectName,
          plateNumber,
          siteId: lane.siteId || null,
          securityZoneId: lane.securityZoneId || null,
          laneReference: String(lane.id || ''),
          laneName: String(lane.name || ''),
          direction: 'Entry',
          reason,
          durationMinutes: 30,
        }, isDuress)

        const pass = response.data?.emergencyPass
        const receiptId = isDuress
          ? `DRS-${pass?.emergencyPassId || Date.now()}`
          : `EMG-${pass?.emergencyPassId || Date.now()}`

        this.showAuditToast('danger', 'Đã cấp quyền khẩn cấp',
          `${subjectName}${plateNumber ? ` - ${plateNumber}` : ''} được phép qua ngay.`,
          receiptId)
      } catch (e) {
        this.showAuditToast('danger', 'Cấp quyền thất bại',
          e?.response?.data?.message || e?.message || 'Không thể cấp quyền khẩn cấp.',
          'ERR-' + Date.now())
      }
    },

    async executeEmergency(lane, reason, details = {}, responsibility) {
      // Only Admin can execute emergency directly
      if (!this.isAdmin) {
        this.showAuditToast('warning', 'Không có quyền', 'Chỉ Admin mới có quyền cấp quyền khẩn cấp. Vui lòng gửi yêu cầu.', '')
        return
      }

      if (!responsibility) throw new Error('Phải xác nhận trách nhiệm trước khi cấp quyền khẩn cấp.')
      const subjectName = String(details?.subjectName || lane.qr.employeeName || 'Đối tượng khẩn cấp').trim()
      const plateNumber = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()
      const response = await enterpriseApi.createEmergencyPass({
        subjectType: lane.qr.guestId ? 'Guest' : 'EmergencyService',
        subjectId: String(details?.subjectId || lane.qr.employeeId || lane.qr.guestId || ''),
        subjectName,
        plateNumber,
        siteId: lane.siteId || null,
        securityZoneId: lane.securityZoneId || null,
        laneReference: String(lane.id || ''),
        laneName: String(lane.name || ''),
        direction: 'Entry',
        reason,
        durationMinutes: 30,
      })
      const pass = response.data?.emergencyPass
      this.showAuditToast('danger', 'Đã cấp thông hành khẩn cấp', `${subjectName}${plateNumber ? ` - ${plateNumber}` : ''} được phép qua ngay. Cảnh báo toàn công ty đã phát.`, `EMG-${pass?.emergencyPassId || Date.now()}`)
    },

    async onStepUpConfirmed(result) {
      const lane = this.lanes.find((l) => l.id === this.stepUpLaneId)
      if (!lane || !this.stepUpAction) return

      try {
        const res = await enterpriseApi.createEmergencyState({
          reason: this.stepUpAction.reason,
          stepUpSessionId: result.sessionId,
        })
        const receiptId = res.data?.emergencyStateId || `EMG-${Date.now()}`
        this.showAuditToast('danger', 'Trạng thái khẩn cấp đã kích hoạt', 'Quyền khẩn cấp đã được cấp. Tất cả các quy tắc đã được ghi đè.', receiptId)
      } catch (e) {
        this.showAuditToast('danger', 'Thất bại', e?.response?.data?.message || e?.message || 'Không thể kích hoạt trạng thái khẩn cấp.', '')
      } finally {
        this.stepUpVisible = false
        this.stepUpAction = null
        this.stepUpLaneId = null
      }
    },

    onStepUpCancelled() {
      this.stepUpVisible = false
      this.stepUpAction = null
      this.stepUpLaneId = null
    },

    showAuditToast(type, title, message, receiptId) {
      this.auditToast = {
        visible: true,
        type,
        title,
        message,
        receiptId,
        timestamp: new Date().toLocaleString('vi-VN'),
      }
    },

    dismissAuditToast() {
      this.auditToast.visible = false
    },

    // ================= CAMERA SEARCH =================

async loadCameraList() {
  try {
    const res = await getCameras()
    this.cameras = res || []
  } catch (e) {
    console.error("loadCameraList error:", e)
  }
},

filterCameras(keyword) {
  if (!keyword) return this.cameras

  const key = keyword.toLowerCase()

  return this.cameras.filter(c =>
    String(c.cameraName || "").toLowerCase().includes(key) ||
    String(c.cameraId).includes(key)
  )
},

selectCamera(cam, lane, type) {
  if (!cam.urlView) {
    alert("Camera chưa có UrlView. Hãy reload go2rtc trước")
    return
  }

  if (type === "qr") {
    const qrStreamValue = this.preferMainQrStream(cam.streamUrl)
    lane.qr.cameraIp = qrStreamValue
    lane.qr.viewUrl = cam.urlView   // 🔥 thêm
    lane.qr.currentIp = qrStreamValue
    lane.cameraId = cam.cameraId

    this.cameraSearch[lane.id + '-qr'] = cam.cameraName
    this.mountPreview(lane.qr, cam.urlView)
  }

  if (type === "plate") {
    const streamValue = String(cam.streamUrl || "").trim()
    const viewValue = String(cam.urlView || "").trim()
    lane.plate.cameraIp = streamValue || viewValue
    lane.plate.viewUrl = cam.urlView
    lane.plate.currentIp = streamValue || viewValue
    lane.cameraId = cam.cameraId

    this.cameraSearch[lane.id + '-plate'] = cam.cameraName
    this.mountPreview(lane.plate, cam.urlView)
  }
}
    
  }
}
</script>

<style scoped>
* {
  box-sizing: border-box;
}

.page {
  height: calc(100dvh - var(--header-height, 76px) - 18px);
  min-height: 620px;
  background: transparent;
  padding: 10px 18px 12px;
  font-family: "IBM Plex Sans", "Segoe UI", sans-serif;
  color: #0f172a;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  position: relative;
}

.topbar {
  margin-bottom: 14px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.topbar-main {
  min-width: 0;
}

.topbar-eyebrow {
  display: inline-flex;
  align-items: center;
  min-height: 26px;
  margin-bottom: 5px;
  padding: 0 10px;
  border-radius: 999px;
  background: rgba(15, 130, 144, 0.1);
  color: #0f8290;
  font-size: 11px;
  font-weight: 900;
  letter-spacing: 0.08em;
}

.topbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.topbar-settings-btn {
  flex-shrink: 0;
  height: 36px;
  padding: 0 14px;
  border-radius: 10px;
  border: 1px solid #0f8290;
  background: #0f8290;
  font-size: 13px;
  font-weight: 800;
  color: #f8fafc;
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.12);
}

.topbar-settings-btn:hover {
  background: #0c6f7a;
}

.topbar-toggle {
  flex-shrink: 0;
  height: 36px;
  padding: 0 12px;
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  background: #fff;
  font-size: 13px;
  font-weight: 700;
  color: #334155;
  cursor: pointer;
}

.topbar-toggle:hover {
  border-color: #94a3b8;
  background: #f8fafc;
}

.topbar.compact {
  margin-bottom: 12px;
}

.topbar.compact h1 {
  font-size: 28px;
}

.topbar-desc {
  margin: 5px 0 0;
  color: #52677c;
  font-size: 13px;
}

.topbar h1 {
  margin: 0;
  font-size: 30px;
  line-height: 1.08;
  font-weight: 900;
  letter-spacing: -0.035em;
}

.gate-layout {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-height: 0;
  flex: 1;
  padding-bottom: calc(82px + env(safe-area-inset-bottom, 0px));
  position: relative;
  z-index: 3;
}

.cam-wall {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  grid-template-rows: repeat(2, minmax(0, 1fr));
  gap: 12px;
  position: relative;
  z-index: 4;
}

.page::before,
.page::after,
.gate-layout::before,
.gate-layout::after,
.cam-wall::before,
.cam-wall::after {
  content: none !important;
  display: none !important;
}

.cam-cell {
  min-height: 0;
  min-width: 0;
  display: flex;
  position: relative;
  z-index: 5;
}

.ops-dock {
  position: absolute;
  left: 18px;
  right: 18px;
  bottom: 10px;
  z-index: 55;
  padding: 10px 16px calc(10px + env(safe-area-inset-bottom, 0px));
  background: rgba(255, 255, 255, 0.96);
  border: 1px solid rgba(203, 213, 225, 0.85);
  border-radius: 18px;
  box-shadow: 0 14px 40px rgba(15, 23, 42, 0.13);
  backdrop-filter: blur(14px);
  max-height: 92px;
  overflow: hidden;
}

.ops-dock-grid {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  max-width: 1600px;
  margin: 0 auto;
}

.ops-dock-center {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  align-items: flex-start;
  gap: 12px 32px;
  min-width: 0;
}

.lane-action-group {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  min-width: 0;
}

.lane-action-title {
  font-size: 11px;
  font-weight: 900;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #64748b;
}

.lane-action-btns {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
  max-width: min(560px, 44vw);
}

.ops-dock-side {
  justify-self: end;
  align-self: center;
  min-width: 0;
  position: relative;
  z-index: 4;
  pointer-events: auto;
}

.btn-open-drawer {
  min-height: 34px;
  padding: 0 12px;
  border-radius: 10px;
  border: 1px solid #64748b;
  background: #1e293b;
  color: #f8fafc;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
  position: relative;
  z-index: 5;
  pointer-events: auto;
}

.btn-open-drawer:hover {
  background: #0f172a;
}

.btn-dock {
  min-height: 36px;
  height: auto;
  padding: 0 14px;
  font-size: 12px;
  font-weight: 800;
  border-radius: 10px;
}

.ops-drawer-root {
  position: fixed;
  inset: 0;
  z-index: 60;
}

.ops-drawer-root[aria-hidden="true"] {
  display: none !important;
}

.ops-drawer-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  pointer-events: auto;
}

.ops-drawer-panel {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  width: min(440px, 92vw);
  max-width: 100%;
  background: #ffffff;
  border-left: 1px solid #e2e8f0;
  box-shadow: -16px 0 40px rgba(15, 23, 42, 0.12);
  display: flex;
  flex-direction: column;
  min-height: 0;
  pointer-events: auto;
}

.ops-drawer-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 18px;
  border-bottom: 1px solid #e2e8f0;
  flex-shrink: 0;
}

.ops-drawer-title {
  margin: 0;
  font-size: 20px;
  font-weight: 900;
  color: #0f172a;
}

.ops-drawer-close {
  width: 44px;
  height: 44px;
  border: none;
  border-radius: 10px;
  background: #f1f5f9;
  color: #0f172a;
  font-size: 26px;
  line-height: 1;
  cursor: pointer;
}

.ops-drawer-close:hover {
  background: #e2e8f0;
}

.ops-drawer-tabs {
  display: flex;
  gap: 8px;
  padding: 12px 18px 0;
  flex-shrink: 0;
}

.ops-drawer-tab {
  flex: 1;
  min-height: 44px;
  border-radius: 10px;
  border: 2px solid #e2e8f0;
  background: #fff;
  font-size: 15px;
  font-weight: 800;
  color: #475569;
  cursor: pointer;
}

.ops-drawer-tab.active {
  border-color: #2563eb;
  background: #eff6ff;
  color: #1d4ed8;
}

.ops-drawer-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 14px 18px 22px;
}

.lane-controls--drawer {
  display: flex;
  flex-direction: column;
  border: none;
  box-shadow: none;
  padding: 0;
  gap: 12px;
}

.lane-controls--drawer.ready {
  padding: 12px;
  border-radius: 14px;
  border: 2px solid #93c5fd;
  background: #f8fbff;
  box-shadow: 0 8px 24px rgba(37, 99, 235, 0.1);
}

.lane-controls {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 0;
}

.lane-controls--drawer .lane-head h2 {
  font-size: 20px;
}

.lane-controls--drawer .lane-final-status {
  font-size: 13px;
  min-width: 160px;
  padding: 10px 12px;
}

.lane-controls--drawer .ip-box label {
  font-size: 13px;
}

.lane-controls--drawer .ip-box input {
  height: 44px;
  font-size: 15px;
}

.lane-controls--drawer .summary-item .label {
  font-size: 12px;
}

.lane-controls--drawer .summary-item .value {
  font-size: 15px;
}

.lane-head {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  margin-bottom: 0;
}

.lane-head h2 {
  margin: 0;
  font-size: 22px;
  font-weight: 800;
}

.lane-head p {
  margin: 4px 0 0;
  color: #64748b;
  font-size: 13px;
}

.lane-final-status {
  min-width: 180px;
  text-align: center;
  padding: 10px 14px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 900;
}

.lane-final-status.ok {
  background: #dcfce7;
  color: #166534;
}

.lane-final-status.wait {
  background: #fff7ed;
  color: #c2410c;
}

.lane-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 0;
}

.btn {
  height: 30px;
  border: none;
  border-radius: 10px;
  padding: 0 14px;
  color: white;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-preview {
  background: #0f766e;
}

.btn-main {
  background: #0f8290;
}

.btn-sub {
  background: #475569;
}

.btn-off {
  background: #dc2626;
}

.btn-confirm {
  background: #111827;
}

.btn-decision {
  background: #b86d23;
}

.btn-decision:hover:not(:disabled) {
  background: #98581b;
}

.ip-row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 0;
}

.ip-box label {
  display: block;
  font-size: 12px;
  font-weight: 700;
  margin-bottom: 6px;
  color: #334155;
}

.ip-box input {
  width: 100%;
  height: 36px;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 0 12px;
  font-size: 14px;
  outline: none;
}

.ip-box input:focus {
  border-color: #60a5fa;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.08);
}

.summary-bar {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 0;
}

.summary-item {
  background: #f8fafc;
  border: 1px solid #e9eef5;
  border-radius: 10px;
  padding: 6px 8px;
}

.summary-item .label {
  display: block;
  font-size: 11px;
  color: #64748b;
  margin-bottom: 6px;
}

.summary-item .value {
  display: block;
  font-size: 14px;
  font-weight: 800;
  word-break: break-word;
}

.strong {
  font-size: 16px !important;
  font-weight: 900 !important;
}

.plate {
  color: #15803d;
  letter-spacing: 1px;
}

.ok-text {
  color: #15803d;
}

.warn-text {
  color: #c2410c;
}

.danger-text {
  color: #b91c1c;
}

.cam-block {
  border: 1px solid rgba(203, 213, 225, 0.8);
  border-radius: 18px;
  padding: 10px;
  background: rgba(255, 255, 255, 0.96);
  min-height: 0;
  display: flex;
  flex-direction: column;
  flex: 1;
  width: 100%;
  position: relative;
  z-index: 6;
}

.cam-block--hero {
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.08);
}

.cam-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 12px;
  font-weight: 800;
  flex-shrink: 0;
}

.cam-head-titles {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.cam-lane-tag {
  display: inline-block;
  font-size: 11px;
  font-weight: 900;
  letter-spacing: 0.02em;
  color: #0f8290;
  text-transform: uppercase;
}

.cam-kind {
  font-size: 14px;
  font-weight: 900;
  color: #0f172a;
}

.mini-status {
  padding: 5px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 900;
}

.mini-status.ok {
  background: #dcfce7;
  color: #166534;
}

.mini-status.wait {
  background: #fff7ed;
  color: #c2410c;
}

.cam-preview {
  width: 100%;
  flex: 1;
  min-height: 0;
  background: #0f172a;
  border-radius: 13px;
  overflow: hidden;
  margin-bottom: 7px;
  position: relative;
  border: 1px solid #cbd5e1;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.preview-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  min-height: 0;
}

.cam-off {
  width: 100%;
  height: 100%;
  display: flex;
  color: #94a3b8;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 10px;
  font-size: 13px;
  font-weight: 800;
}

.cam-off-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #64748b;
  box-shadow: 0 0 0 6px rgba(100, 116, 139, 0.14);
}

.quick-result {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  flex-wrap: wrap;
  flex-shrink: 0;
}

.result-pill {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 900;
}

.result-hint {
  font-size: 12px;
  line-height: 1.35;
  font-weight: 700;
}

.result-hint--seen {
  color: #9a3412;
}

.result-hint--waiting {
  color: #475569;
}

.cam-overlay {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.cam-preview-toolbar {
  position: absolute;
  top: 8px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 5;
  display: flex;
  gap: 6px;
  pointer-events: auto;
}

.cam-refresh-btn {
  width: 32px;
  height: 32px;
  padding: 0;
  border-radius: 12px;
  border: 1px solid rgba(248, 250, 252, 0.35);
  background: rgba(15, 23, 42, 0.72);
  color: #f8fafc;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  backdrop-filter: blur(8px);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.35);
}

.cam-refresh-btn:hover:not(:disabled) {
  background: rgba(30, 41, 59, 0.88);
  border-color: rgba(248, 250, 252, 0.5);
}

.cam-refresh-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.cam-refresh-icon {
  display: block;
}

.cam-refresh-icon--rerun {
  color: #fde68a;
}

.cam-refresh-icon--spin {
  animation: cam-toolbar-spin 0.85s linear infinite;
}

@keyframes cam-toolbar-spin {
  to {
    transform: rotate(360deg);
  }
}

.drawer-settings-panel {
  margin: 0 0 16px;
  padding: 0 0 16px;
  border-bottom: 1px solid #e2e8f0;
}

.drawer-settings-title {
  margin: 0 0 8px;
  font-size: 15px;
  font-weight: 900;
  color: #0f172a;
}

.drawer-settings-meta {
  margin: 0 0 8px;
  font-size: 11px;
  color: #64748b;
  word-break: break-all;
}

.drawer-settings-meta-sep {
  margin: 0 6px;
  color: #94a3b8;
}

.drawer-settings-hint {
  margin: 0 0 14px;
  font-size: 12px;
  line-height: 1.45;
  color: #475569;
}

.settings-toggle-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid #f1f5f9;
}

.settings-toggle-row:last-of-type {
  border-bottom: none;
}

.settings-toggle-text {
  min-width: 0;
  flex: 1;
}

.settings-toggle-name {
  display: block;
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
}

.settings-toggle-desc {
  display: block;
  margin-top: 2px;
  font-size: 11px;
  color: #64748b;
}

.toggle-switch {
  position: relative;
  flex-shrink: 0;
  width: 50px;
  height: 28px;
  border-radius: 999px;
  border: 2px solid #cbd5e1;
  background: #e2e8f0;
  cursor: pointer;
  padding: 0;
  transition: background 0.2s ease, border-color 0.2s ease;
}

.toggle-switch.on {
  background: #22c55e;
  border-color: #16a34a;
}

.toggle-switch.pending {
  background: #facc15;
  border-color: #eab308;
}

.toggle-switch:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.toggle-switch-knob {
  position: absolute;
  top: 2px;
  left: 2px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 1px 4px rgba(15, 23, 42, 0.2);
  transition: transform 0.2s ease;
}

.toggle-switch.on .toggle-switch-knob {
  transform: translateX(22px);
}

.toggle-switch.pending .toggle-switch-knob {
  background: #fef08a;
}

.auto-start-btn {
  min-height: 30px;
  padding: 0 10px;
  border-radius: 999px;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  color: #334155;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}

.auto-start-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.drawer-secondary-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 4px;
}

.btn-drawer-secondary {
  min-height: 42px;
  height: auto;
  border-radius: 10px;
  padding: 0 16px;
  font-size: 13px;
  font-weight: 800;
  color: #fff;
  border: none;
  cursor: pointer;
}

.btn-drawer-secondary:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.bbox-box {
  position: absolute;
  border: 2px solid #22c55e;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.3);
}

.overlay-tag {
  position: absolute;
  max-width: 88%;
  background: rgba(15, 23, 42, 0.8);
  color: #f8fafc;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 7px;
  border-radius: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cam-preview.state-idle {
  border-color: #94a3b8;
}

.cam-preview.state-scanning {
  border-color: #eab308;
  box-shadow: 0 0 0 2px rgba(234, 179, 8, 0.28);
}

.cam-preview.state-valid {
  border-color: #22c55e;
  box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.24);
}

.cam-preview.state-invalid {
  border-color: #ef4444;
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.2);
}

.result-pill.state-idle {
  background: #e2e8f0;
  color: #334155;
}

.result-pill.state-scanning {
  background: #fef3c7;
  color: #92400e;
}

.result-pill.state-valid {
  background: #dcfce7;
  color: #166534;
}

.result-pill.state-invalid {
  background: #fee2e2;
  color: #991b1b;
}

@media (max-width: 1200px) {
  .ops-drawer-panel {
    width: min(400px, 96vw);
  }
}

@media (max-width: 900px) {
  .page {
    height: auto;
    min-height: calc(100dvh - var(--header-height, 76px));
    padding: 10px 12px 16px;
    overflow: visible;
  }

  .gate-layout {
    padding-bottom: 16px;
  }

  .cam-wall {
    grid-template-columns: 1fr;
    grid-template-rows: none;
  }

  .cam-cell {
    min-height: 300px;
  }

  .topbar {
    flex-direction: column;
    align-items: stretch;
  }

  .topbar-actions {
    width: 100%;
    justify-content: space-between;
  }

  .topbar-settings-btn,
  .topbar-toggle {
    flex: 1 1 0;
  }

  .ops-dock-grid {
    display: block;
    justify-items: stretch;
  }

  .ops-dock-spacer {
    display: none;
  }

  .ops-dock-center {
    order: 1;
    width: 100%;
  }

  .ops-dock {
    position: sticky;
    left: auto;
    right: auto;
    bottom: 8px;
    max-height: none;
    margin-top: 8px;
  }

  .ops-dock-side {
    order: 0;
    justify-self: stretch;
    width: 100%;
  }

  .btn-open-drawer {
    width: 100%;
  }

  .lane-action-btns {
    max-width: 100%;
  }

  .ops-drawer-panel {
    width: 100%;
    border-left: none;
  }

  .summary-bar,
  .ip-row {
    grid-template-columns: 1fr;
  }

  .lane-head {
    flex-direction: column;
    align-items: flex-start;
  }

  .lane-final-status {
    min-width: unset;
    width: 100%;
  }
}
.search-box {
  position: relative;
}

.dropdown {
  position: absolute;
  background: white;
  border: 1px solid #ccc;
  width: 100%;
  max-height: 200px;
  overflow-y: auto;
  z-index: 9999;
}

.dropdown-item {
  padding: 8px;
  cursor: pointer;
}

.dropdown-item:hover {
  background: #eee;
}
</style>
