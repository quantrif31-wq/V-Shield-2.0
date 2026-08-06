import fs from 'node:fs'
import { expect, test } from '@playwright/test'
import { bearer, loginViaApi } from './helpers/auth.js'
import { apiUrl, uatEnvironment } from './helpers/environment.js'

const modules = [
  ['Employees', '/Employees'],
  ['Visitors', '/pre-registrations'],
  ['Vehicles', '/Vehicles'],
  ['Access Logs', '/access-logs'],
  ['Device Management', '/device-management/overview'],
  ['Watchlist Queue', '/enterprise/visitor-vehicle/watchlist-entries'],
  ['AI Review Queue', '/enterprise/situational-awareness/ai-adjudications'],
  ['Redaction Queue', '/enterprise/evidence/redaction-requests'],
  ['Operations Dashboard', '/enterprise/operations/overview'],
]

test('nine migrated modules reach real list/filter/pagination endpoints', async ({ request }) => {
  const { token } = await loginViaApi(request)
  for (const [name, path] of modules) {
    const response = await request.get(apiUrl(path), { headers: bearer(token), params: { page: 1, pageSize: 5, sort: 'id', search: '__uat_no_match__' } })
    expect(response.status(), `${name} integration`).toBeLessThan(500)
    expect([200, 204]).toContain(response.status())
  }
})

test('backend enforces unauthenticated, missing-resource and validation responses', async ({ request }) => {
  const unauthorized = await request.get(apiUrl('/Employees'))
  expect(unauthorized.status()).toBe(401)
  const { token } = await loginViaApi(request)
  const missing = await request.get(apiUrl('/Employees/2147483647'), { headers: bearer(token) })
  expect(missing.status()).toBe(404)
  const invalid = await request.post(apiUrl('/Employees'), { headers: bearer(token), data: {} })
  expect([400, 422]).toContain(invalid.status())
})

test('approved mutation/import/export/upload/conflict/timeout cases execute with cleanup', async ({ request }) => {
  expect(uatEnvironment.allowMutations).toBe(true)
  const manifest = JSON.parse(uatEnvironment.mutationManifestJson || fs.readFileSync(uatEnvironment.mutationManifestPath, 'utf8'))
  const variables = { timestamp: new Date().toISOString().replace(/\D/g, '').slice(0, 14) }
  const render = value => JSON.parse(JSON.stringify(value ?? {}).replace(/\{\{(\w+)\}\}/g, (_, key) => variables[key] ?? ''))
  const cleanup = []
  const sessions = new Map()
  const tokenFor = async role => {
    if (!sessions.has(role)) sessions.set(role, await loginViaApi(request, role))
    return sessions.get(role).token
  }
  try {
    for (const item of manifest.cases) {
      expect(item.allowedTenant, `${item.name} tenant boundary`).toBe(uatEnvironment.tenant)
      expect(item.allowedSite, `${item.name} site boundary`).toBe(uatEnvironment.site)
      const token = await tokenFor(item.role)
      const renderedPath = render(item.path)
      const fullTarget = apiUrl(renderedPath)
      for (const forbidden of item.forbiddenTargets) expect(fullTarget.toLowerCase(), `${item.name} forbidden target`).not.toContain(String(forbidden).toLowerCase())
      const response = await request.fetch(apiUrl(render(item.path)), {
        method: item.method,
        headers: { ...bearer(token), ...(item.headers || {}) },
        data: render(item.body),
        timeout: item.timeoutMs || 30_000,
      }).catch(error => ({ status: () => 0, error }))
      expect(item.expectedStatuses, item.name).toContain(response.status())
      if (item.capture && response.status() !== 0) {
        const payload = await response.json()
        variables[item.capture.as] = item.capture.path.split('.').reduce((value, key) => value?.[key], payload)
      }
      const audit = await request.fetch(apiUrl(render(item.auditCheck.path)), {
        method: item.auditCheck.method,
        headers: bearer(token),
        data: render(item.auditCheck.body),
      })
      expect(item.auditCheck.expectedStatuses, `${item.name} audit record`).toContain(audit.status())
      if (item.cleanup) cleanup.unshift({ ...item.cleanup, role: item.role })
    }
  } finally {
    for (const item of cleanup) {
      const token = await tokenFor(item.role)
      const response = await request.fetch(apiUrl(render(item.path)), { method: item.method, headers: bearer(token), data: render(item.body) })
      expect(item.expectedStatuses || [200, 204, 404], `cleanup ${item.path}`).toContain(response.status())
    }
  }
})
