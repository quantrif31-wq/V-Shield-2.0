import { computed } from 'vue'
import { authState } from '../../stores/auth'

const userRole = computed(() => authState.user?.role)

export const capabilityFlags = {
  canOperateLane: computed(() => userRole.value === 'Admin' || userRole.value === 'BaoVe'),
  canManagePolicy: computed(() => userRole.value === 'Admin'),
  canManageEvidenceGovernance: computed(() => userRole.value === 'Admin'),
  canManageFoundation: computed(() => userRole.value === 'Admin' || userRole.value === 'QuanLy'),
  canManageIdentity: computed(() => userRole.value === 'Admin'),
  canViewSoc: computed(() => userRole.value === 'Admin' || userRole.value === 'BaoVe'),
  canViewDevices: computed(() => userRole.value === 'Admin' || userRole.value === 'BaoVe'),
  canManageVisitor: computed(() => userRole.value === 'Admin'),
  canViewVisitor: computed(() => userRole.value === 'Admin' || userRole.value === 'BaoVe'),
  canViewReports: computed(() => userRole.value === 'Admin' || userRole.value === 'QuanLy'),
  canViewOperations: computed(() => userRole.value === 'Admin' || userRole.value === 'BaoVe'),
  canActEmergency: computed(() => userRole.value === 'Admin'),
  canReleaseApproval: computed(() => userRole.value === 'Admin'),
  canExportEvidence: computed(() => userRole.value === 'Admin'),
}

export function useCapabilityFlags() {
  return capabilityFlags
}
