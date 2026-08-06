import { expect, test } from '@playwright/test'
import { credentialsFor, fillSensitiveInput, loginViaApi, loginViaUi } from './helpers/auth.js'
import { generateTotp } from './helpers/totp.js'
import { apiUrl, uatEnvironment } from './helpers/environment.js'

test('deep-link survives production username/password and MFA flow', async ({ page }) => {
  await page.goto('/employees?status=active')
  await expect(page).toHaveURL(/\/login\?redirect=/)
  await loginViaUi(page, 'Admin')
  await expect(page).toHaveURL(/\/employees\?status=active$/)
})

test('MFA input accepts paste and rejects a bad code without leaking it', async ({ page }) => {
  const account = credentialsFor('Admin')
  test.skip(!account.totpSecret, 'Bad/expired TOTP boundary requires the approved TOTP mechanism.')
  await page.goto('/login')
  await fillSensitiveInput(page.getByLabel('Tên đăng nhập'), account.username)
  await fillSensitiveInput(page.getByLabel('Mật khẩu truy cập'), account.password)
  await page.getByRole('button', { name: /Vào trung tâm điều phối/i }).click()
  const field = page.getByLabel('Mã xác thực 6 số')
  await expect(field).toBeVisible()
  await fillSensitiveInput(field, '000000', { paste: true })
  await page.getByRole('button', { name: /Xác thực và đăng nhập/i }).click()
  await expect(page.getByRole('alert')).toContainText(/không đúng|hết hạn/i)
  await fillSensitiveInput(field, generateTotp(account.totpSecret), { paste: true })
  await page.getByRole('button', { name: /Xác thực và đăng nhập/i }).click()
  await expect(page).not.toHaveURL(/\/login/)
})

test('an expired MFA code is rejected before a current code succeeds', async ({ page }) => {
  const account = credentialsFor('Admin')
  test.skip(!account.totpSecret, 'Expired TOTP boundary requires the approved TOTP mechanism.')
  await page.goto('/login')
  await fillSensitiveInput(page.getByLabel('Tên đăng nhập'), account.username)
  await fillSensitiveInput(page.getByLabel('Mật khẩu truy cập'), account.password)
  await page.getByRole('button', { name: /Vào trung tâm điều phối/i }).click()
  const field = page.getByLabel('Mã xác thực 6 số')
  await fillSensitiveInput(field, generateTotp(account.totpSecret, Date.now() - 90_000), { paste: true })
  await page.getByRole('button', { name: /Xác thực và đăng nhập/i }).click()
  await expect(page.getByRole('alert')).toContainText(/không đúng|hết hạn/i)
  await fillSensitiveInput(field, generateTotp(account.totpSecret), { paste: true })
  await page.getByRole('button', { name: /Xác thực và đăng nhập/i }).click()
  await expect(page).not.toHaveURL(/\/login/)
})

test('refresh and logout use the real backend and revoke the session', async ({ request }) => {
  const session = await loginViaApi(request)
  test.skip(session.fromStorageState || !session.refreshToken, 'Refresh-token flow requires credential login rather than pre-created storage state.')
  const refreshed = await request.post(apiUrl('/Auth/refresh'), { data: { refreshToken: session.refreshToken } })
  expect(refreshed.ok()).toBeTruthy()
  const refreshedPayload = await refreshed.json()
  const logout = await request.post(apiUrl('/Auth/logout'), { data: { refreshToken: refreshedPayload.refreshToken || session.refreshToken }, headers: { Authorization: `Bearer ${refreshedPayload.token}` } })
  expect(logout.ok()).toBeTruthy()
  const me = await request.get(apiUrl('/Auth/me'), { headers: { Authorization: `Bearer ${refreshedPayload.token}` } })
  expect([401, 403]).toContain(me.status())
})

test('wrong password is rejected without redirect loop', async ({ page }) => {
  const account = credentialsFor('Admin')
  await page.goto('/login')
  await fillSensitiveInput(page.getByLabel('Tên đăng nhập'), account.username)
  await fillSensitiveInput(page.getByLabel('Mật khẩu truy cập'), `invalid-${Date.now()}`)
  await page.getByRole('button', { name: /Vào trung tâm điều phối/i }).click()
  await expect(page.getByRole('alert')).toContainText(/không đúng|đăng nhập/i)
  await expect(page).toHaveURL(/\/login/)
})

test('external redirect is blocked and authenticated refresh remains usable', async ({ page }) => {
  await page.goto('/login?redirect=https://example.com/phishing')
  await loginViaUi(page, 'Admin')
  expect(new URL(page.url()).origin).toBe(new URL(uatEnvironment.frontendUrl).origin)
  await page.reload()
  await expect(page).not.toHaveURL(/\/login/)
  await expect(page.locator('main, .main-content').first()).toBeVisible()
})
