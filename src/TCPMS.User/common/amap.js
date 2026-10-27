import { fetchMapConfig } from './api.js'

// 高德在微信小程序里只提供数据能力（逆地理编码、POI 检索、路径规划等），
// 底图由微信原生 map 组件渲染（腾讯瓦片）。这里封装小程序端 Key 的 REST 调用。
const AMAP_BASE_URL = 'https://restapi.amap.com'
const REQUEST_TIMEOUT = 6000

// 小程序类型的 Key 只接受带微信小程序标识的请求，缺少这些参数会返回
// 10009 USERKEY_PLAT_NOMATCH。取值与官方 amap-wx.js 的 requestConfig 一致。
const MINI_PROGRAM_IDENTITY = {
	s: 'rsx',
	platform: 'WXJS',
	sdkversion: '1.2.0',
	logversion: '2.0'
}

let configTask = null

const createAmapError = (info) => {
	const error = new Error(info || '高德接口调用失败')
	error.code = 'amap_error'
	return error
}

// 小程序端 Key 由后端 /map/config 下发，失败时降级为未启用，不影响地图浏览。
export const loadAmapConfig = () => {
	if (!configTask) {
		configTask = fetchMapConfig().catch(() => ({
			amapEnabled: false,
			miniProgramKey: '',
			defaultRadiusKm: 50
		}))
	}
	return configTask
}

const amapGet = (path, params, key) => new Promise((resolve, reject) => {
	uni.request({
		url: `${AMAP_BASE_URL}${path}`,
		method: 'GET',
		data: { key, appname: key, ...MINI_PROGRAM_IDENTITY, output: 'json', ...params },
		timeout: REQUEST_TIMEOUT,
		success: (response) => {
			const body = response.data || {}
			if (body.status === '1') {
				resolve(body)
				return
			}
			const error = createAmapError(body.info)
			error.infocode = body.infocode || ''
			reject(error)
		},
		fail: (error) => {
			const failure = createAmapError((error && error.errMsg) || '高德接口请求失败')
			failure.code = 'amap_network_error'
			reject(failure)
		}
	})
})

const readText = (value) => {
	if (Array.isArray(value)) return value.length ? String(value[0]) : ''
	if (value === undefined || value === null) return ''
	return String(value)
}

const parseLocation = (location) => {
	const parts = readText(location).split(',')
	if (parts.length !== 2) return null
	const longitude = Number(parts[0])
	const latitude = Number(parts[1])
	if (!Number.isFinite(longitude) || !Number.isFinite(latitude)) return null
	return { latitude, longitude }
}

export const isAmapReady = async () => {
	const config = await loadAmapConfig()
	return Boolean(config.amapEnabled && config.miniProgramKey)
}

// 把定位坐标换成「河南省郑州市金水区xx路」这类可读地址。
export const reverseGeocode = async (latitude, longitude) => {
	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')

	const body = await amapGet('/v3/geocode/regeo', {
		location: `${Number(longitude).toFixed(6)},${Number(latitude).toFixed(6)}`,
		extensions: 'base'
	}, config.miniProgramKey)

	const regeocode = body.regeocode || {}
	const component = regeocode.addressComponent || {}
	return {
		formattedAddress: readText(regeocode.formatted_address),
		province: readText(component.province),
		city: readText(component.city) || readText(component.province),
		district: readText(component.district),
		adcode: readText(component.adcode),
		citycode: readText(component.citycode)
	}
}

// 地址转坐标：后端门店可能没存经纬度，用地址兜底解析出落点。
export const geocodeAddress = async (address, city = '') => {
	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')
	const keyword = String(address || '').trim()
	if (!keyword) throw createAmapError('门店暂未配置地址')

	const params = { address: keyword }
	if (city) params.city = city

	const body = await amapGet('/v3/geocode/geo', params, config.miniProgramKey)
	const geocodes = Array.isArray(body.geocodes) ? body.geocodes : []
	const point = parseLocation(geocodes[0] && geocodes[0].location)
	if (!point) throw createAmapError('未解析到地址坐标')
	return { ...point, formattedAddress: readText(geocodes[0].formatted_address) }
}

// 周边 POI：餐饮、购物、风景、地铁、公交、医疗，用来自民宿详情页的周边地图。
const NEARBY_TYPES = '050000|060000|110000|150500|150700|090000'
const MAX_PER_TYPE = 2

const readTypeLabel = (type) => {
	const segments = readText(type).split(';').filter(Boolean)
	if (!segments.length) return '周边'
	return segments[1] || segments[0]
}

// 按距离排序时同一类 POI 会扎堆（一条街全是咖啡厅），限制每类数量让列表更有信息量。
const diversify = (places, limit) => {
	const counters = {}
	const result = []
	places.forEach((place) => {
		const used = counters[place.typeLabel] || 0
		if (used >= MAX_PER_TYPE) return
		counters[place.typeLabel] = used + 1
		result.push(place)
	})
	return result.slice(0, limit)
}

