<template>
	<view class="proto-page proto-page-plain auth-page">
		<view class="auth-topbar">
			<view class="auth-brand">
				<view class="auth-brand-icon"><ProtoIcon name="user" tone="white" :size="24" /></view>
				<text>TCPMS 智选旅宿</text>
			</view>
		</view>

		<view class="auth-logo">
			<view class="logo-mark">
				<image src="/static/logo.png" mode="aspectFit"></image>
			</view>
			<text>TCPMS 智选云宿</text>
		</view>

		<view class="privacy-card">
			<view class="privacy-title-row">
				<view>
					<text class="privacy-title">欢迎使用 TCPMS</text>
					<text class="privacy-subtitle">多门店快捷预订 · 智能客房管理</text>
				</view>
				<text class="proto-pill info">安全合规保障</text>
			</view>

			<view class="privacy-intro">
				<view class="privacy-intro-icon"><ProtoIcon name="check" tone="white" :size="20" /></view>
				<text>为了向您提供<text class="strong">门店浏览、精准定位、房源预订、微信支付及极速退款</text>等核心服务，我们将依据合法、正当、必要的原则收集与使用您的相关信息。</text>
			</view>

			<view class="privacy-summary-title">
				<text>隐私保护要点概览</text>
				<text class="proto-link">可向下滑动阅读全文 ↓</text>
			</view>
			<scroll-view scroll-y class="privacy-scroll">
				<view v-for="item in privacyPoints" :key="item.title" class="privacy-point">
					<text class="privacy-point-dot"></text>
					<view>
						<text class="privacy-point-title">{{ item.index }}. {{ item.title }}</text>
						<text class="privacy-point-body">{{ item.body }}</text>
					</view>
				</view>
			</scroll-view>
		</view>

		<view class="proto-check" @tap="agreed = !agreed">
			<view class="proto-check-box" :class="{ checked: agreed }"><ProtoIcon v-if="agreed" name="check" tone="white" :size="22" /></view>
			<text>我已充分阅读并理解 <text class="proto-link" @tap.stop="showAgreement = true">《TCPMS用户协议》</text> 与 <text class="proto-link" @tap.stop="showAgreement = true">《隐私政策》</text>，并同意依法使用必要服务。</text>
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
					<text class="agreement-heading">位置权限原则</text>
					<text>定位仅用于附近门店排序，不是下单必填项。用户拒绝授权后仍可手动切换城市并浏览完整房源。</text>
					<text class="agreement-heading">入住人信息</text>
					<text>预订整套民宿或青年旅舍床位时需要核验真实姓名、联系电话和身份证件号，页面展示时默认脱敏。</text>
					<text class="agreement-heading">支付与退款</text>
					<text>交易通过微信官方支付完成，平台仅记录订单和支付流水，不保存银行卡密码。</text>
				</view>
				<button class="proto-primary-button" hover-class="none" @tap="showAgreement = false">已阅读并同意</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'

	export default {
		components: { ProtoIcon },
		data() {
			return {
				agreed: false,
				showAgreement: false,
				errorMessage: '',
				privacyPoints: [
					{
						index: 1,
						title: '地理位置与多门店查找',
						body: '仅在您明确授权后获取当前位置，用于智能推荐距离最近的直营门店及显示路线导航；若您拒绝授权，仍可手动切换城市。'
					},
					{
						index: 2,
						title: '入住人身份与预订信息',
						body: '根据旅馆业实名管理要求，预订整套民宿或青年旅舍床位时需核验真实姓名、联系电话与身份证件号。'
					},
					{
						index: 3,
						title: '微信安全支付与退款结算',
						body: '平台只记录订单编号与支付流水，不保存银行卡密码或敏感支付凭据。'
					},
					{
						index: 4,
						title: '您的权利与授权撤回',
						body: '您可以在我的页面查看、更正个人资料或注销账户，撤回权限后我们将停止采集后续信息。'
					}
				]
			}
		},
		methods: {
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
		width: 112rpx;
		height: 112rpx;
		margin-bottom: 18rpx;
		color: var(--proto-primary);
		background: #ffffff;
		border-radius: 28rpx;
		box-shadow: 0 10rpx 28rpx rgba(42, 169, 169, 0.12);
	}

	.logo-mark image {
		width: 62rpx;
		height: 62rpx;
		border-radius: 16rpx;
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

	.privacy-intro .strong {
		font-weight: 800;
	}

	.privacy-intro-icon,
	.load-error-icon {
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
