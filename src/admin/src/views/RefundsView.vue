<template>
  <div class="view-stack">
    <section class="page-heading">
      <div><span class="eyebrow">REFUND CONTROL</span><h2>退款审核</h2><p>审核用户退款申请，保留完整的退款原因和处理流水。</p></div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新申请</el-button>
    </section>
    <el-card class="panel-card filter-card" shadow="never"><div class="filter-row"><el-select v-model="status" clearable placeholder="退款状态" @change="load"><el-option label="全部状态" value="" /><el-option label="待审核" value="PendingReview" /><el-option label="已退款" value="Refunded" /><el-option label="已驳回" value="Rejected" /></el-select><el-button type="primary" @click="load">查询</el-button></div></el-card>
    <el-card class="panel-card" shadow="never">
      <div class="table-toolbar"><div><strong>退款申请</strong><span class="toolbar-count">{{ refunds.length }} 条记录</span></div><el-tag type="warning" effect="light">待审核 {{ pendingCount }}</el-tag></div>
      <el-table v-loading="loading" :data="pagedRefunds" class="clean-table admin-data-table">
        <el-table-column label="退款单号" min-width="180"><template #default="{ row }"><strong>{{ row.refundNumber }}</strong><small class="table-sub">{{ formatDate(row.createdAt) }} 申请</small></template></el-table-column>
        <el-table-column label="原订单" min-width="180"><template #default="{ row }"><span>{{ row.orderNumber }}</span><small class="table-sub">{{ row.roomTypeName }}</small></template></el-table-column>
        <el-table-column prop="storeName" label="门店" min-width="170" />
        <el-table-column label="申请金额" width="120"><template #default="{ row }"><strong class="price-text">{{ money(row.amountCents) }}</strong></template></el-table-column>
        <el-table-column prop="reason" label="退款原因" min-width="160" show-overflow-tooltip />
        <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag :type="refundType(row.status)" effect="light">{{ refundLabel(row.status) }}</el-tag></template></el-table-column>
        <el-table-column label="操作" width="180" fixed="right"><template #default="{ row }"><el-button v-if="row.status === 'PendingReview'" type="primary" link @click="review(row, true)">通过</el-button><el-button v-if="row.status === 'PendingReview'" type="danger" link @click="review(row, false)">驳回</el-button><el-button link type="primary" @click="detail = row">查看</el-button></template></el-table-column>
      </el-table>
      <div class="list-footer"><span>共 {{ refunds.length }} 条记录</span><el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="refunds.length" background /></div>
    </el-card>
    <el-dialog v-model="detailVisible" title="退款申请详情" width="520px"><div v-if="detail" class="refund-detail"><div class="detail-summary-grid"><div><small>退款单号</small><strong>{{ detail.refundNumber }}</strong></div><div><small>原订单</small><strong>{{ detail.orderNumber }}</strong></div><div><small>申请金额</small><strong class="price-text">{{ money(detail.amountCents) }}</strong></div><div><small>申请时间</small><strong>{{ formatDate(detail.createdAt) }}</strong></div></div><el-divider /><div class="reason-box"><small>退款原因</small><p>{{ detail.reason }}</p></div><div v-if="detail.reviewComment" class="reason-box"><small>审核备注</small><p>{{ detail.reviewComment }}</p></div></div></el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { formatDate, get, money, post } from '../api'

const refunds = ref([])
const loading = ref(false)
const status = ref('')
const detail = ref(null)
const page = ref(1)
const pageSize = ref(10)
const detailVisible = computed({ get: () => Boolean(detail.value), set: (value) => { if (!value) detail.value = null } })
const pendingCount = computed(() => refunds.value.filter((item) => item.status === 'PendingReview').length)
const pagedRefunds = computed(() => refunds.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([status, pageSize], () => { page.value = 1 })
function refundLabel(value) { return ({ PendingReview: '待审核', Approved: '已通过', Rejected: '已驳回', Refunded: '已退款', RefundFailed: '退款失败' })[value] || value }
function refundType(value) { return ({ PendingReview: 'warning', Approved: 'primary', Rejected: 'info', Refunded: 'success', RefundFailed: 'danger' })[value] || 'info' }
async function load() { loading.value = true; try { refunds.value = await get(`/admin/refunds${status.value ? `?status=${status.value}` : ''}`); page.value = 1 } catch (error) { ElMessage.error(error.message) } finally { loading.value = false } }
async function review(row, approved) {
  let comment = approved ? '审核通过，开发环境模拟微信退款' : ''
  if (!approved) {
    try { comment = await ElMessageBox.prompt('请输入驳回原因', '驳回退款申请', { inputPlaceholder: '例如：不符合当前退改规则', inputValidator: (value) => value ? true : '请输入原因' }).then((result) => result.value) } catch { return }
  } else {
    try { await ElMessageBox.confirm(`确认审核通过 ${row.refundNumber}，并发起退款吗？`, '退款审核', { type: 'warning' }) } catch { return }
  }
  try { await post(`/admin/refunds/${row.id}/review`, { approved, comment }); ElMessage.success(approved ? '退款已处理' : '申请已驳回'); await load() } catch (error) { ElMessage.error(error.message) }
}
onMounted(load)
</script>
