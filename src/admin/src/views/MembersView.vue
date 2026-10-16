<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <p>查看微信授权后自动注册的会员资料和登录状态。</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
      </div>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <el-alert class="member-source-alert" title="会员由用户端微信授权后自动注册，后台不支持手动添加会员。" type="info" :closable="false" show-icon />
      <el-alert v-if="loadError" class="member-source-alert" :title="`会员接口不可用，当前未加载本地演示数据：${loadError}`" type="error" :closable="false" show-icon />
      <div class="status-tabs">
        <button v-for="tab in tabs" :key="tab.key" type="button" role="tab" class="status-tab" :class="{ active: activeTab === tab.key }" :aria-selected="activeTab === tab.key" @click="activeTab = tab.key">
          {{ tab.label }} <span>{{ tab.key === 'all' ? members.length : members.filter((item) => tab.key === 'enabled' ? item.isEnabled : !item.isEnabled).length }}</span>
        </button>
      </div>
      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新" @click="load" />
          <el-button type="primary" plain :icon="Edit" :disabled="source !== 'api' || selected.length !== 1" @click="openEdit(selected[0])">编辑</el-button>
          <el-button type="danger" plain :icon="Delete" :disabled="source !== 'api' || !selected.length" @click="removeSelected">删除</el-button>
        </div>
        <div class="toolbar-right">
          <el-input v-model="keyword" class="table-search" clearable placeholder="搜索用户、昵称、手机号" :prefix-icon="Search" @keyup.enter="page = 1" />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedMembers" class="clean-table admin-data-table" row-key="id" @selection-change="selected = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column label="用户名" min-width="220">
          <template #default="{ row }"><div class="user-cell"><span class="profile-avatar">{{ String(row.openId || row.nickname || '会').slice(0, 1) }}</span><div><strong>{{ row.openId || '-' }}</strong><small>{{ row.createdAt ? formatDateTime(row.createdAt) : '注册时间未知' }}</small></div></div></template>
        </el-table-column>
        <el-table-column prop="nickname" label="昵称" min-width="120" />
        <el-table-column prop="phoneNumber" label="手机号" width="140" />
        <el-table-column label="头像" width="82">
          <template #default="{ row }">
            <el-image v-if="row.avatarUrl" class="table-avatar" :src="row.avatarUrl" :preview-src-list="[row.avatarUrl]" preview-teleported />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="最近登录" min-width="170"><template #default="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template></el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-dropdown trigger="click" @command="(command) => handleMore(command, row)">
              <el-button type="primary" link>更多操作<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="detail">查看资料</el-dropdown-item>
                  <el-dropdown-item command="toggle" :disabled="source !== 'api'">{{ row.isEnabled ? '停用会员' : '启用会员' }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button type="primary" link :disabled="source !== 'api'" @click="openEdit(row)">编辑</el-button>
            <el-button type="danger" link :disabled="source !== 'api'" @click="removeRows([row])">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredMembers.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredMembers.length) }} 条记录，共 {{ filteredMembers.length }} 条记录</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredMembers.length" background />
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" class="member-edit-dialog" title="编辑会员资料" width="680px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="104px" class="dialog-form member-edit-form">
        <el-form-item label="用户名" prop="openId" required>
          <el-input v-model="form.openId" disabled placeholder="微信 OpenID 或用户标识" />
        </el-form-item>
        <el-form-item label="昵称" prop="nickname" required>
          <el-input v-model="form.nickname" disabled placeholder="微信授权昵称" />
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phoneNumber" disabled placeholder="微信授权手机号" />
        </el-form-item>
        <el-form-item label="头像">
          <div class="member-avatar-editor">
            <div class="member-avatar-input">
              <el-input v-model="form.avatarUrl" disabled placeholder="微信授权头像" />
            </div>
            <div class="member-avatar-preview">
              <el-image v-if="form.avatarUrl" :src="form.avatarUrl" fit="cover" :preview-src-list="[form.avatarUrl]" preview-teleported>
                <template #error><div class="avatar-preview-fallback"><el-icon><Picture /></el-icon><span>图片不可用</span></div></template>
              </el-image>
              <div v-else class="avatar-preview-fallback"><el-icon><Picture /></el-icon><span>暂无头像</span></div>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="性别">
          <el-radio-group v-model="form.gender" class="member-radio-group">
            <el-radio value="Male">男</el-radio>
            <el-radio value="Female">女</el-radio>
            <el-radio value="Unknown">未知</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="form.isEnabled" class="member-radio-group">
            <el-radio :value="true">正常</el-radio>
            <el-radio :value="false">隐藏</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存会员</el-button></template>
    </el-dialog>

    <el-drawer v-model="detailVisible" title="会员资料" size="460px">
      <div v-if="detailMember" class="member-detail">
        <div class="member-detail-head">
          <el-image v-if="detailMember.avatarUrl" class="profile-avatar large member-detail-avatar" :src="detailMember.avatarUrl" fit="cover" />
          <span v-else class="profile-avatar large">{{ String(detailMember.nickname || detailMember.openId || '会').slice(0, 1) }}</span>
          <div><h3>{{ detailMember.nickname || '未设置昵称' }}</h3><p>{{ detailMember.openId }}</p></div>
          <el-tag :type="detailMember.isEnabled ? 'success' : 'info'" effect="light">{{ detailMember.isEnabled ? '正常' : '隐藏' }}</el-tag>
        </div>
        <div class="detail-summary-grid">
          <div><small>手机号</small><strong>{{ detailMember.phoneNumber || '-' }}</strong></div>
          <div><small>性别</small><strong>{{ genderLabel(detailMember.gender) }}</strong></div>
          <div><small>最近登录</small><strong>{{ formatDateTime(detailMember.lastLoginAt) }}</strong></div>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Delete, Edit, Picture, Refresh, Search } from '@element-plus/icons-vue'
