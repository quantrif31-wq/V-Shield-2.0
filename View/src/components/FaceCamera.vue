<template>
  <div class="page-container animate-in">
    <header class="page-header">
      <div>
        <span class="panel-kicker">Giám sát an ninh</span>
        <h1 class="page-title">Giám sát FaceID</h1>
        <p class="page-subtitle">Nhận diện khuôn mặt realtime, cấu hình luồng camera và theo dõi quyết định truy cập.</p>
      </div>
    </header>

    <div class="page-tabs">
      <button
        class="page-tab"
        :class="{ active: activeTab === 'monitor' }"
        @click="switchTab('monitor')"
      >
        Giám sát FaceID
      </button>
      <button
        class="page-tab"
        :class="{ active: activeTab === 'enroll' }"
        @click="switchTab('enroll')"
      >
        Thu thập mẫu nhận diện
      </button>
    </div>

    <div class="face-monitor-grid" v-show="activeTab === 'monitor'">
      <!-- Left Column: Video Stream & Evidence -->
      <div class="monitor-left-col">
        <section class="card panel video-card">
          <div class="panel-head">
            <h2 class="panel-title">Xem trực tuyến (Live Preview)</h2>
          </div>

          <div class="video-wrapper" ref="videoWrapperRef" @dblclick="handleDoubleClick" @contextmenu="handleRightClick">
            <iframe
              v-if="previewRunning && directCameraUrl"
              :key="directCameraKey"
              :src="directCameraUrl"
              class="video"
              title="Xem trước camera"
              allow="autoplay; fullscreen"
              frameborder="0"
              @load="handleDirectPreviewLoaded"
            ></iframe>
            <div v-else class="video-off">
              Cam ngoại tuyến
            </div>
          </div>
        </section>

        <section class="card panel evidence-panel-card">
          <div class="panel-head">
            <h2 class="panel-title">Ảnh chụp bằng chứng</h2>
          </div>

          <div class="evidence-grid">
            <div class="evidence-item">
              <span class="evidence-label">Ảnh chụp toàn khung</span>
              <img
                v-if="lockedSnapshot"
                :src="lockedSnapshot"
                class="evidence-image"
                alt="Ảnh chụp khóa"
              />
              <div v-else class="evidence-empty">Chưa có ảnh</div>
            </div>

            <div class="evidence-item">
              <span class="evidence-label">Ảnh crop khuôn mặt</span>
              <img
                v-if="lockedFaceCrop"
                :src="lockedFaceCrop"
                class="evidence-image"
                alt="Ảnh crop khuôn mặt khóa"
              />
              <div v-else class="evidence-empty">Chưa có ảnh</div>
            </div>
          </div>
        </section>
      </div>

      <!-- Right Column: Controls & Realtime Status -->
      <div class="monitor-right-col">
        <section class="card panel config-card">
          <div class="panel-head">
            <h2 class="panel-title">Cấu hình & Kết nối</h2>
          </div>

          <div class="config-row">
            <label class="field-label">Tìm kiếm camera</label>
            <div class="search-box">
              <input
                v-model="cameraSearch"
                type="text"
                class="form-input ip-input"
                placeholder="Nhập tên / ID camera để tìm..."
                :disabled="loading"
                @focus="cameraOpen = true"
                @blur="cameraOpen = false"
              />
              <div v-if="cameraOpen && cameraDropdownMatches.length" class="dropdown">
                <div
                  v-for="cam in cameraDropdownMatches"
                  :key="cam.cameraId"
                  class="dropdown-item"
                  @mousedown.prevent
                  @click="selectCamera(cam)"
                >
                  <span class="dropdown-item-name">{{ cam.cameraName }}</span>
                  <span class="dropdown-item-meta">ID: {{ cam.cameraId }}{{ cam.gateName ? ' · ' + cam.gateName : '' }}</span>
                </div>
              </div>
              <div v-else-if="camerasLoading" class="dropdown-hint">Đang tải camera...</div>
              <div v-else-if="cameraOpen && !allCameras.length" class="dropdown-hint">Chưa có camera nào trong hệ thống.</div>
            </div>
          </div>

          <div v-if="selectedConfiguration" class="config-meta-box">
            <div class="meta-item"><span>Camera đã cấu hình:</span> <strong>{{ selectedConfiguration.cameraName }}</strong></div>
            <div class="meta-item"><span>Trạng thái:</span> <strong class="badge info">{{ selectedConfiguration.runtimeStatus }}</strong></div>
            <div class="meta-item auto-restore-row">
              <label class="auto-restore">
                <input
                  type="checkbox"
                  :checked="selectedConfiguration.autoRestore"
                  :disabled="loading"
                  @change="handleAutoRestoreChange"
                />
                Tự động khôi phục (Auto restore)
              </label>
            </div>
            <div v-if="selectedConfiguration.lastSyncError" class="sync-error-text">
              {{ selectedConfiguration.lastSyncError }}
            </div>
          </div>

          <div class="control-actions-grid">
            <button class="btn btn-primary" @click="handleTurnOnPreview" :disabled="loading">
              {{ loading ? "Đang xử lý..." : "Bật preview" }}
            </button>

            <button
              class="btn btn-primary"
              @click="handleInitOrResetSession"
              :disabled="loading || (!selectedConfiguration && !cameraIp.trim())"
            >
              {{ loading ? "Đang xử lý..." : sessionActionLabel }}
            </button>

            <button class="btn btn-outline" @click="handleTurnOff" :disabled="loading">
              {{ loading ? "Đang xử lý..." : "Tắt camera" }}
            </button>

            <button class="btn btn-secondary" @click="handleCheckModels" :disabled="loading">
              {{ loading ? "Đang xử lý..." : "Kiểm tra model" }}
            </button>
          </div>

          <div v-if="faceServiceError" class="service-error-box alert-danger-soft" role="status">
            {{ faceServiceError.message }}
          </div>

          <div v-if="modelInfo" class="model-status-box alert-info-soft">
            <div><span>Phiên bản model:</span> <strong>{{ modelInfo.version }}</strong></div>
            <div><span>Số model:</span> <strong>{{ modelInfo.modelCount }}</strong></div>
            <div><span>Số encoding:</span> <strong>{{ modelInfo.encodingCount }}</strong></div>
          </div>
        </section>

        <section class="card panel live-state-card">
          <div class="panel-head">
            <h2 class="panel-title">Kết quả nhận diện Realtime</h2>
          </div>

          <div class="status-grid">
            <div class="status-badge-item">
              <span class="label">Camera:</span>
              <span class="badge" :class="cameraRunning ? 'success' : 'neutral'">{{ cameraRunning ? "Đang chạy" : "Đang tắt" }}</span>
            </div>
            <div class="status-badge-item">
              <span class="label">Kết nối:</span>
              <span class="badge" :class="cameraConnected ? 'success' : 'danger'">{{ cameraConnected ? "Đã kết nối" : "Chưa kết nối" }}</span>
            </div>
            <div class="status-badge-item">
              <span class="label">Xem trước:</span>
              <span class="badge" :class="previewRunning && previewHealthy ? 'success' : 'warn'">{{ previewRunning ? (previewHealthy ? "Đang hiển thị" : "Đang kết nối") : "Đang tắt" }}</span>
            </div>
            <div class="status-badge-item">
              <span class="label">FPS:</span>
              <span class="badge info">{{ fps }}</span>
            </div>
          </div>

          <div class="face-result-boxes">
            <div class="face-result-box">
              <div class="face-result-label">Mã nhân viên</div>
              <div class="face-result-value" :class="{ confirmed: employeeId }">
                {{ employeeId || "-----" }}
              </div>
            </div>

            <div class="face-result-box">
              <div class="face-result-label">Trạng thái nhận dạng</div>
              <div class="face-result-value" :class="scanLocked ? 'locked' : trackingActive ? 'tracking' : 'idle'">
                {{ detectionLabel }}
              </div>
            </div>
          </div>

          <div class="lock-banner-alert alert-warn-soft" v-if="scanLocked">
            🔒 Đã có kết quả cuối và khóa phiên. Bấm "{{ sessionActionLabel }}" để quét người tiếp theo.
          </div>

          <div class="alert-banner-danger alert-danger-soft" v-if="alert">
            🚨 CẢNH BÁO: Người lạ / không xác nhận được danh tính
          </div>

          <div class="detail-state-list">
            <div class="detail-state-item">
              <span>Khớp khuôn mặt</span>
              <strong>{{ faceMatch ? "Có" : "Không" }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Đã xác nhận danh tính</span>
              <strong>{{ identityConfirmed ? "Có" : "Không" }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Số lần xác nhận</span>
              <strong>{{ confirmCount }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Khoảng cách</span>
              <strong>{{ distanceText }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Lý do khóa</span>
              <strong>{{ lockReason || "-----" }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Khung giới hạn</span>
              <strong class="font-mono">{{ bboxText }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Thông điệp</span>
              <strong>{{ message || "-----" }}</strong>
            </div>

            <div class="detail-state-item">
              <span>Cập nhật cuối</span>
              <strong>{{ lastUpdate || "-----" }}</strong>
            </div>
          </div>
        </section>
      </div>
    </div>

    <!-- History Sections -->
    <section class="card panel history-section-card" v-show="activeTab === 'monitor'">
      <div class="panel-head flex-head">
        <div>
          <span class="panel-kicker">Nhật ký hệ thống</span>
          <h2 class="panel-title">Lịch sử nhận diện gần đây</h2>
        </div>
        <div class="history-badges-row">
          <span v-if="eventHistoryError" class="badge danger">
            Trình thu thập/collector không khả dụng
          </span>
          <span v-if="collectorGap" class="badge warning">
            Lịch sử có thể bị thiếu
          </span>
        </div>
      </div>

      <div class="toolbar-shell select-filter-bar">
        <div class="toolbar-filters">
          <input v-model.trim="eventFilters.cameraId" class="form-input" placeholder="Mã Camera ID..." />
          <input v-model.trim="eventFilters.employeeId" class="form-input" type="number" min="1" placeholder="Mã nhân viên (Employee ID)..." />
          <input v-model="eventFilters.fromUtc" class="date-input" type="datetime-local" />
          <button class="btn btn-secondary btn-sm" @click="loadRecognitionEvents">Lọc / làm mới</button>
        </div>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Thời gian</th>
              <th>Camera / Làn</th>
              <th>Nhân viên</th>
              <th>Khoảng cách</th>
              <th>Model</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in recognitionEvents" :key="item.id">
              <td>{{ formatEventTime(item.occurredAtUtc) }}</td>
              <td>{{ item.cameraId }} / {{ item.laneId ?? "-----" }}</td>
              <td>{{ item.employeeName || item.employeeId || item.runtimeSubjectId || "-----" }}</td>
              <td>{{ formatEventDistance(item.recognitionDistance) }}</td>
              <td>v{{ item.modelVersion ?? "-----" }}</td>
              <td>
                <span class="badge" :class="eventStatusClass(item.matchStatus)">
                  {{ item.matchStatus }}
                </span>
              </td>
            </tr>
            <tr v-if="!recognitionEvents.length">
              <td colspan="6" class="text-center text-muted">Chưa có sự kiện nhận diện.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card panel history-section-card" v-show="activeTab === 'monitor'">
      <div class="panel-head">
        <div>
          <span class="panel-kicker">Đánh giá chính sách</span>
          <h2 class="panel-title">So sánh chính sách truy cập</h2>
        </div>
      </div>
      <div class="alert-banner-warning alert-warn-soft">
        ⚠️ Kết quả so sánh chỉ phục vụ đánh giá chính sách. Không phải quyết định mở cổng.
      </div>
      <div class="comparison-summary-row">
        <span class="soft-chip">Đồng thuận cho phép: <strong>{{ comparisonSummary.agreeAllow || 0 }}</strong></span>
        <span class="soft-chip">Đồng thuận từ chối: <strong>{{ comparisonSummary.agreeDeny || 0 }}</strong></span>
        <span class="soft-chip warn">Legacy cho phép / Enterprise từ chối: <strong>{{ comparisonSummary.legacyAllowEnterpriseDeny || 0 }}</strong></span>
        <span class="soft-chip warn">Legacy từ chối / Enterprise cho phép: <strong>{{ comparisonSummary.legacyDenyEnterpriseAllow || 0 }}</strong></span>
        <span class="soft-chip">Không đủ dữ liệu: <strong>{{ comparisonSummary.indeterminate || 0 }}</strong></span>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Thời gian</th>
              <th>Camera / Cổng / AP</th>
              <th>Legacy</th>
              <th>Enterprise</th>
              <th>So sánh</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in policyComparisons" :key="item.id">
              <td>{{ formatEventTime(item.occurredAtUtc) }}</td>
              <td>{{ item.cameraId }} / {{ item.gateId ?? "-" }} / {{ item.accessPointId ?? "-" }}</td>
              <td>{{ item.legacyDecision }} — {{ item.legacyReasonCode }}</td>
              <td>{{ item.enterpriseDecision }} — {{ item.enterpriseReasonCode }}</td>
              <td>{{ item.comparisonResult }}</td>
            </tr>
            <tr v-if="!policyComparisons.length">
              <td colspan="5" class="text-center text-muted">Chưa có dữ liệu so sánh.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card panel history-section-card" v-show="activeTab === 'monitor'">
      <div class="panel-head">
        <div>
          <span class="panel-kicker">Nhật ký audit</span>
          <h2 class="panel-title">Quyết định truy cập Face ID</h2>
        </div>
      </div>
      <div class="alert-banner-warning alert-warn-soft">
        ⚠️ Allowed chỉ là quyết định phần mềm phục vụ kiểm tra và audit. Không phải lệnh mở cổng.
      </div>
      <div class="comparison-summary-row">
        <span class="soft-chip success">Được phép: <strong>{{ decisionSummary.allowed || 0 }}</strong></span>
        <span class="soft-chip danger">Bị từ chối: <strong>{{ decisionSummary.denied || 0 }}</strong></span>
        <span class="soft-chip warn">Cần rà soát: <strong>{{ decisionSummary.reviewRequired || 0 }}</strong></span>
        <span class="soft-chip">Không xác định: <strong>{{ decisionSummary.indeterminate || 0 }}</strong></span>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Thời gian</th>
              <th>Camera / Cổng / AP</th>
              <th>Quyết định</th>
              <th>Lý do</th>
              <th>Đầu vào</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in accessDecisions" :key="item.id">
              <td>{{ formatEventTime(item.occurredAtUtc) }}</td>
              <td>{{ item.cameraId }} / {{ item.gateId ?? "-" }} / {{ item.accessPointId ?? "-" }}</td>
              <td>
                <span class="badge" :class="decisionStatusClass(item.decision)">
                  {{ item.decision }}
                </span>
              </td>
              <td>{{ item.reasonCode }}</td>
              <td>{{ item.legacyDecision }} VÀ {{ item.enterpriseDecision }}</td>
            </tr>
            <tr v-if="!accessDecisions.length">
              <td colspan="5" class="text-center text-muted">Chưa có quyết định truy cập.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card panel enroll-card" v-show="activeTab === 'enroll'">
      <div class="panel-head flex-head">
        <div>
          <span class="panel-kicker">Đăng ký mẫu</span>
          <h2 class="panel-title">Đăng ký khuôn mặt</h2>
          <p class="panel-subtitle">
            Chọn camera, nhấn Bắt đầu, rồi quay đầu nhẹ theo hướng dẫn tới khi đủ 5 góc.
          </p>
        </div>
        <span v-if="enrollStep !== 'capture'" class="badge info">5 góc: thẳng · trái · phải · lên · xuống</span>
        <span v-else class="badge warn">Đã thu: {{ guidedSamples }} mẫu · {{ guidedDurationText }}</span>
      </div>

      <div class="enroll-grid">
        <div class="enroll-preview-col">
          <div class="enroll-video-wrapper" :class="enrollOverlayClass">
            <iframe
              v-if="directCameraUrl"
              :key="'enroll-preview-' + directCameraKey"
              :src="directCameraUrl"
              class="enroll-video"
              title="Xem trước camera"
              allow="autoplay; fullscreen"
              frameborder="0"
            ></iframe>
            <div v-else class="enroll-video-off">
              Chọn camera ở ô bên phải để xem preview
            </div>

            <div v-if="enrollStep === 'capture' && guidedErrorMessage" class="guided-error-banner">
              ⚠ {{ guidedErrorMessage }}
            </div>

            <div v-if="enrollStep === 'capture'" class="guided-overlay">
              <div class="guided-arrow" :class="enrollArrowClass">{{ enrollArrowGlyph }}</div>
              <div class="guided-prompt">{{ guidedGuidance }}</div>
            </div>

            <div v-if="enrollStep === 'capture'" class="guided-grid">
              <div
                v-for="bin in guidedGridBins"
                :key="bin.key"
                class="guided-cell"
                :class="bin.class"
              >
                {{ bin.label }}
              </div>
            </div>
          </div>
        </div>

        <div class="enroll-form-col">
          <template v-if="enrollStep !== 'done'">
            <label class="field-label">Camera</label>
            <div class="search-box">
              <input
                v-model="cameraSearch"
                type="text"
                class="form-input"
                placeholder="Nhập tên / ID camera để tìm..."
                :disabled="enrollStep === 'capture'"
                @focus="cameraOpen = true"
                @blur="cameraOpen = false"
              />
              <div v-if="cameraOpen && cameraDropdownMatches.length" class="dropdown">
                <div
                  v-for="cam in cameraDropdownMatches"
                  :key="cam.cameraId"
                  class="dropdown-item"
                  @mousedown.prevent
                  @click="selectEnrollCamera(cam)"
                >
                  <span class="dropdown-item-name">{{ cam.cameraName }}</span>
                  <span class="dropdown-item-meta">ID: {{ cam.cameraId }}{{ cam.gateName ? ' · ' + cam.gateName : '' }}</span>
                </div>
              </div>
              <div v-else-if="camerasLoading" class="dropdown-hint">Đang tải camera...</div>
              <div v-else-if="cameraOpen && !allCameras.length" class="dropdown-hint">Chưa có camera nào.</div>
            </div>

            <div v-if="selectedEnrollCamera" class="enroll-source-badge ok">
              <span>Camera:</span> <strong>{{ selectedEnrollCamera.cameraName }}</strong>
              <span class="enroll-source-id">({{ selectedEnrollCamera.cameraId }})</span>
            </div>

            <div v-if="enrollStep === 'capture'" class="enroll-progress-box">
              <div class="enroll-progress-label">
                <span>Còn thiếu {{ guidedMissingAngles.length }}/5 góc:</span>
                <strong>{{ guidedMissingAngleText || 'Đã đủ 5 góc!' }}</strong>
              </div>
              <div class="enroll-progress-track">
                <div class="enroll-progress-bar" :style="{ width: enrollProgressPercent + '%' }"></div>
              </div>
              <div class="enroll-progress-hint">
                Đã đủ 5 góc — có thể bấm Xác nhận, hoặc quay tiếp để cải thiện mẫu.
              </div>
            </div>

            <div class="enroll-button-row">
              <button
                v-if="enrollStep !== 'capture'"
                class="btn btn-primary enroll-big-btn"
                :disabled="!selectedEnrollCamera || enrollSending"
                @click="handleStartGuidedEnroll"
              >
                {{ enrollSending ? "Đang chuẩn bị..." : "Bắt đầu đăng ký" }}
              </button>
              <button
                v-else
                class="btn btn-outline"
                @click="handleStopGuidedEnroll"
              >
                Dừng
              </button>
              <button
                v-if="enrollStep === 'capture' && guidedAnglesComplete"
                class="btn btn-primary enroll-big-btn"
                :disabled="enrollSending"
                @click="openConfirmEnroll"
              >
                Xác nhận & gán đối tượng
              </button>
              <button
                v-if="enrollStep !== 'capture' && selectedEnrollCamera"
                class="btn btn-outline"
                @click="clearEnrollCamera"
                :disabled="enrollSending"
              >
                Bỏ chọn
              </button>
            </div>

            <div v-if="enrollError" class="service-error-box alert-danger-soft">
              {{ enrollError }}
            </div>
          </template>

          <template v-else>
            <h3 class="enroll-done-title">Đã thu đủ 5 góc ✓</h3>
            <p class="enroll-done-hint">Nhập mã đối tượng để gắn mẫu nhận diện:</p>

            <label class="field-label">{{ enrollSubjectType === 'employee' ? 'Mã nhân viên (Employee ID)' : 'Mã khách (Guest ID)' }}</label>
            <input
              v-model.trim="enrollSubjectId"
              type="number"
              min="1"
              class="form-input"
              :placeholder="enrollSubjectType === 'employee' ? 'VD: 1001' : 'VD: 9001'"
              :disabled="enrollSending"
            />

            <label v-if="enrollSubjectType === 'guest'" class="field-label">Tên khách (tùy chọn)</label>
            <input
              v-if="enrollSubjectType === 'guest'"
              v-model.trim="enrollDisplayName"
              type="text"
              class="form-input"
              placeholder="VD: Nguyễn Văn A"
              :disabled="enrollSending"
            />

            <div class="enroll-submit-row">
              <button
                class="btn btn-primary enroll-big-btn"
                :disabled="!enrollSubjectId || enrollSending"
                @click="handleConfirmGuidedEnroll"
              >
                {{ enrollSending ? "Đang lưu mẫu..." : "Xác nhận & lưu mẫu" }}
              </button>
              <button
                class="btn btn-outline"
                :disabled="enrollSending"
                @click="resetGuidedEnroll"
              >
                Làm lại
              </button>
            </div>

            <div v-if="enrollError" class="service-error-box alert-danger-soft">
              {{ enrollError }}
            </div>

            <div v-if="enrollResult" class="enroll-result alert-success-soft">
              <div><span>Đối tượng:</span> <strong>{{ enrollSubjectType === 'employee' ? 'Nhân viên' : 'Khách' }} {{ enrollResult.subjectId }}</strong></div>
              <div><span>Số mẫu dùng được:</span> <strong>{{ enrollResult.encodingCount }}</strong></div>
              <div><span>File model:</span> <strong class="font-mono">{{ enrollResult.modelFileName }}</strong></div>
              <div><span>Phiên bản registry:</span> <strong>v{{ enrollResult.registryVersion }}</strong></div>
              <div class="enroll-result-hint">
                Đã lưu mẫu nhận diện. Chuyển sang tab "Giám sát FaceID" để quét thử.
              </div>
              <button class="btn btn-primary btn-sm enroll-again-btn" @click="resetGuidedEnroll">Thu thập mẫu khác</button>
            </div>
          </template>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
import {
  startCamera,
  stopCamera,
  resetCamera,
  getCameraStatus,
  getCameraResult,
  getLockedImages,
  getModels,
  liveEnroll,
  normalizeFaceApiError,
  shouldStopFacePolling
} from "../services/faceApi"
import { ensureCameraRegistered, getCameras } from "../services/cameraRuntimeApi"
import { guidedStart, guidedProgress, guidedStop, guidedConfirm } from "../services/guidedEnrollmentApi"
import {
  getFaceCameraConfigurations,
  updateFaceCameraConfiguration,
  startConfiguredFaceCamera,
  stopConfiguredFaceCamera,
  reconcileFaceCameras
} from "../services/faceCameraConfigurationApi"
import {
  getFaceRecognitionEvents,
  getFaceRecognitionCollectorHealth
} from "../services/faceRecognitionEventsApi"
import {
  getFacePolicyComparisons,
  getFacePolicyComparisonSummary
} from "../services/faceAccessPolicyComparisonApi"
import {
  getFaceAccessDecisions,
  getFaceAccessDecisionSummary
} from "../services/faceAccessDecisionApi"
import { captureError, recordMetric } from "../services/observability"

export default {
  name: "FaceIdSecurity",

  props: {
    cameraId: {
      type: String,
      default: "monitoring-face-camera"
    },
    laneId: {
      type: String,
      default: null
    }
  },

  data() {
    return {
      cameraIp: "",
      savedConfigurations: [],
      selectedRuntimeCameraId: "",
      currentIp: "",
      cameraRunning: false,
      cameraConnected: false,
      previewRunning: false,
      loading: false,

      allCameras: [],
      cameraSearch: "",
      cameraOpen: false,
      camerasLoading: false,

      employeeId: "",
      trackingActive: false,
      identityConfirmed: false,
      faceMatch: false,
      confirmCount: 0,
      distance: null,
      bbox: null,
      timeoutState: false,
      alert: false,

      lockedSnapshot: "",
      lockedFaceCrop: "",
      scanLocked: false,
      lockReason: "",

      fps: 0,
      message: "",
      lastUpdate: "",
      faceServiceError: null,
      modelInfo: null,

      directCameraUrl: "",
      directCameraSourceUrl: "",
      directCameraKey: 0,
      previewHealthy: false,
      previewRetryCount: 0,
      previewRetryTimer: null,

      resultTimer: null,
      busyResult: false,
      isFetchingLockedImages: false,
      recognitionEvents: [],
      eventHistoryTimer: null,
      eventHistoryError: false,
      collectorGap: false,
      eventFilters: { cameraId: "", employeeId: "", fromUtc: "" },
      policyComparisons: [],
      comparisonSummary: {},
      accessDecisions: [],
      decisionSummary: {},

      activeTab: "monitor",
      enrollStep: "idle", // idle | capture | done
      selectedEnrollCamera: null,
      guidedProgress: 0,
      guidedTotal: 5,
      guidedGuidance: "",
      guidedStatus: "idle",
      guidedFaceState: "none", // none | single | multiple
      guidedCoveredAngles: [],
      guidedMissingAngles: [],
      guidedAnglesComplete: false,
      guidedSamples: 0,
      guidedDurationMs: 0,
      guidedBins: [],
      guidedGridBins: [],
      guidedPollTimer: null,
      guidedArrow: "none",
      enrollSubjectType: "employee",
      enrollSubjectId: "",
      enrollDisplayName: "",
      enrollSending: false,
      enrollResult: null,
      enrollError: "",

      destroyed: false
    }
  },

  computed: {
    selectedConfiguration() {
      return this.savedConfigurations.find(
        item => item.runtimeCameraId === this.selectedRuntimeCameraId
      ) || null
    },

    activeCameraId() {
      return this.selectedRuntimeCameraId || this.cameraId
    },

    safeInputUrl() {
      const value = String(
        this.selectedConfiguration?.streamUrlMasked || this.currentIp || this.cameraIp || ""
      ).trim()
      if (!value) return "-----"
      try {
        const parsed = new URL(value)
        if (parsed.username || parsed.password) {
          parsed.username = "***"
          parsed.password = "***"
        }
        return parsed.toString()
      } catch {
        return "Camera stream đã cấu hình"
      }
    },

    bboxText() {
      if (!this.bbox) return "-----"

      return `left=${this.bbox.left}, top=${this.bbox.top}, right=${this.bbox.right}, bottom=${this.bbox.bottom}`
    },

    sessionActionLabel() {
      return this.cameraRunning ? "Reset phiên nhận diện" : "Khởi tạo phiên nhận diện"
    },

    detectionLabel() {
      if (this.scanLocked) {
        if (this.lockReason === "confirmed") return "LOCKED - IDENTIFIED"
        if (this.lockReason === "timeout") return "LOCKED - TIMEOUT"
        if (this.lockReason === "alert") return "LOCKED - ALERT"
        return "LOCKED"
      }

      if (!this.trackingActive) return "IDLE"
      if (this.identityConfirmed) return "IDENTIFIED"
      if (this.faceMatch) return "VERIFYING"
      return "UNKNOWN"
    },

    distanceText() {
      const num = Number(this.distance)
      if (Number.isNaN(num)) return "-----"
      return num.toFixed(4)
    },

    enrollProgressPercent() {
      if (!this.guidedTotal) return 0
      return Math.min(100, Math.round((this.guidedProgress / this.guidedTotal) * 100))
    },

    guidedMissingAngleText() {
      if (!this.guidedMissingAngles.length) return ""
      const labels = {
        straight: "Thẳng",
        left: "Trái",
        right: "Phải",
        up: "Lên",
        down: "Xuống"
      }
      return this.guidedMissingAngles.map(a => labels[a] || a).join(", ")
    },

    guidedDurationText() {
      const seconds = Math.round(this.guidedDurationMs / 1000)
      return `${seconds}s`
    },

    cameraDropdownMatches() {
      const keyword = String(this.cameraSearch || "").trim().toLowerCase()
      let list = Array.isArray(this.allCameras) ? this.allCameras : []
      if (keyword) {
        list = list.filter(cam =>
          String(cam.cameraName || "").toLowerCase().includes(keyword) ||
          String(cam.cameraId || "").includes(keyword)
        )
      }
      return list.slice(0, 5)
    },

    guidedOverlayClass() {
      if (this.enrollStep !== "capture") return "overlay-off"
      // Red border when no face or multiple faces; green when exactly one.
      if (this.guidedFaceState === "none" || this.guidedFaceState === "multiple") {
        return "overlay-danger"
      }
      if (this.guidedAnglesComplete) return "overlay-ok"
      return "overlay-wait"
    },

    guidedErrorMessage() {
      if (this.enrollStep !== "capture") return ""
      if (this.guidedFaceState === "none") {
        return "Không phát hiện khuôn mặt — hãy bước vào trước camera"
      }
      if (this.guidedFaceState === "multiple") {
        return "Phát hiện nhiều khuôn mặt — chỉ để lại 1 người trong khung"
      }
      return ""
    },

    enrollArrowClass() {
      if (this.guidedArrow === "left") return "arrow-left"
      if (this.guidedArrow === "right") return "arrow-right"
      if (this.guidedArrow === "up") return "arrow-up"
      if (this.guidedArrow === "down") return "arrow-down"
      return "arrow-center"
    },

    enrollArrowGlyph() {
      if (this.guidedArrow === "left") return "◀"
      if (this.guidedArrow === "right") return "▶"
      if (this.guidedArrow === "up") return "▲"
      if (this.guidedArrow === "down") return "▼"
      return "●"
    }
  },

  async mounted() {
    this.destroyed = false
    await this.loadAllCameras()
    await this.loadSavedConfigurations()
    if (!this.selectedConfiguration || this.selectedConfiguration.runtimeEnabled) {
      await this.loadCurrentStatus()
    }
    if (this.selectedConfiguration?.previewUrl && !this.previewRunning) {
      this.mountRegisteredPreview(
        { urlView: this.selectedConfiguration.previewUrl },
        ""
      )
    }

    if (this.cameraRunning) {
      this.startResultLoop()
    }
    await this.loadRecognitionEvents()
    this.startEventHistoryLoop()
  },

  beforeUnmount() {
    this.destroyed = true
    this.stopResultLoop()
    this.stopEventHistoryLoop()
    this.resetDirectPreview()
    this.stopGuidedPolling()
  },

  activated() {
    // Resuming from keep-alive: restart timers if camera was running
    this.destroyed = false
    this.startEventHistoryLoop()
    if (this.cameraRunning) {
      if (this.currentIp && !this.previewRunning) {
        this.mountDirectPreview(this.currentIp)
      }
      this.startResultLoop()
    }
  },

  deactivated() {
    // Pausing for keep-alive: stop timers but keep state
    this.stopResultLoop()
    this.stopEventHistoryLoop()
    this.stopGuidedPolling()
  },

  methods: {
    async loadAllCameras() {
      if (this.destroyed) return
      this.camerasLoading = true
      try {
        const list = await getCameras()
        this.allCameras = Array.isArray(list) ? list : []
      } catch (error) {
        console.warn("Không tải được danh sách camera:", error)
        this.allCameras = []
      } finally {
        this.camerasLoading = false
      }
    },

    async selectCamera(cam) {
      if (!cam) return
      this.cameraIp = cam.streamUrl || cam.urlView || ""
      this.cameraSearch = cam.cameraName || ""
      this.cameraOpen = false
      // Detect whether this camera matches a saved Face configuration.
      const match = this.savedConfigurations.find(
        item => item.cameraId === cam.cameraId || item.cameraName === cam.cameraName
      )
      if (match) {
        this.selectedRuntimeCameraId = match.runtimeCameraId
      } else {
        this.selectedRuntimeCameraId = ""
      }
      // Register + open preview via go2rtc.
      try {
        const camera = await ensureCameraRegistered({
          cameraName: cam.cameraName || "Face Monitor Camera",
          cameraType: "Face",
          streamUrl: cam.streamUrl || cam.urlView || "",
        })
        if (camera?.urlView) {
          this.mountRegisteredPreview(camera, cam.streamUrl || "")
        }
      } catch (e) {
        console.warn("selectCamera preview error:", e)
      }
      this.message = `Đã chọn camera: ${cam.cameraName || ""} (ID: ${cam.cameraId || ""})`
    },
    async loadRecognitionEvents() {
      if (this.destroyed) return
      try {
        const params = { page: 1, pageSize: 50 }
        if (this.eventFilters.cameraId) params.cameraId = this.eventFilters.cameraId
        if (this.eventFilters.employeeId) params.employeeId = Number(this.eventFilters.employeeId)
        if (this.eventFilters.fromUtc) {
          params.fromUtc = new Date(this.eventFilters.fromUtc).toISOString()
        }
        const [history, health] = await Promise.all([
          getFaceRecognitionEvents(params),
          getFaceRecognitionCollectorHealth()
        ])
        this.recognitionEvents = Array.isArray(history?.items) ? history.items : []
        this.collectorGap = Number(health?.gapCount || 0) > 0 ||
          this.recognitionEvents.some(item => item.historyGapWarning)
        this.eventHistoryError = false
        const [comparisons, summary, decisions, decisionSummary] = await Promise.all([
          getFacePolicyComparisons({ page: 1, pageSize: 50 }),
          getFacePolicyComparisonSummary(),
          getFaceAccessDecisions({ page: 1, pageSize: 50 }),
          getFaceAccessDecisionSummary()
        ])
        this.policyComparisons = Array.isArray(comparisons?.items) ? comparisons.items : []
        this.comparisonSummary = summary || {}
        this.accessDecisions = Array.isArray(decisions?.items) ? decisions.items : []
        this.decisionSummary = decisionSummary || {}
      } catch {
        this.eventHistoryError = true
      }
    },

    startEventHistoryLoop() {
      this.stopEventHistoryLoop()
      this.eventHistoryTimer = setInterval(() => this.loadRecognitionEvents(), 5000)
    },

    stopEventHistoryLoop() {
      if (this.eventHistoryTimer) clearInterval(this.eventHistoryTimer)
      this.eventHistoryTimer = null
    },

    formatEventTime(value) {
      return value ? new Date(value).toLocaleString("vi-VN") : "-----"
    },

    formatEventDistance(value) {
      const number = Number(value)
      return Number.isFinite(number) ? number.toFixed(4) : "-----"
    },

    eventStatusClass(status) {
      if (status === "Matched") return "success"
      if (status === "ModelMismatch" || status === "EmployeeMissing") {
        return "danger"
      }
      return "warning"
    },

    decisionStatusClass(status) {
      if (status === "Allowed") return "success"
      if (status === "Denied" || status === "Indeterminate") {
        return "danger"
      }
      return "warning"
    },

    async loadSavedConfigurations() {
      try {
        const overview = await getFaceCameraConfigurations()
        this.savedConfigurations = Array.isArray(overview?.configurations)
          ? overview.configurations
          : []
        if (!this.selectedRuntimeCameraId && this.savedConfigurations.length) {
          this.selectedRuntimeCameraId = this.savedConfigurations[0].runtimeCameraId
        }
      } catch (error) {
        this.handleFaceServiceError(error, { polling: true })
      }
    },

    async handleConfiguredCameraChange() {
      this.stopResultLoop()
      this.hardResetUiState()
      this.resetDirectPreview()
      if (this.selectedConfiguration?.previewUrl) {
        this.mountRegisteredPreview(
          { urlView: this.selectedConfiguration.previewUrl },
          ""
        )
      }
      if (this.selectedConfiguration?.runtimeEnabled) {
        await this.loadCurrentStatus()
        if (this.cameraRunning) this.startResultLoop()
      }
    },

    async handleAutoRestoreChange(event) {
      const configuration = this.selectedConfiguration
      if (!configuration) return
      try {
        this.loading = true
        await updateFaceCameraConfiguration(configuration.runtimeCameraId, {
          cameraId: configuration.cameraId,
          laneId: configuration.laneId,
          autoRestore: event.target.checked,
          rowVersion: configuration.rowVersion
        })
        await this.loadSavedConfigurations()
      } catch (error) {
        this.handleFaceServiceError(error)
      } finally {
        this.loading = false
      }
    },

    async handleManualReconcile() {
      try {
        this.loading = true
        await reconcileFaceCameras()
        await this.loadSavedConfigurations()
        if (this.selectedConfiguration?.runtimeEnabled) {
          await this.loadCurrentStatus()
        }
      } catch (error) {
        this.handleFaceServiceError(error)
      } finally {
        this.loading = false
      }
    },

    buildDirectCameraUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return ""

      const sep = raw.includes("?") ? "&" : "?"
      return `${raw}${sep}t=${Date.now()}`
    },

    mountRegisteredPreview(camera, sourceUrl) {
      const previewUrl = String(camera?.urlView || "").trim()
      const directWebUrl = /^https?:\/\//i.test(sourceUrl || "") ? String(sourceUrl).trim() : ""
      let browserUrl = previewUrl || directWebUrl

      if (previewUrl) {
        try {
          const parsed = new URL(previewUrl, window.location.origin)
          if (parsed.pathname.endsWith("/stream.html")) {
            // Camera RTSP trả H.264. go2rtc không thể chuyển thẳng H.264 thành
            // MJPEG nếu không có transcoder, nên dùng player MSE/WebRTC tích hợp.
            parsed.searchParams.set("mode", "mse,webrtc")
            browserUrl = parsed.toString()
          }
        } catch {
          browserUrl = previewUrl
        }
      }

      if (!browserUrl) {
        throw new Error("Camera chưa có URL preview cho trình duyệt. Vui lòng kiểm tra go2rtc.")
      }

      this.mountDirectPreview(browserUrl)
    },

    clearResultStateOnly() {
      this.employeeId = ""
      this.trackingActive = false
      this.identityConfirmed = false
      this.faceMatch = false
      this.confirmCount = 0
      this.distance = null
      this.bbox = null
      this.timeoutState = false
      this.alert = false

      this.lockedSnapshot = ""
      this.lockedFaceCrop = ""
      this.scanLocked = false
      this.lockReason = ""

      this.fps = 0
      this.message = ""
      this.lastUpdate = ""
    },

    hardResetUiState() {
      this.cameraRunning = false
      this.cameraConnected = false
      this.currentIp = ""
      this.clearResultStateOnly()
    },

    mountDirectPreview(url) {
      const cleanUrl = String(url || "").trim()
      if (!cleanUrl) return

      if (this.previewRetryTimer) {
        clearTimeout(this.previewRetryTimer)
        this.previewRetryTimer = null
      }
      this.directCameraSourceUrl = cleanUrl
      this.directCameraUrl = this.buildDirectCameraUrl(cleanUrl)
      this.directCameraKey += 1
      this.previewHealthy = false
      this.previewRetryCount = 0
      this.previewRunning = true
    },

    resetDirectPreview() {
      if (this.previewRetryTimer) {
        clearTimeout(this.previewRetryTimer)
        this.previewRetryTimer = null
      }
      this.directCameraUrl = ""
      this.directCameraSourceUrl = ""
      this.directCameraKey += 1
      this.previewHealthy = false
      this.previewRetryCount = 0
      this.previewRunning = false
    },

    stopResultLoop() {
      if (this.resultTimer) {
        clearInterval(this.resultTimer)
        this.resultTimer = null
      }
      this.busyResult = false
    },

    clearFaceServiceError() {
      this.faceServiceError = null
    },

    handleFaceServiceError(error, { polling = false } = {}) {
      const normalized = normalizeFaceApiError(error)
      if (normalized.cancelled || this.destroyed) return

      const isNewError = this.faceServiceError?.code !== normalized.code
      this.faceServiceError = {
        code: normalized.code,
        message: normalized.message
      }
      this.cameraConnected = false

      if (shouldStopFacePolling(normalized)) {
        this.stopResultLoop()
      }

      if (!polling && isNewError) {
        alert(normalized.message)
      }
    },

    startResultLoop() {
      this.stopResultLoop()

      this.resultTimer = setInterval(async () => {
        if (this.destroyed) return
        if (!this.cameraRunning) return
        if (this.busyResult) return

        this.busyResult = true
        try {
          await this.refreshResult()
        } finally {
          this.busyResult = false
        }
      }, 500)
    },

    async loadCurrentStatus() {
      try {
        const res = await getCameraStatus(this.activeCameraId)
        this.clearFaceServiceError()
        await this.applyRealtimeState(res, false)

        if (this.currentIp) {
          this.cameraIp = this.currentIp
        }

        if (this.currentIp) {
          const camera = await ensureCameraRegistered({
            cameraName: "Face Monitor Camera",
            cameraType: "Face",
            streamUrl: this.currentIp,
          })
          this.mountRegisteredPreview(camera, this.currentIp)
        } else {
          this.resetDirectPreview()
        }

        if (this.cameraRunning) {
          await this.fetchLockedImagesIfNeeded(true)
        }
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      }
    },

    async handleCheckModels() {
      try {
        this.loading = true
        const res = await getModels()
        this.clearFaceServiceError()
        this.modelInfo = {
          version: res?.version ?? "-----",
          modelCount: Number(res?.successfulFileCount ?? res?.models?.length ?? 0),
          encodingCount: Number(res?.encodingCount ?? 0)
        }
      } catch (e) {
        this.handleFaceServiceError(e)
      } finally {
        this.loading = false
      }
    },

    async handleTurnOnPreview() {
      if (this.selectedConfiguration?.previewUrl) {
        this.mountRegisteredPreview(
          { urlView: this.selectedConfiguration.previewUrl },
          ""
        )
        this.message = "Đã mở preview camera đã lưu"
        return
      }
      const ip = (this.cameraIp || this.currentIp || "").trim()
      if (!ip) {
        alert("Vui lòng nhập URL camera")
        return
      }

      try {
        this.loading = true
        const camera = await ensureCameraRegistered({
          cameraName: "Face Monitor Camera",
          cameraType: "Face",
          streamUrl: ip,
        })
        this.currentIp = ip
        this.mountRegisteredPreview(camera, ip)
        this.message = "Đã mở preview camera qua go2rtc"
      } catch (e) {
        console.error("Turn on preview error:", e)
        alert(e?.message || "Lỗi mở preview camera")
      } finally {
        this.loading = false
      }
    },

    async handleInitOrResetSession() {
      const initializationStartedAt = performance.now()
      const ip = (this.cameraIp || this.currentIp || "").trim()
      if (!this.selectedConfiguration && !ip) {
        alert("Vui lòng nhập URL camera")
        return
      }

      try {
        this.loading = true
        if (!this.selectedConfiguration) {
          const camera = await ensureCameraRegistered({
            cameraName: "Face Monitor Camera",
            cameraType: "Face",
            streamUrl: ip,
          })

          this.currentIp = ip
          if (!this.previewRunning) {
            this.mountRegisteredPreview(camera, ip)
          }
        }

        this.clearResultStateOnly()

        if (!this.cameraRunning) {
          this.stopResultLoop()

          const res = this.selectedConfiguration
            ? await startConfiguredFaceCamera(this.activeCameraId)
            : await startCamera(this.activeCameraId, ip, this.laneId)
          this.clearFaceServiceError()
          if (!this.selectedConfiguration && !res?.success) {
            alert(res?.message || "Không thể khởi tạo phiên nhận diện")
            return
          }

          this.cameraRunning = true
          this.currentIp = ip || this.currentIp
          this.message = res?.configuration
            ? "Đã lưu trạng thái Running và đồng bộ Face Runtime"
            : (res.message || "Khởi tạo phiên nhận diện thành công")
          if (this.selectedConfiguration?.previewUrl && !this.previewRunning) {
            this.mountRegisteredPreview(
              { urlView: this.selectedConfiguration.previewUrl },
              ""
            )
          }

          await this.refreshResult()
          this.startResultLoop()
          return
        }

        const res = await resetCamera(this.activeCameraId)
        this.clearFaceServiceError()
        this.message = res?.message || "Đã reset phiên nhận diện"

        await this.refreshResult()

        if (!this.resultTimer) {
          this.startResultLoop()
        }
      } catch (e) {
        captureError(e, "camera_initialization_failure", { component: "FaceCamera" })
        this.handleFaceServiceError(e)
      } finally {
        recordMetric("camera_initialization", performance.now() - initializationStartedAt, { component: "FaceCamera" })
        this.loading = false
      }
    },

    async handleTurnOff() {
      try {
        this.loading = true

        this.stopResultLoop()

        try {
          const res = this.selectedConfiguration
            ? await stopConfiguredFaceCamera(this.activeCameraId)
            : await stopCamera(this.activeCameraId)
          this.clearFaceServiceError()
          this.message = res?.message || "Đã tắt camera"
          if (this.selectedConfiguration) {
            await this.loadSavedConfigurations()
          }
        } catch (e) {
          if (e?.status === 404) {
            this.clearFaceServiceError()
            this.message = "Camera đã ở trạng thái tắt"
            this.hardResetUiState()
            this.resetDirectPreview()
            return
          }
          this.handleFaceServiceError(e)
        }

        this.hardResetUiState()
        this.resetDirectPreview()
      } catch (e) {
        this.handleFaceServiceError(e)
      } finally {
        this.loading = false
      }
    },

    async refreshResult() {
      try {
        const res = await getCameraResult(this.activeCameraId)
        this.clearFaceServiceError()
        await this.applyRealtimeState(res, true)
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      }
    },

    async fetchLockedImagesIfNeeded(force = false) {
      if (this.destroyed) return
      if (!this.cameraRunning) return
      if (!this.scanLocked && !force) {
        this.lockedSnapshot = ""
        this.lockedFaceCrop = ""
        return
      }
      if (this.isFetchingLockedImages) return

      this.isFetchingLockedImages = true
      try {
        const res = await getLockedImages(this.activeCameraId)
        this.clearFaceServiceError()

        if (res?.scan_locked) {
          this.lockedSnapshot = res.locked_snapshot || ""
          this.lockedFaceCrop = res.locked_face_crop || ""
        } else {
          this.lockedSnapshot = ""
          this.lockedFaceCrop = ""
        }
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      } finally {
        this.isFetchingLockedImages = false
      }
    },

    async applyRealtimeState(res, allowTurnOffReset = true) {
      if (!res || this.destroyed) return

      const incomingCameraEnabled = !!res.camera_enabled

      this.cameraRunning = incomingCameraEnabled
      this.cameraConnected = !!res.camera_connected
      this.currentIp = res.ip || this.currentIp

      this.employeeId = res.employee_id || ""
      this.trackingActive = !!res.tracking_active
      this.identityConfirmed = !!res.identity_confirmed
      this.faceMatch = !!res.face_match
      this.confirmCount = Number(res.confirm_count || 0)
      this.distance = res.distance ?? null
      this.bbox = res.bbox || null
      this.timeoutState = !!res.timeout
      this.alert = !!res.alert

      this.scanLocked = !!res.scan_locked
      this.lockReason = res.lock_reason || ""

      this.fps = Number(res.fps || 0)
      this.message = res.message || ""
      this.lastUpdate = res.last_update || ""

      if (!this.scanLocked) {
        this.lockedSnapshot = ""
        this.lockedFaceCrop = ""
      }

      if (!incomingCameraEnabled && allowTurnOffReset) {
        this.stopResultLoop()
        this.hardResetUiState()
        return
      }

      if (this.scanLocked) {
        await this.fetchLockedImagesIfNeeded(false)
      }
    },

    handleDirectPreviewLoaded() {
      this.previewHealthy = true
      this.previewRetryCount = 0
    },

    handleDirectPreviewError() {
      this.previewHealthy = false
      this.message = "Không nhận được hình ảnh camera. Hãy kiểm tra địa chỉ, mạng và trạng thái go2rtc."

      if (!this.previewRunning || !this.directCameraSourceUrl || this.previewRetryCount >= 1) {
        return
      }

      this.previewRetryCount += 1
      this.previewRetryTimer = setTimeout(() => {
        this.previewRetryTimer = null
        if (!this.previewRunning || !this.directCameraSourceUrl) return
        this.directCameraUrl = this.buildDirectCameraUrl(this.directCameraSourceUrl)
        this.directCameraKey += 1
      }, 1500)
    },

    async handleDoubleClick() {
      try {
        if (!document.fullscreenElement) {
          const el = this.$refs.videoWrapperRef
          if (!el) return
          if (el.requestFullscreen) {
            await el.requestFullscreen()
          } else if (el.webkitRequestFullscreen) {
            await el.webkitRequestFullscreen()
          } else if (el.msRequestFullscreen) {
            await el.msRequestFullscreen()
          }
        } else {
          if (document.exitFullscreen) {
            await document.exitFullscreen()
          } else if (document.webkitExitFullscreen) {
            await document.webkitExitFullscreen()
          } else if (document.msExitFullscreen) {
            await document.msExitFullscreen()
          }
        }
      } catch (error) {
        console.error('Lỗi khi chuyển đổi toàn màn hình:', error)
      }
    },

    async handleRightClick(event) {
      if (document.fullscreenElement) {
        event.preventDefault()
        try {
          if (document.exitFullscreen) {
            await document.exitFullscreen()
          } else if (document.webkitExitFullscreen) {
            await document.webkitExitFullscreen()
          } else if (document.msExitFullscreen) {
            await document.msExitFullscreen()
          }
        } catch (error) {
          console.error('Lỗi khi thoát toàn màn hình:', error)
        }
      }
    },

    switchTab(tab) {
      this.activeTab = tab
      if (tab === "monitor") {
        this.handleStopGuidedEnroll()
      }
    },

    async selectEnrollCamera(cam) {
      if (!cam) return
      this.selectedEnrollCamera = cam
      this.cameraSearch = cam.cameraName || ""
      this.cameraOpen = false
      this.enrollError = ""
      const ip = cam.streamUrl || cam.urlView || ""
      if (!ip) return
      try {
        const camera = await ensureCameraRegistered({
          cameraName: cam.cameraName || "Face Enroll Camera",
          cameraType: "Face",
          streamUrl: ip,
        })
        if (camera?.urlView) {
          this.mountRegisteredPreview(camera, ip)
        }
      } catch (e) {
        this.enrollError = e?.message || "Không thể mở preview camera."
      }
    },

    clearEnrollCamera() {
      this.selectedEnrollCamera = null
      this.cameraSearch = ""
      this.resetDirectPreview()
    },

    async handleStartGuidedEnroll() {
      if (!this.selectedEnrollCamera) {
        this.enrollError = "Vui lòng chọn camera trước."
        return
      }
      const ip = this.selectedEnrollCamera.streamUrl || ""
      if (!ip) {
        this.enrollError = "Camera chưa có stream URL."
        return
      }

      this.enrollError = ""
      this.enrollResult = null
      this.enrollSending = true
      try {
        const res = await guidedStart({ streamUrl: ip })
        if (!res?.success) throw new Error(res?.message || "Không bắt đầu được.")
        this.enrollStep = "capture"
        this.guidedStatus = "running"
        this.guidedGuidance = "Đang khởi động..."
        this.guidedFaceState = "none"
        this.guidedProgress = 0
        this.guidedTotal = 5
        this.guidedCoveredAngles = []
        this.guidedMissingAngles = ["straight", "left", "right", "up", "down"]
        this.guidedAnglesComplete = false
        this.guidedSamples = 0
        this.guidedDurationMs = 0
        this.guidedBins = []
        this.guidedGridBins = this.buildGuidedGrid([])
        this.guidedArrow = "none"
        this.startGuidedPolling()
      } catch (e) {
        this.enrollError = e?.message || e?.response?.data?.message || "Không thể bắt đầu thu thập."
      } finally {
        this.enrollSending = false
      }
    },

    startGuidedPolling() {
      this.stopGuidedPolling()
      this.guidedPollTimer = setInterval(async () => {
        if (this.destroyed) return
        if (this.enrollStep !== "capture") {
          this.stopGuidedPolling()
          return
        }
        try {
          const res = await guidedProgress()
          const snap = res?.snapshot || {}
          this.guidedStatus = snap.status || "running"
          this.guidedGuidance = snap.guidance || ""
          this.guidedFaceState = snap.faceState || "none"
          this.guidedProgress = Number(snap.progress || 0)
          this.guidedTotal = Number(snap.totalAngles || 5)
          this.guidedCoveredAngles = snap.coveredAngles || []
          this.guidedMissingAngles = snap.missingAngles || []
          this.guidedAnglesComplete = !!snap.anglesComplete
          this.guidedSamples = Number(snap.samplesCollected || 0)
          this.guidedDurationMs = Number(snap.durationMs || 0)
          this.guidedGridBins = this.buildGuidedGrid(snap.coveredAngles || [])
          this.guidedArrow = this.inferArrow(snap.guidance || "")
          if (snap.status === "error") {
            this.stopGuidedPolling()
            this.enrollStep = "idle"
            this.enrollError = snap.lastError || "Lỗi khi thu thập mẫu."
          }
        } catch (e) {
          if (this.enrollStep !== "capture") return
          this.enrollError = e?.response?.data?.message || e?.message || "Mất kết nối khi thu thập."
        }
      }, 400)
    },

    stopGuidedPolling() {
      if (this.guidedPollTimer) {
        clearInterval(this.guidedPollTimer)
        this.guidedPollTimer = null
      }
    },

    buildGuidedGrid(covered) {
      const order = ["straight", "left", "right", "up", "down"]
      const labels = {
        straight: "Thẳng",
        left: "Trái",
        right: "Phải",
        up: "Lên",
        down: "Xuống"
      }
      return order.map(key => ({
        key,
        label: labels[key],
        class: covered.includes(key) ? "cell-ok" : "cell-wait"
      }))
    },

    inferArrow(guidance) {
      const text = String(guidance || "").toLowerCase()
      if (text.includes("phải")) return "right"
      if (text.includes("trái")) return "left"
      if (text.includes("lên") || text.includes("ngẩng")) return "up"
      if (text.includes("xuống") || text.includes("cúi")) return "down"
      return "none"
    },

    async handleStopGuidedEnroll() {
      this.stopGuidedPolling()
      try {
        await guidedStop()
      } catch (e) {
        console.warn("guidedStop warning:", e)
      }
      this.enrollStep = "idle"
      this.guidedStatus = "idle"
    },

    openConfirmEnroll() {
      // Stop recording, then show the subject-assignment form.
      this.handleStopGuidedEnroll()
      this.enrollStep = "done"
      this.enrollError = ""
    },

    async handleConfirmGuidedEnroll() {
      if (!this.enrollSubjectId) {
        this.enrollError = "Vui lòng nhập mã đối tượng."
        return
      }
      this.enrollError = ""
      this.enrollSending = true
      try {
        const res = await guidedConfirm(this.enrollSubjectId)
        if (!res?.success) throw new Error(res?.message || "Không lưu được mẫu.")
        this.enrollResult = {
          subjectId: this.enrollSubjectId,
          encodingCount: res?.encodingCount ?? "-----",
          modelFileName: res?.modelFileName ?? "-----",
          registryVersion: res?.registryVersion ?? "-----",
          message: res?.message || ""
        }
        this.stopGuidedPolling()
      } catch (e) {
        this.enrollError = e?.response?.data?.message || e?.message || "Không thể lưu mẫu nhận diện."
      } finally {
        this.enrollSending = false
      }
    },

    resetGuidedEnroll() {
      this.stopGuidedPolling()
      this.enrollStep = "idle"
      this.guidedStatus = "idle"
      this.guidedProgress = 0
      this.guidedTotal = 5
      this.guidedFaceState = "none"
      this.guidedCoveredAngles = []
      this.guidedMissingAngles = []
      this.guidedAnglesComplete = false
      this.guidedSamples = 0
      this.guidedDurationMs = 0
      this.guidedBins = []
      this.guidedGridBins = []
      this.guidedGuidance = ""
      this.enrollError = ""
      this.enrollResult = null
      this.enrollSubjectId = ""
      this.enrollDisplayName = ""
      this.enrollSubjectType = "employee"
      this.resetDirectPreview()
    }
  }
}
</script>

<style scoped>
.face-monitor-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(360px, 0.8fr);
  gap: 20px;
  margin-bottom: 24px;
}

@media (max-width: 1024px) {
  .face-monitor-grid {
    grid-template-columns: 1fr;
  }
}

.monitor-left-col,
.monitor-right-col {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.video-card {
  padding: 16px;
}

.video-wrapper {
  width: 100%;
  aspect-ratio: 16 / 9;
  background: #000;
  position: relative;
  overflow: hidden;
  border-radius: var(--border-radius, 12px);
  box-shadow: inset 0 0 20px rgba(0, 0, 0, 0.6);
}

.video {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
}

.video-off {
  color: var(--text-muted, #5d7a90);
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  font-size: 1.2rem;
  font-weight: 500;
}

.evidence-panel-card {
  padding: 20px;
}

.evidence-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

@media (max-width: 640px) {
  .evidence-grid {
    grid-template-columns: 1fr;
  }
}

.evidence-item {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.evidence-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.evidence-image {
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
  border-radius: var(--border-radius-sm, 8px);
  border: 1px solid var(--border-color);
}

.evidence-empty {
  width: 100%;
  aspect-ratio: 4 / 3;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  background: var(--bg-input);
  border-radius: var(--border-radius-sm, 8px);
  border: 1px dashed var(--border-color);
}

.config-card,
.live-state-card {
  padding: 20px;
}

.config-row {
  margin-bottom: 16px;
}

.config-row label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-weight: 600;
  color: var(--text-primary);
  text-align: left;
}

.filter-select,
.form-input,
.date-input {
  width: 100%;
  min-height: 40px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-primary);
  padding: 0 12px;
  transition: border-color var(--transition-fast);
}

.filter-select:focus,
.form-input:focus,
.date-input:focus {
  border-color: var(--accent-primary);
}

.config-meta-box {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 14px;
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
}

.meta-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.9rem;
}

.meta-item span {
  color: var(--text-secondary);
}

.auto-restore-row {
  margin-top: 4px;
  border-top: 1px solid var(--border-color);
  padding-top: 8px;
}

.auto-restore {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-weight: 500;
}

.auto-restore input[type="checkbox"] {
  width: 16px;
  height: 16px;
  accent-color: var(--accent-primary);
}

.btn-reconcile {
  margin-top: 8px;
  align-self: flex-start;
}

.sync-error-text {
  color: var(--accent-danger);
  font-size: 0.85rem;
  margin-top: 4px;
}

.control-panel-inputs {
  margin-bottom: 16px;
  text-align: left;
}

.search-box { position: relative; }

.dropdown {
  position: absolute;
  z-index: 50;
  left: 0;
  right: 0;
  top: 100%;
  margin-top: 4px;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.15);
  max-height: 240px;
  overflow-y: auto;
}

.dropdown-item {
  padding: 10px 12px;
  cursor: pointer;
  font-size: 0.9rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.dropdown-item:last-child { border-bottom: none; }

.dropdown-item:hover { background: var(--bg-input); }

.dropdown-item-name {
  font-weight: 700;
  color: var(--text-primary);
}

.dropdown-item-meta {
  font-size: 0.78rem;
  color: var(--text-muted);
}

.dropdown-hint {
  position: absolute;
  z-index: 50;
  left: 0;
  right: 0;
  top: 100%;
  margin-top: 4px;
  padding: 10px 12px;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 0.85rem;
}

.field-label {
  display: block;
  font-weight: 600;
  margin-bottom: 6px;
  color: var(--text-secondary);
}

.control-actions-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
}

.control-actions-grid .btn {
  min-width: 0;
  width: 100%;
}

.service-error-box {
  margin-top: 16px;
  padding: 10px 14px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.9rem;
  text-align: left;
}

.model-status-box {
  margin-top: 16px;
  padding: 12px 14px;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 0.9rem;
  text-align: left;
}

.model-status-box div {
  display: flex;
  justify-content: space-between;
}

/* Status colors classes alert */
.alert-danger-soft {
  background: rgba(195, 81, 70, 0.08);
  border: 1px solid rgba(195, 81, 70, 0.2);
  color: var(--accent-danger);
}

.alert-info-soft {
  background: rgba(84, 196, 211, 0.08);
  border: 1px solid rgba(84, 196, 211, 0.2);
  color: var(--ink-800);
}

.alert-warn-soft {
  background: rgba(216, 155, 55, 0.08);
  border: 1px solid rgba(216, 155, 55, 0.2);
  color: var(--warning-500);
}

.status-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
  margin-bottom: 20px;
}

.status-badge-item {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  padding: 8px 12px;
  border-radius: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.85rem;
}

.status-badge-item .label {
  color: var(--text-secondary);
  font-weight: 500;
}

.face-result-boxes {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 20px;
}

.face-result-box {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 14px 16px;
  text-align: left;
}

.face-result-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 4px;
}

.face-result-value {
  font-size: 1.6rem;
  font-weight: 800;
  font-family: var(--font-heading);
  color: var(--text-primary);
}

.face-result-value.confirmed {
  color: var(--accent-success);
}

.face-result-value.locked {
  color: var(--warning-500);
}

.face-result-value.tracking {
  color: var(--accent-info);
}

.lock-banner-alert,
.alert-banner-danger {
  margin-bottom: 20px;
  padding: 12px 14px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.9rem;
  text-align: left;
}

.detail-state-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  border-top: 1px solid var(--border-color);
  padding-top: 16px;
  text-align: left;
}

