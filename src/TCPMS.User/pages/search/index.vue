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

			<view class="location-toolbar">
				<view class="location-heading">
					<text>区域选择</text>
				</view>
				<button class="current-location-button" hover-class="none" @tap="useCurrentLocation">
					<text>{{ locationStatus }}</text>
				</button>
			</view>

			<scroll-view class="city-scroll" scroll-x enable-flex>
				<view class="city-list">
					<text
						v-for="item in cityOptions"
						:key="item"
						class="city-chip"
						:class="{ selected: activeCity === item }"
						@tap="selectCity(item)"
					>{{ item }}</text>
				</view>
			</scroll-view>

			<view class="region-panel">
				<view class="region-panel-heading">
					<text>{{ activeCity }}区域</text>
					<text v-if="keyword" class="region-result-count">匹配“{{ keyword }}”</text>
				</view>
				<view v-if="filteredAreas.length" class="region-grid">
					<text
						v-for="area in filteredAreas"
						:key="area"
						class="region-chip"
						:class="{ selected: selectedArea === area }"
						@tap="selectArea(area)"
					>{{ area }}</text>
				</view>
				<view v-else class="region-empty">
					<ProtoEmptyState compact />
				</view>
			</view>

			<view class="search-hint">
				<ProtoIcon name="info" :size="22" />
				<text>区域数据先使用本地城市目录，接入高德后将按定位和关键词实时更新。</text>
			</view>

			<button class="confirm-search-button" hover-class="none" @tap="confirmKeyword">
				<ProtoIcon name="search" tone="white" :size="28" />
				<text>使用当前条件</text>
			</button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { getBooking, saveBooking } from '@/common/app-store.js'
	import { showToast } from '@/common/prototype.js'

	const REGION_GROUPS = [
		{
			city: '郑州市',
			areas: ['金水区', '二七区', '中原区', '管城回族区', '惠济区', '郑东新区', '高新区', '经开区', '新密市', '荥阳市', '中牟县']
		},
		{
			city: '开封市',
			areas: ['龙亭区', '鼓楼区', '禹王台区', '顺河回族区', '祥符区', '尉氏县', '杞县', '通许县']
		},
		{
			city: '洛阳市',
			areas: ['涧西区', '西工区', '老城区', '瀍河回族区', '洛龙区', '孟津区', '偃师区', '栾川县']
		},
		{
			city: '安阳市',
			areas: ['文峰区', '北关区', '殷都区', '龙安区', '安阳县', '林州市', '滑县']
		},
		{
			city: '新乡市',
			areas: ['红旗区', '卫滨区', '凤泉区', '牧野区', '新乡县', '辉县市', '卫辉市']
		},
		{
			city: '焦作市',
			areas: ['解放区', '山阳区', '中站区', '马村区', '修武县', '沁阳市', '孟州市']
		}
	]

	const decodeValue = (value = '') => {
		try {
			return decodeURIComponent(String(value))
		} catch (error) {
			return String(value)
		}
	}

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				keyword: '',
				activeCity: '郑州市',
				selectedArea: '',
				locationStatus: '我的位置',
				regionGroups: REGION_GROUPS
			}
		},
		computed: {
			cityOptions() {
				return this.regionGroups.map((item) => item.city)
			},
			activeGroup() {
				return this.regionGroups.find((item) => item.city === this.activeCity) || this.regionGroups[0]
			},
			filteredAreas() {
				const value = this.keyword.trim().toLowerCase()
				if (!value) return this.activeGroup.areas
				return this.activeGroup.areas.filter((area) => area.toLowerCase().indexOf(value) >= 0)
			}
		},
		onLoad(options) {
			const booking = getBooking()
			const city = decodeValue(options && options.city) || booking.city || '郑州市'
			this.activeCity = this.cityOptions.indexOf(city) >= 0 ? city : '郑州市'
			this.keyword = decodeValue(options && options.keyword) || booking.searchKeyword || ''
			this.selectedArea = decodeValue(options && options.region) || booking.region || ''
		},
		methods: {
			selectCity(city) {
				this.activeCity = city
				this.selectedArea = ''
			},
			selectArea(area) {
				this.selectedArea = area
				this.saveSelection(true)
			},
			clearKeyword() {
				this.keyword = ''
			},
			useCurrentLocation() {
				this.locationStatus = '定位中'
				uni.getLocation({
					type: 'gcj02',
					success: () => {
						this.locationStatus = '已定位'
						showToast('已获取当前位置，请选择区域')
					},
					fail: () => {
						this.locationStatus = '我的位置'
						showToast('定位未开启，可手动选择城市')
					}
				})
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
		background: var(--proto-page);
	}

	.search-content {
		padding: 8rpx 24rpx 48rpx;
	}

	.search-input-shell {
		display: flex;
		align-items: center;
		min-height: 92rpx;
		padding: 0 24rpx;
		background: var(--proto-surface-low);
		border: 1rpx solid var(--proto-border);
		border-radius: 48rpx;
	}

	.search-input-shell > .proto-icon {
		margin-right: 16rpx;
	}

	.search-input {
		flex: 1;
		min-width: 0;
		height: 92rpx;
		color: var(--proto-text);
		font-size: 28rpx;
	}

	.search-input::placeholder {
		color: var(--proto-muted);
	}

	.clear-keyword {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 44rpx;
		height: 44rpx;
		margin-left: 8rpx;
		background: var(--proto-border);
		border-radius: 50%;
	}

	.location-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 44rpx;
	}

	.location-heading {
		display: flex;
		align-items: center;
		color: var(--proto-text);
		font-size: 30rpx;
		font-weight: 800;
	}

	.current-location-button {
		display: flex;
		align-items: center;
		padding: 10rpx 4rpx;
		color: var(--proto-primary-dark);
		background: transparent;
		border: 0;
		border-radius: 0;
		font-size: 20rpx;
	}

	.current-location-button .proto-icon {
		margin-right: 6rpx;
	}

	.city-scroll {
		width: 100%;
		margin-top: 22rpx;
		white-space: nowrap;
	}

	.city-list {
		display: inline-flex;
		gap: 16rpx;
		padding-bottom: 4rpx;
	}

	.city-chip,
	.region-chip {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-height: 62rpx;
		padding: 0 24rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border: 1rpx solid transparent;
		border-radius: 32rpx;
		font-size: 22rpx;
		white-space: nowrap;
	}

	.city-chip.selected,
	.region-chip.selected {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-color: rgba(0, 106, 106, 0.14);
		font-weight: 700;
	}

	.region-panel {
		margin-top: 24rpx;
		padding: 26rpx 24rpx 30rpx;
		background: var(--proto-surface);
		border-radius: 24rpx;
		box-shadow: 0 12rpx 30rpx rgba(23, 29, 30, 0.05);
	}

	.region-panel-heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.region-result-count {
		color: var(--proto-muted);
		font-size: 18rpx;
		font-weight: 400;
	}

	.region-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 18rpx 14rpx;
		margin-top: 24rpx;
	}

	.region-chip {
		min-width: 132rpx;
		min-height: 66rpx;
		padding: 0 18rpx;
		font-size: 21rpx;
	}

	.region-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 42rpx 24rpx 26rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
		line-height: 1.5;
		text-align: center;
	}

	.region-empty .proto-icon {
		margin-bottom: 12rpx;
	}

	.search-hint {
		display: flex;
		align-items: flex-start;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.search-hint .proto-icon {
		margin-top: 2rpx;
		margin-right: 8rpx;
	}

	.confirm-search-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 86rpx;
		margin-top: 24rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 44rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.24);
		font-size: 27rpx;
		font-weight: 750;
	}

	.confirm-search-button .proto-icon {
		margin-right: 10rpx;
	}
</style>
