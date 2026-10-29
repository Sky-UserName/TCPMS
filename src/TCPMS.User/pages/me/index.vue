<template>
	<view class="proto-page me-page">
		<view class="me-profile-hero" :style="{ paddingTop: statusBarHeightRpx + 12 + 'rpx' }">
			<text class="me-profile-page-title">我的</text>
			<view class="me-profile-row">
				<view class="me-avatar-wrap">
					<image v-if="loggedIn" :src="avatar" mode="aspectFill" class="me-avatar"></image>
					<view v-else class="me-avatar guest"><ProtoIcon name="user" tone="white" :size="72" /></view>
					<view class="avatar-edit"><ProtoIcon name="edit" tone="white" :size="19" /></view>
				</view>
				<view class="me-profile-copy" @tap="goLogin">
					<text class="me-profile-name">{{ loggedIn ? profile.name : '去登录' }}</text>
					<view class="me-profile-sub"><ProtoIcon name="phone" tone="white" :size="20" /><text>{{ loggedIn ? (profile.phone || '完善手机号') : '登录后享受更多服务' }}</text></view>
				</view>
				<button class="me-home-button" hover-class="none" @tap="loggedIn ? showSettings() : goLogin()">我的主页</button>
			</view>
			<view class="me-profile-fade" aria-hidden="true"></view>
		</view>

		<view class="me-panel order-panel">
			<view class="me-panel-heading"><view class="me-heading-title"><text>我的订单</text><text class="me-heading-caption">{{ ordersSyncError ? '同步失败' : '实时同步' }}</text></view><view @tap="goOrders('all')"><text>查看更多</text><ProtoIcon name="chevron-right" :size="22" /></view></view>
			<view class="me-order-summary"><view><text class="me-order-total">{{ orders.length }}</text><text>笔订单</text></view><text>{{ ordersSyncError ? '当前显示本机缓存' : '状态实时更新，入住更安心' }}</text></view>
			<view class="me-order-grid">
				<view v-for="item in orderShortcuts" :key="item.key" class="me-order-entry" @tap="goOrders(item.key)">
					<view class="me-order-icon"><ProtoIcon :name="item.icon" :size="36" /></view>
					<text>{{ item.label }}</text>
				</view>
			</view>
		</view>

		<view v-if="merchantPanelVisible" class="me-panel merchant-panel">
			<view class="me-panel-heading merchant-panel-heading">
				<view class="me-heading-title"><text>民宿管理</text><text class="me-heading-caption">{{ merchant.storeName }}</text></view>
				<view class="merchant-store-switch" @tap="handleMerchantStore"><text>{{ merchant.role || '店主' }}</text><ProtoIcon name="chevron-down" :size="22" /></view>
			</view>
			<view class="merchant-summary">
				<view class="merchant-summary-copy"><text class="merchant-summary-title">{{ merchant.storeName }}</text><text class="merchant-summary-desc">{{ merchantSummaryText }}</text></view>
				<view class="merchant-summary-status" :class="merchant.status"><ProtoIcon :name="merchantStatusIcon" :size="26" /><text>{{ merchantStatusLabel }}</text></view>
			</view>
			<view class="merchant-grid">
				<view v-for="item in merchantEntries" :key="item.key" class="merchant-entry" :class="{ disabled: item.disabled }" @tap="handleMerchantEntry(item)">
					<view class="merchant-entry-icon"><image v-if="item.iconUrl" :src="item.iconUrl" mode="aspectFit" /><ProtoIcon v-else :name="item.icon" :size="38" /></view>
					<text class="merchant-entry-title">{{ item.title }}</text>
					<text v-if="item.disabled" class="merchant-entry-status">待开放</text>
				</view>
			</view>
			<view v-if="merchantMenuError" class="merchant-menu-error" @tap="loadMerchantEntries">管理入口同步失败，点击重试</view>
		</view>

		<view class="me-panel function-panel">
			<view class="me-panel-heading"><text>我的功能</text></view>
			<view class="me-function-grid">
				<view v-for="item in visibleFeatureEntries" :key="item.title" class="me-function-entry" @tap="handleFeature(item)">
					<view class="me-function-icon"><ProtoIcon :name="item.icon" :size="38" /></view>
					<text>{{ item.title }}</text>
				</view>
			</view>
		</view>

		<ProtoBottomNav active="me" variant="root" />
	</view>
</template>

