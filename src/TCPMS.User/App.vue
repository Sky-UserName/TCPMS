<script>
	export default {
		onLaunch: function() {
			console.log('择居青旅 user app launch')
			uni.hideTabBar({ animation: false, fail: () => {} })
		},
		onShow: function() {
			console.log('择居青旅 user app show')
			uni.hideTabBar({ animation: false, fail: () => {} })
		},
		onHide: function() {
			console.log('择居青旅 user app hide')
		}
	}
</script>

<style lang="scss">
	@import "./common/prototype.scss";

	page {
		background: var(--proto-page);
		color: var(--proto-text);
		font-family: "PingFang SC", "Microsoft YaHei", Arial, sans-serif;
	}
</style>
