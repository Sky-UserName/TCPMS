export const PERMISSION_MENU_STORAGE_KEY = 'tcpms.admin.permission-menu.v1'
export const STORE_STAFF_STORAGE_KEY = 'tcpms.admin.store-staff.v1'

export const DEMO_STORES = [
  { id: 'store-001', name: '天空之蓝' },
  { id: 'store-002', name: '上海人民广场店' },
  { id: 'store-003', name: '杭州西湖店' }
]

// This list mirrors merchantEntries in src/TCPMS.User/pages/me/index.vue.
export const MINIAPP_MERCHANT_MENU = [
  { key: 'finance', title: '财务统计', iconKey: 'wallet', route: '/pages/merchant/finance' },
  { key: 'rooms', title: '房间管理', iconKey: 'house', route: '/pages/merchant/rooms' },
  { key: 'orders', title: '订单管理', iconKey: 'receipt', route: '/pages/merchant/orders' },
  { key: 'verify', title: '扫码核销', iconKey: 'qr', route: '/pages/merchant/verify' },
  { key: 'records', title: '核销记录', iconKey: 'document', route: '/pages/merchant/records' },
  { key: 'staff', title: '员工管理', iconKey: 'group', route: '/pages/merchant/staff' },
  { key: 'property', title: '民宿管理', iconKey: 'store', route: '/pages/merchant/index' },
  { key: 'publish', title: '发布房源', iconKey: 'plus', route: '/pages/merchant/publish' },
  { key: 'reviews', title: '评价管理', iconKey: 'star', route: '/pages/merchant/reviews' },
  { key: 'coupon', title: '优惠券管理', iconKey: 'document', route: '/pages/merchant/coupons' },
  { key: 'settings', title: '民宿设置', iconKey: 'settings', route: '/pages/merchant/settings' },
  { key: 'subscriptions', title: '订阅消息', iconKey: 'document', route: '/pages/merchant/messages' }
]

export function createPermissionMenuSeed(now = new Date()) {
  const timestamp = now.toLocaleString('zh-CN')
  return MINIAPP_MERCHANT_MENU.map((item, index) => ({
    ...item,
    iconUrl: '',
    weight: index + 1,
    status: 'normal',
    createdAt: timestamp,
    updatedAt: timestamp
  }))
}

export function readPermissionMenus() {
  try {
    const value = localStorage.getItem(PERMISSION_MENU_STORAGE_KEY)
    const parsed = value ? JSON.parse(value) : null
    return Array.isArray(parsed) && parsed.length ? parsed : createPermissionMenuSeed()
  } catch {
    return createPermissionMenuSeed()
  }
}
