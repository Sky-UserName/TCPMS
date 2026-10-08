<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="扫码核销" theme="green" />
		<view class="tool-hero proto-card">
			<view class="tool-hero-icon"><ProtoIcon name="qr" :size="54" /></view>
			<text class="tool-hero-title">核验住客入住凭证</text>
			<text class="tool-hero-desc">扫描订单二维码，或输入订单号进行本地演示核验。</text>
		</view>
		<view class="verify-panel proto-card">
			<button class="proto-primary-button verify-scan-button" hover-class="none" @tap="scan"><ProtoIcon name="qr" tone="white" :size="28" />扫描二维码</button>
			<view class="verify-divider"><text>或输入订单号</text></view>
			<view class="verify-input-row"><input v-model="code" class="proto-input" placeholder="例如 M202610060001" placeholder-class="tool-placeholder" /><button class="proto-secondary-button" hover-class="none" @tap="verifyCode">核验</button></view>
		</view>
		<view class="tool-section-heading"><text>最近核销</text><text @tap="goRecords">查看记录 <ProtoIcon name="chevron-right" :size="18" /></text></view>
		<ProtoEmptyState v-if="!records.length" title="暂无核销记录" description="完成首笔核验后，记录会显示在这里" />
		<view v-else class="record-list">
			<view v-for="record in records.slice(0, 3)" :key="record.id" class="record-item proto-card">
				<view class="record-icon"><ProtoIcon name="check-circle" :size="30" /></view>
				<view class="record-copy"><text>{{ record.roomName }}</text><text>{{ record.guest }} · {{ record.time }}</text></view>
				<text class="record-status">{{ record.status }}</text>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { addMerchantVerifyRecord, getMerchantOrders, getMerchantVerifyRecords } from '@/common/app-store.js'
	import { goPage, showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return { code: '', records: [] }
		},
		onShow() {
			this.records = getMerchantVerifyRecords()
		},
		methods: {
			scan() {
				if (!uni.scanCode) {
					showToast('当前预览环境暂不支持扫码，请输入订单号')
					return
				}
				uni.scanCode({
					success: ({ result = '' }) => {
						this.code = result
						this.verifyCode()
					},
					fail: () => showToast('未识别到有效二维码')
				})
			},
			verifyCode() {
				const code = this.code.trim()
				if (!code) {
					showToast('请输入订单号')
					return
				}
				const order = getMerchantOrders().find((item) => String(item.id).toLowerCase() === code.toLowerCase())
				const record = addMerchantVerifyRecord({
					code,
					guest: order ? order.guest : '演示住客',
					roomName: order ? order.roomName : '演示房间'
				})
				this.records = getMerchantVerifyRecords()
				this.code = ''
				showToast(`核验成功：${record.roomName}`)
			},
			goRecords() {
				goPage('/pages/merchant/records')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-tool-page {
		padding-bottom: 48rpx;
		background: #f4fafb;
	}

	.merchant-tool-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.tool-hero {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 34rpx 28rpx;
		text-align: center;
	}

	.tool-hero-icon,
	.record-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 22rpx;
	}

	.tool-hero-icon { width: 96rpx; height: 96rpx; }
	.tool-hero-title { margin-top: 18rpx; color: var(--proto-text); font-size: 28rpx; font-weight: 800; }
	.tool-hero-desc { max-width: 580rpx; margin-top: 10rpx; color: var(--proto-muted); font-size: 19rpx; line-height: 1.5; }

	.verify-panel { padding: 24rpx; }
	.verify-scan-button { gap: 8rpx; margin: 0; height: 78rpx; }
	.verify-divider { display: flex; align-items: center; gap: 18rpx; margin: 24rpx 0; color: var(--proto-muted); font-size: 18rpx; }
	.verify-divider::before, .verify-divider::after { flex: 1; height: 1rpx; background: #e5edef; content: ''; }
	.verify-input-row { display: flex; align-items: center; gap: 12rpx; }
	.verify-input-row input { flex: 1; min-width: 0; }
	.verify-input-row button { flex: 0 0 130rpx; min-width: 130rpx; height: 76rpx; margin: 0; padding: 0; font-size: 21rpx; white-space: nowrap; }
	.tool-placeholder { color: #9ba7ad; }

	.tool-section-heading { display: flex; align-items: center; justify-content: space-between; margin: 26rpx 24rpx 12rpx; color: var(--proto-text); font-size: 27rpx; font-weight: 800; }
	.tool-section-heading > text:last-child { display: flex; align-items: center; color: var(--proto-primary-dark); font-size: 18rpx; font-weight: 500; white-space: nowrap; }
	.record-list { padding-bottom: 20rpx; }
	.record-item { display: flex; align-items: center; gap: 14rpx; padding: 18rpx; }
	.record-icon { width: 60rpx; height: 60rpx; flex: 0 0 60rpx; }
	.record-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.record-copy > text:first-child { overflow: hidden; color: var(--proto-text); font-size: 22rpx; font-weight: 700; text-overflow: ellipsis; white-space: nowrap; }
	.record-copy > text:last-child { margin-top: 6rpx; color: var(--proto-muted); font-size: 17rpx; }
	.record-status { flex: 0 0 auto; color: var(--proto-primary-dark); font-size: 18rpx; white-space: nowrap; }
	.merchant-tool-page :deep(.proto-empty-state) { padding-top: 76rpx; padding-bottom: 92rpx; }
	.merchant-tool-page :deep(.proto-empty-state-image) { width: 210rpx; height: 176rpx; }
</style>
