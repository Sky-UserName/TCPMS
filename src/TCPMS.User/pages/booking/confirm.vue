<template>
	<view class="proto-page booking-confirm-page">
		<ProtoHeader title="订单填写" title-align="center" theme="green" />

			<view class="confirm-summary proto-card">
				<view class="confirm-room-preview">
					<image class="confirm-room-image" :src="booking.roomImage || booking.image" mode="aspectFill"></image>
					<view class="confirm-room-copy">
						<text class="confirm-room-title">{{ booking.room }}</text>
						<view class="confirm-room-tags">
							<text v-for="tag in roomTags" :key="tag">{{ tag }}</text>
						</view>
						<text class="confirm-room-location">{{ booking.store }} · 面积{{ booking.roomArea || '舒适房型' }}</text>
					</view>
				</view>
			<view class="confirm-dates">
				<view class="confirm-date-side">
					<text>入住时间</text>
					<text class="strong">{{ booking.checkIn }}</text>
					<text class="small">{{ checkInWeekday }} · 14:00后</text>
				</view>
				<view class="confirm-arrow">
					<text>共 {{ booking.nights }} 晚</text>
					<ProtoIcon name="chevron-right" :size="22" />
				</view>
				<view class="confirm-date-side confirm-date-side-right">
					<text>离店时间</text>
					<text class="strong">{{ booking.checkOut }}</text>
					<text class="small">{{ checkOutWeekday }} · 12:00前</text>
				</view>
			</view>
			<view class="confirm-meta">
				<text>{{ booking.roomCount }}间 · {{ booking.guestCount }}位成人出行</text>
				<text class="proto-pill info">免费双人手作早餐</text>
				<text class="proto-pill info">独立卫浴</text>
			</view>
		</view>

		<view class="confirm-guest proto-card">
			<view class="guest-section-head">
				<view>
					<text class="guest-section-title">入住信息</text>
					<text class="guest-section-subtitle">实名登记，保障入住顺利办理</text>
				</view>
				<view class="proto-link confirm-guest-link" @tap="goGuests"><text>需填{{ booking.guestCount }}人</text><ProtoIcon name="plus" :size="17" /><text>添加入住人</text></view>
			</view>
			<view v-for="(guest, index) in guests" :key="guest.id || guest.name" class="guest-row">
				<view class="guest-avatar"><text>{{ guest.name ? guest.name.slice(0, 1) : '客' }}</text></view>
				<view class="guest-info">
					<view class="guest-name-row">
						<text class="guest-name">{{ guest.name }}</text>
						<text class="proto-pill" :class="index === 0 ? 'primary' : 'info'">{{ index === 0 ? '主入住人' : '同行住客' }}</text>
					</view>
					<view class="guest-meta-line"><ProtoIcon name="document" :size="19" /><text>身份证 {{ guest.identity }}</text></view>
					<view class="guest-meta-line"><ProtoIcon name="phone" :size="19" /><text>手机号 {{ guest.phone }}</text></view>
				</view>
				<view class="guest-edit-button" @tap="goGuests"><ProtoIcon class="guest-edit" name="edit" :size="22" /></view>
			</view>
			<view v-if="guests.length < booking.guestCount" class="confirm-error">当前仅登记 {{ guests.length }} 位入住人，请补齐实名信息后继续。</view>
			<button v-if="guests.length < booking.guestCount" class="proto-ghost-button" hover-class="none" @tap="goGuests">
				<ProtoIcon name="plus" :size="22" />
				<text>添加入住人</text>
			</button>
		</view>

		<view class="confirm-fee proto-card">
			<view class="card-heading">
				<text>费用明细</text>
				<text class="proto-caption">明细透明 · 无隐藏消费</text>
			</view>
			<view v-for="item in feeItems" :key="item.label" class="fee-row">
				<text>{{ item.label }}</text>
				<text :class="{ discount: item.discount }">{{ item.value }}</text>
			</view>
			<view class="fee-total">
				<text>实付总计</text>
				<text class="strong">¥{{ total }}</text>
			</view>
		</view>

		<view class="confirm-policy proto-card">
			<view class="card-heading">
				<view class="inline-icon-text"><ProtoIcon name="shield" :size="22" /><text>退改规则与入离须知</text></view>
				<text class="policy-required">支付前必读</text>
			</view>
			<view class="policy-summary-trigger" @tap="showPolicyDetail">
				<text>入住日前1天18:00前可免费取消，逾期按规则收取费用</text>
				<view class="proto-link"><text>查看详情</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view class="proto-check" @tap="toggleRuleAgreement">
				<view class="proto-check-box" :class="{ checked: ruleAgreed }"><ProtoIcon v-if="ruleAgreed" name="check" :size="18" tone="white" /></view>
				<text>我已阅读并完全同意《订房退改规则与入离守则》与《人身财产安全保障协议》</text>
			</view>
		</view>

		<view class="payment-security">
			<view><ProtoIcon name="shield" :size="20" /><text>微信商户官方资金担保 · 入住后离店清算结算</text></view>
		</view>

			<view class="proto-bottom-actions confirm-bottom-actions">
			<view class="pay-total">
				<text>¥</text>
				<text class="strong">{{ total }}</text>
				<view class="pay-total-detail"><text>明细</text><ProtoIcon name="chevron-up" :size="18" /></view>
			</view>
			<button class="proto-primary-button" hover-class="none" @tap="submitOrder"><ProtoIcon name="shield" tone="white" :size="21" /><text>{{ submitStatus === 'locking' ? '正在锁定库存...' : '提交订单' }}</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, showToast } from '@/common/prototype.js'
	import { createOrder, getBooking, getGuests } from '@/common/app-store.js'
	import { createRemoteOrder } from '@/common/api.js'

	const weekdayLabels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

	const getWeekday = (isoDate) => {
		const date = new Date(isoDate)
		return Number.isNaN(date.getTime()) ? '' : weekdayLabels[date.getDay()]
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				booking: getBooking(),
				guests: getGuests(),
				ruleAgreed: false,
				submitStatus: 'idle'
			}
		},
		computed: {
			checkInWeekday() {
				return getWeekday(this.booking.isoCheckIn)
			},
			checkOutWeekday() {
				return getWeekday(this.booking.isoCheckOut)
			},
			roomSubtotal() {
				return Number(this.booking.roomPrice || 0) * Number(this.booking.roomCount || 1) * Number(this.booking.nights || 1)
			},
			discount() {
				return Math.min(60, Math.round(this.roomSubtotal * 0.15))
			},
			total() {
				return Math.max(0, this.roomSubtotal - this.discount)
			},
			roomTags() {
				const tags = Array.isArray(this.booking.roomTags) ? this.booking.roomTags.filter(Boolean) : []
				return (tags.length ? tags : ['私家花园', '近商圈', '近地铁']).slice(0, 4)
			},
			feeItems() {
				return [
					{ label: `客房房费（${this.booking.roomCount}间 × ${this.booking.nights}晚）`, value: `¥${this.roomSubtotal}` },
					{ label: '平台初秋连住立减', value: `-¥${this.discount}`, discount: true },
					{ label: '住宿押金（微信信用免押）', value: '¥0' },
					{ label: '清洁保洁与管家服务费', value: '已包含' },
					{ label: '优惠券抵扣', value: '暂无可用券' }
				]
			}
		},
		onLoad() {
			this.refreshData()
		},
		onShow() {
			this.refreshData()
		},
		methods: {
			refreshData() {
				this.booking = getBooking()
				this.guests = getGuests()
			},
			toggleRuleAgreement() {
				this.ruleAgreed = !this.ruleAgreed
			},
			showPolicyDetail() {
				uni.showModal({
					title: '退改规则与入离须知',
					content: '入住时间：当日14:00后办理入住，次日12:00前退房。入住日前1天18:00前在小程序内申请，可享全额免手续费取消；逾期取消或未入住，将按首晚房费收取。入住时需提供真实有效的身份证件，房内禁止吸烟，具体以门店现场规则为准。',
					showCancel: false,
					confirmText: '我知道了'
				})
			},
			async submitOrder() {
				if (this.submitStatus === 'locking') return
				if (this.guests.length < Number(this.booking.guestCount || 1)) {
					showToast('请补齐入住人实名信息')
					return
				}
				if (!this.ruleAgreed) {
					showToast('请先勾选并同意退改规则')
					return
				}
				this.submitStatus = 'locking'
				const booking = {
					...this.booking,
					guests: this.guests.slice(0, Number(this.booking.guestCount || this.guests.length)),
					total: this.total
				}
				let order
				if (/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(booking.storeId))
					&& /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(booking.roomId))) {
					try {
						order = await createRemoteOrder(booking)
					} catch (error) {
						order = null
					}
				}
				if (!order) order = createOrder(booking)
				setTimeout(() => {
					if (this.submitStatus === 'locking') {
						goPage(`/pages/pay/result?orderId=${order.id}`)
					}
				}, 700)
			},
			goGuests() {
				goPage('/pages/me/guests')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.booking-confirm-page {
		padding-bottom: 154rpx;
		padding-bottom: calc(154rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(154rpx + env(safe-area-inset-bottom));
	}

	.confirm-store-row {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}

	.confirm-store-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 42rpx;
		height: 42rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 14rpx;
	}

	.confirm-store-row > view {
		flex: 1;
	}

	.confirm-store,
	.confirm-room {
		display: block;
	}

	.confirm-store {
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.confirm-room {
		margin-top: 8rpx;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 800;
	}

	.confirm-dates {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 20rpx;
		padding: 20rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.confirm-date-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.confirm-date-side-right {
		align-items: flex-end;
		text-align: right;
	}

	.confirm-date-side > text:first-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.confirm-date-side .strong {
		margin-top: 6rpx;
		color: var(--proto-text);
		font-size: 28rpx;
	}

	.confirm-date-side .small {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.confirm-arrow {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		flex: 0 0 98rpx;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
		text-align: center;
	}

	.confirm-arrow > .proto-icon {
		margin-top: 4rpx;
	}

	.confirm-meta {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-top: 16rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.confirm-meta .proto-pill {
		min-height: 32rpx;
		padding: 0 9rpx;
		font-size: 16rpx;
	}

	.card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.card-heading .proto-link,
	.card-heading .proto-caption {
		color: var(--proto-primary-dark);
		font-size: 18rpx;
		font-weight: 400;
	}

	.confirm-guest-link {
		display: flex;
		align-items: center;
		gap: 4rpx;
	}

	.guest-row {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 16rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.guest-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 52rpx;
		height: 52rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.guest-row > view {
		flex: 1;
	}

	.guest-name-row {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.guest-name {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.guest-name-row .proto-pill {
		min-height: 30rpx;
		padding: 0 9rpx;
		font-size: 16rpx;
	}

	.guest-row view > text:not(.guest-name) {
		display: block;
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.guest-edit {
		color: var(--proto-primary-dark);
	}

	.confirm-error {
		margin-top: 14rpx;
		padding: 16rpx;
		color: var(--proto-error);
		background: #ffe7e4;
		border-radius: 14rpx;
		font-size: 19rpx;
	}

	.confirm-guest .proto-ghost-button {
		height: 68rpx;
		margin: 16rpx 0 0;
		font-size: 21rpx;
	}

	.inline-icon-text {
		display: flex;
		align-items: center;
	}

	.inline-icon-text .proto-icon {
		margin-right: 5rpx;
	}

	.fee-row,
	.fee-total {
		display: flex;
		justify-content: space-between;
		margin-top: 16rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.fee-row .discount {
		color: var(--proto-error);
	}

	.fee-total {
		margin-top: 20rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid #e3ebec;
		color: var(--proto-text);
	}

	.fee-total .strong {
		color: var(--proto-primary-dark);
		font-size: 34rpx;
	}

	.payment-security {
		padding: 20rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-align: center;
	}

	.payment-security > view {
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.payment-security .proto-icon {
		margin-right: 5rpx;
	}
	.confirm-bottom-actions {
		align-items: center;
	}

	.pay-total {
		display: flex;
		align-items: baseline;
		width: 190rpx;
		color: var(--proto-primary-dark);
	}

	.pay-total > text:first-child {
		font-size: 19rpx;
	}

	.pay-total .strong {
		font-size: 42rpx;
	}

	.pay-total-detail {
		display: flex;
		align-items: center;
		gap: 3rpx;
		margin-left: 8rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.confirm-bottom-actions .proto-primary-button {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 6rpx;
		flex: 1;
		margin: 0;
	}

	.failed-copy {
		display: block;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 22rpx;
		line-height: 1.6;
	}

	/* Booking form visual pass: green accents and a room-first confirmation card. */
	.booking-confirm-page {
		--booking-green: #2aa9a9;
		--booking-green-deep: #006a6a;
		--booking-green-tint: #dff6f5;
		background: #f5f9fc;
	}

	.booking-confirm-page :deep(.proto-header),
	.booking-confirm-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.16) !important;
	}

	.booking-confirm-page :deep(.proto-header-title),
	.booking-confirm-page :deep(.proto-header-icon-button),
	.booking-confirm-page :deep(.proto-header-user-button),
	.booking-confirm-page .proto-header-title,
	.booking-confirm-page .proto-header-icon-button,
	.booking-confirm-page .proto-header-user-button {
		color: #ffffff !important;
	}

	.booking-confirm-page .proto-card {
		border-color: rgba(42, 169, 169, 0.14);
		box-shadow: 0 10rpx 28rpx rgba(0, 106, 106, 0.07);
	}

	.confirm-room-preview {
		display: flex;
		align-items: flex-start;
		gap: 16rpx;
	}

	.confirm-room-image {
		width: 190rpx;
		height: 154rpx;
		flex: 0 0 190rpx;
		border-radius: 16rpx;
		background: #e8f6f5;
	}

	.confirm-room-copy {
		min-width: 0;
		flex: 1;
	}

	.confirm-room-title {
		display: block;
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 850;
		line-height: 1.3;
	}

	.confirm-room-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 12rpx;
	}

	.confirm-room-tags text {
		padding: 6rpx 12rpx;
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.16);
		border-radius: 8rpx;
		font-size: 17rpx;
		white-space: nowrap;
	}

	.confirm-room-location {
		display: block;
		margin-top: 12rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.4;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.booking-confirm-page .confirm-dates {
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.12);
	}

	.booking-confirm-page .confirm-arrow,
	.booking-confirm-page .confirm-guest-link,
	.booking-confirm-page .guest-edit,
	.booking-confirm-page .pay-total,
	.booking-confirm-page .fee-total .strong {
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .proto-pill.info {
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border-color: rgba(42, 169, 169, 0.14);
	}

	.booking-confirm-page .confirm-bottom-actions .proto-primary-button,
	.booking-confirm-page .confirm-bottom-actions .proto-primary-button > text {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 0;
		height: 78rpx;
		margin: 0;
		padding: 0 24rpx;
		color: #ffffff !important;
		background: var(--booking-green) !important;
		border: 0 !important;
		border-radius: 39rpx;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 78rpx;
		text-align: center;
		text-shadow: none;
		white-space: nowrap;
	}

	.booking-confirm-page .guest-section-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 16rpx;
	}

	.booking-confirm-page .guest-section-title {
		display: block;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 850;
		line-height: 1.3;
	}

	.booking-confirm-page .guest-section-subtitle {
		display: block;
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.35;
	}

	.booking-confirm-page .confirm-guest-link {
		flex: 0 0 auto;
		padding: 8rpx 12rpx;
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border-radius: 18rpx;
		font-size: 18rpx;
	}

	.booking-confirm-page .guest-row {
		align-items: center;
		gap: 14rpx;
		margin-top: 14rpx;
		padding: 16rpx 14rpx;
		background: #f5fbfb;
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 18rpx;
	}

	.booking-confirm-page .guest-row > .guest-avatar {
		flex: 0 0 58rpx;
		width: 58rpx;
		height: 58rpx;
		color: #ffffff;
		background: var(--booking-green);
		border-radius: 18rpx;
		font-size: 24rpx;
		font-weight: 800;
	}

	.booking-confirm-page .guest-row > .guest-info {
		min-width: 0;
		flex: 1;
	}

	.booking-confirm-page .guest-name-row {
		min-width: 0;
	}

	.booking-confirm-page .guest-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.booking-confirm-page .guest-meta-line {
		display: flex;
		align-items: center;
		gap: 5rpx;
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.25;
	}

	.booking-confirm-page .guest-meta-line .proto-icon {
		flex: 0 0 auto;
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .guest-row > .guest-edit-button {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 44rpx;
		width: 44rpx;
		height: 44rpx;
		border-radius: 50%;
	}

	.booking-confirm-page .guest-edit-button .guest-edit {
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .confirm-policy {
		margin-top: 16rpx;
	}

	.booking-confirm-page .policy-required {
		color: var(--proto-muted);
		font-size: 17rpx;
		font-weight: 400;
	}

	.booking-confirm-page .policy-summary-trigger {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 14rpx;
		padding: 14rpx 16rpx;
		color: var(--proto-muted);
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 14rpx;
		font-size: 19rpx;
		line-height: 1.4;
	}

	.booking-confirm-page .policy-summary-trigger > text {
		min-width: 0;
		flex: 1;
	}

	.booking-confirm-page .policy-summary-trigger .proto-link {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		color: var(--booking-green-deep);
		font-size: 18rpx;
		white-space: nowrap;
	}

	.booking-confirm-page .policy-summary-trigger .proto-icon {
		margin-left: 2rpx;
	}

	.booking-confirm-page .confirm-policy .proto-check {
		display: flex;
		align-items: flex-start;
		gap: 10rpx;
		margin: 14rpx 0 0;
	}

	.booking-confirm-page .confirm-policy .proto-check > text {
		min-width: 0;
		flex: 1;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.booking-confirm-page .confirm-policy .proto-check-box {
		flex: 0 0 34rpx;
		margin-top: 2rpx;
	}
</style>
