<template>
  <div class="view-stack">
    <section class="page-heading">
      <div><p>查看订单支付与核销状态，处理待核销订单。</p></div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新订单</el-button>
    </section>

    <el-card class="panel-card filter-card" shadow="never">
      <div class="filter-row">
        <el-input v-model="filters.keyword" clearable placeholder="用户昵称 / 订单号 / 门店 / 房型" :prefix-icon="Search" />
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
      <div class="table-toolbar"><div><strong>订单列表</strong><span class="toolbar-count">{{ filteredOrders.length }} 条记录</span></div><div class="toolbar-actions"><el-tag type="warning" effect="light">待核销 {{ pendingCount }}</el-tag><el-button text :icon="Download" :disabled="!selectedRows.length" @click="exportSelected">导出选中</el-button><el-button text :icon="Refresh" @click="load">刷新</el-button></div></div>
      <el-table v-loading="loading" :data="pagedOrders" class="clean-table admin-data-table order-data-table" row-key="id" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column label="用户昵称" min-width="150" show-overflow-tooltip><template #default="{ row }"><span class="table-primary-value">{{ row.userNickname || '微信用户' }}</span></template></el-table-column>
        <el-table-column label="订单号" min-width="190"><template #default="{ row }"><button class="link-button" @click="openDetail(row)">{{ row.orderNumber }}</button></template></el-table-column>
        <el-table-column label="商品总价格" width="125"><template #default="{ row }"><strong>{{ money(row.totalAmountCents) }}</strong></template></el-table-column>
        <el-table-column label="实际支付价格" width="135"><template #default="{ row }"><strong>{{ money(row.actualPaidAmountCents) }}</strong><small class="table-sub">{{ paymentLabel(row.paymentStatus) }}</small></template></el-table-column>
        <el-table-column label="状态" width="132"><template #default="{ row }"><el-tag :type="statusType(row.status)" effect="light">{{ statusLabel(row.status) }}</el-tag></template></el-table-column>
        <el-table-column label="支付时间" width="175"><template #default="{ row }">{{ formatDateTime(row.paidAt) }}</template></el-table-column>
        <el-table-column label="核销状态" width="112"><template #default="{ row }"><span class="dot-status" :class="row.verificationStatus === 'Verified' ? 'success' : 'muted'"><i></i>{{ verificationLabel(row.verificationStatus) }}</span></template></el-table-column>
        <el-table-column label="核销时间" width="175"><template #default="{ row }">{{ formatDateTime(row.verifiedAt) }}</template></el-table-column>
        <el-table-column label="退款时间" width="175"><template #default="{ row }">{{ formatDateTime(row.refundedAt) }}</template></el-table-column>
        <el-table-column label="预约时间" width="175"><template #default="{ row }">{{ formatStayDate(row.checkIn) }}</template></el-table-column>
        <el-table-column label="预约结束时间" width="175"><template #default="{ row }">{{ formatStayDate(row.checkOut) }}</template></el-table-column>
        <el-table-column label="门店ID" min-width="220" show-overflow-tooltip><template #default="{ row }"><span class="table-primary-value">{{ row.storeId }}</span><small class="table-sub">{{ row.storeName }}</small></template></el-table-column>
        <el-table-column label="备注" min-width="180" show-overflow-tooltip><template #default="{ row }"><span class="table-note">{{ row.note || '-' }}</span></template></el-table-column>
        <el-table-column label="总人数" width="90"><template #default="{ row }">{{ row.guestCount }} 人</template></el-table-column>
        <el-table-column label="订单创建时间" width="175"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column>
        <el-table-column label="操作" width="225" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="openDetail(row)">详情</el-button><el-button v-if="canVerify(row)" link type="success" @click="verifyOrder(row)">核销</el-button><el-button v-if="row.status === 'CheckedIn'" link type="primary" @click="checkOutLegacyStay(row)">离店</el-button><el-button v-if="row.status === 'PendingPayment'" link type="danger" @click="cancelOrder(row)">取消</el-button></template></el-table-column>
      </el-table>
      <div class="list-footer">
        <span>显示第 {{ filteredOrders.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredOrders.length) }} 条记录，共 {{ filteredOrders.length }} 条记录</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredOrders.length" background />
      </div>
    </el-card>

    <el-drawer v-model="drawer.visible" size="720px" :with-header="false">
      <div class="detail-topbar">
        <span class="eyebrow">ORDER DETAIL</span>
        <el-button text :icon="Close" aria-label="关闭订单详情" @click="drawer.visible = false" />
      </div>

      <div v-if="drawer.data" class="order-detail-drawer">
        <div class="detail-order-head">
          <div class="detail-head-main">
            <div class="detail-title-line">
              <h3>{{ drawer.data.orderNumber }}</h3>
              <el-button link :icon="CopyDocument" @click="copyOrderNumber">复制</el-button>
            </div>
            <div class="detail-head-meta">
              <span>{{ drawer.data.userNickname || '微信用户' }}</span>
              <i></i>
              <span>{{ drawer.data.storeName || '未关联门店' }}</span>
              <i></i>
              <span>{{ drawer.data.roomTypeName || '未关联房型' }}</span>
            </div>
          </div>
          <div class="detail-order-tags">
            <el-tag :type="statusType(drawer.data.status)" effect="light">{{ statusLabel(drawer.data.status) }}</el-tag>
            <el-tag :type="verificationType(drawer.data.verificationStatus)" effect="light">{{ verificationLabel(drawer.data.verificationStatus) }}</el-tag>
          </div>
        </div>

        <div class="detail-stat-row">
          <div class="detail-stat">
            <small>商品总价格</small>
            <strong>{{ money(drawer.data.totalAmountCents) }}</strong>
          </div>
          <div class="detail-stat">
            <small>实际支付价格</small>
            <strong class="price-text">{{ money(drawer.data.actualPaidAmountCents) }}</strong>
            <em>{{ paymentLabel(drawer.data.paymentStatus) }}</em>
          </div>
          <div class="detail-stat">
            <small>入住人数</small>
            <strong>{{ drawer.data.guestCount }} 人</strong>
            <em>{{ drawer.data.quantity }} 间 / 床位</em>
          </div>
        </div>

        <section class="detail-section">
          <div class="detail-section-head">
            <h4>订单信息</h4>
            <span>与订单列表字段保持一致</span>
          </div>
          <div class="detail-info-grid">
            <div class="detail-info-item"><small>用户昵称</small><strong>{{ drawer.data.userNickname || '微信用户' }}</strong></div>
            <div class="detail-info-item"><small>订单状态</small><strong>{{ statusLabel(drawer.data.status) }}</strong></div>
            <div class="detail-info-item"><small>支付状态</small><strong>{{ paymentLabel(drawer.data.paymentStatus) }}</strong></div>
            <div class="detail-info-item"><small>支付时间</small><strong>{{ formatDateTime(drawer.data.paidAt) }}</strong></div>
            <div class="detail-info-item"><small>核销状态</small><strong><span class="dot-status" :class="drawer.data.verificationStatus === 'Verified' ? 'success' : 'muted'"><i></i>{{ verificationLabel(drawer.data.verificationStatus) }}</span></strong></div>
            <div class="detail-info-item"><small>核销时间</small><strong>{{ formatDateTime(drawer.data.verifiedAt) }}</strong></div>
            <div class="detail-info-item"><small>退款时间</small><strong>{{ formatDateTime(drawer.data.refundedAt) }}</strong></div>
            <div class="detail-info-item"><small>预约时间</small><strong>{{ formatStayDate(drawer.data.checkIn) }}</strong></div>
            <div class="detail-info-item"><small>预约结束时间</small><strong>{{ formatStayDate(drawer.data.checkOut) }}</strong></div>
            <div class="detail-info-item"><small>订单创建时间</small><strong>{{ formatDateTime(drawer.data.createdAt) }}</strong></div>
            <div class="detail-info-item"><small>门店</small><strong>{{ drawer.data.storeName || '-' }}</strong><span class="detail-info-sub">{{ drawer.data.storeId }}</span></div>
            <div class="detail-info-item"><small>房型</small><strong>{{ drawer.data.roomTypeName || '-' }}</strong></div>
            <div class="detail-info-item detail-info-wide"><small>备注</small><strong class="detail-note-value">{{ drawer.data.note || '暂无备注' }}</strong></div>
          </div>
        </section>

        <section class="detail-section">
          <div class="detail-section-head">
            <h4>入住人信息</h4>
            <span>{{ guestList.length ? `${guestList.length} 位` : '未登记' }}</span>
          </div>
          <div v-if="guestList.length" class="guest-list">
            <div v-for="(guest, index) in guestList" :key="`${guest.name}-${index}`" class="guest-card">
              <div class="guest-avatar">{{ (guest.name || '客').slice(0, 1) }}</div>
              <div class="guest-main">
                <strong>{{ guest.name || '未填写姓名' }}</strong>
                <span>{{ guest.identity || '未填写证件号' }}</span>
              </div>
              <div class="guest-phone">{{ guest.phone || '未填写手机号' }}</div>
            </div>
          </div>
          <div v-else class="detail-empty">暂无入住人信息</div>
        </section>

        <section class="detail-section">
          <div class="detail-section-head">
            <h4>支付流水</h4>
            <span>{{ drawer.data.payments?.length || 0 }} 笔</span>
          </div>
          <el-table v-if="drawer.data.payments?.length" :data="drawer.data.payments" size="small" class="clean-table">
            <el-table-column prop="transactionNumber" label="流水号" min-width="170" show-overflow-tooltip />
            <el-table-column label="金额" width="90"><template #default="{ row }">{{ money(row.amountCents) }}</template></el-table-column>
            <el-table-column label="状态" width="90"><template #default="{ row }">{{ paymentLabel(row.status) }}</template></el-table-column>
            <el-table-column label="支付时间" width="170"><template #default="{ row }">{{ formatDateTime(row.paidAt || row.createdAt) }}</template></el-table-column>
          </el-table>
          <div v-else class="detail-empty">暂无支付流水</div>
        </section>

        <section class="detail-section">
          <div class="detail-section-head">
            <h4>退款记录</h4>
            <span>{{ drawer.data.refunds?.length || 0 }} 笔</span>
          </div>
          <el-table v-if="drawer.data.refunds?.length" :data="drawer.data.refunds" size="small" class="clean-table">
            <el-table-column prop="refundNumber" label="退款单号" min-width="160" show-overflow-tooltip />
            <el-table-column label="金额" width="90"><template #default="{ row }">{{ money(row.amountCents) }}</template></el-table-column>
            <el-table-column label="状态" width="90"><template #default="{ row }">{{ row.status }}</template></el-table-column>
            <el-table-column prop="reason" label="退款原因" min-width="140" show-overflow-tooltip />
            <el-table-column label="完成时间" width="170"><template #default="{ row }">{{ formatDateTime(row.completedAt || row.reviewedAt || row.createdAt) }}</template></el-table-column>
          </el-table>
          <div v-else class="detail-empty">暂无退款记录</div>
        </section>
      </div>
      <el-skeleton v-else :rows="10" animated />

      <template #footer>
        <div class="detail-drawer-footer">
          <el-button @click="drawer.visible = false">关闭</el-button>
          <div class="detail-footer-actions">
            <el-button v-if="drawer.data?.status === 'PendingPayment'" type="danger" plain @click="cancelOrder(drawer.data)">取消订单</el-button>
            <el-button v-if="drawer.data?.status === 'CheckedIn'" type="primary" plain @click="checkOutLegacyStay(drawer.data)">办理离店</el-button>
            <el-button v-if="drawer.data && canVerify(drawer.data)" type="success" @click="verifyOrder(drawer.data)">核销订单</el-button>
          </div>
        </div>
      </template>
    </el-drawer>

    <el-dialog v-model="verifyDialog.visible" title="订单核销" width="460px" align-center>
      <div v-if="verifyDialog.data" class="verification-dialog-content">
        <div class="verification-order-number">{{ verifyDialog.data.orderNumber }}</div>
        <img class="verification-qr-image" :src="getVerificationQrUrl(verifyDialog.data.verificationCode)" alt="订单核销二维码" />
        <div class="verification-dialog-hint">请使用商户端扫码核销，或手动完成核销。</div>
      </div>
      <el-skeleton v-else :rows="4" animated />
      <template #footer>
        <el-button @click="verifyDialog.visible = false">关闭</el-button>
        <el-button type="success" :loading="verifying" :disabled="!verifyDialog.data" @click="manualVerify">手动核销</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Download, Refresh, Search, Close, CopyDocument } from '@element-plus/icons-vue'
