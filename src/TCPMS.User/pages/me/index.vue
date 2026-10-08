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
			<view class="me-panel-heading"><view class="me-heading-title"><text>我的订单</text><text class="me-heading-caption">实时同步</text></view><view @tap="goOrders"><text>查看更多</text><ProtoIcon name="chevron-right" :size="22" /></view></view>
			<view class="me-order-summary"><view><text class="me-order-total">{{ orders.length }}</text><text>笔订单</text></view><text>状态实时更新，入住更安心</text></view>
			<view class="me-order-grid">
				<view v-for="item in orderShortcuts" :key="item.key" class="me-order-entry" @tap="goOrders">
					<view class="me-order-icon"><ProtoIcon :name="item.icon" :size="36" /></view>
					<text>{{ item.label }}</text>
				</view>
			</view>
		</view>

		<view v-if="loggedIn && merchant.enabled && merchant.status === 'approved'" class="me-panel merchant-panel">
			<view class="me-panel-heading merchant-panel-heading">
				<view class="me-heading-title"><text>民宿管理</text><text class="me-heading-caption">{{ merchant.storeName }}</text></view>
				<view class="merchant-store-switch" @tap="handleMerchantStore"><text>{{ merchant.role || '店主' }}</text><ProtoIcon name="chevron-down" :size="22" /></view>
			</view>
			<view class="merchant-summary">
				<view class="merchant-summary-copy"><text class="merchant-summary-title">{{ merchant.storeName }}</text><text class="merchant-summary-desc">已审核 · {{ merchant.roomCount || 0 }} 间房源在管理</text></view>
				<view class="merchant-summary-status"><ProtoIcon name="verified" :size="26" /><text>经营中</text></view>
			</view>
			<view class="merchant-grid">
				<view v-for="item in merchantEntries" :key="item.key" class="merchant-entry" :class="{ disabled: item.disabled }" @tap="handleMerchantEntry(item)">
					<view class="merchant-entry-icon"><ProtoIcon :name="item.icon" :size="38" /></view>
					<text class="merchant-entry-title">{{ item.title }}</text>
					<text v-if="item.disabled" class="merchant-entry-status">待开放</text>
				</view>
			</view>
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
	import { goPage, goRoot, prototypeImages, showToast } from '@/common/prototype.js'
	import { getFavorites, getGuests, getMerchantProfile, getOrders, getProfile, saveMerchantProfile } from '@/common/app-store.js'

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
				merchant: getMerchantProfile(),
				guests: [],
				favoriteCount: 0,
				orderShortcuts: [
					{ key: 'all', label: '全部', icon: 'wallet', badge: '' },
					{ key: 'pay', label: '待付款', icon: 'clock', badge: '1' },
					{ key: 'stay', label: '可使用', icon: 'receipt', badge: '1' },
					{ key: 'done', label: '已使用', icon: 'check-circle', badge: '1' },
					{ key: 'refund', label: '取消/退款', icon: 'refresh', badge: '1' }
				],
				featureEntries: [
					{ title: '入驻申请', icon: 'store', action: 'apply' },
					{ title: '在线客服', icon: 'support', route: '/pages/service/contact' },
					{ title: '退出登录', icon: 'refresh', action: 'logout' }
				],
				merchantEntries: [
					{ key: 'finance', title: '财务统计', icon: 'wallet', route: '/pages/merchant/finance' },
					{ key: 'rooms', title: '房间管理', icon: 'house', route: '/pages/merchant/rooms' },
					{ key: 'orders', title: '订单管理', icon: 'receipt', route: '/pages/merchant/orders' },
					{ key: 'verify', title: '扫码核销', icon: 'qr', route: '/pages/merchant/verify' },
					{ key: 'records', title: '核销记录', icon: 'document', route: '/pages/merchant/records' },
					{ key: 'staff', title: '员工管理', icon: 'group', route: '/pages/merchant/staff' },
					{ key: 'property', title: '民宿管理', icon: 'store', route: '/pages/merchant/index' },
					{ key: 'publish', title: '发布房源', icon: 'plus', route: '/pages/merchant/publish' },
					{ key: 'reviews', title: '评价管理', icon: 'star', route: '/pages/merchant/reviews' },
					{ key: 'coupon', title: '优惠券管理', icon: 'document', route: '/pages/merchant/coupons' },
					{ key: 'settings', title: '民宿设置', icon: 'settings', route: '/pages/merchant/settings' },
					{ key: 'subscriptions', title: '订阅消息', icon: 'document', route: '/pages/merchant/messages' }
				],
				travelEntries: [
					{ title: '常用入住人', desc: '已登记2位常住人信息，免重复输入', icon: 'group', route: '/pages/me/guests' },
					{ title: '我的发票抬头', desc: '增值税电子普通/专用发票抬头管理', icon: 'document', action: 'invoice' }
				],
				serviceEntries: [
					{ title: '官方微信客服', desc: '在线解答预订变更、房费退还与入住咨询', icon: 'support', route: '/pages/service/contact' },
					{ title: '联系直营门店', desc: '快速呼叫前台管家、停车指引与房卡取用', icon: 'store', route: '/pages/service/contact' },
					{ title: '出行攻略与旅宿公告', desc: '探索本地私房指南与时令特惠活动', icon: 'book', route: '/pages/content/detail' }
				],
				policyEntries: [
					{ title: '用户服务协议', desc: '', icon: 'document', route: '/pages/rule/index' },
					{ title: '隐私政策与授权权限', desc: '', icon: 'shield', route: '/pages/rule/index' },
					{ title: '关于 TCPMS 智选旅宿', desc: '', icon: 'info', version: 'v2.4.1', message: 'TCPMS 智选旅宿小程序' }
				]
			}
		},
		computed: {
			visibleFeatureEntries() {
				const merchantApproved = this.loggedIn && this.merchant.enabled && this.merchant.status === 'approved'
				return merchantApproved ? this.featureEntries.filter((item) => item.action !== 'apply') : this.featureEntries
			},
			stats() {
				const completedNights = this.orders
					.filter((order) => order.statusKey === 'done')
					.reduce((total, order) => total + Number(order.nights || 1), 0)
				return [
					{ value: '1,280', label: '旅宿积分' },
					{ value: String(completedNights), label: '入住天数' },
					{ value: String(this.favoriteCount), label: '收藏门店' }
				]
			},
			todayOrder() {
				return this.orders.find((order) => order.statusKey === 'stay' || order.statusKey === 'pay')
			},
			todayOrderText() {
				return this.todayOrder ? `当前预订：${this.todayOrder.room}` : '暂无进行中的预订'
			}
		},
		onShow() {
			this.refreshData()
		},
		methods: {
			refreshData() {
				this.profile = getProfile()
				this.loggedIn = Boolean(this.profile.loggedIn)
				this.avatar = this.profile.avatar || prototypeImages.avatar
				this.orders = getOrders()
				this.merchant = getMerchantProfile()
				this.guests = getGuests()
				this.favoriteCount = getFavorites().length
				this.orderShortcuts = [
					{ key: 'all', label: '全部', icon: 'wallet', badge: String(this.orders.length) },
					{ key: 'pay', label: '待付款', icon: 'clock', badge: String(this.orders.filter((order) => order.statusKey === 'pay').length) },
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
					showToast(this.loggedIn ? '已退出登录' : '当前未登录')
					return
				}
				if (item.action === 'apply') {
					this.applyMerchant()
					return
				}
				showToast('功能即将开放')
			},
			applyMerchant() {
				if (!this.loggedIn) {
					this.goLogin()
					return
				}
				uni.showModal({
					title: '申请入驻商家',
					content: '提交后将创建演示民宿“天空之蓝”，审核通过后可使用民宿管理功能。',
					confirmText: '提交申请',
					success: ({ confirm }) => {
						if (!confirm) return
						saveMerchantProfile({
							enabled: true,
							status: 'approved',
							storeId: 'store-001',
							storeName: '天空之蓝',
							role: '店主',
							roomCount: 0,
							pendingOrderCount: 0
						})
						this.refreshData()
						showToast('入驻成功，民宿管理已开放')
					}
				})
			},
			handleMerchantEntry(item) {
				if (item.disabled) {
					showToast('该功能将在商家服务完善后开放')
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
			goOrders() {
				goPage('/pages/order/list')
			},
			goTodayOrder() {
				if (this.todayOrder) {
					goPage(`/pages/order/detail?id=${this.todayOrder.id}`)
					return
				}
				goRoot('/pages/store/list')
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

	.profile-card {
		position: relative;
		display: flex;
		align-items: flex-start;
		gap: 14rpx;
		min-height: 160rpx;
	}

	.profile-card > image {
		width: 112rpx;
		height: 112rpx;
		border: 5rpx solid #8debf2;
		border-radius: 50%;
	}

	.profile-main {
		flex: 1;
		min-width: 0;
	}

	.profile-name-row {
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.profile-name {
		color: var(--proto-text);
		font-size: 37rpx;
		font-weight: 850;
	}

	.profile-name-row .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}

	.profile-uid,
	.profile-medal {
		display: block;
		margin-top: 7rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.profile-medal {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark);
	}

	.profile-medal .proto-icon {
		margin-right: 5rpx;
	}

	.profile-tools {
		display: flex;
		gap: 8rpx;
	}

	.profile-tools button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 58rpx;
		height: 58rpx;
		padding: 0;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 50%;
		font-size: 28rpx;
	}

	.member-progress {
		position: absolute;
		right: 24rpx;
		bottom: 18rpx;
		left: 24rpx;
		display: flex;
		justify-content: space-between;
		padding: 14rpx 18rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 18rpx;
		font-size: 19rpx;
	}

	.member-progress > view {
		display: flex;
		align-items: center;
	}

	.member-progress .proto-icon {
		margin-right: 5rpx;
	}

	.member-progress .proto-link {
		display: flex;
		align-items: center;
	}

	.guest-profile {
		flex-direction: row;
		align-items: center;
	}

	.guest-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100rpx;
		height: 100rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.guest-profile .profile-name {
		font-size: 27rpx;
	}

	.guest-profile .proto-button-small {
		width: auto;
		min-width: 0;
		flex: 0 0 auto;
		height: 60rpx;
		padding: 0 16rpx;
		font-size: 18rpx;
	}

	.stats-card {
		display: flex !important;
		flex-direction: row;
		flex-wrap: nowrap;
		align-items: stretch;
		gap: 10rpx;
		text-align: center;
	}

	.stats-card > view {
		display: flex;
		flex-direction: column;
		flex: 1 1 0;
		width: 0;
		min-width: 0;
	}

	.stats-card .strong {
		color: var(--proto-primary-dark);
		font-size: 30rpx;
		font-weight: 800;
	}

	.stats-card text {
		margin-top: 8rpx;
		color: var(--proto-text);
		font-size: 18rpx;
	}

	.my-order-card,
	.me-section {
		padding: 24rpx;
	}

	.section-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 800;
	}

	.section-heading > text:first-child::before {
		display: inline-block;
		width: 8rpx;
		height: 28rpx;
		margin-right: 10rpx;
		background: var(--proto-primary);
		border-radius: 6rpx;
		content: "";
		vertical-align: -4rpx;
	}

	.section-heading .proto-link {
		display: flex;
		align-items: center;
		color: var(--proto-muted);
		font-size: 19rpx;
		font-weight: 400;
	}

	.section-heading .proto-link .proto-icon {
		margin-left: 3rpx;
	}

	.order-shortcuts {
		display: flex !important;
		flex-direction: row;
		flex-wrap: nowrap;
		gap: 10rpx;
		margin-top: 22rpx;
	}

	.order-shortcuts > view {
		position: relative;
		display: flex;
		align-items: center;
		flex-direction: column;
		flex: 1 1 0;
		width: 0;
		min-width: 0;
		color: var(--proto-text);
		font-size: 19rpx;
	}

	.shortcut-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		margin-bottom: 10rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		font-size: 31rpx;
	}

	.shortcut-badge {
		position: absolute;
		top: -8rpx;
		right: 18rpx;
		min-width: 28rpx;
		height: 28rpx;
		padding: 0 6rpx;
		color: #ffffff;
		background: var(--proto-error);
		border-radius: 14rpx;
		font-size: 16rpx;
		line-height: 28rpx;
		text-align: center;
	}

	.today-order {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 22rpx;
		padding: 14rpx 16rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		font-size: 19rpx;
	}

	.today-order > view {
		display: flex;
		align-items: center;
		min-width: 0;
	}

	.today-order > view > text {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.today-order > view .proto-icon {
		flex: 0 0 auto;
		margin-right: 5rpx;
	}

	.today-order .proto-button-small {
		height: 48rpx;
		padding: 0 14rpx;
		font-size: 17rpx;
	}

	.section-label {
		display: block;
		margin-bottom: 8rpx;
		color: var(--proto-muted);
		font-size: 23rpx;
		font-weight: 750;
	}

	.me-entry {
		display: flex;
		align-items: center;
		gap: 14rpx;
		padding: 20rpx 0;
	}

	.me-entry + .me-entry {
		border-top: 1rpx solid #e3ebec;
	}

	.entry-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 58rpx;
		height: 58rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 18rpx;
		font-size: 31rpx;
	}

	.me-entry > view {
		flex: 1;
	}

	.entry-title,
	.entry-desc {
		display: block;
	}

	.entry-title {
		color: var(--proto-text);
		font-size: 25rpx;
	}

	.entry-desc {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.entry-arrow,
	.entry-version {
		color: var(--proto-muted);
	}

	.entry-version {
		font-size: 18rpx;
	}

	.me-footer {
		display: flex;
		align-items: center;
		flex-direction: column;
		gap: 8rpx;
		padding: 18rpx 28rpx 58rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
		text-align: center;
	}

	.footer-brand {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark);
		font-size: 23rpx;
		font-weight: 750;
	}

	.footer-brand .proto-icon {
		margin-right: 5rpx;
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
