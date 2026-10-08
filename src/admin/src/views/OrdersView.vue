<template>
  <div class="view-stack">
    <section class="page-heading">
      <div><span class="eyebrow">ORDER OPERATIONS</span><h2>订单运营</h2><p>统一处理订单确认、入住核验、离店和异常取消。</p></div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新订单</el-button>
    </section>

    <el-card class="panel-card filter-card" shadow="never">
      <div class="filter-row">
        <el-input v-model="filters.keyword" clearable placeholder="订单号 / 门店 / 房型" :prefix-icon="Search" />
        <el-select v-model="filters.status" clearable placeholder="订单状态" @change="load"><el-option label="全部状态" value="" /><el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select>
        <el-select v-model="filters.storeId" clearable placeholder="全部门店"><el-option label="全部门店" value="" /><el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" /></el-select>
        <el-button type="primary" @click="load">查询</el-button><el-button text @click="reset">重置</el-button>
      </div>
    </el-card>

    <el-card class="panel-card" shadow="never">
      <div class="status-tabs order-tabs">
        <button v-for="tab in orderTabs" :key="tab.key" type="button" role="tab" class="status-tab" :class="{ active: statusTab === tab.key }" :aria-selected="statusTab === tab.key" @click="statusTab = tab.key">
          {{ tab.label }} <span>{{ countForTab(tab.key) }}</span>
        </button>
      </div>
      <div class="table-toolbar"><div><strong>订单列表</strong><span class="toolbar-count">{{ filteredOrders.length }} 条记录</span></div><div class="toolbar-actions"><el-tag type="warning" effect="light">待确认 {{ pendingCount }}</el-tag><el-button text :icon="Download" :disabled="!selectedRows.length" @click="exportSelected">导出选中</el-button><el-button text :icon="Refresh" @click="load">刷新</el-button></div></div>
      <el-table v-loading="loading" :data="pagedOrders" class="clean-table admin-data-table" row-key="id" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column prop="id" label="ID" width="74" />
        <el-table-column label="用户 ID" width="150"><template #default="{ row }">{{ row.wxUserId || row.id.slice(0, 12) }}</template></el-table-column>
        <el-table-column label="订单号" min-width="180"><template #default="{ row }"><button class="link-button" @click="openDetail(row)">{{ row.orderNumber }}</button><small class="table-sub">{{ formatDate(row.createdAt) }} 创建</small></template></el-table-column>
        <el-table-column label="门店 / 房型" min-width="220"><template #default="{ row }"><strong>{{ row.storeName }}</strong><small class="table-sub">{{ row.roomTypeName }}</small></template></el-table-column>
        <el-table-column label="入住日期" width="178"><template #default="{ row }">{{ formatDate(row.checkIn) }} - {{ formatDate(row.checkOut) }}</template></el-table-column>
        <el-table-column label="商品总价" width="112"><template #default="{ row }"><strong>{{ money(row.totalAmountCents) }}</strong><small class="table-sub">{{ row.quantity }} 间/床</small></template></el-table-column>
        <el-table-column label="实际支付" width="112"><template #default="{ row }"><strong>{{ money(row.paymentStatus === 'Paid' || row.paymentStatus === 'Refunded' ? row.totalAmountCents : 0) }}</strong></template></el-table-column>
        <el-table-column label="支付" width="100"><template #default="{ row }"><el-tag :type="row.paymentStatus === 'Paid' ? 'success' : row.paymentStatus === 'Refunded' ? 'info' : 'warning'" effect="light">{{ paymentLabel(row.paymentStatus) }}</el-tag></template></el-table-column>
        <el-table-column label="订单状态" width="136"><template #default="{ row }"><el-tag :type="statusType(row.status)" effect="light">{{ statusLabel(row.status) }}</el-tag></template></el-table-column>
        <el-table-column label="支付时间" width="168"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column>
        <el-table-column label="核销状态" width="110"><template #default="{ row }"><span class="dot-status" :class="row.status === 'Completed' ? 'success' : 'muted'"><i></i>{{ row.status === 'Completed' ? '已核销' : '未核销' }}</span></template></el-table-column>
        <el-table-column label="操作" width="260" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openDetail(row)">详情</el-button><el-button v-if="row.status === 'PaidPendingConfirmation'" link type="primary" @click="confirmOrder(row)">确认</el-button><el-button v-if="row.status === 'ConfirmedPendingCheckIn'" link type="primary" @click="checkIn(row)">入住</el-button><el-button v-if="row.status === 'CheckedIn'" link type="primary" @click="checkOut(row)">离店</el-button><el-button v-if="row.status === 'PendingPayment'" link type="danger" @click="cancelOrder(row)">取消</el-button></template></el-table-column>
      </el-table>
      <div class="list-footer">
        <span>显示第 {{ filteredOrders.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredOrders.length) }} 条记录，共 {{ filteredOrders.length }} 条记录</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredOrders.length" background />
      </div>
    </el-card>

    <el-drawer v-model="drawer.visible" title="订单详情" size="620px">
      <div v-if="drawer.data" class="order-detail-drawer">
        <div class="detail-order-head"><div><span class="eyebrow">ORDER DETAIL</span><h3>{{ drawer.data.orderNumber }}</h3></div><el-tag :type="statusType(drawer.data.status)" effect="light">{{ statusLabel(drawer.data.status) }}</el-tag></div>
        <div class="detail-summary-grid"><div><small>门店</small><strong>{{ drawer.data.storeName }}</strong></div><div><small>房型</small><strong>{{ drawer.data.roomTypeName }}</strong></div><div><small>入住日期</small><strong>{{ formatDate(drawer.data.checkIn) }}</strong></div><div><small>离店日期</small><strong>{{ formatDate(drawer.data.checkOut) }}</strong></div><div><small>入住人数</small><strong>{{ drawer.data.guestCount }} 人</strong></div><div><small>订单金额</small><strong class="price-text">{{ money(drawer.data.totalAmountCents) }}</strong></div></div>
        <el-divider />
        <h4>入住人信息</h4><pre class="json-box">{{ prettyJson(drawer.data.guestSnapshotJson) }}</pre>
        <h4>房间/床位分配</h4><div class="assignment-box">{{ prettyJson(drawer.data.assignedResourcesJson) || '尚未分配具体资源' }}</div>
        <h4>支付流水</h4><el-table :data="drawer.data.payments" size="small" class="clean-table"><el-table-column prop="transactionNumber" label="流水号" min-width="170" /><el-table-column label="金额" width="90"><template #default="{ row }">{{ money(row.amountCents) }}</template></el-table-column><el-table-column prop="status" label="状态" width="80" /></el-table>
        <h4>退款记录</h4><el-table v-if="drawer.data.refunds?.length" :data="drawer.data.refunds" size="small" class="clean-table"><el-table-column prop="refundNumber" label="退款单号" min-width="160" /><el-table-column label="金额" width="90"><template #default="{ row }">{{ money(row.amountCents) }}</template></el-table-column><el-table-column prop="status" label="状态" width="90" /></el-table><span v-else class="muted-text">暂无退款记录</span>
      </div>
      <el-skeleton v-else :rows="8" animated />
    </el-drawer>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Download, Refresh, Search } from '@element-plus/icons-vue'
