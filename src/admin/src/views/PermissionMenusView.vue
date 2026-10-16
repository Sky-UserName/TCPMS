<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <p>同步用户端个人首页的民宿管理入口，配置页面路径、图标、权重和可用状态。</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="RefreshRight" @click="syncFromMiniapp">同步用户端菜单</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">新增菜单</el-button>
      </div>
    </section>

    <el-alert class="sync-alert" type="info" :closable="false" show-icon>
      <template #title>同步来源：用户端 <code>pages/me/index.vue</code> 的 <code>merchantEntries</code>。菜单保存后会同步到服务端。</template>
    </el-alert>

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
          <el-button :icon="Refresh" :loading="loading" title="刷新菜单" @click="refresh" />
          <el-button type="success" :icon="Plus" @click="openCreate">添加</el-button>
          <el-button type="primary" plain :disabled="selectedRows.length !== 1" @click="openEdit(selectedRows[0])">编辑</el-button>
          <el-button type="danger" plain :disabled="!selectedRows.length" @click="removeSelected">删除</el-button>
          <strong>菜单列表</strong>
          <span class="toolbar-count">{{ filteredRows.length }} 个入口</span>
        </div>
        <div class="toolbar-right">
          <el-input v-model="keyword" clearable class="table-search" placeholder="搜索标题、页面路径" :prefix-icon="Search" @keyup.enter="page = 1" />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedRows" class="clean-table admin-data-table" row-key="key" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column label="规则（页面路径）" min-width="280" fixed="left">
          <template #default="{ row }">
            <div class="route-cell"><strong>{{ row.route }}</strong><small>{{ row.key }}</small></div>
          </template>
        </el-table-column>
        <el-table-column prop="title" label="标题" min-width="150" />
        <el-table-column label="图标" width="110">
          <template #default="{ row }">
            <el-image v-if="row.iconUrl" class="menu-icon-image" :src="row.iconUrl" :preview-src-list="[row.iconUrl]" preview-teleported fit="cover" />
            <span v-else class="menu-icon-fallback"><el-icon><Menu /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column prop="iconKey" label="图标标识" width="120" />
        <el-table-column prop="weight" label="权重" width="90" sortable />
        <el-table-column label="状态" width="100">
          <template #default="{ row }"><span class="dot-status" :class="row.status === 'normal' ? 'success' : 'muted'"><i></i>{{ statusLabel(row.status) }}</span></template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" min-width="170" />
        <el-table-column prop="updatedAt" label="更新时间" min-width="170" />
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

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '新增权限菜单' : '编辑权限菜单'" width="660px" destroy-on-close>
      <el-form :model="form" label-width="108px" class="dialog-form">
        <el-form-item label="页面路径" required><el-input v-model="form.route" placeholder="/pages/merchant/rooms" /></el-form-item>
        <div class="form-grid">
          <el-form-item label="标题" required><el-input v-model="form.title" placeholder="例如：房间管理" /></el-form-item>
          <el-form-item label="图标标识"><el-input v-model="form.iconKey" placeholder="例如：house" /></el-form-item>
          <el-form-item label="权重"><el-input-number v-model="form.weight" :min="0" controls-position="right" /></el-form-item>
          <el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio value="normal">正常</el-radio><el-radio value="hidden">隐藏</el-radio></el-radio-group></el-form-item>
        </div>
        <el-form-item label="图标">
          <div class="icon-editor">
            <div class="icon-upload-preview">
              <el-image v-if="form.iconUrl" :src="form.iconUrl" fit="cover" />
              <span v-else><el-icon><Menu /></el-icon><small>{{ form.iconKey || '默认图标' }}</small></span>
            </div>
            <div class="icon-editor-actions">
              <el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="handleIconChange">
                <el-button type="primary" plain :icon="Upload">上传图片</el-button>
              </el-upload>
              <el-button v-if="form.iconUrl" text type="danger" @click="form.iconUrl = ''">移除图片</el-button>
              <small>上传后会立即显示预览；API 接入后保存到对象存储。</small>
            </div>
          </div>
        </el-form-item>
      </el-form>
      <template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存菜单</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Menu, Plus, Refresh, RefreshRight, Search, Upload } from '@element-plus/icons-vue'
import { get, post, put, del } from '../api'
import { MINIAPP_MERCHANT_MENU } from '../data/permissionMenus'

const statusTabs = [
  { key: 'all', label: '全部' },
  { key: 'normal', label: '正常' },
  { key: 'hidden', label: '隐藏' }
]
const rows = ref([])
const selectedRows = ref([])
const loading = ref(false)
const keyword = ref('')
const activeTab = ref('all')
const page = ref(1)
const pageSize = ref(10)
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive(emptyForm())

