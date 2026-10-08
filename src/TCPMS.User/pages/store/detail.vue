<template>
	<view class="proto-page store-detail-page">
		<view class="detail-hero">
			<image :src="store.image" mode="aspectFill" class="detail-hero-image"></image>
			<view class="detail-hero-shade"></view>
			<button class="hero-back-button" hover-class="none" aria-label="返回" @tap="goBack">
				<ProtoIcon name="back" tone="white" :size="38" />
			</button>
			<text class="hero-count">1 / 12 · 客房与环境</text>
		</view>
		<view v-if="showScrollHeader" class="store-scroll-header">
			<ProtoHeader title="门店详情" title-align="center" theme="green" />
		</view>

		<view class="store-summary proto-card">
			<view class="summary-name-row">
				<view>
					<text class="summary-name">{{ store.name }}</text>
					<view class="summary-badges">
						<text class="proto-pill success">{{ store.status }}</text>
						<text class="proto-pill info">官方直营</text>
						<text class="proto-pill info">品质认证</text>
					</view>
				</view>
				<view class="summary-rating"><ProtoIcon name="star" :size="20" /><text>{{ store.rating }}</text><text class="small">超棒</text></view>
			</view>
			<view class="summary-review"><text>{{ store.reviews || 0 }} 条房客真实评价</text><ProtoIcon name="chevron-right" :size="20" /></view>
			<view class="summary-location">
				<ProtoIcon name="location" :size="24" />
				<view>
					<text>距您 {{ store.distance }}</text>
					<text v-if="store.city">{{ store.city }} · {{ store.address }}</text>
					<text v-else>{{ store.address }}</text>
				</view>
				<button hover-class="none" @tap="goMap">去这里</button>
			</view>
		</view>

		<view class="date-summary proto-card" @tap="goDate">
			<view class="date-summary-heading">
				<view class="inline-icon-text"><ProtoIcon name="calendar" :size="22" /><text>入住日期</text></view>
				<view class="proto-link" @tap.stop="goDate"><text>修改日期</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view class="date-summary-content">
				<view class="date-summary-side">
					<text>入住时间</text>
					<text class="strong">{{ booking.checkIn }}</text>
				</view>
				<text class="date-night">共 {{ booking.nights }} 晚</text>
				<view class="date-summary-side">
					<text>离店时间</text>
					<text class="strong">{{ booking.checkOut }}</text>
				</view>
			</view>
		</view>

		<view class="room-preview-list">
			<view class="room-preview-heading">
				<text>房型选择</text>
				<text class="proto-caption">{{ rooms.length }}间房源</text>
			</view>
			<view v-for="(room, index) in rooms" :key="room.id" class="room-preview-entry">
				<view v-if="index === 0 || index === 1" class="room-group-title">{{ index === 0 ? '当前房型' : '其他房源' }}</view>
				<view class="room-preview-card" @tap="goRoom(room)">
				<view class="room-preview-image-wrap">
					<image :src="room.image" mode="aspectFill"></image>
					<text class="proto-pill primary room-image-label">{{ room.id === 1 ? '即时确认' : room.id === 2 ? '热门推荐' : '青年特惠' }}</text>
				</view>
				<view class="room-preview-body">
					<view class="room-name-row">
						<text class="room-name">{{ room.name }}</text>
						<text class="proto-pill" :class="room.soldOut ? 'danger' : 'info'">{{ room.soldOut ? '已售罄' : '库存充足' }}</text>
					</view>
					<text class="room-spec">{{ room.beds }} · {{ room.area }} · 独立卫浴 · 有窗</text>
					<view class="room-tags">
						<text v-for="tag in (room.tags || []).slice(0, 2)" :key="tag">{{ tag }}</text>
					</view>
					<view class="room-price-row">
						<text class="room-price">¥{{ room.price }}<text class="small">/晚</text></text>
						<button class="room-preview-book-button" :class="{ 'proto-button-disabled': room.soldOut }" hover-class="none" @tap.stop="bookRoom(room)">
							<text>{{ room.soldOut ? '查看' : '订' }}</text>
						</button>
					</view>
				</view>
				</view>
			</view>
		</view>

		<view class="contact-grid proto-card">
			<view class="contact-row">
				<view class="contact-icon"><ProtoIcon name="phone" :size="26" /></view>
				<view>
					<text class="contact-title">门店前台专线</text>
					<text class="contact-sub">{{ store.phone || '暂无公开电话' }} · 点击显示</text>
				</view>
				<button class="proto-button-small proto-button-light" hover-class="none" @tap="callStore">拨打前台</button>
			</view>
			<view class="contact-row">
				<view class="contact-icon"><ProtoIcon name="support" :size="26" /></view>
				<view>
					<text class="contact-title">在线客房管家</text>
					<text class="contact-sub">实时响应退订、寄存与加被需求</text>
				</view>
				<button class="proto-button-small" hover-class="none" @tap="goService">联系管家</button>
			</view>
		</view>

		<view class="proto-card store-feature-card">
			<view class="card-heading"><text>房屋特色</text><text class="proto-caption">入住更安心</text></view>
			<text class="feature-copy">南北通透，干净整洁，公共区域舒适，适合短住与周末放松。</text>
		</view>

		<view class="proto-card facility-card">
			<view class="card-heading">
				<text>服务设施</text>
				<view class="proto-link"><text>全部设施 ({{ facilityCount }})</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view v-for="group in facilityGroups" :key="group.title" class="facility-group">
				<text class="facility-group-title">{{ group.title }}</text>
				<view class="facility-items">
					<view v-for="item in group.items" :key="item"><ProtoIcon name="check-circle" :size="21" /><text>{{ item }}</text></view>
				</view>
			</view>
		</view>

		<view class="proto-card notice-card">
			<view class="card-heading">
				<text>预订须知</text>
				<view class="proto-link" @tap="goRule"><text>查看完整</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view v-for="item in notices" :key="item.title" class="notice-row">
				<ProtoIcon class="notice-icon" :name="item.icon" :size="24" />
				<view>
					<text class="notice-title">{{ item.title }}</text>
					<text v-if="item.desc" class="notice-desc">{{ item.desc }}</text>
				</view>
			</view>
		</view>

		<view class="proto-card guest-card">
			<view class="card-heading"><text>入住人</text><text class="proto-caption">入住时填写</text></view>
			<view class="guest-placeholder"><ProtoIcon name="user" :size="30" /><text>入住时填写姓名，方便快速办理入住</text></view>
		</view>

		<view class="proto-card location-card">
			<view class="card-heading"><text>周边位置</text><view class="proto-link" @tap="copyAddress"><text>复制地址</text></view></view>
			<view class="detail-map-placeholder">
				<view class="map-mini-water"></view>
				<view class="map-mini-road map-mini-road-one"></view>
				<view class="map-mini-road map-mini-road-two"></view>
				<view class="map-mini-road map-mini-road-three"></view>
				<view class="map-mini-marker"><ProtoIcon name="house" :size="30" /></view>
				<text class="map-mini-label">{{ store.city || '郑州市' }} · 出行便利</text>
			</view>
			<view class="location-title-row"><text class="location-title">{{ store.address }}</text><text class="proto-link" @tap="goMap">地图</text></view>
			<text class="location-note">步行可达餐饮、商圈与公共交通</text>
		</view>

		<view class="proto-bottom-actions">
			<button class="detail-bottom-link" hover-class="none" @tap="goHome"><ProtoIcon name="home" :size="22" /><text>首页</text></button>
			<button class="detail-bottom-link" hover-class="none" @tap="toggleFavorite"><ProtoIcon :name="favorite ? 'heart-filled' : 'heart'" :size="22" /><text>{{ favorite ? '已藏' : '收藏' }}</text></button>
			<button class="detail-bottom-link" hover-class="none" @tap="shareStore"><ProtoIcon name="share" :size="22" /><text>分享</text></button>
			<button class="proto-primary-button store-primary-cta" hover-class="none" @tap="goRoomList"><text class="store-primary-cta-label">立即预订</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoRooms, demoStores, goPage, showToast } from '@/common/prototype.js'
	import { getBooking, getFavorites, recordRecent, saveBooking, toggleFavorite } from '@/common/app-store.js'
	import { fetchStoreDetail } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				store: demoStores[0],
				rooms: demoRooms,
				favorite: false,
				booking: getBooking(),
				showScrollHeader: false,
				facilityGroups: [
					{ title: '服务', items: ['围炉煮茶', '前台接待', '行李寄存', '免费停车'] },
					{ title: '基础', items: ['零食、饮品', '网络电视', '空调', '窗户', '无线网络'] },
					{ title: '卫浴', items: ['电吹风', '独立卫浴', '热水'] },
					{ title: '周边', items: ['医院', '商场', '超市'] }
				],
				notices: [
					{ icon: 'clock', title: '入离时间：当日 14:00 后办理入住；次日 12:00 前退房。', desc: '' },
					{ icon: 'heart', title: '宠物接待：允许携带小型宠物（须提前报备保洁押金 ¥100）。', desc: '' },
					{ icon: 'document', title: '发票提供：支持开具增值税电子普通发票/专票。', desc: '' },
					{ icon: 'check-circle', title: '退订规则：入住日前1天中午12:00前可免费全额取消。', desc: '' }
				]
			}
		},
		computed: {
			facilityCount() {
				return this.facilityGroups.reduce((count, group) => count + group.items.length, 0)
			}
		},
		async onLoad(options) {
			if (options && options.id) {
				this.store = demoStores.find((item) => String(item.id) === String(options.id)) || demoStores[0]
			}
			this.favorite = getFavorites().map(String).includes(`store-${this.store.id}`)
			if (!options || !options.id || !/^\d+$/.test(String(options.id))) {
				try {
					const result = await fetchStoreDetail(options && options.id ? options.id : this.store.id)
					if (result.store) this.store = result.store
					if (result.rooms.length) this.rooms = result.rooms
					this.favorite = getFavorites().map(String).includes(`store-${this.store.id}`)
				} catch (error) {
					// Keep the bundled detail page available when the API is offline.
				}
			}
			recordRecent({
				id: this.store.id,
				type: 'store',
				title: this.store.name,
				subtitle: this.store.address,
				image: this.store.image,
				path: `/pages/store/detail?id=${this.store.id}`
			})
		},
		onShow() {
			this.booking = getBooking()
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
			goRoom(room) {
				const roomId = room ? room.id : 1
				goPage(`/pages/room/detail?id=${roomId}&storeId=${this.store.id}`)
			},
			goDate() {
				goPage(`/pages/booking/date?from=store-detail&storeId=${encodeURIComponent(this.store.id)}`)
			},
			prepareBooking(room) {
				const target = room || this.rooms[0] || {}
				this.booking = saveBooking({
					...getBooking(),
					storeId: this.store.id,
					store: this.store.name,
					address: this.store.address,
					roomId: target.id,
					room: target.name,
					roomImage: target.image,
					roomPrice: Number(target.price || 0),
					roomArea: target.area || '',
					roomTags: target.tags || []
				})
				return this.booking
			},
			bookRoom(room) {
				if (!room || room.soldOut) {
					this.goRoom(room)
					return
				}
				this.prepareBooking(room)
				goPage('/pages/booking/confirm')
			},
			goRoomList() {
				saveBooking({
					...getBooking(),
					storeId: this.store.id,
					store: this.store.name,
					address: this.store.address
				})
				goPage(`/pages/room/list?storeId=${this.store.id}`)
			},
			goHome() {
				uni.reLaunch({ url: '/pages/index/index' })
			},
			goMap() {
				goPage(`/pages/store/map?id=${this.store.id}`)
			},
			goService() {
				goPage('/pages/service/contact')
			},
			goRule() {
				goPage('/pages/rule/index')
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.store.phone || '037188886622',
					fail: () => showToast('暂无法拨打前台电话')
				})
			},
			toggleFavorite() {
				this.favorite = toggleFavorite(`store-${this.store.id}`, {
					type: 'store',
					title: this.store.name,
					subtitle: this.store.address,
					image: this.store.image,
					path: `/pages/store/detail?id=${this.store.id}`
				})
				showToast(this.favorite ? '已收藏门店' : '已取消收藏')
			},
			shareStore() {
				uni.setClipboardData({
					data: `${this.store.name}，${this.store.address}`,
					success: () => showToast('门店信息已复制')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.store-detail-page {
		padding-bottom: 150rpx;
		padding-bottom: calc(150rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(150rpx + env(safe-area-inset-bottom));
	}

	.detail-hero {
		position: relative;
		display: block;
		width: 100%;
		height: 430rpx;
		overflow: hidden;
	}

	.detail-hero > image.detail-hero-image {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		display: block;
		width: 100%;
		height: 100%;
		max-width: 100%;
		object-fit: cover;
	}

	.detail-hero-shade {
		position: absolute;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 2;
		height: 40%;
		background: linear-gradient(180deg, transparent, rgba(32, 37, 43, 0.45));
	}

	.detail-hero :deep(.proto-header),
	.detail-hero .proto-header {
		position: absolute;
		top: 0;
		right: 0;
		left: 0;
		z-index: 4;
		width: 100%;
		background: transparent;
		border-bottom: 0;
	}

	.hero-count {
		position: absolute;
		right: 24rpx;
		bottom: 20rpx;
		z-index: 5;
		padding: 8rpx 14rpx;
		color: #ffffff;
		background: rgba(32, 37, 43, 0.68);
		border-radius: 14rpx;
		font-size: 19rpx;
	}

	.store-summary {
		position: relative;
		margin-top: -22rpx;
	}

	.summary-name-row {
		display: flex;
		justify-content: space-between;
	}

	.summary-name-row > view {
		flex: 1;
		min-width: 0;
	}

	.summary-name {
		display: block;
		overflow: hidden;
		color: var(--proto-text);
		font-size: 29rpx;
		font-weight: 800;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.summary-badges {
		display: flex;
		gap: 8rpx;
		margin-top: 12rpx;
	}

	.summary-badges .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}

	.summary-rating {
		display: flex;
		align-items: center;
		gap: 4rpx;
		color: var(--proto-primary-dark);
		font-size: 25rpx;
		font-weight: 800;
		white-space: nowrap;
	}

	.summary-rating .small {
		font-size: 17rpx;
	}

	.summary-review {
		display: flex;
		align-items: center;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.summary-review .proto-icon {
		margin-left: 4rpx;
	}

	.summary-location {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 20rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid #e3ebec;
	}

	.summary-location > .proto-icon {
		color: var(--proto-primary-dark);
		flex: 0 0 auto;
	}

	.summary-location > view {
		flex: 1;
	}

	.summary-location > view text {
		display: block;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.5;
	}

	.summary-location button {
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 20rpx;
	}

	.contact-grid {
		padding: 12rpx 22rpx;
	}

	.contact-row {
		display: flex;
		align-items: center;
		gap: 14rpx;
		padding: 18rpx 0;
	}

	.contact-row + .contact-row {
		border-top: 1rpx solid #e3ebec;
	}

	.contact-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 48rpx;
		height: 48rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.contact-row > view {
		flex: 1;
	}

	.contact-title,
	.contact-sub {
		display: block;
	}

	.contact-title {
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 700;
	}

	.contact-sub {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.contact-row .proto-button-small {
		height: 54rpx;
		padding: 0 16rpx;
		font-size: 18rpx;
	}

	.card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.card-heading .proto-link {
		display: flex;
		align-items: center;
		font-size: 18rpx;
	}

	.card-heading .proto-link .proto-icon {
		margin-left: 3rpx;
	}

	.facility-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 18rpx;
	}

	.facility-item {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: calc(25% - 9rpx);
		gap: 8rpx;
		padding: 16rpx 6rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 12rpx;
		font-size: 17rpx;
		text-align: center;
	}

	.facility-item .proto-icon {
		margin-bottom: 2rpx;
	}

	.notice-row {
		display: flex;
		gap: 12rpx;
		margin-top: 16rpx;
	}

	.notice-icon {
		flex: 0 0 auto;
		color: var(--proto-primary-dark);
	}

	.notice-title {
		display: block;
		color: var(--proto-muted);
		font-size: 20rpx;
		line-height: 1.5;
	}

	.date-summary {
		display: flex;
		align-items: center;
		gap: 14rpx;
	}

	.date-summary-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.date-summary-side > text:first-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.date-summary-side .strong {
		margin-top: 4rpx;
		color: var(--proto-text);
		font-size: 28rpx;
	}

	.date-night {
		padding: 8rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		font-size: 18rpx;
	}

	.date-summary button {
		display: flex;
		align-items: center;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 18rpx;
	}

	.date-summary button .proto-icon {
		margin-left: 4rpx;
	}

	.room-preview-card {
		margin: 14rpx 24rpx;
		overflow: hidden;
		background: var(--proto-surface);
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 22rpx;
	}

	.room-preview-image-wrap {
		position: relative;
		height: 300rpx;
	}

	.room-preview-image-wrap image {
		width: 100%;
		height: 100%;
	}

	.room-image-label {
		position: absolute;
		top: 18rpx;
		left: 18rpx;
	}

	.room-preview-body {
		padding: 20rpx;
	}

	.room-name-row {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.room-name {
		flex: 1;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.room-name-row .proto-pill {
		min-height: 32rpx;
		padding: 0 9rpx;
		font-size: 17rpx;
	}

	.room-spec {
		display: block;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.room-tags {
		display: flex;
		gap: 10rpx;
		margin-top: 12rpx;
		overflow: hidden;
		white-space: nowrap;
	}

	.room-tags text {
		padding: 7rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 8rpx;
		font-size: 17rpx;
	}

	.room-price-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 18rpx;
	}

	.room-price {
		color: var(--proto-primary-dark);
		font-size: 34rpx;
		font-weight: 800;
	}

	.room-price .small {
		color: var(--proto-muted);
		font-size: 17rpx;
		font-weight: 400;
	}

	.proto-button-disabled {
		color: var(--proto-muted) !important;
		background: var(--proto-surface-low) !important;
	}

	.store-detail-page .proto-bottom-actions {
		min-height: 112rpx;
		box-sizing: border-box;
		gap: 10rpx;
	}

	.detail-bottom-link {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 82rpx;
		flex-direction: column;
		gap: 4rpx;
		width: 82rpx;
		height: 76rpx;
		padding: 0;
		color: var(--proto-text);
		background: transparent;
		font-size: 18rpx;
	}

	.detail-bottom-link .proto-icon {
		margin-right: 0;
	}

	.store-detail-page .proto-bottom-actions > .proto-primary-button {
		flex: 1;
		min-width: 0;
		height: 76rpx;
		margin: 0;
		padding: 0 18rpx;
		color: #ffffff !important;
		background: var(--proto-primary) !important;
		border-radius: 38rpx;
		font-size: 22rpx;
		white-space: nowrap;
	}

	/* Store detail controls must stay readable after mp-weixin button normalization. */
	.store-detail-page .detail-hero :deep(.proto-header),
	.store-detail-page .detail-hero .proto-header {
		position: absolute !important;
		top: 0 !important;
		right: 0 !important;
		left: 0 !important;
		background: transparent !important;
		border-bottom-color: transparent !important;
		box-shadow: none !important;
	}

	.store-detail-page .detail-hero :deep(.proto-header-title),
	.store-detail-page .detail-hero :deep(.proto-header-icon-button),
	.store-detail-page .detail-hero :deep(.proto-header-user-button),
	.store-detail-page .detail-hero .proto-header-title,
	.store-detail-page .detail-hero .proto-header-icon-button,
	.store-detail-page .detail-hero .proto-header-user-button {
		color: #ffffff !important;
		text-shadow: 0 1rpx 4rpx rgba(0, 45, 51, 0.32);
	}

	.store-detail-page .proto-button-small,
	.store-detail-page .proto-button-small > text,
	.store-detail-page .store-primary-cta,
	.store-detail-page .store-primary-cta > text {
		box-sizing: border-box;
		color: #ffffff !important;
		font-family: inherit;
		font-weight: 750;
		line-height: 1.2;
		text-align: center;
		text-decoration: none;
		text-shadow: none;
		white-space: nowrap;
	}

	.store-detail-page .proto-button-small {
		background: #2aa9a9 !important;
		border: 1rpx solid #2aa9a9 !important;
	}

	.store-detail-page .proto-button-small.proto-button-light {
		color: #006a6a !important;
		background: #dff6f5 !important;
		border-color: #8bcfca !important;
	}

	.store-detail-page .store-primary-cta {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 0;
		height: 76rpx;
		margin: 0;
		padding: 0 20rpx;
		background: #2aa9a9 !important;
		border: 1rpx solid #2aa9a9 !important;
		border-radius: 38rpx;
		box-shadow: 0 10rpx 22rpx rgba(42, 169, 169, 0.22);
		font-size: 23rpx;
	}

	.store-detail-page .store-primary-cta > text {
		display: inline-block;
		color: #ffffff !important;
	}

	.store-detail-page .store-primary-cta .proto-icon {
		margin-left: 6rpx;
	}

	.store-detail-page .detail-bottom-link,
	.store-detail-page .detail-bottom-link > text {
		color: #006a6a !important;
		font-family: inherit;
		line-height: 1.2;
		text-align: center;
		white-space: nowrap;
	}

	/* The store page follows the room-detail reference: hero overlay, white cards, green actions. */
	.store-detail-page {
		--store-green: #2aa9a9;
		--store-green-deep: #006a6a;
		--store-green-tint: #dff6f5;
		background: var(--proto-page);
		color: #20252b;
	}

	.store-detail-page .detail-hero {
		height: 520rpx;
		background: var(--store-green-tint);
	}

	.store-detail-page .detail-hero-shade {
		height: 42%;
		background: linear-gradient(180deg, transparent 0%, rgba(16, 42, 67, 0.06) 45%, rgba(16, 42, 67, 0.56) 100%);
	}

	.store-detail-page .detail-hero :deep(.proto-header-user-button),
	.store-detail-page .detail-hero .proto-header-user-button {
		display: none !important;
	}

	.store-detail-page .store-summary {
		z-index: 6;
		margin: -58rpx 24rpx 18rpx;
		padding: 26rpx 22rpx 22rpx;
		background: #ffffff !important;
		border: 1rpx solid rgba(42, 169, 169, 0.16);
		border-radius: 26rpx;
		box-shadow: 0 14rpx 32rpx rgba(42, 169, 169, 0.1);
	}

	.store-detail-page .summary-name {
		font-size: 31rpx;
		line-height: 1.3;
	}

	.store-detail-page .summary-rating,
	.store-detail-page .summary-location > .proto-icon,
	.store-detail-page .summary-location button,
	.store-detail-page .summary-review .proto-icon {
		color: var(--store-green-deep);
	}

	.store-detail-page .summary-badges .proto-pill.success {
		color: #157447;
		background: #e7f8ef;
	}

	.store-detail-page .summary-badges .proto-pill.info {
		color: var(--store-green-deep);
		background: var(--store-green-tint);
	}

	.store-detail-page .proto-card {
		background: #ffffff;
		border-color: rgba(42, 169, 169, 0.14);
		box-shadow: 0 8rpx 22rpx rgba(42, 169, 169, 0.06);
	}

	.store-detail-page .contact-grid,
	.store-detail-page .date-summary,
	.store-detail-page .facility-card,
	.store-detail-page .notice-card,
	.store-detail-page .store-feature-card,
	.store-detail-page .guest-card,
	.store-detail-page .location-card {
		margin-top: 14rpx;
		margin-bottom: 14rpx;
	}

	.store-detail-page .contact-icon,
	.store-detail-page .date-summary .proto-icon,
	.store-detail-page .notice-icon,
	.store-detail-page .facility-item .proto-icon,
	.store-detail-page .guest-placeholder .proto-icon,
	.store-detail-page .detail-map-placeholder .proto-icon {
		color: var(--store-green);
	}

	.store-detail-page .contact-row + .contact-row,
	.store-detail-page .summary-location,
	.store-detail-page .notice-row + .notice-row {
		border-top-color: rgba(42, 169, 169, 0.14);
	}

	.store-detail-page .card-heading,
	.store-detail-page .section-heading {
		color: #20252b;
		font-weight: 800;
	}

	.store-detail-page .card-heading .proto-link,
	.store-detail-page .proto-link,
	.store-detail-page .date-summary-heading {
		color: var(--store-green-deep);
	}

	.store-detail-page .date-summary {
		display: block;
		padding: 22rpx;
	}

	.store-detail-page .date-summary-heading,
	.store-detail-page .date-summary-content {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.store-detail-page .date-summary-heading {
		font-size: 24rpx;
		font-weight: 750;
	}

	.store-detail-page .date-summary-heading .proto-link {
		display: flex;
		align-items: center;
		font-size: 18rpx;
	}

	.store-detail-page .date-summary-content {
		gap: 14rpx;
		margin-top: 18rpx;
	}

	.store-detail-page .date-summary-side .strong {
		font-size: 30rpx;
		font-weight: 850;
	}

	.store-detail-page .date-night {
		color: var(--store-green-deep);
		background: var(--store-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.16);
	}

	.store-detail-page .facility-grid {
		gap: 12rpx;
	}

	.store-detail-page .facility-item {
		background: var(--store-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.08);
	}

	.store-detail-page .facility-group {
		display: flex;
		align-items: flex-start;
		gap: 18rpx;
		margin-top: 18rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid rgba(42, 169, 169, 0.12);
	}

	.store-detail-page .facility-group:first-of-type {
		padding-top: 0;
		border-top: 0;
	}

	.store-detail-page .facility-group-title {
		width: 72rpx;
		flex: 0 0 72rpx;
		color: #20252b;
		font-size: 21rpx;
		font-weight: 800;
	}

	.store-detail-page .facility-items {
		display: flex;
		flex: 1;
		flex-wrap: wrap;
		gap: 12rpx 18rpx;
	}

	.store-detail-page .facility-items > view {
		display: flex;
		align-items: center;
		color: #5f7080;
		font-size: 19rpx;
		line-height: 1.4;
	}

	.store-detail-page .facility-items .proto-icon {
		flex: 0 0 auto;
		margin-right: 5rpx;
	}

	.store-detail-page .notice-title,
	.store-detail-page .notice-desc {
		color: #4f6579;
		line-height: 1.5;
	}

	.store-detail-page .feature-copy {
		margin-top: 16rpx;
		color: #34495e;
		font-size: 22rpx;
		line-height: 1.65;
	}

	.store-detail-page .guest-placeholder {
		background: var(--store-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.1);
	}

	.store-detail-page .detail-map-placeholder {
		position: relative;
		display: block;
		height: 210rpx;
		overflow: hidden;
		background: var(--proto-surface-low);
		border: 1rpx solid rgba(42, 169, 169, 0.1);
	}

	.store-detail-page .map-mini-water {
		position: absolute;
		top: -40rpx;
		left: -82rpx;
		width: 270rpx;
		height: 280rpx;
		background: #c8efed;
		border-radius: 48% 58% 50% 42%;
		transform: rotate(-12deg);
	}

	.store-detail-page .map-mini-road {
		position: absolute;
		height: 12rpx;
		background: #ffffff;
		border: 3rpx solid #e3d4b7;
		transform-origin: left center;
	}

	.store-detail-page .map-mini-road-one {
		top: 76rpx;
		left: 126rpx;
		width: 530rpx;
		transform: rotate(22deg);
	}

	.store-detail-page .map-mini-road-two {
		top: 150rpx;
		left: 58rpx;
		width: 650rpx;
		transform: rotate(-7deg);
	}

	.store-detail-page .map-mini-road-three {
		top: 10rpx;
		left: 360rpx;
		width: 14rpx;
		height: 250rpx;
		border-width: 0 3rpx;
		transform: rotate(18deg);
	}

	.store-detail-page .map-mini-marker {
		position: absolute;
		top: 64rpx;
		left: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 72rpx;
		height: 72rpx;
		color: var(--store-green);
		background: #ffffff;
		border: 1rpx solid rgba(42, 169, 169, 0.18);
		border-radius: 16rpx;
		box-shadow: 0 8rpx 18rpx rgba(0, 106, 106, 0.16);
		transform: translateX(-50%);
	}

	.store-detail-page .map-mini-label {
		position: absolute;
		right: 22rpx;
		bottom: 18rpx;
		padding: 6rpx 10rpx;
		color: #4b6478;
		background: rgba(255, 255, 255, 0.86);
		border-radius: 8rpx;
		font-size: 19rpx;
		font-weight: 700;
	}

	.store-detail-page .location-title-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16rpx;
		margin-top: 14rpx;
	}

	.store-detail-page .location-title {
		margin-top: 0;
		overflow: hidden;
		font-size: 21rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.store-detail-page .location-note {
		color: #6f8085;
	}

	.store-detail-page .room-preview-list {
		padding-top: 2rpx;
	}

	.store-detail-page .room-preview-heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin: 18rpx 24rpx 8rpx;
		color: #20252b;
		font-size: 26rpx;
		font-weight: 800;
	}

	.store-detail-page .room-preview-heading .proto-caption {
		color: #6f8085;
		font-size: 18rpx;
		font-weight: 400;
	}

	.store-detail-page .room-preview-entry {
		margin: 0;
	}

	.store-detail-page .room-group-title {
		margin: 16rpx 24rpx 8rpx;
		color: #20252b;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 1.35;
	}

	.store-detail-page .room-preview-card {
		margin-top: 14rpx;
		margin-bottom: 14rpx;
		background: #ffffff;
		border-color: rgba(42, 169, 169, 0.14);
		box-shadow: 0 8rpx 22rpx rgba(42, 169, 169, 0.06);
	}

	.store-detail-page .room-preview-card:active {
		opacity: 0.94;
	}

	.store-detail-page .room-image-label {
		color: var(--store-green-deep);
		background: rgba(223, 246, 245, 0.94);
	}

	.store-detail-page .room-tags text {
		color: var(--store-green-deep);
		background: var(--store-green-tint);
	}

	.store-detail-page .room-price {
		color: var(--store-green-deep);
	}

	.store-detail-page .room-preview-book-button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 80rpx;
		height: 56rpx;
		margin: 0;
		padding: 0 22rpx;
		color: #ffffff !important;
		background: var(--store-green) !important;
		border: 0;
		border-radius: 28rpx;
		font-size: 22rpx;
		font-weight: 800;
		line-height: 56rpx;
		text-align: center;
	}

	.store-detail-page .room-preview-book-button > text {
		color: #ffffff !important;
	}

	.store-detail-page .room-preview-book-button.proto-button-disabled {
		color: var(--proto-muted) !important;
		background: var(--proto-surface-low) !important;
	}

	.store-detail-page .room-preview-book-button.proto-button-disabled > text {
		color: var(--proto-muted) !important;
	}

	.store-detail-page .store-primary-cta {
		position: relative;
		z-index: 2;
		background: var(--store-green) !important;
		background-color: #2aa9a9 !important;
		background-image: none !important;
		border-color: var(--store-green) !important;
		box-shadow: 0 10rpx 22rpx rgba(42, 169, 169, 0.22);
	}

	.store-detail-page button.store-primary-cta,
	.store-detail-page button.store-primary-cta:hover,
	.store-detail-page button.store-primary-cta:active {
		display: flex !important;
		align-items: center;
		justify-content: center;
		min-width: 0;
		height: 76rpx;
		margin: 0;
		padding: 0 24rpx;
		color: #ffffff !important;
		background: #2aa9a9 !important;
		background-color: #2aa9a9 !important;
		border: 1rpx solid #2aa9a9 !important;
		border-radius: 38rpx;
		opacity: 1 !important;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 76rpx;
		text-align: center;
	}

	.store-detail-page button.store-primary-cta::after {
		display: none !important;
		border: 0 !important;
	}

	.store-detail-page .store-primary-cta-label {
		display: block !important;
		color: #ffffff !important;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 76rpx;
		opacity: 1 !important;
	}

	/* Room cards use the compact image-left layout from the reference screens. */
	.store-detail-page .room-preview-card {
		display: flex;
		align-items: stretch;
		gap: 16rpx;
		padding: 14rpx;
	}

	.store-detail-page .room-preview-image-wrap {
		flex: 0 0 220rpx;
		height: 260rpx;
		overflow: hidden;
		border-radius: 16rpx;
	}

	.store-detail-page .room-preview-body {
		display: flex;
		min-width: 0;
		flex: 1;
		flex-direction: column;
		padding: 10rpx 4rpx 8rpx 0;
	}

	.store-detail-page .room-name-row {
		align-items: flex-start;
	}

	.store-detail-page .room-name {
		font-size: 24rpx;
		line-height: 1.35;
	}

	.store-detail-page .room-spec {
		margin-top: 12rpx;
		line-height: 1.45;
	}

	.store-detail-page .room-price-row {
		margin-top: auto;
		padding-top: 16rpx;
	}

	/* Keep the hero immersive on first load; reveal the shared green title bar after scrolling. */
	.store-detail-page .hero-back-button {
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

	.store-detail-page .store-scroll-header {
		position: fixed;
		top: 0;
		right: 0;
		left: 0;
		z-index: 60;
		pointer-events: none;
	}

	.store-detail-page .store-scroll-header .proto-header {
		position: relative !important;
		top: auto !important;
		right: auto !important;
		left: auto !important;
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18) !important;
		box-shadow: 0 6rpx 18rpx rgba(0, 106, 106, 0.16);
		pointer-events: auto;
	}

	.store-detail-page .store-scroll-header .proto-header-title,
	.store-detail-page .store-scroll-header .proto-header-icon-button,
	.store-detail-page .store-scroll-header .proto-header-user-button {
		color: #ffffff !important;
	}

	.store-detail-page .store-scroll-header .proto-header-trailing {
		display: none !important;
	}
</style>
