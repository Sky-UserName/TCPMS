// 用户端民宿管理图标库：与 src/TCPMS.User/components/ProtoIcon.vue 保持同一套
// 别名规则 + src/TCPMS.User/static/prototype-icons 的同一批 PNG（已复制到
// src/assets/prototype-icons，用 scripts/sync-miniapp-icons.ps1 可重新同步）。
// 权限菜单只需要保存 iconKey，管理端和用户端会渲染成完全相同的图标。

const iconModules = import.meta.glob('../assets/prototype-icons/ui-*.png', {
  eager: true,
  query: '?url',
  import: 'default'
})

// 与 ProtoIcon.vue 的 aliases 完全一致，兼容历史数据里的 Material 图标名。
export const MINIAPP_ICON_ALIASES = {
  arrow_back: 'back',
  arrow_back_ios_new: 'back',
  back: 'back',
  more_horiz: 'more',
  more: 'more',
  adjust: 'status',
  trip_origin: 'status',
  radio_button_checked: 'status',
  person: 'user',
  account_circle: 'user',
  map: 'map',
  location_on: 'location',
  explore: 'navigation',
  my_location: 'navigation',
  near_me: 'navigation',
  search: 'search',
  manage_search: 'search',
  tune: 'tune',
  calendar_month: 'calendar',
  chevron_right: 'chevron-right',
  keyboard_arrow_right: 'chevron-right',
  arrow_forward: 'chevron-right',
  chevron_down: 'chevron-down',
  expand_more: 'chevron-down',
  keyboard_arrow_down: 'chevron-down',
  chevron_up: 'chevron-up',
  expand_less: 'chevron-up',
  share: 'share',
  favorite: 'heart',
  phone: 'phone',
  support_agent: 'support',
  chat: 'support',
  storefront: 'store',
  apartment: 'store',
  hotel: 'bed',
  bed: 'bed',
  receipt: 'receipt',
  receipt_long: 'receipt',
  group: 'group',
  qr_code_2: 'qr',
  settings: 'settings',
  check: 'check',
  check_circle: 'check-circle',
  verified: 'verified',
  verified_user: 'shield',
  security: 'shield',
  lock: 'lock',
  key: 'key',
  description: 'document',
  auto_stories: 'book',
  info: 'info',
  star: 'star',
  schedule: 'clock',
  access_time: 'clock',
  image: 'image',
  photo_camera: 'image',
  edit: 'edit',
  add: 'plus',
  remove: 'minus',
  close: 'close',
  warning: 'warning',
  help: 'help',
  refresh: 'refresh',
  wifi: 'wifi',
  local_parking: 'parking',
  cottage: 'house',
  home: 'house',
  roofing: 'house'
}

const ICON_LABELS = {
  wallet: '财务',
  house: '房源',
  receipt: '订单',
  qr: '核销',
  document: '单据',
  group: '员工',
  store: '门店',
  plus: '新增',
  star: '评价',
  settings: '设置',
  bed: '房型',
  calendar: '日历',
  clock: '时间',
  user: '用户',
  verified: '认证',
  shield: '安全',
  support: '客服',
  phone: '电话',
  location: '定位',
  map: '地图',
  navigation: '导航',
  tune: '筛选',
  refresh: '刷新',
  search: '搜索',
  'check-circle': '完成',
  check: '勾选',
  image: '图片',
  book: '手册',
  key: '密钥',
  lock: '锁定',
  share: '分享',
  heart: '收藏',
  'heart-filled': '已收藏',
  info: '说明',
  help: '帮助',
  warning: '警告',
  more: '更多',
  minus: '移除',
  close: '关闭',
  back: '返回',
  'chevron-down': '展开',
  'chevron-right': '右箭头',
  'chevron-up': '收起',
  edit: '编辑',
  trash: '删除',
  status: '状态',
  parking: '停车',
  wifi: '网络'
}

// 面板里优先展示运营入口常用图标，其余按字母序补齐。
const PREFERRED_ORDER = [
  'wallet', 'house', 'receipt', 'qr', 'document', 'group', 'store', 'plus',
  'star', 'settings', 'bed', 'calendar', 'clock', 'user', 'verified', 'shield',
  'support', 'phone', 'location', 'map', 'navigation', 'tune', 'refresh', 'search',
  'check-circle', 'check', 'image', 'book', 'share', 'heart', 'edit', 'trash'
]

const ICON_SRC = {}
for (const [path, url] of Object.entries(iconModules)) {
  const match = path.match(/ui-(.+)\.png$/)
  if (match) ICON_SRC[match[1]] = url
}

const ALL_KEYS = Object.keys(ICON_SRC)
const ORDERED_KEYS = [
  ...PREFERRED_ORDER.filter((key) => ALL_KEYS.includes(key)),
  ...ALL_KEYS.filter((key) => !PREFERRED_ORDER.includes(key)).sort()
]

export const MINIAPP_ICON_OPTIONS = ORDERED_KEYS.map((key) => ({
  key,
  label: ICON_LABELS[key] || key,
  src: ICON_SRC[key]
}))

/** 把 iconKey / Material 别名 / 带 -white 后缀的历史值归一成图标库里的 key。 */
export function resolveMiniAppIconKey(iconKey) {
  const raw = String(iconKey || '').trim().toLowerCase().replace(/^ui-/, '').replace(/\.png$/, '')
  if (!raw) return ''
  const normalized = MINIAPP_ICON_ALIASES[raw] || raw.replace(/-white$/, '')
  return ICON_SRC[normalized] ? normalized : ''
}

/** 返回图标资源地址；iconKey 不在图标库时返回空字符串，由调用方走兜底。 */
export function miniAppIconSrc(iconKey) {
  const key = resolveMiniAppIconKey(iconKey)
  return key ? ICON_SRC[key] : ''
}

/** 返回「财务」「房源」这类中文名，用于下拉选项和列表展示。 */
export function miniAppIconLabel(iconKey) {
  const key = resolveMiniAppIconKey(iconKey)
  return key ? ICON_LABELS[key] || key : ''
}
