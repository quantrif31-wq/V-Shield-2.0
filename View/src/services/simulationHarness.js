import http from "./http"

const SIM_STORAGE_KEY = "vshield_sim"

export function isSimMode() {
  if (typeof window === "undefined") return false
  const params = new URLSearchParams(window.location.search)
  if (params.has("simulate")) return true
  return window.localStorage.getItem(SIM_STORAGE_KEY) === "1"
}

let installed = false

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

export function installSimulation(component) {
  if (!component || installed) return component
  installed = true

  const sim = {
    plateSessionId: 1,
    qrLane1: "",
    qrLane2: "",
    plateLane: "",
    plateValue: "",
    empPayload: "",
    empEmployeeId: 0,
    empName: "",
    foreignPlate: "",
    foreignOwner: 0
  }
  component.__sim = sim

  if (!component.simState) {
    component.simState = {
      expanded: true,
      targetLane: "lane1",
      laneSynced: false,
      empName: "",
      empEmployeeId: 0,
      qr1: false,
      qr2: false,
      injectPlate: "59K-12345",
      foreignPlate: "",
      foreignOwner: 0,
      logText: ""
    }
  }

  component.getLaneQrScanResult = async function (lane) {
    const s = this.__sim
    const payload = lane.id === "lane2" ? s.qrLane2 : s.qrLane1
    if (payload) {
      return {
        connected: true,
        frame_ready: true,
        scan_enabled: false,
        phase: "locked",
        candidate_payload: payload,
        candidate_source: "sim",
        candidate_seen_count: 3,
        locked_payload: payload,
        locked_at: Date.now(),
        last_update: ""
      }
    }
    return {
      connected: true,
      frame_ready: true,
      scan_enabled: false,
      phase: "idle",
      locked_payload: "",
      last_update: ""
    }
  }

  component.startLaneQrScanner = async () => ({ success: true, message: "SIM" })
  component.scanLaneQrOnce = async () => ({ success: true, message: "SIM" })
  component.resetLaneQrSession = async () => ({ success: true, message: "SIM" })
  component.stopLaneQrScanner = async () => ({ success: true, message: "SIM" })

  component.refreshPlate = async function (lane) {
    const s = this.__sim
    const myTurn = s.plateLane === lane.id
    const plate = myTurn ? s.plateValue : ""
    const res = {
      session_id: s.plateSessionId,
      ip: "sim-camera",
      camera_enabled: true,
      confirmed_plate: plate,
      last_raw_plate: plate,
      scan_locked: !!plate,
      scan_active: !!plate,
      fps: 25,
      ocr_running: true,
      stable_count: plate ? 6 : 0,
      moving_fast: false,
      message: plate ? "SIM: biển đang hiện diện" : "SIM: chờ biển",
      last_update: ""
    }
    try {
      await this.applyPlateRealtimeState(lane, res, true)
    } catch (e) {
      console.warn("sim refreshPlate", e)
    }
  }

  const plateApi = component.lanes && component.lanes[0] ? component.lanes[0].plateApi : null
  if (plateApi) {
    component.simPlateOriginals = {
      turnOnCamera: plateApi.turnOnCamera,
      turnOffCamera: plateApi.turnOffCamera,
      resetCameraState: plateApi.resetCameraState,
      getCameraStatus: plateApi.getCameraStatus,
      getLockedImages: plateApi.getLockedImages
    }
    plateApi.turnOnCamera = async () => {
      sim.plateSessionId += 1
      return { success: true, session_id: sim.plateSessionId, message: "SIM: bật camera biển" }
    }
    plateApi.turnOffCamera = async () => ({ success: true, message: "SIM" })
    plateApi.resetCameraState = async () => {
      sim.plateSessionId += 1
      return { success: true, session_id: sim.plateSessionId, message: "SIM: reset" }
    }
    plateApi.getCameraStatus = async () => ({
      session_id: sim.plateSessionId,
      camera_enabled: false,
      confirmed_plate: "",
      ip: "sim-camera"
    })
    plateApi.getLockedImages = async () => ({
      session_id: sim.plateSessionId,
      scan_locked: true,
      locked_snapshot: "",
      locked_plate_crop: ""
    })
  }

  const log = (msg) => {
    const text = `[${new Date().toLocaleTimeString("vi-VN")}] ${msg}`
    if (component.simState) {
      component.simState.logText = (text + "\n" + component.simState.logText).slice(0, 1500)
    }
    console.log("[SIM]", msg)
  }

  component.simGenerateQr = async function () {
    const s = this.__sim
    try {
      const res = await http.post("/dynamic-qr/my")
      const data = res?.data?.data || {}
      if (data?.qrPayload) {
        s.empPayload = String(data.qrPayload).trim()
        s.empEmployeeId = Number(data.employeeId || 0)
        s.empName = data.employeeName || ""
        this.simState.empEmployeeId = s.empEmployeeId
        this.simState.empName = s.empName
        log(`Đã tạo QR thật: ${s.empName} (emp ${s.empEmployeeId})`)
      } else {
        log(`Tạo QR thất bại: ${res?.data?.message || "không rõ lỗi"}`)
      }
    } catch (e) {
      log(`Tạo QR lỗi: ${e?.response?.data?.message || e?.message || e}`)
    }
  }

  component.simToggleQr = async function (laneId, on) {
    const s = this.__sim
    const key = laneId === "lane2" ? "qrLane2" : "qrLane1"
    const stateKey = laneId === "lane2" ? "qr2" : "qr1"
    if (on && !s.empPayload) {
      await this.simGenerateQr()
    }
    s[key] = on ? s.empPayload : ""
    this.simState[stateKey] = on
    log(`${laneId}: QR ${on ? "bật" : "tắt"}`)
  }

  component.simSetPlate = function (laneId, value) {
    const s = this.__sim
    const plate = String(value || "").trim()
    if (!plate) {
      log("Plate rỗng, không gắn vào được")
      return
    }
    if (s.plateLane && s.plateLane !== laneId) {
      log(`Chuyển plate từ ${s.plateLane} sang ${laneId}`)
    }
    s.plateLane = laneId
    s.plateValue = plate
    this.simState.injectPlate = plate
    log(`${laneId}: gắn plate ${plate}`)
  }

  component.simClearPlate = function (laneId) {
    const s = this.__sim
    s.plateValue = ""
    s.plateLane = laneId
    log(`${laneId}: bỏ plate (mô phỏng biển rời khỏi vùng quét)`)
  }

  component.simRefreshForeign = async function () {
    const s = this.__sim
    try {
      const res = await http.get("/vehicles")
      const raw =
        (Array.isArray(res?.data) && res.data) ||
        res?.data?.items ||
        res?.data?.data ||
        []
      const candidates = raw
        .map((v) => String(v?.licensePlate || v?.LicensePlate || "").trim())
        .filter((p) => p)
      const mine = s.empEmployeeId
      let chosen = null
      let fallback = null
      for (const plate of candidates.slice(0, 14)) {
        try {
          const detail = await http.get(
            `/gate-transit/vehicle-by-plate/${encodeURIComponent(plate)}`
          )
          const rows = (detail?.data?.data && Array.isArray(detail.data.data)
            ? detail.data.data
            : [detail?.data?.data]
          ).filter(Boolean)
          for (const row of rows) {
            const owner = Number(row?.EmployeeId ?? 0)
            const status = String(row?.ParkingStatus || "")
            if (!owner || owner === mine) continue
            if (status === "IN") {
              chosen = { plate, owner, status }
              break
            }
            if (!fallback) fallback = { plate, owner, status }
          }
        } catch (e) {
          // bỏ qua xe không tra được
        }
        if (chosen) break
      }
      const target = chosen || fallback
      if (target) {
        s.foreignPlate = target.plate
        s.foreignOwner = target.owner
        this.simState.foreignPlate = s.foreignPlate
        this.simState.foreignOwner = s.foreignOwner
        log(
          chosen
            ? `Xe ngoài (DENY OK): ${s.foreignPlate} (emp ${s.foreignOwner}, ${target.status})`
            : `CẢNH BÁO: không có xe ngoài trạng thái IN, dùng ${s.foreignPlate} (emp ${s.foreignOwner}, ${target.status}) => có thể bị REASSIGN chứ không deny.`
        )
      } else {
        log("Không tìm thấy xe thuộc employee khác trong DB")
      }
    } catch (e) {
      log(`Lấy danh sách xe lỗi: ${e?.response?.data?.message || e?.message}`)
    }
  }

  component.simUseForeign = async function () {
    const s = this.__sim
    if (!s.foreignPlate) {
      await this.simRefreshForeign()
    }
    if (!s.foreignPlate) return
    this.simSetPlate(this.simState.targetLane, s.foreignPlate)
  }

  component.simMakeAllowPlate = function () {
    const plate = `59K-${10000 + Math.floor(Math.random() * 89000)}`
    this.simState.injectPlate = plate
    this.simSetPlate(this.simState.targetLane, plate)
  }

  component.simSetTargetLane = function (laneId) {
    this.simState.targetLane = laneId
  }

  component.simRunScenario = async function (laneId, kind) {
    const s = this.__sim
    if (!this.simState.laneSynced) await this.simSyncLaneConfig()
    if (!s.empPayload) await this.simGenerateQr()
    if (!s.empPayload) {
      log("Chưa có QR thật, không chạy kịch bản được")
      return
    }

    let plate = ""
    if (kind === "deny") {
      if (!s.foreignPlate) await this.simRefreshForeign()
      plate = s.foreignPlate
      if (!plate) {
        log("Chưa có plate ngoài để test DENY")
        return
      }
    } else {
      plate = `59K-${10000 + Math.floor(Math.random() * 89000)}`
    }

    if (!this.autoActive) {
      log("Đang bắt đầu auto-monitor...")
      await this.startAutoMonitor()
    }
    await sleep(700)

    this.simSetPlate(laneId, plate)
    await sleep(500)
    const stateKey = laneId === "lane2" ? "qr2" : "qr1"
    if (!this.simState[stateKey]) {
      await this.simToggleQr(laneId, true)
    }
    log(`Kịch bản ${kind === "deny" ? "DENY" : "ALLOW"} đang chạy trên ${laneId} (plate ${plate})`)
  }

  component.simResetAll = function () {
    const s = this.__sim
    s.qrLane1 = ""
    s.qrLane2 = ""
    s.plateValue = ""
    s.plateLane = ""
    this.simState.qr1 = false
    this.simState.qr2 = false
    for (const lane of this.lanes) {
      this.clearQrState(lane.qr)
      this.clearPlateState(lane.plate)
    }
    log("Đã reset toàn bộ trạng thái giả lập")
  }

  component.simEnable = true
  log("Simulation harness active (?simulate=1)")
  component.simSyncLaneConfig = async function () {
    const sync = (lane, entryDirection, gateOf) => {
      const target = this.lanes.find((l) => l.id === lane)
      if (!target) return
      target.laneId = entryDirection.LaneId
      target.gateId = null
      target.direction = entryDirection.Direction === "OUT" ? "OUT" : "IN"
      if (gateOf && gateOf.GateId) target.gateId = gateOf.GateId
      log(
        `${lane}: LaneId=${target.laneId} GateId=${target.gateId || "(tự resolve)"} Direction=${target.direction}`
      )
    }
    try {
      const gates = await http.get("/gate-transit/gates")
      const gateList =
        (Array.isArray(gates?.data?.data) && gates.data.data) || gates?.data || []
      const health = await http.get("/enterprise/visitor-vehicle/lane-health")
      const laneList =
        (Array.isArray(health?.data) && health.data) ||
        health?.data?.items ||
        health?.data?.data ||
        []
      const normDir = (d) => String(d || "").toLowerCase()
      const inLane =
        laneList.find((l) => normDir(l.Direction) === "entry") ||
        laneList.find((l) => normDir(l.Direction) === "in") ||
        laneList[0]
      const outLane =
        laneList.find((l) => normDir(l.Direction) === "exit") ||
        laneList.find((l) => normDir(l.Direction) === "out") ||
        laneList[1]
      if (inLane) sync("lane1", inLane)
      if (outLane) sync("lane2", outLane)
      this.simState.laneSynced = true
    } catch (e) {
      log(`Đồng bộ cổng/làn lỗi: ${e?.response?.data?.message || e?.message}`)
    }
  }
  component.simSyncLaneConfig()
  return component
}