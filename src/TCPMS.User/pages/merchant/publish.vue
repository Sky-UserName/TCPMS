<template>
	<view class="proto-page merchant-publish-page">
		<ProtoHeader title="发布房源" theme="green" />
		<view class="publish-form">
			<view class="publish-section publish-image-section">
				<view class="publish-section-heading">
					<text class="publish-section-title">房源图片</text>
					<text class="publish-image-count">{{ roomImages.length }}/9</text>
				</view>
				<view class="publish-image-grid">
					<view v-for="(image, index) in roomImages" :key="`${image}-${index}`" class="publish-image-item">
						<image :src="image" mode="aspectFill" @tap="previewImages(index)" />
						<text v-if="index === 0" class="publish-image-badge">封面</text>
						<view v-else class="publish-image-action" @tap.stop="setCover(index)">设为封面</view>
						<view class="publish-image-delete" @tap.stop="removeImage(index)"><ProtoIcon name="close" tone="white" :size="18" /></view>
					</view>
					<view v-if="roomImages.length < 9" class="publish-image-add" @tap="chooseImages">
						<ProtoIcon name="plus" :size="34" />
						<text>{{ uploading ? '上传中…' : '上传图片' }}</text>
					</view>
				</view>
				<text class="publish-image-tip">最多 9 张，第一张自动作为封面，可逐张删除</text>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">基础信息</text>
				<text class="publish-label">出租房源长标题</text>
				<input v-model="form.title" class="publish-input" placeholder="请输入房源长标题" placeholder-class="publish-placeholder" />
				<text class="publish-label">房间名称</text>
				<input v-model="form.roomName" class="publish-input" placeholder="例如：阳光实木标准大床房" placeholder-class="publish-placeholder" />
				<text class="publish-label">房间类型</text>
				<view class="publish-chip-grid">
					<text v-for="item in kindOptions" :key="item.value" class="publish-chip" :class="{ selected: form.kind === item.value }" @tap="selectKind(item.value)">{{ item.label }}</text>
				</view>
				<text v-if="form.kind === 'Bed'" class="publish-label">性别限制</text>
				<view v-if="form.kind === 'Bed'" class="publish-chip-grid">
					<text v-for="item in genderOptions" :key="item.value" class="publish-chip" :class="{ selected: form.gender === item.value }" @tap="form.gender = item.value">{{ item.label }}</text>
				</view>
				<text class="publish-label">房型分类</text>
				<view v-if="roomCategories.length" class="publish-chip-grid">
					<text v-for="item in roomCategories" :key="`${item.name}-${item.kind}`" class="publish-chip" :class="{ selected: form.type === item.name && form.kind === item.kind }" @tap="pickCategory(item)">{{ item.name }}{{ item.kind === 'Bed' ? ' · 床位' : '' }}</text>
					<text class="publish-chip publish-chip-custom" :class="{ selected: showCustomCategory }" @tap="toggleCustomCategory">自定义</text>
				</view>
				<input v-if="showCustomCategory || !roomCategories.length" v-model="form.type" class="publish-input" placeholder="请输入房型分类，例如：大床房" placeholder-class="publish-placeholder" />
				<text v-if="optionsLoading" class="publish-option-tip">正在加载房间选项…</text>
				<text v-else-if="!roomCategories.length" class="publish-option-tip">还没有房型分类，直接输入即可；保存后会进入后台房型分类列表。</text>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">价格与库存</text>
				<view class="publish-grid publish-grid-2">
					<view><text class="publish-label">房租默认价格</text><view class="publish-number"><text>¥</text><input v-model="form.price" type="number" placeholder="请输入" placeholder-class="publish-placeholder" /><text> /晚</text></view></view>
					<view>
						<text class="publish-label">{{ form.kind === 'Bed' ? '床铺数量' : '房屋默认库存' }}</text>
						<input v-if="form.kind === 'Bed'" v-model="form.bedStock" class="publish-input" type="number" placeholder="请输入床铺数量" placeholder-class="publish-placeholder" />
						<input v-else v-model="form.stock" class="publish-input" type="number" placeholder="1" placeholder-class="publish-placeholder" />
						<text v-if="form.kind === 'Bed'" class="publish-field-hint">库存按床铺数量计算</text>
					</view>
				</view>
				<view class="publish-grid publish-grid-3">
					<view><text class="publish-label">面积</text><input v-model="form.area" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">朝向</text><input v-model="form.orientation" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">户型</text><input v-model="form.layout" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
				</view>
				<view v-if="form.kind !== 'Bed'" class="publish-grid publish-grid-2">
					<view><text class="publish-label">房间数量</text><input v-model="form.rooms" class="publish-input" type="number" placeholder="0" placeholder-class="publish-placeholder" /></view>
				</view>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">容纳人数</text>
				<view class="publish-grid publish-grid-2">
					<view><text class="publish-label">最低人数</text><input v-model="form.minGuests" class="publish-input" type="number" placeholder="0" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">最高人数</text><input v-model="form.maxGuests" class="publish-input" type="number" placeholder="0" placeholder-class="publish-placeholder" /></view>
				</view>
			</view>

			<view v-for="section in optionSections" :key="section.key" class="publish-section">
				<text class="publish-section-title">{{ section.title }}</text>
				<view v-if="section.items.length" class="publish-chip-grid">
					<text v-for="item in section.items" :key="item" class="publish-chip" :class="{ selected: form[section.key].includes(item) }" @tap="toggleOption(section.key, item)">{{ item }}</text>
				</view>
				<text v-else class="publish-option-tip">暂无{{ section.title }}选项，请到后台添加</text>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">房屋介绍</text>
				<textarea v-model="form.description" class="publish-textarea" placeholder="介绍房间特色、采光和入住体验" placeholder-class="publish-placeholder" />
			</view>
		</view>

		<view class="publish-actions">
			<button class="proto-secondary-button" hover-class="none" @tap="saveDraft">保存草稿</button>
			<button class="proto-primary-button" hover-class="none" @tap="publish">立即发布</button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { showToast } from '@/common/prototype.js'
	import { fetchMerchantRoom, fetchMerchantRoomOptions, saveMerchantRoom, uploadMerchantRoomImage } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				editingId: '',
				roomImages: [],
				uploading: false,
				optionsLoading: false,
				showCustomCategory: false,
				roomCategories: [],
				kindOptions: [
					{ label: '独立房间', value: 'PrivateRoom' },
					{ label: '多人间床位', value: 'Bed' }
				],
				genderOptions: [
					{ label: '不限性别', value: 'Any' },
					{ label: '女生专区', value: 'Female' },
					{ label: '男生专区', value: 'Male' }
				],
				optionSections: [],
				form: {
					title: '', roomName: '', kind: 'PrivateRoom', type: '', gender: 'Any', price: '', stock: '1', bedStock: '1',
					area: '', orientation: '', layout: '', rooms: '0', minGuests: '0', maxGuests: '0',
					description: '',
					tags: [], services: [], facilities: [], bathroom: [], nearby: []
				}
			}
		},
		async onLoad(options = {}) {
			await this.loadOptions()
			if (!options.id) return
			try {
				const room = await fetchMerchantRoom(options.id)
				if (!room) return
				this.editingId = room.id
				this.roomImages = Array.isArray(room.images) && room.images.length
					? room.images.slice(0, 9)
					: (room.image ? [room.image] : [])
				const type = room.roomCategory || room.type || ''
				const kind = room.kind === 'Bed' || String(type).includes('床位') ? 'Bed' : 'PrivateRoom'
				this.form = {
					...this.form,
					title: room.title || room.name || '',
					roomName: room.name || '',
					kind,
					type,
					gender: room.gender || 'Any',
					price: room.price || '',
					stock: room.stock || '1',
					bedStock: String(room.bedCount || room.stock || 1),
					area: String(room.area || '').replace('㎡', ''),
					orientation: room.orientation || '',
					layout: room.layout || '',
					rooms: String(room.roomCount || 1),
					minGuests: '1',
					maxGuests: String(room.maxGuests || 2),
					description: room.description || '',
					tags: room.tags || [],
					services: room.services || [],
					facilities: room.facilities || [],
					bathroom: room.bathroomFacilities || [],
					nearby: room.nearbyFacilities || []
				}
				this.showCustomCategory = Boolean(type) && !this.roomCategories.some((item) => item.name === type)
			} catch (error) {
				showToast(error?.message || '房源加载失败')
			}
		},
		methods: {
			async loadOptions() {
				this.optionsLoading = true
				try {
					const data = await fetchMerchantRoomOptions()
					this.roomCategories = Array.isArray(data.categories) ? data.categories : []
					this.optionSections = [
						{ key: 'tags', title: '民宿标签', items: Array.isArray(data.tags) ? data.tags : [] },
						{ key: 'services', title: '民宿服务', items: Array.isArray(data.services) ? data.services : [] },
						{ key: 'facilities', title: '基础设施', items: Array.isArray(data.facilities) ? data.facilities : [] },
						{ key: 'bathroom', title: '卫浴设施', items: Array.isArray(data.bathroomFacilities) ? data.bathroomFacilities : [] },
						{ key: 'nearby', title: '周边设施', items: Array.isArray(data.nearbyFacilities) ? data.nearbyFacilities : [] }
					]
					if (!this.roomCategories.length) this.showCustomCategory = true
				} catch (error) {
					showToast(error?.message || '房间选项加载失败')
					this.roomCategories = []
					this.optionSections = []
					this.showCustomCategory = true
				} finally {
					this.optionsLoading = false
				}
			},
			selectKind(value) {
				this.form.kind = value === 'Bed' ? 'Bed' : 'PrivateRoom'
				if (this.form.kind === 'Bed' && !this.form.bedStock) this.form.bedStock = '1'
			},
			pickCategory(item) {
				this.form.type = item.name
				this.form.kind = item.kind === 'Bed' ? 'Bed' : 'PrivateRoom'
				this.showCustomCategory = false
				if (this.form.kind === 'Bed' && !this.form.bedStock) this.form.bedStock = '1'
			},
			toggleCustomCategory() {
				this.showCustomCategory = true
				this.form.type = ''
			},
			async chooseImages() {
				if (this.uploading) return
				const remain = 9 - this.roomImages.length
				if (remain <= 0) {
					showToast('最多上传 9 张图片')
					return
				}
				if (!uni.chooseImage) {
					showToast('当前预览环境暂不支持选图')
					return
				}
				uni.chooseImage({
					count: remain,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async ({ tempFilePaths = [] }) => {
						if (!tempFilePaths.length) return
						this.uploading = true
						uni.showLoading({ title: '上传中...', mask: true })
						try {
							const urls = []
							for (const filePath of tempFilePaths) {
								const result = await uploadMerchantRoomImage(filePath)
								if (result?.url) urls.push(result.url)
							}
							this.roomImages = [...this.roomImages, ...urls].slice(0, 9)
							if (!urls.length) showToast('图片上传失败')
						} catch (error) {
							showToast(error?.message || '图片上传失败')
						} finally {
							this.uploading = false
							uni.hideLoading()
						}
					},
					fail: () => {}
				})
			},
			removeImage(index) {
				this.roomImages.splice(index, 1)
			},
			setCover(index) {
				if (index <= 0 || index >= this.roomImages.length) return
				const [image] = this.roomImages.splice(index, 1)
				this.roomImages.unshift(image)
			},
			previewImages(index) {
				uni.previewImage({
					urls: this.roomImages,
					current: this.roomImages[index]
				})
			},
			toggleOption(key, item) {
				const values = this.form[key]
				const index = values.indexOf(item)
				if (index >= 0) values.splice(index, 1)
				else values.push(item)
			},
			async saveDraft() {
				if (!this.form.roomName.trim()) {
					showToast('请先填写房间名称')
					return
				}
				const saved = await this.saveRoom('草稿')
				if (!saved) return
				showToast('草稿已保存')
				setTimeout(() => uni.navigateBack({ delta: 1 }), 360)
			},
			async publish() {
				if (!this.form.title.trim() || !this.form.roomName.trim() || !this.form.price) {
					showToast('请完善标题、房间名称和价格')
					return
				}
				if (this.form.kind === 'Bed' && Number(this.form.bedStock) <= 0) {
					showToast('请填写床铺数量')
					return
				}
				if (!this.roomImages.length) {
					showToast('请至少上传一张房源图片')
					return
				}
				const saved = await this.saveRoom('已上架')
				if (!saved) return
				showToast('房源已发布')
				setTimeout(() => uni.navigateBack({ delta: 1 }), 360)
			},
			async saveRoom(status) {
				if (!String(this.form.type || '').trim()) {
					showToast('请选择或填写房型分类')
					return null
				}
				const kind = this.form.kind === 'Bed' ? 'Bed' : 'PrivateRoom'
				const bedStock = Math.max(Number(this.form.bedStock) || 1, 1)
				const stock = kind === 'Bed' ? bedStock : Math.max(Number(this.form.stock) || 1, 1)
				const rooms = kind === 'Bed' ? 1 : Math.max(Number(this.form.rooms) || 1, 1)
				const beds = kind === 'Bed' ? bedStock : 1
				const payload = {
					name: this.form.roomName.trim(),
					title: this.form.title.trim(),
					type: String(this.form.type || '').trim(),
					kind,
					gender: kind === 'Bed' ? this.form.gender : null,
					priceCents: Math.round(Number(this.form.price || 0) * 100),
					stock,
					area: this.form.area ? Number(this.form.area) : null,
					orientation: this.form.orientation,
					layout: this.form.layout,
					rooms,
					beds,
					maxGuests: Number(this.form.maxGuests || 2),
					description: this.form.description,
					tags: this.form.tags,
					services: this.form.services,
					facilities: this.form.facilities,
					bathroomFacilities: this.form.bathroom,
					nearbyFacilities: this.form.nearby,
					imageUrls: this.roomImages,
					imageUrl: this.roomImages[0] || '',
					published: status === '已上架'
				}
				try {
					const room = await saveMerchantRoom(this.editingId, payload)
					this.editingId = room.id
					return room
				} catch (error) {
					showToast(error?.message || '房源保存失败')
					return null
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.merchant-publish-page {
		padding-bottom: 150rpx;
		background: #f4fafb;
	}

	.merchant-publish-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.publish-form {
		padding: 18rpx 24rpx 24rpx;
	}

	.publish-image-section {
		margin-top: 0;
	}

	.publish-section-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.publish-section-heading .publish-section-title {
		margin-bottom: 0;
	}

	.publish-image-count {
		color: var(--proto-muted, #6f8085);
		font-size: 19rpx;
	}

	.publish-image-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
		margin-top: 18rpx;
	}

	.publish-image-item,
	.publish-image-add {
		position: relative;
		width: calc(33.3333% - 10rpx);
		height: 198rpx;
		overflow: hidden;
		border-radius: 18rpx;
	}

	.publish-image-item {
		background: #eef4f6;
	}

	.publish-image-item > image {
		width: 100%;
		height: 100%;
	}

	.publish-image-badge {
		position: absolute;
		top: 0;
		left: 0;
		padding: 6rpx 14rpx;
		color: #ffffff;
		background: rgba(0, 106, 106, 0.86);
		border-bottom-right-radius: 14rpx;
		font-size: 16rpx;
	}

	.publish-image-action {
		position: absolute;
		right: 0;
		bottom: 0;
		left: 0;
		height: 38rpx;
		color: #ffffff;
		background: rgba(12, 30, 34, 0.46);
		font-size: 17rpx;
		line-height: 38rpx;
		text-align: center;
	}

	.publish-image-delete {
		position: absolute;
		top: 8rpx;
		right: 8rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 40rpx;
		height: 40rpx;
		background: rgba(18, 34, 38, 0.62);
		border-radius: 50%;
	}

	.publish-image-add {
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		gap: 8rpx;
		color: #4f8f93;
		background: #f1fafb;
		border: 2rpx dashed #a6dcdf;
		font-size: 19rpx;
	}

	.publish-image-tip {
		display: block;
		margin-top: 14rpx;
		color: #8d9aa0;
		font-size: 18rpx;
	}

	.publish-section {
		margin-top: 16rpx;
		padding: 24rpx 20rpx 28rpx;
		background: #ffffff;
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 22rpx;
	}

	.publish-section-title {
		display: block;
		margin-bottom: 18rpx;
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 800;
	}

	.publish-label {
		display: block;
		margin: 18rpx 0 10rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.publish-input,
	.publish-textarea,
	.publish-number {
		box-sizing: border-box;
		width: 100%;
		color: var(--proto-text);
		background: #f5f8fa;
		border: 1rpx solid transparent;
		border-radius: 14rpx;
		font-size: 23rpx;
	}

	.publish-input {
		height: 72rpx;
		padding: 0 18rpx;
	}

	.publish-number {
		display: flex;
		align-items: center;
		height: 72rpx;
		padding: 0 18rpx;
		color: var(--proto-primary-dark);
	}

	.publish-number input {
		flex: 1;
		min-width: 0;
		height: 100%;
		padding: 0 8rpx;
		color: var(--proto-text);
		font-size: 23rpx;
	}

	.publish-textarea {
		min-height: 150rpx;
		padding: 16rpx;
		line-height: 1.5;
	}

	.publish-placeholder {
		color: #9ba6ac;
	}

	.publish-grid {
		display: flex;
		gap: 16rpx;
	}

	.publish-grid > view {
		min-width: 0;
	}

	.publish-grid > view,
	.publish-chip-grid,
	.publish-actions button {
		box-sizing: border-box;
	}

	.publish-grid-2 > view {
		width: calc(50% - 8rpx);
	}

	.publish-grid-3 > view {
		width: calc(33.3333% - 11rpx);
	}

	.publish-chip-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 14rpx;
	}

	.publish-chip {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 126rpx;
		height: 58rpx;
		padding: 0 16rpx;
		color: var(--proto-text);
		background: #f2f6f8;
		border: 1rpx solid transparent;
		border-radius: 12rpx;
		font-size: 20rpx;
	}

	.publish-chip.selected {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-color: #9edcdd;
		font-weight: 750;
	}

	.publish-chip-custom {
		color: #316d70;
		background: #ffffff;
		border-color: #b9dfe0;
		border-style: dashed;
	}

	.publish-option-tip {
		display: block;
		margin-top: 10rpx;
		color: #6f8085;
		font-size: 19rpx;
		line-height: 1.5;
	}

	.publish-field-hint {
		display: block;
		margin-top: 6rpx;
		color: #6f8085;
		font-size: 17rpx;
	}

	.publish-actions {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 40;
		display: flex;
		gap: 16rpx;
		padding: 14rpx 24rpx;
		padding-bottom: calc(14rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(197, 199, 202, 0.4);
		box-shadow: 0 -8rpx 24rpx rgba(32, 37, 43, 0.06);
	}

	.publish-actions button {
		flex: 1;
		min-width: 0;
		margin: 0;
		height: 78rpx;
		white-space: nowrap;
	}
	
	.publish-actions .proto-secondary-button {
		color: var(--proto-primary-dark);
		background: #eaf8f8;
		border-color: #a8dddd;
	}
</style>
