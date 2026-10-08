<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="核销记录" theme="green" />
		<view class="records-summary proto-card"><view><text class="records-summary-value">{{ records.length }}</text><text>条核销记录</text></view><text>仅展示当前民宿</text></view>
		<ProtoEmptyState v-if="!records.length" title="暂无核销记录" description="扫码核验住客订单后，记录会显示在这里" />
		<view v-else class="record-list">
			<view v-for="record in records" :key="record.id" class="record-card proto-card">
				<view class="record-card-heading"><view><ProtoIcon name="check-circle" :size="26" /><text>{{ record.status }}</text></view><text>{{ record.time }}</text></view>
				<view class="record-card-body"><text>{{ record.roomName }}</text><text>入住人：{{ record.guest }}</text><text>凭证号：{{ record.code }}</text></view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { getMerchantVerifyRecords } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() { return { records: [] } },
		onShow() { this.records = getMerchantVerifyRecords() }
	}
</script>

<style lang="scss" scoped>
	.merchant-tool-page { padding-bottom: 48rpx; background: #f4fafb; }
	.merchant-tool-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}
	.records-summary { display: flex; align-items: center; gap: 16rpx; justify-content: space-between; padding: 22rpx; }
	.records-summary > view { display: flex; align-items: baseline; gap: 8rpx; color: var(--proto-muted); font-size: 19rpx; }
	.records-summary-value { color: var(--proto-primary-dark); font-size: 38rpx; font-weight: 800; }
	.records-summary > text { flex: 0 0 auto; color: var(--proto-muted); font-size: 17rpx; white-space: nowrap; }
	.record-list { padding-bottom: 20rpx; }
	.record-card { padding: 20rpx; }
	.record-card-heading, .record-card-heading > view { display: flex; align-items: center; }
	.record-card-heading { justify-content: space-between; gap: 12rpx; color: var(--proto-muted); font-size: 17rpx; }
	.record-card-heading > view { gap: 8rpx; color: var(--proto-primary-dark); font-size: 20rpx; font-weight: 700; }
	.record-card-body { display: flex; flex-direction: column; margin-top: 18rpx; gap: 8rpx; }
	.record-card-body > text:first-child { overflow: hidden; color: var(--proto-text); font-size: 24rpx; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
	.record-card-body > text:not(:first-child) { overflow: hidden; color: var(--proto-muted); font-size: 18rpx; text-overflow: ellipsis; white-space: nowrap; }
	.merchant-tool-page :deep(.proto-empty-state) { padding-top: 76rpx; padding-bottom: 92rpx; }
	.merchant-tool-page :deep(.proto-empty-state-image) { width: 210rpx; height: 176rpx; }
</style>
