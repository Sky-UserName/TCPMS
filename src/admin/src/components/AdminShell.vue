<template>
  <div class="admin-shell">
    <aside class="sidebar" :class="{ 'sidebar-collapsed': collapsed }">
      <div class="sidebar-brand">
        <div class="brand-mark small"><img src="/brand-mark.jpg" alt="择居青旅" /></div>
        <div class="brand-copy">
          <strong>择居青旅</strong>
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
          <button type="button" @click="openPasswordDialog">修改密码</button>
          <button type="button" @click="$emit('logout')">退出登录</button>
        </div>
      </div>
    </aside>

    <el-dialog v-model="passwordVisible" title="修改密码" width="380px" append-to-body>
      <el-form ref="passwordFormRef" :model="passwordForm" :rules="passwordRules" label-position="top">
        <el-form-item prop="currentPassword" label="当前密码">
          <el-input v-model="passwordForm.currentPassword" type="password" show-password />
        </el-form-item>
        <el-form-item prop="newPassword" label="新密码">
          <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="至少 8 位，需包含字母和数字" />
        </el-form-item>
        <el-form-item prop="confirmPassword" label="确认新密码">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="passwordVisible = false">取消</el-button>
        <el-button type="primary" :loading="passwordSaving" @click="submitPassword">保存</el-button>
      </template>
    </el-dialog>

    <section class="main-shell">
      <header class="topbar">
        <div>
          <span class="topbar-kicker">ZE JU HOSTEL · OPERATIONS</span>
          <h1>{{ currentTitle }}</h1>
        </div>
        <div class="topbar-actions">
          <span class="topbar-date">{{ todayLabel }}</span>
          <el-popover
            v-model:visible="notificationVisible"
            placement="bottom-end"
            :width="390"
            trigger="click"
            popper-class="notification-popover"
          >
            <template #reference>
              <button class="icon-button notification-button" title="消息提醒" aria-label="消息提醒" type="button">
                <el-badge :value="notificationCount" :max="99" :hidden="notificationCount === 0" class="notification-badge">
                  <el-icon><BellFilled /></el-icon>
                </el-badge>
              </button>
            </template>
            <div class="notification-center">
              <div class="notification-center-head">
                <div><strong>消息提醒</strong><span>{{ notificationCount ? `${notificationCount} 项待处理` : '暂无待处理事项' }}</span></div>
                <button class="notification-refresh" title="刷新消息" aria-label="刷新消息" type="button" :disabled="notificationState.loading" @click="syncNotifications">
                  <el-icon><Refresh /></el-icon>
                </button>
              </div>
              <div v-if="notificationItems.length" class="notification-list">
                <button v-for="item in notificationItems.slice(0, 6)" :key="item.id" type="button" class="notification-entry" @click="openNotification(item)">
                  <span class="notification-entry-icon" :class="item.tone"><el-icon><component :is="notificationIcon(item.type)" /></el-icon></span>
                  <span class="notification-entry-copy"><strong>{{ item.title }}</strong><small>{{ item.description }}</small><em>{{ formatNotificationTime(item.createdAt) }}</em></span>
                  <el-icon class="notification-entry-arrow"><ArrowRight /></el-icon>
                </button>
              </div>
              <el-empty v-else description="暂无需要处理的消息" :image-size="52" />
              <button class="notification-center-footer" type="button" @click="openAllNotifications">查看全部消息<el-icon><ArrowRight /></el-icon></button>
            </div>
          </el-popover>
          <button class="icon-button" title="回到控制台" type="button" @click="navigate('dashboard')"><el-icon><House /></el-icon></button>
          <button class="icon-button" title="清除本地演示缓存" type="button" @click="clearDemoCache"><el-icon><Delete /></el-icon></button>
          <button class="icon-button" title="切换全屏" type="button" @click="toggleFullscreen"><el-icon><FullScreen /></el-icon></button>
          <button class="topbar-user" type="button" @click="profileVisible = !profileVisible">
            <span class="profile-avatar">{{ initials }}</span>
            <span>{{ user?.displayName || 'Admin1' }}</span>
          </button>
        </div>
      </header>
      <main class="content-area"><slot /></main>
    </section>

    <el-dialog v-model="helpVisible" title="需要帮助？" width="420px">
      <p class="dialog-copy">当前后台已覆盖门店、房源、订单、会员、内容、财务和权限管理。生产环境还需要配置微信支付、高德地图、对象存储和数据库账号。</p>
      <template #footer><el-button type="primary" @click="helpVisible = false">知道了</el-button></template>
    </el-dialog>

    <el-dialog v-model="firstNoticeVisible" title="待处理消息" width="540px" class="first-notification-dialog">
      <div class="first-notification-intro">以下事项需要管理人员处理，控制台待办和右上角消息提醒会同步更新。</div>
      <div class="first-notification-list">
        <button v-for="item in notificationItems.slice(0, 12)" :key="item.id" type="button" class="first-notification-entry" @click="openNotification(item)">
          <span class="notification-entry-icon" :class="item.tone"><el-icon><component :is="notificationIcon(item.type)" /></el-icon></span>
          <span class="notification-entry-copy"><strong>{{ item.title }}</strong><small>{{ item.description }}</small></span>
          <el-icon class="notification-entry-arrow"><ArrowRight /></el-icon>
        </button>
      </div>
      <template #footer>
        <el-button @click="firstNoticeVisible = false">稍后处理</el-button>
        <el-button type="primary" @click="openAllNotifications">进入消息中心</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElNotification } from 'element-plus'
