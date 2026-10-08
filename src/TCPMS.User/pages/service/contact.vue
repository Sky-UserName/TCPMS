<template>
	<view class="proto-page proto-page-plain service-page">
		<ProtoHeader title="联系客服" theme="green" />

		<view class="service-intro">
			<text class="service-kicker">TCPMS 智选旅宿</text>
			<text class="service-title">需要帮助？我们随时在这里</text>
			<text class="service-desc">预订、改期、入住和退款问题，都可以从官方服务入口获得帮助。</text>
		</view>

		<view class="official-service proto-card">
			<view class="official-mark"><ProtoIcon name="support" tone="white" :size="38" /></view>
			<view class="official-copy">
				<text class="service-card-title">官方微信客服</text>
				<text class="service-card-desc">工作日 09:00 - 22:00 在线，订单问题会优先处理</text>
				<view class="service-tags">
					<text class="proto-pill info">订单变更</text>
					<text class="proto-pill info">入住咨询</text>
					<text class="proto-pill info">退款进度</text>
				</view>
			</view>
			<button class="proto-button-small" hover-class="none" @tap="openWechatService">进入客服</button>
		</view>

		<view class="proto-section-title">
			<text>门店服务</text>
			<text class="proto-muted proto-small">优先联系对应门店</text>
		</view>
		<view class="store-service proto-card">
			<view class="store-service-head">
				<view>
					<text class="store-name">{{ store.name }}</text>
					<text class="store-status">营业中 · 前台 24 小时值守</text>
				</view>
				<text class="proto-pill success">{{ store.status }}</text>
			</view>
			<view class="service-line">
				<view class="service-line-icon"><ProtoIcon name="phone" :size="25" /></view>
				<view>
					<text class="service-line-title">门店前台专线</text>
					<text class="service-line-desc">{{ store.phone || '暂无公开电话' }}</text>
				</view>
				<button class="proto-button-small proto-button-light" hover-class="none" @tap="callStore">拨打</button>
			</view>
			<view class="service-line">
				<view class="service-line-icon"><ProtoIcon name="location" :size="25" /></view>
				<view>
					<text class="service-line-title">门店地址</text>
					<text class="service-line-desc">{{ store.address }}</text>
				</view>
				<button class="proto-button-small proto-button-light" hover-class="none" @tap="goMap">地图</button>
			</view>
		</view>

		<view class="proto-section-title">
			<text>关联订单</text>
			<text class="proto-muted proto-small">让客服更快定位问题</text>
		</view>
		<view class="order-link-card proto-card" @tap="goOrder">
			<image :src="order.image" mode="aspectFill"></image>
			<view class="order-link-main">
				<text class="order-link-title">{{ order.room }}</text>
				<text class="order-link-sub">{{ order.id }} · {{ order.date }}</text>
				<text class="order-link-status">{{ order.status }} · ¥{{ order.price }}</text>
			</view>
			<ProtoIcon class="order-link-arrow" name="chevron-right" :size="30" />
		</view>

		<view class="service-notice proto-card">
			<text class="service-notice-title">服务说明</text>
			<text class="service-notice-item">官方客服不会索要支付密码、短信验证码或完整身份证号码。</text>
			<text class="service-notice-item">退款、改期等操作以订单详情页展示的规则和状态为准。</text>
			<text class="service-notice-item">如需人工服务，请通过上方客服入口或门店前台电话联系。</text>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoStores, goPage, showToast } from '@/common/prototype.js'
	import { findOrder, getBooking, getOrders } from '@/common/app-store.js'
	import { fetchStoreDetail } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				store: demoStores[0],
				order: getOrders()[0],
				storeId: getBooking().storeId || ''
			}
		},
		async onLoad(options) {
			if (options && options.orderId) {
				const order = findOrder(options.orderId)
				if (order) {
					this.order = order
					this.storeId = order.storeId || this.storeId
				}
			}
			const booking = getBooking()
			this.storeId = this.storeId || booking.storeId
			const localStore = demoStores.find((item) => String(item.id) === String(this.storeId))
			if (localStore) this.store = localStore
			if (this.storeId && !/^\d+$/.test(String(this.storeId))) {
				try {
					const result = await fetchStoreDetail(this.storeId)
					if (result.store) this.store = result.store
				} catch (error) {
					// Keep the bundled support contact available when the API is offline.
				}
			}
		},
		methods: {
			openWechatService() {
				uni.setClipboardData({
					data: 'TCPMS 官方客服：请在微信中搜索“TCPMS智选旅宿”进入客服会话。',
					success: () => showToast('客服信息已复制')
				})
			},
			callStore() {
				uni.makePhoneCall({
					phoneNumber: this.store.phone || '037188886622',
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			goMap() {
				goPage(`/pages/store/map?id=${this.store.id}`)
			},
			goOrder() {
				goPage(`/pages/order/detail?id=${this.order.id}`)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.service-page {
		padding-bottom: 40rpx;
	}

	.service-intro {
		padding: 30rpx 28rpx 18rpx;
	}

	.service-kicker,
	.service-title,
	.service-desc {
		display: block;
	}

	.service-kicker {
		color: var(--proto-primary-dark);
		font-size: 21rpx;
		font-weight: 750;
		letter-spacing: 2rpx;
	}

	.service-title {
		margin-top: 10rpx;
		color: var(--proto-text);
		font-size: 38rpx;
		font-weight: 850;
	}

	.service-desc {
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
		line-height: 1.55;
	}

	.official-service {
		display: flex;
		align-items: center;
		gap: 16rpx;
		padding: 24rpx;
		background: linear-gradient(135deg, #e0f5f3, #ffffff);
	}

	.official-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 76rpx;
		height: 76rpx;
		flex: 0 0 76rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 24rpx;
	}

	.official-copy {
		flex: 1;
		min-width: 0;
	}

	.service-card-title,
	.service-card-desc {
		display: block;
	}

	.service-card-title {
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 800;
	}

	.service-card-desc {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.45;
	}

	.service-tags {
		display: flex;
		gap: 8rpx;
		margin-top: 10rpx;
	}

	.service-tags .proto-pill {
		min-height: 30rpx;
		padding: 0 10rpx;
		font-size: 16rpx;
	}

	.official-service .proto-button-small {
		flex: 0 0 auto;
		height: 58rpx;
		padding: 0 16rpx;
		font-size: 18rpx;
	}

	.store-service {
		padding: 16rpx 24rpx;
	}

	.store-service-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		padding: 8rpx 0 16rpx;
	}

	.store-name,
	.store-status {
		display: block;
	}

	.store-name {
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 800;
	}

	.store-status {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.store-service-head .proto-pill {
		min-height: 34rpx;
		padding: 0 12rpx;
		font-size: 17rpx;
	}

	.service-line {
		display: flex;
		align-items: center;
		gap: 14rpx;
		padding: 18rpx 0;
		border-top: 1rpx solid #e3ebec;
	}

	.service-line-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 50rpx;
		height: 50rpx;
		flex: 0 0 50rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.service-line > view:not(.service-line-icon) {
		flex: 1;
		min-width: 0;
	}

	.service-line-title,
	.service-line-desc {
		display: block;
	}

	.service-line-title {
		color: var(--proto-text);
		font-size: 22rpx;
		font-weight: 700;
	}

	.service-line-desc {
		margin-top: 5rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.service-line .proto-button-small {
		height: 54rpx;
		padding: 0 18rpx;
		font-size: 18rpx;
	}

	.order-link-card {
		display: flex;
		align-items: center;
		gap: 16rpx;
		padding: 18rpx;
	}

	.order-link-card image {
		width: 118rpx;
		height: 118rpx;
		flex: 0 0 118rpx;
		border-radius: 16rpx;
	}

	.order-link-main {
		flex: 1;
		min-width: 0;
	}

	.order-link-title,
	.order-link-sub,
	.order-link-status {
		display: block;
	}

	.order-link-title {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.order-link-sub,
	.order-link-status {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.order-link-status {
		color: var(--proto-primary-dark);
	}

	.order-link-arrow {
		display: block;
		color: var(--proto-muted);
	}

	.service-notice {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		padding: 22rpx 24rpx;
	}

	.service-notice-title {
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
	}

	.service-notice-item {
		position: relative;
		padding-left: 22rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.5;
	}

	.service-notice-item::before {
		position: absolute;
		top: 12rpx;
		left: 3rpx;
		width: 8rpx;
		height: 8rpx;
		background: var(--proto-primary);
		border-radius: 50%;
		content: "";
	}
</style>
