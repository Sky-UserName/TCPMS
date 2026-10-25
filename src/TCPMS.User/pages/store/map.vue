<template>
	<view class="proto-page map-page proto-page-plain">
		<ProtoHeader title="门店地图" theme="green" />
		<view class="map-notice">
			<view><ProtoIcon name="location" :size="22" /><text>{{ amapReady ? '高德地图定位服务' : '微信原生地图' }}</text></view>
			<text class="map-city">{{ mapCity }}</text>
			<button hover-class="none" @tap="viewList"><ProtoIcon name="document" :size="22" /><text>列表视图</text></button>
		</view>
		<view class="map-coordinate-notice">
			<view><ProtoIcon name="info" :size="20" /><text>{{ noticeText }}</text></view>
			<ProtoIcon name="close" :size="20" @tap="viewList" />
		</view>
		<view class="map-canvas">
			<map
				v-if="!searchVisible"
				id="storeMap"
				class="amap-canvas"
				:latitude="center.latitude"
				:longitude="center.longitude"
				:scale="scale"
				:markers="markers"
				:show-location="hasUserLocation"
				@markertap="onMarkerTap"
				@callouttap="onMarkerTap"
				@regionchange="onRegionChange"
				@error="onMapError"
			></map>
			<view v-if="mapError" class="map-error-notice"><ProtoIcon name="info" :size="20" /><text>地图渲染失败：{{ mapError }}</text></view>
			<view v-if="!searchVisible" class="map-controls">
				<button hover-class="none" aria-label="放大" @tap="zoomIn"><ProtoIcon name="plus" :size="22" /></button>
				<button hover-class="none" aria-label="缩小" @tap="zoomOut"><ProtoIcon name="minus" :size="22" /></button>
				<button hover-class="none" aria-label="定位" @tap="locateUser"><ProtoIcon name="navigation" :size="22" /></button>
			</view>
			<view v-if="!searchVisible" class="map-search" @tap="openSearch">
				<ProtoIcon name="search" :size="24" />
				<text>{{ searchLabel || '搜索此区域门店' }}</text>
			</view>

			<view v-if="searchVisible" class="map-search-panel">
				<view class="map-search-bar">
					<view class="map-search-input">
						<ProtoIcon name="search" :size="24" />
						<input
							v-model="searchKeyword"
							:focus="true"
							confirm-type="search"
							placeholder="搜索地标、商圈或门店名称"
							placeholder-class="search-placeholder"
							@confirm="runSearch"
						/>
					</view>
					<text class="map-search-cancel" @tap="closeSearch">取消</text>
				</view>
				<scroll-view scroll-y class="map-search-results">
					<view v-if="searching" class="map-search-tip">正在搜索…</view>
					<view v-else-if="searchError" class="map-search-tip">{{ searchError }}</view>
					<view v-else-if="!searchResults.length" class="map-search-tip">输入关键字后点击键盘“搜索”</view>
					<view v-else>
						<view
							v-for="place in searchResults"
							:key="place.id"
							class="map-search-result"
							@tap="pickPlace(place)"
						>
							<text class="result-name">{{ place.name }}</text>
							<text class="result-address">{{ place.district }}{{ place.address || '暂无详细地址' }}</text>
						</view>
					</view>
				</scroll-view>
			</view>
		</view>
		<view v-if="selectedStore" class="map-store-card">
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
		<button class="map-all-button" hover-class="none" @tap="viewList"><ProtoIcon name="document" :size="22" /><text>查看完整门店列表（共{{ allStores.length }}家）</text></button>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoStores, goPage, showToast } from '@/common/prototype.js'
	import { fetchStores, fetchNearbyStores } from '@/common/api.js'
	import { isAmapReady, reverseGeocode, searchPlaces } from '@/common/amap.js'
	import { ALLOW_DEMO_DATA } from '@/common/env.js'

	const RADIUS_KM = 50

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

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				allStores: ALLOW_DEMO_DATA ? demoStores : [],
				stores: ALLOW_DEMO_DATA ? demoStores : [],
				markers: [],
				markerStores: {},
				selectedStore: ALLOW_DEMO_DATA ? (demoStores[0] || null) : null,
				center: { latitude: 34.74725, longitude: 113.62493 },
				scale: 11,
				// 用户拖动后的视野，只在重建 map 时回填，避免和绑定值互相触发形成循环。
				restoreCenter: null,
				restoreScale: 11,
				mapError: '',
				userLocation: null,
				currentCity: '',
				amapReady: false,
				noticeText: '正在加载门店…',
				searchVisible: false,
				searchKeyword: '',
				searchLabel: '',
				searchResults: [],
				searching: false,
				searchError: ''
			}
		},
		computed: {
			mapCity() {
				return this.currentCity || (this.selectedStore && this.selectedStore.city) || '全城门店'
			},
			hasUserLocation() {
				return Boolean(this.userLocation)
			}
		},
		async onLoad(options) {
			const requestedId = options && options.id ? options.id : ''
			await this.loadStores(requestedId)
			this.amapReady = await isAmapReady()
			this.requestLocation()
		},
		methods: {
			async loadStores(requestedId) {
				let list = ALLOW_DEMO_DATA ? demoStores : []
				try {
					const remote = await fetchStores()
					if (remote.length) list = remote
				} catch (error) {
					// 开发环境保留演示数据方便预览；生产环境保持空列表并提示，不展示假门店。
					if (!ALLOW_DEMO_DATA) list = []
				}
				this.allStores = list.filter((store) => toPoint(store))
				this.stores = this.allStores
				this.selectedStore = this.stores.find((store) => String(store.id) === String(requestedId))
					|| this.stores[0]
					|| null
				this.buildMarkers()
				this.centerOnStores()
				this.noticeText = this.stores.length
					? `已加载 ${this.stores.length} 家营业门店，可点击价格标记查看详情`
					: '暂时没有可展示的门店，请检查网络后重试'
			},
			buildMarkers() {
				const selectedId = this.selectedStore ? String(this.selectedStore.id) : ''
				this.markerStores = {}
				this.markers = this.stores.reduce((result, store, index) => {
					const point = toPoint(store)
					if (!point) return result
					const markerId = index + 1
					this.markerStores[markerId] = store
					const selected = String(store.id) === selectedId
					result.push({
						id: markerId,
						latitude: point.latitude,
						longitude: point.longitude,
						// 默认图标也必须给宽高，否则基础库渲染层会报 marker 缺尺寸。
						width: 24,
						height: 34,
						callout: {
							content: `¥${store.price || '--'}`,
							color: selected ? '#ffffff' : '#0b6b6b',
							fontSize: 11,
							borderRadius: 9,
							borderWidth: 1,
							borderColor: selected ? '#0f6b6b' : '#d8e6e6',
							bgColor: selected ? '#0f6b6b' : '#ffffff',
							padding: 5,
							display: 'ALWAYS',
							textAlign: 'center'
						}
					})
					return result
				}, [])
			},
			centerOnStores() {
				if (!this.stores.length) return
				const total = this.stores.reduce((acc, store) => {
					const point = toPoint(store)
					acc.latitude += point.latitude
					acc.longitude += point.longitude
					return acc
				}, { latitude: 0, longitude: 0 })
				this.center = {
					latitude: total.latitude / this.stores.length,
					longitude: total.longitude / this.stores.length
				}
				this.scale = 11
			},
			requestLocation() {
				uni.getLocation({
					type: 'gcj02',
					isHighAccuracy: true,
					success: (result) => {
						this.userLocation = { latitude: result.latitude, longitude: result.longitude }
						this.loadNearby(result.latitude, result.longitude)
						this.loadAddress(result.latitude, result.longitude)
					},
					fail: () => {
						// 用户拒绝授权时保持门店中心，不阻塞地图浏览。
					}
				})
			},
			async loadNearby(latitude, longitude) {
				try {
					const nearby = (await fetchNearbyStores(latitude, longitude, RADIUS_KM))
						.filter((store) => toPoint(store))
					if (!nearby.length) {
						this.noticeText = `附近 ${RADIUS_KM} 公里内暂无门店，已展示全部 ${this.allStores.length} 家`
						return
					}
					this.stores = nearby
					this.selectedStore = nearby[0]
					this.buildMarkers()
					this.center = { latitude, longitude }
					this.scale = 12
					this.noticeText = `附近 ${RADIUS_KM} 公里内 ${nearby.length} 家营业门店`
				} catch (error) {
					this.noticeText = `已加载 ${this.stores.length} 家营业门店，可点击价格标记查看详情`
				}
			},
			async loadAddress(latitude, longitude) {
				try {
					const result = await reverseGeocode(latitude, longitude)
					this.currentCity = result.city
				} catch (error) {
					// 高德未配置时静默失败，城市名回退到门店所在城市。
					this.currentCity = ''
				}
			},
			onMarkerTap(event) {
				const detail = (event && event.detail) || {}
				const store = this.markerStores[Number(detail.markerId)]
				if (!store) return
				this.selectedStore = store
				this.buildMarkers()
			},
			// 只记录视野，不回写绑定值：绑定值一变地图会重新定位，再触发 regionchange 就成了死循环。
			onRegionChange(event) {
				const detail = (event && event.detail) || {}
				const phase = detail.type || (event && event.type)
				if (phase !== 'end') return
				const center = detail.centerLocation
				if (center && Number.isFinite(center.latitude) && Number.isFinite(center.longitude)) {
					this.restoreCenter = { latitude: center.latitude, longitude: center.longitude }
				}
				if (Number.isFinite(detail.scale)) this.restoreScale = detail.scale
			},
			onMapError(event) {
				const detail = (event && event.detail) || {}
				this.mapError = detail.errMsg || '未知错误'
			},
			viewList() {
				uni.switchTab({ url: '/pages/store/list', fail: () => uni.reLaunch({ url: '/pages/store/list' }) })
			},
			goDetail() {
				if (!this.selectedStore) return
				goPage(`/pages/store/detail?id=${this.selectedStore.id}`)
			},
			callStore() {
				const phoneNumber = (this.selectedStore && this.selectedStore.phone) || '037188886622'
				uni.makePhoneCall({
					phoneNumber,
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			openLocation() {
				const point = toPoint(this.selectedStore)
				if (!point) {
					showToast('该门店暂无坐标信息')
					return
				}
				uni.openLocation({
					latitude: point.latitude,
					longitude: point.longitude,
					name: this.selectedStore.name,
					address: this.selectedStore.address,
					fail: () => showToast('地图服务暂不可用')
				})
			},
			zoomIn() {
				this.scale = Math.min(20, this.scale + 1)
			},
			zoomOut() {
				this.scale = Math.max(3, this.scale - 1)
			},
			locateUser() {
				uni.getLocation({
					type: 'gcj02',
					isHighAccuracy: true,
					success: (result) => {
						this.userLocation = { latitude: result.latitude, longitude: result.longitude }
						this.center = { latitude: result.latitude, longitude: result.longitude }
						this.scale = 14
						this.loadNearby(result.latitude, result.longitude)
						this.loadAddress(result.latitude, result.longitude)
						showToast('已回到当前位置')
					},
					fail: () => showToast('未开启定位权限，请在设置中允许位置信息')
				})
			},
			openSearch() {
				if (this.restoreCenter) this.center = this.restoreCenter
				this.restoreScale = this.scale
				this.searchVisible = true
				this.searchError = ''
				this.searchResults = []
			},
			closeSearch() {
				// 地图在搜索期间被卸载，关闭前把视野补回去，重新挂载时不会跳回初始位置。
				if (this.restoreCenter) this.center = this.restoreCenter
				this.scale = this.restoreScale
				this.searchVisible = false
				this.searchKeyword = ''
				this.searchResults = []
				this.searchError = ''
			},
			async runSearch() {
				const keyword = this.searchKeyword.trim()
				if (!keyword) return
				this.searching = true
				this.searchError = ''
				try {
					const results = await searchPlaces(keyword, this.currentCity)
					this.searchResults = results
					if (!results.length) this.searchError = '没有找到匹配的地点'
				} catch (error) {
					this.searchResults = []
					this.searchError = (error && error.message) || '高德搜索失败，请检查 Key 与合法域名'
				} finally {
					this.searching = false
				}
			},
			async pickPlace(place) {
				this.searchVisible = false
				this.searchKeyword = ''
				this.searchResults = []
				this.searchLabel = place.name
				this.center = { latitude: place.latitude, longitude: place.longitude }
				this.scale = 14
				this.loadAddress(place.latitude, place.longitude)
				await this.loadNearby(place.latitude, place.longitude)
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
	}

	.amap-canvas {
		width: 100%;
		height: 860rpx;
	}

	.map-error-notice {
		position: absolute;
		top: 130rpx;
		left: 26rpx;
		right: 26rpx;
		z-index: 12;
		display: flex;
		align-items: center;
		padding: 16rpx 22rpx;
		color: #8a3b1f;
		background: #ffe9dd;
		border-radius: 14rpx;
		font-size: 20rpx;
	}

	.map-error-notice .proto-icon {
		margin-right: 8rpx;
		flex: 0 0 auto;
	}

	.map-controls {
		position: absolute;
		top: 220rpx;
		right: 24rpx;
		z-index: 10;
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
		z-index: 10;
		padding: 16rpx 28rpx;
		color: var(--proto-text);
		background: #ffffff;
		border-radius: 34rpx;
		font-size: 23rpx;
		transform: translateX(-50%);
		box-shadow: 0 8rpx 20rpx rgba(32, 37, 43, 0.1);
	}

	.map-search-panel {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 20;
		display: flex;
		flex-direction: column;
		background: #ffffff;
	}

	.map-search-bar {
		display: flex;
		align-items: center;
		gap: 16rpx;
		padding: 24rpx 26rpx;
		border-bottom: 1rpx solid #e4ebec;
	}

	.map-search-input {
		display: flex;
		align-items: center;
		flex: 1;
		height: 72rpx;
		padding: 0 22rpx;
		background: #f1f5f5;
		border-radius: 36rpx;
	}

	.map-search-input input {
		flex: 1;
		margin-left: 10rpx;
		color: var(--proto-text);
		font-size: 24rpx;
	}

	.map-search-input .proto-icon {
		flex: 0 0 auto;
	}

	.search-placeholder {
		color: var(--proto-muted);
		font-size: 23rpx;
	}

	.map-search-cancel {
		color: var(--proto-primary-dark);
		font-size: 24rpx;
		font-weight: 700;
	}

	.map-search-results {
		flex: 1;
	}

	.map-search-tip {
		padding: 60rpx 0;
		color: var(--proto-muted);
		font-size: 23rpx;
		text-align: center;
	}

	.map-search-result {
		padding: 24rpx 30rpx;
		border-bottom: 1rpx solid #f0f4f4;
	}

	.result-name {
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 700;
	}

	.result-address {
		display: block;
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
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