<script>
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, goTab, prototypeImages, showToast } from '@/common/prototype.js'
	import { getApiToken, getGuests, getMerchantProfile, getOrders, getProfile, isLoggedIn, logout, saveMerchantProfile, setLoggedIn } from '@/common/app-store.js'
	import { IS_DEV_BUILD } from '@/common/env.js'
	import { fetchMerchantPermissionMenus, fetchMerchantProfile, fetchMiniappProfile, fetchMySummary, fetchRemoteOrders } from '@/common/api.js'
	import { scanAndVerifyOrder } from '@/common/verify.js'

	// 个人中心「民宿管理」不展示的入口：发布房源、订阅消息。
	const HIDDEN_MERCHANT_MENU_KEYS = ['publish', 'subscriptions']
	const HIDDEN_MERCHANT_MENU_ROUTES = ['/pages/merchant/publish', '/pages/merchant/messages']
	// 订单列表是 tabBar 页，switchTab 不支持带参数，用一次性缓存把要看的分类传过去。
	const ORDER_TAB_STORAGE_KEY = 'tcpms.orderListTab'

	export default {
		components: { ProtoBottomNav, ProtoIcon },
		data() {
			let statusBarHeightRpx = 0
			try {
				const info = uni.getSystemInfoSync()
				statusBarHeightRpx = Math.round((Number(info.statusBarHeight) || 0) * 750 / (Number(info.windowWidth) || 375))
			} catch (error) {
				statusBarHeightRpx = 0
			}
			return {
				statusBarHeightRpx,
				loggedIn: false,
				profile: {
					name: '微信用户',
					uid: '',
					phone: '',
					avatar: prototypeImages.avatar
				},
				avatar: prototypeImages.avatar,
				orders: [],
				ordersSyncError: false,
				merchant: getMerchantProfile(),
				guests: [],
				orderShortcuts: [
					{ key: 'all', label: '全部', icon: 'wallet', badge: '' },
					{ key: 'pay', label: '待支付', icon: 'clock', badge: '' },
					{ key: 'stay', label: '可使用', icon: 'receipt', badge: '' },
					{ key: 'done', label: '已使用', icon: 'check-circle', badge: '' },
					{ key: 'refund', label: '取消/退款', icon: 'refresh', badge: '' }
				],
				featureEntries: [
					{ title: '入驻申请', icon: 'store', action: 'apply' },
					{ title: '在线客服', icon: 'support', route: '/pages/service/contact' },
					...(IS_DEV_BUILD ? [{ title: '清除本地缓存', icon: 'refresh', action: 'clearLocal' }] : []),
					{ title: '退出登录', icon: 'refresh', action: 'logout' }
				],
				merchantEntries: [],
				merchantMenuError: false,
				travelEntries: [
					{ title: '常用入住人', desc: '添加常用入住人，预订时免重复输入', icon: 'group', route: '/pages/me/guests' },
					{ title: '我的发票抬头', desc: '增值税电子普通/专用发票抬头管理', icon: 'document', action: 'invoice' }
				],
				serviceEntries: [
					{ title: '官方微信客服', desc: '在线解答预订变更、房费退还与入住咨询', icon: 'support', route: '/pages/service/contact' },
					{ title: '联系直营门店', desc: '快速呼叫前台管家、停车指引与房卡取用', icon: 'store', route: '/pages/service/contact' },
					{ title: '出行攻略与旅宿公告', desc: '探索本地私房指南与时令特惠活动', icon: 'book', route: '/pages/content/detail' }
				],
				policyEntries: [
					{ title: '用户服务协议', desc: '', icon: 'document', route: '/pages/rule/index' },
					{ title: '用户隐私手册', desc: '', icon: 'shield', route: '/pages/rule/index?tab=privacy' },
					{ title: '关于 TCPMS 智选旅宿', desc: '', icon: 'info', version: 'v2.4.1', message: 'TCPMS 智选旅宿小程序' }
				]
			}
		},
		computed: {
			merchantPanelVisible() {
				return Boolean(this.loggedIn && this.merchant.enabled && this.merchant.status === 'approved')
			},
			merchantSummaryText() {
				if (this.merchant.status === 'pending') return '资料审核中 · 审核期间门店和所有房间暂不展示'
				if (this.merchant.status === 'rejected') return '资料未通过 · 请修改资料后重新提交'
				return `已审核 · ${this.merchant.roomCount || 0} 间房源在管理`
			},
			merchantStatusLabel() {
				if (this.merchant.status === 'pending') return '审核中'
				if (this.merchant.status === 'rejected') return '未通过'
				return '经营中'
			},
			merchantStatusIcon() {
				if (this.merchant.status === 'pending') return 'clock'
				if (this.merchant.status === 'rejected') return 'info'
				return 'verified'
			},
			visibleFeatureEntries() {
				const merchantApproved = this.loggedIn && this.merchant.enabled && this.merchant.status === 'approved'
				return merchantApproved ? this.featureEntries.filter((item) => item.action !== 'apply') : this.featureEntries
			},
			todayOrder() {
				return this.orders.find((order) => order.statusKey === 'stay' || order.statusKey === 'pay')
			},
			todayOrderText() {
				return this.todayOrder ? `当前预订：${this.todayOrder.room}` : '暂无进行中的预订'
			}
		},
		async onShow() {
			uni.hideTabBar({ animation: false, fail: () => {} })
			this.refreshData()
			await this.refreshMiniappProfile()
			await this.refreshAccount()
			this.loadMerchantEntries()
			this.refreshRemoteOrders()
		},
		methods: {
			async refreshAccount() {
				if (!getApiToken()) return
				try {
					const [profile, summary] = await Promise.all([fetchMerchantProfile(), fetchMySummary()])
					if (profile) {
						this.merchant = {
							...this.merchant,
							enabled: Boolean(profile.enabled),
							status: profile.status || 'unapplied',
							storeId: profile.storeId || '',
							storeName: profile.storeName || '',
							role: profile.role || '',
							roomCount: Number(profile.roomCount || 0),
							pendingOrderCount: Number(profile.pendingOrderCount || 0)
						}
						saveMerchantProfile(this.merchant)
					}
					if (summary) {
						const guestCount = Number(summary.guestCount || 0)
						const guestEntry = this.travelEntries.find((item) => item.title === '常用入住人')
						if (guestEntry) {
							guestEntry.desc = guestCount
								? `已登记${guestCount}位常入住人，预订时免重复输入`
								: '添加常用入住人，预订时免重复输入'
						}
					}
				} catch (error) {
					// 接口异常时保留上一次缓存，不阻塞个人中心其他数据。
				}
			},
			async refreshMiniappProfile() {
				if (!getApiToken()) return
				try {
					const remoteProfile = await fetchMiniappProfile()
					if (!remoteProfile?.id) return
					setLoggedIn({
						uid: remoteProfile.id,
						name: remoteProfile.nickname || this.profile.name || '微信用户',
						phone: remoteProfile.phoneNumber || this.profile.phone || '',
						avatar: remoteProfile.avatarUrl || this.profile.avatar || prototypeImages.avatar
					})
					this.refreshData()
				} catch (error) {
					// apiRequest handles expired tokens and redirects to the login page.
				}
			},
			async refreshRemoteOrders() {
				if (!this.loggedIn) return
				try {
					this.refreshData(await fetchRemoteOrders())
					this.ordersSyncError = false
				} catch (error) {
					this.ordersSyncError = true
				}
			},
			async loadMerchantEntries() {
				this.merchantMenuError = false
				try {
					const items = await fetchMerchantPermissionMenus()
					const entries = items
						.filter((item) => item.key && item.title && /^\/pages\/[A-Za-z0-9/_-]+$/.test(item.route || ''))
						.filter((item) => !HIDDEN_MERCHANT_MENU_KEYS.includes(item.key) && !HIDDEN_MERCHANT_MENU_ROUTES.includes(item.route))
						.map((item) => ({
							...item,
							icon: item.iconKey || 'store',
							iconUrl: item.iconUrl || '',
							route: item.key === 'property' || item.route === '/pages/merchant/index' ? '/pages/merchant/apply' : item.route
						}))
					if (!entries.some((item) => item.key === 'notice' || item.route === '/pages/merchant/notice')) {
						const settingsIndex = entries.findIndex((item) => item.key === 'settings')
						const noticeEntry = { key: 'notice', title: '预定须知', icon: 'document', iconUrl: '', route: '/pages/merchant/notice' }
						entries.splice(settingsIndex >= 0 ? settingsIndex + 1 : entries.length, 0, noticeEntry)
					}
					this.merchantEntries = entries
				} catch (error) {
					this.merchantEntries = []
					this.merchantMenuError = true
				}
			},
			refreshData(remoteOrders = null) {
				this.profile = getProfile()
				this.loggedIn = isLoggedIn()
				this.avatar = this.profile.avatar || prototypeImages.avatar
				this.orders = Array.isArray(remoteOrders) ? remoteOrders : getOrders()
				this.merchant = getMerchantProfile()
				this.guests = getGuests()
				this.orderShortcuts = [
					{ key: 'all', label: '全部', icon: 'wallet', badge: String(this.orders.length) },
					{ key: 'pay', label: '待支付', icon: 'clock', badge: String(this.orders.filter((order) => order.statusKey === 'pay').length) },
					{ key: 'stay', label: '可使用', icon: 'receipt', badge: String(this.orders.filter((order) => order.statusKey === 'stay').length) },
					{ key: 'done', label: '已使用', icon: 'check-circle', badge: String(this.orders.filter((order) => order.statusKey === 'done').length) },
					{ key: 'refund', label: '取消/退款', icon: 'refresh', badge: String(this.orders.filter((order) => ['refund', 'cancel'].includes(order.statusKey)).length) }
				]
			},
			handleFeature(item) {
				if (item.route) {
					goPage(item.route)
					return
				}
				if (item.action === 'logout') {
					if (!this.loggedIn) {
						showToast('当前未登录')
						return
					}
					uni.showModal({
						title: '退出登录',
						content: '退出后需要重新进行微信授权登录',
						confirmText: '退出',
						success: (result) => {
							if (!result.confirm) return
							logout()
							uni.reLaunch({ url: '/pages/auth/login' })
						}
					})
					return
				}
				if (item.action === 'apply') {
					this.applyMerchant()
					return
				}
				if (item.action === 'clearLocal') {
					uni.showModal({
						title: '清除本地缓存',
						content: '会清空本地订单、收藏、常用入住人和登录状态，服务端数据不受影响。',
						confirmText: '清除',
						success: (result) => {
							if (!result.confirm) return
							try {
								uni.clearStorageSync()
							} catch (error) {
								// 清缓存失败时保持页面不变，避免误导用户。
							}
							uni.reLaunch({ url: '/pages/index/index' })
						}
					})
					return
				}
				showToast('功能即将开放')
			},
			applyMerchant() {
				if (!this.loggedIn) {
					this.goLogin()
					return
				}
				goPage('/pages/merchant/apply')
			},
			handleMerchantEntry(item) {
				if (item.disabled) {
					showToast('该功能将在商家服务完善后开放')
					return
				}
				// 扫码核销已改为直接唤起微信扫码，不再进入单独的核销页。
				if (item.key === 'verify' || item.route === '/pages/merchant/verify') {
					scanAndVerifyOrder()
					return
				}
				if (item.route) {
					goPage(item.route)
				}
			},
			handleMerchantStore() {
				uni.showActionSheet({
					itemList: [this.merchant.storeName || '当前民宿'],
					fail: () => {}
				})
			},
			handleEntry(item) {
				if (!this.loggedIn && item.route) {
					this.goLogin()
					return
				}
				if (item.route) {
					goPage(item.route)
					return
				}
				if (item.action === 'invoice') {
					uni.showModal({
						title: '发票抬头',
						content: '离店后打开对应订单详情，即可提交电子发票申请。发票将发送至您填写的邮箱。',
						showCancel: false
					})
					return
				}
				showToast(item.message || '暂无可用信息')
			},
			// 点哪个分类（待支付/可使用…）就带着它进订单页，而不是永远打开「全部」。
			goOrders(tab = 'all') {
				try {
					uni.setStorageSync(ORDER_TAB_STORAGE_KEY, String(tab || 'all'))
				} catch (error) {
					// 缓存写失败不影响跳转，订单页会退回「全部」。
				}
				goTab('/pages/order/list')
			},
			goTodayOrder() {
				if (this.todayOrder) {
					goPage(`/pages/order/detail?id=${this.todayOrder.id}`)
					return
				}
				goTab('/pages/store/list')
			},
			goLogin() {
				goPage('/pages/auth/login')
			},
			copyProfileCode() {
				uni.setClipboardData({
					data: `TCPMS会员 UID ${this.profile.uid || '未登录'}`,
					success: () => showToast('会员信息已复制')
				})
			},
			showSettings() {
				uni.showModal({
					title: '个人设置',
					content: '当前账号资料由微信授权提供，如需修改请联系官方客服。',
					showCancel: false
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.me-page {
		padding-bottom: 136rpx;
		padding-bottom: calc(136rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(136rpx + env(safe-area-inset-bottom));
	}


</style>

<style lang="scss" scoped>
	.me-page {
		padding-bottom: 150rpx;
		background: #ffffff;
	}

	.me-profile-hero {
		position: relative;
		overflow: hidden;
		padding-right: 24rpx;
		padding-left: 24rpx;
		background: #28a0a0;
	}

	.me-profile-fade {
		height: 68rpx;
		margin: 0 -24rpx;
		background: linear-gradient(180deg, #28a0a0 0%, #ffffff 100%);
	}

	.me-profile-page-title {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 72rpx;
		margin-bottom: 12rpx;
		color: #ffffff;
		font-size: 34rpx;
		font-weight: 750;
		line-height: 1;
		text-shadow: 0 2rpx 8rpx rgba(0, 61, 80, 0.18);
	}

	.me-profile-row {
		display: flex;
		align-items: center;
		gap: 18rpx;
		min-height: 146rpx;
	}

	.me-avatar-wrap {
		position: relative;
		flex: 0 0 auto;
		width: 116rpx;
		height: 116rpx;
	}

	.me-avatar {
		width: 116rpx;
		height: 116rpx;
		border: 6rpx solid rgba(255, 255, 255, 0.76);
		border-radius: 50%;
		box-shadow: 0 8rpx 24rpx rgba(0, 57, 88, 0.18);
	}

	.me-avatar.guest {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 116rpx;
		height: 116rpx;
		color: rgba(255, 255, 255, 0.92);
		background: rgba(4, 58, 89, 0.24);
		border: 6rpx solid rgba(255, 255, 255, 0.76);
		border-radius: 50%;
		box-shadow: 0 8rpx 24rpx rgba(0, 57, 88, 0.18);
	}

	.avatar-edit {
		position: absolute;
		right: -4rpx;
		bottom: -2rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 36rpx;
		height: 36rpx;
		color: #ffffff;
		background: #168f9c;
		border: 3rpx solid rgba(255, 255, 255, 0.9);
		border-radius: 50%;
		box-shadow: 0 4rpx 12rpx rgba(0, 57, 88, 0.2);
	}

	.me-profile-copy {
		flex: 1;
		min-width: 0;
		padding-top: 2rpx;
	}

	.me-profile-name {
		display: block;
		overflow: hidden;
		color: #ffffff;
		font-size: 36rpx;
		font-weight: 850;
		line-height: 1.2;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.me-profile-sub {
		display: flex;
		align-items: center;
		margin-top: 14rpx;
		overflow: hidden;
		color: rgba(255, 255, 255, 0.82);
		font-size: 20rpx;
		line-height: 1.35;
	}

	.me-profile-sub .proto-icon {
		flex: 0 0 auto;
		margin-right: 7rpx;
	}

	.me-profile-sub > text {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.me-home-button {
		flex: 0 0 auto;
		width: 132rpx;
		height: 58rpx;
		margin: 0;
		padding: 0 10rpx;
		color: #087c87;
		background: rgba(255, 255, 255, 0.94);
		border: 1rpx solid rgba(255, 255, 255, 0.98);
		border-radius: 29rpx;
		font-size: 21rpx;
		font-weight: 750;
		line-height: 56rpx;
		box-shadow: 0 6rpx 14rpx rgba(0, 57, 88, 0.12);
	}

	.me-home-button::after {
		display: none;
	}

	.me-panel {
		margin: 18rpx 24rpx 0;
		padding: 22rpx 26rpx 24rpx;
		background: #ffffff;
		border: 1rpx solid #e0e7ed;
		border-radius: 20rpx;
		box-shadow: 0 6rpx 18rpx rgba(21, 73, 112, 0.05);
	}

	.order-panel {
		margin-top: 0;
	}

	.me-panel-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: #151f28;
		font-size: 31rpx;
		font-weight: 850;
	}

	.me-heading-title {
		display: flex;
		align-items: baseline;
		gap: 10rpx;
	}

	.me-heading-caption {
		color: var(--proto-primary-dark);
		font-size: 17rpx;
		font-weight: 500;
	}

	.me-panel-heading > text::before {
		display: inline-block;
		width: 8rpx;
		height: 31rpx;
		margin-right: 12rpx;
		background: var(--proto-primary);
		border-radius: 6rpx;
		content: '';
		vertical-align: -5rpx;
	}

	.me-panel-heading > .me-heading-title {
		color: #151f28;
		font-size: 31rpx;
		font-weight: 850;
	}

	.me-panel-heading > .me-heading-title > text:first-child::before {
		display: inline-block;
		width: 8rpx;
		height: 31rpx;
		margin-right: 12rpx;
		background: var(--proto-primary);
		border-radius: 6rpx;
		content: '';
		vertical-align: -5rpx;
	}

	.me-panel-heading > view {
		display: flex;
		align-items: center;
		color: #8b969f;
		font-size: 22rpx;
		font-weight: 400;
	}

	.me-panel-heading > .me-heading-title > .me-heading-caption {
		color: var(--proto-primary-dark);
		font-size: 17rpx;
		font-weight: 500;
	}

	.me-order-grid {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		margin-top: 28rpx;
	}

	.me-order-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 16rpx;
		padding: 14rpx 16rpx;
		color: #75828c;
		background: #eef9fb;
		border-radius: 14rpx;
		font-size: 18rpx;
	}

	.me-order-summary > view {
		display: flex;
		align-items: baseline;
		gap: 6rpx;
		color: #70808b;
	}

	.me-order-total {
		color: var(--proto-primary-dark);
		font-size: 29rpx;
		font-weight: 850;
	}

	.me-order-entry,
	.me-function-entry {
		display: flex;
		align-items: center;
		flex-direction: column;
		min-width: 0;
		color: #5d6873;
		font-size: 22rpx;
		text-align: center;
	}

	.me-order-entry {
		width: 20%;
	}

	.me-order-icon,
	.me-function-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		margin-bottom: 12rpx;
		color: var(--proto-primary-dark);
		background: #eef9fb;
		border-radius: 18rpx;
	}

	.me-order-entry > text,
	.me-function-entry > text {
		max-width: 126rpx;
		line-height: 1.25;
	}

	.function-panel {
		padding-bottom: 30rpx;
	}

	.merchant-panel {
		padding-bottom: 28rpx;
		background: #fafdfe;
		border-color: #cfe7ea;
	}

	.merchant-panel-heading {
		padding-bottom: 2rpx;
	}

	.merchant-store-switch {
		display: flex;
		align-items: center;
		gap: 4rpx;
		max-width: 220rpx;
		padding: 10rpx 12rpx 10rpx 16rpx;
		color: var(--proto-primary-dark);
		background: #eaf8f8;
		border-radius: 14rpx;
		font-size: 19rpx;
		font-weight: 650;
	}

	.merchant-store-switch > text {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 18rpx;
		padding: 18rpx 18rpx;
		background: #eaf8f8;
		border-radius: 16rpx;
	}

	.merchant-summary-copy {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.merchant-summary-title {
		overflow: hidden;
		color: #193338;
		font-size: 24rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.merchant-summary-desc {
		margin-top: 7rpx;
		color: #6b8389;
		font-size: 18rpx;
	}

	.merchant-summary-status {
		display: flex;
		align-items: center;
		gap: 5rpx;
		flex: 0 0 auto;
		padding: 8rpx 12rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border-radius: 20rpx;
		font-size: 18rpx;
		font-weight: 700;
	}

	.merchant-summary-status.pending {
		color: #8a5a12;
		background: #fff7e8;
	}

	.merchant-summary-status.rejected {
		color: #a13f3f;
		background: #fff0f0;
	}

	.merchant-grid {
		display: flex;
		flex-wrap: wrap;
		margin-top: 24rpx;
		row-gap: 26rpx;
	}

	.merchant-entry {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: 25%;
		min-height: 126rpx;
		color: #536c73;
		font-size: 19rpx;
		text-align: center;
	}

	.merchant-entry-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 68rpx;
		height: 68rpx;
		margin-bottom: 10rpx;
		color: var(--proto-primary-dark);
		background: #e9f7f7;
		border: 1rpx solid #d3eeee;
		border-radius: 20rpx;
	}

	.merchant-entry-icon image {
		width: 42rpx;
		height: 42rpx;
	}

	.merchant-menu-error {
		margin-top: 14rpx;
		color: #8a4b13;
		font-size: 18rpx;
		text-align: center;
	}

	.merchant-entry-title {
		max-width: 138rpx;
		line-height: 1.3;
	}

	.merchant-entry-status {
		margin-top: 4rpx;
		color: #91a2a7;
		font-size: 16rpx;
	}

	.merchant-entry.disabled {
		opacity: 0.68;
	}

	.me-function-grid {
		display: flex;
		flex-wrap: wrap;
		margin-top: 28rpx;
		row-gap: 30rpx;
	}

	.me-function-entry {
		width: 33.3333%;
	}

	.me-function-entry:last-child {
		margin-top: 2rpx;
	}
</style>
