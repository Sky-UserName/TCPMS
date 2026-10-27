import { demoOrders, demoRooms, demoStores, prototypeImages } from './prototype.js'

const storageKeys = {
	auth: 'tcpms.auth',
	apiToken: 'tcpms.apiToken',
	profile: 'tcpms.profile',
	merchant: 'tcpms.merchant',
	reviews: 'tcpms.reviews',
	guests: 'tcpms.guests',
	orders: 'tcpms.orders',
	booking: 'tcpms.booking',
	favorites: 'tcpms.favorites',
	favoriteMeta: 'tcpms.favoriteMeta',
	recent: 'tcpms.recent'
}

const clone = (value) => JSON.parse(JSON.stringify(value))

const parseStored = (value) => {
	if (typeof value !== 'string') return value
	try {
		return JSON.parse(value)
	} catch (error) {
		// 令牌等纯字符串不是 JSON，按原值返回，不能当成空值。
		return value
	}
}

const read = (key, fallback) => {
	try {
		const value = uni.getStorageSync(key)
		if (value === undefined || value === null || value === '') return clone(fallback)
		return clone(parseStored(value))
	} catch (error) {
		return clone(fallback)
	}
}

const write = (key, value) => {
	try {
		uni.setStorageSync(key, clone(value))
	} catch (error) {
		// Storage is optional in browser preview; page state still remains usable.
	}
	return clone(value)
}

const defaultProfile = {
	loggedIn: false,
	name: '微信用户',
	phone: '',
	uid: '',
	avatar: prototypeImages.avatar
}

// 本地商家资料。接入后端后由接口返回的商家状态覆盖，页面仅在审核通过时开放管理入口。
const defaultMerchant = {
	enabled: false,
	status: 'unapplied',
	storeId: demoStores[0].id,
	storeName: '天空之蓝',
	role: '店主',
	roomCount: 3,
	pendingOrderCount: 0
}

const defaultUserReviews = [
	{
		id: 'user-review-001',
		orderId: 'history-review-001',
		roomId: 1,
		storeId: 1,
		roomName: '阳光实木标准大床房',
		storeName: '云舍民宿·郑州二七路店',
		guest: '鑫',
		nickname: '鑫',
		avatar: prototypeImages.storeOne,
		checkIn: '2026-04-21',
		roomRating: 5,
		storeRating: 5,
		rating: 5,
		content: '房子非常漂亮啊，可以来看看体验下',
		images: [prototypeImages.storeOne, prototypeImages.roomTwo, prototypeImages.storeTwo, prototypeImages.hostel],
		createdAt: '2026-04-22T09:20:00'
	},
	{
		id: 'user-review-002',
		orderId: 'history-review-002',
		roomId: 1,
		storeId: 1,
		roomName: '阳光实木标准大床房',
		storeName: '云舍民宿·郑州二七路店',
		guest: '心',
		nickname: '心',
		avatar: prototypeImages.roomTwo,
		checkIn: '2026-04-24',
		roomRating: 4,
		storeRating: 4,
		rating: 4,
		content: '房子非常不错OK，干净整洁卫生，服务好，设施完善',
		images: [prototypeImages.roomOne, prototypeImages.roomThree, prototypeImages.roomFour],
		createdAt: '2026-04-25T11:10:00'
	}
]

const formatDate = (date) => {
	const year = date.getFullYear()
	const month = String(date.getMonth() + 1).padStart(2, '0')
	const day = String(date.getDate()).padStart(2, '0')
	return {
		iso: `${year}-${month}-${day}T00:00:00`,
		label: `${month}月${day}日`
	}
}

const today = new Date()
today.setHours(0, 0, 0, 0)
const defaultCheckIn = new Date(today)
defaultCheckIn.setDate(defaultCheckIn.getDate() + 1)
const defaultCheckOut = new Date(defaultCheckIn)
defaultCheckOut.setDate(defaultCheckOut.getDate() + 1)
const defaultCheckInDate = formatDate(defaultCheckIn)
const defaultCheckOutDate = formatDate(defaultCheckOut)

