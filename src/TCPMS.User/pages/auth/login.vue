<template>
	<view class="proto-page proto-page-plain login-page">
		<view class="login-shell">
			<view class="login-brand">
				<view class="brand-mark"><image src="/static/logo.png" mode="aspectFit"></image></view>
				<view class="brand-copy">
					<text class="brand-title">TCPMS</text>
					<text class="brand-subtitle">智选旅宿</text>
				</view>
			</view>

			<view class="login-identity">
				<button class="identity-avatar" open-type="chooseAvatar" hover-class="none" @chooseavatar="handleChooseAvatar">
					<image v-if="avatarUrl" :src="avatarUrl" mode="aspectFill" />
					<image v-else src="/static/logo.png" mode="aspectFit" />
					<view class="identity-avatar-edit"><ProtoIcon name="edit" tone="white" :size="17" /></view>
				</button>
				<text class="identity-avatar-tip">{{ avatarUrl ? '点击可更换微信头像' : '点击获取微信头像' }}</text>
				<input
					v-model="nickname"
					class="identity-name"
					type="nickname"
					maxlength="32"
					placeholder="点击获取微信昵称"
					placeholder-style="color:#8aa8a9;font-size:32rpx;font-weight:600"
					confirm-type="done"
					:focus="nicknameFocus"
					@focus="handleNicknameFocus"
					@blur="syncNickname"
				/>
				<text class="identity-hint">头像和昵称均读取自你的微信资料</text>
				<text v-if="isDevtools" class="identity-dev-tip">开发者工具不显示「用微信昵称」，真机预览才会出现</text>
			</view>

			<view class="login-action-area">
				<button
					v-if="canTriggerPhone"
					class="login-button"
					:class="{ disabled: status === 'loading' }"
					open-type="getPhoneNumber"
					hover-class="none"
					:disabled="status === 'loading'"
					@getphonenumber="handlePhoneNumber"
				>
					{{ status === 'loading' ? '授权处理中...' : status === 'error' ? '重新授权登录' : '微信授权登录' }}
				</button>
				<button
					v-else
					class="login-button"
					:class="{ disabled: status === 'loading' }"
					hover-class="none"
					:disabled="status === 'loading'"
					@tap="validateAuthorization"
				>
					{{ status === 'loading' ? '授权处理中...' : status === 'error' ? '重新授权登录' : '微信授权登录' }}
				</button>
			</view>

			<view class="login-feedback" :class="status">
				<text v-if="status === 'success'">资料已授权，正在进入 TCPMS</text>
				<text v-else-if="status === 'loading'">正在连接微信，请稍候</text>
				<text v-else-if="status === 'error'">{{ errorMessage || '授权失败，请检查网络后重试' }}</text>
			</view>

			<view class="agreement-list">
				<view class="agreement-row" @tap="toggleAgreement('service')">
					<checkbox :checked="agreements.service" color="#148b83" />
					<text>我已阅读并同意 <text class="agreement-link">《TCPMS用户协议》</text> 与 <text class="agreement-link">《隐私政策》</text></text>
				</view>
				<view class="agreement-row" @tap="toggleAgreement('thirdParty')">
					<checkbox :checked="agreements.thirdParty" color="#148b83" />
					<text>我已阅读并同意 <text class="agreement-link">《第三方服务与会员注册条款》</text></text>
				</view>
			</view>

			<button class="login-later" hover-class="none" @tap="browse">暂不登录，先逛逛</button>
			<view class="login-footer-note"><ProtoIcon name="shield" :size="18" /><text>仅用于身份识别、订单联系和入住服务</text></view>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { getProfile, setLoggedIn } from '@/common/app-store.js'
	import { miniappLogin, uploadMiniappAvatar } from '@/common/api.js'

	export default {
		components: { ProtoIcon },
		data() {
			return {
				status: 'idle',
				phoneStatus: 'idle',
				nickname: '',
				avatarUrl: '',
				avatarTempPath: '',
				nicknameFocus: false,
				isDevtools: false,
				errorMessage: '',
				agreements: {
					service: false,
					thirdParty: false
				}
			}
		},
		computed: {
			canTriggerPhone() {
				return this.agreements.service &&
					this.agreements.thirdParty &&
					Boolean(String(this.nickname || '').trim()) &&
					this.status !== 'loading'
			}
		},
		onLoad() {
			this.prefillWechatProfile()
			try {
				this.isDevtools = uni.getSystemInfoSync()?.platform === 'devtools'
			} catch (error) {
				this.isDevtools = false
			}
		},
		methods: {
			// 微信已回收静默获取昵称头像的接口，这里只回填上一次从微信带过来的资料，避免重复选择。
			prefillWechatProfile() {
				const profile = getProfile()
				const name = String(profile?.name || '').trim()
				if (name && name !== '微信用户') this.nickname = name
				const avatar = String(profile?.avatar || '')
				const isTempPath = /^(wxfile:|https?:\/\/tmp\/)/i.test(avatar)
				if (!isTempPath && (/^https?:\/\//i.test(avatar) || avatar.startsWith('/uploads/'))) {
					this.avatarUrl = avatar
				}
			},
			toggleAgreement(key) {
				this.agreements[key] = !this.agreements[key]
				this.errorMessage = ''
			},
			handleChooseAvatar(event) {
				const url = event?.detail?.avatarUrl || ''
				if (!url) {
					this.errorMessage = '没有取到微信头像，请再点一次'
					return
				}
				this.avatarUrl = url
				this.avatarTempPath = url
				this.errorMessage = ''
			},
			handleNicknameFocus() {
				this.nicknameFocus = true
			},
			syncNickname(event) {
				this.nicknameFocus = false
				const value = event?.detail?.value
				// 开发者工具 blur 可能不回传 value，此时保留 v-model 已同步的内容。
				if (typeof value === 'string') this.nickname = value.trim()
			},
			focusNickname() {
				this.nicknameFocus = false
				this.$nextTick(() => {
					this.nicknameFocus = true
				})
			},
			validateAuthorization() {
				if (!this.agreements.service || !this.agreements.thirdParty) {
					this.errorMessage = '请先阅读并同意相关协议'
					this.status = 'error'
					return
				}
				if (!String(this.nickname || '').trim()) {
					this.errorMessage = '请点击昵称，从键盘上方选择你的微信昵称'
					this.status = 'error'
					this.focusNickname()
					return
				}
				this.errorMessage = '请再次点击授权登录，并在微信弹窗中允许获取手机号'
				this.status = 'idle'
			},
			async handlePhoneNumber(event) {
				if (this.status === 'loading') return
				if (!this.agreements.service || !this.agreements.thirdParty) {
					this.validateAuthorization()
					return
				}
				const phoneCode = event?.detail?.code
				if (!phoneCode) {
					this.errorMessage = event?.detail?.errMsg || '你已取消手机号授权，登录未完成'
					this.status = 'error'
					return
				}
				const nickname = String(this.nickname || '').trim()
				if (!nickname) {
					this.errorMessage = '请点击昵称，从键盘上方选择你的微信昵称'
					this.status = 'error'
					this.focusNickname()
					return
				}
				this.status = 'loading'
				this.errorMessage = ''
				let profile = null
				try {
					// 头像临时路径不能直接入库，先登录拿到 token，再上传换成正式地址。
					const result = await miniappLogin({ nickname, phoneCode })
					profile = result?.profile || {}
					if (!profile.id) throw new Error('登录返回的用户资料不完整，请稍后重试')
					setLoggedIn({
						uid: profile.id,
						name: profile.nickname || nickname || result.displayName || '微信用户',
						phone: profile.phoneNumber || '',
						avatar: profile.avatarUrl || ''
					})
					if (this.avatarTempPath) {
						try {
							const uploaded = await uploadMiniappAvatar(this.avatarTempPath)
							if (uploaded?.avatarUrl) {
								profile = { ...profile, ...uploaded }
								this.avatarTempPath = ''
								this.avatarUrl = uploaded.avatarUrl
								setLoggedIn({
									uid: uploaded.id || profile.id,
									name: uploaded.nickname || profile.nickname || nickname || '微信用户',
									phone: uploaded.phoneNumber || profile.phoneNumber || '',
									avatar: uploaded.avatarUrl
								})
							}
						} catch (uploadError) {
							uni.showToast({ title: '头像上传失败，可稍后重试', icon: 'none' })
						}
					}
					this.phoneStatus = profile.phoneNumber ? 'success' : 'idle'
				} catch (error) {
					console.error('[TCPMS] miniapp login failed', error)
					this.errorMessage = error?.message || error?.errMsg || '微信授权失败，请稍后重试'
					this.status = 'error'
					return
				}
				this.status = 'success'
				this.redirectAfterLogin()
			},
			redirectAfterLogin() {
				setTimeout(() => {
					uni.reLaunch({ url: '/pages/index/index' })
				}, 500)
			},
			browse() {
				uni.reLaunch({ url: '/pages/index/index' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.login-page {
		min-height: 100vh;
		padding: 48rpx 28rpx 38rpx;
		background: #f5fbfb;
	}

	.login-shell {
		display: flex;
		align-items: center;
		flex-direction: column;
		min-height: calc(100vh - 86rpx);
		padding: 22rpx 24rpx 34rpx;
		background: #ffffff;
		border: 1rpx solid #e0efef;
		border-radius: 28rpx;
		box-shadow: 0 18rpx 44rpx rgba(17, 111, 111, 0.08);
	}

	.login-brand {
		display: flex;
		align-items: center;
		align-self: flex-start;
	}

	.brand-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 64rpx;
		height: 64rpx;
		background: #e7f8f7;
		border: 1rpx solid #ccebe8;
		border-radius: 18rpx;
	}

	.brand-mark image {
		width: 42rpx;
		height: 42rpx;
		border-radius: 12rpx;
	}

	.brand-copy {
		display: flex;
		flex-direction: column;
		margin-left: 14rpx;
	}

	.brand-title {
		color: #123e43;
		font-size: 28rpx;
		font-weight: 850;
		letter-spacing: 1rpx;
	}

	.brand-subtitle {
		margin-top: 4rpx;
		color: #6b8688;
		font-size: 18rpx;
	}

	.login-identity {
		display: flex;
		align-items: center;
		flex-direction: column;
		margin-top: 78rpx;
		text-align: center;
	}

	.identity-avatar {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 176rpx;
		height: 176rpx;
		margin: 0;
		padding: 0;
		background: #edf8f8;
		border: 8rpx solid #ffffff;
		border-radius: 50%;
		box-shadow: 0 10rpx 28rpx rgba(20, 139, 131, 0.16), 0 0 0 2rpx #cce9e7;
		overflow: visible;
	}

	.identity-avatar::after {
		display: none;
	}

	.identity-avatar > image {
		width: 160rpx;
		height: 160rpx;
		border-radius: 50%;
	}

	.identity-avatar-edit {
		position: absolute;
		right: -2rpx;
		bottom: 2rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 42rpx;
		height: 42rpx;
		color: #ffffff;
		background: #148b83;
		border: 4rpx solid #ffffff;
		border-radius: 50%;
	}

	.identity-avatar-tip {
		margin-top: 14rpx;
		color: #3f8b85;
		font-size: 20rpx;
		font-weight: 600;
	}

	.identity-name {
		width: 300rpx;
		margin-top: 16rpx;
		padding: 0;
		color: #163b40;
		font-size: 38rpx;
		font-weight: 850;
		line-height: 1.25;
		text-align: center;
	}

	.identity-hint {
		margin-top: 12rpx;
		color: #759091;
		font-size: 19rpx;
	}

	.identity-dev-tip {
		margin-top: 8rpx;
		color: #b07a2a;
		font-size: 18rpx;
		line-height: 1.5;
	}

	.login-action-area {
		width: 100%;
		margin-top: 72rpx;
	}

	.login-button,
	.phone-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 92rpx;
		margin: 0;
		padding: 0 28rpx;
		border-radius: 46rpx;
		font-size: 29rpx;
		font-weight: 750;
		line-height: 92rpx;
	}

	.login-button {
		color: #ffffff;
		background: #148b83;
		box-shadow: 0 12rpx 24rpx rgba(20, 139, 131, 0.2);
	}

	.login-button::after,
	.phone-button::after {
		display: none;
	}

	.login-button.disabled {
		color: #ffffff;
		background: #a9c9c7;
		box-shadow: none;
	}

	.phone-button {
		margin-top: 18rpx;
		color: #147870;
		background: #e9f7f6;
		border: 1rpx solid #c8e8e5;
	}

	.login-feedback {
		width: 100%;
		min-height: 42rpx;
		margin-top: 22rpx;
		color: #6e8585;
		font-size: 20rpx;
		line-height: 42rpx;
		text-align: center;
	}

	.login-feedback.error {
		color: #bc554f;
	}

	.login-feedback.success {
		color: #167b73;
	}

	.agreement-list {
		width: 100%;
		margin-top: 42rpx;
	}

	.agreement-row {
		display: flex;
		align-items: flex-start;
		padding: 11rpx 0;
		color: #687c7d;
		font-size: 21rpx;
		line-height: 1.55;
	}

	.agreement-row checkbox {
		flex: 0 0 auto;
		transform: scale(0.78);
		transform-origin: left top;
	}

	.agreement-row > text {
		flex: 1;
		min-width: 0;
		margin-left: 2rpx;
	}

	.agreement-link {
		color: #148b83;
	}

	.login-later {
		margin: 30rpx 0 0;
		padding: 12rpx 22rpx;
		color: #789091;
		background: transparent;
		font-size: 21rpx;
	}

	.login-later::after {
		display: none;
	}

	.login-footer-note {
		display: flex;
		align-items: center;
		justify-content: center;
		margin-top: auto;
		padding-top: 44rpx;
		color: #91a5a5;
		font-size: 18rpx;
	}

	.login-footer-note .proto-icon {
		margin-right: 6rpx;
		color: #5d9f99;
	}
</style>