import { get, getVerificationQrUrl, money, post } from '../api'
import { requestNotificationRefresh } from '../notifications'

const loading = ref(false)
const orders = ref([])
const stores = ref([])
const filters = reactive({ keyword: '', status: '', storeId: '' })
const statusTab = ref('all')
const selectedRows = ref([])
const page = ref(1)
const pageSize = ref(10)
const drawer = reactive({ visible: false, data: null })
const verifyDialog = reactive({ visible: false, data: null })
const verifying = ref(false)
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
  { value: 'PaidPendingVerification', label: '已支付待核销' },
  { value: 'CheckedIn', label: '已入住' },
  { value: 'Completed', label: '已完成' },
  { value: 'Cancelled', label: '已取消' },
  { value: 'Refunding', label: '退款中' },
  { value: 'Refunded', label: '已退款' },
  { value: 'RefundFailed', label: '退款失败' }
]
const filteredOrders = computed(() => {
  const keyword = filters.keyword.trim().toLowerCase()
  return orders.value.filter((row) => matchesTab(row, statusTab.value) && (!filters.status || row.status === filters.status) && (!filters.storeId || row.storeId === filters.storeId) && (!keyword || `${row.userNickname} ${row.orderNumber} ${row.storeName} ${row.roomTypeName}`.toLowerCase().includes(keyword)))
})
const pagedOrders = computed(() => filteredOrders.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
const pendingCount = computed(() => orders.value.filter((row) => row.status === 'PaidPendingVerification').length)
const guestList = computed(() => parseGuests(drawer.data?.guestSnapshotJson))
watch([statusTab, () => filters.status, () => filters.storeId, () => filters.keyword, pageSize], () => { page.value = 1 })
function matchesTab(row, key) {
  if (key === 'all') return true
  if (key === 'closed') return row.status === 'Cancelled' || row.status === 'RefundFailed'
  if (key === 'cancelled') return row.status === 'Cancelled'
  if (key === 'pendingPayment') return row.status === 'PendingPayment'
  if (key === 'pendingUse') return ['PaidPendingVerification', 'CheckedIn'].includes(row.status)
  if (key === 'completed') return row.status === 'Completed'
  if (key === 'refunded') return row.status === 'Refunded' || row.status === 'Refunding'
  return true
}
function countForTab(key) { return orders.value.filter((row) => matchesTab(row, key)).length }
function statusLabel(value) { return ({ ...Object.fromEntries(statusOptions.map((item) => [item.value, item.label])), PaidPendingConfirmation: '已支付待核销', ConfirmedPendingCheckIn: '已支付待核销', CheckedIn: '已入住', Completed: '已完成', Cancelled: '已取消', Refunded: '已退款' })[value] || value }
function statusType(value) { return ({ PendingPayment: 'warning', PaidPendingVerification: 'warning', PaidPendingConfirmation: 'warning', ConfirmedPendingCheckIn: 'warning', CheckedIn: 'success', Completed: 'success', Cancelled: 'info', Refunding: 'warning', Refunded: 'success', RefundFailed: 'danger' })[value] || 'info' }
function paymentLabel(value) { return ({ Paid: '已支付', Unpaid: '未支付', Refunded: '已退款' })[value] || value }
function verificationLabel(value) { return value === 'Verified' ? '已核销' : '未核销' }
function verificationType(value) { return value === 'Verified' ? 'success' : 'info' }
function parseUtcDate(value) {
  if (!value) return null
  const text = String(value).trim()
  // MySQL 返回的 UTC 时间不带时区标记，直接解析会按本地时间处理，导致显示差 8 小时。
  const normalized = /(?:Z|[+-]\d{2}:?\d{2})$/.test(text) ? text : `${text}Z`
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? null : date
}
function formatDateTime(value) {
  const date = parseUtcDate(value)
  return date ? date.toLocaleString('zh-CN', { hour12: false }) : '-'
}
function formatStayDate(value) {
  if (!value) return '-'
  const text = String(value)
  const date = text.slice(0, 10)
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? date : text
}
function parseGuests(value) {
  if (!value) return []
  let list = value
  if (typeof value === 'string') {
    try { list = JSON.parse(value) } catch { return [] }
  }
  if (!Array.isArray(list)) return []
  return list
    .filter((item) => item && (item.name || item.identity || item.phone))
    .map((item) => ({
      name: String(item.name || '').trim(),
      identity: String(item.identity || item.idNumber || '').trim(),
      phone: String(item.phone || item.phoneNumber || '').trim()
    }))
}
function copyOrderNumber() {
  const text = drawer.data?.orderNumber
  if (!text) return
  if (!navigator.clipboard?.writeText) {
    ElMessage.warning('当前浏览器不支持自动复制，请手动选择订单号')
    return
  }
  navigator.clipboard.writeText(text)
    .then(() => ElMessage.success('订单号已复制'))
    .catch(() => ElMessage.warning('复制失败，请手动选择订单号'))
}
function canVerify(row) {
  return ['PaidPendingVerification', 'PaidPendingConfirmation', 'ConfirmedPendingCheckIn'].includes(row.status) && row.paymentStatus === 'Paid' && row.verificationStatus !== 'Verified'
}
function exportSelected() {
  const rows = selectedRows.value
  const header = ['用户昵称', '订单号', '商品总价格', '实际支付价格', '状态', '支付时间', '核销状态', '核销时间', '退款时间', '预约时间', '预约结束时间', '门店ID', '备注', '总人数', '订单创建时间']
  const body = rows.map((row) => [
    row.userNickname || '微信用户',
    row.orderNumber,
    money(row.totalAmountCents),
    money(row.actualPaidAmountCents),
    statusLabel(row.status),
    formatDateTime(row.paidAt),
    verificationLabel(row.verificationStatus),
    formatDateTime(row.verifiedAt),
    formatDateTime(row.refundedAt),
    formatStayDate(row.checkIn),
    formatStayDate(row.checkOut),
    row.storeId,
    row.note || '',
    row.guestCount,
    formatDateTime(row.createdAt)
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
async function verifyOrder(row) {
  verifyDialog.visible = true
  verifyDialog.data = null
  try {
    const order = await get(`/admin/orders/${row.id}`)
    if (!order.verificationCode) throw new Error('订单核销码不存在，请确认支付状态后刷新')
    verifyDialog.data = order
  } catch (error) {
    verifyDialog.visible = false
    ElMessage.error(error.message || '核销码加载失败')
  }
}
async function manualVerify() {
  const order = verifyDialog.data
  if (!order || verifying.value) return
  try {
    await ElMessageBox.confirm(`确认手动核销订单“${order.orderNumber}”吗？`, '手动核销', { type: 'warning' })
    verifying.value = true
    await post(`/admin/orders/${order.id}/verify`)
    ElMessage.success('订单已核销并完成')
    requestNotificationRefresh()
    verifyDialog.visible = false
    await load()
    if (drawer.visible && drawer.data?.id === order.id) drawer.data = await get(`/admin/orders/${order.id}`)
  } catch (error) { if (error !== 'cancel') ElMessage.error(error.message || '核销失败') } finally { verifying.value = false }
}
async function checkOutLegacyStay(row) { await runAction(row, '办理离店', `/admin/orders/${row.id}/check-out`) }
async function cancelOrder(row) { await runAction(row, '取消订单', `/admin/orders/${row.id}/cancel?reason=后台运营取消`) }
async function runAction(row, label, path, body) {
  try {
    await ElMessageBox.confirm(`确定要${label}“${row.orderNumber}”吗？`, '确认操作', { type: label === '取消订单' ? 'warning' : 'info' })
    await post(path, body)
    ElMessage.success(`${label}成功`)
    requestNotificationRefresh()
    await load()
    if (drawer.visible && drawer.data?.id === row.id) drawer.data = await get(`/admin/orders/${row.id}`)
  } catch (error) { if (error !== 'cancel') ElMessage.error(error.message || '操作失败') }
}
onMounted(load)
</script>

<style scoped>
.detail-order-tags {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 6px;
}

.detail-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: -4px 0 8px;
}

.detail-topbar .el-button {
  color: var(--admin-muted);
}

.detail-order-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 4px 0 18px;
  border-bottom: 1px solid var(--admin-border);
}

.detail-head-main {
  min-width: 0;
}

.detail-title-line {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
}

.detail-title-line h3 {
  margin: 0;
  color: var(--admin-text);
  font-size: 21px;
  line-height: 1.3;
  overflow-wrap: anywhere;
}

.detail-title-line .el-button {
  flex: none;
  color: var(--admin-muted);
  font-size: 12px;
}

.detail-head-meta {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 10px;
  color: var(--admin-muted);
  font-size: 12px;
}

.detail-head-meta i {
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: #b8c9c9;
}

.detail-stat-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 16px;
}

.detail-stat {
  padding: 13px 14px;
  border: 1px solid var(--admin-border);
  border-radius: 9px;
  background: linear-gradient(180deg, #fff 0%, var(--admin-surface-soft) 100%);
}

.detail-stat small {
  display: block;
  color: var(--admin-muted);
  font-size: 11px;
}

.detail-stat strong {
  display: block;
  margin-top: 6px;
  color: var(--admin-text);
  font-size: 18px;
}

.detail-stat em {
  display: block;
  margin-top: 4px;
  color: var(--admin-muted);
  font-size: 11px;
  font-style: normal;
}

.detail-section {
  margin-top: 22px;
}

.detail-section-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 10px;
}

.detail-section-head h4 {
  margin: 0;
  color: var(--admin-text);
  font-size: 13px;
}

.detail-section-head span {
  color: var(--admin-muted);
  font-size: 11px;
}

.detail-info-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1px;
  overflow: hidden;
  border: 1px solid var(--admin-border);
  border-radius: 9px;
  background: var(--admin-border);
}