const defaultBooking = {
	city: '郑州市',
	region: '',
	searchKeyword: '',
	storeId: demoStores[0].id,
	store: demoStores[0].name,
	address: demoStores[0].address,
	roomId: demoRooms[0].id,
	room: demoRooms[0].name,
	roomImage: demoRooms[0].image,
	roomPrice: Number(demoRooms[0].price),
	checkIn: defaultCheckInDate.label,
	checkOut: defaultCheckOutDate.label,
	isoCheckIn: defaultCheckInDate.iso,
	isoCheckOut: defaultCheckOutDate.iso,
	date: `${defaultCheckInDate.label} - ${defaultCheckOutDate.label}`,
	nights: 1,
	roomCount: 1,
	guestCount: 2,
	bedCount: 0,
	// 入住信息默认留空：订单页不预填任何入住人，必须由用户手动选择/新增。
	guests: []
}

export const getProfile = () => read(storageKeys.profile, defaultProfile)

// 增量更新资料：先保留已缓存的资料，再用新字段覆盖，避免局部更新把登录态清空。
export const saveProfile = (profile) => write(storageKeys.profile, {
	...defaultProfile,
	...getProfile(),
	...profile
})

export const setLoggedIn = (profile = {}) => {
	write(storageKeys.auth, true)
	const current = getProfile()
	return saveProfile({
		...profile,
		loggedIn: true,
		uid: profile.uid || current.uid || '',
		name: profile.name || current.name || '微信用户',
		phone: profile.phone || current.phone || '',
		avatar: profile.avatar || current.avatar || prototypeImages.avatar
	})
}

export const isLoggedIn = () => Boolean(
	read(storageKeys.auth, false) &&
	getProfile().loggedIn &&
	getApiToken()
)

export const getMerchantProfile = () => read(storageKeys.merchant, defaultMerchant)

export const saveMerchantProfile = (merchant = {}) => write(storageKeys.merchant, {
	...defaultMerchant,
	...merchant
})

export const getUserReviews = () => read(storageKeys.reviews, defaultUserReviews)

export const saveUserReviews = (reviews) => write(storageKeys.reviews, reviews)

export const findReviewByOrder = (orderId) => getUserReviews().find((review) => String(review.orderId) === String(orderId)) || null

export const hasOrderReview = (orderId) => Boolean(orderId && findReviewByOrder(orderId))

export const getRoomReviews = (roomId, storeId = '') => {
	const roomKey = String(roomId || '')
	const storeKey = String(storeId || '')
	return getUserReviews().filter((review) => {
		const roomMatches = !roomKey || String(review.roomId || '') === roomKey
		const storeMatches = !storeKey || String(review.storeId || '') === storeKey
		return roomMatches && storeMatches
	})
}

export const getRoomReviewSummary = (roomId, storeId = '') => {
	const reviews = getRoomReviews(roomId, storeId)
	const average = (key) => reviews.length
		? Number((reviews.reduce((total, review) => total + Number(review[key] || review.rating || 0), 0) / reviews.length).toFixed(1))
		: 0
	return {
		roomAverageRating: average('roomRating'),
		storeAverageRating: average('storeRating'),
		reviewCount: reviews.length
	}
}

export const addUserReview = (review = {}) => {
	const existing = getUserReviews()
	const value = {
		id: review.id || `review-${Date.now()}`,
		orderId: review.orderId || '',
		roomId: review.roomId || '',
		storeId: review.storeId || '',
		roomName: review.roomName || '已入住房型',
		storeName: review.storeName || 'TCPMS 门店',
		guest: review.guest || getProfile().name || '微信用户',
		nickname: review.nickname || review.guest || getProfile().name || '微信用户',
		avatar: review.avatar || getProfile().avatar || prototypeImages.avatar,
		checkIn: review.checkIn || '',
		roomRating: Number(review.roomRating || review.rating || 5),
		storeRating: Number(review.storeRating || review.rating || 5),
		rating: Number(review.rating || review.roomRating || 5),
		content: String(review.content || '').trim(),
		images: Array.isArray(review.images) ? review.images.slice(0, 9) : [],
		createdAt: review.createdAt || new Date().toISOString()
	}
	const index = existing.findIndex((item) => String(item.orderId) === String(value.orderId) && value.orderId)
	if (index >= 0) existing.splice(index, 1, { ...existing[index], ...value })
	else existing.unshift(value)
	saveUserReviews(existing)

	return value
}

export const getApiToken = () => read(storageKeys.apiToken, '')

export const saveApiToken = (token) => write(storageKeys.apiToken, token || '')

