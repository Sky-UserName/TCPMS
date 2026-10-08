<template>
	<view class="proto-page message-page">
		<ProtoHeader title="消息" :back="false" title-align="center" theme="green" />

		<view v-if="messages.length" class="message-list">
			<view v-for="item in messages" :key="item.id" class="message-card" @tap="openMessage(item)">
				<view class="message-icon" :class="`message-icon-${item.type}`">
					<ProtoIcon :name="item.icon" :size="42" />
				</view>
				<view class="message-copy">
					<view class="message-heading">
						<text class="message-title">{{ item.title }}</text>
						<text class="message-time">{{ item.time }}</text>
					</view>
					<text v-if="item.summary" class="message-summary">{{ item.summary }}</text>
				</view>
				<view v-if="item.unread" class="message-unread"></view>
			</view>
		</view>

		<ProtoEmptyState v-else />

		<ProtoBottomNav active="messages" variant="root" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				messages: [
					{
						id: 'platform-announcement',
						type: 'platform',
						icon: 'support',
						title: '平台公告',
						summary: '民宿活动开始啦',
						time: '2026.10.06 14:10',
						unread: true
					},
					{
						id: 'order-message',
						type: 'order',
						icon: 'receipt',
						title: '订单消息',
						summary: '订单状态有更新，请及时查看',
						time: '2026.10.06 14:10',
						unread: false
					},
					{
						id: 'order-reminder',
						type: 'reminder',
						icon: 'clock',
						title: '订单提醒',
						summary: '入住日期将近，提前准备更从容',
						time: '2026.10.06 14:10',
						unread: false
					}
				]
			}
		},
		methods: {
			openMessage(item) {
				if (item.unread) {
					item.unread = false
				}
				uni.showToast({ title: '消息已读', icon: 'none' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.message-page {
		min-height: 100vh;
		padding-bottom: 156rpx;
		padding-bottom: calc(156rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(156rpx + env(safe-area-inset-bottom));
		background: #f5fafb;
	}

	.message-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.message-list {
		display: flex;
		flex-direction: column;
		gap: 18rpx;
		padding: 28rpx 28rpx 36rpx;
	}

	.message-card {
		position: relative;
		display: flex;
		align-items: center;
		gap: 20rpx;
		min-height: 156rpx;
		padding: 26rpx 24rpx;
		background: #ffffff;
		border: 1rpx solid #dceff1;
		border-radius: 26rpx;
		box-shadow: 0 10rpx 24rpx rgba(30, 112, 130, 0.06);
	}

	.message-card:active {
		background: #f0fbfc;
	}

	.message-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 96rpx;
		height: 96rpx;
		flex: 0 0 96rpx;
		color: var(--proto-primary-dark);
		border-radius: 28rpx;
	}

	.message-icon-platform {
		background: #dff7f8;
	}

	.message-icon-order {
		background: #e6f7f6;
	}

	.message-icon-reminder {
		background: #e8f7f0;
	}

	.message-copy {
		flex: 1;
		min-width: 0;
	}

	.message-heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 16rpx;
	}

	.message-title {
		color: var(--proto-text);
		font-size: 30rpx;
		font-weight: 800;
		line-height: 1.25;
	}

	.message-time {
		flex: 0 0 auto;
		color: #8a98a0;
		font-size: 19rpx;
		white-space: nowrap;
	}

	.message-summary {
		display: block;
		margin-top: 10rpx;
		overflow: hidden;
		color: #687b83;
		font-size: 23rpx;
		line-height: 1.4;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.message-unread {
		position: absolute;
		top: 20rpx;
		right: 20rpx;
		width: 12rpx;
		height: 12rpx;
		background: var(--proto-primary);
		border: 3rpx solid #ffffff;
		border-radius: 50%;
		box-shadow: 0 0 0 1rpx #a9e5e6;
	}

	.message-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 180rpx 30rpx 120rpx;
		color: #8d99a4;
		text-align: center;
	}

	.message-empty-illustration {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 190rpx;
		height: 190rpx;
		color: #8d99a4;
	}

	.message-empty-search {
		position: absolute;
		right: 4rpx;
		bottom: 20rpx;
		color: var(--proto-primary);
	}

	.message-empty-title {
		margin-top: 28rpx;
		color: #7b8792;
		font-size: 28rpx;
	}
</style>
