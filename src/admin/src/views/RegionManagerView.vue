<template>
  <div class="view-stack compact-view region-view">
    <section class="page-heading">
      <div>
        <p>维护中国省、市、区县三级行政区，供门店定位和地区筛选使用。</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
        <el-button type="primary" :icon="Plus" :disabled="!apiBacked" @click="openCreate">添加地区</el-button>
      </div>
    </section>

    <el-alert v-if="!apiBacked" title="地区接口不可用，当前仅展示只读种子数据，新增、编辑和删除已停用。" type="warning" :closable="false" show-icon />

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs" role="tablist">
        <button
          v-for="tab in statusTabs"
          :key="tab.key"
          type="button"
          role="tab"
          class="status-tab"
          :class="{ active: activeStatus === tab.key }"
          :aria-selected="activeStatus === tab.key"
          @click="activeStatus = tab.key"
        >
          {{ tab.label }} <span>{{ countForStatus(tab.key) }}</span>
        </button>
      </div>

      <div class="region-filter-row">
        <el-radio-group v-model="levelFilter" size="small">
          <el-radio-button label="all">全部省市区</el-radio-button>
          <el-radio-button label="Province">省级</el-radio-button>
          <el-radio-button label="City">市级</el-radio-button>
          <el-radio-button label="District">区县</el-radio-button>
        </el-radio-group>
        <el-input
          v-model="keyword"
          clearable
          class="table-search"
          placeholder="搜索编码、地区名称"
          :prefix-icon="Search"
          @keyup.enter="page = 1"
        />
        <el-button :icon="Search" @click="page = 1">查询</el-button>
        <el-button text @click="resetFilters">重置</el-button>
      </div>

      <div v-if="browsePath.length" class="browse-path">
        <span class="browse-label">当前位置</span>
        <el-button text @click="clearBrowse">全部地区</el-button>
        <template v-for="(item, index) in browsePath" :key="item.code">
          <span class="browse-separator">/</span>
          <el-button text @click="jumpToBrowse(index)">{{ item.name }}</el-button>
        </template>
      </div>
    </el-card>

    <el-card class="panel-card" shadow="never">
      <div class="table-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新地区" :loading="loading" @click="load" />
          <el-button type="success" :icon="Plus" :disabled="!apiBacked" @click="openCreate">添加</el-button>
          <el-button type="primary" plain :disabled="!apiBacked || selectedRows.length !== 1" @click="openEdit(selectedRows[0])">编辑</el-button>
          <el-button type="danger" plain :disabled="!apiBacked || !selectedRows.length" @click="removeSelected">删除</el-button>
          <strong>地区列表</strong>
          <span class="toolbar-count">{{ filteredRows.length }} 条地区</span>
          <el-tag v-if="apiBacked" size="small" type="success" effect="plain">接口数据</el-tag>
          <el-tag v-else size="small" type="info" effect="plain">种子数据</el-tag>
        </div>
        <el-dropdown trigger="click">
          <el-button text :icon="MoreFilled" title="更多操作" />
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="exportRows">导出当前列表</el-dropdown-item>
              <el-dropdown-item @click="clearSelection">清空选择</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>

      <el-table
        v-loading="loading"
        :data="pagedRows"
        class="clean-table admin-data-table region-table"
        row-key="code"
        @selection-change="selectedRows = $event"
      >
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column prop="code" label="编码" width="112" />
        <el-table-column prop="shortName" label="简称" min-width="150" />
        <el-table-column prop="name" label="名称" min-width="190" />
        <el-table-column prop="fullName" label="全称" min-width="190">
          <template #default="{ row }">{{ row.fullName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="levelLabel" label="级别" width="92">
          <template #default="{ row }"><el-tag size="small" effect="plain">{{ levelLabel(row.level) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="initial" label="首字母" width="82" />
        <el-table-column label="是否热门" width="100">
          <template #default="{ row }">
            <el-switch v-model="row.isHot" :disabled="!apiBacked" inline-prompt active-text="是" inactive-text="否" @change="toggleHot(row)" />
          </template>
        </el-table-column>
        <el-table-column label="上下架状态" width="120">
          <template #default="{ row }">
            <span class="dot-status" :class="row.isEnabled ? 'success' : 'muted'"><i></i>{{ row.isEnabled ? '上架' : '下架' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="parentName" label="上级地区" min-width="170">
          <template #default="{ row }">{{ row.parentName || '中国' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="250" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <el-button v-if="row.level !== 'District'" type="primary" link @click="drillInto(row)">
                {{ row.level === 'Province' ? '城市' : '区县' }}
              </el-button>
              <el-button type="primary" link :disabled="!apiBacked" @click="openEdit(row)">编辑</el-button>
              <el-button type="danger" link :disabled="!apiBacked" @click="removeRows([row])">删除</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredRows.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredRows.length) }} 条记录，共 {{ filteredRows.length }} 条记录</span>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100]"
          layout="sizes, prev, pager, next, jumper"
          :total="filteredRows.length"
          background
        />
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '添加地区' : '编辑地区'" width="620px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="96px" class="dialog-form" @submit.prevent>
        <div class="form-grid">
          <el-form-item label="编码">
            <el-input v-model="form.code" :disabled="dialog.mode === 'edit'" placeholder="新增时可留空，系统自动生成" />
          </el-form-item>
          <el-form-item label="级别" required>
            <el-select v-model="form.level" :disabled="dialog.mode === 'edit'" @change="handleFormLevelChange">
              <el-option v-for="option in levelOptions" :key="option.value" :label="option.label" :value="option.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="简称" required>
            <el-input v-model="form.shortName" placeholder="例如：北京" />
          </el-form-item>
          <el-form-item label="名称" required>
            <el-input v-model="form.name" placeholder="例如：北京市" />
          </el-form-item>
          <el-form-item label="全称" class="field-wide">
            <el-input v-model="form.fullName" placeholder="可选，默认使用名称" />
          </el-form-item>
          <el-form-item v-if="form.level !== 'Province'" label="上级地区" class="field-wide" required>
            <el-cascader
              v-model="form.parentPath"
              :options="parentOptions"
              :props="{ checkStrictly: true, value: 'value', label: 'label', children: 'children' }"
              clearable
              filterable
              placeholder="请选择上级省或市"
              @change="handleParentChange"
            />
          </el-form-item>
          <el-form-item label="首字母">
            <el-input v-model="form.initial" maxlength="1" placeholder="例如：B" />
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" controls-position="right" />
          </el-form-item>
          <el-form-item label="是否热门">
            <el-switch v-model="form.isHot" active-text="是" inactive-text="否" />
          </el-form-item>
          <el-form-item label="上下架状态">
            <el-radio-group v-model="form.isEnabled">
              <el-radio :value="true">上架</el-radio>
              <el-radio :value="false">下架</el-radio>
            </el-radio-group>
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="dialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="dialog.saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { MoreFilled, Plus, Refresh, Search } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'
import { buildChinaRegionTree, normalizeRegionPayload, regionTreeFromRows, seedChinaRegions } from '../data/regions'

