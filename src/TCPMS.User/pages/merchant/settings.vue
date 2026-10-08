<template>
	<view class="proto-page merchant-settings-page">
		<ProtoHeader title="民宿设置" theme="green" />
		<view class="settings-section proto-card">
			<text class="settings-section-title">基础资料</text>
			<text class="settings-label">民宿名称</text><input v-model="form.storeName" class="proto-input" placeholder="请输入民宿名称" />
			<text class="settings-label">联系电话</text><input v-model="form.phone" class="proto-input" placeholder="请输入联系电话" />
		</view>
		<view class="settings-section proto-card">
			<text class="settings-section-title">入住规则</text>
			<view class="settings-grid"><view><text class="settings-label">入住时间</text><input v-model="form.checkIn" class="proto-input" placeholder="14:00" /></view><view><text class="settings-label">退房时间</text><input v-model="form.checkOut" class="proto-input" placeholder="12:00" /></view></view>
			<text class="settings-label">退订规则</text><textarea v-model="form.cancelRule" class="proto-textarea" placeholder="请输入退订规则" />
		</view>
		<view class="settings-section proto-card">
			<text class="settings-section-title">运营偏好</text>
			<view class="settings-row"><view><text>自动接受订单</text><text>订单进入后自动确认，需后端规则支持</text></view><switch :checked="form.autoAccept" color="#2aa9a9" @change="form.autoAccept = $event.detail.value" /></view>
			<view class="settings-row"><view><text>展示联系电话</text><text>是否在住客端展示民宿联系电话</text></view><switch :checked="form.showPhone" color="#2aa9a9" @change="form.showPhone = $event.detail.value" /></view>
		</view>
		<button class="proto-primary-button settings-save" hover-class="none" @tap="save">保存设置</button>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import { getMerchantSettings, saveMerchantSettings } from '@/common/app-store.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader },
		data() { return { form: getMerchantSettings() } },
		onShow() { this.form = getMerchantSettings() },
		methods: {
			save() {
				if (!this.form.storeName.trim()) {
					showToast('请填写民宿名称')
					return
				}
				this.form = saveMerchantSettings(this.form)
				showToast('设置已保存（本地演示）')
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-settings-page { padding-bottom: 50rpx; background: #f4fafb; }
	.merchant-settings-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}
	.settings-section { padding: 22rpx; }
	.settings-section-title { display: block; margin-bottom: 14rpx; color: var(--proto-text); font-size: 27rpx; font-weight: 800; }
	.settings-label { display: block; margin: 16rpx 0 9rpx; color: var(--proto-muted); font-size: 19rpx; }
	.settings-grid { display: flex; gap: 14rpx; }
	.settings-grid > view { flex: 1; min-width: 0; }
	.settings-grid .settings-label { margin-top: 0; }
	.settings-row { display: flex; align-items: center; justify-content: space-between; gap: 14rpx; padding: 16rpx 0; }
	.settings-row + .settings-row { border-top: 1rpx solid #edf1f2; }
	.settings-row > view { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.settings-row > view > text:first-child { color: var(--proto-text); font-size: 22rpx; font-weight: 700; }
	.settings-row > view > text:last-child { margin-top: 6rpx; color: var(--proto-muted); font-size: 17rpx; line-height: 1.4; }
	.settings-row switch { flex: 0 0 auto; }
	.settings-save { height: 78rpx; margin-top: 24rpx; white-space: nowrap; }
</style>
