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
			<view class="detail-title-row">
				<view class="detail-title-copy">
					<text class="room-detail-title">{{ room.name }}</text>
					<text class="room-detail-subtitle">静谧采光 · 独立卫浴 · 入住更安心</text>
				</view>
				<text class="room-status-badge">{{ room.availability || '今日可预订' }}</text>
			</view>
			<view class="room-detail-tags">
				<text v-for="tag in detailTags" :key="tag">{{ tag }}</text>
			</view>
			<view class="detail-highlight-grid">
				<view class="detail-highlight review-highlight" @tap="goReviews">
					<view class="highlight-value"><ProtoIcon name="star" :size="25" /><text>{{ roomRatingLabel }} 超棒</text></view>
					<text>{{ reviewSummary.reviewCount }}条真实评价</text>
				</view>
				<view class="detail-highlight location-highlight" @tap="copyAddress">
					<view class="highlight-value"><ProtoIcon name="location" :size="25" /><text>{{ store.shortName || store.name || '民宿位置' }}</text></view>
					<text>{{ store.address || '郑州市中心 · 交通便利' }}</text>
				</view>
			</view>
		</view>

		<view class="proto-card itinerary-card" @tap="goDate">
			<view class="section-heading itinerary-heading">
				<view class="inline-icon-text"><ProtoIcon name="calendar" :size="22" /><text>入住日期</text></view>
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
			<view class="room-choice-footer"><text><ProtoIcon name="check-circle" :size="20" /> {{ room.availability || '今日可预订' }}</text></view>
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
			<text class="feature-copy">静谧采光，干净整洁，适合短住与周末放松。</text>
		</view>

		<view class="proto-card facilities-card">
			<view class="section-heading"><text>服务与设施</text><text class="proto-caption">已为你准备</text></view>
			<view v-for="group in facilityGroups" :key="group.title" class="facility-group"><text class="facility-group-title">{{ group.title }}</text><view class="facility-items"><view v-for="item in group.items" :key="item"><ProtoIcon name="check-circle" :size="21" /><text>{{ item }}</text></view></view></view>
		</view>

		<view class="proto-card policy-card">
			<view class="section-heading"><text>预订须知</text><text class="proto-caption">请提前了解入住规则</text></view>
			<view v-for="item in policies" :key="item.title" class="policy-item">
				<view class="policy-title-row" @tap="togglePolicy(item.key)"><view class="inline-icon-text"><ProtoIcon :name="item.icon" :size="22" /><text>{{ item.title }}</text></view><ProtoIcon :name="openPolicies.indexOf(item.key) >= 0 ? 'chevron-up' : 'chevron-down'" :size="22" /></view>
				<view v-if="openPolicies.indexOf(item.key) >= 0" class="policy-body"><text v-for="line in item.lines" :key="line">{{ line }}</text></view>
			</view>
		</view>

		<view class="proto-card guest-card"><view class="section-heading"><text>入住人</text><text class="proto-link">添加入住人 <ProtoIcon name="chevron-right" :size="20" /></text></view><view class="guest-placeholder"><ProtoIcon name="user" :size="32" /><text>入住时填写姓名，方便快速办理入住</text></view></view>

		<view class="proto-card location-card"><view class="section-heading"><text>周边位置</text><text class="proto-link" @tap="copyAddress">复制地址</text></view><view class="detail-map-placeholder"><ProtoIcon name="map" :size="42" /><text>{{ store.shortName || store.name || '民宿周边' }}</text></view><text class="location-title">{{ store.address || '郑州市中心，出行便利' }}</text><text class="location-note">步行可达餐饮、商圈与公共交通</text></view>

		<view class="proto-bottom-actions room-bottom-actions">
			<button class="detail-bottom-link" hover-class="none" @tap="goService"><ProtoIcon name="support" :size="22" /><text>客服</text></button>
			<button class="detail-bottom-link" hover-class="none" @tap="callStore"><ProtoIcon name="phone" :size="22" /><text>前台</text></button>
			<button class="detail-bottom-link" hover-class="none" @tap="toggleFavorite"><ProtoIcon :name="favorite ? 'heart-filled' : 'heart'" :size="22" /><text>{{ favorite ? '已收藏' : '收藏' }}</text></button>
			<button class="proto-primary-button" hover-class="none" @tap="goBooking"><text>立即预订</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoRooms, demoStores, goPage, showToast } from '@/common/prototype.js'
	import { getBooking, getFavorites, getRoomReviewSummary, recordRecent, saveBooking, toggleFavorite } from '@/common/app-store.js'
	import { fetchRoomReviews, fetchStoreDetail } from '@/common/api.js'

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
				facilityGroups: [
					{ title: '服务', items: ['围炉煮茶', '前台接待', '行李寄存', '免费停车'] },
					{ title: '基础', items: ['零食、饮品', '网络电视', '空调', '窗户', '无线网络'] },
					{ title: '卫浴', items: ['电吹风', '独立卫浴', '热水'] },
					{ title: '周边', items: ['医院', '商场', '超市'] }
				],
				amenities: [
					{ icon: 'house', label: '卫浴洗漱' },
					{ icon: 'status', label: '24小时恒温热水' },
					{ icon: 'support', label: '独立干湿分离淋浴' },
					{ icon: 'tune', label: '负离子大功率电吹风' },
					{ icon: 'star', label: '植物精油洗沐套装' },
					{ icon: 'settings', label: '智能电器' },
					{ icon: 'wifi', label: '千兆专属高速 Wi-Fi' },
					{ icon: 'status', label: '全静音冷暖变频空调' },
					{ icon: 'lock', label: '智能免卡密码门锁' },
					{ icon: 'key', label: '304不锈钢静音电水壶' },
					{ icon: 'image', label: '双层高密度遮光窗帘' },
					{ icon: 'book', label: '休闲实木工作书桌椅' }
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
				return demoRooms.filter((item) => String(item.id) !== String(this.room && this.room.id)).slice(0, 2)
			},
			checkInContext() {
				return getDateContext(this.booking.isoCheckIn, '入住')
			},
			checkOutContext() {
				return getDateContext(this.booking.isoCheckOut, '离店')
			},
			roomRatingLabel() {
				return Number(this.reviewSummary.roomAverageRating || 0).toFixed(1)
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
			recordRecent({
				id: this.room.id,
				type: 'room',
				title: this.room.name,
				subtitle: `¥${this.room.price}/晚 · ${this.room.capacity}`,
				image: this.room.image,
				path: `/pages/room/detail?id=${this.room.id}&storeId=${this.storeId || ''}`
			})
		},
		onShow() {
			this.booking = getBooking()
			this.favorite = getFavorites().map(String).includes(`room-${this.room.id}`)
			this.reviewSummary = getRoomReviewSummary(this.room.id, this.storeId)
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
					roomTags: room.tags || []
				})
				return this.booking
			},
			goDate() {
				goPage(`/pages/booking/date?from=room-detail&id=${encodeURIComponent(this.room.id)}&storeId=${encodeURIComponent(this.storeId || this.room.storeId || '')}`)
			},
			goBooking() {
				this.prepareBooking()
				goPage('/pages/booking/confirm')
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
			toggleFavorite() {
				this.favorite = toggleFavorite(`room-${this.room.id}`, {
					type: 'room',
					title: this.room.name,
					subtitle: `¥${this.room.price}/晚 · ${this.room.capacity}`,
					image: this.room.image,
					path: `/pages/room/detail?id=${this.room.id}&storeId=${this.storeId || this.room.storeId || ''}`
				})
				showToast(this.favorite ? '已收藏房型' : '已取消收藏')
			},
			shareRoom() {
				uni.setClipboardData({
					data: `${this.room.name}，¥${this.room.price}/晚`,
					success: () => showToast('房型信息已复制')
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
				goPage('/pages/booking/confirm')
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

	.hero-floating-actions {
		position: absolute;
		top: 132rpx;
		right: 24rpx;
		left: 24rpx;
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.hero-floating-actions button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 68rpx;
		height: 68rpx;
		margin-right: 10rpx;
		padding: 0;
		color: var(--proto-text);
		background: rgba(255, 255, 255, 0.92);
		border-radius: 50%;
		font-size: 36rpx;
	}

	.hero-floating-actions > view {
		display: flex;
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

	.room-label-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.room-label-row .proto-pill {
		font-size: 18rpx;
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

	.detail-price-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 20rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.detail-price {
		color: var(--proto-primary-dark);
		font-size: 42rpx;
		font-weight: 850;
	}

	.detail-price-unit {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.original-price {
		margin-left: 14rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		text-decoration: line-through;
	}

	.discount-tag {
		display: flex;
		align-items: center;
		padding: 8rpx 14rpx;
		color: var(--proto-primary-dark);
		background: #baf2f4;
		border-radius: 18rpx;
		font-size: 19rpx;
	}

	.discount-tag .proto-icon {
		margin-right: 5rpx;
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

	.price-trend {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 16rpx;
	}

	.price-trend > view {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: calc(20% - 6.4rpx);
		padding: 12rpx 5rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 10rpx;
	}

	.price-trend > view.today {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.price-trend > view.hot {
		color: var(--proto-error);
	}

	.price-trend .small {
		font-size: 17rpx;
	}

	.price-trend .strong {
		margin-top: 6rpx;
		font-size: 21rpx;
	}

	.price-trend .em {
		margin-top: 4rpx;
		font-size: 16rpx;
		font-style: normal;
	}

	.amenity-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 18rpx;
	}

	.amenity-grid > view {
		display: flex;
		align-items: center;
		width: calc(50% - 6rpx);
		padding: 13rpx 12rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 10rpx;
		font-size: 18rpx;
	}

	.amenity-grid > view .proto-icon {
		flex: 0 0 auto;
		margin-right: 7rpx;
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
		font-size: 21rpx;
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
		font-size: 19rpx;
		line-height: 1.5;
	}

	.review-heading {
		display: flex;
		align-items: baseline;
		gap: 8rpx;
	}

	.review-score {
		font-size: 30rpx;
		font-weight: 800;
	}

	.review-stars {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark);
	}

	.review-label {
		flex: 1;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.review-heading .proto-link {
		display: flex;
		align-items: center;
		font-size: 17rpx;
	}

	.review-heading .proto-link .proto-icon {
		margin-left: 3rpx;
	}

	.review-quote {
		display: block;
		margin-top: 16rpx;
		padding: 18rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 19rpx;
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
	.total-block {
		display: flex;
		align-items: baseline;
		flex: 1;
		flex-wrap: wrap;
	}

	.total-block > text {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.total-block .strong {
		margin-left: 4rpx;
		color: var(--proto-primary-dark);
		font-size: 29rpx;
	}

	.total-block .small {
		width: 100%;
		color: var(--proto-muted);
		font-size: 15rpx;
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

	.hero-floating-actions {
		top: 136rpx;
		right: 26rpx;
		left: auto;
		gap: 12rpx;
	}

	.hero-floating-actions button {
		width: 70rpx;
		height: 70rpx;
		margin-right: 0;
		color: var(--proto-primary-dark);
		background: rgba(255, 255, 255, 0.92);
		border: 1rpx solid rgba(255, 255, 255, 0.65);
		box-shadow: 0 8rpx 18rpx rgba(0, 65, 77, 0.14);
	}

	.hero-floating-actions > view {
		gap: 12rpx;
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

	.room-status-badge {
		max-width: 180rpx;
		padding: 8rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 18rpx;
		font-size: 17rpx;
		line-height: 1.3;
		text-align: center;
		white-space: normal;
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

	.detail-price-row {
		margin-top: 18rpx;
		padding: 14rpx 16rpx;
		background: #eaf7f7;
		border: 1rpx solid rgba(42, 169, 169, 0.12);
		border-radius: 16rpx;
	}

	.detail-price {
		font-size: 40rpx;
	}

	.discount-tag {
		padding: 8rpx 12rpx;
		font-size: 17rpx;
	}

	.itinerary-card,
	.room-choice-card,
	.other-room-card,
	.review-card,
	.feature-card,
	.facilities-card,
	.policy-card,
	.guest-card,
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

	.stay-summary-price {
		color: var(--proto-primary-dark);
		font-weight: 750;
		white-space: nowrap;
	}

	.price-trend-toggle {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		margin-top: 14rpx;
		color: var(--proto-primary-dark);
		font-size: 18rpx;
	}

	.price-trend-toggle .proto-icon {
		margin-left: 4rpx;
	}

	.price-trend {
		gap: 8rpx;
		margin-top: 12rpx;
	}

	.price-trend > view {
		padding: 11rpx 4rpx;
		background: #f2f8f9;
		border-radius: 12rpx;
	}

	.price-trend > view.today {
		color: #ffffff;
		background: var(--proto-primary);
	}

	.price-trend > view.today .small,
	.price-trend > view.today .strong,
	.price-trend > view.today .em {
		color: #ffffff;
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
		justify-content: space-between;
		gap: 12rpx;
		margin-top: 16rpx;
		padding-top: 14rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.36);
	}

	.room-choice-footer > text {
		display: flex;
		align-items: center;
		min-width: 0;
		color: var(--proto-primary-dark);
		font-size: 17rpx;
	}

	.room-choice-footer > text .proto-icon {
		margin-right: 4rpx;
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

	.review-item {
		margin-top: 18rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.38);
	}

	.review-item:first-of-type {
		padding-top: 0;
		border-top: 0;
	}

	.review-user-row {
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.review-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 58rpx;
		height: 58rpx;
		color: var(--proto-primary-dark);
		background: #dff6f5;
		border-radius: 50%;
		font-size: 24rpx;
		font-weight: 800;
	}

	.review-user-copy {
		display: flex;
		flex: 1;
		flex-direction: column;
		gap: 3rpx;
		min-width: 0;
	}

	.review-user-copy text:first-child {
		font-size: 21rpx;
		font-weight: 700;
	}

	.review-user-copy text:last-child {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.review-stars {
		gap: 2rpx;
		color: var(--proto-primary);
	}

	.review-quote {
		margin-top: 12rpx;
		padding: 0;
		background: transparent;
		font-size: 20rpx;
		line-height: 1.5;
	}

	.review-images {
		display: flex;
		gap: 10rpx;
		margin-top: 12rpx;
	}

	.review-images image {
		width: 148rpx;
		height: 112rpx;
		border-radius: 12rpx;
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
		width: 74rpx;
		flex: 0 0 74rpx;
		color: var(--proto-text);
		font-size: 21rpx;
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
		font-size: 19rpx;
	}

	.facility-items .proto-icon {
		margin-right: 5rpx;
	}

	.policy-card .policy-item {
		margin-top: 12rpx;
		padding: 15rpx 16rpx;
		background: #f3f8f9;
		border-radius: 14rpx;
	}

	.policy-title-row {
		font-size: 21rpx;
	}

	.policy-body {
		gap: 8rpx;
		margin-top: 12rpx;
		padding-top: 12rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.32);
		font-size: 18rpx;
	}

	.guest-placeholder {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-top: 16rpx;
		padding: 18rpx;
		color: var(--proto-muted);
		background: #f3f8f9;
		border-radius: 14rpx;
		font-size: 19rpx;
	}

	.guest-placeholder .proto-icon {
		color: var(--proto-primary-dark);
	}

	.detail-map-placeholder {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 190rpx;
		margin-top: 16rpx;
		color: var(--proto-primary-dark);
		background: #dff2f6;
		border-radius: 16rpx;
		font-size: 22rpx;
		font-weight: 750;
	}

	.detail-map-placeholder .proto-icon {
		margin-right: 10rpx;
	}

	.location-title {
		display: block;
		margin-top: 14rpx;
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 750;
	}

	.location-note {
		display: block;
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.room-bottom-actions {
		gap: 8rpx;
		padding-right: 16rpx;
		padding-left: 16rpx;
	}

	.room-bottom-actions .detail-bottom-link {
		width: 58rpx;
		flex: 0 0 58rpx;
		flex-direction: column;
		gap: 3rpx;
		color: var(--proto-muted);
		font-size: 15rpx;
	}

	.room-bottom-actions .detail-bottom-link .proto-icon {
		margin-right: 0;
	}

	.total-block {
		width: 112rpx;
		flex: 0 0 112rpx;
	}

	.total-block > text {
		font-size: 15rpx;
	}

	.total-block .strong {
		font-size: 26rpx;
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

	.room-detail-page .hero-floating-actions button {
		color: var(--proto-primary-dark);
		background: rgba(255, 255, 255, 0.94);
		line-height: 1;
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
		width: 62rpx;
		min-width: 62rpx;
		height: 68rpx;
		margin: 0;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		border: 0;
		font-size: 16rpx;
		font-weight: 650;
		line-height: 1.15;
		text-align: center;
		white-space: normal;
	}

	.room-detail-page .room-bottom-actions .detail-bottom-link .proto-icon {
		margin: 0 0 4rpx;
	}

	.room-detail-page .room-bottom-actions .total-block {
		width: 126rpx;
		min-width: 126rpx;
		flex: 0 0 126rpx;
		padding-left: 4rpx;
	}

	.room-detail-page .room-bottom-actions .total-block > text {
		color: var(--proto-muted);
		line-height: 1.25;
	}

	.room-detail-page .room-bottom-actions .total-block .strong {
		color: var(--proto-primary-dark);
		font-size: 28rpx;
		font-weight: 850;
	}

	.room-detail-page .room-bottom-actions .total-block .small {
		font-size: 14rpx;
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

	.room-detail-page .stay-night,
	.room-detail-page .discount-tag,
	.room-detail-page .room-status-badge {
		line-height: 1.25;
		white-space: nowrap;
	}

	.room-detail-page .room-status-badge {
		flex: 0 0 auto;
	}

	.room-detail-page .proto-link,
	.room-detail-page .price-trend-toggle {
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
	.room-detail-page .room-choice-footer,
	.room-detail-page .guest-placeholder {
		background: #f1f8f9;
		border-color: rgba(42, 169, 169, 0.12);
	}

	.room-detail-page .room-choice-footer {
		min-height: 74rpx;
	}

	.room-detail-page .facility-items > view,
	.room-detail-page .review-user-copy text:last-child,
	.room-detail-page .policy-body,
	.room-detail-page .guest-placeholder,
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
