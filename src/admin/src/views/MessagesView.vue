<template>
  <div class="view-stack notification-page">
    <section class="page-heading">
      <div>
      <p>集中查看订单、退款、审核和门店房源提醒，处理结果会同步到控制台待办。</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="notificationState.loading" @click="load">刷新提醒</el-button>
        <el-button type="primary" @click="$emit('navigate', 'dashboard')">返回控制台</el-button>
      </div>
    </section>

    <section class="notification-summary-grid">
      <button class="notification-summary-card teal" type="button" @click="activeType = 'order'">
        <span class="notification-summary-icon"><el-icon><Tickets /></el-icon></span>
        <span><small>订单待处理</small><strong>{{ notificationState.pendingOrderCount }}</strong></span>
      </button>
      <button class="notification-summary-card orange" type="button" @click="activeType = 'refund'">
        <span class="notification-summary-icon"><el-icon><Wallet /></el-icon></span>
        <span><small>退款待审核</small><strong>{{ notificationState.pendingRefundCount }}</strong></span>
      </button>
      <button class="notification-summary-card red" type="button" @click="activeType = 'store'">
        <span class="notification-summary-icon"><el-icon><Location /></el-icon></span>
        <span><small>门店待审核</small><strong>{{ notificationState.pendingStoreReviewCount }}</strong></span>
      </button>
      <button class="notification-summary-card blue" type="button" @click="activeType = 'room'">
        <span class="notification-summary-icon"><el-icon><House /></el-icon></span>
        <span><small>房间待审核</small><strong>{{ notificationState.pendingRoomReviewCount }}</strong></span>
      </button>
      <button class="notification-summary-card purple" type="button" @click="activeType = 'review'">
        <span class="notification-summary-icon"><el-icon><CollectionTag /></el-icon></span>
        <span><small>评价待审核</small><strong>{{ notificationState.pendingReviewCount }}</strong></span>
      </button>
      <button class="notification-summary-card orange" type="button" @click="activeType = 'withdrawal'">
        <span class="notification-summary-icon"><el-icon><Wallet /></el-icon></span>
        <span><small>提现待审核</small><strong>{{ notificationState.pendingWithdrawalCount }}</strong></span>
      </button>
    </section>

    <el-card class="panel-card notification-list-card" shadow="never">
      <div class="notification-list-toolbar">
        <div>
          <strong>待处理消息</strong>
          <span class="toolbar-count">{{ filteredItems.length }} 项</span>
        </div>
        <div class="notification-filter-tabs" role="tablist">
          <button v-for="tab in tabs" :key="tab.key" type="button" :class="{ active: activeType === tab.key }" @click="activeType = tab.key">
            {{ tab.label }}<span>{{ tab.count }}</span>
          </button>
        </div>
      </div>

      <div v-loading="notificationState.loading" class="notification-page-list">
        <button v-for="item in filteredItems" :key="item.id" type="button" class="notification-page-entry" @click="$emit('navigate', item.view)">
          <span class="notification-entry-icon" :class="item.tone"><el-icon><component :is="notificationIcon(item.type)" /></el-icon></span>
          <span class="notification-entry-copy"><strong>{{ item.title }}</strong><small>{{ item.description }}</small><em>{{ formatTime(item.createdAt) }}</em></span>
          <span class="notification-page-entry-action">去处理<el-icon><ArrowRight /></el-icon></span>
        </button>
        <el-empty v-if="!filteredItems.length && !notificationState.loading" description="暂无需要处理的消息" :image-size="72" />
      </div>

      <div v-if="notificationState.generatedAt" class="notification-page-foot">
        最近同步：{{ formatTime(notificationState.generatedAt) }}
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { ArrowRight, CollectionTag, House, Location, Refresh, Tickets, Wallet } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { refreshNotifications, useAdminNotifications } from '../notifications'

defineEmits(['navigate'])

const activeType = ref('all')
const { state: notificationState } = useAdminNotifications()

const tabs = computed(() => [
  { key: 'all', label: '全部', count: notificationState.pendingCount },
  { key: 'order', label: '订单', count: notificationState.pendingOrderCount },
  { key: 'refund', label: '退款', count: notificationState.pendingRefundCount },
  { key: 'store', label: '门店审核', count: notificationState.pendingStoreReviewCount },
  { key: 'room', label: '房间审核', count: notificationState.pendingRoomReviewCount },
  { key: 'review', label: '评价审核', count: notificationState.pendingReviewCount },
  { key: 'withdrawal', label: '提现审核', count: notificationState.pendingWithdrawalCount }
])
const filteredItems = computed(() => activeType.value === 'all'
  ? notificationState.items
  : notificationState.items.filter((item) => item.type === activeType.value))

function notificationIcon(type) {
  return ({ order: Tickets, refund: Wallet, store: Location, room: House, review: CollectionTag, withdrawal: Wallet })[type] || House
}
function formatTime(value) {
  if (!value) return '刚刚'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '刚刚'
  return date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
async function load() {
  try {
    await refreshNotifications()
  } catch (error) {
    ElMessage.error(error.message || '消息刷新失败')
  }
}

onMounted(load)
</script>
