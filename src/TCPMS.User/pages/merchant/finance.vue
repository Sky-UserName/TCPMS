<template>
	<view class="proto-page finance-page">
		<ProtoHeader title="财务统计" theme="green" />

		<view class="finance-wallet">
			<view class="finance-wallet-heading"><view><text class="finance-property">{{ storeName }}</text><ProtoIcon name="chevron-down" :size="22" /></view><button hover-class="none" @tap="withdraw">提现</button></view>
			<text class="finance-total">¥{{ orderStats.totalAmount.toFixed(2) }}</text>
			<text class="finance-total-label">累计金额</text>
			<view class="finance-wallet-grid">
				<view v-for="item in walletItems" :key="item.label"><text>{{ item.value }}</text><text>{{ item.label }}</text></view>
			</view>
		</view>

		<view class="finance-section-title"><text>经营概览</text><text>数据实时同步</text></view>
		<view v-for="section in sections" :key="section.title" class="finance-data-card proto-card">
			<view class="finance-card-heading"><text>{{ section.title }}</text><view v-if="section.more" class="finance-more" @tap="openDetail(section)"><text>更多</text><ProtoIcon name="chevron-right" :size="20" /></view></view>
			<view class="finance-metric-grid">
				<view v-for="metric in section.metrics" :key="metric.label" class="finance-metric"><text class="finance-metric-value" :class="{ money: metric.money }">{{ metric.value }}</text><text class="finance-metric-label">{{ metric.label }}</text></view>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { showToast } from '@/common/prototype.js'
	import { createMerchantWithdrawal, fetchMerchantFinance, fetchMerchantProfile } from '@/common/api.js'

	const yuan = (cents) => (Number(cents || 0) / 100).toFixed(2)

	const metricSection = (title, metric, more) => ({
		title,
		more,
		metrics: [
			{ value: String(metric.pendingCount || 0), label: '待使用订单' },
			{ value: String(metric.doneCount || 0), label: '核销订单' },
			{ value: String(metric.cancelCount || 0), label: '取消订单' },
			{ value: `¥${yuan(metric.pendingAmountCents)}`, label: '待使用金额', money: true },
			{ value: `¥${yuan(metric.doneAmountCents)}`, label: '核销订单金额', money: true },
			{ value: `¥${yuan(metric.cancelAmountCents)}`, label: '取消订单金额', money: true }
		]
	})

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				storeName: '我的民宿',
				finance: {
					all: {},
					wallet: { frozenCents: 0, availableCents: 0, withdrawnCents: 0, cancelledCents: 0 }
				}
			}
		},
		computed: {
			orderStats() {
				const all = this.finance.all || {}
				return {
					pending: Number(all.pendingCount || 0),
					done: Number(all.doneCount || 0),
					cancel: Number(all.cancelCount || 0),
					pendingAmount: Number(all.pendingAmountCents || 0) / 100,
					doneAmount: Number(all.doneAmountCents || 0) / 100,
					cancelAmount: Number(all.cancelAmountCents || 0) / 100,
					totalAmount: Number(all.totalAmountCents || 0) / 100
				}
			},
			walletItems() {
				const wallet = this.finance.wallet || {}
				return [
					{ value: `¥${yuan(wallet.frozenCents)}`, label: '冻结中' },
					{ value: `¥${yuan(wallet.availableCents)}`, label: '可提现' },
					{ value: `¥${yuan(wallet.withdrawnCents)}`, label: '已提现' },
					{ value: `¥${yuan(wallet.cancelledCents)}`, label: '已取消' }
				]
			},
			sections() {
				return [
					metricSection('今日数据', this.finance.today || {}, false),
					metricSection('近7天数据', this.finance.days7 || {}, true),
					metricSection('近30天数据', this.finance.days30 || {}, true),
					metricSection('全部数据', this.finance.all || {}, true)
				]
			}
		},
		async onShow() {
			await this.loadFinance()
		},
		methods: {
			async loadFinance() {
				try {
					const [profile, finance] = await Promise.all([fetchMerchantProfile(), fetchMerchantFinance()])
					if (profile?.storeName) this.storeName = profile.storeName
					this.finance = finance || this.finance
				} catch (error) {
					showToast(error?.message || '财务数据加载失败')
				}
			},
			async withdraw() {
				const available = Number((this.finance.wallet || {}).availableCents || 0)
				if (available <= 0) {
					showToast('暂无可提现余额')
					return
				}
				try {
					await createMerchantWithdrawal(available, this.storeName)
					showToast(`已提交提现 ¥${yuan(available)}，等待审核`)
					await this.loadFinance()
				} catch (error) {
					showToast(error?.message || '提现申请失败')
				}
			},
			openDetail(section) {
				showToast(`${section.title}由服务端按订单创建时间统计`)
			}
		}
	}
