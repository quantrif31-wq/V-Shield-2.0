import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../guestProfileApi', () => ({ getGuestProfiles: vi.fn() }))

const { getAll } = await import('../employeeApi')
const { getGuestProfiles } = await import('../guestProfileApi')
const { searchGlobal } = await import('../globalSearchProviders')

beforeEach(() => vi.clearAllMocks())

describe('searchGlobal', () => {
  it('returns an empty list for blank queries', async () => {
    expect(await searchGlobal('  ')).toEqual([])
    expect(getAll).not.toHaveBeenCalled()
  })

  it('combines employee and guest matches', async () => {
    getAll.mockResolvedValue({
      data: [
        { employeeId: 1, fullName: 'Nguyễn A', departmentName: 'An Ninh' },
        { employeeId: 2, fullName: 'Trần B', departmentName: 'Lễ Tân' },
        { employeeId: 3, fullName: 'Lê C', departmentName: 'Nhân Sự' },
      ],
    })
    getGuestProfiles.mockResolvedValue({
      data: { items: [{ guestId: 11, fullName: 'Khách D', phone: '0901234567' }] },
    })
    const results = await searchGlobal('Nguyen')
    expect(results[0]).toMatchObject({ id: 'emp_1', type: 'employee', name: 'Nguyễn A' })
    expect(results[0].badge).toBe('Nhân sự')
    expect(results[1]).toMatchObject({ id: 'emp_2', type: 'employee', name: 'Trần B', sub: 'Lễ Tân' })
    expect(results[3]).toMatchObject({ id: 'guest_11', type: 'guest', name: 'Khách D' })
  })

  it('caps results at 12 entries', async () => {
    const employees = Array.from({ length: 6 }, (_, i) => ({ employeeId: i + 1, fullName: `User ${i}` }))
    getAll.mockResolvedValue({ data: employees })
    const guests = Array.from({ length: 20 }, (_, i) => ({ guestId: i + 100, fullName: `Guest ${i}` }))
    getGuestProfiles.mockResolvedValue({ data: { items: guests } })
    const results = await searchGlobal('User')
    expect(results.length).toBe(12)
  })

  it('drops rejected providers and still returns others', async () => {
    getAll.mockRejectedValue(new Error('fail'))
    getGuestProfiles.mockResolvedValue({
      data: { items: [{ guestId: 1, fullName: 'Khách' }] },
    })
    const results = await searchGlobal('x')
    expect(results).toHaveLength(1)
    expect(results[0].type).toBe('guest')
  })

  it('throws when every provider rejects', async () => {
    getAll.mockRejectedValue(new Error('emp down'))
    getGuestProfiles.mockRejectedValue(new Error('guest down'))
    await expect(searchGlobal('x')).rejects.toThrow('emp down')
  })
})
