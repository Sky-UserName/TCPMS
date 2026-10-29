<template>
	<view class="proto-page pay-result-page">
		<ProtoHeader title="支付结果" title-align="center" theme="green" />

		<view class="pay-hero" :class="'is-' + status">
			<view class="pay-icon">
				<ProtoIcon :name="statusIcon" :size="54" :tone="status === 'success' ? 'white' : 'default'" />
			</view>
			<text class="pay-title">{{ statusTitle }}</text>
			<text class="pay-desc">{{ statusDesc }}</text>
			<view class="pay-amount">
				<text class="pay-amount-unit">¥</text>
				<text class="pay-amount-value">{{ formatYuan(order.price) }}</text>
			</view>
			<view class="pay-channel"><ProtoIcon name="shield" :size="20" /><text>微信安全支付 · 平台担保交易</text></view>
		</view>

		<view class="pay-card proto-card">
			<view class="pay-room">
				<image :src="roomImage" mode="aspectFill"></image>
				<view class="pay-room-body">
					<text class="pay-store">{{ order.store }}</text>
					<text class="pay-room-name">{{ order.room }}</text>
					<view class="pay-room-date"><ProtoIcon name="calendar" :size="18" /><text>{{ order.date }} · 共{{ order.nights || 1 }}晚</text></view>
				</view>
			</view>

			<view class="pay-divider"></view>

			<view class="pay-info-row">
				<text class="pay-info-label">订单编号</text>
				<view class="pay-info-value">
					<text class="pay-code">{{ order.orderNumber || order.id }}</text>
					<text class="pay-copy" @tap.stop="copyOrderNumber">复制</text>
				</view>
			</view>
			<view class="pay-info-row">
				<text class="pay-info-label">入住人员</text>
				<text class="pay-info-value">{{ order.guest || '未填写' }}</text>
			</view>
			<view class="pay-info-row">
				<text class="pay-info-label">预订房间</text>
				<text class="pay-info-value">{{ order.roomCount || 1 }} 间</text>
			</view>
			<view class="pay-info-row">
				<text class="pay-info-label">支付方式</text>
				<view class="pay-info-value pay-info-pay"><ProtoIcon name="wallet" :size="20" /><text>微信支付</text></view>
			</view>
		</view>

		<view class="pay-actions">
			<view class="pay-actions-main">
				<view class="pay-actions-total">
					<text class="pay-actions-label">{{ status === 'success' ? '实付金额' : '应付金额' }}</text>
					<text class="pay-actions-amount">¥{{ formatYuan(order.price) }}</text>
				</view>
				<button v-if="status === 'success'" class="pay-cta" hover-class="none" @tap="goOrder">查看订单详情</button>
				<button v-else class="pay-cta" :class="{ 'is-loading': status === 'query' }" hover-class="none" @tap="payOrder">{{ status === 'query' ? '正在确认支付...' : '微信安全支付' }}</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, goRoot, goTab, prototypeImages, showToast } from '@/common/prototype.js'
	import { findOrder, getOrders } from '@/common/app-store.js'
	import { fetchRemoteOrder, confirmRemotePayment, mockPayRemoteOrder, requestRemotePayment } from '@/common/api.js'
	import { ALLOW_MOCK_PAY } from '@/common/env.js'
	import { formatYuan } from '@/common/format.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				status: 'pending',
				// 没有取到真实订单时给一个空壳，避免模板直接读 demo 数据。
				order: {
					id: '',
					price: 0,
					store: '',
					room: '',
					date: '',
					nights: 1,
					roomCount: 1,
					guest: ''
				},
				roomImage: prototypeImages.roomOne,
			}
		},
		computed: {
			statusTitle() {
				return {
					success: '支付成功',
					query: '正在确认支付结果'
				}[this.status] || '待完成支付'
			},
			statusDesc() {
				return {
					success: '订单已提交，门店正在快速确认',
					query: '正在查询微信支付状态，请稍候'
				}[this.status] || '确认订单信息后完成微信支付'
			},
			statusIcon() {
				return this.status === 'success' ? 'check-circle' : this.status === 'query' ? 'refresh' : 'wallet'
			}
		},
		onLoad(options) {
			const order = options && options.orderId ? findOrder(options.orderId) : getOrders()[0]
			if (!order) {
				showToast('订单不存在或已失效')
				goRoot('/pages/index/index')
				return
			}

			this.order = order
			this.roomImage = order.image || prototypeImages.roomOne
			if (order.statusKey === 'stay' || order.statusKey === 'done') {
				this.status = 'success'
				this.goOrderList()
			}
		},
		methods: {
			formatYuan,
			copyOrderNumber() {
				const code = String(this.order.orderNumber || this.order.id || '')
				if (!code) {
					showToast('订单编号暂不可用')
					return
				}
				uni.setClipboardData({
					data: code,
					success: () => showToast('订单编号已复制')
				})
			},
			goOrder() {
				goPage(`/pages/order/detail?id=${this.order.id}`)
			},
			goOrderList() {
				goTab('/pages/order/list')
			},
			async payOrder() {
				if (this.status === 'query') return
				const orderId = String(this.order.id || '')
				// 只有服务端真实订单才能发起支付，本地草稿单不再走任何降级逻辑。
				if (!/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(orderId)) {
					showToast('订单尚未提交成功，请返回重新下单')
					return
				}

				this.status = 'query'
				try {
					const payment = await requestRemotePayment(orderId)
					await new Promise((resolve, reject) => {
						uni.requestPayment({
							provider: 'wxpay',
							timeStamp: payment.timeStamp,
							nonceStr: payment.nonceStr,
							package: payment.package,
							signType: payment.signType,
							paySign: payment.paySign,
							success: resolve,
							fail: reject
						})
					})
				} catch (error) {
					this.status = 'pending'
					// 接口返回的业务错误带 code；uni.requestPayment 失败只有 errMsg，两者分开处理。
					const isApiError = Boolean(error?.code) && !error?.errMsg
					if (isApiError && (error.code === 'wechat_pay_not_configured' || error.code === 'wechat_pay_rejected')) {
						const reason = error.message || '微信支付未受理'
						if (!ALLOW_MOCK_PAY) {
							showToast(reason)
							return
						}
						// 本地联调：微信支付不通时给出真实原因，并允许用模拟支付继续走完流程。
						const confirmed = await new Promise((resolve) => {
							uni.showModal({
								title: '微信支付未受理',
								content: `${reason}。开发环境下可用模拟支付继续联调。`,
								confirmText: '模拟支付',
								cancelText: '取消',
								success: (res) => resolve(Boolean(res.confirm)),
								fail: () => resolve(false)
							})
						})
						if (!confirmed) return
						try {
							this.order = await mockPayRemoteOrder(orderId)
							this.status = 'success'
							showToast('已使用模拟支付')
							this.goOrderList()
						} catch (mockError) {
							showToast(mockError?.message || '模拟支付失败')
						}
						return
					}
					showToast(error?.errMsg?.includes('cancel') ? '已取消支付' : error?.message || '微信支付失败')
					return
				}

				// 支付成功后只认服务端状态：先让服务端主动查单，查不到再读订单详情。
				let confirmedOrder = null
				for (let attempt = 0; attempt < 5; attempt += 1) {
					confirmedOrder = await confirmRemotePayment(orderId, this.order).catch(() => null)
					if (!confirmedOrder) {
						confirmedOrder = await fetchRemoteOrder(orderId, this.order).catch(() => null)
					}
					if (confirmedOrder && confirmedOrder.paymentStatus === 'Paid') break
					await new Promise((resolve) => setTimeout(resolve, 800))
				}

				if (!confirmedOrder || confirmedOrder.paymentStatus !== 'Paid') {
					// 微信已扣款但服务端还没确认，交给「我的订单」和支付对账任务兜底，不在本地伪造成功。
					this.status = 'pending'
					showToast('支付结果确认中，请稍后在“我的订单”查看')
					return
				}

				this.order = confirmedOrder
				this.status = 'success'
				this.goOrderList()
			}
		}
	}
