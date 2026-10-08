<template>
	<view class="home-page">
		<view class="page-content">
			<view class="hero-shell">
				<swiper class="hero-swiper" :current="heroIndex" circular autoplay :interval="4600" :duration="420" @change="handleHeroChange">
					<swiper-item v-for="(slide, index) in heroSlides" :key="slide.image">
						<view class="hero-media">
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
								<view class="destination-city-row">
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
								<ProtoIcon name="group" :size="28" />
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
						<ProtoIcon name="search" tone="white" :size="30" />
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
						<text class="popular-badge">实时好房</text>
					</view>
					<button class="sort-button" hover-class="none" @tap="sortStores">
						<text>{{ sortLabel }}</text>
						<ProtoIcon name="tune" :size="24" />
					</button>
				</view>

				<view v-if="visibleStores.length" class="store-list">
					<view
						v-for="store in visibleStores"
						:key="store.id"
						class="store-card"
						@tap="openStore(store)"
					>
						<view class="store-image-wrap">
							<view class="store-media-grid" :class="`store-media-grid-${Math.min(store.images.length, 3)}`">
								<image
									v-for="(image, imageIndex) in store.images.slice(0, 3)"
									:key="`${store.id}-${imageIndex}-${image}`"
									class="store-image"
									:src="image"
									mode="aspectFill"
								></image>
							</view>
							<view class="store-image-tags">
								<text class="distance-badge">距您 {{ store.distance }}</text>
								<text class="area-badge" :class="store.badgeTone">{{ store.badge }}</text>
							</view>
							<view class="rating-badge">
								<ProtoIcon name="star" :size="18" />
								<text class="rating-number">{{ store.rating }}</text>
								<text class="rating-reviews">· {{ store.reviews }}条好评</text>
							</view>
						</view>
						<view class="store-card-body">
							<view class="store-heading">
								<view class="store-heading-copy">
									<text class="store-card-title">{{ store.name }}</text>
									<text class="store-room">{{ store.room }}</text>
								</view>
								<text class="stock-badge" :class="store.stockTone">{{ store.stock }}</text>
							</view>
							<view class="tag-list">
								<text v-for="tag in store.tags" :key="tag" class="store-tag">{{ tag }}</text>
							</view>
							<view class="store-footer">
								<view class="price-block">
									<text class="price-currency">¥</text>
									<text class="price-value">{{ store.price }}</text>
									<text class="price-unit">/ {{ store.unit }}起</text>
								</view>
								<button class="book-button" hover-class="none" @tap.stop="bookStore(store)">立即预订</button>
							</view>
						</view>
					</view>
				</view>
				<ProtoEmptyState v-else action-text="清除筛选" @action="resetSearch" />
			</view>

			<view class="brand-footer">
				<view class="brand-footer-line">
					<text class="brand-footer-rule"></text>
					<text>TCPMS 智能旅宿云管系统承载</text>
					<text class="brand-footer-rule"></text>
				</view>
				<text class="brand-footer-desc">房源直联房态系统 · 零押金免查房 · 离店开票</text>
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

				<view v-if="sheetType === 'city'" class="sheet-content">
					<text class="sheet-caption">当前城市</text>
					<view class="current-city" @tap="selectCity(city)">
						<text>{{ city }}</text>
						<text class="current-city-state">已定位</text>
					</view>
					<text class="sheet-caption">热门城市</text>
					<view class="city-grid">
						<text
							v-for="item in cities"
							:key="item"
							class="city-option"
							:class="{ selected: city === item }"
							@tap="selectCity(item)"
						>{{ item }}</text>
					</view>
				</view>

				<view v-else-if="sheetType === 'store'" class="sheet-content">
					<text class="sheet-caption">选择入住门店</text>
					<view class="store-option-list">
						<view
							v-for="store in stores"
							:key="store.id"
							class="store-option"
							:class="{ selected: selectedStore.id === store.id }"
							@tap="selectStore(store)"
						>
							<view class="store-option-copy">
								<text class="store-option-title">{{ store.name }}</text>
								<text class="store-option-desc">{{ store.subtitle }} · 距您 {{ store.distance }}</text>
							</view>
							<ProtoIcon v-if="selectedStore.id === store.id" name="check" :size="24" />
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
	import { getBooking, recordRecent, saveBooking } from '@/common/app-store.js'
	import { fetchStores } from '@/common/api.js'
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

	const normalizeStoreImages = (store = {}, fallback = {}) => {
		const candidates = [store.images, store.imageUrls, store.gallery, store.photos]
			.find((images) => Array.isArray(images) && images.length)
		const fallbackImages = Array.isArray(fallback.images) && fallback.images.length
			? fallback.images
			: [fallback.image].filter(Boolean)
		const toImageUrl = (item) => {
			if (typeof item === 'string') return item
			if (!item || typeof item !== 'object') return ''
			return item.url || item.imageUrl || item.src || item.path || ''
		}
		const images = (candidates || fallbackImages).map(toImageUrl).filter(Boolean)
		if (!images.length && (store.image || fallback.image)) {
			images.push(store.image || fallback.image)
		}
		return images.slice(0, 3)
	}

	const toHomeStore = (store, index = 0) => {
		const fallback = demoStores[index] || demoStores[0]
		const storeRooms = Array.isArray(store.rooms) ? store.rooms : []
		return {
			...fallback,
			...store,
			id: store.id || fallback.id,
			name: store.name || fallback.name,
			subtitle: store.address || fallback.address,
			address: store.address || store.subtitle || fallback.address,
			room: store.room || fallback.room || '品质房型 · 独立卫浴 · 采光飘窗',
			featuredRoomId: store.featuredRoomId || store.roomId || (storeRooms[0] && storeRooms[0].id) || fallback.featuredRoomId || demoRooms[0].id,
			badge: store.badge || fallback.badge || (index === 0 ? '官方直营' : '品质优选'),
			badgeTone: store.badgeTone || (index % 2 ? 'badge-secondary' : 'badge-primary'),
			status: store.status || fallback.status,
			rating: store.rating || fallback.rating,
			reviews: store.reviews || fallback.reviews,
			stock: store.stock || '今日可预订',
			stockTone: store.stockTone || 'stock-success',
			tags: store.tags && store.tags.length ? store.tags : fallback.tags,
			price: store.price || fallback.price,
			unit: store.unit || '晚',
			image: store.image || fallback.image,
			images: normalizeStoreImages(store, fallback)
		}
	}

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
				city: booking.city || '郑州市',
				region: booking.region || '',
				searchKeyword: booking.searchKeyword || '',
				storeCount: demoStores.length,
				selectedStoreId: booking.storeId || demoStores[0].id,
				checkIn: booking.checkIn,
				checkOut: booking.checkOut,
				checkInDay: weekdayOf(booking.isoCheckIn),
				checkOutDay: weekdayOf(booking.isoCheckOut),
				nights: `共 ${booking.nights || 1} 晚`,
				guestSummary: `${booking.roomCount || 1}间 · ${booking.guestCount || 1}位成人 · 0位儿童`,
				priceSummary: '不限价格 / ¥150-¥500',
				preferenceDraft: {
					guestCount: Number(booking.guestCount || 1),
					bedCount: Number(booking.bedCount || 0),
					roomCount: Number(booking.roomCount || 1),
					priceMin: 150,
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
				priceOptions: ['不限价格 / ¥150-¥500', '¥0 - ¥200', '¥200 - ¥400', '¥400 - ¥800', '¥800以上'],
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
				stores: [
					{
						id: 1,
						featuredRoomId: 1,
						name: '云舍民宿 · 郑州旗舰店',
						subtitle: '金水区核心商圈 · 近二七广场',
						room: '标准大床房 · 28㎡ | 独立卫浴 | 采光飘窗',
						distance: '1.2km',
						badge: '金水区商务核心区',
						badgeTone: 'badge-primary',
						rating: '4.9',
						reviews: '1280',
						stock: '仅剩 2 间',
						stockTone: 'stock-danger',
						tags: ['免费千兆WiFi', '智能客控', '免费行李寄存'],
						price: '300',
						unit: '晚',
						image: PROTOTYPE_IMAGES.storeOne,
						images: [PROTOTYPE_IMAGES.storeOne, PROTOTYPE_IMAGES.storeTwo, PROTOTYPE_IMAGES.hostel]
					},
					{
						id: 2,
						featuredRoomId: 2,
						name: '城市花园民宿 · 郑州二七广场店',
						subtitle: '近地铁 1/3 号线',
						room: '西湖景观/城景双床房 · 35㎡ | 高清百寸投影',
						distance: '2.5km',
						badge: '近地铁1/3号线',
						badgeTone: 'badge-secondary',
						rating: '4.8',
						reviews: '890',
						stock: '库存充足',
						stockTone: 'stock-success',
						tags: ['巨幕影音', '乳胶床垫', '落日景观露台'],
						price: '368',
						unit: '晚',
						image: PROTOTYPE_IMAGES.storeTwo,
						images: [PROTOTYPE_IMAGES.storeTwo, PROTOTYPE_IMAGES.storeOne]
					},
					{
						id: 3,
						featuredRoomId: 3,
						name: '青年旅舍床位专区 · 郑州大学科技园店',
						subtitle: '大学科技园区',
						room: '女生四人间单床位 · 独立隐私帘/专属密码柜',
						distance: '4.8km',
						badge: '大学科技园区',
						badgeTone: 'badge-tertiary',
						rating: '4.9',
						reviews: '452',
						stock: '今日余 3 床位',
						stockTone: 'stock-neutral',
						tags: ['自习阅读吧', '咖啡共享公区', '干湿分离卫浴'],
						price: '69',
						unit: '床',
						image: PROTOTYPE_IMAGES.storeThree,
						images: [PROTOTYPE_IMAGES.storeThree]
					}
				]
			}
		},
		onLoad() {
			this.loadStores()
		},
		onShow() {
			this.applyBooking(getBooking())
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
			visibleStores() {
				if (!this.storeCount) {
					return []
				}
				const stores = this.stores.filter((store) => {
					const cityMatch = storeMatchesCity(store, this.city)
					const price = Number(store.price || 0)
					const priceMatch = price >= Number(this.preferenceDraft.priceMin || 0) && price <= Number(this.preferenceDraft.priceMax || 500)
					const text = `${store.name} ${store.room} ${(store.tags || []).join(' ')}`
					const featureMatch = this.selectedFilters.every((filter) => {
						if (filter === '独立房间') return text.indexOf('房') >= 0 && text.indexOf('床位') < 0
						if (filter === '多人床位') return text.indexOf('床位') >= 0 || text.indexOf('青年') >= 0
						if (filter === '整套出租') return text.indexOf('整套') >= 0 || text.indexOf('度假') >= 0
						if (filter === '可带宠物') return text.indexOf('宠物') >= 0
						return true
					})
					return cityMatch && priceMatch && featureMatch
				})
				if (this.sortLabel === '评分最高优先') {
					return stores.sort((left, right) => Number(right.rating) - Number(left.rating))
				}
				return stores.sort((left, right) => parseFloat(left.distance) - parseFloat(right.distance))
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
				return titles[this.sheetType] || 'TCPMS'
			}
		},
		methods: {
			handleHeroChange(event) {
				this.heroIndex = event.detail.current
			},
			async loadStores() {
				try {
					const remoteStores = await fetchStores()
					if (!remoteStores.length) return
					this.stores = remoteStores.map((store, index) => toHomeStore(store, index))
					if (!this.stores.some((store) => storeMatchesCity(store, this.city))) {
						this.city = this.stores[0].city || '全部城市'
					}
					this.storeCount = this.stores.length
					this.storeCount = this.stores.filter((store) => storeMatchesCity(store, this.city)).length
					const booking = getBooking()
					const selected = this.stores.find((store) => String(store.id) === String(booking.storeId))
					this.selectedStoreId = selected ? selected.id : this.stores[0].id
					if (!selected) this.persistBooking(this.stores[0])
				} catch (error) {
					// Keep the bundled catalog available when the API is offline.
				}
			},
			applyBooking(booking) {
				this.booking = booking
				this.city = booking.city || this.city || '郑州市'
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
			openLocationSearch() {
				const query = [
					`city=${encodeURIComponent(this.city || '郑州市')}`,
					`region=${encodeURIComponent(this.region || '')}`,
					`keyword=${encodeURIComponent(this.searchKeyword || '')}`
				].join('&')
				uni.navigateTo({
					url: `/pages/search/index?${query}`
				})
			},
			openDatePage() {
				uni.navigateTo({
					url: '/pages/booking/date?from=home'
				})
			},
			selectCity(city) {
				this.city = city
				this.storeCount = this.stores.filter((store) => storeMatchesCity(store, city)).length
				this.closeSheet()
				this.showMessage(this.storeCount ? `已切换到${city}` : `${city}暂无可展示门店`)
			},
			selectStore(store) {
				this.selectedStoreId = store.id
				this.persistBooking(store)
				this.closeSheet()
				this.showMessage(`已选择${store.name}`)
			},
			selectDatePreset(preset) {
				this.booking = saveBooking({
					...this.booking,
					storeId: this.selectedStore.id,
					store: this.selectedStore.name,
					address: this.selectedStore.address,
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
			triggerLocation() {
				if (this.locationState === 'locating') {
					return
				}
				this.locationState = 'locating'
				this.showMessage('正在获取你的位置')
				uni.getLocation({
					type: 'gcj02',
					success: () => {
						this.locationState = 'ready'
						this.showMessage('已更新定位：距离郑州二七路店 1.2km')
					},
					fail: () => {
						this.locationState = 'failed'
						this.showMessage('定位未开启，仍为你展示全部门店')
					}
				})
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
				this.searching = true
				this.persistBooking()
				setTimeout(() => {
					this.searching = false
					const query = [
						`storeId=${encodeURIComponent(this.selectedStore.id)}`,
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
			handleQuickEntrance(item) {
				if (item.key === 'popular') {
					uni.pageScrollTo({ selector: '#popularStores', duration: 280 })
					return
				}
				if (item.key === 'order') {
					uni.reLaunch({ url: '/pages/order/list' })
					return
				}
				if (item.key === 'quality') {
					uni.navigateTo({ url: '/pages/store/list?type=quality' })
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
					uni.navigateTo({ url: '/pages/store/list?type=whole' })
					return
				}
				this.showMessage('已为你准备好推荐房源')
			},
			openStore(store) {
				recordRecent({
					id: store.id,
					type: 'store',
					title: store.name,
					subtitle: store.address,
					image: store.image,
					path: `/pages/store/detail?id=${store.id}`
				})
				uni.navigateTo({
					url: `/pages/store/detail?id=${store.id}`
				})
			},
			bookStore(store) {
				if (!store) return
				this.selectedStoreId = store.id
				this.persistBooking(store)
				const roomId = store.featuredRoomId || store.roomId || demoRooms[0].id
				const room = demoRooms.find((item) => String(item.id) === String(roomId))
				this.booking = saveBooking({
					...getBooking(),
					storeId: store.id,
					store: store.name,
					address: store.address,
					roomId,
					room: room ? room.name : store.room,
					roomImage: room ? room.image : store.image,
					roomPrice: Number(room ? room.price : store.price || 0)
				})
				goPage(`/pages/room/detail?id=${encodeURIComponent(roomId)}&storeId=${encodeURIComponent(store.id)}`)
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
				const hasZhengzhou = this.stores.some((store) => storeMatchesCity(store, '郑州市'))
				this.city = hasZhengzhou ? '郑州市' : (this.stores[0].city || '全部城市')
				this.region = ''
				this.searchKeyword = ''
				this.storeCount = this.stores.filter((store) => storeMatchesCity(store, this.city)).length
				this.preferenceDraft.priceMin = 150
				this.preferenceDraft.priceMax = 500
				this.priceSummary = '不限价格 / ¥150-¥500'
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

	.topbar {
		position: fixed;
		top: 0;
		right: 0;
		left: 0;
		z-index: 50;
		display: flex;
		align-items: center;
		justify-content: flex-start;
		height: 128rpx;
		height: calc(128rpx + constant(safe-area-inset-top));
		height: calc(128rpx + env(safe-area-inset-top));
		padding: 20rpx 32rpx 14rpx;
		padding: calc(20rpx + constant(safe-area-inset-top)) 32rpx 14rpx;
		padding: calc(20rpx + env(safe-area-inset-top)) 32rpx 14rpx;
		background: #f5fafb;
		background: rgba(245, 250, 251, 0.96);
		box-shadow: 0 2rpx 16rpx rgba(0, 0, 0, 0.03);
	}

	.brand-block {
		display: flex;
		align-items: center;
		min-width: 0;
	}

	.brand-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 64rpx;
		height: 64rpx;
		margin-right: 16rpx;
		color: #ffffff;
		background: $primary;
		border-radius: 50%;
	}

	.brand-copy {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.brand-name {
		color: $text-muted;
		font-size: 22rpx;
		line-height: 1.25;
	}

	.topbar-title {
		margin-top: 2rpx;
		color: $text;
		font-size: 38rpx;
		font-weight: 700;
		line-height: 1.1;
	}

	.page-content {
		padding-top: 0;
		padding-top: constant(safe-area-inset-top);
		padding-top: env(safe-area-inset-top);
	}

	.location-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 10rpx 32rpx 16rpx;
	}

	.city-picker {
		display: flex;
		align-items: center;
		min-width: 0;
		padding: 12rpx 18rpx;
		color: $text;
		background: $surface-container;
		border-radius: 34rpx;
		box-shadow: 0 4rpx 12rpx rgba(23, 29, 30, 0.03);
		text-align: left;
	}

	.city-picker .proto-icon {
		margin-right: 8rpx;
	}

	.city-name {
		font-size: 30rpx;
		font-weight: 700;
		white-space: nowrap;
	}

	.city-chevron {
		margin: 0 8rpx;
	}

	.open-count {
		padding: 4rpx 10rpx;
		color: #004f55;
		background: $secondary-fixed;
		border-radius: 16rpx;
		font-size: 18rpx;
		font-weight: 700;
		white-space: nowrap;
	}

	.location-tools {
		display: flex;
		align-items: center;
		gap: 14rpx;
	}

	.service-button,
	.scan-button {
		display: flex;
		align-items: center;
		justify-content: center;
		color: $text-variant;
		background: $surface;
		box-shadow: 0 4rpx 14rpx rgba(23, 29, 30, 0.06);
	}

	.service-button {
		height: 56rpx;
		padding: 0 18rpx;
		gap: 6rpx;
		border-radius: 28rpx;
		font-size: 20rpx;
	}

	.service-button .proto-icon {
		margin-right: 4rpx;
	}

	.scan-button {
		width: 56rpx;
		height: 56rpx;
		border-radius: 50%;
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

	.store-row,
	.date-row,
	.preference-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.store-row {
		min-height: 86rpx;
	}

	.store-summary {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
		padding-right: 16rpx;
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

	.store-name {
		margin-top: 4rpx;
		overflow: hidden;
		color: $text;
		font-size: 28rpx;
		font-weight: 650;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.store-subtitle {
		margin-top: 2rpx;
		overflow: hidden;
		color: $text-muted;
		font-size: 18rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.nearby-button {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		min-width: 92rpx;
		padding: 10rpx 12rpx;
		color: $primary;
		background: $surface-low;
		border-radius: 14rpx;
	}

	.nearby-button .proto-icon {
		margin-bottom: 2rpx;
	}

	.nearby-distance {
		margin-top: 2rpx;
		font-size: 18rpx;
		font-weight: 700;
	}

	.nearby-label {
		margin-top: 2rpx;
		color: $text-muted;
		font-size: 16rpx;
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
		font-size: 17rpx;
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

	.quick-card-green .quick-icon {
		color: $secondary;
		background: rgba(210, 231, 229, 0.76);
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

	.popular-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 18rpx;
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

	.area-badge.badge-tertiary {
		background: #4f6261;
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

	.stock-danger {
		color: #93000a;
		background: #ffdad6;
	}

	.stock-success {
		color: #006f78;
		background: $secondary-fixed;
	}

	.stock-neutral {
		color: #384a49;
		background: #d2e7e5;
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

	.empty-state {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 90rpx 30rpx;
		background: $surface;
		border-radius: 20rpx;
	}

	.empty-icon {
		color: $primary;
		font-size: 64rpx;
	}

	.empty-title {
		margin-top: 16rpx;
		color: $text;
		font-size: 28rpx;
		font-weight: 700;
	}

	.empty-desc {
		margin-top: 8rpx;
		color: $text-muted;
		font-size: 20rpx;
	}

	.empty-reset {
		height: 64rpx;
		margin-top: 24rpx;
		padding: 0 24rpx;
		color: $primary;
		background: $surface-low;
		border-radius: 16rpx;
		font-size: 21rpx;
		line-height: 64rpx;
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

	.bottom-nav {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 40;
		display: flex;
		align-items: center;
		justify-content: space-around;
		padding: 12rpx 24rpx;
		padding-bottom: calc(12rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(12rpx + env(safe-area-inset-bottom));
		background: #ffffff;
		background: rgba(255, 255, 255, 0.96);
		box-shadow: 0 -8rpx 30rpx rgba(42, 169, 169, 0.08);
	}

	.nav-item {
		display: flex;
		align-items: center;
		flex: 1;
		flex-direction: column;
		justify-content: center;
		height: 76rpx;
		color: $text-variant;
		background: transparent;
	}

	.nav-item.active {
		color: $primary;
		font-weight: 700;
	}

	.nav-icon {
		height: 34rpx;
		font-size: 30rpx;
	}

	.nav-label {
		margin-top: 8rpx;
		font-size: 18rpx;
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
	.guest-option,
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

	.city-option,
	.price-option {
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
	.price-option.selected,
	.store-option.selected,
	.guest-option.selected {
		color: $primary;
		background: #eaf8f6;
		border-color: #c1e3e0;
	}

	.store-option-list,
	.date-preset-list,
	.guest-option-list,
	.menu-sheet-content {
		display: flex;
		flex-direction: column;
		gap: 14rpx;
	}

	.store-option-copy,
	.guest-option > view,
	.menu-option-copy {
		flex: 1;
		min-width: 0;
	}

	.store-option-title,
	.preset-title,
	.guest-option-title,
	.menu-option-title {
		display: block;
		color: $text;
		font-size: 23rpx;
		font-weight: 650;
	}

	.store-option-desc,
	.preset-detail,
	.guest-option-desc,
	.menu-option-desc {
		display: block;
		margin-top: 6rpx;
		color: $text-muted;
		font-size: 18rpx;
		line-height: 1.45;
	}

	.option-check {
		margin-left: 14rpx;
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

	.price-option-list {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
	}

	.price-option {
		padding: 16rpx 18rpx;
	}

	.sheet-caption-spaced {
		margin-top: 28rpx;
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
