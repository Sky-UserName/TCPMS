<template>
	<view class="proto-page review-list-page">
		<ProtoHeader title="评价列表" title-align="center" theme="plain" />

		<view class="review-room-strip">
			<text class="review-room-name">{{ roomName || '房型评价' }}</text>
			<text class="review-store-name">{{ storeName || 'TCPMS 门店' }}</text>
		</view>

		<view class="review-summary-panel">
			<view class="review-summary-item">
				<text class="summary-score">{{ formatRating(summary.roomAverageRating) }}</text>
				<view class="summary-copy">
					<text>房型评分</text>
					<view class="rating-stars">
						<view v-for="score in 5" :key="score" :class="['rating-star', { selected: score <= Math.round(summary.roomAverageRating) }]">
							<ProtoIcon name="star" :size="24" />
						</view>
					</view>
				</view>
			</view>
			<view class="review-summary-divider"></view>
			<view class="review-summary-item">
				<text class="summary-score">{{ formatRating(summary.storeAverageRating) }}</text>
				<view class="summary-copy">
					<text>门店评分</text>
					<view class="rating-stars">
						<view v-for="score in 5" :key="score" :class="['rating-star', { selected: score <= Math.round(summary.storeAverageRating) }]">
							<ProtoIcon name="star" :size="24" />
						</view>
					</view>
				</view>
			</view>
			<text class="review-total">{{ summary.reviewCount }}条评价</text>
		</view>

		<view class="review-section-heading">
			<text>住客评价</text>
			<text>{{ summary.reviewCount }}条真实入住反馈</text>
		</view>

		<ProtoEmptyState v-if="!reviews.length" title="暂无评价" description="完成入住后，住客评价会显示在这里" />
		<view v-else class="review-list">
			<view v-for="review in reviews" :key="review.id" class="review-card">
				<view class="review-card-heading">
					<view class="review-user">
						<image v-if="review.avatar" class="review-avatar-image" :src="review.avatar" mode="aspectFill"></image>
						<view v-else class="review-avatar">{{ initial(review) }}</view>
						<view class="review-user-copy">
							<text>{{ review.nickname || '微信用户' }}</text>
							<text>{{ formatStayDate(review) }}</text>
						</view>
					</view>
					<view class="review-rating">
						<view class="rating-stars">
							<view v-for="score in 5" :key="score" :class="['rating-star', { selected: score <= Number(review.roomRating || review.rating || 0) }]">
								<ProtoIcon name="star" :size="27" />
							</view>
						</view>
						<text>{{ Number(review.roomRating || review.rating || 0) }}分</text>
					</view>
				</view>

				<text v-if="review.content" class="review-content">{{ review.content }}</text>

				<view class="review-store-score">
					<text>门店评分</text>
					<view class="rating-stars">
						<view v-for="score in 5" :key="score" :class="['rating-star', { selected: score <= Number(review.storeRating || review.rating || 0) }]">
							<ProtoIcon name="star" :size="22" />
						</view>
					</view>
				</view>

				<view v-if="review.images && review.images.length" class="review-images">
					<image
						v-for="(image, index) in review.images"
						:key="`${review.id}-${index}`"
						:src="image"
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
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { getRoomReviewSummary, getRoomReviews } from '@/common/app-store.js'
	import { fetchRoomReviews } from '@/common/api.js'

	const isGuid = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
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
			initial(review) {
				return String(review && (review.nickname || review.guest) || '微').slice(0, 1)
			},
			formatStayDate(review) {
				const value = String(review && (review.checkIn || review.createdAt) || '')
				const date = value.slice(0, 10)
				return date ? `${date} 入住` : '已完成入住'
			},
			previewImages(images, current) {
				if (!Array.isArray(images) || !images.length) return
				uni.previewImage({ current, urls: images })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.review-list-page {
		padding-bottom: 48rpx;
		background: #f7f7f7;
	}

	.review-list-page :deep(.proto-header) {
		background: #ffffff;
		border-bottom-color: rgba(197, 199, 202, 0.32);
	}

	.review-room-strip {
		display: flex;
		flex-direction: column;
		gap: 6rpx;
		padding: 20rpx 30rpx 18rpx;
		background: #ffffff;
	}

	.review-room-name {
		color: #252525;
		font-size: 29rpx;
		font-weight: 800;
		line-height: 1.35;
	}

	.review-store-name {
		color: #999999;
		font-size: 18rpx;
		line-height: 1.4;
	}

	.review-summary-panel {
		display: flex;
		align-items: center;
		gap: 18rpx;
		margin: 16rpx 24rpx 0;
		padding: 22rpx 20rpx;
		background: #ffffff;
		border-radius: 18rpx;
		box-shadow: 0 6rpx 18rpx rgba(32, 37, 43, 0.04);
	}

	.review-summary-item {
		display: flex;
		align-items: center;
		gap: 10rpx;
		min-width: 0;
		flex: 1;
	}

	.summary-score {
		color: #ff7b2c;
		font-size: 34rpx;
		font-weight: 850;
		line-height: 1;
	}

	.summary-copy {
		display: flex;
		min-width: 0;
		flex-direction: column;
		gap: 6rpx;
		color: #666666;
		font-size: 17rpx;
	}

	.rating-stars {
		display: flex;
		align-items: center;
		gap: 3rpx;
	}

	.rating-star {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 27rpx;
		height: 27rpx;
		opacity: 0.18;
		filter: grayscale(1);
	}

	.rating-star.selected {
		opacity: 1;
		filter: brightness(0) saturate(100%) invert(57%) sepia(93%) saturate(1874%) hue-rotate(347deg) brightness(102%) contrast(103%);
	}

	.review-summary-divider {
		width: 1rpx;
		height: 54rpx;
		background: #eeeeee;
	}

	.review-total {
		flex: 0 0 auto;
		color: #999999;
		font-size: 17rpx;
		white-space: nowrap;
	}

	.review-section-heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 12rpx;
		padding: 24rpx 30rpx 12rpx;
		color: #252525;
		font-size: 25rpx;
		font-weight: 800;
	}

	.review-section-heading > text:last-child {
		color: #999999;
		font-size: 17rpx;
		font-weight: 400;
	}

	.review-list {
		padding-bottom: 24rpx;
	}

	.review-card {
		margin: 0 24rpx 16rpx;
		padding: 22rpx 22rpx 24rpx;
		background: #ffffff;
		border-radius: 20rpx;
		box-shadow: 0 6rpx 18rpx rgba(32, 37, 43, 0.04);
	}

	.review-card-heading {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 12rpx;
	}

	.review-user {
		display: flex;
		align-items: center;
		gap: 12rpx;
		min-width: 0;
	}

	.review-avatar,
	.review-avatar-image {
		width: 68rpx;
		height: 68rpx;
		flex: 0 0 68rpx;
		border-radius: 50%;
	}

	.review-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		color: #ffffff;
		background: #20252b;
		font-size: 26rpx;
		font-weight: 750;
	}

	.review-avatar-image {
		background: #edf2f3;
	}

	.review-user-copy {
		display: flex;
		min-width: 0;
		flex-direction: column;
		gap: 5rpx;
	}

	.review-user-copy text:first-child {
		overflow: hidden;
		color: #333333;
		font-size: 23rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.review-user-copy text:last-child {
		color: #999999;
		font-size: 18rpx;
	}

	.review-rating {
		display: flex;
		align-items: flex-end;
		flex-direction: column;
		gap: 4rpx;
		flex: 0 0 auto;
		color: #ff7b2c;
		font-size: 17rpx;
	}

	.review-content {
		display: block;
		margin-top: 18rpx;
		color: #333333;
		font-size: 23rpx;
		line-height: 1.55;
		overflow-wrap: break-word;
		word-break: break-all;
	}

	.review-store-score {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-top: 14rpx;
		color: #999999;
		font-size: 17rpx;
	}

	.review-images {
		display: flex;
		flex-wrap: wrap;
		gap: 10rpx;
		margin-top: 16rpx;
	}

	.review-images image {
		width: calc(25% - 8rpx);
		height: 142rpx;
		border-radius: 12rpx;
		background: #edf2f3;
	}

	.review-list-page :deep(.proto-empty-state) {
		padding-top: 78rpx;
		padding-bottom: 100rpx;
	}
</style>
