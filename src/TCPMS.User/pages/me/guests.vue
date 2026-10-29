<template>
	<view class="proto-page guests-page" :class="{ 'select-mode': selectMode }">
		<ProtoHeader :title="selectMode ? '选择入住人' : '入住人管理'" theme="green" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />

		<view class="security-banner">
			<view class="security-icon"><ProtoIcon name="check" tone="white" :size="25" /></view>
			<view>
				<text class="security-title">公安部实名联网认证</text>
				<text class="security-desc">根据文旅部与公安部门实名制治安规定，入住酒店/民宿须提供真实有效身份证件。信息仅用于前台核验及报备。</text>
			</view>
		</view>

		<view class="guests-heading">
			<text v-if="selectMode">选择本次入住人（已选 {{ selectedIds.length }} 位）</text>
			<text v-else>已登记入住人（{{ guests.length }}/10）</text>
			<view><ProtoIcon name="shield" :size="18" /><text>隐私已脱敏保护</text></view>
		</view>

		<ProtoEmptyState v-if="!guests.length" action-text="立即添加常用入住人" @action="openEditor" />
		<view v-else class="guest-list">
			<view
				v-for="guest in guests"
				:key="guest.id"
				class="guest-card proto-card"
				:class="{ selected: selectMode && isSelected(guest) }"
				@tap="selectMode && toggleSelect(guest)"
			>
				<view class="guest-card-title">
					<text class="guest-card-name">{{ guest.name }}</text>
					<view class="proto-pill" :class="guest.isDefault ? 'primary' : 'info'">
						<ProtoIcon v-if="guest.isDefault" :name="'check'" :tone="guest.isDefault ? 'white' : 'default'" :size="17" />
						<text>{{ guest.isDefault ? '默认入住人' : '同行人' }}</text>
					</view>
					<text v-if="guest.isDefault" class="proto-pill muted">本人</text>
					<view class="guest-title-actions">
						<button class="edit-button" hover-class="none" @tap.stop="openEditor(guest)"><ProtoIcon name="edit" :size="24" /></button>
						<view v-if="selectMode" class="select-check" :class="{ checked: isSelected(guest) }">
							<ProtoIcon v-if="isSelected(guest)" name="check" tone="white" :size="20" />
						</view>
					</view>
				</view>
				<view class="guest-sensitive">
					<view class="guest-sensitive-row"><ProtoIcon name="document" :size="20" /><text>居民身份证　<text class="strong">{{ guest.identity }}</text></text></view>
					<view class="guest-sensitive-row"><ProtoIcon name="phone" :size="20" /><text>手机号码　{{ guest.phone }}</text></view>
				</view>
				<view v-if="!selectMode" class="guest-card-footer">
					<view v-if="guest.isDefault" class="verified"><ProtoIcon name="verified" :size="18" /><text>已实名核验</text></view>
					<view v-else class="set-default" @tap="setDefault(guest)"><ProtoIcon name="status" :size="18" /><text>设为默认入住人</text></view>
					<view class="delete-text" @tap="deleteGuest(guest)"><ProtoIcon name="close" :size="17" /><text>删除</text></view>
				</view>
			</view>
		</view>

		<button class="proto-primary-button add-guest-button" hover-class="none" @tap="openEditor()"><ProtoIcon name="plus" tone="white" :size="22" /><text>添加入住人</text></button>

		<view v-if="selectMode" class="select-bar">
			<view class="select-bar-copy">
				<text class="select-bar-count">已选 {{ selectedIds.length }} 位</text>
				<text class="select-bar-hint">{{ selectHint }}</text>
			</view>
			<button class="proto-primary-button" hover-class="none" :disabled="!selectedIds.length" @tap="confirmSelection">确定</button>
		</view>

		<view v-if="editorVisible" class="proto-mask" @tap="closeEditor">
			<view class="proto-sheet editor-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>{{ editingGuest ? '编辑入住人' : '新增入住人' }}</text>
					<button class="proto-close" hover-class="none" @tap="closeEditor"><ProtoIcon name="close" :size="24" /></button>
				</view>
				<text class="proto-form-label">真实姓名 <text class="required">*</text></text>
				<input v-model="form.name" class="proto-input" placeholder="请输入2-15字真实中文姓名" placeholder-class="editor-placeholder" />
				<text class="proto-form-label">证件类型 <text class="required">*</text></text>
				<view class="select-input"><text>居民身份证（中国大陆）</text><ProtoIcon name="chevron-down" :size="20" /></view>
				<text class="proto-form-label">证件号码 <text class="required">*</text></text>
				<input v-model="form.identity" class="proto-input" placeholder="请输入正确的18位二代身份证号码" placeholder-class="editor-placeholder" />
				<text class="proto-form-label">联系电话</text>
				<input v-model="form.phone" class="proto-input" placeholder="请输入合法的11位手机号码" placeholder-class="editor-placeholder" />
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
	import CacheRefreshIndicator from '@/components/CacheRefreshIndicator.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { showToast } from '@/common/prototype.js'
	import { getGuests, saveGuests } from '@/common/app-store.js'
	import { fetchMyGuests, saveMyGuests } from '@/common/api.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				guests: [],
				pullRefreshing: false,
				editorVisible: false,
				editingGuest: null,
				// 选择模式：从「订单填写」页带 mode=select 进来，勾选后回传入住人。
				// 不限制勾选人数：订单只要求至少登记 1 位实名入住人，同行人可自由多选。
				selectMode: false,
				selectedIds: [],
				pendingEditId: '',
				form: {
					name: '',
					identity: '',
					phone: '',
					isDefault: false
				}
			}
		},
		computed: {
			selectHint() {
				if (!this.selectedIds.length) return '请至少选择 1 位入住人'
				return '至少登记 1 位，同行入住人可继续勾选'
			}
		},
		onLoad(options = {}) {
			this.selectMode = String(options.mode || '') === 'select'
			this.selectedIds = String(options.selected || '')
				.split(',')
				.map((id) => id.trim())
				.filter(Boolean)
			// 订单页点某个入住人的「编辑」图标进来时，加载完直接打开这个人。
			this.pendingEditId = String(options.edit || '')
		},
		async onShow() {
			await this.loadGuests()
			this.openPendingEdit()
		},
		onPullDownRefresh() {
			return runPullRefresh(this, () => this.loadGuests())
		},
		methods: {
			async loadGuests() {
				try {
					this.guests = await fetchMyGuests()
					saveGuests(this.guests)
				} catch (error) {
					this.guests = getGuests()
					showToast(error?.message || '入住人同步失败，已显示本地缓存')
				}
				this.normalizeSelection()
			},
			// 已选 id 里可能有被删掉的人，进页面时对齐一次；勾选人数不做上限限制。
			normalizeSelection() {
				if (!this.selectMode) return
				const ids = this.guests.map((guest) => String(guest.id))
				this.selectedIds = this.selectedIds.filter((id) => ids.includes(String(id)))
			},
			openPendingEdit() {
				if (!this.pendingEditId) return
				const guest = this.guests.find((item) => String(item.id) === this.pendingEditId)
				this.pendingEditId = ''
				if (guest) this.openEditor(guest)
			},
			isSelected(guest) {
				return this.selectedIds.some((id) => String(id) === String(guest.id))
			},
			toggleSelect(guest) {
				if (!this.selectMode || !guest) return
				const id = String(guest.id)
				const index = this.selectedIds.findIndex((item) => String(item) === id)
				if (index >= 0) {
					this.selectedIds.splice(index, 1)
					return
				}
				// 多选不设上限：订单只要 ≥1 位实名入住人即可，同行人由用户自行决定。
				this.selectedIds.push(id)
			},
			// 确认后把选中的人回传给订单页：优先走 eventChannel，再发一次全局事件兜底。
			confirmSelection() {
				const picked = this.selectedIds
					.map((id) => this.guests.find((guest) => String(guest.id) === String(id)))
					.filter(Boolean)
					.map((guest) => ({ ...guest }))
				const channel = typeof this.getOpenerEventChannel === 'function' ? this.getOpenerEventChannel() : null
				if (channel && typeof channel.emit === 'function') {
					channel.emit('guestsSelected', picked)
				}
				uni.$emit('booking:guests-selected', picked)
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/booking/confirm' })
				})
			},
			async persist() {
				const payload = this.guests.map((guest) => ({ ...guest, id: String(guest.id) }))
				saveGuests(payload)
				await saveMyGuests(payload)
			},
			openEditor(guest) {
				this.editingGuest = guest || null
				this.form = guest ? { name: guest.name, identity: guest.identity, phone: guest.phone, isDefault: guest.isDefault } : { name: '', identity: '', phone: '', isDefault: false }
				this.editorVisible = true
			},
			closeEditor() {
				this.editorVisible = false
				this.editingGuest = null
			},
			async saveGuest() {
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
				const editing = this.editingGuest
				const created = editing ? null : { ...this.form, id: String(Date.now()), tag: this.form.isDefault ? '默认入住人' : '同行人' }
				if (this.form.isDefault) {
					this.guests.forEach((guest) => { guest.isDefault = false })
				}
				if (editing) {
					Object.assign(editing, this.form)
				} else {
					this.guests.push(created)
				}
				try {
					await this.persist()
					this.closeEditor()
					// 选择模式下新增的人直接勾上，省得再点一次。
					if (created && this.selectMode) {
						this.selectedIds.push(String(created.id))
					}
					showToast('操作成功')
				} catch (error) {
					showToast(error?.message || '保存失败')
				}
			},
			async setDefault(guest) {
				this.guests.forEach((item) => { item.isDefault = item.id === guest.id })
				try {
					await this.persist()
					showToast('已设置默认入住人')
				} catch (error) {
					showToast(error?.message || '设置失败')
				}
			},
			deleteGuest(guest) {
				uni.showModal({
					title: '确认删除该入住人？',
					content: '删除后预订房间需重新输入证件信息。',
					success: async (res) => {
						if (!res.confirm) return
						this.guests = this.guests.filter((item) => item.id !== guest.id)
						this.selectedIds = this.selectedIds.filter((id) => String(id) !== String(guest.id))
						if (!this.guests.some((item) => item.isDefault) && this.guests[0]) this.guests[0].isDefault = true
						try {
							await this.persist()
							showToast('已删除')
						} catch (error) {
							showToast(error?.message || '删除失败')
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

	/* 选择模式下底部有固定确认栏，页面末端的「添加入住人」不能被挡住。 */
	.guests-page.select-mode {
		padding-bottom: 250rpx;
		padding-bottom: calc(250rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(250rpx + env(safe-area-inset-bottom));
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

	/* 选择模式：右上角是「编辑 + 勾选圈」，勾选圈放最右侧方便点。 */
	.guest-title-actions {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-left: auto;
	}

	.guest-title-actions .edit-button {
		margin-left: 0;
		width: 56rpx;
		height: 56rpx;
	}

	.select-check {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 46rpx;
		height: 46rpx;
		flex: 0 0 46rpx;
		color: #ffffff;
		background: #ffffff;
		border: 3rpx solid #a9c6c6;
		border-radius: 50%;
	}

	.select-check.checked {
		background: #2aa9a9;
		border-color: #2aa9a9;
	}

	.guest-card.selected {
		border-color: #2aa9a9;
		box-shadow: 0 0 0 2rpx rgba(42, 169, 169, 0.22);
	}

	/* 选择模式底部固定确认栏 */
	.select-bar {
		position: fixed;
		left: 0;
		right: 0;
		bottom: 0;
		z-index: 80;
		display: flex;
		align-items: center;
		gap: 20rpx;
		padding: 18rpx 28rpx;
		padding-bottom: 18rpx;
		padding-bottom: calc(18rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(18rpx + env(safe-area-inset-bottom));
		background: #ffffff;
		box-shadow: 0 -10rpx 26rpx rgba(0, 106, 106, 0.12);
	}

	.select-bar-copy {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-width: 0;
	}

	.select-bar-count {
		color: #20252b;
		font-size: 26rpx;
		font-weight: 800;
	}

	.select-bar-hint {
		margin-top: 4rpx;
		color: #6f8085;
		font-size: 18rpx;
	}

	.select-bar .proto-primary-button {
		width: 240rpx;
		height: 78rpx;
		flex: 0 0 auto;
		margin: 0;
		font-size: 24rpx;
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
		background: #ffffff;
		color: #20252b;
		opacity: 1;
		box-shadow: 0 -14rpx 36rpx rgba(32, 37, 43, 0.18);
	}

	.guests-page .proto-mask {
		z-index: 90;
		background: rgba(32, 37, 43, 0.5);
	}

	.guests-page .editor-sheet {
		position: relative;
		z-index: 91;
		background-color: #ffffff;
	}

	.editor-sheet .proto-sheet-title,
	.editor-sheet .proto-form-label,
	.editor-sheet .default-toggle {
		color: #20252b;
	}

	.editor-sheet .proto-input {
		color: #20252b;
		background-color: #f1f7f7;
		border: 1rpx solid #d7e7e7;
	}

	.editor-sheet .select-input {
		color: #20252b;
		background-color: #f1f7f7;
		border: 1rpx solid #d7e7e7;
	}

	.editor-placeholder {
		color: #849398;
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
