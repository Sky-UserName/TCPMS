<template>
  <div class="admin-shell">
    <aside class="sidebar" :class="{ 'sidebar-collapsed': collapsed }">
      <div class="sidebar-brand">
        <div class="brand-mark small">T</div>
        <div class="brand-copy">
          <strong>TCPMS 智选旅宿</strong>
          <span>运营管理中台</span>
        </div>
        <button
          class="collapse-button"
          :title="collapsed ? '展开菜单' : '收起菜单'"
          :aria-label="collapsed ? '展开菜单' : '收起菜单'"
          type="button"
          @click="collapsed = !collapsed"
        >
          <el-icon><component :is="collapsed ? Expand : Fold" /></el-icon>
        </button>
      </div>

      <div class="workspace-label">运营工作区</div>
      <div class="menu-search">
        <el-input v-model="menuQuery" clearable placeholder="搜索菜单" :prefix-icon="Search" />
      </div>
      <nav class="sidebar-nav">
        <template v-for="group in filteredGroups" :key="group.key">
          <div class="nav-section">
            <button class="nav-section-title" type="button" @click="toggleGroup(group.key)">
              <span>{{ group.label }}</span>
              <el-icon :class="{ rotated: expandedGroups[group.key] }"><ArrowDown /></el-icon>
            </button>
            <div v-show="expandedGroups[group.key] || menuSearching" class="nav-section-items">
              <button
                v-for="item in group.items"
                :key="item.key"
                class="nav-button"
                :class="{ active: activeView === item.key }"
                type="button"
                @click="navigate(item.key)"
              >
                <el-icon><component :is="item.icon" /></el-icon>
                <span>{{ item.label }}</span>
                <b v-if="item.badge">{{ item.badge }}</b>
              </button>
              <div v-for="child in group.children" :key="child.key" class="nav-subsection">
                <button class="nav-subsection-title" type="button" @click="toggleGroup(child.key)">
                  <span>{{ child.label }}</span>
                  <el-icon :class="{ rotated: expandedGroups[child.key] }"><ArrowDown /></el-icon>
                </button>
                <div v-show="expandedGroups[child.key] || menuSearching" class="nav-subsection-items">
                  <button
                    v-for="item in child.items"
                    :key="item.key"
                    class="nav-button"
                    :class="{ active: activeView === item.key }"
                    type="button"
                    @click="navigate(item.key)"
                  >
                    <el-icon><component :is="item.icon" /></el-icon>
                    <span>{{ item.label }}</span>
                    <b v-if="item.badge">{{ item.badge }}</b>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </template>
      </nav>

      <div class="sidebar-divider"></div>
      <div class="sidebar-bottom">
        <button class="profile-button" type="button" @click="profileVisible = !profileVisible">
          <span class="profile-avatar">{{ initials }}</span>
          <span class="profile-copy"><strong>{{ user?.displayName || '管理员' }}</strong><small>{{ roleLabel }}</small></span>
          <el-icon><MoreFilled /></el-icon>
        </button>
        <div v-if="profileVisible" class="profile-menu">
          <button type="button" @click="navigate('users')">账号与权限</button>
          <button type="button" @click="$emit('logout')">退出登录</button>
        </div>
      </div>
    </aside>

    <section class="main-shell">
      <header class="topbar">
        <div>
          <span class="topbar-kicker">TCPMS · SERENE STAY</span>
          <h1>{{ currentTitle }}</h1>
        </div>
        <div class="topbar-actions">
          <span class="topbar-date">{{ todayLabel }}</span>
          <button class="icon-button notification-button" title="消息提醒" type="button" @click="navigate('messages')">
            <el-icon><Bell /></el-icon><i></i>
          </button>
          <button class="icon-button" title="回到控制台" type="button" @click="navigate('dashboard')"><el-icon><House /></el-icon></button>
          <button class="icon-button" title="清除本地演示缓存" type="button" @click="clearDemoCache"><el-icon><Delete /></el-icon></button>
          <button class="icon-button" title="切换全屏" type="button" @click="toggleFullscreen"><el-icon><FullScreen /></el-icon></button>
          <button class="topbar-user" type="button" @click="profileVisible = !profileVisible">
            <span class="profile-avatar">{{ initials }}</span>
            <span>{{ user?.displayName || 'Admin1' }}</span>
          </button>
          <button class="icon-button" title="常规管理" type="button" @click="navigate('common')"><el-icon><Setting /></el-icon></button>
        </div>
      </header>
      <main class="content-area"><slot /></main>
    </section>

    <el-dialog v-model="helpVisible" title="需要帮助？" width="420px">
      <p class="dialog-copy">当前后台已覆盖门店、房源、订单、会员、内容、财务和权限管理。生产环境还需要配置微信支付、高德地图、对象存储和数据库账号。</p>
      <template #footer><el-button type="primary" @click="helpVisible = false">知道了</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  ArrowDown,
  Bell,
  CollectionTag,
  Delete,
  Document,
  Expand,
  Fold,
  FullScreen,
  House,
  Location,
  MoreFilled,
  Operation,
  Search,
  Setting,
  Tickets,
  UserFilled,
  Wallet
} from '@element-plus/icons-vue'