</script>

<style lang="scss" scoped>
	.pay-result-page {
		padding-bottom: 260rpx;
		padding-bottom: calc(260rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(260rpx + env(safe-area-inset-bottom));
	}

	/* ---------- 顶部状态 ---------- */
	.pay-hero {
		display: flex;
		align-items: center;
		flex-direction: column;
		margin: 20rpx 24rpx 0;
		padding: 48rpx 30rpx 38rpx;
		background: linear-gradient(180deg, #e6fbfa 0%, #ffffff 78%);
		border: 1rpx solid rgba(42, 169, 169, 0.14);
		border-radius: 30rpx;
		box-shadow: 0 14rpx 36rpx rgba(42, 169, 169, 0.08);
	}

	.pay-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 136rpx;
		height: 136rpx;
		color: #006a6a;
		color: var(--proto-primary-dark, #006a6a);
		background: #d3f4f3;
		border-radius: 50%;
		box-shadow: 0 14rpx 30rpx rgba(42, 169, 169, 0.16);
	}

	.pay-hero.is-success .pay-icon {
		background: #10b981;
		background: var(--proto-success, #10b981);
	}

	.pay-hero.is-query .pay-icon {
		background: #fff0cf;
		box-shadow: 0 14rpx 30rpx rgba(245, 158, 11, 0.16);
	}

	.pay-title {
		margin-top: 26rpx;
		color: var(--proto-text);
		font-size: 42rpx;
		font-weight: 850;
		letter-spacing: 1rpx;
	}

	.pay-desc {
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 24rpx;
		line-height: 1.5;
		text-align: center;
	}

	.pay-amount {
		display: flex;
		align-items: baseline;
		margin-top: 26rpx;
		color: var(--proto-text);
	}

	.pay-amount-unit {
		margin-right: 4rpx;
		font-size: 34rpx;
		font-weight: 700;
	}

	.pay-amount-value {
		font-size: 72rpx;
		font-weight: 900;
		letter-spacing: -1rpx;
		line-height: 1;
	}

	.pay-channel {
		display: flex;
		align-items: center;
		gap: 6rpx;
		margin-top: 20rpx;
		padding: 8rpx 20rpx;
		color: var(--proto-primary-dark);
		background: rgba(42, 169, 169, 0.1);
		border-radius: 24rpx;
		font-size: 21rpx;
	}

	/* ---------- 订单信息 ---------- */
	.pay-card {
		margin-top: 20rpx;
		padding: 26rpx;
	}

	.pay-room {
		display: flex;
		gap: 20rpx;
	}

	.pay-room image {
		flex: 0 0 152rpx;
		width: 152rpx;
		height: 152rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.pay-room-body {
		display: flex;
		justify-content: center;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.pay-store,
	.pay-room-name {
		display: block;
	}

	.pay-store {
		color: var(--proto-muted);
		font-size: 24rpx;
	}

	.pay-room-name {
		margin-top: 8rpx;
		color: var(--proto-text);
		font-size: 32rpx;
		font-weight: 800;
		line-height: 1.35;
	}

	.pay-room-date {
		display: flex;
		align-items: center;
		gap: 6rpx;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 23rpx;
	}

	.pay-divider {
		height: 1rpx;
		margin: 24rpx 0 4rpx;
		background: #eef2f3;
	}

	.pay-info-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 20rpx;
		margin-top: 22rpx;
		font-size: 26rpx;
	}

	.pay-info-label {
		flex: 0 0 auto;
		color: var(--proto-muted);
	}

	.pay-info-value {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		gap: 10rpx;
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		text-align: right;
	}

	.pay-code {
		overflow: hidden;
		font-size: 24rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.pay-copy {
		flex: 0 0 auto;
		padding: 4rpx 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 18rpx;
		font-size: 20rpx;
	}

	.pay-info-pay {
		color: var(--proto-primary-dark);
	}

	/* ---------- 底部支付栏 ---------- */
	.pay-actions {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 40;
		padding: 18rpx 24rpx 12rpx;
		padding-bottom: calc(12rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(12rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(197, 199, 202, 0.28);
		box-shadow: 0 -10rpx 30rpx rgba(32, 37, 43, 0.06);
	}

	.pay-actions-main {
		display: flex;
		align-items: center;
		gap: 20rpx;
	}

	.pay-actions-total {
		display: flex;
		flex: 0 0 auto;
		flex-direction: column;
	}

	.pay-actions-label {
		color: var(--proto-muted);
		font-size: 22rpx;
	}

	.pay-actions-amount {
		margin-top: 2rpx;
		color: var(--proto-text);
		font-size: 36rpx;
		font-weight: 850;
		line-height: 1.1;
	}

	.pay-cta {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 1;
		height: 88rpx;
		color: #ffffff;
		background: #2aa9a9;
		background: var(--proto-primary, #2aa9a9);
		border-radius: 44rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.24);
		font-size: 30rpx;
		font-weight: 800;
	}

	.pay-cta.is-loading {
		opacity: 0.7;
	}

</style>
