import fs from 'node:fs'
import { expect, test } from '@playwright/test'
import { bearer, loginViaApi, loginViaUi } from './helpers/auth.js'
import { apiUrl, uatEnvironment, uatRoles } from './helpers/environment.js'

const fallbackMatrix = Object.fromEntries(uatRoles.map(role => [role, {
  visibleMenus: [], hiddenMenus: [], allowedRoutes: ['/chat'], deniedRoutes: [], apiChecks: [],
}]))
const matrix = uatEnvironment.roleMatrixJson
  ? JSON.parse(uatEnvironment.roleMatrixJson)
  : uatEnvironment.roleMatrixPath && fs.existsSync(uatEnvironment.roleMatrixPath)
    ? JSON.parse(fs.readFileSync(uatEnvironment.roleMatrixPath, 'utf8'))
    : fallbackMatrix

const readPath = (value, fieldPath) => String(fieldPath).split('.').reduce((current, key) => current?.[key], value)

test('Admin context is the approved UAT tenant and site', async ({ request }) => {
  test.skip(!matrix.contextCheck, 'Protected role matrix context check is required by preflight.')
  const { token } = await loginViaApi(request, 'Admin')
  const response = await request.fetch(apiUrl(matrix.contextCheck.path), { method: matrix.contextCheck.method, headers: bearer(token) })
  expect(response.ok(), 'tenant/site context endpoint').toBeTruthy()
  const payload = await response.json()
  const tenantValues = matrix.contextCheck.tenantFieldPaths.map(field => readPath(payload, field)).filter(Boolean).map(String)
  const siteValues = matrix.contextCheck.siteFieldPaths.map(field => readPath(payload, field)).filter(Boolean).map(String)
  expect(tenantValues, 'approved UAT tenant').toContain(uatEnvironment.tenant)
  expect(siteValues, 'approved UAT site').toContain(uatEnvironment.site)
})

for (const role of uatRoles) {
  test(`${role}: real sidebar, direct routes and API action matrix`, async ({ page, request }) => {
    const policy = matrix[role]
    await loginViaUi(page, role)

    const sidebar = page.locator('aside, .sidebar').first()
    for (const label of policy.visibleMenus) await expect(sidebar.getByText(label, { exact: true })).toBeVisible()
    for (const label of policy.hiddenMenus) await expect(sidebar.getByText(label, { exact: true })).toHaveCount(0)
    for (const route of policy.allowedRoutes) {
      await page.goto(route)
      await expect(page).toHaveURL(new RegExp(`${route.replaceAll('/', '\\/')}(?:[/?]|$)`))
      await expect(page.locator('main, .main-content').first()).toBeVisible()
    }
    for (const route of policy.deniedRoutes) {
      await page.goto(route)
      await expect(page).not.toHaveURL(new RegExp(`${route.replaceAll('/', '\\/')}(?:[/?]|$)`))
    }

    const { token } = await loginViaApi(request, role)
    for (const check of policy.apiChecks) {
      expect(check.probeOnly, `${role}/${check.action} must be a non-mutating permission probe`).toBe(true)
      const response = await request.fetch(apiUrl(check.path), {
        method: check.method,
        headers: bearer(token),
        data: check.body,
      })
      expect(check.expectedStatuses, `${role}: ${check.name}`).toContain(response.status())
    }
  })
}