.detail-info-item {
  min-width: 0;
  padding: 11px 13px;
  background: var(--admin-surface);
}

.detail-info-item small {
  display: block;
  color: var(--admin-muted);
  font-size: 11px;
}

.detail-info-item strong {
  display: block;
  margin-top: 4px;
  color: var(--admin-text);
  font-size: 13px;
  font-weight: 600;
  overflow-wrap: anywhere;
}

.detail-info-sub {
  display: block;
  margin-top: 3px;
  color: #93a5a8;
  font-family: Consolas, "SFMono-Regular", monospace;
  font-size: 10px;
  overflow-wrap: anywhere;
}

.detail-info-wide {
  grid-column: 1 / -1;
}

.detail-note-value {
  font-weight: 500 !important;
  line-height: 1.6;
  white-space: pre-wrap;
}

.guest-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.guest-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 13px;
  border: 1px solid var(--admin-border);
  border-radius: 9px;
  background: var(--admin-surface);
}

.guest-avatar {
  display: grid;
  width: 34px;
  height: 34px;
  flex: none;
  place-items: center;
  border-radius: 50%;
  background: var(--admin-primary-soft);
  color: var(--admin-primary-dark);
  font-weight: 700;
}

.guest-main {
  flex: 1;
  min-width: 0;
}

