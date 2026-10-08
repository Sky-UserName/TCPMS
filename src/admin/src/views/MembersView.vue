<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <span class="eyebrow">MEMBER CENTER</span>
        <h2>会员管理</h2>
        <p>查看会员资料、余额、分销关系和最近登录记录。</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">添加会员</el-button>
      </div>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs">
        <button v-for="tab in tabs" :key="tab.key" type="button" role="tab" class="status-tab" :class="{ active: activeTab === tab.key }" :aria-selected="activeTab === tab.key" @click="activeTab = tab.key">
          {{ tab.label }} <span>{{ tab.key === 'all' ? members.length : members.filter((item) => tab.key === 'enabled' ? item.isEnabled : !item.isEnabled).length }}</span>
        </button>
      </div>
      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新" @click="load" />
          <el-button type="success" :icon="Plus" @click="openCreate">添加</el-button>
          <el-button type="primary" plain :icon="Edit" :disabled="selected.length !== 1" @click="openEdit(selected[0])">编辑</el-button>
          <el-button type="danger" plain :icon="Delete" :disabled="!selected.length" @click="removeSelected">删除</el-button>
        </div>
        <div class="toolbar-right">
          <el-input v-model="keyword" class="table-search" clearable placeholder="搜索用户、昵称、手机号" :prefix-icon="Search" @keyup.enter="page = 1" />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedMembers" class="clean-table admin-data-table" row-key="id" @selection-change="selected = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column prop="id" label="ID" width="74" sortable />
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
        <el-table-column label="余额" width="110"><template #default="{ row }"><strong>{{ money(row.balanceCents) }}</strong></template></el-table-column>
        <el-table-column label="累计佣金" width="120"><template #default="{ row }"><strong class="price-text">{{ money(row.commissionCents) }}</strong></template></el-table-column>
        <el-table-column prop="directCount" label="直属用户" width="100" />
        <el-table-column prop="indirectCount" label="间属用户" width="100" />
        <el-table-column label="最近登录" min-width="170"><template #default="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template></el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-dropdown trigger="click" @command="(command) => handleMore(command, row)">
              <el-button type="primary" link>更多操作<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="detail">查看资料</el-dropdown-item>
                  <el-dropdown-item command="toggle">{{ row.isEnabled ? '停用会员' : '启用会员' }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button type="primary" link @click="openEdit(row)">编辑</el-button>
            <el-button type="danger" link @click="removeRows([row])">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredMembers.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredMembers.length) }} 条记录，共 {{ filteredMembers.length }} 条记录</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredMembers.length" background />
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" class="member-edit-dialog" :title="dialog.mode === 'create' ? '添加会员' : '编辑会员'" width="680px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="104px" class="dialog-form member-edit-form">
        <el-form-item label="用户名" prop="openId" required>
          <el-input v-model="form.openId" placeholder="微信 OpenID 或用户标识" />
        </el-form-item>
        <el-form-item label="昵称" prop="nickname" required>
          <el-input v-model="form.nickname" placeholder="请输入会员昵称" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="form.password" type="password" show-password readonly placeholder="会员使用微信登录，不设置后台密码" />
          <div class="form-hint member-password-note">小程序会员不使用后台账号密码；编辑时留空即可。</div>
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phoneNumber" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="头像">
          <div class="member-avatar-editor">
            <div class="member-avatar-input">
              <el-input v-model="form.avatarUrl" placeholder="输入头像地址，或选择本地图片" />
              <el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="handleAvatarChange">
                <el-button type="primary" plain :icon="Upload">选择图片</el-button>
              </el-upload>
              <el-button plain :icon="Link" @click="focusAvatarUrl">使用地址</el-button>
            </div>
            <div class="member-avatar-preview">
              <el-image v-if="form.avatarUrl" :src="form.avatarUrl" fit="cover" :preview-src-list="[form.avatarUrl]" preview-teleported>
                <template #error><div class="avatar-preview-fallback"><el-icon><Picture /></el-icon><span>图片不可用</span></div></template>
              </el-image>
              <div v-else class="avatar-preview-fallback"><el-icon><Picture /></el-icon><span>暂无头像</span></div>
              <el-button v-if="form.avatarUrl" class="member-avatar-remove" type="danger" plain :icon="Delete" @click="clearAvatar">移除头像</el-button>
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
        <div class="form-grid member-finance-fields">
          <el-form-item label="余额（分）"><el-input-number v-model="form.balanceCents" :min="0" controls-position="right" /></el-form-item>
          <el-form-item label="累计佣金（分）"><el-input-number v-model="form.commissionCents" :min="0" controls-position="right" /></el-form-item>
        </div>
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
          <div><small>余额</small><strong>{{ money(detailMember.balanceCents) }}</strong></div>
          <div><small>累计佣金</small><strong>{{ money(detailMember.commissionCents) }}</strong></div>
          <div><small>直属用户</small><strong>{{ detailMember.directCount || 0 }}</strong></div>
          <div><small>间属用户</small><strong>{{ detailMember.indirectCount || 0 }}</strong></div>
          <div><small>最近登录</small><strong>{{ formatDateTime(detailMember.lastLoginAt) }}</strong></div>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Delete, Edit, Link, Picture, Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'

