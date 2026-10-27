<template>
	<view class="proto-page proto-page-plain pay-result-page">
		<ProtoHeader title="支付结果" title-align="center" theme="green" />

		<view class="pay-hero" :class="status">
			<view class="pay-icon">
				<ProtoIcon :name="status === 'success' ? 'check-circle' : status === 'query' ? 'refresh' : 'wallet'" :size="58" />
			</view>
			<text class="pay-title">{{ status === 'success' ? '支付成功' : status === 'query' ? '正在确认支付结果' : '待完成支付' }}</text>
			<view class="pay-status-pill"><ProtoIcon :name="status === 'success' ? 'check-circle' : status === 'query' ? 'refresh' : 'wallet'" :size="17" /><text>{{ status === 'success' ? '订单已提交 · 门店快速确认中' : status === 'query' ? '正在查询微信支付状态，请稍候' : '请确认订单信息后完成微信支付' }}</text></view>
			<text class="pay-amount">¥{{ formatYuan(order.price) }}</text>
			<text class="pay-method">微信安全支付</text>
		</view>

		<view class="pay-order-card proto-card">
			<view class="pay-room-summary">
				<image :src="roomImage" mode="aspectFill"></image>
				<view>
					<text class="proto-pill info">特级精选</text>
					<text class="pay-store">{{ order.store }}</text>
					<text class="pay-room">{{ order.room }}</text>
					<view class="pay-date"><ProtoIcon name="calendar" :size="20" /><text>{{ order.date }} · 共{{ order.nights || 1 }}晚</text></view>
				</view>
			</view>
			<view class="pay-info-row"><text>订单编号</text><view><text>{{ order.id }}</text><ProtoIcon name="document" :size="20" /></view></view>
			<view class="pay-info-row"><text>入住人员</text><text>{{ order.guest || '未填写' }}</text></view>
			<view class="pay-info-row"><text>保留房间</text><text>{{ order.roomCount || 1 }}间 · 预订房型</text></view>
			<view class="pay-info-row"><text>结算方式</text><view class="pay-safe"><ProtoIcon name="check-circle" :size="20" /><text>微信免密/指纹快捷支付</text></view></view>
		</view>

		<view class="guarantee-card">
			<view class="guarantee-title"><ProtoIcon name="shield" :size="22" /><text>云舍·暖心无忧保障</text></view>
			<text>•　门店预计将在15分钟内快速完成接单，确认后同步发送微信服务通知及短信凭证。</text>
			<text>•　入住日前1天18:00前申请取消，享免手续费并极速原路退款。</text>
			<text>•　已为您免费配置无接触自助密码锁与一次性纯棉亲肤洗漱套装。</text>
		</view>

		<view class="manager-card proto-card">
			<view class="manager-avatar"><ProtoIcon name="support" :size="30" /></view>
			<view>
				<text class="manager-title">门店24小时旅宿管家</text>
				<text class="manager-desc">路线指引 · 密码门锁 · 行李寄存协助</text>
			</view>
			<button class="proto-button-small proto-button-light" hover-class="none" @tap="goService"><ProtoIcon name="phone" :size="20" /><text>联系管家</text></button>
		</view>

		<button v-if="status === 'success'" class="proto-primary-button" hover-class="none" @tap="goOrder"><ProtoIcon name="receipt" tone="white" :size="24" /><text>查看订单详情</text></button>
		<button v-else class="proto-primary-button" hover-class="none" @tap="payOrder">{{ status === 'query' ? '正在确认支付...' : '微信安全支付' }}</button>
		<button class="proto-secondary-button" hover-class="none" @tap="goHome">{{ status === 'success' ? '返回预订首页' : '稍后再付' }}</button>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, goRoot, prototypeImages, showToast } from '@/common/prototype.js'
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
		onLoad(options) {
			const order = options && options.orderId ? findOrder(options.orderId) : getOrders()[0]
			if (!order) {
				showToast('订单不存在或已失效')
				goRoot('/pages/index/index')
				return
			}

			this.order = order
			this.roomImage = order.image || prototypeImages.roomOne
			if (order.statusKey === 'stay' || order.statusKey === 'done') this.status = 'success'
		},
		methods: {
			formatYuan,
			goOrder() {
				goPage(`/pages/order/detail?id=${this.order.id}`)
			},
			goHome() {
				goRoot('/pages/index/index')
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
					// 本地联调：微信支付没配好时走开发态模拟支付，服务端状态仍然真实落库，不伪造本地订单。
					if (error?.code === 'wechat_pay_not_configured' && ALLOW_MOCK_PAY) {
						try {
							this.order = await mockPayRemoteOrder(orderId)
							this.status = 'success'
							showToast('已使用本地模拟支付')
						} catch (mockError) {
							this.status = 'pending'
							showToast(mockError?.message || '模拟支付失败')
						}
						return
					}
					this.status = 'pending'
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
			},
			goService() {
				goPage(`/pages/service/contact?orderId=${this.order.id}`)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.pay-result-page {
		padding-bottom: 40rpx;
	}

	.pay-hero {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 56rpx 30rpx 34rpx;
	}

	.pay-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 132rpx;
		height: 132rpx;
		color: var(--proto-primary-dark);
		background: #80e7ee;
		border-radius: 50%;
		box-shadow: 0 16rpx 36rpx rgba(42, 169, 169, 0.18);
	}

	.pay-hero.fail .pay-icon {
		color: #ffffff;
		background: var(--proto-error);
	}

	.pay-hero.query .pay-icon {
		color: var(--proto-warning);
		background: #fff0cf;
	}

	.pay-title {
		margin-top: 28rpx;
		color: var(--proto-text);
		font-size: 40rpx;
		font-weight: 850;
	}

	.pay-status-pill {
		display: flex;
		align-items: center;
		gap: 5rpx;
		margin-top: 12rpx;
		padding: 8rpx 18rpx;
		color: var(--proto-primary-dark);
		background: #8debf2;
		border-radius: 20rpx;
		font-size: 20rpx;
	}

	.pay-hero.fail .pay-status-pill {
		color: var(--proto-error);
		background: #ffe3df;
	}

	.pay-hero.query .pay-status-pill {
		color: #93580b;
		background: #fff0cf;
	}

	.pay-amount {
		margin-top: 28rpx;
		color: var(--proto-text);
		font-size: 58rpx;
		font-weight: 850;
	}

	.pay-method {
		margin-top: 8rpx;
		padding: 7rpx 12rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 8rpx;
		font-size: 18rpx;
	}

	.pay-order-card {
		padding: 24rpx;
	}

	.pay-room-summary {
		display: flex;
		gap: 16rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.pay-room-summary image {
		width: 138rpx;
		height: 138rpx;
		border-radius: 14rpx;
	}

	.pay-room-summary > view {
		flex: 1;
		min-width: 0;
	}

	.pay-room-summary .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 16rpx;
	}

	.pay-store,
	.pay-room {
		display: block;
	}

	.pay-store {
		margin-top: 8rpx;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
	}

	.pay-room {
		margin-top: 7rpx;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 800;
	}

	.pay-date {
		display: flex;
		align-items: center;
		gap: 6rpx;
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.pay-info-row {
		display: flex;
		justify-content: space-between;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 22rpx;
	}

	.pay-info-row > text:last-child,
	.pay-info-row > view:last-child {
		min-width: 0;
		max-width: 68%;
		color: var(--proto-text);
		overflow: hidden;
		text-overflow: ellipsis;
		text-align: right;
		white-space: nowrap;
	}

	.pay-info-row > view:last-child,
	.pay-info-row .pay-safe {
		display: flex;
		align-items: center;
		gap: 6rpx;
	}

	.pay-info-row .pay-safe {
		color: var(--proto-success);
	}

	.guarantee-card {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		margin: 18rpx 24rpx;
		padding: 24rpx;
		color: var(--proto-text);
		background: #c7f4f5;
		border-radius: 22rpx;
		font-size: 21rpx;
		line-height: 1.5;
	}

	.guarantee-title {
		display: flex;
		align-items: center;
		gap: 8rpx;
		color: var(--proto-primary-dark);
		font-size: 27rpx;
		font-weight: 800;
	}

	.manager-card {
		display: flex;
		align-items: center;
		gap: 14rpx;
		padding: 18rpx;
	}

	.manager-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
		font-size: 31rpx;
	}

	.manager-card > view:nth-child(2) {
		flex: 1;
	}

	.manager-title,
	.manager-desc {
		display: block;
	}

	.manager-title {
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
	}

	.manager-desc {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.manager-card .proto-button-small {
		height: 56rpx;
		padding: 0 16rpx;
		font-size: 18rpx;
	}
</style>
