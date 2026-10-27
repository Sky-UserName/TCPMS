<template>
	<view class="location-picker">
		<view class="picker-nav" :style="{ paddingTop: statusBarHeightRpx + 'rpx', height: 88 + statusBarHeightRpx + 'rpx' }">
			<text class="nav-action" @tap="close">取消</text>
			<text class="nav-title">选择位置</text>
			<view class="nav-action nav-action-ghost"></view>
		</view>

		<view class="picker-search">
			<view class="search-box">
				<ProtoIcon name="search" :size="30" />
				<input
					v-model="keyword"
					class="search-input"
					type="text"
					confirm-type="search"
					placeholder="搜索地点"
					placeholder-class="search-placeholder"
					@confirm="runSearch"
				/>
				<view v-if="keyword" class="search-clear" @tap="clearSearch">
					<ProtoIcon name="close" :size="16" />
				</view>
			</view>
		</view>

		<view class="picker-map">
			<map
				id="locationPickerMap"
				class="picker-map-canvas"
				:latitude="mapCenter.latitude"
				:longitude="mapCenter.longitude"
				:scale="mapScale"
				:show-location="hasUserLocation"
				:enable-rotate="false"
				:enable-overlooking="false"
				:enable-poi="true"
				@regionchange="onRegionChange"
				@error="onMapError"
			></map>

			<view class="map-pin">
				<image class="map-pin-image" src="/static/prototype-icons/map-pin.png" mode="aspectFit" />
			</view>

			<button class="map-locate" hover-class="none" aria-label="回到当前位置" @tap="locate()">
				<ProtoIcon name="navigation" tone="white" :size="30" />
			</button>

			<view v-if="mapError" class="map-notice">
				<ProtoIcon name="info" :size="20" />
				<text>地图加载异常，可用搜索选择地点</text>
			</view>
			<view v-else-if="locateTip" class="map-notice map-notice-info">
				<ProtoIcon name="info" :size="20" />
				<text>{{ locateTip }}</text>
			</view>
			<view v-else-if="refreshing" class="map-loading"><text>正在获取周边地点…</text></view>
		</view>

		<view class="picker-panel">
			<scroll-view scroll-y class="panel-scroll">
				<view v-if="searching" class="panel-tip"><text>正在搜索「{{ keyword }}」…</text></view>
				<view v-else-if="!displayList.length" class="panel-empty">
					<ProtoIcon name="search" :size="46" />
					<text class="panel-empty-text">{{ emptyText }}</text>
				</view>
				<view v-else class="poi-list">
					<view
						v-for="item in displayList"
						:key="item.key"
						class="poi-item"
						:class="{ active: item.active }"
						@tap="pickPoi(item)"
					>
						<view class="poi-main">
							<text class="poi-name">{{ item.name }}</text>
							<text class="poi-address">{{ item.addressText || '暂无详细地址' }}</text>
						</view>
						<view class="poi-side">
							<text v-if="item.distanceText" class="poi-distance">{{ item.distanceText }}</text>
							<text v-if="item.active" class="poi-flag">已选</text>
						</view>
					</view>
				</view>
			</scroll-view>
		</view>

		<view class="picker-footer">
			<view class="footer-summary">
				<text class="footer-region">{{ selectionRegion || '当前位置' }}</text>
				<text class="footer-detail">{{ footerDetail }}</text>
			</view>
			<button class="footer-confirm" hover-class="footer-confirm-active" @tap="confirm">确定</button>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { reverseGeocode, searchAroundPois, searchPlaces } from '@/common/amap.js'
	// 距离计算与格式化统一在 common/geo.js，首页「距您 x km」用的是同一套实现。
	import { distanceMeters as distanceBetween, formatDistance } from '@/common/geo.js'
	import { showToast } from '@/common/prototype.js'

	// 定位失败时的兜底中心，和其他地图页面保持一致（郑州）。
	const DEFAULT_CENTER = { latitude: 34.74725, longitude: 113.62493 }
	const NEARBY_RADIUS = 1200
	const NEARBY_LIMIT = 20
	const MOVE_TOLERANCE = 0.00002

	const readNumber = (value) => {
		const number = Number(value)
		return Number.isFinite(number) ? number : null
	}

	const decodeValue = (value = '') => {
		try {
			return decodeURIComponent(String(value))
		} catch (error) {
			return String(value)
		}
	}

	// 拖动后地图中心会抖动几米，用容差判断是否真的换了位置，避免反复请求。
	const samePoint = (left, right, tolerance = MOVE_TOLERANCE) => {
		if (!left || !right) return false
		return Math.abs(Number(left.latitude) - Number(right.latitude)) <= tolerance
			&& Math.abs(Number(left.longitude) - Number(right.longitude)) <= tolerance
	}

	// 高德把省市区和街道门牌分开返回，列表里要拼成完整地址，省市区重名（直辖市）只保留一次。
	const composeAddress = (poi) => {
		if (!poi) return ''
		const detail = String(poi.address || '').trim()
		const parts = [poi.province, poi.city, poi.district]
		let prefix = ''
		parts.forEach((raw) => {
			const value = String(raw || '').trim()
			if (!value || prefix.endsWith(value) || detail.startsWith(value)) return
			prefix += value
		})
		return `${prefix}${detail}` || prefix
	}

	// 直辖市 province 和 city 都是「北京市」，拼省市区时只留一次。
	const buildRegionLabel = (region) => {
		if (!region) return ''
		const parts = []
		;[region.province, region.city, region.district].forEach((raw) => {
			const value = String(raw || '').trim()
			if (!value || parts.includes(value)) return
			parts.push(value)
		})
		return parts.join('')
	}

	// formatted_address 带省市前缀，回填「详细地址」时把前缀去掉，只留街道门牌。
	const stripRegionPrefix = (address, region) => {
		const origin = String(address || '').trim()
		let value = origin
		if (region) {
			;[region.province, region.city, region.district].forEach((raw) => {
				const part = String(raw || '').trim()
				if (part && value.startsWith(part)) value = value.slice(part.length).trim()
			})
		}
		return value || origin
	}

	// 地点名（小区、写字楼）比门牌更好认，拼在门牌后面一起回填。
	const composeDetail = (name, address) => {
		const title = String(name || '').trim()
		const detail = String(address || '').trim()
		if (!detail) return title
		if (!title || detail.includes(title)) return detail
		return `${detail}${title}`
	}

	export default {
		components: { ProtoIcon },
		data() {
			let statusBarHeightRpx = 0
			try {
				const info = uni.getSystemInfoSync()
				statusBarHeightRpx = Math.round((Number(info.statusBarHeight) || 0) * 750 / (Number(info.windowWidth) || 375))
			} catch (error) {
				statusBarHeightRpx = 0
			}
			return {
				statusBarHeightRpx,
				keyword: '',
				mode: 'nearby',
				searching: false,
				refreshing: false,
				locating: false,
				hasUserLocation: false,
				mapError: '',
				locateTip: '',
				mapCenter: { ...DEFAULT_CENTER },
				mapScale: 15,
				pinPoint: { ...DEFAULT_CENTER },
				mapPoint: null,
				selectedPoi: null,
				nearbyPois: [],
				searchResults: [],
				currentCity: '',
				initialPoint: null,
				initialLabel: '',
				emptyHint: '',
				addressToken: 0,
				nearbyToken: 0,
				refreshTimer: null,
				regionChangeLocked: false,
				regionLockTimer: null
			}
		},
		computed: {
			// 当前图钉指向的地点：优先用用户点选的地点，其次用地图中心逆地理编码的结果。
			// selectedPoi 只在用户拖动地图时才清空，这里不再比对坐标，
			// 否则地图程序化移动带来的几米偏移会把刚点选的地点丢掉。
			activePin() {
				if (this.selectedPoi) {
					return { ...this.selectedPoi, source: 'poi' }
				}
				const point = this.mapPoint && samePoint(this.mapPoint, this.pinPoint) ? this.mapPoint : null
				return {
					source: 'map',
					name: (point && point.name) || '',
					address: (point && point.address) || '',
					province: (point && point.province) || '',
					city: (point && point.city) || '',
					district: (point && point.district) || '',
					latitude: this.pinPoint.latitude,
					longitude: this.pinPoint.longitude
				}
			},
			displayList() {
				const source = this.mode === 'search' ? this.searchResults : this.nearbyPois
				const rows = source.map((poi) => ({
					...poi,
					key: poi.id || `${poi.latitude},${poi.longitude}`,
					addressText: composeAddress(poi),
					distanceText: formatDistance(poi.distance || distanceBetween(this.pinPoint, poi)),
					active: Boolean(this.selectedPoi && this.selectedPoi.id === poi.id)
				}))
				const pin = this.activePin
				if (this.mode !== 'nearby') return rows
				if (pin.source === 'map' && pin.address) {
					rows.unshift({
						key: '__map-pin__',
						name: pin.name || '地图选点',
						addressText: pin.address,
						distanceText: '',
						active: true,
						isPin: true,
						latitude: pin.latitude,
						longitude: pin.longitude
					})
				}
				// 搜索到远处地点后，周边列表里未必包含它，把当前选中的地点置顶，避免看不到选了什么。
				if (pin.source === 'poi' && !rows.some((row) => row.key === pin.id || samePoint(row, pin))) {
					rows.unshift({
						...pin,
						key: pin.id || '__selected-poi__',
						addressText: pin.fullAddress || composeAddress(pin),
						distanceText: '',
						active: true
					})
				}
				return rows
			},
			// 底部确认条展示的内容，和回填到申请页的数据同源。
			selectionRegion() {
				return buildRegionLabel(this.activePin)
			},
			selectionDetail() {
				const pin = this.activePin
				return composeDetail(pin.name, stripRegionPrefix(pin.address, pin))
			},
			footerDetail() {
				if (this.selectionDetail) return this.selectionDetail
				if (this.refreshing) return '正在解析当前位置…'
				return '拖动地图或点击列表选择地点'
			},
			emptyText() {
				if (this.mode === 'search') return `没有找到「${this.keyword}」相关地点，换个关键词试试`
				return this.emptyHint || '周边暂无地点，可拖动地图或搜索地点名称'
			}
		},
		onLoad(options) {
			const query = options || {}
			const latitude = readNumber(query.latitude)
			const longitude = readNumber(query.longitude)
			const validPoint = latitude !== null && longitude !== null
				&& Math.abs(latitude) <= 90 && Math.abs(longitude) <= 180
				&& !(latitude === 0 && longitude === 0)

			if (validPoint) {
				// 从申请页带着已选坐标进来时，直接停在原位置，并把原来的地点名保留下来。
				this.initialPoint = { latitude, longitude }
				this.initialLabel = decodeValue(query.name)
				this.pinPoint = { ...this.initialPoint }
				this.mapCenter = { ...this.initialPoint }
				this.mapScale = 16
				this.mapPoint = {
					...this.initialPoint,
					name: this.initialLabel,
					address: decodeValue(query.address),
					province: decodeValue(query.province),
					city: decodeValue(query.city),
					district: decodeValue(query.district)
				}
				this.refreshAt(this.initialPoint)
				return
			}
			this.locate(true)
		},
		onUnload() {
			this.clearRefreshTimer()
			if (this.regionLockTimer) {
				clearTimeout(this.regionLockTimer)
				this.regionLockTimer = null
			}
		},
		methods: {
			clearRefreshTimer() {
				if (this.refreshTimer) {
					clearTimeout(this.refreshTimer)
					this.refreshTimer = null
				}
			},
			// 点列表、点定位按钮都会改地图中心，同样会触发 regionchange。
			// 短时间内忽略这个事件，避免被当成「用户拖动地图」把刚选中的地点清掉。
			suppressRegionChange() {
				this.regionChangeLocked = true
				if (this.regionLockTimer) clearTimeout(this.regionLockTimer)
				this.regionLockTimer = setTimeout(() => {
					this.regionLockTimer = null
					this.regionChangeLocked = false
				}, 500)
			},
			close() {
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/merchant/apply' })
				})
			},
			async locate(auto = false) {
				if (this.locating) return
				this.locating = true
				const position = await new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						isHighAccuracy: true,
						success: (result) => resolve(result),
						fail: (error) => {
							console.warn('[地图选点] 定位失败：', error && (error.errMsg || error.message || error))
							resolve(null)
						}
					})
				})
				this.locating = false

				const latitude = position ? readNumber(position.latitude) : null
				const longitude = position ? readNumber(position.longitude) : null
				if (latitude === null || longitude === null) {
					if (!auto) showToast('未获取到定位，可拖动地图或搜索地点')
					this.locateTip = '未获取到定位，可搜索地点或拖动地图选点'
					if (!this.mapPoint) {
						this.pinPoint = { ...DEFAULT_CENTER }
						this.mapCenter = { ...DEFAULT_CENTER }
						this.mapScale = 13
						this.refreshAt(this.pinPoint)
					}
					return
				}

				this.hasUserLocation = true
				this.locateTip = ''
				this.initialPoint = null
				this.initialLabel = ''
				this.selectedPoi = null
				const point = { latitude, longitude }
				this.suppressRegionChange()
				this.pinPoint = point
				this.mapCenter = { ...point }
				this.mapScale = 16
				this.refreshAt(point)
			},
			onMapError(event) {
				const detail = (event && event.detail) || {}
				this.mapError = detail.errMsg || detail.message || '未知错误'
			},
			onRegionChange(event) {
				const detail = (event && event.detail) || {}
				const phase = detail.type || (event && event.type)
				if (phase !== 'end') return
				// 刚才是代码在移动地图（点列表 / 点定位），不算用户拖动，保留当前选中项。
				if (this.regionChangeLocked) return
				if (Number.isFinite(detail.scale) && detail.scale > 0) this.mapScale = detail.scale
				const center = detail.centerLocation
				if (center && readNumber(center.latitude) !== null && readNumber(center.longitude) !== null) {
					this.handleMapCenter({ latitude: Number(center.latitude), longitude: Number(center.longitude) })
					return
				}
				this.readMapCenter()
			},
			// 部分平台 regionchange 不带中心点，兜底问一次地图实例。
			readMapCenter() {
				if (typeof uni.createMapContext !== 'function') return
				uni.createMapContext('locationPickerMap', this).getCenterLocation({
					success: (result) => {
						const latitude = readNumber(result && result.latitude)
						const longitude = readNumber(result && result.longitude)
						if (latitude === null || longitude === null) return
						this.handleMapCenter({ latitude, longitude })
					},
					fail: () => {}
				})
			},
			handleMapCenter(center) {
				this.mapCenter = { ...center }
				if (samePoint(center, this.pinPoint)) return
				// 地图动画结束后中心偶尔有几十米内的偏差，别把刚点选的地点清掉。
				if (this.selectedPoi && samePoint(center, this.selectedPoi, 0.0002)) return
				this.pinPoint = { ...center }
				this.selectedPoi = null
				this.locateTip = ''
				if (this.mode === 'search') {
					this.mode = 'nearby'
					this.keyword = ''
					this.searchResults = []
				}
				this.clearRefreshTimer()
				this.refreshTimer = setTimeout(() => {
					this.refreshTimer = null
					this.refreshAt(this.pinPoint)
				}, 320)
			},
			refreshAt(point) {
				if (!point) return
				this.loadNearby(point)
				this.resolveAddress(point)
			},
			async loadNearby(point) {
				const token = ++this.nearbyToken
				this.refreshing = true
				try {
					const list = await searchAroundPois(point.latitude, point.longitude, {
						radius: NEARBY_RADIUS,
						limit: NEARBY_LIMIT
					})
					if (token !== this.nearbyToken) return
					this.nearbyPois = list
					this.emptyHint = list.length ? '' : '周边暂无地点，可拖动地图或搜索地点名称'
				} catch (error) {
					if (token !== this.nearbyToken) return
					this.nearbyPois = []
					this.emptyHint = '地点服务暂不可用，可直接拖动地图后点击确定'
				} finally {
					if (token === this.nearbyToken) this.refreshing = false
				}
			},
			async resolveAddress(point) {
				const token = ++this.addressToken
				try {
					const info = await reverseGeocode(point.latitude, point.longitude)
					if (token !== this.addressToken) return
					const keepLabel = this.initialLabel && samePoint(this.initialPoint, point)
					this.mapPoint = {
						latitude: point.latitude,
						longitude: point.longitude,
						name: keepLabel ? this.initialLabel : '',
						address: info.formattedAddress || '',
						province: info.province || '',
						city: info.city || '',
						district: info.district || ''
					}
					if (info.city) this.currentCity = info.city
				} catch (error) {
					if (token !== this.addressToken) return
					this.mapPoint = {
						latitude: point.latitude,
						longitude: point.longitude,
						name: this.initialLabel && samePoint(this.initialPoint, point) ? this.initialLabel : '',
						address: '',
						province: '',
						city: '',
						district: ''
					}
				}
			},
			pickPoi(item) {
				if (!item || item.isPin) return
				const latitude = readNumber(item.latitude)
				const longitude = readNumber(item.longitude)
				if (latitude === null || longitude === null) return

				this.selectedPoi = {
					id: item.id,
					name: item.name,
					address: item.address,
					fullAddress: item.addressText,
					province: item.province,
					city: item.city,
					district: item.district,
					latitude,
					longitude
				}
				// 地图跟着图钉走是代码行为，别让它把自己刚选中的地点清掉。
				this.suppressRegionChange()
				this.pinPoint = { latitude, longitude }
				this.mapCenter = { latitude, longitude }
				this.mapScale = Math.max(this.mapScale, 16)
				this.locateTip = ''

				if (this.mode === 'search') {
					this.mode = 'nearby'
					this.keyword = ''
					this.searchResults = []
					this.loadNearby(this.pinPoint)
				}
			},
			async runSearch() {
				const keyword = String(this.keyword || '').trim()
				if (!keyword) {
					this.mode = 'nearby'
					this.searchResults = []
					return
				}
				if (this.searching) return
				this.searching = true
				this.mode = 'search'
				try {
					let list = await searchPlaces(keyword, this.currentCity)
					// 当前城市限制过严时（比如搜外地的地标）再放开城市限制兜底一次。
					if (!list.length && this.currentCity) list = await searchPlaces(keyword, '')
					this.searchResults = list
				} catch (error) {
					this.searchResults = []
					showToast((error && error.message) || '搜索失败，请稍后重试')
				} finally {
					this.searching = false
				}
			},
			clearSearch() {
				this.keyword = ''
				this.mode = 'nearby'
				this.searchResults = []
				this.searching = false
			},
			confirm() {
				const pin = this.activePin
				const latitude = readNumber(pin.latitude)
				const longitude = readNumber(pin.longitude)
				if (latitude === null || longitude === null) {
					showToast('请先在地图上选择位置')
					return
				}

				const region = {
					province: String(pin.province || '').trim(),
					city: String(pin.city || '').trim(),
					district: String(pin.district || '').trim()
				}
				const regionLabel = buildRegionLabel(region)
				const fullAddress = pin.source === 'poi'
					? (pin.fullAddress || composeAddress(pin) || '')
					: String(pin.address || '').trim()
				// 详细地址去掉省市前缀，只保留街道门牌，避免和「民宿位置」的省市区重复。
				const detail = composeDetail(pin.name, stripRegionPrefix(fullAddress, region))

				if (!regionLabel && !detail) {
					showToast(this.refreshing ? '正在解析地址，请稍候' : '未获取到地址，请改用搜索选择地点')
					return
				}

				const payload = {
					name: pin.name || '',
					region: regionLabel,
					detail: detail || regionLabel,
					address: fullAddress || detail || regionLabel,
					province: region.province,
					city: region.city,
					district: region.district,
					latitude,
					longitude
				}

				const channel = typeof this.getOpenerEventChannel === 'function' ? this.getOpenerEventChannel() : null
				if (channel && typeof channel.emit === 'function') {
					channel.emit('locationSelected', payload)
				}
				// 再发一次全局事件兜底：部分平台不支持 navigateTo 的 events 参数。
				uni.$emit('merchant:location-selected', payload)
				this.close()
			}
		}
	}
