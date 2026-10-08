<template>
	<view class="proto-page merchant-publish-page">
		<ProtoHeader title="发布房源" theme="green" />
		<view class="publish-form">
			<view class="publish-cover" @tap="chooseCover">
				<image v-if="cover" :src="cover" mode="aspectFill" />
				<template v-else><ProtoIcon name="image" :size="46" /><text>上传房源图片</text><text class="publish-cover-caption">建议上传采光清晰的房间照片</text></template>
				<text v-if="cover" class="publish-cover-change">点击更换封面</text>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">基础信息</text>
				<text class="publish-label">出租房源长标题</text>
				<input v-model="form.title" class="publish-input" placeholder="请输入房源长标题" placeholder-class="publish-placeholder" />
				<text class="publish-label">房间名称</text>
				<input v-model="form.roomName" class="publish-input" placeholder="例如：阳光实木标准大床房" placeholder-class="publish-placeholder" />
				<text class="publish-label">房源类型</text>
				<view class="publish-chip-grid">
					<text v-for="item in roomTypes" :key="item" class="publish-chip" :class="{ selected: form.type === item }" @tap="form.type = item">{{ item }}</text>
				</view>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">价格与库存</text>
				<view class="publish-grid publish-grid-2">
					<view><text class="publish-label">房租默认价格</text><view class="publish-number"><text>¥</text><input v-model="form.price" type="number" placeholder="请输入" placeholder-class="publish-placeholder" /><text> /晚</text></view></view>
					<view><text class="publish-label">房屋默认库存</text><input v-model="form.stock" class="publish-input" type="number" placeholder="1" placeholder-class="publish-placeholder" /></view>
				</view>
				<view class="publish-grid publish-grid-3">
					<view><text class="publish-label">面积</text><input v-model="form.area" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">朝向</text><input v-model="form.orientation" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">户型</text><input v-model="form.layout" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
				</view>
				<view class="publish-grid publish-grid-2">
					<view><text class="publish-label">居室</text><input v-model="form.rooms" class="publish-input" type="number" placeholder="0" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">床铺</text><input v-model="form.beds" class="publish-input" placeholder="请输入" placeholder-class="publish-placeholder" /></view>
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
				<view class="publish-chip-grid">
					<text v-for="item in section.items" :key="item" class="publish-chip" :class="{ selected: form[section.key].includes(item) }" @tap="toggleOption(section.key, item)">{{ item }}</text>
				</view>
			</view>

			<view class="publish-section">
				<text class="publish-section-title">预定须知</text>
				<view class="publish-grid publish-grid-2">
					<view><text class="publish-label">入住时间</text><input v-model="form.checkIn" class="publish-input" placeholder="例如 14:00" placeholder-class="publish-placeholder" /></view>
					<view><text class="publish-label">退房时间</text><input v-model="form.checkOut" class="publish-input" placeholder="例如 12:00" placeholder-class="publish-placeholder" /></view>
				</view>
				<text class="publish-label">退订规则</text>
				<textarea v-model="form.cancelRule" class="publish-textarea" placeholder="请输入退订规则" placeholder-class="publish-placeholder" />
				<text class="publish-label">房屋介绍</text>
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
	import { prototypeImages, showToast } from '@/common/prototype.js'
	import { findMerchantRoom, upsertMerchantRoom } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				editingId: '',
				cover: '',
				roomTypes: ['独立房间', '电竞房', '阁楼', '江景房', '山景房', '海景房', '小区', '花园洋房', '老式公寓', '别墅'],
				optionSections: [
					{ key: 'tags', title: '民宿标签', items: ['交通方便', '近景区', '近地铁', '近商圈', '现代风', '超赞房东', '私家花园'] },
					{ key: 'services', title: '民宿服务', items: ['围炉煮茶', '免费接送', '前台接待', '管家服务', '行李寄存', '免费停车'] },
					{ key: 'facilities', title: '基础设施', items: ['零食、 水', '网络电视', '空调', '地暖', '窗户', '无线网络'] },
					{ key: 'bathroom', title: '卫浴设施', items: ['电吹风', '独立卫浴', '热水'] },
					{ key: 'nearby', title: '周边设施', items: ['医院', '商场', '餐厅', '便利店', '超市'] }
				],
				form: {
					title: '', roomName: '', type: '独立房间', price: '', stock: '1', area: '', orientation: '', layout: '', rooms: '0', beds: '', minGuests: '0', maxGuests: '0', checkIn: '', checkOut: '', cancelRule: '', description: '',
					tags: [], services: [], facilities: [], bathroom: [], nearby: []
				}
			}
		},
		onLoad(options = {}) {
			if (!options.id) return
			const room = findMerchantRoom(options.id)
			if (!room) return
			this.editingId = room.id
			this.cover = room.image || ''
			this.form = {
				...this.form,
				title: room.title || room.name || '',
				roomName: room.name || '',
				type: room.type || '独立房间',
				price: room.price || '',
				stock: room.stock || '1',
				area: room.area || '',
				capacity: room.capacity || ''
			}
		},
		methods: {
			chooseCover() {
				if (!uni.chooseImage) {
					showToast('当前预览环境暂不支持选图')
					return
				}
				uni.chooseImage({
					count: 1,
					sourceType: ['album', 'camera'],
					success: ({ tempFilePaths = [] }) => {
						if (tempFilePaths[0]) this.cover = tempFilePaths[0]
					},
					fail: () => showToast('未选择图片')
				})
			},
			toggleOption(key, item) {
				const values = this.form[key]
				const index = values.indexOf(item)
				if (index >= 0) values.splice(index, 1)
				else values.push(item)
			},
			saveDraft() {
				if (!this.form.roomName.trim()) {
					showToast('请先填写房间名称')
					return
				}
				this.saveRoom('草稿')
				showToast('草稿已保存')
				setTimeout(() => uni.navigateBack({ delta: 1 }), 360)
			},
			publish() {
				if (!this.form.title.trim() || !this.form.roomName.trim() || !this.form.price) {
					showToast('请完善标题、房间名称和价格')
					return
				}
				this.saveRoom('已上架')
				showToast('房源已发布（本地演示）')
				setTimeout(() => uni.navigateBack({ delta: 1 }), 360)
			},
			saveRoom(status) {
				const minGuests = Number(this.form.minGuests || 0)
				const maxGuests = Number(this.form.maxGuests || 0)
				upsertMerchantRoom({
					id: this.editingId || undefined,
					title: this.form.title.trim(),
					name: this.form.roomName.trim(),
					type: this.form.type,
					status,
					key: status === '已上架' ? 'online' : 'draft',
					price: this.form.price,
					stock: this.form.stock || '1',
					capacity: maxGuests ? `可住${minGuests || 1}-${maxGuests}人 · ${this.form.beds || '床型待完善'}` : (this.form.beds ? `可住2人 · ${this.form.beds}` : '入住人数待完善'),
					area: this.form.area || '待完善',
					image: this.cover || prototypeImages.roomOne
				})
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

	.publish-cover {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		width: 100%;
		height: 230rpx;
		color: var(--proto-primary-dark);
		background: #e8f7f8;
		border: 2rpx dashed #9fdadd;
		border-radius: 24rpx;
		overflow: hidden;
	}

	.publish-cover > image {
		width: 100%;
		height: 100%;
	}

	.publish-cover-change {
		position: absolute;
		right: 18rpx;
		bottom: 16rpx;
		padding: 8rpx 14rpx;
		color: #ffffff;
		background: rgba(0, 106, 106, 0.84);
		border-radius: 20rpx;
		font-size: 17rpx;
	}

	.publish-cover > text:nth-child(2) {
		margin-top: 10rpx;
		font-size: 24rpx;
		font-weight: 750;
	}

	.publish-cover-caption {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
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
