import { demoRooms, demoStores, prototypeImages } from './prototype.js'
import { getApiToken, getBooking, saveApiToken, upsertOrder } from './app-store.js'

const defaultApiBaseUrl = 'http://localhost:5180/api/v1'

const getApiBaseUrl = () => {
	try {
		return uni.getStorageSync('tcpms.apiBaseUrl') || defaultApiBaseUrl
	} catch (error) {
		return defaultApiBaseUrl
	}
}

const toQuery = (query = {}) => {
	const entries = Object.keys(query)
		.filter((key) => query[key] !== undefined && query[key] !== null && query[key] !== '')
		.map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(query[key])}`)
	return entries.length ? `?${entries.join('&')}` : ''
}

export const apiRequest = ({ path, method = 'GET', data, query } = {}) => new Promise((resolve, reject) => {
	const token = getApiToken()
	uni.request({
		url: `${getApiBaseUrl()}${path}${toQuery(query)}`,
		method,
		data,
		timeout: 8000,
		header: {
			'Content-Type': 'application/json',
			...(token ? { Authorization: `Bearer ${token}` } : {})
		},
		success: (response) => {
			if (response.statusCode >= 200 && response.statusCode < 300) {
				resolve(response.data)
				return
			}
			reject(response.data || { message: '请求失败' })
		},
		fail: reject
	})
})

const findStoreFallback = (store) => {
	return demoStores.find((item) => item.name === store.name)
		|| demoStores.find((item) => item.name.includes(store.name) || store.name.includes(item.name))
		|| demoStores[0]
}

const findRoomFallback = (room) => {
	return demoRooms.find((item) => item.name === room.name)
		|| demoRooms.find((item) => item.name.includes(room.name) || room.name.includes(item.name))
		|| demoRooms[0]
}

export const mapStore = (store, index = 0) => {
	const fallback = findStoreFallback(store)
	const distance = store.distanceKm === null || store.distanceKm === undefined
		? fallback.distance
		: `${Number(store.distanceKm).toFixed(2)}km`
	const images = [store.images, store.imageUrls, store.gallery, store.photos]
		.find((items) => Array.isArray(items) && items.length)
	return {
		...fallback,
		id: store.id || fallback.id,
		name: store.name || fallback.name,
		shortName: store.name || fallback.shortName,
		status: store.status === 'Active' ? '营业中' : store.status || fallback.status,
		city: store.city || fallback.city || '',
		address: store.address || fallback.address,
		phone: store.phone || fallback.phone,
		latitude: store.latitude || fallback.latitude,
		longitude: store.longitude || fallback.longitude,
		distance,
		unknownDistance: store.distanceKm === null || store.distanceKm === undefined ? fallback.unknownDistance : false,
		image: store.image || fallback.image || prototypeImages.storeOne,
		...(images ? { images: images.slice(0, 3) } : {}),
		sortIndex: index
	}
}

export const mapRoom = (room, index = 0) => {
	const fallback = findRoomFallback(room)
	const isBed = room.kind === 'Bed' || room.kind === 'Dormitory' || room.kind === 'Bedspace' || fallback.type === '多人间床位'
	return {
		...fallback,
		id: room.id || fallback.id,
		storeId: room.storeId || fallback.storeId,
		name: room.name || fallback.name,
		type: isBed ? '多人间床位' : '独立房间',
		gender: room.gender || fallback.gender,
		capacity: room.maxGuests ? `可住${room.maxGuests}人` : fallback.capacity,
		beds: room.bedCount ? `${room.bedCount}张床` : fallback.beds,
		price: String(Math.round(Number(room.basePriceCents || 0) / 100) || Number(fallback.price)),
		originalPrice: fallback.originalPrice,
		description: room.description || fallback.description,
		isPublished: room.isPublished !== false,
		image: fallback.image || prototypeImages.roomOne,
		sortIndex: index
	}
}

const statusMap = {
	PendingPayment: { status: '待支付', statusKey: 'pay', action: '立即支付' },
	PaidPendingConfirmation: { status: '已支付·待确认', statusKey: 'stay', action: '申请退款' },
	ConfirmedPendingCheckIn: { status: '已确认·待入住', statusKey: 'stay', action: '申请退款' },
	CheckedIn: { status: '入住中', statusKey: 'stay', action: '联系管家' },
	Completed: { status: '已完成', statusKey: 'done', action: '再次预订' },
	Refunding: { status: '退款审核中', statusKey: 'refund', action: '查看退款进度' },
	Refunded: { status: '已退款', statusKey: 'refund', action: '查看退款进度' },
	Cancelled: { status: '已取消', statusKey: 'cancel', action: '再次预订' }
}

export const mapOrder = (order, context = {}) => {
	const meta = statusMap[order.status] || {
		status: order.status || '待支付',
		statusKey: 'pay',
		action: '查看订单'
	}
	const checkIn = order.checkIn ? String(order.checkIn).slice(5, 10).replace('-', '月') + '日' : context.checkIn
	const checkOut = order.checkOut ? String(order.checkOut).slice(5, 10).replace('-', '月') + '日' : context.checkOut
	const quantity = Number(order.quantity || context.roomCount || 1)
	const nights = order.checkIn && order.checkOut
		? Math.max(1, Math.round((new Date(order.checkOut) - new Date(order.checkIn)) / 86400000))
		: Number(context.nights || 1)
	const roomFallback = findRoomFallback({ name: order.roomTypeName || context.room })
	const mapped = {
		...context,
		id: order.id || order.orderNumber,
		orderNumber: order.orderNumber,
		storeId: order.storeId || context.storeId,
		roomId: order.roomTypeId || context.roomId,
		store: order.storeName || context.store || 'TCPMS 直营门店',
		room: order.roomTypeName || context.room || '已预订房型',
		image: context.image || roomFallback.image,
		status: meta.status,
		statusKey: meta.statusKey,
		date: `${checkIn || ''} - ${checkOut || ''}`,
		checkIn,
		checkOut,
		nights,
		roomCount: quantity,
		guest: context.guest || '已登记入住人',
		price: String((Number(order.totalAmountCents || 0) / 100).toFixed(2).replace(/\.00$/, '')),
		action: meta.action,
		paymentStatus: order.paymentStatus,
		hasReview: order.hasReview === true || context.hasReview === true,
		createdAt: order.createdAt || new Date().toISOString()
	}
	return mapped
}

const mapReviewItem = (review = {}) => ({
	id: review.id,
	orderId: review.orderId,
	nickname: review.nickname || '微信用户',
	avatar: review.avatarUrl || '',
	createdAt: review.createdAt || '',
	checkIn: review.checkIn || '',
	roomRating: Number(review.roomRating || 0),
	storeRating: Number(review.storeRating || 0),
	rating: Number(review.roomRating || review.rating || 0),
	content: review.content || '',
	images: Array.isArray(review.imageUrls) ? review.imageUrls : []
})

const mapReviewResponse = (data = {}) => ({
	roomId: data.roomTypeId,
	storeId: data.storeId,
	roomName: data.roomTypeName || '',
	storeName: data.storeName || '',
	roomAverageRating: Number(data.roomAverageRating || 0),
	storeAverageRating: Number(data.storeAverageRating || 0),
	reviewCount: Number(data.reviewCount || 0),
	items: Array.isArray(data.items) ? data.items.map(mapReviewItem) : []
})

export const miniappLogin = async () => {
	const data = await apiRequest({
		path: '/auth/miniapp-login',
		method: 'POST',
		data: {
			code: null,
			devOpenId: 'dev-user-001',
			nickname: '张三',
			avatarUrl: prototypeImages.avatar
		}
	})
	if (data && data.token) saveApiToken(data.token)
	return data
}

export const fetchStores = async (keyword = '') => {
	const data = await apiRequest({ path: '/stores/', query: { keyword, status: 'Active' } })
	return Array.isArray(data) ? data.map(mapStore) : []
}

export const fetchNearbyStores = async (latitude, longitude, radiusKm = 50) => {
	const data = await apiRequest({
		path: '/stores/nearby',
		query: { latitude, longitude, radiusKm }
	})
	return Array.isArray(data) ? data.map(mapStore) : []
}

export const fetchStoreDetail = async (id) => {
	const data = await apiRequest({ path: `/stores/${id}` })
	return {
		store: mapStore(data.store || data),
		rooms: Array.isArray(data.roomTypes) ? data.roomTypes.map(mapRoom) : []
	}
}

export const fetchRooms = async (storeId) => {
	const data = await apiRequest({ path: '/room-types/', query: { storeId } })
	return Array.isArray(data) ? data.map(mapRoom) : []
}

export const createRemoteOrder = async (booking) => {
	const defaultBooking = getBooking()
	const data = await apiRequest({
		path: '/orders/',
		method: 'POST',
		data: {
			storeId: booking.storeId,
			roomTypeId: booking.roomId,
			checkIn: booking.isoCheckIn || defaultBooking.isoCheckIn,
			checkOut: booking.isoCheckOut || defaultBooking.isoCheckOut,
			quantity: Number(booking.roomCount || 1),
			guestCount: Number(booking.guestCount || 1),
			guestSnapshotJson: JSON.stringify(booking.guests || [])
		}
	})
	const mapped = mapOrder(data, booking)
	return upsertOrder(mapped)
}

export const fetchRemoteOrders = async () => {
	const data = await apiRequest({ path: '/orders/me' })
	return Array.isArray(data) ? data.map((item) => mapOrder(item)) : []
}

export const fetchRemoteOrder = async (id, context = {}) => {
	const data = await apiRequest({ path: `/orders/${id}` })
	const mapped = mapOrder(data, context)
	return upsertOrder(mapped)
}

export const fetchRoomReviews = async (roomTypeId, limit = 50) => {
	const data = await apiRequest({
		path: `/room-types/${roomTypeId}/reviews`,
		query: { limit }
	})
	return mapReviewResponse(data)
}

export const fetchRemoteOrderReview = async (id) => {
	const data = await apiRequest({ path: `/orders/${id}/review` })
	return mapReviewItem(data)
}

export const createRemoteReview = async (id, review = {}) => {
	const data = await apiRequest({
		path: `/orders/${id}/review`,
		method: 'POST',
		data: {
			roomRating: Number(review.roomRating),
			storeRating: Number(review.storeRating),
			content: review.content || '',
			imageUrls: Array.isArray(review.images) ? review.images : []
		}
	})
	return mapReviewItem(data)
}

export const cancelRemoteOrder = async (id) => {
	const data = await apiRequest({ path: `/orders/${id}/cancel`, method: 'POST' })
	const mapped = mapOrder(data)
	return upsertOrder(mapped)
}

export const payRemoteOrder = async (id) => {
	const data = await apiRequest({ path: `/orders/${id}/mock-pay`, method: 'POST' })
	const mapped = mapOrder(data)
	return upsertOrder(mapped)
}

export const refundRemoteOrder = async (id, amount, reason, context = {}) => {
	const data = await apiRequest({
		path: `/orders/${id}/refunds`,
		method: 'POST',
		data: {
			amountCents: Math.round(Number(amount) * 100),
			reason
		}
	})
	const mapped = mapOrder({
		id,
		status: 'Refunding',
		totalAmountCents: Math.round(Number(context.price || amount) * 100),
		paymentStatus: 'Paid'
	}, context)
	return {
		refund: data,
		order: upsertOrder(mapped)
	}
}
