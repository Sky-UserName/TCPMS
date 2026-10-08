<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="评价管理" theme="green" />
		<view class="review-overview proto-card"><view><text class="review-score">4.8</text><text>综合评分</text></view><view class="review-stars">★★★★★</view><text>{{ reviews.length }} 条评价</text></view>
		<scroll-view class="review-tabs" scroll-x :show-scrollbar="false"><text v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">{{ tab.label }}</text></scroll-view>
		<ProtoEmptyState v-if="!filteredReviews.length" title="暂无评价" description="住客完成入住后，评价会显示在这里" />
		<view v-else class="review-list">
			<view v-for="review in filteredReviews" :key="review.id" class="review-card proto-card">
				<view class="review-card-heading"><view class="review-user"><view class="review-avatar">{{ review.guest.slice(0, 1) }}</view><view><text>{{ review.guest }}</text><text>{{ review.date }} 入住</text></view></view><text class="review-stars small">{{ '★★★★★'.slice(0, review.rating) }}</text></view>
				<text class="review-content">{{ review.content }}</text>
				<view v-if="review.reply" class="review-reply"><text>商家回复：</text>{{ review.reply }}</view>
				<button v-else class="proto-secondary-button review-reply-button" hover-class="none" @tap="reply(review)">回复评价</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { getMerchantReviews, saveMerchantReviews } from '@/common/app-store.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() { return { reviews: [], activeTab: 'all', tabs: [{ key: 'all', label: '全部' }, { key: 'unreply', label: '待回复' }, { key: 'replied', label: '已回复' }] } },
		computed: {
			filteredReviews() {
				if (this.activeTab === 'unreply') return this.reviews.filter((review) => !review.reply)
				if (this.activeTab === 'replied') return this.reviews.filter((review) => review.reply)
				return this.reviews
			}
		},
		onShow() { this.reviews = getMerchantReviews() },
		methods: {
			reply(review) {
				uni.showModal({
					title: '回复评价',
					editable: true,
					placeholderText: '请输入回复内容',
					confirmText: '发布',
					success: ({ confirm, content = '' }) => {
						if (!confirm || !content.trim()) return
						const reviews = this.reviews.map((item) => item.id === review.id ? { ...item, reply: content.trim() } : item)
						this.reviews = saveMerchantReviews(reviews)
						showToast('回复已保存（本地演示）')
					}
				})
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
	.review-overview { display: flex; align-items: center; gap: 18rpx; padding: 22rpx; }
	.review-overview > view:first-child { display: flex; align-items: baseline; gap: 8rpx; }
	.review-score { color: var(--proto-primary-dark); font-size: 42rpx; font-weight: 850; }
	.review-overview > view:first-child > text:last-child, .review-overview > text:last-child { color: var(--proto-muted); font-size: 17rpx; }
	.review-overview > text:last-child { flex: 0 0 auto; }
	.review-stars { flex: 1; min-width: 0; color: var(--proto-primary); letter-spacing: 4rpx; font-size: 25rpx; white-space: nowrap; }
	.review-stars.small { flex: 0 0 auto; font-size: 21rpx; }
	.review-tabs { padding: 4rpx 24rpx 0; background: #ffffff; white-space: nowrap; }
	.review-tabs text { display: inline-flex; align-items: center; justify-content: center; min-width: 120rpx; height: 66rpx; margin-right: 14rpx; color: var(--proto-muted); border-bottom: 5rpx solid transparent; font-size: 21rpx; }
	.review-tabs text.active { color: var(--proto-primary-dark); border-bottom-color: var(--proto-primary); font-weight: 750; }
	.review-card { padding: 20rpx; }
	.review-card-heading, .review-user { display: flex; align-items: center; }
	.review-card-heading { justify-content: space-between; }
	.review-user { gap: 12rpx; }
	.review-avatar { display: flex; align-items: center; justify-content: center; width: 56rpx; height: 56rpx; color: #ffffff; background: var(--proto-primary); border-radius: 50%; font-size: 23rpx; font-weight: 750; }
	.review-user > view:last-child { display: flex; min-width: 0; flex-direction: column; }
	.review-user text:first-child { overflow: hidden; color: var(--proto-text); font-size: 22rpx; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
	.review-user text:last-child { margin-top: 5rpx; color: var(--proto-muted); font-size: 17rpx; }
	.review-content { display: block; margin-top: 18rpx; color: var(--proto-text); font-size: 22rpx; line-height: 1.5; overflow-wrap: break-word; word-break: break-all; }
	.review-reply { margin-top: 14rpx; padding: 14rpx 16rpx; color: var(--proto-muted); background: #eef8f8; border-radius: 12rpx; font-size: 18rpx; line-height: 1.5; }
	.review-reply > text { color: var(--proto-primary-dark); font-weight: 700; }
	.review-reply-button { min-width: 152rpx; height: 54rpx; margin: 16rpx 0 0; padding: 0 20rpx; font-size: 18rpx; white-space: nowrap; }
	.merchant-tool-page :deep(.proto-empty-state) { padding-top: 76rpx; padding-bottom: 92rpx; }
	.merchant-tool-page :deep(.proto-empty-state-image) { width: 210rpx; height: 176rpx; }
</style>
