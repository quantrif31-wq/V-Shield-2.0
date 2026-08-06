import fs from 'node:fs'
import path from 'node:path'
import { roleCredentials, uatEnvironment, uatRoles } from '../uat/helpers/environment.js'

const roots = ['dist', 'playwright-report', 'test-results', 'uat-report', 'uat-results']
if (uatEnvironment.ciArtifactPath) roots.push(uatEnvironment.ciArtifactPath)

const secretValues = []
for (const role of uatRoles) {
  const credentials = roleCredentials(role)
  for (const [label, value] of Object.entries({
    [`${credentials.variablePrefix}_USERNAME`]: credentials.username,
    [`${credentials.variablePrefix}_PASSWORD`]: credentials.password,
    [`${credentials.variablePrefix}_TOTP_SECRET`]: credentials.totpSecret,
    [`${credentials.variablePrefix}_OTP`]: credentials.manualOtp,
  })) if (String(value || '').length >= 4) secretValues.push({ label, value: String(value) })
}
if (uatEnvironment.mfaTestApiKey.length >= 4) secretValues.push({ label: 'VSHIELD_UAT_MFA_TEST_API_KEY', value: uatEnvironment.mfaTestApiKey })

const signatures = [
  ['JWT or bearer token', /(?:Bearer\s+)?eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}(?:\.[A-Za-z0-9_-]{8,})?/i],
  ['authorization header', /["']?authorization["']?\s*[:=]\s*["']?(?:Bearer|Basic)\s+[^\s"']+/i],
  ['stored V-Shield session', /v_shield_(?:token|refresh_token)["']?\s*[:=]\s*["'][^"']+/i],
  ['TOTP provisioning URI', /otpauth:\/\/totp\//i],
  ['secret query parameter', /[?&](?:token|access_token|refresh_token|secret|otp|mfa|qr)=[^&#\s]+/i],
  ['private key', /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/i],
]
const structuredPiiSignatures = [
  ['email/PII', /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/i],
  ['Vietnamese phone/PII', /(?<!\d)(?:\+84\d{9,10}|0\d{9,10})(?!\d)/],
]
const structuredExtensions = new Set(['.json', '.txt', '.log', '.xml'])

const findings = []
let scannedFiles = 0
for (const rootValue of roots) {
  const root = path.resolve(rootValue)
  if (!fs.existsSync(root)) continue
  const stack = [root]
  while (stack.length) {
    const current = stack.pop()
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const file = path.join(current, entry.name)
      if (entry.isDirectory()) { stack.push(file); continue }
      scannedFiles += 1
      const content = fs.readFileSync(file).toString('utf8')
      const relative = path.relative(process.cwd(), file)
      for (const secret of secretValues) if (content.includes(secret.value)) findings.push(`${relative}: contains ${secret.label}`)
      for (const [label, pattern] of signatures) if (pattern.test(content)) findings.push(`${relative}: ${label}`)
      if (structuredExtensions.has(path.extname(file).toLowerCase())) {
        for (const [label, pattern] of structuredPiiSignatures) if (pattern.test(content)) findings.push(`${relative}: ${label}`)
      }
    }
  }
}

if (findings.length) {
  console.error(`Sensitive artifact scan failed (${findings.length} finding(s)). Values are intentionally omitted.\n- ${[...new Set(findings)].join('\n- ')}`)
  process.exit(1)
}
console.log(`Sensitive artifact scan passed: ${scannedFiles} files across available production/test/CI artifact roots.`)
