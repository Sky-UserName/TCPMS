// 登录跳转状态：跳转期间小程序会打断正在进行的 navigateTo，
// 引用方（goPage）据此判断失败是“被登录跳转打断”还是真正的跳转异常。
let loginRedirecting = false

export const isLoginRedirecting = () => loginRedirecting

export const setLoginRedirecting = (value) => {
	loginRedirecting = Boolean(value)
}
