<template>
	<view
		class="proto-header"
		:style="headerStyle"
		:class="{
			'proto-header-overlay': overlay,
			'proto-header-no-chrome': !trailingAvatar && !action,
			'proto-header-plain': theme === 'plain',
			'proto-header-tint': theme === 'tint',
			'proto-header-green': theme === 'green'
		}"
	>
		<view class="proto-header-leading">
			<button v-if="back" class="proto-header-icon-button" hover-class="none" aria-label="返回" @tap="handleBack">
				<ProtoIcon name="back" :tone="iconTone" :size="38" />
			</button>
			<button v-else-if="leadingAvatar" class="proto-header-user-button" hover-class="none" aria-label="个人中心" @tap="handleProfile">
				<ProtoIcon name="user" tone="white" :size="28" />
			</button>
		</view>

		<text
			class="proto-header-title"
			:class="{
				'proto-header-title-left': titleAlign === 'left',
				'proto-header-title-center': titleAlign === 'center'
			}"
		>{{ title }}</text>

		<view class="proto-header-trailing">
			<button v-if="action && theme !== 'green'" class="proto-header-icon-button proto-header-action" hover-class="none" :aria-label="actionLabel" @tap="handleAction">
				<ProtoIcon :name="actionIcon" :tone="iconTone" :size="34" />
			</button>
			<button v-if="trailingAvatar && theme !== 'green'" class="proto-header-user-button" hover-class="none" aria-label="个人中心" @tap="handleProfile">
				<ProtoIcon name="user" tone="white" :size="28" />
			</button>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'

	export default {
		name: 'ProtoHeader',
		components: { ProtoIcon },
		data() {
			let statusBarHeightRpx = 0
			try {
				const info = uni.getSystemInfoSync()
				statusBarHeightRpx = Math.round((Number(info.statusBarHeight) || 0) * 750 / (Number(info.windowWidth) || 375))
			} catch (error) {
				statusBarHeightRpx = 0
			}
			return { statusBarHeightRpx }
		},
		props: {
			title: {
				type: String,
				default: ''
			},
			back: {
				type: Boolean,
				default: true
			},
			action: {
				type: String,
				default: ''
			},
			titleAlign: {
				type: String,
				default: 'left'
			},
			backPath: {
				type: String,
				default: ''
			},
			leadingAvatar: {
				type: Boolean,
				default: false
			},
			trailingAvatar: {
				type: Boolean,
				default: false
			},
			actionMenu: {
				type: Boolean,
				default: true
			},
			overlay: {
				type: Boolean,
				default: false
			},
			theme: {
				type: String,
				default: ''
			}
		},
		emits: ['action'],
		computed: {
			iconTone() {
				return this.overlay || this.theme === 'green' ? 'white' : 'default'
			},
			headerStyle() {
				const statusBar = Number(this.statusBarHeightRpx) || 0
				return {
					minHeight: `${112 + statusBar}rpx`,
					height: `${112 + statusBar}rpx`,
					paddingTop: `${20 + statusBar}rpx`
				}
			},
			actionIcon() {
				const value = String(this.action || '')
				if (value.indexOf('⌖') >= 0 || value.indexOf('map') >= 0) return 'map'
				if (value.indexOf('share') >= 0) return 'share'
				if (value.indexOf('人') >= 0) return 'user'
				return 'more'
			},
			actionLabel() {
				if (this.actionIcon === 'map') return '地图视图'
				if (this.actionIcon === 'share') return '分享'
				return '更多选项'
			}
		},
		methods: {
			handleBack() {
				if (this.backPath) {
					uni.navigateTo({ url: this.backPath })
					return
				}
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/index/index' })
				})
			},
			handleAction() {
				this.$emit('action')
				if (!this.actionMenu) {
					return
				}
				this.openMenu()
			},
			openMenu() {
				uni.showActionSheet({
					itemList: ['联系客服', '法律政策'],
					success: ({ tapIndex }) => {
						if (tapIndex === 0) {
							uni.navigateTo({ url: '/pages/service/contact' })
							return
						}
						uni.navigateTo({ url: '/pages/rule/index' })
					}
				})
			},
			handleProfile() {
				const pages = getCurrentPages()
				const current = pages.length ? pages[pages.length - 1].route : ''
				if (current === 'pages/me/index') return
				uni.navigateTo({
					url: '/pages/me/index',
					fail: () => uni.reLaunch({ url: '/pages/me/index' })
				})
			}
		}
	}
</script>
