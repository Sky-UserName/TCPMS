<template>
	<view class="proto-page merchant-orders-page">
		<ProtoHeader title="订单管理" theme="green" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />
		<view class="merchant-search-box"><ProtoIcon name="search" :size="34" /><input v-model="keyword" placeholder="输入订单号或房间名" placeholder-class="merchant-search-placeholder" /></view>
		<scroll-view class="merchant-order-tabs" scroll-x :show-scrollbar="false">
			<text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text>
		</scroll-view>
		<view class="merchant-order-summary"><view><text class="merchant-order-summary-value">{{ filteredOrders.length }}</text><text>笔订单</text></view></view>
		<ProtoEmptyState v-if="!filteredOrders.length" title="暂无订单数据" description="住客下单后，订单会显示在这里" />
		<view v-else class="merchant-order-list">
			<view v-for="order in filteredOrders" :key="order.id" class="merchant-order-card proto-card">
				<view class="merchant-order-heading"><view><text class="merchant-order-id">订单 {{ order.id }}</text><text class="proto-pill" :class="order.statusKey === 'pending' ? 'warn' : order.statusKey === 'cancel' ? 'danger' : 'info'">{{ order.status }}</text></view><text class="merchant-order-date">{{ order.createdAt }}</text></view>
				<view class="merchant-order-main"><image v-if="order.image" :src="order.image" mode="aspectFill" /><view v-else class="merchant-order-image-empty"><ProtoIcon name="image" :size="44" /></view><view><text class="merchant-order-room">{{ order.roomName }}</text><text class="merchant-order-meta">入住 {{ order.date }}</text><text class="merchant-order-meta">入住人 {{ order.guest }}</text></view></view>
				<view class="merchant-order-footer"><view><text class="merchant-order-amount">¥{{ order.amount }}</text><text class="merchant-order-caption">订单金额</text></view><view v-if="order.status === '待核销'" class="merchant-order-actions"><button class="proto-ghost-button merchant-cancel-button" hover-class="none" @tap="cancelOrder(order)">取消订单</button><button class="proto-primary-button" hover-class="none" @tap="goVerify">扫码核销</button></view></view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import CacheRefreshIndicator from '@/components/CacheRefreshIndicator.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { cancelMerchantOrder, fetchMerchantOrders, getApiAssetUrl } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'
	import { scanAndVerifyOrder } from '@/common/verify.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				keyword: '',
				pullRefreshing: false,
				activeTab: 'all',
				tabs: [
					{ key: 'all', label: '全部' },
					{ key: 'pending', label: '待核销' },
					{ key: 'done', label: '已完成' },
					{ key: 'cancel', label: '退款/取消' }
				],
				orders: []
			}
		},
		async onShow() {
			await this.loadOrders()
		},
		onPullDownRefresh() {
			return runPullRefresh(this, () => this.loadOrders())
		},
		computed: {
			filteredOrders() {
				const keyword = this.keyword.trim().toLowerCase()
				return this.orders.filter((order) => {
					const tabMatch = this.activeTab === 'all' || order.statusKey === this.activeTab
					const textMatch = !keyword || `${order.id} ${order.roomName} ${order.guest}`.toLowerCase().includes(keyword)
					return tabMatch && textMatch
				})
			}
		},
		methods: {
			async loadOrders() {
				try {
					const data = await fetchMerchantOrders()
					this.orders = data.map((order) => ({
						...order,
						image: getApiAssetUrl(order.image)
					}))
				} catch (error) {
					showToast(error?.message || '订单加载失败')
				}
			},
			async goVerify() {
				await scanAndVerifyOrder()
				await this.loadOrders()
			},
			cancelOrder(order) {
				const paid = order.paymentStatus === 'Paid'
				uni.showModal({
					title: '取消订单',
					content: paid ? '取消后将把已支付金额原路退回用户，确定继续吗？' : '确定取消该订单吗？',
					confirmText: paid ? '退款取消' : '确认取消',
					confirmColor: '#c25b5b',
					success: (result) => {
						if (result.confirm) this.submitCancel(order, paid)
					},
					fail: (error) => {
						showToast(error?.errMsg || '确认弹窗打开失败，请重试')
					}
				})
			},
			async submitCancel(order, paid) {
				uni.showLoading({ title: paid ? '正在退款' : '正在取消', mask: true })
				try {
					await cancelMerchantOrder(order.orderId)
					uni.hideLoading()
					uni.showToast({ title: paid ? '已取消，退款将原路退回' : '订单已取消', icon: 'none' })
					await this.loadOrders()
				} catch (error) {
					uni.hideLoading()
					showToast(error?.message || '取消订单失败')
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-orders-page {
		padding-bottom: 44rpx;
		background: #f4fafb;
	}

	.merchant-orders-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.merchant-search-box {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin: 22rpx 24rpx 14rpx;
		padding: 0 22rpx;
		height: 78rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border: 1rpx solid #dce8ed;
		border-radius: 39rpx;
	}

	.merchant-search-box input {
		flex: 1;
		min-width: 0;
		font-size: 23rpx;
	}

	.merchant-search-placeholder {
		color: #97a2a8;
	}

	.merchant-order-tabs {
		padding: 4rpx 24rpx 0;
		background: #ffffff;
		white-space: nowrap;
	}

	.merchant-order-tabs text {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 108rpx;
		height: 68rpx;
		margin-right: 14rpx;
		color: var(--proto-muted);
		border-bottom: 5rpx solid transparent;
		font-size: 21rpx;
	}

	.merchant-order-tabs text.active {
		color: var(--proto-primary-dark);
		border-bottom-color: var(--proto-primary);
		font-weight: 750;
	}

	.merchant-order-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 18rpx;
		margin: 18rpx 24rpx 0;
		padding: 18rpx 20rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		font-size: 19rpx;
	}

	.merchant-order-summary > view {
		display: flex;
		align-items: center;
		min-width: 0;
		gap: 8rpx;
	}

	.merchant-order-summary-value {
		color: var(--proto-text);
		font-size: 32rpx;
		font-weight: 800;
	}

	.merchant-order-summary > view:last-child {
		color: var(--proto-primary-dark);
	}

	.merchant-order-list {
		padding-bottom: 18rpx;
	}

	.merchant-order-card {
		padding: 20rpx;
	}

	.merchant-order-heading,
	.merchant-order-heading > view,
	.merchant-order-footer,
	.merchant-order-actions {
		display: flex;
		align-items: center;
	}

	.merchant-order-heading {
		justify-content: space-between;
		gap: 14rpx;
	}

	.merchant-order-heading > view {
		gap: 10rpx;
		flex: 1;
		min-width: 0;
	}

	.merchant-order-id {
		max-width: 330rpx;
		overflow: hidden;
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-order-date {
		flex: 0 0 auto;
		color: var(--proto-muted);
		font-size: 16rpx;
	}

	.merchant-order-main {
		display: flex;
		gap: 16rpx;
		margin-top: 20rpx;
	}

	.merchant-order-main image {
		width: 156rpx;
		height: 118rpx;
		flex: 0 0 156rpx;
		border-radius: 14rpx;
		background: var(--proto-surface-low);
	}

	.merchant-order-image-empty {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 156rpx;
		height: 118rpx;
		flex: 0 0 156rpx;
		color: #a9b9bd;
		background: var(--proto-surface-low);
		border-radius: 14rpx;
	}

	.merchant-order-main > view {
		display: flex;
		justify-content: center;
		flex-direction: column;
		min-width: 0;
	}

	.merchant-order-room {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-order-meta {
		margin-top: 9rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-order-footer {
		justify-content: space-between;
		gap: 12rpx;
		margin-top: 18rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #edf1f2;
	}

	.merchant-order-footer > view:first-child {
		display: flex;
		align-items: baseline;
		gap: 8rpx;
	}

	.merchant-order-amount {
		color: var(--proto-primary-dark);
		font-size: 28rpx;
		font-weight: 800;
	}

	.merchant-order-caption {
		color: var(--proto-muted);
		font-size: 16rpx;
	}

	.merchant-order-actions {
		flex: 0 0 auto;
		gap: 10rpx;
	}

	.merchant-order-actions button {
		min-width: 132rpx;
		height: 54rpx;
		margin: 0;
		padding: 0 18rpx;
		font-size: 18rpx;
		white-space: nowrap;
	}

	.merchant-order-actions .merchant-cancel-button {
		color: #b44f4f;
		background: #fff2f2;
		border: 1rpx solid #f0cccc;
	}

	.merchant-orders-page :deep(.proto-empty-state) {
		padding-top: 76rpx;
		padding-bottom: 92rpx;
	}

	.merchant-orders-page :deep(.proto-empty-state-image) {
		width: 210rpx;
		height: 176rpx;
	}
</style>
