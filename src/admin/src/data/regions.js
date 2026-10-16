import areaData from 'china-area-data'
import { pinyin } from 'pinyin-pro'

const AREA_DATA = areaData?.default || areaData
const ROOT_CODE = '86'
const EXCLUDED_ROOT_CODES = new Set(['710000', '810000', '820000'])

const LEVEL_LABELS = {
  Province: '省',
  City: '市',
  District: '区'
}

function normalizeCode(value) {
  return String(value ?? '')
}

function shortName(name, level) {
  const value = String(name || '')
  if (level === 'Province') {
    return value
      .replace(/(壮族|回族|维吾尔|蒙古)自治区$/, '')
      .replace(/自治区$/, '')
      .replace(/省$/, '')
      .replace(/市$/, '')
  }
  if (level === 'City') {
    return value.replace(/市$/, '').replace(/自治州$/, '').replace(/地区$/, '')
  }
  return value.replace(/(自治州|地区)$/, '')
}

function initialOf(name) {
  const value = pinyin(String(name || ''), {
    pattern: 'first',
    toneType: 'none',
    type: 'array'
  })
  return String(value?.[0] || '').toUpperCase()
}

function makeNode(code, name, level, parentCode = null, parentName = null, order = 0) {
  const normalizedCode = normalizeCode(code)
  const normalizedName = String(name || '')
  const displayName = level === 'City' && normalizedName === '市辖区'
    ? String(parentName || normalizedName)
    : normalizedName
  const short = shortName(displayName, level)

  return {
    code: normalizedCode,
    value: normalizedCode,
    parentCode: parentCode ? normalizeCode(parentCode) : null,
    parentName: parentName || '中国',
    name: displayName,
    label: displayName,
    shortName: short,
    fullName: displayName,
    level,
    levelLabel: LEVEL_LABELS[level],
    initial: initialOf(short || displayName),
    isHot: false,
    isEnabled: true,
    status: 'active',
    sortOrder: order,
    children: []
  }
}

/**
 * Build the three-level mainland China tree from the bundled seed dictionary.
 * The source package includes Hong Kong, Macao and Taiwan; those roots are
 * intentionally excluded to match the backend seed and store selector scope.
 */
export function buildChinaRegionTree() {
  const provinceMap = AREA_DATA?.[ROOT_CODE] || {}

  return Object.entries(provinceMap)
    .filter(([provinceCode]) => !EXCLUDED_ROOT_CODES.has(normalizeCode(provinceCode)))
    .map(([provinceCode, provinceName], provinceIndex) => {
      const province = makeNode(provinceCode, provinceName, 'Province', null, null, provinceIndex + 1)
      const cityMap = AREA_DATA?.[provinceCode] || {}

      province.children = Object.entries(cityMap).map(([cityCode, cityName], cityIndex) => {
        const city = makeNode(cityCode, cityName, 'City', province.code, province.name, cityIndex + 1)
        const districtMap = AREA_DATA?.[cityCode] || {}

        // The bundled dictionary also contains 9-digit street/town entries
        // for a few directly-administered cities. Region management stops at
        // the district/county level, so keep only standard 6-digit codes.
        city.children = Object.entries(districtMap)
          .filter(([districtCode]) => /^\d{6}$/.test(normalizeCode(districtCode)))
          .map(([districtCode, districtName], districtIndex) => (
            makeNode(districtCode, districtName, 'District', city.code, city.name, districtIndex + 1)
          ))

        return city
      })

      return province
    })
}

export function flattenRegionTree(tree = buildChinaRegionTree()) {
  const rows = []
  const visit = (node) => {
    const { children, ...row } = node
    rows.push({ ...row, children: undefined })
    ;(children || []).forEach(visit)
  }
  tree.forEach(visit)
  return rows.map(({ children, ...row }) => row)
}

export function seedChinaRegions() {
  return flattenRegionTree()
}

export function toRegionCascaderOptions(tree = buildChinaRegionTree()) {
  return tree.map((province) => ({
    value: province.code,
    label: province.name,
    children: province.children.map((city) => ({
      value: city.code,
      label: city.name,
      children: city.children.map((district) => ({
        value: district.code,
        label: district.name
      }))
    }))
  }))
}

function extractRows(payload) {
  if (Array.isArray(payload)) return payload
  return payload?.items || payload?.rows || payload?.data || payload?.regions || []
}

function mapApiNode(item, parent = null, order = 0) {
  const code = item.code ?? item.value ?? item.id
  const levelValue = item.level ?? item.levelName ?? item.type
  const level = ({
    province: 'Province',
    省: 'Province',
    city: 'City',
    市: 'City',
    district: 'District',
    区: 'District',
    county: 'District',
    区县: 'District'
  })[String(levelValue || '')] || levelValue || (parent ? (parent.level === 'Province' ? 'City' : 'District') : 'Province')
  const parentCode = item.parentCode ?? item.parentId ?? parent?.code ?? null
  const parentName = item.parentName ?? parent?.name ?? '中国'
  const name = item.name ?? item.label ?? item.title ?? ''
  const node = makeNode(code, name, level, parentCode, parentName, item.sortOrder ?? item.sort ?? order)

  return {
    ...node,
    shortName: item.shortName ?? node.shortName,
    fullName: item.fullName ?? item.full_name ?? node.fullName,
    initial: item.initial ?? node.initial,
    isHot: item.isHot ?? item.hot ?? false,
    isEnabled: item.isEnabled ?? item.enabled ?? item.status !== 'hidden',
    status: item.status ?? ((item.isEnabled ?? item.enabled ?? true) ? 'active' : 'hidden'),
    children: extractRows(item.children).map((child, index) => mapApiNode(child, node, index + 1))
  }
}

export function normalizeRegionPayload(payload) {
  const source = extractRows(payload)
  if (!source.length) return []

  if (source.some((item) => Array.isArray(item?.children) && item.children.length)) {
    return flattenRegionTree(source.map((item, index) => mapApiNode(item, null, index + 1)))
  }

  const raw = source.map((item, index) => mapApiNode(item, null, index + 1))
  const byCode = new Map(raw.map((item) => [item.code, item]))
  const roots = []

  raw.forEach((row) => {
    if (!row.parentCode || !byCode.has(row.parentCode)) {
      roots.push(row)
      return
    }
    const parent = byCode.get(row.parentCode)
    parent.children = parent.children || []
    parent.children.push(row)
  })

  return flattenRegionTree(roots)
}

export function regionTreeFromRows(rows = []) {
  const byCode = new Map(rows.map((row) => [String(row.code), { ...row, children: [] }]))
  const roots = []
  byCode.forEach((row) => {
    if (row.parentCode && byCode.has(String(row.parentCode))) {
      byCode.get(String(row.parentCode)).children.push(row)
    } else {
      roots.push(row)
    }
  })
  return roots
}

export const regionLevelLabels = LEVEL_LABELS
