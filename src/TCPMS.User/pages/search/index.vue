<template>
	<view class="proto-page proto-page-plain location-search-page">
		<ProtoHeader title="搜索" title-align="center" theme="green" />

		<view class="search-content">
			<view class="search-input-shell">
				<ProtoIcon name="search" :size="34" />
				<input
					v-model="keyword"
					class="search-input"
					type="text"
					confirm-type="search"
					placeholder="输入你要搜索的内容"
					@confirm="confirmKeyword"
				/>
				<button v-if="keyword" class="clear-keyword" hover-class="none" aria-label="清除搜索内容" @tap="clearKeyword">
					<ProtoIcon name="close" :size="24" />
				</button>
			</view>

			<view class="region-heading">
				<view class="region-heading-copy">
					<ProtoIcon name="location" :size="30" />
					<text class="region-heading-title">区域选择</text>
				</view>
				<button class="region-city-button" hover-class="none" @tap="openCityPicker">
					<text>{{ activeCity || '全部城市' }}</text>
					<ProtoIcon name="chevron-down" :size="18" />
				</button>
			</view>

			<view v-if="regionState === 'loading'" class="region-state">
				<text>正在加载 {{ activeCity }} 的区域…</text>
			</view>

			<view v-else-if="filteredAreas.length" class="region-grid">
				<text
					v-for="area in filteredAreas"
					:key="area"
					class="region-chip"
					:class="{ selected: selectedArea === area }"
					@tap="selectArea(area)"
				>{{ area }}</text>
			</view>

			<view v-else class="region-state region-state-empty">
				<text v-if="!activeCity || activeCity === '全部城市'">先选择城市，即可按区域筛选</text>
				<text v-else-if="keyword">没有匹配「{{ keyword }}」的区域</text>
				<text v-else>{{ activeCity }}暂未收录区域，可直接搜索门店名称或地址</text>
				<button v-if="!activeCity || activeCity === '全部城市'" class="region-state-action" hover-class="none" @tap="openCityPicker">
					选择城市
				</button>
			</view>

			<view class="search-hint">
				<ProtoIcon name="info" :size="22" />
				<text>区域数据按所选城市实时加载，选择后会带入门店列表筛选。</text>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { fetchDistricts } from '@/common/amap.js'
	import { getBooking, saveBooking } from '@/common/app-store.js'
	import { fetchStoreFilters } from '@/common/api.js'
	import { ALL_CITY_LABEL, DISTRICT_FALLBACK } from '@/common/city-data.js'
	import { showToast } from '@/common/prototype.js'

	const decodeValue = (value = '') => {
		try {
			return decodeURIComponent(String(value))
		} catch (error) {
			return String(value)
		}
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				keyword: '',
				activeCity: '',
				selectedArea: '',
				regionState: 'idle',
				areas: [],
				remoteAreas: {}
			}
		},
		computed: {
			filteredAreas() {
				const value = this.keyword.trim().toLowerCase()
				if (!value) return this.areas
				return this.areas.filter((area) => area.toLowerCase().indexOf(value) >= 0)
			}
		},
		async onLoad(options) {
			const booking = getBooking()
			this.activeCity = decodeValue(options && options.city) || booking.city || ''
			this.keyword = decodeValue(options && options.keyword) || booking.searchKeyword || ''
			this.selectedArea = decodeValue(options && options.region) || booking.region || ''

			// 后端门店覆盖的区域留作兜底：高德查不到时至少还能列出平台已有门店的区县。
			await this.loadRemoteAreas()
			await this.loadRegions()
		},
		onShow() {
			// 从「选择城市」页返回时城市可能已变，重新拉该城市的区域。
			const booking = getBooking()
			const city = booking.city || ''
			if (city && city !== this.activeCity) {
				this.activeCity = city
				this.selectedArea = ''
				this.loadRegions()
			}
		},
		methods: {
			async loadRemoteAreas() {
				try {
					const data = await fetchStoreFilters()
					const cities = Array.isArray(data && data.cities) ? data.cities : []
					const map = {}
					cities.forEach((item) => {
						if (!item || !item.city) return
						map[item.city] = Array.isArray(item.areas) ? item.areas.filter(Boolean) : []
					})
					this.remoteAreas = map
				} catch (error) {
					this.remoteAreas = {}
				}
			},
			async loadRegions() {
				const city = this.activeCity
				if (!city || city === ALL_CITY_LABEL) {
					this.areas = []
					this.regionState = 'ready'
					return
				}
				this.regionState = 'loading'
				let items = []
				try {
					// 高德行政区划：能拿到该城市全部区县，不依赖平台是否已在该城市开门店。
					items = await fetchDistricts(city)
				} catch (error) {
					console.warn('[搜索] 区域加载失败：', error && (error.message || error.errMsg || error))
					items = []
				}
				if (!items.length) items = DISTRICT_FALLBACK[city] || []
				if (!items.length) items = this.remoteAreas[city] || []
				// 保留已选区域：接口返回的顺序可能和本地兜底不一致，这里不做删除。
				this.areas = items
				this.regionState = 'ready'
			},
			openCityPicker() {
				uni.navigateTo({
					url: `/pages/city/select?city=${encodeURIComponent(this.activeCity || ALL_CITY_LABEL)}`
				})
			},
			selectArea(area) {
				this.selectedArea = area
				this.saveSelection(true)
			},
			clearKeyword() {
				this.keyword = ''
			},
			confirmKeyword() {
				if (!this.keyword.trim() && !this.selectedArea) {
					showToast('请输入关键词或选择区域')
					return
				}
				this.saveSelection(true)
			},
			saveSelection(goBack = false) {
				const booking = saveBooking({
					...getBooking(),
					city: this.activeCity,
					region: this.selectedArea,
					searchKeyword: this.keyword.trim()
				})
				if (!goBack) return booking
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/index/index' })
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.location-search-page {
		min-height: 100vh;
		padding-bottom: 56rpx;
		background: #ffffff;
	}

	.search-content {
		padding: 8rpx 24rpx 48rpx;
	}

	.search-input-shell {
		display: flex;
		align-items: center;
		min-height: 88rpx;
		padding: 0 24rpx;
		background: #f4f5f7;
		border: 1rpx solid transparent;
		border-radius: 44rpx;
	}

	.search-input-shell > .proto-icon {
		margin-right: 16rpx;
	}

	.search-input {
		flex: 1;
		min-width: 0;
		height: 88rpx;
		color: var(--proto-text);
		font-size: 28rpx;
	}

	.search-input::placeholder {
		color: #a8b0b8;
	}

	.clear-keyword {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 44rpx;
		height: 44rpx;
		margin-left: 8rpx;
		background: #dfe3e8;
		border-radius: 50%;
	}

	.region-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin: 44rpx 0 24rpx;
	}

	.region-heading-copy {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.region-heading-title {
		color: #1a1a1a;
		font-size: 32rpx;
		font-weight: 700;
	}

	.region-city-button {
		display: flex;
		align-items: center;
		gap: 4rpx;
		padding: 8rpx 0;
		color: var(--proto-primary-deep);
		background: transparent;
		border: 0;
		border-radius: 0;
		font-size: 24rpx;
	}

	.region-city-button::after {
		border: 0;
	}

	.region-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 26rpx;
	}

	.region-chip {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 156rpx;
		height: 64rpx;
		box-sizing: border-box;
		overflow: hidden;
		color: #333333;
		font-size: 22rpx;
		line-height: 1;
		text-align: center;
		text-overflow: ellipsis;
		white-space: nowrap;
		background: #f5f6f8;
		border: 1rpx solid transparent;
		border-radius: 10rpx;
	}

	.region-chip.selected {
		color: var(--proto-primary-deep);
		font-weight: 600;
		background: #eaf8f6;
		border-color: rgba(24, 133, 143, 0.28);
	}

	.region-state {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 44rpx 24rpx 30rpx;
		color: #9aa4ad;
		font-size: 22rpx;
		line-height: 1.6;
		text-align: center;
	}

	.region-state-empty {
		padding-top: 56rpx;
	}

	.region-state-action {
		margin-top: 20rpx;
		padding: 0 34rpx;
		height: 62rpx;
		line-height: 62rpx;
		color: var(--proto-primary-deep);
		font-size: 22rpx;
		font-weight: 600;
		background: #eaf8f6;
		border-radius: 32rpx;
	}

	.search-hint {
		display: flex;
		align-items: flex-start;
		margin-top: 34rpx;
		color: #9aa4ad;
		font-size: 20rpx;
		line-height: 1.5;
	}

	.search-hint .proto-icon {
		margin-top: 2rpx;
		margin-right: 8rpx;
	}
</style>
