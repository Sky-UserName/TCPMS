<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="优惠券管理" theme="green" />
		<view class="coupon-banner proto-card"><view><text>优惠活动</text><text>用优惠券提升房源转化</text></view><button class="proto-primary-button" hover-class="none" @tap="addCoupon">新建优惠券</button></view>
		<scroll-view class="coupon-tabs" scroll-x :show-scrollbar="false"><text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text></scroll-view>
		<ProtoEmptyState v-if="!filteredCoupons.length" title="暂无优惠券" description="创建优惠券后，可在这里查看发放和使用情况" />
		<view v-else class="coupon-list">
			<view v-for="coupon in filteredCoupons" :key="coupon.id" class="coupon-card proto-card"><view class="coupon-value"><text>¥{{ coupon.amount }}</text><text>{{ coupon.threshold }}</text></view><view class="coupon-copy"><text>{{ coupon.name }}</text><text>已使用 {{ coupon.used }} / {{ coupon.stock }} 张</text><text class="coupon-status">{{ coupon.status }}</text></view><button class="coupon-toggle" hover-class="none" @tap="toggleCoupon(coupon)">{{ coupon.status === '生效中' ? '停用' : '启用' }}</button></view>
		</view>
		<view class="tool-note"><ProtoIcon name="info" :size="21" /><text>优惠券核销与发放规则将在订单服务接入后同步到住客端。</text></view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { addMerchantCoupon, getMerchantCoupons, updateMerchantCoupon } from '@/common/app-store.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() { return { coupons: [], activeTab: 'all', tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '生效中' }, { key: 'ended', label: '已结束' }] } },
		computed: {
			filteredCoupons() {
				if (this.activeTab === 'active') return this.coupons.filter((coupon) => coupon.status === '生效中')
				if (this.activeTab === 'ended') return this.coupons.filter((coupon) => coupon.status !== '生效中')
				return this.coupons
			}
		},
		onShow() { this.coupons = getMerchantCoupons() },
		methods: {
			addCoupon() {
				const coupon = addMerchantCoupon({ name: `演示优惠券${this.coupons.length + 1}` })
				this.coupons = getMerchantCoupons()
				showToast(`${coupon.name}已创建`)
			},
			toggleCoupon(coupon) {
				const nextStatus = coupon.status === '生效中' ? '已停用' : '生效中'
				updateMerchantCoupon(coupon.id, { status: nextStatus })
				this.coupons = getMerchantCoupons()
				showToast(nextStatus === '生效中' ? '优惠券已启用' : '优惠券已停用')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-tool-page { padding-bottom: 48rpx; background: #f4fafb; }
	.merchant-tool-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}
	.coupon-banner { display: flex; align-items: center; gap: 18rpx; justify-content: space-between; padding: 24rpx; background: var(--proto-primary-deep); }
	.coupon-banner > view { display: flex; flex: 1; min-width: 0; flex-direction: column; color: #ffffff; }
	.coupon-banner > view > text:first-child { font-size: 28rpx; font-weight: 800; }
	.coupon-banner > view > text:last-child { max-width: 100%; margin-top: 7rpx; overflow: hidden; color: rgba(255,255,255,.78); font-size: 17rpx; text-overflow: ellipsis; white-space: nowrap; }
	.coupon-banner button { flex: 0 0 150rpx; min-width: 150rpx; height: 60rpx; margin: 0; color: var(--proto-primary-dark); background: #ffffff; box-shadow: none; font-size: 19rpx; white-space: nowrap; }
	.coupon-tabs { padding: 4rpx 24rpx 0; background: #ffffff; white-space: nowrap; }
	.coupon-tabs text { display: inline-flex; align-items: center; justify-content: center; min-width: 110rpx; height: 66rpx; margin-right: 14rpx; color: var(--proto-muted); border-bottom: 5rpx solid transparent; font-size: 21rpx; }
	.coupon-tabs text.active { color: var(--proto-primary-dark); border-bottom-color: var(--proto-primary); font-weight: 750; }
	.coupon-card { display: flex; align-items: center; gap: 16rpx; padding: 18rpx; }
	.coupon-value { display: flex; align-items: center; flex-direction: column; justify-content: center; width: 126rpx; height: 102rpx; flex: 0 0 126rpx; color: var(--proto-primary-dark); background: #e8f8f8; border-radius: 16rpx; }
	.coupon-value text:first-child { font-size: 31rpx; font-weight: 850; }
	.coupon-value text:last-child { margin-top: 5rpx; color: var(--proto-muted); font-size: 15rpx; }
	.coupon-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.coupon-copy > text:first-child { overflow: hidden; color: var(--proto-text); font-size: 22rpx; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
	.coupon-copy > text:nth-child(2) { margin-top: 7rpx; color: var(--proto-muted); font-size: 17rpx; }
	.coupon-status { margin-top: 6rpx; color: var(--proto-primary-dark); font-size: 17rpx; }
	.coupon-toggle { display: flex; align-items: center; justify-content: center; flex: 0 0 92rpx; min-width: 92rpx; height: 52rpx; padding: 0 10rpx; color: var(--proto-primary-dark); background: #edf8f8; border-radius: 26rpx; font-size: 17rpx; white-space: nowrap; }
	.tool-note { display: flex; align-items: flex-start; gap: 8rpx; margin: 22rpx 30rpx; color: var(--proto-muted); font-size: 17rpx; line-height: 1.5; }
	.merchant-tool-page :deep(.proto-empty-state) { padding-top: 76rpx; padding-bottom: 92rpx; }
	.merchant-tool-page :deep(.proto-empty-state-image) { width: 210rpx; height: 176rpx; }
</style>
