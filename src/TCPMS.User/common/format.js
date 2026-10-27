// 金额展示统一走这里。
// 后端一律以「分」为单位返回，页面里 mapOrder 已经把分转成了元并去掉了末尾的 .00，
// 之前各处再手工拼 ".00" 会导致 105.6 元显示成 「105.6.00」，所以统一用这个函数格式化。

export const formatYuan = (value) => {
	const amount = Number(value)
	if (!Number.isFinite(amount)) {
		return '0.00'
	}
	return amount.toFixed(2)
}
