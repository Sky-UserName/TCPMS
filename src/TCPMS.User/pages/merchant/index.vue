<template>
	<view class="proto-page merchant-page">
		<ProtoHeader title="民宿管理" title-align="center" theme="green" />

		<view class="merchant-property proto-card">
			<view class="merchant-property-main">
				<view class="merchant-property-mark"><ProtoIcon name="house" :size="34" /></view>
				<view class="merchant-property-copy">
					<text class="merchant-property-name">天空之蓝</text>
					<text class="merchant-property-sub">已入驻 · 资料状态正常</text>
				</view>
			</view>
			<button class="merchant-property-switch" hover-class="none" @tap="switchProperty">
				<text>切换门店</text><ProtoIcon name="chevron-down" :size="20" />
			</button>
		</view>

		<view class="merchant-highlight">
			<view><text class="merchant-highlight-label">今日待处理</text><text class="merchant-highlight-value">{{ pendingCount }}</text><text class="merchant-highlight-caption">项运营任务</text></view>
			<view class="merchant-highlight-action" @tap="goOrders"><text>查看订单</text><ProtoIcon name="chevron-right" :size="22" /></view>
		</view>

		<view class="merchant-section-title"><text>运营管理</text><text>高效管理房源与订单</text></view>
		<view class="merchant-module-grid proto-card">
			<view v-for="item in modules" :key="item.key" class="merchant-module" @tap="openModule(item)">
				<view class="merchant-module-icon"><ProtoIcon :name="item.icon" :size="36" /></view>
				<text class="merchant-module-title">{{ item.title }}</text>
				<text class="merchant-module-caption">{{ item.caption }}</text>
			</view>
		</view>

		<view class="merchant-tip">
			<ProtoIcon name="info" :size="24" />
			<text>商家资料、房间信息和订单数据将在服务端接入后实时同步。</text>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, showToast } from '@/common/prototype.js'
	import { getMerchantOrders, getMerchantRooms } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				pendingCount: 0,
				modules: [
					{ key: 'finance', title: '财务统计', caption: '收入与提现', icon: 'wallet', route: '/pages/merchant/finance' },
					{ key: 'rooms', title: '房间管理', caption: '发布与上下架', icon: 'house', route: '/pages/merchant/rooms' },
					{ key: 'orders', title: '订单管理', caption: '处理入住订单', icon: 'receipt', route: '/pages/merchant/orders' },
					{ key: 'verify', title: '扫码核销', caption: '核验入住凭证', icon: 'qr', route: '/pages/merchant/verify' },
					{ key: 'records', title: '核销记录', caption: '查看历史核销', icon: 'document', route: '/pages/merchant/records' },
					{ key: 'staff', title: '员工管理', caption: '协同运营成员', icon: 'group', route: '/pages/merchant/staff' },
					{ key: 'publish', title: '发布房源', caption: '创建新房型', icon: 'plus', route: '/pages/merchant/publish' },
					{ key: 'settings', title: '民宿设置', caption: '基础资料与规则', icon: 'settings', route: '/pages/merchant/settings' },
					{ key: 'reviews', title: '评价管理', caption: '维护住客口碑', icon: 'star', route: '/pages/merchant/reviews' },
					{ key: 'coupon', title: '优惠券管理', caption: '配置优惠活动', icon: 'document', route: '/pages/merchant/coupons' },
					{ key: 'messages', title: '订阅消息', caption: '运营通知管理', icon: 'support', route: '/pages/merchant/messages' }
				]
			}
		},
		onShow() {
			const orders = getMerchantOrders()
			const rooms = getMerchantRooms()
			this.pendingCount = orders.filter((order) => ['pending', 'stay'].includes(order.statusKey)).length + rooms.filter((room) => room.status === '草稿' || room.status === '待审核').length
		},
		methods: {
			openModule(item) {
				if (item.route) {
					goPage(item.route)
					return
				}
				showToast('该功能将在商家服务接入后开放')
			},
			goOrders() {
				goPage('/pages/merchant/orders')
			},
			switchProperty() {
				showToast('当前仅有 1 个已入驻民宿')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-page {
		padding-bottom: 48rpx;
		background: #f4fafb;
	}

	.merchant-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.merchant-property {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16rpx;
		margin-top: 20rpx;
		padding: 24rpx;
	}

	.merchant-property-main {
		display: flex;
		align-items: center;
		min-width: 0;
	}

	.merchant-property-mark,
	.merchant-module-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 18rpx;
	}

	.merchant-property-mark {
		width: 76rpx;
		height: 76rpx;
		margin-right: 16rpx;
	}

	.merchant-property-copy {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.merchant-property-name {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 31rpx;
		font-weight: 800;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-property-sub {
		overflow: hidden;
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-property-switch {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 4rpx;
		flex: 0 0 auto;
		min-width: 138rpx;
		height: 58rpx;
		padding: 0 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 29rpx;
		font-size: 20rpx;
		white-space: nowrap;
	}

	.merchant-highlight {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin: 16rpx 24rpx 24rpx;
		padding: 24rpx 26rpx;
		color: #ffffff;
		background: var(--proto-primary-deep);
		border-radius: 24rpx;
		box-shadow: 0 12rpx 26rpx rgba(24, 133, 143, 0.18);
	}

	.merchant-highlight > view:first-child {
		display: flex;
		align-items: baseline;
		flex-wrap: wrap;
		column-gap: 10rpx;
	}

	.merchant-highlight-label,
	.merchant-highlight-caption {
		width: 100%;
		color: rgba(255, 255, 255, 0.82);
		font-size: 19rpx;
	}

	.merchant-highlight-value {
		margin-top: 6rpx;
		font-size: 48rpx;
		font-weight: 800;
	}

	.merchant-highlight-caption {
		width: auto;
	}

	.merchant-highlight-action {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		padding: 14rpx 18rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border-radius: 30rpx;
		font-size: 20rpx;
		font-weight: 700;
		white-space: nowrap;
	}

	.merchant-section-title {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 16rpx;
		margin: 8rpx 24rpx 14rpx;
	}

	.merchant-section-title > text:first-child {
		color: var(--proto-text);
		font-size: 29rpx;
		font-weight: 800;
	}

	.merchant-section-title > text:last-child {
		flex: 0 0 auto;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-align: right;
	}

	.merchant-module-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 24rpx 0;
		padding: 26rpx 14rpx;
	}

	.merchant-module {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: 33.3333%;
		min-height: 132rpx;
		padding: 0 8rpx;
		text-align: center;
	}

	.merchant-module-icon {
		width: 66rpx;
		height: 66rpx;
		border-radius: 20rpx;
	}

	.merchant-module-title {
		max-width: 190rpx;
		overflow: hidden;
		margin-top: 12rpx;
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 700;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-module-caption {
		max-width: 190rpx;
		overflow: hidden;
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 16rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-tip {
		display: flex;
		align-items: flex-start;
		gap: 8rpx;
		margin: 18rpx 30rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.5;
	}

	.merchant-tip .proto-icon {
		flex: 0 0 auto;
		margin-top: 2rpx;
	}
</style>
