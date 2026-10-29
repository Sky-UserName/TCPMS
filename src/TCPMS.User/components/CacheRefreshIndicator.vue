<template>
	<view v-if="refreshing" class="cache-refresh-indicator" aria-live="polite">
		<ProtoIcon name="refresh" :size="22" />
		<text>正在同步最新数据，缓存内容暂时保留</text>
	</view>
</template>

<script>
	import ProtoIcon from '@/components/ProtoIcon.vue'

	export default {
		components: { ProtoIcon },
		props: {
			refreshing: { type: Boolean, default: false }
		}
	}
</script>

<style scoped>
	.cache-refresh-indicator {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 10rpx;
		min-height: 48rpx;
		padding: 8rpx 20rpx;
		color: var(--proto-primary-dark, #168783);
		font-size: 22rpx;
	}

	.cache-refresh-indicator :deep(.proto-icon) {
		animation: refresh-spin 0.9s linear infinite;
	}

	@keyframes refresh-spin {
		to { transform: rotate(360deg); }
	}
</style>