const tabs = [{ key: 'all', label: '全部' }, { key: 'enabled', label: '正常' }, { key: 'disabled', label: '停用' }]
const members = ref([])
const loading = ref(false)
const activeTab = ref('all')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(10)
const selected = ref([])
const source = ref('api')
const detailMember = ref(null)
const detailVisible = computed({ get: () => Boolean(detailMember.value), set: (value) => { if (!value) detailMember.value = null } })
const dialog = reactive({ visible: false, mode: 'create', saving: false })
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
    const text = `${row.id} ${row.openId || ''} ${row.nickname || ''} ${row.phoneNumber || ''}`.toLowerCase()
    return matchesTab && (!query || text.includes(query))
  })
})
const pagedMembers = computed(() => filteredMembers.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([activeTab, keyword, pageSize], () => { page.value = 1 })

function emptyForm() { return { id: '', openId: '', nickname: '', password: '', phoneNumber: '', avatarUrl: '', gender: 'Unknown', balanceCents: 0, commissionCents: 0, isEnabled: true } }
function money(cents) { return `¥${((Number(cents) || 0) / 100).toFixed(2)}` }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-' }
function genderLabel(value) { return ({ Male: '男', Female: '女', Unknown: '未知' })[value] || '未知' }
function seedMembers() {
  return [
    { id: 17, openId: 'user_a2cb01e05252e7dcf9824424', nickname: '星星', phoneNumber: '15900003334', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-07T10:12:00+08:00', isEnabled: true, createdAt: '2026-09-22T10:12:00+08:00' },
    { id: 16, openId: 'user_06cd4af92194276af9678528', nickname: '用户69eadf96783be', phoneNumber: '18355330761', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-07T09:42:00+08:00', isEnabled: true, createdAt: '2026-09-20T09:42:00+08:00' },
    { id: 15, openId: 'user_d2ac82afe2cc926e4ed093b06', nickname: '刘', phoneNumber: '18652949949', avatarUrl: '', balanceCents: 100, commissionCents: 0, directCount: 1, indirectCount: 0, lastLoginAt: '2026-10-06T20:02:00+08:00', isEnabled: true, createdAt: '2026-09-18T20:02:00+08:00' },
    { id: 14, openId: 'user_48e0d02f37a34a8e', nickname: '墨', phoneNumber: '18862515515', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 1, indirectCount: 1, lastLoginAt: '2026-10-06T17:22:00+08:00', isEnabled: true, createdAt: '2026-09-16T17:22:00+08:00' },
    { id: 13, openId: 'user_2b762c3b7f8b4a80', nickname: '月亮', phoneNumber: '19952195103', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-05T14:02:00+08:00', isEnabled: true, createdAt: '2026-09-12T14:02:00+08:00' },
    { id: 11, openId: 'user_0b7f2a7807d848e0', nickname: 'Carl', phoneNumber: '18952505817', avatarUrl: '', balanceCents: 10, commissionCents: 10, directCount: 1, indirectCount: 0, lastLoginAt: '2026-10-05T13:12:00+08:00', isEnabled: true, createdAt: '2026-09-10T13:12:00+08:00' },
    { id: 10, openId: 'user_aa7b0c5f8d7a4210', nickname: 'Jane', phoneNumber: '18921915967', avatarUrl: '', balanceCents: 20, commissionCents: 120, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-04T10:22:00+08:00', isEnabled: true, createdAt: '2026-09-08T10:22:00+08:00' },
    { id: 9, openId: 'user_12adcae2f3ee49da', nickname: '星星', phoneNumber: '15929039049', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-03T15:02:00+08:00', isEnabled: true, createdAt: '2026-09-01T15:02:00+08:00' },
    { id: 7, openId: 'user_0d8f3ac8ce1a41c7', nickname: '健康最重要', phoneNumber: '15716770770', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-02T18:12:00+08:00', isEnabled: true, createdAt: '2026-08-29T18:12:00+08:00' },
    { id: 6, openId: 'user_817a0b9cc4bf4c6f', nickname: '133****0825', phoneNumber: '1333709825', avatarUrl: '', balanceCents: 0, commissionCents: 0, directCount: 0, indirectCount: 0, lastLoginAt: '2026-10-01T11:12:00+08:00', isEnabled: false, createdAt: '2026-08-26T11:12:00+08:00' }
  ]
}
async function load() {
  loading.value = true
  try {
    const data = await get('/admin/members')
    const items = Array.isArray(data) ? data : data.items || []
    members.value = items.map(normalizeMember)
    source.value = 'api'
  } catch {
    const saved = localStorage.getItem('tcpms.admin.members')
    const localMembers = saved ? JSON.parse(saved) : seedMembers()
    members.value = localMembers.map(normalizeMember)
    if (!saved) persist()
    source.value = 'local'
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
function persist() { localStorage.setItem('tcpms.admin.members', JSON.stringify(members.value)) }
function openCreate() { Object.assign(form, emptyForm()); dialog.mode = 'create'; dialog.visible = true }
function openEdit(row) { Object.assign(form, emptyForm(), row, { password: '' }); dialog.mode = 'edit'; dialog.visible = true }
async function save() {
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
      balanceCents: form.balanceCents,
      commissionCents: form.commissionCents,
      parentUserId: form.parentUserId || null,
      isEnabled: form.isEnabled
    }
    let createdMember = null
    if (source.value === 'api') {
      if (form.id) await put(`/admin/members/${form.id}`, payload)
      else createdMember = normalizeMember(await post('/admin/members', payload))
    }
    if (createdMember) {
      members.value.unshift(createdMember)
    } else if (form.id) {
      const index = members.value.findIndex((row) => String(row.id) === String(form.id))
      if (index >= 0) members.value[index] = { ...members.value[index], ...form }
    } else {
      members.value.unshift({ ...form, id: form.id || Date.now(), directCount: 0, indirectCount: 0, lastLoginAt: null, createdAt: new Date().toISOString() })
    }
    persist()
    dialog.visible = false
    ElMessage.success('会员已保存')
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    dialog.saving = false
  }
}
async function removeRows(items) {
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 个会员吗？`, '删除确认', { type: 'warning' })
    if (source.value === 'api') {
      for (const row of items) await del(`/admin/members/${row.id}`)
    }
    const ids = new Set(items.map((row) => row.id))
    members.value = members.value.filter((row) => !ids.has(row.id))
    persist()
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
    const nextEnabled = !row.isEnabled
    try {
      if (source.value === 'api') {
        await put(`/admin/members/${row.id}`, {
          openId: row.openId,
          nickname: row.nickname,
          phoneNumber: row.phoneNumber,
          avatarUrl: row.avatarUrl,
          gender: row.gender,
          balanceCents: row.balanceCents,
          commissionCents: row.commissionCents,
          parentUserId: row.parentUserId || null,
          isEnabled: nextEnabled
        })
      }
      row.isEnabled = nextEnabled
      persist()
      ElMessage.success(nextEnabled ? '会员已启用' : '会员已停用')
    } catch (error) {
      ElMessage.error(error.message || '会员状态更新失败')
    }
  }
}
function handleAvatarChange(uploadFile) {
  const file = uploadFile.raw
  if (!file) return
  if (!file.type.startsWith('image/')) return ElMessage.warning('请选择图片文件')
  if (file.size > 2 * 1024 * 1024) return ElMessage.warning('头像图片不能超过 2MB')
  const reader = new FileReader()
  reader.onload = () => { form.avatarUrl = String(reader.result || '') }
  reader.readAsDataURL(file)
}
function clearAvatar() { form.avatarUrl = '' }
function focusAvatarUrl() {
  requestAnimationFrame(() => document.querySelector('.member-avatar-input .el-input__inner')?.focus())
}
onMounted(load)
</script>
