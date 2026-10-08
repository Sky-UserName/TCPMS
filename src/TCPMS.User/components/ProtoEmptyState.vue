<template>
	<view class="proto-empty-state" :class="{ compact }">
		<image class="proto-empty-state-image" src="/static/prototype-empty-data.png" mode="aspectFit" />
		<text class="proto-empty-state-title">{{ title }}</text>
		<text v-if="description" class="proto-empty-state-desc">{{ description }}</text>
		<button v-if="actionText" class="proto-empty-state-action" hover-class="none" @tap="$emit('action')">{{ actionText }}</button>
	</view>
</template>

<script>
	export default {
		name: 'ProtoEmptyState',
		emits: ['action'],
		props: {
			title: {
				type: String,
				default: '暂无相关数据'
			},
			description: {
				type: String,
				default: ''
			},
		actionText: {
			type: String,
			default: ''
		},
		compact: {
			type: Boolean,
			default: false
		}
		}
	}
</script>

<style lang="scss" scoped>
	.proto-empty-state {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 132rpx 30rpx 110rpx;
		color: #87939b;
		text-align: center;
	}

	.proto-empty-state-image {
		width: 250rpx;
		height: 210rpx;
	}

	.proto-empty-state-title {
		margin-top: 28rpx;
		color: #7b8792;
		font-size: 28rpx;
	}

	.proto-empty-state-desc {
		max-width: 620rpx;
		margin-top: 10rpx;
		color: #9aa5ad;
		font-size: 21rpx;
		line-height: 1.5;
	}

	.proto-empty-state-action {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 264rpx;
		height: 68rpx;
		margin: 30rpx auto 0;
		padding: 0 24rpx;
		color: var(--proto-primary-dark, #006a6a);
		background: var(--proto-surface-tint, #e7f7f7);
		border: 1rpx solid #b9e4e5;
		border-radius: 34rpx;
		box-shadow: none;
		font-size: 23rpx;
		font-weight: 650;
		line-height: 1;
		white-space: nowrap;
	}

	.proto-empty-state.compact {
		padding: 42rpx 18rpx;
	}

	.proto-empty-state.compact .proto-empty-state-image {
		width: 190rpx;
		height: 160rpx;
	}

	.proto-empty-state.compact .proto-empty-state-title {
		margin-top: 16rpx;
		font-size: 23rpx;
	}
</style>
