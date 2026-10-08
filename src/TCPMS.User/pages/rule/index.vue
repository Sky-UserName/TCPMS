<template>
	<view class="proto-page proto-page-plain rule-page">
		<ProtoHeader title="规则与政策" theme="green" />
		<view class="rule-summary proto-card">
			<view class="rule-summary-top">
				<view>
					<view class="proto-pill info"><ProtoIcon name="shield" :size="18" /><text>合规与数字隐私标准</text></view>
					<text class="rule-summary-title">TCPMS 智选旅宿服务条款</text>
					<text class="rule-summary-desc">我们致力于守护您的旅途隐私与数据安全，为您提供透明、值得信赖的数字化入住体验。</text>
				</view>
				<view class="rule-summary-mark"><ProtoIcon name="shield" tone="white" :size="30" /></view>
			</view>
			<view class="rule-summary-meta">
				<view><text>版本更新</text><text class="rule-summary-date">2026年09月30日</text></view>
				<view><text>正式生效</text><text class="rule-summary-date">2026年10月01日</text></view>
			</view>
		</view>
		<view class="proto-tabs rule-tabs">
			<text
				v-for="item in tabs"
				:key="item.key"
				class="proto-tab"
				:class="{ active: activeTab === item.key }"
				@tap="activeTab = item.key"
			>
				{{ item.label }}
			</text>
		</view>

		<scroll-view scroll-y class="rule-scroll">
			<view v-if="activeTab === 'service'" class="rule-document">
				<text class="rule-kicker">TCPMS 智选旅宿服务协议</text>
				<text class="rule-updated">更新日期：2026年09月30日</text>
				<text class="rule-title">让每一次入住，都有清晰的规则和可预期的服务</text>
				<text class="rule-paragraph">欢迎使用 TCPMS 智选旅宿小程序。使用门店浏览、房型预订、支付、入住、退款和客服等服务前，请阅读并理解本协议。完成登录、勾选同意或继续使用服务，视为你已阅读并同意本协议。</text>
				<text class="rule-heading">核心隐私与合规摘要</text>
				<view class="rule-compliance-list">
					<view v-for="item in complianceItems" :key="item.title" class="rule-compliance-item">
						<view class="rule-compliance-icon"><ProtoIcon :name="item.icon" :size="26" /></view>
						<view><text class="rule-compliance-title">{{ item.title }} <text class="proto-pill info">{{ item.tag }}</text></text><text class="rule-compliance-desc">{{ item.desc }}</text></view>
					</view>
				</view>
				<text class="rule-heading">一、服务范围</text>
				<text class="rule-paragraph">平台提供多门店旅宿信息展示、房型预订、订单管理、支付退款申请、入住指引和官方客服入口。具体服务内容以页面实际展示和订单确认页为准。</text>
				<text class="rule-heading">二、预订与订单</text>
				<text class="rule-paragraph">你需要提供真实、准确的入住日期、入住人数和入住人信息。订单提交后，房型库存会在指定时间内锁定；未在规定时间完成支付的订单可能自动取消并释放库存。</text>
				<text class="rule-heading">三、入住与退订</text>
				<text class="rule-paragraph">入住时间、离店时间、身份核验、房间或床位分配以及退改规则由订单详情页和门店政策共同确定。申请退款后，平台会按照规则审核并更新退款状态。</text>
				<text class="rule-heading">四、用户行为</text>
				<text class="rule-paragraph">不得使用平台从事违法活动，不得冒用他人身份，不得恶意占用库存、重复提交订单或干扰平台正常运行。发生违规时，平台可以采取限制服务、取消订单等措施。</text>
			</view>

			<view v-else-if="activeTab === 'privacy'" class="rule-document">
				<text class="rule-kicker">TCPMS 智选旅宿隐私政策</text>
				<text class="rule-updated">更新日期：2026年09月30日</text>
				<text class="rule-title">我们只为完成旅宿服务而使用必要信息</text>
				<view class="privacy-highlights">
					<view v-for="item in privacyHighlights" :key="item.title" class="privacy-highlight">
						<ProtoIcon class="privacy-icon" :name="item.icon" :size="30" />
						<text class="privacy-title">{{ item.title }}</text>
						<text class="privacy-desc">{{ item.desc }}</text>
					</view>
				</view>
				<text class="rule-heading">一、我们收集的信息</text>
				<text class="rule-paragraph">为完成登录、下单、支付、入住核验和售后服务，我们可能处理微信身份、手机号、入住人姓名、证件信息、订单信息和必要的设备及日志信息。</text>
				<text class="rule-heading">二、位置信息的使用</text>
				<text class="rule-paragraph">获得授权后，位置信息仅用于附近门店排序、距离展示和地图导航入口。拒绝授权不会影响你浏览全部门店、预订房型和查看订单。</text>
				<text class="rule-heading">三、敏感信息保护</text>
				<text class="rule-paragraph">身份证等敏感信息会进行加密存储和页面脱敏，仅在订单履约、入住核验和相关售后场景中由经过授权的人员访问。</text>
				<text class="rule-heading">四、你的权利</text>
				<text class="rule-paragraph">你可以通过客服入口查询、更正或申请删除相关个人信息，也可以在微信设置中管理位置、手机号和订阅消息等授权。</text>
			</view>

			<view v-else class="rule-document">
				<text class="rule-kicker">权限与第三方 SDK 说明</text>
				<text class="rule-updated">更新日期：2026年09月30日</text>
				<text class="rule-title">每一项权限，都有明确的使用场景</text>
				<view v-for="item in sdkItems" :key="item.title" class="sdk-item">
					<view class="sdk-icon"><ProtoIcon :name="item.icon" :size="28" /></view>
					<view class="sdk-main">
						<text class="sdk-title">{{ item.title }}</text>
						<text class="sdk-desc">{{ item.desc }}</text>
					</view>
					<text class="proto-pill" :class="item.required ? 'warn' : 'info'">{{ item.required ? '必要' : '可选' }}</text>
				</view>
				<text class="rule-heading">第三方服务</text>
				<text class="rule-paragraph">地图与地址解析能力由高德开放平台提供，用于门店定位、地址标准化和距离计算。微信登录、支付、订阅消息和官方客服入口由微信相关能力提供。</text>
				<text class="rule-heading">授权管理</text>
				<text class="rule-paragraph">你可以根据页面提示选择是否授权可选权限。关闭可选权限后，相关功能可能无法提供，但不影响不依赖该权限的其他旅宿服务。</text>
			</view>
		</scroll-view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				activeTab: 'service',
				tabs: [
					{ key: 'service', label: '服务协议' },
					{ key: 'privacy', label: '隐私政策' },
					{ key: 'sdk', label: '权限与SDK' }
				],
				complianceItems: [
					{ icon: 'location', title: '位置权限原则', tag: '非必需', desc: '仅用于附近门店排序与推荐，拒绝授权仍可手动切换城市。' },
					{ icon: 'shield', title: '身份证件严格脱敏加密', tag: 'AES-256', desc: '实名信息仅用于入住核验与报备，页面统一脱敏展示。' },
					{ icon: 'wallet', title: '直连清算与资金安全', tag: '官方通道', desc: '支付由微信官方渠道完成，平台不保存银行卡密码等敏感凭据。' }
				],
				privacyHighlights: [
					{ icon: 'info', title: '目的明确', desc: '只为完成预订与履约使用' },
					{ icon: 'location', title: '位置可选', desc: '拒绝定位仍可正常预订' },
					{ icon: 'shield', title: '安全保护', desc: '敏感信息加密与脱敏' }
				],
				sdkItems: [
					{ icon: 'user', title: '微信登录与手机号', desc: '用于账号创建、身份识别和订单联系。', required: true },
					{ icon: 'location', title: '位置权限', desc: '用于附近门店排序、距离展示和导航入口。', required: false },
					{ icon: 'image', title: '相册与相机', desc: '用于后续客服举证、发票或入住材料上传。', required: false },
					{ icon: 'book', title: '订阅消息', desc: '用于发送订单状态、入住提醒和退款结果。', required: false },
					{ icon: 'map', title: '高德地图服务', desc: '用于门店地图、地址解析和坐标能力。', required: true }
				]
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
