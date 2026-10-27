<template>
	<view class="proto-page room-detail-page">
		<view class="room-detail-hero">
			<image :src="roomImages[0] || room.image" mode="aspectFill"></image>
			<view class="hero-shade"></view>
			<button class="hero-back-button" hover-class="none" aria-label="返回" @tap="goBack">
				<ProtoIcon name="back" tone="white" :size="38" />
			</button>
			<text class="hero-photo-count">1 / {{ roomImages.length || 1 }}</text>
		</view>
		<view v-if="showScrollHeader" class="room-scroll-header">
			<ProtoHeader title="房型详情" title-align="center" theme="green" />
		</view>

		<view class="room-hero-summary">
			<view class="room-store-bar" @tap="openLocation">
				<view class="room-store-title-row">
					<ProtoIcon name="store" :size="22" />
					<text class="room-store-name">{{ store.name || '门店信息加载中' }}</text>
					<text v-if="store.status" class="room-store-status">{{ store.status }}</text>
				</view>
				<view class="room-store-meta-row">
					<text v-if="store.rating">{{ store.rating }} 分</text>
					<text v-if="store.reviews">{{ store.reviews }} 条门店评价</text>
					<text v-if="store.distance && !store.unknownDistance">距您 {{ store.distance }}</text>
					<view class="room-store-link"><text>门店位置</text><ProtoIcon name="chevron-right" :size="16" /></view>
				</view>
			</view>
			<view class="detail-title-row">
				<view class="detail-title-copy">
					<text class="room-detail-title">{{ room.name }}</text>
					<text class="room-detail-subtitle">静谧采光 · 独立卫浴 · 入住更安心</text>
				</view>
			</view>
			<view class="room-detail-tags">
				<text v-for="tag in detailTags" :key="tag">{{ tag }}</text>
			</view>
			<view class="detail-highlight-grid">
				<view class="detail-highlight review-highlight" @tap="goReviews">
					<view class="highlight-value"><ProtoIcon name="star" :size="25" /><text>{{ roomRatingLabel }} 超棒</text></view>
					<text>{{ reviewSummary.reviewCount }}条真实评价</text>
				</view>
				<view class="detail-highlight location-highlight" @tap="openLocation">
					<view class="highlight-value"><ProtoIcon name="location" :size="25" /><text>{{ store.distance && !store.unknownDistance ? `距您 ${store.distance}` : '民宿位置' }}</text></view>
					<text>{{ store.address || '郑州市中心 · 交通便利' }}</text>
				</view>
			</view>
		</view>

		<view class="proto-card itinerary-card" @tap="goDate">
			<view class="section-heading itinerary-heading">
				<view class="inline-icon-text"><ProtoIcon name="calendar" :size="26" /><text>入住日期</text></view>
				<view class="proto-link" @tap.stop="goDate"><text>修改日期</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view class="stay-strip">
				<view class="stay-side">
					<view class="date-title"><text class="strong">{{ booking.checkIn || '10月07日' }}</text><text class="date-context">{{ checkInContext }}</text></view>
					<text class="small">14:00后</text>
				</view>
				<view class="stay-night"><text>共{{ booking.nights || 1 }}晚</text><ProtoIcon name="chevron-right" :size="22" /></view>
				<view class="stay-side stay-side-right">
					<view class="date-title"><text class="strong">{{ booking.checkOut || '10月08日' }}</text><text class="date-context">{{ checkOutContext }}</text></view>
					<text class="small">12:00前</text>
				</view>
			</view>
			<view class="stay-summary"><text>当前房型 · {{ room.capacity || '可住2人' }} · {{ room.beds || '舒适床铺' }}</text></view>
		</view>

		<view class="proto-card room-choice-card">
			<view class="section-heading"><text>房型信息</text><text class="proto-caption">实时库存 · 即时确认</text></view>
			<view class="room-choice-body">
				<image :src="roomImages[0] || room.image" mode="aspectFill"></image>
				<view class="room-choice-copy">
					<text class="room-choice-title">{{ room.name }}</text>
					<text class="room-choice-spec">{{ room.capacity }} · {{ room.beds }} · {{ room.area }}</text>
					<view class="room-choice-tags"><text v-for="tag in (room.tags || []).slice(0, 3)" :key="tag">{{ tag }}</text></view>
				</view>
				<view class="room-choice-price">
					<text>¥{{ room.price }}</text>
					<text>/晚起</text>
					<button class="room-book-button" hover-class="none" @tap.stop="goBooking"><text>订</text></button>
				</view>
			</view>
			<view class="room-choice-footer"><ProtoIcon name="check-circle" :size="20" /><text>{{ room.availability || '今日可预订' }}</text></view>
		</view>

		<view class="proto-card other-room-card">
			<view class="section-heading"><text>其他房型</text><text class="proto-link">{{ otherRooms.length }}间可选</text></view>
			<view v-for="item in otherRooms" :key="item.id" class="other-room-item" @tap="goOtherRoom(item)">
				<image :src="item.image" mode="aspectFill"></image>
				<view class="other-room-copy"><text class="other-room-title">{{ item.name }}</text><text class="other-room-spec">{{ item.capacity }} · {{ item.area }}</text><view class="other-room-tags"><text v-for="tag in (item.tags || []).slice(0, 2)" :key="tag">{{ tag }}</text></view></view>
				<view class="other-room-price"><text>¥{{ item.price }}</text><text>/晚</text><button class="other-room-button" hover-class="none" @tap.stop="bookOtherRoom(item)"><text>订</text></button></view>
			</view>
		</view>

		<view class="proto-card feature-card">
			<view class="section-heading"><text>房屋特色</text><text class="proto-caption">让每次入住都更舒服</text></view>
			<text class="feature-copy">{{ store.description || '静谧采光，干净整洁，适合短住与周末放松。' }}</text>
		</view>

		<view class="proto-card facilities-card">
			<view class="section-heading"><text>服务与设施</text><text class="proto-caption">已为你准备</text></view>
			<view v-for="group in facilityGroups" :key="group.title" class="facility-group"><text class="facility-group-title">{{ group.title }}</text><view class="facility-items"><view v-for="item in group.items" :key="item"><ProtoIcon name="check-circle" :size="26" /><text>{{ item }}</text></view></view></view>
		</view>

		<view class="proto-card policy-card">
			<view class="section-heading"><text>预订须知</text><text class="proto-caption">请提前了解入住规则</text></view>
			<view v-for="item in policies" :key="item.title" class="policy-item">
				<view class="policy-title-row" @tap="togglePolicy(item.key)"><view class="inline-icon-text"><ProtoIcon :name="item.icon" :size="26" /><text>{{ item.title }}</text></view><ProtoIcon :name="openPolicies.indexOf(item.key) >= 0 ? 'chevron-up' : 'chevron-down'" :size="24" /></view>
				<view v-if="openPolicies.indexOf(item.key) >= 0" class="policy-body"><text v-for="line in item.lines" :key="line">{{ line }}</text></view>
			</view>
		</view>

		<view class="proto-card location-card">
			<view class="section-heading">
				<text>周边位置</text>
				<view class="proto-link location-heading-action" @tap="openLocation"><text>查看地图</text><ProtoIcon name="chevron-right" :size="18" /></view>
			</view>
			<view class="location-map">
				<map
					id="roomDetailMap"
					class="location-map-canvas"
					:latitude="mapCenter.latitude"
					:longitude="mapCenter.longitude"
					:scale="mapScale"
					:markers="mapMarkers"
					:enable-scroll="true"
					:enable-zoom="true"
					:enable-rotate="false"
					:enable-overlooking="false"
					@markertap="onMarkerTap"
					@callouttap="onMarkerTap"
					@error="onMapError"
				></map>
				<view class="location-map-hint"><ProtoIcon name="map" :size="18" /><text>{{ mapNotice || '双指缩放、拖动地图查看周边' }}</text></view>
				<view v-if="mapError" class="location-map-error"><ProtoIcon name="info" :size="18" /><text>地图渲染失败：{{ mapError }}</text></view>
			</view>
			<view class="location-map-tools">
				<button class="location-map-tool" hover-class="none" @tap="resetMapView"><ProtoIcon name="store" :size="20" /><text>民宿位置</text></button>
				<button class="location-map-tool" hover-class="none" @tap="openLocation"><ProtoIcon name="navigation" :size="20" /><text>导航前往</text></button>
			</view>
			<scroll-view v-if="nearbyPlaces.length" class="nearby-scroll" scroll-x :show-scrollbar="false">
				<view
					v-for="place in nearbyPlaces"
					:key="place.id"
					class="nearby-chip"
					:class="{ 'nearby-chip-active': selectedPlace && selectedPlace.id === place.id }"
					@tap="focusPlace(place)"
				>
					<text class="nearby-chip-name">{{ place.name }}</text>
					<text class="nearby-chip-meta">{{ place.typeLabel }} · {{ formatDistance(place.distance) }}</text>
				</view>
			</scroll-view>
			<view v-else-if="nearbyLoading" class="nearby-tip">正在加载周边地点…</view>
			<view class="location-address" @tap="openLocation">
				<view class="location-address-main"><ProtoIcon name="location" :size="22" /><text class="location-title">{{ store.address || '郑州市中心，出行便利' }}</text></view>
				<ProtoIcon name="chevron-right" :size="18" />
			</view>
			<view class="location-meta-row">
				<text class="location-note">{{ locationNote }}</text>
				<text class="location-copy" @tap.stop="copyAddress">复制地址</text>
			</view>
		</view>

		<view class="proto-bottom-actions room-bottom-actions">
			<button class="detail-bottom-link" hover-class="none" @tap="goHome"><ProtoIcon name="home" :size="38" /><text>首页</text></button>
			<button class="detail-bottom-link" hover-class="none" @tap="toggleFavorite"><ProtoIcon :name="favorite ? 'heart-filled' : 'heart'" :size="38" /><text>{{ favorite ? '已收藏' : '收藏' }}</text></button>
			<button class="detail-bottom-link" hover-class="none" open-type="share"><ProtoIcon name="share" :size="38" /><text>分享</text></button>
			<button class="proto-primary-button" hover-class="none" @tap="goBooking"><text>立即预订</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoRooms, demoStores, goPage, showToast } from '@/common/prototype.js'
	import { getBooking, getFavorites, getRoomReviewSummary, saveBooking, toggleFavorite } from '@/common/app-store.js'
	import { addMyFavorite, fetchMyFavorites, fetchRoomReviews, fetchStoreDetail, recordMyRecent, removeMyFavorite } from '@/common/api.js'
	import { openBookingConfirm } from '@/common/navigation.js'
	import { ALLOW_DEMO_DATA } from '@/common/env.js'
	import { geocodeAddress, searchNearby } from '@/common/amap.js'

	// 门店坐标来自后端 decimal，可能是字符串或 null，统一转成数字再交给 map 组件。
	const toPoint = (store) => {
		if (!store || store.latitude === null || store.latitude === undefined) return null
		if (store.longitude === null || store.longitude === undefined) return null
		const latitude = Number(store.latitude)
		const longitude = Number(store.longitude)
		if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) return null
		if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) return null
		return { latitude, longitude }
	}

	// 坐标彻底拿不到时的兜底视野，避免地图白屏。
	const CITY_CENTER = { latitude: 34.74725, longitude: 113.62493 }

	const getDateContext = (isoDate, fallback) => {
		const parts = String(isoDate || '').split('T')[0].split('-').map(Number)
		if (parts.length !== 3 || parts.some((part) => !Number.isFinite(part))) return fallback
		const date = new Date(parts[0], parts[1] - 1, parts[2])
		const today = new Date()
		today.setHours(0, 0, 0, 0)
		const diff = Math.round((date.getTime() - today.getTime()) / 86400000)
		if (diff === 0) return '今天'
		if (diff === 1) return '明天'
		if (diff === 2) return '后天'
		return fallback
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				room: demoRooms[0],
				store: demoStores[0],
				relatedRooms: [],
				storeId: getBooking().storeId || '',
				favorite: false,
				booking: getBooking(),
				reviewSummary: {
					roomAverageRating: 0,
					storeAverageRating: 0,
					reviewCount: 0
				},
				showScrollHeader: false,
				openPolicies: ['checkin', 'cancel'],
				mapCenter: { ...CITY_CENTER },
				mapScale: 14,
				mapPoint: null,
				mapMarkers: [],
				markerPlaces: {},
				mapNotice: '',
				mapError: '',
				nearbyPlaces: [],
				nearbyLoading: false,
				selectedPlace: null,
				facilityGroups: [
					{ title: '服务', items: ['围炉煮茶', '前台接待', '行李寄存', '免费停车'] },
					{ title: '基础', items: ['零食、饮品', '网络电视', '空调', '窗户', '无线网络'] },
					{ title: '卫浴', items: ['电吹风', '独立卫浴', '热水'] },
					{ title: '周边', items: ['医院', '商场', '超市'] }
				],
				policies: [
					{
						key: 'checkin',
						icon: 'clock',
						title: '入住离店与安保须知',
						lines: ['入离时间：当日14:00以后办理入住，次日12:00之前办理退房。', '押金政策：免收住宿押金，入住需办理居民身份证实名核验。', '环境守则：房内全面禁烟，严禁携带宠物，22:00后请调低音量。']
					},
					{
						key: 'cancel',
						icon: 'refresh',
						title: '取消与退改说明',
						lines: ['免费取消：入住日前1天18:00前申请，支持免费无条件全额退还。', '逾期扣减：超过规定时间取消或未如期入住，将扣除首晚房费。']
					}
				]
			}
		},
		computed: {
			roomImages() {
				const raw = this.room && (this.room.images || this.room.imageUrls || this.room.gallery || this.room.photos)
				const values = (Array.isArray(raw) ? raw : [])
					.map((item) => typeof item === 'string' ? item : item && (item.url || item.src || item.path || item.image))
				const fallback = this.room && this.room.image ? [this.room.image] : []
				return [...new Set([...values, ...fallback].filter(Boolean))].slice(0, 6)
			},
			detailTags() {
				const room = this.room || {}
				return [...new Set([room.type, room.gender, room.capacity, room.beds, room.area ? `建筑面积 ${room.area}` : '', '有外窗'].filter(Boolean))].slice(0, 6)
			},
			otherRooms() {
				if (this.relatedRooms.length) return this.relatedRooms
				if (!ALLOW_DEMO_DATA) return []
				return demoRooms.filter((item) => String(item.id) !== String(this.room && this.room.id))
			},
			checkInContext() {
				return getDateContext(this.booking.isoCheckIn, '入住')
			},
			checkOutContext() {
				return getDateContext(this.booking.isoCheckOut, '离店')
			},
			roomRatingLabel() {
				return Number(this.reviewSummary.roomAverageRating || 0).toFixed(1)
			},
			locationNote() {
				const place = this.selectedPlace
				if (place) return `${place.typeLabel} · 距离民宿约 ${this.formatDistance(place.distance)}`
				if (this.nearbyPlaces.length) return `周边 ${this.nearbyPlaces.length} 个地点，点击标签可定位`
				return '步行可达餐饮、商圈与公共交通'
			}
		},
		async onLoad(options) {
			const requestedRoomId = options && options.id
			const localRoom = demoRooms.find((item) => String(item.id) === String(requestedRoomId))
			if (localRoom) this.room = localRoom
			this.storeId = options && options.storeId ? options.storeId : this.booking.storeId
			const localStore = demoStores.find((item) => String(item.id) === String(this.storeId))
			if (localStore) this.store = localStore
			this.reviewSummary = getRoomReviewSummary(this.room.id, this.storeId)
			if (this.storeId && (!localStore || !localRoom)) {
				try {
					const result = await fetchStoreDetail(this.storeId)
					if (result.store) this.store = result.store
					const remoteRoom = result.rooms.find((item) => String(item.id) === String(requestedRoomId))
					if (remoteRoom) this.room = remoteRoom
					else if (result.rooms.length) {
						// 房型已下线或链接过期时退回该门店第一间房，避免继续展示内置演示房型。
						this.room = result.rooms[0]
					}
					this.relatedRooms = result.rooms.filter((item) => String(item.id) !== String(this.room.id))
				} catch (error) {
					// Keep the bundled room detail available when the API is offline.
				}
			}
			this.reviewSummary = getRoomReviewSummary(this.room.id, this.storeId)
			if (/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(this.room.id))) {
				try {
					this.reviewSummary = await fetchRoomReviews(this.room.id)
				} catch (error) {
					// Keep local review data available when the API is offline.
				}
			}
			this.booking = saveBooking({
				...this.booking,
				storeId: this.storeId || this.room.storeId,
				store: this.store.name || this.booking.store,
				address: this.store.address || this.booking.address,
				roomId: this.room.id,
				room: this.room.name,
				roomImage: this.room.image,
				roomPrice: Number(this.room.price || 0)
			})
			this.favorite = getFavorites().map(String).includes(`room-${this.room.id}`)
			this.refreshFavoriteState()
			recordMyRecent({
				id: String(this.room.id),
				type: 'room',
				title: this.room.name,
				subtitle: `¥${this.room.price}/晚 · ${this.room.capacity}`,
				image: this.room.image,
				path: `/pages/room/detail?id=${this.room.id}&storeId=${this.storeId || ''}`
			}).catch(() => {})
			this.prepareLocation()
		},
		onShow() {
			this.booking = getBooking()
			this.refreshFavoriteState()
			this.reviewSummary = getRoomReviewSummary(this.room.id, this.storeId)
		},
		onShareAppMessage() {
			// 底部「分享」按钮用 open-type="share" 触发，这里提供标题、路径与封面。
			const room = this.room || {}
			const storeId = this.storeId || room.storeId || ''
			const price = Number(room.price || 0)
			return {
				title: price > 0 ? `${room.name || '房型详情'} · ¥${price}/晚` : (room.name || '房型详情'),
				path: `/pages/room/detail?id=${encodeURIComponent(room.id || '')}&storeId=${encodeURIComponent(storeId)}`,
				imageUrl: room.image || ''
			}
		},
		onPageScroll(event) {
			const scrollTop = Number(event && event.scrollTop) || 0
			const nextVisible = scrollTop > 300
			if (nextVisible !== this.showScrollHeader) {
				this.showScrollHeader = nextVisible
			}
		},
		methods: {
			goBack() {
				uni.navigateBack({ delta: 1 })
			},
			prepareBooking(room = this.room) {
				const maxGuests = Number(room.maxGuests || 0) || null
				const roomCount = Math.max(1, Number(this.booking.roomCount || 1))
				const wantedGuests = Math.max(1, Number(this.booking.guestCount || 1))
				// 床位房等房型有人数上限（maxGuests × 间数），超员会在后端报价时 400，保存前先收敛。
				const guestCount = maxGuests ? Math.min(wantedGuests, maxGuests * roomCount) : wantedGuests
				this.booking = saveBooking({
					...this.booking,
					storeId: this.storeId || room.storeId || this.booking.storeId,
					store: this.store.name || this.booking.store,
					address: this.store.address || this.booking.address,
					roomId: room.id,
					room: room.name,
					roomImage: room.image,
					roomPrice: Number(room.price || 0),
					roomArea: room.area || '',
					roomTags: room.tags || [],
					maxGuests,
					guestCount
				})
				return this.booking
			},
			goDate() {
				goPage(`/pages/booking/date?from=room-detail&id=${encodeURIComponent(this.room.id)}&storeId=${encodeURIComponent(this.storeId || this.room.storeId || '')}`)
			},
			goBooking() {
				this.prepareBooking()
				openBookingConfirm()
			},
			goReviews() {
				const query = [
					`roomId=${encodeURIComponent(this.room.id || '')}`,
					`storeId=${encodeURIComponent(this.storeId || this.room.storeId || '')}`,
					`roomName=${encodeURIComponent(this.room.name || '')}`,
					`storeName=${encodeURIComponent(this.store.name || '')}`
				].join('&')
				goPage(`/pages/review/list?${query}`)
			},
			goService() {
				goPage('/pages/service/contact')
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.store.phone || '037188886622',
					fail: () => showToast('暂无法拨打前台电话')
				})
			},
			async refreshFavoriteState() {
				if (!this.room || !this.room.id) return
				const id = `room-${this.room.id}`
				try {
					const favorites = await fetchMyFavorites()
					this.favorite = favorites.map((item) => item.id).includes(id)
				} catch (error) {
					this.favorite = getFavorites().map(String).includes(id)
				}
			},
			async toggleFavorite() {
				if (!this.room || !this.room.id) return
				const id = `room-${this.room.id}`
				const record = {
					id,
					type: 'room',
					title: this.room.name,
					subtitle: `¥${this.room.price}/晚 · ${this.room.capacity}`,
					image: this.room.image,
					path: `/pages/room/detail?id=${this.room.id}&storeId=${this.storeId || this.room.storeId || ''}`
				}
				const locallyFavorited = getFavorites().map(String).includes(id)
				try {
					if (this.favorite) {
						await removeMyFavorite(id)
						this.favorite = false
						if (locallyFavorited) toggleFavorite(id)
					} else {
						await addMyFavorite(record)
						this.favorite = true
						if (!locallyFavorited) toggleFavorite(id, record)
					}
					showToast(this.favorite ? '已收藏房型' : '已取消收藏')
				} catch (error) {
					showToast(error?.message || '收藏操作失败')
				}
			},
			goHome() {
				uni.switchTab({
					url: '/pages/index/index',
					fail: () => uni.reLaunch({ url: '/pages/index/index' })
				})
			},
			goOtherRoom(item) {
				if (!item || !item.id) return
				goPage(`/pages/room/detail?id=${item.id}&storeId=${this.storeId || item.storeId || ''}`)
			},
			bookOtherRoom(item) {
				if (!item || item.soldOut) {
					showToast('当前房型暂不可预订')
					return
				}
				this.prepareBooking(item)
				openBookingConfirm()
			},
			// 详情页周边地图：先用门店坐标，缺失时按地址反查，最后才退回城市中心。
			prepareLocation() {
				if (!this.locationTask) this.locationTask = this.resolveLocation()
				return this.locationTask
			},
			async resolveLocation() {
				const store = this.store || {}
				let point = toPoint(store)
				if (!point) {
					try {
						point = await geocodeAddress(store.address || '', store.city || '')
					} catch (error) {
						point = null
					}
				}
				this.mapNotice = point ? '' : '未获取到民宿坐标，当前显示城市中心'
				this.mapPoint = point || { ...CITY_CENTER }
				this.mapCenter = { ...this.mapPoint }
				this.buildMapMarkers()
				this.loadNearby()
			},
			async loadNearby() {
				if (!this.mapPoint) return
				this.nearbyLoading = true
				try {
					const places = await searchNearby(this.mapPoint.latitude, this.mapPoint.longitude, { radius: 1200, limit: 6 })
					this.nearbyPlaces = Array.isArray(places) ? places : []
				} catch (error) {
					// 高德不可用时不打扰用户，地图仍可浏览民宿位置。
					this.nearbyPlaces = []
				} finally {
					this.nearbyLoading = false
					this.buildMapMarkers()
				}
			},
			buildMapMarkers() {
				if (!this.mapPoint) return
				const store = this.store || {}
				const places = {}
				const markers = [{
					id: 1,
					latitude: this.mapPoint.latitude,
					longitude: this.mapPoint.longitude,
					// 不传 iconPath 时用微信默认图标，但宽高必须显式给，否则渲染层直接报错。
					width: 24,
					height: 34,
					callout: {
						content: store.shortName || store.name || '民宿位置',
						color: '#0f6b6b',
						fontSize: 11,
						borderRadius: 8,
						borderWidth: 1,
						borderColor: '#cfe4e4',
						bgColor: '#ffffff',
						padding: 6,
						textAlign: 'center',
						display: 'ALWAYS'
					}
				}]
				this.nearbyPlaces.forEach((place, index) => {
					const markerId = index + 2
					places[markerId] = place
					markers.push({
						id: markerId,
						latitude: place.latitude,
						longitude: place.longitude,
						width: 20,
						height: 28,
						callout: {
							content: place.name,
							color: '#25545a',
							fontSize: 10,
							borderRadius: 6,
							borderWidth: 1,
							borderColor: '#d8e6e6',
							bgColor: '#ffffff',
							padding: 4,
							display: 'BYCLICK'
						}
					})
				})
				this.markerPlaces = places
				this.mapMarkers = markers
			},
			moveMapTo(point) {
				if (!point) return
				try {
					const context = uni.createMapContext('roomDetailMap', this)
					if (context && typeof context.moveToLocation === 'function') {
						context.moveToLocation({ latitude: point.latitude, longitude: point.longitude })
					}
				} catch (error) {
					// 低版本基础库缺少 moveToLocation，地图仍可手动拖动。
				}
			},
			resetMapView() {
				this.selectedPlace = null
				this.mapCenter = { ...this.mapPoint }
				this.mapScale = 14
				this.moveMapTo(this.mapPoint)
			},
			focusPlace(place) {
				if (!place) return
				this.selectedPlace = place
				this.mapCenter = { latitude: place.latitude, longitude: place.longitude }
				this.mapScale = 16
				this.moveMapTo(place)
			},
			onMarkerTap(event) {
				const markerId = Number((event && event.detail && event.detail.markerId) || 0)
				if (markerId < 2) return
				const place = this.markerPlaces[markerId]
				if (place) this.selectedPlace = place
			},
			onMapError(event) {
				const detail = (event && event.detail) || {}
				this.mapError = detail.errMsg || '地图组件不可用'
			},
			async openLocation() {
				// 地址反查还没回来时先等一步，避免跳到城市中心。
				if (!this.mapPoint) await this.prepareLocation()
				const point = this.mapPoint || this.mapCenter || { ...CITY_CENTER }
				uni.openLocation({
					latitude: Number(point.latitude),
					longitude: Number(point.longitude),
					name: this.store.shortName || this.store.name || '民宿地址',
					address: this.store.address || '',
					scale: 18,
					fail: () => showToast('暂无法打开地图')
				})
			},
			formatDistance(distance) {
				const meters = Number(distance)
				if (!Number.isFinite(meters) || meters <= 0) return '附近'
				return meters < 1000 ? `${Math.round(meters)}m` : `${(meters / 1000).toFixed(1)}km`
			},
			copyAddress() {
				const address = this.store.address || ''
				if (!address) return showToast('暂无详细地址')
				uni.setClipboardData({ data: address, success: () => showToast('地址已复制') })
			},
			togglePolicy(key) {
				const index = this.openPolicies.indexOf(key)
				if (index >= 0) this.openPolicies.splice(index, 1)
				else this.openPolicies.push(key)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.room-detail-page {
		padding-bottom: 160rpx;
		padding-bottom: calc(160rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(160rpx + env(safe-area-inset-bottom));
	}

	.room-detail-hero {
		position: relative;
		height: 540rpx;
	}

	.room-detail-hero > image {
		width: 100%;
		height: 100%;
	}

	.hero-shade {
		position: absolute;
		right: 0;
		bottom: 0;
		left: 0;
		height: 34%;
		background: linear-gradient(180deg, transparent, rgba(32, 37, 43, 0.45));
	}

	.room-detail-hero .proto-header {
		position: absolute;
		top: 0;
		right: 0;
		left: 0;
		background: transparent;
		border-bottom: 0;
	}


	.hero-photo-count {
		position: absolute;
		right: 24rpx;
		bottom: 18rpx;
		padding: 8rpx 14rpx;
		color: #ffffff;
		background: rgba(32, 37, 43, 0.72);
		border-radius: 14rpx;
		font-size: 18rpx;
	}

	.room-hero-summary {
		position: relative;
		margin-top: -20rpx;
	}


	.room-detail-title {
		display: block;
		margin-top: 14rpx;
		color: var(--proto-text);
		font-size: 31rpx;
		font-weight: 800;
	}

	.room-detail-subtitle {
		display: block;
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.room-detail-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 10rpx;
		margin-top: 16rpx;
	}

	.room-detail-tags text {
		padding: 8rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 8rpx;
		font-size: 18rpx;
	}


	.section-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.inline-icon-text {
		display: flex;
		align-items: center;
	}

	.inline-icon-text .proto-icon {
		margin-right: 5rpx;
	}

	.section-heading .proto-link,
	.section-heading .proto-caption {
		font-size: 18rpx;
		font-weight: 400;
	}

	.stay-strip {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 20rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.stay-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.stay-side-right {
		align-items: flex-end;
		text-align: right;
	}

	.stay-side > text:first-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.stay-side .strong {
		margin-top: 6rpx;
		color: var(--proto-text);
		font-size: 25rpx;
	}

	.stay-side .small {
		color: var(--proto-muted);
		font-size: 17rpx;
		font-weight: 400;
	}

	.stay-night {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		padding: 8rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		font-size: 18rpx;
		text-align: center;
	}

	.stay-night > .proto-icon {
		margin-top: 3rpx;
	}

	.stay-strip button {
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 19rpx;
	}


	.policy-item {
		margin-top: 16rpx;
		padding: 16rpx;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.policy-title-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 700;
	}

	.policy-title-row .inline-icon-text {
		min-width: 0;
	}

	.policy-body {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		margin-top: 14rpx;
		color: var(--proto-muted);
		font-size: 22rpx;
		line-height: 1.55;
	}


	.room-bottom-actions {
		align-items: center;
		gap: 10rpx;
	}

	.room-bottom-actions .detail-bottom-link {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		font-size: 17rpx;
	}

	.room-bottom-actions .detail-bottom-link .proto-icon {
		margin-right: 3rpx;
	}


	.room-bottom-actions .proto-primary-button {
		flex: 1.5;
		height: 78rpx;
		font-size: 23rpx;
	}

	/* Detail page visual hierarchy: image first, then a compact green-toned booking flow. */
	.room-detail-page {
		background: #f2f9fa;
		padding-bottom: 182rpx;
		padding-bottom: calc(182rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(182rpx + env(safe-area-inset-bottom));
	}

	.room-detail-hero {
		height: 620rpx;
		background: var(--proto-surface-low);
	}

	.room-detail-hero > image {
		width: 100%;
		height: 100%;
	}

	.hero-shade {
		height: 46%;
		background: linear-gradient(180deg, rgba(0, 29, 39, 0.04), rgba(0, 63, 76, 0.64));
	}

	.room-detail-hero .proto-header {
		background: rgba(7, 53, 64, 0.1);
		border-bottom-color: transparent;
	}

	.room-detail-hero .proto-header-title,
	.room-detail-hero .proto-header-icon-button,
	.room-detail-hero .proto-header-user-button {
		color: #ffffff;
	}

	.room-detail-hero .proto-header-user-button {
		background: rgba(0, 106, 106, 0.8);
	}


	.hero-photo-count {
		right: 26rpx;
		bottom: 28rpx;
		padding: 8rpx 16rpx;
		background: rgba(0, 55, 67, 0.72);
		border: 1rpx solid rgba(255, 255, 255, 0.2);
		border-radius: 18rpx;
		font-size: 18rpx;
	}

	.room-hero-summary {
		position: relative;
		z-index: 5;
		margin: -62rpx 24rpx 18rpx;
		padding: 28rpx 24rpx 22rpx;
		background: #ffffff;
		border: 1rpx solid rgba(24, 133, 143, 0.14);
		border-radius: 28rpx;
		box-shadow: 0 14rpx 32rpx rgba(0, 106, 106, 0.12);
	}

	// 门店详情页已并入房型详情，这里展示门店名称与门店级信息。
	.room-store-bar {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		margin-bottom: 18rpx;
		padding: 14rpx 16rpx;
		background: rgba(24, 133, 143, 0.06);
		border: 1rpx solid rgba(24, 133, 143, 0.12);
		border-radius: 16rpx;
	}

	.room-store-title-row {
		display: flex;
		align-items: center;
		gap: 8rpx;
		min-width: 0;
	}

	.room-store-name {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		color: var(--proto-primary-dark);
		font-size: 25rpx;
		font-weight: 700;
		white-space: nowrap;
		text-overflow: ellipsis;
	}

	.room-store-status {
		flex-shrink: 0;
		padding: 2rpx 10rpx;
		border-radius: 999rpx;
		background: rgba(42, 169, 169, 0.14);
		color: var(--proto-primary-dark);
		font-size: 19rpx;
		line-height: 1.6;
	}

	.room-store-meta-row {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 12rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.room-store-link {
		display: inline-flex;
		align-items: center;
		margin-left: auto;
		color: var(--proto-primary-dark);
	}

	.detail-title-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 16rpx;
	}

	.detail-title-copy {
		min-width: 0;
		flex: 1;
	}

	.room-detail-title {
		display: block;
		margin-top: 0;
		font-size: 32rpx;
		line-height: 1.25;
	}

	.room-detail-subtitle {
		margin-top: 9rpx;
		font-size: 20rpx;
		line-height: 1.4;
	}

	.room-detail-tags {
		gap: 10rpx;
		margin-top: 20rpx;
	}

	.room-detail-tags text {
		padding: 8rpx 13rpx;
		color: var(--proto-primary-dark);
		background: #edf8f8;
		border: 1rpx solid rgba(42, 169, 169, 0.16);
		border-radius: 10rpx;
		font-size: 18rpx;
	}

	.detail-highlight-grid {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 12rpx;
		margin-top: 18rpx;
	}

	.detail-highlight {
		min-width: 0;
		padding: 15rpx 14rpx;
		background: #f2f8f9;
		border-radius: 16rpx;
	}

	.detail-highlight .highlight-value {
		display: flex;
		align-items: center;
		min-width: 0;
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 750;
	}

	.detail-highlight .highlight-value .proto-icon {
		margin-right: 7rpx;
		flex: 0 0 auto;
	}

	.review-highlight .highlight-value {
		color: var(--proto-primary-dark);
	}

	.detail-highlight > text:last-child {
		display: block;
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.location-highlight .highlight-value text,
	.location-highlight > text:last-child {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}


	.itinerary-card,
	.room-choice-card,
	.other-room-card,
	.feature-card,
	.facilities-card,
	.policy-card,
	.location-card {
		margin-top: 14rpx;
		margin-bottom: 14rpx;
		border-color: rgba(24, 133, 143, 0.12);
		box-shadow: 0 8rpx 22rpx rgba(0, 106, 106, 0.06);
	}

	.section-heading {
		font-size: 25rpx;
	}

	.section-heading .proto-link,
	.section-heading .proto-caption {
		color: var(--proto-primary-dark);
		font-size: 18rpx;
	}

	.stay-strip {
		gap: 10rpx;
		margin-top: 16rpx;
		padding: 16rpx;
		background: #edf8f8;
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 18rpx;
	}

	.stay-side .strong {
		margin-top: 4rpx;
		font-size: 28rpx;
		font-weight: 800;
	}

	.stay-side .small {
		margin-top: 2rpx;
	}

	.stay-night {
		padding: 10rpx 13rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 20rpx;
		font-weight: 700;
	}

	.stay-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12rpx;
		margin-top: 14rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.stay-summary > text:first-child {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}


	.room-choice-body {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 16rpx;
	}

	.room-choice-body > image {
		width: 170rpx;
		height: 150rpx;
		flex: 0 0 170rpx;
		border-radius: 16rpx;
	}

	.room-choice-copy {
		min-width: 0;
		flex: 1;
	}

	.room-choice-title,
	.other-room-title {
		display: block;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 800;
		line-height: 1.35;
	}

	.room-choice-spec,
	.other-room-spec {
		display: block;
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.35;
	}

	.room-choice-tags,
	.other-room-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 6rpx;
		margin-top: 9rpx;
	}

	.room-choice-tags text,
	.other-room-tags text {
		padding: 5rpx 8rpx;
		color: var(--proto-primary-dark);
		background: #edf8f8;
		border-radius: 6rpx;
		font-size: 16rpx;
	}

	.room-choice-price {
		align-self: flex-start;
		flex: 0 0 auto;
		color: var(--proto-primary-dark);
		font-size: 16rpx;
		text-align: right;
		white-space: nowrap;
	}

	.room-choice-price text:first-child,
	.other-room-price text:first-child {
		display: block;
		font-size: 29rpx;
		font-weight: 850;
	}

	.room-choice-footer {
		display: flex;
		align-items: center;
		gap: 6rpx;
		margin-top: 16rpx;
		padding-top: 14rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.36);
	}

	.room-choice-footer > text {
		min-width: 0;
		color: var(--proto-primary-dark);
		font-size: 17rpx;
	}

	.room-choice-footer .proto-button-small {
		height: 58rpx;
		padding: 0 18rpx;
		font-size: 19rpx;
	}

	.other-room-item {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 14rpx;
		padding: 12rpx;
		background: #f5f9fa;
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 16rpx;
	}

	.other-room-item > image {
		width: 142rpx;
		height: 132rpx;
		flex: 0 0 142rpx;
		border-radius: 12rpx;
	}

	.other-room-copy {
		min-width: 0;
		flex: 1;
	}

	.other-room-title {
		font-size: 21rpx;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.other-room-price {
		align-self: stretch;
		display: flex;
		align-items: flex-end;
		flex-direction: column;
		justify-content: space-between;
		flex: 0 0 auto;
		color: var(--proto-primary-dark);
		font-size: 16rpx;
		text-align: right;
		white-space: nowrap;
	}

	.other-room-button {
		min-width: 74rpx;
		height: 46rpx;
		padding: 0 12rpx;
		color: var(--proto-primary-dark);
		background: #dff6f5;
		border-radius: 23rpx;
		font-size: 17rpx;
	}


	.feature-copy {
		display: block;
		margin-top: 16rpx;
		color: var(--proto-text);
		font-size: 22rpx;
		line-height: 1.6;
	}

	.facility-group {
		display: flex;
		align-items: flex-start;
		gap: 18rpx;
		margin-top: 18rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.38);
	}

	.facility-group:first-of-type {
		padding-top: 0;
		border-top: 0;
	}

	.facility-group-title {
		width: 80rpx;
		flex: 0 0 80rpx;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.facility-items {
		display: flex;
		flex: 1;
		flex-wrap: wrap;
		gap: 12rpx 18rpx;
	}

	.facility-items > view {
		display: flex;
		align-items: center;
		color: var(--proto-muted);
		font-size: 22rpx;
	}

	.facility-items .proto-icon {
		margin-right: 7rpx;
	}

	.policy-card .policy-item {
		margin-top: 12rpx;
		padding: 15rpx 16rpx;
		background: #f3f8f9;
		border-radius: 14rpx;
	}

	.policy-title-row {
		font-size: 24rpx;
	}

	.policy-body {
		gap: 10rpx;
		margin-top: 12rpx;
		padding-top: 12rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.32);
		font-size: 22rpx;
	}

	.location-heading-action {
		display: flex;
		align-items: center;
	}

	.location-map {
		position: relative;
		margin-top: 14rpx;
		overflow: hidden;
		background: #e8f2f4;
		border-radius: 16rpx;
	}

	.location-map-canvas {
		width: 100%;
		height: 300rpx;
	}

	.location-map-hint {
		position: absolute;
		right: 10rpx;
		bottom: 10rpx;
		display: flex;
		align-items: center;
		padding: 5rpx 10rpx;
		color: #2a5f63;
		font-size: 17rpx;
		background: rgba(255, 255, 255, 0.88);
		border-radius: 999rpx;
	}

	.location-map-hint text {
		margin-left: 4rpx;
	}

	.location-map-error {
		position: absolute;
		top: 8rpx;
		right: 8rpx;
		left: 8rpx;
		display: flex;
		align-items: center;
		padding: 6rpx 10rpx;
		color: #8a4b12;
		font-size: 17rpx;
		background: rgba(255, 241, 224, 0.94);
		border-radius: 10rpx;
	}

	.location-map-error text {
		margin-left: 6rpx;
	}

	.location-map-tools {
		display: flex;
		margin-top: 10rpx;
	}

	.location-map-tool {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 1;
		height: 60rpx;
		padding: 0;
		color: var(--proto-primary-dark);
		background: #eef7f8;
		border-radius: 12rpx;
		font-size: 19rpx;
		font-weight: 700;
		line-height: 1;
	}

	.location-map-tool + .location-map-tool {
		margin-left: 10rpx;
	}

	.location-map-tool .proto-icon {
		margin-right: 6rpx;
	}

	.nearby-scroll {
		height: 86rpx;
		margin-top: 10rpx;
		white-space: nowrap;
	}

	.nearby-chip {
		display: inline-flex;
		flex-direction: column;
		justify-content: center;
		height: 76rpx;
		max-width: 300rpx;
		margin-right: 10rpx;
		padding: 0 14rpx;
		vertical-align: middle;
		background: #f4f9fa;
		border: 1rpx solid rgba(42, 169, 169, 0.14);
		border-radius: 12rpx;
	}

	.nearby-chip-active {
		background: #e2f3f4;
		border-color: rgba(42, 169, 169, 0.45);
	}

	.nearby-chip-name {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 19rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.nearby-chip-meta {
		margin-top: 3rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 16rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.nearby-tip {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.location-address {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 10rpx;
		padding: 9rpx 12rpx;
		background: #f6fafa;
		border-radius: 12rpx;
	}

	.location-address-main {
		display: flex;
		align-items: center;
		flex: 1;
		min-width: 0;
	}

	.location-address-main .proto-icon {
		margin-right: 6rpx;
	}

	.location-title {
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 750;
		line-height: 1.35;
	}

	.location-meta-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		margin-top: 8rpx;
	}

	.location-note {
		flex: 1;
		margin-right: 10rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.location-copy {
		flex: 0 0 auto;
		color: var(--proto-primary-dark);
		font-size: 18rpx;
		font-weight: 700;
	}

	.room-bottom-actions {
		gap: 8rpx;
		padding-right: 16rpx;
		padding-left: 16rpx;
	}

	.room-bottom-actions .detail-bottom-link {
		width: 88rpx;
		flex: 0 0 88rpx;
		flex-direction: column;
		gap: 6rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
	}

	.room-bottom-actions .detail-bottom-link .proto-icon {
		margin-right: 0;
	}


	.room-bottom-actions .proto-primary-button {
		min-width: 168rpx;
		height: 74rpx;
		flex: 1;
		font-size: 22rpx;
	}

	/* Final detail-page controls: keep labels visible across mp-weixin button resets. */
	.room-detail-page button {
		box-sizing: border-box;
		line-height: 1.2;
		white-space: nowrap;
		opacity: 1;
	}


	.room-detail-page .room-choice-footer .proto-button-small {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 184rpx;
		height: 60rpx;
		margin: 0;
		padding: 0 20rpx;
		color: #ffffff;
		background: var(--proto-primary-dark);
		border: 0;
		border-radius: 30rpx;
		font-size: 19rpx;
		font-weight: 750;
		line-height: 60rpx;
		text-align: center;
	}

	.room-detail-page .other-room-button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 82rpx;
		height: 50rpx;
		margin: 0;
		padding: 0 16rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.2);
		border-radius: 25rpx;
		font-size: 18rpx;
		font-weight: 700;
		line-height: 50rpx;
		text-align: center;
	}

	.room-detail-page .room-bottom-actions {
		min-height: 112rpx;
		padding-top: 12rpx;
		padding-bottom: calc(12rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(12rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(24, 133, 143, 0.12);
		box-shadow: 0 -12rpx 30rpx rgba(0, 106, 106, 0.1);
	}

	.room-detail-page .room-bottom-actions .detail-bottom-link {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 88rpx;
		min-width: 88rpx;
		height: 84rpx;
		margin: 0;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		border: 0;
		font-size: 21rpx;
		font-weight: 650;
		line-height: 1.15;
		text-align: center;
		white-space: normal;
	}

	.room-detail-page .room-bottom-actions .detail-bottom-link .proto-icon {
		margin: 0;
	}


	.room-detail-page .room-bottom-actions .proto-primary-button {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 196rpx;
		height: 76rpx;
		flex: 1;
		margin: 0;
		padding: 0 22rpx;
		color: #ffffff;
		background: var(--proto-primary-dark);
		border: 0;
		border-radius: 38rpx;
		box-shadow: 0 10rpx 22rpx rgba(0, 106, 106, 0.22);
		font-size: 23rpx;
		font-weight: 800;
		line-height: 76rpx;
		text-align: center;
	}

	.room-detail-page .stay-night {
		line-height: 1.25;
		white-space: nowrap;
	}

	.room-detail-page .proto-link {
		color: var(--proto-primary-dark);
		line-height: 1.3;
	}

	.room-detail-page .proto-card {
		background: #ffffff;
		border-color: rgba(24, 133, 143, 0.14);
		box-shadow: 0 8rpx 22rpx rgba(0, 106, 106, 0.06);
	}

	.room-detail-page .section-heading > text:first-child,
	.room-detail-page .section-heading .inline-icon-text {
		color: var(--proto-text);
		font-weight: 800;
		line-height: 1.3;
	}

	.room-detail-page .room-detail-title,
	.room-detail-page .room-choice-title,
	.room-detail-page .other-room-title {
		color: var(--proto-text);
		line-height: 1.3;
	}

	.room-detail-page .room-detail-title {
		font-size: 34rpx;
		font-weight: 850;
	}

	.room-detail-page .room-detail-subtitle,
	.room-detail-page .room-choice-spec,
	.room-detail-page .other-room-spec,
	.room-detail-page .location-note {
		color: var(--proto-muted);
		line-height: 1.45;
	}

	.room-detail-page .detail-highlight,
	.room-detail-page .stay-strip,
	.room-detail-page .room-choice-footer {
		background: #f1f8f9;
		border-color: rgba(42, 169, 169, 0.12);
	}

	.room-detail-page .room-choice-footer {
		min-height: 74rpx;
	}

	.room-detail-page .facility-items > view,
	.room-detail-page .policy-body,
	.room-detail-page .location-note {
		line-height: 1.45;
	}

	/* Match the reference booking flow: solid green chrome, compact dates and a readable CTA. */
	.room-detail-page .room-detail-hero :deep(.proto-header),
	.room-detail-page .room-detail-hero .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.16) !important;
	}

	.room-detail-page .room-detail-hero :deep(.proto-header-title),
	.room-detail-page .room-detail-hero :deep(.proto-header-icon-button),
	.room-detail-page .room-detail-hero :deep(.proto-header-user-button),
	.room-detail-page .room-detail-hero .proto-header-title,
	.room-detail-page .room-detail-hero .proto-header-icon-button,
	.room-detail-page .room-detail-hero .proto-header-user-button {
		color: #ffffff !important;
	}

	.room-detail-page .room-detail-hero :deep(.proto-header-user-button),
	.room-detail-page .room-detail-hero .proto-header-user-button {
		background: #006a6a !important;
	}

	.room-detail-page .itinerary-card {
		padding-top: 22rpx;
		padding-bottom: 20rpx;
	}

	.room-detail-page .itinerary-heading {
		margin-bottom: 14rpx;
	}

	.room-detail-page .itinerary-heading .proto-link {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		gap: 4rpx;
		margin-left: 12rpx;
		white-space: nowrap;
	}

	.room-detail-page .itinerary-heading .proto-link text {
		white-space: nowrap;
	}

	.room-detail-page .stay-strip {
		align-items: center;
		margin-top: 0;
		padding: 18rpx 14rpx;
		background: #eef8fa;
		border: 1rpx solid rgba(42, 169, 169, 0.14);
	}

	.room-detail-page .date-title {
		display: flex;
		align-items: baseline;
		gap: 6rpx;
	}

	.room-detail-page .stay-side .strong {
		margin-top: 0;
		font-size: 31rpx;
		font-weight: 850;
	}

	.room-detail-page .date-context {
		color: var(--proto-muted);
		font-size: 20rpx;
		white-space: nowrap;
	}

	.room-detail-page .stay-side .small {
		margin-top: 2rpx;
		font-size: 17rpx;
	}

	.room-detail-page .stay-night {
		min-width: 96rpx;
		padding: 10rpx 13rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border: 1rpx solid rgba(24, 133, 143, 0.18);
		border-radius: 24rpx;
		font-size: 18rpx;
	}

	.room-detail-page .stay-night .proto-icon {
		display: none;
	}

	.room-detail-page .stay-summary {
		margin-top: 12rpx;
		padding-top: 12rpx;
		border-top: 1rpx solid rgba(24, 133, 143, 0.12);
	}

	.room-detail-page .stay-summary > text:first-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.room-detail-page .room-bottom-actions {
		gap: 8rpx;
	}

	.room-detail-page .room-bottom-actions .proto-primary-button,
	.room-detail-page .room-bottom-actions .proto-primary-button > text {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 0;
		height: 76rpx;
		margin: 0;
		padding: 0 20rpx;
		color: #ffffff !important;
		background: #2aa9a9 !important;
		border: 0 !important;
		border-radius: 38rpx;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 76rpx;
		text-align: center;
		text-shadow: none;
		white-space: nowrap;
	}

	.room-detail-page .room-book-button,
	.room-detail-page .other-room-button,
	.room-detail-page .room-book-button > text,
	.room-detail-page .other-room-button > text {
		color: #ffffff !important;
		background: #2aa9a9 !important;
		border-color: #2aa9a9 !important;
		font-weight: 800;
		text-align: center;
	}

	.room-detail-page .room-choice-price {
		display: flex;
		align-items: flex-end;
		flex-direction: column;
		gap: 4rpx;
	}

	.room-detail-page .room-choice-price .room-book-button {
		min-width: 76rpx;
		height: 56rpx;
		margin-top: 8rpx;
		padding: 0 22rpx;
		border-radius: 28rpx;
		font-size: 21rpx;
		line-height: 56rpx;
	}

	/* Keep the first viewport focused on the room image. The full title bar appears after scrolling. */
	.room-detail-page .hero-back-button {
		position: absolute;
		top: 76rpx;
		left: 24rpx;
		z-index: 6;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 64rpx;
		height: 64rpx;
		margin: 0;
		padding: 0;
		color: #ffffff;
		background: rgba(0, 55, 67, 0.42);
		border: 1rpx solid rgba(255, 255, 255, 0.24);
		border-radius: 50%;
	}

	.room-detail-page .room-scroll-header {
		position: fixed;
		top: 0;
		right: 0;
		left: 0;
		z-index: 60;
		pointer-events: none;
	}

	.room-detail-page .room-scroll-header .proto-header {
		position: relative !important;
		top: auto !important;
		right: auto !important;
		left: auto !important;
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18) !important;
		box-shadow: 0 6rpx 18rpx rgba(0, 106, 106, 0.16);
		pointer-events: auto;
	}

	.room-detail-page .room-scroll-header .proto-header-title,
	.room-detail-page .room-scroll-header .proto-header-icon-button,
	.room-detail-page .room-scroll-header .proto-header-user-button {
		color: #ffffff !important;
	}

	.room-detail-page .room-scroll-header .proto-header-trailing {
		display: none !important;
	}
</style>
