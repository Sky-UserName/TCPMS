/**
 * tab 页面之间无法通过 URL 传参（uni.switchTab 不支持 query），
 * 这里用一次性的内存中转：调用方 setTabParams 写入意图，目标页 onShow 时 takeTabParams 取走。
 * 读取后立即清空，避免下次进入时误触发上一轮的筛选。
 */
let pending = null

export const setTabParams = (params) => {
	pending = params && typeof params === 'object' ? { ...params } : null
}

export const takeTabParams = () => {
	const value = pending
	pending = null
	return value
}
