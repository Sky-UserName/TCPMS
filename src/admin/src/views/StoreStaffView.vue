<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <p>管理民宿员工账号、门店归属和可访问的运营菜单。</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增员工</el-button>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs" role="tablist">
        <button
          v-for="tab in statusTabs"
          :key="tab.key"
          type="button"
          role="tab"
          class="status-tab"
          :class="{ active: activeTab === tab.key }"
          :aria-selected="activeTab === tab.key"
          @click="activeTab = tab.key"
        >
          {{ tab.label }} <span>{{ countForTab(tab.key) }}</span>
        </button>
      </div>

      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" :loading="loading" title="刷新员工" @click="refresh" />
          <el-button type="success" :icon="Plus" @click="openCreate">添加</el-button>
          <el-button type="primary" plain :disabled="selectedRows.length !== 1" @click="openEdit(selectedRows[0])">编辑</el-button>
          <el-button type="danger" plain :disabled="!selectedRows.length" @click="removeSelected">删除</el-button>
          <strong>员工列表</strong>
          <span class="toolbar-count">{{ filteredRows.length }} 位员工</span>
        </div>
        <div class="toolbar-right">
          <el-input v-model="keyword" clearable class="table-search" placeholder="搜索员工名称、用户名、手机号或民宿" :prefix-icon="Search" @keyup.enter="page = 1" />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedRows" class="clean-table admin-data-table" row-key="id" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column prop="displayName" label="员工名称" min-width="200" fixed="left">
          <template #default="{ row }">
            <div class="staff-user-cell">
              <span class="profile-avatar">{{ initial(row) }}</span>
              <div class="staff-name-cell">
                <strong>{{ row.displayName || row.username }}</strong>
                <span class="staff-username">{{ row.username }}</span>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="150" />
        <el-table-column prop="gender" label="性别" width="90" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <span class="dot-status" :class="row.status === 'normal' ? 'success' : 'muted'"><i></i>{{ statusLabel(row.status) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="民宿" min-width="190">
          <template #default="{ row }">{{ storeName(row.storeId) }}</template>
        </el-table-column>
        <el-table-column label="权限" min-width="300">
          <template #default="{ row }">
            <div class="tag-list">
              <el-tag v-for="permission in permissionLabels(row.permissions)" :key="permission" size="small" effect="light">{{ permission }}</el-tag>
              <span v-if="!row.permissions?.length" class="muted-text">未配置</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" min-width="170"><template #default="{ row }">{{ row.createdAt }}</template></el-table-column>
        <el-table-column label="更新时间" min-width="170"><template #default="{ row }">{{ row.updatedAt }}</template></el-table-column>
        <el-table-column label="操作" width="130" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="removeRows([row])">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredRows.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredRows.length) }} 条记录，共 {{ filteredRows.length }} 条记录</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredRows.length" background />
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '新增门店员工' : '编辑门店员工'" width="640px" destroy-on-close>
      <el-form :model="form" label-width="92px" class="dialog-form">
        <div class="form-grid">
          <el-form-item label="用户名" required>
            <el-select
              v-model="form.username"
              filterable
              remote
              clearable
              allow-create
              default-first-option
              :remote-method="searchMembers"
              :loading="memberLoading"
              placeholder="搜索并选择会员昵称"
              @change="handleMemberPick"
              @focus="ensureMemberOptions"
            >
              <el-option v-for="member in memberOptions" :key="member.id" :label="member.nickname" :value="member.nickname">
                <div class="member-option">
                  <span>{{ member.nickname }}</span>
                  <small>{{ member.phone || '未绑定手机号' }}</small>
                </div>
              </el-option>
            </el-select>
            <div class="form-help">从「会员管理」选择会员昵称作为用户名，选中后自动带出手机号；也可以直接输入账号。</div>
          </el-form-item>
          <el-form-item label="员工名称"><el-input v-model="form.displayName" placeholder="员工展示名称，留空默认与用户名一致" /></el-form-item>
          <el-form-item label="手机号" required><el-input v-model="form.phone" placeholder="选择会员后自动带出，也可手动填写" /></el-form-item>
          <el-form-item label="性别"><el-radio-group v-model="form.gender"><el-radio value="男">男</el-radio><el-radio value="女">女</el-radio><el-radio value="未知">未知</el-radio></el-radio-group></el-form-item>
          <el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio value="normal">正常</el-radio><el-radio value="hidden">隐藏</el-radio></el-radio-group></el-form-item>
          <el-form-item label="民宿" required><el-select v-model="form.storeId" placeholder="请选择民宿"><el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" /></el-select></el-form-item>
        </div>
        <el-form-item label="权限菜单">
          <div class="permission-check-grid">
            <el-checkbox-group v-model="form.permissions">
              <el-checkbox v-for="item in activePermissions" :key="item.key" :value="item.key">{{ item.title }}</el-checkbox>
            </el-checkbox-group>
          </div>
          <div class="form-help">权限选项来自“权限菜单”中状态为正常的用户端民宿管理入口。</div>
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存员工</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, Search } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'

