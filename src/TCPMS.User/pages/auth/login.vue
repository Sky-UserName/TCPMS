<template>
	<view class="proto-page proto-page-plain login-page">
		<ProtoHeader title="登录 TCPMS" theme="green" />
		<view class="login-content">
			<view class="login-logo">
				<image src="/static/logo.png" mode="aspectFit"></image>
			</view>
			<text class="login-title">登录 TCPMS</text>
			<text class="login-subtitle">登录后即可预订心仪民宿</text>

			<view v-if="status === 'success'" class="login-feedback success">登录成功，正在返回预订流程...</view>
			<view v-else-if="status === 'loading'" class="login-feedback">正在请求微信授权，请稍候...</view>
			<view v-else-if="status === 'error'" class="login-feedback error">授权失败，请检查权限后重新尝试</view>

			<button class="proto-primary-button login-button" hover-class="none" @tap="wechatLogin">
				{{ status === 'loading' ? '授权处理中...' : status === 'error' ? '重新授权' : '微信一键登录' }}
			</button>
			<button class="proto-secondary-button login-button" hover-class="none" @tap="phoneLogin">手机号授权</button>
			<button class="login-later" hover-class="none" @tap="browse">暂时不登录，先逛逛</button>

			<view class="login-benefits">
				<view v-for="item in benefits" :key="item.title" class="login-benefit">
					<view class="benefit-icon"><ProtoIcon :name="item.icon" :size="28" /></view>
					<view>
						<text class="benefit-title">{{ item.title }}</text>
						<text class="benefit-desc">{{ item.desc }}</text>
					</view>
				</view>
			</view>

			<text class="login-agreement">登录即代表同意 <text class="proto-link">《TCPMS用户协议》</text> 与 <text class="proto-link">《隐私政策》</text></text>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { setLoggedIn } from '@/common/app-store.js'
	import { miniappLogin } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				status: 'idle',
				benefits: [
					{ icon: 'calendar', title: '快速预订', desc: '保存入住人信息，减少重复填写' },
					{ icon: 'receipt', title: '订单可追踪', desc: '支付、确认、退款状态实时同步' },
					{ icon: 'shield', title: '入住更安心', desc: '获取门店导航与智能入住凭据' }
				]
			}
		},
		methods: {
			async wechatLogin() {
				if (this.status === 'loading') return
				this.status = 'loading'
				try {
					const result = await miniappLogin()
					setLoggedIn({
						name: result.displayName || '张三',
						phone: '138****8888'
					})
				} catch (error) {
					// Development preview remains usable when the local API is not running.
					setLoggedIn({
						name: '张三',
						phone: '138****8888'
					})
				}
				this.status = 'success'
				setTimeout(() => {
					uni.reLaunch({ url: '/pages/index/index' })
				}, 500)
			},
			phoneLogin() {
				uni.showModal({
					title: '手机号授权',
					content: '将使用微信授权获取手机号，用于订单通知和入住联系。',
					confirmText: '继续授权',
					success: (res) => {
						if (res.confirm) this.wechatLogin()
					}
				})
			},
			browse() {
				uni.reLaunch({ url: '/pages/index/index' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.login-page {
		padding-bottom: 40rpx;
	}

	.login-content {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 90rpx 30rpx 40rpx;
	}

	.login-logo {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 120rpx;
		height: 120rpx;
		color: var(--proto-primary);
		background: var(--proto-surface);
		border-radius: 32rpx;
		box-shadow: 0 12rpx 32rpx rgba(42, 169, 169, 0.14);
	}

	.login-logo image {
		width: 72rpx;
		height: 72rpx;
		border-radius: 18rpx;
	}

	.login-title {
		margin-top: 30rpx;
		color: var(--proto-text);
		font-size: 40rpx;
		font-weight: 800;
	}

	.login-subtitle {
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 23rpx;
	}

	.login-feedback {
		width: 100%;
		margin-top: 34rpx;
		padding: 18rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		font-size: 21rpx;
		text-align: center;
	}

	.login-feedback.success {
		color: #087654;
		background: #d9f7ea;
	}

	.login-feedback.error {
		color: var(--proto-error);
		background: #ffe3df;
	}

	.login-button {
		width: 100%;
		margin-right: 0;
		margin-left: 0;
	}

	.login-button:first-of-type {
		margin-top: 48rpx;
	}

	.login-later {
		margin-top: 22rpx;
		padding: 12rpx 20rpx;
		color: var(--proto-muted);
		background: transparent;
		font-size: 22rpx;
	}

	.login-benefits {
		width: 100%;
		margin-top: 56rpx;
		padding: 24rpx;
		background: var(--proto-surface);
		border-radius: 24rpx;
	}

	.login-benefit {
		display: flex;
		align-items: center;
		padding: 16rpx 0;
	}

	.login-benefit + .login-benefit {
		border-top: 1rpx solid #e3ebec;
	}

	.benefit-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 54rpx;
		height: 54rpx;
		margin-right: 16rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.benefit-title,
	.benefit-desc {
		display: block;
	}

	.benefit-title {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 700;
	}

	.benefit-desc {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.login-agreement {
		margin-top: 28rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}
</style>
