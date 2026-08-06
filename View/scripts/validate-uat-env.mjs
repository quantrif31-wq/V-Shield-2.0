import crypto from 'node:crypto'
import fs from 'node:fs'
import path from 'node:path'
import { apiUrl, roleCredentials, uatEnvironment, uatRoles } from '../uat/helpers/environment.js'

const smokeOnly = process.argv.includes('--smoke')
const missing = []
const failures = []
const report = {
  timestamp: new Date().toISOString(),
  mode: smokeOnly ? 'smoke' : 'full',
  status: 'failed',
  checks: {},
}

function requireValue(label, value) {
  if (!String(value || '').trim()) missing.push(label)
}

function validateUrl(label, value, protocols = ['https:', 'http:']) {
  if (!value) return
  try {
    const url = new URL(value)
    if (!protocols.includes(url.protocol)) failures.push(`${label}: unsupported protocol`)
  } catch {
    failures.push(`${label}: invalid URL`)
  }
}

function healthUrl() {
  if (uatEnvironment.apiHealthUrl) return uatEnvironment.apiHealthUrl
  const url = new URL(uatEnvironment.apiUrl)
  url.pathname = `${url.pathname.replace(/\/api\/?$/i, '').replace(/\/$/, '')}/health/ready`
  url.search = ''
  return url.toString()
}

function loadManifest() {
  if (uatEnvironment.mutationManifestJson) return JSON.parse(uatEnvironment.mutationManifestJson)
  return JSON.parse(fs.readFileSync(uatEnvironment.mutationManifestPath, 'utf8'))
}

function validateMutationManifest(manifest) {
  const requiredKinds = ['create', 'update', 'delete', 'import', 'export', 'upload', 'validation', 'conflict', 'timeout']
  const kinds = new Set((manifest.cases || []).map(item => String(item.kind || '').toLowerCase()))
  for (const kind of requiredKinds) if (!kinds.has(kind)) failures.push(`mutation manifest: missing kind ${kind}`)
  if (!Array.isArray(manifest.cases) || !manifest.cases.length) failures.push('mutation manifest: cases must be a non-empty array')

  for (const [index, item] of (manifest.cases || []).entries()) {
    const label = `mutation manifest case ${index + 1}`
    for (const field of ['module', 'role', 'action', 'testDataPrefix', 'allowedTenant', 'allowedSite', 'expectedAuditRecord', 'auditCheck', 'cleanup', 'rollback', 'forbiddenTargets']) {
      if (item[field] === undefined || item[field] === null || item[field] === '') failures.push(`${label}: missing ${field}`)
    }
    if (!/^UAT-RC1-(?:\{\{timestamp\}\}|\d{8,})-[A-Za-z0-9_-]+$/.test(String(item.testDataPrefix || ''))) {
      failures.push(`${label}: testDataPrefix must match UAT-RC1-<timestamp>-<entity>`)
    }
    if (String(item.allowedTenant) !== uatEnvironment.tenant) failures.push(`${label}: allowedTenant does not match preflight tenant`)
    if (String(item.allowedSite) !== uatEnvironment.site) failures.push(`${label}: allowedSite does not match preflight site`)
    if (!Array.isArray(item.expectedStatuses) || !item.expectedStatuses.length) failures.push(`${label}: expectedStatuses must be a non-empty array`)
    if (!item.cleanup?.method || !item.cleanup?.path) failures.push(`${label}: cleanup method/path required`)
    if (!item.auditCheck?.method || !item.auditCheck?.path || !Array.isArray(item.auditCheck?.expectedStatuses)) failures.push(`${label}: auditCheck method/path/expectedStatuses required`)
    if (!item.rollback?.method && !item.rollback?.procedure) failures.push(`${label}: rollback method or procedure required`)
    if (!Array.isArray(item.forbiddenTargets) || !item.forbiddenTargets.length) failures.push(`${label}: forbiddenTargets must be non-empty`)
    const targetMaterial = `${uatEnvironment.frontendUrl} ${uatEnvironment.apiUrl} ${uatEnvironment.signalRUrl}`.toLowerCase()
    for (const forbidden of item.forbiddenTargets || []) {
      if (targetMaterial.includes(String(forbidden).toLowerCase())) failures.push(`${label}: configured UAT target matches forbidden target`)
    }
    if (!JSON.stringify({ path: item.path, body: item.body }).includes(String(item.testDataPrefix || ''))) failures.push(`${label}: testDataPrefix must be present in path or body`)
  }
}