.detail-state-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
}

.detail-state-item span {
  color: var(--text-secondary);
}

.detail-state-item strong {
  color: var(--text-primary);
}

.font-mono {
  font-family: monospace;
}

.history-section-card {
  padding: 22px;
  margin-top: 24px;
}

.flex-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.history-badges-row {
  display: flex;
  gap: 8px;
}

.select-filter-bar {
  margin-top: 14px;
  margin-bottom: 16px;
}

.comparison-summary-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
}

.alert-banner-warning {
  padding: 10px 14px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.85rem;
  margin-bottom: 14px;
  text-align: left;
}

.text-center {
  text-align: center;
}

.text-muted {
  color: var(--text-muted);
}

/* Fullscreen mode override */
.video-wrapper:fullscreen {
  width: 100vw !important;
  height: 100vh !important;
  margin: 0 !important;
  border-radius: 0 !important;
  max-width: none !important;
  background: #000;
  display: flex;
  align-items: center;
  justify-content: center;
}

.video-wrapper:fullscreen .video {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.video-wrapper:fullscreen .video-off {
  font-size: 3vw;
}

.page-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 12px;
}

.page-tab {
  padding: 8px 18px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.page-tab:hover {
  border-color: var(--accent-primary);
  color: var(--accent-primary);
}

.page-tab.active {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
  color: #fff;
}

.enroll-card {
  padding: 22px;
}

.panel-subtitle {
  margin-top: 6px;
  font-size: 0.9rem;
  color: var(--text-muted);
}

.enroll-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.4fr) minmax(300px, 0.6fr);
  gap: 20px;
  margin-top: 16px;
}

