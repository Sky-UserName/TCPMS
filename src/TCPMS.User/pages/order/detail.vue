<template>
	<view class="proto-page order-detail-page">
		<ProtoHeader title="订单详情" title-align="center" theme="green" />

		<view class="status-card" :class="'is-' + statusClass">
			<view class="status-top">
				<view class="status-icon"><ProtoIcon :name="statusMeta.icon" :size="42" :tone="status === 'pay' || status === 'stay' ? 'white' : 'default'" /></view>
				<view class="status-copy">
					<text class="status-title">{{ statusMeta.title }}</text>
					<text class="status-desc">{{ statusMeta.desc }}</text>
				</view>
			</view>
			<view class="progress-line">
				<view class="progress-track"></view>
				<view class="progress-fill" :class="{ 'is-failed': status === 'refundFailed' }" :style="{ width: progressFillWidth }"></view>
				<view v-for="item in progress" :key="item.key" class="progress-item" :class="{ active: progressIndex >= item.index, failed: status === 'refundFailed' && item.key === 'refundResult' }">
					<view class="progress-dot">
						<ProtoIcon v-if="status === 'refundFailed' && item.key === 'refundResult'" name="close" tone="white" :size="22" />
						<ProtoIcon v-else-if="progressIndex >= item.index" name="check" tone="white" :size="22" />
						<text v-else>{{ item.index + 1 }}</text>
					</view>
					<text class="progress-label">{{ item.label }}</text>
				</view>
			</view>
		</view>

		<view class="detail-store proto-card">
			<view class="store-heading">
				<view class="store-mark"><ProtoIcon name="store" :size="32" /></view>
				<view class="store-copy">
					<text class="store-name">{{ storeName }}</text>
					<text v-if="storeStatusText" class="store-status">{{ storeStatusText }}</text>
				</view>
			</view>
			<view class="detail-address"><ProtoIcon name="location" :size="22" /><text>{{ storeAddress || '门店地址待补充' }}</text></view>
			<view class="detail-store-actions">
				<button class="store-action store-action-contact" hover-class="none" @tap="callStore">
					<ProtoIcon name="phone" :size="22" /><text>{{ storePhoneDisplay }}</text>
				</button>
				<button class="store-action store-action-map" hover-class="none" @tap="openLocation">
					<ProtoIcon name="navigation" tone="white" :size="22" /><text>去这里</text>
				</button>
			</view>
		</view>

		<view class="detail-room proto-card">
			<view class="room-heading">
				<image :src="roomImage" mode="aspectFill"></image>
				<view class="room-copy">
					<text class="room-name">{{ order.room }}</text>
					<text class="room-spec">{{ roomSpec }}</text>
				</view>
			</view>
			<view v-if="roomTags.length" class="room-tags"><text v-for="tag in roomTags" :key="tag">{{ tag }}</text></view>
			<view class="room-date-row">
				<view class="room-date-side"><text class="date-label">入住</text><text class="date-value">{{ checkInText }}</text><text class="date-note">{{ checkInTimeText }}</text></view>
				<text class="date-mid">共 {{ order.nights || 1 }} 晚</text>
				<view class="room-date-side room-date-side-right"><text class="date-label">离店</text><text class="date-value">{{ checkOutText }}</text><text class="date-note">{{ checkOutTimeText }}</text></view>
			</view>
		</view>

		<view class="detail-guests proto-card">
			<view class="card-heading"><text>实名入住信息</text><view class="card-heading-note"><ProtoIcon name="shield" :size="20" /><text>{{ guestCountText }}</text></view></view>
			<view v-if="!displayGuests.length" class="guest-empty">本单暂未登记入住人，可到店后由前台补录实名信息</view>
			<view v-for="(guest, index) in displayGuests" :key="index" class="info-row guest-row">
				<text class="info-label">{{ index === 0 ? '主要住客' : '同住客人' }}</text>
				<view class="identity-value">
					<text class="identity-name">{{ guest.name || '待补充' }}</text>
					<text class="identity-id">{{ guest.identityText }}</text>
				</view>
			</view>
			<view class="info-row"><text class="info-label">预留电话</text><text class="info-value">{{ guestPhoneDisplay }}</text></view>
		</view>

		<view class="detail-fee proto-card">
			<view class="card-heading"><text>支付与费用</text><text class="proto-pill" :class="payStateMeta.className">{{ payStateMeta.label }}</text></view>
			<view class="payment-code">
				<view class="payment-code-row"><text class="info-label">订单编号</text><text class="payment-value">{{ order.orderNumber || order.id }}</text></view>
				<view class="payment-code-row"><text class="info-label">下单时间</text><text class="payment-value">{{ createdAtText }}</text></view>
			</view>
			<view class="fee-row"><text>房费总计（{{ order.roomCount || 1 }}间 × {{ order.nights || 1 }}晚）</text><text>¥{{ formatYuan(order.price) }}</text></view>
			<view class="fee-total"><text>实付金额</text><text class="fee-amount">¥{{ formatYuan(order.price) }}</text></view>
		</view>

		<view v-if="isRefundFlow" class="detail-refund proto-card">
			<view class="card-heading">
				<view class="card-heading-title"><ProtoIcon name="refresh" :size="24" /><text>退款信息</text></view>
				<text class="proto-pill" :class="refundPillMeta.className">{{ refundPillMeta.label }}</text>
			</view>
			<view class="refund-amount-block">
				<text class="refund-amount-label">{{ status === 'refunded' ? '已退款金额' : '申请退款金额' }}</text>
				<text class="refund-amount-value">¥{{ formatYuan(refundAmount) }}</text>
			</view>
			<view class="info-row"><text class="info-label">退款单号</text><text class="info-value">{{ refundNumber || '系统生成中' }}</text></view>
			<view class="info-row"><text class="info-label">申请时间</text><text class="info-value">{{ refundAppliedAtText }}</text></view>
			<view v-if="refundCompletedAtText" class="info-row"><text class="info-label">退款完成时间</text><text class="info-value">{{ refundCompletedAtText }}</text></view>
			<view class="info-row"><text class="info-label">退回方式</text><text class="info-value">微信支付原路退回</text></view>
			<view v-if="refundReasonText" class="info-row"><text class="info-label">退款原因</text><text class="info-value">{{ refundReasonText }}</text></view>
			<view class="refund-note" :class="'is-' + statusClass">
				<ProtoIcon :name="refundNoteMeta.icon" :size="22" />
				<text>{{ refundNoteMeta.text }}</text>
			</view>
		</view>

		<view v-if="!isRefundFlow" class="detail-cancel proto-card">
			<view class="card-heading"><view class="card-heading-title"><ProtoIcon name="shield" :size="24" /><text>退订与取消保障</text></view></view>
			<view class="cancel-tip">
				<view class="cancel-tip-title"><ProtoIcon name="clock" :size="22" /><text>限时无损全额取消</text></view>
				<text>入住日前1天18:00前申请取消订单，享无手续费极速全额原路退款；若逾期申请取消，将依法扣除首晚房费。</text>
			</view>
		</view>

		<view class="proto-bottom-actions order-detail-actions">
			<button v-if="status === 'stay'" class="action-refund" hover-class="none" @tap="goRefund">
				<ProtoIcon name="refresh" :size="24" /><text>申请退款</text>
			</button>
			<button v-if="status === 'done' && !hasReview" class="action-primary" hover-class="none" @tap="goReview">
				<ProtoIcon name="star" tone="white" :size="24" /><text>评价本次入住</text>
			</button>
			<button v-else-if="status === 'done'" class="action-secondary is-solo" hover-class="none" @tap="goReviews">
				<ProtoIcon name="star" :size="24" /><text>查看我的评价</text>
			</button>
			<button v-else class="action-primary" hover-class="none" @tap="primaryAction">
				<ProtoIcon :name="primaryActionMeta.icon" tone="white" :size="24" /><text>{{ primaryActionMeta.label }}</text>
			</button>
		</view>

		<view v-if="verifyVisible" class="verify-mask" @tap="closeVerify">
			<view class="verify-sheet" @tap.stop>
				<view class="verify-head">
					<text class="verify-title">订单核销码</text>
					<view class="verify-close" @tap="closeVerify"><ProtoIcon name="close" :size="28" /></view>
				</view>
				<text class="verify-tip">到店后请向门店工作人员出示此二维码完成核销</text>
				<view class="verify-body">
					<image v-if="verificationQrUrl" class="verify-qr-image" :src="verificationQrUrl" mode="aspectFit" />
					<view v-else class="verify-qr"><ProtoIcon name="qr" :size="132" /></view>
					<view class="verify-groups">
						<text v-for="(group, index) in verifyCodeGroups" :key="index" class="verify-group">{{ group }}</text>
					</view>
				</view>
				<view class="verify-meta">
					<view class="verify-meta-row"><text class="verify-meta-label">门店</text><text class="verify-meta-value">{{ order.store }}</text></view>
					<view class="verify-meta-row"><text class="verify-meta-label">房型</text><text class="verify-meta-value">{{ order.room }}</text></view>
					<view class="verify-meta-row"><text class="verify-meta-label">入住</text><text class="verify-meta-value">{{ checkInText }}</text></view>
				</view>
				<button class="verify-button" hover-class="none" @tap="copyVerifyCode">复制核销码</button>
			</view>
		</view>
	</view>
