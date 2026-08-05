import fs from 'node:fs'
import { expect } from '@playwright/test'
import { generateTotp } from './totp.js'
import { apiUrl, roleCredentials, uatEnvironment } from './environment.js'

export const credentialsFor = roleCredentials

export async function fillSensitiveInput(locator, value, { paste = false } = {}) {
  await locator.evaluate((element, payload) => {
    if (payload.paste) {
      const clipboard = new DataTransfer()
      clipboard.setData('text/plain', payload.value)
      element.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: clipboard }))
    }
    const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set
    setter?.call(element, payload.value)
    element.dispatchEvent(new Event('input', { bubbles: true }))
    element.dispatchEvent(new Event('change', { bubbles: true }))
  }, { value, paste })
}

async function mfaCode(request, role, credentials) {
  if (credentials.totpSecret) return generateTotp(credentials.totpSecret)
  if (credentials.manualOtp) return credentials.manualOtp
  if (uatEnvironment.mfaMode === 'test-api') {
    const response = await request.post(uatEnvironment.mfaTestApiUrl, {
      headers: { 'X-UAT-Test-Key': uatEnvironment.mfaTestApiKey },
      data: { username: credentials.username, tenant: uatEnvironment.tenant, site: uatEnvironment.site },
    })
    if (!response.ok()) throw new Error(`Approved UAT MFA API failed for ${role} with HTTP ${response.status()}.`)
    const payload = await response.json()
    if (!/^\d{6}$/.test(String(payload.otp || ''))) throw new Error('Approved UAT MFA API returned an invalid OTP shape.')
    return String(payload.otp)
  }
  throw new Error(`${credentials.variablePrefix} requires an approved TOTP, one-time OTP, test API or storage-state mechanism.`)
}

function readStorageState(credentials) {
  if (!credentials.storageStatePath) return null
  const state = JSON.parse(fs.readFileSync(credentials.storageStatePath, 'utf8'))
  return state
}

function tokenFromStorageState(state) {
  for (const origin of state?.origins || []) {
    const entry = (origin.localStorage || []).find(item => item.name === 'v_shield_token')
    if (entry?.value) return entry.value
  }
  return ''
}

export async function loginViaApi(request, role = 'Admin') {
  const credentials = roleCredentials(role)
  const storedState = readStorageState(credentials)
  const storedToken = tokenFromStorageState(storedState)
  if (storedToken) return { token: storedToken, refreshToken: null, user: { role }, fromStorageState: true }

  const loginUrl = apiUrl('/Auth/login')
  let response = await request.post(loginUrl, { data: { username: credentials.username, password: credentials.password } })
  expect(response.status(), `${role} username/password login`).toBeLessThan(500)
  let payload = await response.json()
  if (payload.requiresMfa) {
    response = await request.post(loginUrl, {
      data: { username: credentials.username, password: credentials.password, mfaCode: await mfaCode(request, role, credentials) },
    })
    payload = await response.json()
  }
  expect(response.ok(), `${role} authenticated API login`).toBeTruthy()
  expect(payload.token).toBeTruthy()
  return { token: payload.token, refreshToken: payload.refreshToken, user: payload }
}

async function restoreStorageState(page, state) {
  if (state.cookies?.length) await page.context().addCookies(state.cookies)
  const matchingOrigin = (state.origins || []).find(origin => new URL(origin.origin).origin === new URL(uatEnvironment.frontendUrl).origin)
  await page.goto('/login')
  if (matchingOrigin?.localStorage?.length) {
    await page.evaluate(entries => {
      for (const entry of entries) localStorage.setItem(entry.name, entry.value)
    }, matchingOrigin.localStorage)
    await page.reload()
  }
}

export async function loginViaUi(page, role = 'Admin') {
  const credentials = roleCredentials(role)
  const storedState = readStorageState(credentials)
  if (storedState) {
    await restoreStorageState(page, storedState)
    await expect(page).not.toHaveURL(/\/login(?:\?|$)/, { timeout: 15_000 })
    return
  }

  if (!new URL(page.url()).pathname.endsWith('/login')) await page.goto('/login')
  await fillSensitiveInput(page.getByLabel('Tên đăng nhập'), credentials.username)
  await fillSensitiveInput(page.getByLabel('Mật khẩu truy cập'), credentials.password)
  await page.getByRole('button', { name: /Vào trung tâm điều phối/i }).click()
  const mfa = page.getByLabel('Mã xác thực 6 số')
  if (await mfa.isVisible({ timeout: 3_000 }).catch(() => false)) {
    await fillSensitiveInput(mfa, await mfaCode(page.request, role, credentials), { paste: true })
    await page.getByRole('button', { name: /Xác thực và đăng nhập/i }).click()
  }
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/, { timeout: 15_000 })
}

export const bearer = token => ({ Authorization: `Bearer ${token}` })
