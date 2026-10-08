<template>
  <div class="view-stack">
    <section class="page-heading"><div><span class="eyebrow">AUDIT TRAIL</span><h2>操作审计</h2><p>查看门店、库存、订单、退款和权限变更记录。</p></div><el-button :icon="Refresh" :loading="loading" @click="load">刷新日志</el-button></section>
    <el-card class="panel-card filter-card" shadow="never"><div class="filter-row"><el-input v-model="keyword" clearable placeholder="操作 / 资源 / 摘要" :prefix-icon="Search" @keyup.enter="load" /><el-button type="primary" @click="load">查询</el-button></div></el-card>
    <el-card class="panel-card" shadow="never"><el-table v-loading="loading" :data="pagedLogs" class="clean-table admin-data-table"><el-table-column label="时间" width="180"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column><el-table-column prop="actorType" label="操作者" width="110" /><el-table-column prop="action" label="动作" width="180" /><el-table-column label="资源" min-width="180"><template #default="{ row }">{{ row.resourceType }} / {{ row.resourceId }}</template></el-table-column><el-table-column prop="summary" label="摘要" min-width="280" show-overflow-tooltip /></el-table><div class="list-footer"><span>共 {{ logs.length }} 条记录</span><el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="logs.length" background /></div></el-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Search } from '@element-plus/icons-vue'
import { get } from '../api'
const logs = ref([])
const keyword = ref('')
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const pagedLogs = computed(() => logs.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([keyword, pageSize], () => { page.value = 1 })
function formatDateTime(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-' }
function formatDate(value) { return value ? new Date(value).toLocaleDateString('zh-CN') : '-' }
async function load() { loading.value = true; try { const query = keyword.value ? `?keyword=${encodeURIComponent(keyword.value)}` : ''; logs.value = await get(`/admin/audit-logs${query}`); page.value = 1 } catch (error) { ElMessage.error(error.message) } finally { loading.value = false } }
onMounted(load)
</script>