const statusTabs = [
  { key: 'all', label: '全部' },
  { key: 'normal', label: '正常' },
  { key: 'hidden', label: '隐藏' }
]
const stores = ref([])
const permissions = ref([])
const rows = ref([])
const memberOptions = ref([])
const memberLoading = ref(false)
const selectedRows = ref([])
const loading = ref(false)
const keyword = ref('')
const activeTab = ref('all')
const page = ref(1)
const pageSize = ref(10)
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive(emptyForm())

const activePermissions = computed(() => permissions.value.filter((item) => item.status === 'normal'))
const filteredRows = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  return rows.value.filter((row) => {
    const matchesTab = activeTab.value === 'all' || row.status === activeTab.value
    const matchesQuery = !query || `${row.displayName || ''} ${row.username} ${row.phone} ${storeName(row.storeId)} ${permissionLabels(row.permissions).join(' ')}`.toLowerCase().includes(query)
    return matchesTab && matchesQuery
  })
})
const pagedRows = computed(() => filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch([activeTab, keyword, pageSize], () => { page.value = 1 })

function emptyForm() {
  return { id: '', username: '', displayName: '', gender: '未知', phone: '', status: 'normal', storeId: '', permissions: [], wxUserId: '' }
}
async function loadRows() {
  loading.value = true
  try {
    rows.value = await get('/admin/store-staff')
  } catch (error) {
    ElMessage.error(error.message || '员工列表加载失败')
  } finally {
    loading.value = false
  }
}
async function loadPermissions() {
  try {
    permissions.value = await get('/admin/permission-menus')
  } catch (error) {
    permissions.value = []
    ElMessage.error(error.message || '权限菜单加载失败')
  }
}
async function loadStores() {
  try {
    const result = await get('/admin/stores')
    stores.value = Array.isArray(result) ? result.map((store) => ({ id: store.id, name: store.name })) : []
  } catch (error) {
    stores.value = []
    ElMessage.error(error.message || '门店列表加载失败')
  }
}
async function refresh() {
  loading.value = true
  try {
    await Promise.all([loadRows(), loadPermissions(), loadStores()])
    ElMessage.success('员工列表已刷新')
  } finally {
    loading.value = false
  }
}
function countForTab(key) { return key === 'all' ? rows.value.length : rows.value.filter((row) => row.status === key).length }
function statusLabel(value) { return value === 'hidden' ? '隐藏' : '正常' }
function storeName(id) { return stores.value.find((store) => store.id === id)?.name || id || '未分配民宿' }
function permissionLabels(keys = []) { return keys.map((key) => permissions.value.find((item) => item.key === key)?.title || key) }
function initial(row) { return String(row.displayName || row.username || '员').slice(0, 1).toUpperCase() }

// 用户名从「会员管理」选人：远程搜索会员昵称，选中后把手机号、性别一起带出来。
let memberSearchTimer = null
function searchMembers(query = '') {
  window.clearTimeout(memberSearchTimer)
  memberSearchTimer = window.setTimeout(async () => {
    const text = String(query || '').trim()
    memberLoading.value = true
    try {
      const data = await get(`/admin/members?keyword=${encodeURIComponent(text)}&pageSize=50`)
      const items = Array.isArray(data) ? data : data.items || []
      memberOptions.value = items
        .filter((item) => item && item.nickname)
        .map((item) => ({ id: item.id, nickname: item.nickname, phone: item.phone || '', gender: item.gender || 'Unknown' }))
    } catch (error) {
      memberOptions.value = []
    } finally {
      memberLoading.value = false
    }
  }, 260)
}
function ensureMemberOptions() {
  if (!memberOptions.value.length) searchMembers('')
}
function handleMemberPick(username) {
  const member = memberOptions.value.find((item) => item.nickname === username)
  if (!member) {
    form.wxUserId = ''
    return
  }
  form.wxUserId = member.id
  if (member.phone) form.phone = member.phone
  if (!String(form.displayName || '').trim()) form.displayName = member.nickname
  if (member.gender === 'Male' || member.gender === '男') form.gender = '男'
  else if (member.gender === 'Female' || member.gender === '女') form.gender = '女'
}
function openCreate() {
  Object.assign(form, emptyForm(), { storeId: stores.value[0]?.id || '', permissions: activePermissions.value.slice(0, 2).map((item) => item.key) })
  memberOptions.value = []
  dialog.mode = 'create'
  dialog.visible = true
}
function openEdit(row) {
  Object.assign(form, emptyForm(), { ...row, permissions: [...(row.permissions || [])] })
  memberOptions.value = []
  dialog.mode = 'edit'
  dialog.visible = true
}
async function save() {
  if (!form.username.trim() || !form.phone.trim() || !form.storeId) return ElMessage.warning('请填写用户名、手机号和民宿')
  dialog.saving = true
  try {
    const payload = {
      username: form.username.trim(),
      displayName: (form.displayName || '').trim() || form.username.trim(),
      gender: form.gender,
      phone: form.phone.trim(),
      status: form.status,
      storeId: form.storeId || null,
      permissions: form.permissions,
      wxUserId: form.wxUserId || null
    }
    const saved = dialog.mode === 'create'
      ? await post('/admin/store-staff', payload)
      : await put(`/admin/store-staff/${form.id}`, payload)
    if (dialog.mode === 'create') rows.value.unshift(saved)
    else {
      const index = rows.value.findIndex((row) => row.id === form.id)
      if (index >= 0) rows.value[index] = saved
    }
    dialog.visible = false
    ElMessage.success('员工信息已保存')
  } catch (error) {
    ElMessage.error(error.message || '员工信息保存失败')
  } finally {
    dialog.saving = false
  }
}
async function removeRows(items) {
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 位员工吗？`, '删除员工', { type: 'warning' })
    await Promise.all(items.map((item) => del(`/admin/store-staff/${item.id}`)))
    const ids = new Set(items.map((item) => item.id))
    rows.value = rows.value.filter((row) => !ids.has(row.id))
    selectedRows.value = []
    ElMessage.success('员工已删除')
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') ElMessage.error(error.message || '员工删除失败')
  }
}
function removeSelected() { return removeRows(selectedRows.value) }
function handlePermissionUpdate() { loadPermissions() }
onMounted(() => {
  loadRows()
  loadPermissions()
  loadStores()
  window.addEventListener('tcpms:permission-menu-updated', handlePermissionUpdate)
})
onBeforeUnmount(() => window.removeEventListener('tcpms:permission-menu-updated', handlePermissionUpdate))
</script>

<style scoped>
.staff-user-cell{display:flex;align-items:center;gap:9px}
.staff-user-cell strong{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.staff-name-cell{display:flex;min-width:0;flex-direction:column}
.staff-username{margin-top:2px;color:var(--admin-muted);font-size:12px}
.member-option{display:flex;align-items:center;justify-content:space-between;gap:16px;width:100%}
.member-option small{color:var(--admin-muted);font-size:12px}
.permission-check-grid{width:100%;padding:10px 12px;border:1px solid var(--admin-border);border-radius:8px;background:var(--admin-surface-soft)}
.permission-check-grid :deep(.el-checkbox-group){display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px 8px}
.form-help{margin-top:8px;color:var(--admin-muted);font-size:12px;line-height:1.5}
@media (max-width:760px){.permission-check-grid :deep(.el-checkbox-group){grid-template-columns:repeat(2,minmax(0,1fr))}}
</style>
