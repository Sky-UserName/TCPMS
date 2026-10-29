import { verifyMerchantOrder } from './api.js'
import { goPage, showToast } from './prototype.js'

let scanning = false

// 二维码既可能是纯核销码，也可能是核销二维码图片的链接，这里统一取出最后的核销码。
export const normalizeVerifyCode = (raw) => {
	const text = String(raw || '').replace(/[\u200b-\u200d\ufeff]/g, '').trim()
	if (!text) return ''
	const matched = text.match(/verification-qr\/([^/?#\s]+)/i)
	if (matched) {
		try {
			return decodeURIComponent(matched[1])
		} catch (error) {
			return matched[1]
		}
	}
	return text.split(/\s+/)[0]
}

const isDevtools = () => {
	try {
		const info = uni.getSystemInfoSync()
		return String((info && info.platform) || '').toLowerCase() === 'devtools'
	} catch (error) {
		return false
	}
}

/**
 * 调起微信扫码。
 * 真机强制使用相机（onlyFromCamera），开发者工具没有摄像头，只能从本地选择二维码图片。
 * 返回 { code, reason }，reason 取值 ok / cancel / fail / unsupported。
 */
const scanQrCode = (onlyFromCamera) => new Promise((resolve) => {
	if (!uni.scanCode) {
		resolve({ code: '', reason: 'unsupported' })
		return
	}
	uni.scanCode({
		scanType: ['qrCode'],
		onlyFromCamera: Boolean(onlyFromCamera),
		success: (result) => resolve({ code: normalizeVerifyCode(result && result.result), reason: 'ok' }),
		fail: (error) => {
			const message = String((error && (error.errMsg || error.message)) || '')
			resolve({ code: '', reason: /cancel/i.test(message) ? 'cancel' : 'fail' })
		}
	})
})

// 手动输入核销码：扫码不可用（或光线不好扫不出来）时的兜底，前台可直接输入住客出示的数字码。
const inputVerifyCode = () => new Promise((resolve) => {
	uni.showModal({
		title: '输入核销码',
		editable: true,
		placeholderText: '请输入订单核销码',
		confirmText: '核销',
		cancelText: '取消',
		success: (result) => resolve(result && result.confirm ? normalizeVerifyCode(result.content) : ''),
		fail: () => resolve('')
	})
})

// 开发者工具无法调起相机，先让操作者明确选择「选二维码图片」还是「手动输入」，避免看起来像误点了上传图片。
const chooseScanMethod = () => new Promise((resolve) => {
	uni.showActionSheet({
		itemList: ['选择二维码图片', '手动输入核销码'],
		success: (result) => resolve(result && result.tapIndex === 1 ? 'manual' : 'scan'),
		fail: () => resolve('')
	})
})

const acquireVerifyCode = async () => {
	if (isDevtools()) {
		const method = await chooseScanMethod()
		if (!method) return ''
		if (method === 'manual') return inputVerifyCode()
		const scanned = await scanQrCode(false)
		if (scanned.code) return scanned.code
		return scanned.reason === 'cancel' ? '' : inputVerifyCode()
	}

	const scanned = await scanQrCode(true)
	if (scanned.code) return scanned.code
	// 用户主动取消扫码不提示，也不强行弹输入框。
	if (scanned.reason === 'cancel') return ''
	// 相机/扫码能力不可用时给一个手动输入的兜底，避免核销彻底做不了。
	return inputVerifyCode()
}

const showVerifyResult = (record) => {
	const lines = [
		`房间：${record.roomName || '—'}`,
		`民宿：${record.storeName || '—'}`,
		`订单号：${record.orderNumber || record.code || '—'}`,
		`核销时间：${record.time || '刚刚'}`
	]
	uni.showModal({
		title: '核销成功',
		content: lines.join('\n'),
		confirmText: '查看记录',
		cancelText: '关闭',
		success: (result) => {
			if (result.confirm) {
				goPage('/pages/merchant/records')
			}
		}
	})
}

const submitVerify = async (code) => {
	uni.showLoading({ title: '正在核销', mask: true })
	let record
	try {
		record = await verifyMerchantOrder(code)
	} finally {
		uni.hideLoading()
	}
	showVerifyResult(record || {})
	return record
}

/**
 * 唤起微信扫码（开发者工具下可选二维码图片或手动输入），识别订单核销码后调用服务端核销。
 * 返回核销记录；取消扫码或核销失败返回 null。
 */
export const scanAndVerifyOrder = async () => {
	if (scanning) return null
	scanning = true
	try {
		const code = await acquireVerifyCode()
		if (!code) return null
		return await submitVerify(code)
	} catch (error) {
		showToast(error?.message || '核销失败，请稍后重试')
		return null
	} finally {
		scanning = false
	}
}