function loadRoleMatrix() {
  if (uatEnvironment.roleMatrixJson) return JSON.parse(uatEnvironment.roleMatrixJson)
  return JSON.parse(fs.readFileSync(uatEnvironment.roleMatrixPath, 'utf8'))
}

function validateRoleMatrix(matrix) {
  if (!matrix.contextCheck?.method || !matrix.contextCheck?.path || !Array.isArray(matrix.contextCheck?.tenantFieldPaths) || !Array.isArray(matrix.contextCheck?.siteFieldPaths)) {
    failures.push('role matrix: contextCheck method/path/tenantFieldPaths/siteFieldPaths required')
  }
  const requiredActions = ['read-list', 'read-detail', 'create', 'edit', 'delete', 'approve', 'reject', 'import', 'export', 'upload', 'evidence', 'face-id', 'device-config', 'watchlist', 'redaction', 'backup']
  for (const role of uatRoles) {
    const policy = matrix[role]
    if (!policy) { failures.push(`role matrix: missing ${role}`); continue }
    for (const field of ['visibleMenus', 'hiddenMenus', 'allowedRoutes', 'deniedRoutes', 'apiChecks']) {
      if (!Array.isArray(policy[field])) failures.push(`role matrix ${role}: ${field} must be an array`)
    }
    const actions = new Set((policy.apiChecks || []).map(item => item.action))
    for (const action of requiredActions) if (!actions.has(action)) failures.push(`role matrix ${role}: missing API action ${action}`)
    for (const [index, check] of (policy.apiChecks || []).entries()) {
      if (!check.name || !check.method || !check.path || !Array.isArray(check.expectedStatuses)) failures.push(`role matrix ${role} API check ${index + 1}: incomplete contract`)
      if (check.probeOnly !== true) failures.push(`role matrix ${role} API check ${index + 1}: probeOnly=true required; real mutations belong in mutation manifest`)
    }
  }
}

function loadPerformancePlan() {
  if (uatEnvironment.performancePlanJson) return JSON.parse(uatEnvironment.performancePlanJson)
  return JSON.parse(fs.readFileSync(uatEnvironment.performancePlanPath, 'utf8'))
}

function validatePerformancePlan(plan) {
  if (!Number.isInteger(plan.iterations) || plan.iterations < 20 || plan.iterations > 30) failures.push('performance plan: iterations must be 20–30')
  if (!Array.isArray(plan.routes) || !plan.routes.length) failures.push('performance plan: routes must be non-empty')
  for (const [index, route] of (plan.routes || []).entries()) {
    if (!route.path || !route.interactionSelector) failures.push(`performance plan route ${index + 1}: path and safe interactionSelector required`)
    if (!['focus', 'click'].includes(route.interactionType || 'focus')) failures.push(`performance plan route ${index + 1}: interactionType must be focus or click`)
    if (route.interactionType === 'click' && route.approvedAction !== true) failures.push(`performance plan route ${index + 1}: approvedAction=true required for click`)
  }
  if (!(plan.routes || []).some(route => route.interactionType === 'click')) failures.push('performance plan: at least one approved click interaction is required for INP')
  if (!plan.map?.route || !plan.map?.metric) failures.push('performance plan: map route/metric required')
  if (!plan.camera?.route || !plan.camera?.metric || plan.camera.approved !== true) failures.push('performance plan: approved camera route/metric required')
  if (!Array.isArray(plan.networkProfiles) || !plan.networkProfiles.includes('corporate') || !plan.networkProfiles.includes('slow')) failures.push('performance plan: corporate and slow network profiles required')
  if (!uatEnvironment.networkProfile || !plan.networkProfiles?.includes(uatEnvironment.networkProfile)) failures.push('VSHIELD_UAT_NETWORK_PROFILE must select a profile in the performance plan')
}

async function probe(label, url, options, accepted) {
  const started = performance.now()
  try {
    const response = await fetch(url, { ...options, signal: AbortSignal.timeout(10_000) })
    const ok = accepted(response.status)
    report.checks[label] = { ok, httpStatus: response.status, durationMs: Math.round(performance.now() - started) }
    if (!ok) failures.push(`${label}: unexpected HTTP ${response.status}`)
    return response
  } catch (error) {
    report.checks[label] = { ok: false, error: error?.name || 'network_error' }
    failures.push(`${label}: network unavailable or timed out`)
    return null
  }
}

