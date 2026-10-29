<template>
	<view class="proto-page notice-page">
		<ProtoHeader title="预定须知" theme="green" />
		<view class="notice-card proto-card">
			<view class="notice-heading">
				<text class="notice-title">门店级预定须知</text>
				<text class="notice-status">{{ statusText }}</text>
			</view>
			<text class="notice-tip">保存后门店会进入审核，审核期间该门店的所有房间不会在小程序展示。</text>

			<view class="notice-field notice-field-block">
				<text class="notice-label">入住离店须知</text>
				<textarea v-model.trim="form.checkInNotice" class="notice-textarea notice-textarea-large" maxlength="2000" placeholder="例如：入住时间当天 14:00 后，退房时间次日 12:00 前；入住需实名登记" placeholder-class="notice-placeholder" />
			</view>
			<view class="notice-field notice-field-block">
				<text class="notice-label">取消退改通知</text>
				<textarea v-model.trim="form.cancelNotice" class="notice-textarea notice-textarea-large" maxlength="2000" placeholder="例如：入住前 24 小时可免费取消；逾期取消扣除首晚房费" placeholder-class="notice-placeholder" />
			</view>
		</view>

		<view class="notice-actions">
			<button class="proto-primary-button" hover-class="none" :disabled="saving" @tap="save">{{ saving ? '提交中...' : '保存并提交审核' }}</button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import { fetchMerchantBookingNotice, saveMerchantBookingNotice } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader },
		data() {
			return {
				saving: false,
				status: 'approved',
				form: {
					checkInNotice: '',
					cancelNotice: ''
				}
			}
		},
		computed: {
			statusText() {
				if (this.status === 'pending') return '审核中'
				if (this.status === 'rejected') return '未通过'
				return '已生效'
			}
		},
		async onLoad() {
			await this.load()
		},
		methods: {
			async load() {
				try {
					const notice = await fetchMerchantBookingNotice()
					this.status = notice?.status || 'approved'
					this.form = {
						checkInNotice: notice?.checkInNotice || '',
						cancelNotice: notice?.cancelNotice || ''
					}
				} catch (error) {
					showToast(error?.message || '预定须知加载失败')
				}
			},
			async save() {
				if (this.saving) return
				const hasContent = ['checkInNotice', 'cancelNotice']
					.some((key) => String(this.form[key] || '').trim())
				if (!hasContent) {
					showToast('请至少填写一项预定须知')
					return
				}

				uni.showModal({
					title: '确认提交审核',
					content: '保存后门店会进入审核，审核期间门店和所有房间在小程序不可见，审核通过后恢复。',
					confirmText: '提交审核',
					cancelText: '再想想',
					success: async (confirmResult) => {
						if (!confirmResult.confirm) return
						this.saving = true
						try {
							const result = await saveMerchantBookingNotice(this.form)
							this.status = result?.status || 'pending'
							uni.showModal({
								title: '已提交审核',
								content: '预定须知已保存，门店审核通过后会重新展示。',
								showCancel: false,
								confirmText: '知道了',
								success: () => uni.navigateBack({ delta: 1 })
							})
						} catch (error) {
							showToast(error?.message || '保存失败，请稍后重试')
						} finally {
							this.saving = false
						}
					}
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.notice-page {
		padding-bottom: 180rpx;
		background: #f4fafb;
	}

	.notice-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.notice-card {
		margin: 20rpx 24rpx 0;
		padding: 24rpx;
	}

	.notice-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16rpx;
	}

	.notice-title {
		color: #20252b;
		font-size: 27rpx;
		font-weight: 800;
	}

	.notice-status {
		flex: 0 0 auto;
		padding: 4rpx 14rpx;
		color: #006a6a;
		background: #e6f7f7;
		border-radius: 999rpx;
		font-size: 18rpx;
	}

	.notice-tip {
		display: block;
		margin-top: 14rpx;
		color: #6f8085;
		font-size: 19rpx;
		line-height: 1.55;
	}

	.notice-field {
		margin-top: 20rpx;
	}

	.notice-field-block {
		display: flex;
		flex-direction: column;
	}

	.notice-label {
		display: block;
		margin-bottom: 8rpx;
		color: #3d5a5c;
		font-size: 21rpx;
		font-weight: 650;
	}

	.notice-input,
	.notice-textarea {
		width: 100%;
		box-sizing: border-box;
		padding: 0 18rpx;
		color: #20252b;
		background: #f5f9fa;
		border: 1rpx solid #dce9ea;
		border-radius: 14rpx;
		font-size: 22rpx;
	}

	.notice-input {
		height: 74rpx;
	}

	.notice-textarea-large {
		height: 190rpx;
	}

	.notice-textarea {
		height: 150rpx;
		padding-top: 14rpx;
		padding-bottom: 14rpx;
		line-height: 1.5;
	}

	.notice-placeholder {
		color: #9aabad;
	}

	.notice-actions {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		padding: 14rpx 24rpx calc(14rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(24, 133, 143, 0.12);
	}

	.notice-actions .proto-primary-button {
		width: 100%;
		margin: 0;
	}
</style>
