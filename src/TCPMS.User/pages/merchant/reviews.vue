<template>
	<view class="proto-page merchant-reviews-page">
		<ProtoHeader title="评价管理" theme="green" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />

		<view class="review-overview proto-card">
			<view class="review-overview-score">
				<text class="review-score-value">{{ averageScore }}</text>
				<view class="review-overview-stars">
					<view v-for="score in 5" :key="score" class="review-star" :class="{ dim: score > averageRound }">
						<ProtoIcon name="star" :size="22" />
					</view>
				</view>
				<text class="review-overview-label">综合评分</text>
			</view>
			<view class="review-overview-divider"></view>
			<view class="review-overview-stat">
				<text class="review-overview-count">{{ reviews.length }}</text>
				<text class="review-overview-label">全部评价</text>
			</view>
			<view class="review-overview-stat">
				<text class="review-overview-count warn">{{ pendingCount }}</text>
				<text class="review-overview-label">待审核</text>
			</view>
		</view>

		<scroll-view class="review-tabs" scroll-x :show-scrollbar="false">
			<text v-for="tab in tabs" :key="tab.key" class="review-tab" :class="{ active: activeTab === tab.key }" @tap="activeTab = tab.key">
				{{ tab.label }}<text v-if="tab.count" class="review-tab-count">{{ tab.count }}</text>
			</text>
		</scroll-view>

		<ProtoEmptyState v-if="!filteredReviews.length" title="暂无评价" description="住客完成入住并提交评价后，会显示在这里" />
		<view v-else class="review-list">
			<view v-for="review in filteredReviews" :key="review.id" class="review-card proto-card">
				<view class="review-card-heading">
					<view class="review-user">
						<image v-if="review.avatar" class="review-avatar-image" :src="resolveImage(review.avatar)" mode="aspectFill" />
						<view v-else class="review-avatar">{{ review.guest.slice(0, 1) }}</view>
						<view class="review-user-copy">
							<text class="review-user-name">{{ review.guest }}</text>
							<text class="review-user-date">{{ review.date }} 评价</text>
						</view>
					</view>
					<text class="proto-pill" :class="statusTone(review.status)">{{ review.statusLabel }}</text>
				</view>

				<view class="review-room">
					<view class="review-room-icon"><ProtoIcon name="bed" :size="22" /></view>
					<text>{{ review.room }}</text>
				</view>

				<view class="review-stars">
					<view v-for="score in 5" :key="score" class="review-star" :class="{ dim: score > review.roomRating }">
						<ProtoIcon name="star" :size="26" />
					</view>
					<text class="review-rating-text">{{ review.roomRating }}分</text>
				</view>

				<text class="review-content">{{ review.content || '用户未填写文字评价' }}</text>

				<view v-if="review.images && review.images.length" class="review-images">
					<view
						v-for="(image, index) in review.images"
						:key="`${review.id}-${index}`"
						class="review-image-item"
						@tap="previewImages(review.images, index)"
					>
						<image :src="resolveImage(image)" mode="aspectFill"></image>
					</view>
				</view>

				<view class="review-footer">
					<text class="review-footer-tip">{{ footerTip(review.status) }}</text>
					<view class="review-footer-actions">
						<button
							v-if="review.status !== 'hidden'"
							class="review-action-button is-ghost"
							hover-class="none"
							@tap="moderate(review, 'hidden')"
						>{{ review.status === 'published' ? '取消通过' : '不通过' }}</button>
						<button
							v-if="review.status !== 'published'"
							class="review-action-button is-primary"
							hover-class="none"
							@tap="moderate(review, 'published')"
						>{{ review.status === 'hidden' ? '重新通过' : '通过' }}</button>
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
	import { fetchMerchantReviews, getApiAssetUrl, moderateMerchantReview } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				reviews: [],
				activeTab: 'all',
				pullRefreshing: false
			}
		},
		computed: {
			publishedReviews() {
				return this.reviews.filter((review) => review.status === 'published')
			},
			pendingCount() {
				return this.reviews.filter((review) => review.status === 'pending').length
			},
			averageScore() {
				if (!this.publishedReviews.length) return '—'
				const total = this.publishedReviews.reduce((sum, review) => sum + Number(review.roomRating || 0), 0)
				return (total / this.publishedReviews.length).toFixed(1)
			},
			averageRound() {
				return Math.round(Number(this.averageScore) || 0)
			},
			tabs() {
				const count = (status) => this.reviews.filter((review) => review.status === status).length
				return [
					{ key: 'all', label: '全部', count: this.reviews.length },
					{ key: 'pending', label: '待审核', count: count('pending') },
					{ key: 'published', label: '已通过', count: count('published') },
					{ key: 'hidden', label: '未通过', count: count('hidden') }
				]
			},
			filteredReviews() {
				if (this.activeTab === 'all') return this.reviews
				return this.reviews.filter((review) => review.status === this.activeTab)
			}
		},
		async onShow() {
			await this.loadReviews()
		},
		onPullDownRefresh() {
			return runPullRefresh(this, () => this.loadReviews())
		},
		methods: {
			resolveImage(value) {
				return getApiAssetUrl(value)
			},
			statusTone(status) {
				return { pending: 'warn', published: 'success', hidden: 'muted' }[status] || 'muted'
			},
			footerTip(status) {
				if (status === 'pending') return '确认评价内容真实合规后再通过'
				return status === 'published' ? '已通过，用户端评价列表可见' : '未通过，用户端不可见'
			},
			async loadReviews() {
				try {
					const data = await fetchMerchantReviews()
					this.reviews = data.map((review) => ({
						...review,
						roomRating: Number(review.roomRating || 0),
						images: Array.isArray(review.images) ? review.images : []
					}))
				} catch (error) {
					showToast(error?.message || '评价加载失败')
				}
			},
			previewImages(images, current) {
				if (!Array.isArray(images) || !images.length) return
				const urls = images.map((item) => this.resolveImage(item)).filter(Boolean)
				if (!urls.length) return
				uni.previewImage({ current: urls[current] || urls[0], urls })
			},
			moderate(review, status) {
				const approving = status === 'published'
				uni.showModal({
					title: approving ? '通过评价' : '不通过评价',
					content: approving ? '通过后该评价会展示在房型评价列表，确定通过吗？' : '不通过后该评价不会展示给其他用户，确定不通过吗？',
					confirmText: approving ? '通过' : '不通过',
					confirmColor: approving ? '#2aa9a9' : '#c25b5b',
					success: (result) => {
						if (result.confirm) this.submitModerate(review, status)
					},
					fail: (error) => {
						showToast(error?.errMsg || '确认弹窗打开失败，请重试')
					}
				})
			},
			async submitModerate(review, status) {
				uni.showLoading({ title: '正在提交', mask: true })
				try {
					await moderateMerchantReview(review.id, status)
					uni.hideLoading()
					this.reviews = this.reviews.map((item) => item.id === review.id
						? { ...item, status, statusLabel: status === 'published' ? '已通过' : '未通过' }
						: item)
					showToast(status === 'published' ? '评价已通过' : '评价已设为不通过')
				} catch (error) {
					uni.hideLoading()
					showToast(error?.message || '评价审核失败')
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-reviews-page {
		padding-bottom: 48rpx;
		background: #f4fafb;
	}

	.merchant-reviews-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.review-overview {
		display: flex;
		align-items: center;
		gap: 20rpx;
		padding: 22rpx 24rpx;
	}

	.review-overview-score {
		display: flex;
		min-width: 0;
		flex: 1;
		flex-direction: column;
		gap: 6rpx;
	}

	.review-score-value {
		color: var(--proto-primary-deep, #18858f);
		font-size: 46rpx;
		font-weight: 850;
		line-height: 1.05;
	}

	.review-overview-stars,
	.review-stars {
		display: flex;
		align-items: center;
		gap: 4rpx;
	}

	.review-overview-divider {
		width: 1rpx;
		height: 72rpx;
		background: rgba(197, 199, 202, 0.5);
	}

	.review-overview-stat {
		display: flex;
		min-width: 100rpx;
		flex-direction: column;
		align-items: center;
		gap: 4rpx;
	}

	.review-overview-count {
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 800;
		line-height: 1.1;
	}

	.review-overview-count.warn {
		color: #c27a12;
	}

	.review-overview-label {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.review-tabs {
		padding: 4rpx 24rpx 0;
		background: #ffffff;
		white-space: nowrap;
	}

	.review-tab {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 116rpx;
		height: 68rpx;
		margin-right: 12rpx;
		color: var(--proto-muted);
		border-bottom: 5rpx solid transparent;
		font-size: 21rpx;
	}

	.review-tab.active {
		color: var(--proto-primary-dark);
		border-bottom-color: var(--proto-primary);
		font-weight: 750;
	}

	.review-tab-count {
		margin-left: 6rpx;
		color: inherit;
		font-size: 18rpx;
		opacity: 0.75;
	}

	.review-list {
		padding-bottom: 12rpx;
	}

	.review-card {
		padding: 22rpx;
	}

	.review-card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 14rpx;
	}

	.review-user {
		display: flex;
		align-items: center;
		min-width: 0;
		gap: 12rpx;
	}

	.review-avatar,
	.review-avatar-image {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 60rpx;
		height: 60rpx;
		flex: 0 0 auto;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 50%;
		font-size: 24rpx;
		font-weight: 750;
	}

	.review-avatar-image {
		background: var(--proto-surface-low);
	}

	.review-user-copy {
		display: flex;
		min-width: 0;
		flex-direction: column;
		gap: 4rpx;
	}

	.review-user-name {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.review-user-date {
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.review-room {
		display: flex;
		align-items: center;
		gap: 8rpx;
		margin-top: 16rpx;
		padding: 10rpx 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 12rpx;
		font-size: 19rpx;
	}

	.review-room text {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.review-room-icon {
		display: flex;
		flex: 0 0 auto;
		filter: brightness(0) saturate(100%) invert(72%) sepia(83%) saturate(984%) hue-rotate(122deg) brightness(75%) contrast(68%);
	}

	.review-stars {
		margin-top: 14rpx;
	}

	.review-star {
		display: flex;
		flex: 0 0 auto;
		filter: brightness(0) saturate(100%) invert(72%) sepia(83%) saturate(984%) hue-rotate(122deg) brightness(75%) contrast(68%);
	}

	.review-star.dim {
		filter: none;
		opacity: 0.18;
	}

	.review-rating-text {
		margin-left: 6rpx;
		color: var(--proto-primary-deep, #18858f);
		font-size: 19rpx;
		font-weight: 750;
	}

	.review-content {
		display: block;
		margin-top: 14rpx;
		color: var(--proto-text);
		font-size: 22rpx;
		line-height: 1.55;
		overflow-wrap: break-word;
		word-break: break-all;
	}

	.review-images {
		display: flex;
		flex-wrap: wrap;
		gap: 10rpx;
		margin-top: 14rpx;
	}

	.review-image-item {
		width: calc((100% - 20rpx) / 3);
		height: 180rpx;
		overflow: hidden;
		background: var(--proto-surface-low);
		border-radius: 12rpx;
	}

	.review-image-item image {
		display: block;
		width: 100%;
		height: 100%;
	}

	.review-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 14rpx;
		margin-top: 18rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid rgba(197, 199, 202, 0.36);
	}

	.review-footer-tip {
		min-width: 0;
		flex: 1;
		color: var(--proto-muted);
		font-size: 17rpx;
		line-height: 1.4;
	}

	.review-footer-actions {
		display: flex;
		flex: 0 0 auto;
		align-items: center;
		gap: 12rpx;
	}

	.review-action-button {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 112rpx;
		height: 58rpx;
		margin: 0;
		padding: 0 20rpx;
		border-radius: 29rpx;
		font-size: 20rpx;
		font-weight: 700;
		line-height: 1;
	}

	.review-action-button::after {
		border: 0;
	}

	.review-action-button.is-ghost {
		color: var(--proto-muted);
		background: #ffffff;
		border: 1rpx solid #d9e4e8;
	}

	.review-action-button.is-primary {
		color: #ffffff;
		background: var(--proto-primary);
	}

	.merchant-reviews-page :deep(.proto-empty-state) {
		padding-top: 76rpx;
		padding-bottom: 92rpx;
	}

	.merchant-reviews-page :deep(.proto-empty-state-image) {
		width: 210rpx;
		height: 176rpx;
	}
</style>