async function probeAccount(role) {
  const credentials = roleCredentials(role)
  const response = await fetch(apiUrl('/Auth/login'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: credentials.username, password: credentials.password }),
    signal: AbortSignal.timeout(10_000),
  }).catch(() => null)
  if (!response) {
    failures.push(`account ${role}: login endpoint unavailable`)
    report.checks[`account_${role}`] = { ok: false, error: 'network_error' }
    return
  }
  let payload = {}
  try { payload = await response.json() } catch {}
  const exists = response.ok && (payload.requiresMfa || payload.token)
  report.checks[`account_${role}`] = { ok: !!exists, httpStatus: response.status, requiresMfa: !!payload.requiresMfa }
  if (!exists) failures.push(`account ${role}: credential probe was not accepted`)
  if (payload.token) {
    await fetch(apiUrl('/Auth/logout'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${payload.token}` },
      body: JSON.stringify({ refreshToken: payload.refreshToken || null }),
      signal: AbortSignal.timeout(10_000),
    }).catch(() => {})
  }
}

requireValue('VSHIELD_UAT_FRONTEND_URL', uatEnvironment.frontendUrl)
requireValue('VSHIELD_UAT_API_URL', uatEnvironment.apiUrl)
requireValue('VSHIELD_UAT_SIGNALR_URL', uatEnvironment.signalRUrl)
requireValue('VSHIELD_UAT_ENVIRONMENT=UAT', uatEnvironment.environment)
requireValue('VSHIELD_UAT_TENANT', uatEnvironment.tenant)
requireValue('VSHIELD_UAT_SITE', uatEnvironment.site)
requireValue('VSHIELD_UAT_EXPECTED_VERSION', uatEnvironment.expectedVersion)
requireValue('VSHIELD_UAT_RC_ARTIFACT_DIGEST', uatEnvironment.rcArtifactDigest)
requireValue('VSHIELD_UAT_PREVIOUS_ARTIFACT_DIGEST', uatEnvironment.previousArtifactDigest)
if (uatEnvironment.environment && uatEnvironment.environment.toUpperCase() !== 'UAT') failures.push('VSHIELD_UAT_ENVIRONMENT must equal UAT')

validateUrl('VSHIELD_UAT_FRONTEND_URL', uatEnvironment.frontendUrl)
validateUrl('VSHIELD_UAT_API_URL', uatEnvironment.apiUrl)
validateUrl('VSHIELD_UAT_SIGNALR_URL', uatEnvironment.signalRUrl, ['https:', 'http:', 'wss:', 'ws:'])

for (const role of (smokeOnly ? ['Admin'] : uatRoles)) {
  const credentials = roleCredentials(role)
  requireValue(`${credentials.variablePrefix}_USERNAME`, credentials.username)
  requireValue(`${credentials.variablePrefix}_PASSWORD`, credentials.password)
  if (uatEnvironment.mfaMode === 'totp') requireValue(`${credentials.variablePrefix}_TOTP_SECRET`, credentials.totpSecret)
  if (uatEnvironment.mfaMode === 'manual') requireValue(`${credentials.variablePrefix}_OTP`, credentials.manualOtp)
  if (uatEnvironment.mfaMode === 'storage-state') {
    requireValue(`${credentials.variablePrefix}_STORAGE_STATE_PATH`, credentials.storageStatePath)
    if (credentials.storageStatePath) {
      const statePath = path.resolve(credentials.storageStatePath)
      if (!fs.existsSync(statePath)) failures.push(`${credentials.variablePrefix}_STORAGE_STATE_PATH: file not found`)
      if (statePath.startsWith(path.resolve('.'))) failures.push(`${credentials.variablePrefix}_STORAGE_STATE_PATH: must be outside the repository`)
    }
  }
}
if (uatEnvironment.mfaMode === 'test-api') {
  requireValue('VSHIELD_UAT_MFA_TEST_API_URL', uatEnvironment.mfaTestApiUrl)
  requireValue('VSHIELD_UAT_MFA_TEST_API_KEY', uatEnvironment.mfaTestApiKey)
}
if (!['totp', 'manual', 'test-api', 'storage-state'].includes(uatEnvironment.mfaMode)) failures.push('VSHIELD_UAT_MFA_MODE is unsupported')

if (!smokeOnly) {
  if (!uatEnvironment.allowNegativeMfa) missing.push('VSHIELD_UAT_ALLOW_NEGATIVE_MFA=true')
  if (!uatEnvironment.allowMutations) missing.push('VSHIELD_UAT_ALLOW_MUTATIONS=true')
  requireValue('VSHIELD_UAT_CLEANUP_POLICY=required', uatEnvironment.cleanupPolicy)
  if (uatEnvironment.cleanupPolicy && uatEnvironment.cleanupPolicy !== 'required') failures.push('VSHIELD_UAT_CLEANUP_POLICY must equal required')
  if (!uatEnvironment.mutationManifestJson && !uatEnvironment.mutationManifestPath) missing.push('VSHIELD_UAT_MUTATION_MANIFEST_JSON or PATH')
  if (uatEnvironment.mutationManifestPath && !fs.existsSync(uatEnvironment.mutationManifestPath)) failures.push('mutation manifest path: file not found')
  if (!missing.length) {
    try { validateMutationManifest(loadManifest()) } catch { failures.push('mutation manifest: invalid JSON') }
  }
  if (!uatEnvironment.roleMatrixJson && !uatEnvironment.roleMatrixPath) missing.push('VSHIELD_UAT_ROLE_MATRIX_JSON or PATH')
  if (uatEnvironment.roleMatrixPath && !fs.existsSync(uatEnvironment.roleMatrixPath)) failures.push('role matrix path: file not found')
  if (!missing.length) {
    try { validateRoleMatrix(loadRoleMatrix()) } catch { failures.push('role matrix: invalid JSON') }
  }
  if (!uatEnvironment.performancePlanJson && !uatEnvironment.performancePlanPath) missing.push('VSHIELD_UAT_PERFORMANCE_PLAN_JSON or PATH')
  if (uatEnvironment.performancePlanPath && !fs.existsSync(uatEnvironment.performancePlanPath)) failures.push('performance plan path: file not found')
  if (!missing.length) {
    try { validatePerformancePlan(loadPerformancePlan()) } catch { failures.push('performance plan: invalid JSON') }
  }
}

fs.mkdirSync('uat-results', { recursive: true })

if (!missing.length && !failures.length) {
  const frontend = await probe('frontend', uatEnvironment.frontendUrl, {}, status => status >= 200 && status < 400)
  if (frontend) {
    const html = await frontend.text()
    const version = html.match(/<meta\s+name=["']v-shield-version["']\s+content=["']([^"']+)/i)?.[1] || ''
    const sha256 = crypto.createHash('sha256').update(html).digest('hex')
    report.frontend = { version, htmlSha256: sha256, artifactDigest: uatEnvironment.rcArtifactDigest }
    if (version !== uatEnvironment.expectedVersion) failures.push('frontend: deployed version does not match VSHIELD_UAT_EXPECTED_VERSION')
    if (uatEnvironment.expectedFrontendSha256 && sha256 !== uatEnvironment.expectedFrontendSha256) failures.push('frontend: HTML SHA-256 does not match approved digest')
  }
  await probe('api_health', healthUrl(), {}, status => status >= 200 && status < 300)
  const signalRProbeUrl = uatEnvironment.signalRUrl.replace(/^wss:/i, 'https:').replace(/^ws:/i, 'http:')
  await probe('signalr', signalRProbeUrl, {}, status => status >= 200 && status < 500 && status !== 404)
  if (!failures.length) for (const role of (smokeOnly ? ['Admin'] : uatRoles)) await probeAccount(role)
}

report.status = missing.length || failures.length ? 'failed' : 'passed'
report.missingRequirements = [...new Set(missing)]
report.failures = [...new Set(failures)]
report.environment = uatEnvironment.environment || undefined
report.tenant = uatEnvironment.tenant || undefined
report.site = uatEnvironment.site || undefined
report.previousArtifactDigest = uatEnvironment.previousArtifactDigest || undefined
fs.writeFileSync('uat-results/preflight.json', `${JSON.stringify(report, null, 2)}\n`)

if (report.status !== 'passed') {
  const details = [...report.missingRequirements, ...report.failures]
  console.error(`UAT preflight failed. Mutation was not started.\n- ${details.join('\n- ')}`)
  console.error('No credential or MFA value was printed. Sanitized result: uat-results/preflight.json')
  process.exit(1)
}
console.log(`UAT preflight passed for ${report.environment}. Frontend version ${report.frontend.version}; HTML SHA-256 ${report.frontend.htmlSha256}.`)
