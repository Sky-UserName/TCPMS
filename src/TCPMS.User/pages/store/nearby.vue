<template>
	<view class="proto-page nearby-page">
		<ProtoHeader title="附近门店" theme="green" />
		<view class="nearby-state">
			<view class="nearby-state-icon"><ProtoIcon name="location" :size="30" /></view>
			<view class="nearby-state-copy">
				<text class="nearby-state-title">{{ locationText }}</text>
				<text class="nearby-state-desc">{{ locationDesc }}</text>
			</view>
			<button class="proto-button-small proto-button-light" hover-class="none" @tap="relocate">重新定位</button>
		</view>
		<view class="nearby-heading">
			<text>搜索半径范围</text>
			<text class="proto-caption">当前覆盖全城直选</text>
		</view>
		<view class="radius-row">
			<text v-for="item in radiusOptions" :key="item" :class="{ active: radius === item }" @tap="radius = item">{{ item }}</text>
		</view>

		<ProtoEmptyState v-if="!nearbyStores.length" action-text="查看全部门店" @action="goAllStores" />

		<view v-else class="nearby-list">
			<view v-for="store in nearbyStores" :key="store.id" class="nearby-card" @tap="goDetail(store)">
				<view class="nearby-image-wrap">
					<image :src="store.image" mode="aspectFill" class="nearby-image"></image>
					<text class="proto-pill primary nearby-status">{{ store.status }} · {{ store.badge }}</text>
					<view class="nearby-distance"><ProtoIcon name="location" :size="20" /><text>距您 {{ store.distance }}</text></view>
				</view>
				<view class="nearby-body">
					<view class="nearby-title-row">
						<text class="nearby-title">{{ store.name }}</text>
						<button class="text-nav" hover-class="none" @tap.stop="openLocation(store)"><text>导航</text><ProtoIcon name="navigation" :size="20" /></button>
					</view>
					<view class="nearby-address"><ProtoIcon name="location" :size="20" /><text>{{ store.address }}</text></view>
					<view class="nearby-tags">
						<text v-for="tag in store.tags" :key="tag">{{ tag }}</text>
					</view>
					<view class="nearby-price-row">
						<text class="nearby-price">¥{{ store.price }}<text class="small">/晚起</text></text>
						<button class="proto-button-small" hover-class="none" @tap.stop="goDetail(store)"><text>查看详情</text><ProtoIcon name="chevron-right" tone="white" :size="22" /></button>
					</view>
				</view>
			</view>
		</view>
		<ProtoBottomNav active="stores" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { demoStores, goPage, showToast } from '@/common/prototype.js'
	import { fetchNearbyStores } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				radius: '50km',
				radiusOptions: ['5km', '10km', '30km', '50km'],
				locationStatus: 'ready',
				stores: demoStores
			}
		},
		computed: {
			nearbyStores() {
				const maxDistance = Number.parseFloat(this.radius)
				if (this.radius === '50km') return this.stores
				return this.stores.filter((store) => {
					const distance = Number.parseFloat(store.distance)
					return !store.unknownDistance && !Number.isNaN(distance) && distance <= maxDistance
				})
			},
			locationText() {
				if (this.locationStatus === 'loading') return '正在获取当前 GPS 坐标...'
				if (this.locationStatus === 'denied') return '未开启系统定位，无法精确测距'
				return '已定位到郑州市金水区'
			},
			locationDesc() {
				if (this.locationStatus === 'loading') return '正在搜寻周边精品民宿与旅宿网络'
				if (this.locationStatus === 'denied') return '可以按推荐顺序浏览全部门店'
				return '精准经纬度高精度校准 · 智能测距生效中'
			}
		},
		methods: {
			relocate() {
				this.locationStatus = 'loading'
				uni.getLocation({
					type: 'gcj02',
					success: async (res) => {
						this.locationStatus = 'ready'
						try {
							const stores = await fetchNearbyStores(res.latitude, res.longitude, 50)
							this.stores = stores
						} catch (error) {
							// Keep the bundled catalog available when the API is offline.
						}
						showToast('已重新定位')
					},
					fail: () => {
						this.locationStatus = 'denied'
						showToast('未开启定位，仍可浏览门店')
					}
				})
			},
			goDetail(store) {
				goPage(`/pages/store/detail?id=${store.id}`)
			},
			goAllStores() {
				uni.reLaunch({ url: '/pages/store/list' })
			},
			openLocation(store) {
				uni.openLocation({
					latitude: Number(store.latitude),
					longitude: Number(store.longitude),
					name: store.name,
					address: store.address,
					fail: () => goPage(`/pages/store/map?id=${store.id}`)
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.nearby-state {
		display: flex;
		align-items: center;
		gap: 16rpx;
		margin: 16rpx 24rpx;
		padding: 20rpx;
		background: var(--proto-surface);
		border-radius: 22rpx;
	}

	.nearby-state-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.nearby-state > .nearby-state-copy {
		flex: 1;
		min-width: 0;
	}

	.nearby-state-title,
	.nearby-state-desc {
		display: block;
	}

	.nearby-state-title {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.nearby-state-desc {
		margin-top: 6rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.4;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.nearby-heading {
		display: flex;
		justify-content: space-between;
		margin: 26rpx 28rpx 14rpx;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.radius-row {
		display: flex;
		margin: 0 24rpx 18rpx;
		padding: 6rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.radius-row text {
		flex: 1;
		padding: 18rpx 0;
		color: var(--proto-text);
		border-radius: 12rpx;
		font-size: 21rpx;
		text-align: center;
	}

	.radius-row text.active {
		color: #ffffff;
		background: var(--proto-primary-dark);
	}

	.nearby-card {
		margin: 12rpx 24rpx;
		overflow: hidden;
		background: var(--proto-surface);
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 22rpx;
	}

	.nearby-image-wrap {
		position: relative;
		height: 318rpx;
	}

	.nearby-image {
		width: 100%;
		height: 100%;
	}

	.nearby-status {
		position: absolute;
		top: 18rpx;
		left: 18rpx;
	}

	.nearby-distance {
		display: flex;
		align-items: center;
		position: absolute;
		right: 18rpx;
		bottom: 18rpx;
		padding: 10rpx 16rpx;
		color: var(--proto-primary-dark);
		background: rgba(255, 255, 255, 0.92);
		border-radius: 20rpx;
		font-size: 19rpx;
	}

	.nearby-distance .proto-icon {
		margin-right: 4rpx;
	}

	.nearby-body {
		padding: 22rpx;
	}

	.nearby-title-row {
		display: flex;
		align-items: center;
	}

	.nearby-title {
		flex: 1;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 750;
	}

	.text-nav {
		display: flex;
		align-items: center;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 20rpx;
	}

	.text-nav .proto-icon {
		margin-left: 4rpx;
	}

	.nearby-address {
		display: flex;
		align-items: center;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.nearby-address .proto-icon {
		flex: 0 0 auto;
		margin-right: 4rpx;
	}
	.nearby-tags {
		display: flex;
		gap: 10rpx;
		margin-top: 14rpx;
		overflow: hidden;
		white-space: nowrap;
	}

	.nearby-tags text {
		padding: 7rpx 12rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 8rpx;
		font-size: 18rpx;
	}

	.nearby-price-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 20rpx;
	}

	.nearby-price {
		color: var(--proto-error);
		font-size: 38rpx;
		font-weight: 800;
	}

	.nearby-price .small {
		color: var(--proto-muted);
		font-size: 18rpx;
		font-weight: 400;
	}
</style>
