import { demoGuests, demoOrders, demoRooms, demoStores, prototypeImages } from './prototype.js'

const storageKeys = {
	auth: 'tcpms.auth',
	apiToken: 'tcpms.apiToken',
	profile: 'tcpms.profile',
	merchant: 'tcpms.merchant',
	merchantRooms: 'tcpms.merchantRooms',
	merchantOrders: 'tcpms.merchantOrders',
	merchantVerifyRecords: 'tcpms.merchantVerifyRecords',
	merchantReviews: 'tcpms.merchantReviews',
	merchantCoupons: 'tcpms.merchantCoupons',
	merchantStaff: 'tcpms.merchantStaff',
	merchantSettings: 'tcpms.merchantSettings',
	merchantMessages: 'tcpms.merchantMessages',
	reviews: 'tcpms.reviews',
	guests: 'tcpms.guests',
	orders: 'tcpms.orders',
	booking: 'tcpms.booking',
	favorites: 'tcpms.favorites',
	favoriteMeta: 'tcpms.favoriteMeta',
	recent: 'tcpms.recent'
}

const clone = (value) => JSON.parse(JSON.stringify(value))

const read = (key, fallback) => {
	try {
		const value = uni.getStorageSync(key)
		if (value === undefined || value === null || value === '') return clone(fallback)
		if (typeof value === 'string') return JSON.parse(value)
		return clone(value)
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

const defaultMerchantRooms = [
	{
		id: 'merchant-room-001',
		name: '阳光实木标准大床房',
		type: '独立房间',
		status: '已上架',
		key: 'online',
		price: '300',
		stock: '2',
		capacity: '可住2人 · 1张1.8m大床',
		area: '28㎡',
		image: prototypeImages.roomOne,
		updatedAt: '2026-10-06 10:20'
	},
	{
		id: 'merchant-room-002',
		name: '城市花园观景双床房',
		type: '独立房间',
		status: '草稿',
		key: 'draft',
		price: '368',
		stock: '1',
		capacity: '可住4人 · 2张1.5m双人床',
		area: '35㎡',
		image: prototypeImages.roomTwo,
		updatedAt: '2026-10-05 16:40'
	}
]

const defaultMerchantOrders = [
	{
		id: 'M202610060001',
		roomName: '阳光实木标准大床房',
		guest: '张三 等2人',
		date: '10月07日 - 10月08日',
		amount: '300.00',
		status: '待入住',
		statusKey: 'stay',
		image: prototypeImages.roomOne,
		createdAt: '2026-10-06 09:18'
	},
	{
		id: 'M202610050002',
		roomName: '城市花园观景双床房',
		guest: '李四',
		date: '10月06日 - 10月08日',
		amount: '736.00',
		status: '待处理',
		statusKey: 'pending',
		image: prototypeImages.roomTwo,
		createdAt: '2026-10-05 20:32'
	}
]

const defaultMerchantReviews = [
	{
		id: 'review-001',
		guest: '鑫',
		rating: 5,
		date: '2026-09-28',
		content: '房子很干净，入住流程顺畅。',
		reply: ''
	},
	{
		id: 'review-002',
		guest: '心',
		rating: 4,
		date: '2026-09-21',
		content: '周边交通方便，房间采光很好。',
		reply: '感谢您的入住与反馈。'
	}
]

const defaultMerchantCoupons = [
	{ id: 'coupon-001', name: '新客立减券', amount: '20', threshold: '满200可用', stock: 50, used: 8, status: '生效中' },
	{ id: 'coupon-002', name: '周末房费券', amount: '50', threshold: '满400可用', stock: 20, used: 20, status: '已结束' }
]

const defaultMerchantStaff = [
	{ id: 'staff-001', name: '张三', phone: '138****8888', role: '店主', status: '正常' },
	{ id: 'staff-002', name: '李四', phone: '139****9999', role: '前台', status: '正常' }
]

const defaultMerchantSettings = {
	storeName: '天空之蓝',
	phone: '0371-88886622',
	checkIn: '14:00',
	checkOut: '12:00',
	cancelRule: '入住前 24 小时可免费取消',
	autoAccept: true,
	showPhone: true
}

const defaultMerchantMessages = [
	{ key: 'order', title: '订单状态提醒', desc: '新订单、入住和退款状态变化', enabled: true },
	{ key: 'review', title: '评价提醒', desc: '收到新评价时通知', enabled: true },
	{ key: 'finance', title: '财务结算提醒', desc: '提现和结算状态变化', enabled: false }
]

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
	guests: clone(demoGuests)
}

export const getProfile = () => read(storageKeys.profile, defaultProfile)

export const saveProfile = (profile) => write(storageKeys.profile, {
	...defaultProfile,
	...profile
})

export const setLoggedIn = (profile = {}) => {
	write(storageKeys.auth, true)
	return saveProfile({
		...profile,
		loggedIn: true,
		uid: profile.uid || '88209412',
		name: profile.name || '张三',
		phone: profile.phone || '138****8888'
	})
}

export const isLoggedIn = () => Boolean(read(storageKeys.auth, false) && getProfile().loggedIn)

export const getMerchantProfile = () => read(storageKeys.merchant, defaultMerchant)

export const saveMerchantProfile = (merchant = {}) => write(storageKeys.merchant, {
	...defaultMerchant,
	...merchant
})

export const updateMerchantStatus = (status, patch = {}) => saveMerchantProfile({
	...patch,
	status,
	enabled: status === 'approved'
})

export const getMerchantRooms = () => read(storageKeys.merchantRooms, defaultMerchantRooms)

export const saveMerchantRooms = (rooms) => write(storageKeys.merchantRooms, rooms)

export const findMerchantRoom = (id) => getMerchantRooms().find((room) => String(room.id) === String(id))

export const upsertMerchantRoom = (room = {}) => {
	const rooms = getMerchantRooms()
	const index = rooms.findIndex((item) => String(item.id) === String(room.id))
	const value = {
		id: room.id || `merchant-room-${Date.now()}`,
		name: room.name || '未命名房间',
		type: room.type || '独立房间',
		status: room.status || '草稿',
		key: room.key || (room.status === '已上架' ? 'online' : 'draft'),
		price: String(room.price || '0'),
		stock: String(room.stock || '1'),
		capacity: room.capacity || '可住2人',
		area: room.area || '待完善',
		image: room.image || prototypeImages.roomOne,
		updatedAt: room.updatedAt || new Date().toISOString().slice(0, 16).replace('T', ' '),
		...room
	}
	if (index >= 0) rooms.splice(index, 1, { ...rooms[index], ...value })
	else rooms.unshift(value)
	saveMerchantRooms(rooms)
	return value
}

export const updateMerchantRoom = (id, patch = {}) => {
	const rooms = getMerchantRooms()
	const index = rooms.findIndex((room) => String(room.id) === String(id))
	if (index < 0) return null
	rooms[index] = { ...rooms[index], ...patch, updatedAt: new Date().toISOString().slice(0, 16).replace('T', ' ') }
	return saveMerchantRooms(rooms).find((room) => String(room.id) === String(id))
}

export const getMerchantOrders = () => read(storageKeys.merchantOrders, defaultMerchantOrders)

export const saveMerchantOrders = (orders) => write(storageKeys.merchantOrders, orders)

export const updateMerchantOrder = (id, patch = {}) => {
	const orders = getMerchantOrders()
	const index = orders.findIndex((order) => String(order.id) === String(id))
	if (index < 0) return null
	orders[index] = { ...orders[index], ...patch }
	saveMerchantOrders(orders)
	return orders[index]
}

export const getMerchantVerifyRecords = () => read(storageKeys.merchantVerifyRecords, [])

export const saveMerchantVerifyRecords = (records) => write(storageKeys.merchantVerifyRecords, records)

export const addMerchantVerifyRecord = (record = {}) => {
	const records = getMerchantVerifyRecords()
	const value = {
		id: record.id || `verify-${Date.now()}`,
		code: record.code || '',
		guest: record.guest || '待核验住客',
		roomName: record.roomName || '未指定房间',
		status: record.status || '已核销',
		time: record.time || new Date().toISOString().slice(0, 16).replace('T', ' ')
	}
	records.unshift(value)
	saveMerchantVerifyRecords(records)
	return value
}

export const getMerchantReviews = () => read(storageKeys.merchantReviews, defaultMerchantReviews)

export const saveMerchantReviews = (reviews) => write(storageKeys.merchantReviews, reviews)

export const updateMerchantReview = (id, patch = {}) => {
	const reviews = getMerchantReviews()
	const index = reviews.findIndex((review) => String(review.id) === String(id))
	if (index < 0) return null
	reviews[index] = { ...reviews[index], ...patch }
	saveMerchantReviews(reviews)
	return reviews[index]
}

export const getMerchantCoupons = () => read(storageKeys.merchantCoupons, defaultMerchantCoupons)

export const saveMerchantCoupons = (coupons) => write(storageKeys.merchantCoupons, coupons)

export const addMerchantCoupon = (coupon = {}) => {
	const coupons = getMerchantCoupons()
	const value = {
		id: coupon.id || `coupon-${Date.now()}`,
		name: coupon.name || '新建优惠券',
		amount: String(coupon.amount || '20'),
		threshold: coupon.threshold || '满200可用',
		stock: Number(coupon.stock || 20),
		used: 0,
		status: '生效中'
	}
	coupons.unshift(value)
	saveMerchantCoupons(coupons)
	return value
}

export const updateMerchantCoupon = (id, patch = {}) => {
	const coupons = getMerchantCoupons()
	const index = coupons.findIndex((coupon) => String(coupon.id) === String(id))
	if (index < 0) return null
	coupons[index] = { ...coupons[index], ...patch }
	saveMerchantCoupons(coupons)
	return coupons[index]
}

export const getMerchantStaff = () => read(storageKeys.merchantStaff, defaultMerchantStaff)

export const saveMerchantStaff = (staff) => write(storageKeys.merchantStaff, staff)

export const getMerchantSettings = () => read(storageKeys.merchantSettings, defaultMerchantSettings)

export const saveMerchantSettings = (settings = {}) => write(storageKeys.merchantSettings, {
	...defaultMerchantSettings,
	...settings
})

export const getMerchantMessageSettings = () => read(storageKeys.merchantMessages, defaultMerchantMessages)

export const saveMerchantMessageSettings = (messages) => write(storageKeys.merchantMessages, messages)

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

	const merchantReviews = getMerchantReviews()
	const merchantIndex = merchantReviews.findIndex((item) => String(item.id) === String(value.id))
	const merchantValue = {
		id: value.id,
		guest: value.nickname,
		rating: value.roomRating,
		date: value.checkIn || value.createdAt.slice(0, 10),
		content: value.content,
		reply: ''
	}
	if (merchantIndex >= 0) merchantReviews.splice(merchantIndex, 1, { ...merchantReviews[merchantIndex], ...merchantValue })
	else merchantReviews.unshift(merchantValue)
	saveMerchantReviews(merchantReviews)
	return value
}