export const logout = () => {
	write(storageKeys.auth, false)
	write(storageKeys.apiToken, '')
	return saveProfile({ ...defaultProfile, loggedIn: false })
}

// 常用入住人只来自用户自己添加/服务端同步，不再塞演示数据（张三、李四）。
export const getGuests = () => read(storageKeys.guests, [])

export const saveGuests = (guests) => write(storageKeys.guests, guests)

export const getOrders = () => read(storageKeys.orders, demoOrders).map((order) => ({
	...order,
	hasReview: order.hasReview === true || hasOrderReview(order.id)
}))

export const saveOrders = (orders) => write(storageKeys.orders, orders)

export const upsertOrder = (order) => {
	const orders = getOrders()
	const index = orders.findIndex((item) => String(item.id) === String(order.id))
	if (index >= 0) orders.splice(index, 1, { ...orders[index], ...order })
	else orders.unshift(order)
	saveOrders(orders)
	return order
}

export const findOrder = (id) => getOrders().find((order) => String(order.id) === String(id))

export const updateOrder = (id, patch) => {
	const orders = getOrders()
	const index = orders.findIndex((order) => String(order.id) === String(id))
	if (index < 0) return null
	orders[index] = { ...orders[index], ...patch }
	saveOrders(orders)
	return orders[index]
}

export const resolveReviewTarget = (order = {}) => {
	const room = demoRooms.find((item) =>
		String(item.id) === String(order.roomId || order.roomTypeId)
		|| item.name === order.room
		|| (item.name && order.room && (item.name.includes(order.room) || order.room.includes(item.name))))
	const store = demoStores.find((item) =>
		String(item.id) === String(order.storeId)
		|| item.name === order.store
		|| (item.name && order.store && (item.name.includes(order.store) || order.store.includes(item.name))))
	return {
		roomId: order.roomId || order.roomTypeId || room?.id || '',
		storeId: order.storeId || store?.id || room?.storeId || '',
		roomName: order.room || room?.name || '已入住房型',
		storeName: order.store || store?.name || 'TCPMS 门店',
		roomImage: order.image || room?.image || prototypeImages.roomOne
	}
}

export const getBooking = () => {
	const booking = read(storageKeys.booking, defaultBooking)
	if (!booking.isoCheckIn || new Date(booking.isoCheckIn) < today) {
		return saveBooking({
			...booking,
			checkIn: defaultBooking.checkIn,
			checkOut: defaultBooking.checkOut,
			isoCheckIn: defaultBooking.isoCheckIn,
			isoCheckOut: defaultBooking.isoCheckOut,
			date: defaultBooking.date
		})
	}
	return booking
}

export const saveBooking = (booking) => write(storageKeys.booking, {
	...defaultBooking,
	...booking,
	// 空数组也是有效值（用户主动清空入住人），不能再用默认入住人兜底。
	guests: Array.isArray(booking.guests) ? clone(booking.guests) : []
})

export const getFavorites = () => read(storageKeys.favorites, [])

export const toggleFavorite = (id, metadata = {}) => {
	const favorites = getFavorites()
	const key = String(id)
	const exists = favorites.map(String).includes(key)
	const next = exists
		? favorites.filter((item) => String(item) !== key)
		: [...favorites, id]
	const favoriteMeta = read(storageKeys.favoriteMeta, {})
	if (exists) {
		delete favoriteMeta[key]
	} else {
		favoriteMeta[key] = {
			id: key,
			...metadata,
			updatedAt: new Date().toISOString()
		}
	}
	write(storageKeys.favorites, next)
	write(storageKeys.favoriteMeta, favoriteMeta)
	return !next.map(String).includes(key)
}

export const getRecent = () => read(storageKeys.recent, [])

export const recordRecent = (item = {}) => {
	if (!item.id) return getRecent()
	const record = {
		id: String(item.id),
		type: item.type || 'store',
		title: item.title || '',
		subtitle: item.subtitle || '',
		image: item.image || '',
		path: item.path || '',
		updatedAt: new Date().toISOString()
	}
	const next = [
		record,
		...getRecent().filter((entry) => `${entry.type}-${entry.id}` !== `${record.type}-${record.id}`)
	].slice(0, 20)
	return write(storageKeys.recent, next)
}

export const clearRecent = () => write(storageKeys.recent, [])
