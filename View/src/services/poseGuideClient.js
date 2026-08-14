// State machine hướng dẫn quay 5 góc — tái hiện pose_guide.py trên client.

import { FIVE_ANGLES, classifyAngle } from "./faceLandmarker"

export class PoseGuideClient {
  constructor(minFramesPerBin = 3) {
    this.minFramesPerBin = minFramesPerBin
    this.reset()
  }

  reset() {
    this.binFrames = {}
    this.covered = new Set()
    this.currentBin = null
    this.complete = false
    this.targetBin = null
  }

  update(yaw, pitch) {
    const bin = classifyAngle(yaw, pitch)
    this.currentBin = bin
    if (FIVE_ANGLES.includes(bin)) {
      this.binFrames[bin] = (this.binFrames[bin] || 0) + 1
      if (this.binFrames[bin] >= this.minFramesPerBin) {
        this.covered.add(bin)
      }
    }
    this.complete = FIVE_ANGLES.every((a) => this.covered.has(a))

    const covered = FIVE_ANGLES.filter((a) => this.covered.has(a))
    const missing = FIVE_ANGLES.filter((a) => !this.covered.has(a))
    this.targetBin = this.complete ? null : this.nearestUncovered(bin)

    return {
      guidance: this.guidanceFor(bin),
      progress: covered.length,
      total: FIVE_ANGLES.length,
      currentBin: bin,
      coveredAngles: covered,
      missingAngles: missing,
      complete: this.complete
    }
  }

  guidanceFor(bin) {
    if (this.complete) return "Đã đủ 5 góc — có thể gửi đăng ký"
    if (!this.targetBin) return "Nhìn thẳng vào camera"
    if (bin === this.targetBin) return "Giữ yên, đang thu góc này"

    const yawOrder = { L: 0, C: 1, R: 2 }
    const pitchOrder = { U: 0, M: 1, D: 2 }
    const cur = bin
    const tgt = this.targetBin
    const yawGap = yawOrder[tgt[0]] - yawOrder[cur[0]]
    const pitchGap = pitchOrder[tgt[1]] - pitchOrder[cur[1]]

    if (Math.abs(yawGap) >= Math.abs(pitchGap) && yawGap !== 0) {
      return yawGap < 0 ? "Từ từ quay mặt sang TRÁI" : "Từ từ quay mặt sang PHẢI"
    }
    if (pitchGap !== 0) {
      return pitchGap < 0 ? "Ngẩng mặt nhẹ lên trên" : "Cúi mặt nhẹ xuống dưới"
    }
    return "Nhìn thẳng vào camera"
  }

  nearestUncovered(fromBin) {
    const missing = FIVE_ANGLES.filter((a) => !this.covered.has(a))
    if (!missing.length) return null
    const yawOrder = { L: 0, C: 1, R: 2 }
    const pitchOrder = { U: 0, M: 1, D: 2 }
    const dist = (a, b) =>
      Math.abs(yawOrder[a[0]] - yawOrder[b[0]]) + Math.abs(pitchOrder[a[1]] - pitchOrder[b[1]])
    return missing.reduce((best, a) => (dist(fromBin, a) < dist(fromBin, best) ? a : best), missing[0])
  }
}
