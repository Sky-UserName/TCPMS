<template>
	<view class="proto-page finance-page">
		<ProtoHeader title="财务统计" theme="green" />

		<view class="finance-wallet">
			<view class="finance-wallet-heading"><view><text class="finance-property">天空之蓝</text><ProtoIcon name="chevron-down" :size="22" /></view><button hover-class="none" @tap="withdraw">提现</button></view>
			<text class="finance-total">¥{{ orderStats.totalAmount.toFixed(2) }}</text>
			<text class="finance-total-label">累计金额</text>
			<view class="finance-wallet-grid">
				<view v-for="item in walletItems" :key="item.label"><text>{{ item.value }}</text><text>{{ item.label }}</text></view>
			</view>
		</view>

		<view class="finance-section-title"><text>经营概览</text><text>数据实时同步</text></view>
		<view v-for="section in sections" :key="section.title" class="finance-data-card proto-card">
			<view class="finance-card-heading"><text>{{ section.title }}</text><text v-if="section.more" class="finance-more" @tap="openDetail(section)">更多 <ProtoIcon name="chevron-right" :size="20" /></text></view>
			<view class="finance-metric-grid">
				<view v-for="metric in section.metrics" :key="metric.label" class="finance-metric"><text class="finance-metric-value" :class="{ money: metric.money }">{{ metric.value }}</text><text class="finance-metric-label">{{ metric.label }}</text></view>
			</view>
		</view>

		<view class="finance-footnote"><ProtoIcon name="info" :size="22" /><text>当前为演示数据，服务端接入后将展示真实结算信息。</text></view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { showToast } from '@/common/prototype.js'
	import { getMerchantOrders } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				orders: []
			}
		},
		computed: {
			orderStats() {
				const amount = (order) => Number(order.amount || order.price || 0)
				const count = (keys) => this.orders.filter((order) => keys.includes(order.statusKey)).length
				const sum = (keys) => this.orders.filter((order) => keys.includes(order.statusKey)).reduce((total, order) => total + amount(order), 0)
				return {
					pending: count(['pending', 'stay']),
					done: count(['done']),
					cancel: count(['cancel', 'refund']),
					pendingAmount: sum(['pending', 'stay']),
					doneAmount: sum(['done']),
					cancelAmount: sum(['cancel', 'refund']),
					totalAmount: this.orders.reduce((total, order) => total + amount(order), 0)
				}
			},
			walletItems() {
				return [
					{ value: `¥${this.orderStats.pendingAmount.toFixed(2)}`, label: '冻结中' },
					{ value: `¥${this.orderStats.doneAmount.toFixed(2)}`, label: '可提现' },
					{ value: '¥0.00', label: '已提现' },
					{ value: `¥${this.orderStats.cancelAmount.toFixed(2)}`, label: '已取消' }
				]
			},
			sections() {
				const metrics = () => [
					{ value: String(this.orderStats.pending), label: '待使用订单' },
					{ value: String(this.orderStats.done), label: '核销订单' },
					{ value: String(this.orderStats.cancel), label: '取消订单' },
					{ value: `¥${this.orderStats.pendingAmount.toFixed(2)}`, label: '待使用金额', money: true },
					{ value: `¥${this.orderStats.doneAmount.toFixed(2)}`, label: '核销订单金额', money: true },
					{ value: `¥${this.orderStats.cancelAmount.toFixed(2)}`, label: '取消订单金额', money: true }
				]
				return [
					{ title: '今日数据', more: false, metrics: metrics() },
					{ title: '近7天数据', more: true, metrics: metrics() },
					{ title: '近30天数据', more: true, metrics: metrics() },
					{ title: '全部数据', more: true, metrics: metrics() }
				]
			}
		},
		onShow() {
			this.orders = getMerchantOrders()
		},
		methods: {
			withdraw() {
				showToast('提现功能将在结算账户接入后开放')
			},
			openDetail(section) {
				showToast(`${section.title}明细将在数据接入后开放`)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.finance-page {
		padding-bottom: 50rpx;
		background: #f4fafb;
	}

	.finance-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.finance-wallet {
		margin: 22rpx 24rpx 28rpx;
		padding: 28rpx 28rpx 24rpx;
		color: #ffffff;
		background: var(--proto-primary-deep);
		border-radius: 26rpx;
		box-shadow: 0 14rpx 28rpx rgba(24, 133, 143, 0.18);
	}

	.finance-wallet-heading,
	.finance-wallet-heading > view {
		display: flex;
		align-items: center;
	}

	.finance-wallet-heading {
		gap: 16rpx;
		justify-content: space-between;
	}

	.finance-property {
		display: block;
		max-width: 390rpx;
		overflow: hidden;
		font-size: 29rpx;
		font-weight: 800;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.finance-wallet-heading button {
		flex: 0 0 auto;
		width: 136rpx;
		height: 58rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border-radius: 30rpx;
		font-size: 22rpx;
		font-weight: 750;
		white-space: nowrap;
	}

	.finance-total {
		display: block;
		margin-top: 24rpx;
		font-size: 46rpx;
		font-weight: 800;
	}

	.finance-total-label {
		display: block;
		margin-top: 4rpx;
		color: rgba(255, 255, 255, 0.82);
		font-size: 19rpx;
	}

	.finance-wallet-grid {
		display: flex;
		margin-top: 26rpx;
		padding-top: 20rpx;
		border-top: 1rpx solid rgba(255, 255, 255, 0.2);
	}

	.finance-wallet-grid > view {
		display: flex;
		flex: 1;
		align-items: center;
		flex-direction: column;
		min-width: 0;
	}

	.finance-wallet-grid > view > text:first-child {
		font-size: 24rpx;
		font-weight: 750;
	}

	.finance-wallet-grid > view > text:last-child {
		margin-top: 6rpx;
		color: rgba(255, 255, 255, 0.76);
		font-size: 17rpx;
	}

	.finance-section-title {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin: 0 24rpx 14rpx;
	}

	.finance-section-title > text:first-child {
		color: var(--proto-text);
		font-size: 29rpx;
		font-weight: 800;
	}

	.finance-section-title > text:last-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.finance-data-card {
		margin-top: 14rpx;
		padding: 24rpx 22rpx 26rpx;
	}

	.finance-card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 750;
	}

	.finance-more {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
		font-weight: 500;
		white-space: nowrap;
	}

	.finance-metric-grid {
		display: flex;
		flex-wrap: wrap;
		margin-top: 24rpx;
		row-gap: 24rpx;
	}

	.finance-metric {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: 33.3333%;
		min-width: 0;
	}

	.finance-metric-value {
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 800;
	}

	.finance-metric-value.money {
		color: var(--proto-primary-dark);
		font-size: 28rpx;
	}

	.finance-metric-label {
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		white-space: nowrap;
		text-align: center;
	}

	.finance-footnote {
		display: flex;
		align-items: flex-start;
		gap: 8rpx;
		margin: 22rpx 30rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.finance-footnote .proto-icon {
		flex: 0 0 auto;
	}
</style>
