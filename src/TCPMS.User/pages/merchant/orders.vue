<template>
	<view class="proto-page merchant-orders-page">
		<ProtoHeader title="订单管理" theme="green" />
		<view class="merchant-search-box"><ProtoIcon name="search" :size="34" /><input v-model="keyword" placeholder="输入订单号或房间名" placeholder-class="merchant-search-placeholder" /></view>
		<scroll-view class="merchant-order-tabs" scroll-x :show-scrollbar="false">
			<text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text>
		</scroll-view>
		<view class="merchant-order-summary"><view><text class="merchant-order-summary-value">{{ filteredOrders.length }}</text><text>笔订单</text></view><view><ProtoIcon name="check-circle" :size="22" /><text>本地数据已同步</text></view></view>
		<ProtoEmptyState v-if="!filteredOrders.length" title="暂无订单数据" description="住客下单后，订单会显示在这里" />
		<view v-else class="merchant-order-list">
			<view v-for="order in filteredOrders" :key="order.id" class="merchant-order-card proto-card">
				<view class="merchant-order-heading"><view><text class="merchant-order-id">订单 {{ order.id }}</text><text class="proto-pill" :class="order.statusKey === 'pending' ? 'warn' : order.statusKey === 'cancel' ? 'danger' : 'info'">{{ order.status }}</text></view><text class="merchant-order-date">{{ order.createdAt }}</text></view>
				<view class="merchant-order-main"><image :src="order.image" mode="aspectFill" /><view><text class="merchant-order-room">{{ order.roomName }}</text><text class="merchant-order-meta">入住 {{ order.date }}</text><text class="merchant-order-meta">入住人 {{ order.guest }}</text></view></view>
				<view class="merchant-order-footer"><view><text class="merchant-order-amount">¥{{ order.amount }}</text><text class="merchant-order-caption">订单金额</text></view><view class="merchant-order-actions"><button v-if="order.statusKey === 'pending'" class="proto-primary-button" hover-class="none" @tap="updateStatus(order, 'stay')">确认订单</button><button v-else-if="order.statusKey === 'stay'" class="proto-secondary-button" hover-class="none" @tap="updateStatus(order, 'done')">标记已入住</button><button v-else class="proto-ghost-button" hover-class="none" @tap="showOrderNote(order)">查看备注</button></view></view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { getMerchantOrders, updateMerchantOrder } from '@/common/app-store.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				keyword: '',
				activeTab: 'all',
				tabs: [
					{ key: 'all', label: '全部' },
					{ key: 'pending', label: '待处理' },
					{ key: 'stay', label: '待入住' },
					{ key: 'done', label: '已完成' },
					{ key: 'cancel', label: '退款/取消' }
				],
				orders: []
			}
		},
		onShow() {
			this.orders = getMerchantOrders()
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
			updateStatus(order, nextStatusKey) {
				const statusMap = { stay: '待入住', done: '已完成', cancel: '已取消' }
				const updated = updateMerchantOrder(order.id, { statusKey: nextStatusKey, status: statusMap[nextStatusKey] || order.status })
				if (updated) {
					this.orders = getMerchantOrders()
					showToast(`订单已更新为${updated.status}`)
				}
			},
			showOrderNote(order) {
				showToast(`${order.id}：暂无补充备注`)
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

	.merchant-orders-page :deep(.proto-empty-state) {
		padding-top: 76rpx;
		padding-bottom: 92rpx;
	}

	.merchant-orders-page :deep(.proto-empty-state-image) {
		width: 210rpx;
		height: 176rpx;
	}
</style>
