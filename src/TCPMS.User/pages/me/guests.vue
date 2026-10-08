<template>
	<view class="proto-page guests-page">
		<ProtoHeader title="入住人管理" theme="green" />

		<view class="security-banner">
			<view class="security-icon"><ProtoIcon name="check" tone="white" :size="25" /></view>
			<view>
				<text class="security-title">公安部实名联网认证　<text class="security-encrypt">AES-256 加密</text></text>
				<text class="security-desc">根据文旅部与公安部门实名制治安规定，入住酒店/民宿须提供真实有效身份证件。信息仅用于前台核验及报备。</text>
			</view>
		</view>

		<view class="guests-heading">
			<text>已登记入住人（{{ guests.length }}/10）</text>
			<view><ProtoIcon name="shield" :size="18" /><text>隐私已脱敏保护</text></view>
		</view>

		<ProtoEmptyState v-if="!guests.length" action-text="立即添加常用入住人" @action="openEditor" />
		<view v-else class="guest-list">
			<view v-for="guest in guests" :key="guest.id" class="guest-card proto-card">
				<view class="guest-card-title">
					<text class="guest-card-name">{{ guest.name }}</text>
					<view class="proto-pill" :class="guest.isDefault ? 'primary' : 'info'">
						<ProtoIcon v-if="guest.isDefault" :name="'check'" :tone="guest.isDefault ? 'white' : 'default'" :size="17" />
						<text>{{ guest.isDefault ? '默认入住人' : '同行人' }}</text>
					</view>
					<text v-if="guest.isDefault" class="proto-pill muted">本人</text>
					<button class="edit-button" hover-class="none" @tap="openEditor(guest)"><ProtoIcon name="edit" :size="24" /></button>
				</view>
				<view class="guest-sensitive">
					<view class="guest-sensitive-row"><ProtoIcon name="document" :size="20" /><text>居民身份证　<text class="strong">{{ guest.identity }}</text></text></view>
					<view class="guest-sensitive-row"><ProtoIcon name="phone" :size="20" /><text>手机号码　{{ guest.phone }}</text></view>
				</view>
				<view class="guest-card-footer">
					<view v-if="guest.isDefault" class="verified"><ProtoIcon name="verified" :size="18" /><text>已实名核验</text></view>
					<view v-else class="set-default" @tap="setDefault(guest)"><ProtoIcon name="status" :size="18" /><text>设为默认入住人</text></view>
					<view class="delete-text" @tap="deleteGuest(guest)"><ProtoIcon name="close" :size="17" /><text>删除</text></view>
				</view>
			</view>
		</view>

		<button class="proto-primary-button add-guest-button" hover-class="none" @tap="openEditor()"><ProtoIcon name="plus" tone="white" :size="22" /><text>添加入住人</text></button>

		<view v-if="editorVisible" class="proto-mask" @tap="closeEditor">
			<view class="proto-sheet editor-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>{{ editingGuest ? '编辑入住人' : '新增入住人' }}</text>
					<button class="proto-close" hover-class="none" @tap="closeEditor"><ProtoIcon name="close" :size="24" /></button>
				</view>
				<text class="proto-form-label">真实姓名 <text class="required">*</text></text>
				<input v-model="form.name" class="proto-input" placeholder="请输入2-15字真实中文姓名" />
				<text class="proto-form-label">证件类型 <text class="required">*</text></text>
				<view class="select-input"><text>居民身份证（中国大陆）</text><ProtoIcon name="chevron-down" :size="20" /></view>
				<text class="proto-form-label">证件号码 <text class="required">*</text></text>
				<input v-model="form.identity" class="proto-input" placeholder="请输入正确的18位二代身份证号码" />
				<text class="proto-form-label">联系电话</text>
				<input v-model="form.phone" class="proto-input" placeholder="请输入合法的11位手机号码" />
				<view class="default-toggle" @tap="form.isDefault = !form.isDefault">
					<view class="proto-check-box" :class="{ checked: form.isDefault }"><ProtoIcon v-if="form.isDefault" name="check" tone="white" :size="22" /></view>
					<text>设为默认入住人</text>
				</view>
				<view class="editor-actions">
					<button class="proto-secondary-button" hover-class="none" @tap="closeEditor">取消</button>
					<button class="proto-primary-button" hover-class="none" @tap="saveGuest">保存入住人</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { showToast } from '@/common/prototype.js'
	import { getGuests, saveGuests } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				guests: [],
				editorVisible: false,
				editingGuest: null,
				form: {
					name: '',
					identity: '',
					phone: '',
					isDefault: false
				}
			}
		},
		onShow() {
			this.guests = getGuests()
		},
		methods: {
			openEditor(guest) {
				this.editingGuest = guest || null
				this.form = guest ? { name: guest.name, identity: guest.identity, phone: guest.phone, isDefault: guest.isDefault } : { name: '', identity: '', phone: '', isDefault: false }
				this.editorVisible = true
			},
			closeEditor() {
				this.editorVisible = false
				this.editingGuest = null
			},
			saveGuest() {
				if (!this.form.name || !this.form.identity) {
					showToast('请填写姓名和证件号码')
					return
				}
				if (this.form.phone && !this.form.phone.includes('*') && !/^1\d{10}$/.test(this.form.phone)) {
					showToast('请输入正确的手机号')
					return
				}
				if (!this.editingGuest && this.guests.length >= 10) {
					showToast('最多保存10位入住人')
					return
				}
				if (this.form.isDefault) {
					this.guests.forEach((guest) => { guest.isDefault = false })
				}
				if (this.editingGuest) {
					Object.assign(this.editingGuest, this.form)
				} else {
					this.guests.push({ ...this.form, id: Date.now(), tag: this.form.isDefault ? '默认入住人' : '同行人' })
				}
				saveGuests(this.guests)
				this.closeEditor()
				showToast('操作成功')
			},
			setDefault(guest) {
				this.guests.forEach((item) => { item.isDefault = item.id === guest.id })
				saveGuests(this.guests)
				showToast('已设置默认入住人')
			},
			deleteGuest(guest) {
				uni.showModal({
					title: '确认删除该入住人？',
					content: '删除后预订房间需重新输入证件信息。',
					success: (res) => {
						if (res.confirm) {
							this.guests = this.guests.filter((item) => item.id !== guest.id)
							if (!this.guests.some((item) => item.isDefault) && this.guests[0]) this.guests[0].isDefault = true
							saveGuests(this.guests)
							showToast('已删除')
						}
					}
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.guests-page {
		padding-bottom: 132rpx;
		padding-bottom: calc(132rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(132rpx + env(safe-area-inset-bottom));
	}

	.security-banner {
		display: flex;
		gap: 16rpx;
		margin: 22rpx 28rpx;
		padding: 24rpx;
		background: var(--proto-surface-low);
		border-radius: 22rpx;
	}

	.security-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 66rpx;
		height: 66rpx;
		color: var(--proto-primary-dark);
		background: #8debf2;
		border-radius: 50%;
	}

	.security-banner > view:not(.security-icon) {
		flex: 1;
		min-width: 0;
	}

	.security-title,
	.security-desc {
		display: block;
	}

	.security-title {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 800;
	}

	.security-encrypt {
		padding: 6rpx 10rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 14rpx;
		font-size: 17rpx;
		font-weight: 600;
	}

	.security-desc {
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
		line-height: 1.5;
	}

	.guests-heading {
		display: flex;
		justify-content: space-between;
		margin: 26rpx 30rpx 14rpx;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
	}

	.guests-heading text:last-child {
		color: var(--proto-primary-dark);
		font-size: 19rpx;
	}

	.guests-heading > view {
		display: flex;
		align-items: center;
		gap: 5rpx;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
	}

	.guest-card {
		padding: 24rpx;
	}

	.guest-card-title {
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.guest-card-name {
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 850;
	}

	.guest-card-title .proto-pill {
		display: inline-flex;
		align-items: center;
		gap: 5rpx;
		min-height: 36rpx;
		padding: 0 11rpx;
		font-size: 17rpx;
	}

	.edit-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		margin-left: auto;
		padding: 0;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 50%;
		font-size: 34rpx;
	}

	.guest-sensitive {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		margin-top: 22rpx;
		padding: 22rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		font-size: 21rpx;
	}

	.guest-sensitive-row {
		display: flex;
		align-items: center;
		gap: 7rpx;
	}

	.guest-sensitive-row > text {
		flex: 1;
	}

	.guest-sensitive .strong {
		color: var(--proto-text);
		font-size: 26rpx;
		letter-spacing: 2rpx;
	}

	.guest-card-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 20rpx;
		font-size: 20rpx;
	}

	.verified {
		display: flex;
		align-items: center;
		gap: 5rpx;
		color: var(--proto-primary-dark);
		font-weight: 750;
	}

	.set-default {
		display: flex;
		align-items: center;
		gap: 5rpx;
		color: var(--proto-muted);
	}

	.delete-text {
		display: flex;
		align-items: center;
		gap: 5rpx;
		color: var(--proto-error);
		font-weight: 700;
	}

	.add-guest-button {
		display: flex;
		align-items: center;
		gap: 7rpx;
		margin-top: 30rpx;
	}

	.editor-sheet {
		max-height: 88vh;
	}

	.required {
		color: var(--proto-error);
	}

	.select-input {
		display: flex;
		align-items: center;
		justify-content: space-between;
		height: 76rpx;
		padding: 20rpx;
		color: var(--proto-text);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		font-size: 24rpx;
	}

	.default-toggle {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 24rpx;
		color: var(--proto-text);
		font-size: 21rpx;
	}

	.default-toggle .proto-check-box {
		margin: 0;
	}

	.editor-actions {
		display: flex;
		gap: 14rpx;
		margin-top: 24rpx;
	}

	.editor-actions button {
		flex: 1;
		margin: 0;
	}
</style>
