<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
        <span class="eyebrow">PROPERTY NETWORK</span>
        <h2>门店管理</h2>
        <p>维护营业门店、位置坐标和小程序展示状态。</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增门店</el-button>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs">
        <button v-for="tab in storeTabs" :key="tab.key" type="button" role="tab" class="status-tab" :class="{ active: statusTab === tab.key }" :aria-selected="statusTab === tab.key" @click="statusTab = tab.key">
          {{ tab.label }} <span>{{ countForTab(tab.key) }}</span>
        </button>
      </div>
      <div class="filter-row">
        <el-input v-model="filters.keyword" clearable placeholder="搜索门店名称或地址" :prefix-icon="Search" @keyup.enter="load" />
        <el-select v-model="filters.status" clearable placeholder="营业状态" @change="load">
          <el-option label="全部状态" value="" />
          <el-option label="营业中" value="Active" />
          <el-option label="待审核" value="PendingReview" />
          <el-option label="草稿" value="Draft" />
          <el-option label="已下架" value="Offline" />
          <el-option label="已禁用" value="Disabled" />
        </el-select>
        <el-select v-model="filters.map" clearable placeholder="地图状态" @change="load">
          <el-option label="已配置坐标" value="configured" />
          <el-option label="待配置坐标" value="pending" />
        </el-select>
        <el-button :icon="Search" @click="load">查询</el-button>
        <el-button text @click="reset">重置</el-button>
      </div>
    </el-card>

    <el-card class="panel-card" shadow="never">
      <div class="table-toolbar">
        <div class="toolbar-left"><el-button :icon="Refresh" title="刷新门店" :loading="loading" @click="load" /><el-button type="success" :icon="Plus" @click="openCreate">添加</el-button><el-button type="primary" plain :disabled="!selectedRows.length" @click="approveSelected">批量通过</el-button><strong>门店列表</strong><span class="toolbar-count">{{ filteredStores.length }} 家门店</span></div>
        <el-button text :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
      </div>
      <el-table v-loading="loading" :data="filteredStores" class="clean-table admin-data-table" row-key="id" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column prop="id" label="ID" width="74" />
        <el-table-column label="账号" min-width="160"><template #default="{ row }">{{ row.code || row.id.slice(0, 10) }}</template></el-table-column>
        <el-table-column label="民宿名称" min-width="210">
          <template #default="{ row }">
            <div class="store-cell"><span class="store-avatar">{{ row.name?.slice(0, 1) }}</span><div><strong>{{ row.name }}</strong><small>{{ row.id }}</small></div></div>
          </template>
        </el-table-column>
        <el-table-column label="房东姓名" width="130"><template #default="{ row }">{{ row.ownerName || '门店管理员' }}</template></el-table-column>
        <el-table-column prop="phone" label="房东电话" width="145" />
        <el-table-column label="房源等级" width="120"><template #default="{ row }"><el-tag size="small" effect="light">{{ row.level || (row.status === 'Active' ? '优质民宿' : '无等级') }}</el-tag></template></el-table-column>
        <el-table-column label="审核状态" width="115"><template #default="{ row }"><span class="dot-status" :class="auditTone(row.status)"><i></i>{{ auditLabel(row.status) }}</span></template></el-table-column>
        <el-table-column label="详细地址" min-width="260"><template #default="{ row }"><span class="muted-text">{{ row.address || '尚未填写地址' }}</span></template></el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'PendingReview'" link type="success" @click="changeStatus(row, 'Active')">通过</el-button>
            <el-button v-if="row.status === 'PendingReview'" link type="primary" @click="openEdit(row)">查看资料</el-button>
            <el-button link type="primary" @click="emit('navigate', 'roomManagement')">房间</el-button>
            <el-button link type="primary" @click="openGeo(row)">地图</el-button>
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-dropdown @command="(command) => changeStatus(row, command)">
              <el-button link type="primary">更多<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="Active">上架营业</el-dropdown-item>
                  <el-dropdown-item command="Offline">下架门店</el-dropdown-item>
                  <el-dropdown-item command="Disabled">禁用门店</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>
      <div class="list-footer"><span>显示 {{ filteredStores.length }} 家门店</span><el-button text :icon="Refresh" @click="load">刷新列表</el-button></div>
    </el-card>

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '新增门店' : '编辑门店'" width="900px" destroy-on-close>
      <el-form :model="form" label-width="96px" class="dialog-form store-form">
        <div class="form-section-title">账号信息</div>
        <div class="form-grid">
          <el-form-item label="用户名"><el-input v-model="form.ownerUsername" placeholder="房东账号或小程序用户标识" /></el-form-item>
          <el-form-item label="昵称"><el-input v-model="form.ownerNickname" placeholder="房东昵称" /></el-form-item>
          <el-form-item label="房东姓名"><el-input v-model="form.ownerName" placeholder="真实姓名" /></el-form-item>
          <el-form-item label="房东手机号"><el-input v-model="form.ownerPhone" placeholder="用于审核联系" /></el-form-item>
        </div>
        <el-form-item label="头像">
          <div class="asset-field">
            <el-input v-model="form.ownerAvatarUrl" placeholder="输入头像地址，或选择本地图片" />
            <el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('ownerAvatarUrl', file)">
              <el-button type="primary" plain :icon="Upload">选择图片</el-button>
            </el-upload>
            <el-avatar v-if="uploadPreview.ownerAvatarUrl || form.ownerAvatarUrl" :src="uploadPreview.ownerAvatarUrl || form.ownerAvatarUrl" :size="38"><el-icon><Picture /></el-icon></el-avatar>
          </div>
        </el-form-item>

        <div class="form-section-title">门店信息</div>
        <div class="form-grid">
          <el-form-item label="民宿名称" required><el-input v-model="form.name" placeholder="例如：上海人民广场店" /></el-form-item>
          <el-form-item label="门店编码" required><el-input v-model="form.code" :disabled="dialog.mode === 'edit'" placeholder="大写字母、数字和短横线" /></el-form-item>
          <el-form-item label="房东电话"><el-input v-model="form.phone" placeholder="门店公开联系电话" /></el-form-item>
          <el-form-item label="评分"><el-input-number v-model="form.rating" :min="0" :max="5" :precision="1" :step="0.1" controls-position="right" /></el-form-item>
          <el-form-item label="省份"><el-input v-model="form.province" /></el-form-item>
          <el-form-item label="城市"><el-input v-model="form.city" /></el-form-item>
          <el-form-item label="区域"><el-input v-model="form.district" /></el-form-item>
          <el-form-item label="排序"><el-input-number v-model="form.sortOrder" :min="0" controls-position="right" /></el-form-item>
        </div>
        <el-form-item label="详细地址"><el-input v-model="form.address" placeholder="填写门店详细地址，可在地图配置中解析坐标" /></el-form-item>
        <el-form-item label="门店简介"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
        <div class="form-grid">
          <el-form-item label="经度"><el-input-number v-model="form.longitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
          <el-form-item label="纬度"><el-input-number v-model="form.latitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
          <el-form-item label="状态"><el-select v-model="form.status"><el-option v-for="item in statusOptions" :key="item.value" v-bind="item" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">资质与图片</div>
        <div class="asset-grid">
          <el-form-item label="身份证正面"><div class="asset-field"><el-input v-model="form.idCardFrontUrl" placeholder="图片地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('idCardFrontUrl', file)"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div></el-form-item>
          <el-form-item label="身份证反面"><div class="asset-field"><el-input v-model="form.idCardBackUrl" placeholder="图片地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('idCardBackUrl', file)"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div></el-form-item>
          <el-form-item label="房产证"><div class="asset-field"><el-input v-model="form.propertyCertificateUrl" placeholder="图片地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('propertyCertificateUrl', file)"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div></el-form-item>
          <el-form-item label="营业执照"><div class="asset-field"><el-input v-model="form.businessLicenseUrl" placeholder="图片地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('businessLicenseUrl', file)"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div></el-form-item>
          <el-form-item label="门头照片"><div class="asset-field"><el-input v-model="form.doorImageUrl" placeholder="图片地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview('doorImageUrl', file)"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div></el-form-item>
          <el-form-item label="房间照片"><el-input v-model="form.roomImageUrlsText" type="textarea" :rows="2" placeholder="每行一个图片地址" /></el-form-item>
        </div>
      </el-form>
      <template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存门店</el-button></template>
    </el-dialog>

    <el-dialog v-model="geoDialog.visible" title="高德地图位置配置" width="620px" destroy-on-close>
      <div class="geo-dialog">
        <div class="geo-intro"><span class="geo-pin"><el-icon><Location /></el-icon></span><div><strong>{{ geoDialog.store?.name }}</strong><small>{{ geoDialog.store?.address || '暂无详细地址' }}</small></div></div>
        <el-alert v-if="geoDialog.error" :title="geoDialog.error" type="warning" show-icon :closable="false" />
        <el-form :model="geoForm" label-width="88px" class="dialog-form">
          <el-form-item label="解析城市"><el-input v-model="geoForm.city" placeholder="例如：上海市" /></el-form-item>
          <el-form-item label="详细地址"><el-input v-model="geoForm.address" placeholder="输入完整地址后调用高德地理编码" /></el-form-item>
          <div class="form-grid">
            <el-form-item label="经度"><el-input-number v-model="geoForm.longitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
            <el-form-item label="纬度"><el-input-number v-model="geoForm.latitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
          </div>
        </el-form>
        <div class="geo-preview"><div class="map-grid"></div><span class="map-center-pin"><el-icon><LocationFilled /></el-icon></span><span class="map-preview-label">{{ geoForm.longitude && geoForm.latitude ? `${geoForm.longitude}, ${geoForm.latitude}` : '待配置坐标' }}</span></div>
      </div>
      <template #footer><el-button @click="geoDialog.visible = false">取消</el-button><el-button :loading="geoDialog.loading" @click="reverseGeo">逆地理解析</el-button><el-button type="primary" :loading="geoDialog.loading" @click="geocode">地址解析并保存</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Location, LocationFilled, Picture, Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'
