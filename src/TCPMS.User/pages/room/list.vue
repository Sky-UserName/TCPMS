<template>
	<view class="proto-page room-list-page">
		<ProtoHeader title="房型列表" title-align="center" theme="green" />

		<view class="room-search-row">
			<view class="room-search-shell">
				<button class="room-city-button" hover-class="none" @tap="showCityNotice">
					<text>{{ booking.city || store.city || '郑州市' }}</text>
					<ProtoIcon name="chevron-down" :size="16" />
				</button>
				<view class="room-keyword-input">
					<ProtoIcon name="search" :size="24" />
					<input v-model="roomKeyword" placeholder="请输入商圈/房源" placeholder-class="room-search-placeholder" />
				</view>
			</view>
			<button class="room-map-button" hover-class="none" @tap="goMap">
				<ProtoIcon name="map" :size="30" />
				<text>地图</text>
			</button>
		</view>

		<view class="date-card proto-card" @tap="goDate">
			<view>
				<text class="date-card-label">入住时间</text>
				<text class="date-card-value">{{ booking.checkIn }}</text>
				<text class="date-card-sub">14:00后</text>
			</view>
			<view class="date-card-center">
				<view class="date-card-night">
					<text>共 {{ booking.nights }} 晚</text>
					<ProtoIcon name="chevron-right" :size="22" />
				</view>
			</view>
			<view class="date-card-right">
				<text class="date-card-label">离店时间</text>
				<text class="date-card-value">{{ booking.checkOut }}</text>
				<text class="date-card-sub">12:00前</text>
			</view>
			<button class="date-edit" hover-class="none" @tap.stop="goDate">
				<text>修改日期</text>
				<ProtoIcon name="calendar" :size="22" />
			</button>
		</view>

		<view class="room-filter-panel">
			<button class="room-filter-tab active" hover-class="none" @tap="toggleRoomSort">
				<text>排序</text><ProtoIcon name="chevron-down" :size="16" />
			</button>
			<button class="room-filter-tab" hover-class="none" @tap="filterVisible = true">
				<text>位置</text><ProtoIcon name="chevron-down" :size="16" />
			</button>
			<button class="room-filter-tab" hover-class="none" @tap="filterVisible = true">
				<text>价格/人数</text><ProtoIcon name="chevron-down" :size="16" />
			</button>
			<button class="room-filter-tab" :class="{ active: activeTab !== 'all' }" hover-class="none" @tap="filterVisible = true">
				<text>{{ activeTab === 'all' ? '筛选' : activeRoomTypeLabel }}</text><ProtoIcon name="chevron-down" :size="16" />
			</button>
		</view>

		<ProtoEmptyState v-if="!filteredRooms.length" />

		<view v-else class="room-list">
			<view class="room-list-heading">
				<view>
					<text class="room-list-heading-title">为你推荐</text>
					<text class="room-list-heading-sub">实拍房源 · 到店可订</text>
				</view>
				<text class="room-list-heading-sort">{{ roomSort === 'price' ? '价格优先' : '综合排序' }}</text>
			</view>
			<view v-for="room in roomCards" :key="room.id" class="room-card" @tap="openRoomDetail(room)">
				<view class="room-image-wrap" :class="'gallery-' + room.gallery.length">
					<image
						v-for="(image, imageIndex) in room.gallery"
						:key="`${room.id}-${imageIndex}-${image}`"
						class="room-image"
						:src="image"
						mode="aspectFill"
					/>
					<text class="proto-pill" :class="room.soldOut ? 'muted' : room.type === '多人间床位' ? 'danger' : 'info'">{{ room.soldOut ? '今日已售罄' : room.type }}</text>
					<view v-if="room.gallery.length > 1" class="image-count">
						<ProtoIcon name="image" :size="20" />
						<text>{{ room.gallery.length }}张实拍</text>
					</view>
				</view>
				<view class="room-card-body">
					<view class="room-card-title-row">
						<text class="room-card-title">{{ room.name }}</text>
						<text class="room-card-price">¥{{ room.price }}<text class="small">/晚起</text></text>
					</view>
					<text class="room-card-spec">{{ room.capacity }} · {{ room.beds }} · {{ room.area }} · 独立采光飘窗</text>
					<view class="room-tag-row">
						<text v-for="tag in room.tags" :key="tag">{{ tag }}</text>
					</view>
					<view class="room-card-footer">
						<view class="room-availability" :class="{ sold: room.soldOut }">
							<ProtoIcon :name="room.soldOut ? 'warning' : 'check-circle'" :size="18" />
							<text>{{ room.availability }}</text>
						</view>
						<button class="room-book-button" :class="{ 'room-book-button-disabled': room.soldOut }" hover-class="none" @tap.stop="bookRoom(room)"><text>{{ room.soldOut ? '改日期' : '订' }}</text></button>
					</view>
				</view>
			</view>
		</view>

		<view class="room-help-bar">
			<view @tap="goService"><ProtoIcon name="support" :size="22" /><text>微信客服</text></view>
			<view @tap="callStore"><ProtoIcon name="phone" :size="22" /><text>联系前台</text></view>
			<text>房源支持　入住前24h免费退</text>
		</view>

		<view v-if="filterVisible" class="proto-mask" @tap="filterVisible = false">
			<view class="proto-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>房型筛选</text>
					<button class="proto-close" hover-class="none" aria-label="关闭" @tap="filterVisible = false">
						<ProtoIcon name="close" :size="24" />
					</button>
				</view>
				<text class="sheet-caption">房型偏好</text>
				<view class="filter-selection-summary">
					<text>当前选择</text>
					<text class="filter-selection-value">{{ activeRoomTypeLabel }}</text>
				</view>
				<view class="filter-options">
					<text v-for="tab in roomTabs" :key="tab.key" :class="{ selected: activeTab === tab.key }" @tap="selectRoomType(tab.key)">{{ tab.label }}</text>
				</view>
				<button class="proto-primary-button" hover-class="none" @tap="filterVisible = false">确定筛选</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoRooms, demoStores, goPage, prototypeImages, showToast } from '@/common/prototype.js'
	import { fetchStoreDetail } from '@/common/api.js'
	import { getBooking, saveBooking } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				rooms: demoRooms,
				storeId: getBooking().storeId || '',
				store: demoStores[0],
				booking: getBooking(),
				roomKeyword: '',
				activeTab: 'all',
				roomSort: 'default',
				filterVisible: false,
				roomTabs: [
					{ key: 'all', label: '全部' },
					{ key: 'private', label: '独立房间' },
					{ key: 'bed', label: '多人床位' },
					{ key: 'female', label: '限女生' }
				]
			}
		},
		async onLoad(options) {
			this.storeId = options && options.storeId ? options.storeId : this.booking.storeId
			const localStore = demoStores.find((item) => String(item.id) === String(this.storeId))
			if (localStore) {
				this.store = localStore
			}
			if (this.storeId && !/^\d+$/.test(String(this.storeId))) {
				try {
					const result = await fetchStoreDetail(this.storeId)
					if (result.store) {
						this.store = result.store
					}
					if (result.rooms.length) this.rooms = result.rooms
				} catch (error) {
					// Keep the bundled room catalog available when the API is offline.
				}
			}
			this.booking = saveBooking({
				...this.booking,
				storeId: this.storeId || this.booking.storeId,
				store: this.store.name || this.booking.store,
				address: this.store.address || this.booking.address
			})
		},
		onShow() {
			this.booking = getBooking()
		},
		computed: {
			roomCards() {
				return this.filteredRooms.map((room) => ({
					...room,
					gallery: this.getRoomGallery(room)
				}))
			},
			filteredRooms() {
				const keyword = this.roomKeyword.trim().toLowerCase()
				const list = this.rooms.filter((room) => {
					const text = [room.name, room.type, room.capacity, room.beds, room.area].concat(room.tags || []).join(' ').toLowerCase()
					if (keyword && text.indexOf(keyword) < 0) return false
					if (this.activeTab === 'private') return room.type === '独立房间'
					if (this.activeTab === 'bed') return room.type === '多人间床位'
					if (this.activeTab === 'female') return room.gender === '女生专区'
					return true
				})
				if (this.roomSort === 'price') return list.slice().sort((a, b) => Number(a.price) - Number(b.price))
				return list
			},
			activeRoomTypeLabel() {
				const current = this.roomTabs.find((tab) => tab.key === this.activeTab)
				return current ? current.label : '全部'
			}
		},
		methods: {
			getRoomGallery(room) {
				const source = room.images || room.imageUrls || room.photos || room.gallery
				const candidates = Array.isArray(source) ? source : source ? [source] : []
				const images = candidates
					.map((item) => typeof item === 'string' ? item : item && (item.url || item.src || item.path))
					.filter(Boolean)
				if (images.length) return [...new Set(images)].slice(0, 3)

				const fallback = room.image ? [room.image] : []
				// Keep the bundled prototype visually representative while remote room
				// records continue to work with their single image fallback.
				if (String(room.image || '').indexOf('/static/') === 0) {
					if (String(room.id) === '2') return [...new Set([room.image, prototypeImages.homeHero])]
					if (String(room.id) === '3') return [...new Set([room.image, prototypeImages.homeHero, prototypeImages.storeTwo])]
				}
				return fallback.slice(0, 1)
			},
			toggleRoomSort() {
				this.roomSort = this.roomSort === 'price' ? 'default' : 'price'
				showToast(this.roomSort === 'price' ? '已按价格从低到高排序' : '已恢复默认排序')
			},
			goDate() {
				goPage(`/pages/booking/date?from=room-list&storeId=${encodeURIComponent(this.storeId || this.store.id || '')}`)
			},
			goRoom(room) {
				if (room.soldOut) {
					showToast('当前日期无房，可选择其他日期')
					this.goDate()
					return
				}
				this.openRoomDetail(room)
			},
			bookRoom(room) {
				if (!room || room.soldOut) {
					this.goDate()
					return
				}
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
				goPage('/pages/booking/confirm')
			},
			openRoomDetail(room) {
				if (!room || !room.id) return
				goPage(`/pages/room/detail?id=${encodeURIComponent(room.id)}&storeId=${encodeURIComponent(this.storeId || this.store.id || '')}`)
			},
			clearFilter() {
				this.activeTab = 'all'
				this.roomSort = 'default'
				this.roomKeyword = ''
			},
			selectRoomType(key) {
				this.activeTab = key
			},
			goMap() {
				goPage(`/pages/store/map?id=${this.storeId || ''}`)
			},
			showCityNotice() {
				showToast('城市切换请返回门店搜索页操作')
			},
			goService() {
				goPage('/pages/service/contact')
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.store.phone || '037188886622',
					fail: () => showToast('暂无法拨打前台电话')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.room-list-page {
		background: #f3f9fb;
	}

	.date-card {
		position: relative;
		display: flex;
		align-items: center;
		gap: 8rpx;
		min-height: 178rpx;
		margin: 16rpx 24rpx 18rpx;
		padding: 22rpx 20rpx 38rpx;
		background: #ffffff;
		border: 1rpx solid #d8e9ee;
		border-radius: 24rpx;
		box-shadow: 0 10rpx 28rpx rgba(31, 102, 126, 0.07);
	}

	.date-card > view {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.date-card > view:first-child {
		align-items: flex-start;
	}

	.date-card-right {
		align-items: flex-end;
		text-align: right;
	}

	.date-card-label,
	.date-card-sub {
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.date-card-value {
		margin-top: 7rpx;
		color: var(--proto-text);
		font-size: 38rpx;
		font-weight: 800;
		line-height: 1.05;
		white-space: nowrap;
	}

	.date-card-center {
		flex: 0 0 142rpx !important;
		align-items: center;
		text-align: center;
	}

	.date-card-night {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 126rpx;
		padding: 12rpx 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border: 1rpx solid #a3e4e6;
		border-radius: 28rpx;
		font-size: 20rpx;
		white-space: nowrap;
	}

	.date-card-night .proto-icon {
		margin-left: 4rpx;
	}

	.date-edit {
		position: absolute;
		right: 20rpx;
		bottom: 11rpx;
		display: flex;
		align-items: center;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 18rpx;
	}

	.room-filter-panel {
		display: flex;
		align-items: center;
		justify-content: space-between;
		min-height: 78rpx;
		margin: 0 24rpx;
		padding: 0 4rpx;
		background: transparent;
		border-top: 1rpx solid rgba(185, 211, 217, 0.54);
		border-bottom: 1rpx solid rgba(185, 211, 217, 0.54);
	}

	.room-filter-tab {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 1;
		gap: 3rpx;
		height: 74rpx;
		margin: 0;
		padding: 0 4rpx;
		color: #6d818c;
		background: transparent;
		font-size: 21rpx;
		white-space: nowrap;
	}

	.room-filter-tab.active {
		color: var(--proto-primary-dark);
		font-weight: 750;
	}

	.room-filter-tab.active::after {
		position: absolute;
		bottom: -1rpx;
		width: 36rpx;
		height: 4rpx;
		background: var(--proto-primary);
		border-radius: 5rpx;
		content: '';
	}

	.room-filter-tab .proto-icon {
		margin-left: 2rpx;
	}

	.date-edit .proto-icon {
		margin-left: 4rpx;
	}

	.room-search-row {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin: 16rpx 24rpx 16rpx;
	}

	.room-search-shell {
		display: flex;
		flex: 1;
		align-items: center;
		min-width: 0;
		height: 82rpx;
		background: #ffffff;
		border: 1rpx solid #d8e9ee;
		border-radius: 24rpx;
		box-shadow: 0 6rpx 18rpx rgba(31, 102, 126, 0.05);
	}

	.room-city-button,
	.room-map-button {
		display: flex;
		align-items: center;
		justify-content: center;
		margin: 0;
		padding: 0;
		color: #2d3a43;
		background: transparent;
		font-size: 21rpx;
		white-space: nowrap;
	}

	.room-city-button {
		flex: 0 0 142rpx;
		gap: 4rpx;
		color: var(--proto-text);
	}

	.room-keyword-input {
		display: flex;
		align-items: center;
		flex: 1;
		min-width: 0;
		height: 54rpx;
		padding: 0 12rpx;
		border-left: 1rpx solid #dfe8ed;
	}

	.room-keyword-input .proto-icon {
		flex: 0 0 auto;
		margin-right: 6rpx;
	}

	.room-keyword-input input {
		width: 100%;
		min-width: 0;
		height: 52rpx;
		color: #2d3a43;
		font-size: 20rpx;
	}

	.room-search-placeholder {
		color: #9aa5ad;
	}

	.room-map-button {
		flex: 0 0 72rpx;
		height: 82rpx;
		flex-direction: column;
		gap: 1rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border: 1rpx solid #b6e7e8;
		border-radius: 22rpx;
		font-size: 17rpx;
		line-height: 1.1;
	}

	.room-map-button .proto-icon {
		margin-bottom: 3rpx;
	}

	.room-list-heading {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		margin: 18rpx 24rpx 10rpx;
	}

	.room-list-heading-title {
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 800;
	}

	.room-list-heading-sub {
		margin-left: 12rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.room-list-heading-sort {
		color: var(--proto-primary-dark);
		font-size: 18rpx;
	}

	.room-card {
		margin: 0 24rpx 22rpx;
		padding: 14rpx 14rpx 20rpx;
		background: var(--proto-surface);
		border: 1rpx solid #d5e7eb;
		border-radius: 26rpx;
		box-shadow: 0 10rpx 28rpx rgba(31, 102, 126, 0.075);
	}

	.room-image-wrap {
		position: relative;
		display: grid;
		gap: 8rpx;
		height: 344rpx;
		overflow: hidden;
		border-radius: 16rpx;
		background: #eaf4f6;
	}

	.room-image-wrap.gallery-2 {
		grid-template-columns: repeat(2, minmax(0, 1fr));
		height: 292rpx;
	}

	.room-image-wrap.gallery-3 {
		height: 340rpx;
	}

	.room-image-wrap.gallery-1 {
		grid-template-columns: 1fr;
	}

	.room-image-wrap.gallery-3 {
		grid-template-columns: 1.65fr 1fr;
		grid-template-rows: 1fr 1fr;
	}

	.room-image-wrap.gallery-3 .room-image:first-child {
		grid-row: 1 / 3;
	}

	.room-image {
		width: 100%;
		height: 100%;
		min-width: 0;
		min-height: 0;
		background: #edf5f6;
	}

	.room-image-wrap .proto-pill {
		position: absolute;
		top: 14rpx;
		left: 14rpx;
		box-shadow: 0 4rpx 12rpx rgba(20, 40, 50, 0.08);
	}

	.image-count {
		position: absolute;
		right: 14rpx;
		bottom: 14rpx;
		display: flex;
		align-items: center;
		padding: 8rpx 12rpx;
		color: #ffffff;
		background: rgba(32, 37, 43, 0.7);
		border-radius: 14rpx;
		font-size: 18rpx;
	}

	.image-count .proto-icon {
		margin-right: 5rpx;
	}

	.room-card-body {
		padding: 20rpx 4rpx 0;
	}

	.room-card-title-row {
		display: flex;
		align-items: flex-start;
		gap: 12rpx;
	}

	.room-card-title {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 750;
		line-height: 1.3;
	}

	.room-card-price {
		color: var(--proto-primary-dark);
		font-size: 38rpx;
		font-weight: 800;
		line-height: 1.1;
		white-space: nowrap;
	}

	.room-card-price .small {
		color: var(--proto-muted);
		font-size: 17rpx;
		font-weight: 400;
	}

	.room-card-spec {
		display: block;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.45;
	}

	.room-tag-row {
		display: flex;
		gap: 10rpx;
		margin-top: 16rpx;
		overflow: hidden;
		white-space: nowrap;
	}

	.room-tag-row text {
		padding: 7rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border: 1rpx solid #dcecee;
		border-radius: 8rpx;
		font-size: 17rpx;
	}

	.room-card-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 20rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #edf3f4;
	}

	.room-availability {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
	}

	.room-availability .proto-icon {
		margin-right: 5rpx;
	}

	.room-availability.sold {
		color: var(--proto-muted);
	}

	.room-card-footer .proto-button-small {
		height: 62rpx;
		min-width: 150rpx;
		background: var(--proto-primary-dark);
	}

	.room-card-footer .proto-button-disabled {
		color: var(--proto-muted);
		background: var(--proto-surface-low);
	}

	.room-help-bar {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		justify-content: space-around;
		gap: 12rpx 18rpx;
		margin: 4rpx 24rpx 12rpx;
		padding: 18rpx 12rpx 24rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		border-top: 1rpx solid rgba(185, 211, 217, 0.54);
	}

	.room-help-bar > view {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
	}

	.room-help-bar .proto-icon {
		margin-right: 5rpx;
	}

	.room-help-bar > text {
		flex: 0 0 100%;
		color: var(--proto-primary-dark);
		font-size: 17rpx;
		text-align: center;
	}

	.filter-options {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
		margin-top: 20rpx;
	}

	.filter-selection-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 16rpx;
		padding: 16rpx 18rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 19rpx;
	}

	.filter-selection-value {
		color: var(--proto-primary-dark);
		font-weight: 700;
	}

	.filter-options text {
		padding: 18rpx 22rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 20rpx;
	}

	.filter-options text.selected {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.sheet-caption {
		display: block;
		margin-top: 28rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
	}

	.room-card-footer .room-book-button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 82rpx;
		height: 58rpx;
		margin: 0;
		padding: 0 24rpx;
		color: #ffffff !important;
		background: #2aa9a9 !important;
		border: 0;
		border-radius: 29rpx;
		font-size: 22rpx;
		font-weight: 800;
		line-height: 58rpx;
		text-align: center;
	}

	.room-card-footer .room-book-button-disabled {
		color: var(--proto-muted) !important;
		background: var(--proto-surface-low) !important;
	}

	.room-card-footer .room-book-button > text {
		color: inherit;
		font-size: inherit;
		font-weight: inherit;
		line-height: inherit;
	}
</style>
