<template>
  <div class="page">
    <div class="topbar" :class="{ compact: topbarCompact }">
      <div class="topbar-main">
        <span class="topbar-eyebrow">GIÁM SÁT VẬN HÀNH</span>
        <h1>Điều phối thông hành khuôn mặt</h1>
        <p v-show="!topbarCompact" class="topbar-desc">
          Theo dõi camera khuôn mặt, biển số và xử lý quyết định thông hành theo từng làn.
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
          <!-- FACE CAMERA CELL -->
          <div class="cam-cell">
            <div class="cam-block cam-block--hero">
              <div class="cam-head">
                <div class="cam-head-titles">
                  <span class="cam-lane-tag">{{ lane.name }}</span>
                  <span class="cam-kind">Camera Face</span>
                </div>
                <span class="mini-status" :class="lane.face.previewHealthy ? 'ok' : 'wait'">
                  {{
                    !lane.face.previewRunning
                      ? "Đang tắt"
                      : lane.face.lockedSnapshot
                        ? "Ảnh đã chụp"
                        : (lane.face.previewHealthy ? "Đang trực tuyến" : "Đang kết nối")
                  }}
                </span>
              </div>

              <div class="cam-preview" :class="`state-${cameraVisualState('face', lane)}${autoCamClass(lane)}`">
                <img
                  v-if="lane.face.previewRunning && lane.face.directCameraUrl && isImageUrl(lane.face.directCameraUrl)"
                  :key="lane.face.directCameraKey + '-img'"
                  :src="lane.face.directCameraUrl"
                  class="preview-image"
                  alt="Camera Face"
                  @load="onPreviewLoaded(lane.face)"
                  @error="onPreviewError(lane.face)"
                />
                <iframe
                  v-else-if="lane.face.previewRunning && lane.face.directCameraUrl"
                  :key="lane.face.directCameraKey + '-iframe'"
                  :src="lane.face.directCameraUrl"
                  class="preview-image"
                  style="border: none;"
                ></iframe>
                <div v-else class="cam-off">
                  <span class="cam-off-dot"></span>
                  Camera Face đang tắt
                </div>
                <div class="cam-overlay">
                  <div
                    v-if="lane.face.overlayBox"
                    class="bbox-box"
                    :style="boundingStyle(lane.face.overlayBox)"
                  ></div>
                  <div
                    v-if="lane.face.overlayText"
                    class="overlay-tag"
                    :style="labelStyle(lane.face.overlayBox)"
                  >
                    Khuôn mặt: {{ shortText(lane.face.overlayText, 72) }}
                  </div>
                </div>
                <div class="cam-preview-toolbar">
                  <button
                    type="button"
                    class="cam-refresh-btn"
                    :disabled="autoActive || lane.loading || !lane.face.cameraIp.trim()"
                    :aria-label="
                      lane.loading ? 'Đang xử lý' : lane.face.cameraRunning ? 'Đọc lại Face' : 'Đọc Face'
                    "
                    @click.stop="retryFace(lane)"
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
                      :class="{ 'cam-refresh-icon--rerun': lane.face.cameraRunning }"
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
                <div class="result-pill" :class="`state-${cameraVisualState('face', lane)}`">
                  {{ cameraVisualText("face", lane) }}
                </div>
                <div
                  v-if="lane.face.scanLocked && lane.face.identityConfirmed && lane.face.employeeId"
                  class="result-hint result-hint--ok"
                  style="color: #10b981; font-weight: 600;"
                >
                  ✓ Đã nhận diện: {{ lane.face.employeeName || ('NV ' + lane.face.employeeId) }}
                </div>
                <div
                  v-else-if="lane.face.alert"
                  class="result-hint result-hint--seen"
                  style="color: #ef4444; font-weight: 600;"
                >
                  ⚠ CẢNH BÁO: Người lạ / Không hợp lệ
                </div>
                <div
                  v-else-if="lane.face.timeoutState"
                  class="result-hint result-hint--seen"
                >
                  Hết thời gian nhận diện khuôn mặt.
                </div>
                <div
                  v-else-if="lane.face.faceMatch"
                  class="result-hint result-hint--seen"
                >
                  Đang so khớp khuôn mặt...
                </div>
                <div
                  v-else-if="lane.face.trackingActive"
                  class="result-hint result-hint--waiting"
                >
                  Đang theo dõi khuôn mặt...
                </div>
                <div
                  v-else-if="lane.face.cameraRunning"
                  class="result-hint result-hint--waiting"
                >
                  Đang quét khuôn mặt...
                </div>
              </div>
              <div v-if="lane.auto.error" class="auto-error-banner">{{ lane.auto.error }}</div>
            </div>
          </div>

          <!-- PLATE CAMERA CELL -->
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

              <div class="cam-preview" :class="`state-${cameraVisualState('plate', lane)}${autoCamClass(lane)}`">
                <img
                  v-if="lane.plate.previewRunning && lane.plate.directCameraUrl && isImageUrl(lane.plate.directCameraUrl)"
                  :key="lane.plate.directCameraKey + '-img'"
                  :src="lane.plate.directCameraUrl"
                  class="preview-image"
                  alt="Camera biển số"
                  @load="onPreviewLoaded(lane.plate)"
                  @error="onPreviewError(lane.plate)"
                />
                <iframe
                  v-else-if="lane.plate.previewRunning && lane.plate.directCameraUrl"
                  :key="lane.plate.directCameraKey + '-iframe'"
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
                    :disabled="autoActive || lane.loading || !lane.plate.cameraIp.trim()"
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
              <div v-if="lane.auto.error" class="auto-error-banner">{{ lane.auto.error }}</div>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- OPS DOCK -->
    <div class="ops-dock" role="toolbar" aria-label="Chế độ tự động">
      <div class="ops-dock-auto">
        <div class="ops-dock-auto-main">
          <button
            type="button"
            class="btn btn-auto"
            :class="{ running: autoActive }"
            :disabled="autoStarting"
            @click="toggleAutoMonitor"
          >
            <span v-if="autoStarting" class="btn-auto-spinner" aria-hidden="true"></span>
            {{ autoStarting ? "Đang khởi động..." : (autoActive ? "Dừng" : "Bắt đầu") }}
          </button>
          <span class="auto-status-text">
            {{
              autoActive
                ? "Đang tự động quét liên tục: Khuôn mặt + biển số, quyết định và lưu nhật ký 1 lần mỗi phiên."
                : "Nhấn Bắt đầu để chạy nhận diện tự động. Preview camera vẫn hoạt động khi Dừng."
            }}
          </span>
        </div>
        <div class="ops-dock-auto-lanes">
          <span
            v-for="lane in lanes"
            :key="lane.id + '-auto-state'"
            class="auto-lane-chip"
            :class="autoChipClass(lane)"
          >
            {{ lane.name }}: {{ autoStatusText(lane) }}
          </span>
        </div>
      </div>
    </div>

    <!-- OPS DRAWER (SETTINGS) -->
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

            <div class="lane-direction-setting">
              <div>
                <label :for="`lane-direction-${activeOpsLane.id}`">Hướng thông hành</label>
                <p>Được lưu cho riêng {{ activeOpsLane.name }}. Hai làn có thể cùng IN hoặc cùng OUT.</p>
              </div>
              <select
                :id="`lane-direction-${activeOpsLane.id}`"
                v-model="activeOpsLane.direction"
                :disabled="activeOpsLane.directionSaving || autoActive"
                @change="saveLaneDirection(activeOpsLane)"
              >
                <option value="IN">IN · Làn vào</option>
                <option value="OUT">OUT · Làn ra</option>
              </select>
              <button
                type="button"
                class="btn btn-drawer-secondary"
                :disabled="activeOpsLane.directionSaving || autoActive"
                @click="saveLaneDirection(activeOpsLane)"
              >
                {{ activeOpsLane.directionSaving ? 'Đang lưu...' : 'Lưu hướng' }}
              </button>
            </div>
            <p v-if="activeOpsLane.directionMessage" class="lane-direction-message">{{ activeOpsLane.directionMessage }}</p>

            <div class="ip-row">
              <div class="ip-box">
                <label>URL Camera Face</label>
                <div class="search-box">
                  <input
                    v-model="cameraSearch[activeOpsLane.id + '-face']"
                    placeholder="Tìm camera Face..."
                    :disabled="activeOpsLane.loading"
                    @focus="faceDropdownOpen[activeOpsLane.id] = true"
                  />

                  <div class="dropdown" v-if="faceDropdownOpen[activeOpsLane.id] && filterCameras(cameraSearch[activeOpsLane.id + '-face']).length">
                    <div
                      v-for="cam in filterCameras(cameraSearch[activeOpsLane.id + '-face'])"
                      :key="cam.cameraId"
                      @click="selectCamera(cam, activeOpsLane, 'face')"
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
                    @focus="plateDropdownOpen[activeOpsLane.id] = true"
                  />

                  <div class="dropdown" v-if="plateDropdownOpen[activeOpsLane.id] && filterCameras(cameraSearch[activeOpsLane.id + '-plate']).length">
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
                <span class="label">Nhân viên</span>
                <span class="value strong">{{ activeOpsLane.face.employeeName || activeOpsLane.face.employeeId || "-----" }}</span>
              </div>

              <div class="summary-item">
                <span class="label">Khuôn mặt</span>
                <span class="value" :class="faceStateClass(activeOpsLane.face)">
                  {{ faceStateText(activeOpsLane.face) }}
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
                {{ activeOpsLane.loading ? "Đang xử lý..." : "Mở Preview" }}
              </button>
              <button
                type="button"
                class="btn btn-drawer-secondary btn-doc-face"
                :disabled="activeOpsLane.loading || !activeOpsLane.face.cameraIp.trim()"
                @click="retryFace(activeOpsLane)"
              >
                Đọc Face
              </button>
              <button
                type="button"
                class="btn btn-drawer-secondary btn-doc-plate"
                :disabled="activeOpsLane.loading || !activeOpsLane.plate.cameraIp.trim()"
                @click="retryPlate(activeOpsLane)"
              >
                Đọc biển số
              </button>
              <button
                type="button"
                class="btn btn-drawer-secondary btn-doc-all"
                :disabled="activeOpsLane.loading || !activeOpsLane.face.cameraIp.trim() || !activeOpsLane.plate.cameraIp.trim()"
                @click="readAllLane(activeOpsLane)"
              >
                Đọc cả 2 (Face + Biển số)
              </button>
              <button
                type="button"
                class="btn btn-drawer-secondary btn-off"
                :disabled="activeOpsLane.loading"
                @click="stopLane(activeOpsLane)"
              >
                {{ activeOpsLane.loading ? "Đang xử lý..." : "Tắt làn" }}
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
    :subject-name="(decisionLane ? (decisionLane.face.employeeName || (decisionLane.face.employeeId ? 'NV ' + decisionLane.face.employeeId : 'Nhân viên')) : '')"
    :subject-id="(decisionLane ? (decisionLane.face.employeeId || '') : '')"
    :subject-type="'EMPLOYEE'"
    :plate-number="(decisionLane ? (decisionLane.plate.confirmedPlate || decisionLane.plate.lastRawPlate || '') : '')"
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

  <!-- Simulation Harness (only with ?simulate=1 or vshield_sim=1) -->
  <div v-if="simEnable" class="sim-panel">
    <div class="sim-panel-head" @click="simState.expanded = !simState.expanded">
      <span class="sim-badge">SIM</span>
      <span>Mô phỏng auto-monitor (Face)</span>
      <span class="sim-caret">{{ simState.expanded ? '▼' : '▲' }}</span>
    </div>
    <div v-if="simState.expanded" class="sim-panel-body">
      <div class="sim-row">
        <label>Làn mục tiêu</label>
        <select :value="simState.targetLane" @change="simSetTargetLane($event.target.value)">
          <option value="lane1">Làn 1 (IN)</option>
          <option value="lane2">Làn 2 (OUT)</option>
        </select>
        <button @click="simSyncLaneConfig" title="Tự mapping cổng/làn thật từ backend vào 2 làn">Đồng bộ cổng/làn</button>
        <span v-if="simState.laneSynced" class="sim-ok">đã map</span>
      </div>
      <div class="sim-row">
        <label>Kịch bản</label>
        <button @click="simRunScenario(simState.targetLane, 'allow')">ALLOW (plate mới)</button>
        <button class="sim-btn-danger" @click="simRunScenario(simState.targetLane, 'deny')">DENY (plate ngoài)</button>
      </div>
      <div class="sim-row">
        <label>Face nhân viên</label>
        <input v-model="simState.injectEmpId" placeholder="Mã NV: 7" />
        <button @click="simSetFace(simState.targetLane, simState.injectEmpId)">Gắn Face</button>
        <button @click="simClearFace(simState.targetLane)">Bỏ Face</button>
      </div>
      <div class="sim-row">
        <label>Plate gắn/bỏ</label>
        <input v-model="simState.injectPlate" placeholder="59K-12345" />
        <button @click="simSetPlate(simState.targetLane, simState.injectPlate)">Gắn</button>
        <button @click="simClearPlate(simState.targetLane)">Bỏ</button>
        <button @click="simMakeAllowPlate" title="Sinh plate mới ngẫu nhiên">Random mới</button>
      </div>
      <div class="sim-row">
        <button @click="simResetAll">Reset giả lập</button>
        <button @click="simState.logText = ''">Xoá log</button>
      </div>
      <pre class="sim-log">{{ simState.logText }}</pre>
    </div>
  </div>
