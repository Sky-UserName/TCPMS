<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="核销记录" theme="green" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />
		<view class="records-summary proto-card">
			<view><text class="records-summary-value">{{ records.length }}</text><text>条核销记录</text></view>
			<text>仅展示当前民宿</text>
		</view>
		<ProtoEmptyState v-if="!records.length" title="暂无核销记录" description="扫描住客订单二维码完成核销后，记录会显示在这里" />
		<view v-else class="record-list">
			<view v-for="record in records" :key="record.id" class="record-card proto-card">
				<view class="record-card-heading">
					<view class="record-status"><ProtoIcon name="check-circle" :size="26" /><text>{{ record.status || '已核销' }}</text></view>
					<text class="record-time">{{ record.time || '—' }}</text>
				</view>
				<view class="record-room-row">
					<text class="record-room">{{ record.roomName || '已预订房型' }}</text>
					<text class="record-store">{{ record.storeName || '当前民宿' }}</text>
				</view>
				<view class="record-fields">
					<view class="record-field">
						<text class="record-label">订单号</text>
						<text class="record-value record-order-number">{{ record.orderNumber || record.code || '—' }}</text>
					</view>
					<view class="record-field">
						<text class="record-label">房间</text>
						<text class="record-value">{{ record.roomName || '—' }}</text>
					</view>
					<view class="record-field">
						<text class="record-label">民宿</text>
						<text class="record-value">{{ record.storeName || '—' }}</text>
					</view>
					<view class="record-field">
						<text class="record-label">入住人</text>
						<text class="record-value">{{ record.guest || '未登记' }}</text>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import CacheRefreshIndicator from '@/components/CacheRefreshIndicator.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { fetchMerchantVerifyRecords } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon, ProtoEmptyState },
		data() {
			return { records: [], pullRefreshing: false }
		},
		async onShow() {
			await this.loadRecords()
		},
		onPullDownRefresh() {
			return runPullRefresh(this, () => this.loadRecords())
		},
		methods: {
			async loadRecords() {
				try {
					this.records = await fetchMerchantVerifyRecords()
				} catch (error) {
					showToast(error?.message || '核销记录加载失败')
				}
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

	.records-summary { display: flex; align-items: center; gap: 16rpx; justify-content: space-between; padding: 22rpx; }
	.records-summary > view { display: flex; align-items: baseline; gap: 8rpx; color: #6b7a80; font-size: 19rpx; }
	.records-summary-value { color: #1f7f7f; font-size: 38rpx; font-weight: 800; }
	.records-summary > text { flex: 0 0 auto; color: #6b7a80; font-size: 17rpx; white-space: nowrap; }

	.record-list { display: flex; flex-direction: column; padding-bottom: 20rpx; }
	.record-card { padding: 22rpx; }
	.record-card-heading { display: flex; align-items: center; justify-content: space-between; gap: 12rpx; }
	.record-status { display: flex; align-items: center; gap: 8rpx; color: #1f7f7f; font-size: 22rpx; font-weight: 750; }
	.record-time { flex: 0 0 auto; color: #6b7a80; font-size: 18rpx; }

	.record-room-row { display: flex; align-items: baseline; gap: 12rpx; margin-top: 18rpx; }
	.record-room { overflow: hidden; max-width: 420rpx; color: #20252b; font-size: 28rpx; font-weight: 800; text-overflow: ellipsis; white-space: nowrap; }
	.record-store { overflow: hidden; flex: 1; min-width: 0; color: #6b7a80; font-size: 19rpx; text-overflow: ellipsis; white-space: nowrap; }

	.record-fields { display: flex; flex-direction: column; gap: 10rpx; margin-top: 18rpx; padding-top: 18rpx; border-top: 1rpx dashed #dfe9ea; }
	.record-field { display: flex; align-items: baseline; gap: 14rpx; font-size: 19rpx; line-height: 1.5; }
	.record-label { flex: 0 0 96rpx; color: #8a999e; }
	.record-value { flex: 1; min-width: 0; color: #3c4a50; word-break: break-all; }
	.record-order-number { color: #1f7f7f; font-weight: 650; letter-spacing: 0.5rpx; }

	.merchant-tool-page :deep(.proto-empty-state) { padding-top: 76rpx; padding-bottom: 92rpx; }
	.merchant-tool-page :deep(.proto-empty-state-image) { width: 210rpx; height: 176rpx; }
</style>
