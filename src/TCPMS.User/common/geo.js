// 经纬度距离计算与展示格式化。
// 小程序定位（uni.getLocation / wx.getLocation，gcj02）拿到的是用户坐标，
// 门店坐标来自高德（同样是 gcj02），两边直接算球面距离即可，无需再转换坐标系。

const EARTH_RADIUS_METERS = 6371000

const toPoint = (value) => {
	if (!value || typeof value !== 'object') return null
	const { latitude, longitude } = value
	// 后端门店未设置地图坐标时返回 null，Number(null) 会变成 0（几内亚湾），必须单独挡掉。
	if (latitude === null || latitude === undefined || latitude === '') return null
	if (longitude === null || longitude === undefined || longitude === '') return null
	const lat = Number(latitude)
	const lng = Number(longitude)
	if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null
	if (Math.abs(lat) > 90 || Math.abs(lng) > 180) return null
	return { latitude: lat, longitude: lng }
}

// 两点直线（球面）距离，单位：米。坐标缺失或非法时返回 0；
// 需要区分「同一位置」和「缺坐标」时用 distanceKmBetween（缺坐标返回 null）。
export const distanceMeters = (from, to) => {
	const start = toPoint(from)
	const end = toPoint(to)
	if (!start || !end) return 0
	const rad = Math.PI / 180
	const latDelta = (end.latitude - start.latitude) * rad
	const lngDelta = (end.longitude - start.longitude) * rad
	const a = Math.sin(latDelta / 2) ** 2
		+ Math.cos(start.latitude * rad) * Math.cos(end.latitude * rad) * Math.sin(lngDelta / 2) ** 2
	return Math.round(EARTH_RADIUS_METERS * 2 * Math.asin(Math.min(1, Math.sqrt(a))))
}

// 米制展示：<50m 给「<50m」，1km 内取整到 10 米，1km 以上保留一位小数，10km 以上取整公里。
export const formatDistance = (meters) => {
	const value = Number(meters)
	if (!Number.isFinite(value) || value < 0) return ''
	if (value < 50) return '<50m'
	// 995m 以上按公里展示，否则四舍五入到 10 米会得出「1000m」这种别扭的文案。
	if (value < 995) return `${Math.round(value / 10) * 10}m`
	if (value < 10000) return `${(value / 1000).toFixed(1)}km`
	return `${Math.round(value / 1000)}km`
}

// 公里制展示（后端 distanceKm 与门店距离都用这个），内部复用米制规则。
export const formatDistanceKm = (km) => {
	const value = Number(km)
	if (!Number.isFinite(value) || value < 0) return ''
	return formatDistance(value * 1000)
}

// 计算「门店/房型距用户」的公里数；缺坐标返回 null，前端据此决定是否展示距离。
export const distanceKmBetween = (from, to) => {
	if (!toPoint(from) || !toPoint(to)) return null
	return distanceMeters(from, to) / 1000
}
