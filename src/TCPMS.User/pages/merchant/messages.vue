<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="订阅消息" theme="green" />
		<view class="message-intro proto-card"><view class="message-intro-icon"><ProtoIcon name="support" :size="38" /></view><view><text>经营提醒</text><text>选择你希望接收的民宿运营消息</text></view></view>
		<view class="message-list proto-card">
			<view v-for="item in messages" :key="item.key" class="message-row"><view class="message-row-icon"><ProtoIcon :name="item.key === 'order' ? 'receipt' : item.key === 'review' ? 'star' : 'wallet'" :size="30" /></view><view class="message-copy"><text>{{ item.title }}</text><text>{{ item.desc }}</text></view><switch :checked="item.enabled" color="#2aa9a9" @change="toggle(item, $event.detail.value)" /></view>
		</view>
		<button class="proto-primary-button message-save" hover-class="none" @tap="save">保存偏好</button>
		<view class="tool-note"><ProtoIcon name="info" :size="21" /><text>订阅偏好已保存到服务端，微信订阅消息需配置模板后生效。</text></view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { fetchMerchantMessageSettings, saveMerchantMessageSettingsRemote } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() { return { messages: [] } },
		async onShow() {
			try {
				this.messages = await fetchMerchantMessageSettings()
			} catch (error) {
				this.messages = []
				showToast(error?.message || '订阅设置加载失败')
			}
		},
		methods: {
			toggle(item, value) { item.enabled = value },
			async save() {
				try {
					this.messages = await saveMerchantMessageSettingsRemote(this.messages)
					showToast('订阅偏好已保存')
				} catch (error) {
					showToast(error?.message || '保存失败')
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-tool-page { padding-bottom: 48rpx; background: #f4fafb; }
	.merchant-tool-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}
	.message-intro { display: flex; align-items: center; gap: 14rpx; padding: 22rpx; }
	.message-intro-icon, .message-row-icon { display: flex; align-items: center; justify-content: center; color: var(--proto-primary-dark); background: var(--proto-surface-tint); border-radius: 18rpx; }
	.message-intro-icon { width: 72rpx; height: 72rpx; flex: 0 0 72rpx; }
	.message-intro > view:last-child { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.message-intro > view:last-child > text:first-child { color: var(--proto-text); font-size: 24rpx; font-weight: 750; }
	.message-intro > view:last-child > text:last-child { margin-top: 7rpx; overflow: hidden; color: var(--proto-muted); font-size: 18rpx; text-overflow: ellipsis; white-space: nowrap; }
	.message-list { padding: 8rpx 22rpx; }
	.message-row { display: flex; align-items: center; gap: 14rpx; padding: 18rpx 0; }
	.message-row + .message-row { border-top: 1rpx solid #edf1f2; }
	.message-row-icon { width: 58rpx; height: 58rpx; flex: 0 0 58rpx; }
	.message-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.message-copy > text:first-child { color: var(--proto-text); font-size: 22rpx; font-weight: 700; }
	.message-copy > text:last-child { margin-top: 6rpx; overflow: hidden; color: var(--proto-muted); font-size: 17rpx; line-height: 1.4; text-overflow: ellipsis; white-space: nowrap; }
	.message-row switch { flex: 0 0 auto; }
	.message-save { height: 78rpx; margin-top: 24rpx; white-space: nowrap; }
	.tool-note { display: flex; align-items: flex-start; gap: 8rpx; margin: 22rpx 30rpx; color: var(--proto-muted); font-size: 17rpx; line-height: 1.5; }
</style>
