<template>
	<view class="apply-page">
		<view class="apply-hero">
			<image class="apply-hero-image" :src="heroImageSrc" mode="aspectFill" @error="useLocalHero" />
			<view class="apply-hero-mask"></view>
			<view class="apply-nav" :style="{ paddingTop: statusBarHeightRpx + 'rpx', height: 88 + statusBarHeightRpx + 'rpx' }">
				<button class="apply-nav-button" hover-class="none" aria-label="返回" @tap="goBack">
					<ProtoIcon name="back" tone="white" :size="38" />
				</button>
				<text class="apply-nav-title">{{ pageTitle }}</text>
				<view class="apply-nav-tools">
					<ProtoIcon name="more" tone="white" :size="32" />
				</view>
			</view>
		</view>

		<view class="apply-card">
			<view v-if="statusNotice" class="apply-status" :class="`status-${applicationStatus}`">
				<view class="status-dot"></view>
				<text>{{ statusNotice }}</text>
			</view>

			<view class="apply-field">
				<text class="field-label">民宿名称</text>
				<input v-model.trim="form.name" class="field-input" maxlength="80" placeholder="请输入店铺名称" placeholder-class="field-placeholder" />
			</view>
			<view class="apply-field">
				<text class="field-label">房东姓名</text>
				<input v-model.trim="form.ownerName" class="field-input" maxlength="40" placeholder="请输入联系人姓名" placeholder-class="field-placeholder" />
			</view>
			<view class="apply-field">
				<text class="field-label">房东电话</text>
				<input v-model.trim="form.ownerPhone" class="field-input" type="number" maxlength="11" placeholder="请输入联系电话" placeholder-class="field-placeholder" />
			</view>
			<view class="apply-field apply-field-clickable" @tap="openLocationPicker">
				<text class="field-label">民宿位置</text>
				<view class="field-value-wrap">
					<text class="field-value" :class="{ empty: !locationLabel }">{{ locationLabel || '点击地图选点' }}</text>
					<ProtoIcon name="chevron-right" :size="24" />
				</view>
			</view>
			<text v-if="!locationLabel" class="field-hint">选点后自动填入省市区，详细地址同步带到下方</text>
			<view class="apply-textarea-field">
				<text class="field-label">民宿详细地址</text>
				<textarea v-model.trim="form.address" class="field-textarea" maxlength="160" placeholder="请输入详细地址" placeholder-class="field-placeholder" />
			</view>
			<view class="apply-field apply-field-clickable" @tap="chooseBusinessArea">
				<text class="field-label">选择商圈</text>
				<view class="field-value-wrap">
					<text class="field-value" :class="{ empty: !form.district }">{{ form.district || '请选择商圈' }}</text>
					<ProtoIcon name="chevron-right" :size="24" />
				</view>
			</view>

			<view class="apply-upload-section">
				<text class="upload-section-title">身份证正反面</text>
				<view class="upload-grid">
					<view v-for="item in identityUploads" :key="item.key" class="upload-box" @tap="pickImage(item.key)">
						<image v-if="form[item.key]" class="upload-preview" :src="form[item.key]" mode="aspectFill" />
						<view v-else class="upload-empty">
							<view class="upload-plus"><ProtoIcon name="plus" tone="white" :size="24" /></view>
							<text>{{ item.label }}</text>
						</view>
					</view>
				</view>
			</view>

			<view v-for="item in documentUploads" :key="item.key" class="apply-upload-section">
				<text class="upload-section-title">{{ item.label }}</text>
				<view class="upload-box" @tap="pickImage(item.key)">
					<image v-if="form[item.key]" class="upload-preview" :src="form[item.key]" mode="aspectFill" />
					<view v-else class="upload-empty">
						<view class="upload-plus"><ProtoIcon name="plus" tone="white" :size="24" /></view>
						<text>上传图片</text>
					</view>
				</view>
			</view>

			<view class="apply-upload-section room-upload-section">
				<view class="upload-title-row">
					<text class="upload-section-title">房间照片</text>
					<text class="upload-count">{{ form.roomImageUrls.length }}/9</text>
				</view>
				<view class="room-upload-grid">
					<view v-for="(image, index) in form.roomImageUrls" :key="`${image}-${index}`" class="upload-box room-upload-box">
						<image class="upload-preview" :src="image" mode="aspectFill" />
						<button class="image-remove" hover-class="none" aria-label="删除图片" @tap.stop="removeRoomImage(index)">
							<ProtoIcon name="close" tone="white" :size="20" />
						</button>
					</view>
					<view v-if="form.roomImageUrls.length < 9" class="upload-box room-upload-box" @tap="pickImage('roomImageUrls', true)">
						<view class="upload-empty">
							<view class="upload-plus"><ProtoIcon name="plus" tone="white" :size="24" /></view>
							<text>上传图片</text>
						</view>
					</view>
				</view>
			</view>

			<text class="apply-tip">图片仅用于入驻审核，请确保内容清晰、真实有效</text>
		</view>

		<view class="apply-submit-bar">
			<button class="apply-submit" :disabled="submitting || uploadingKey" hover-class="none" @tap="submitApplication">
				{{ submitting ? '提交中...' : (isEditing ? '保存并提交审核' : '确定申请') }}
			</button>
		</view>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { fetchMerchantApplication, submitMerchantApplication, uploadMerchantApplicationImage } from '@/common/api.js'
	import { showToast } from '@/common/prototype.js'
	import { brandImages, brandImageFallbacks } from '@/common/brand-assets.js'

	// 省市区重名（直辖市）只保留一次，拼成「河南省郑州市金水区」这样的位置文案。
	const composeRegion = (province, city, district) => {
		const parts = []
		;[province, city, district].forEach((raw) => {
			const value = String(raw || '').trim()
			if (!value || parts.includes(value)) return
			parts.push(value)
		})
		return parts.join('')
	}

	const emptyForm = () => ({
		name: '',
		ownerName: '',
		ownerPhone: '',
		province: '',
		city: '',
		district: '',
		address: '',
		standardAddress: '',
		longitude: null,
		latitude: null,
		description: '',
		idCardFrontUrl: '',
		idCardBackUrl: '',
		propertyCertificateUrl: '',
		businessLicenseUrl: '',
		doorImageUrl: '',
		roomImageUrls: []
	})

	export default {
		components: { ProtoIcon },
		data() {
			let statusBarHeightRpx = 0
			try {
				const info = uni.getSystemInfoSync()
				statusBarHeightRpx = Math.round((Number(info.statusBarHeight) || 0) * 750 / (Number(info.windowWidth) || 375))
			} catch (error) {
				statusBarHeightRpx = 0
			}
			return {
				statusBarHeightRpx,
				heroImageSrc: brandImages.hero,
				form: emptyForm(),
				applicationStatus: 'unapplied',
				isEditing: false,
				loading: false,
				submitting: false,
				uploadingKey: '',
				identityUploads: [
					{ key: 'idCardFrontUrl', label: '身份证正面' },
					{ key: 'idCardBackUrl', label: '身份证反面' }
				],
				documentUploads: [
					{ key: 'propertyCertificateUrl', label: '房产证/承包协议' },
					{ key: 'businessLicenseUrl', label: '营业执照' },
					{ key: 'doorImageUrl', label: '门头照片' }
				]
			}
		},
		computed: {
			pageTitle() {
				return this.isEditing ? '编辑民宿资料' : '民宿申请'
			},
			locationLabel() {
				// 「民宿位置」只展示省市区，详细门牌放到下面的输入框。
				return composeRegion(this.form.province, this.form.city, this.form.district)
					|| this.form.standardAddress
					|| this.form.address
					|| ''
			},
			statusNotice() {
				if (this.applicationStatus === 'pending') return '资料审核中，审核期间门店所有房间暂不展示'
				if (this.applicationStatus === 'rejected') return '上次申请未通过，请修改资料后重新提交'
				if (this.isEditing) return '修改资料并提交后，门店会重新进入审核，审核期间房源不展示'
				return ''
			}
		},
		onLoad() {
			// 选点页拿不到 eventChannel 时会走全局事件，这里两路都接上，回填逻辑本身是幂等的。
			uni.$on('merchant:location-selected', this.applyLocationSelection)
			this.loadApplication()
		},
		onUnload() {
			uni.$off('merchant:location-selected', this.applyLocationSelection)
		},
		methods: {
			// 后端托管图不可用时退回包内图片。
			useLocalHero() {
				this.heroImageSrc = brandImageFallbacks.hero
			},
			async loadApplication() {
				this.loading = true
				try {
					const application = await fetchMerchantApplication()
					this.applicationStatus = application?.status || 'unapplied'
					this.isEditing = Boolean(application?.exists)
					if (application?.exists) {
						this.form = {
							...emptyForm(),
							...application,
							roomImageUrls: Array.isArray(application.roomImageUrls) ? application.roomImageUrls : []
						}
					}
				} catch (error) {
					showToast(error?.message || '申请资料加载失败')
				} finally {
					this.loading = false
				}
			},
			goBack() {
				uni.navigateBack({
					delta: 1,
					fail: () => uni.reLaunch({ url: '/pages/me/index' })
				})
			},
			openLocationPicker() {
				const latitude = Number(this.form.latitude)
				const longitude = Number(this.form.longitude)
				const query = []
				if (Number.isFinite(latitude) && Number.isFinite(longitude) && latitude && longitude) {
					query.push(`latitude=${latitude}`, `longitude=${longitude}`)
				}
				// 带上已经填过的详细地址和行政区，逆地理编码失败时选点页也能原样回填。
				if (this.form.address) query.push(`address=${encodeURIComponent(this.form.address)}`)
				if (this.form.province) query.push(`province=${encodeURIComponent(this.form.province)}`)
				if (this.form.city) query.push(`city=${encodeURIComponent(this.form.city)}`)

				uni.navigateTo({
					url: `/pages/merchant/location${query.length ? `?${query.join('&')}` : ''}`,
					events: {
						locationSelected: (result) => this.applyLocationSelection(result)
					},
					// 页面栈满等极端情况下打不开选点页，退回微信原生的选点界面。
					fail: () => this.pickLocationNatively()
				})
			},
			applyLocationSelection(result) {
				if (!result) return
				const province = String(result.province || '').trim()
				const city = String(result.city || '').trim()
				const district = String(result.district || '').trim()
				const regionLabel = String(result.region || '').trim() || composeRegion(province, city, district)
				const detail = String(result.detail || '').trim()
				const address = String(result.address || '').trim()
				const name = String(result.name || '').trim()
				if (!regionLabel && !detail && !address && !name) return

				if (province) this.form.province = province
				if (city) this.form.city = city
				// 重新选点后行政区要跟着更新，否则「民宿位置」会停留在上一次的区县。
				if (district) this.form.district = district

				// 详细地址回填街道门牌（可继续补充门牌号），标准地址存完整地址给门店列表展示。
				const fullAddress = !regionLabel
					? detail
					: (detail.startsWith(regionLabel) ? detail : `${regionLabel}${detail}`)
				this.form.standardAddress = fullAddress || name || address
				this.form.address = detail || address || name

				const latitude = Number(result.latitude)
				const longitude = Number(result.longitude)
				if (Number.isFinite(latitude) && Number.isFinite(longitude)) {
					this.form.latitude = latitude
					this.form.longitude = longitude
				}
			},
			pickLocationNatively() {
				if (typeof uni.chooseLocation !== 'function') {
					showToast('当前环境暂不支持地图选点，请填写详细地址')
					return
				}
				uni.chooseLocation({
					success: (result) => {
						const latitude = Number(result.latitude)
						const longitude = Number(result.longitude)
						this.applyLocationSelection({
							name: result.name || '',
							address: result.address || result.name || '',
							city: result.city || '',
							district: result.district || '',
							latitude: Number.isFinite(latitude) ? latitude : null,
							longitude: Number.isFinite(longitude) ? longitude : null
						})
					}
				})
			},
			// 门店列表展示的是 standardAddress，提交时按最新的省市区 + 详细地址重新拼一次。
			composeStandardAddress() {
				const region = composeRegion(this.form.province, this.form.city, this.form.district)
				const detail = String(this.form.address || '').trim()
				if (!region) return this.form.standardAddress || detail
				return detail.startsWith(region) ? detail : `${region}${detail}`
			},
			chooseBusinessArea() {
				uni.showModal({
					title: '填写商圈',
					editable: true,
					placeholderText: '请输入商圈名称',
					content: this.form.district || '',
					success: (result) => {
						if (result.confirm && result.content) this.form.district = result.content.trim()
					}
				})
			},
			async pickImage(key, multiple = false) {
				if (this.uploadingKey) return
				uni.chooseImage({
					count: multiple ? Math.min(9 - this.form.roomImageUrls.length, 9) : 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async ({ tempFilePaths = [] }) => {
						if (!tempFilePaths.length) return
						this.uploadingKey = key
						uni.showLoading({ title: '上传中...', mask: true })
						try {
							const urls = []
							for (const filePath of tempFilePaths) {
								const result = await uploadMerchantApplicationImage(filePath)
								if (result?.url) urls.push(result.url)
							}
							if (multiple) this.form.roomImageUrls = [...this.form.roomImageUrls, ...urls].slice(0, 9)
							else if (urls[0]) this.form[key] = urls[0]
						} catch (error) {
							showToast(error?.message || '图片上传失败')
						} finally {
							this.uploadingKey = ''
							uni.hideLoading()
						}
					}
				})
			},
			removeRoomImage(index) {
				this.form.roomImageUrls.splice(index, 1)
			},
			validate() {
				const checks = [
					[this.form.name, '请输入民宿名称'],
					[this.form.ownerName, '请输入房东姓名'],
					[/^1[3-9]\d{9}$/.test(this.form.ownerPhone), '请输入正确的房东电话'],
					[this.form.address, '请选择民宿位置并填写详细地址'],
					[this.form.idCardFrontUrl, '请上传身份证正面'],
					[this.form.idCardBackUrl, '请上传身份证反面'],
					[this.form.propertyCertificateUrl, '请上传房产证或承包协议'],
					[this.form.businessLicenseUrl, '请上传营业执照'],
					[this.form.doorImageUrl, '请上传门头照片'],
					[this.form.roomImageUrls.length, '请至少上传一张房间照片']
				]
				const invalid = checks.find((item) => !item[0])
				if (invalid) {
					showToast(invalid[1])
					return false
				}
				return true
			},
			async submitApplication() {
				if (this.submitting || this.uploadingKey || !this.validate()) return
				if (!this.isEditing) {
					await this.performSubmit()
					return
				}
				uni.showModal({
					title: '确认提交审核',
					content: '修改资料后门店会进入审核，审核期间门店和所有房间在小程序不可见，审核通过后恢复。',
					confirmText: '提交审核',
					cancelText: '再想想',
					success: async (result) => {
						if (result.confirm) await this.performSubmit()
					}
				})
			},
			async performSubmit() {
				if (this.submitting) return
				this.submitting = true
				try {
					const result = await submitMerchantApplication({
						...this.form,
						standardAddress: this.composeStandardAddress(),
						roomImageUrlsJson: JSON.stringify(this.form.roomImageUrls)
					})
					this.applicationStatus = result?.status || 'pending'
					uni.showModal({
						title: '提交成功',
						content: '资料已提交审核，审核期间门店和所有房间暂不展示。',
						showCancel: false,
						confirmText: '知道了',
						success: () => this.goBack()
					})
				} catch (error) {
					showToast(error?.message || '提交失败，请稍后重试')
				} finally {
					this.submitting = false
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.apply-page {
		min-height: 100vh;
		padding-bottom: 150rpx;
		padding-bottom: calc(150rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(150rpx + env(safe-area-inset-bottom));
		background: #f4f7f8;
	}

	.apply-hero {
		position: relative;
		height: 300rpx;
		overflow: hidden;
		background: #2e4b58;
	}

	.apply-hero-image,
	.apply-hero-mask {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		width: 100%;
		height: 100%;
	}

	.apply-hero-mask {
		background: linear-gradient(180deg, rgba(16, 46, 66, 0.72) 0%, rgba(31, 71, 82, 0.34) 48%, rgba(38, 67, 72, 0.66) 100%);
	}

	.apply-nav {
		position: relative;
		z-index: 2;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding-right: 22rpx;
		padding-left: 22rpx;
	}

	.apply-nav-button,
	.apply-nav-tools {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 64rpx;
		height: 64rpx;
		padding: 0;
	}

	.apply-nav-title {
		color: #ffffff;
		font-size: 32rpx;
		font-weight: 700;
		text-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.2);
	}

	.apply-card {
		position: relative;
		z-index: 3;
		margin: -126rpx 16rpx 30rpx;
		padding: 22rpx 16rpx 42rpx;
		background: #ffffff;
		border-radius: 24rpx;
		box-shadow: 0 8rpx 30rpx rgba(31, 61, 74, 0.1);
	}

	.apply-status {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin: 0 0 16rpx;
		padding: 16rpx 20rpx;
		color: #1b6e6e;
		background: #edfafa;
		border-radius: 14rpx;
		font-size: 22rpx;
		line-height: 1.45;
	}

	.apply-status.status-rejected {
		color: #9a5513;
		background: #fff6e9;
	}

	.status-dot {
		width: 12rpx;
		height: 12rpx;
		flex: 0 0 12rpx;
		background: currentColor;
		border-radius: 50%;
	}

	.apply-field,
	.apply-textarea-field,
	.apply-upload-section {
		background: #f4f6f8;
		border-radius: 18rpx;
	}

	.apply-field {
		display: flex;
		align-items: center;
		min-height: 88rpx;
		margin-bottom: 16rpx;
		padding: 0 24rpx;
	}

	.apply-field-clickable:active,
	.upload-box:active {
		opacity: 0.78;
	}

	.field-label {
		flex: 0 0 190rpx;
		color: #35434f;
		font-size: 24rpx;
		line-height: 1.4;
	}

	.field-input,
	.field-value-wrap {
		flex: 1;
		min-width: 0;
	}

	.field-input {
		height: 76rpx;
		padding: 0;
		color: #1e252b;
		font-size: 23rpx;
		text-align: right;
	}

	.field-placeholder,
	.field-value.empty {
		color: #8f969d;
	}

	.field-value-wrap {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		gap: 2rpx;
		min-height: 76rpx;
	}

	.field-value {
		max-width: 390rpx;
		overflow: hidden;
		color: #232a30;
		font-size: 23rpx;
		text-align: right;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.field-hint {
		display: block;
		margin: -6rpx 12rpx 16rpx;
		color: #98a1a8;
		font-size: 20rpx;
		line-height: 1.5;
	}

	.apply-textarea-field {
		margin-bottom: 16rpx;
		padding: 22rpx 24rpx 18rpx;
	}

	.apply-textarea-field .field-label {
		display: block;
		margin-bottom: 12rpx;
	}

	.field-textarea {
		display: block;
		width: 100%;
		height: 130rpx;
		padding: 0;
		color: #232a30;
		font-size: 23rpx;
		line-height: 1.55;
	}

	.apply-upload-section {
		margin-bottom: 16rpx;
		padding: 22rpx 24rpx 24rpx;
	}

	.upload-section-title {
		display: block;
		margin-bottom: 18rpx;
		color: #35434f;
		font-size: 24rpx;
		line-height: 1.4;
	}

	.upload-title-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.upload-title-row .upload-section-title {
		margin-bottom: 18rpx;
	}

	.upload-count {
		margin-bottom: 18rpx;
		color: #909aa1;
		font-size: 20rpx;
	}

	.upload-grid,
	.room-upload-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 18rpx;
	}

	.upload-box {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 176rpx;
		height: 176rpx;
		overflow: hidden;
		background: #ffffff;
		border-radius: 12rpx;
	}

	.upload-preview {
		width: 100%;
		height: 100%;
	}

	.upload-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		color: #8d979e;
		font-size: 20rpx;
	}

	.upload-plus {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 38rpx;
		height: 38rpx;
		margin-bottom: 12rpx;
		background: var(--proto-primary, #2aa9a9);
		border-radius: 50%;
	}

	.room-upload-box {
		width: 150rpx;
		height: 150rpx;
	}

	.image-remove {
		position: absolute;
		top: 8rpx;
		right: 8rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 34rpx;
		height: 34rpx;
		padding: 0;
		background: rgba(0, 0, 0, 0.48);
		border-radius: 50%;
	}

	.apply-tip {
		display: block;
		padding: 4rpx 8rpx 0;
		color: #9aa4aa;
		font-size: 19rpx;
		line-height: 1.5;
		text-align: center;
	}

	.apply-submit-bar {
		position: fixed;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 20;
		padding: 12rpx 16rpx;
		padding-bottom: calc(12rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(12rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid #e7eaed;
		box-shadow: 0 -8rpx 22rpx rgba(31, 61, 74, 0.08);
	}

	.apply-submit {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 86rpx;
		padding: 0;
		color: #ffffff;
		background: var(--proto-primary-dark, #006a6a);
		border-radius: 43rpx;
		font-size: 29rpx;
		font-weight: 750;
	}

	.apply-submit[disabled] {
		opacity: 0.58;
	}
</style>
