import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('@mediapipe/tasks-vision', () => ({
  FaceLandmarker: { createFromOptions: vi.fn() },
  FilesetResolver: { forVisionTasks: vi.fn().mockResolvedValue({}) },
}))

let landmarker
let mediapipe

beforeEach(async () => {
  vi.resetModules()
  mediapipe = await import('@mediapipe/tasks-vision')
  landmarker = await import('../faceLandmarker')
})

afterEach(() => vi.clearAllMocks())

describe('classifyAngle', () => {
  it('classifies yaw and pitch into the five angle bins', () => {
    expect(landmarker.classifyAngle(0, 0)).toBe('straight')
    expect(landmarker.classifyAngle(-30, 0)).toBe('left')
    expect(landmarker.classifyAngle(30, 0)).toBe('right')
    expect(landmarker.classifyAngle(0, -30)).toBe('up')
    expect(landmarker.classifyAngle(0, 30)).toBe('down')
    expect(landmarker.classifyAngle(5, 5)).toBe('straight')
    expect(landmarker.classifyAngle(-9, 0)).toBe('straight')
  })
})

describe('detectFace', () => {
  it('returns none when no landmarker is loaded or the video is empty', () => {
    expect(landmarker.detectFace(null)).toEqual({ faceState: 'none' })
    expect(landmarker.detectFace({ videoWidth: 0 })).toEqual({ faceState: 'none' })
  })

  it('returns none when detection throws', async () => {
    mediapipe.FaceLandmarker.createFromOptions.mockResolvedValueOnce({
      detectForVideo: () => { throw new Error('boom') },
    })
    await landmarker.loadLandmarker()
    expect(landmarker.detectFace({ videoWidth: 640 })).toEqual({ faceState: 'none' })
  })

  it('returns none when no faces are detected', async () => {
    mediapipe.FaceLandmarker.createFromOptions.mockImplementation(() => Promise.resolve({
      detectForVideo: () => ({ faceLandmarks: [] }),
    }))
    await landmarker.loadLandmarker()
    expect(landmarker.detectFace({ videoWidth: 640 })).toEqual({ faceState: 'none' })
  })

  it('returns multiple when several faces are detected', async () => {
    mediapipe.FaceLandmarker.createFromOptions.mockImplementation(() => Promise.resolve({
      detectForVideo: () => ({ faceLandmarks: [{}, {}] }),
    }))
    await landmarker.loadLandmarker()
    expect(landmarker.detectFace({ videoWidth: 640 })).toEqual({ faceState: 'multiple' })
  })

  it('decomposes a single face transformation matrix into angles', async () => {
    const identity = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
    mediapipe.FaceLandmarker.createFromOptions.mockImplementation(() => Promise.resolve({
      detectForVideo: () => ({
        faceLandmarks: [{ x: 0.5, y: 0.5 }],
        facialTransformationMatrixes: [{ data: identity }],
      }),
    }))
    await landmarker.loadLandmarker()
    const result = landmarker.detectFace({ videoWidth: 640 })
    expect(result.faceState).toBe('single')
    expect(result.yaw).toBeCloseTo(0)
    expect(result.pitch).toBeCloseTo(0)
    expect(result.roll).toBeCloseTo(0)
    expect(result.landmarks).toEqual({ x: 0.5, y: 0.5 })
  })

  it('handles the gimbal-lock singular decomposition path', async () => {
    const singular = [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1]
    mediapipe.FaceLandmarker.createFromOptions.mockImplementation(() => Promise.resolve({
      detectForVideo: () => ({
        faceLandmarks: [{}],
        facialTransformationMatrixes: [{ data: singular }],
      }),
    }))
    await landmarker.loadLandmarker()
    const result = landmarker.detectFace({ videoWidth: 640 })
    expect(result.faceState).toBe('single')
    expect(Number.isFinite(result.yaw)).toBe(true)
    expect(result.yaw).toBeCloseTo(0)
    expect(result.roll).toBe(0)
  })
})