@media (max-width: 1024px) {
  .enroll-grid {
    grid-template-columns: 1fr;
  }
}

.enroll-preview-col,
.enroll-form-col {
  display: flex;
  flex-direction: column;
  gap: 14px;
  text-align: left;
}

.enroll-video-wrapper {
  width: 100%;
  aspect-ratio: 16 / 9;
  background: #000;
  border-radius: var(--border-radius, 12px);
  overflow: hidden;
  position: relative;
  border: 3px solid var(--border-color);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}

.enroll-video-wrapper.overlay-wait {
  border-color: #eab308;
  box-shadow: 0 0 0 3px rgba(234, 179, 8, 0.35);
}

.enroll-video-wrapper.overlay-ok {
  border-color: #22c55e;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.35);
}

.enroll-video-wrapper.overlay-danger {
  border-color: #dc2626;
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.45);
}

.guided-error-banner {
  position: absolute;
  top: 12px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 20;
  background: rgba(220, 38, 38, 0.9);
  color: #fff;
  padding: 10px 18px;
  border-radius: 999px;
  font-size: 0.95rem;
  font-weight: 800;
  text-align: center;
  max-width: 90%;
  border: 2px solid rgba(255, 255, 255, 0.6);
}

.enroll-progress-box {
  margin-top: 14px;
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
}

