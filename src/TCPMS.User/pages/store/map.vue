<template>
	<view class="proto-page map-page proto-page-plain">
		<ProtoHeader title="门店地图" theme="green" />
		<view class="map-notice">
			<view><ProtoIcon name="location" :size="22" /><text>高德地图精确定位服务</text></view>
			<text class="map-city">{{ mapCity }}</text>
			<button hover-class="none" @tap="viewList"><ProtoIcon name="document" :size="22" /><text>列表视图</text></button>
		</view>
		<view class="map-coordinate-notice">
			<view><ProtoIcon name="info" :size="20" /><text>已加载 {{ stores.length }} 家营业门店，可点击价格标记查看详情</text></view>
			<ProtoIcon name="close" :size="20" @tap="viewList" />
		</view>
		<view class="map-canvas">
			<view class="park park-one"></view>
			<view class="park park-two"></view>
			<view class="river"></view>
			<view class="road road-one"></view>
			<view class="road road-two"></view>
			<view class="road road-three"></view>
			<view class="map-label label-park">人民公园</view>
			<view class="map-label label-metro">二七广场站</view>
			<view class="map-label label-river">金水河滨河带</view>
			<view class="user-marker"><ProtoIcon name="navigation" :size="22" /><text>我在这里</text></view>
			<view v-for="marker in markers" :key="marker.id" class="store-marker" :style="{ top: marker.top, left: marker.left }" @tap="selectedMarker = marker.id">
				<text>¥{{ marker.price }}</text>
				<text class="marker-name" :class="{ selected: selectedMarker === marker.id }">{{ marker.name }}</text>
			</view>
			<view class="map-controls">
				<button hover-class="none" aria-label="放大" @tap="zoomIn"><ProtoIcon name="plus" :size="22" /></button>
				<button hover-class="none" aria-label="缩小" @tap="zoomOut"><ProtoIcon name="minus" :size="22" /></button>
				<button hover-class="none" aria-label="定位" @tap="locateUser"><ProtoIcon name="navigation" :size="22" /></button>
			</view>
			<view class="map-search"><ProtoIcon name="search" :size="24" /><text>搜索此区域门店</text></view>
		</view>
		<view class="map-store-card">
			<image :src="selectedStore.image" mode="aspectFill"></image>
			<view class="map-store-info">
				<view class="map-store-title-row">
					<text class="map-store-title">{{ selectedStore.name }}</text>
					<text class="proto-pill success">营业中</text>
				</view>
				<view class="map-store-rating"><ProtoIcon name="star" :size="20" /><text>{{ selectedStore.rating }}　({{ selectedStore.reviews }}评价)</text></view>
				<view class="map-store-address"><ProtoIcon name="location" :size="20" /><text>距您 {{ selectedStore.distance }} · {{ selectedStore.address }}</text></view>
				<view class="map-store-actions">
					<text class="map-price">¥{{ selectedStore.price }}<text class="small">/晚起</text></text>
					<button class="proto-button-small proto-button-light" hover-class="none" aria-label="联系门店" @tap="callStore"><ProtoIcon name="phone" :size="22" /></button>
					<button class="proto-button-small proto-button-light" hover-class="none" aria-label="导航" @tap="openLocation"><ProtoIcon name="navigation" :size="22" /></button>
					<button class="proto-button-small" hover-class="none" @tap="goDetail">查看详情</button>
				</view>
			</view>
		</view>
		<button class="map-all-button" hover-class="none" @tap="viewList"><ProtoIcon name="document" :size="22" /><text>查看完整门店列表（共{{ stores.length }}家）</text></button>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoStores, goPage, showToast } from '@/common/prototype.js'
	import { fetchStores } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				selectedMarker: demoStores[0].id,
				selectedStore: demoStores[0],
				stores: demoStores,
				zoomLevel: 12,
				markers: []
			}
		},
		computed: {
			mapCity() {
				return this.selectedStore.city || '全城门店'
			}
		},
		async onLoad(options) {
			const requestedId = options && options.id ? options.id : ''
			try {
				const stores = await fetchStores()
				if (stores.length) this.stores = stores
			} catch (error) {
				// Keep the bundled map catalog available when the API is offline.
			}
			this.markers = this.buildMarkers(this.stores)
			const selected = this.stores.find((store) => String(store.id) === String(requestedId)) || this.stores[0]
			if (selected) {
				this.selectedStore = selected
				this.selectedMarker = selected.id
			}
		},
		watch: {
			selectedMarker(value) {
				const store = this.stores.find((item) => String(item.id) === String(value))
				if (store) this.selectedStore = store
			}
		},
		methods: {
			buildMarkers(stores) {
				const positions = [
					{ top: '42%', left: '44%' },
					{ top: '55%', left: '65%' },
					{ top: '70%', left: '26%' },
					{ top: '28%', left: '27%' },
					{ top: '36%', left: '72%' },
					{ top: '76%', left: '62%' }
				]
				return stores.map((store, index) => ({
					id: store.id,
					name: store.shortName || store.name,
					price: store.price,
					...positions[index % positions.length]
				}))
			},
			viewList() {
				uni.navigateTo({ url: '/pages/store/list' })
			},
			goDetail() {
				goPage(`/pages/store/detail?id=${this.selectedStore.id}`)
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.selectedStore.phone || '037188886622',
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			openLocation() {
				uni.openLocation({
					latitude: Number(this.selectedStore.latitude),
					longitude: Number(this.selectedStore.longitude),
					name: this.selectedStore.name,
					address: this.selectedStore.address,
					fail: () => showToast('地图服务暂不可用')
				})
			},
			zoomIn() {
				this.zoomLevel = Math.min(18, this.zoomLevel + 1)
				showToast(`地图级别 ${this.zoomLevel}`)
			},
			zoomOut() {
				this.zoomLevel = Math.max(8, this.zoomLevel - 1)
				showToast(`地图级别 ${this.zoomLevel}`)
			},
			locateUser() {
				uni.getLocation({
					type: 'gcj02',
					success: () => showToast('已回到当前位置'),
					fail: () => showToast('未开启定位权限')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.map-notice {
		display: flex;
		align-items: center;
		gap: 12rpx;
		padding: 22rpx 28rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface);
		font-size: 23rpx;
	}

	.map-city {
		padding: 7rpx 12rpx;
		background: #8debf2;
		border-radius: 8rpx;
	}

	.map-notice button {
		display: flex;
		align-items: center;
		margin-left: auto;
		padding: 12rpx 18rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 24rpx;
		font-size: 20rpx;
	}

	.map-notice > view,
	.map-coordinate-notice > view,
	.map-store-rating,
	.map-store-address,
	.map-search,
	.map-all-button {
		display: flex;
		align-items: center;
	}

	.map-notice > view .proto-icon,
	.map-coordinate-notice > view .proto-icon,
	.map-store-rating .proto-icon,
	.map-store-address .proto-icon,
	.map-search .proto-icon,
	.map-all-button .proto-icon {
		margin-right: 5rpx;
		flex: 0 0 auto;
	}

	.map-coordinate-notice {
		display: flex;
		justify-content: space-between;
		padding: 20rpx 28rpx;
		color: var(--proto-primary-dark);
		background: #bff4f6;
		font-size: 20rpx;
	}

	.map-coordinate-notice > .proto-icon {
		margin-left: 8rpx;
	}

	.map-state {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		height: 820rpx;
		padding: 40rpx;
		background: #e9eff0;
		text-align: center;
	}

	.map-state-icon {
		color: var(--proto-primary);
		font-size: 86rpx;
	}

	.map-state-title {
		margin-top: 22rpx;
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 750;
	}

	.map-state-desc {
		max-width: 560rpx;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
		line-height: 1.5;
	}

	.map-state button {
		margin-top: 24rpx;
	}

	.map-canvas {
		position: relative;
		height: 860rpx;
		overflow: hidden;
		background-color: #edf2f3;
		background-image: linear-gradient(90deg, rgba(255, 255, 255, 0.85) 3rpx, transparent 3rpx), linear-gradient(rgba(255, 255, 255, 0.85) 3rpx, transparent 3rpx);
		background-size: 124rpx 124rpx;
	}

	.park {
		position: absolute;
		width: 250rpx;
		height: 210rpx;
		background: #daf1da;
		border-radius: 45% 55% 48% 52%;
	}

	.park-one {
		top: 110rpx;
		left: -50rpx;
	}

	.park-two {
		right: -70rpx;
		bottom: 80rpx;
	}

	.river {
		position: absolute;
		right: -160rpx;
		bottom: 170rpx;
		width: 900rpx;
		height: 100rpx;
		background: #c5edf3;
		transform: rotate(-10deg);
	}

	.road {
		position: absolute;
		height: 12rpx;
		background: #ffffff;
		border: 4rpx solid #f7dca4;
		transform: rotate(-52deg);
	}

	.road-one {
		top: 280rpx;
		left: -180rpx;
		width: 1040rpx;
	}

	.road-two {
		top: 450rpx;
		left: 0;
		width: 920rpx;
		transform: rotate(2deg);
	}

	.road-three {
		top: 160rpx;
		left: 370rpx;
		width: 14rpx;
		height: 720rpx;
		transform: rotate(12deg);
	}

	.map-label {
		position: absolute;
		color: #7f9c9f;
		font-size: 20rpx;
	}

	.label-park {
		top: 168rpx;
		left: 70rpx;
	}

	.label-metro {
		top: 200rpx;
		right: 42rpx;
	}

	.label-river {
		right: 90rpx;
		bottom: 235rpx;
	}

	.user-marker {
		position: absolute;
		top: 50%;
		left: 48%;
		display: flex;
		align-items: center;
		flex-direction: column;
		color: #ffffff;
		text-shadow: 0 4rpx 10rpx rgba(0, 106, 106, 0.24);
	}

	.user-marker text {
		margin-top: 4rpx;
		padding: 7rpx 14rpx;
		color: var(--proto-primary-dark);
		background: rgba(255, 255, 255, 0.86);
		border-radius: 16rpx;
		font-size: 19rpx;
		text-shadow: none;
	}

	.store-marker {
		position: absolute;
		display: flex;
		align-items: center;
		flex-direction: column;
		color: #ffffff;
		background: var(--proto-primary);
		border: 8rpx solid #ffffff;
		border-radius: 50% 50% 50% 0;
		transform: rotate(-45deg);
		box-shadow: 0 8rpx 18rpx rgba(0, 106, 106, 0.22);
	}

	.store-marker > text:first-child {
		padding: 11rpx 7rpx;
		font-size: 20rpx;
		font-weight: 800;
		transform: rotate(45deg);
	}

	.marker-name {
		position: absolute;
		top: 68rpx;
		width: 180rpx;
		padding: 6rpx 8rpx;
		color: var(--proto-text);
		background: #ffffff;
		border-radius: 8rpx;
		font-size: 18rpx;
		text-align: center;
		transform: rotate(45deg);
	}

	.marker-name.selected {
		color: #ffffff;
		background: var(--proto-text);
	}

	.map-controls {
		position: absolute;
		top: 220rpx;
		right: 24rpx;
		display: flex;
		align-items: center;
		flex-direction: column;
		gap: 2rpx;
	}

	.map-controls button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 70rpx;
		height: 70rpx;
		color: var(--proto-text);
		background: #ffffff;
	}

	.map-controls button:last-child {
		margin-top: 18rpx;
		border-radius: 50%;
	}

	.map-search {
		position: absolute;
		top: 42rpx;
		left: 50%;
		padding: 16rpx 28rpx;
		color: var(--proto-text);
		background: #ffffff;
		border-radius: 34rpx;
		font-size: 23rpx;
		transform: translateX(-50%);
		box-shadow: 0 8rpx 20rpx rgba(32, 37, 43, 0.1);
	}

	.map-store-card {
		display: flex;
		gap: 18rpx;
		margin: -46rpx 26rpx 20rpx;
		padding: 18rpx;
		position: relative;
		z-index: 5;
		background: #ffffff;
		border-radius: 22rpx;
		box-shadow: 0 10rpx 30rpx rgba(32, 37, 43, 0.12);
	}

	.map-store-card > image {
		width: 160rpx;
		height: 160rpx;
		border-radius: 16rpx;
	}

	.map-store-info {
		flex: 1;
		min-width: 0;
	}

	.map-store-title-row {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.map-store-title {
		flex: 1;
		overflow: hidden;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.map-store-title-row .proto-pill {
		min-height: 32rpx;
		padding: 0 8rpx;
		font-size: 16rpx;
	}

	.map-store-rating {
		margin-top: 8rpx;
		color: #c08313;
		font-size: 21rpx;
	}

	.map-store-address {
		margin-top: 12rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.map-store-actions {
		display: flex;
		align-items: center;
		gap: 8rpx;
		margin-top: 18rpx;
	}

	.map-price {
		flex: 1;
		color: var(--proto-error);
		font-size: 32rpx;
		font-weight: 800;
	}

	.map-price .small {
		color: var(--proto-muted);
		font-size: 16rpx;
		font-weight: 400;
	}

	.map-store-actions .proto-button-small {
		height: 56rpx;
		padding: 0 14rpx;
		font-size: 18rpx;
	}

	.map-all-button {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 82rpx;
		margin: 0 26rpx 22rpx;
		color: var(--proto-primary-dark);
		background: #e4ebec;
		border-radius: 22rpx;
		font-size: 23rpx;
		font-weight: 700;
	}
</style>
