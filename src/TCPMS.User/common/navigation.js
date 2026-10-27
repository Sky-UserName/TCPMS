import { goPage, showToast } from './prototype.js'
import { fetchStoreDetail, redirectToLogin } from './api.js'
import { getApiToken, getBooking } from './app-store.js'

const isRemoteId = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

// 远程房源下单必须先登录：未登录时直接跳登录页。
// 否则会先 navigateTo 到确认页，再被 401 触发的 reLaunch 打断，小程序报 `navigateTo:fail timeout`。
export const openBookingConfirm = () => {
	const booking = getBooking()
	if (isRemoteId(booking.storeId) && isRemoteId(booking.roomId) && !getApiToken()) {
		redirectToLogin()
		return false
	}
	goPage('/pages/booking/confirm')
	return true
}

// 门店不再有独立详情页，所有门店入口统一落到房型详情（pages/room/detail）。
// 卡片自带推荐房型时直接跳；缺失时按门店接口取第一间房型，仍取不到才退回房型列表。
export const storeRoomPath = (storeId, roomId) => {
	const query = [
		`id=${encodeURIComponent(roomId || '')}`,
		`storeId=${encodeURIComponent(storeId || '')}`
	].join('&')
	return `/pages/room/detail?${query}`
}

export const openStoreRoom = async (store = {}) => {
	const storeId = store.id || store.storeId || ''
	const roomId = store.featuredRoomId || store.roomId || ''
	if (storeId && roomId) {
		goPage(storeRoomPath(storeId, roomId))
		return
	}
	if (!storeId) {
		showToast('门店信息不完整，请稍后重试')
		return
	}
	try {
		const result = await fetchStoreDetail(storeId)
		const fallbackRoomId = (result.store && result.store.featuredRoomId)
			|| (Array.isArray(result.rooms) && result.rooms[0] && result.rooms[0].id)
		if (fallbackRoomId) {
			goPage(storeRoomPath(storeId, fallbackRoomId))
			return
		}
	} catch (error) {
		// 门店接口不可用或门店没有房型时，退回房型列表，避免点击无反应。
	}
	showToast('该门店暂未配置可预订房型')
	goPage(`/pages/room/list?storeId=${encodeURIComponent(storeId)}`)
}

// 兼容历史数据：旧的收藏/浏览记录里仍可能保存 /pages/store/detail 路径。
export const openLegacyStorePath = (path) => {
	const match = /^\/pages\/store\/detail(?:\?|$)/.exec(String(path || ''))
	if (!match) return false
	const idMatch = /[?&]id=([^&]+)/.exec(String(path || ''))
	if (!idMatch) return false
	openStoreRoom({ id: decodeURIComponent(idMatch[1]) })
	return true
}
