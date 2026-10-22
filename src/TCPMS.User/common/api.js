import { demoRooms, demoStores, prototypeImages } from './prototype.js'
import { getApiToken, getBooking, logout, saveApiToken, saveOrders, upsertOrder } from './app-store.js'

// 真机预览/真机调试时手机访问不到 localhost，必须用开发机的局域网地址。
// 换了 Wi-Fi 或路由器重新分配 IP 后，只改下面这一行。
const devMachineApiBaseUrl = 'http://192.168.1.16:5180/api/v1'
const devtoolsApiBaseUrl = 'http://localhost:5180/api/v1'

// 延迟判断：模块加载早期 uni 可能还没就绪，取不到 platform 时按开发者工具处理。
const resolveDefaultApiBaseUrl = () => {
	if (import.meta.env.VITE_API_BASE_URL) return import.meta.env.VITE_API_BASE_URL
	try {
		return uni.getSystemInfoSync().platform === 'devtools'
			? devtoolsApiBaseUrl
			: devMachineApiBaseUrl
	} catch (error) {
		return devtoolsApiBaseUrl
	}
}

const getApiBaseUrl = () => {
	try {
		return uni.getStorageSync('tcpms.apiBaseUrl') || resolveDefaultApiBaseUrl()
	} catch (error) {
		return resolveDefaultApiBaseUrl()
	}
}