import { get, post, put } from '../api'

const emit = defineEmits(['navigate'])
const loading = ref(false)
const stores = ref([])
const filters = reactive({ keyword: '', status: '', map: '' })
const statusTab = ref('all')
const selectedRows = ref([])
const storeTabs = [
  { key: 'all', label: '全部' },
  { key: 'pending', label: '审核中' },
  { key: 'approved', label: '通过' },
  { key: 'rejected', label: '已拒绝' }
]
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const geoDialog = reactive({ visible: false, loading: false, store: null, error: '' })
const form = reactive(emptyForm())
const uploadPreview = reactive({ ownerAvatarUrl: '', idCardFrontUrl: '', idCardBackUrl: '', propertyCertificateUrl: '', businessLicenseUrl: '', doorImageUrl: '' })
const geoForm = reactive({ address: '', city: '', longitude: null, latitude: null })
const statusOptions = [
  { label: '草稿', value: 'Draft' },
  { label: '待审核', value: 'PendingReview' },
  { label: '营业中', value: 'Active' },
  { label: '已下架', value: 'Offline' },
  { label: '已禁用', value: 'Disabled' }
]

function emptyForm() {
  return {
    id: '', name: '', code: '', description: '', province: '', city: '', district: '', address: '', phone: '',
    longitude: null, latitude: null, status: 'Draft', sortOrder: 0, rating: 0,
    ownerUsername: '', ownerNickname: '', ownerAvatarUrl: '', ownerName: '', ownerPhone: '',
    idCardFrontUrl: '', idCardBackUrl: '', propertyCertificateUrl: '', businessLicenseUrl: '', doorImageUrl: '',
    roomImageUrlsText: '', roomImageUrlsJson: ''
  }
}
const filteredStores = computed(() => stores.value.filter((row) => {
  const keyword = filters.keyword.trim().toLowerCase()
  const matchesKeyword = !keyword || [row.name, row.address, row.phone].join(' ').toLowerCase().includes(keyword)
  const matchesMap = !filters.map || (filters.map === 'configured' ? row.hasMapLocation : !row.hasMapLocation)
  return matchesKeyword && matchesTab(row) && (!filters.status || row.status === filters.status) && matchesMap
}))
watch(statusTab, () => { filters.status = '' })
function matchesTab(row) {
  if (statusTab.value === 'all') return true
  if (statusTab.value === 'pending') return row.status === 'PendingReview' || row.status === 'Draft'
  if (statusTab.value === 'approved') return row.status === 'Active'
  return row.status === 'Disabled' || row.status === 'Offline'
}
function countForTab(key) {
  return stores.value.filter((row) => {
    if (key === 'all') return true
    if (key === 'pending') return row.status === 'PendingReview' || row.status === 'Draft'
    if (key === 'approved') return row.status === 'Active'
    return row.status === 'Disabled' || row.status === 'Offline'
  }).length
}
function auditLabel(value) { return ({ Active: '通过', PendingReview: '审核中', Draft: '审核中', Disabled: '已拒绝', Offline: '已拒绝' })[value] || value }
function auditTone(value) { return ({ Active: 'success', PendingReview: 'warning', Draft: 'warning', Disabled: 'danger', Offline: 'danger' })[value] || 'muted' }
function statusLabel(value) {
  return ({ Active: '营业中', PendingReview: '待审核', Draft: '草稿', Offline: '已下架', Disabled: '已禁用' })[value] || value
}
function statusType(value) {
  return ({ Active: 'success', PendingReview: 'warning', Draft: 'info', Offline: 'info', Disabled: 'danger' })[value] || 'info'
}
async function load() {
  loading.value = true
  try { stores.value = await get('/admin/stores') } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
}
function reset() { Object.assign(filters, { keyword: '', status: '', map: '' }); load() }
async function approveSelected() {
  if (!selectedRows.value.length) return
  try {
    await ElMessageBox.confirm(`确定通过选中的 ${selectedRows.value.length} 家门店吗？`, '批量审核', { type: 'info' })
    for (const row of selectedRows.value.filter((item) => item.status === 'PendingReview' || item.status === 'Draft')) {
      await put(`/admin/stores/${row.id}`, { ...row, code: row.code || row.id.slice(0, 8), status: 'Active' })
    }
    ElMessage.success('审核已通过')
    selectedRows.value = []
    await load()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error(error.message || '批量审核失败')
  }
}
function resetUploadPreview() {
  Object.keys(uploadPreview).forEach((key) => { uploadPreview[key] = '' })
}
function handleLocalPreview(field, uploadFile) {
  const file = uploadFile?.raw
  if (!file || !file.type?.startsWith('image/')) return ElMessage.warning('请选择图片文件')
  if (file.size > 5 * 1024 * 1024) return ElMessage.warning('图片不能超过 5MB')
  const reader = new FileReader()
  reader.onload = () => {
    const dataUrl = String(reader.result || '')
    uploadPreview[field] = dataUrl
    form[field] = dataUrl
  }
  reader.readAsDataURL(file)
}
function openCreate() { Object.assign(form, emptyForm()); resetUploadPreview(); dialog.mode = 'create'; dialog.visible = true }
async function openEdit(row) {
  try {
    const detail = await get(`/admin/stores/${row.id}`)
    Object.assign(form, { ...emptyForm(), ...detail, roomImageUrlsText: parseArray(detail.roomImageUrlsJson).join('\n') })
  } catch {
    Object.assign(form, { ...emptyForm(), ...row, code: row.code || row.id.slice(0, 8).toUpperCase(), roomImageUrlsText: parseArray(row.roomImageUrlsJson).join('\n') })
  }
  resetUploadPreview()
  dialog.mode = 'edit'
  dialog.visible = true
}
async function save() {
  if (!form.name || !form.code) return ElMessage.warning('请填写门店名称和编码')
  dialog.saving = true
  try {
    const payload = {
      ...form,
      roomImageUrlsJson: JSON.stringify(form.roomImageUrlsText.split(/\r?\n|,/).map((item) => item.trim()).filter(Boolean))
    }
    delete payload.roomImageUrlsText
    if (dialog.mode === 'create') await post('/admin/stores', payload)
    else await put(`/admin/stores/${form.id}`, payload)
    ElMessage.success('门店已保存')
    dialog.visible = false
    await load()
  } catch (error) { ElMessage.error(error.message) } finally { dialog.saving = false }
}
function parseArray(value) {
  if (Array.isArray(value)) return value
  try { const parsed = JSON.parse(value || '[]'); return Array.isArray(parsed) ? parsed : [] } catch { return value ? value.split(',').map((item) => item.trim()).filter(Boolean) : [] }
}
function openGeo(row) {
  geoDialog.store = row
  geoDialog.error = ''
  Object.assign(geoForm, { address: row.address || '', city: '', longitude: row.longitude, latitude: row.latitude })
  geoDialog.visible = true
}
async function geocode() {
  if (!geoForm.address) return ElMessage.warning('请输入详细地址')
  geoDialog.loading = true
  geoDialog.error = ''
  try {
    const result = await post(`/stores/${geoDialog.store.id}/geo/geocode`, { address: geoForm.address, city: geoForm.city || null })
    Object.assign(geoForm, { longitude: result.longitude, latitude: result.latitude })
    ElMessage.success('高德地址解析成功，坐标已保存')
    geoDialog.store = { ...geoDialog.store, ...result }
    await load()
  } catch (error) { geoDialog.error = error.message; ElMessage.error(error.message) } finally { geoDialog.loading = false }
}
async function reverseGeo() {
  if (geoForm.longitude == null || geoForm.latitude == null) return ElMessage.warning('请先填写经纬度')
  geoDialog.loading = true
  try {
    const result = await post(`/stores/${geoDialog.store.id}/geo/reverse`, { longitude: geoForm.longitude, latitude: geoForm.latitude })
    Object.assign(geoForm, { address: result.address, longitude: result.longitude, latitude: result.latitude })
    ElMessage.success('逆地理解析成功')
    await load()
  } catch (error) { geoDialog.error = error.message; ElMessage.error(error.message) } finally { geoDialog.loading = false }
}
async function changeStatus(row, status) {
  if (status === row.status) return
  await ElMessageBox.confirm(`确定将“${row.name}”设置为${statusLabel(status)}吗？`, '确认操作', { type: status === 'Disabled' ? 'warning' : 'info' })
    .then(async () => {
      await put(`/admin/stores/${row.id}`, { ...row, code: row.code || row.id.slice(0, 8), status })
      ElMessage.success('状态已更新')
      load()
    })
    .catch(() => {})
}
onMounted(load)
</script>

<style scoped>
.form-section-title{font-size:14px;font-weight:700;color:var(--text-primary);padding:12px 0 8px;border-top:1px solid var(--line);margin-top:4px}
.form-section-title:first-child{border-top:0;padding-top:0}
.store-form :deep(.el-form-item){margin-bottom:14px}
.store-form :deep(.el-input-number),.store-form :deep(.el-select){width:100%}
.asset-field{display:flex;gap:8px;align-items:center;width:100%}
.asset-field :deep(.el-input){flex:1}
.asset-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:18px}
.asset-grid :deep(.el-form-item:last-child){grid-column:1 / -1}
@media (max-width: 760px){.asset-grid{grid-template-columns:1fr}}
</style>
