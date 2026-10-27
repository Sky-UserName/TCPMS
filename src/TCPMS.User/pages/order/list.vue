<template>
	<view class="proto-page order-list-page">
		<ProtoHeader title="我的订单" title-align="center" theme="green" :back="false"/>
		<template v-if="loggedIn">
		<view class="order-search-box">
			<ProtoIcon name="search" :size="34" />
			<input v-model="searchText" placeholder="请输入房间名、订单号" placeholder-class="order-search-placeholder" />
		</view>
		<view class="order-tabs">
			<text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text>
		</view>
		<view class="order-summary-strip">
			<view class="order-summary-item"><text class="summary-value">{{ orderSummary.total }}</text><text class="summary-label">全部订单</text></view>
			<view class="order-summary-item"><text class="summary-value">{{ orderSummary.pending }}</text><text class="summary-label">待处理</text></view>
			<view class="order-summary-sync" @tap="loadOrders"><ProtoIcon :name="syncError ? 'warning' : syncing ? 'refresh' : 'check-circle'" :size="22" /><text>{{ syncError ? '同步失败，点此重试' : syncing ? '正在同步' : '接口已同步' }}</text></view>
		</view>
		<view v-if="syncError" class="order-sync-error">暂时无法连接订单服务，当前列表可能是本机缓存。{{ syncError }}</view>
		<view v-if="visibleOrders.length" class="order-section-heading"><text>订单列表</text><text>共 {{ visibleOrders.length }} 笔</text></view>

		<ProtoEmptyState v-if="!visibleOrders.length" action-text="去首页逛逛" @action="goHome" />
		<view v-else class="order-list">
			<view v-for="order in visibleOrders" :key="order.id" class="order-card proto-card" @tap="goDetail(order)">
				<view class="order-card-heading">
					<view class="order-store">
						<ProtoIcon name="store" :size="24" />
						<text>{{ order.store }}</text>
						<ProtoIcon name="chevron-right" :size="22" />
					</view>
					<text class="proto-pill" :class="order.statusKey === 'pay' ? 'danger' : order.statusKey === 'refund' ? 'info' : order.statusKey === 'done' ? 'muted' : 'success'">{{ order.status }}</text>
				</view>
				<view class="order-number">
					<text>订单号：{{ order.id }}</text>
					<ProtoIcon name="document" :size="20" />
				</view>
				<view class="order-content">
					<image :src="order.image" mode="aspectFill"></image>
					<view>
						<text class="order-room">{{ order.room }}</text>
						<text class="order-room-desc">{{ order.statusKey === 'pay' ? '独立阅读灯 · 静音防夹遮光帘 · 独立密码储物柜' : '独立房间 · 配备高品质床品与智能投影' }}</text>
						<view class="order-date"><ProtoIcon name="calendar" :size="20" /><text>{{ order.date }}　共{{ order.nights || 1 }}晚</text></view>
						<view class="order-guest"><ProtoIcon name="group" :size="20" /><text>入住人：{{ order.guest }}</text></view>
					</view>
				</view>
				<view class="order-price-row">
					<text class="order-note">{{ order.statusKey === 'pay' ? '房间已锁定15分钟' : order.statusKey === 'refund' ? '极速原路返还' : '初秋连住立减 · 在线免押担保' }}</text>
					<text class="order-price">¥{{ order.price }}<text class="small">.00</text></text>
				</view>
				<view class="order-actions">
					<button v-if="order.statusKey === 'stay'" class="proto-secondary-button" hover-class="none" @tap.stop="goRefund(order)">申请退款</button>
					<button v-else-if="order.statusKey === 'pay'" class="proto-secondary-button" hover-class="none" @tap.stop="cancelOrder(order)">取消订单</button>
					<button v-else-if="order.statusKey === 'done' && !order.hasReview" class="proto-secondary-button" hover-class="none" @tap.stop="goReview(order)">评价</button>
					<button v-else-if="order.statusKey === 'done' && order.hasReview" class="proto-secondary-button" hover-class="none" @tap.stop="goReviews(order)">查看评价</button>
					<button class="proto-ghost-button" hover-class="none" @tap.stop="handleAction(order)">
						<text>{{ order.action }}</text>
						<ProtoIcon name="chevron-right" :size="22" />
					</button>
				</view>
			</view>
		</view>

		<view v-if="visibleOrders.length" class="order-footer-note">已加载全部订单 · TCPMS 智选保障<text class="small">房源真实性 100% 官方现场实勘</text></view>
		</template>

		<template v-else>
			<view class="order-login-panel">
				<view class="order-login-icon"><ProtoIcon name="receipt" :size="64" /></view>
				<text class="order-login-title">登录后查看订单</text>
				<text class="order-login-desc">订单与入住信息仅对本人可见，登录后自动同步你的全部预订。</text>
				<button class="order-login-button" hover-class="none" @tap="goLogin">
					<ProtoIcon name="user" tone="white" :size="26" />
					<text>微信授权登录</text>
				</button>
			</view>
		</template>

		<ProtoBottomNav active="orders" variant="root" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { goPage, goRoot, showToast } from '@/common/prototype.js'
	import { getApiToken, getOrders, isLoggedIn, resolveReviewTarget, updateOrder } from '@/common/app-store.js'
	import { cancelRemoteOrder, fetchRemoteOrders, redirectToLogin } from '@/common/api.js'

	const isRemoteId = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				orders: [],
				loggedIn: false,
				syncError: '',
				syncing: true,
				searchText: '',
				activeTab: 'all',
				tabs: [
					{ key: 'all', label: '全部' },
					{ key: 'pay', label: '待付款' },
					{ key: 'stay', label: '可使用' },
					{ key: 'done', label: '已使用' },
					{ key: 'cancel', label: '取消/退款' }
				]
			}
		},
		computed: {
			orderSummary() {
				return {
					total: this.orders.length,
					pending: this.orders.filter((order) => ['pay', 'stay', 'refund'].includes(order.statusKey)).length
				}
			},
			visibleOrders() {
				const keyword = this.searchText.trim().toLowerCase()
				return this.orders.filter((order) => {
					const tabMatch = this.activeTab === 'all'
						|| (this.activeTab === 'cancel' && ['cancel', 'refund'].includes(order.statusKey))
						|| order.statusKey === this.activeTab
					const text = [order.id, order.store, order.room, order.guest].join(' ').toLowerCase()
					return tabMatch && (!keyword || text.indexOf(keyword) >= 0)
				})
			}
		},
		async onShow() {
			uni.hideTabBar({ animation: false, fail: () => {} })
			await this.loadOrders()
		},
		methods: {
			async loadOrders() {
				this.loggedIn = isLoggedIn() && Boolean(getApiToken())
				this.syncError = ''
				if (!this.loggedIn) {
					// 未登录不展示任何订单（包括演示订单），登录后才拉取本人订单。
					this.orders = []
					this.syncing = false
					return
				}
				// 先铺本机缓存的真实订单（GUID），再用服务端 /orders/me 的结果覆盖。
				this.orders = getOrders().filter((order) => isRemoteId(order.id))
				this.syncing = true
				try {
					this.orders = await fetchRemoteOrders()
				} catch (error) {
					this.syncError = error?.message || '网络异常'
				} finally {
					this.syncing = false
				}
			},
			goLogin() {
				redirectToLogin()
			},
			goDetail(order) {
				goPage(`/pages/order/detail?id=${order.id}`)
			},
			handleAction(order) {
				if (order.statusKey === 'pay') {
					goPage(`/pages/pay/result?orderId=${order.id}`)
					return
				}
				if (order.statusKey === 'refund') {
					goPage(`/pages/order/detail?id=${order.id}`)
					return
				}
				if (order.statusKey === 'done') {
					if (order.hasReview) {
						this.goReviews(order)
					} else {
						this.goReview(order)
					}
					return
				}
				goPage(`/pages/order/detail?id=${order.id}`)
			},
			reviewQuery(order) {
				const target = resolveReviewTarget(order)
				return [
					`orderId=${encodeURIComponent(order.id || '')}`,
					`roomId=${encodeURIComponent(target.roomId || '')}`,
					`storeId=${encodeURIComponent(target.storeId || '')}`,
					`roomName=${encodeURIComponent(target.roomName || '')}`,
					`storeName=${encodeURIComponent(target.storeName || '')}`
				].join('&')
			},
			goReview(order) {
				goPage(`/pages/review/create?${this.reviewQuery(order)}`)
			},
			goReviews(order) {
				const target = resolveReviewTarget(order)
				goPage(`/pages/review/list?roomId=${encodeURIComponent(target.roomId || '')}&storeId=${encodeURIComponent(target.storeId || '')}&roomName=${encodeURIComponent(target.roomName || '')}&storeName=${encodeURIComponent(target.storeName || '')}`)
			},
			goRefund(order) {
				goPage(`/pages/order/refund?id=${order.id}`)
			},
			cancelOrder(order) {
				uni.showModal({
					title: '取消订单',
					content: '确认取消这笔待支付订单吗？',
					success: async (res) => {
						if (!res.confirm) return
						if (isRemoteId(order.id)) {
							try {
								await cancelRemoteOrder(order.id)
							} catch (error) {
								showToast(error?.message || '取消未提交，请重试')
								return
							}
						} else {
							updateOrder(order.id, {
								status: '已取消',
								statusKey: 'cancel',
								action: '再次预订'
							})
							this.orders = getOrders().filter((item) => isRemoteId(item.id))
							showToast('演示订单仅在本机更新')
							return
						}
						// 取消成功后重新拉取本人订单，避免本地缓存里残留旧状态。
						await this.loadOrders()
						showToast('订单已取消')
					}
				})
			},
			goHome() {
				goRoot('/pages/index/index')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.order-sync-error {
		margin: 12rpx 24rpx;
		padding: 14rpx 18rpx;
		color: #8a4b13;
		background: #fff4e5;
		border-radius: 12rpx;
		font-size: 19rpx;
		line-height: 1.45;
	}

	.order-tabs {
		width: 100%;
		padding: 0 24rpx;
		background: var(--proto-surface);
		white-space: nowrap;
	}

	.order-tabs text {
		display: inline-block;
		margin-right: 30rpx;
		padding: 20rpx 0 16rpx;
		color: var(--proto-muted);
		border-bottom: 5rpx solid transparent;
		font-size: 22rpx;
	}

	.order-tabs text.active {
		color: var(--proto-primary-dark);
		border-bottom-color: var(--proto-primary);
		font-weight: 750;
	}


	.order-card {
		padding: 20rpx;
		border: 1rpx solid rgba(197, 199, 202, 0.24);
	}

	.order-card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		min-width: 0;
	}

	.order-store {
		display: flex;
		align-items: center;
		flex: 1;
		overflow: hidden;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.order-store .proto-icon {
		flex: 0 0 auto;
		margin-right: 6rpx;
	}

	.order-store .proto-icon:last-child {
		margin-right: 0;
		margin-left: 4rpx;
	}

	.order-card-heading .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}

	.order-number {
		display: flex;
		align-items: center;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.order-number .proto-icon {
		margin-left: 5rpx;
	}

	.order-content {
		display: flex !important;
		flex-direction: row;
		align-items: flex-start;
		width: 100%;
		gap: 16rpx;
		margin-top: 16rpx;
		padding: 16rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.order-content image {
		display: block;
		flex: 0 0 150rpx;
		width: 150rpx !important;
		min-width: 150rpx;
		max-width: 150rpx;
		height: 150rpx !important;
		border-radius: 12rpx;
	}

	.order-content > view {
		flex: 1 1 auto;
		width: 0;
		min-width: 0;
	}

	.order-room,
	.order-room-desc,
	.order-date,
	.order-guest {
		display: block;
	}

	.order-room {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.order-room-desc {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.35;
	}

	.order-date,
	.order-guest {
		display: flex;
		align-items: center;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.order-date .proto-icon,
	.order-guest .proto-icon {
		flex: 0 0 auto;
		margin-right: 5rpx;
	}
	.order-price-row {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin-top: 16rpx;
	}

	.order-note {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.order-price {
		color: var(--proto-text);
		font-size: 38rpx;
		font-weight: 850;
	}

	.order-price .small {
		font-size: 18rpx;
	}

	.order-actions {
		display: flex !important;
		flex-direction: row;
		flex-wrap: nowrap;
		justify-content: flex-end;
		gap: 12rpx;
		margin-top: 16rpx;
	}

	.order-actions button {
		width: auto !important;
		min-width: 0;
		flex: 0 0 auto;
		height: 64rpx;
		margin: 0;
		font-size: 20rpx;
	}

	.order-actions .proto-secondary-button,
	.order-actions .proto-ghost-button {
		padding: 0 20rpx;
		border-radius: 32rpx;
	}

	.order-actions .proto-secondary-button {
		color: var(--proto-muted);
		border-color: #e1e8e9;
	}

	.order-footer-note {
		padding: 30rpx 0 48rpx;
		color: var(--proto-primary-dark);
		font-size: 18rpx;
		text-align: center;
	}

	.order-footer-note .small {
		display: block;
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}
</style>

<style lang="scss" scoped>
	.order-list-page {
		padding-bottom: 156rpx;
		background: #ffffff;
	}

	.order-list-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.order-search-box {
		display: flex;
		align-items: center;
		margin: 18rpx 32rpx 20rpx;
		padding: 0 22rpx;
		height: 86rpx;
		background: #f5f7fa;
		border: 1rpx solid #e1e7ee;
		border-radius: 43rpx;
	}

	.order-search-box .proto-icon {
		flex: 0 0 auto;
		margin-right: 12rpx;
	}

	.order-search-box input {
		width: 100%;
		min-width: 0;
		height: 74rpx;
		color: #27313a;
		font-size: 25rpx;
	}

	.order-search-placeholder {
		color: #9aa5b1;
	}

	.order-list-page .order-tabs {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		width: 100%;
		box-sizing: border-box;
		gap: 0;
		padding: 0 24rpx;
		background: #ffffff;
		white-space: nowrap;
		overflow: hidden;
	}

	.order-list-page .order-tabs text {
		display: block;
		flex: 1 1 0;
		min-width: 0;
		margin: 0;
		box-sizing: border-box;
		padding: 22rpx 2rpx 20rpx;
		color: #737d86;
		border-bottom: 6rpx solid transparent;
		font-size: 22rpx;
		line-height: 1.2;
		text-align: center;
		white-space: nowrap;
	}

	.order-list-page .order-tabs text.active {
		color: #1f2831;
		border-bottom-color: var(--proto-primary);
		font-weight: 800;
	}

	.order-summary-strip {
		display: flex;
		align-items: center;
		margin: 12rpx 24rpx 0;
		padding: 18rpx 20rpx;
		background: #eef9fb;
		border: 1rpx solid #d7eef1;
		border-radius: 16rpx;
	}

	.order-summary-item {
		display: flex;
		align-items: baseline;
		gap: 7rpx;
		padding-right: 22rpx;
		border-right: 1rpx solid #cde4e7;
	}

	.summary-value {
		color: var(--proto-primary-dark);
		font-size: 29rpx;
		font-weight: 850;
	}

	.summary-label,
	.order-summary-sync {
		color: #73818b;
		font-size: 19rpx;
	}

	.order-summary-sync {
		display: flex;
		align-items: center;
		gap: 6rpx;
		margin-left: auto;
		color: var(--proto-primary-dark);
	}

	.order-section-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 24rpx 32rpx 4rpx;
		color: #1f2932;
		font-size: 25rpx;
		font-weight: 800;
	}

	.order-section-heading > text:last-child {
		color: #87939d;
		font-size: 19rpx;
		font-weight: 400;
	}

	.order-list-page .order-list {
		padding-top: 2rpx;
		background: #f6fbfc;
	}

	.order-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 170rpx 30rpx 80rpx;
		text-align: center;
	}

	.order-empty-illustration {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 180rpx;
		height: 180rpx;
		color: #8d99a4;
	}

	.order-empty-search {
		position: absolute;
		right: 6rpx;
		bottom: 22rpx;
		color: var(--proto-primary);
	}

	.order-empty-title {
		margin-top: 28rpx;
		color: #7b8792;
		font-size: 28rpx;
	}

	.order-empty-desc {
		margin-top: 10rpx;
		color: #a1abb4;
		font-size: 21rpx;
	}

	.order-empty-button {
		margin-top: 34rpx;
		padding: 0 36rpx;
		height: 66rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 33rpx;
		font-size: 22rpx;
	}

	.order-list-page .order-card {
		margin: 18rpx 24rpx;
		padding: 22rpx;
		border-color: #e2e8ee;
		border-radius: 20rpx;
		box-shadow: 0 7rpx 18rpx rgba(21, 73, 112, 0.05);
	}

	.order-list-page .order-card-heading,
	.order-list-page .order-number,
	.order-list-page .order-price-row,
	.order-list-page .order-actions {
		min-width: 0;
	}

	.order-list-page .order-card-heading .proto-pill {
		color: var(--proto-primary-dark);
		background: #e4f7f8;
	}

	.order-list-page .order-content {
		margin-top: 20rpx;
		padding: 18rpx;
		background: #f3fafb;
		border: 1rpx solid #e5f0f2;
	}

	.order-list-page .order-price-row {
		margin-top: 20rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #edf0f1;
	}

	.order-list-page .order-actions .proto-secondary-button,
	.order-list-page .order-actions .proto-ghost-button {
		color: var(--proto-primary-dark);
		background: #effafa;
		border: 1rpx solid #b8e7e7;
	}

	.order-list-page .order-footer-note {
		padding: 20rpx 0 44rpx;
		color: #8a949d;
		font-size: 18rpx;
		text-align: center;
	}

	.order-login-panel {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 180rpx 64rpx 80rpx;
		text-align: center;
	}

	.order-login-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 168rpx;
		height: 168rpx;
		color: var(--proto-primary-deep);
		background: #eef9fa;
		border: 1rpx solid #d7eef1;
		border-radius: 50%;
	}

	.order-login-title {
		margin-top: 32rpx;
		color: #1f2932;
		font-size: 32rpx;
		font-weight: 800;
	}

	.order-login-desc {
		margin-top: 14rpx;
		color: #87939d;
		font-size: 22rpx;
		line-height: 1.6;
	}

	.order-login-button {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 10rpx;
		width: 424rpx;
		height: 84rpx;
		margin-top: 48rpx;
		color: #ffffff;
		background: #2aa9a9;
		border-radius: 42rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.24);
		font-size: 26rpx;
		font-weight: 700;
	}

	.order-login-button::after {
		border: 0;
	}
</style>
