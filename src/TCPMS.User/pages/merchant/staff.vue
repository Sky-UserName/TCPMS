<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="员工管理" theme="green" />
		<view class="staff-hero proto-card"><view class="staff-hero-icon"><ProtoIcon name="group" :size="42" /></view><view><text>协同运营成员</text><text>为民宿添加前台、管家等角色</text></view><button class="proto-secondary-button" hover-class="none" @tap="inviteStaff">添加员工</button></view>
		<view class="tool-section-heading"><text>员工列表</text><text>{{ staff.length }} 人</text></view>
		<view class="staff-list">
			<view v-for="member in staff" :key="member.id" class="staff-card proto-card"><view class="staff-avatar"><ProtoIcon name="user" :size="30" /></view><view class="staff-copy"><text>{{ member.name }}</text><text>{{ member.phone }} · {{ member.role }}</text></view><text class="staff-status">{{ member.status }}</text></view>
		</view>
		<view class="tool-note"><ProtoIcon name="info" :size="21" /><text>员工账号创建后需使用初始密码登录管理端并立即修改密码。</text></view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { createMerchantStaff, fetchMerchantStaff } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() { return { staff: [] } },
		async onShow() {
			await this.loadStaff()
		},
		methods: {
			async loadStaff() {
				try {
					this.staff = await fetchMerchantStaff()
				} catch (error) {
					this.staff = []
					showToast(error?.message || '员工加载失败')
				}
			},
			inviteStaff() {
				if (!uni.showModal) {
					showToast('当前环境暂不支持添加员工')
					return
				}
				uni.showModal({
					title: '添加员工',
					editable: true,
					placeholderText: '请输入员工姓名',
					confirmText: '添加',
					success: async ({ confirm, content = '' }) => {
						if (!confirm || !content.trim()) return
						try {
							await createMerchantStaff({ name: content.trim() })
							showToast('员工已添加，初始密码为 Admin@123456')
							await this.loadStaff()
						} catch (error) {
							showToast(error?.message || '添加失败')
						}
					}
				})
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
	.staff-hero { display: flex; align-items: center; gap: 14rpx; padding: 20rpx; }
	.staff-hero-icon, .staff-avatar { display: flex; align-items: center; justify-content: center; color: var(--proto-primary-dark); background: var(--proto-surface-tint); border-radius: 18rpx; }
	.staff-hero-icon { width: 72rpx; height: 72rpx; flex: 0 0 72rpx; }
	.staff-hero > view:nth-child(2) { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.staff-hero > view:nth-child(2) > text:first-child { color: var(--proto-text); font-size: 23rpx; font-weight: 750; }
	.staff-hero > view:nth-child(2) > text:last-child { margin-top: 6rpx; overflow: hidden; color: var(--proto-muted); font-size: 17rpx; line-height: 1.4; text-overflow: ellipsis; white-space: nowrap; }
	.staff-hero button { flex: 0 0 130rpx; min-width: 130rpx; height: 58rpx; margin: 0; padding: 0; font-size: 18rpx; white-space: nowrap; }
	.tool-section-heading { display: flex; align-items: center; justify-content: space-between; margin: 26rpx 24rpx 12rpx; color: var(--proto-text); font-size: 27rpx; font-weight: 800; }
	.tool-section-heading > text:last-child { color: var(--proto-muted); font-size: 18rpx; font-weight: 400; }
	.staff-card { display: flex; align-items: center; gap: 14rpx; padding: 18rpx; }
	.staff-avatar { width: 62rpx; height: 62rpx; flex: 0 0 62rpx; }
	.staff-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.staff-copy > text:first-child { color: var(--proto-text); font-size: 23rpx; font-weight: 750; }
	.staff-copy > text:last-child { margin-top: 6rpx; overflow: hidden; color: var(--proto-muted); font-size: 18rpx; text-overflow: ellipsis; white-space: nowrap; }
	.staff-status { flex: 0 0 auto; color: var(--proto-primary-dark); font-size: 18rpx; white-space: nowrap; }
	.tool-note { display: flex; align-items: flex-start; gap: 8rpx; margin: 22rpx 30rpx; color: var(--proto-muted); font-size: 17rpx; line-height: 1.5; }
	.tool-note .proto-icon { flex: 0 0 auto; }
</style>
