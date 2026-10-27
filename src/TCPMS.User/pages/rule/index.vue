<template>
	<view class="proto-page proto-page-plain rule-page">
		<ProtoHeader :title="activeTab === 'privacy' ? '用户隐私手册' : '规则与政策'" theme="green" />
		<view class="rule-summary proto-card">
			<view class="rule-summary-top">
				<view>
					<view class="proto-pill info"><ProtoIcon name="shield" :size="18" /><text>{{ page.kicker || '合规与数字隐私标准' }}</text></view>
					<text class="rule-summary-title">{{ page.subtitle || page.title || 'TCPMS 智选旅宿服务条款' }}</text>
					<text class="rule-summary-desc">{{ page.summary }}</text>
				</view>
				<view class="rule-summary-mark"><ProtoIcon name="shield" tone="white" :size="30" /></view>
			</view>
			<view v-if="page.version || page.effectiveAt" class="rule-summary-meta">
				<view><text>版本更新</text><text class="rule-summary-date">{{ page.version || '--' }}</text></view>
				<view><text>正式生效</text><text class="rule-summary-date">{{ page.effectiveAt || '--' }}</text></view>
			</view>
		</view>
		<view v-if="page.tabs.length" class="proto-tabs rule-tabs">
			<text
				v-for="item in page.tabs"
				:key="item.key"
				class="proto-tab"
				:class="{ active: activeTab === item.key }"
				@tap="activeTab = item.key"
			>
				{{ item.label }}
			</text>
		</view>

		<scroll-view scroll-y class="rule-scroll">
			<view v-if="loadState !== 'ready'" class="rule-document rule-state">
				<text class="rule-state-title">{{ loadState === 'error' ? '协议内容加载失败' : '正在加载协议内容' }}</text>
				<text class="rule-state-desc">{{ loadState === 'error' ? '请检查网络后点击下方按钮重试。' : '稍候即可查看完整的服务条款与隐私说明。' }}</text>
				<button v-if="loadState === 'error'" class="proto-primary-button" hover-class="none" @tap="loadPage"><text>重新加载</text></button>
			</view>
			<view v-else class="rule-document">
				<text class="rule-kicker">{{ activeContent.kicker }}</text>
				<text class="rule-updated">{{ activeContent.updatedAt }}</text>
				<text class="rule-title">{{ activeContent.title }}</text>
				<block v-for="(block, index) in activeContent.blocks" :key="index">
					<text v-if="block.type === 'heading'" class="rule-heading">{{ block.text }}</text>
					<view v-else-if="block.type === 'highlights'" :class="block.style === 'cards' ? 'privacy-highlights' : 'rule-compliance-list'">
						<view
							v-for="item in activeContent.highlights"
							:key="item.title"
							:class="block.style === 'cards' ? 'privacy-highlight' : 'rule-compliance-item'"
						>
							<block v-if="block.style === 'cards'">
								<ProtoIcon class="privacy-icon" :name="item.icon" :size="30" />
								<text class="privacy-title">{{ item.title }}</text>
								<text class="privacy-desc">{{ item.desc }}</text>
							</block>
							<block v-else>
								<view class="rule-compliance-icon"><ProtoIcon :name="item.icon" :size="26" /></view>
								<view>
									<text class="rule-compliance-title">{{ item.title }} <text class="proto-pill info">{{ item.tag }}</text></text>
									<text class="rule-compliance-desc">{{ item.desc }}</text>
								</view>
							</block>
						</view>
					</view>
					<view v-else-if="block.type === 'permissions'" class="rule-permissions">
						<view v-for="item in activeContent.permissions" :key="item.title" class="sdk-item">
							<view class="sdk-icon"><ProtoIcon :name="item.icon" :size="28" /></view>
							<view class="sdk-main">
								<text class="sdk-title">{{ item.title }}</text>
								<text class="sdk-desc">{{ item.desc }}</text>
							</view>
							<text class="proto-pill" :class="item.required ? 'warn' : 'info'">{{ item.required ? '必要' : '可选' }}</text>
						</view>
					</view>
					<text v-else class="rule-paragraph">{{ block.text }}</text>
				</block>
			</view>
		</scroll-view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { fetchPageContent } from '@/common/api.js'

	const EMPTY_PAGE = {
		key: 'rule',
		title: '',
		subtitle: '',
		kicker: '',
		summary: '',
		version: '',
		effectiveAt: '',
		tabs: []
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				activeTab: '',
				loadState: 'loading',
				page: { ...EMPTY_PAGE }
			}
		},
		computed: {
			activeContent() {
				const tabs = this.page.tabs || []
				return tabs.find((item) => item.key === this.activeTab) || tabs[0] || { blocks: [], highlights: [], permissions: [] }
			}
		},
		onLoad(options) {
			this.activeTab = options && options.tab ? options.tab : ''
			this.loadPage()
		},
		methods: {
			async loadPage() {
				if (this.loadState !== 'loading') this.loadState = 'loading'
				try {
					const data = await fetchPageContent('rule')
					this.page = { ...EMPTY_PAGE, ...data, tabs: Array.isArray(data.tabs) ? data.tabs : [] }
					if (!this.activeTab || !this.page.tabs.some((item) => item.key === this.activeTab)) {
						this.activeTab = this.page.tabs.length ? this.page.tabs[0].key : ''
					}
					this.loadState = 'ready'
				} catch (error) {
					// 文案接口异常时给出重试入口，不展示过期的硬编码条款。
					this.page = { ...EMPTY_PAGE }
					this.loadState = 'error'
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.rule-page {
		padding-bottom: 40rpx;
	}

	.rule-summary {
		margin: 16rpx 24rpx 12rpx;
		padding: 20rpx;
		background: linear-gradient(135deg, #e0f5f3, #ffffff);
	}

	.rule-summary-top {
		display: flex;
		gap: 14rpx;
	}

	.rule-summary-top > view {
		flex: 1;
		min-width: 0;
	}

	.rule-summary-top .proto-pill {
		display: inline-flex;
		align-items: center;
		gap: 6rpx;
	}

	.rule-summary-title,
	.rule-summary-desc {
		display: block;
	}

	.rule-summary-title {
		margin-top: 16rpx;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 800;
	}

	.rule-summary-desc {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.45;
	}

	.rule-summary-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 62rpx;
		height: 62rpx;
		flex: 0 0 62rpx;
		color: #ffffff;
		background: var(--proto-primary-dark);
		border-radius: 16rpx;
		font-size: 30rpx;
	}

	.rule-summary-meta {
		display: flex;
		gap: 20rpx;
		margin-top: 18rpx;
		padding: 14rpx 16rpx;
		background: rgba(255, 255, 255, 0.78);
		border-radius: 14rpx;
	}

	.rule-summary-meta view {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.rule-summary-meta view + view {
		border-left: 1rpx solid #d9e5e5;
		padding-left: 20rpx;
	}

	.rule-summary-meta text {
		color: var(--proto-muted);
		font-size: 16rpx;
	}

	.rule-summary-date {
		margin-top: 4rpx;
		color: var(--proto-text);
		font-size: 18rpx;
	}

	.rule-tabs {
		position: relative;
		z-index: 1;
		box-shadow: 0 6rpx 18rpx rgba(32, 37, 43, 0.04);
	}

	.rule-scroll {
		height: calc(100vh - 430rpx);
	}

	.rule-document {
		padding: 30rpx 28rpx 64rpx;
	}

	.rule-state {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding-top: 120rpx;
		text-align: center;
	}

	.rule-state-title {
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 800;
	}

	.rule-state-desc {
		margin: 12rpx 0 32rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
		line-height: 1.6;
	}

	.rule-state .proto-primary-button {
		width: 320rpx;
	}

	.rule-permissions {
		display: flex;
		flex-direction: column;
		gap: 4rpx;
		margin-top: 4rpx;
	}

	.rule-kicker,
	.rule-updated,
	.rule-title,
	.rule-paragraph,
	.rule-heading {
		display: block;
	}

	.rule-kicker {
		color: var(--proto-primary-dark);
		font-size: 21rpx;
		font-weight: 750;
		letter-spacing: 2rpx;
	}

	.rule-updated {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.rule-title {
		margin-top: 22rpx;
		color: var(--proto-text);
		font-size: 35rpx;
		font-weight: 850;
		line-height: 1.35;
	}

	.rule-heading {
		margin-top: 34rpx;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 800;
	}

	.rule-compliance-list {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		margin-top: 16rpx;
	}

	.rule-compliance-item {
		display: flex;
		gap: 14rpx;
		padding: 18rpx;
		background: var(--proto-surface);
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 16rpx;
	}

	.rule-compliance-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 50rpx;
		height: 50rpx;
		flex: 0 0 50rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 14rpx;
	}

	.rule-compliance-item > view {
		flex: 1;
		min-width: 0;
	}

	.rule-compliance-title,
	.rule-compliance-desc {
		display: block;
	}

	.rule-compliance-title {
		color: var(--proto-text);
		font-size: 22rpx;
		font-weight: 750;
	}

	.rule-compliance-title .proto-pill {
		min-height: 28rpx;
		padding: 0 8rpx;
		font-size: 15rpx;
		vertical-align: 2rpx;
	}

	.rule-compliance-desc {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.45;
	}

	.rule-paragraph {
		margin-top: 16rpx;
		color: var(--proto-text);
		font-size: 23rpx;
		line-height: 1.8;
	}

	.privacy-highlights {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 26rpx;
	}

	.privacy-highlight {
		display: flex;
		align-items: center;
		flex-direction: column;
		width: calc(33.333% - 8rpx);
		padding: 18rpx 8rpx;
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		text-align: center;
	}

	.privacy-icon {
		display: block;
		color: var(--proto-primary-dark);
	}

	.privacy-title {
		margin-top: 8rpx;
		color: var(--proto-text);
		font-size: 20rpx;
		font-weight: 750;
	}

	.privacy-desc {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 16rpx;
		line-height: 1.4;
	}

	.sdk-item {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 14rpx;
		padding: 18rpx;
		background: var(--proto-surface);
		border: 1rpx solid #e0e9ea;
		border-radius: 18rpx;
	}

	.sdk-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 54rpx;
		height: 54rpx;
		flex: 0 0 54rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
	}

	.sdk-main {
		flex: 1;
		min-width: 0;
	}

	.sdk-title,
	.sdk-desc {
		display: block;
	}

	.sdk-title {
		color: var(--proto-text);
		font-size: 22rpx;
		font-weight: 750;
	}

	.sdk-desc {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.45;
	}

	.sdk-item .proto-pill {
		min-height: 34rpx;
		padding: 0 10rpx;
		font-size: 17rpx;
	}
</style>
