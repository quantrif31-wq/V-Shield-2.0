import { describe, it, expect, vi, beforeEach } from 'vitest'
import axios from 'axios'
import { portalApi } from '../portalApi'

vi.mock('axios', () => {
  const get = vi.fn()
  const post = vi.fn()
  return {
    default: {
      create: vi.fn(() => ({ get, post })),
      get,
      post
    }
  }
})

describe('portalApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('getOverview returns overview data or fallback', async () => {
    const overview = await portalApi.getOverview()
    expect(overview).toBeDefined()
    expect(overview.systemName).toBe('V-SHIELD 2.0')
    expect(overview.version).toBe('2.0.0')
  })

  it('getReviews returns review list', async () => {
    const reviews = await portalApi.getReviews()
    expect(Array.isArray(reviews)).toBe(true)
    expect(reviews.length).toBeGreaterThan(0)
    expect(reviews[0].rating).toBe(5)
  })

  it('createReview posts new review', async () => {
    const res = await portalApi.createReview({
      authorName: 'Tester',
      rating: 5,
      content: 'Tuyệt vời',
      platform: 'Web'
    })
    expect(res.success).toBe(true)
    expect(res.data.authorName).toBe('Tester')
  })

  it('getComments returns comments', async () => {
    const comments = await portalApi.getComments()
    expect(Array.isArray(comments)).toBe(true)
    expect(comments.length).toBeGreaterThan(0)
  })

  it('createComment posts new comment', async () => {
    const res = await portalApi.createComment({
      authorName: 'OperatorX',
      content: 'Hệ thống chạy mượt!'
    })
    expect(res.success).toBe(true)
    expect(res.data.authorName).toBe('OperatorX')
  })

  it('submitFeedback and subscribeNewsletter handle requests', async () => {
    const fbRes = await portalApi.submitFeedback({
      fullName: 'John',
      email: 'john@example.com',
      category: 'Feature',
      message: 'Add more themes'
    })
    expect(fbRes.success).toBe(true)

    const nlRes = await portalApi.subscribeNewsletter({ email: 'john@example.com' })
    expect(nlRes.success).toBe(true)
  })

  it('authGoogle returns community profile', async () => {
    const authRes = await portalApi.authGoogle({
      googleTokenOrEmail: 'operator1@gmail.com',
      fullName: 'Operator One'
    })
    expect(authRes.success).toBe(true)
    expect(authRes.data.email).toBe('operator1@gmail.com')
  })
})