const rows = ref([])
const loading = ref(false)
const apiBacked = ref(false)
const selectedRows = ref([])
const activeStatus = ref('all')
const levelFilter = ref('all')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(50)
const browsePath = ref([])
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive(emptyForm())

const statusTabs = [
  { key: 'all', label: '全部' },
  { key: 'active', label: '上架' },
  { key: 'hidden', label: '下架' }
]
const levelOptions = [
  { value: 'Province', label: '省/直辖市' },
  { value: 'City', label: '城市' },
  { value: 'District', label: '区县' }
]

const regionTree = computed(() => regionTreeFromRows(rows.value.length ? rows.value : buildChinaRegionTree()))
const parentOptions = computed(() => {
  if (form.level === 'City') {
    return regionTree.value
      .filter((item) => item.level === 'Province')
      .map((item) => ({ value: item.code, label: item.name }))
  }
  if (form.level === 'District') {
    return regionTree.value
      .filter((item) => item.level === 'Province')
      .map((province) => ({
        value: province.code,
        label: province.name,
        children: (province.children || [])
          .filter((item) => item.level === 'City')
          .map((city) => ({ value: city.code, label: city.name }))
      }))
  }
  return []
})
const filteredRows = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  const scope = browsePath.value.at(-1)
  return rows.value.filter((row) => {
    const statusMatches = activeStatus.value === 'all' || (activeStatus.value === 'active' ? row.isEnabled : !row.isEnabled)
    const levelMatches = levelFilter.value === 'all' || row.level === levelFilter.value
    const browseMatches = !scope || row.parentCode === scope.code
    const queryMatches = !query || [row.code, row.shortName, row.name, row.fullName, row.parentName].some((value) => String(value || '').toLowerCase().includes(query))
    return statusMatches && levelMatches && browseMatches && queryMatches
  })
})
const pagedRows = computed(() => filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch([activeStatus, levelFilter, keyword, pageSize], () => {
  page.value = 1
})

function emptyForm() {
  return {
    code: '',
    parentCode: null,
    parentPath: [],
    parentName: '中国',
    level: 'Province',
    shortName: '',
    name: '',
    fullName: '',
    initial: '',
    isHot: false,
    isEnabled: true,
    status: 'active',
    sortOrder: 0
  }
}

function toOption(node) {
  return {
    value: node.code,
    label: node.name,
    children: node.children?.length ? node.children.map(toOption) : undefined
  }
}

function levelLabel(level) {
  return ({ Province: '省', City: '市', District: '区' })[level] || level || '-'
}

function countForStatus(status) {
  if (status === 'all') return rows.value.length
  return rows.value.filter((row) => status === 'active' ? row.isEnabled : !row.isEnabled).length
}

function resetFilters() {
  activeStatus.value = 'all'
  levelFilter.value = 'all'
  keyword.value = ''
  clearBrowse()
}

function clearSelection() {
  selectedRows.value = []
}

function clearBrowse() {
  browsePath.value = []
}

function jumpToBrowse(index) {
  browsePath.value = browsePath.value.slice(0, index + 1)
  page.value = 1
}

function drillInto(row) {
  browsePath.value = [...browsePath.value, row]
  page.value = 1
}

function findPath(code) {
  const path = []
  const byCode = new Map(rows.value.map((row) => [String(row.code), row]))
  let current = byCode.get(String(code))
  while (current) {
    path.unshift(current)
    current = current.parentCode ? byCode.get(String(current.parentCode)) : null
  }
  return path.map((item) => item.code)
}

function parentPathFor(row) {
  const path = findPath(row.parentCode)
  return row.level === 'City' ? path.slice(-1) : path.slice(-2)
}

function handleFormLevelChange() {
  form.parentPath = []
  form.parentCode = null
  form.parentName = '中国'
}

function handleParentChange(path) {
  const code = path?.at(-1) || null
  const parent = rows.value.find((row) => String(row.code) === String(code))
  form.parentCode = code
  form.parentName = parent?.name || '中国'
}

function openCreate() {
  Object.assign(form, emptyForm())
  dialog.mode = 'create'
  dialog.visible = true
}

function openEdit(row) {
  if (!row) return
  Object.assign(form, emptyForm(), {
    ...row,
    parentPath: parentPathFor(row),
    status: row.isEnabled ? 'active' : 'hidden'
  })
  dialog.mode = 'edit'
  dialog.visible = true
}

function makeCode() {
  const existing = new Set(rows.value.map((row) => String(row.code)))
  if (form.level === 'Province') {
    for (let prefix = 90; prefix <= 99; prefix += 1) {
      const code = `${prefix}0000`
      if (!existing.has(code)) return code
    }
  }
  const parentCode = form.parentPath?.at(-1) || form.parentCode || ''
  if (form.level === 'City' && /^\d{6}$/.test(parentCode)) {
    for (let serial = 1; serial <= 99; serial += 1) {
      const code = `${parentCode.slice(0, 2)}${String(serial).padStart(2, '0')}00`
      if (!existing.has(code)) return code
    }
  }
  if (form.level === 'District' && /^\d{6}$/.test(parentCode)) {
    for (let serial = 1; serial <= 99; serial += 1) {
      const code = `${parentCode.slice(0, 4)}${String(serial).padStart(2, '0')}`
      if (!existing.has(code)) return code
    }
  }
  return ''
}

function buildPayload() {
  const parentCode = form.level === 'Province' ? null : (form.parentPath?.at(-1) || form.parentCode)
  const parent = rows.value.find((row) => String(row.code) === String(parentCode))
  return {
    ...form,
    code: form.code || makeCode(),
    parentCode: parentCode || null,
    parentName: parent?.name || '中国',
    fullName: form.fullName || form.name,
    initial: form.initial || String(form.shortName || form.name).slice(0, 1).toUpperCase(),
    status: form.isEnabled ? 'active' : 'hidden'
  }
}

async function save() {
  if (!apiBacked.value) return ElMessage.error('地区接口不可用，未保存任何更改')
  if (!form.name || !form.shortName) return ElMessage.warning('请填写简称和名称')
  if (form.level !== 'Province' && !form.parentPath?.length && !form.parentCode) return ElMessage.warning('请选择上级地区')

  const payload = buildPayload()
  dialog.saving = true
  try {
    if (dialog.mode === 'create') await post('/admin/regions', payload)
    else await put(`/admin/regions/${payload.code}`, payload)
    dialog.visible = false
    ElMessage.success('地区已保存')
    await load()
  } catch (error) {
    ElMessage.error(error.message || '地区保存失败，服务端数据未变更')
  } finally {
    dialog.saving = false
  }
}

async function removeRows(items) {
  if (!apiBacked.value) return ElMessage.error('地区接口不可用，未删除任何数据')
  if (!items.length) return
  const parentCodes = new Set(rows.value.map((row) => String(row.parentCode || '')))
  const blocked = items.find((row) => parentCodes.has(String(row.code)))
  if (blocked) return ElMessage.warning(`“${blocked.name}”下仍有子地区，请先处理子地区`)
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 条地区吗？`, '删除确认', { type: 'warning' })
    for (const row of items) await del(`/admin/regions/${row.code}`)
    clearSelection()
    ElMessage.success('地区已删除')
    await load()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

function removeSelected() {
  removeRows(selectedRows.value)
}

async function toggleHot(row) {
  if (!apiBacked.value) return ElMessage.error('地区接口不可用，未更改热门状态')
  const value = Boolean(row.isHot)
  try {
    await put(`/admin/regions/${row.code}`, { ...row, isHot: value })
  } catch (error) {
    ElMessage.error(error.message || '热门状态未保存')
    await load()
  }
}

function readLocalRows() {
  return seedChinaRegions()
}

async function load() {
  loading.value = true
  try {
    let payload
    try {
      payload = await get('/admin/regions?includeDisabled=true')
    } catch {
      payload = await get('/admin/regions/tree')
    }
    const normalized = normalizeRegionPayload(payload)
    if (!normalized.length) throw new Error('empty region response')
    rows.value = normalized
    apiBacked.value = true
  } catch {
    rows.value = readLocalRows()
    apiBacked.value = false
  } finally {
    loading.value = false
  }
}

function exportRows() {
  const header = ['编码', '简称', '名称', '全称', '级别', '首字母', '是否热门', '上下架状态', '上级地区'].join(',')
  const body = filteredRows.value.map((row) => [
    row.code,
    row.shortName,
    row.name,
    row.fullName,
    levelLabel(row.level),
    row.initial,
    row.isHot ? '是' : '否',
    row.isEnabled ? '上架' : '下架',
    row.parentName || '中国'
  ].map((value) => JSON.stringify(value ?? '')).join(',')).join('\n')
  const blob = new Blob([`\ufeff${header}\n${body}`], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = '地区管理.csv'
  link.click()
  URL.revokeObjectURL(url)
  ElMessage.success('导出已开始')
}

onMounted(load)
</script>

<style scoped>
.region-filter-row{display:flex;align-items:center;gap:10px;flex-wrap:wrap}
.region-filter-row .table-search{width:260px;margin-left:auto}
.browse-path{display:flex;align-items:center;gap:3px;margin-top:12px;padding-top:11px;border-top:1px solid var(--line);font-size:13px}
.browse-label{color:var(--admin-muted);margin-right:5px}
.browse-separator{color:var(--admin-muted)}
.region-table :deep(.el-table__cell){white-space:nowrap}
.region-table :deep(.el-switch){--el-switch-on-color:var(--admin-accent);--el-switch-off-color:#cfd8dd}
.region-table :deep(.el-switch__core){min-width:50px}
.row-actions{display:flex;align-items:center;gap:2px}
.list-footer{display:flex;align-items:center;justify-content:space-between;gap:16px;padding-top:16px;color:var(--admin-muted);font-size:12px}
.toolbar-left{display:flex;align-items:center;gap:9px;flex-wrap:wrap}
.toolbar-left strong{margin-left:6px;color:var(--text-primary)}
.toolbar-count{color:var(--admin-muted)}
.dialog-form :deep(.el-select),.dialog-form :deep(.el-cascader),.dialog-form :deep(.el-input-number){width:100%}
.dialog-form :deep(.el-radio-group){display:flex;gap:18px;height:32px;align-items:center}
@media (max-width: 780px){
  .region-filter-row .table-search{width:100%;margin-left:0}
  .list-footer{align-items:flex-start;flex-direction:column}
}
</style>
