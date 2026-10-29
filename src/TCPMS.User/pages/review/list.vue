<template>
	<view class="proto-page review-list-page">
		<ProtoHeader title="评价列表" title-align="center" theme="plain" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />

		<ProtoEmptyState v-if="!reviews.length" title="暂无评价" description="完成入住后，住客评价会显示在这里" />
		<view v-else class="review-list">
			<view v-for="review in reviews" :key="review.id" class="review-card">
				<view class="review-card-heading">
					<view class="review-user">
						<image v-if="review.avatar" class="review-avatar-image" :src="resolveImage(review.avatar)" mode="aspectFill"></image>
						<view v-else class="review-avatar">{{ initial(review) }}</view>
						<view class="review-user-copy">
							<text class="review-nickname">{{ review.nickname || '微信用户' }}</text>
							<text class="review-stay-date">{{ formatStayDate(review) }}</text>
						</view>
					</view>
					<view class="review-stars">
						<view v-for="score in 5" :key="score" :class="['review-star', { selected: score <= reviewScore(review) }]">
							<ProtoIcon name="star" :size="30" />
						</view>
					</view>
				</view>

				<text v-if="review.content" class="review-content">{{ review.content }}</text>

				<view v-if="review.images && review.images.length" class="review-images">
					<image
						v-for="(image, index) in review.images"
						:key="`${review.id}-${index}`"
						:src="resolveImage(image)"
						mode="aspectFill"
						@tap="previewImages(review.images, index)"
					></image>
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
	import { getRoomReviewSummary, getRoomReviews } from '@/common/app-store.js'
	import { fetchRoomReviews, getApiAssetUrl } from '@/common/api.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	const isGuid = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				pullRefreshing: false,
				roomId: '',
				storeId: '',
				roomName: '',
				storeName: '',
				summary: {
					roomAverageRating: 0,
					storeAverageRating: 0,
					reviewCount: 0
				},
				reviews: [],
				remoteLoaded: false
			}
		},
		onLoad(options) {
			this.roomId = options && (options.roomId || options.id) ? decodeURIComponent(options.roomId || options.id) : ''
			this.storeId = options && options.storeId ? decodeURIComponent(options.storeId) : ''
			this.roomName = options && options.roomName ? decodeURIComponent(options.roomName) : ''
			this.storeName = options && options.storeName ? decodeURIComponent(options.storeName) : ''
			this.refreshLocalReviews()
			this.loadRemoteReviews()
		},
		onShow() {
			if (!this.remoteLoaded) this.refreshLocalReviews()
		},
		onPullDownRefresh() {
			return runPullRefresh(this, async () => {
				this.refreshLocalReviews()
				await this.loadRemoteReviews()
			})
		},
		methods: {
			refreshLocalReviews() {
				this.summary = getRoomReviewSummary(this.roomId, this.storeId)
				this.reviews = getRoomReviews(this.roomId, this.storeId)
			},
			async loadRemoteReviews() {
				if (!isGuid(this.roomId)) return
				try {
					const response = await fetchRoomReviews(this.roomId)
					this.summary = response
					this.reviews = response.items || []
					this.roomName = response.roomName || this.roomName
					this.storeName = response.storeName || this.storeName
					this.remoteLoaded = true
				} catch (error) {
					// Keep the local review list available when the API is offline.
				}
			},
			formatRating(value) {
				return Number(value || 0).toFixed(1)
			},
			reviewScore(review) {
				const value = Number(review && (review.roomRating || review.rating) || 0)
				return Math.max(0, Math.min(5, Math.round(value)))
			},
			resolveImage(image) {
				return getApiAssetUrl(image)
			},
			initial(review) {
				return String(review && (review.nickname || review.guest) || '微').slice(0, 1)
			},
			formatStayDate(review) {
				const value = String(review && (review.checkIn || review.createdAt) || '')
				const date = value.slice(0, 10)
				return date ? `${date}入住` : '已完成入住'
			},
			previewImages(images, current) {
				if (!Array.isArray(images) || !images.length) return
				const urls = images.map((item) => getApiAssetUrl(item))
				uni.previewImage({ current: urls[current] || urls[0], urls })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.review-list-page {
		min-height: 100vh;
		padding-bottom: 40rpx;
		background: #f5f5f5;
	}

	.review-list-page :deep(.proto-header) {
		background: #f5f5f5;
		border-bottom-color: rgba(0, 0, 0, 0.04);
	}

	.review-list {
		padding: 8rpx 0 24rpx;
	}

	.review-card {
		margin: 0 20rpx 18rpx;
		padding: 24rpx 22rpx 22rpx;
		background: #ffffff;
		border-radius: 18rpx;
	}

	.review-card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16rpx;
	}

	.review-user {
		display: flex;
		align-items: center;
		flex: 1;
		gap: 14rpx;
		min-width: 0;
	}

	.review-avatar,
	.review-avatar-image {
		width: 66rpx;
		height: 66rpx;
		flex: 0 0 66rpx;
		border-radius: 50%;
	}

	.review-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		color: #ffffff;
		background: #c7ccd1;
		font-size: 26rpx;
		font-weight: 700;
	}

	.review-avatar-image {
		background: #edf0f2;
	}

	.review-user-copy {
		display: flex;
		min-width: 0;
		flex-direction: column;
		gap: 6rpx;
	}

	.review-nickname {
		overflow: hidden;
		color: #1f2329;
		font-size: 26rpx;
		font-weight: 700;
		line-height: 1.2;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.review-stay-date {
		color: #a3a8ae;
		font-size: 20rpx;
		line-height: 1.2;
	}

	.review-stars {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		gap: 2rpx;
	}

	.review-star {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 30rpx;
		height: 30rpx;
		opacity: 0.24;
		filter: grayscale(1);
	}

	.review-star.selected {
		opacity: 1;
		// 染色为品牌主色 #2aa9a9（青绿），保持与全局 UI 一致。
		filter: brightness(0) saturate(100%) invert(72%) sepia(83%) saturate(984%) hue-rotate(122deg) brightness(75%) contrast(68%);
	}

	.review-content {
		display: block;
		margin-top: 16rpx;
		color: #333333;
		font-size: 26rpx;
		line-height: 1.58;
		overflow-wrap: break-word;
		word-break: break-all;
	}

	.review-images {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 16rpx;
	}

	.review-images image {
		width: calc(25% - 9rpx);
		height: 158rpx;
		border-radius: 8rpx;
		background: #edf0f2;
	}

	.review-list-page :deep(.proto-empty-state) {
		padding-top: 96rpx;
		padding-bottom: 120rpx;
	}
</style>
