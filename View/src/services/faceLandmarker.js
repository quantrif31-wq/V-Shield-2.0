// Client-side MediaPipe FaceLandmarker wrapper (WASM) dùng cho nhận diện khuôn
// mặt + hướng dẫn quay 5 góc ngay trên thiết bị khách, không cần server AI.

let landmarker = null
let loaded = false
let loadingPromise = null

export const FIVE_ANGLES = ["straight", "left", "right", "up", "down"]
export const ANGLE_LABELS = {
  straight: "Thẳng",
  left: "Trái",
  right: "Phải",
  up: "Lên",
  down: "Xuống"
}

const YAW_THRESHOLD = 10
const PITCH_THRESHOLD = 6

export function classifyAngle(yaw, pitch) {
  if (yaw < -YAW_THRESHOLD) return "left"
  if (yaw > YAW_THRESHOLD) return "right"
  if (pitch < -PITCH_THRESHOLD) return "up"
  if (pitch > PITCH_THRESHOLD) return "down"
  return "straight"
}

export async function loadLandmarker(modelPath = "/models/face_landmarker.task") {
  if (loaded && landmarker) return landmarker
  if (loadingPromise) return loadingPromise

  loadingPromise = (async () => {
    const { FaceLandmarker, FilesetResolver } = await import("@mediapipe/tasks-vision")
    const filesetResolver = await FilesetResolver.forVisionTasks(
      "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.14/wasm"
    )
    landmarker = await FaceLandmarker.createFromOptions(filesetResolver, {
      baseOptions: {
        modelAssetPath: modelPath,
        delegate: "GPU"
      },
      outputFaceBlendshapes: false,
      outputFacialTransformationMatrixes: true,
      runningMode: "VIDEO",
      numFaces: 1
    })
    loaded = true
    return landmarker
  })()

  try {
    return await loadingPromise
  } finally {
    loadingPromise = null
  }
}

// Chạy detect trên 1 khung video, trả về { faceState, yaw, pitch, roll, landmarks }.
export function detectFace(video) {
  if (!landmarker || !video || !video.videoWidth) {
    return { faceState: "none" }
  }
  let result
  try {
    result = landmarker.detectForVideo(video, performance.now())
  } catch {
    return { faceState: "none" }
  }
  const faces = result?.faceLandmarks || []
  if (faces.length === 0) return { faceState: "none" }
  if (faces.length > 1) return { faceState: "multiple" }

  const matrix = result.facialTransformationMatrixes?.[0]
  let yaw = 0
  let pitch = 0
  let roll = 0
  if (matrix?.data) {
    const m = matrix.data
    // MediaPipe transformation matrix 4x4 (column-major). Decompose yaw/pitch/roll.
    // See pose_guide.py for the reference implementation.
    const r00 = m[0], r10 = m[1], r20 = m[2]
    const r01 = m[4], r11 = m[5], r21 = m[6]
    const r02 = m[8], r12 = m[9], r22 = m[10]
    const sy = Math.sqrt(r00 * r00 + r10 * r10)
    const singular = sy < 1e-6
    if (!singular) {
      pitch = Math.atan2(-r20, sy)
      yaw = Math.atan2(r10, r00)
      roll = Math.atan2(r21, r22)
    } else {
      pitch = Math.atan2(-r20, sy)
      yaw = Math.atan2(-r12, r11)
      roll = 0
    }
    yaw = (yaw * 180) / Math.PI
    pitch = (pitch * 180) / Math.PI
    roll = (roll * 180) / Math.PI
  }
  return { faceState: "single", yaw, pitch, roll, landmarks: faces[0] }
}