</template>

<script>
import * as faceApi from "../services/faceApi"
import * as plateLane1Api from "../services/plateCameraApi"
import { createPlateCameraApi } from "../services/plateCameraApi"
import axios from "axios"
import { scanGate, getTransitLanes, updateTransitLaneDirection } from "../services/gateTransitApi"
import { getCameras, getPythonProcessStatus } from "../services/cameraRuntimeApi"
import { getRuntimeServices, updateRuntimeService, startRuntimeService, stopRuntimeService } from "../services/runtimeServiceApi"
import { PLATE_API_BASE_URL, PLATE_API_BASE_URL_LANE2 } from "../config/api"
import { normalizeCameraUrl } from "../utils/cameraNetwork"
import { isSimMode, installSimulation } from "../services/simulationHarness"
import { enterpriseApi, zoneAuthorityApi } from "../services/enterpriseSecurityApi"
import { authState } from "../stores/auth"
import { onEntityChanged } from "../services/notificationApi"
import DecisionDrawer from "./shared/DecisionDrawer.vue"
import StepUpModal from "./shared/StepUpModal.vue"
import AuditReceiptToast from "./shared/AuditReceiptToast.vue"

const plateLane2Api = createPlateCameraApi(PLATE_API_BASE_URL_LANE2)

function createFaceModule(faceCameraId = "lane-1-face") {
  return {
    faceCameraId,
    cameraId: null,
    cameraIp: "",
    currentIp: "",
    cameraRunning: false,
    cameraConnected: false,
    previewRunning: false,
    pollingBusy: false,

    previewHealthy: false,
    imgBusy: false,
    scanKickoffBusy: false,
    controlSessionId: 0,
    lastAutoScanAt: 0,

    directCameraUrl: "",
    directCameraKey: 0,
    viewUrl: "",

    employeeId: "",
    employeeName: "",
    trackingActive: false,
    identityConfirmed: false,
    faceMatch: false,
    confirmCount: 0,
    distance: null,
    timeoutState: false,
    alert: false,
    scanLocked: false,
    lockReason: "",

    lockedSnapshot: "",
    lockedFaceCrop: "",

    message: "",
    fps: 0,
    lastUpdate: "",
    serviceErrorCode: "",
    serviceErrorMessage: "",

    frameWidth: 0,
    frameHeight: 0,
    overlayText: "",
    overlayBox: null,

    resultTimer: null,
    busyResult: false,
    isFetchingLockedImages: false,
    destroyed: false
  }
}

function createPlateModule() {
  return {
    cameraId: null,
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
    confirmedAt: 0,

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
    viewUrl: "",
    previewHealthy: false,
    overlayText: "",
    overlayBox: null,

    resultTimer: null,
    busyResult: false,
    isFetchingLockedImages: false,
    destroyed: false
  }
}

function createAutoModule() {
  return {
    on: false,
    status: "idle",
    sessionId: 0,
    deciding: false,
    saved: false,
    decideCooldownUntil: 0,
    decidedAt: 0,
    flash: "",
    flashUntil: 0,
    flashTimer: null,
    error: "",
    errorUntil: 0,
    faceStarting: false,
    faceStartedForSession: false,
    faceValue: "",
    faceSeenAt: 0,
    faceLostAt: 0,
    faceLastPresentAt: 0,
    faceEmptySinceAt: 0,
    plateValue: "",
    plateSeenAt: 0,
    plateLostAt: 0,
    plateLastPresentAt: 0,
    plateEmptySinceAt: 0,
    plateExitHoldMs: 1000,
    plateCooldownUntil: 0
  }
}