export const searchNearby = async (latitude, longitude, options = {}) => {
	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')
	const radius = Number(options.radius) > 0 ? Number(options.radius) : 1500
	const limit = Number(options.limit) > 0 ? Number(options.limit) : 8

	const body = await amapGet('/v3/place/around', {
		location: `${Number(longitude).toFixed(6)},${Number(latitude).toFixed(6)}`,
		types: NEARBY_TYPES,
		radius,
		offset: 20,
		page: 1,
		sortrule: 'distance',
		extensions: 'base'
	}, config.miniProgramKey)

	const pois = Array.isArray(body.pois) ? body.pois : []
	const places = pois
		.map((poi) => {
			const point = parseLocation(poi.location)
			if (!point) return null
			return {
				id: readText(poi.id) || `${readText(poi.name)}-${readText(poi.location)}`,
				name: readText(poi.name),
				address: readText(poi.address),
				typeLabel: readTypeLabel(poi.type),
				distance: Number(poi.distance) || 0,
				latitude: point.latitude,
				longitude: point.longitude
			}
		})
		.filter((poi) => poi && poi.name)
	return diversify(places, limit)
}

// 高德 POI 统一成前端使用的结构：省市区拆开，地址只保留街道门牌，方便拼装。
const mapPoi = (poi) => {
	const point = parseLocation(poi.location)
	if (!point) return null
	const name = readText(poi.name)
	if (!name) return null
	return {
		id: readText(poi.id) || `${name}-${readText(poi.location)}`,
		name,
		address: readText(poi.address),
		province: readText(poi.pname),
		city: readText(poi.cityname),
		district: readText(poi.adname),
		distance: Number(poi.distance) || 0,
		latitude: point.latitude,
		longitude: point.longitude
	}
}

// 关键字检索地点，用于地图页顶部的区域搜索。
export const searchPlaces = async (keyword, city = '') => {
	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')

	const params = {
		keywords: keyword,
		offset: 20,
		page: 1,
		extensions: 'base'
	}
	if (city) {
		params.city = city
		params.citylimit = 'true'
	}

	const body = await amapGet('/v3/place/text', params, config.miniProgramKey)
	const pois = Array.isArray(body.pois) ? body.pois : []
	return pois.map(mapPoi).filter(Boolean)
}

// 地图选点页的周边地点：不按类型过滤，小区、写字楼、学校都要出现在列表里，
// 所以和详情页的 searchNearby 分开，按距离排序取前 N 条。
export const searchAroundPois = async (latitude, longitude, options = {}) => {
	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')
	const radius = Number(options.radius) > 0 ? Number(options.radius) : 1000
	const limit = Number(options.limit) > 0 ? Math.min(25, Number(options.limit)) : 20

	const body = await amapGet('/v3/place/around', {
		location: `${Number(longitude).toFixed(6)},${Number(latitude).toFixed(6)}`,
		radius,
		offset: limit,
		page: 1,
		sortrule: 'distance',
		extensions: 'base'
	}, config.miniProgramKey)

	const pois = Array.isArray(body.pois) ? body.pois : []
	return pois.map(mapPoi).filter(Boolean).slice(0, limit)
}

// 城市 → 区县。搜索结果页的「区域选择」用这份数据，
// 不依赖后端门店是否覆盖该城市，所以定位到郑州也能列出郑州的区县。
// 带版本号，取数逻辑变化时直接换 key，避免读到旧缓存（如直辖市只缓存了「上海城区」）。
const DISTRICT_CACHE_PREFIX = 'tcpms:districts:v2:'
const DISTRICT_CACHE_TTL = 24 * 60 * 60 * 1000

const readDistrictCache = (city) => {
	try {
		const raw = uni.getStorageSync(`${DISTRICT_CACHE_PREFIX}${city}`)
		if (!raw || typeof raw !== 'object') return null
		if (!Array.isArray(raw.items) || !raw.items.length) return null
		if (Date.now() - Number(raw.savedAt || 0) > DISTRICT_CACHE_TTL) return null
		return raw.items
	} catch (error) {
		return null
	}
}

const writeDistrictCache = (city, items) => {
	try {
		uni.setStorageSync(`${DISTRICT_CACHE_PREFIX}${city}`, { items, savedAt: Date.now() })
	} catch (error) {
		// 存储写失败不影响展示，忽略即可。
	}
}

export const fetchDistricts = async (city) => {
	const keyword = String(city || '').trim()
	if (!keyword) return []

	const cached = readDistrictCache(keyword)
	if (cached) return cached

	const config = await loadAmapConfig()
	if (!config.miniProgramKey) throw createAmapError('高德小程序 Key 未配置')

	const body = await amapGet('/v3/config/district', {
		keywords: keyword,
		// 取两级：普通地级市第一层就是区县；直辖市第一层只有「上海城区」这类聚合节点，
		// 需要靠第二层才能拿到真正的区（浦东新区、黄浦区等）。
		subdistrict: 2,
		extensions: 'base'
	}, config.miniProgramKey)

	const root = Array.isArray(body.districts) ? body.districts[0] : null
	let children = root && Array.isArray(root.districts) ? root.districts : []
	if (children.length === 1 && children[0] && children[0].level !== 'district') {
		children = Array.isArray(children[0].districts) ? children[0].districts : []
	}

	const items = children
		// 高德的「街道」级别不放进区域选择，避免出现几百个街道。
		.filter((item) => item && readText(item.level) !== 'street')
		// 高德返回顺序不固定，按行政区划代码升序排，主城区会排在县市前面。
		.sort((left, right) => readText(left.adcode).localeCompare(readText(right.adcode)))
		.map((item) => readText(item.name))
		.filter((name) => name)

	if (items.length) writeDistrictCache(keyword, items)
	return items
}
