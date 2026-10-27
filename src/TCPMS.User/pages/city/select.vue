<template>
	<view class="city-select-page">
		<ProtoHeader title="选择城市" title-align="center" theme="green" />

		<view class="city-body">
			<view class="city-section">
				<text class="section-label">当前定位</text>
				<view class="city-grid">
					<text
						class="city-chip city-chip-located"
						:class="{ selected: currentCity === locatedCity, pending: locating || !locatedCity }"
						@tap="pickLocatedCity"
					>{{ locatedCity || (locating ? '正在定位…' : '点击定位') }}</text>
					<text
						class="city-chip city-chip-all"
						:class="{ selected: currentCity === ALL_CITY_LABEL }"
						@tap="pick(ALL_CITY_LABEL)"
					>全部城市</text>
				</view>
				<text v-if="locatedHint" class="section-hint">{{ locatedHint }}</text>
			</view>

			<view class="city-section">
				<text class="section-label">热门城市</text>
				<view class="city-grid">
					<text
						v-for="item in hotCities"
						:key="item"
						class="city-chip"
						:class="{ selected: currentCity === item }"
						@tap="pick(item)"
					>{{ item }}</text>
				</view>
			</view>

			<view
				v-for="group in cityGroups"
				:id="`group-${group.letter}`"
				:key="group.letter"
				class="city-section letter-section"
			>
				<text class="letter-label">{{ group.letter }}</text>
				<view class="city-grid">
					<text
						v-for="item in group.cities"
						:key="item"
						class="city-chip"
						:class="{ selected: currentCity === item }"
						@tap="pick(item)"
					>{{ item }}</text>
				</view>
			</view>

			<view class="city-body-tail">
				<text>找不到城市？可直接搜索门店名称或地址</text>
			</view>
		</view>

		<view class="letter-index">
			<text
				v-for="letter in indexLetters"
				:key="letter"
				class="letter-index-item"
				:class="{ active: activeLetter === letter }"
				@tap="scrollToLetter(letter)"
			>{{ letter }}</text>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import { reverseGeocode } from '@/common/amap.js'
	import { getBooking, saveBooking } from '@/common/app-store.js'
	import {
		ALL_CITY_LABEL,
		CITY_GROUPS,
		CITY_INDEX_LETTERS,
		HOT_CITIES,
		normalizeCityName
	} from '@/common/city-data.js'

	const LOCATED_CITY_KEY = 'tcpms:located-city'

	const decodeValue = (value = '') => {
		try {
			return decodeURIComponent(String(value))
		} catch (error) {
			return String(value)
		}
	}

	export default {
		components: { ProtoHeader },
		data() {
			return {
				ALL_CITY_LABEL,
				currentCity: ALL_CITY_LABEL,
				locatedCity: '',
				locatedHint: '',
				locating: false,
				activeLetter: '',
				cityGroups: CITY_GROUPS,
				indexLetters: CITY_INDEX_LETTERS,
				sectionTops: [],
				headerHeightPx: 44
			}
		},
		computed: {
			hotCities() {
				// 定位城市已经单独占一格，热门城市里不再重复出现。
				return HOT_CITIES.filter((item) => item !== this.locatedCity)
			}
		},
		onLoad(options) {
			const booking = getBooking()
			const city = decodeValue(options && options.city) || booking.city || ALL_CITY_LABEL
			this.currentCity = city || ALL_CITY_LABEL

			let cached = ''
			try {
				cached = String(uni.getStorageSync(LOCATED_CITY_KEY) || '')
			} catch (error) {
				cached = ''
			}
			this.locatedCity = cached
			if (!cached) {
				this.locate()
			}
		},
		onReady() {
			try {
				const info = uni.getSystemInfoSync()
				const rpx2px = (Number(info.windowWidth) || 375) / 750
				this.headerHeightPx = (88 + Math.round((Number(info.statusBarHeight) || 0) * 750 / (Number(info.windowWidth) || 375))) * rpx2px
			} catch (error) {
				this.headerHeightPx = 44
			}
			this.measureSections()
		},
		onPageScroll(event) {
			const top = Number(event && event.scrollTop) || 0
			const sections = this.sectionTops
			if (!sections.length) return
			let current = sections[0].letter
			sections.forEach((section) => {
				// 留 80px 容差，滚动到分组标题附近就切换高亮。
				if (top + this.headerHeightPx + 80 >= section.top) current = section.letter
			})
			if (current !== this.activeLetter) this.activeLetter = current
		},
		methods: {
			letterOfCity(city) {
				const group = CITY_GROUPS.find((item) => item.cities.includes(city))
				return group ? group.letter : ''
			},
			measureSections() {
				const query = uni.createSelectorQuery().in(this)
				query.selectAll('.letter-section').boundingClientRect()
				query.selectViewport().scrollOffset()
				query.exec((result) => {
					const rects = (result && result[0]) || []
					const viewport = (result && result[1]) || {}
					const scrollTop = Number(viewport.scrollTop) || 0
					this.sectionTops = rects.map((rect, index) => ({
						letter: (CITY_GROUPS[index] && CITY_GROUPS[index].letter) || '',
						top: Number(rect.top) + scrollTop
					})).filter((item) => item.letter)
					this.syncActiveLetter()
				})
			},
			syncActiveLetter() {
				const letter = this.letterOfCity(this.currentCity)
				if (letter) this.activeLetter = letter
			},
			scrollToLetter(letter) {
				this.activeLetter = letter
				const query = uni.createSelectorQuery().in(this)
				query.select(`#group-${letter}`).boundingClientRect()
				query.selectViewport().scrollOffset()
				query.exec((result) => {
					const rect = result && result[0]
					if (!rect) return
					const scrollTop = Number(result[1] && result[1].scrollTop) || 0
					const target = Math.max(0, Number(rect.top) + scrollTop - this.headerHeightPx - 8)
					uni.pageScrollTo({ scrollTop: target, duration: 220 })
				})
			},
			async locate() {
				if (this.locating) return
				this.locating = true
				this.locatedHint = ''
				const position = await new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						success: (result) => resolve(result),
						fail: (error) => {
							console.warn('[选择城市] 定位失败：', error && (error.errMsg || error.message || error))
							resolve(null)
						}
					})
				})
				if (!position) {
					this.locating = false
					this.locatedHint = '定位未开启，可手动选择城市'
					return
				}
				try {
					const address = await reverseGeocode(position.latitude, position.longitude)
					const city = normalizeCityName(address.city || address.district || '')
					if (!city) {
						this.locatedHint = '定位成功，但未能识别所在城市'
					} else {
						this.locatedCity = city
						try {
							uni.setStorageSync(LOCATED_CITY_KEY, city)
						} catch (error) {
							// 存储失败不影响当前页面使用。
						}
					}
				} catch (error) {
					this.locatedHint = '定位解析失败，可手动选择城市'
				}
				this.locating = false
			},
			pickLocatedCity() {
				if (!this.locatedCity) {
					this.locate()
					return
				}
				this.pick(this.locatedCity)
			},
			pick(city) {
				const target = city || ALL_CITY_LABEL
				// 换城市后原来的区域一定失效，这里一并清掉。
				saveBooking({
					...getBooking(),
					city: target,
					region: ''
				})
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/index/index' })
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.city-select-page {
		min-height: 100vh;
		padding-right: 40rpx;
		box-sizing: border-box;
		background: #ffffff;
	}

	.city-body {
		padding: 8rpx 0 40rpx 32rpx;
	}

	.city-section {
		margin-bottom: 28rpx;
	}

	.section-label {
		display: block;
		margin-bottom: 20rpx;
		color: #1a1a1a;
		font-size: 30rpx;
		font-weight: 600;
	}

	.section-hint {
		display: block;
		margin-top: 14rpx;
		color: #999999;
		font-size: 22rpx;
	}

	.city-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 18rpx;
	}

	.city-chip {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 auto;
		min-width: 200rpx;
		max-width: 100%;
		height: 68rpx;
		padding: 0 22rpx;
		box-sizing: border-box;
		overflow: hidden;
		color: #333333;
		font-size: 24rpx;
		line-height: 1;
		text-align: center;
		text-overflow: ellipsis;
		white-space: nowrap;
		background: #f5f6f8;
		border: 1rpx solid transparent;
		border-radius: 10rpx;
	}

	.city-chip.selected {
		color: var(--proto-primary-deep);
		font-weight: 600;
		background: #eaf8f6;
		border-color: rgba(24, 133, 143, 0.28);
	}

	.city-chip-located {
		color: var(--proto-primary-deep);
		font-weight: 600;
		background: #eaf8f6;
	}

	.city-chip-located.pending {
		color: #999999;
		font-weight: 400;
		background: #f5f6f8;
	}

	.city-chip-all {
		color: #666666;
	}

	.letter-section {
		margin-bottom: 10rpx;
	}

	.letter-label {
		display: block;
		padding: 12rpx 0 18rpx;
		color: #1a1a1a;
		font-size: 28rpx;
		font-weight: 600;
	}

	.city-body-tail {
		padding: 30rpx 0 10rpx;
		color: #aaaaaa;
		font-size: 22rpx;
		text-align: center;
	}

	.letter-index {
		position: fixed;
		top: 50%;
		right: 4rpx;
		z-index: 20;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 8rpx 2rpx;
		transform: translateY(-50%);
	}

	.letter-index-item {
		padding: 2rpx 8rpx;
		color: #7c8794;
		font-size: 18rpx;
		line-height: 1.3;
	}

	.letter-index-item.active {
		color: var(--proto-primary-deep);
		font-weight: 700;
	}
</style>