</script>

<style lang="scss" scoped>
	.finance-page {
		padding-bottom: 50rpx;
		background: #f4fafb;
	}

	.finance-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.18);
	}

	.finance-wallet {
		margin: 22rpx 24rpx 28rpx;
		padding: 28rpx 28rpx 24rpx;
		color: #ffffff;
		background: #2aa9a9;
		border-radius: 26rpx;
		box-shadow: 0 14rpx 28rpx rgba(42, 169, 169, 0.26);
	}

	.finance-wallet-heading,
	.finance-wallet-heading > view {
		display: flex;
		align-items: center;
	}

	.finance-wallet-heading {
		gap: 16rpx;
		justify-content: space-between;
	}

	.finance-property {
		display: block;
		max-width: 390rpx;
		overflow: hidden;
		font-size: 29rpx;
		font-weight: 800;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.finance-wallet-heading button {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 auto;
		width: 136rpx;
		height: 58rpx;
		padding: 0;
		color: #006a6a;
		background: #ffffff;
		border-radius: 30rpx;
		font-size: 22rpx;
		font-weight: 700;
		line-height: 1;
		white-space: nowrap;
	}

	.finance-total {
		display: block;
		margin-top: 24rpx;
		font-size: 46rpx;
		font-weight: 800;
	}

	.finance-total-label {
		display: block;
		margin-top: 4rpx;
		color: rgba(255, 255, 255, 0.82);
		font-size: 19rpx;
	}

	.finance-wallet-grid {
		display: flex;
		margin-top: 26rpx;
		padding-top: 20rpx;
		border-top: 1rpx solid rgba(255, 255, 255, 0.2);
	}

	.finance-wallet-grid > view {
		display: flex;
		flex: 1;
		align-items: center;
		flex-direction: column;
		min-width: 0;
	}

	.finance-wallet-grid > view > text:first-child {
		font-size: 24rpx;
		font-weight: 750;
	}

	.finance-wallet-grid > view > text:last-child {
		margin-top: 6rpx;
		color: rgba(255, 255, 255, 0.76);
		font-size: 17rpx;
	}

	.finance-section-title {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin: 0 24rpx 14rpx;
	}

	.finance-section-title > text:first-child {
		color: var(--proto-text, #20252b);
		font-size: 29rpx;
		font-weight: 800;
	}

	.finance-section-title > text:last-child {
		color: var(--proto-muted, #6f8085);
		font-size: 18rpx;
	}

	.finance-data-card {
		margin-top: 14rpx;
		padding: 24rpx 22rpx 26rpx;
	}

	.finance-card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text, #20252b);
		font-size: 27rpx;
		font-weight: 750;
	}

	.finance-more {
		display: flex;
		align-items: center;
		color: var(--proto-primary-dark, #006a6a);
		font-size: 19rpx;
		font-weight: 500;
		white-space: nowrap;
	}

	.finance-metric-grid {
		display: flex;
		flex-wrap: wrap;
		margin-top: 24rpx;
		row-gap: 24rpx;
	}

	.finance-metric {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: 33.3333%;
		min-width: 0;
	}

	.finance-metric-value {
		color: var(--proto-text, #20252b);
		font-size: 34rpx;
		font-weight: 800;
	}

	.finance-metric-value.money {
		color: #d9363e;
		font-size: 28rpx;
	}

	.finance-metric-label {
		margin-top: 7rpx;
		color: var(--proto-muted, #6f8085);
		font-size: 18rpx;
		white-space: nowrap;
		text-align: center;
	}
</style>
