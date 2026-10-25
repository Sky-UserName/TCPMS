// 演示数据开关。
// 开发环境默认允许用 demo 数据兜底，方便没有后端时看页面；
// 生产构建默认关闭，接口失败时展示空态/错误态，而不是假门店、假价格。
// 需要给人演示时，构建时加 VITE_ALLOW_DEMO=true。
const raw = import.meta.env?.VITE_ALLOW_DEMO

export const ALLOW_DEMO_DATA = String(
	raw === undefined ? (import.meta.env?.DEV ? 'true' : 'false') : raw
).toLowerCase() === 'true'
