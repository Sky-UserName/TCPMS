<template>
  <div v-if="session.checking" class="session-check">
    <div class="session-check-card">
      <span class="session-check-spinner"></span>
      <p>正在验证登录状态…</p>
    </div>
  </div>
  <LoginView v-else-if="!session.token" @success="handleLogin" />
  <AdminShell
    v-else
    :user="session.user"
    :active-view="activeView"
    @navigate="activeView = $event"
    @logout="logout"
  >
    <component
      :is="views[activeView]"
      :key="activeView"
      :user="session.user"
      :view-key="activeView"
      @navigate="activeView = $event"
    />
  </AdminShell>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { clearToken, get, getToken, setToken } from './api'
import LoginView from './components/LoginView.vue'
import AdminShell from './components/AdminShell.vue'
import DashboardView from './views/DashboardView.vue'
import StoresView from './views/StoresView.vue'
import CatalogView from './views/CatalogView.vue'
import OrdersView from './views/OrdersView.vue'
import RefundsView from './views/RefundsView.vue'
import UsersView from './views/UsersView.vue'
import AuditView from './views/AuditView.vue'
import MembersView from './views/MembersView.vue'
import DemoManagerView from './views/DemoManagerView.vue'
import RegionManagerView from './views/RegionManagerView.vue'
import StoreStaffView from './views/StoreStaffView.vue'
import PermissionMenusView from './views/PermissionMenusView.vue'
import MessagesView from './views/MessagesView.vue'
import MessageLogsView from './views/MessageLogsView.vue'
import SubscriptionMessagesView from './views/SubscriptionMessagesView.vue'
import AppCenterView from './views/AppCenterView.vue'

const activeView = ref('dashboard')
// getToken 已过滤过期令牌；有令牌时先进入校验态，避免首帧就把后台外壳挂起来。
const initialToken = getToken()
const session = reactive({
  token: initialToken,
  user: null,
  checking: Boolean(initialToken)
})

const views = {
  dashboard: DashboardView,
  stores: StoresView,
  catalog: CatalogView,
  refunds: RefundsView,
  users: UsersView,
  audit: AuditView,
  members: MembersView,
  levels: DemoManagerView,
  roomManagement: CatalogView,
  financialOrders: OrdersView,
  permissions: UsersView,
  roomTypes: DemoManagerView,
  facilities: DemoManagerView,
  tags: DemoManagerView,
  regions: RegionManagerView,
  reviews: DemoManagerView,
  withdrawals: DemoManagerView,
  income: DemoManagerView,
  messages: MessagesView,
  messageLogs: MessageLogsView,
  appCenter: AppCenterView,
  carousel: DemoManagerView,
  subscriptions: SubscriptionMessagesView,
  storeStaff: StoreStaffView,
  permissionMenus: PermissionMenusView
}

function handleLogin(payload) {
  setToken(payload.token)
  session.token = payload.token
  session.user = payload.user
  session.checking = false
  activeView.value = 'dashboard'
}

function logout() {
  clearToken()
  session.token = ''
  session.user = null
  session.checking = false
}

// api.js 里遇到 401 会广播事件，这里统一退回登录页。
window.addEventListener('tcpms:unauthorized', logout)

onMounted(async () => {
  if (!session.token) {
    session.checking = false
    return
  }
  // 先用 /admin/me 校验令牌，通过后再挂载后台外壳。
  // 否则外壳一挂载就会并发打 metrics / notifications，令牌失效时满屏 401。
  session.checking = true
  try {
    session.user = await get('/admin/me')
  } catch (error) {
    clearToken()
    session.token = ''
    session.user = null
    // 403 通常表示初始密码未修改（需重新走登录改密流程），不必再弹一次错误。
    if (error.status !== 401 && error.code !== 'password_change_required') {
      ElMessage.error(error.message)
    }
  } finally {
    session.checking = false
  }
})
</script>

<style scoped>
.session-check {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: #f4f8f8;
}

.session-check-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 18px 26px;
  border-radius: 14px;
  background: #ffffff;
  box-shadow: 0 12px 30px rgba(15, 119, 119, 0.12);
  color: #3d5a5c;
  font-size: 14px;
}

.session-check-card p {
  margin: 0;
}

.session-check-spinner {
  width: 18px;
  height: 18px;
  border: 2px solid #cfe4e4;
  border-top-color: #0f7777;
  border-radius: 50%;
  animation: session-spin 0.8s linear infinite;
}

@keyframes session-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