</template>

<script>
	import ProtoHeader from '@/components/ProtoHeader.vue'
	import ProtoIcon from '@/components/ProtoIcon.vue'
	import { demoOrders, demoRooms, demoStores, goPage, goRoot, prototypeImages, showToast } from '@/common/prototype.js'
	import { findOrder, getOrders, hasOrderReview, resolveReviewTarget } from '@/common/app-store.js'
	import { fetchRemoteOrder, fetchStoreDetail, fetchOrderRefunds, getVerificationQrUrl } from '@/common/api.js'
	import { formatYuan } from '@/common/format.js'

	const isGuid = (value) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(String(value || ''))

	// 房型里配的时间可能已经是「当天14:00后」这种文案，只有纯时间才补「后/前」。
	const formatTimeNote = (value, suffix, fallback) => {
		const text = String(value || '').trim()
		if (!text) return fallback
		return /[后前]/.test(text) ? text : `${text} ${suffix}`
	}

	// 门店/房型详情优先取接口数据；接口不可用时用同名原型数据兜底，保证地址、电话、设施不会空着。
	const findLocalStore = (order = {}) => demoStores.find((item) =>
		(item.name && order.store && item.name === order.store)
		|| (item.name && order.store && (item.name.includes(order.store) || order.store.includes(item.name)))) || null

	const findLocalRoom = (order = {}) => demoRooms.find((item) =>
		String(item.id) === String(order.roomId || order.roomTypeId)
		|| (item.name && order.room && item.name === order.room)
		|| (item.name && order.room && (item.name.includes(order.room) || order.room.includes(item.name)))) || null

	export default {
		components: { ProtoHeader, ProtoIcon },
		data() {
			return {
				order: demoOrders[0],
				roomImage: prototypeImages.roomOne,
				storeInfo: null,
				roomInfo: null,
				refund: null,
				verifyVisible: false
			}
		},
		computed: {
			// 退款成功/退款失败要和「退款审核中」分开，否则已退款的订单会一直显示审核中。
			status() {
				const orderStatus = String(this.order.orderStatus || '')
				if (orderStatus === 'Refunded') return 'refunded'
				if (orderStatus === 'RefundFailed') return 'refundFailed'
				if (orderStatus === 'Refunding') return 'refund'
				const key = this.order.statusKey || 'stay'
				if (key !== 'refund') return key
				const text = String(this.order.status || '')
				if (text.includes('已退款') || text.includes('退款成功')) return 'refunded'
				if (text.includes('失败')) return 'refundFailed'
				return 'refund'
			},
			statusClass() {
				return { refund: 'refund', refunded: 'refunded', refundFailed: 'refund-failed' }[this.status] || this.status
			},
			isRefundFlow() {
				return ['refund', 'refunded', 'refundFailed'].includes(this.status)
			},
			statusMeta() {
				const metas = {
					stay: { label: '支付成功，待扫码核销', title: '支付成功·待核销', desc: '入住当天到店后，点击下方核销码向门店工作人员出示二维码。', icon: 'check-circle' },
					pay: { label: '订单已提交，待支付', title: '订单已提交', desc: '订单已提交，请在剩余时间内完成微信支付。', icon: 'wallet' },
					refund: { label: '退款中', title: '退款审核中', desc: '退款申请已提交，平台审核通过后将原路返还至您的支付账户。', icon: 'refresh' },
					refunded: { label: '已退款', title: '退款成功', desc: '退款已原路退回您的微信支付账户，请注意查收到账通知。', icon: 'check-circle' },
					refundFailed: { label: '退款失败', title: '退款未成功', desc: '本次退款未能完成，款项仍在平台账户，请联系客服协助处理。', icon: 'warning' },
					done: { label: '订单完成', title: '订单完成', desc: '核销已完成，感谢您的入住，期待下次继续为您服务。', icon: 'check-circle' },
					cancel: { label: '已取消', title: '订单已取消', desc: '这笔订单已取消，如需出行可以重新选择房型。', icon: 'close' }
				}
				return metas[this.status] || metas.stay
			},
			// 退款是独立流程，进度条要换成「退款申请→退款成功」，不能继续显示扫码核销。
			progress() {
				if (this.isRefundFlow) {
					return [
						{ key: 'created', label: '提交订单', index: 0 },
						{ key: 'paid', label: '支付成功', index: 1 },
						{ key: 'refundApply', label: '退款申请', index: 2 },
						{ key: 'refundResult', label: this.status === 'refundFailed' ? '退款失败' : '退款成功', index: 3 }
					]
				}
				return [
					{ key: 'created', label: '提交订单', index: 0 },
					{ key: 'paid', label: '支付成功', index: 1 },
					{ key: 'verified', label: '扫码核销', index: 2 },
					{ key: 'done', label: '订单完成', index: 3 }
				]
			},
			progressIndex() {
				const indexes = { pay: 0, stay: 1, refund: 2, refunded: 3, refundFailed: 3, done: 3, cancel: 0 }
				return indexes[this.status] || 0
			},
			hasReview() {
				return Boolean(this.order.hasReview || hasOrderReview(this.order.id))
			},
			progressFillWidth() {
				// 4 个节点各占 25%，索引 0 时线长为 0，索引 3 时正好连到最后一个圆心。
				return `${Math.max(0, Math.min(3, this.progressIndex)) * 25}%`
			},
			// 退款记录只在退款流程里查；演示/本地兜底数据没有接口，就用订单里缓存的退款字段。
			refundRecord() {
				return this.refund || {}
			},
			refundAmount() {
				const amount = Number(this.refundRecord.amount || 0)
				if (amount > 0) return amount
				return Number(this.order.refundAmount || this.order.price || 0)
			},
			refundNumber() {
				return this.refundRecord.refundNumber || this.order.refundNumber || ''
			},
			refundReasonText() {
				return this.refundRecord.reason || this.order.refundReason || ''
			},
			refundAppliedAtText() {
				return this.formatDateTime(this.refundRecord.createdAt || this.order.refundCreatedAt) || '—'
			},
			refundCompletedAtText() {
				return this.formatDateTime(this.refundRecord.completedAt || this.order.refundCompletedAt)
			},
			refundPillMeta() {
				if (this.status === 'refunded') return { label: '退款成功', className: 'success' }
				if (this.status === 'refundFailed') return { label: '退款失败', className: 'danger' }
				return { label: '审核中', className: 'info' }
			},
			refundNoteMeta() {
				if (this.status === 'refunded') {
					return {
						icon: 'check-circle',
						text: this.refundCompletedAtText
							? `退款已于 ${this.refundCompletedAtText} 处理完成并原路退回，微信到账可能有 1-3 个工作日延迟。`
							: '退款已原路退回微信支付账户，到账可能有 1-3 个工作日延迟。'
					}
				}
				if (this.status === 'refundFailed') {
					return {
						icon: 'warning',
						text: this.refundRecord.reviewComment || '退款未能完成，资金不会从平台扣除，请联系客服重新发起退款。'
					}
				}
				return { icon: 'clock', text: '申请已提交，平台审核通过后资金原路退回，通常 1-15 分钟内完成审核。' }
			},
			// 门店三件套：名称、地址、电话全部取门店档案，不再写死「TCPMS 精品直营门店」。
			storeName() {
				return (this.storeInfo && this.storeInfo.name) || this.order.store || '预订门店'
			},
			storeStatusText() {
				const status = (this.storeInfo && this.storeInfo.status) || ''
				// 只展示中文营业态，避免把 Inactive 之类的原始枚举值露出来。
				return status === '营业中' ? status : ''
			},
			storeAddress() {
				return (this.storeInfo && this.storeInfo.address) || this.order.address || ''
			},
			storePhone() {
				return (this.storeInfo && this.storeInfo.phone) || this.order.phone || ''
			},
			storePhoneDisplay() {
				return this.storePhone || '联系门店'
			},
			// 房型规格与标签取房型自己的设置（面积/床型/标签），不再套固定的「免费双早、高速Wi-Fi」。
			roomSpec() {
				const room = this.roomInfo || {}
				const parts = [
					room.type || room.roomCategory,
					room.layout,
					room.area,
					room.beds,
					room.capacity
				].filter(Boolean)
				return parts.length ? [...new Set(parts)].join(' · ') : '房型配置待补充'
			},
			roomTags() {
				const room = this.roomInfo || {}
				const tags = Array.isArray(room.tags) ? room.tags.filter(Boolean) : []
				if (tags.length) return tags.slice(0, 3)
				const facilities = Array.isArray(room.facilities) ? room.facilities.filter(Boolean) : []
				return facilities.slice(0, 3)
			},
			checkInTimeText() {
				return formatTimeNote(this.roomInfo && this.roomInfo.checkInTime, '后', '14:00 后')
			},
			checkOutTimeText() {
				return formatTimeNote(this.roomInfo && this.roomInfo.checkOutTime, '前', '12:00 前')
			},
			displayGuests() {
				const guests = Array.isArray(this.order.guests)
					? this.order.guests.filter((guest) => guest && (guest.name || guest.identity || guest.phone))
					: []
				return guests.map((guest) => ({
					name: guest.name || '',
					phone: guest.phone || '',
					identityText: guest.identity ? `${guest.identity}（居民身份证）` : '证件信息待补充'
				}))
			},
			guestCountText() {
				return this.displayGuests.length ? `共 ${this.displayGuests.length} 位入住人` : '待补全'
			},
			guestPhoneDisplay() {
				const guest = this.displayGuests.find((item) => item.phone)
				return guest ? guest.phone : '未填写'
			},
			// 后端时间统一是 UTC 时钟，旧数据没有 Z 标记，这里补上再转本地，避免下单时间差 8 小时。
			createdAtText() {
				return this.formatDateTime(this.order && this.order.createdAt) || '—'
			},
			payStateMeta() {
				if (this.status === 'pay') return { label: '待支付', className: 'warn' }
				if (this.status === 'cancel') return { label: '已取消', className: 'muted' }
				if (this.status === 'refund') return { label: '退款中', className: 'info' }
				if (this.status === 'refunded') return { label: '已退款', className: 'success' }
				if (this.status === 'refundFailed') return { label: '退款失败', className: 'danger' }
				return { label: '微信支付已完成', className: 'success' }
			},
			checkInText() {
				return this.order.checkIn || String(this.order.date || '').split(' - ')[0] || '待填写'
			},
			checkOutText() {
				return this.order.checkOut || String(this.order.date || '').split(' - ')[1] || '待填写'
			},
			verifyCode() {
				return String(this.order.verificationCode || '')
			},
			verificationQrUrl() {
				return getVerificationQrUrl(this.verifyCode)
			},
			verifyCodeGroups() {
				return this.verifyCode.replace(/\s/g, '').match(/.{1,4}/g) || []
			},
			primaryActionMeta() {
				return {
					pay: { label: '继续支付', icon: 'wallet' },
					stay: this.order.verificationCode
						? { label: '核销', icon: 'qr' }
						: { label: '联系门店', icon: 'phone' },
					refund: { label: '联系客服', icon: 'support' },
					refunded: { label: '再次预订', icon: 'house' },
					refundFailed: { label: '联系客服', icon: 'support' },
					cancel: { label: '重新预订', icon: 'house' }
				}[this.status] || { label: '核销码', icon: 'qr' }
			}
		},
		onLoad(options) {
			this.loadOrder(options && options.id)
		},
		onShow() {
			this.loadOrder(this.order.id)
		},
		methods: {
			formatYuan,
			formatDateTime(raw) {
				if (!raw) return ''
				let value = String(raw)
				if (/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}/.test(value) && !/(z|[+-]\d{2}:?\d{2})$/i.test(value)) {
					value = `${value.replace(' ', 'T')}Z`
				}
				const date = new Date(value)
				if (Number.isNaN(date.getTime())) return String(raw).slice(0, 16).replace('T', ' ')
				const pad = (num) => String(num).padStart(2, '0')
				return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
			},
			async loadOrder(id) {
				const order = id ? findOrder(id) : getOrders()[0]
				if (order) {
					this.order = order
					this.roomImage = order.image || prototypeImages.roomOne
				}
				if (id && isGuid(id)) {
					try {
						const remoteOrder = await fetchRemoteOrder(id, this.order)
						this.order = remoteOrder
						this.roomImage = remoteOrder.image || this.roomImage
					} catch (error) {
						// Keep the cached order detail available when the API is offline.
					}
				}
				await this.loadRefundRecord()
				this.loadStoreContext()
			},
			// 订单主表只有退款状态，金额/单号/到账时间在退款记录里，进页面补查一次。
			async loadRefundRecord() {
				this.refund = null
				const orderId = this.order && this.order.id
				if (!orderId || !this.isRefundFlow || !isGuid(orderId)) return
				try {
					const records = await fetchOrderRefunds(orderId)
					this.refund = records.length ? records[0] : null
				} catch (error) {
					// 退款接口不可用时用订单缓存里的退款字段兜底，状态卡仍按订单状态展示。
				}
			},
			// 订单只带门店/房型名称，地址、电话、坐标和房型标签要去门店详情补一次。
			async loadStoreContext() {
				const localStore = findLocalStore(this.order)
				const localRoom = findLocalRoom(this.order)
				this.storeInfo = localStore
				this.roomInfo = localRoom
				if (localRoom && localRoom.image) this.roomImage = localRoom.image

				const storeId = this.order.storeId
				if (!isGuid(storeId)) return
				try {
					const detail = await fetchStoreDetail(storeId)
					if (detail.store) this.storeInfo = detail.store
					const roomId = String(this.order.roomId || '')
					const matched = detail.rooms.find((room) => String(room.id) === roomId)
						|| detail.rooms.find((room) => room.name === this.order.room)
					if (matched) {
						this.roomInfo = matched
						if (matched.image) this.roomImage = matched.image
					}
				} catch (error) {
					// 门店接口不可用时保持原型兜底数据，页面仍可正常展示。
				}
			},
			goRefund() {
				if (this.status === 'pay' || this.status === 'cancel' || this.status === 'done') {
					showToast(this.status === 'pay' ? '请先完成支付后再申请退款' : '当前订单不可申请退款')
					return
				}
				goPage(`/pages/order/refund?id=${this.order.id}`)
			},
			primaryAction() {
				if (this.status === 'pay') {
					goPage(`/pages/pay/result?orderId=${this.order.id}`)
					return
				}
				if (this.status === 'stay') {
					if (this.order.orderStatus === 'CheckedIn') {
						goPage(`/pages/service/contact?orderId=${this.order.id}`)
					} else {
						this.openVerifyCode()
					}
					return
				}
				if (this.status === 'refund' || this.status === 'refundFailed') {
					goPage(`/pages/service/contact?orderId=${this.order.id}`)
					return
				}
				if (this.status === 'refunded' || this.status === 'cancel') {
					goRoot('/pages/index/index')
					return
				}
				this.openVerifyCode()
			},
			openVerifyCode() {
				if (!this.verifyCode) {
					showToast('订单支付后生成核销码，请刷新订单')
					return
				}
				this.verifyVisible = true
			},
			closeVerify() {
				this.verifyVisible = false
			},
			copyVerifyCode() {
				if (!this.verifyCode) {
					showToast('核销码暂不可用')
					return
				}
				uni.setClipboardData({
					data: this.verifyCode,
					success: () => showToast('核销码已复制')
				})
			},
			reviewQuery() {
				const target = resolveReviewTarget(this.order)
				return [
					`orderId=${encodeURIComponent(this.order.id || '')}`,
					`roomId=${encodeURIComponent(target.roomId || '')}`,
					`storeId=${encodeURIComponent(target.storeId || '')}`,
					`roomName=${encodeURIComponent(target.roomName || '')}`,
					`storeName=${encodeURIComponent(target.storeName || '')}`
				].join('&')
			},
			goReview() {
				goPage(`/pages/review/create?${this.reviewQuery()}`)
			},
			goReviews() {
				const target = resolveReviewTarget(this.order)
				goPage(`/pages/review/list?roomId=${encodeURIComponent(target.roomId || '')}&storeId=${encodeURIComponent(target.storeId || '')}&roomName=${encodeURIComponent(target.roomName || '')}&storeName=${encodeURIComponent(target.storeName || '')}`)
			},
			callStore() {
				if (!this.storePhone) {
					showToast('门店暂未预留联系电话')
					return
				}
				uni.makePhoneCall({
					phoneNumber: String(this.storePhone),
					fail: () => showToast('暂无法拨打门店电话')
				})
			},
			openLocation() {
				const latitude = Number(this.storeInfo && this.storeInfo.latitude)
				const longitude = Number(this.storeInfo && this.storeInfo.longitude)
				if (!Number.isFinite(latitude) || !Number.isFinite(longitude) || (!latitude && !longitude)) {
					showToast('门店暂未标注地图位置')
					return
				}
				uni.openLocation({
					latitude,
					longitude,
					name: this.storeName,
					address: this.storeAddress,
					scale: 18,
					fail: () => showToast('地图服务暂不可用，请复制地址查看')
				})
			}
		}
	}
