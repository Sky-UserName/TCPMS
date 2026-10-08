<template>
	<view class="proto-page store-list-page">
		<ProtoHeader title="民宿搜索" title-align="center" theme="green" />

		<view class="store-search-panel">
			<button class="city-button" hover-class="none" @tap="showCity = true">
				<text>{{ city }}</text>
				<ProtoIcon name="chevron-down" :size="18" />
			</button>
			<button class="date-button" hover-class="none" @tap="openDate">
				<text class="date-label">住</text><text>{{ booking.checkIn || '10月06日' }}</text>
				<text class="date-label">离</text><text>{{ booking.checkOut || '10月07日' }}</text>
			</button>
			<view class="store-search-input">
				<ProtoIcon name="search" :size="25" />
				<input v-model="searchText" placeholder="请输入商圈/房源" placeholder-class="search-placeholder" />
			</view>
			<button class="map-button" hover-class="none" aria-label="地图" @tap="goMap">
				<ProtoIcon name="map" :size="34" />
				<text>地图</text>
			</button>
		</view>

		<view class="store-filter-row">
			<button class="filter-tab active" hover-class="none" @tap="sort = sort === 'price' ? 'distance' : 'price'"><text>排序</text><ProtoIcon name="chevron-down" :size="17" /></button>
			<button class="filter-tab" hover-class="none" @tap="filterVisible = true"><text>位置</text><ProtoIcon name="chevron-down" :size="17" /></button>
			<button class="filter-tab" hover-class="none" @tap="filterVisible = true"><text>价格/人数</text><ProtoIcon name="chevron-down" :size="17" /></button>
			<button class="filter-tab" :class="{ active: featureFilter }" hover-class="none" @tap="filterVisible = true"><text>筛选</text><ProtoIcon name="chevron-down" :size="17" /></button>
		</view>

		<ProtoEmptyState v-if="!filteredStores.length" />

		<view v-else class="store-list">
			<view v-for="store in filteredStores" :key="store.id" class="store-card" @tap="goDetail(store)">
				<view class="store-collage">
					<image :src="store.image" mode="aspectFill" class="collage-main"></image>
					<view class="collage-side">
						<image :src="storeThumb(store, 1)" mode="aspectFill"></image>
						<image :src="storeThumb(store, 2)" mode="aspectFill"></image>
					</view>
					<view class="store-rating"><ProtoIcon name="star" :size="19" /><text>{{ store.rating }} 超棒</text><text class="rating-muted">{{ store.reviews }}条评价</text></view>
				</view>
				<view class="store-card-body">
					<view class="store-name-row"><text class="store-name">{{ store.name }}</text></view>
					<scroll-view class="store-tags" scroll-x show-scrollbar="false"><text v-for="tag in store.tags" :key="tag">{{ tag }}</text></scroll-view>
					<view class="store-meta"><text>{{ store.tags[0] || '品质民宿' }}</text><text>·</text><text>{{ store.tags[1] || '独立房间' }}</text><text>·</text><text>整套{{ store.tags.length + 26 }}㎡</text></view>
					<view class="store-address"><ProtoIcon name="location" :size="21" /><text>{{ store.address }}</text><text class="store-distance">{{ store.distance }}</text></view>
					<view class="store-price-row"><text class="store-price">¥{{ store.price }}</text><text class="price-unit">/晚起</text><button class="store-detail-button" hover-class="none" @tap.stop="goDetail(store)">查看房型</button></view>
				</view>
			</view>
		</view>

		<view v-if="filterVisible || showCity" class="proto-mask" @tap="filterVisible = false; showCity = false">
			<view class="proto-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>{{ showCity ? '选择城市' : '距离与范围筛选' }}</text>
					<button class="proto-close" hover-class="none" aria-label="关闭" @tap="filterVisible = false; showCity = false">
						<ProtoIcon name="close" :size="24" />
					</button>
				</view>
				<view v-if="showCity" class="city-grid">
					<text v-for="item in cities" :key="item" :class="{ selected: item === city }" @tap="selectCity(item)">{{ item }}</text>
				</view>
				<view v-else>
					<text class="sheet-caption">距离范围</text>
					<view class="option-grid">
						<text v-for="item in radiusOptions" :key="item.key" :class="{ selected: radius === item.key }" @tap="selectRadius(item.key)">{{ item.label }}</text>
					</view>
					<text class="sheet-caption">排序优先级</text>
					<view class="option-grid">
						<text :class="{ selected: sort === 'distance' }" @tap="sort = 'distance'">距离优先</text>
						<text :class="{ selected: sort === 'rating' }" @tap="sort = 'rating'">评分优先</text>
						<text :class="{ selected: sort === 'price' }" @tap="sort = 'price'">低价起步</text>
					</view>
					<button class="proto-primary-button" hover-class="none" @tap="filterVisible = false">确定筛选（{{ filteredStores.length }}家）</button>
				</view>
			</view>
		</view>

		<ProtoBottomNav active="stores" variant="root" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { demoStores, goPage, showToast } from '@/common/prototype.js'
	import { fetchStores } from '@/common/api.js'
	import { getBooking } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				stores: demoStores,
				booking: getBooking(),
				city: '郑州市',
				searchText: '',
				radius: '5km',
				sort: 'distance',
				featureFilter: '',
				locationText: '已根据您当前位置优先推荐最近门店（郑州二七区）',
				filterVisible: false,
				showCity: false,
				cities: ['全部城市', '郑州市', '洛阳市', '开封市', '安阳市', '新乡市', '焦作市', '上海市', '杭州市'],
				radiusOptions: [
					{ key: '1km', label: '1公里以内' },
					{ key: '3km', label: '3公里以内' },
					{ key: '5km', label: '5公里以内' },
					{ key: 'all', label: '全城范围' }
				]
			}
		},
		onLoad() {
			this.loadStores()
		},
		onShow() {
			this.booking = getBooking()
		},
		computed: {
			radiusLabel() {
				return this.radius === '5km' ? '5km' : this.radius === '1km' ? '1km' : this.radius === '3km' ? '3km' : '全城'
			},
			filteredStores() {
				const keyword = this.searchText.trim().toLowerCase()
				const list = this.stores.filter((store) => {
					const text = [store.name, store.address, store.badge].concat(store.tags).join(' ').toLowerCase()
					const distance = parseFloat(store.distance)
					const radiusMatch = this.radius === 'all'
						|| store.unknownDistance
						|| (this.radius === '1km' && distance <= 1)
						|| (this.radius === '3km' && distance <= 3)
						|| (this.radius === '5km' && distance <= 5)
					const cityMatch = this.city === '全部城市'
						|| (this.city === '郑州市' && !store.city && (store.address || '').indexOf('郑州') >= 0)
						|| store.city === this.city
						|| (store.address || '').indexOf(this.city) >= 0
					const featureMatch = !this.featureFilter
						|| (this.featureFilter === 'subway' && store.tags.some((tag) => tag.indexOf('地铁') >= 0))
						|| (this.featureFilter === 'host' && store.tags.some((tag) => tag.indexOf('房东') >= 0))
					return cityMatch && radiusMatch && featureMatch && (!keyword || text.indexOf(keyword) >= 0)
				})
				return list.sort((a, b) => {
					if (this.sort === 'rating') return Number(b.rating) - Number(a.rating)
					if (this.sort === 'price') return Number(a.price) - Number(b.price)
					if (a.unknownDistance) return 1
					if (b.unknownDistance) return -1
					return parseFloat(a.distance) - parseFloat(b.distance)
				})
			}
		},
		methods: {
			openDate() {
				goPage('/pages/booking/date')
			},
			storeThumb(store, offset = 1) {
				const index = this.stores.findIndex((item) => item.id === store.id)
				const next = this.stores[(index + offset) % this.stores.length]
				return (next && next.image) || store.image
			},
			async loadStores() {
				try {
					const stores = await fetchStores()
					if (!stores.length) return
					this.stores = stores
					const hasCurrentCity = stores.some((store) =>
						store.city === this.city || (store.address || '').indexOf(this.city) >= 0
					)
					if (!hasCurrentCity) {
						this.city = '全部城市'
						this.locationText = `已加载 ${stores.length} 家在线门店，当前展示全部城市`
					}
				} catch (error) {
					// Keep the local catalog available when the API is offline.
				}
			},
			goDetail(store) {
				goPage(`/pages/store/detail?id=${store.id}`)
			},
			goMap() {
				goPage('/pages/store/map')
			},
			selectCity(city) {
				this.city = city
				this.showCity = false
				showToast(`已切换至${city}`)
			},
			toggleFeature(feature) {
				this.featureFilter = this.featureFilter === feature ? '' : feature
			},
			selectRadius(radius) {
				this.radius = radius
			},
			clearFilters() {
				this.city = this.stores.some((store) =>
					store.city === '郑州市' || (store.address || '').indexOf('郑州') >= 0
				) ? '郑州市' : '全部城市'
				this.searchText = ''
				this.radius = '5km'
				this.sort = 'distance'
				this.featureFilter = ''
			},
			refreshLocation() {
				uni.getLocation({
					type: 'gcj02',
					success: () => {
						this.locationText = '已根据您当前位置优先推荐最近门店（郑州二七区）'
						showToast('定位已刷新')
					},
					fail: () => {
						this.locationText = '未开启定位，当前按城市与距离展示门店'
						showToast('未开启定位，仍可手动浏览门店')
					}
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.store-search-row {
		display: flex;
		align-items: center;
		width: 100%;
		min-height: 92rpx;
		gap: 10rpx;
		padding: 14rpx 24rpx 10rpx;
		background: var(--proto-surface);
	}

	.city-filter {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 154rpx;
		width: 154rpx;
		min-width: 154rpx;
		height: 68rpx;
		padding: 0 14rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 21rpx;
		white-space: nowrap;
	}

	.city-filter .proto-icon {
		margin-right: 5rpx;
	}

	.city-filter .proto-icon:last-child {
		margin-right: 0;
		margin-left: 4rpx;
	}

	.store-search {
		display: flex;
		align-items: center;
		flex: 1;
		min-width: 0;
		height: 68rpx;
		padding: 0 14rpx;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.store-search > .proto-icon {
		margin-right: 10rpx;
	}

	.store-search input {
		min-width: 0;
		flex: 1;
		height: 64rpx;
		color: var(--proto-text);
		font-size: 20rpx;
	}

	.search-placeholder {
		color: var(--proto-muted);
	}

	.view-toggle {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 58rpx;
		width: 62rpx;
		min-width: 58rpx;
		height: 68rpx;
		padding: 0;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.view-toggle.active {
		color: #ffffff;
		background: var(--proto-primary);
	}

	.filter-row {
		width: 100%;
		padding: 12rpx 24rpx 14rpx;
		background: var(--proto-surface);
		white-space: nowrap;
	}

	.filter-chip {
		display: inline-flex;
		align-items: center;
		width: auto;
		min-width: 0;
		flex: 0 0 auto;
		gap: 4rpx;
		height: 58rpx;
		margin-right: 10rpx;
		padding: 0 18rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 29rpx;
		font-size: 20rpx;
	}

	.filter-chip.active {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.location-notice {
		display: flex;
		align-items: center;
		min-height: 68rpx;
		padding: 14rpx 24rpx;
		color: var(--proto-primary-dark);
		background: #d8f6f5;
		font-size: 20rpx;
	}

	.notice-icon {
		margin-right: 8rpx;
	}

	.location-notice > .proto-icon {
		margin-right: 8rpx;
		flex: 0 0 auto;
	}

	.location-notice > text {
		flex: 1;
		overflow: hidden;
		white-space: nowrap;
		text-overflow: ellipsis;
	}

	.location-notice button {
		width: auto;
		min-width: 0;
		flex: 0 0 auto;
		margin-left: 12rpx;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 19rpx;
	}

	.store-list {
		padding: 10rpx 0 20rpx;
	}

	.store-card {
		margin: 10rpx 24rpx 16rpx;
		overflow: hidden;
		background: var(--proto-surface);
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 24rpx;
		box-shadow: 0 8rpx 24rpx rgba(42, 169, 169, 0.06);
	}

	.store-photo-wrap {
		position: relative;
		height: 326rpx;
	}

	.store-photo {
		width: 100%;
		height: 100%;
	}

	.photo-badges {
		position: absolute;
		top: 18rpx;
		left: 18rpx;
		display: flex;
		gap: 8rpx;
	}

	.store-rating {
		display: flex;
		align-items: center;
		gap: 4rpx;
		position: absolute;
		right: 18rpx;
		bottom: 18rpx;
		padding: 8rpx 14rpx;
		color: #ffffff;
		background: rgba(32, 37, 43, 0.76);
		border-radius: 16rpx;
		font-size: 20rpx;
	}

	.store-card-body {
		padding: 18rpx 20rpx 16rpx;
	}

	.store-name-row {
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.store-name {
		flex: 1;
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 750;
		line-height: 1.3;
	}

	.store-name-row .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}

	.store-address {
		display: flex;
		align-items: center;
		margin-top: 14rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 20rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.store-address .proto-icon {
		flex: 0 0 auto;
		margin-right: 5rpx;
	}

	.store-tags {
		display: flex;
		gap: 10rpx;
		margin-top: 14rpx;
		overflow: hidden;
		white-space: nowrap;
	}

	.store-tags text {
		padding: 7rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 8rpx;
		font-size: 18rpx;
	}

	.store-price-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 20rpx;
	}

	.store-price-row > view:first-child {
		display: flex;
		align-items: baseline;
		flex-wrap: wrap;
	}

	.available-text {
		margin-left: 6rpx;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
	}

	.old-price {
		margin-left: 12rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-decoration: line-through;
	}

	.store-price {
		margin-left: 6rpx;
		color: var(--proto-primary-dark);
		font-size: 38rpx;
		font-weight: 800;
	}

	.price-unit {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.skeleton-list {
		padding: 20rpx 24rpx;
	}

	.skeleton-card {
		margin-bottom: 20rpx;
		padding: 18rpx;
		background: var(--proto-surface);
		border-radius: 24rpx;
	}

	.skeleton-image,
	.skeleton-line {
		background: #e5eeee;
		border-radius: 12rpx;
	}

	.skeleton-image {
		height: 340rpx;
	}

	.skeleton-line {
		height: 28rpx;
		margin-top: 18rpx;
	}

	.skeleton-line-long {
		width: 75%;
	}

	.skeleton-line-short {
		width: 42%;
	}

	.city-grid,
	.option-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
		margin-top: 28rpx;
	}

	.city-grid text,
	.option-grid text {
		width: calc(33.333% - 10rpx);
		padding: 20rpx 10rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 21rpx;
		text-align: center;
	}

	.city-grid text.selected,
	.option-grid text.selected {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.sheet-caption {
		display: block;
		margin-top: 28rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
	}
</style>

<style lang="scss" scoped>
	.store-list-page {
		padding-bottom: 156rpx;
		background: #ffffff;
	}

	.store-list-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.store-search-panel {
		display: flex;
		align-items: center;
		gap: 8rpx;
		padding: 20rpx 24rpx 12rpx;
		background: #ffffff;
	}

	.city-button,
	.date-button,
	.map-button,
	.filter-tab,
	.store-detail-button {
		margin: 0;
		padding: 0;
		border: 0;
	}

	.city-button,
	.date-button,
	.store-search-input {
		display: flex;
		align-items: center;
		min-width: 0;
		height: 82rpx;
		color: #20252b;
		background: #f5f7fa;
		border: 1rpx solid #e1e7ee;
	}

	.city-button {
		flex: 0 0 148rpx;
		justify-content: center;
		gap: 5rpx;
		border-radius: 41rpx 0 0 41rpx;
		font-size: 26rpx;
	}

	.date-button {
		flex: 0 0 154rpx;
		justify-content: center;
		flex-wrap: wrap;
		column-gap: 5rpx;
		row-gap: 0;
		padding: 0 8rpx;
		border-left: 0;
		border-radius: 0;
		font-size: 24rpx;
		line-height: 1.12;
	}

	.date-button .date-label {
		color: #7b8b96;
		font-size: 21rpx;
	}

	.store-search-input {
		flex: 1 1 auto;
		padding: 0 14rpx;
		border-left: 0;
		border-radius: 0 41rpx 41rpx 0;
	}

	.store-search-input .proto-icon {
		flex: 0 0 auto;
		margin-right: 8rpx;
	}

	.store-search-input input {
		width: 100%;
		min-width: 0;
		height: 70rpx;
		font-size: 23rpx;
	}

	.search-placeholder {
		color: #9aa5b1;
	}

	.map-button {
		display: flex;
		align-items: center;
		flex: 0 0 72rpx;
		flex-direction: column;
		justify-content: center;
		width: 72rpx;
		height: 82rpx;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 19rpx;
		line-height: 1.2;
	}

	.map-button .proto-icon {
		margin-bottom: 3rpx;
	}

	.store-filter-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 16rpx 26rpx 12rpx;
		background: #ffffff;
		border-bottom: 1rpx solid #eef1f4;
	}

	.filter-tab {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 4rpx;
		min-width: 0;
		color: #6d7781;
		background: transparent;
		font-size: 23rpx;
	}

	.filter-tab.active {
		color: var(--proto-primary);
		font-weight: 750;
	}

	.store-location-line {
		display: flex;
		align-items: center;
		min-height: 58rpx;
		padding: 10rpx 28rpx;
		color: #7b8791;
		background: #fbfcfd;
		font-size: 18rpx;
	}

	.store-location-line > .proto-icon {
		flex: 0 0 auto;
		margin-right: 7rpx;
	}

	.store-location-line > text {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.store-location-line button {
		flex: 0 0 auto;
		margin-left: 10rpx;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 18rpx;
	}

	.store-list {
		padding: 14rpx 24rpx 30rpx;
		background: #ffffff;
	}

	.store-card {
		margin: 0 0 22rpx;
		overflow: hidden;
		background: #ffffff;
		border: 1rpx solid #e3e9ef;
		border-radius: 22rpx;
		box-shadow: 0 7rpx 20rpx rgba(21, 73, 112, 0.06);
	}

	.store-collage {
		position: relative;
		display: flex;
		gap: 8rpx;
		height: 326rpx;
		padding: 16rpx 16rpx 0;
	}

	.collage-main {
		width: calc(68% - 4rpx);
		height: 294rpx;
		border-radius: 12rpx;
	}

	.collage-side {
		display: flex;
		flex: 1;
		flex-direction: column;
		gap: 8rpx;
	}

	.collage-side image {
		width: 100%;
		height: 143rpx;
		border-radius: 12rpx;
	}

	.store-rating {
		position: absolute;
		bottom: 14rpx;
		left: 28rpx;
		display: flex;
		align-items: center;
		gap: 6rpx;
		padding: 8rpx 14rpx;
		color: #ffffff;
		background: rgba(31, 47, 64, 0.78);
		border-radius: 26rpx;
		font-size: 22rpx;
	}

	.store-rating .proto-icon {
		filter: brightness(0) invert(1);
	}

	.rating-muted {
		color: rgba(255, 255, 255, 0.7);
		font-size: 18rpx;
	}

	.store-card-body {
		padding: 18rpx 18rpx 20rpx;
	}

	.store-name-row {
		display: flex;
		align-items: center;
	}

	.store-name {
		color: #1f2730;
		font-size: 29rpx;
		font-weight: 800;
		line-height: 1.3;
	}

	.store-tags {
		width: 100%;
		margin-top: 14rpx;
		white-space: nowrap;
	}

	.store-tags text {
		display: inline-block;
		margin-right: 12rpx;
		padding: 7rpx 14rpx;
		color: #66717b;
		background: #ffffff;
		border: 1rpx solid #d8dee5;
		border-radius: 5rpx;
		font-size: 19rpx;
	}

	.store-meta {
		display: flex;
		align-items: center;
		gap: 8rpx;
		margin-top: 15rpx;
		color: #727e88;
		font-size: 21rpx;
	}

	.store-address {
		display: flex;
		align-items: center;
		margin-top: 14rpx;
		color: #6f7b85;
		font-size: 21rpx;
	}

	.store-address .proto-icon {
		flex: 0 0 auto;
		margin-right: 7rpx;
	}

	.store-address > text:first-of-type {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.store-distance {
		flex: 0 0 auto;
		margin-left: 10rpx;
		color: #717b85;
	}

	.store-price-row {
		display: flex;
		align-items: baseline;
		margin-top: 20rpx;
	}

	.store-price {
		color: #1d2732;
		font-size: 36rpx;
		font-weight: 850;
	}

	.price-unit {
		margin-left: 4rpx;
		color: #7b8790;
		font-size: 18rpx;
	}

	.store-detail-button {
		margin-left: auto;
		padding: 0 22rpx;
		height: 54rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 27rpx;
		font-size: 19rpx;
	}

</style>
