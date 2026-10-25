<template>
	<view class="proto-bottom-nav">
		<button
			v-for="item in navItems"
			:key="item.key"
			class="proto-nav-item"
			:class="{ active: active === item.key }"
			hover-class="none"
			@tap="handleTap(item)"
		>
			<image
				class="proto-nav-icon"
				:src="active === item.key ? item.activeIcon : item.icon"
				mode="aspectFit"
			></image>
			<text class="proto-nav-label">{{ item.label }}</text>
		</button>
	</view>
</template>

<script>
	export default {
		name: 'ProtoBottomNav',
		props: {
			active: {
				type: String,
				default: 'home'
			},
			variant: {
				type: String,
				default: 'root'
			}
		},
		data() {
			return {
				items: [
					{
						key: 'home',
						label: '首页',
						icon: '/static/prototype-icons/nav-home-idle.png',
						activeIcon: '/static/prototype-icons/nav-home-active.png',
						path: '/pages/index/index'
					},
					{
						key: 'stores',
						label: '门店',
						icon: '/static/prototype-icons/nav-stores-idle.png',
						activeIcon: '/static/prototype-icons/nav-stores-active.png',
						path: '/pages/store/list'
					},
					{
						key: 'orders',
						label: '订单',
						icon: '/static/prototype-icons/nav-orders-idle.png',
						activeIcon: '/static/prototype-icons/nav-orders-active-2.png',
						path: '/pages/order/list'
					},
					{
						key: 'messages',
						label: '消息',
						icon: '/static/prototype-icons/ui-support.png',
						activeIcon: '/static/prototype-icons/ui-support-active.png',
						path: '/pages/message/list'
					},
					{
						key: 'me',
						label: '我的',
						icon: '/static/prototype-icons/nav-me-idle.png',
						activeIcon: '/static/prototype-icons/nav-me-active-2.png',
						path: '/pages/me/index'
					}
				]
			}
		},
		computed: {
			navItems() {
				return this.items
			}
		},
		methods: {
			handleTap(item) {
				if (item.key === this.active) {
					return
				}
				// 5 个主页面已在 pages.json 注册为 tabBar 页面，switchTab 复用页面实例、不重建，避免切换闪屏。
				uni.switchTab({
					url: item.path,
					fail: () => uni.reLaunch({ url: item.path })
				})
			}
		}
	}
</script>
