import { getAll as getAllEmployees } from './employeeApi'
import { getGuestProfiles } from './guestProfileApi'

const providers = [
  async (query) => {
    const response = await getAllEmployees({ search: query })
    return (response.data || []).slice(0, 6).map((employee) => ({ id: `emp_${employee.employeeId}`, type: 'employee', name: employee.fullName, sub: employee.departmentName || 'Chưa gán phòng ban', badge: 'Nhân sự' }))
  },
  async (query) => {
    const response = await getGuestProfiles({ query, page: 1, pageSize: 6 })
    return (response.data?.items || []).map((guest) => ({ id: `guest_${guest.guestId}`, type: 'guest', name: guest.fullName, sub: guest.phone || guest.defaultLicensePlate || 'Hồ sơ khách', badge: 'Khách' }))
  },
]

export async function searchGlobal(query) {
  const keyword = String(query || '').trim()
  if (!keyword) return []
  const settled = await Promise.allSettled(providers.map((provider) => provider(keyword)))
  const results = settled.flatMap((result) => result.status === 'fulfilled' ? result.value : [])
  if (!results.length && settled.every((result) => result.status === 'rejected')) throw settled[0].reason
  return results.slice(0, 12)
}
