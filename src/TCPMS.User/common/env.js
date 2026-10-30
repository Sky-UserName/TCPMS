// 构建环境判断。
// 不同编译方式（HBuilderX / vite）对 import.meta.env 的注入不完全一致，
// 这里依次退让：PROD -> MODE -> NODE_ENV，全都认不出来时按开发环境处理，保证本地联调可用。
const buildEnv = import.meta.env || {}

const detectProductionBuild = () => {
	if (typeof buildEnv.PROD === 'boolean') return buildEnv.PROD
	if (typeof buildEnv.MODE === 'string') return buildEnv.MODE === 'production'
	if (typeof process !== 'undefined' && process.env && process.env.NODE_ENV) {
		return process.env.NODE_ENV === 'production'
	}
	return false
}

export const IS_DEV_BUILD = !detectProductionBuild()

// 接口地址覆盖：发行包用 .env.production 或 VITE_API_BASE_URL 指定线上域名。
export const API_BASE_URL_OVERRIDE = buildEnv.VITE_API_BASE_URL || ''

// 接口地址默认值：开发连本机后端，发行包连部署服务器（真机访问不到 localhost）。
export const DEFAULT_API_BASE_URL = IS_DEV_BUILD
	? 'http://localhost:5181/api/v1'
	: 'http://39.96.200.62/api/v1'

const readFlag = (value, fallback) => {
	if (value === undefined || value === null || value === '') return fallback
	return String(value).toLowerCase() === 'true'
}

// 演示数据开关：开发环境默认允许 demo 兜底，生产默认关闭。
export const ALLOW_DEMO_DATA = readFlag(buildEnv.VITE_ALLOW_DEMO, IS_DEV_BUILD)

// 模拟支付开关：只在本机联调时把订单标记为已支付，生产包恒为关闭。
export const ALLOW_MOCK_PAY = readFlag(buildEnv.VITE_ALLOW_MOCK_PAY, IS_DEV_BUILD)

// 开发态登录兜底：开发者工具里 code 换不到 openId 时，用固定测试账号登录。
export const ALLOW_DEV_LOGIN = IS_DEV_BUILD
