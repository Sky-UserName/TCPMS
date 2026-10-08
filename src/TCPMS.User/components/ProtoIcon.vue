<template>
	<image
		v-if="iconSrc"
		class="proto-icon"
		:class="{ 'proto-icon-white-filter': needsWhiteFilter }"
		:src="iconSrc"
		:style="{ width: `${size}rpx`, height: `${size}rpx` }"
		mode="aspectFit"
	></image>
	<text
		v-else
		class="proto-icon-fallback"
		:style="{ width: `${size}rpx`, height: `${size}rpx`, lineHeight: `${size}rpx` }"
	>{{ fallback }}</text>
</template>

<script>
	const aliases = {
		arrow_back: 'back',
		arrow_back_ios_new: 'back',
		back: 'back',
		more_horiz: 'more',
		more: 'more',
		adjust: 'status',
		trip_origin: 'status',
		radio_button_checked: 'status',
		person: 'user',
		account_circle: 'user',
		map: 'map',
		location_on: 'location',
		explore: 'navigation',
		my_location: 'navigation',
		near_me: 'navigation',
		search: 'search',
		manage_search: 'search',
		tune: 'tune',
		calendar_month: 'calendar',
		chevron_right: 'chevron-right',
		keyboard_arrow_right: 'chevron-right',
		arrow_forward: 'chevron-right',
		chevron_down: 'chevron-down',
		expand_more: 'chevron-down',
		keyboard_arrow_down: 'chevron-down',
		chevron_up: 'chevron-up',
		expand_less: 'chevron-up',
		share: 'share',
		favorite: 'heart',
		phone: 'phone',
		support_agent: 'support',
		chat: 'support',
		storefront: 'store',
		apartment: 'store',
		hotel: 'bed',
		bed: 'bed',
		receipt: 'receipt',
		receipt_long: 'receipt',
		group: 'group',
		qr_code_2: 'qr',
		settings: 'settings',
		check: 'check',
		check_circle: 'check-circle',
		verified: 'verified',
		verified_user: 'shield',
		security: 'shield',
		lock: 'lock',
		key: 'key',
		description: 'document',
		receipt_long: 'receipt',
		auto_stories: 'book',
		info: 'info',
		star: 'star',
		schedule: 'clock',
		access_time: 'clock',
		image: 'image',
		photo_camera: 'image',
		edit: 'edit',
		add: 'plus',
		remove: 'minus',
		close: 'close',
		warning: 'warning',
		help: 'help',
		refresh: 'refresh',
		wifi: 'wifi',
		local_parking: 'parking',
		cottage: 'house',
		home: 'house',
		roofing: 'house'
	}

	export default {
		name: 'ProtoIcon',
		props: {
			name: {
				type: String,
				default: ''
			},
			size: {
				type: [Number, String],
				default: 32
			},
			tone: {
				type: String,
				default: 'default'
			},
			fallback: {
				type: String,
				default: ''
			}
		},
		computed: {
			iconName() {
				const value = String(this.name || '').trim()
				return aliases[value] || value
			},
			iconSrc() {
				if (!this.iconName) return ''
				const suffix = this.tone === 'white' && this.whiteAssetAvailable ? '-white' : ''
				return `/static/prototype-icons/ui-${this.iconName}${suffix}.png`
			},
			whiteAssetAvailable() {
				return ['check-circle', 'check', 'chevron-right', 'navigation', 'plus', 'receipt', 'search', 'shield', 'support', 'user'].indexOf(this.iconName) >= 0
			},
			needsWhiteFilter() {
				return this.tone === 'white' && !this.whiteAssetAvailable
			}
		}
	}
</script>

<style lang="scss" scoped>
	.proto-icon {
		display: block;
		flex: 0 0 auto;
	}

	.proto-icon-white-filter {
		filter: brightness(0) invert(1);
	}

	.proto-icon-fallback {
		display: inline-block;
		overflow: hidden;
		flex: 0 0 auto;
		color: var(--proto-primary-dark);
		font-size: 26rpx;
		text-align: center;
		white-space: nowrap;
	}
</style>
