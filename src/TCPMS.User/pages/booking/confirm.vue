<template>
	<view class="proto-page booking-confirm-page">
		<ProtoHeader title="订单填写" title-align="center" theme="green" />

			<view class="confirm-summary proto-card">
				<view class="confirm-room-preview">
					<image class="confirm-room-image" :src="booking.roomImage || booking.image" mode="aspectFill"></image>
					<view class="confirm-room-copy">
						<text class="confirm-room-title">{{ booking.room }}</text>
						<view class="confirm-room-tags">
							<text v-for="tag in roomTags" :key="tag">{{ tag }}</text>
						</view>
						<text class="confirm-room-location">{{ booking.store }} · 面积{{ booking.roomArea || '舒适房型' }}</text>
					</view>
				</view>
			<view class="confirm-dates">
				<view class="confirm-date-side">
					<text>入住时间</text>
					<text class="strong">{{ booking.checkIn }}</text>
					<text class="small">{{ checkInWeekday }} · 14:00后</text>
				</view>
				<view class="confirm-arrow">
					<text>共 {{ booking.nights }} 晚</text>
					<ProtoIcon name="chevron-right" :size="22" />
				</view>
				<view class="confirm-date-side confirm-date-side-right">
					<text>离店时间</text>
					<text class="strong">{{ booking.checkOut }}</text>
					<text class="small">{{ checkOutWeekday }} · 12:00前</text>
				</view>
			</view>
			<view class="confirm-meta">
				<text>{{ booking.roomCount }}间 · {{ booking.guestCount }}位成人出行</text>
				<text class="proto-pill info">免费双人手作早餐</text>
				<text class="proto-pill info">独立卫浴</text>
			</view>
		</view>

		<view class="confirm-guest proto-card">
			<view class="guest-section-head">
				<view>
					<text class="guest-section-title">入住信息</text>
					<text class="guest-section-subtitle">实名登记，保障入住顺利办理</text>
				</view>
				<view class="proto-link confirm-guest-link" @tap="goGuests()">
					<ProtoIcon name="plus" :size="17" />
					<text>{{ guestLinkText }}</text>
				</view>
			</view>
			<view v-if="!guests.length" class="guest-empty">
				<view class="guest-empty-icon"><ProtoIcon name="group" :size="40" /></view>
				<text class="guest-empty-title">还没有添加入住人</text>
				<text class="guest-empty-desc">至少选择 1 位实名入住人，同行人可一起勾选</text>
				<button class="proto-ghost-button guest-empty-action" hover-class="none" @tap="goGuests()">
					<ProtoIcon name="plus" :size="22" />
					<text>添加入住人</text>
				</button>
			</view>
			<view v-for="(guest, index) in guests" :key="guest.id || guest.name" class="guest-row">
				<view class="guest-avatar"><text>{{ guest.name ? guest.name.slice(0, 1) : '客' }}</text></view>
				<view class="guest-info">
					<view class="guest-name-row">
						<text class="guest-name">{{ guest.name }}</text>
						<text class="proto-pill" :class="index === 0 ? 'primary' : 'info'">{{ index === 0 ? '主入住人' : '同行住客' }}</text>
					</view>
					<view class="guest-meta-line" v-if="guest.identity"><ProtoIcon name="document" :size="19" /><text>身份证 {{ guest.identity }}</text></view>
					<view class="guest-meta-line" v-if="guest.phone"><ProtoIcon name="phone" :size="19" /><text>手机号 {{ guest.phone }}</text></view>
				</view>
				<view class="guest-actions">
					<view class="guest-edit-button" aria-label="修改入住人" @tap="goGuests(guest.id)">
						<ProtoIcon class="guest-edit" name="edit" :size="22" />
					</view>
					<view class="guest-delete-button" aria-label="删除入住人" @tap="deleteGuest(guest)">
						<ProtoIcon name="trash" :size="22" />
					</view>
				</view>
			</view>
			<view v-if="guests.length" class="guest-add-more" @tap="goGuests()">
				<ProtoIcon name="plus" :size="20" />
				<text>继续添加入住人</text>
			</view>
		</view>

		<view class="confirm-fee proto-card">
			<view class="card-heading">
				<text>费用明细</text>
				<text class="proto-caption">明细透明 · 无隐藏消费</text>
			</view>
			<view v-for="item in feeItems" :key="item.label" class="fee-row">
				<text>{{ item.label }}</text>
				<text :class="{ discount: item.discount }">{{ item.value }}</text>
			</view>
			<view class="fee-total">
				<text>实付总计</text>
				<text class="strong">¥{{ total }}</text>
			</view>
			<view v-if="isApiBooking && quoteError" class="confirm-error">
				<text>报价暂不可用：{{ quoteError }}</text>
				<text v-if="quoteErrorAction === 'date'" class="confirm-error-link" @tap="goDate">修改入住日期</text>
				<text v-if="quoteErrorAction === 'date' && canReduceRooms" class="confirm-error-link" @tap="reduceRoomCount">改为 1 间</text>
			</view>
			<view v-else-if="!isApiBooking" class="confirm-error">当前房源为本地演示数据，连接真实服务端房源后才能提交订单。</view>
		</view>

		<view class="confirm-policy proto-card">
			<view class="card-heading">
				<view class="inline-icon-text"><ProtoIcon name="shield" :size="22" /><text>退改规则与入离须知</text></view>
				<text class="policy-required">支付前必读</text>
			</view>
			<view class="policy-summary-trigger" @tap="showPolicyDetail">
				<text>入住日前1天18:00前可免费取消，逾期按规则收取费用</text>
				<view class="proto-link"><text>查看详情</text><ProtoIcon name="chevron-right" :size="20" /></view>
			</view>
			<view class="proto-check agreement-check" hover-class="agreement-check-hover" @tap="toggleRuleAgreement">
				<view class="proto-check-box" :class="{ checked: ruleAgreed }"><ProtoIcon v-if="ruleAgreed" name="check" :size="18" tone="white" /></view>
				<text>我已阅读并完全同意《订房退改规则与入离守则》与《人身财产安全保障协议》</text>
			</view>
		</view>

		<view class="payment-security">
			<view><ProtoIcon name="shield" :size="20" /><text>微信商户官方资金担保 · 入住后离店清算结算</text></view>
		</view>

			<view class="proto-bottom-actions confirm-bottom-actions">
			<view class="pay-total">
				<text>¥</text>
				<text class="strong">{{ total }}</text>
				<view class="pay-total-detail"><text>明细</text><ProtoIcon name="chevron-up" :size="18" /></view>
			</view>
			<button class="proto-primary-button" hover-class="none" :disabled="submitStatus === 'locking' || !isApiBooking || quoteLoading || quoteCents === null" @tap="submitOrder"><ProtoIcon name="shield" tone="white" :size="21" /><text>{{ submitStatus === 'locking' ? '正在锁定库存...' : quoteLoading ? '正在核对价格...' : '提交订单' }}</text></button>
		</view>

		<view v-if="authRedirecting" class="auth-redirect-mask">
			<view class="auth-redirect-card">
				<text class="auth-redirect-title">需要登录</text>
				<text class="auth-redirect-desc">正在前往登录页，请稍候…</text>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { goPage, showToast } from '@/common/prototype.js'
	import { getApiToken, getBooking, saveBooking } from '@/common/app-store.js'
	import { createRemoteOrder, fetchRemoteOrders, fetchStoreDetail, previewRemoteOrder, redirectToLogin } from '@/common/api.js'

	const weekdayLabels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
	const isGuid = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(String(value || ''))

	const getWeekday = (isoDate) => {
		const date = new Date(isoDate)
		return Number.isNaN(date.getTime()) ? '' : weekdayLabels[date.getDay()]
	}

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			const booking = getBooking()
			return {
				booking,
				// 入住人默认留空：必须由用户自己从「常用入住人」里选择或新增，不做任何预填。
				guests: [],
				ruleAgreed: false,
				submitStatus: 'idle',
				quoteCents: null,
				quoteLoading: false,
				quoteError: '',
				// 报价失败后可以引导用户去做的动作：'date' 改日期；空串表示只能提示文案。
				quoteErrorAction: '',
				// 同一轮失败里 onLoad/onShow 会各请求一次，只弹一次 toast，避免连续两个提示框。
				quoteErrorToasted: false,
				quoteRequestId: 0,
				quoteFixAttempted: false,
				authRedirecting: false
			}
		},
		computed: {
			checkInWeekday() {
				return getWeekday(this.booking.isoCheckIn)
			},
			checkOutWeekday() {
				return getWeekday(this.booking.isoCheckOut)
			},
			isApiBooking() {
				return isGuid(this.booking.storeId) && isGuid(this.booking.roomId)
			},
			// 库存不足且订了多间时，允许一键收回到 1 间重新报价（房源紧俏时最常见的卡点）。
			canReduceRooms() {
				return Number(this.booking.roomCount || 1) > 1
			},
			roomSubtotal() {
				return Number(this.booking.roomPrice || 0) * Number(this.booking.roomCount || 1) * Number(this.booking.nights || 1)
			},
			discount() {
				return Math.min(60, Math.round(this.roomSubtotal * 0.15))
			},
			total() {
				if (this.isApiBooking) return this.quoteCents === null ? '0.00' : (this.quoteCents / 100).toFixed(2)
				return Math.max(0, this.roomSubtotal - this.discount)
			},
			roomTags() {
				const tags = Array.isArray(this.booking.roomTags) ? this.booking.roomTags.filter(Boolean) : []
				return (tags.length ? tags : ['私家花园', '近商圈', '近地铁']).slice(0, 4)
			},
			// 入住信息只在「至少 1 位」这个前提上校验，选择数量不限，所以入口只显示已选人数。
			guestLinkText() {
				return this.guests.length ? `已选 ${this.guests.length} 位 · 修改` : '添加入住人'
			},
			feeItems() {
				if (this.isApiBooking) {
					return [{
						label: `服务端报价（${this.booking.roomCount}间 × ${this.booking.nights}晚）`,
						value: this.quoteCents === null ? (this.quoteLoading ? '计算中' : '待计算') : `¥${(this.quoteCents / 100).toFixed(2)}`
					}]
				}
				return [
					{ label: `客房房费（${this.booking.roomCount}间 × ${this.booking.nights}晚）`, value: `¥${this.roomSubtotal}` },
					{ label: '平台初秋连住立减', value: `-¥${this.discount}`, discount: true },
					{ label: '住宿押金（微信信用免押）', value: '¥0' },
					{ label: '清洁保洁与管家服务费', value: '已包含' },
					{ label: '优惠券抵扣', value: '暂无可用券' }
				]
			}
		},
		onLoad() {
			// 选人页拿不到 eventChannel 时会走全局事件，这里两路都接上，回填逻辑本身是幂等的。
			uni.$on('booking:guests-selected', this.applyGuestSelection)
			// 远程房源报价必须登录：没有 token 就别先发请求了，直接进入跳转态，避免 401 后白屏等待。
			if (this.isApiBooking && !getApiToken()) {
				this.authRedirecting = true
				return
			}
			this.refreshData()
		},
		onUnload() {
			uni.$off('booking:guests-selected', this.applyGuestSelection)
		},
		onReady() {
			// 等首帧渲染完再 reLaunch：页面切换动画期间的跳转会被小程序排队，先给出跳转提示更稳；
			// 留 150ms 也是为了让当前的 navigateTo 先结束，避免它被登录跳转打断成 fail timeout。
			if (this.authRedirecting) redirectToLogin(150)
		},
		onShow() {
			if (this.authRedirecting) return
			this.refreshData()
		},
		methods: {
			refreshData() {
				this.booking = getBooking()
				// 入住人只来自「选择入住人」页面的回传，这里不做任何预填/兜底；
				// 实名入住人数与出行人数（guestCount）已解耦：至少 1 位、可多选，因此不做截断。
				this.quoteCents = null
				this.quoteError = ''
				this.quoteErrorAction = ''
				this.quoteFixAttempted = false
				this.loadQuote()
			},
			// 房型可住人数上限（maxGuests）只有从房型详情页进来才会写进 booking，
			// 从订单、首页等入口进来可能是空值，这里按门店详情补齐一次，避免要报价了才发现人数超限。
			async ensureRoomMeta() {
				if (!this.isApiBooking) return
				if (Number(this.booking.maxGuests || 0) > 0) return
				try {
					const { rooms } = await fetchStoreDetail(this.booking.storeId)
					const room = (rooms || []).find((item) => String(item.id) === String(this.booking.roomId))
					const maxGuests = Number((room && room.maxGuests) || 0)
					if (!maxGuests) return
					this.booking = saveBooking({
						...this.booking,
						maxGuests,
						room: room.name || this.booking.room,
						roomImage: room.image || this.booking.roomImage,
						roomPrice: Number(room.price || this.booking.roomPrice || 0)
					})
				} catch (error) {
					// 门店详情拿不到不影响报价本身，交给后面的报价错误提示兜底。
				}
			},
			// 床位房等房型限制「maxGuests × 间数」，超员会让报价接口 400；发请求前先收敛到上限内。
			sanitizeGuestCount() {
				const maxGuests = Number(this.booking.maxGuests || 0)
				if (!maxGuests) return
				const limit = maxGuests * Math.max(1, Number(this.booking.roomCount || 1))
				if (Number(this.booking.guestCount || 1) > limit) {
					this.booking = saveBooking({ ...this.booking, guestCount: limit })
				}
			},
			// 报价 400 的收尾：能就地修的（人数超限、日期过期）修完重试一次；
			// 修不了的给出可读原因和去处（换日期），避免页面只剩一句「库存不足」。
			// 返回值：'retry' 重试 / 'handled' 已给出提示 / false 交给默认文案。
			resolveQuoteError(error) {
				const message = typeof error?.message === 'string' ? error.message : ''
				if (!message) return false
				const canRetry = !this.quoteFixAttempted
				const roomCount = Math.max(1, Number(this.booking.roomCount || 1))

				if (message.includes('承载人数')) {
					// 人数超限但本地没有 maxGuests（不是从房型详情进来的）：按「每间 1 位」兜底收敛。
					const maxGuests = Number(this.booking.maxGuests || 0)
					const limit = maxGuests ? maxGuests * roomCount : roomCount
					if (Number(this.booking.guestCount || 1) <= limit) return false
					if (!canRetry) return false
					this.quoteFixAttempted = true
					this.booking = saveBooking({ ...this.booking, guestCount: limit })
					return 'retry'
				}

				if (message.includes('不能早于今天') || message.includes('必须早于离店日期') || message.includes('最多 90 晚')) {
					if (!canRetry) return false
					const pad = (value) => String(value).padStart(2, '0')
					const toIso = (date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T00:00:00`
					const toLabel = (date) => `${pad(date.getMonth() + 1)}月${pad(date.getDate())}日`
					const checkIn = new Date(Date.now() + 86400000)
					const checkOut = new Date(Date.now() + 2 * 86400000)
					this.booking = saveBooking({
						...this.booking,
						checkIn: toLabel(checkIn),
						checkOut: toLabel(checkOut),
						isoCheckIn: toIso(checkIn),
						isoCheckOut: toIso(checkOut),
						date: `${toLabel(checkIn)} - ${toLabel(checkOut)}`,
						nights: 1
					})
					this.quoteFixAttempted = true
					return 'retry'
				}

				if (message.includes('库存')) {
					const rooms = roomCount > 1 ? `（当前预订 ${roomCount} 间，可减少房间数）` : ''
					this.quoteError = `${message}${rooms}，请更换入住日期或稍后再试`
					this.quoteErrorAction = 'date'
					return 'handled'
				}

				if (message.includes('房型不存在或已下架')) {
					this.quoteError = '该房型已下架或暂不可售，请返回重新选择房源'
					this.quoteErrorAction = ''
					return 'handled'
				}

				return false
			},
			async loadQuote() {
				const requestId = ++this.quoteRequestId
				if (!this.isApiBooking) {
					this.quoteLoading = false
					return
				}
				this.quoteLoading = true
				try {
					// 补齐 maxGuests 再收敛人数，避免「人数超承载」这类本来能自动避免的 400。
					await this.ensureRoomMeta()
					if (requestId !== this.quoteRequestId) return
					this.sanitizeGuestCount()
					const quote = await previewRemoteOrder(this.booking)
					if (requestId !== this.quoteRequestId) return
					this.quoteCents = Number(quote.totalAmountCents)
					this.quoteError = ''
					this.quoteErrorAction = ''
					this.quoteErrorToasted = false
				} catch (error) {
					if (requestId !== this.quoteRequestId) return
					this.quoteCents = null
					if (error && error.code === 'unauthorized') {
						// 登录态已失效：接口层已经在跳登录，这里补一层遮罩，避免跳过订单页时出现白屏。
						this.authRedirecting = true
						return
					}
					const resolution = this.resolveQuoteError(error)
					if (resolution === 'retry') {
						await this.loadQuote()
						return
					}
					if (resolution !== 'handled') {
						this.quoteError = error?.message || '请检查登录状态、库存或网络后重试'
						this.quoteErrorAction = ''
					}
					// 报价失败时提交按钮是禁用的，光靠费用区的小字容易被忽略，这里显式提示一次原因。
					if (this.quoteError && !this.quoteErrorToasted) {
						this.quoteErrorToasted = true
						showToast(this.quoteError)
					}
				} finally {
					if (requestId === this.quoteRequestId) this.quoteLoading = false
				}
			},
			// 选人页确认后回填：勾选几位就保留几位（至少 1 位），按 id/证件号去重避免重复叠加。
			applyGuestSelection(picked) {
				if (!Array.isArray(picked)) return
				const seen = {}
				const guests = []
				picked.forEach((guest) => {
					if (!guest || !guest.name) return
					const key = String(guest.id || guest.identity || guest.name)
					if (seen[key]) return
					seen[key] = true
					guests.push({ ...guest })
				})
				this.guests = guests
			},
			toggleRuleAgreement() {
				this.ruleAgreed = !this.ruleAgreed
			},
			showPolicyDetail() {
				uni.showModal({
					title: '退改规则与入离须知',
					content: '入住时间：当日14:00后办理入住，次日12:00前退房。入住日前1天18:00前在小程序内申请，可享全额免手续费取消；逾期取消或未入住，将按首晚房费收取。入住时需提供真实有效的身份证件，房内禁止吸烟，具体以门店现场规则为准。',
					showCancel: false,
					confirmText: '我知道了'
				})
			},
			async submitOrder() {
				if (this.submitStatus === 'locking') return
				if (!this.isApiBooking) {
					showToast('演示房源不支持真实下单')
					return
				}
				if (this.quoteLoading || this.quoteCents === null) {
					await this.loadQuote()
					if (this.quoteCents === null) {
						showToast(this.quoteError || '暂时无法获取服务端报价')
						return
					}
				}
				if (!this.guests.length) {
					showToast('请至少选择 1 位入住人')
					return
				}
				if (!this.ruleAgreed) {
					showToast('请先勾选并同意退改规则')
					return
				}
				this.submitStatus = 'locking'
				const booking = {
					...this.booking,
					guests: this.guests.slice(),
					total: this.total
				}
				let order
				try {
					order = await createRemoteOrder(booking)
				} catch (error) {
					this.submitStatus = 'idle'
					// 409 库存不足最常见的场景：自己刚下过一单还没支付，库存已被这笔订单锁住
					//（房量少的房型尤其明显）。这时把用户直接领到待支付订单，而不是只弹“库存不足”。
					if (error?.code === 'inventory_insufficient' && await this.focusPendingOrder()) return
					showToast(error?.message || '订单未提交，请检查网络后重试')
					return
				}
				setTimeout(() => {
					if (this.submitStatus === 'locking') {
						goPage(`/pages/pay/result?orderId=${order.id}`)
					}
				}, 700)
			},
			// 库存不足时先看看是不是自己已有待支付订单占着库存：是就引导去支付/取消，避免反复撞 409。
			async focusPendingOrder() {
				let orders = []
				try {
					orders = await fetchRemoteOrders()
				} catch (error) {
					return false
				}
				const target = (Array.isArray(orders) ? orders : []).find((item) =>
					item &&
					item.statusKey === 'pay' &&
					String(item.roomId) === String(this.booking.roomId) &&
					(!this.booking.storeId || String(item.storeId) === String(this.booking.storeId)))
				if (!target) return false
				uni.showModal({
					title: '你已有一笔待支付订单',
					content: `订单 ${target.orderNumber} 已锁定该房型库存，请先完成支付或取消这笔订单，再重新下单。`,
					confirmText: '去支付',
					cancelText: '稍后处理',
					success: (res) => {
						if (res.confirm) goPage(`/pages/pay/result?orderId=${target.id}`)
					}
				})
				return true
			},
			deleteGuest(guest) {
				uni.showModal({
					title: '确认移除该入住人？',
					content: '仅从本次订单的入住人列表移除，不会删除常用入住人资料。',
					success: (res) => {
						if (!res.confirm) return
						this.guests = this.guests.filter((item) => String(item.id) !== String(guest.id))
						showToast('已从本次订单移除')
					}
				})
			},
			// 去「入住人管理」选人：带 mode=select 进入选择模式，确认后通过 eventChannel 回传选中的人。
			// 不传人数上限，选人页支持任意多选（订单只要求 ≥1 位）。
			// editId 有值时（点某个入住人的编辑图标）进页面就直接打开那个人的编辑弹层。
			goGuests(editId) {
				const selected = this.guests.map((guest) => String(guest.id || '')).filter(Boolean).join(',')
				const query = ['mode=select']
				if (selected) query.push(`selected=${encodeURIComponent(selected)}`)
				if (editId) query.push(`edit=${encodeURIComponent(String(editId))}`)
				uni.navigateTo({
					url: `/pages/me/guests?${query.join('&')}`,
					events: {
						guestsSelected: (picked) => this.applyGuestSelection(picked)
					}
				})
			},
			// 报价失败时直接去改入住日期，回来 onShow 会自动重新报价。
			goDate() {
				goPage('/pages/booking/date?from=confirm')
			},
			// 库存不足时一键收回房间数，立刻重新报价。
			reduceRoomCount() {
				this.booking = saveBooking({ ...this.booking, roomCount: 1 })
				this.quoteFixAttempted = false
				this.quoteErrorToasted = false
				this.loadQuote()
			}
		}
	}
</script>

<style lang="scss" scoped>
	.booking-confirm-page {
		padding-bottom: 154rpx;
		padding-bottom: calc(154rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(154rpx + env(safe-area-inset-bottom));
	}


	.confirm-dates {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 20rpx;
		padding: 20rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.confirm-date-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.confirm-date-side-right {
		align-items: flex-end;
		text-align: right;
	}

	.confirm-date-side > text:first-child {
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.confirm-date-side .strong {
		margin-top: 6rpx;
		color: var(--proto-text);
		font-size: 28rpx;
	}

	.confirm-date-side .small {
		margin-top: 5rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.confirm-arrow {
		display: flex;
		align-items: center;
		flex-direction: column;
		justify-content: center;
		flex: 0 0 98rpx;
		color: var(--proto-primary-dark);
		font-size: 19rpx;
		text-align: center;
	}

	.confirm-arrow > .proto-icon {
		margin-top: 4rpx;
	}

	.confirm-meta {
		display: flex;
		align-items: center;
		gap: 10rpx;
		margin-top: 16rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.confirm-meta .proto-pill {
		min-height: 32rpx;
		padding: 0 9rpx;
		font-size: 16rpx;
	}

	.card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		color: var(--proto-text);
		font-size: 25rpx;
		font-weight: 750;
	}

	.card-heading .proto-link,
	.card-heading .proto-caption {
		color: var(--proto-primary-dark);
		font-size: 18rpx;
		font-weight: 400;
	}

	.confirm-guest-link {
		display: flex;
		align-items: center;
		gap: 4rpx;
	}

	.guest-row {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 16rpx;
		padding: 18rpx;
		background: var(--proto-surface-low);
		border-radius: 16rpx;
	}

	.guest-avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 52rpx;
		height: 52rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 50%;
	}

	.guest-row > view {
		flex: 1;
	}

	.guest-name-row {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.guest-name {
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 750;
	}

	.guest-name-row .proto-pill {
		min-height: 30rpx;
		padding: 0 9rpx;
		font-size: 16rpx;
	}

	.guest-row view > text:not(.guest-name) {
		display: block;
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
	}

	.guest-edit {
		color: var(--proto-primary-dark);
	}

	.confirm-error {
		margin-top: 14rpx;
		padding: 16rpx;
		color: var(--proto-error);
		background: #ffe7e4;
		border-radius: 14rpx;
		font-size: 19rpx;
	}

	.confirm-error-link {
		margin-left: 8rpx;
		font-weight: 600;
		color: var(--proto-error);
		text-decoration: underline;
	}

	.confirm-guest .proto-ghost-button {
		height: 68rpx;
		margin: 16rpx 0 0;
		font-size: 21rpx;
	}

	.inline-icon-text {
		display: flex;
		align-items: center;
	}

	.inline-icon-text .proto-icon {
		margin-right: 5rpx;
	}

	.fee-row,
	.fee-total {
		display: flex;
		justify-content: space-between;
		margin-top: 16rpx;
		color: var(--proto-muted);
		font-size: 20rpx;
	}

	.fee-row .discount {
		color: var(--proto-error);
	}

	.fee-total {
		margin-top: 20rpx;
		padding-top: 18rpx;
		border-top: 1rpx solid #e3ebec;
		color: var(--proto-text);
	}

	.fee-total .strong {
		color: var(--proto-primary-dark);
		font-size: 34rpx;
	}

	.payment-security {
		padding: 20rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		text-align: center;
	}

	.payment-security > view {
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.payment-security .proto-icon {
		margin-right: 5rpx;
	}
	.confirm-bottom-actions {
		align-items: center;
	}

	.pay-total {
		display: flex;
		align-items: baseline;
		width: 190rpx;
		color: var(--proto-primary-dark);
	}

	.pay-total > text:first-child {
		font-size: 19rpx;
	}

	.pay-total .strong {
		font-size: 42rpx;
	}

	.pay-total-detail {
		display: flex;
		align-items: center;
		gap: 3rpx;
		margin-left: 8rpx;
		color: var(--proto-muted);
		font-size: 17rpx;
	}

	.confirm-bottom-actions .proto-primary-button {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 6rpx;
		flex: 1;
		margin: 0;
	}


	/* Booking form visual pass: green accents and a room-first confirmation card. */
	.booking-confirm-page {
		--booking-green: #2aa9a9;
		--booking-green-deep: #006a6a;
		--booking-green-tint: #dff6f5;
		background: #f5f9fc;
	}

	.booking-confirm-page :deep(.proto-header),
	.booking-confirm-page .proto-header {
		background: #2aa9a9 !important;
		border-bottom-color: rgba(255, 255, 255, 0.16) !important;
	}

	.booking-confirm-page :deep(.proto-header-title),
	.booking-confirm-page :deep(.proto-header-icon-button),
	.booking-confirm-page :deep(.proto-header-user-button),
	.booking-confirm-page .proto-header-title,
	.booking-confirm-page .proto-header-icon-button,
	.booking-confirm-page .proto-header-user-button {
		color: #ffffff !important;
	}

	.booking-confirm-page .proto-card {
		border-color: rgba(42, 169, 169, 0.14);
		box-shadow: 0 10rpx 28rpx rgba(0, 106, 106, 0.07);
	}

	.confirm-room-preview {
		display: flex;
		align-items: flex-start;
		gap: 16rpx;
	}

	.confirm-room-image {
		width: 190rpx;
		height: 154rpx;
		flex: 0 0 190rpx;
		border-radius: 16rpx;
		background: #e8f6f5;
	}

	.confirm-room-copy {
		min-width: 0;
		flex: 1;
	}

	.confirm-room-title {
		display: block;
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 850;
		line-height: 1.3;
	}

	.confirm-room-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 12rpx;
	}

	.confirm-room-tags text {
		padding: 6rpx 12rpx;
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.16);
		border-radius: 8rpx;
		font-size: 17rpx;
		white-space: nowrap;
	}

	.confirm-room-location {
		display: block;
		margin-top: 12rpx;
		overflow: hidden;
		color: var(--proto-muted);
		font-size: 19rpx;
		line-height: 1.4;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.booking-confirm-page .confirm-dates {
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.12);
	}

	.booking-confirm-page .confirm-arrow,
	.booking-confirm-page .confirm-guest-link,
	.booking-confirm-page .guest-edit,
	.booking-confirm-page .pay-total,
	.booking-confirm-page .fee-total .strong {
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .proto-pill.info {
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border-color: rgba(42, 169, 169, 0.14);
	}

	.booking-confirm-page .confirm-bottom-actions .proto-primary-button,
	.booking-confirm-page .confirm-bottom-actions .proto-primary-button > text {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 0;
		height: 78rpx;
		margin: 0;
		padding: 0 24rpx;
		color: #ffffff !important;
		background: var(--booking-green) !important;
		border: 0 !important;
		border-radius: 39rpx;
		font-size: 23rpx;
		font-weight: 800;
		line-height: 78rpx;
		text-align: center;
		text-shadow: none;
		white-space: nowrap;
	}

	.booking-confirm-page .guest-section-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 16rpx;
	}

	.booking-confirm-page .guest-section-title {
		display: block;
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 850;
		line-height: 1.3;
	}

	.booking-confirm-page .guest-section-subtitle {
		display: block;
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.35;
	}

	.booking-confirm-page .confirm-guest-link {
		flex: 0 0 auto;
		padding: 8rpx 12rpx;
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border-radius: 18rpx;
		font-size: 18rpx;
	}

	.booking-confirm-page .guest-row {
		align-items: center;
		gap: 14rpx;
		margin-top: 14rpx;
		padding: 16rpx 14rpx;
		background: #f5fbfb;
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 18rpx;
	}

	/* 默认没有任何入住人，这里给出一个可点击的空状态，避免出现红色报错。 */
	.booking-confirm-page .guest-empty {
		display: flex;
		align-items: center;
		flex-direction: column;
		margin-top: 16rpx;
		padding: 34rpx 20rpx 26rpx;
		background: #f5fbfb;
		border: 2rpx dashed rgba(42, 169, 169, 0.32);
		border-radius: 18rpx;
	}

	.booking-confirm-page .guest-empty-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 92rpx;
		height: 92rpx;
		color: var(--booking-green-deep);
		background: var(--booking-green-tint);
		border-radius: 50%;
	}

	.booking-confirm-page .guest-empty-title {
		margin-top: 14rpx;
		color: var(--proto-text);
		font-size: 24rpx;
		font-weight: 800;
	}

	.booking-confirm-page .guest-empty-desc {
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 19rpx;
		text-align: center;
	}

	.booking-confirm-page .guest-empty-action {
		width: 100%;
		height: 70rpx;
		margin: 18rpx 0 0;
		font-size: 21rpx;
	}

	.booking-confirm-page .guest-row > .guest-avatar {
		flex: 0 0 58rpx;
		width: 58rpx;
		height: 58rpx;
		color: #ffffff;
		background: var(--booking-green);
		border-radius: 18rpx;
		font-size: 24rpx;
		font-weight: 800;
	}

	.booking-confirm-page .guest-row > .guest-info {
		min-width: 0;
		flex: 1;
	}

	.booking-confirm-page .guest-name-row {
		min-width: 0;
	}

	.booking-confirm-page .guest-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.booking-confirm-page .guest-meta-line {
		display: flex;
		align-items: center;
		gap: 5rpx;
		margin-top: 6rpx;
		color: var(--proto-muted);
		font-size: 18rpx;
		line-height: 1.25;
	}

	.booking-confirm-page .guest-meta-line .proto-icon {
		flex: 0 0 auto;
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .guest-actions {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		gap: 8rpx;
	}

	.booking-confirm-page .guest-row > .guest-actions {
		flex: 0 0 auto;
	}

	.booking-confirm-page .guest-edit-button,
	.booking-confirm-page .guest-delete-button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 44rpx;
		height: 44rpx;
		flex: 0 0 44rpx;
		padding: 0;
		border: 1rpx solid transparent;
		border-radius: 50%;
	}

	.booking-confirm-page .guest-edit-button {
		background: var(--booking-green-tint);
		border-color: rgba(42, 169, 169, 0.08);
	}

	.booking-confirm-page .guest-edit-button .guest-edit {
		color: var(--booking-green-deep);
	}

	.booking-confirm-page .guest-delete-button {
		border-color: rgba(180, 35, 24, 0.12);
		background: #fff0ee;
	}

	/* 已选入住人后仍可继续多选：给一个轻量入口，不再用「人数不足」的红色报错。 */
	.booking-confirm-page .guest-add-more {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 6rpx;
		margin-top: 14rpx;
		padding: 16rpx;
		color: var(--booking-green-deep);
		background: #f5fbfb;
		border: 2rpx dashed rgba(42, 169, 169, 0.34);
		border-radius: 16rpx;
		font-size: 21rpx;
		font-weight: 700;
	}

	.booking-confirm-page .confirm-policy {
		margin-top: 16rpx;
	}

	.booking-confirm-page .policy-required {
		color: var(--proto-muted);
		font-size: 17rpx;
		font-weight: 400;
	}

	.booking-confirm-page .policy-summary-trigger {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 14rpx;
		padding: 14rpx 16rpx;
		color: var(--proto-muted);
		background: var(--booking-green-tint);
		border: 1rpx solid rgba(42, 169, 169, 0.1);
		border-radius: 14rpx;
		font-size: 19rpx;
		line-height: 1.4;
	}

	.booking-confirm-page .policy-summary-trigger > text {
		min-width: 0;
		flex: 1;
	}

	.booking-confirm-page .policy-summary-trigger .proto-link {
		display: flex;
		align-items: center;
		flex: 0 0 auto;
		color: var(--booking-green-deep);
		font-size: 18rpx;
		white-space: nowrap;
	}

	.booking-confirm-page .policy-summary-trigger .proto-icon {
		margin-left: 2rpx;
	}

	.booking-confirm-page .confirm-policy .proto-check {
		display: flex;
		align-items: flex-start;
		gap: 10rpx;
		min-height: 64rpx;
		margin: 14rpx 0 0;
		padding: 10rpx 12rpx;
		background: #f7fbfb;
		border: 1rpx solid rgba(42, 169, 169, 0.14);
		border-radius: 14rpx;
	}

	.booking-confirm-page .confirm-policy .proto-check > text {
		min-width: 0;
		flex: 1;
		color: #3f5d60;
		font-size: 20rpx;
		line-height: 1.5;
	}

	.booking-confirm-page .confirm-policy .proto-check-box {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 48rpx;
		height: 48rpx;
		flex: 0 0 48rpx;
		margin-top: 0;
		color: #ffffff;
		background: #ffffff;
		border: 4rpx solid var(--booking-green-deep);
		border-radius: 10rpx;
		box-shadow: 0 2rpx 5rpx rgba(0, 106, 106, 0.12);
	}

	.booking-confirm-page .confirm-policy .proto-check-box.checked {
		background: var(--booking-green);
		border-color: var(--booking-green);
		box-shadow: none;
	}

	.booking-confirm-page .agreement-check-hover {
		opacity: 0.78;
	}

	/* 未登录/登录过期时盖住订单页，避免跳转前出现整页白屏 */
	.auth-redirect-mask {
		position: fixed;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 999;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #ffffff;
	}

	.auth-redirect-card {
		display: flex;
		align-items: center;
		flex-direction: column;
		padding: 0 48rpx;
	}

	.auth-redirect-title {
		color: #1a1a1a;
		font-size: 32rpx;
		font-weight: 700;
	}

	.auth-redirect-desc {
		margin-top: 14rpx;
		color: #8a969c;
		font-size: 23rpx;
	}
</style>