const toQuery = (query = {}) => {
	const entries = Object.keys(query)
		.filter((key) => query[key] !== undefined && query[key] !== null && query[key] !== '')
		.map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(query[key])}`)
	return entries.length ? `?${entries.join('&')}` : ''
}

const handleUnauthorized = () => {
	logout()
	try {
		const pages = getCurrentPages()
		const current = pages?.[pages.length - 1]
		if (current?.route !== 'pages/auth/login') {
			uni.reLaunch({ url: '/pages/auth/login' })
		}
	} catch (error) {
		// Navigation is unavailable in some non-page runtimes.
	}
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
			const payload = response.data && typeof response.data === 'object'
				? response.data
				: { message: '请求失败' }
			if (response.statusCode === 401) {
				handleUnauthorized()
				reject({
					...payload,
					code: 'unauthorized',
					message: payload.message || '登录状态已失效，请重新授权登录'
				})
				return
			}
			reject(payload)
		},
		fail: (error) => reject({
			code: 'network_error',
			message: error?.errMsg || '无法连接本地后端，请确认 API 服务已启动',
			errMsg: error?.errMsg || ''
		})
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

const isRemoteRecord = (value) => /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(String(value || ''))

const parseImageList = (value) => {
	if (Array.isArray(value)) return value.filter((item) => typeof item === 'string' && item)
	if (typeof value !== 'string' || !value.trim()) return []
	try {
		const parsed = JSON.parse(value)
		return Array.isArray(parsed) ? parsed.filter((item) => typeof item === 'string' && item) : []
	} catch (error) {
		return []
	}
}

export const mapStore = (store, index = 0) => {
	const isRemote = isRemoteRecord(store.id)
	const fallback = isRemote ? {} : findStoreFallback(store)
	const distance = store.distanceKm === null || store.distanceKm === undefined
		? (isRemote ? '距离未知' : fallback.distance)
		: `${Number(store.distanceKm).toFixed(2)}km`
	const images = [
		store.coverImageUrl,
		store.doorImageUrl,
		...parseImageList(store.images),
		...parseImageList(store.imageUrls),
		...parseImageList(store.gallery),
		...parseImageList(store.photos),
		...parseImageList(store.roomImageUrlsJson)
	].filter((item) => typeof item === 'string' && item)
	const image = store.coverImageUrl || store.doorImageUrl || store.image || images[0] || fallback.image || (isRemote ? '' : prototypeImages.storeOne)
	const startingPrice = store.startingPriceCents === null || store.startingPriceCents === undefined
		? (isRemote ? null : fallback.price)
		: (Number(store.startingPriceCents) / 100).toFixed(2).replace(/\.00$/, '')
	return {
		...fallback,
		isRemote,
		id: store.id || fallback.id,
		name: store.name || fallback.name,
		shortName: store.name || fallback.shortName,
		status: store.status === 'Active' ? '营业中' : store.status || fallback.status,
		city: store.city || fallback.city || '',
		address: store.address || fallback.address || '',
		phone: store.phone || fallback.phone || '',
		latitude: store.latitude ?? fallback.latitude ?? null,
		longitude: store.longitude ?? fallback.longitude ?? null,
		distance,
		unknownDistance: store.distanceKm === null || store.distanceKm === undefined ? (isRemote ? true : fallback.unknownDistance) : false,
		image,
		images: images.length ? images.slice(0, 3) : (fallback.images || []),
		featuredRoomId: store.featuredRoomId || (isRemote ? null : fallback.featuredRoomId),
		featuredRoomName: store.featuredRoomName || '',
		room: store.featuredRoomName || (isRemote ? '' : fallback.room),
		description: store.description || '',
		price: startingPrice,
		rating: store.rating ?? (isRemote ? null : fallback.rating),
		reviews: store.reviewCount ?? (isRemote ? null : fallback.reviews),
		tags: Array.isArray(store.tags) ? store.tags : (isRemote ? [] : fallback.tags || []),
		sortIndex: index
	}
}

export const mapRoom = (room, index = 0) => {
	const isRemote = isRemoteRecord(room.id)
	const fallback = isRemote ? {} : findRoomFallback(room)
	const isBed = room.kind === 'Bed' || room.kind === 'Dormitory' || room.kind === 'Bedspace' || fallback.type === '多人间床位'
	const images = parseImageList(room.imageUrlsJson)
	const tags = parseImageList(room.tagsJson)
	const facilities = parseImageList(room.facilitiesJson)
	const services = parseImageList(room.servicesJson)
	const priceCents = room.basePriceCents === null || room.basePriceCents === undefined
		? null
		: Number(room.basePriceCents)
	return {
		...fallback,
		isRemote,
		id: room.id || fallback.id,
		storeId: room.storeId || fallback.storeId,
		name: room.name || fallback.name,
		type: isBed ? '多人间床位' : '独立房间',
		gender: room.gender || fallback.gender || '',
		capacity: room.maxGuests ? `可住${room.maxGuests}人` : (isRemote ? '' : fallback.capacity),
		beds: room.bedCount ? `${room.bedCount}张床` : (isRemote ? '' : fallback.beds),
		price: priceCents === null ? (isRemote ? null : String(fallback.price)) : (priceCents / 100).toFixed(2).replace(/\.00$/, ''),
		originalPrice: isRemote ? null : fallback.originalPrice,
		description: room.description || fallback.description || '',
		area: room.area ? `${room.area}㎡` : (isRemote ? '' : fallback.area),
		roomCount: room.roomCount || 0,
		tags: tags.length ? tags : (isRemote ? facilities.concat(services) : fallback.tags || []),
		facilities,
		services,
		isPublished: room.isPublished !== false,
		availability: isRemote ? '选择日期查询房态' : fallback.availability,
		image: images[0] || room.imageUrl || fallback.image || (isRemote ? '' : prototypeImages.roomOne),
		images,
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

export const miniappLogin = async ({ nickname = '', avatarUrl = '', phoneCode = null } = {}) => {
	const loginResult = await new Promise((resolve, reject) => {
		uni.login({
			provider: 'weixin',
			success: resolve,
			fail: (error) => reject({
				code: 'wechat_login_api_error',
				message: error?.errMsg || '微信登录接口调用失败',
				errMsg: error?.errMsg || ''
			})
		})
	})
	const code = loginResult?.code || null
	if (!code) throw new Error('微信未返回登录凭证')

	const data = await apiRequest({
		path: '/auth/miniapp-login',
		method: 'POST',
		data: {
			code,
			devOpenId: null,
			nickname: nickname || null,
			avatarUrl: avatarUrl || null,
			phoneCode: phoneCode || null
		}
	})
	if (data && data.token) saveApiToken(data.token)
	return data
}

export const bindMiniappPhone = async (phoneCode) => {
	const data = await apiRequest({
		path: '/auth/phone',
		method: 'POST',
		data: { phoneCode }
	})
	return data
}

export const fetchMiniappProfile = async () => {
	return apiRequest({ path: '/auth/me' })
}

export const uploadMiniappAvatar = (filePath) => new Promise((resolve, reject) => {
	const token = getApiToken()
	if (!token) {
		reject(new Error('请先登录'))
		return
	}
	uni.uploadFile({
		url: `${getApiBaseUrl()}/auth/avatar`,
		filePath,
		name: 'file',
		header: { Authorization: `Bearer ${token}` },
		success: (response) => {
			let data = response.data
			try {
				data = typeof data === 'string' ? JSON.parse(data) : data
			} catch (error) {
				// Keep the raw response for a useful error below.
			}
			if (response.statusCode >= 200 && response.statusCode < 300) {
				resolve(data)
				return
			}
			if (response.statusCode === 401) {
				handleUnauthorized()
				reject({ code: 'unauthorized', message: '登录状态已失效，请重新授权登录' })
				return
			}
			reject(data || { message: '头像上传失败' })
		},
		fail: reject
	})
})

export const fetchStores = async (keyword = '') => {
	const data = await apiRequest({ path: '/stores/', query: { keyword, status: 'Active' } })
	return Array.isArray(data) ? data.map(mapStore) : []
}

export const fetchMerchantPermissionMenus = async () => {
	const data = await apiRequest({ path: '/merchant/permission-menus' })
	return Array.isArray(data) ? data : []
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

export const previewRemoteOrder = async (booking) => {
	const defaultBooking = getBooking()
	return apiRequest({
		path: '/orders/preview',
		method: 'POST',
		data: {
			storeId: booking.storeId,
			roomTypeId: booking.roomId,
			checkIn: booking.isoCheckIn || defaultBooking.isoCheckIn,
			checkOut: booking.isoCheckOut || defaultBooking.isoCheckOut,
			quantity: Number(booking.roomCount || 1),
			guestCount: Number(booking.guestCount || 1)
		}
	})
}

export const fetchRemoteOrders = async () => {
	const data = await apiRequest({ path: '/orders/me' })
	const orders = Array.isArray(data) ? data.map((item) => mapOrder(item)) : []
	saveOrders(orders)
	return orders
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

export const requestRemotePayment = async (id) => {
	return apiRequest({ path: `/orders/${id}/pay`, method: 'POST' })
}

// 支付成功后向服务端确认：服务端会主动向微信查单，回调不可达时也能把订单置为已支付。
export const confirmRemotePayment = async (id, context = {}) => {
	const data = await apiRequest({ path: `/orders/${id}/pay-status` })
	const mapped = mapOrder(data, context)
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
