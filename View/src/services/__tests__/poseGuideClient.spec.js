import { describe, expect, it } from 'vitest'
import { PoseGuideClient } from '../poseGuideClient'

describe('PoseGuideClient', () => {
  it('starts reset with no coverage', () => {
    const guide = new PoseGuideClient()
    expect(guide.covered.size).toBe(0)
    expect(guide.complete).toBe(false)
    expect(guide.targetBin).toBeNull()
    expect(guide.currentBin).toBeNull()
  })

  it('gives a straight-ahead hint before any target is chosen', () => {
    const guide = new PoseGuideClient()
    expect(guide.guidanceFor('straight')).toBe('Nhìn thẳng vào camera')
  })

  it('collects angles across updates and reports progress', () => {
    const guide = new PoseGuideClient(1)
    const first = guide.update(0, 0)
    expect(first.currentBin).toBe('straight')
    expect(first.progress).toBe(1)
    expect(first.coveredAngles).toContain('straight')
    expect(first.missingAngles).not.toContain('straight')

    guide.update(-30, 0)
    guide.update(30, 0)
    guide.update(0, -30)
    const last = guide.update(0, 30)
    expect(last.progress).toBe(5)
    expect(last.complete).toBe(true)
    expect(guide.targetBin).toBeNull()
    expect(guide.complete).toBe(true)
  })

  it('requires minFramesPerBin frames before an angle counts', () => {
    const guide = new PoseGuideClient(2)
    guide.update(0, 0)
    expect(guide.covered.has('straight')).toBe(false)
    guide.update(0, 0)
    expect(guide.covered.has('straight')).toBe(true)
  })

  it('announces completion in guidance text', () => {
    const guide = new PoseGuideClient(1)
    const poses = [[0, 0], [-30, 0], [30, 0], [0, -30], [0, 30]]
    poses.forEach(([yaw, pitch]) => guide.update(yaw, pitch))
    expect(guide.complete).toBe(true)
    expect(guide.guidanceFor('straight')).toBe('Đã đủ 5 góc — có thể gửi đăng ký')
  })

  it('reset wipes accumulated coverage', () => {
    const guide = new PoseGuideClient(1)
    guide.update(0, 0)
    expect(guide.covered.size).toBe(1)
    guide.reset()
    expect(guide.covered.size).toBe(0)
    expect(guide.complete).toBe(false)
  })
})