.guest-main strong {
  display: block;
  color: var(--admin-text);
  font-size: 13px;
}

.guest-main span {
  display: block;
  margin-top: 3px;
  color: var(--admin-muted);
  font-size: 11px;
  overflow-wrap: anywhere;
}

.guest-phone {
  flex: none;
  color: var(--admin-text);
  font-size: 12px;
  font-variant-numeric: tabular-nums;
}

.detail-empty {
  padding: 16px;
  border: 1px dashed var(--admin-border);
  border-radius: 9px;
  background: var(--admin-surface-soft);
  color: var(--admin-muted);
  font-size: 12px;
  text-align: center;
}

.detail-drawer-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.detail-footer-actions {
  display: flex;
  gap: 8px;
}

@media (max-width: 640px) {
  .detail-order-head {
    flex-direction: column;
  }

  .detail-order-tags {
    justify-content: flex-start;
  }

  .detail-stat-row,
  .detail-info-grid {
    grid-template-columns: 1fr;
  }

  .guest-card {
    flex-wrap: wrap;
  }

  .guest-phone {
    width: 100%;
    padding-left: 46px;
  }

  .detail-drawer-footer {
    align-items: stretch;
    flex-direction: column-reverse;
  }

  .detail-footer-actions {
    display: grid;
    grid-template-columns: 1fr;
  }
}

.verification-dialog-content {
  display: flex;
  align-items: center;
  flex-direction: column;
  gap: 14px;
}

.verification-order-number {
  color: var(--admin-text);
  font-size: 14px;
  font-weight: 650;
}

.verification-qr-image {
  display: block;
  width: 240px;
  height: 240px;
  object-fit: contain;
  border: 1px solid var(--admin-border);
  border-radius: 6px;
  background: #fff;
}

.verification-dialog-hint {
  color: var(--admin-muted);
  font-size: 13px;
  text-align: center;
}
</style>
