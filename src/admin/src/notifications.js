import { computed, reactive } from 'vue'
import { get } from './api'

export const notificationState = reactive({
  items: [],
  pendingCount: 0,
  pendingOrderCount: 0,
  pendingRefundCount: 0,
  pendingStoreReviewCount: 0,
  pendingRoomReviewCount: 0,
  pendingReviewCount: 0,
  pendingWithdrawalCount: 0,
  generatedAt: null,
  loading: false,
  loaded: false,
  error: null
})

let activeRequest = null

export async function refreshNotifications({ silent = false } = {}) {
  if (activeRequest) return activeRequest

  notificationState.loading = true
  notificationState.error = null
  activeRequest = get('/admin/notifications')
    .then((payload) => {
      notificationState.items = Array.isArray(payload?.items) ? payload.items : []
      notificationState.pendingCount = Number(payload?.pendingCount ?? notificationState.items.length)
      notificationState.pendingOrderCount = Number(payload?.pendingOrderCount || 0)
      notificationState.pendingRefundCount = Number(payload?.pendingRefundCount || 0)
      notificationState.pendingStoreReviewCount = Number(payload?.pendingStoreReviewCount || 0)
      notificationState.pendingRoomReviewCount = Number(payload?.pendingRoomReviewCount || 0)
      notificationState.pendingReviewCount = Number(payload?.pendingReviewCount || 0)
      notificationState.pendingWithdrawalCount = Number(payload?.pendingWithdrawalCount || 0)
      notificationState.generatedAt = payload?.generatedAt || null
      notificationState.loaded = true
      return payload
    })
    .catch((error) => {
      notificationState.error = error
      if (!silent) throw error
      return null
    })
    .finally(() => {
      notificationState.loading = false
      activeRequest = null
    })

  return activeRequest
}

export function requestNotificationRefresh() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('tcpms:notifications-refresh'))
  }
}

export function useAdminNotifications() {
  return {
    state: notificationState,
    items: computed(() => notificationState.items),
    pendingCount: computed(() => notificationState.pendingCount),
    refresh: refreshNotifications
  }
}