const filteredRows = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  return rows.value
    .filter((row) => activeTab.value === 'all' || row.status === activeTab.value)
    .filter((row) => !query || `${row.route} ${row.title} ${row.key} ${row.iconKey}`.toLowerCase().includes(query))
    .sort((a, b) => Number(a.weight || 0) - Number(b.weight || 0))
})
const pagedRows = computed(() => filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch([activeTab, keyword, pageSize], () => { page.value = 1 })

function emptyForm() {
  return { key: '', title: '', iconKey: '', iconUrl: '', route: '', weight: 0, status: 'normal' }
}
async function loadRows({ notify = false } = {}) {
  loading.value = true
  try {
    rows.value = await get('/admin/permission-menus')
    if (notify) ElMessage.success('权限菜单已刷新')
  } catch (error) {
    ElMessage.error(error.message || '权限菜单加载失败')
  } finally {
    loading.value = false
  }
}
function refresh() { return loadRows({ notify: true }) }
function countForTab(key) { return key === 'all' ? rows.value.length : rows.value.filter((row) => row.status === key).length }
function statusLabel(value) { return value === 'hidden' ? '隐藏' : '正常' }
function openCreate() {
  Object.assign(form, emptyForm(), { weight: rows.value.length + 1 })
  dialog.mode = 'create'
  dialog.visible = true
}
function openEdit(row) {
  Object.assign(form, emptyForm(), row)
  dialog.mode = 'edit'
  dialog.visible = true
}
function handleIconChange(uploadFile) {
  const raw = uploadFile?.raw
  if (!raw) return
  const reader = new FileReader()
  reader.onload = () => { form.iconUrl = String(reader.result) }
  reader.readAsDataURL(raw)
}
async function syncFromMiniapp() {
  loading.value = true
  try {
    rows.value = await post('/admin/permission-menus/sync', {
      items: MINIAPP_MERCHANT_MENU.map((item, index) => ({
        ...item,
        iconUrl: null,
        weight: index + 1,
        status: 'normal'
      }))
    })
    window.dispatchEvent(new CustomEvent('tcpms:permission-menu-updated'))
    ElMessage.success(`已同步用户端 ${rows.value.length} 个民宿管理入口`)
  } catch (error) {
    ElMessage.error(error.message || '菜单同步失败')
  } finally {
    loading.value = false
  }
}
async function save() {
  if (!form.route.trim() || !form.title.trim()) return ElMessage.warning('请填写页面路径和标题')
  dialog.saving = true
  try {
    const key = form.key.trim() || `custom-${Date.now()}`
    const payload = {
      key,
      title: form.title.trim(),
      iconKey: form.iconKey?.trim() || '',
      iconUrl: form.iconUrl || null,
      route: form.route.trim(),
      weight: Number(form.weight) || 0,
      status: form.status
    }
    const saved = dialog.mode === 'create'
      ? await post('/admin/permission-menus', payload)
      : await put(`/admin/permission-menus/${encodeURIComponent(form.key)}`, payload)
    if (dialog.mode === 'create') rows.value.push(saved)
    else {
      const index = rows.value.findIndex((row) => row.key === form.key)
      if (index >= 0) rows.value[index] = saved
    }
    window.dispatchEvent(new CustomEvent('tcpms:permission-menu-updated'))
    dialog.visible = false
    ElMessage.success('权限菜单已保存')
  } catch (error) {
    ElMessage.error(error.message || '权限菜单保存失败')
  } finally {
    dialog.saving = false
  }
}
async function removeRows(items) {
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 个权限菜单吗？`, '删除权限菜单', { type: 'warning' })
    await Promise.all(items.map((item) => del(`/admin/permission-menus/${encodeURIComponent(item.key)}`)))
    const keys = new Set(items.map((item) => item.key))
    rows.value = rows.value.filter((row) => !keys.has(row.key))
    selectedRows.value = []
    window.dispatchEvent(new CustomEvent('tcpms:permission-menu-updated'))
    ElMessage.success('权限菜单已删除')
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') ElMessage.error(error.message || '权限菜单删除失败')
  }
}
function removeSelected() { return removeRows(selectedRows.value) }
onMounted(() => {
  loadRows()
  window.addEventListener('tcpms:permission-menu-updated', loadRows)
})
onBeforeUnmount(() => window.removeEventListener('tcpms:permission-menu-updated', loadRows))
</script>

<style scoped>
.sync-alert{margin-top:-4px}
.sync-alert code{padding:2px 5px;border-radius:4px;background:rgba(15,119,119,.08);color:var(--admin-primary-dark);font-size:12px}
.route-cell strong,.route-cell small{display:block}
.route-cell strong{overflow:hidden;color:var(--admin-text);text-overflow:ellipsis;white-space:nowrap}
.route-cell small{margin-top:4px;color:var(--admin-muted);font-size:11px}
.menu-icon-image,.menu-icon-fallback{display:inline-flex;width:42px;height:42px;align-items:center;justify-content:center;border:1px solid var(--admin-border);border-radius:9px;background:var(--admin-primary-soft);color:var(--admin-primary-dark);object-fit:cover}
.menu-icon-fallback{font-size:18px}
.icon-editor{display:flex;align-items:center;gap:14px;width:100%}
.icon-upload-preview{display:flex;width:88px;height:88px;flex:0 0 auto;align-items:center;justify-content:center;overflow:hidden;border:1px dashed var(--admin-border);border-radius:10px;background:var(--admin-surface-soft);color:var(--admin-primary-dark)}
.icon-upload-preview .el-image{width:100%;height:100%}
.icon-upload-preview > span{display:flex;align-items:center;flex-direction:column;gap:6px}
.icon-upload-preview small{max-width:76px;overflow:hidden;color:var(--admin-muted);font-size:10px;text-overflow:ellipsis;white-space:nowrap}
.icon-editor-actions{display:flex;align-items:flex-start;flex-direction:column;gap:7px}
.icon-editor-actions small{color:var(--admin-muted);font-size:11px;line-height:1.5}
@media (max-width:640px){.icon-editor{align-items:flex-start;flex-direction:column}}
</style>
