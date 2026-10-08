<template>
	<view class="proto-page refund-page">
		<ProtoHeader title="退款申请" theme="green" />

		<view class="refund-order proto-card">
			<view class="refund-order-title">
				<view class="refund-store-icon"><ProtoIcon name="store" :size="28" /></view>
				<view>
					<text>{{ order.store }}</text>
					<text class="small">{{ order.room }} · {{ order.roomCount || 1 }}间 {{ order.nights || 1 }}晚</text>
				</view>
				<text class="strong">实付 ¥{{ order.price }}.00</text>
			</view>
			<view class="refund-order-meta"><view><ProtoIcon name="calendar" :size="18" /><text>{{ order.date }}</text></view><view><ProtoIcon name="document" :size="18" /><text>单号 {{ order.id }}</text></view></view>
		</view>

		<view class="refund-policy">
			<view class="refund-policy-title"><ProtoIcon name="shield" :size="22" /><text>无损免费退款期</text><text class="small">免手续费</text></view>
			<text>入住日前1天18:00前申请，享极速原路返还，无任何扣款。</text>
		</view>

		<view class="refund-form proto-card">
			<view class="refund-maximum"><text>最大可退金额</text><text class="strong">¥{{ maxAmount }}.00</text><text class="proto-pill info">全额可退</text></view>
			<text class="form-hint">本次申请退款金额</text>
			<view class="amount-input">
				<text>¥</text>
				<input v-model="amount" type="digit" />
				<button hover-class="none" @tap="amount = maxAmount.toFixed(2)">全部退款</button>
			</view>
			<text class="amount-limit">最多可退 ¥{{ maxAmount }}.00，支持部分或全额退款</text>

			<text class="proto-form-label">退款原因 <text class="required">*</text></text>
			<view class="reason-list">
				<view v-for="item in reasons" :key="item" class="reason-item" :class="{ selected: reason === item }" @tap="reason = item">
					<view class="reason-check"><ProtoIcon :name="reason === item ? 'check-circle' : 'status'" :size="25" /></view>
					<text>{{ item }}</text>
				</view>
			</view>
			<text v-if="state === 'noReason'" class="form-error">请先选择退款原因方可继续</text>

			<text class="proto-form-label">退款说明与凭证 <text class="optional">选填</text></text>
			<textarea v-model="remark" class="proto-textarea" maxlength="200" placeholder="请补充退款说明，最多200字" placeholder-class="placeholder"></textarea>
			<text class="word-count">{{ remark.length }}/200</text>
			<view class="upload-row" @tap="chooseProof">
				<view class="upload-placeholder"><ProtoIcon name="plus" :size="32" /><text class="small">{{ uploadLabel }}</text></view>
				<text>上传补充凭证（最多3张）</text>
			</view>
		</view>

		<view class="refund-security">
			<view class="security-title"><ProtoIcon name="shield" :size="22" /><text>平台安全退款保障</text></view>
			<view class="security-line"><ProtoIcon name="wallet" :size="19" /><text>原路返还：将通过微信支付原路返还至您的原支付账户。</text></view>
			<view class="security-line"><ProtoIcon name="clock" :size="19" /><text>极速时效：全自动化审批链路，系统预计1-15分钟内审核完毕并原路打款。</text></view>
			<view class="security-line"><ProtoIcon name="support" :size="19" /><text>进度推送：申请将由平台与门店同步复核，结果通过微信服务通知实时推送。</text></view>
		</view>

		<button class="proto-primary-button refund-submit" hover-class="none" @tap="submitRefund"><ProtoIcon name="receipt" tone="white" :size="22" /><text>提交退款申请（¥{{ amount || '0.00' }}）</text></button>
		<text class="refund-agreement">点击提交即代表同意《TCPMS旅宿退订规则与保障协议》</text>

		<view v-if="state === 'success' || state === 'existing'" class="proto-mask" @tap="state = 'default'">
			<view class="proto-sheet success-sheet" @tap.stop>
				<view class="proto-sheet-handle"></view>
				<view class="success-icon"><ProtoIcon name="check-circle" :size="62" /></view>
				<text class="success-title">{{ state === 'success' ? '退款申请已提交成功' : '已有退款申请' }}</text>
				<text class="success-desc">{{ state === 'success' ? '系统已启动快速审核，资金将原路返回' : '该订单已有退款申请正在审核，无需重复提交。' }}</text>
				<view class="refund-result">
					<view class="refund-result-row"><text>退款流水号</text><text class="strong">REF202610010892</text></view>
					<view class="refund-result-row"><text>申请退款金额</text><text class="strong">¥{{ amountDisplay }}</text></view>
					<view class="refund-result-row"><text>返还账户</text><text class="strong">微信支付原账户</text></view>
					<view class="refund-result-row"><text>预计到账时间</text><text class="strong">1 - 15分钟内</text></view>
				</view>
				<button class="proto-primary-button" hover-class="none" @tap="goOrder">查看订单与退款进度</button>
				<button class="proto-secondary-button" hover-class="none" @tap="state = 'default'">返回上一页</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoOrders, goPage, showToast } from '@/common/prototype.js'
	import { findOrder, getOrders, updateOrder } from '@/common/app-store.js'
	import { refundRemoteOrder } from '@/common/api.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				order: demoOrders[0],
				amount: '536.00',
				reason: '',
				remark: '',
				state: 'default',
				reasons: ['行程有变 / 无法出行', '预订信息错误（日期/房型/人数填错）', '门店协商一致取消 / 设施问题', '其他原因'],
				proofs: []
			}
		},
		computed: {
			maxAmount() {
				return Number(this.order.price || 0)
			},
			uploadLabel() {
				return this.proofs.length ? `${this.proofs.length}张已添加` : '上传凭证'
			},
			amountDisplay() {
				return this.amount || this.maxAmount.toFixed(2)
			}
		},
		onLoad(options) {
			this.loadOrder(options && options.id)
		},
		methods: {
			loadOrder(id) {
				const order = id ? findOrder(id) : getOrders()[0]
				if (!order) return
				this.order = order
				this.amount = Number(order.price || 0).toFixed(2)
				if (order.statusKey === 'refund') this.state = 'existing'
			},
			async submitRefund() {
				if (this.state === 'existing') return
				if (Number(this.amount) <= 0 || Number(this.amount) > this.maxAmount) {
					showToast('退款金额不能超过最大可退金额')
					return
				}
				if (!this.reason) {
					this.state = 'noReason'
					showToast('请先选择退款原因')
					return
				}
				this.state = 'submitting'
				let updated
				if (/^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(this.order.id))) {
					try {
						const result = await refundRemoteOrder(this.order.id, this.amount, this.reason, this.order)
						updated = result.order
					} catch (error) {
						updated = null
					}
				}
				if (!updated) {
					updated = updateOrder(this.order.id, {
						status: '退款审核中',
						statusKey: 'refund',
						action: '查看退款进度',
						refundAmount: Number(this.amount).toFixed(2),
						refundReason: this.reason,
						refundRemark: this.remark,
						refundCreatedAt: new Date().toISOString()
					})
				}
				if (updated) this.order = updated
				setTimeout(() => {
					this.state = 'success'
				}, 650)
			},
			goOrder() {
				goPage(`/pages/order/detail?id=${this.order.id}`)
			},
			chooseProof() {
				uni.chooseImage({
					count: 3,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: (res) => {
						this.proofs = res.tempFilePaths || []
					},
					fail: () => showToast('未选择凭证')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.refund-page {
		padding-bottom: 150rpx;
		padding-bottom: calc(150rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(150rpx + env(safe-area-inset-bottom));
	}

	.refund-order-title {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}

	.refund-store-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 54rpx;
		height: 54rpx;
		flex: 0 0 54rpx;
		background: var(--proto-surface-tint);
		border-radius: 50%;
		color: var(--proto-primary-dark);
	}

	.refund-order-title > view {
		flex: 1;
	}

	.refund-order-title view text {
		display: block;
	}

	.refund-order-title view text {
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.refund-order-title view .small {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.refund-order-title .strong {
		color: var(--proto-muted);
		font-size: 20rpx;
		white-space: nowrap;
	}

	.refund-order-meta {
		display: flex;
		justify-content: space-between;
		margin-top: 18rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #e3ebec;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.refund-order-meta > view {
		display: flex;
		align-items: center;
		gap: 5rpx;
		min-width: 0;
	}

	.refund-policy {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		margin: 18rpx 24rpx;
		padding: 20rpx;
		color: var(--proto-primary-dark);
		background: #c7f4f5;
		border-radius: 18rpx;
		font-size: 19rpx;
		line-height: 1.5;
	}

	.refund-policy-title {
		display: flex;
		align-items: center;
		gap: 6rpx;
		font-size: 24rpx;
		font-weight: 750;
	}

	.refund-policy-title .small {
		padding: 5rpx 10rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border-radius: 8rpx;
		font-size: 16rpx;
	}

	.refund-maximum {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 14rpx;
	}

	.refund-maximum > text:first-child {
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.refund-maximum .strong {
		flex: 1;
		min-width: 160rpx;
		margin-left: auto;
		color: var(--proto-primary-dark);
		font-size: 42rpx;
		text-align: right;
	}

	.refund-maximum .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}

	.form-hint,
	.amount-limit {
		display: block;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
	}

	.amount-input {
		display: flex;
		align-items: center;
		margin-top: 10rpx;
		padding: 0 18rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.amount-input > text {
		color: var(--proto-text);
		font-size: 36rpx;
	}

	.amount-input input {
		flex: 1;
		height: 88rpx;
		padding: 0 10rpx;
		color: var(--proto-text);
		font-size: 38rpx;
	}

	.amount-input button {
		padding: 10rpx 16rpx;
		color: var(--proto-primary-dark);
		background: #ffffff;
		border-radius: 16rpx;
		font-size: 18rpx;
	}

	.reason-list {
		display: flex;
		flex-direction: column;
		gap: 14rpx;
	}

	.reason-item {
		display: flex;
		align-items: center;
		gap: 12rpx;
		padding: 16rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 14rpx;
		font-size: 20rpx;
	}

	.reason-item.selected {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.reason-check {
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.required {
		color: var(--proto-error);
	}

	.optional {
		color: var(--proto-muted);
		font-size: 18rpx;
		font-weight: 400;
	}

	.placeholder {
		color: var(--proto-muted);
	}

	.word-count {
		display: block;
		margin-top: -28rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
		text-align: right;
	}

	.upload-row {
		display: flex;
		align-items: center;
		gap: 16rpx;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.upload-placeholder {
		display: flex;
		align-items: center;
		justify-content: center;
		flex-direction: column;
		width: 120rpx;
		height: 120rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.upload-placeholder .small {
		font-size: 17rpx;
	}

	.form-error {
		display: block;
		margin-top: 12rpx;
		color: var(--proto-error);
		font-size: 19rpx;
	}

	.refund-security {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		margin: 18rpx 24rpx;
		padding: 22rpx;
		color: var(--proto-muted);
		background: #c7f4f5;
		border-radius: 20rpx;
		font-size: 19rpx;
		line-height: 1.5;
	}

	.security-title {
		display: flex;
		align-items: center;
		gap: 7rpx;
		color: var(--proto-primary-dark);
		font-size: 27rpx;
		font-weight: 800;
	}

	.security-line {
		display: flex;
		align-items: flex-start;
		gap: 7rpx;
	}

	.security-line > text {
		flex: 1;
	}

	.refund-submit {
		display: flex;
		align-items: center;
		gap: 7rpx;
		margin-top: 26rpx;
	}

	.refund-agreement {
		display: block;
		color: var(--proto-muted);
		font-size: 17rpx;
		text-align: center;
	}

	.success-sheet {
		display: flex;
		align-items: center;
		flex-direction: column;
	}

	.success-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 112rpx;
		height: 112rpx;
		margin-top: 18rpx;
		color: var(--proto-primary-dark);
		background: #80e7ee;
		border-radius: 50%;
	}

	.success-title {
		margin-top: 20rpx;
		color: var(--proto-text);
		font-size: 30rpx;
		font-weight: 800;
	}

	.success-desc {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.refund-result {
		display: flex;
		flex-direction: column;
		gap: 14rpx;
		width: 100%;
		margin-top: 24rpx;
		padding: 20rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.refund-result-row {
		display: flex;
		justify-content: space-between;
	}

	.refund-result-row .strong {
		color: var(--proto-text);
		font-weight: 500;
	}

	.success-sheet .proto-primary-button,
	.success-sheet .proto-secondary-button {
		width: 100%;
		margin-right: 0;
		margin-left: 0;
	}
</style>