.enroll-progress-label {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
  font-weight: 700;
  margin-bottom: 8px;
}

.enroll-progress-label span {
  color: var(--text-secondary);
}

.enroll-progress-track {
  height: 8px;
  border-radius: 999px;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  overflow: hidden;
  margin-bottom: 8px;
}

.enroll-progress-bar {
  height: 100%;
  border-radius: 999px;
  background: var(--accent-primary);
  transition: width 200ms ease;
}

.enroll-progress-hint {
  font-size: 0.82rem;
  color: var(--text-muted);
}

.enroll-video {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
}

.enroll-video-off {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  font-size: 1rem;
  font-weight: 500;
}

.enroll-button-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 12px;
}

.enroll-big-btn {
  min-height: 48px;
  font-size: 1rem;
  flex: 1;
}

.enroll-source-badge {
  margin-top: 8px;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.enroll-source-badge.ok {
  background: rgba(80, 190, 130, 0.1);
  border: 1px solid rgba(80, 190, 130, 0.3);
  color: var(--accent-success);
}

.enroll-source-id {
  color: var(--text-muted);
  font-weight: 500;
}

.enroll-submit-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.enroll-result {
  padding: 12px 14px;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 0.9rem;
}

.enroll-result div {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.enroll-result span {
  color: var(--text-secondary);
}

.enroll-result-hint {
  border-top: 1px solid var(--border-color);
  padding-top: 8px;
  font-weight: 600;
}

.alert-success-soft {
  background: rgba(80, 190, 130, 0.1);
  border: 1px solid rgba(80, 190, 130, 0.25);
  color: var(--accent-success);
}

.enroll-again-btn {
  margin-top: 6px;
  align-self: flex-start;
}

.enroll-done-title {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--accent-success);
}

.enroll-done-hint {
  margin: 0;
  font-size: 0.9rem;
  color: var(--text-secondary);
}

.subject-type-row {
  display: flex;
  gap: 8px;
}

.pose-mode-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.pose-mode-btn {
  flex: 1;
  min-height: 38px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
}

.pose-mode-btn.active {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
  color: #fff;
}

.pose-mode-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pose-mode-hint {
  margin: 0;
  font-size: 0.8rem;
  color: var(--text-muted);
}

.subject-type-btn {
  flex: 1;
  min-height: 44px;
  border-radius: 10px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-weight: 700;
  cursor: pointer;
}

.subject-type-btn.active {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
  color: #fff;
}

/* Guided overlay */
.guided-overlay {
  position: absolute;
  left: 12px;
  right: 12px;
  bottom: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  pointer-events: none;
}

.guided-arrow {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: rgba(2, 6, 23, 0.65);
  border: 3px solid var(--text-muted);
  color: var(--text-muted);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 30px;
  font-weight: 900;
}

.guided-arrow.arrow-left { border-color: #eab308; color: #eab308; }
.guided-arrow.arrow-right { border-color: #eab308; color: #eab308; }
.guided-arrow.arrow-up { border-color: #eab308; color: #eab308; }
.guided-arrow.arrow-down { border-color: #eab308; color: #eab308; }

.guided-prompt {
  padding: 10px 18px;
  border-radius: 999px;
  background: rgba(2, 6, 23, 0.78);
  color: #fff;
  font-size: 1.05rem;
  font-weight: 800;
  text-align: center;
  border: 2px solid #eab308;
}

.guided-grid {
  position: absolute;
  top: 12px;
  right: 12px;
  display: grid;
  grid-template-columns: 1fr;
  gap: 6px;
  background: rgba(2, 6, 23, 0.7);
  padding: 8px;
  border-radius: 10px;
}

.guided-cell {
  min-width: 64px;
  height: 30px;
  padding: 0 10px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.82rem;
  font-weight: 800;
  border: 2px solid var(--border-color);
  color: var(--text-muted);
  white-space: nowrap;
}

.guided-cell.cell-wait {
  border-color: #eab308;
  color: #eab308;
  background: rgba(234, 179, 8, 0.12);
}

.guided-cell.cell-ok {
  border-color: #22c55e;
  color: #22c55e;
  background: rgba(34, 197, 94, 0.15);
}

.guided-progress-text {
  position: absolute;
  top: 12px;
  left: 12px;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(2, 6, 23, 0.7);
  color: #fff;
  font-size: 0.85rem;
  font-weight: 700;
  border: 1px solid rgba(148, 163, 184, 0.5);
}
</style>
