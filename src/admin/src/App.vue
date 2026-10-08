<template>
  <LoginView v-if="!session.token" @success="handleLogin" />
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

const activeView = ref('dashboard')
const session = reactive({
  token: getToken(),
  user: null
})

const views = {
  dashboard: DashboardView,
  stores: StoresView,
  catalog: CatalogView,
  orders: OrdersView,
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
  regions: DemoManagerView,
  commonConfig: DemoManagerView,
  pages: DemoManagerView,
  reviews: DemoManagerView,
  guideTypes: DemoManagerView,
  guides: DemoManagerView,
  search: DemoManagerView,
  withdrawals: DemoManagerView,
  memberWithdrawals: DemoManagerView,
  distribution: DemoManagerView,
  income: DemoManagerView,
  messages: DemoManagerView,
  plugins: DemoManagerView,
  appCenter: DemoManagerView,
  storePermissions: DemoManagerView,
  common: DemoManagerView
}

function handleLogin(payload) {
  setToken(payload.token)
  session.token = payload.token
  session.user = payload.user
  activeView.value = 'dashboard'
}

function logout() {
  clearToken()
  session.token = ''
  session.user = null
}

onMounted(async () => {
  if (!session.token) return
  try {
    session.user = await get('/admin/me')
  } catch (error) {
    clearToken()
    session.token = ''
    ElMessage.error(error.message)
  }
})
</script>
