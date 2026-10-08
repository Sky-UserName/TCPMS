<template>
	<view class="proto-page collection-page">
		<ProtoHeader title="我的收藏" theme="green" />

		<view v-if="items.length" class="collection-list">
			<view v-for="item in items" :key="item.id" class="collection-card proto-card" @tap="openItem(item)">
				<image :src="item.image || fallbackImage" mode="aspectFill"></image>
				<view class="collection-copy">
					<view class="collection-heading">
						<text class="collection-title">{{ item.title }}</text>
						<text class="collection-type">{{ typeLabel(item.type) }}</text>
					</view>
					<text class="collection-subtitle">{{ item.subtitle || '已保存的 TCPMS 内容' }}</text>
					<view class="collection-action"><text>查看详情</text><ProtoIcon name="chevron-right" :size="24" /></view>
				</view>
				<button class="collection-remove" hover-class="none" @tap.stop="removeItem(item)"><ProtoIcon name="heart" :size="25" /></button>
			</view>
		</view>

		<ProtoEmptyState v-else class="collection-empty" action-text="去发现房源" @action="goStores" />

		<ProtoBottomNav active="me" />
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoBottomNav from '@/components/ProtoBottomNav.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import ProtoEmptyState from '@/components/ProtoEmptyState.vue'
	import { demoRooms, demoStores, goPage, prototypeImages, showToast } from '@/common/prototype.js'
	import { getFavoriteRecords, toggleFavorite } from '@/common/app-store.js'

	const resolveRecord = (record) => {
		if (record.title) return record
		const id = String(record.id || '')
		if (id.indexOf('store-') === 0) {
			const item = demoStores.find((store) => String(store.id) === id.slice(6))
			return item ? {
				...record,
				type: 'store',
				title: item.name,
				subtitle: item.address,
				image: item.image,
				path: `/pages/store/detail?id=${item.id}`
			} : record
		}
		if (id.indexOf('room-') === 0) {
			const item = demoRooms.find((room) => String(room.id) === id.slice(5))
			return item ? {
				...record,
				type: 'room',
				title: item.name,
				subtitle: `¥${item.price}/晚 · ${item.capacity}`,
				image: item.image,
				path: `/pages/room/detail?id=${item.id}`
			} : record
		}
		return record
	}

	export default {
		components: { ProtoHeader, ProtoBottomNav, ProtoIcon, ProtoEmptyState },
		data() {
			return {
				items: [],
				fallbackImage: prototypeImages.homeHero
			}
		},
		onShow() {
			this.loadItems()
		},
		methods: {
			loadItems() {
				this.items = getFavoriteRecords().map(resolveRecord)
			},
			typeLabel(type) {
				return type === 'room' ? '房型' : type === 'article' ? '攻略' : '门店'
			},
			openItem(item) {
				if (item.path) {
					goPage(item.path)
					return
				}
				showToast('该收藏内容暂不可打开')
			},
			removeItem(item) {
				toggleFavorite(item.id)
				this.loadItems()
				showToast('已取消收藏')
			},
			goStores() {
				uni.reLaunch({ url: '/pages/store/list' })
			}
		}
	}
</script>

<style lang="scss" scoped>
	.collection-page {
		padding-bottom: 136rpx;
		padding-bottom: calc(136rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(136rpx + env(safe-area-inset-bottom));
	}

	.collection-list {
		display: flex;
		flex-direction: column;
		gap: 18rpx;
		padding: 18rpx 24rpx 30rpx;
	}

	.collection-card {
		position: relative;
		display: flex;
		align-items: center;
		gap: 18rpx;
		padding: 16rpx;
	}

	.collection-card > image {
		width: 176rpx;
		height: 150rpx;
		flex: 0 0 176rpx;
		border-radius: 16rpx;
	}

	.collection-copy {
		flex: 1;
		min-width: 0;
		padding-right: 48rpx;
	}

	.collection-heading {
		display: flex;
		align-items: flex-start;
		gap: 8rpx;
	}

	.collection-title {
		flex: 1;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
		line-height: 1.35;
	}

	.collection-type {
		padding: 5rpx 9rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 10rpx;
		font-size: 16rpx;
		white-space: nowrap;
	}

	.collection-subtitle {
		display: block;
		margin-top: 10rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.45;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.collection-action {
		display: flex;
		align-items: center;
		gap: 4rpx;
		margin-top: 16rpx;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
		font-weight: 650;
	}

	.collection-remove {
		position: absolute;
		top: 16rpx;
		right: 12rpx;
		width: 52rpx;
		height: 52rpx;
		padding: 0;
		color: var(--proto-primary-dark);
		background: transparent;
		line-height: 52rpx;
	}

	.collection-empty {
		margin: 24rpx;
	}
</style>
