<template>
	<view class="proto-page merchant-tool-page">
		<ProtoHeader title="员工管理" theme="green" />
		<CacheRefreshIndicator :refreshing="pullRefreshing" />
		<view class="staff-hero proto-card">
			<view class="staff-hero-icon"><ProtoIcon name="group" :size="42" /></view>
			<view class="staff-hero-copy"><text>协同运营成员</text><text>为民宿添加前台、管家等角色</text></view>
			<button class="proto-secondary-button" hover-class="none" @tap="openEditor">添加员工</button>
		</view>

		<view class="tool-section-heading"><text>员工列表</text><text>{{ staff.length }} 人</text></view>
		<view v-if="!staff.length" class="staff-empty proto-card">
			<ProtoIcon name="group" :size="40" />
			<text>还没有员工，点击右上角「添加员工」邀请前台或管家。</text>
		</view>
		<view v-else class="staff-list">
			<view v-for="member in staff" :key="member.id" class="staff-card proto-card">
				<view class="staff-avatar"><ProtoIcon name="user" :size="30" /></view>
				<view class="staff-copy">
					<view class="staff-title">
						<text>{{ member.name }}</text>
						<text class="staff-role">{{ member.role }}</text>
					</view>
					<text class="staff-meta">{{ member.phone }} · {{ member.gender || '未知' }} · {{ member.status }}</text>
					<view v-if="member.permissions && member.permissions.length" class="staff-perms">
						<text v-for="key in member.permissions" :key="key">{{ permissionTitle(key) }}</text>
					</view>
				</view>
			</view>
		</view>

		<view class="tool-note"><ProtoIcon name="info" :size="21" /><text>员工账号创建后需使用初始密码登录管理端并立即修改密码。</text></view>

		<view v-if="editorVisible" class="proto-mask" @tap="closeEditor">
			<view class="proto-sheet staff-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="proto-sheet-title">
					<text>添加员工</text>
					<button class="proto-close" hover-class="none" @tap="closeEditor"><ProtoIcon name="close" :size="24" /></button>
				</view>

				<text class="proto-form-label">名称 <text class="required">*</text></text>
				<input v-model="form.name" class="proto-input" placeholder="请输入员工名称" placeholder-class="sheet-placeholder" />

				<text class="proto-form-label">手机号 <text class="required">*</text></text>
				<input v-model="form.phone" class="proto-input" type="number" maxlength="11" placeholder="请输入11位手机号" placeholder-class="sheet-placeholder" @input="onPhoneInput" />
				<view class="search-hint">
					<ProtoIcon name="search" :size="18" />
					<text v-if="phoneDigits.length < 4">输入手机号第 4 位起自动搜索会员</text>
					<text v-else-if="memberSearching">正在搜索会员…</text>
					<text v-else-if="selectedMember">已匹配会员：{{ selectedMember.nickname }}</text>
					<text v-else-if="memberResults.length">找到 {{ memberResults.length }} 位会员，点击选择</text>
					<text v-else>没有匹配到会员，可继续手动填写</text>
				</view>
				<view v-if="memberResults.length" class="member-results">
					<view v-for="item in memberResults" :key="item.id" class="member-item" hover-class="member-item-hover" @tap="pickMember(item)">
						<view class="member-avatar"><ProtoIcon name="user" :size="22" /></view>
						<view class="member-copy">
							<text>{{ item.nickname }}</text>
							<text>{{ item.phone || '未绑定手机号' }} · {{ item.genderText }}</text>
						</view>
						<text class="member-pick">选择</text>
					</view>
				</view>
				<view v-if="selectedMember" class="member-selected">
					<ProtoIcon name="check" :size="17" tone="white" />
					<text>已选择 {{ selectedMember.nickname }}（{{ selectedMember.phone || '未绑定手机号' }}）</text>
					<text class="member-clear" @tap="clearMember">取消</text>
				</view>

				<text class="proto-form-label">性别</text>
				<view class="gender-row">
					<view v-for="item in genderOptions" :key="item" class="gender-chip" :class="{ active: form.gender === item }" @tap="form.gender = item">{{ item }}</view>
				</view>

				<text class="proto-form-label">民宿 <text class="required">*</text></text>
				<picker :range="storeNames" :value="storeIndex" @change="onStoreChange">
					<view class="picker-field">
						<text>{{ currentStoreName || '请选择民宿' }}</text>
						<ProtoIcon name="chevron-down" :size="20" />
					</view>
				</picker>

				<view class="perm-heading">
					<text>权限</text>
					<text>可多选，决定员工在民宿管理中的可见入口</text>
				</view>
				<view class="perm-grid">
					<view v-for="menu in permissionMenus" :key="menu.key" class="perm-item" :class="{ checked: hasPermission(menu.key) }" @tap="togglePermission(menu.key)">
						<view class="proto-check-box" :class="{ checked: hasPermission(menu.key) }">
							<ProtoIcon v-if="hasPermission(menu.key)" name="check" tone="white" :size="18" />
						</view>
						<text>{{ menu.title }}</text>
					</view>
				</view>

				<view class="editor-actions">
					<button class="proto-secondary-button" hover-class="none" @tap="closeEditor">取消</button>
					<button class="proto-primary-button" hover-class="none" :disabled="saving" @tap="save">{{ saving ? '保存中…' : '保存员工' }}</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import CacheRefreshIndicator from '@/components/CacheRefreshIndicator.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import {
		createMerchantStaff,
		fetchMerchantPermissionMenus,
		fetchMerchantStaff,
		fetchMerchantStaffOptions,
		searchMerchantMembers
	} from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'
	import { runPullRefresh } from '@/common/pull-refresh.js'

	const createEmptyForm = () => ({ name: '', phone: '', gender: '未知', storeId: '', permissions: [], memberId: '' })

	export default {
		components: { ProtoHeader, CacheRefreshIndicator, ProtoIcon },
		data() {
			return {
				staff: [],
				pullRefreshing: false,
				editorVisible: false,
				saving: false,
				stores: [],
				currentStoreId: '',
				permissionMenus: [],
				memberResults: [],
				memberSearching: false,
				selectedMember: null,
				searchTimer: null,
				searchSeq: 0,
				genderOptions: ['男', '女', '未知'],
				form: createEmptyForm()
			}
		},
		computed: {
			phoneDigits() {
				return String(this.form.phone || '').replace(/\D/g, '')
			},
			storeNames() {
				return this.stores.map((item) => item.name)
			},
			storeIndex() {
				const index = this.stores.findIndex((item) => item.id === this.form.storeId)
				return index >= 0 ? index : 0
			},
			currentStoreName() {
				const store = this.stores.find((item) => item.id === this.form.storeId)
				return store ? store.name : ''
			}
		},
		async onShow() {
			await Promise.all([this.loadStaff(), this.loadOptions()])
		},
		onPullDownRefresh() {
			return runPullRefresh(this, () => Promise.all([this.loadStaff(), this.loadOptions()]))
		},
		onUnload() {
			this.clearSearchTimer()
		},
		methods: {
			emptyForm() {
				return createEmptyForm()
			},
			async loadStaff() {
				try {
					this.staff = await fetchMerchantStaff()
				} catch (error) {
					showToast(error?.message || '员工加载失败')
				}
			},
			async loadOptions() {
				try {
					const data = await fetchMerchantStaffOptions()
					this.stores = Array.isArray(data?.stores) ? data.stores : []
					this.currentStoreId = data?.currentStoreId || (this.stores[0] && this.stores[0].id) || ''
					this.permissionMenus = Array.isArray(data?.permissions) ? data.permissions : []
				} catch (error) {
					try {
						this.permissionMenus = await fetchMerchantPermissionMenus()
					} catch (innerError) {
						// 加载失败时保留此前已加载的门店和权限选项。
					}
				}
				// 弹层已打开但门店还没加载回来时，补上当前民宿。
				if (this.editorVisible && !this.form.storeId) {
					this.form.storeId = this.currentStoreId || (this.stores[0] && this.stores[0].id) || ''
				}
			},
			openEditor() {
				this.form = this.emptyForm()
				this.form.storeId = this.currentStoreId || (this.stores[0] && this.stores[0].id) || ''
				this.selectedMember = null
				this.memberResults = []
				this.memberSearching = false
				this.editorVisible = true
				if (!this.stores.length || !this.permissionMenus.length) this.loadOptions()
			},
			closeEditor() {
				if (this.saving) return
				this.clearSearchTimer()
				this.memberResults = []
				this.editorVisible = false
			},
			clearSearchTimer() {
				if (this.searchTimer) {
					clearTimeout(this.searchTimer)
					this.searchTimer = null
				}
			},
			onPhoneInput(event) {
				const raw = String((event && event.detail && event.detail.value) || '').replace(/\D/g, '').slice(0, 11)
				this.form.phone = raw
				if (this.selectedMember && this.selectedMember.phone !== raw) {
					this.selectedMember = null
					this.form.memberId = ''
				}
				this.clearSearchTimer()
				// 只输入 1-3 位时按需求不发起请求，避免过多无效查询。
				if (raw.length < 4) {
					this.memberResults = []
					this.memberSearching = false
					return
				}
				this.searchTimer = setTimeout(() => this.searchMembers(), 350)
			},
			async searchMembers() {
				const keyword = this.phoneDigits
				if (keyword.length < 4) return
				const seq = ++this.searchSeq
				this.memberSearching = true
				try {
					const items = await searchMerchantMembers(keyword)
					if (seq !== this.searchSeq) return
					this.memberResults = items.map((item) => ({
						id: item.id,
						nickname: item.nickname || '未命名会员',
						phone: item.phone || '',
						gender: item.gender,
						genderText: this.genderText(item.gender),
						avatar: item.avatar || ''
					}))
				} catch (error) {
					if (seq === this.searchSeq) this.memberResults = []
				} finally {
					if (seq === this.searchSeq) this.memberSearching = false
				}
			},
			pickMember(member) {
				this.selectedMember = member
				this.form.memberId = member.id
				if (member.phone) this.form.phone = member.phone
				if (!String(this.form.name || '').trim()) this.form.name = member.nickname
				const gender = this.genderText(member.gender)
				if (gender !== '未知') this.form.gender = gender
				this.memberResults = []
				this.searchSeq += 1
				this.memberSearching = false
			},
			clearMember() {
				this.selectedMember = null
				this.form.memberId = ''
			},
			genderText(value) {
				const text = String(value || '').toLowerCase()
				if (text === 'male' || text === '男' || text === '1') return '男'
				if (text === 'female' || text === '女' || text === '2') return '女'
				return '未知'
			},
			onStoreChange(event) {
				const index = Number(event.detail.value)
				const store = this.stores[index]
				if (store) this.form.storeId = store.id
			},
			hasPermission(key) {
				return this.form.permissions.indexOf(key) >= 0
			},
			togglePermission(key) {
				const index = this.form.permissions.indexOf(key)
				if (index >= 0) this.form.permissions.splice(index, 1)
				else this.form.permissions.push(key)
			},
			permissionTitle(key) {
				const menu = this.permissionMenus.find((item) => item.key === key)
				return menu ? menu.title : key
			},
			async save() {
				if (this.saving) return
				if (!String(this.form.name || '').trim()) {
					showToast('请填写员工名称')
					return
				}
				if (!/^1[3-9]\d{9}$/.test(this.form.phone)) {
					showToast('请填写正确的11位手机号')
					return
				}
				if (!this.form.storeId) {
					showToast('请选择民宿')
					return
				}
				this.saving = true
				try {
					await createMerchantStaff({
						name: this.form.name.trim(),
						phone: this.form.phone,
						gender: this.form.gender,
						storeId: this.form.storeId,
						permissions: this.form.permissions.slice(),
						memberId: this.form.memberId || null
					})
					showToast('员工已添加')
					this.editorVisible = false
					await this.loadStaff()
				} catch (error) {
					showToast(error?.message || '添加失败')
				} finally {
					this.saving = false
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

	.staff-hero { display: flex; align-items: center; gap: 14rpx; padding: 20rpx; }
	.staff-hero-icon, .staff-avatar { display: flex; align-items: center; justify-content: center; color: #1f7f7f; background: #e3f3f3; border-radius: 18rpx; }
	.staff-hero-icon { width: 72rpx; height: 72rpx; flex: 0 0 72rpx; }
	.staff-hero-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.staff-hero-copy > text:first-child { color: #20252b; font-size: 26rpx; font-weight: 750; }
	.staff-hero-copy > text:last-child { margin-top: 6rpx; overflow: hidden; color: #6b7a80; font-size: 19rpx; line-height: 1.4; text-overflow: ellipsis; white-space: nowrap; }
	.staff-hero button { flex: 0 0 148rpx; min-width: 148rpx; height: 62rpx; margin: 0; padding: 0; font-size: 20rpx; white-space: nowrap; }

	.tool-section-heading { display: flex; align-items: center; justify-content: space-between; margin: 26rpx 24rpx 12rpx; color: #20252b; font-size: 27rpx; font-weight: 800; }
	.tool-section-heading > text:last-child { color: #6b7a80; font-size: 19rpx; font-weight: 400; }

	.staff-empty { display: flex; align-items: center; gap: 16rpx; margin: 0 0 8rpx; padding: 30rpx 24rpx; color: #6b7a80; font-size: 21rpx; line-height: 1.55; }
	.staff-empty .proto-icon { flex: 0 0 auto; color: #b9cccd; }

	.staff-list { display: flex; flex-direction: column; gap: 14rpx; }
	.staff-card { display: flex; align-items: flex-start; gap: 14rpx; padding: 20rpx; }
	.staff-avatar { width: 66rpx; height: 66rpx; flex: 0 0 66rpx; }
	.staff-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.staff-title { display: flex; align-items: center; gap: 10rpx; }
	.staff-title > text:first-child { overflow: hidden; color: #20252b; font-size: 26rpx; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
	.staff-role { flex: 0 0 auto; padding: 2rpx 12rpx; color: #1f7f7f; background: #e3f3f3; border-radius: 8rpx; font-size: 18rpx; }
	.staff-meta { margin-top: 8rpx; overflow: hidden; color: #6b7a80; font-size: 20rpx; text-overflow: ellipsis; white-space: nowrap; }
	.staff-perms { display: flex; flex-wrap: wrap; gap: 8rpx; margin-top: 10rpx; }
	.staff-perms > text { padding: 3rpx 12rpx; color: #55707a; background: #eef5f6; border-radius: 7rpx; font-size: 17rpx; }

	.tool-note { display: flex; align-items: flex-start; gap: 8rpx; margin: 22rpx 30rpx; color: #6b7a80; font-size: 19rpx; line-height: 1.5; }
	.tool-note .proto-icon { flex: 0 0 auto; }

	.staff-sheet { max-height: 88vh; background-color: #ffffff; }
	.staff-sheet .proto-input {
		color: #20252b;
		background-color: #f1f7f7;
		border: 1rpx solid #d7e7e7;
	}
	.sheet-placeholder { color: #9aa8ac; }
	.required { color: #e05b4b; }

	.search-hint { display: flex; align-items: center; gap: 8rpx; margin-top: 12rpx; color: #6b7a80; font-size: 19rpx; line-height: 1.45; }
	.search-hint .proto-icon { flex: 0 0 auto; color: #8fa5a8; }

	.member-results { margin-top: 12rpx; overflow: hidden; background: #f7fbfb; border: 1rpx solid #dfecee; border-radius: 16rpx; }
	.member-item { display: flex; align-items: center; gap: 12rpx; padding: 16rpx 18rpx; border-bottom: 1rpx solid #e8f1f2; }
	.member-item:last-child { border-bottom: 0; }
	.member-item-hover { background: #eef7f7; }
	.member-avatar { display: flex; align-items: center; justify-content: center; width: 52rpx; height: 52rpx; flex: 0 0 52rpx; color: #1f7f7f; background: #e3f3f3; border-radius: 50%; }
	.member-copy { display: flex; flex: 1; min-width: 0; flex-direction: column; }
	.member-copy > text:first-child { overflow: hidden; color: #20252b; font-size: 23rpx; font-weight: 700; text-overflow: ellipsis; white-space: nowrap; }
	.member-copy > text:last-child { margin-top: 4rpx; color: #6b7a80; font-size: 18rpx; }
	.member-pick { flex: 0 0 auto; padding: 6rpx 18rpx; color: #ffffff; background: #2aa9a9; border-radius: 24rpx; font-size: 19rpx; }

	.member-selected { display: flex; align-items: center; gap: 8rpx; margin-top: 12rpx; padding: 12rpx 16rpx; color: #1f7f7f; background: #e3f3f3; border-radius: 12rpx; font-size: 19rpx; }
	.member-selected .proto-icon { flex: 0 0 auto; }
	.member-selected > text:nth-child(2) { flex: 1; min-width: 0; }
	.member-clear { flex: 0 0 auto; color: #6b7a80; text-decoration: underline; }

	.gender-row { display: flex; gap: 14rpx; }
	.gender-chip { display: flex; align-items: center; justify-content: center; min-width: 132rpx; height: 66rpx; color: #55707a; background: #f1f7f7; border: 1rpx solid #d7e7e7; border-radius: 33rpx; font-size: 22rpx; }
	.gender-chip.active { color: #ffffff; background: #2aa9a9; border-color: #2aa9a9; font-weight: 700; }

	.picker-field { display: flex; align-items: center; justify-content: space-between; height: 76rpx; padding: 0 20rpx; color: #20252b; background: #f1f7f7; border: 1rpx solid #d7e7e7; border-radius: 16rpx; font-size: 24rpx; }

	.perm-heading { display: flex; align-items: baseline; justify-content: space-between; margin: 24rpx 0 12rpx; }
	.perm-heading > text:first-child { color: #20252b; font-size: 23rpx; font-weight: 700; }
	.perm-heading > text:last-child { color: #6b7a80; font-size: 17rpx; }
	.perm-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10rpx 12rpx; }
	.perm-item { display: flex; align-items: center; gap: 10rpx; padding: 14rpx 14rpx; color: #55707a; background: #f7fbfb; border: 1rpx solid #dfecee; border-radius: 12rpx; font-size: 20rpx; }
	.perm-item.checked { color: #1f7f7f; background: #e9f6f6; border-color: #9fd6d6; }
	.perm-item .proto-check-box { margin: 0; flex: 0 0 auto; width: 32rpx; height: 32rpx; border-width: 2rpx; }
	.perm-item > text { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

	.editor-actions { display: flex; gap: 14rpx; margin-top: 28rpx; }
	.editor-actions button { flex: 1; margin: 0; }
</style>
