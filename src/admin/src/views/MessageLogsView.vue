<template>
  <div class="view-stack compact-view message-logs-page">
    <section class="page-heading">
      <div>
        <p>记录小程序订阅消息发送情况，便于按模板、会员和 OpenID 查询投递结果。</p>
        <div class="page-meta">
          <el-tag size="small" effect="plain" :type="sourceMode === 'api' ? 'success' : 'warning'">
            {{ sourceMode === 'api' ? '接口数据' : '演示数据' }}
          </el-tag>
          <span>共 {{ filteredLogs.length }} 条记录</span>
        </div>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
      </div>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新日志" @click="load" />
          <el-button type="danger" plain :icon="Delete" :disabled="!selectedIds.length" @click="removeSelected">删除</el-button>
        </div>
        <div class="toolbar-right">
          <el-input
            v-model="keyword"
            clearable
            class="table-search"
            placeholder="搜索模板名称、模板 ID 或 OpenID"
            :prefix-icon="Search"
            @keyup.enter="page = 1"
          />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <el-table
        v-loading="loading"
        :data="pagedLogs"
        row-key="id"
        class="clean-table admin-data-table"
        @selection-change="selectedRows = $event"
      >
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column label="模板名称" min-width="160" fixed="left">
          <template #default="{ row }">
            <div class="template-name-cell">
              <strong>{{ row.templateName || '-' }}</strong>
              <small>{{ row.identifier || '小程序订阅消息' }}</small>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="templateId" label="模板 ID" min-width="230" show-overflow-tooltip />
        <el-table-column prop="openid" label="openid" min-width="190" show-overflow-tooltip />
        <el-table-column label="模板内容" min-width="300">
          <template #default="{ row }">
            <el-tooltip v-if="row.content" effect="light" placement="top" :content="row.content">
              <span class="content-preview">{{ row.content }}</span>
            </el-tooltip>
            <span v-else class="muted-text">-</span>
          </template>
        </el-table-column>
        <el-table-column label="发送结果" width="130">
          <template #default="{ row }">
            <el-tag :type="resultType(row.result)" effect="light">{{ resultLabel(row.result) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" min-width="166">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="更新时间" min-width="166">
          <template #default="{ row }">{{ formatDateTime(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="76" fixed="right">
          <template #default="{ row }">
            <el-button link type="danger" :icon="Delete" title="删除日志" @click="removeRows([row])">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredLogs.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredLogs.length) }} 条记录，共 {{ filteredLogs.length }} 条记录</span>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50]"
          layout="sizes, prev, pager, next, jumper"
          :total="filteredLogs.length"
          background
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Delete, Refresh, Search } from '@element-plus/icons-vue'
import { del, get } from '../api'

const LOG_ENDPOINT = '/admin/message-logs'

const loading = ref(false)
const sourceMode = ref('api')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(10)
const logs = ref([])
const selectedRows = ref([])

