<template>
	<view class="home-page">
		<view class="page-content">
			<view class="hero-shell">
				<swiper class="hero-swiper" :current="heroIndex" circular autoplay :interval="4600" :duration="420" @change="handleHeroChange">
					<swiper-item v-for="(slide, index) in heroSlides" :key="slide.image">
						<view class="hero-media" @tap="handleHeroTap(slide)">
							<image class="hero-image" :src="slide.image" mode="aspectFill" @error="handleHeroImageError(index)"></image>
							<view class="hero-shade"></view>
							<view class="hero-copy">
								<view class="hero-badge">
									<ProtoIcon name="check-circle" tone="white" :size="22" />
									<text>{{ slide.badge }}</text>
								</view>
								<text class="hero-title">{{ slide.title }}</text>
								<text class="hero-subtitle">{{ slide.subtitle }}</text>
							</view>
							<view class="hero-indicators">
								<text v-for="(_, indicatorIndex) in heroSlides" :key="indicatorIndex" class="hero-indicator" :class="{ active: heroIndex === indicatorIndex }"></text>
							</view>
						</view>
					</swiper-item>
				</swiper>
			</view>

			<view class="booking-shell">
				<view class="booking-panel">
					<view class="search-destination-row">
						<button class="destination-main" hover-class="none" @tap="openLocationSearch">
							<view class="destination-copy">
								<view class="destination-city-row" @tap.stop="openCityPicker">
									<text class="destination-city">{{ city }}</text>
									<ProtoIcon name="chevron-down" :size="20" />
								</view>
								<text class="destination-placeholder">{{ searchKeyword || region || '位置 / 民宿 / 关键词' }}</text>
							</view>
						</button>
						<view class="destination-tools">
							<button class="destination-tool" hover-class="none" @tap="goMap">
								<ProtoIcon name="map" :size="30" />
								<text>地图</text>
							</button>
							<button class="destination-tool" hover-class="none" @tap="triggerLocation">
								<ProtoIcon name="navigation" :size="30" />
								<text>我的位置</text>
							</button>
						</view>
					</view>

					<view class="booking-divider"></view>

					<view class="date-row" @tap="openDatePage">
						<view class="date-side">
							<text class="field-label">入住时间</text>
							<view class="date-value-row">
								<text class="date-value">{{ checkIn }}</text>
								<text class="date-day">{{ checkInDay }}</text>
							</view>
						</view>
						<view class="night-block">
							<text class="night-count">{{ nights }}</text>
							<text class="night-line"></text>
						</view>
						<view class="date-side date-side-right">
							<text class="field-label">离店时间</text>
							<view class="date-value-row">
								<text class="date-value">{{ checkOut }}</text>
								<text class="date-day">{{ checkOutDay }}</text>
							</view>
						</view>
					</view>

					<view class="booking-divider"></view>

					<view class="preference-row" @tap="openSheet('preference')">
						<view class="preference-summary">
							<view class="preference-title">
								<ProtoIcon name="group" :size="33" />
								<text>人数 / 床数 / 居室数</text>
							</view>
							<view class="preference-tags">
							<text class="preference-tag preference-tag-primary">{{ preferenceSummary }}</text>
								<text class="preference-tag">整套房源/床位</text>
								<text class="preference-tag">{{ priceSummary }}</text>
							</view>
						</view>
						<ProtoIcon name="chevron-right" :size="30" />
					</view>

					<button class="search-submit" hover-class="none" :class="{ searching: searching }" @tap="handleSearch">
						<ProtoIcon name="search" tone="white" :size="35" />
						<text>{{ searching ? '正在查询房态...' : '查找民宿' }}</text>
					</button>

					<view class="assurance-row">
						<view><ProtoIcon name="check" :size="18" /><text>极速确认</text></view>
						<view><ProtoIcon name="check" :size="18" /><text>免费取消</text></view>
						<view><ProtoIcon name="check" :size="18" /><text>24h自助门禁</text></view>
					</view>
				</view>
			</view>

			<view class="quick-grid">
				<view
					v-for="item in quickEntrances"
					:key="item.key"
					class="quick-card"
					:class="item.tone"
					@tap="handleQuickEntrance(item)"
					>
						<view class="quick-icon">
							<image :src="`/static/prototype-icons/home/${item.icon}.png`" mode="aspectFit"></image>
						</view>
					<text class="quick-title">{{ item.title }}</text>
					<text class="quick-desc">{{ item.desc }}</text>
				</view>
			</view>

			<view id="popularStores" class="popular-section">
				<view class="popular-heading">
					<view class="popular-title-group">
						<text class="popular-title">本地热门精选</text>
						<text class="popular-badge">{{ catalogState === 'error' ? '加载失败' : catalogState === 'loading' ? '正在同步' : '实时好房' }}</text>
					</view>
					<button class="sort-button" hover-class="none" @tap="sortStores">
						<text>{{ sortLabel }}</text>
						<ProtoIcon name="tune" :size="24" />
					</button>
				</view>

				<view v-if="cityFallback" class="city-fallback-tip">
					<ProtoIcon name="info" :size="22" />
					<text>{{ city }}暂无在售房型，以下为你推荐其他城市房源</text>
					<text class="city-fallback-action" @tap.stop="openCityPicker">切换城市</text>
				</view>

				<view v-if="!userLocation && catalogState === 'synced'" class="distance-hint" @tap="triggerLocation">
					<ProtoIcon name="navigation" :size="20" />
					<text>开启定位即可查看房源距您的距离</text>
					<text class="distance-hint-action">去开启</text>
				</view>

				<view v-if="visibleRooms.length" class="store-list">
					<view
						v-for="room in visibleRooms"
						:key="room.key"
						class="store-card"
						@tap="openRoom(room)"
					>
						<view class="store-image-wrap">
							<view class="store-media-grid" :class="`store-media-grid-${Math.min(room.images.length, 3)}`">
								<image
									v-for="(image, imageIndex) in room.images.slice(0, 3)"
									:key="`${room.key}-${imageIndex}-${image}`"
									class="store-image"
									:src="image"
									mode="aspectFill"
								></image>
							</view>
							<view class="store-image-tags">
								<text v-if="room.hasDistance" class="distance-badge">距您 {{ room.distance }}</text>
								<text class="area-badge" :class="room.badgeTone">{{ room.type }}</text>
							</view>
						</view>
						<view class="store-card-body">
							<view class="store-heading">
								<view class="store-heading-copy">
									<text class="store-card-title">{{ room.name }}</text>
									<text class="store-room">{{ room.spec }}</text>
								</view>
							</view>
							<view v-if="room.tags.length" class="tag-list">
								<text v-for="tag in room.tags.slice(0, 3)" :key="tag" class="store-tag">{{ tag }}</text>
							</view>
							<view class="store-footer">
								<view class="price-block">
									<text class="price-currency">¥</text>
									<text class="price-value">{{ room.price }}</text>
									<text class="price-unit">/ 晚起</text>
								</view>
								<button class="book-button" hover-class="none" @tap.stop="bookRoom(room)">立即预订</button>
							</view>
						</view>
					</view>
				</view>
				<view v-else-if="catalogState === 'loading'" class="popular-loading">
					<text>正在同步在售房型…</text>
				</view>
				<ProtoEmptyState
					v-else
					:title="allRooms.length ? '没有符合条件的房型' : '暂无在售房型'"
					:description="allRooms.length ? '' : '门店在后台发布房型后会自动出现在这里'"
					:action-text="allRooms.length ? '清除筛选' : ''"
					@action="resetSearch"
				/>
			</view>

			<view class="brand-footer">
				<view class="brand-footer-line">
					<text class="brand-footer-rule"></text>
					<text>择居青旅，为每次出发留一盏灯</text>
					<text class="brand-footer-rule"></text>
				</view>
				<text class="brand-footer-desc">房源直联房态系统 · 入住流程清晰 · 旅途轻松无忧</text>
			</view>
		</view>

		<ProtoBottomNav active="home" variant="root" />

		<view v-if="toast" class="toast-message">{{ toast }}</view>

		<view v-if="sheetVisible" class="sheet-mask" @tap="closeSheet">
			<view class="bottom-sheet" :class="{ 'preference-bottom-sheet': sheetType === 'preference' }" @tap.stop>
				<view class="sheet-handle"></view>
				<view class="sheet-header">
					<text class="sheet-title">{{ sheetTitle }}</text>
					<button class="sheet-close" hover-class="none" aria-label="关闭" @tap="closeSheet">
						<ProtoIcon name="close" :size="24" />
					</button>
				</view>

				<view v-if="sheetType === 'store'" class="sheet-content">
					<text class="sheet-caption">选择入住门店</text>
					<view class="store-option-list">
						<view
							v-for="store in stores"
							:key="store.id"
							class="store-option"
							:class="{ selected: selectedStore && selectedStore.id === store.id }"
							@tap="selectStore(store)"
						>
							<view class="store-option-copy">
								<text class="store-option-title">{{ store.name }}</text>
								<text class="store-option-desc">{{ store.subtitle }} · {{ storeDistanceLabel(store) }}</text>
							</view>
							<ProtoIcon v-if="selectedStore && selectedStore.id === store.id" name="check" :size="24" />
						</view>
					</view>
				</view>

				<view v-else-if="sheetType === 'date'" class="sheet-content">
					<view class="date-sheet-summary">
						<view>
							<text class="date-sheet-label">入住</text>
							<text class="date-sheet-value">{{ checkIn }}</text>
						</view>
						<ProtoIcon name="chevron-right" :size="28" />
						<view>
							<text class="date-sheet-label">离店</text>
							<text class="date-sheet-value">{{ checkOut }}</text>
						</view>
						<text class="date-sheet-night">{{ nights }}</text>
					</view>
					<text class="sheet-caption">快捷选择</text>
					<view class="date-preset-list">
						<view v-for="preset in datePresets" :key="preset.label" class="date-preset" @tap="selectDatePreset(preset)">
							<text class="preset-title">{{ preset.label }}</text>
							<text class="preset-detail">{{ preset.checkIn }} - {{ preset.checkOut }}</text>
						</view>
					</view>
					<button class="sheet-primary-button" hover-class="none" @tap="closeSheet">完成</button>
				</view>

				<view v-else-if="sheetType === 'preference'" class="sheet-content preference-sheet-content">
					<view class="preference-row-list">
						<view v-for="row in preferenceRows" :key="row.key" class="preference-control-row">
							<text class="preference-control-label">{{ row.label }}</text>
							<view class="preference-stepper">
								<button class="preference-stepper-button" hover-class="none" :aria-label="`减少${row.label}`" @tap="adjustPreference(row.key, -1)">
									<ProtoIcon name="minus" :size="28" />
								</button>
								<text class="preference-stepper-value">{{ preferenceDraft[row.key] }}</text>
								<button class="preference-stepper-button" hover-class="none" :aria-label="`增加${row.label}`" @tap="adjustPreference(row.key, 1)">
									<ProtoIcon name="plus" :size="28" />
								</button>
							</view>
						</view>
					</view>

					<view class="preference-price-section">
						<view class="preference-price-heading">
							<text>价格范围</text>
							<text>¥{{ preferenceDraft.priceMin }} - ¥{{ preferenceDraft.priceMax }}</text>
						</view>
						<view class="price-range" @tap="setPriceFromTrack">
							<view class="price-range-track"></view>
							<view class="price-range-active" :style="priceRangeStyle"></view>
							<view class="price-range-handle" :style="priceMinHandleStyle" @touchstart.stop="startPriceDrag('priceMin', $event)" @touchmove.stop="movePriceDrag($event)" @touchend.stop="endPriceDrag"></view>
							<view class="price-range-handle" :style="priceMaxHandleStyle" @touchstart.stop="startPriceDrag('priceMax', $event)" @touchmove.stop="movePriceDrag($event)" @touchend.stop="endPriceDrag"></view>
						</view>
						<view class="price-range-labels">
							<text>¥0</text>
							<text>¥500</text>
						</view>
					</view>

					<button class="sheet-primary-button preference-confirm-button" hover-class="none" @tap="confirmPreference">确认</button>
				</view>

				<view v-else-if="sheetType === 'portal'" class="sheet-content portal-sheet">
					<view class="portal-sheet-icon">
						<ProtoIcon :name="selectedPortal.icon" :size="46" />
					</view>
					<text class="portal-sheet-title">{{ selectedPortal.title }}</text>
					<text class="portal-sheet-desc">{{ selectedPortal.longDesc }}</text>
					<button class="sheet-primary-button" hover-class="none" @tap="continuePortal">继续</button>
				</view>

				<view v-else-if="sheetType === 'menu'" class="sheet-content menu-sheet-content">
					<view v-for="item in menuItems" :key="item.title" class="menu-option" @tap="handleMenuItem(item)">
						<ProtoIcon class="menu-option-icon" :name="item.icon" :size="32" />
						<view class="menu-option-copy">
							<text class="menu-option-title">{{ item.title }}</text>
							<text class="menu-option-desc">{{ item.desc }}</text>
						</view>
						<ProtoIcon name="chevron-right" :size="28" />
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { demoRooms, demoStores, goPage, prototypeImages } from '@/common/prototype.js'
	import { brandImageFallbacks } from '@/common/brand-assets.js'
	import { getBooking, saveBooking } from '@/common/app-store.js'
	import { fetchCarousel, fetchStores } from '@/common/api.js'
	import { reverseGeocode } from '@/common/amap.js'
	import { distanceKmBetween, formatDistanceKm } from '@/common/geo.js'
	import { storeRoomPath } from '@/common/navigation.js'
	import { setTabParams } from '@/common/tab-params.js'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'

	const PROTOTYPE_IMAGES = {
		hero: prototypeImages.homeHero,
		storeOne: prototypeImages.storeOne,
		storeTwo: prototypeImages.storeTwo,
		storeThree: prototypeImages.hostel
	}

	const weekdayLabels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

	const weekdayOf = (isoDate) => {
		const date = isoDate ? new Date(isoDate) : new Date()
		return weekdayLabels[date.getDay()]
	}

	const formatDateLabel = (date) => {
		return `${String(date.getMonth() + 1).padStart(2, '0')}月${String(date.getDate()).padStart(2, '0')}日`
	}

	const formatIsoDate = (date) => {
		return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}T00:00:00`
	}

	const createPreset = (label, offset, nights) => {
		const checkIn = new Date()
		checkIn.setHours(0, 0, 0, 0)
		checkIn.setDate(checkIn.getDate() + offset)
		const checkOut = new Date(checkIn)
		checkOut.setDate(checkOut.getDate() + nights)
		return {
			label,
			checkIn: formatDateLabel(checkIn),
			checkOut: formatDateLabel(checkOut),
			checkInDay: weekdayLabels[checkIn.getDay()],
			checkOutDay: weekdayLabels[checkOut.getDay()],
			isoCheckIn: formatIsoDate(checkIn),
			isoCheckOut: formatIsoDate(checkOut),
			date: `${formatDateLabel(checkIn)} - ${formatDateLabel(checkOut)}`,
			nights: `共 ${nights} 晚`
		}
	}

	const createDatePresets = () => [
		createPreset('明日入住', 1, 1),
		createPreset('周末出行', 1, 2),
		createPreset('下周出行', 6, 2)
	]

	const storeMatchesCity = (store, city) => {
		if (city === '全部城市') return true
		if (store.city) return store.city === city
		return city === '郑州市' && (store.address || '').indexOf('郑州') >= 0
	}

	// 逆地理编码返回的城市名不一定带「市」（直辖市/自治州另有写法），统一成后端门店的写法。
	const normalizeCityName = (name) => {
		const value = String(name || '').trim()
		if (!value) return ''
		if (/(省|自治区|特别行政区)$/.test(value)) return ''
		if (/[市州盟县区]$/.test(value)) return value
		return `${value}市`
	}

	const normalizeStoreImages = (store = {}, fallback = {}) => {
		const candidates = [store.images, store.imageUrls, store.gallery, store.photos]
			.find((images) => Array.isArray(images) && images.length)
		const fallbackImages = !store.isRemote && Array.isArray(fallback.images) && fallback.images.length
			? fallback.images
			: (!store.isRemote ? [fallback.image].filter(Boolean) : [])
		const toImageUrl = (item) => {
			if (typeof item === 'string') return item
			if (!item || typeof item !== 'object') return ''
			return item.url || item.imageUrl || item.src || item.path || ''
		}
		const images = (candidates || fallbackImages).map(toImageUrl).filter(Boolean)
		if (!images.length && store.image) {
			images.push(store.image)
		} else if (!images.length && !store.isRemote && fallback.image) {
			images.push(fallback.image)
		}
		return images.slice(0, 3)
	}

	const toHomeStore = (store, index = 0) => {
		const fallback = store.isRemote ? {} : (demoStores[index] || demoStores[0])
		const storeRooms = Array.isArray(store.rooms) ? store.rooms : []
		return {
			...fallback,
			...store,
			id: store.id || fallback.id,
			name: store.name || fallback.name,
			subtitle: store.address || fallback.address || '',
			address: store.address || store.subtitle || fallback.address || '',
			room: store.room || fallback.room || '',
			featuredRoomId: store.featuredRoomId || store.roomId || (storeRooms[0] && storeRooms[0].id) || (store.isRemote ? null : fallback.featuredRoomId || demoRooms[0].id),
			badge: store.badge || fallback.badge || (store.isRemote ? '营业门店' : (index === 0 ? '官方直营' : '品质优选')),
			badgeTone: store.badgeTone || (index % 2 ? 'badge-secondary' : 'badge-primary'),
			status: store.status || fallback.status || '',
			rating: store.rating ?? fallback.rating ?? '暂无评分',
			reviews: store.reviews ?? fallback.reviews ?? 0,
			stock: store.stock || (store.isRemote ? '查询实时房态' : '今日可预订'),
			stockTone: store.stockTone || 'stock-success',
			tags: store.tags && store.tags.length ? store.tags : (fallback.tags || []),
			price: store.price ?? fallback.price ?? '--',
			unit: store.unit || '晚',
			image: store.image || fallback.image || '',
			images: normalizeStoreImages(store, fallback)
		}
	}

	const toCarouselSlide = (slide, index = 0) => ({
		image: slide.image || [PROTOTYPE_IMAGES.hero, PROTOTYPE_IMAGES.storeTwo, PROTOTYPE_IMAGES.hostel][index % 3],
		badge: slide.badge || '择居青旅',
		title: slide.title || '住进城市，走近风景',
		subtitle: slide.subtitle || '为每一次出发，准备一个舒服的落脚点',
		link: slide.link || ''
	})

	export default {
		components: {
			ProtoIcon,
			ProtoBottomNav,
			ProtoEmptyState
		},
		data() {
			const booking = getBooking()
			return {
				heroImage: PROTOTYPE_IMAGES.hero,
				heroIndex: 0,
				heroSlides: [
					{ image: PROTOTYPE_IMAGES.hero, badge: '官方直营 · 连住特惠', title: '寻一处静谧，归心旅途', subtitle: '阳光实木套房与极简美学空间现已开放' },
					{ image: PROTOTYPE_IMAGES.storeTwo, badge: '城市精选 · 即时房态', title: '住进城市，走近风景', subtitle: '通勤、旅行与周末短住，都有合适的落脚点' },
					{ image: PROTOTYPE_IMAGES.hostel, badge: '青年友好 · 轻装出发', title: '今晚就住得舒服', subtitle: '整洁床位与安心服务，轻松开启下一段行程' }
				],
				city: booking.city || '全部城市',
				region: booking.region || '',
				searchKeyword: booking.searchKeyword || '',
				storeCount: 0,
				catalogState: 'loading',
				selectedStoreId: booking.storeId || '',
				checkIn: booking.checkIn,
				checkOut: booking.checkOut,
				checkInDay: weekdayOf(booking.isoCheckIn),
				checkOutDay: weekdayOf(booking.isoCheckOut),
				nights: `共 ${booking.nights || 1} 晚`,
				guestSummary: `${booking.roomCount || 1}间 · ${booking.guestCount || 1}位成人 · 0位儿童`,
				priceSummary: '不限价格 / ¥0-¥500',
				preferenceDraft: {
					guestCount: Number(booking.guestCount || 1),
					bedCount: Number(booking.bedCount || 0),
					roomCount: Number(booking.roomCount || 1),
					priceMin: 0,
					priceMax: 500
				},
				preferenceRows: [
					{ key: 'guestCount', label: '人数' },
					{ key: 'bedCount', label: '床数' },
					{ key: 'roomCount', label: '居室数' }
				],
				priceDragField: '',
				priceRangeLeft: 0,
				priceRangeWidth: 0,
				booking,
				locationState: 'idle',
				// 用户当前定位（gcj02）。拿到后「本地热门精选」用它算房型距用户的公里数。
				userLocation: null,
				locatedCity: '',
				sortLabel: '距离最近优先',
				searching: false,
				toast: '',
				toastTimer: null,
				sheetVisible: false,
				sheetType: '',
				selectedPortal: {
					key: 'map',
					icon: 'map',
					title: '多门店地图',
					longDesc: '浏览全部营业门店，选择离你最近的入住地点。'
				},
				selectedFilters: [],
				cities: ['全部城市', '郑州市', '洛阳市', '开封市', '西安市', '成都市', '上海市', '杭州市'],
				datePresets: createDatePresets(),
				guestOptions: [
					{ value: '1间 · 2位成人 · 0位儿童', roomCount: 1, guestCount: 2, desc: '适合标准双人入住' },
					{ value: '1间 · 1位成人 · 0位儿童', roomCount: 1, guestCount: 1, desc: '适合独自出行或短住' },
					{ value: '1间 · 2位成人 · 1位儿童', roomCount: 1, guestCount: 3, desc: '适合亲子出行' },
					{ value: '2间 · 4位成人 · 0位儿童', roomCount: 2, guestCount: 4, desc: '适合多人同行' }
				],
				priceOptions: ['不限价格 / ¥0-¥500', '¥0 - ¥200', '¥200 - ¥400', '¥400 - ¥800', '¥800以上'],
				houseTypeOptions: ['整套出租', '独立房间', '多人床位', '可带宠物'],
				quickEntrances: [
					{
						key: 'quality',
						icon: 'house',
						title: '品质民宿',
						desc: '甄选好房',
						longDesc: '从采光、卫生到入住体验层层筛选，优先为你推荐稳定可靠的品质民宿。',
						tone: 'quick-card-cyan'
					},
					{
						key: 'popular',
						icon: 'bed',
						title: '热门门店',
						desc: '附近热住',
						longDesc: '查看当前城市热度最高的门店与房型，快速找到大家正在入住的好房。',
						tone: 'quick-card-blue'
					},
					{
						key: 'order',
						icon: 'receipt',
						title: '订单速查',
						desc: '电子入住码',
						longDesc: '已自动匹配您的手机号，快速查看订单和入住密码。',
						tone: 'quick-card-gray'
					}
				],
				menuItems: [
					{ icon: 'heart', title: '我的收藏', desc: '保存喜欢的门店与房型', route: '/pages/me/favorites' },
					{ icon: 'clock', title: '浏览记录', desc: '找回最近看过的房源', route: '/pages/me/history' },
					{ icon: 'help', title: '帮助与反馈', desc: '需要帮助时联系我们', route: '/pages/service/contact' }
				],
				stores: [],
			}
		},
		onLoad() {
			this.loadCarousel()
			this.loadStores()
			// 进入首页就申请微信定位授权（腾讯定位能力），拿到城市后覆盖默认城市。
			this.requestLocationCity({ silent: true })
		},
		onShow() {
			uni.hideTabBar({ animation: false, fail: () => {} })
			const booking = getBooking()
			const previousCity = this.city
			this.applyBooking(booking)
			// 从「选择城市」页返回时城市可能已经变了，这里重算一次条数。
			if (this.stores.length && previousCity !== this.city) {
				this.storeCount = this.stores.filter((store) => storeMatchesCity(store, this.city)).length
			}
		},
		computed: {
			preferenceSummary() {
				const draft = this.preferenceDraft
				return `${draft.guestCount}人 · ${draft.bedCount}床 · ${draft.roomCount}间`
			},
			priceRangeStyle() {
				const min = Math.max(0, Math.min(500, Number(this.preferenceDraft.priceMin || 0)))
				const max = Math.max(min, Math.min(500, Number(this.preferenceDraft.priceMax || 500)))
				return {
					left: `${(min / 500) * 100}%`,
					right: `${100 - (max / 500) * 100}%`
				}
			},
			priceMinHandleStyle() {
				return { left: `${(Number(this.preferenceDraft.priceMin || 0) / 500) * 100}%` }
			},
			priceMaxHandleStyle() {
				return { left: `${(Number(this.preferenceDraft.priceMax || 500) / 500) * 100}%` }
			},
			selectedStore() {
				return this.stores.find((store) => store.id === this.selectedStoreId) || this.stores[0]
			},
			// 门店 → 距用户公里数。用户定位或门店经纬度缺失时不进表，卡片就不显示距离。
			storeDistanceMap() {
				const location = this.userLocation
				if (!location) return {}
				const distances = {}
				this.stores.forEach((store) => {
					const km = distanceKmBetween(location, { latitude: store.latitude, longitude: store.longitude })
					if (km === null) return
					distances[store.id] = km
				})
				return distances
			},
			// 首页热门只展示房型：把各门店的在售房型摊平，带上门店的城市/距离用于本地筛选。
			allRooms() {
				const rooms = []
				this.stores.forEach((store) => {
					const storeRooms = Array.isArray(store.rooms) ? store.rooms : []
					storeRooms.forEach((room, index) => {
						if (!room || !room.id) return
						const source = (room.images && room.images.length ? room.images : store.images) || []
						const images = source
							.filter(Boolean)
							.slice(0, 3)
						// 优先用定位算出来的真实距离；没有定位时退回门店自带文案（可能是「距离未知」）。
						const distanceKm = this.storeDistanceMap[store.id]
						const hasDistance = Number.isFinite(distanceKm)
						rooms.push({
							...room,
							key: `${store.id}-${room.id}`,
							storeId: store.id,
							city: store.city || '',
							address: store.address || '',
							rating: store.rating,
							distance: hasDistance ? formatDistanceKm(distanceKm) : store.distance,
							hasDistance,
							distanceValue: hasDistance ? distanceKm : Number.MAX_SAFE_INTEGER,
							images: images.length ? images : [PROTOTYPE_IMAGES.hero],
							spec: [room.capacity, room.beds, room.area].filter(Boolean).join(' · ') || room.type || '',
							badgeTone: room.type === '多人间床位' ? 'badge-secondary' : '',
							price: room.price || store.price || '--',
							tags: (room.tags || []).filter(Boolean),
							sortIndex: index
						})
					})
				})
				return rooms
			},
			// 当前城市没有在售房型：改为推荐其他城市房源，同时提示原因。
			cityFallback() {
				if (!this.allRooms.length) return false
				if (!this.city || this.city === '全部城市') return false
				return !this.allRooms.some((room) => storeMatchesCity({ city: room.city, address: room.address }, this.city))
			},
			visibleRooms() {
				if (!this.allRooms.length) {
					return []
				}
				const priceMin = Number(this.preferenceDraft.priceMin || 0)
				const priceMax = Number(this.preferenceDraft.priceMax || 500)
				// 滑块两端（0-500）表示不限价格，只有用户收窄区间时才真正过滤。
				const priceLimited = priceMin > 0 || priceMax < 500
				const cityFilterActive = this.city !== '全部城市' && !this.cityFallback
				const rooms = this.allRooms.filter((room) => {
					const cityMatch = !cityFilterActive || storeMatchesCity({ city: room.city, address: room.address }, this.city)
					const price = Number(room.price)
					const priceMatch = !priceLimited || room.price === '--' || Number.isNaN(price)
						|| (price >= priceMin && price <= priceMax)
					const text = `${room.name} ${room.type} ${room.tags.join(' ')}`
					const featureMatch = this.selectedFilters.every((filter) => {
						if (filter === '独立房间') return text.indexOf('床位') < 0
						if (filter === '多人床位') return text.indexOf('床位') >= 0
						if (filter === '整套出租') return room.type !== '多人间床位'
						if (filter === '可带宠物') return text.indexOf('宠物') >= 0
						return true
					})
					return cityMatch && priceMatch && featureMatch
				})
				if (this.sortLabel === '评分最高优先') {
					return rooms.sort((left, right) => Number(right.rating) - Number(left.rating))
				}
				return rooms.sort((left, right) => left.distanceValue - right.distanceValue)
			},
			sheetTitle() {
				const titles = {
					city: '切换当前城市',
					store: '选择入住门店',
					date: '入住日期',
					preference: '选择人数/床数/居室数',
					portal: this.selectedPortal.title,
					menu: '更多服务'
				}
				return titles[this.sheetType] || '择居青旅'
			}
		},
		methods: {
			async loadCarousel() {
				try {
					const slides = await fetchCarousel()
					if (slides.length) {
						this.heroSlides = slides.map(toCarouselSlide)
						this.heroIndex = 0
					}
				} catch (error) {
					// 轮播配置不可用时保留本地兜底，首页其他数据不受影响。
				}
			},
			handleHeroChange(event) {
				this.heroIndex = event.detail.current
			},
			handleHeroTap(slide) {
				if (!slide || !slide.link || !slide.link.startsWith('/pages/')) return
				uni.navigateTo({ url: slide.link })
			},
			async loadStores() {
				try {
					const remoteStores = await fetchStores()
					this.catalogState = 'synced'
					if (!remoteStores.length) {
						this.stores = []
						this.storeCount = 0
						return
					}
					this.stores = remoteStores.map((store, index) => toHomeStore(store, index))
					// 城市筛选交给 visibleRooms 处理：该城市没有在售房型时改为推荐其他城市房源，
					// 但顶部城市名保持定位/手选的城市不变，不再悄悄改回「全部城市」。
					this.storeCount = this.stores.filter((store) => storeMatchesCity(store, this.city)).length
					const booking = getBooking()
					const selected = this.stores.find((store) => String(store.id) === String(booking.storeId))
					this.selectedStoreId = selected ? selected.id : this.stores[0].id
					if (!selected) this.persistBooking(this.stores[0])
				} catch (error) {
					this.catalogState = 'error'
					this.stores = []
					this.storeCount = 0
				}
			},
			applyBooking(booking) {
				this.booking = booking
				this.city = booking.city || this.city || '全部城市'
				this.region = booking.region || ''
				this.searchKeyword = booking.searchKeyword || ''
				this.checkIn = booking.checkIn
				this.checkOut = booking.checkOut
				this.checkInDay = weekdayOf(booking.isoCheckIn)
				this.checkOutDay = weekdayOf(booking.isoCheckOut)
				this.nights = `共 ${booking.nights || 1} 晚`
				this.guestSummary = `${booking.roomCount || 1}间 · ${booking.guestCount || 1}位成人 · 0位儿童`
				this.preferenceDraft = {
					...this.preferenceDraft,
					guestCount: Number(booking.guestCount || 1),
					bedCount: Number(booking.bedCount || 0),
					roomCount: Number(booking.roomCount || 1)
				}
				if (booking.storeId) this.selectedStoreId = booking.storeId
			},
			persistBooking(store = this.selectedStore) {
				if (!store) return
				this.booking = saveBooking({
					...this.booking,
					city: this.city,
					region: this.region,
					searchKeyword: this.searchKeyword,
					storeId: store.id,
					store: store.name,
					address: store.address || store.subtitle || '',
					roomImage: this.booking.roomImage || store.image
				})
				this.applyBooking(this.booking)
			},
			openSheet(type) {
				this.sheetType = type
				this.sheetVisible = true
				if (type === 'preference') {
					const booking = this.booking || getBooking()
					this.preferenceDraft = {
						...this.preferenceDraft,
						guestCount: Number(booking.guestCount || 1),
						bedCount: Number(booking.bedCount || 0),
						roomCount: Number(booking.roomCount || 1)
					}
					this.priceDragField = ''
					this.$nextTick(() => this.measurePriceRange())
				}
			},
			closeSheet() {
				this.sheetVisible = false
				this.sheetType = ''
			},
			// 城市切换走独立页面（当前定位 / 热门城市 / A-Z 索引），不再用底部弹层。
			openCityPicker() {
				uni.navigateTo({
					url: `/pages/city/select?city=${encodeURIComponent(this.city || '全部城市')}`
				})
			},
			openLocationSearch() {
				const query = [
					`city=${encodeURIComponent(this.city || '全部城市')}`,
					`region=${encodeURIComponent(this.region || '')}`,
					`keyword=${encodeURIComponent(this.searchKeyword || '')}`
				].join('&')
				uni.navigateTo({
					url: `/pages/search/index?${query}`
				})
			},
			openDatePage() {
				const store = this.selectedStore
				const query = ['from=home']
				if (store && store.id) query.push(`storeId=${encodeURIComponent(store.id)}`)
				uni.navigateTo({
					url: `/pages/booking/date?${query.join('&')}`
				})
			},
			selectStore(store) {
				this.selectedStoreId = store.id
				this.persistBooking(store)
				this.closeSheet()
				this.showMessage(`已选择${store.name}`)
			},
			selectDatePreset(preset) {
				const store = this.selectedStore
				if (!store || !store.id) {
					this.closeSheet()
					this.showMessage(this.catalogState === 'loading' ? '门店信息正在加载，请稍后再试' : '暂无可用门店，请先添加门店')
					return
				}
				this.booking = saveBooking({
					...this.booking,
					storeId: store.id,
					store: store.name,
					address: store.address,
					checkIn: preset.checkIn,
					checkOut: preset.checkOut,
					isoCheckIn: preset.isoCheckIn,
					isoCheckOut: preset.isoCheckOut,
					date: preset.date,
					nights: Number(preset.nights.replace(/\D/g, '')) || 1
				})
				this.applyBooking(this.booking)
				this.closeSheet()
			},
			adjustPreference(field, delta) {
				const minimum = field === 'guestCount' || field === 'roomCount' ? 1 : 0
				const maximum = field === 'guestCount' ? 20 : field === 'bedCount' ? 20 : 10
				const current = Number(this.preferenceDraft[field] || 0)
				this.preferenceDraft[field] = Math.max(minimum, Math.min(maximum, current + delta))
			},
			measurePriceRange() {
				uni.createSelectorQuery().in(this).select('.price-range').boundingClientRect((rect) => {
					if (!rect) return
					this.priceRangeLeft = rect.left
					this.priceRangeWidth = rect.width
				}).exec()
			},
			updatePriceFromClientX(clientX) {
				if (!this.priceDragField || !this.priceRangeWidth) return
				const ratio = Math.max(0, Math.min(1, (clientX - this.priceRangeLeft) / this.priceRangeWidth))
				const value = Math.round((ratio * 500) / 10) * 10
				if (this.priceDragField === 'priceMin') {
					this.preferenceDraft.priceMin = Math.min(value, Number(this.preferenceDraft.priceMax) - 20)
				} else {
					this.preferenceDraft.priceMax = Math.max(value, Number(this.preferenceDraft.priceMin) + 20)
				}
			},
			startPriceDrag(field) {
				this.priceDragField = field
				this.measurePriceRange()
			},
			movePriceDrag(event) {
				const touch = event.touches && event.touches[0]
				if (touch) this.updatePriceFromClientX(touch.clientX)
			},
			endPriceDrag() {
				this.priceDragField = ''
			},
			setPriceFromTrack(event) {
				const touch = (event.touches && event.touches[0]) || (event.changedTouches && event.changedTouches[0])
				if (!touch) return
				this.measurePriceRange()
				setTimeout(() => {
					const ratio = Math.max(0, Math.min(1, (touch.clientX - this.priceRangeLeft) / this.priceRangeWidth))
					const value = ratio * 500
					this.priceDragField = Math.abs(value - Number(this.preferenceDraft.priceMin)) <= Math.abs(value - Number(this.preferenceDraft.priceMax)) ? 'priceMin' : 'priceMax'
					this.updatePriceFromClientX(touch.clientX)
					this.priceDragField = ''
				}, 0)
			},
			confirmPreference() {
				const draft = this.preferenceDraft
				this.booking = saveBooking({
					...this.booking,
					roomCount: draft.roomCount,
					guestCount: draft.guestCount,
					bedCount: draft.bedCount
				})
				this.guestSummary = `${draft.roomCount}间 · ${draft.guestCount}位成人 · ${draft.bedCount}张床`
				this.priceSummary = `不限价格 / ¥${draft.priceMin}-${draft.priceMax}`
				this.closeSheet()
			},
			triggerNearby() {
				this.triggerLocation()
			},
			async triggerLocation() {
				if (this.locationState === 'locating') {
					return
				}
				this.showMessage('正在获取你的位置')
				const city = await this.requestLocationCity()
				if (city) return
				// 定位失败时：授权被拒过的，微信不会再弹窗，只能引导用户去设置页重新打开。
				this.openLocationSetting()
			},
			// 只有「用户明确拒绝过位置授权」才跳设置页，系统级定位关闭时跳过去也没用。
			async openLocationSetting() {
				const setting = await new Promise((resolve) => {
					uni.getSetting({ success: resolve, fail: () => resolve(null) })
				})
				const authorized = setting && setting.authSetting
					? setting.authSetting['scope.userLocation']
					: undefined
				if (authorized === false && uni.openSetting) {
					uni.openSetting({
						success: (result) => {
							if (result && result.authSetting && result.authSetting['scope.userLocation']) {
								this.requestLocationCity()
							}
						}
					})
				}
			},
			// 微信定位授权（腾讯定位）→ 逆地理编码换城市名 → 用城市刷新「本地热门精选」。
			async requestLocationCity(options = {}) {
				if (this.locationState === 'locating') return ''
				const silent = Boolean(options.silent)
				this.locationState = 'locating'
				const position = await new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						success: (result) => resolve(result),
						fail: (error) => {
							// 定位失败时把原始错误打出来：常见为 auth deny（未授权）、
							// system permission denied（系统定位关闭）、ERROR_NOCELL&WIFI_LOCATIONSWITCHOFF。
							console.warn('[定位] getLocation 失败：', error && (error.errMsg || error.message || error))
							resolve(null)
						}
					})
				})
				if (!position || typeof position.latitude !== 'number' || typeof position.longitude !== 'number') {
					this.locationState = 'failed'
					if (!silent) this.showMessage('未开启定位授权，为你展示全部城市')
					return ''
				}
				// 先记住用户坐标：即使后面逆地理编码失败，热门房型卡片也能算出「距您 x km」。
				this.userLocation = { latitude: position.latitude, longitude: position.longitude }
				// 关键排查点：这里打的是定位源真正给的坐标。
				// 开发者工具没开模拟位置时，坐标来自电脑系统定位 / 网络出口 IP，经常不是本机所在城市；
				// accuracy 为 65、speed 为 -1 基本就是开发者工具的模拟点位。
				console.log('[定位] 原始坐标：', {
					latitude: position.latitude,
					longitude: position.longitude,
					accuracy: position.accuracy,
					horizontalAccuracy: position.horizontalAccuracy,
					verticalAccuracy: position.verticalAccuracy,
					speed: position.speed,
					provider: position.provider || '',
					coordType: 'gcj02'
				})
				let city = ''
				try {
					const address = await reverseGeocode(position.latitude, position.longitude)
					console.log('[定位] 逆地理结果：', address)
					city = normalizeCityName(address.city || address.district || '')
				} catch (error) {
					console.warn('[定位] 逆地理编码失败：', error && (error.message || error.errMsg || error))
					city = ''
				}
				if (!city) {
					this.locationState = 'failed'
					if (!silent) this.showMessage('定位成功，但未能识别所在城市')
					return ''
				}
				this.locationState = 'ready'
				this.locatedCity = city
				// 缓存定位城市，「选择城市」页打开时能直接展示，不用重新定位。
				try {
					uni.setStorageSync('tcpms:located-city', city)
				} catch (error) {
					// 存储失败不影响当前页面。
				}
				this.applyLocatedCity(city, silent)
				return city
			},
			// 城市切换后同步列表条数、城市可选项与预订上下文。
			applyLocatedCity(city, silent = false) {
				if (!this.cities.includes(city)) {
					const rest = this.cities.filter((item) => item !== '全部城市')
					this.cities = ['全部城市', city, ...rest]
				}
				// 定位到的城市始终作为当前城市展示。
				// 以前这里在「该城市暂无门店」时会改回「全部城市」，导致用户定位到郑州却看到「全部城市」，
				// 现在保留城市名，列表改由 visibleRooms 推荐其他城市房源，并用 cityFallback 说明原因。
				this.city = city
				this.storeCount = this.stores.filter((store) => storeMatchesCity(store, city)).length
				this.booking = saveBooking({ ...this.booking, city })
				if (this.storeCount > 0) {
					if (!silent) this.showMessage(`已定位到${city}`)
					return
				}
				this.showMessage(`${city}暂无在售房型，为你推荐其他城市房源`)
			},
			handleService() {
				goPage('/pages/service/contact')
			},
			startScan() {
				uni.scanCode({
					onlyFromCamera: false,
					success: () => this.showMessage('已识别入住码，正在打开订单'),
					fail: () => this.showMessage('已取消扫码')
				})
			},
			handleSearch() {
				if (this.searching) {
					return
				}
				const store = this.selectedStore
				if (!store || !store.id) {
					this.showMessage(this.catalogState === 'loading'
						? '门店信息正在加载，请稍后再试'
						: this.catalogState === 'error'
							? '门店数据加载失败，请稍后重试'
							: '暂无可查询的民宿，请先添加门店')
					return
				}
				this.searching = true
				this.persistBooking(store)
				setTimeout(() => {
					this.searching = false
					const query = [
						`storeId=${encodeURIComponent(store.id)}`,
						`city=${encodeURIComponent(this.city || '')}`,
						`region=${encodeURIComponent(this.region || '')}`,
						`keyword=${encodeURIComponent(this.searchKeyword || '')}`
					].join('&')
					goPage(`/pages/room/list?${query}`)
				}, 450)
			},
			sortStores() {
				this.sortLabel = this.sortLabel === '距离最近优先' ? '评分最高优先' : '距离最近优先'
				this.showMessage(`已按“${this.sortLabel}”重排房态`)
			},
			// 门店距离文案：有定位就用实时算出来的公里数，没定位时退回门店自带文案。
			storeDistanceLabel(store) {
				if (!store) return ''
				const km = this.storeDistanceMap[store.id]
				if (Number.isFinite(km)) return `距您 ${formatDistanceKm(km)}`
				if (store.unknownDistance || !store.distance) return '距离未知'
				return `距您 ${store.distance}`
			},
			handleQuickEntrance(item) {
				if (item.key === 'popular') {
					uni.pageScrollTo({ selector: '#popularStores', duration: 280 })
					return
				}
				if (item.key === 'order') {
					uni.switchTab({ url: '/pages/order/list', fail: () => uni.reLaunch({ url: '/pages/order/list' }) })
					return
				}
				if (item.key === 'quality') {
					// 门店是 tab 页，query 会被丢弃，筛选意图改用内存中转传给 store/list
					setTabParams({ type: 'quality' })
					uni.switchTab({ url: '/pages/store/list', fail: () => uni.reLaunch({ url: '/pages/store/list' }) })
					return
				}
				this.selectedPortal = item
				this.openSheet('portal')
			},
			continuePortal() {
				const key = this.selectedPortal.key
				this.closeSheet()
				if (key === 'bed') {
					uni.navigateTo({ url: '/pages/room/list?type=bed' })
					return
				}
				if (key === 'house') {
					setTabParams({ type: 'whole' })
					uni.switchTab({ url: '/pages/store/list', fail: () => uni.reLaunch({ url: '/pages/store/list' }) })
					return
				}
				this.showMessage('已为你准备好推荐房源')
			},
			// 房型卡片与「立即预订」都进入房型详情，同时把门店上下文写进预订信息。
			openRoom(room) {
				if (!room || !room.id) return
				const store = this.stores.find((item) => String(item.id) === String(room.storeId))
				if (store) {
					this.selectedStoreId = store.id
					this.persistBooking(store)
				}
				goPage(storeRoomPath(room.storeId, room.id))
			},
			bookRoom(room) {
				this.openRoom(room)
			},
			goMap() {
				uni.navigateTo({
					url: '/pages/store/map'
				})
			},
			handleMenuItem(item) {
				if (item.route) {
					this.closeSheet()
					uni.navigateTo({ url: item.route })
					return
				}
				this.closeSheet()
				this.showMessage(item.message)
			},
			resetSearch() {
				// 重置回用户所在城市；没有定位结果时才回到「全部城市」，不再用写死的郑州兜底。
				this.city = this.locatedCity || '全部城市'
				this.region = ''
				this.searchKeyword = ''
				this.storeCount = this.stores.filter((store) => storeMatchesCity(store, this.city)).length
				this.preferenceDraft.priceMin = 0
				this.preferenceDraft.priceMax = 500
				this.priceSummary = '不限价格 / ¥0-¥500'
				this.selectedFilters = []
				this.booking = saveBooking({
					...this.booking,
					city: this.city,
					region: '',
					searchKeyword: ''
				})
				this.searching = false
				this.showMessage('筛选条件已清除')
			},
			handleHeroImageError(index = 0) {
				const slide = this.heroSlides[index]
				// 托管在后端的品牌图加载失败时，先用包内同款图片兜底；再失败才用门店实拍图。
				if (slide && /^https?:/i.test(String(slide.image || ''))) {
					slide.image = [brandImageFallbacks.hero, brandImageFallbacks.storeCity, brandImageFallbacks.hostel][index % 3]
					return
				}
				const fallback = this.stores[0] && this.stores[0].image
				if (!fallback || !this.heroSlides[index]) return
				this.heroSlides[index].image = fallback
				this.heroImage = fallback
			},
			showMessage(message) {
				this.toast = message
				if (this.toastTimer) {
					clearTimeout(this.toastTimer)
				}
				this.toastTimer = setTimeout(() => {
					this.toast = ''
				}, 1800)
			}
		},
		onUnload() {
			if (this.toastTimer) {
				clearTimeout(this.toastTimer)
			}
		}
	}
</script>

<style lang="scss">
	$surface: #ffffff;
	$page: #f5fafb;
	$primary: #006a6a;
	$primary-container: #2aa9a9;
	$primary-fixed: #95f1fc;
	$secondary: #006971;
	$secondary-fixed: #95f1fc;
	$text: #171d1e;
	$text-variant: #3d4949;
	$text-muted: #6d7979;
	$outline: #bcc9c8;
	$surface-container: #eaeff0;
	$surface-low: #eff4f5;
	$error: #ba1a1a;

	page {
		background: $page;
	}

	.home-page {
		overflow-x: hidden;
		min-height: 100vh;
		padding-bottom: 132rpx;
		padding-bottom: calc(132rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(132rpx + env(safe-area-inset-bottom));
		color: $text;
		background: $page;
		font-family: "PingFang SC", "Microsoft YaHei", Arial, sans-serif;
	}

	.home-page button {
		margin: 0;
		padding: 0;
		border: 0;
	}


	.page-content {
		padding-top: 0;
		padding-top: constant(safe-area-inset-top);
		padding-top: env(safe-area-inset-top);
	}


	.hero-shell {
		padding: 0;
	}

	.hero-swiper {
		width: 100%;
		height: 472rpx;
	}

	.hero-media {
		position: relative;
		height: 100%;
		overflow: hidden;
		border-radius: 0;
		box-shadow: 0 8rpx 20rpx rgba(23, 29, 30, 0.09);
	}

	.hero-image,
	.store-image {
		display: block;
		width: 100%;
		height: 100%;
	}

	.hero-shade {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		background: linear-gradient(to top, rgba(23, 29, 30, 0.76), rgba(23, 29, 30, 0.08) 64%, rgba(23, 29, 30, 0));
	}

	.hero-copy {
		position: absolute;
		right: 32rpx;
		bottom: 26rpx;
		left: 32rpx;
		display: flex;
		flex-direction: column;
		align-items: flex-start;
	}

	.hero-badge {
		display: inline-flex;
		align-items: center;
		padding: 6rpx 12rpx;
		color: #ffffff;
		background: rgba(0, 106, 106, 0.8);
		border-radius: 6rpx;
		font-size: 18rpx;
		font-weight: 600;
	}

	.hero-badge .proto-icon {
		margin-right: 6rpx;
	}

	.hero-title {
		margin-top: 8rpx;
		color: #ffffff;
		font-size: 34rpx;
		font-weight: 750;
		line-height: 1.25;
	}

	.hero-subtitle {
		margin-top: 4rpx;
		color: rgba(255, 255, 255, 0.82);
		font-size: 19rpx;
		line-height: 1.4;
	}

	.hero-indicators {
		position: absolute;
		right: 32rpx;
		bottom: 18rpx;
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.hero-indicator {
		display: block;
		width: 12rpx;
		height: 12rpx;
		background: rgba(255, 255, 255, 0.62);
		border-radius: 50%;
	}

	.hero-indicator.active {
		width: 32rpx;
		background: #95f1fc;
		border-radius: 8rpx;
	}

	.booking-shell {
		position: relative;
		z-index: 3;
		margin: -52rpx 24rpx 30rpx;
	}

	.booking-panel {
		padding: 30rpx 28rpx 26rpx;
		background: $surface;
		border-radius: 28rpx;
		box-shadow: 0 20rpx 56rpx rgba(24, 133, 143, 0.14);
	}

	.search-destination-row {
		display: flex;
		align-items: center;
		min-height: 86rpx;
	}

	.destination-main {
		display: flex;
		flex: 1;
		align-items: center;
		min-width: 0;
		color: $text;
		background: transparent;
		text-align: left;
	}

	.destination-main > .proto-icon {
		margin-right: 10rpx;
	}

	.destination-copy {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.destination-city-row {
		display: flex;
		align-items: center;
		min-width: 0;
	}

	.destination-city {
		overflow: hidden;
		color: $text;
		font-size: 28rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.destination-city-row .proto-icon {
		margin-left: 6rpx;
	}

	.destination-placeholder {
		display: block;
		overflow: hidden;
		margin-top: 5rpx;
		color: $text-muted;
		font-size: 18rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.destination-tools {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-left: 10rpx;
	}

	.destination-tool {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		width: 84rpx;
		min-height: 74rpx;
		color: $text-variant;
		background: transparent;
		border-radius: 0;
		font-size: 16rpx;
	}

	.destination-tool .proto-icon {
		margin-bottom: 3rpx;
	}

	.date-row,
	.preference-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}


	.field-label {
		display: flex;
		align-items: center;
		color: $text-muted;
		font-size: 18rpx;
		line-height: 1.3;
	}

	.field-label .proto-icon {
		margin-right: 6rpx;
	}


	.booking-divider {
		width: 100%;
		height: 1rpx;
		margin: 22rpx 0;
		background: $surface-container;
	}

	.date-row {
		min-height: 82rpx;
	}

	.date-side {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.date-side-right {
		align-items: flex-end;
	}

	.date-value-row {
		display: flex;
		align-items: baseline;
		margin-top: 6rpx;
	}

	.date-value {
		color: $text;
		font-size: 32rpx;
		font-weight: 750;
		white-space: nowrap;
	}

	.date-day {
		margin-left: 8rpx;
		color: $text-muted;
		font-size: 18rpx;
		white-space: nowrap;
	}

	.night-block {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		margin: 0 10rpx;
	}

	.night-count {
		padding: 6rpx 14rpx;
		color: #004f55;
		background: $secondary-fixed;
		border-radius: 18rpx;
		font-size: 19rpx;
		font-weight: 700;
		white-space: nowrap;
	}

	.night-line {
		display: block;
		width: 90rpx;
		height: 1rpx;
		margin-top: 8rpx;
		background: rgba(109, 121, 121, 0.42);
	}

	.preference-row {
		min-height: 84rpx;
		padding-bottom: 8rpx;
	}

	.preference-summary {
		flex: 1;
		min-width: 0;
	}

	.preference-title {
		display: flex;
		align-items: center;
		color: $text;
		font-size: 22rpx;
		font-weight: 600;
	}

	.preference-title .proto-icon {
		margin-right: 8rpx;
	}

	.preference-tags {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 10rpx;
		overflow: hidden;
	}

	.preference-tag {
		padding: 5rpx 10rpx;
		color: $text-muted;
		background: $surface-container;
		border-radius: 4rpx;
		font-size: 16rpx;
		white-space: nowrap;
	}

	.preference-tag-primary {
		color: $primary;
		background: rgba(149, 241, 252, 0.45);
	}

	.preference-row > .proto-icon {
		margin-left: 12rpx;
	}

	.search-submit {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 84rpx;
		margin-top: 44rpx;
		color: #ffffff;
		background: $primary-container;
		border-radius: 44rpx;
		box-shadow: 0 8rpx 24rpx rgba(42, 169, 169, 0.3);
		font-size: 28rpx;
		font-weight: 700;
	}

	.search-submit.searching {
		opacity: 0.72;
	}

	.search-submit .proto-icon {
		margin-right: 10rpx;
	}

	.assurance-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 18rpx 4rpx 0;
		color: $text-muted;
		font-size: 16rpx;
		white-space: nowrap;
	}

	.assurance-row > view {
		display: flex;
		align-items: center;
	}

	.assurance-row .proto-icon {
		margin-right: 4rpx;
	}

	.quick-grid {
		display: flex;
		flex-wrap: nowrap;
		gap: 12rpx;
		margin: 0 24rpx 30rpx;
	}

	.quick-card {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		width: calc(33.3333% - 8rpx);
		min-width: 0;
		min-height: 142rpx;
		padding: 12rpx 4rpx 10rpx;
		background: $surface;
		border-radius: 20rpx;
		box-shadow: 0 4rpx 16rpx rgba(23, 29, 30, 0.04);
		text-align: center;
	}

	.quick-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 60rpx;
		height: 60rpx;
		margin-bottom: 8rpx;
		border-radius: 18rpx;
	}

	.quick-icon image {
		display: block;
		width: 60rpx;
		height: 60rpx;
	}

	.quick-card-cyan .quick-icon {
		color: $primary;
		background: rgba(149, 241, 252, 0.52);
	}

	.quick-card-blue .quick-icon {
		color: $primary;
		background: rgba(131, 244, 244, 0.42);
	}


	.quick-card-gray .quick-icon {
		color: $text-variant;
		background: $surface-container;
	}

	.quick-title {
		display: block;
		max-width: 100%;
		overflow: hidden;
		color: $text;
		font-size: 20rpx;
		font-weight: 600;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.quick-desc {
		display: block;
		margin-top: 5rpx;
		overflow: hidden;
		color: $text-muted;
		font-size: 16rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.popular-section {
		padding: 0 24rpx 30rpx;
	}

	.popular-loading {
		padding: 60rpx 0;
		color: $text-muted;
		font-size: 22rpx;
		text-align: center;
	}

	.popular-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 18rpx;
	}

	.city-fallback-tip {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-bottom: 20rpx;
		padding: 18rpx 22rpx;
		color: #8a6d3b;
		font-size: 22rpx;
		line-height: 1.4;
		background: #fdf6e7;
		border: 1rpx solid #f5e3bd;
		border-radius: 16rpx;
	}

	.city-fallback-tip text {
		flex: 1;
		min-width: 0;
	}

	.city-fallback-tip .city-fallback-action {
		flex: 0 0 auto;
		color: var(--proto-primary-deep);
		font-weight: 600;
		white-space: nowrap;
	}

	.distance-hint {
		display: flex;
		align-items: center;
		gap: 8rpx;
		margin-bottom: 18rpx;
		padding: 14rpx 20rpx;
		color: $text-variant;
		font-size: 20rpx;
		background: rgba(149, 241, 252, 0.22);
		border: 1rpx solid rgba(42, 169, 169, 0.22);
		border-radius: 14rpx;
	}

	.distance-hint text {
		flex: 1;
		min-width: 0;
	}

	.distance-hint .distance-hint-action {
		flex: 0 0 auto;
		color: $primary;
		font-weight: 600;
		white-space: nowrap;
	}

	.popular-title-group {
		display: flex;
		align-items: center;
		min-width: 0;
	}

	.popular-title {
		color: $text;
		font-size: 30rpx;
		font-weight: 750;
		white-space: nowrap;
	}

	.popular-badge {
		margin-left: 12rpx;
		padding: 5rpx 12rpx;
		color: #006f78;
		background: $secondary-fixed;
		border-radius: 16rpx;
		font-size: 16rpx;
		font-weight: 600;
		white-space: nowrap;
	}

	.sort-button {
		display: flex;
		align-items: center;
		margin-left: 12rpx;
		color: $primary;
		background: transparent;
		font-size: 18rpx;
		white-space: nowrap;
	}

	.sort-button .proto-icon {
		margin-left: 5rpx;
	}

	.store-list {
		display: flex;
		flex-direction: column;
		gap: 20rpx;
	}

	.store-card {
		overflow: hidden;
		padding: 12rpx 12rpx 0;
		background: $surface;
		border-radius: 20rpx;
		box-shadow: 0 8rpx 28rpx rgba(42, 169, 169, 0.07);
	}

	.store-image-wrap {
		position: relative;
		height: 352rpx;
		overflow: hidden;
		border-radius: 16rpx;
	}

	.store-media-grid {
		display: grid;
		width: 100%;
		height: 100%;
		gap: 8rpx;
		background: $surface-container;
	}

	.store-media-grid-1 {
		grid-template-columns: minmax(0, 1fr);
	}

	.store-media-grid-2 {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}

	.store-media-grid-3 {
		grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
		grid-template-rows: repeat(2, minmax(0, 1fr));
	}

	.store-media-grid-3 .store-image:first-child {
		grid-row: 1 / span 2;
	}

	.store-image {
		display: block;
		width: 100%;
		height: 100%;
		min-width: 0;
		min-height: 0;
		background: $surface-container;
	}

	.store-image-tags {
		position: absolute;
		top: 20rpx;
		left: 20rpx;
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.distance-badge,
	.area-badge {
		padding: 6rpx 12rpx;
		border-radius: 5rpx;
		font-size: 16rpx;
		font-weight: 600;
		line-height: 1.2;
		white-space: nowrap;
	}

	.distance-badge {
		color: $primary;
		background: rgba(255, 255, 255, 0.9);
	}

	.area-badge {
		color: #ffffff;
		background: $primary;
	}

	.area-badge.badge-secondary {
		background: $secondary;
	}


	.rating-badge {
		position: absolute;
		left: 20rpx;
		bottom: 18rpx;
		display: flex;
		align-items: center;
		padding: 7rpx 12rpx;
		color: #ffffff;
		background: rgba(23, 29, 30, 0.76);
		border-radius: 5rpx;
		font-size: 16rpx;
	}

	.rating-badge .proto-icon {
		margin-right: 4rpx;
	}

	.rating-number {
		font-weight: 700;
	}

	.rating-reviews {
		margin-left: 4rpx;
		color: rgba(255, 255, 255, 0.76);
	}

	.store-card-body {
		padding: 24rpx 16rpx 28rpx;
	}

	.store-heading {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
	}

	.store-heading-copy {
		flex: 1;
		min-width: 0;
		padding-right: 12rpx;
	}

	.store-card-title {
		display: block;
		color: $text;
		font-size: 27rpx;
		font-weight: 700;
		line-height: 1.35;
	}

	.store-room {
		display: block;
		margin-top: 4rpx;
		overflow: hidden;
		color: $text-variant;
		font-size: 18rpx;
		line-height: 1.4;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.stock-badge {
		flex: 0 0 auto;
		padding: 5rpx 10rpx;
		border-radius: 16rpx;
		font-size: 16rpx;
		font-weight: 600;
		white-space: nowrap;
	}


	.stock-success {
		color: #006f78;
		background: $secondary-fixed;
	}


	.tag-list {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 14rpx;
	}

	.store-tag {
		padding: 5rpx 10rpx;
		color: $text-muted;
		background: $surface-container;
		border-radius: 4rpx;
		font-size: 16rpx;
		white-space: nowrap;
	}

	.store-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 18rpx;
	}

	.price-block {
		display: flex;
		align-items: baseline;
		min-width: 0;
	}

	.price-currency {
		color: $error;
		font-size: 18rpx;
		font-weight: 700;
	}

	.price-value {
		margin-left: 2rpx;
		color: $error;
		font-size: 48rpx;
		font-weight: 750;
		line-height: 1;
	}

	.price-unit {
		margin-left: 6rpx;
		color: $text-muted;
		font-size: 17rpx;
	}

	.book-button {
		height: 66rpx;
		padding: 0 26rpx;
		color: #ffffff;
		background: $primary-container;
		border-radius: 18rpx;
		font-size: 21rpx;
		font-weight: 650;
		line-height: 66rpx;
	}


	.brand-footer {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		padding: 6rpx 24rpx 34rpx;
		text-align: center;
	}

	.brand-footer-line {
		display: flex;
		align-items: center;
		color: $text-muted;
		font-size: 16rpx;
		letter-spacing: 1rpx;
		white-space: nowrap;
	}

	.brand-footer-rule {
		width: 56rpx;
		height: 1rpx;
		margin: 0 10rpx;
		background: rgba(109, 121, 121, 0.42);
	}

	.brand-footer-desc {
		margin-top: 8rpx;
		color: $outline;
		font-size: 16rpx;
	}


	.toast-message {
		position: fixed;
		right: 50%;
		bottom: 154rpx;
		bottom: calc(154rpx + constant(safe-area-inset-bottom));
		bottom: calc(154rpx + env(safe-area-inset-bottom));
		z-index: 80;
		max-width: 78%;
		padding: 18rpx 28rpx;
		color: #ffffff;
		background: rgba(44, 49, 50, 0.92);
		border-radius: 18rpx;
		font-size: 21rpx;
		line-height: 1.35;
		text-align: center;
		transform: translateX(50%);
	}

	.sheet-mask {
		position: fixed;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 70;
		display: flex;
		align-items: flex-end;
		background: rgba(23, 29, 30, 0.42);
	}

	.bottom-sheet {
		width: 100%;
		max-height: 84vh;
		overflow-y: auto;
		padding: 16rpx 28rpx 30rpx;
		padding-bottom: calc(30rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(30rpx + env(safe-area-inset-bottom));
		background: $surface;
		border-radius: 30rpx 30rpx 0 0;
	}

	.preference-bottom-sheet {
		max-height: 90vh;
		padding: 28rpx 30rpx calc(30rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(30rpx + env(safe-area-inset-bottom));
		border-radius: 28rpx 28rpx 0 0;
	}

	.preference-bottom-sheet .sheet-handle {
		display: none;
	}

	.sheet-handle {
		width: 72rpx;
		height: 8rpx;
		margin: 0 auto 18rpx;
		background: $surface-container;
		border-radius: 8rpx;
	}

	.sheet-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		min-height: 64rpx;
	}

	.preference-bottom-sheet .sheet-header {
		position: relative;
		justify-content: center;
		min-height: 70rpx;
	}

	.sheet-title {
		color: $text;
		font-size: 30rpx;
		font-weight: 750;
	}

	.preference-bottom-sheet .sheet-title {
		font-size: 34rpx;
		font-weight: 800;
	}

	.sheet-close {
		width: 58rpx;
		height: 58rpx;
		color: $text-muted;
		background: $surface-low;
		border-radius: 50%;
		font-size: 38rpx;
		line-height: 56rpx;
	}

	.preference-bottom-sheet .sheet-close {
		position: absolute;
		top: 4rpx;
		right: 0;
		width: 64rpx;
		height: 64rpx;
		color: $primary;
		background: transparent;
		border-radius: 0;
	}

	.sheet-content {
		padding-top: 22rpx;
	}

	.preference-sheet-content {
		padding-top: 24rpx;
	}

	.preference-row-list {
		display: flex;
		flex-direction: column;
		gap: 22rpx;
	}

	.preference-control-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		min-height: 92rpx;
	}

	.preference-control-label {
		color: $text;
		font-size: 30rpx;
		font-weight: 650;
	}

	.preference-stepper {
		display: flex;
		align-items: center;
		gap: 26rpx;
	}

	.preference-stepper-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		padding: 0;
		color: $primary-container;
		background: transparent;
		border: 3rpx solid $primary-container;
		border-radius: 50%;
	}

	.preference-stepper-value {
		min-width: 42rpx;
		color: $text;
		font-size: 32rpx;
		text-align: center;
	}

	.preference-price-section {
		margin-top: 26rpx;
		padding-top: 24rpx;
		border-top: 1rpx solid $surface-container;
	}

	.preference-price-heading,
	.price-range-labels {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.preference-price-heading {
		color: $text;
		font-size: 23rpx;
		font-weight: 650;
	}

	.preference-price-heading > text:last-child {
		color: $primary;
		font-size: 21rpx;
	}

	.price-range {
		position: relative;
		height: 76rpx;
		margin: 10rpx 10rpx 0;
	}

	.price-range-track,
	.price-range-active {
		position: absolute;
		top: 35rpx;
		height: 8rpx;
		border-radius: 8rpx;
	}

	.price-range-track {
		right: 0;
		left: 0;
		background: $surface-container;
	}

	.price-range-active {
		background: $primary-container;
	}

	.price-range-handle {
		position: absolute;
		top: 18rpx;
		width: 42rpx;
		height: 42rpx;
		margin-left: -21rpx;
		background: $surface;
		border: 4rpx solid $primary-container;
		border-radius: 50%;
		box-shadow: 0 4rpx 10rpx rgba(42, 169, 169, 0.18);
	}

	.price-range-labels {
		color: $text-muted;
		font-size: 18rpx;
	}

	.preference-confirm-button {
		margin-top: 34rpx;
	}

	.sheet-caption {
		display: block;
		margin: 22rpx 0 12rpx;
		color: $text-muted;
		font-size: 19rpx;
	}

	.current-city,
	.store-option,
	.date-preset,
	.menu-option {
		display: flex;
		align-items: center;
		padding: 22rpx;
		background: $surface-low;
		border-radius: 16rpx;
	}

	.current-city {
		color: $primary;
		background: #eaf8f6;
		border: 1rpx solid #cfe8e5;
		font-size: 23rpx;
	}

	.current-city .proto-icon {
		margin-right: 10rpx;
	}

	.current-city-state {
		margin-left: auto;
		color: $text-muted;
		font-size: 18rpx;
	}

	.city-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
	}

	.city-option {
		padding: 20rpx 8rpx;
		color: $text;
		background: $surface-low;
		border: 1rpx solid transparent;
		border-radius: 14rpx;
		font-size: 21rpx;
		text-align: center;
	}

	.city-option {
		width: calc(33.333% - 10rpx);
	}

	.city-option.selected,
	.store-option.selected {
		color: $primary;
		background: #eaf8f6;
		border-color: #c1e3e0;
	}

	.store-option-list,
	.date-preset-list,
	.menu-sheet-content {
		display: flex;
		flex-direction: column;
		gap: 14rpx;
	}

	.store-option-copy,
	.menu-option-copy {
		flex: 1;
		min-width: 0;
	}

	.store-option-title,
	.preset-title,
	.menu-option-title {
		display: block;
		color: $text;
		font-size: 23rpx;
		font-weight: 650;
	}

	.store-option-desc,
	.preset-detail,
	.menu-option-desc {
		display: block;
		margin-top: 6rpx;
		color: $text-muted;
		font-size: 18rpx;
		line-height: 1.45;
	}


	.date-sheet-summary {
		display: flex;
		align-items: center;
		padding: 22rpx;
		background: #eaf8f6;
		border-radius: 18rpx;
	}

	.date-sheet-summary > view {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.date-sheet-label {
		color: $text-muted;
		font-size: 18rpx;
	}

	.date-sheet-value {
		margin-top: 8rpx;
		color: $primary;
		font-size: 30rpx;
		font-weight: 750;
	}

	.date-sheet-summary > .proto-icon {
		margin: 0 18rpx;
	}

	.date-sheet-night {
		padding: 8rpx 12rpx;
		color: $primary;
		background: #d7eeeb;
		border-radius: 16rpx;
		font-size: 18rpx;
		white-space: nowrap;
	}

	.date-preset {
		justify-content: space-between;
	}


	.sheet-primary-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 84rpx;
		margin-top: 26rpx;
		color: #ffffff;
		background: $primary-container;
		border-radius: 42rpx;
		box-shadow: 0 8rpx 24rpx rgba(42, 169, 169, 0.22);
		font-size: 26rpx;
		font-weight: 700;
	}

	.portal-sheet {
		display: flex;
		align-items: center;
		flex-direction: column;
		text-align: center;
	}

	.portal-sheet-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 92rpx;
		height: 92rpx;
		color: $primary;
		background: #eaf8f6;
		border-radius: 28rpx;
	}

	.portal-sheet-title {
		margin-top: 18rpx;
		color: $text;
		font-size: 30rpx;
		font-weight: 750;
	}

	.portal-sheet-desc {
		margin-top: 12rpx;
		color: $text-muted;
		font-size: 21rpx;
		line-height: 1.6;
	}

	.menu-option {
		padding: 24rpx;
	}

	.menu-option-icon {
		margin-right: 16rpx;
	}

	.menu-option > .proto-icon:last-child {
		margin-left: 12rpx;
	}
</style>
