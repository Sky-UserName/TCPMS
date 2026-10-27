<template>
	<view class="proto-page proto-page-plain service-page">
		<ProtoHeader title="联系客服" theme="green" />

		<view class="service-intro">
			<text class="service-kicker">{{ page.kicker }}</text>
			<text class="service-title">{{ page.title }}</text>
			<text class="service-desc">{{ page.summary }}</text>
		</view>

		<view class="official-service proto-card">
			<view class="official-mark"><ProtoIcon name="support" tone="white" :size="38" /></view>
			<view class="official-copy">
				<text class="service-card-title">{{ page.badge }}</text>
				<text class="service-card-desc">{{ page.serviceHours }}</text>
				<view v-if="page.tags.length" class="service-tags">
					<text v-for="tag in page.tags" :key="tag" class="proto-pill info">{{ tag }}</text>
				</view>
			</view>
			<button class="proto-button-small" hover-class="none" @tap="openWechatService">进入客服</button>
		</view>

		<block v-if="store">
			<view class="proto-section-title">
				<text>门店服务</text>
				<text class="proto-muted proto-small">优先联系对应门店</text>
			</view>
			<view class="store-service proto-card">
				<view class="store-service-head">
					<view>
						<text class="store-name">{{ store.name }}</text>
						<text class="store-status">{{ store.serviceNote }}</text>
					</view>
					<text class="proto-pill success">{{ store.status || '营业中' }}</text>
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
						<text class="service-line-desc">{{ store.address || '暂未填写门店地址' }}</text>
					</view>
					<button class="proto-button-small proto-button-light" hover-class="none" @tap="goMap">地图</button>
				</view>
			</view>
		</block>

		<block v-if="order">
			<view class="proto-section-title">
				<text>关联订单</text>
				<text class="proto-muted proto-small">让客服更快定位问题</text>
			</view>
			<view class="order-link-card proto-card" @tap="goOrder">
				<image :src="order.image" mode="aspectFill"></image>
				<view class="order-link-main">
					<text class="order-link-title">{{ order.room }}</text>
					<text class="order-link-sub">{{ order.orderNumber || order.id }} · {{ order.date }}</text>
					<text class="order-link-status">{{ order.status }} · ¥{{ order.price }}</text>
				</view>
				<ProtoIcon class="order-link-arrow" name="chevron-right" :size="30" />
			</view>
		</block>

		<view v-if="page.notices.length" class="service-notice proto-card">
			<text class="service-notice-title">服务说明</text>
			<text v-for="notice in page.notices" :key="notice" class="service-notice-item">{{ notice }}</text>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, showToast } from '@/common/prototype.js'
	import { getBooking } from '@/common/app-store.js'
	import { fetchPageContent, fetchRemoteOrder, fetchRemoteOrders, fetchStoreDetail } from '@/common/api.js'

	const EMPTY_PAGE = {
		kicker: '',
		title: '',
		summary: '',
		badge: '',
		serviceHours: '',
		supportText: '',
		tags: [],
		notices: []
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				page: { ...EMPTY_PAGE },
				store: null,
				order: null,
				storeId: '',
				orderId: ''
			}
		},
		async onLoad(options) {
			this.orderId = (options && options.orderId) || ''
			this.storeId = (options && options.storeId) || getBooking().storeId || ''
			await Promise.all([this.loadPage(), this.loadOrder()])
			await this.loadStore()
		},
		methods: {
			async loadPage() {
				try {
					const data = await fetchPageContent('service-contact')
					this.page = {
						...EMPTY_PAGE,
						...data,
						tags: Array.isArray(data.tags) ? data.tags : [],
						notices: Array.isArray(data.notices) ? data.notices : []
					}
				} catch (error) {
					this.page = { ...EMPTY_PAGE }
				}
			},
			async loadOrder() {
				try {
					if (this.orderId) {
						this.order = await fetchRemoteOrder(this.orderId)
					} else {
						const orders = await fetchRemoteOrders()
						this.order = orders.length ? orders[0] : null
					}
				} catch (error) {
					this.order = null
				}
				if (this.order && this.order.storeId) {
					this.storeId = this.order.storeId
				}
			},
			async loadStore() {
				if (!this.storeId) return
				try {
					const result = await fetchStoreDetail(this.storeId)
					if (!result || !result.store) return
					const store = result.store
					this.store = {
						...store,
						serviceNote: store.description || '前台值守，可咨询入住与停车指引'
					}
				} catch (error) {
					this.store = null
				}
			},
			openWechatService() {
				if (!this.page.supportText) {
					showToast('客服信息加载中，请稍后再试')
					return
				}
				uni.setClipboardData({
					data: this.page.supportText,
					success: () => showToast('客服信息已复制')
				})
			},
			callStore() {
				if (!this.store || !this.store.phone) {
					showToast('该门店暂未公开联系电话')
					return
				}
				uni.makePhoneCall({
					phoneNumber: this.store.phone,
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			goMap() {
				if (!this.store) return
				goPage(`/pages/store/map?id=${this.store.id}`)
			},
			goOrder() {
				if (!this.order) return
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
