<template>
	<view class="proto-page order-detail-page">
		<ProtoHeader title="订单详情" title-align="center" theme="green" />
		<view class="detail-status proto-card">
			<view class="status-badge">
				<text class="proto-pill" :class="statusClass">{{ statusMeta.label }}</text>
				<text class="proto-caption">平台安心住保障</text>
			</view>
			<text class="status-title">{{ statusMeta.title }}</text>
			<text class="status-desc">{{ statusMeta.desc }}</text>
			<view class="progress-line">
				<view v-for="item in progress" :key="item.key" class="progress-item" :class="{ active: progressIndex >= item.index }">
					<view class="progress-dot">
						<ProtoIcon v-if="progressIndex >= item.index" name="check" tone="white" :size="20" />
						<text v-else>{{ item.index + 1 }}</text>
					</view>
					<text class="small">{{ item.label }}</text>
				</view>
			</view>
		</view>

		<view v-if="status === 'stay'" class="door-card proto-card">
			<view class="door-heading">
				<view class="door-icon"><ProtoIcon name="lock" :size="28" /></view>
				<view>
					<text class="door-title">智能门锁入住凭据</text>
					<text class="door-desc">客房号系统自动分配 · 密码即开</text>
				</view>
				<text class="proto-pill info">已自动同步门锁</text>
			</view>
			<view class="door-code-row">
				<view>
					<text>门锁临时访问密码（入户专用）</text>
					<view class="door-code-value"><text class="strong">820619</text><ProtoIcon name="key" :size="22" /></view>
					<text class="small">有效期：{{ doorValidity }}</text>
				</view>
				<view class="qr-placeholder">
					<ProtoIcon name="qr" :size="54" />
					<text class="small">扫码开门/梯控</text>
				</view>
			</view>
			<view class="door-safe"><ProtoIcon name="shield" :size="20" /><text>凭证包含隐私权限，请勿截图外传。到店后若遇门锁未响应，可直接按门铃联动管家协助。</text></view>
		</view>

		<view class="detail-store proto-card">
			<view class="store-heading">
				<view class="store-mark"><ProtoIcon name="store" :size="28" /></view>
				<view>
					<text class="store-brand">TCPMS 精品直营门店</text>
					<text class="store-name">{{ order.store }}</text>
				</view>
			</view>
			<view class="detail-address"><ProtoIcon name="location" :size="20" /><text>{{ order.address || '河南省郑州市金水区郑路辅路28号' }}</text></view>
			<view class="detail-store-actions">
				<view class="walk-caption"><ProtoIcon name="navigation" :size="20" /><text>步行直达无需换乘</text></view>
				<button class="proto-button-small proto-button-light" hover-class="none" @tap="callStore"><ProtoIcon name="phone" :size="18" /><text>联系门店</text></button>
				<button class="proto-button-small" hover-class="none" @tap="openLocation"><ProtoIcon name="navigation" tone="white" :size="18" /><text>去这里</text></button>
			</view>
		</view>

		<view class="detail-room proto-card">
			<view class="room-heading">
				<image :src="roomImage" mode="aspectFill"></image>
				<view>
					<text class="room-name">{{ order.room }}</text>
					<text class="room-spec">独立房间 · 带外窗采光 · 25㎡ · 1张1.8米大床</text>
					<view class="room-tags"><text>免费双早</text><text>高速Wi-Fi</text><text>一客一消毒</text></view>
				</view>
			</view>
			<view class="room-date-row">
				<view class="room-date-side"><text>入住时间</text><text class="strong">{{ order.checkIn || order.date.split(' - ')[0] }}</text><text class="small">14:00后</text></view>
				<text class="date-mid">共{{ order.nights || 1 }}晚</text>
				<view class="room-date-side room-date-side-right"><text>离店时间</text><text class="strong">{{ order.checkOut || order.date.split(' - ')[1] }}</text><text class="small">12:00前</text></view>
			</view>
			<view class="room-service"><ProtoIcon name="verified" :size="19" /><text>早餐 07:30 - 09:30　凭房卡于一楼茶舍享用双人手作粗粮早餐</text></view>
			<view class="room-service"><ProtoIcon name="shield" :size="19" /><text>客房内布草纯棉封签，洗漱用品均采用无接触环保封装</text></view>
		</view>

		<view class="detail-guests proto-card">
			<view class="card-heading"><text>实名入住信息</text><view class="proto-caption card-heading-note"><ProtoIcon name="shield" :size="18" /><text>公安旅馆业系统已核验</text></view></view>
			<view v-for="(guest, index) in displayGuests" :key="guest.id || guest.name" class="identity-row">
				<text>{{ index === 0 ? '主要住客' : '同住客人' }}</text>
				<view class="identity-value">
					<text>{{ guest.name }}</text>
					<text class="small">{{ guest.identity }}（居民身份证）</text>
				</view>
			</view>
			<view class="identity-row"><text>预留联系电话</text><text>{{ displayGuests[0] ? displayGuests[0].phone : '未填写' }}</text></view>
		</view>

		<view class="detail-fee proto-card">
			<view class="card-heading"><text>支付与费用明细</text><text class="proto-pill success">微信免密支付已完成</text></view>
			<view class="payment-code">
				<view class="payment-code-row"><view class="payment-code-label"><ProtoIcon name="document" :size="18" /><text>订单编号</text></view><text class="payment-value">{{ order.id }}</text></view>
				<view class="payment-code-row"><view class="payment-code-label"><ProtoIcon name="calendar" :size="18" /><text>下单时间</text></view><text class="payment-value">{{ order.createdAt ? order.createdAt.slice(0, 19).replace('T', ' ') : '刚刚' }}</text></view>
				<view class="payment-code-row"><view class="payment-code-label"><ProtoIcon name="receipt" :size="18" /><text>支付流水号</text></view><text class="payment-value">{{ paymentId }}</text></view>
			</view>
			<view class="fee-row"><text>房费总计（{{ order.roomCount || 1 }}间 × {{ order.nights || 1 }}晚）</text><text>¥{{ roomSubtotal }}</text></view>
			<view class="fee-row"><text>平台初秋连住立减</text><text class="discount">-¥{{ discount }}</text></view>
			<view class="fee-row"><text>住宿押金（微信支付分信用免押）</text><text>¥0.00</text></view>
			<view class="fee-total"><text>实付金额</text><text class="strong">¥{{ order.price }}.00</text></view>
		</view>

		<view class="detail-cancel proto-card">
			<view class="card-heading"><view class="card-heading-title"><ProtoIcon name="shield" :size="21" /><text>退订与取消保障</text></view><text class="proto-link">开票指引</text></view>
			<view class="cancel-tip">
				<view class="cancel-tip-title"><ProtoIcon name="clock" :size="19" /><text>限时无损全额取消</text></view>
				<text>入住日前1天18:00前申请取消订单，享无手续费极速全额原路退款；若逾期申请取消，将依法扣除首晚房费。</text>
			</view>
			<text class="invoice-note">发票申请：离店次日即可在线开具增值税电子普通发票</text>
		</view>

		<view class="proto-bottom-actions order-detail-actions">
			<button class="detail-action-link" hover-class="none" @tap="callStore"><ProtoIcon name="phone" :size="20" /><text>电话</text></button>
			<button v-if="!['pay', 'done', 'cancel'].includes(status)" class="proto-secondary-button" hover-class="none" @tap="goRefund">申请退款</button>
			<button v-if="status === 'done' && !hasReview" class="proto-primary-button" hover-class="none" @tap="goReview"><ProtoIcon name="star" tone="white" :size="20" /><text>评价本次入住</text></button>
			<button v-else-if="status === 'done'" class="proto-secondary-button" hover-class="none" @tap="goReviews"><ProtoIcon name="star" :size="20" /><text>查看我的评价</text></button>
			<button v-else class="proto-primary-button" hover-class="none" @tap="primaryAction"><ProtoIcon name="support" tone="white" :size="20" /><text>{{ status === 'pay' ? '继续支付' : '联系管家 / 入住指引' }}</text></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoOrders, goPage, prototypeImages, showToast } from '@/common/prototype.js'
	import { findOrder, getOrders, hasOrderReview, resolveReviewTarget } from '@/common/app-store.js'
	import { fetchRemoteOrder } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				order: demoOrders[0],
				roomImage: prototypeImages.roomOne,
				progress: [
					{ key: 'created', label: '提交订单', index: 0 },
					{ key: 'paid', label: '支付成功', index: 1 },
					{ key: 'confirmed', label: '门店确认', index: 2 },
					{ key: 'checkin', label: '到店入住', index: 3 },
					{ key: 'done', label: '离店结算', index: 4 }
				]
			}
		},
		computed: {
			status() {
				return this.order.statusKey || 'stay'
			},
			statusMeta() {
				return {
					stay: { label: '门店已接单备房', title: '已确认·待入住', desc: '门店已为您备妥客房，请凭身份证件直接办理入住。' },
					pay: { label: '待支付', title: '订单待支付', desc: '房源已锁定，请在剩余时间内完成微信支付。' },
					refund: { label: '退款中', title: '退款审核中', desc: '退款申请已提交，平台正在审核并原路返还。' },
					done: { label: '已离店', title: '订单已完成', desc: '感谢您的入住，期待下次继续为您服务。' },
					cancel: { label: '已取消', title: '订单已取消', desc: '这笔订单已取消，如需出行可以重新选择房型。' }
				}[this.status]
			},
			statusClass() {
				return this.status === 'pay' ? 'danger' : this.status === 'refund' ? 'info' : this.status === 'cancel' ? 'muted' : 'success'
			},
			progressIndex() {
				return { pay: 0, stay: 2, refund: 1, done: 4, cancel: 0 }[this.status]
			},
			hasReview() {
				return Boolean(this.order.hasReview || hasOrderReview(this.order.id))
			},
			displayGuests() {
				return this.order.guests && this.order.guests.length ? this.order.guests : [{ name: this.order.guest || '未填写', identity: '未填写', phone: '未填写' }]
			},
			roomSubtotal() {
				return Number(this.order.price || 0) + this.discount
			},
			discount() {
				return Math.min(60, Math.round(Number(this.order.price || 0) * 0.15))
			},
			paymentId() {
				return this.order.paidAt ? `WX${String(this.order.id).slice(-12)}` : '待支付'
			},
			doorValidity() {
				const checkIn = this.order.checkIn || (this.order.date || '').split(' - ')[0] || '入住日'
				const checkOut = this.order.checkOut || (this.order.date || '').split(' - ')[1] || '离店日'
				return `${checkIn} 14:00 至 ${checkOut} 12:00`
			}
		},
		onLoad(options) {
			this.loadOrder(options && options.id)
		},
		onShow() {
			this.loadOrder(this.order.id)
		},
		methods: {
			async loadOrder(id) {
				const order = id ? findOrder(id) : getOrders()[0]
				if (order) {
					this.order = order
					this.roomImage = order.image || prototypeImages.roomOne
				}
				if (id && /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(id))) {
					try {
						const remoteOrder = await fetchRemoteOrder(id, this.order)
						this.order = remoteOrder
						this.roomImage = remoteOrder.image || this.roomImage
					} catch (error) {
						// Keep the cached order detail available when the API is offline.
					}
				}
			},
			goRefund() {
				if (this.status === 'pay' || this.status === 'cancel' || this.status === 'done') {
					showToast(this.status === 'pay' ? '请先完成支付后再申请退款' : '当前订单不可申请退款')
					return
				}
				goPage(`/pages/order/refund?id=${this.order.id}`)
			},
			primaryAction() {
				if (this.status === 'pay') {
					goPage(`/pages/pay/result?orderId=${this.order.id}`)
					return
				}
				goPage(`/pages/service/contact?orderId=${this.order.id}`)
			},
			reviewQuery() {
				const target = resolveReviewTarget(this.order)
				return [
					`orderId=${encodeURIComponent(this.order.id || '')}`,
					`roomId=${encodeURIComponent(target.roomId || '')}`,
					`storeId=${encodeURIComponent(target.storeId || '')}`,
					`roomName=${encodeURIComponent(target.roomName || '')}`,
					`storeName=${encodeURIComponent(target.storeName || '')}`
				].join('&')
			},
			goReview() {
				goPage(`/pages/review/create?${this.reviewQuery()}`)
			},
			goReviews() {
				const target = resolveReviewTarget(this.order)
				goPage(`/pages/review/list?roomId=${encodeURIComponent(target.roomId || '')}&storeId=${encodeURIComponent(target.storeId || '')}&roomName=${encodeURIComponent(target.roomName || '')}&storeName=${encodeURIComponent(target.storeName || '')}`)
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.order.phone || '037188886622',
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			openLocation() {
				uni.openLocation({
					latitude: Number(this.order.latitude || 34.74725),
					longitude: Number(this.order.longitude || 113.62493),
					name: this.order.store,
					address: this.order.address || '河南省郑州市金水区郑路辅路28号',
					fail: () => showToast('地图服务暂不可用，请复制地址查看')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.order-detail-page {
		padding-bottom: 154rpx;
		padding-bottom: calc(154rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(154rpx + env(safe-area-inset-bottom));
	}

	.detail-status {
		padding: 22rpx;
	}

	.status-badge {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.status-title {
		display: block;
		margin-top: 18rpx;
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 850;
	}

	.status-desc {
		display: block;
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.progress-line {
		display: flex;
		margin-top: 30rpx;
	}

	.progress-item {
		position: relative;
		display: flex;
		align-items: center;
		flex: 1;
		flex-direction: column;
		color: var(--proto-muted);
		font-size: 17rpx;
		text-align: center;
	}

	.progress-item::before {
		position: absolute;
		top: 18rpx;
		left: -50%;
		width: 100%;
		height: 2rpx;
		background: #dbe4e5;
		content: "";
	}

	.progress-item:first-child::before {
		display: none;
	}

	.progress-dot {
		position: relative;
		z-index: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 38rpx;
		height: 38rpx;
		color: var(--proto-muted);
		background: #e6edef;
		border-radius: 50%;
	}

	.progress-dot > text {
		color: inherit;
	}

	.progress-item .small {
		margin-top: 8rpx;
		font-size: 16rpx;
	}

	.progress-item.active {
		color: var(--proto-primary-dark);
	}

	.progress-item.active .progress-dot {
		color: #ffffff;
		background: var(--proto-primary-dark);
	}

	.progress-item.active::before {
		background: var(--proto-primary);
	}

	.door-heading,
	.store-heading,
	.room-heading {
		display: flex;
		align-items: center;
		gap: 14rpx;
	}

	.door-icon,
	.store-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 54rpx;
		height: 54rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.door-heading > .door-icon,
	.store-heading > .store-mark {
		flex: 0 0 54rpx;
	}

	.door-heading > view,
	.store-heading > view,
	.room-heading > view {
		flex: 1;
		min-width: 0;
	}

	.door-title,
	.door-desc,
	.store-brand,
	.store-name,
	.room-name,
	.room-spec {
		display: block;
	}

	.door-title {
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.door-desc,
	.store-brand,
	.room-spec {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.door-heading .proto-pill {
		min-height: 34rpx;
		font-size: 17rpx;
	}

	.door-code-row {
		display: flex;
		gap: 18rpx;
		margin-top: 18rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.door-code-row > view:first-child {
		flex: 1;
	}

	.door-code-row text,
	.door-code-row .small {
		display: block;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.door-code-row .strong {
		margin-top: 8rpx;
		color: var(--proto-primary-dark);
		font-size: 38rpx;
		letter-spacing: 5rpx;
	}

	.door-code-value {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.door-code-row .small {
		margin-top: 7rpx;
	}

	.qr-placeholder {
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		width: 120rpx;
		color: var(--proto-text);
		background: #ffffff;
		border-radius: 12rpx;
	}

	.qr-placeholder-icon {
		display: block;
	}

	.qr-placeholder .small {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 16rpx;
	}

	.door-safe {
		display: flex;
		align-items: flex-start;
		gap: 7rpx;
		margin-top: 14rpx;
		padding: 14rpx;
		color: var(--proto-muted);
		background: #eff8f7;
		border-radius: 12rpx;
		font-size: 18rpx;
		line-height: 1.5;
	}

	.door-safe > text {
		flex: 1;
	}

	.detail-store {
		padding: 22rpx;
	}

	.store-name {
		margin-top: 5rpx;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.detail-address {
		display: flex;
		align-items: flex-start;
		gap: 6rpx;
		margin-top: 18rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.5;
	}

	.detail-address > text {
		flex: 1;
	}

	.detail-store-actions {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-top: 18rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.walk-caption {
		display: flex;
		align-items: center;
		gap: 5rpx;
		flex: 1;
	}

	.walk-caption > text {
		flex: 1;
	}

	.detail-store-actions .proto-button-small {
		display: inline-flex;
		align-items: center;
		gap: 5rpx;
		height: 56rpx;
		padding: 0 14rpx;
		font-size: 17rpx;
	}

	.room-heading image {
		width: 128rpx;
		height: 128rpx;
		border-radius: 14rpx;
	}

	.room-name {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.room-tags {
		display: flex;
		gap: 8rpx;
		margin-top: 12rpx;
		white-space: nowrap;
	}

	.room-tags text {
		padding: 6rpx 10rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 7rpx;
		font-size: 16rpx;
	}

	.room-date-row {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 18rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.room-date-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.room-date-side-right {
		align-items: flex-end;
		text-align: right;
	}

	.room-date-side > text:first-child {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.room-date-side .strong {
		margin-top: 5rpx;
		font-size: 24rpx;
	}

	.room-date-side .small {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 16rpx;
	}

	.date-mid {
		padding: 7rpx 10rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 14rpx;
		font-size: 16rpx;
	}

	.room-service {
		display: flex;
		align-items: flex-start;
		gap: 6rpx;
		margin-top: 14rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.room-service > text {
		flex: 1;
	}

	.card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.card-heading-title,
	.card-heading-note {
		display: flex;
		align-items: center;
		gap: 6rpx;
	}

	.identity-row,
	.fee-row {
		display: flex;
		justify-content: space-between;
		margin-top: 18rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.identity-row > text:last-child,
	.identity-value {
		color: var(--proto-text);
		text-align: right;
	}

	.identity-value {
		display: flex;
		align-items: flex-end;
		flex-direction: column;
	}

	.identity-value .small {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.detail-fee .proto-pill {
		min-height: 32rpx;
		padding: 0 9rpx;
		font-size: 16rpx;
	}

	.payment-code {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		margin-top: 18rpx;
		padding: 16rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 18rpx;
	}

	.payment-code-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.payment-code-label {
		display: flex;
		align-items: center;
		gap: 6rpx;
	}

	.payment-value {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		text-align: right;
	}

	.fee-row .discount {
		color: var(--proto-error);
	}

	.fee-total {
		display: flex;
		justify-content: space-between;
		margin-top: 18rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid #e3ebec;
		color: var(--proto-text);
	}

	.fee-total .strong {
		color: var(--proto-primary-dark);
		font-size: 36rpx;
	}

	.cancel-tip {
		margin-top: 16rpx;
		padding: 16rpx;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.cancel-tip text {
		display: block;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.cancel-tip text:first-child {
		color: var(--proto-primary-dark);
		font-weight: 750;
	}

	.cancel-tip-title {
		display: flex;
		align-items: center;
		gap: 6rpx;
		color: var(--proto-primary-dark);
		font-weight: 750;
	}

	.invoice-note {
		display: block;
		margin-top: 14rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.order-detail-actions .detail-action-link {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 5rpx;
		width: 86rpx;
		padding: 0;
		color: var(--proto-text);
		background: transparent;
		font-size: 18rpx;
	}

	.order-detail-actions .proto-secondary-button {
		flex: 0.8;
		margin: 0;
		height: 78rpx;
		font-size: 22rpx;
	}

	.order-detail-actions .proto-primary-button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 6rpx;
		flex: 1.4;
		margin: 0;
		height: 78rpx;
		font-size: 21rpx;
	}
</style>
