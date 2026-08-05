const read = (...names) => {
  for (const name of names) {
    const value = String(process.env[name] || '').trim()
    if (value) return value
  }
  return ''
}

export const uatEnvironment = {
  frontendUrl: read('VSHIELD_UAT_FRONTEND_URL', 'UAT_BASE_URL'),
  apiUrl: read('VSHIELD_UAT_API_URL', 'UAT_API_BASE_URL'),
  signalRUrl: read('VSHIELD_UAT_SIGNALR_URL'),
  apiHealthUrl: read('VSHIELD_UAT_API_HEALTH_URL'),
  environment: read('VSHIELD_UAT_ENVIRONMENT'),
  tenant: read('VSHIELD_UAT_TENANT'),
  site: read('VSHIELD_UAT_SITE'),
  expectedVersion: read('VSHIELD_UAT_EXPECTED_VERSION'),
  expectedFrontendSha256: read('VSHIELD_UAT_FRONTEND_SHA256'),
  rcArtifactDigest: read('VSHIELD_UAT_RC_ARTIFACT_DIGEST'),
  previousArtifactDigest: read('VSHIELD_UAT_PREVIOUS_ARTIFACT_DIGEST'),
  mutationManifestJson: read('VSHIELD_UAT_MUTATION_MANIFEST_JSON', 'UAT_API_CASES_JSON'),
  mutationManifestPath: read('VSHIELD_UAT_MUTATION_MANIFEST_PATH', 'UAT_API_CASES_PATH'),
  roleMatrixJson: read('VSHIELD_UAT_ROLE_MATRIX_JSON'),
  roleMatrixPath: read('VSHIELD_UAT_ROLE_MATRIX_PATH'),
  cleanupPolicy: read('VSHIELD_UAT_CLEANUP_POLICY'),
  ciArtifactPath: read('VSHIELD_UAT_CI_ARTIFACT_PATH'),
  performancePlanJson: read('VSHIELD_UAT_PERFORMANCE_PLAN_JSON'),
  performancePlanPath: read('VSHIELD_UAT_PERFORMANCE_PLAN_PATH'),
  networkProfile: read('VSHIELD_UAT_NETWORK_PROFILE'),
  allowMutations: read('VSHIELD_UAT_ALLOW_MUTATIONS', 'UAT_ALLOW_MUTATIONS') === 'true',
  allowNegativeMfa: read('VSHIELD_UAT_ALLOW_NEGATIVE_MFA') === 'true',
  mfaMode: read('VSHIELD_UAT_MFA_MODE') || 'totp',
  mfaTestApiUrl: read('VSHIELD_UAT_MFA_TEST_API_URL'),
  mfaTestApiKey: read('VSHIELD_UAT_MFA_TEST_API_KEY'),
  ignoreHttpsErrors: read('VSHIELD_UAT_IGNORE_HTTPS_ERRORS', 'UAT_IGNORE_HTTPS_ERRORS') === 'true',
}

const roleVariables = {
  Admin: ['ADMIN', 'ADMIN'],
  BaoVe: ['GUARD', 'BAOVE'],
  LeTan: ['RECEPTIONIST', 'LETAN'],
  QuanLy: ['MANAGER', 'QUANLY'],
  NhanSu: ['HR', 'NHANSU'],
}

export function roleCredentials(role) {
  const [current, legacy] = roleVariables[role] || []
  if (!current) throw new Error(`Unknown UAT role: ${role}`)
  return {
    username: read(`VSHIELD_UAT_${current}_USERNAME`, `UAT_${legacy}_USERNAME`),
    password: read(`VSHIELD_UAT_${current}_PASSWORD`, `UAT_${legacy}_PASSWORD`),
    totpSecret: read(`VSHIELD_UAT_${current}_TOTP_SECRET`, `UAT_${legacy}_TOTP_SECRET`),
    manualOtp: read(`VSHIELD_UAT_${current}_OTP`),
    storageStatePath: read(`VSHIELD_UAT_${current}_STORAGE_STATE_PATH`),
    variablePrefix: `VSHIELD_UAT_${current}`,
  }
}

export const uatRoles = Object.keys(roleVariables)

export function apiUrl(path = '') {
  return `${uatEnvironment.apiUrl.replace(/\/$/, '')}${path}`
}
