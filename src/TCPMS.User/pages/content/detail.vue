<template>
	<view class="proto-page proto-page-plain content-page">
		<ProtoHeader title="攻略详情" theme="green" />
		<scroll-view scroll-y class="article-scroll">
			<view class="article-meta">
				<text class="proto-pill info">出行攻略 / 官方精选</text>
				<view class="article-actions">
					<button hover-class="none" @tap="toggleArticleFavorite"><ProtoIcon :name="favorite ? 'heart-filled' : 'heart'" :size="25" /></button>
					<button hover-class="none" @tap="shareArticle"><ProtoIcon name="share" :size="25" /></button>
				</view>
			</view>
			<text class="article-title">早上好！从附近开始一段清爽舒心的秋日漫居旅程</text>
			<view class="article-byline">
				<view class="article-author"><ProtoIcon name="user" :size="20" /><text>TCPMS 智选生活局</text></view>
				<text>2026年09月30日　·　1,420次阅读</text>
			</view>
			<image class="article-cover" :src="articleImage" mode="aspectFill"></image>
			<text class="article-cover-caption">云舍民宿·二七路店 · 晨光燕枝中的原木茶室</text>

			<view class="article-body">
				<text class="article-quote">秋风渐起，逃离城市喧嚣并不需要太远跋涉。TCPMS 智选旅宿为您甄选城市中心偏中取静的质感住所，无论是独自放空，好友小聚还是青年创客同行，这里都有恰到好处的停留方式。</text>
				<text class="article-heading">01　从附近开始，给旅途减一点负担</text>
				<text class="article-paragraph">距离地铁、商圈和公园都不远的门店，适合临时起意的短途出行。抵达后可以把时间留给散步、阅读和一顿慢慢吃完的晚餐，而不是消耗在往返路上。</text>
				<text class="article-heading">02　选一间适合自己的房间</text>
				<text class="article-paragraph">喜欢安静，可以选择采光好的独立大床房；和朋友同行，可以看看双床房或多人间床位。先确定入住日期和人数，再根据设施、价格和评价做决定，会比只看一张照片更可靠。</text>
				<text class="article-heading">03　把入住变成旅途的一部分</text>
				<text class="article-paragraph">提前保存门店地址，打开导航入口，抵达时直接联系前台。需要发票、寄存行李或调整入住时间，也可以在订单详情里联系官方客服和门店管家。</text>

				<view class="article-store-card">
					<image :src="store.image" mode="aspectFill"></image>
					<view class="article-store-main">
						<text class="article-store-title">{{ store.name }}</text>
						<text class="article-store-desc">{{ store.address }}</text>
						<view class="article-store-price"><text>¥{{ store.price }} 起 ·</text><ProtoIcon name="star" :size="18" /><text>{{ store.rating }}</text></view>
					</view>
					<button class="proto-button-small" hover-class="none" @tap="goStore">查看门店</button>
				</view>

				<view class="related-heading">
					<text>继续探索</text>
					<text class="proto-muted proto-small">更多旅宿灵感</text>
				</view>
				<view class="related-list">
					<view v-for="item in related" :key="item.title" class="related-item" @tap="showToast(item.title)">
						<image :src="item.image" mode="aspectFill"></image>
						<view>
							<text class="related-title">{{ item.title }}</text>
							<text class="related-desc">{{ item.desc }}</text>
						</view>
					</view>
				</view>
			</view>
		</scroll-view>

		<view class="proto-bottom-actions">
			<button class="detail-bottom-link" hover-class="none" @tap="toggleArticleFavorite"><ProtoIcon :name="favorite ? 'heart-filled' : 'heart'" :size="22" /><text>{{ favorite ? '已收藏' : '收藏' }}</text></button>
			<button class="proto-primary-button" hover-class="none" @tap="goRoom"><text>立即预订同款房型</text><ProtoIcon name="chevron-right" tone="white" :size="26" /></button>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoRooms, demoStores, goPage, prototypeImages, showToast } from '@/common/prototype.js'
	import { getFavorites, recordRecent, toggleFavorite } from '@/common/app-store.js'

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				articleImage: prototypeImages.articleDetail || prototypeImages.article,
				store: demoStores[0],
				favorite: false,
				related: [
					{
						title: '一张地图，发现城市里的安静角落',
						desc: '附近门店与周末散步路线',
						image: prototypeImages.storeTwo
					},
					{
						title: '入住前要准备的 5 件小事',
						desc: '让抵达和离店都更从容',
						image: prototypeImages.roomTwo
					}
			]
			}
		},
		onShow() {
			this.favorite = getFavorites().map(String).includes('article-detail')
			recordRecent({
				id: 'article-detail',
				type: 'article',
				title: '早上好！从附近开始一段清爽舒心的秋日漫居旅程',
				subtitle: '出行攻略 / 官方精选',
				image: this.articleImage,
				path: '/pages/content/detail'
			})
		},
		methods: {
			goStore() {
				goPage(`/pages/store/detail?id=${this.store.id}`)
			},
			goRoom() {
				goPage(`/pages/room/detail?id=${demoRooms[0].id}`)
			},
			toggleArticleFavorite() {
				this.favorite = toggleFavorite('article-detail', {
					type: 'article',
					title: '早上好！从附近开始一段清爽舒心的秋日漫居旅程',
					subtitle: '出行攻略 / 官方精选',
					image: this.articleImage,
					path: '/pages/content/detail'
				})
				showToast(this.favorite ? '已收藏攻略' : '已取消收藏')
			},
			shareArticle() {
				uni.setClipboardData({
					data: '早上好！从附近开始一段清爽舒心的秋日漫居旅程',
					success: () => showToast('攻略标题已复制')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.content-page {
		padding-bottom: 136rpx;
		padding-bottom: calc(136rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(136rpx + env(safe-area-inset-bottom));
	}

	.article-scroll {
		height: calc(100vh - 112rpx);
		padding-bottom: 136rpx;
	}

	.article-meta {
		display: flex;
		align-items: center;
		gap: 12rpx;
		padding: 18rpx 28rpx 0;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.article-meta .proto-pill {
		min-height: 34rpx;
		padding: 0 12rpx;
		font-size: 17rpx;
	}

	.article-actions {
		display: flex;
		gap: 8rpx;
		margin-left: auto;
	}

	.article-actions button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 56rpx;
		height: 56rpx;
		padding: 0;
		color: var(--proto-text);
		background: var(--proto-surface);
		border-radius: 50%;
	}

	.article-title {
		display: block;
		padding: 18rpx 28rpx 0;
		color: var(--proto-text);
		font-size: 32rpx;
		font-weight: 850;
		line-height: 1.3;
	}

	.article-byline {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 14rpx;
		padding: 16rpx 28rpx 18rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
		line-height: 1.4;
	}

	.article-author {
		display: flex;
		align-items: center;
		gap: 5rpx;
		flex: 0 0 auto;
	}

	.article-byline > text {
		flex: 1 1 220rpx;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.article-cover {
		display: block;
		width: calc(100% - 48rpx);
		height: 372rpx;
		margin: 0 24rpx;
		border-radius: 22rpx;
	}

	.article-cover-caption {
		display: block;
		padding: 8rpx 28rpx 0;
		color: var(--proto-muted);
		font-size: 16rpx;
		text-align: center;
	}

	.article-body {
		padding: 24rpx 28rpx;
	}

	.article-paragraph,
	.article-heading,
	.article-quote {
		display: block;
	}

	.article-paragraph {
		margin-top: 20rpx;
		color: var(--proto-text);
		font-size: 21rpx;
		line-height: 1.75;
	}

	.article-heading {
		margin-top: 34rpx;
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 800;
	}

	.article-quote {
		margin-top: 22rpx;
		padding: 20rpx 22rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-left: 6rpx solid var(--proto-primary);
		border-radius: 0 16rpx 16rpx 0;
		font-size: 21rpx;
		font-weight: 500;
		line-height: 1.6;
	}

	.article-store-card {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 32rpx;
		padding: 16rpx;
		background: var(--proto-surface);
		border: 1rpx solid #dfe9ea;
		border-radius: 20rpx;
		box-shadow: 0 8rpx 24rpx rgba(42, 169, 169, 0.06);
	}

	.article-store-card image {
		width: 116rpx;
		height: 116rpx;
		flex: 0 0 116rpx;
		border-radius: 14rpx;
	}

	.article-store-main {
		flex: 1;
		min-width: 0;
	}

	.article-store-title,
	.article-store-desc,
	.article-store-price {
		display: block;
	}

	.article-store-title {
		overflow: hidden;
		color: var(--proto-text);
		font-size: 23rpx;
		font-weight: 750;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.article-store-desc,
	.article-store-price {
		margin-top: 8rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.45;
	}

	.article-store-price {
		display: flex;
		align-items: center;
		gap: 4rpx;
		color: var(--proto-primary-dark);
	}

	.article-store-card .proto-button-small {
		height: 56rpx;
		padding: 0 14rpx;
		font-size: 18rpx;
	}

	.related-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 34rpx;
		color: var(--proto-text);
		font-size: 26rpx;
		font-weight: 800;
	}

	.related-list {
		display: flex;
		gap: 14rpx;
		margin-top: 16rpx;
		overflow-x: auto;
	}

	.related-item {
		width: 300rpx;
		flex: 0 0 300rpx;
		overflow: hidden;
		background: var(--proto-surface);
		border: 1rpx solid rgba(197, 199, 202, 0.28);
		border-radius: 18rpx;
	}

	.related-item image {
		display: block;
		width: 100%;
		height: 154rpx;
	}

	.related-item > view {
		padding: 14rpx;
	}

	.related-title,
	.related-desc {
		display: block;
	}

	.related-title {
		color: var(--proto-text);
		font-size: 21rpx;
		font-weight: 700;
		line-height: 1.4;
	}

	.related-desc {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.detail-bottom-link {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 5rpx;
		width: 140rpx;
		padding: 0;
		color: var(--proto-text);
		background: transparent;
		font-size: 19rpx;
	}

	.proto-bottom-actions > .proto-primary-button {
		flex: 1;
	}
</style>
