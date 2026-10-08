<script>
	export default {
		onLaunch: function() {
			console.log('TCPMS user app launch')
		},
		onShow: function() {
			console.log('TCPMS user app show')
		},
		onHide: function() {
			console.log('TCPMS user app hide')
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