const selectedIds = computed(() => selectedRows.value.map((row) => row.id))
const filteredLogs = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  if (!query) return logs.value
  return logs.value.filter((row) => [
    row.templateName,
    row.identifier,
    row.templateId,
    row.memberId,
    row.openid,
    row.content,
    row.result
  ].some((value) => String(value ?? '').toLowerCase().includes(query)))
})
const pagedLogs = computed(() => filteredLogs.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch([keyword, pageSize], () => { page.value = 1 })

function normalizeLog(item = {}, index = 0) {
  return {
    id: item.id ?? `demo-log-${index + 1}`,
    templateName: item.templateName || item.name || item.title || '',
    identifier: item.identifier || item.code || '',
    templateId: item.templateId || item.templateID || '',
    memberId: item.memberId ?? item.userId ?? item.memberID ?? '-',
    openid: item.openid || item.openId || '',
    content: stringifyContent(item.content ?? item.template ?? item.data ?? ''),
    result: normalizeResult(item.result ?? item.sendStatus ?? item.status ?? item.sendResult ?? item.errmsg),
    createdAt: item.createdAt || item.createTime || '',
    updatedAt: item.updatedAt || item.updateTime || item.createdAt || ''
  }
}

function stringifyContent(value) {
  if (value === null || value === undefined || value === '') return ''
  if (typeof value === 'string') return value
  try { return JSON.stringify(value) } catch { return String(value) }
}

function normalizeResult(value) {
  const text = String(value || '').toLowerCase()
  if (['success', 'sent', 'ok', '0', '成功', '已发送'].includes(text)) return 'success'
  if (['insufficient', 'insufficientquota', '次数不足', 'quota', '频次不足'].includes(text)) return 'insufficient'
  if (['failed', 'fail', 'error', '失败', '发送失败'].includes(text)) return 'failed'
  return value || 'unknown'
}

function normalizeList(payload) {
  const list = Array.isArray(payload) ? payload : payload?.items || payload?.data || payload?.rows || []
  return Array.isArray(list) ? list.map(normalizeLog) : []
}

function demoLogs() {
  return [
    { id: 138, templateName: '客人退房提醒', templateId: '************************HIfzZUNNNNE', memberId: 45, openid: '', content: '', result: 'insufficient', createdAt: '2026-07-31 10:00:00', updatedAt: '2026-07-31 10:00:00' },
    { id: 137, templateName: '客人退房提醒', templateId: '************************HIfzZUNNNNE', memberId: 136, openid: '', content: '', result: 'insufficient', createdAt: '2026-07-31 10:00:00', updatedAt: '2026-07-31 10:00:00' },
    { id: 135, templateName: '预定民宿成功', templateId: 'CObfez5LQVt07Cs-T3YSaf80e5fU8BFz_9w-LWV0QbE', memberId: 45, openid: '', content: '', result: 'insufficient', createdAt: '2026-07-29 17:54:57', updatedAt: '2026-07-29 17:54:57' },
    { id: 134, templateName: '预定民宿成功', templateId: 'CObfez5LQVt07Cs-T3YSaf80e5fU8BFz_9w-LWV0QbE', memberId: 45, openid: '', content: '', result: 'insufficient', createdAt: '2026-07-29 17:54:56', updatedAt: '2026-07-29 17:54:56' },
    { id: 132, templateName: '客人退房提醒', templateId: '************************HIfzZUNNNNE', memberId: 96, openid: 'orO6Q6I8B4POurRLJeR3axF7iMss', content: '{"thing1":"好人民宿","name3":"我和你","date4":"2026-06-20"}', result: 'success', createdAt: '2026-06-20 10:00:03', updatedAt: '2026-06-20 10:00:03' },
    { id: 130, templateName: '预定民宿成功', templateId: 'CObfez5LQVt07Cs-T3YSaf80e5fU8BFz_9w-LWV0QbE', memberId: 97, openid: 'orO6Q66hhgPUfSgEzdkmnsi6qb4', content: '{"thing6":"好人民宿","thing7":"广东省...","thing8":"2026-06-19"}', result: 'success', createdAt: '2026-06-19 11:33:21', updatedAt: '2026-06-19 11:33:21' },
    { id: 126, templateName: '提现审核通过', templateId: '************************ifTPilylIHA', memberId: 1, openid: 'orO6Q6_9GU3QjTq45MOWs-Xhd3hg', content: '{"time1":"2026-05-13 14:43","amount3":"300.00","state":"通过"}', result: 'success', createdAt: '2026-05-13 14:44:54', updatedAt: '2026-05-13 14:44:54' },
    { id: 125, templateName: '提现审核通过', templateId: '************************ifTPilylIHA', memberId: 1, openid: '', content: '', result: 'insufficient', createdAt: '2026-05-13 14:40:53', updatedAt: '2026-05-13 14:40:53' }
  ].map(normalizeLog)
}

async function requestList() {
  return normalizeList(await get(`${LOG_ENDPOINT}?page=1&pageSize=100`))
}

async function load() {
  loading.value = true
  try {
    logs.value = await requestList()
    sourceMode.value = 'api'
    selectedRows.value = []
  } catch (error) {
    ElMessage.error(error.message || '消息日志加载失败')
  } finally {
    loading.value = false
  }
}

async function removeRows(items) {
  if (!items.length) return
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 条消息日志吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }

  try {
    await Promise.all(items.map((item) => del(`${LOG_ENDPOINT}/${encodeURIComponent(item.id)}`)))
  } catch (error) {
    ElMessage.error(error.message || '消息日志删除失败')
    return
  }
  const ids = new Set(items.map((item) => item.id))
  logs.value = logs.value.filter((item) => !ids.has(item.id))
  selectedRows.value = []
  ElMessage.success('消息日志已删除')
}

function removeSelected() {
  removeRows(selectedRows.value)
}

function resultLabel(value) {
  return ({ success: '发送成功', insufficient: '次数不足', failed: '发送失败', unknown: '未知' })[value] || value || '未知'
}

function resultType(value) {
  return ({ success: 'success', insufficient: 'warning', failed: 'danger', unknown: 'info' })[value] || 'info'
}

function formatDateTime(value) {
  if (!value) return '未记录'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

onMounted(load)
</script>

<style scoped>
.message-logs-page .page-meta{display:flex;align-items:center;gap:8px;margin-top:9px;color:var(--admin-muted);font-size:11px}
.template-name-cell{display:grid;gap:4px;min-width:0}
.template-name-cell strong,.template-name-cell small{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.template-name-cell strong{color:var(--admin-text);font-size:13px}
.template-name-cell small{color:var(--admin-muted);font-size:10px}
.content-preview{display:block;overflow:hidden;color:var(--admin-muted);text-overflow:ellipsis;white-space:nowrap}
@media (max-width:820px){.message-logs-page .toolbar-right{width:100%}.message-logs-page .table-search{width:100%}}
</style>