const props = defineProps({
  user: { type: Object, default: null },
  activeView: { type: String, default: 'dashboard' }
})
const emit = defineEmits(['navigate', 'logout'])
const collapsed = ref(false)
const menuQuery = ref('')
const profileVisible = ref(false)
const helpVisible = ref(false)
const expandedGroups = ref({ core: true, property: true, storeManagement: true, config: false, discovery: false, finance: false, system: false })
const menuSearching = computed(() => Boolean(menuQuery.value.trim()))

const groups = [
  { key: 'core', label: '工作台', items: [
    { key: 'dashboard', label: '控制台', icon: Operation, badge: 'hot' },
  ] },
  {
    key: 'property',
    label: '民宿管理',
    items: [
      { key: 'members', label: '会员管理', icon: UserFilled },
      { key: 'commonConfig', label: '通用配置', icon: Setting },
      { key: 'storePermissions', label: '门店权限', icon: UserFilled }
    ],
    children: [
      { key: 'storeManagement', label: '门店管理', items: [
        { key: 'reviews', label: '评价管理', icon: CollectionTag },
        { key: 'stores', label: '门店管理', icon: Location },
        { key: 'roomManagement', label: '房间管理', icon: House },
      ] },
      { key: 'config', label: '基础配置', items: [
        { key: 'facilities', label: '服务设施', icon: CollectionTag },
        { key: 'tags', label: '民宿标签', icon: CollectionTag },
        { key: 'regions', label: '地区管理', icon: Location },
        { key: 'pages', label: '单页文章', icon: Document },
        { key: 'roomTypes', label: '房间类型', icon: House },
        { key: 'levels', label: '民宿等级', icon: CollectionTag }
      ] },
      { key: 'discovery', label: '发现管理', items: [
        { key: 'guideTypes', label: '攻略类型', icon: CollectionTag },
        { key: 'guides', label: '攻略管理', icon: Document },
        { key: 'search', label: '搜索记录', icon: Search }
      ] },
      { key: 'finance', label: '财务管理', items: [
        { key: 'orders', label: '订单运营', icon: Tickets },
        { key: 'financialOrders', label: '民宿订单', icon: Tickets },
        { key: 'refunds', label: '退款审核', icon: Wallet },
        { key: 'withdrawals', label: '门店提现', icon: Wallet },
        { key: 'memberWithdrawals', label: '会员提现', icon: Wallet },
        { key: 'distribution', label: '分销日志', icon: Tickets },
        { key: 'income', label: '门店收入', icon: Wallet }
      ] },
      { key: 'system', label: '系统管理', items: [
        { key: 'common', label: '常规管理', icon: Setting },
        { key: 'users', label: '权限管理', icon: UserFilled },
        { key: 'plugins', label: '插件管理', icon: Operation, badge: 'new' },
        { key: 'appCenter', label: '应用中心', icon: CollectionTag },
        { key: 'messages', label: '消息管理', icon: Bell },
        { key: 'audit', label: '操作审计', icon: Document }
      ] }
    ]
  }
]
const filteredGroups = computed(() => {
  const query = menuQuery.value.trim().toLowerCase()
  if (!query) return groups
  return groups.map((group) => {
    const items = (group.items || []).filter((item) => `${group.label}${item.label}`.toLowerCase().includes(query))
    const children = (group.children || []).map((child) => ({
      ...child,
      items: child.items.filter((item) => `${group.label}${child.label}${item.label}`.toLowerCase().includes(query))
    })).filter((child) => child.items.length)
    return { ...group, items, children }
  }).filter((group) => group.items.length || group.children.length)
})
const titleMap = {
  dashboard: '控制台', members: '会员管理', stores: '门店管理', catalog: '房源与库存',
  roomManagement: '房间管理', orders: '订单运营', financialOrders: '民宿订单', refunds: '退款审核',
  users: '权限管理', audit: '操作审计', commonConfig: '通用配置',
  storePermissions: '门店权限', reviews: '评价管理', facilities: '服务设施', tags: '民宿标签',
  regions: '地区管理', pages: '单页文章', roomTypes: '房间类型', levels: '民宿等级',
  guideTypes: '攻略类型', guides: '攻略管理', search: '搜索记录', withdrawals: '门店提现',
  memberWithdrawals: '会员提现', distribution: '分销日志', income: '门店收入', plugins: '插件管理',
  appCenter: '应用中心', messages: '消息管理', common: '常规管理'
}
const currentTitle = computed(() => titleMap[props.activeView] || '控制台')
const initials = computed(() => (props.user?.displayName || '管').slice(0, 1))
const roleLabel = computed(() => props.user?.roles?.[0] || '总部管理员')
const todayLabel = computed(() => new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'short' }).format(new Date()))

function navigate(key) { emit('navigate', key); profileVisible.value = false }
function toggleGroup(key) { expandedGroups.value[key] = !expandedGroups.value[key] }
function clearDemoCache() {
  Object.keys(localStorage).filter((key) => key.startsWith('tcpms.admin.demo.') || key === 'tcpms.admin.members').forEach((key) => localStorage.removeItem(key))
  ElMessage.success('演示数据缓存已清除，刷新页面后恢复初始数据')
}
function toggleFullscreen() {
  if (!document.fullscreenElement) document.documentElement.requestFullscreen?.()
  else document.exitFullscreen?.()
}
</script>