import { del, get, put } from '../api'

const tabs = [{ key: 'all', label: '全部' }, { key: 'enabled', label: '正常' }, { key: 'disabled', label: '停用' }]
const members = ref([])
const loading = ref(false)
const activeTab = ref('all')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(10)
const selected = ref([])
const source = ref('loading')
const loadError = ref('')
const detailMember = ref(null)
const detailVisible = computed({ get: () => Boolean(detailMember.value), set: (value) => { if (!value) detailMember.value = null } })
const dialog = reactive({ visible: false, saving: false })
const form = reactive(emptyForm())
const formRef = ref()
const formRules = {
  openId: [{ required: true, message: '请输入用户名或用户标识', trigger: 'blur' }],
  nickname: [{ required: true, message: '请输入会员昵称', trigger: 'blur' }]
}

const filteredMembers = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  return members.value.filter((row) => {
    const matchesTab = activeTab.value === 'all' || (activeTab.value === 'enabled' ? row.isEnabled !== false : row.isEnabled === false)
    const text = `${row.openId || ''} ${row.nickname || ''} ${row.phoneNumber || ''}`.toLowerCase()
    return matchesTab && (!query || text.includes(query))
  })
})
const pagedMembers = computed(() => filteredMembers.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([activeTab, keyword, pageSize], () => { page.value = 1 })

function emptyForm() {
  return {
    id: '',
    openId: '',
    nickname: '',
    phoneNumber: '',
    avatarUrl: '',
    gender: 'Unknown',
    isEnabled: true,
    // Kept internally for compatibility with the current full-member PUT contract.
    balanceCents: 0,
    commissionCents: 0,
    parentUserId: null
  }
}
function formatDateTime(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-' }
function genderLabel(value) { return ({ Male: '男', Female: '女', Unknown: '未知' })[value] || '未知' }
async function load() {
  loading.value = true
  loadError.value = ''
  try {
    const data = await get('/admin/members')
    const items = Array.isArray(data) ? data : data.items || []
    members.value = items.map(normalizeMember)
    source.value = 'api'
  } catch (error) {
    members.value = []
    selected.value = []
    source.value = 'error'
    loadError.value = error.message || '请求失败'
  } finally {
    loading.value = false
  }
}
function normalizeMember(row) {
  return {
    ...row,
    phoneNumber: row.phoneNumber ?? row.phone ?? '',
    avatarUrl: row.avatarUrl ?? row.avatar ?? '',
    gender: row.gender ?? 'Unknown',
    directCount: row.directCount ?? row.directUserCount ?? 0,
    indirectCount: row.indirectCount ?? row.indirectUserCount ?? 0,
    isEnabled: row.isEnabled !== false
  }
}
function openEdit(row) { Object.assign(form, emptyForm(), row); dialog.visible = true }
async function save() {
  if (source.value !== 'api') return ElMessage.error('会员接口不可用，未保存任何更改')
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  dialog.saving = true
  try {
    const payload = {
      openId: form.openId,
      nickname: form.nickname,
      phoneNumber: form.phoneNumber,
      avatarUrl: form.avatarUrl,
      gender: form.gender,
      balanceCents: Number(form.balanceCents) || 0,
      commissionCents: Number(form.commissionCents) || 0,
      parentUserId: form.parentUserId || null,
      isEnabled: form.isEnabled
    }
    if (!form.id) {
      ElMessage.warning('会员只能由用户端微信授权后自动注册')
      return
    }
    await put(`/admin/members/${form.id}`, payload)
    if (form.id) {
      const index = members.value.findIndex((row) => String(row.id) === String(form.id))
      if (index >= 0) members.value[index] = { ...members.value[index], ...form }
    }
    dialog.visible = false
    ElMessage.success('会员已保存')
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    dialog.saving = false
  }
}
async function removeRows(items) {
  if (source.value !== 'api') return ElMessage.error('会员接口不可用，未删除任何数据')
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 个会员吗？`, '删除确认', { type: 'warning' })
    for (const row of items) await del(`/admin/members/${row.id}`)
    const ids = new Set(items.map((row) => row.id))
    members.value = members.value.filter((row) => !ids.has(row.id))
    selected.value = []
    ElMessage.success('会员已删除')
  } catch (error) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}
function removeSelected() { removeRows(selected.value) }
async function handleMore(command, row) {
  if (command === 'detail') detailMember.value = row
  if (command === 'toggle') {
    if (source.value !== 'api') return ElMessage.error('会员接口不可用，未更改会员状态')
    const nextEnabled = !row.isEnabled
    try {
      await put(`/admin/members/${row.id}`, {
        openId: row.openId,
        nickname: row.nickname,
        phoneNumber: row.phoneNumber,
        avatarUrl: row.avatarUrl,
        gender: row.gender,
        balanceCents: Number(row.balanceCents) || 0,
        commissionCents: Number(row.commissionCents) || 0,
        parentUserId: row.parentUserId || null,
        isEnabled: nextEnabled
      })
      row.isEnabled = nextEnabled
      ElMessage.success(nextEnabled ? '会员已启用' : '会员已停用')
    } catch (error) {
      ElMessage.error(error.message || '会员状态更新失败')
    }
  }
}
onMounted(load)
</script>