export default {
  name: "VShieldGateFaceMinimal",
  components: { DecisionDrawer, StepUpModal, AuditReceiptToast },

  data() {
    return {
      cameras: [],
      cameraSearch: {},
      faceDropdownOpen: {},
      plateDropdownOpen: {},
      lanes: [
        {
          id: "lane1",
          laneId: 1,
          accessLaneId: 1,
          faceCameraId: "lane-1-face",
          name: "Làn 1",
          desc: "Face trên / Biển dưới",
          gateId: 1,
          direction: "IN",
          savedDirection: "IN",
          directionSaving: false,
          directionMessage: "",
          cameraId: null,
          loading: false,
          faceApi,
          plateApi: plateLane1Api,
          face: createFaceModule("lane-1-face"),
          plate: createPlateModule(),
          auto: createAutoModule()
        },
        {
          id: "lane2",
          laneId: 2,
          accessLaneId: 2,
          faceCameraId: "lane-2-face",
          name: "Làn 2",
          desc: "Face trên / Biển dưới",
          gateId: 1,
          direction: "OUT",
          savedDirection: "OUT",
          directionSaving: false,
          directionMessage: "",
          cameraId: null,
          loading: false,
          faceApi,
          plateApi: plateLane2Api,
          face: createFaceModule("lane-2-face"),
          plate: createPlateModule(),
          auto: createAutoModule()
        }
      ],
      opsDrawerOpen: false,
      opsActiveLaneId: "lane1",
      topbarCompact: true,
      autoActive: false,
      autoStarting: false,
      autoTimer: null,
      simEnable: false,
      simState: {
        expanded: false,
        targetLane: 'lane1',
        laneSynced: false,
        injectEmpId: '7',
        injectPlate: '59K-12345',
        logText: ''
      },
      runtimeServices: [],
      runtimeBusy: {},
      uiTogglePending: {},
      decisionDrawerVisible: false,
      decisionLaneId: null,
      stepUpVisible: false,
      stepUpAction: null,
      stepUpLaneId: null,
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
      if (lane.plate.confirmedPlate) {
        warnings.push({
          severity: 'warn',
          text: 'Cảnh báo: Biển số đã được quét gần đây. Kiểm tra anti-passback.',
          icon: '&#9888;'
        })
      }
      if (lane.face.alert) {
        warnings.push({
          severity: 'critical',
          text: 'Khuôn mặt người lạ hoặc không hợp lệ. Vui lòng kiểm tra lại.',
          icon: '&#9940;'
        })
      }
      return warnings
    },
  },

  async mounted() {
    document.body.classList.add("gate-transit-compact")
    if (isSimMode()) {
      this.simEnable = true
      installSimulation(this)
    }
    await this.loadCameraList()
    await this.loadTransitLaneDirections()
    await this.fetchUserZones()

    this.unsubscribeSync = onEntityChanged(['AccessLog', 'LaneEvent', 'Gate', 'Camera'], () => {
      this.fetchUserZones()
    })

    for (const lane of this.lanes) {
      lane.face.destroyed = false
      lane.plate.destroyed = false
      await this.loadStatusFace(lane)
      await this.loadStatusPlate(lane)
      if (lane.face.cameraRunning) this.startFaceLoop(lane)
      if (lane.plate.cameraRunning) this.startPlateLoop(lane)
    }
  },

  beforeUnmount() {
    this.unsubscribeSync?.()
    document.body.classList.remove("gate-transit-compact")
    this.autoActive = false
    if (this.autoTimer) {
      clearInterval(this.autoTimer)
      this.autoTimer = null
    }
    for (const lane of this.lanes) {
      lane.face.destroyed = true
      lane.plate.destroyed = true

      this.stopFaceLoop(lane)
      this.stopPlateLoop(lane)

      this.resetPreview(lane.face)
      this.resetPreview(lane.plate)
    }
  },

  activated() {
    document.body.classList.add("gate-transit-compact")
    for (const lane of this.lanes) {
      lane.face.destroyed = false
      lane.plate.destroyed = false

      if (lane.face.cameraRunning) {
        if (lane.face.viewUrl && !lane.face.previewRunning) {
          this.mountPreview(lane.face, lane.face.viewUrl)
        }
        this.startFaceLoop(lane)
      }

      if (lane.plate.cameraRunning) {
        if (lane.plate.viewUrl && !lane.plate.previewRunning) {
          this.mountPreview(lane.plate, lane.plate.viewUrl)
        }
        this.startPlateLoop(lane)
      }
    }
    if (this.autoActive) this.startAutoMonitor()
  },

  deactivated() {
    document.body.classList.remove("gate-transit-compact")
    if (this.autoTimer) {
      clearInterval(this.autoTimer)
      this.autoTimer = null
    }
    for (const lane of this.lanes) {
      this.stopFaceLoop(lane)
      this.stopPlateLoop(lane)
    }
  },

  methods: {
    async loadTransitLaneDirections() {
      try {
        const response = await getTransitLanes()
        const configuredLanes = response?.data?.data || response?.data || []
        for (const lane of this.lanes) {
          const configured = configuredLanes.find(item => Number(item.laneId ?? item.LaneId) === Number(lane.laneId))
          const direction = String(configured?.direction ?? configured?.Direction ?? '').toUpperCase()
          if (direction === 'IN' || direction === 'OUT') {
            lane.direction = direction
            lane.savedDirection = direction
          }
        }
      } catch (error) {
        console.warn('Không tải được cấu hình hướng làn', error)
      }
    },

    async saveLaneDirection(lane) {
      if (this.autoActive) return
      const direction = String(lane.direction || '').toUpperCase()
      if (!['IN', 'OUT'].includes(direction)) {
        lane.direction = lane.savedDirection || 'IN'
        lane.directionMessage = 'Hướng làn không hợp lệ.'
        return
      }

      lane.directionSaving = true
      lane.directionMessage = ''
      try {
        const response = await updateTransitLaneDirection(lane.laneId, direction)
        const saved = String(response?.data?.data?.direction ?? response?.data?.data?.Direction ?? direction).toUpperCase()
        lane.direction = saved
        lane.savedDirection = saved
        lane.directionMessage = `Đã lưu ${lane.name} là ${saved}.`
      } catch (error) {
        lane.direction = lane.savedDirection || 'IN'
        lane.directionMessage = error?.response?.data?.message || 'Không thể lưu hướng làn.'
      } finally {
        lane.directionSaving = false
      }
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

      for (const lane of this.lanes) {
        try {
          await this.loadStatusFace(lane)
          await this.loadStatusPlate(lane)
        } catch (e) {
          console.warn("loadStatus", e)
        }
      }
    },

    isLaneReady(lane) {
      return (
        lane.face.scanLocked &&
        lane.plate.scanLocked &&
        !!lane.face.employeeId &&
        !!lane.plate.confirmedPlate &&
        !lane.face.alert
      )
    },

    laneAnyRunning(lane) {
      return lane.face.cameraRunning || lane.plate.cameraRunning
    },

    faceStateText(face) {
      if (face.scanLocked) {
        if (face.lockReason === "confirmed") return "ĐÃ NHẬN DIỆN"
        if (face.lockReason === "alert") return "CẢNH BÁO"
        if (face.lockReason === "timeout") return "TIMEOUT"
        return "ĐÃ KHÓA"
      }

      if (!face.cameraRunning) return "CHỜ"
      if (face.identityConfirmed) return "ĐÃ XÁC NHẬN"
      if (face.faceMatch) return "ĐANG SO KHỚP"
      if (face.trackingActive) return "ĐANG THEO DÕI"
      return "ĐANG QUÉT"
    },

    faceStateClass(face) {
      if (face.alert) return "danger-text"
      if (face.scanLocked && face.identityConfirmed) return "ok-text"
      return "warn-text"
    },

    cameraVisualState(type, lane) {
      if (type === "face") {
        const face = lane.face
        if (!face.cameraRunning) return "idle"
        if (face.scanLocked && face.identityConfirmed && face.employeeId) return "valid"
        if (face.alert || face.timeoutState) return "invalid"
        if (face.trackingActive || face.faceMatch || face.cameraRunning) return "scanning"
        return "idle"
      }

      const plate = lane.plate
      if (!plate.cameraRunning) return "idle"
      if (plate.scanLocked && !!plate.confirmedPlate) return "valid"
      if (plate.alert) return "invalid"
      if (plate.scanActive || plate.ocrRunning) return "scanning"
      return "idle"
    },

    cameraVisualText(type, lane) {
      if (type === "face") {
        const face = lane.face
        const state = this.cameraVisualState(type, lane)
        if (state === "valid") return "ĐÃ NHẬN DIỆN"
        if (state === "invalid") return "CẢNH BÁO"
        if (face.identityConfirmed) return "ĐÃ XÁC NHẬN"
        if (face.faceMatch) return "ĐANG SO KHỚP"
        if (face.trackingActive) return "ĐANG THEO DÕI"
        if (face.cameraRunning) return "ĐANG QUÉT"
        return "TẮT"
      }

      const plate = lane.plate
      const state = this.cameraVisualState(type, lane)
      if (state === "valid") return "ĐÃ NHẬN DIỆN"
      if (state === "invalid") return "KHÔNG HỢP LỆ"
      if (plate.scanActive || plate.cameraRunning) return "ĐANG QUÉT"
      return "TẮT"
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

    buildDirectCameraUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return ""
      if (raw.startsWith("data:image/")) return raw

      const normalized = normalizeCameraUrl(raw)
      if (/^https?:\/\//i.test(normalized) || normalized.startsWith("/")) {
        try {
          const parsed = new URL(normalized, window.location.origin)
          if (parsed.pathname.endsWith("/stream.html")) {
            parsed.searchParams.set("mode", "mse,webrtc")
            return parsed.toString()
          }
        } catch {
          // ignore URL parsing error
        }
        return normalized
      }
      return normalized
    },

    isImageUrl(url) {
      if (!url || typeof url !== "string") return false
      const clean = url.split("?")[0].toLowerCase()
      return (
        clean.endsWith(".jpg") ||
        clean.endsWith(".jpeg") ||
        clean.endsWith(".png") ||
        clean.endsWith(".webp") ||
        clean.endsWith("/frame.jpg") ||
        clean.includes("/video_feed") ||
        clean.startsWith("data:image/") ||
        clean.endsWith("/snapshot")
      )
    },

    mountPreview(module, url) {
      const cleanUrl = String(url || "").trim()
      if (!cleanUrl) return
      module.directCameraUrl = this.buildDirectCameraUrl(cleanUrl)
      module.directCameraKey += 1
      module.previewHealthy = false
      module.previewRunning = true
    },

    resetPreview(module) {
      module.directCameraUrl = ""
      module.directCameraKey += 1
      module.previewHealthy = false
      module.previewRunning = false
      module.imgBusy = false
      module.frameWidth = 0
      module.frameHeight = 0
    },

    onPreviewLoaded(module) {
      module.previewHealthy = true
    },

    onPreviewError(module) {
      module.previewHealthy = false
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

    clearFaceState(face) {
      face.employeeId = ""
      face.employeeName = ""
      face.trackingActive = false
      face.identityConfirmed = false
      face.faceMatch = false
      face.confirmCount = 0
      face.distance = null
      face.timeoutState = false
      face.alert = false
      face.scanLocked = false
      face.lockReason = ""
      face.lockedSnapshot = ""
      face.lockedFaceCrop = ""
      face.message = ""
      face.fps = 0
      face.lastUpdate = ""
      face.serviceErrorCode = ""
      face.serviceErrorMessage = ""
      face.overlayText = ""
      face.overlayBox = null
      face.lastLockedImageSessionId = 0
    },

    clearPlateState(plate) {
      plate.confirmedPlate = ""
      plate.confirmedAt = 0
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

    hardResetFace(face) {
      face.cameraRunning = false
      face.cameraConnected = false
      face.currentIp = ""
      face.pollingBusy = false
      this.clearFaceState(face)
    },

    hardResetPlate(plate) {
      plate.cameraRunning = false
      plate.currentIp = ""
      plate.sessionId = 0
      plate.lastAppliedSessionId = 0
      this.clearPlateState(plate)
    },

    startFaceLoop(lane) {
      this.stopFaceLoop(lane)

      lane.face.resultTimer = setInterval(async () => {
        if (lane.face.destroyed) return
        if (!lane.face.cameraRunning) return
        if (lane.face.busyResult) return

        lane.face.busyResult = true
        try {
          await this.refreshFace(lane)
        } finally {
          lane.face.busyResult = false
        }
      }, 500)
    },

    stopFaceLoop(lane) {
      const face = lane.face
      if (face.resultTimer) {
        clearInterval(face.resultTimer)
        face.resultTimer = null
      }
      face.busyResult = false
    },

    async loadStatusFace(lane) {
      try {
        const res = await lane.faceApi.getCameraStatus(lane.face.faceCameraId || lane.faceCameraId)
        await this.applyFaceRealtimeState(lane, res, false)

        if (lane.face.viewUrl && !lane.face.previewRunning) {
          this.mountPreview(lane.face, lane.face.viewUrl)
        }
      } catch (e) {
        console.error("loadStatusFace error:", e)
      }
    },

    async refreshFace(lane) {
      try {
        const res = await lane.faceApi.getCameraResult(lane.face.faceCameraId || lane.faceCameraId)
        await this.applyFaceRealtimeState(lane, res, true)
      } catch (e) {
        console.warn("refreshFace error:", e)
      }
    },

    async fetchFaceLockedImages(lane, force = false) {
      const face = lane.face
      if (face.destroyed || !face.cameraRunning) return

      if (!face.scanLocked) {
        face.lockedSnapshot = ""
        face.lockedFaceCrop = ""
        return
      }

      if (face.isFetchingLockedImages) return
      face.isFetchingLockedImages = true
      try {
        const res = await lane.faceApi.getLockedImages(lane.face.faceCameraId || lane.faceCameraId)
        if (res?.scan_locked || res?.locked_snapshot || res?.locked_face_crop) {
          face.lockedSnapshot = res.locked_snapshot || face.lockedSnapshot || ""
          face.lockedFaceCrop = res.locked_face_crop || face.lockedFaceCrop || ""
          if (face.previewRunning && (face.lockedSnapshot || face.lockedFaceCrop)) {
            face.previewHealthy = true
          }
        }
      } catch (e) {
        console.warn("fetchFaceLockedImages error:", e)
      } finally {
        face.isFetchingLockedImages = false
      }
    },

    async applyFaceRealtimeState(lane, res, allowTurnOffReset = true) {
      if (!res || lane.face.destroyed) return

      const face = lane.face
      const incomingCameraEnabled = !!res.camera_enabled
      if (typeof res.camera_enabled !== "undefined") {
        face.cameraRunning = incomingCameraEnabled
      }

      face.cameraConnected = !!res.camera_connected
      face.currentIp = res.ip || face.currentIp

      face.employeeId = res.employee_id ? String(res.employee_id) : ""
      face.employeeName = res.employee_name || res.subject_name || (face.employeeId ? 'NV ' + face.employeeId : '')
      face.trackingActive = !!res.tracking_active
      face.identityConfirmed = !!res.identity_confirmed
      face.faceMatch = !!res.face_match
      face.confirmCount = Number(res.confirm_count || 0)
      face.distance = res.distance ?? null
      face.timeoutState = !!res.timeout
      face.alert = !!res.alert

      face.scanLocked = !!res.scan_locked
      face.lockReason = res.lock_reason || ""

      face.fps = Number(res.fps || 0)
      face.message = res.message || ""
      face.lastUpdate = res.last_update || ""

      face.overlayBox = this.normalizeBox(res.bbox || res.bounding_box || null)
      face.overlayText = face.employeeName || (face.employeeId ? 'NV ' + face.employeeId : (face.alert ? 'Người lạ' : ''))

      if (!face.scanLocked) {
        face.lockedSnapshot = ""
        face.lockedFaceCrop = ""
      }

      if (!incomingCameraEnabled && allowTurnOffReset) {
        this.stopFaceLoop(lane)
        this.hardResetFace(face)
        return
      }

      if (face.scanLocked) {
        await this.fetchFaceLockedImages(lane, false)
      }
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

    stopPlateLoop(lane) {
      const plate = lane.plate
      if (plate.resultTimer) {
        clearInterval(plate.resultTimer)
        plate.resultTimer = null
      }
      plate.busyResult = false
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

      const configuredIp = String(plate.currentIp || plate.cameraIp || "").trim()
      const sameConfiguredCamera =
        !incomingIp || normalizeCameraUrl(incomingIp) === normalizeCameraUrl(configuredIp)
      if (!configuredIp || !sameConfiguredCamera) {
        this.stopPlateLoop(lane)
        this.hardResetPlate(plate)
        return
      }

      if (incomingSessionId > 0) {
        plate.sessionId = incomingSessionId
        plate.lastAppliedSessionId = incomingSessionId
      }

      const incomingCameraEnabled = !!res.camera_enabled
      if (typeof res.camera_enabled !== "undefined") {
        plate.cameraRunning = incomingCameraEnabled
      }

      plate.currentIp = res.ip || plate.currentIp
      const nextConfirmedPlate = String(res.confirmed_plate || "").trim()
      if (nextConfirmedPlate && nextConfirmedPlate !== plate.confirmedPlate) {
        plate.confirmedAt = Date.now()
      }
      if (!nextConfirmedPlate) plate.confirmedAt = 0
      plate.confirmedPlate = nextConfirmedPlate
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
      if (!lane.face.cameraIp.trim() && !lane.plate.cameraIp.trim()) {
        alert("Vui lòng nhập ít nhất 1 URL camera")
        return
      }

      if (lane.loading) return

      try {
        lane.loading = true

        if (lane.face.viewUrl) {
          if (lane.face.previewRunning) {
            this.resetPreview(lane.face)
            await new Promise(r => setTimeout(r, 300))
          }
          this.mountPreview(lane.face, lane.face.viewUrl)
          lane.face.message = "Đã tải lại preview Face"
        }

        if (lane.plate.viewUrl) {
          if (lane.plate.previewRunning) {
            this.resetPreview(lane.plate)
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

    async restartFaceSession(lane, message = "Đang quét lại khuôn mặt...") {
      const face = lane?.face
      const ip = String(face?.cameraIp || face?.currentIp || face?.viewUrl || "").trim()
      if (!ip) {
        throw new Error("Vui lòng chọn hoặc nhập URL camera Face")
      }

      this.stopFaceLoop(lane)
      this.clearFaceState(face)
      face.message = "Đang khởi động lại nhận diện Face..."

      try {
        const res = await lane.faceApi.startCamera(face.faceCameraId || lane.faceCameraId, ip, lane.laneId)
        face.cameraRunning = true
        face.currentIp = ip
        face.message = res?.message || message

        if (face.viewUrl && !face.previewRunning) {
          this.mountPreview(face, face.viewUrl)
        }

        this.startFaceLoop(lane)
        await this.refreshFace(lane)
      } catch (e) {
        this.hardResetFace(face)
        throw e
      }
    },

    async readAllLane(lane) {
      const faceIp = String(lane.face.cameraIp || lane.face.currentIp || "").trim()
      const plateIp = String(lane.plate.currentIp || lane.plate.cameraIp || lane.plate.viewUrl || "").trim()
      if (!faceIp || !plateIp) {
        alert("Vui lòng chọn hoặc nhập đủ URL Face và Biển số")
        return
      }

      try {
        lane.loading = true
        await this.restartFaceSession(lane, "Đang nhận diện Face...")

        this.stopPlateLoop(lane)
        const resPlate = await lane.plateApi.turnOnCamera(plateIp)
        if (!resPlate?.success && !resPlate?.session_id) {
          alert(resPlate?.message || "Không thể khởi tạo trình nhận diện biển số")
          return
        }
        lane.plate.cameraRunning = true
        lane.plate.sessionId = Number(resPlate.session_id || 0)
        lane.plate.lastAppliedSessionId = lane.plate.sessionId
        lane.plate.currentIp = plateIp
        lane.plate.message = resPlate.message || "Khởi tạo trình nhận diện biển số thành công"

        if (lane.plate.viewUrl && !lane.plate.previewRunning) {
          this.mountPreview(lane.plate, lane.plate.viewUrl)
        }

        await this.refreshPlate(lane)
        if (!lane.plate.resultTimer) this.startPlateLoop(lane)
      } catch (e) {
        console.error("readAllLane error:", e)
        alert(e?.message || "Lỗi đọc cả 2")
      } finally {
        lane.loading = false
      }
    },

    async retryFace(lane) {
      const faceIp = String(lane.face.cameraIp || lane.face.currentIp || lane.face.viewUrl || "").trim()
      if (!faceIp) {
        alert("Vui lòng chọn hoặc nhập URL Face")
        return
      }

      try {
        lane.loading = true
        await this.restartFaceSession(lane, "Đang nhận diện lại khuôn mặt...")
      } catch (e) {
        console.error("retryFace error:", e)
        alert(e?.message || "Lỗi đọc lại Face")
      } finally {
        lane.loading = false
      }
    },

    async retryPlate(lane) {
      const plateIp = String(lane.plate.cameraIp || lane.plate.currentIp || lane.plate.viewUrl || "").trim()
      if (!plateIp) {
        alert("Vui lòng chọn hoặc nhập URL Plate")
        return
      }

      try {
        lane.loading = true
        this.clearPlateState(lane.plate)

        this.stopPlateLoop(lane)
        const res = await lane.plateApi.turnOnCamera(plateIp)
        if (!res?.success && !res?.session_id) {
          alert(res?.message || "Không thể khởi tạo Plate")
          return
        }
        lane.plate.cameraRunning = true
        lane.plate.sessionId = Number(res.session_id || 0)
        lane.plate.lastAppliedSessionId = lane.plate.sessionId
        lane.plate.currentIp = plateIp
        lane.plate.message = res.message || "Khởi tạo Plate thành công"

        if (lane.plate.viewUrl && !lane.plate.previewRunning) {
          this.mountPreview(lane.plate, lane.plate.viewUrl)
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

        this.stopFaceLoop(lane)
        try {
          await lane.faceApi.stopCamera(lane.face.faceCameraId || lane.faceCameraId)
        } catch (e) {
          console.warn("stopCamera face warning:", e)
        }
        this.hardResetFace(lane.face)
        this.resetPreview(lane.face)

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
      const employeeId = Number(lane.face.employeeId || 0)

      if (!licensePlate) {
        alert(`${lane.name}: chưa có biển số`)
        return
      }

      if (!employeeId) {
        alert(`${lane.name}: chưa có Employee ID`)
        return
      }

      try {
        lane.loading = true

        const payload = {
          LicensePlate: licensePlate,
          GateId: lane.gateId,
          LaneId: lane.laneId,
          Direction: lane.direction,
          CameraId: lane.plate.cameraId || lane.face.cameraId || lane.cameraId,
          CredentialType: "FACE",
          EmployeeId: employeeId,
          PlateSnapshotBase64: lane.plate.lockedSnapshot || null,
          PlateCropBase64: lane.plate.lockedPlateCrop || null,
          FaceSnapshotBase64: lane.face.lockedSnapshot || lane.face.lockedFaceCrop || null
        }

        const res = await scanGate(payload)
        const data = res.data

        if (data?.success) {
          alert(`${lane.name}: ${data.message}`)
        } else {
          alert(`${lane.name}: ${data?.message || "Xử lý thất bại"}`)
        }
      } catch (error) {
        const message = error?.response?.data?.message || error?.message || "Không gọi được API Gate"
        alert(`${lane.name}: ${message}`)
      } finally {
        lane.loading = false
      }
    },

    // ================= AUTO MONITOR =================

    toggleAutoMonitor() {
      if (this.autoActive) {
        this.stopAutoMonitor()
      } else {
        this.startAutoMonitor()
      }
    },

    async startAutoMonitor() {
      if (this.autoActive && this.autoTimer) return
      this.autoActive = true
      this.autoStarting = true
      try {
        for (const lane of this.lanes) {
          lane.auto.on = true
          lane.auto.sessionId = this.newAutoSessionId(lane)
          await this.setupAutoLaneFace(lane)
        }
        this.autoTimer = setInterval(() => {
          for (const lane of this.lanes) this.autoTick(lane)
        }, 350)
      } finally {
        this.autoStarting = false
      }
    },

    async stopAutoMonitor() {
      if (!this.autoActive) return
      this.autoActive = false
      if (this.autoTimer) {
        clearInterval(this.autoTimer)
        this.autoTimer = null
      }
      for (const lane of this.lanes) {
        await this.teardownAutoLane(lane)
      }
    },

    async setupAutoLaneFace(lane) {
      this.stopFaceLoop(lane)
      this.clearFaceState(lane.face)
      lane.face.cameraRunning = false
      lane.auto.faceStarting = false
      lane.auto.faceStartedForSession = false
    },

    async teardownAutoLane(lane) {
      const auto = lane.auto
      auto.on = false
      if (auto.flashTimer) {
        clearTimeout(auto.flashTimer)
        auto.flashTimer = null
      }
      auto.flash = ""
      auto.flashUntil = 0
      auto.error = ""
      auto.errorUntil = 0
      auto.faceValue = ""
      auto.plateValue = ""
      auto.status = "idle"

      this.stopFaceLoop(lane)
      try {
        await lane.faceApi.stopCamera(lane.face.faceCameraId || lane.faceCameraId)
      } catch (e) {
        console.warn("stopCamera auto", e)
      }
      this.clearFaceState(lane.face)
      lane.face.cameraRunning = false

      this.stopPlateLoop(lane)
      try {
        await lane.plateApi.turnOffCamera()
      } catch (e) {
        console.warn("turnOffCamera auto", e)
      }
      this.clearPlateState(lane.plate)
      lane.plate.cameraRunning = false
    },

    async autoTick(lane) {
      const auto = lane.auto
      if (!auto.on || !this.autoActive || lane.loading) return
      const now = Date.now()
      const face = lane.face
      const plate = lane.plate

      if (!plate.cameraRunning && !auto.plateValue) {
        const acquired = await this.tryAcquirePlateForLane(lane)
        if (!acquired || !plate.cameraRunning) return
      }

      const plateValue = String(plate.confirmedPlate || "").trim()

      if (plate.overlayBox) {
        auto.plateLastPresentAt = now
        auto.plateEmptySinceAt = 0
      }

      if (plateValue) {
        if (auto.plateValue !== plateValue) auto.plateValue = plateValue
        auto.plateSeenAt = now
        auto.plateLostAt = 0
      } else if (auto.plateValue) {
        if (!auto.plateLostAt) auto.plateLostAt = now
      }

      if (auto.plateValue && !plate.overlayBox) {
        if (!auto.plateEmptySinceAt) auto.plateEmptySinceAt = now
        if (now - auto.plateEmptySinceAt >= auto.plateExitHoldMs) {
          this.releaseAutoSession(lane, "Biển số đã rời vùng quét")
          return
        }
      }

      if (auto.plateValue && !auto.faceStartedForSession && !auto.faceStarting) {
        await this.startAutoFaceAfterPlate(lane)
        return
      }

      const faceIdentity = String(face.employeeId || "").trim()
      if (faceIdentity && (face.identityConfirmed || face.scanLocked)) {
        if (auto.faceValue !== faceIdentity) auto.faceValue = faceIdentity
        auto.faceSeenAt = now
        auto.faceLostAt = 0
      } else if (auto.faceValue && !auto.faceLostAt) {
        auto.faceLostAt = now
      }

      if (auto.faceValue && auto.plateValue && !auto.saved && !auto.deciding) {
        if (now >= auto.decideCooldownUntil) {
          await this.autoDecideSession(lane)
        }
      }
    },

    async startAutoFaceAfterPlate(lane) {
      const auto = lane.auto
      if (!auto.on || auto.faceStarting || auto.faceStartedForSession) return
      if (!lane.face.cameraIp.trim()) {
        auto.status = "collecting"
        auto.error = "Đã nhận biển số; chưa cấu hình camera Face"
        return
      }
      auto.faceStarting = true
      auto.status = "collecting"
      auto.error = ""
      try {
        await this.restartFaceSession(lane, "Đã khóa biển số, đang nhận diện khuôn mặt...")
        auto.faceStartedForSession = true
      } catch (e) {
        auto.error = e?.message || "Không khởi động được nhận diện Face sau khi nhận biển số"
      } finally {
        auto.faceStarting = false
      }
    },

    releaseAutoSession(lane, reason) {
      const auto = lane.auto
      if (!auto.on) return
      auto.sessionId = this.newAutoSessionId(lane)
      auto.saved = false
      auto.faceStarting = false
      auto.faceStartedForSession = false
      auto.status = "idle"
      auto.flash = ""
      auto.flashUntil = 0
      auto.error = ""
      auto.errorUntil = 0
      auto.faceValue = ""
      auto.faceSeenAt = 0
      auto.faceLostAt = 0
      auto.faceLastPresentAt = 0
      auto.faceEmptySinceAt = 0
      auto.plateValue = ""
      auto.plateSeenAt = 0
      auto.plateLostAt = 0
      auto.plateLastPresentAt = 0
      auto.plateEmptySinceAt = 0
      this.clearFaceState(lane.face)
      this.clearPlateState(lane.plate)
      lane.faceApi.stopCamera(lane.face.faceCameraId || lane.faceCameraId).catch(() => {})
      this.stopFaceLoop(lane)
      lane.face.cameraRunning = false
      lane.plateApi.resetCameraState().catch(() => {})
    },

    newAutoSessionId(lane) {
      const nonce = globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`
      return `gate-${lane.gateId}-lane-${lane.laneId}-${nonce}`
    },

    async tryAcquirePlateForLane(lane) {
      if (!lane.auto.on) return false
      if (lane.plate.cameraRunning) return true
      if (Date.now() < lane.auto.plateCooldownUntil) return false

      const targetIp = String(
        lane.plate.currentIp || lane.plate.cameraIp || lane.plate.viewUrl || ""
      ).trim()
      if (!targetIp) {
        lane.auto.plateCooldownUntil = Date.now() + 5000
        lane.auto.error = "Chưa chọn camera biển số"
        return false
      }

      try {
        this.stopPlateLoop(lane)
        const res = await lane.plateApi.turnOnCamera(targetIp)
        if (res?.success || res?.session_id) {
          lane.plate.cameraRunning = true
          lane.plate.sessionId = Number(res.session_id || 0)
          lane.plate.lastAppliedSessionId = lane.plate.sessionId
          lane.plate.currentIp = targetIp
          lane.plate.scanActive = true
          this.startPlateLoop(lane)
          return true
        }
        lane.plate.cameraRunning = false
        lane.auto.plateCooldownUntil = Date.now() + 5000
        lane.auto.error = res?.message || "Không khởi tạo được biển số"
        return false
      } catch (e) {
        lane.plate.cameraRunning = false
        lane.auto.plateCooldownUntil = Date.now() + 5000
        lane.auto.error =
          e?.response?.data?.message ||
          e?.message ||
          "Không kết nối được Python biển số"
        return false
      }
    },

    async autoDecideSession(lane) {
      const auto = lane.auto
      const face = lane.face
      const plate = lane.plate

      const licensePlate = String(plate.confirmedPlate || "").trim()
      const employeeId = Number(face.employeeId || 0)

      if (!licensePlate) return
      if (!employeeId) return

      auto.deciding = true
      auto.status = "deciding"
      auto.flash = ""
      auto.flashUntil = 0

      try {
        const payload = {
          LicensePlate: licensePlate,
          GateId: lane.gateId,
          LaneId: lane.laneId,
          Direction: lane.direction,
          CameraId: plate.cameraId || face.cameraId || lane.cameraId,
          CredentialType: "FACE",
          EmployeeId: employeeId,
          TransitSessionId: String(auto.sessionId || this.newAutoSessionId(lane)),
          PlateSnapshotBase64: plate.lockedSnapshot || null,
          PlateCropBase64: plate.lockedPlateCrop || null,
          FaceSnapshotBase64: face.lockedSnapshot || face.lockedFaceCrop || null
        }

        const res = await scanGate(payload)
        const data = res?.data || {}
        const ok = !!data?.success

        auto.saved = true
        auto.status = "decided"
        auto.decidedAt = Date.now()

        if (ok) {
          this.flashLane(lane, "allow", "")
          this.showAuditToast(
            "success",
            "Thông hành tự động",
            `${licensePlate} (NV ${employeeId}) được phép qua.`,
            data?.receiptId || data?.logId ? `RCP-${data.logId || data.receiptId || Date.now()}` : `RCP-${Date.now()}`
          )
        } else {
          this.flashLane(lane, "deny", data?.message || "Từ chối thông hành")
          this.showAuditToast(
            "warning",
            "Từ chối tự động",
            `${licensePlate}: ${data?.message || "Không được phép"}`,
            `RCP-${Date.now()}`
          )
        }
      } catch (e) {
        const status = Number(e?.response?.status || 0)
        const message =
          e?.response?.data?.message || e?.message || "Xử lý thất bại"

        if (status === 409) {
          auto.saved = true
          auto.status = "decided"
          auto.decidedAt = Date.now()
          this.flashLane(lane, "deny", message)
          this.showAuditToast("danger", "Từ chối tự động", `${licensePlate}: ${message}`, `RCP-${Date.now()}`)
        } else {
          auto.saved = false
          auto.status = "idle"
          auto.decideCooldownUntil = Date.now() + 3000
          this.flashLane(lane, "deny", message)
        }
      } finally {
        auto.deciding = false
      }
    },

    flashLane(lane, type, errorMsg) {
      const auto = lane.auto
      const now = Date.now()
      auto.flash = type
      auto.flashUntil = now + 1700
      auto.error = ""
      auto.errorUntil = 0
      if (type === "deny" && errorMsg) {
        auto.error = errorMsg
        auto.errorUntil = now + 8000
      }
      if (auto.flashTimer) clearTimeout(auto.flashTimer)
      auto.flashTimer = setTimeout(() => {
        if (auto.flashUntil <= Date.now()) auto.flash = ""
      }, 1800)
    },

    autoCamClass(lane) {
      const auto = lane.auto
      if (auto.flash === "allow") return " flash-allow"
      if (auto.flash === "deny") return " flash-deny"
      return ""
    },

    autoStatusText(lane) {
      const auto = lane.auto
      if (!auto.on) return "chờ"
      if (auto.error) return "có lỗi"
      if (auto.status === "deciding") return "đang quyết định"
      if (auto.status === "decided") return auto.flash === "deny" ? "từ chối" : "đã cho qua"
      if (auto.faceValue && auto.plateValue) return "đủ thông tin"
      if (auto.faceValue) return "đã đọc Face"
      if (auto.plateValue) return "đã đọc biển"
      return "đang quét"
    },

    autoChipClass(lane) {
      const auto = lane.auto
      if (auto.status === "decided") return auto.flash === "deny" ? "deny" : "allow"
      if (auto.error) return "error"
      if (auto.faceValue && auto.plateValue) return "ready"
      if (auto.faceValue || auto.plateValue) return "seen"
      return ""
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
      const isReady = lane && (!!lane.face.employeeId || !!lane.plate.confirmedPlate)
      const roleOk = role === 'Admin' || role === 'BaoVe'
      let zoneOk = true
      if (role === 'BaoVe' && lane && (lane.siteId || lane.securityZoneId)) {
        if (lane.securityZoneId) {
          zoneOk = this.userZoneIds.includes(lane.securityZoneId)
        }
      }
      return {
        allow: isReady && roleOk,
        deny: isReady && roleOk,
        manual: roleOk,
        unifiedEmergency: isReady && roleOk && zoneOk,
        escalate: isReady && roleOk,
      }
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
      const employeeId = Number(lane.face.employeeId || 0)

      if (!licensePlate) throw new Error('Chưa có biển số')
      if (!employeeId) throw new Error('Chưa có Employee ID')

      const payload = {
        LicensePlate: licensePlate,
        GateId: lane.gateId || null,
        LaneId: lane.laneId || null,
        Direction: lane.direction,
        CameraId: lane.cameraId || null,
        CredentialType: "FACE",
        EmployeeId: employeeId,
        PlateSnapshotBase64: lane.plate.lockedSnapshot || null,
        PlateCropBase64: lane.plate.lockedPlateCrop || null,
        FaceSnapshotBase64: lane.face.lockedSnapshot || lane.face.lockedFaceCrop || null
      }

      const res = await scanGate(payload)

      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'ACCESS_GRANTED',
          direction: lane.direction,
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
      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'ACCESS_DENIED',
          direction: lane.direction,
          plateText: lane.plate.confirmedPlate || '',
          note: reason || 'Từ chối',
        })
      } catch (e) {
        console.warn('recordLaneEvent failed:', e)
      }

      this.clearFaceState(lane.face)
      this.clearPlateState(lane.plate)
      this.showAuditToast('warning', 'Đã từ chối', `Từ chối: ${reason || 'Không có lý do'}.`, `RCP-${Date.now()}`)
    },

    async executeManual(lane, reason, details = {}) {
      const plate = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()
      const subjectName = String(details?.subjectName || lane.face.employeeName || '').trim()
      const subjectId = String(details?.subjectId || lane.face.employeeId || '').trim()
      if (!subjectName && !plate) throw new Error('Cần nhập họ tên/đơn vị hoặc biển số để vận hành thủ công.')

      const response = await enterpriseApi.recordLaneEvent({
        laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
        eventType: 'MANUAL_PASS',
        direction: lane.direction,
        plateText: plate,
        note: `[manual] ${subjectName}; subjectId=${subjectId}; reason=${reason}`,
      })

      this.showAuditToast('success', 'Đã cho qua thủ công', `${subjectName || plate} đã được cho qua và ghi nhận trách nhiệm.`, `MAN-${response.data?.laneEventId || Date.now()}`)
    },

    async executeOverride(lane, reason, responsibility) {
      const licensePlate = String(lane.plate.confirmedPlate || '').trim()
      const employeeId = Number(lane.face.employeeId || 0)

      if (!licensePlate) throw new Error('Chưa có biển số')

      const payload = {
        LicensePlate: licensePlate,
        GateId: lane.gateId || null,
        LaneId: lane.laneId || null,
        Direction: lane.direction,
        CameraId: lane.cameraId || null,
        CredentialType: "FACE",
        EmployeeId: employeeId,
        Responsibility: true,
        Reason: reason || 'Override không có lý do',
        PlateSnapshotBase64: lane.plate.lockedSnapshot || null,
        PlateCropBase64: lane.plate.lockedPlateCrop || null,
        FaceSnapshotBase64: lane.face.lockedSnapshot || lane.face.lockedFaceCrop || null
      }

      const res = await scanGate(payload)

      try {
        await enterpriseApi.recordLaneEvent({
          laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
          eventType: 'OVERRIDE',
          direction: lane.direction,
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
      try {
        const res = await enterpriseApi.recordDuressEvent({
          userId: authState.user?.userId || null,
          employeeId: authState.user?.employeeId || null,
          accessPointId: null,
          siteId: lane.siteId || null,
          securityZoneId: lane.securityZoneId || null,
          credentialType: 'FaceID',
          description: `${lane.name || 'Gate'}: ${reason || 'Ghi nhận duress'}`,
        })
        const receiptId = res.data?.duressEventId || `DRS-${Date.now()}`
        this.showAuditToast('danger', 'ĐÃ GHI NHẬN DURESS', 'Tín hiệu ép buộc đã được gửi đến trung tâm giám sát. Hỗ trợ sẽ được điều động.', receiptId)
      } catch (e) {
        console.error('recordDuressEvent failed:', e)
        this.showAuditToast('danger', 'DURESS (Offline)', 'Không thể gửi tín hiệu đến server. Đã lưu local. Vui lòng báo ngay quản lý.', `DRS-LOCAL-${Date.now()}`)
      }
    },

    async executeEscalate(lane, reason) {
      try {
        const res = await enterpriseApi.createInterventionRequest({
          interventionType: 'other',
          reason: reason || 'Yêu cầu can thiệp từ lane',
          siteId: lane.siteId || null,
          securityZoneId: lane.securityZoneId || null,
          laneId: String(lane.id || ''),
          laneName: String(lane.name || ''),
          subjectName: String(lane.face.employeeName || (lane.face.employeeId ? 'NV ' + lane.face.employeeId : 'Nhân viên')),
          subjectId: String(lane.face.employeeId || ''),
          subjectType: 'EMPLOYEE',
          plateNumber: String(lane.plate.confirmedPlate || lane.plate.lastRawPlate || ''),
          priority: 'medium',
          expiresInMinutes: 240,
        })

        try {
          await enterpriseApi.recordLaneEvent({
            laneId: Number.parseInt(String(lane.id).replace(/\D/g, ''), 10) || null,
            eventType: 'ESCALATION_REQUEST',
            direction: lane.direction,
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
      const subjectName = String(details?.subjectName || lane.face.employeeName || 'Đối tượng khẩn cấp').trim()
      const plateNumber = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()

      try {
        const response = await enterpriseApi.createEmergencyPass({
          subjectType: 'Person',
          subjectId: String(details?.subjectId || lane.face.employeeId || ''),
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
      if (!this.isAdmin) {
        this.showAuditToast('warning', 'Không có quyền', 'Chỉ Admin mới có quyền cấp quyền khẩn cấp. Vui lòng gửi yêu cầu.', '')
        return
      }

      if (!responsibility) throw new Error('Phải xác nhận trách nhiệm trước khi cấp quyền khẩn cấp.')
      const subjectName = String(details?.subjectName || lane.face.employeeName || 'Đối tượng khẩn cấp').trim()
      const plateNumber = String(details?.plateNumber || lane.plate.confirmedPlate || lane.plate.lastRawPlate || '').trim()
      const response = await enterpriseApi.createEmergencyPass({
        subjectType: 'EmergencyService',
        subjectId: String(details?.subjectId || lane.face.employeeId || ''),
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
      if (!cam) return
      if (!cam.urlView) {
        alert("Camera chưa có UrlView. Hãy reload go2rtc trước")
        return
      }
      const streamValue = String(cam.streamUrl || cam.urlView || "").trim()
      const viewValue = String(cam.urlView || "").trim()

      if (type === "face") {
        lane.face.cameraIp = streamValue
        lane.face.viewUrl = viewValue
        lane.face.currentIp = streamValue
        lane.face.cameraId = cam.cameraId
        lane.cameraId = cam.cameraId

        this.cameraSearch[lane.id + '-face'] = cam.cameraName
        this.faceDropdownOpen[lane.id] = false
        if (viewValue) {
          this.mountPreview(lane.face, viewValue)
        }
      }

      if (type === "plate") {
        lane.plate.cameraIp = streamValue
        lane.plate.viewUrl = viewValue
        lane.plate.currentIp = streamValue
        lane.plate.cameraId = cam.cameraId
        lane.cameraId = cam.cameraId

        this.cameraSearch[lane.id + '-plate'] = cam.cameraName
        this.plateDropdownOpen[lane.id] = false
        if (viewValue) {
          this.mountPreview(lane.plate, viewValue)
        }
      }
    },

    // ================= SIMULATION HELPERS =================
    simSetTargetLane(laneId) {
      this.simState.targetLane = laneId
    },
    simSyncLaneConfig() {
      this.simState.laneSynced = true
    },
    simSetFace(laneId, empId) {
      const lane = this.lanes.find(l => l.id === laneId)
      if (lane) {
        lane.face.cameraRunning = true
        lane.face.scanLocked = true
        lane.face.identityConfirmed = true
        lane.face.employeeId = empId
        lane.face.employeeName = 'NV ' + empId
        lane.face.alert = false
      }
    },
    simClearFace(laneId) {
      const lane = this.lanes.find(l => l.id === laneId)
      if (lane) {
        this.clearFaceState(lane.face)
      }
    },
    simSetPlate(laneId, plateNum) {
      const lane = this.lanes.find(l => l.id === laneId)
      if (lane) {
        lane.plate.cameraRunning = true
        lane.plate.scanLocked = true
        lane.plate.confirmedPlate = plateNum
        lane.plate.overlayBox = { x: 20, y: 30, width: 40, height: 20, unit: '%' }
      }
    },
    simClearPlate(laneId) {
      const lane = this.lanes.find(l => l.id === laneId)
      if (lane) {
        this.clearPlateState(lane.plate)
      }
    },
    simMakeAllowPlate() {
      const r = Math.floor(10000 + Math.random() * 90000)
      this.simState.injectPlate = `51F-${r}`
    },
    simRunScenario(laneId, type) {
      this.simSetFace(laneId, this.simState.injectEmpId || '7')
      if (type === 'allow') {
        this.simMakeAllowPlate()
        this.simSetPlate(laneId, this.simState.injectPlate)
      } else {
        this.simSetPlate(laneId, '99Z-99999')
      }
    },
    simResetAll() {
      for (const lane of this.lanes) {
        this.clearFaceState(lane.face)
        this.clearPlateState(lane.plate)
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
  color: var(--text-primary);
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
  background: rgba(15, 124, 130, 0.12);
  color: var(--accent-primary);
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
  border: 1px solid var(--interactive-primary);
  background: var(--interactive-primary);
  font-size: 13px;
  font-weight: 800;
  color: var(--text-on-interactive);
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.12);
  transition: background-color var(--transition-fast);
}

.topbar-settings-btn:hover {
  background: var(--interactive-primary-hover);
}

.topbar-toggle {
  flex-shrink: 0;
  height: 36px;
  padding: 0 12px;
  border-radius: 10px;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  font-size: 13px;
  font-weight: 700;
  color: var(--text-primary);
  cursor: pointer;
  transition: background-color var(--transition-fast), border-color var(--transition-fast);
}

.topbar-toggle:hover {
  border-color: var(--border-focus);
  background: var(--surface-hover);
}

.topbar.compact {
  margin-bottom: 12px;
}

.topbar.compact h1 {
  font-size: 28px;
}

.topbar-desc {
  margin: 5px 0 0;
  color: var(--text-secondary);
  font-size: 13px;
}

.topbar h1 {
  margin: 0;
  font-size: 30px;
  line-height: 1.08;
  font-weight: 900;
  letter-spacing: -0.035em;
  color: var(--text-primary);
}

.gate-layout {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-height: 0;
  flex: 1;
  padding-bottom: calc(136px + env(safe-area-inset-bottom, 0px));
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
  background: var(--surface-raised);
  border: 1px solid var(--border-subtle);
  border-radius: 18px;
  box-shadow: var(--shadow-md);
  backdrop-filter: blur(14px);
  max-height: none;
  overflow: hidden;
  color: var(--text-primary);
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
  background: var(--accent-primary);
}

.btn-main {
  background: var(--interactive-primary);
}

.btn-sub {
  background: var(--interactive-secondary);
}

.btn-off {
  background: var(--status-danger-text);
}

.ops-dock-auto {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
}

.ops-dock-auto-main {
  display: flex;
  align-items: center;
  gap: 14px;
}

.btn-auto {
  min-height: 44px;
  height: auto;
  padding: 0 28px;
  font-size: 15px;
  font-weight: 900;
  letter-spacing: 0.02em;
  border-radius: 12px;
  border: 2px solid var(--interactive-primary);
  background: var(--interactive-primary);
  color: var(--text-on-interactive);
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(15, 124, 130, 0.22);
  transition: background 0.2s ease, transform 0.15s ease;
}

.btn-auto:hover:not(:disabled) {
  background: var(--interactive-primary-hover);
  transform: translateY(-1px);
}

.btn-auto.running {
  background: #dc2626;
  border-color: #b91c1c;
}

.btn-auto:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-auto-spinner {
  width: 14px;
  height: 14px;
  border-radius: 50%;
  border: 2px solid rgba(248, 250, 252, 0.4);
  border-top-color: #f8fafc;
  animation: cam-toolbar-spin 0.7s linear infinite;
  flex-shrink: 0;
}

.auto-status-text {
  max-width: 460px;
  font-size: 12px;
  line-height: 1.4;
  font-weight: 700;
  color: var(--text-secondary);
}

.ops-dock-auto-lanes {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
}

.auto-lane-chip {
  display: inline-flex;
  align-items: center;
  min-height: 26px;
  padding: 0 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 900;
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
  color: var(--text-secondary);
}

.auto-lane-chip.seen {
  background: #fef3c7;
  color: #92400e;
}

.auto-lane-chip.ready {
  background: #93c5fd;
  color: #1e3a8a;
}

.auto-lane-chip.allow {
  background: #dcfce7;
  color: #166534;
}

.auto-lane-chip.deny {
  background: #fee2e2;
  color: #991b1b;
}

.auto-lane-chip.error {
  background: #fed7aa;
  color: #9a3412;
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
  background: rgba(15, 23, 42, 0.55);
  pointer-events: auto;
}

.ops-drawer-panel {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  width: min(440px, 92vw);
  max-width: 100%;
  background: var(--surface-default);
  border-left: 1px solid var(--border-subtle);
  box-shadow: -16px 0 40px rgba(15, 23, 42, 0.18);
  display: flex;
  flex-direction: column;
  min-height: 0;
  pointer-events: auto;
  color: var(--text-primary);
}

.ops-drawer-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 18px;
  border-bottom: 1px solid var(--border-subtle);
  flex-shrink: 0;
}

.ops-drawer-title {
  margin: 0;
  font-size: 20px;
  font-weight: 900;
  color: var(--text-primary);
}

.ops-drawer-close {
  width: 44px;
  height: 44px;
  border: none;
  border-radius: 10px;
  background: var(--surface-subtle);
  color: var(--text-primary);
  font-size: 26px;
  line-height: 1;
  cursor: pointer;
  transition: background-color var(--transition-fast);
}

.ops-drawer-close:hover {
  background: var(--surface-hover);
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
  border: 2px solid var(--border-subtle);
  background: var(--surface-subtle);
  font-size: 15px;
  font-weight: 800;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.ops-drawer-tab.active {
  border-color: var(--border-focus);
  background: var(--surface-selected);
  color: var(--accent-primary);
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
  border: 2px solid var(--border-focus);
  background: var(--surface-subtle);
  box-shadow: var(--shadow-sm);
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
  color: var(--text-primary);
}

.lane-head p {
  margin: 4px 0 0;
  color: var(--text-muted);
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
  background: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.lane-final-status.wait {
  background: var(--status-warning-bg);
  color: var(--status-warning-text);
  border: 1px solid var(--status-warning-border);
}

.ip-row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 0;
}

.lane-direction-setting {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 150px auto;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 12px;
  background: rgba(248, 250, 252, 0.72);
}

.lane-direction-setting label {
  display: block;
  font-size: 13px;
  font-weight: 800;
}

.lane-direction-setting p,
.lane-direction-message {
  margin: 3px 0 0;
  color: var(--text-secondary);
  font-size: 12px;
}

.lane-direction-setting select {
  min-height: 36px;
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  padding: 0 8px;
  background: #fff;
  font-weight: 700;
}

.ip-box label {
  display: block;
  font-size: 12px;
  font-weight: 700;
  margin-bottom: 6px;
  color: var(--text-secondary);
}

.ip-box input {
  width: 100%;
  height: 36px;
  border: 1px solid var(--border-default);
  background: var(--surface-subtle);
  color: var(--text-primary);
  border-radius: 10px;
  padding: 0 12px;
  font-size: 14px;
  outline: none;
  transition: border-color var(--transition-fast);
}

.ip-box input:focus {
  border-color: var(--border-focus);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--border-focus) 20%, transparent);
}

.summary-bar {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 0;
}

.summary-item {
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  padding: 6px 8px;
}

.summary-item .label {
  display: block;
  font-size: 11px;
  color: var(--text-muted);
  margin-bottom: 6px;
}

.summary-item .value {
  display: block;
  font-size: 14px;
  font-weight: 800;
  color: var(--text-primary);
  word-break: break-word;
}

.strong {
  font-size: 16px !important;
  font-weight: 900 !important;
}

.plate {
  color: var(--status-success-text);
  letter-spacing: 1px;
}

.ok-text {
  color: var(--status-success-text);
}

.warn-text {
  color: var(--status-warning-text);
}

.danger-text {
  color: var(--status-danger-text);
}

.cam-block {
  border: 1px solid var(--border-subtle);
  border-radius: 18px;
  padding: 10px;
  background: var(--surface-default);
  color: var(--text-primary);
  min-height: 0;
  display: flex;
  flex-direction: column;
  flex: 1;
  width: 100%;
  position: relative;
  z-index: 6;
  box-shadow: var(--shadow-sm);
}

.cam-block--hero {
  box-shadow: var(--shadow-sm);
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
  color: var(--accent-primary);
  text-transform: uppercase;
}

.cam-kind {
  font-size: 14px;
  font-weight: 900;
  color: var(--text-primary);
}

.mini-status {
  padding: 5px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 900;
}

.mini-status.ok {
  background: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.mini-status.wait {
  background: var(--status-warning-bg);
  color: var(--status-warning-text);
  border: 1px solid var(--status-warning-border);
}

.cam-preview {
  width: 100%;
  flex: 1;
  min-height: 0;
  background: #000000;
  border-radius: 13px;
  overflow: hidden;
  margin-bottom: 7px;
  position: relative;
  border: 1px solid var(--border-subtle);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.preview-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  min-height: 0;
  border: none;
  background: #000000;
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
  color: var(--status-warning-text);
}

.result-hint--waiting {
  color: var(--text-muted);
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

.btn-doc-face {
  background: var(--interactive-primary);
}

.btn-doc-plate {
  background: var(--interactive-secondary);
}

.btn-doc-all {
  background: var(--accent-primary);
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

.cam-preview.flash-allow {
  border-color: #22c55e !important;
  box-shadow: 0 0 0 4px rgba(34, 197, 94, 0.6), 0 0 22px rgba(34, 197, 94, 0.55) !important;
  animation: cam-flash-allow-pulse 0.28s ease 4;
}

.cam-preview.flash-deny {
  border-color: #ef4444 !important;
  box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.65), 0 0 22px rgba(239, 68, 68, 0.6) !important;
  animation: cam-flash-deny-pulse 0.28s ease 4;
}

@keyframes cam-flash-allow-pulse {
  0% { box-shadow: 0 0 0 4px rgba(34, 197, 94, 0.65), 0 0 26px rgba(34, 197, 94, 0.6); }
  50% { box-shadow: 0 0 0 8px rgba(34, 197, 94, 0.95), 0 0 34px rgba(34, 197, 94, 0.8); }
  100% { box-shadow: 0 0 0 4px rgba(34, 197, 94, 0.65), 0 0 26px rgba(34, 197, 94, 0.6); }
}

@keyframes cam-flash-deny-pulse {
  0% { box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.7), 0 0 26px rgba(239, 68, 68, 0.65); }
  50% { box-shadow: 0 0 0 8px rgba(239, 68, 68, 0.98), 0 0 34px rgba(239, 68, 68, 0.85); }
  100% { box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.7), 0 0 26px rgba(239, 68, 68, 0.65); }
}

.auto-error-banner {
  position: absolute;
  left: 14px;
  top: 44px;
  right: 14px;
  z-index: 12;
  padding: 6px 10px;
  border-radius: 8px;
  background: rgba(185, 28, 28, 0.92);
  color: #fff;
  font-size: 12px;
  font-weight: 900;
  line-height: 1.35;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.25);
  pointer-events: none;
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

  .ops-dock {
    position: sticky;
    left: auto;
    right: auto;
    bottom: 8px;
    max-height: none;
    margin-top: 8px;
  }

  .summary-bar,
  .ip-row {
    grid-template-columns: 1fr;
  }

  .lane-direction-setting {
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
  background: var(--surface-default);
  border: 1px solid var(--border-default);
  color: var(--text-primary);
  border-radius: var(--radius-control, 8px);
  width: 100%;
  max-height: 200px;
  overflow-y: auto;
  z-index: 9999;
  box-shadow: var(--shadow-md);
}

.dropdown-item {
  padding: 8px 12px;
  cursor: pointer;
  color: var(--text-primary);
  transition: background-color var(--transition-fast);
}

.dropdown-item:hover {
  background: var(--surface-hover);
}

.sim-panel {
  position: fixed;
  right: 12px;
  bottom: 12px;
  z-index: 1200;
  width: 360px;
  max-width: calc(100vw - 24px);
  background: #1e2430;
  color: #e8e8e8;
  border: 1px solid #3a4253;
  border-radius: 10px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.45);
  font-size: 12px;
}

.sim-panel-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 12px;
  cursor: pointer;
  border-bottom: 1px solid #3a4253;
  user-select: none;
}

.sim-badge {
  background: #f59e0b;
  color: #111;
  font-weight: 700;
  padding: 1px 7px;
  border-radius: 5px;
  letter-spacing: 1px;
}

.sim-caret {
  margin-left: auto;
}

.sim-panel-body {
  padding: 10px 12px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 62vh;
  overflow-y: auto;
}

.sim-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
}

.sim-row label {
  width: 100%;
  color: #9aa4b8;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-size: 10px;
}

.sim-row button {
  background: #2c3547;
  color: #e8e8e8;
  border: 1px solid #46506a;
  border-radius: 6px;
  padding: 4px 9px;
  cursor: pointer;
  font-size: 12px;
}

.sim-row button:hover:not(:disabled) {
  background: #3a455c;
}

.sim-row button:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.sim-btn-danger {
  border-color: #b2544a !important;
  color: #ff8f87 !important;
}

.sim-row input,
.sim-row select {
  background: #14181f;
  color: #e8e8e8;
  border: 1px solid #3a4253;
  border-radius: 6px;
  padding: 4px 7px;
  font-size: 12px;
  max-width: 130px;
}

.sim-ok {
  color: #4ade80;
}

.sim-warn {
  color: #fbbf24;
}

.sim-log {
  background: #10141b;
  border: 1px solid #2c3547;
  border-radius: 6px;
  padding: 6px 8px;
  min-height: 48px;
  max-height: 130px;
  overflow-y: auto;
  white-space: pre-wrap;
  word-break: break-all;
  font-family: Consolas, monospace;
  font-size: 11px;
  color: #aab4c8;
  margin: 0;
}
</style>