</script>

<style lang="scss" scoped>
	.order-detail-page {
		padding-bottom: 200rpx;
		padding-bottom: calc(200rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(200rpx + env(safe-area-inset-bottom));
	}

	/* ---------- 状态卡 ---------- */
	.status-card {
		margin: 20rpx 24rpx 0;
		padding: 28rpx 26rpx 30rpx;
		background: linear-gradient(160deg, #e6fbfa 0%, #ffffff 70%);
		border: 1rpx solid rgba(42, 169, 169, 0.16);
		border-radius: 28rpx;
		box-shadow: 0 12rpx 30rpx rgba(42, 169, 169, 0.08);
	}

	.status-card.is-pay {
		background: linear-gradient(160deg, #fff3e2 0%, #ffffff 70%);
		border-color: rgba(245, 158, 11, 0.22);
	}

	.status-card.is-refund {
		background: linear-gradient(160deg, #eef4ff 0%, #ffffff 70%);
		border-color: rgba(59, 130, 246, 0.18);
	}

	/* 退款成功用主色收尾，退款失败用暖色提示，避免和审核中混在一起 */
	.status-card.is-refunded {
		background: linear-gradient(160deg, #e6fbfa 0%, #ffffff 70%);
		border-color: rgba(42, 169, 169, 0.18);
	}

	.status-card.is-refund-failed {
		background: linear-gradient(160deg, #fdeeea 0%, #ffffff 70%);
		border-color: rgba(194, 80, 58, 0.2);
	}

	.status-card.is-cancel {
		background: linear-gradient(160deg, #f2f4f5 0%, #ffffff 70%);
		border-color: rgba(111, 128, 133, 0.18);
	}

	.status-top {
		display: flex;
		align-items: flex-start;
		gap: 20rpx;
	}

	.status-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 88rpx;
		width: 88rpx;
		height: 88rpx;
		color: #006a6a;
		color: var(--proto-primary-dark, #006a6a);
		background: #d3f4f3;
		border-radius: 26rpx;
	}

	.status-card.is-pay .status-icon {
		background: #f59e0b;
		background: var(--proto-warning, #f59e0b);
	}

	.status-card.is-cancel .status-icon {
		background: #c9d3d6;
	}

	.status-card.is-refunded .status-icon {
		color: #006a6a;
		background: #d3f4f3;
	}

	.status-card.is-refund-failed .status-icon {
		color: #b4442f;
		background: #f7d9d2;
	}

	.status-copy {
		flex: 1;
		min-width: 0;
	}

	.status-title {
		display: block;
		color: var(--proto-text);
		font-size: 40rpx;
		font-weight: 850;
		line-height: 1.25;
	}

	.status-desc {
		display: block;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 25rpx;
		line-height: 1.55;
	}

	.progress-line {
		position: relative;
		display: flex;
		margin-top: 34rpx;
	}

	/* 连接线单独画一条轨道，避免 flex 容器里的伪元素在部分机型上不渲染 */
	/* 四个节点均分，圆心在 12.5% / 37.5% / 62.5% / 87.5%，线只连到首尾圆心 */
	.progress-track,
	.progress-fill {
		position: absolute;
		top: 23rpx;
		left: 12.5%;
		height: 2rpx;
		border-radius: 2rpx;
	}

	.progress-track {
		right: 12.5%;
		background: #e2eaec;
	}

	.progress-fill {
		width: 0;
		background: linear-gradient(90deg, #2aa9a9 0%, #6ed3cd 100%);
	}

	.progress-fill.is-failed {
		background: linear-gradient(90deg, #2aa9a9 0%, #c2503a 100%);
	}

	.progress-item {
		position: relative;
		z-index: 1;
		display: flex;
		align-items: center;
		flex: 1;
		flex-direction: column;
		color: #9aa8ac;
		font-size: 21rpx;
		text-align: center;
	}

	.progress-dot {
		position: relative;
		z-index: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 48rpx;
		height: 48rpx;
		color: #8f9ea2;
		background: #e9eff1;
		border-radius: 50%;
		font-size: 22rpx;
		font-weight: 700;
	}

	.progress-dot > text {
		color: inherit;
	}

	.progress-label {
		margin-top: 12rpx;
	}

	.progress-item.active {
		color: var(--proto-primary-dark);
		font-weight: 700;
	}

	.progress-item.active .progress-dot {
		color: #ffffff;
		background: #2aa9a9;
		background: var(--proto-primary, #2aa9a9);
		box-shadow: 0 0 0 4rpx rgba(42, 169, 169, 0.12);
	}

	.progress-item.failed {
		color: #b4442f;
	}

	.progress-item.failed .progress-dot {
		color: #ffffff;
		background: #c2503a;
		box-shadow: 0 0 0 4rpx rgba(194, 80, 58, 0.14);
	}

	/* ---------- 门店 ---------- */
	.detail-store {
		margin-top: 20rpx;
		padding: 26rpx;
	}

	.store-heading {
		display: flex;
		align-items: center;
		gap: 18rpx;
	}

	.store-mark {
		display: flex;
		align-items: center;
		justify-content: center;
		flex: 0 0 76rpx;
		width: 76rpx;
		height: 76rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 24rpx;
	}

	.store-copy {
		flex: 1;
		min-width: 0;
	}

	.store-name {
		display: block;
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 800;
		line-height: 1.3;
	}

	.store-status {
		display: inline-block;
		margin-top: 8rpx;
		padding: 2rpx 12rpx;
		color: var(--proto-primary-dark, #006a6a);
		background: var(--proto-surface-tint, #dff6f5);
		border-radius: 8rpx;
		font-size: 21rpx;
	}

	.detail-address {
		display: flex;
		align-items: flex-start;
		gap: 8rpx;
		margin-top: 20rpx;
		color: var(--proto-muted);
		font-size: 25rpx;
		line-height: 1.55;
	}

	.detail-address > text {
		flex: 1;
	}

	.detail-store-actions {
		display: flex;
		justify-content: flex-end;
		gap: 16rpx;
		margin-top: 24rpx;
	}

	.store-action {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 8rpx;
		height: 68rpx;
		padding: 0 26rpx;
		border-radius: 34rpx;
		font-size: 24rpx;
		font-weight: 700;
		line-height: 1.2;
		white-space: nowrap;
	}

	.store-action-contact {
		color: var(--proto-primary-dark, #006a6a);
		background: var(--proto-surface-tint, #dff6f5);
	}

	.store-action-map {
		color: #ffffff;
		background: #2aa9a9;
		background: var(--proto-primary, #2aa9a9);
		box-shadow: 0 8rpx 20rpx rgba(42, 169, 169, 0.24);
	}

	/* ---------- 房型 ---------- */
	.detail-room {
		margin-top: 20rpx;
		padding: 26rpx;
	}

	.room-heading {
		display: flex;
		align-items: center;
		gap: 20rpx;
	}

	.room-heading image {
		flex: 0 0 160rpx;
		width: 160rpx;
		height: 160rpx;
		border-radius: 18rpx;
	}

	.room-copy {
		flex: 1;
		min-width: 0;
	}

	.room-name {
		display: block;
		color: var(--proto-text);
		font-size: 32rpx;
		font-weight: 800;
		line-height: 1.35;
	}

	.room-spec {
		display: block;
		margin-top: 10rpx;
		color: var(--proto-muted);
		font-size: 24rpx;
		line-height: 1.5;
	}

	.room-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 18rpx;
	}

	.room-tags text {
		padding: 8rpx 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 10rpx;
		font-size: 21rpx;
	}

	.room-date-row {
		display: flex;
		align-items: center;
		gap: 14rpx;
		margin-top: 22rpx;
		padding: 22rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.room-date-side {
		display: flex;
		flex: 1;
		flex-direction: column;
	}

	.room-date-side-right {
		align-items: flex-end;
		text-align: right;
	}

	.date-label {
		color: var(--proto-muted);
		font-size: 22rpx;
	}

	.date-value {
		margin-top: 6rpx;
		color: var(--proto-text);
		font-size: 32rpx;
		font-weight: 800;
	}

	.date-note {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 21rpx;
	}

	.date-mid {
		padding: 8rpx 14rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
		border-radius: 16rpx;
		font-size: 21rpx;
	}

	.guest-empty {
		margin-top: 20rpx;
		padding: 20rpx;
		color: var(--proto-muted, #6f8085);
		background: var(--proto-surface-low, #eef4f5);
		border-radius: 16rpx;
		font-size: 24rpx;
		line-height: 1.55;
	}

	/* ---------- 通用信息行 ---------- */
	.card-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12rpx;
		color: var(--proto-text);
		font-size: 30rpx;
		font-weight: 800;
	}

	.card-heading-title,
	.card-heading-note {
		display: flex;
		align-items: center;
		gap: 8rpx;
	}

	.card-heading-note {
		color: var(--proto-muted);
		font-size: 21rpx;
		font-weight: 500;
	}

	.detail-guests,
	.detail-fee,
	.detail-cancel {
		margin-top: 20rpx;
		padding: 28rpx 26rpx;
	}

	.info-row,
	.fee-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 20rpx;
		margin-top: 22rpx;
		font-size: 26rpx;
		line-height: 1.45;
	}

	.info-label {
		flex: 0 0 auto;
		color: var(--proto-muted);
	}

	.info-value {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		text-align: right;
		word-break: break-all;
	}

	.identity-value {
		display: flex;
		align-items: flex-end;
		flex: 1;
		flex-direction: column;
		min-width: 0;
		text-align: right;
	}

	.identity-name {
		color: var(--proto-text);
		font-size: 27rpx;
		font-weight: 700;
	}

	.identity-id {
		margin-top: 4rpx;
		color: var(--proto-muted);
		font-size: 22rpx;
	}

	.detail-fee .proto-pill {
		min-height: 40rpx;
		padding: 0 14rpx;
		font-size: 20rpx;
	}

	.payment-code {
		display: flex;
		flex-direction: column;
		gap: 16rpx;
		margin-top: 22rpx;
		padding: 22rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.payment-code-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 20rpx;
		font-size: 24rpx;
	}

	.payment-value {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		text-align: right;
		word-break: break-all;
	}

	.fee-row {
		color: var(--proto-muted);
	}

	.fee-row > text:last-child {
		color: var(--proto-text);
		text-align: right;
	}

	.fee-total {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		margin-top: 24rpx;
		padding-top: 24rpx;
		border-top: 1rpx solid #e6edef;
		color: var(--proto-text);
		font-size: 28rpx;
		font-weight: 700;
	}

	.fee-amount {
		color: var(--proto-primary-dark);
		font-size: 44rpx;
		font-weight: 900;
	}

	/* ---------- 退订 ---------- */
	.cancel-tip {
		margin-top: 20rpx;
		padding: 20rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.cancel-tip text {
		display: block;
		color: var(--proto-muted);
		font-size: 24rpx;
		line-height: 1.6;
	}

	.cancel-tip-title {
		display: flex;
		align-items: center;
		gap: 8rpx;
		margin-bottom: 10rpx;
		color: var(--proto-primary-dark);
		font-size: 25rpx;
		font-weight: 750;
	}

	/* ---------- 退款信息 ---------- */
	.detail-refund {
		margin-top: 20rpx;
		padding: 28rpx 26rpx;
	}

	.refund-amount-block {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 20rpx;
		margin-top: 22rpx;
		padding: 22rpx;
		background: var(--proto-surface-low);
		border-radius: 18rpx;
	}

	.refund-amount-label {
		color: var(--proto-muted);
		font-size: 24rpx;
	}

	.refund-amount-value {
		color: var(--proto-primary-dark);
		font-size: 44rpx;
		font-weight: 900;
		line-height: 1.1;
	}

	.refund-note {
		display: flex;
		align-items: flex-start;
		gap: 10rpx;
		margin-top: 22rpx;
		padding: 18rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 16rpx;
		font-size: 23rpx;
		line-height: 1.55;
	}

	.refund-note > text {
		flex: 1;
		min-width: 0;
	}

	.refund-note.is-refunded {
		color: var(--proto-primary-dark);
		background: var(--proto-surface-tint);
	}

	.refund-note.is-refund-failed {
		color: #b4442f;
		background: #fdeeea;
	}

	/* ---------- 底部操作栏 ---------- */
	.order-detail-actions {
		gap: 18rpx;
		padding: 16rpx 24rpx;
		padding-bottom: calc(16rpx + constant(safe-area-inset-bottom));
		padding-bottom: calc(16rpx + env(safe-area-inset-bottom));
		background: rgba(255, 255, 255, 0.98);
		border-top: 1rpx solid rgba(197, 199, 202, 0.28);
		box-shadow: 0 -10rpx 30rpx rgba(32, 37, 43, 0.06);
	}

	.order-detail-actions .action-refund {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8rpx;
		flex: 0 0 auto;
		min-width: 208rpx;
		height: 88rpx;
		padding: 0 26rpx;
		color: #c2503a;
		background: linear-gradient(135deg, #fff4ef 0%, #ffe5db 100%);
		border: 1rpx solid rgba(194, 80, 58, 0.26);
		border-radius: 44rpx;
		box-shadow: 0 8rpx 20rpx rgba(194, 80, 58, 0.12);
		font-size: 27rpx;
		font-weight: 700;
	}

	.order-detail-actions .action-secondary {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8rpx;
		flex: 0 0 auto;
		min-width: 200rpx;
		height: 88rpx;
		padding: 0 28rpx;
		color: var(--proto-primary-dark);
		background: var(--proto-surface);
		border: 1rpx solid var(--proto-primary);
		border-radius: 44rpx;
		font-size: 27rpx;
		font-weight: 700;
	}

	.order-detail-actions .action-secondary.is-solo {
		flex: 1;
	}

	.order-detail-actions .action-primary {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 10rpx;
		flex: 1;
		height: 88rpx;
		color: #ffffff;
		background: #2aa9a9;
		background: var(--proto-primary, #2aa9a9);
		border-radius: 44rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.24);
		font-size: 29rpx;
		font-weight: 800;
	}

	/* ---------- 核销码弹层 ---------- */
	.verify-mask {
		position: fixed;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		z-index: 90;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 0 48rpx;
		background: rgba(18, 28, 30, 0.55);
	}

	.verify-sheet {
		width: 100%;
		padding: 32rpx 30rpx 34rpx;
		background: var(--proto-surface);
		border-radius: 32rpx;
		box-shadow: 0 24rpx 60rpx rgba(0, 0, 0, 0.18);
	}

	.verify-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.verify-title {
		color: var(--proto-text);
		font-size: 34rpx;
		font-weight: 850;
	}

	.verify-close {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 56rpx;
		height: 56rpx;
		color: var(--proto-muted);
		background: var(--proto-surface-low);
		border-radius: 50%;
	}

	.verify-tip {
		display: block;
		margin-top: 12rpx;
		color: var(--proto-muted);
		font-size: 24rpx;
		line-height: 1.55;
	}

	.verify-body {
		display: flex;
		align-items: center;
		flex-direction: column;
		margin-top: 26rpx;
		padding: 30rpx 20rpx;
		background: var(--proto-surface-low);
		border-radius: 24rpx;
	}

	.verify-qr {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 200rpx;
		height: 200rpx;
		background: #ffffff;
		border-radius: 20rpx;
		box-shadow: 0 10rpx 24rpx rgba(42, 169, 169, 0.1);
	}

	.verify-qr-image {
		width: 360rpx;
		height: 360rpx;
		max-width: 100%;
		flex: 0 0 auto;
	}

	.verify-groups {
		display: flex;
		justify-content: center;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 26rpx;
	}

	.verify-group {
		color: var(--proto-text);
		font-family: "DIN Alternate", Menlo, Consolas, monospace;
		font-size: 30rpx;
		font-weight: 800;
		letter-spacing: 2rpx;
	}

	.verify-meta {
		margin-top: 22rpx;
	}

	.verify-meta-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 20rpx;
		margin-top: 14rpx;
		font-size: 25rpx;
	}

	.verify-meta-label {
		flex: 0 0 auto;
		color: var(--proto-muted);
	}

	.verify-meta-value {
		flex: 1;
		min-width: 0;
		color: var(--proto-text);
		text-align: right;
		word-break: break-all;
	}

	.verify-button {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 88rpx;
		margin-top: 30rpx;
		color: #ffffff;
		background: #2aa9a9;
		background: var(--proto-primary, #2aa9a9);
		border-radius: 44rpx;
		font-size: 29rpx;
		font-weight: 800;
	}
</style>
