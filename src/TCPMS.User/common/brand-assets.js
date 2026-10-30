// 品牌 logo 与必要的品牌图片统一由后端托管（GET /brand/...）。
// 小程序包里只保留一份同名的本地副本作为兜底：即使重新打包时产物漏拷或服务器临时不可用，
// 页面也不会出现空白图；打包体积也不会再被这些大图拖大。
import { API_BASE_URL_OVERRIDE, DEFAULT_API_BASE_URL } from './env.js'

const readStoredBaseUrl = () => {
	try {
		return uni.getStorageSync('tcpms.apiBaseUrl') || ''
	} catch (error) {
		return ''
	}
}

// 后端根地址：去掉 /api/v1 后缀，图片由站点根路径直接返回。
export const getServerOrigin = () => {
	const base = API_BASE_URL_OVERRIDE || readStoredBaseUrl() || DEFAULT_API_BASE_URL
	return String(base || '').replace(/\/api\/v\d+\/?$/i, '').replace(/\/+$/, '')
}

export const brandAsset = (file) => `${getServerOrigin()}/brand/${String(file || '').replace(/^\/+/, '')}`

// 线上（后端托管）地址
export const brandImages = {
	mark: brandAsset('brand-mark.jpg'),
	logo: brandAsset('brand-logo.jpg'),
	hero: brandAsset('prototype/home-hero.jpg'),
	storeCity: brandAsset('prototype/store-city.jpg'),
	hostel: brandAsset('prototype/store-hostel.jpg')
}

// 包内兜底地址：<image> 加载失败时切到本地文件。
export const brandImageFallbacks = {
	mark: '/static/brand-mark.jpg',
	logo: '/static/brand-logo.jpg',
	hero: '/static/prototype/home-hero.jpg',
	storeCity: '/static/prototype/store-city.jpg',
	hostel: '/static/prototype/store-hostel.jpg'
}