</script>

<style lang="scss" scoped>
	.location-picker {
		display: flex;
		flex-direction: column;
		height: 100vh;
		overflow: hidden;
		background: #ffffff;
	}

	.picker-nav {
		flex: 0 0 auto;
		display: flex;
		align-items: center;
		justify-content: space-between;
		box-sizing: border-box;
		padding: 0 28rpx;
		background: #ffffff;
	}

	.nav-action {
		flex: 0 0 auto;
		width: 96rpx;
		padding: 12rpx 0;
		color: #1f2429;
		font-size: 30rpx;
		line-height: 1.2;
	}

	.nav-action-ghost {
		height: 12rpx;
	}

	.nav-title {
		flex: 1;
		color: #1f2429;
		font-size: 30rpx;
		font-weight: 700;
		text-align: center;
	}

	.picker-search {
		flex: 0 0 auto;
		padding: 6rpx 24rpx 20rpx;
		background: #ffffff;
	}

	.search-box {
		display: flex;
		align-items: center;
		height: 72rpx;
		padding: 0 20rpx;
		background: #f2f4f5;
		border-radius: 10rpx;
	}

	.search-input {
		flex: 1;
		min-width: 0;
		height: 72rpx;
		margin-left: 12rpx;
		color: #1f2429;
		font-size: 28rpx;
	}

	.search-placeholder {
		color: #9aa1a7;
		font-size: 28rpx;
	}

	.search-clear {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 auto;
		width: 34rpx;
		height: 34rpx;
		margin-left: 10rpx;
		background: #c6ccd2;
		border-radius: 50%;
	}

	.picker-map {
		position: relative;
		flex: 1;
		min-height: 0;
		overflow: hidden;
		background: #e9edf0;
	}

	.picker-map-canvas {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		width: 100%;
		height: 100%;
	}

	.map-pin {
		position: absolute;
		top: 50%;
		left: 50%;
		z-index: 5;
		width: 66rpx;
		height: 88rpx;
		pointer-events: none;
		transform: translate(-50%, -90%);
	}

	.map-pin-image {
		width: 100%;
		height: 100%;
	}

	.map-locate {
		position: absolute;
		left: 24rpx;
		bottom: 120rpx;
		z-index: 6;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 76rpx;
		height: 76rpx;
		background: var(--proto-primary-dark, #006a6a);
		border-radius: 50%;
		box-shadow: 0 8rpx 18rpx rgba(6, 56, 56, 0.28);
	}

	.map-locate:active {
		opacity: 0.82;
	}

	.map-notice {
		position: absolute;
		top: 20rpx;
		right: 24rpx;
		left: 24rpx;
		z-index: 6;
		display: flex;
		align-items: center;
		gap: 8rpx;
		padding: 14rpx 20rpx;
		color: #8a5a13;
		background: #fff5e8;
		border-radius: 12rpx;
		font-size: 21rpx;
	}

	.map-notice-info {
		color: #4d5b62;
		background: rgba(255, 255, 255, 0.96);
		box-shadow: 0 6rpx 16rpx rgba(31, 61, 74, 0.12);
	}

	.map-loading {
		position: absolute;
		left: 50%;
		bottom: 28rpx;
		z-index: 6;
		padding: 10rpx 24rpx;
		color: #ffffff;
		background: rgba(32, 37, 43, 0.7);
		border-radius: 28rpx;
		font-size: 20rpx;
		transform: translateX(-50%);
	}

	.picker-panel {
		box-sizing: border-box;
		flex: 0 0 auto;
		height: 34vh;
		min-height: 340rpx;
		background: #ffffff;
		border-top: 1rpx solid #e9edef;
	}

	.panel-scroll {
		height: 100%;
	}

	.poi-item {
		position: relative;
		display: flex;
		align-items: center;
		gap: 16rpx;
		padding: 24rpx 28rpx;
		border-bottom: 1rpx solid #f1f3f4;
	}

	.poi-item.active {
		background: #f1faf9;
	}

	.poi-item.active::before {
		position: absolute;
		top: 0;
		bottom: 0;
		left: 0;
		width: 6rpx;
		background: var(--proto-primary-dark, #006a6a);
		content: '';
	}

	.poi-item:active {
		background: #f7f9fa;
	}

	.poi-main {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.poi-name {
		overflow: hidden;
		color: #1c2126;
		font-size: 28rpx;
		line-height: 1.35;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.poi-item.active .poi-name {
		color: var(--proto-primary-dark, #006a6a);
		font-weight: 700;
	}

	.poi-address {
		margin-top: 8rpx;
		overflow: hidden;
		color: #8d959c;
		font-size: 22rpx;
		line-height: 1.35;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.poi-distance {
		flex: 0 0 auto;
		color: #a2a9b0;
		font-size: 21rpx;
	}

	.poi-side {
		display: flex;
		align-items: center;
		gap: 10rpx;
		flex: 0 0 auto;
	}

	.poi-flag {
		padding: 4rpx 12rpx;
		color: var(--proto-primary-dark, #006a6a);
		background: #ddf2f0;
		border-radius: 20rpx;
		font-size: 20rpx;
		line-height: 1.4;
	}

	.panel-tip {
		padding: 70rpx 0;
		color: #8d959c;
		font-size: 24rpx;
		text-align: center;
	}

	.panel-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		padding: 70rpx 48rpx;
	}

	.panel-empty-text {
		margin-top: 18rpx;
		color: #9aa1a8;
		font-size: 24rpx;
		line-height: 1.6;
		text-align: center;
	}

	.picker-footer {
		flex: 0 0 auto;
		display: flex;
		align-items: center;
		gap: 20rpx;
		box-sizing: border-box;
		padding: 18rpx 24rpx;
		padding-bottom: calc(18rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(18rpx + env(safe-area-inset-bottom));
		background: #ffffff;
		border-top: 1rpx solid #e9edef;
		box-shadow: 0 -6rpx 18rpx rgba(31, 61, 74, 0.06);
	}

	.footer-summary {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.footer-region {
		display: block;
		overflow: hidden;
		color: #8d959c;
		font-size: 21rpx;
		line-height: 1.3;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.footer-detail {
		display: block;
		margin-top: 6rpx;
		overflow: hidden;
		color: #1c2126;
		font-size: 26rpx;
		font-weight: 700;
		line-height: 1.3;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.footer-confirm {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 auto;
		width: 200rpx;
		height: 76rpx;
		padding: 0;
		color: #ffffff;
		background: var(--proto-primary-dark, #006a6a);
		border-radius: 38rpx;
		font-size: 28rpx;
		font-weight: 700;
	}

	.footer-confirm-active {
		opacity: 0.82;
	}
</style>