import { formatDate, get, money, post } from '../api'

const loading = ref(false)
const orders = ref([])
const stores = ref([])
const filters = reactive({ keyword: '', status: '', storeId: '' })
const statusTab = ref('all')
const selectedRows = ref([])
const page = ref(1)
const pageSize = ref(10)
const drawer = reactive({ visible: false, data: null })
const orderTabs = [
  { key: 'all', label: '全部' },
  { key: 'closed', label: '订单关闭' },
  { key: 'cancelled', label: '已取消' },
  { key: 'pendingPayment', label: '待支付' },
  { key: 'pendingUse', label: '待使用' },
  { key: 'completed', label: '已完成' },
  { key: 'refunded', label: '已退款' }
]
const statusOptions = [
  { value: 'PendingPayment', label: '待支付' },
  { value: 'PaidPendingConfirmation', label: '已支付待确认' },
  { value: 'ConfirmedPendingCheckIn', label: '已确认待入住' },
  { value: 'CheckedIn', label: '已入住' },
  { value: 'Completed', label: '已完成' },
  { value: 'Cancelled', label: '已取消' },
  { value: 'Refunding', label: '退款中' },
  { value: 'Refunded', label: '已退款' },
  { value: 'RefundFailed', label: '退款失败' }
]
const filteredOrders = computed(() => {
  const keyword = filters.keyword.trim().toLowerCase()
  return orders.value.filter((row) => matchesTab(row, statusTab.value) && (!filters.status || row.status === filters.status) && (!filters.storeId || row.storeId === filters.storeId) && (!keyword || `${row.orderNumber} ${row.storeName} ${row.roomTypeName}`.toLowerCase().includes(keyword)))
})
const pagedOrders = computed(() => filteredOrders.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
const pendingCount = computed(() => orders.value.filter((row) => row.status === 'PaidPendingConfirmation').length)
watch([statusTab, () => filters.status, () => filters.storeId, () => filters.keyword, pageSize], () => { page.value = 1 })
function matchesTab(row, key) {
  if (key === 'all') return true
  if (key === 'closed') return row.status === 'Cancelled' || row.status === 'RefundFailed'
  if (key === 'cancelled') return row.status === 'Cancelled'
  if (key === 'pendingPayment') return row.status === 'PendingPayment'
  if (key === 'pendingUse') return ['PaidPendingConfirmation', 'ConfirmedPendingCheckIn', 'CheckedIn'].includes(row.status)
  if (key === 'completed') return row.status === 'Completed'
  if (key === 'refunded') return row.status === 'Refunded' || row.status === 'Refunding'
  return true
}
function countForTab(key) { return orders.value.filter((row) => matchesTab(row, key)).length }
function statusLabel(value) { return ({ ...Object.fromEntries(statusOptions.map((item) => [item.value, item.label])), PaidPendingConfirmation: '待使用', ConfirmedPendingCheckIn: '待使用', CheckedIn: '待使用', Cancelled: '已取消', Refunded: '已退款' })[value] || value }
function statusType(value) { return ({ PendingPayment: 'warning', PaidPendingConfirmation: 'warning', ConfirmedPendingCheckIn: 'primary', CheckedIn: 'success', Completed: 'info', Cancelled: 'info', Refunding: 'warning', Refunded: 'success', RefundFailed: 'danger' })[value] || 'info' }
function paymentLabel(value) { return ({ Paid: '已支付', Unpaid: '未支付', Refunded: '已退款' })[value] || value }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-' }
function prettyJson(value) { if (!value) return ''; try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value } }
function exportSelected() {
  const rows = selectedRows.value
  const header = ['订单号', '门店', '房型', '入住日期', '离店日期', '商品总价', '支付状态', '订单状态']
  const body = rows.map((row) => [
    row.orderNumber,
    row.storeName,
    row.roomTypeName,
    formatDate(row.checkIn),
    formatDate(row.checkOut),
    money(row.totalAmountCents),
    paymentLabel(row.paymentStatus),
    statusLabel(row.status)
  ].map((value) => JSON.stringify(value ?? '')).join(',')).join('\n')
  const blob = new Blob([`\ufeff${header.join(',')}\n${body}`], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = '订单列表.csv'
  link.click()
  URL.revokeObjectURL(url)
  ElMessage.success(`已导出 ${rows.length} 条订单`)
}
async function load() {
  loading.value = true
  try { const [data, storeData] = await Promise.all([get(`/admin/orders${filters.status ? `?status=${filters.status}` : ''}`), get('/admin/stores')]); orders.value = data; stores.value = storeData } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
}
function reset() { Object.assign(filters, { keyword: '', status: '', storeId: '' }); load() }
async function openDetail(row) {
  drawer.visible = true
  drawer.data = null
  try { drawer.data = await get(`/admin/orders/${row.id}`) } catch (error) { drawer.visible = false; ElMessage.error(error.message) }
}
async function confirmOrder(row) { await runAction(row, '确认订单', `/admin/orders/${row.id}/confirm`) }
async function checkIn(row) { await runAction(row, '办理入住', `/admin/orders/${row.id}/check-in`, { resourceCodes: [], note: '后台工作台办理入住' }) }
async function checkOut(row) { await runAction(row, '办理离店', `/admin/orders/${row.id}/check-out`) }
async function cancelOrder(row) { await runAction(row, '取消订单', `/admin/orders/${row.id}/cancel?reason=后台运营取消`) }
async function runAction(row, label, path, body) {
  try {
    await ElMessageBox.confirm(`确定要${label}“${row.orderNumber}”吗？`, '确认操作', { type: label === '取消订单' ? 'warning' : 'info' })
    await post(path, body)
    ElMessage.success(`${label}成功`)
    await load()
    if (drawer.visible && drawer.data?.id === row.id) drawer.data = await get(`/admin/orders/${row.id}`)
  } catch (error) { if (error !== 'cancel') ElMessage.error(error.message || '操作失败') }
}
onMounted(load)
</script>
