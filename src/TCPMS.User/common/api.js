import { getApiToken, getBooking, logout, saveApiToken, saveOrders, upsertOrder } from './app-store.js'
import { ALLOW_DEV_LOGIN, API_BASE_URL_OVERRIDE, IS_DEV_BUILD } from './env.js'
import { isLoginRedirecting, setLoginRedirecting } from './auth-gate.js'

// 后端地址：默认连本机后端，发行包用 .env.production 或 VITE_API_BASE_URL 指定线上地址。
// 真机调试访问不到 localhost，同样用 VITE_API_BASE_URL 指到开发机的局域网地址。
const localApiBaseUrl = 'http://localhost:5180/api/v1'
const remoteApiBaseUrl = 'http://39.96.200.62/api/v1'

const resolveDefaultApiBaseUrl = () => {
	if (API_BASE_URL_OVERRIDE) return API_BASE_URL_OVERRIDE
	return IS_DEV_BUILD ? localApiBaseUrl : remoteApiBaseUrl
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

// 401 只允许触发一次跳转：多个并发请求同时失败时，重复 reLaunch 会让页面白屏、跳转被覆盖。
const navigateToLogin = (attempt = 0) => {
	try {
		const pages = typeof getCurrentPages === 'function' ? getCurrentPages() : []
		const current = pages && pages[pages.length - 1]
		if (current && current.route === 'pages/auth/login') {
			setLoginRedirecting(false)
			return
		}
		uni.reLaunch({
			url: '/pages/auth/login',
			success: () => {
				// 登录页接管后稍等再解锁，避免旧页面的延迟响应再次触发跳转。
				setTimeout(() => setLoginRedirecting(false), 600)
			},
			fail: () => {
				// 页面切换动画期间小程序可能拒绝跳转，短暂重试；仍失败则保持当前页可操作。
				if (attempt < 4) setTimeout(() => navigateToLogin(attempt + 1), 180)
				else setLoginRedirecting(false)
			}
		})
	} catch (error) {
		setLoginRedirecting(false)
	}
}

// delay 给当前页面留出一帧渲染「正在前往登录」的机会，避免跳转瞬间整页空白。
export const redirectToLogin = (delay = 0) => {
	if (isLoginRedirecting()) return
	setLoginRedirecting(true)
	if (delay > 0) setTimeout(() => navigateToLogin(0), delay)
	else navigateToLogin(0)
}

const handleUnauthorized = () => {
	if (isLoginRedirecting()) return
	logout()
	redirectToLogin(80)
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

// 旧版发布页曾把微信本机临时文件写进图片字段；这类地址在开发者工具会命中 __tmp__
// 临时服务并触发 CORS，重启后也会失效，读取时统一过滤掉。
const isTemporaryImagePath = (value) => {
	const url = String(value || '').trim().toLowerCase()
	return url.startsWith('http://tmp/') ||
		url.startsWith('wxfile://') ||
		url.startsWith('blob:') ||
		url.includes('/__tmp__/')
}

const isUsableImage = (value) => {
	if (typeof value !== 'string' || !value.trim()) return false
	return !isTemporaryImagePath(value)
}

const parseImageList = (value) => {
	if (Array.isArray(value)) return value.filter(isUsableImage)
	if (typeof value !== 'string' || !value.trim()) return []
	try {
		const parsed = JSON.parse(value)
		return Array.isArray(parsed) ? parsed.filter(isUsableImage) : []
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
	].filter(isUsableImage)
	const image = [store.coverImageUrl, store.doorImageUrl, store.image, images[0]].find(isUsableImage) || fallback.image || (isRemote ? '' : prototypeImages.storeOne)
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
		// 门店列表可选返回在售房型（首页热门精选直接用房型卡片渲染）。
		rooms: Array.isArray(store.rooms) ? store.rooms.map((room, roomIndex) => mapRoom(room, roomIndex)) : [],
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
		maxGuests: Number(room.maxGuests || 0) || null,
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
		image: images[0] || (isUsableImage(room.imageUrl) ? room.imageUrl : '') || fallback.image || (isRemote ? '' : prototypeImages.roomOne),
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
	RefundFailed: { status: '退款失败', statusKey: 'refund', action: '联系客服' },
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

const DEV_OPEN_ID_KEY = 'tcpms.devOpenId'
const DEV_WECHAT_LOGIN_KEY = 'tcpms.devUseWechatLogin'
const DEFAULT_DEV_OPEN_ID = 'dev-user-001'

const readDevStorage = (key, fallback = '') => {
	try {
		return uni.getStorageSync(key) || fallback
	} catch (error) {
		return fallback
	}
}

// 开发环境默认用固定测试账号登录：种子订单、退款单都能稳定复现。
// 需要验证真实微信登录时，在控制台执行 uni.setStorageSync('tcpms.devUseWechatLogin', true)。
const useWechatLoginInDev = () => {
	try {
		return uni.getStorageSync(DEV_WECHAT_LOGIN_KEY) === true
	} catch (error) {
		return false
	}
}

export const miniappLogin = async ({ nickname = '', avatarUrl = '', phoneCode = null } = {}) => {
	const preferDevAccount = ALLOW_DEV_LOGIN && !useWechatLoginInDev()
	const payload = {
		code: null,
		devOpenId: preferDevAccount ? readDevStorage(DEV_OPEN_ID_KEY, DEFAULT_DEV_OPEN_ID) : null,
		nickname: nickname || null,
		avatarUrl: avatarUrl || null,
		phoneCode: null
	}

	if (!preferDevAccount) {
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
		payload.code = loginResult?.code || null
		payload.phoneCode = phoneCode
		if (!payload.code) throw new Error('微信未返回登录凭证')
	}

	let data
	try {
		data = await apiRequest({ path: '/auth/miniapp-login', method: 'POST', data: payload })
	} catch (error) {
		// 开发环境里 code 换不到 openId（AppID 未配、code 失效）时退回固定测试账号，避免联调被配置卡住。
		if (!ALLOW_DEV_LOGIN || preferDevAccount) throw error
		data = await apiRequest({
			path: '/auth/miniapp-login',
			method: 'POST',
			data: {
				...payload,
				code: null,
				phoneCode: null,
				devOpenId: readDevStorage(DEV_OPEN_ID_KEY, DEFAULT_DEV_OPEN_ID)
			}
		})
	}
	if (data && data.token) saveApiToken(data.token)
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

// 高德小程序端 Key 由后端 /map/config 下发，前端不硬编码；接口不可用时返回未启用状态。
export const fetchMapConfig = async () => {
	const data = await apiRequest({ path: '/map/config' })
	return {
		amapEnabled: Boolean(data && data.amapEnabled),
		miniProgramKey: (data && data.miniProgramKey) || '',
		defaultRadiusKm: data && data.defaultRadiusKm ? Number(data.defaultRadiusKm) : 50
	}
}

export const fetchStoreDetail = async (id) => {
	const data = await apiRequest({ path: `/stores/${id}` })
	return {
		store: mapStore(data.store || data),
		rooms: Array.isArray(data.roomTypes) ? data.roomTypes.map(mapRoom) : []
	}
}

// 日历选日期时按天查询真实房态与价格，避免展示已满房的日期。
export const fetchRoomAvailability = async (roomTypeId, checkIn, checkOut) => {
	const data = await apiRequest({
		path: `/room-types/${roomTypeId}/availability`,
		query: { checkIn, checkOut }
	})
	return Array.isArray(data) ? data : []
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

// 模拟支付只在后端 Development 环境存在，前端不再保留任何降级入口，
// 避免支付未配置时把订单直接置为已支付。
export const requestRemotePayment = async (id) => {
	return apiRequest({ path: `/orders/${id}/pay`, method: 'POST' })
}

// 仅开发联调使用：后端 /mock-pay 只在 Development 环境开放，生产构建里 ALLOW_MOCK_PAY 恒为 false。
export const mockPayRemoteOrder = async (id) => {
	const data = await apiRequest({ path: `/orders/${id}/mock-pay`, method: 'POST' })
	return upsertOrder(mapOrder(data))
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

// ---------------- 商家中心 ----------------

export const fetchMerchantProfile = () => apiRequest({ path: '/merchant/profile' })

export const fetchMerchantApplication = () => apiRequest({ path: '/merchant/application' })

export const submitMerchantApplication = (payload = {}) => apiRequest({
	path: '/merchant/application',
	method: 'POST',
	data: payload
})

const uploadMerchantImage = (path, filePath) => new Promise((resolve, reject) => {
	const token = getApiToken()
	if (!token) {
		reject({ code: 'unauthorized', message: '请先登录' })
		return
	}
	uni.uploadFile({
		url: `${getApiBaseUrl()}${path}`,
		filePath,
		name: 'file',
		header: { Authorization: `Bearer ${token}` },
		success: (response) => {
			let data = response.data
			try {
				data = typeof data === 'string' ? JSON.parse(data) : data
			} catch (error) {
				// 保留原始响应，下面统一给出上传失败提示。
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
			reject(data || { code: 'upload_failed', message: '图片上传失败' })
		},
		fail: (error) => reject({
			code: 'network_error',
			message: error?.errMsg || '图片上传失败',
			errMsg: error?.errMsg || ''
		})
	})
})

export const uploadMerchantApplicationImage = (filePath) => uploadMerchantImage('/merchant/application/upload', filePath)

export const uploadMerchantRoomImage = (filePath) => uploadMerchantImage('/merchant/rooms/upload', filePath)

export const fetchMerchantRooms = async () => {
	const data = await apiRequest({ path: '/merchant/rooms' })
	return Array.isArray(data) ? data : []
}

export const fetchMerchantRoom = (id) => apiRequest({ path: `/merchant/rooms/${id}` })

export const saveMerchantRoom = (id, payload) => apiRequest({
	path: id ? `/merchant/rooms/${id}` : '/merchant/rooms',
	method: id ? 'PUT' : 'POST',
	data: payload
})

export const publishMerchantRoom = (id, published) => apiRequest({
	path: `/merchant/rooms/${id}/publish`,
	method: 'POST',
	query: { published }
})

export const fetchMerchantOrders = async (status = '') => {
	const data = await apiRequest({ path: '/merchant/orders', query: { status } })
	return Array.isArray(data) ? data : []
}

export const confirmMerchantOrder = (orderId) => apiRequest({
	path: `/merchant/orders/${orderId}/confirm`,
	method: 'POST'
})

export const checkInMerchantOrder = (orderId) => apiRequest({
	path: `/merchant/orders/${orderId}/check-in`,
	method: 'POST'
})

export const checkOutMerchantOrder = (orderId) => apiRequest({
	path: `/merchant/orders/${orderId}/check-out`,
	method: 'POST'
})

export const verifyMerchantOrder = (code) => apiRequest({
	path: '/merchant/verify',
	method: 'POST',
	data: { code }
})

export const fetchMerchantVerifyRecords = async () => {
	const data = await apiRequest({ path: '/merchant/verify-records' })
	return Array.isArray(data) ? data : []
}

export const fetchMerchantFinance = () => apiRequest({ path: '/merchant/finance' })

export const createMerchantWithdrawal = (amountCents, account = '') => apiRequest({
	path: '/merchant/withdrawals',
	method: 'POST',
	data: { amountCents, account }
})

export const fetchMerchantReviews = async () => {
	const data = await apiRequest({ path: '/merchant/reviews' })
	return Array.isArray(data) ? data : []
}

export const replyMerchantReview = (id, content) => apiRequest({
	path: `/merchant/reviews/${id}/reply`,
	method: 'POST',
	data: { content }
})

export const fetchMerchantCoupons = async () => {
	const data = await apiRequest({ path: '/merchant/coupons' })
	return Array.isArray(data) ? data : []
}

export const createMerchantCoupon = (payload = {}) => apiRequest({
	path: '/merchant/coupons',
	method: 'POST',
	data: payload
})

export const updateMerchantCoupon = (id, payload = {}) => apiRequest({
	path: `/merchant/coupons/${id}`,
	method: 'PUT',
	data: payload
})

export const fetchMerchantStaff = async () => {
	const data = await apiRequest({ path: '/merchant/staff' })
	return Array.isArray(data) ? data : []
}

export const createMerchantStaff = (payload = {}) => apiRequest({
	path: '/merchant/staff',
	method: 'POST',
	data: payload
})

export const fetchMerchantSettings = () => apiRequest({ path: '/merchant/settings' })

export const saveMerchantSettingsRemote = (settings) => apiRequest({
	path: '/merchant/settings',
	method: 'PUT',
	data: settings
})

export const fetchMerchantMessageSettings = async () => {
	const data = await apiRequest({ path: '/merchant/message-settings' })
	return Array.isArray(data) ? data : []
}

export const saveMerchantMessageSettingsRemote = (settings) => apiRequest({
	path: '/merchant/message-settings',
	method: 'PUT',
	data: settings
})

// ---------------- 用户内容 ----------------

// 浏览类接口允许匿名访问页面；未登录时直接返回空数据，避免把访客踢回登录页。
const hasApiToken = () => Boolean(getApiToken())
const requireApiToken = () => {
	if (!hasApiToken()) {
		const error = new Error('请先登录')
		error.code = 'unauthorized'
		throw error
	}
}

export const fetchMySummary = () => hasApiToken()
	? apiRequest({ path: '/me/summary' })
	: Promise.resolve({ favoriteCount: 0, guestCount: 0, recentCount: 0, unreadMessageCount: 0, orderCount: 0, stayNights: 0, points: 0 })

export const fetchMyFavorites = async () => {
	if (!hasApiToken()) return []
	const data = await apiRequest({ path: '/me/favorites' })
	return Array.isArray(data) ? data : []
}

export const addMyFavorite = (record) => {
	requireApiToken()
	return apiRequest({
		path: '/me/favorites',
		method: 'POST',
		data: record
	})
}

export const removeMyFavorite = (id) => {
	requireApiToken()
	return apiRequest({
		path: `/me/favorites/${encodeURIComponent(id)}`,
		method: 'DELETE'
	})
}

export const fetchMyGuests = async () => {
	if (!hasApiToken()) return []
	const data = await apiRequest({ path: '/me/guests' })
	return Array.isArray(data) ? data : []
}

export const saveMyGuests = (guests) => {
	requireApiToken()
	return apiRequest({
		path: '/me/guests',
		method: 'PUT',
		data: guests
	})
}

export const fetchMyRecent = async () => {
	if (!hasApiToken()) return []
	const data = await apiRequest({ path: '/me/recent' })
	return Array.isArray(data) ? data : []
}

export const recordMyRecent = (item) => {
	if (!hasApiToken()) return Promise.resolve(null)
	return apiRequest({
		path: '/me/recent',
		method: 'POST',
		data: item
	})
}

export const clearMyRecent = () => {
	if (!hasApiToken()) return Promise.resolve(null)
	return apiRequest({ path: '/me/recent', method: 'DELETE' })
}

export const fetchMyMessages = async () => {
	if (!hasApiToken()) return []
	const data = await apiRequest({ path: '/me/messages' })
	return Array.isArray(data) ? data : []
}

export const markMyMessageRead = (id) => {
	if (!hasApiToken()) return Promise.resolve(null)
	return apiRequest({
		path: `/me/messages/${encodeURIComponent(id)}/read`,
		method: 'POST'
	})
}

export const fetchArticle = (id) => apiRequest({ path: `/articles/${encodeURIComponent(id)}` })

export const fetchStoreFilters = () => apiRequest({ path: '/stores/filters' })

export const fetchCarousel = async () => {
	const data = await apiRequest({ path: '/carousel' })
	return Array.isArray(data) ? data : []
}

// 服务协议、隐私政策、联系客服等说明页面的文案由后端下发，页面只负责渲染。
export const fetchPageContent = (key) => apiRequest({ path: `/pages/${encodeURIComponent(key)}` })

