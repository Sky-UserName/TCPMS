<template>
	<view class="proto-page proto-page-plain auth-page">
		<view class="auth-topbar">
			<view class="auth-brand">
				<view class="auth-brand-icon"><ProtoIcon name="user" tone="white" :size="24" /></view>
				<text>择居青旅</text>
			</view>
		</view>

		<view class="auth-logo">
			<view class="logo-mark">
				<image :src="brandLogoSrc" mode="aspectFit" @error="useLocalBrandLogo"></image>
			</view>
			<text>住宿预订与安心入住</text>
		</view>

		<view class="privacy-card">
			<view class="privacy-title-row">
				<view>
					<text class="privacy-title">{{ page.title || '欢迎使用择居青旅' }}</text>
					<text class="privacy-subtitle">{{ page.subtitle }}</text>
				</view>
				<text class="proto-pill info">{{ page.badge || '安全合规保障' }}</text>
			</view>

			<view v-if="page.intro" class="privacy-intro">
				<view class="privacy-intro-icon"><ProtoIcon name="check" tone="white" :size="20" /></view>
				<text>{{ page.intro }}</text>
			</view>

			<view class="privacy-summary-title">
				<text>隐私保护要点概览</text>
				<text class="proto-link">可向下滑动阅读全文 ↓</text>
			</view>
			<scroll-view scroll-y class="privacy-scroll">
				<view v-if="loadState !== 'ready'" class="privacy-point">
					<text class="privacy-point-dot"></text>
					<view>
						<text class="privacy-point-title">{{ loadState === 'error' ? '隐私要点加载失败' : '正在加载隐私要点' }}</text>
						<text class="privacy-point-body">{{ loadState === 'error' ? '请检查网络后重新进入本页面。' : '稍候即可查看完整的授权说明。' }}</text>
					</view>
				</view>
				<block v-else>
					<view v-for="item in page.points" :key="item.title" class="privacy-point">
						<text class="privacy-point-dot"></text>
						<view>
							<text class="privacy-point-title">{{ item.index }}. {{ item.title }}</text>
							<text class="privacy-point-body">{{ item.body }}</text>
						</view>
					</view>
				</block>
			</scroll-view>
		</view>

		<view class="proto-check" @tap="agreed = !agreed">
			<view class="proto-check-box" :class="{ checked: agreed }"><ProtoIcon v-if="agreed" name="check" tone="white" :size="22" /></view>
			<text>我已充分阅读并理解 <text class="proto-link" @tap.stop="showAgreement = true">《择居青旅用户协议》</text> 与 <text class="proto-link" @tap.stop="showAgreement = true">《隐私政策》</text>，并同意依法使用必要服务。</text>
		</view>

		<text v-if="errorMessage" class="form-error">{{ errorMessage }}</text>
		<button class="proto-primary-button" hover-class="none" @tap="continueAuth"><text>同意并继续</text><ProtoIcon name="chevron-right" tone="white" :size="28" /></button>

		<view v-if="showAgreement" class="proto-mask" @tap="showAgreement = false">
			<view class="proto-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>协议摘要</text>
					<button class="proto-close" hover-class="none" @tap="showAgreement = false"><ProtoIcon name="close" :size="24" /></button>
				</view>
				<view class="proto-body agreement-body">
					<block v-for="(block, index) in page.blocks" :key="index">
						<text v-if="block.type === 'heading'" class="agreement-heading">{{ block.text }}</text>
						<text v-else>{{ block.text }}</text>
					</block>
				</view>
				<button class="proto-primary-button" hover-class="none" @tap="showAgreement = false">已阅读并同意</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { fetchPageContent } from '@/common/api.js'
	import { brandImages, brandImageFallbacks } from '@/common/brand-assets.js'

	const EMPTY_PAGE = {
		title: '',
		subtitle: '',
		badge: '',
		intro: '',
		points: [],
		blocks: []
	}

	export default {
		components: { ProtoIcon },
		data() {
			return {
				agreed: false,
				showAgreement: false,
				errorMessage: '',
				loadState: 'loading',
				brandLogoSrc: brandImages.logo,
				page: { ...EMPTY_PAGE }
			}
		},
		onLoad() {
			this.loadPage()
		},
		methods: {
			// 后端托管图不可用时退回包内 logo。
			useLocalBrandLogo() {
				this.brandLogoSrc = brandImageFallbacks.logo
			},
			async loadPage() {
				try {
					const data = await fetchPageContent('auth-privacy')
					this.page = {
						...EMPTY_PAGE,
						...data,
						points: Array.isArray(data.points) ? data.points : [],
						blocks: Array.isArray(data.blocks) ? data.blocks : []
					}
					this.loadState = 'ready'
				} catch (error) {
					this.page = { ...EMPTY_PAGE }
					this.loadState = 'error'
				}
			},
			continueAuth() {
				if (!this.agreed) {
					this.errorMessage = '请先阅读并勾选同意相关协议'
					return
				}
				this.errorMessage = ''
				uni.setStorageSync('tcpms.privacyAgreed', true)
				uni.navigateTo({ url: '/pages/auth/login' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.auth-page {
		padding-bottom: 40rpx;
	}

	.auth-topbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		height: 96rpx;
		padding: 22rpx 32rpx 8rpx;
		padding: calc(22rpx + constant(safe-area-inset-top)) 32rpx 8rpx;
		padding: calc(22rpx + env(safe-area-inset-top)) 32rpx 8rpx;
	}

	.auth-brand {
		display: flex;
		align-items: center;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 700;
	}

	.auth-brand-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 42rpx;
		height: 42rpx;
		margin-right: 10rpx;
		background: var(--proto-primary-dark);
		border-radius: 50%;
	}

	.auth-topbar {
		justify-content: flex-start;
	}

	.auth-brand {
		min-width: 0;
	}

	.auth-brand > text {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.auth-logo {
		display: flex;
		align-items: center;
		flex-direction: column;
		margin: 36rpx 0 24rpx;
		color: var(--proto-primary-dark);
		font-size: 30rpx;
		font-weight: 800;
	}

	.logo-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 270rpx;
		height: 150rpx;
		margin-bottom: 18rpx;
		color: var(--proto-primary);
		background: #ffffff;
		border-radius: 28rpx;
		box-shadow: 0 10rpx 28rpx rgba(42, 169, 169, 0.12);
	}

	.logo-mark image {
		width: 250rpx;
		height: 136rpx;
		border-radius: 12rpx;
	}

	.privacy-card {
		margin: 0 28rpx;
		padding: 28rpx;
		background: var(--proto-surface);
		border-radius: 26rpx;
		box-shadow: 0 10rpx 28rpx rgba(42, 169, 169, 0.08);
	}

	.privacy-title-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		padding-bottom: 20rpx;
		border-bottom: 1rpx solid #e3ebec;
	}

	.privacy-title {
		display: block;
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 800;
	}

	.privacy-subtitle {
		display: block;
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.privacy-intro {
		display: flex;
		align-items: flex-start;
		gap: 14rpx;
		margin-top: 22rpx;
		padding: 20rpx;
		color: var(--proto-text);
		background: #f1faf9;
		border: 1rpx solid #d8eeee;
		border-radius: 18rpx;
		font-size: 22rpx;
		line-height: 1.55;
	}


	.privacy-intro-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 34rpx;
		height: 34rpx;
		flex: 0 0 34rpx;
		color: #ffffff;
		background: var(--proto-primary);
		border-radius: 50%;
	}

	.privacy-summary-title {
		display: flex;
		justify-content: space-between;
		margin-top: 24rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
	}

	.privacy-scroll {
		height: 540rpx;
		margin-top: 4rpx;
	}

	.privacy-point {
		display: flex;
		gap: 14rpx;
		margin-top: 20rpx;
	}

	.privacy-point-dot {
		width: 14rpx;
		height: 14rpx;
		flex: 0 0 14rpx;
		margin-top: 10rpx;
		background: var(--proto-primary);
		border-radius: 50%;
	}

	.privacy-point-title {
		display: block;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
	}

	.privacy-point-body {
		display: block;
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
		line-height: 1.55;
	}

	.form-error {
		display: block;
		margin: 0 32rpx;
		color: var(--proto-error);
		font-size: 21rpx;
		text-align: center;
	}

	.agreement-body {
		display: flex;
		flex-direction: column;
		gap: 14rpx;
		padding: 24rpx 0;
		color: var(--proto-muted);
	}

	.agreement-heading {
		color: var(--proto-text);
		font-weight: 750;
	}
</style>
