<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
        <span class="eyebrow">TODAY AT A GLANCE</span>
        <h2>早上好，{{ displayName }} <span class="heading-mark">·</span></h2>
        <p>这里是你的旅宿经营概览，优先处理待确认订单和退款申请。</p>
      </div>
      <el-button type="primary" :icon="Refresh" :loading="loading" @click="load">刷新数据</el-button>
    </section>

    <section class="metric-grid">
      <div v-for="metric in metrics" :key="metric.key" class="metric-card" :class="metric.tone">
        <div class="metric-top">
          <span>{{ metric.label }}</span>
          <span class="metric-icon"><el-icon><component :is="metric.icon" /></el-icon></span>
        </div>
        <strong>{{ metric.value }}</strong>
        <small>{{ metric.note }}</small>
      </div>
    </section>

    <section class="dashboard-grid">
      <el-card class="panel-card trend-card" shadow="never">
        <template #header>
          <div class="panel-header">
            <div><strong>经营趋势</strong><span>近 7 天订单与销售额</span></div>
            <el-select v-model="trendDays" size="small" style="width: 112px" @change="loadTrend">
              <el-option label="近 7 天" :value="7" />
              <el-option label="近 30 天" :value="30" />
            </el-select>
          </div>
        </template>
        <div class="trend-summary">
          <div><small>累计订单</small><strong>{{ trendTotals.orders }}</strong></div>
          <div><small>支付销售额</small><strong>{{ money(trendTotals.sales) }}</strong></div>
          <div><small>退款金额</small><strong class="danger-text">{{ money(trendTotals.refunds) }}</strong></div>
        </div>
        <div v-loading="trendLoading" class="bar-chart" :class="{ muted: !trend.length }">
          <div v-for="item in trend" :key="item.date" class="bar-column">
            <div class="bar-value">{{ item.orderCount || '' }}</div>
            <div class="bar-track"><i :style="{ height: `${barHeight(item.orderCount)}%` }"></i></div>
            <span>{{ shortDate(item.date) }}</span>
          </div>
          <el-empty v-if="!trend.length" description="暂无趋势数据" :image-size="58" />
        </div>
        <div class="chart-legend"><span><i class="legend-dot primary"></i>订单量</span><span class="muted-text">销售额见上方汇总</span></div>
      </el-card>

      <el-card class="panel-card todo-card" shadow="never">
        <template #header>
          <div class="panel-header"><div><strong>待办事项</strong><span>需要优先处理的运营任务</span></div><el-icon class="muted-icon"><MoreFilled /></el-icon></div>
        </template>
        <div class="todo-list">
          <button v-for="item in todos" :key="item.title" type="button" class="todo-item" @click="$emit('navigate', item.view)">
            <span class="todo-icon" :class="item.tone"><el-icon><component :is="item.icon" /></el-icon></span>
            <span class="todo-copy"><strong>{{ item.title }}</strong><small>{{ item.desc }}</small></span>
            <span class="todo-count">{{ item.count }}</span>
            <el-icon><ArrowRight /></el-icon>
          </button>
        </div>
        <div class="todo-foot">所有关键操作都会写入操作审计日志</div>
      </el-card>
    </section>

    <section class="dashboard-grid lower-grid">
      <el-card class="panel-card" shadow="never">
        <template #header><div class="panel-header"><div><strong>门店经营排行</strong><span>按销售额排序</span></div><el-button link type="primary" @click="$emit('navigate', 'stores')">查看门店</el-button></div></template>
        <el-table :data="rank" class="clean-table" size="small">
          <el-table-column type="index" label="#" width="52" />
          <el-table-column prop="storeName" label="门店" min-width="190" />
          <el-table-column prop="orderCount" label="订单量" width="90" />
          <el-table-column label="销售额" width="120">
            <template #default="{ row }"><strong>{{ money(row.salesCents) }}</strong></template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!rank.length" description="暂无门店排行" :image-size="58" />
      </el-card>
      <el-card class="panel-card quick-card" shadow="never">
        <template #header><div class="panel-header"><div><strong>快捷操作</strong><span>从这里开始今天的工作</span></div></div></template>
        <div class="quick-actions">
          <button v-for="item in quickActions" :key="item.title" type="button" @click="$emit('navigate', item.view)">
            <span class="quick-action-icon"><el-icon><component :is="item.icon" /></el-icon></span>
            <span><strong>{{ item.title }}</strong><small>{{ item.desc }}</small></span>
            <el-icon><ArrowRight /></el-icon>
          </button>
        </div>
      </el-card>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { ArrowRight, Bell, Calendar, DataAnalysis, Location, MoreFilled, Refresh, Tickets, Wallet } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { formatDate, get, money } from '../api'

