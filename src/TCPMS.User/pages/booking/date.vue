<template>
	<view class="booking-date-page">
		<ProtoHeader title="选择入住日期" title-align="center" theme="green" />

		<view class="date-summary">
			<view class="date-summary-side">
				<text class="date-summary-label">入住日期</text>
				<text class="date-summary-value">{{ checkInLabel }}</text>
				<text class="date-summary-weekday">{{ checkInWeekday || '请选择' }}</text>
			</view>
			<view class="date-summary-night">
				<text>{{ nights ? `共${nights}晚` : '请选择离店日' }}</text>
			</view>
			<view class="date-summary-side date-summary-right">
				<text class="date-summary-label">离店日期</text>
				<text class="date-summary-value">{{ checkOutLabel }}</text>
				<text class="date-summary-weekday">{{ checkOutWeekday || '请选择' }}</text>
			</view>
		</view>

		<view class="week-row">
			<text v-for="day in weekDays" :key="day">{{ day }}</text>
		</view>

		<scroll-view class="calendar-scroll" scroll-y>
			<view v-for="month in months" :key="month.key" class="month-block">
				<text class="month-title">{{ month.title }}</text>
				<view class="month-grid">
					<view v-for="(day, index) in month.days" :key="day ? day.key : `${month.key}-empty-${index}`" class="calendar-day-wrap">
						<view v-if="day" class="calendar-day" :class="dayClass(day)" @tap="selectDay(day)">
							<text class="calendar-day-number">{{ day.date }}</text>
							<text v-if="day.iso === checkInKey" class="calendar-day-label">入住</text>
							<text v-else-if="day.iso === checkOutKey" class="calendar-day-label">离店</text>
						</view>
					</view>
				</view>
			</view>
		</scroll-view>

		<view class="date-bottom-action">
			<button class="date-confirm-button" hover-class="none" :class="{ disabled: !canConfirm }" @tap="confirmSelection"><text>确认</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { getBooking, saveBooking } from '@/common/app-store.js'
	import { goPage, showToast } from '@/common/prototype.js'

	const WEEKDAY_LABELS = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

	const normalizeDate = (date) => {
		const value = new Date(date)
		value.setHours(0, 0, 0, 0)
		return value
	}

	const isoDate = (date) => {
		const value = normalizeDate(date)
		return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}T00:00:00`
	}

	const dateLabel = (date) => `${date.getMonth() + 1}月${date.getDate()}日`

	const buildMonths = (count = 6) => {
		const today = normalizeDate(new Date())
		const firstMonth = new Date(today.getFullYear(), today.getMonth(), 1)
		return Array.from({ length: count }, (_, monthIndex) => {
			const monthDate = new Date(firstMonth.getFullYear(), firstMonth.getMonth() + monthIndex, 1)
			const daysInMonth = new Date(monthDate.getFullYear(), monthDate.getMonth() + 1, 0).getDate()
			const leading = monthDate.getDay()
			const days = Array.from({ length: leading + daysInMonth }, (_, index) => {
				if (index < leading) return null
				const date = new Date(monthDate.getFullYear(), monthDate.getMonth(), index - leading + 1)
				return { key: isoDate(date), iso: isoDate(date), date: date.getDate(), past: date < today }
			})
			return {
				key: `${monthDate.getFullYear()}-${monthDate.getMonth() + 1}`,
				title: `${monthDate.getFullYear()}年${monthDate.getMonth() + 1}月`,
				days
			}
		})
	}

	const parseDate = (value) => {
		const date = value ? new Date(value) : null
		return date && !Number.isNaN(date.getTime()) ? normalizeDate(date) : null
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			const booking = getBooking()
			return {
				booking,
				fromHome: false,
				from: '',
				checkInKey: booking.isoCheckIn,
				checkOutKey: booking.isoCheckOut,
				weekDays: ['日', '一', '二', '三', '四', '五', '六'],
				months: buildMonths()
			}
		},
		computed: {
			checkInLabel() {
				const date = parseDate(this.checkInKey)
				return date ? dateLabel(date) : '选择日期'
			},
			checkOutLabel() {
				const date = parseDate(this.checkOutKey)
				return date ? dateLabel(date) : '选择日期'
			},
			checkInWeekday() {
				const date = parseDate(this.checkInKey)
				return date ? WEEKDAY_LABELS[date.getDay()] : ''
			},
			checkOutWeekday() {
				const date = parseDate(this.checkOutKey)
				return date ? WEEKDAY_LABELS[date.getDay()] : ''
			},
			nights() {
				const checkIn = parseDate(this.checkInKey)
				const checkOut = parseDate(this.checkOutKey)
				if (!checkIn || !checkOut) return 0
				return Math.max(0, Math.round((checkOut - checkIn) / 86400000))
			},
			canConfirm() {
				return Boolean(this.checkInKey && this.checkOutKey && this.nights > 0)
			}
		},
		onLoad(options) {
			this.from = options && options.from ? String(options.from) : ''
			this.fromHome = this.from === 'home'
			if (options && (options.id || options.storeId)) {
				this.booking = saveBooking({
					...this.booking,
					roomId: options.id || this.booking.roomId,
					storeId: options.storeId || this.booking.storeId
				})
				this.checkInKey = this.booking.isoCheckIn
				this.checkOutKey = this.booking.isoCheckOut
			}
		},
		methods: {
			dayClass(day) {
				const classes = []
				if (day.past) classes.push('is-past')
				if (day.iso === this.checkInKey) classes.push('is-start')
				if (day.iso === this.checkOutKey) classes.push('is-end')
				if (this.checkInKey && this.checkOutKey && day.iso > this.checkInKey && day.iso < this.checkOutKey) classes.push('is-range')
				return classes.join(' ')
			},
			selectDay(day) {
				if (day.past) {
					showToast('过去日期不可选择')
					return
				}
				if (!this.checkInKey || this.checkOutKey || day.iso <= this.checkInKey) {
					this.checkInKey = day.iso
					this.checkOutKey = ''
					return
				}
				this.checkOutKey = day.iso
			},
			goBack() {
				uni.navigateBack({ delta: 1, fail: () => uni.reLaunch({ url: '/pages/index/index' }) })
			},
			confirmSelection() {
				if (!this.canConfirm) {
					showToast('请先选择入住和离店日期')
					return
				}
				this.booking = saveBooking({
					...this.booking,
					checkIn: this.checkInLabel,
					checkOut: this.checkOutLabel,
					isoCheckIn: this.checkInKey,
					isoCheckOut: this.checkOutKey,
					date: `${this.checkInLabel} - ${this.checkOutLabel}`,
					nights: this.nights
				})
				if (this.fromHome || this.from === 'room-detail' || this.from === 'room-list') {
					uni.navigateBack({ delta: 1, fail: () => uni.reLaunch({ url: '/pages/index/index' }) })
					return
				}
				if (this.from === 'store-detail') {
					uni.navigateBack({ delta: 1, fail: () => goPage(`/pages/store/detail?id=${encodeURIComponent(this.booking.storeId || '')}`) })
					return
				}
				goPage('/pages/booking/confirm')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.booking-date-page {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
		padding-bottom: calc(136rpx + env(safe-area-inset-bottom));
		background: #f4fafb;
		color: #20252b;
	}

	.date-page-header {
		display: flex;
		align-items: center;
		height: 112rpx;
		padding: 20rpx 24rpx 12rpx;
		background: #ffffff;
		border-bottom: 1rpx solid rgba(197, 199, 202, 0.28);
	}

	.date-back-button,
	.date-header-spacer {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 68rpx;
		height: 64rpx;
		padding: 0;
		background: transparent;
	}

	.date-back-button {
		color: #006a6a;
	}

	.date-page-title {
		flex: 1;
		color: #20252b;
		font-size: 34rpx;
		font-weight: 750;
		text-align: center;
	}

	.date-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 28rpx 48rpx 26rpx;
		background: #ffffff;
	}

	.date-summary-side {
		display: flex;
		flex-direction: column;
		min-width: 190rpx;
	}

	.date-summary-right {
		align-items: flex-end;
		text-align: right;
	}

	.date-summary-label {
		color: #6f8085;
		font-size: 22rpx;
	}

	.date-summary-value {
		margin-top: 8rpx;
		color: #20252b;
		font-size: 40rpx;
		font-weight: 750;
		white-space: nowrap;
	}

	.date-summary-weekday {
		margin-top: 4rpx;
		color: #006a6a;
		font-size: 22rpx;
	}

	.date-summary-night {
		padding: 10rpx 18rpx;
		color: #006a6a;
		background: #dff6f5;
		border: 1rpx solid rgba(42, 169, 169, 0.22);
		border-radius: 24rpx;
		font-size: 20rpx;
		white-space: nowrap;
	}

	.week-row {
		display: flex;
		align-items: center;
		justify-content: space-around;
		min-height: 74rpx;
		background: #eef4f5;
		color: #6f8085;
		font-size: 22rpx;
	}

	.week-row text {
		width: 14.2857%;
		text-align: center;
	}

	.calendar-scroll {
		flex: 1;
		min-height: 0;
		background: #ffffff;
	}

	.month-block {
		padding: 30rpx 24rpx 18rpx;
	}

	.month-title {
		display: block;
		color: #2a3034;
		font-size: 32rpx;
		font-weight: 800;
		text-align: center;
	}

	.month-grid {
		display: flex;
		flex-wrap: wrap;
		margin-top: 22rpx;
	}

	.calendar-day-wrap {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 14.2857%;
		height: 88rpx;
	}

	.calendar-day {
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		width: 82rpx;
		height: 78rpx;
		color: #20252b;
		border-radius: 18rpx;
		font-size: 28rpx;
	}

	.calendar-day.is-past {
		color: #c5c7ca;
	}

	.calendar-day.is-range {
		width: 100%;
		color: #006a6a;
		background: rgba(42, 169, 169, 0.14);
		border-radius: 0;
	}

	.calendar-day.is-start,
	.calendar-day.is-end {
		color: #ffffff;
		background: #18858f;
		font-weight: 750;
	}

	.calendar-day-label {
		margin-top: 2rpx;
		font-size: 17rpx;
		font-weight: 500;
	}

	.date-bottom-action {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 20;
		padding: 16rpx 32rpx calc(16rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(197, 199, 202, 0.35);
		box-shadow: 0 -8rpx 24rpx rgba(32, 37, 43, 0.06);
	}

	.date-confirm-button {
		width: 100%;
		height: 84rpx;
		color: #ffffff;
		background: #2aa9a9;
		border-radius: 42rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.24);
		font-size: 28rpx;
		font-weight: 750;
	}

	.date-confirm-button.disabled {
		background: #9ccfd0;
		box-shadow: none;
	}

	/* Calendar page uses the same green booking accent as room details and checkout. */
	.booking-date-page {
		--booking-green: #2aa9a9;
		--booking-green-deep: #006a6a;
		--booking-green-tint: #dff6f5;
		background: #f5f9fc;
	}

	.booking-date-page :deep(.proto-header),
	.booking-date-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.16) !important;
	}

	.booking-date-page :deep(.proto-header-title),
	.booking-date-page :deep(.proto-header-icon-button),
	.booking-date-page :deep(.proto-header-user-button),
	.booking-date-page .proto-header-title,
	.booking-date-page .proto-header-icon-button,
	.booking-date-page .proto-header-user-button {
		color: #ffffff !important;
	}

	.booking-date-page .date-page-header,
	.booking-date-page .date-summary {
		background: #ffffff;
	}

	.booking-date-page .date-back-button,
	.booking-date-page .date-summary-weekday,
	.booking-date-page .date-summary-night {
		color: var(--booking-green-deep);
	}

	.booking-date-page .date-summary-night {
		background: var(--booking-green-tint);
		border-color: rgba(42, 169, 169, 0.18);
	}

	.booking-date-page .calendar-day.is-range {
		color: var(--booking-green-deep);
		background: rgba(42, 169, 169, 0.12);
	}

	.booking-date-page .calendar-day.is-start,
	.booking-date-page .calendar-day.is-end {
		background: var(--booking-green);
	}

	.booking-date-page .date-confirm-button,
	.booking-date-page .date-confirm-button > text {
		color: #ffffff;
		background: var(--booking-green);
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.22);
	}

	.booking-date-page .date-confirm-button.disabled {
		color: #ffffff;
		background: #9ccfd0;
		box-shadow: none;
	}
</style>
