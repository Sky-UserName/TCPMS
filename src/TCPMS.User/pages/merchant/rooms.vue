<template>
	<view class="proto-page merchant-rooms-page">
		<ProtoHeader title="房间管理" theme="green" />
		<view class="merchant-search-box"><ProtoIcon name="search" :size="34" /><input v-model="keyword" placeholder="输入房间名称" placeholder-class="merchant-search-placeholder" /></view>
		<scroll-view class="merchant-status-tabs" scroll-x :show-scrollbar="false">
			<text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text>
		</scroll-view>
		<view class="merchant-room-summary"><view><text>房源列表</text><text class="merchant-room-summary-caption">本地演示数据</text></view><text>{{ filteredRooms.length }} 间</text></view>
		<ProtoEmptyState v-if="!filteredRooms.length" title="暂无房间数据" description="发布你的第一间房源，开始经营民宿" action-text="发布房源" @action="goPublish" />
		<view v-else class="merchant-room-list">
			<view v-for="room in filteredRooms" :key="room.id" class="merchant-room-card proto-card">
				<view class="merchant-room-card-main">
					<image class="merchant-room-image" :src="room.image" mode="aspectFill" />
					<view class="merchant-room-card-content">
						<view class="merchant-room-card-heading"><view><text class="merchant-room-name">{{ room.name }}</text><text class="proto-pill" :class="room.status === '已上架' ? 'success' : room.status === '待审核' ? 'warn' : 'muted'">{{ room.status }}</text></view><ProtoIcon name="chevron-right" :size="22" /></view>
						<view class="merchant-room-meta"><text>{{ room.type }}</text><text>{{ room.capacity }}</text></view>
						<view class="merchant-room-price"><text>¥{{ room.price }}</text><text>/晚 · 库存 {{ room.stock }}</text></view>
					</view>
				</view>
				<view class="merchant-room-updated">最后编辑 {{ room.updatedAt }}</view>
				<view class="merchant-room-actions"><button class="proto-secondary-button" hover-class="none" @tap="editRoom(room)">编辑房源</button><button class="proto-ghost-button" hover-class="none" @tap="toggleRoom(room)">{{ room.status === '已上架' ? '下架' : '上架' }}</button></view>
			</view>
		</view>
		<view class="merchant-fixed-action"><button class="proto-primary-button" hover-class="none" @tap="goPublish"><ProtoIcon name="plus" tone="white" :size="28" />发布房源</button></view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { goPage, showToast } from '@/common/prototype.js'
	import { getMerchantRooms, updateMerchantRoom } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				keyword: '',
				activeTab: 'all',
				tabs: [
					{ key: 'all', label: '全部' },
					{ key: 'review', label: '待审核' },
					{ key: 'online', label: '已上架' },
					{ key: 'offline', label: '已下架' },
					{ key: 'draft', label: '草稿箱' }
				],
				rooms: []
			}
		},
		onShow() {
			this.rooms = getMerchantRooms()
		},
		computed: {
			filteredRooms() {
				const keyword = this.keyword.trim().toLowerCase()
				return this.rooms.filter((room) => {
					const tabMatch = this.activeTab === 'all' || room.key === this.activeTab
					return tabMatch && (!keyword || room.name.toLowerCase().includes(keyword))
				})
			}
		},
		methods: {
			goPublish() {
				goPage('/pages/merchant/publish')
			},
			editRoom(room) {
				goPage(`/pages/merchant/publish?id=${room.id}`)
			},
			toggleRoom(room) {
				const online = room.status !== '已上架'
				const updated = updateMerchantRoom(room.id, {
					status: online ? '已上架' : '已下架',
					key: online ? 'online' : 'offline'
				})
				if (updated) {
					this.rooms = getMerchantRooms()
					showToast(online ? '房源已上架（本地演示）' : '房源已下架（本地演示）')
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-rooms-page {
		padding-bottom: 140rpx;
		background: #f4fafb;
	}

	.merchant-rooms-page .proto-header {
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
		box-shadow: 0 8rpx 20rpx rgba(42, 169, 169, 0.05);
	}

	.merchant-search-box input {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		font-size: 23rpx;
	}

	.merchant-search-placeholder {
		color: #97a2a8;
	}

	.merchant-status-tabs {
		padding: 4rpx 24rpx 0;
		background: #ffffff;
		white-space: nowrap;
	}

	.merchant-status-tabs text {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 108rpx;
		height: 68rpx;
		margin-right: 18rpx;
		color: var(--proto-muted);
		border-bottom: 5rpx solid transparent;
		font-size: 22rpx;
	}

	.merchant-status-tabs text.active {
		color: var(--proto-primary-dark);
		border-bottom-color: var(--proto-primary);
		font-weight: 750;
	}

	.merchant-room-summary {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin: 22rpx 24rpx 12rpx;
	}

	.merchant-room-summary > view {
		display: flex;
		align-items: baseline;
		gap: 10rpx;
	}

	.merchant-room-summary text:first-child {
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 800;
	}

	.merchant-room-summary text:last-child {
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.merchant-room-summary-caption {
		color: var(--proto-primary-dark) !important;
		font-size: 17rpx !important;
	}

	.merchant-room-list {
		padding-bottom: 10rpx;
	}

	.merchant-room-card {
		padding: 22rpx;
	}

	.merchant-room-card-main {
		display: flex;
		gap: 18rpx;
		min-width: 0;
	}

	.merchant-room-image {
		width: 170rpx;
		height: 140rpx;
		flex: 0 0 170rpx;
		border-radius: 16rpx;
		background: var(--proto-surface-low);
	}

	.merchant-room-card-content {
		flex: 1;
		min-width: 0;
	}

	.merchant-room-card-heading,
	.merchant-room-card-heading > view,
	.merchant-room-actions {
		display: flex;
		align-items: center;
	}

	.merchant-room-card-heading {
		justify-content: space-between;
	}

	.merchant-room-card-heading > view {
		gap: 12rpx;
		flex: 1;
		min-width: 0;
	}

	.merchant-room-name {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-room-meta {
		display: flex;
		flex-wrap: wrap;
		gap: 24rpx;
		margin-top: 18rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.35;
	}

	.merchant-room-meta text:last-child {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-room-price {
		display: flex;
		align-items: baseline;
		gap: 8rpx;
		margin-top: 16rpx;
	}

	.merchant-room-price text:first-child {
		color: var(--proto-primary-dark);
		font-size: 28rpx;
		font-weight: 800;
	}

	.merchant-room-price text:last-child {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.merchant-room-updated {
		margin-top: 18rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.merchant-room-actions {
		justify-content: flex-end;
		gap: 12rpx;
		margin-top: 22rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid #edf1f2;
	}

	.merchant-room-actions button {
		min-width: 150rpx;
		height: 58rpx;
		margin: 0;
		padding: 0 20rpx;
		font-size: 19rpx;
		white-space: nowrap;
	}

	.merchant-fixed-action {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 40;
		padding: 14rpx 24rpx;
		padding-bottom: calc(14rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(197, 199, 202, 0.4);
		box-shadow: 0 -8rpx 24rpx rgba(32, 37, 43, 0.06);
	}

	.merchant-fixed-action .proto-primary-button {
		display: flex;
		gap: 8rpx;
		margin: 0;
		height: 78rpx;
		white-space: nowrap;
	}

	.merchant-rooms-page :deep(.proto-empty-state) {
		padding-top: 76rpx;
		padding-bottom: 92rpx;
	}

	.merchant-rooms-page :deep(.proto-empty-state-image) {
		width: 210rpx;
		height: 176rpx;
	}
</style>
