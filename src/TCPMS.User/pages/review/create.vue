<template>
	<view class="proto-page review-create-page">
		<ProtoHeader title="评价入住体验" title-align="center" theme="green" />

		<view class="review-order-card">
			<image :src="target.roomImage" mode="aspectFill"></image>
			<view class="review-order-copy">
				<text class="review-order-store">{{ target.storeName }}</text>
				<text class="review-order-room">{{ target.roomName }}</text>
				<text class="review-order-date">{{ orderDate }} · {{ order.guest || '入住人信息已登记' }}</text>
			</view>
		</view>

		<view class="review-form-card">
			<view class="review-form-section">
				<view class="review-form-heading">
					<text>房型体验</text>
					<text>{{ roomRating }}分</text>
				</view>
				<view class="picker-stars">
					<view v-for="score in 5" :key="score" class="picker-star" @tap="roomRating = score">
						<ProtoIcon name="star" :size="48" :class="{ selected: score <= roomRating }" />
					</view>
				</view>
				<text class="review-form-caption">房间整洁、舒适度和设施是否符合预期</text>
			</view>

			<view class="review-form-divider"></view>

			<view class="review-form-section">
				<view class="review-form-heading">
					<text>门店服务</text>
					<text>{{ storeRating }}分</text>
				</view>
				<view class="picker-stars">
					<view v-for="score in 5" :key="score" class="picker-star" @tap="storeRating = score">
						<ProtoIcon name="star" :size="48" :class="{ selected: score <= storeRating }" />
					</view>
				</view>
				<text class="review-form-caption">门店环境、服务态度和入住办理是否满意</text>
			</view>
		</view>

		<view class="review-content-card">
			<view class="review-input-heading">
				<text>写下入住感受</text>
				<text>{{ content.length }}/500</text>
			</view>
			<textarea
				v-model="content"
				class="review-textarea"
				maxlength="500"
				placeholder="分享真实入住体验，帮助其他住客做出选择"
				placeholder-class="review-placeholder"
			></textarea>

			<view class="review-image-grid">
				<view v-for="(image, index) in images" :key="`${image}-${index}`" class="review-image-item">
					<image :src="image" mode="aspectFill" @tap="previewImage(index)"></image>
					<button class="remove-image-button" hover-class="none" aria-label="删除图片" @tap="removeImage(index)">
						<ProtoIcon name="close" tone="white" :size="18" />
					</button>
				</view>
				<button v-if="images.length < 9" class="add-image-button" hover-class="none" @tap="chooseImages">
					<ProtoIcon name="image" :size="30" />
					<text>添加图片</text>
				</button>
			</view>
		</view>

		<view class="review-privacy-note">
			<ProtoIcon name="shield" :size="18" />
			<text>评价将公开展示在房型评价列表中，请勿填写身份证号、手机号等隐私信息。</text>
		</view>

		<button class="proto-primary-button review-submit-button" hover-class="none" :disabled="submitting" @tap="submitReview">
			<ProtoIcon name="check" tone="white" :size="22" />
			<text>{{ submitting ? '正在提交...' : '提交评价' }}</text>
		</button>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { addUserReview, findOrder, getOrders, getProfile, resolveReviewTarget, updateOrder } from '@/common/app-store.js'
	import { createRemoteReview } from '@/common/api.js'
	import { goPage, showToast } from '@/common/prototype.js'

	const isGuid = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				order: {
					id: '',
					statusKey: 'done',
					date: '',
					guest: ''
				},
				target: {
					roomId: '',
					storeId: '',
					roomName: '已入住房型',
					storeName: 'TCPMS 门店',
					roomImage: '/static/prototype/home-hero.jpg'
				},
				roomRating: 5,
				storeRating: 5,
				content: '',
				images: [],
				submitting: false
			}
		},
		computed: {
			orderDate() {
				if (this.order.date) return this.order.date
				if (this.order.checkIn || this.order.checkOut) {
					return `${this.order.checkIn || ''} - ${this.order.checkOut || ''}`
				}
				return '已完成入住'
			}
		},
		onLoad(options) {
			const orderId = options && options.orderId ? decodeURIComponent(options.orderId) : ''
			const order = (orderId && findOrder(orderId)) || getOrders()[0]
			if (order) {
				this.order = order
				this.target = resolveReviewTarget(order)
			}
		},
		methods: {
			chooseImages() {
				const remaining = 9 - this.images.length
				if (remaining <= 0) return
				uni.chooseImage({
					count: remaining,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: ({ tempFilePaths = [] }) => {
						this.images = [...this.images, ...tempFilePaths].slice(0, 9)
					},
					fail: () => showToast('暂未选择图片')
				})
			},
			removeImage(index) {
				this.images.splice(index, 1)
			},
			previewImage(index) {
				if (!this.images.length) return
				uni.previewImage({ current: this.images[index], urls: this.images })
			},
			async submitReview() {
				if (this.submitting) return
				if (this.order.statusKey !== 'done' && this.order.status !== '已完成') {
					showToast('订单完成后才可以评价')
					return
				}
				this.submitting = true
				const payload = {
					orderId: this.order.id,
					roomId: this.target.roomId,
					storeId: this.target.storeId,
					roomName: this.target.roomName,
					storeName: this.target.storeName,
					guest: this.order.guest || getProfile().name,
					avatar: getProfile().avatar,
					checkIn: this.order.checkIn || (this.order.date || '').split(' - ')[0],
					roomRating: this.roomRating,
					storeRating: this.storeRating,
					content: this.content.trim(),
					images: this.images
				}
				let savedRemotely = false
				if (isGuid(this.order.id)) {
					try {
						await createRemoteReview(this.order.id, payload)
						savedRemotely = true
					} catch (error) {
						if (error && error.code === 'review_exists') {
							updateOrder(this.order.id, { hasReview: true })
							this.submitting = false
							showToast('该订单已经评价过了')
							setTimeout(() => uni.navigateBack({ delta: 1 }), 400)
							return
						}
					}
				}
				if (!savedRemotely) {
					addUserReview(payload)
				}
				updateOrder(this.order.id, { hasReview: true })
				this.submitting = false
				showToast('评价已提交')
				setTimeout(() => {
					uni.navigateBack({
						delta: 1,
						fail: () => goPage(`/pages/review/list?roomId=${encodeURIComponent(this.target.roomId || '')}&storeId=${encodeURIComponent(this.target.storeId || '')}`)
					})
				}, 500)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.review-create-page {
		padding-bottom: 48rpx;
		background: #f4fafb;
	}

	.review-order-card,
	.review-form-card,
	.review-content-card {
		margin: 16rpx 24rpx;
		padding: 22rpx;
		background: #ffffff;
		border: 1rpx solid rgba(24, 133, 143, 0.12);
		border-radius: 22rpx;
		box-shadow: 0 8rpx 22rpx rgba(0, 106, 106, 0.05);
	}

	.review-order-card {
		display: flex;
		align-items: center;
		gap: 16rpx;
	}

	.review-order-card > image {
		width: 142rpx;
		height: 120rpx;
		flex: 0 0 142rpx;
		border-radius: 14rpx;
		background: #edf2f3;
	}

	.review-order-copy {
		display: flex;
		min-width: 0;
		flex: 1;
		flex-direction: column;
		gap: 7rpx;
	}

	.review-order-store,
	.review-order-room,
	.review-order-date {
		display: block;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.review-order-store {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.review-order-room {
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 800;
	}

	.review-order-date {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.review-form-section {
		padding: 4rpx 0;
	}

	.review-form-heading,
	.review-input-heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 12rpx;
	}

	.review-form-heading > text:first-child,
	.review-input-heading > text:first-child {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 800;
	}

	.review-form-heading > text:last-child,
	.review-input-heading > text:last-child {
		color: var(--proto-primary-dark);
		font-size: 20rpx;
		font-weight: 750;
	}

	.picker-stars {
		display: flex;
		align-items: center;
		gap: 22rpx;
		margin-top: 18rpx;
	}

	.picker-star {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 52rpx;
		height: 52rpx;
	}

	.picker-star :deep(.proto-icon) {
		opacity: 0.18;
		filter: grayscale(1);
	}

	.picker-star :deep(.proto-icon.selected) {
		opacity: 1;
		filter: brightness(0) saturate(100%) invert(57%) sepia(93%) saturate(1874%) hue-rotate(347deg) brightness(102%) contrast(103%);
	}

	.review-form-caption {
		display: block;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
		line-height: 1.45;
	}

	.review-form-divider {
		height: 1rpx;
		margin: 22rpx 0;
		background: rgba(197, 199, 202, 0.42);
	}

	.review-input-heading {
		margin-bottom: 14rpx;
	}

	.review-textarea {
		width: 100%;
		min-height: 220rpx;
		padding: 18rpx;
		color: var(--proto-text);
		background: #f2f8f9;
		border: 1rpx solid rgba(42, 169, 169, 0.12);
		border-radius: 16rpx;
		font-size: 22rpx;
		line-height: 1.55;
	}

	.review-placeholder {
		color: #99a7a9;
	}

	.review-image-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 16rpx;
	}

	.review-image-item,
	.add-image-button {
		position: relative;
		width: calc(25% - 9rpx);
		height: 142rpx;
		border-radius: 12rpx;
	}

	.review-image-item {
		overflow: hidden;
		background: #edf2f3;
	}

	.review-image-item > image {
		width: 100%;
		height: 100%;
	}

	.remove-image-button {
		position: absolute;
		top: 6rpx;
		right: 6rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 34rpx;
		height: 34rpx;
		padding: 0;
		background: rgba(32, 37, 43, 0.68);
		border-radius: 50%;
	}

	.add-image-button {
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		gap: 6rpx;
		color: var(--proto-primary-dark);
		background: #edf8f8;
		border: 1rpx dashed rgba(42, 169, 169, 0.45);
		font-size: 17rpx;
	}

	.review-privacy-note {
		display: flex;
		align-items: flex-start;
		gap: 8rpx;
		margin: 18rpx 30rpx 0;
		color: var(--proto-muted);
		font-size: 17rpx;
		line-height: 1.5;
	}

	.review-privacy-note .proto-icon {
		flex: 0 0 auto;
		margin-top: 2rpx;
	}

	.review-submit-button {
		height: 80rpx;
		margin-top: 26rpx;
		font-size: 24rpx;
	}

	.review-submit-button > .proto-icon {
		margin-right: 8rpx;
	}

	.review-submit-button[disabled] {
		opacity: 0.6;
	}
</style>