import {
  ArrowDown,
  ArrowRight,
  Bell,
  BellFilled,
  CollectionTag,
  Delete,
  Document,
  Picture,
  Expand,
  Fold,
  FullScreen,
  House,
  Location,
  Menu,
  MoreFilled,
  Operation,
  Refresh,
  Search,
  Tickets,
  UserFilled,
  Wallet
} from '@element-plus/icons-vue'
import { refreshNotifications, useAdminNotifications } from '../notifications'
import { post, setToken } from '../api'

const props = defineProps({
  user: { type: Object, default: null },
  activeView: { type: String, default: 'dashboard' }
})
const emit = defineEmits(['navigate', 'logout'])
const collapsed = ref(false)
const menuQuery = ref('')
const profileVisible = ref(false)
const passwordVisible = ref(false)
const passwordSaving = ref(false)
const passwordFormRef = ref()
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const passwordRules = {
  currentPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (!value || value.length < 8 || value.length > 64) {
          callback(new Error('密码长度需在 8 到 64 位之间'))
          return
        }
        if (!/[A-Za-z]/.test(value) || !/\d/.test(value)) {
          callback(new Error('密码需同时包含字母和数字'))
          return
        }
        if (value === passwordForm.currentPassword) {
          callback(new Error('新密码不能与当前密码相同'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的新密码不一致'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}
const helpVisible = ref(false)
const notificationVisible = ref(false)
const firstNoticeVisible = ref(false)
const knownNotificationIds = ref(new Set())
const notificationInitialized = ref(false)
const expandedGroups = ref({ core: true, property: true, storeManagement: true, storePermissions: false, config: false, finance: false, system: false })
const menuSearching = computed(() => Boolean(menuQuery.value.trim()))
const { state: notificationState, items: notificationItems, pendingCount: notificationCount } = useAdminNotifications()
let notificationTimer

const groups = [
  { key: 'core', label: '工作台', items: [
    { key: 'dashboard', label: '控制台', icon: Operation, badge: 'hot' },
  ] },
  {
    key: 'property',
    label: '民宿管理',
    items: [
      { key: 'members', label: '会员管理', icon: UserFilled },
    ],
    children: [
      { key: 'storeManagement', label: '门店管理', items: [
        { key: 'reviews', label: '评价管理', icon: CollectionTag },
        { key: 'stores', label: '门店管理', icon: Location },
        { key: 'roomManagement', label: '房间管理', icon: House },
      ] },
      { key: 'storePermissions', label: '门店权限', items: [
        { key: 'storeStaff', label: '门店员工', icon: UserFilled },
        { key: 'permissionMenus', label: '权限菜单', icon: Menu }
      ] },
      { key: 'config', label: '基础配置', items: [
        { key: 'facilities', label: '服务设施', icon: CollectionTag },
        { key: 'tags', label: '民宿标签', icon: CollectionTag },
        { key: 'services', label: '民宿服务', icon: CollectionTag },
        { key: 'nearbyFacilities', label: '周边设施', icon: Location },
        { key: 'regions', label: '地区管理', icon: Location },
        { key: 'roomTypes', label: '房间类型', icon: House },
        { key: 'levels', label: '民宿等级', icon: CollectionTag }
      ] },
      { key: 'finance', label: '财务管理', items: [
        { key: 'financialOrders', label: '民宿订单', icon: Tickets },
        { key: 'refunds', label: '退款审核', icon: Wallet },
        { key: 'withdrawals', label: '门店提现', icon: Wallet },
        { key: 'income', label: '门店收入', icon: Wallet }
      ] },
      { key: 'system', label: '系统管理', items: [
        { key: 'users', label: '权限管理', icon: UserFilled },
        { key: 'appCenter', label: '应用中心', icon: CollectionTag },
        { key: 'carousel', label: '轮播图配置', icon: Picture },
        { key: 'messages', label: '消息提醒', icon: Bell },
        { key: 'messageLogs', label: '消息日志', icon: Document },
        { key: 'subscriptions', label: '订阅消息', icon: BellFilled },
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
  roomManagement: '房间管理', financialOrders: '民宿订单', refunds: '退款审核',
  users: '权限管理', audit: '操作审计',
  reviews: '评价管理', facilities: '服务设施', tags: '民宿标签',
  services: '民宿服务', nearbyFacilities: '周边设施',
  regions: '地区管理', roomTypes: '房间类型', levels: '民宿等级',
  storeStaff: '门店员工', permissionMenus: '权限菜单', withdrawals: '门店提现',
  income: '门店收入', appCenter: '应用中心', carousel: '轮播图配置', messages: '消息提醒', messageLogs: '消息日志',
  subscriptions: '订阅消息'
}
const currentTitle = computed(() => titleMap[props.activeView] || '控制台')
const initials = computed(() => (props.user?.displayName || '管').slice(0, 1))
const roleLabel = computed(() => props.user?.roles?.[0] || '总部管理员')
const todayLabel = computed(() => new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'short' }).format(new Date()))

function navigate(key) { emit('navigate', key); profileVisible.value = false }
function openPasswordDialog() {
  profileVisible.value = false
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
  passwordVisible.value = true
}
async function submitPassword() {
  const valid = await passwordFormRef.value?.validate().catch(() => false)
  if (!valid) return
  passwordSaving.value = true
  try {
    const result = await post('/auth/change-password', {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword
    })
    // 后端会换发 token，旧 token 里仍带「必须改密」标记。
    if (result?.token) setToken(result.token)
    passwordVisible.value = false
    ElMessage.success('密码已更新')
  } catch (error) {
    ElMessage.error(error.message || '修改密码失败')
  } finally {
    passwordSaving.value = false
  }
}
function toggleGroup(key) { expandedGroups.value[key] = !expandedGroups.value[key] }
function notificationIcon(type) {
  return ({ order: Tickets, refund: Wallet, store: Location, room: House, review: CollectionTag, withdrawal: Wallet })[type] || BellFilled
}
function formatNotificationTime(value) {
  if (!value) return '刚刚'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '刚刚'
  return date.toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
function firstNoticeKey() {
  return `tcpms.admin.notifications.first.${props.user?.id || props.user?.username || 'current'}`
}
function openNotification(item) {
  notificationVisible.value = false
  firstNoticeVisible.value = false
  navigate(item.view)
}
function openAllNotifications() {
  notificationVisible.value = false
  firstNoticeVisible.value = false
  navigate('messages')
}
async function syncNotifications() {
  const previousIds = knownNotificationIds.value
  const payload = await refreshNotifications({ silent: true })
  if (!payload) return

  const currentItems = notificationItems.value
  if (!notificationInitialized.value) {
    notificationInitialized.value = true
    knownNotificationIds.value = new Set(currentItems.map((item) => item.id))
    if (currentItems.length && !sessionStorage.getItem(firstNoticeKey())) {
      sessionStorage.setItem(firstNoticeKey(), '1')
      window.setTimeout(() => { firstNoticeVisible.value = true }, 220)
    }
    return
  }

  const newItems = currentItems.filter((item) => !previousIds.has(item.id))
  knownNotificationIds.value = new Set(currentItems.map((item) => item.id))
  if (newItems.length && document.visibilityState !== 'hidden') {
    ElNotification({
      title: '新的待处理提醒',
      message: newItems.length === 1 ? newItems[0].title : `新增 ${newItems.length} 项待处理事项`,
      type: 'warning',
      duration: 5000,
      onClick: () => openNotification(newItems[0])
    })
  }
}
function clearDemoCache() {
  Object.keys(localStorage).filter((key) => key.startsWith('tcpms.admin.demo.') || key === 'tcpms.admin.members').forEach((key) => localStorage.removeItem(key))
  ElMessage.success('演示数据缓存已清除，刷新页面后恢复初始数据')
}
function toggleFullscreen() {
  if (!document.fullscreenElement) document.documentElement.requestFullscreen?.()
  else document.exitFullscreen?.()
}

onMounted(async () => {
  await syncNotifications()
  notificationTimer = window.setInterval(syncNotifications, 60000)
  window.addEventListener('tcpms:notifications-refresh', syncNotifications)
})

onBeforeUnmount(() => {
  if (notificationTimer) window.clearInterval(notificationTimer)
  window.removeEventListener('tcpms:notifications-refresh', syncNotifications)
})
</script>
