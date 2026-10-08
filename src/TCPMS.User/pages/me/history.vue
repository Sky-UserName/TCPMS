<template>
	<view class="proto-page history-page">
		<ProtoHeader title="浏览记录" theme="green" />

		<view v-if="items.length" class="history-toolbar">
			<text>最近浏览 {{ items.length }} 条</text>
			<button hover-class="none" @tap="clearItems">清空记录</button>
		</view>

		<view v-if="items.length" class="history-list">
			<view v-for="item in items" :key="`${item.type}-${item.id}`" class="history-item proto-card" @tap="openItem(item)">
				<image :src="item.image || fallbackImage" mode="aspectFill"></image>
				<view class="history-copy">
					<text class="history-title">{{ item.title }}</text>
					<text class="history-subtitle">{{ item.subtitle || 'TCPMS 旅宿内容' }}</text>
					<view class="history-time"><text>{{ formatTime(item.updatedAt) }}　查看详情</text><ProtoIcon name="chevron-right" :size="22" /></view>
				</view>
			</view>
		</view>

		<ProtoEmptyState v-else class="history-empty" action-text="去发现房源" @action="goStores" />

		<ProtoBottomNav active="me" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { goPage, prototypeImages, showToast } from '@/common/prototype.js'
	import { clearRecent, getRecent } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				items: [],
				fallbackImage: prototypeImages.homeHero
			}
		},
		onShow() {
			this.items = getRecent()
		},
		methods: {
			openItem(item) {
				if (item.path) {
					goPage(item.path)
					return
				}
				showToast('该记录内容暂不可打开')
			},
			clearItems() {
				uni.showModal({
					title: '清空浏览记录',
					content: '清空后将无法在这里找回最近浏览内容。',
					success: ({ confirm }) => {
						if (!confirm) return
						clearRecent()
						this.items = []
						showToast('浏览记录已清空')
					}
				})
			},
			formatTime(value) {
				if (!value) return '刚刚'
				const time = new Date(value)
				if (Number.isNaN(time.getTime())) return '刚刚'
				return `${String(time.getHours()).padStart(2, '0')}:${String(time.getMinutes()).padStart(2, '0')}`
			},
			goStores() {
				uni.reLaunch({ url: '/pages/store/list' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.history-page {
		padding-bottom: 136rpx;
		padding-bottom: calc(136rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(136rpx + env(safe-area-inset-bottom));
	}

	.history-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 20rpx 28rpx 8rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.history-toolbar button {
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		font-size: 19rpx;
	}

	.history-list {
		display: flex;
		flex-direction: column;
		gap: 16rpx;
		padding: 14rpx 24rpx 30rpx;
	}

	.history-item {
		display: flex;
		align-items: center;
		gap: 18rpx;
		padding: 16rpx;
	}

	.history-item > image {
		width: 150rpx;
		height: 126rpx;
		flex: 0 0 150rpx;
		border-radius: 14rpx;
	}

	.history-copy {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.history-title {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 700;
		line-height: 1.35;
	}

	.history-subtitle {
		margin-top: 8rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.history-time {
		display: flex;
		align-items: center;
		gap: 4rpx;
		margin-top: 16rpx;
		color: var(--proto-primary-dark);
		font-size: 18rpx;
	}

	.history-empty {
		margin: 24rpx;
	}
</style>