const emit = defineEmits(['navigate'])
const props = defineProps({ user: { type: Object, default: null } })
const loading = ref(false)
const trendLoading = ref(false)
const metricsData = ref({})
const trend = ref([])
const rank = ref([])
const trendDays = ref(7)
const displayName = computed(() => props.user?.displayName || '运营同学')

const metrics = computed(() => [
  { key: 'stores', label: '门店总数', value: metricsData.value.storeCount ?? '-', note: `${metricsData.value.activeStoreCount ?? 0} 家营业中`, tone: 'teal', icon: Location },
  { key: 'orders', label: '今日订单', value: metricsData.value.todayOrderCount ?? '-', note: '含待支付与已支付订单', tone: 'blue', icon: Tickets },
  { key: 'sales', label: '今日销售额', value: money(metricsData.value.todaySalesCents), note: '已支付订单金额', tone: 'orange', icon: DataAnalysis },
  { key: 'refunds', label: '待审核退款', value: metricsData.value.pendingRefundCount ?? '-', note: '请在退款审核中处理', tone: 'red', icon: Wallet }
])

const todos = computed(() => [
  { title: '待确认订单', desc: '门店需要确认并备房', count: metricsData.value.pendingOrderCount || 0, view: 'orders', tone: 'teal', icon: Tickets },
  { title: '退款审核', desc: '总部审核后进入微信退款', count: metricsData.value.pendingRefundCount || 0, view: 'refunds', tone: 'orange', icon: Wallet },
  { title: '地图位置待补充', desc: '门店没有有效经纬度', count: 0, view: 'stores', tone: 'blue', icon: Location }
])

const quickActions = [
  { title: '新增门店', desc: '维护门店基础资料', view: 'stores', icon: Location },
  { title: '调整房态', desc: '查看库存与每日余量', view: 'catalog', icon: Calendar },
  { title: '查看订单', desc: '处理入住和离店任务', view: 'orders', icon: Tickets },
  { title: '账号权限', desc: '管理角色与门店范围', view: 'users', icon: Bell }
]

function toDateOnly(date) {
  const value = new Date(date)
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
}

function shortDate(value) {
  const date = new Date(value)
  return `${date.getMonth() + 1}/${date.getDate()}`
}

async function load() {
  loading.value = true
  try {
    metricsData.value = await get('/admin/metrics')
    await Promise.all([loadTrend(), loadRank()])
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    loading.value = false
  }
}

async function loadTrend() {
  trendLoading.value = true
  try {
    const to = new Date()
    to.setDate(to.getDate() + 1)
    const from = new Date()
    from.setDate(from.getDate() - trendDays.value + 1)
    trend.value = await get(`/admin/stats/trend?from=${toDateOnly(from)}&to=${toDateOnly(to)}`)
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    trendLoading.value = false
  }
}

async function loadRank() {
  try {
    const to = new Date()
    to.setDate(to.getDate() + 1)
    const from = new Date()
    from.setDate(from.getDate() - 29)
    rank.value = await get(`/admin/stats/store-rank?from=${toDateOnly(from)}&to=${toDateOnly(to)}`)
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const trendTotals = computed(() => ({
  orders: trend.value.reduce((sum, item) => sum + item.orderCount, 0),
  sales: trend.value.reduce((sum, item) => sum + item.salesCents, 0),
  refunds: trend.value.reduce((sum, item) => sum + item.refundCents, 0)
}))
const maxOrders = computed(() => Math.max(1, ...trend.value.map((item) => item.orderCount)))
function barHeight(value) {
  return Math.max(8, Math.round((value / maxOrders.value) * 100))
}

onMounted(load)
</script>