export const getApiToken = () => read(storageKeys.apiToken, '')

export const saveApiToken = (token) => write(storageKeys.apiToken, token || '')

export const logout = () => {
	write(storageKeys.auth, false)
	write(storageKeys.apiToken, '')
	return saveProfile({ ...defaultProfile, loggedIn: false })
}

export const getGuests = () => read(storageKeys.guests, demoGuests)

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

export const createOrder = (booking = {}) => {
	const orders = getOrders()
	const now = new Date()
	const stamp = [
		now.getFullYear(),
		String(now.getMonth() + 1).padStart(2, '0'),
		String(now.getDate()).padStart(2, '0'),
		String(now.getHours()).padStart(2, '0'),
		String(now.getMinutes()).padStart(2, '0'),
		String(now.getSeconds()).padStart(2, '0')
	].join('')
	const order = {
		id: `TCP${stamp}`,
		store: booking.store || defaultBooking.store,
		storeId: booking.storeId || defaultBooking.storeId,
		address: booking.address || defaultBooking.address,
		room: booking.room || defaultBooking.room,
		roomId: booking.roomId || defaultBooking.roomId,
		image: booking.roomImage || defaultBooking.roomImage,
		status: '待支付',
		statusKey: 'pay',
		date: booking.date || defaultBooking.date,
		checkIn: booking.checkIn || defaultBooking.checkIn,
		checkOut: booking.checkOut || defaultBooking.checkOut,
		isoCheckIn: booking.isoCheckIn || defaultBooking.isoCheckIn,
		isoCheckOut: booking.isoCheckOut || defaultBooking.isoCheckOut,
		nights: Number(booking.nights || 1),
		roomCount: Number(booking.roomCount || 1),
		guest: (booking.guests || defaultBooking.guests).map((guest) => guest.name).join(' 等'),
		guests: clone(booking.guests || defaultBooking.guests),
		price: String(Number(booking.total || booking.roomPrice || defaultBooking.roomPrice)),
		action: '立即支付',
		createdAt: now.toISOString()
	}
	orders.unshift(order)
	saveOrders(orders)
	return order
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
	guests: clone(booking.guests || defaultBooking.guests)
})

export const getFavorites = () => read(storageKeys.favorites, [])

export const getFavoriteRecords = () => {
	const metadata = read(storageKeys.favoriteMeta, {})
	return getFavorites().map((id) => ({
		id: String(id),
		...(metadata[String(id)] || {})
	}))
}

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
