<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
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
      <el-table v-loading="loading" :data="pagedStores" class="clean-table admin-data-table" row-key="id" @selection-change="selectedRows = $event">
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column label="账号" min-width="160"><template #default="{ row }">{{ row.code || '未设置编码' }}</template></el-table-column>
        <el-table-column label="民宿名称" min-width="210">
          <template #default="{ row }">
            <div class="store-cell"><span class="store-avatar">{{ row.name?.slice(0, 1) }}</span><div><strong>{{ row.name }}</strong></div></div>
          </template>
        </el-table-column>
        <el-table-column label="房东姓名" width="130"><template #default="{ row }">{{ row.ownerName || '门店管理员' }}</template></el-table-column>
        <el-table-column prop="phone" label="房东电话" width="145" />
        <el-table-column label="房源等级" width="120"><template #default="{ row }"><el-tag size="small" effect="light">{{ row.level || (row.status === 'Active' ? '优质民宿' : '无等级') }}</el-tag></template></el-table-column>
        <el-table-column label="审核状态" width="115"><template #default="{ row }"><span class="dot-status" :class="auditTone(row.status)"><i></i>{{ auditLabel(row.status) }}</span></template></el-table-column>
        <el-table-column label="详细地址" min-width="260"><template #default="{ row }"><span class="muted-text">{{ row.address || '尚未填写地址' }}</span></template></el-table-column>
        <el-table-column label="身份证正面" width="104">
          <template #default="{ row }">
            <el-image v-if="assetUrl(row, 'idCardFrontUrl')" class="table-thumb credential-thumb" :src="assetUrl(row, 'idCardFrontUrl')" :preview-src-list="[assetUrl(row, 'idCardFrontUrl')]" preview-teleported fit="cover" />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="身份证背面" width="104">
          <template #default="{ row }">
            <el-image v-if="assetUrl(row, 'idCardBackUrl')" class="table-thumb credential-thumb" :src="assetUrl(row, 'idCardBackUrl')" :preview-src-list="[assetUrl(row, 'idCardBackUrl')]" preview-teleported fit="cover" />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="房产证" width="104">
          <template #default="{ row }">
            <el-image v-if="assetUrl(row, 'propertyCertificateUrl')" class="table-thumb credential-thumb" :src="assetUrl(row, 'propertyCertificateUrl')" :preview-src-list="[assetUrl(row, 'propertyCertificateUrl')]" preview-teleported fit="cover" />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="营业执照" width="104">
          <template #default="{ row }">
            <el-image v-if="assetUrl(row, 'businessLicenseUrl')" class="table-thumb credential-thumb" :src="assetUrl(row, 'businessLicenseUrl')" :preview-src-list="[assetUrl(row, 'businessLicenseUrl')]" preview-teleported fit="cover" />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="门头照片" width="104">
          <template #default="{ row }">
            <el-image v-if="assetUrl(row, 'doorImageUrl')" class="table-thumb credential-thumb" :src="assetUrl(row, 'doorImageUrl')" :preview-src-list="[assetUrl(row, 'doorImageUrl')]" preview-teleported fit="cover" />
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="房间照片" width="132">
          <template #default="{ row }">
            <div v-if="roomImageListForRow(row).length" class="table-image-list">
              <el-image v-for="image in roomImageListForRow(row).slice(0, 2)" :key="image" class="table-thumb room-thumb" :src="image" :preview-src-list="roomImageListForRow(row)" preview-teleported fit="cover" />
              <span v-if="roomImageListForRow(row).length > 2" class="image-count">+{{ roomImageListForRow(row).length - 2 }}</span>
            </div>
            <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
          </template>
        </el-table-column>
        <el-table-column label="评分" width="82"><template #default="{ row }">{{ row.rating ?? '—' }}</template></el-table-column>
        <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag :type="statusType(row.status)" effect="light">{{ statusLabel(row.status) }}</el-tag></template></el-table-column>
        <el-table-column label="拒绝理由" min-width="160"><template #default="{ row }">{{ row.rejectionReason || '—' }}</template></el-table-column>
        <el-table-column label="审核时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.reviewedAt || row.approvalAt) }}</template></el-table-column>
        <el-table-column label="排序" width="76"><template #default="{ row }">{{ row.sortOrder ?? 0 }}</template></el-table-column>
        <el-table-column label="创建时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column>
        <el-table-column label="更新时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.updatedAt) }}</template></el-table-column>
        <el-table-column label="操作" width="300" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'PendingReview'" link type="success" @click="changeStatus(row, 'Active')">通过</el-button>
            <el-button v-if="row.status === 'PendingReview'" link type="primary" @click="openEdit(row)">查看资料</el-button>
            <el-button link type="primary" @click="openRooms(row)">房间</el-button>
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
      <div class="list-footer">
        <span>显示第 {{ filteredStores.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredStores.length) }} 家门店，共 {{ filteredStores.length }} 家</span>
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredStores.length" background />
        <el-button text :icon="Refresh" @click="load">刷新列表</el-button>
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '新增门店' : '编辑门店'" width="900px" destroy-on-close>
      <el-form :model="form" label-width="96px" class="dialog-form store-form">
        <div class="form-section-title">账号信息</div>
        <div class="form-grid">
          <el-form-item label="用户名">
            <el-input v-model="form.ownerUsername" :disabled="dialog.mode === 'edit'" placeholder="房东账号或小程序用户标识" />
            <div v-if="dialog.mode === 'edit'" class="field-hint">用户名由小程序用户登录后自动绑定，编辑门店时不可修改。</div>
          </el-form-item>
          <el-form-item label="昵称"><el-input v-model="form.ownerNickname" placeholder="房东昵称" /></el-form-item>
          <el-form-item label="房东姓名"><el-input v-model="form.ownerName" placeholder="真实姓名" /></el-form-item>
          <el-form-item label="房东手机号"><el-input v-model="form.ownerPhone" placeholder="用于审核联系" /></el-form-item>
        </div>

        <div class="form-section-title">门店信息</div>
        <div class="form-grid">
          <el-form-item label="民宿名称" required><el-input v-model="form.name" placeholder="例如：上海人民广场店" /></el-form-item>
          <el-form-item label="门店编码" required><el-input v-model="form.code" :disabled="dialog.mode === 'edit'" placeholder="大写字母、数字和短横线" /></el-form-item>
          <el-form-item label="房东电话"><el-input v-model="form.phone" placeholder="门店公开联系电话" /></el-form-item>
          <el-form-item label="评分"><el-input-number v-model="form.rating" :min="0" :max="5" :precision="1" :step="0.1" controls-position="right" /></el-form-item>
          <el-form-item label="地区" class="field-wide" required>
            <el-cascader
              v-model="form.regionPath"
              class="region-cascader"
              :options="regionOptions"
              :props="{ checkStrictly: false, value: 'value', label: 'label', children: 'children' }"
              clearable
              filterable
              :loading="regionLoading"
              placeholder="请选择省 / 市 / 区"
              @change="handleRegionChange"
            />
            <div class="field-hint">地区数据来自地区管理；接口未接通时使用本地地区配置。</div>
          </el-form-item>
          <el-form-item label="排序"><el-input-number v-model="form.sortOrder" :min="0" controls-position="right" /></el-form-item>
        </div>
        <el-form-item label="详细地址" class="field-wide">
          <div class="address-editor">
            <el-input v-model="form.address" placeholder="填写门店详细地址，也可以在地图上选点后自动带出" />
            <el-button type="primary" plain :icon="Location" @click="openMapPicker">地图选择</el-button>
          </div>
        </el-form-item>
        <el-form-item label="门店简介"><el-input v-model="form.description" type="textarea" :rows="3" /></el-form-item>
        <div class="form-grid">
          <el-form-item label="经度"><el-input-number v-model="form.longitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
          <el-form-item label="纬度"><el-input-number v-model="form.latitude" :precision="6" :step="0.0001" controls-position="right" /></el-form-item>
          <el-form-item label="状态"><el-select v-model="form.status"><el-option v-for="item in statusOptions" :key="item.value" v-bind="item" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">资质与图片</div>
        <div class="asset-grid">
          <el-form-item v-for="item in assetFields" :key="item.field" :label="item.label">
            <div class="asset-editor">
              <div class="asset-preview-grid">
                <div v-if="assetPreview(item.field)" class="asset-preview image-preview-item">
                  <el-image :src="assetPreview(item.field)" :preview-src-list="[assetPreview(item.field)]" preview-teleported fit="cover">
                    <template #error>
                      <div class="image-error"><el-icon><Picture /></el-icon><span>图片不可用</span></div>
                    </template>
                  </el-image>
                  <el-button class="image-remove" type="danger" circle plain :icon="Delete" title="删除图片" @click="clearAsset(item.field)" />
                </div>
                <el-upload class="image-uploader" :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="(file) => handleLocalPreview(item.field, file)">
                  <button type="button" class="image-add-tile">
                    <el-icon><component :is="assetPreview(item.field) ? Upload : Plus" /></el-icon>
                    <span>{{ assetPreview(item.field) ? '替换图片' : '上传图片' }}</span>
                  </button>
                </el-upload>
              </div>
            </div>
          </el-form-item>
          <el-form-item label="房间照片" class="field-wide">
            <div class="asset-editor">
              <div class="asset-preview-grid">
                <div v-for="(image, index) in roomImages" :key="`${index}-${String(image).slice(0, 40)}`" class="asset-preview image-preview-item">
                  <el-image :src="resolveAssetUrl(image)" :preview-src-list="roomPreviewList" :initial-index="index" preview-teleported fit="cover">
                    <template #error>
                      <div class="image-error"><el-icon><Picture /></el-icon><span>图片不可用</span></div>
                    </template>
                  </el-image>
                  <el-button class="image-remove" type="danger" circle plain :icon="Delete" title="删除图片" @click="removeRoomImage(index)" />
                </div>
                <el-upload class="image-uploader" :show-file-list="false" :auto-upload="false" accept="image/*" multiple :on-change="handleRoomImageChange">
                  <button type="button" class="image-add-tile">
                    <el-icon><Plus /></el-icon>
                    <span>上传图片</span>
                  </button>
                </el-upload>
              </div>
              <div class="field-hint">已上传 {{ roomImages.length }} 张房间照片，支持一次选择多张，第一张将作为房间封面。</div>
            </div>
          </el-form-item>
        </div>
      </el-form>
      <template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存门店</el-button></template>
    </el-dialog>

    <el-dialog
      v-model="mapDialog.visible"
      title="地图选点"
      width="760px"
      class="map-picker-dialog"
      destroy-on-close
      @opened="initMap"
      @closed="destroyMap"
    >
      <div class="map-picker">
        <div class="map-search">
          <el-input v-model="mapForm.keyword" clearable placeholder="搜索地址，例如：广州市越秀区北京路" :prefix-icon="Search" @keyup.enter="searchAddress()" />
          <el-input v-model="mapForm.searchCity" clearable class="map-city-input" placeholder="城市（可选）" />
          <el-button type="primary" :loading="mapDialog.searching" @click="searchAddress()">搜索</el-button>
        </div>
        <div class="map-canvas-wrap">
          <div ref="mapContainer" class="map-canvas"></div>
          <div v-if="mapDialog.loading" class="map-loading"><span class="map-loading-dot"></span>正在解析地址…</div>
          <div class="map-canvas-tip">点击地图或拖动图钉选择位置</div>
          <span class="map-canvas-brand">© 高德地图</span>
        </div>
        <el-alert v-if="mapDialog.error" :title="mapDialog.error" type="warning" show-icon :closable="false" />
        <div class="map-summary">
          <div class="map-summary-text">
            <span>当前选点</span>
            <strong>{{ mapForm.address || '尚未选择位置' }}</strong>
          </div>
          <div class="map-coord-inputs">
            <label>经度<el-input-number v-model="mapForm.longitude" :precision="6" :step="0.0001" :controls="false" placeholder="经度" @change="syncFromInputs" /></label>
            <label>纬度<el-input-number v-model="mapForm.latitude" :precision="6" :step="0.0001" :controls="false" placeholder="纬度" @change="syncFromInputs" /></label>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="mapDialog.visible = false">取消</el-button>
        <el-button type="primary" :disabled="mapForm.latitude == null || mapForm.longitude == null" @click="confirmMapPicker">使用该位置</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Delete, Location, Picture, Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { get, post, put } from '../api'
import { toRegionCascaderOptions } from '../data/regions'
import { requestNotificationRefresh } from '../notifications'

const emit = defineEmits(['navigate'])
const loading = ref(false)
const stores = ref([])
const filters = reactive({ keyword: '', status: '', map: '' })
const statusTab = ref('all')
const page = ref(1)
const pageSize = ref(10)
const selectedRows = ref([])
const regionOptions = ref([])
const regionLoading = ref(false)
const storeTabs = [
  { key: 'all', label: '全部' },
  { key: 'pending', label: '审核中' },
  { key: 'approved', label: '通过' },
  { key: 'rejected', label: '已拒绝' }
]
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const mapDialog = reactive({ visible: false, loading: false, searching: false, error: '' })
const form = reactive(emptyForm())
const roomImages = ref([])
const assetFields = [
  { field: 'idCardFrontUrl', label: '身份证正面' },
  { field: 'idCardBackUrl', label: '身份证反面' },
  { field: 'propertyCertificateUrl', label: '房产证' },
  { field: 'businessLicenseUrl', label: '营业执照' },
  { field: 'doorImageUrl', label: '门头照片' }
]
const uploadPreview = reactive({ idCardFrontUrl: '', idCardBackUrl: '', propertyCertificateUrl: '', businessLicenseUrl: '', doorImageUrl: '' })
const mapForm = reactive({ keyword: '', searchCity: '', address: '', province: '', city: '', district: '', longitude: null, latitude: null })
const mapContainer = ref(null)
const MAP_FALLBACK_CENTER = { latitude: 34.7466, longitude: 113.6254 }
const mapPinIcon = L.divIcon({
  className: 'map-pin-marker',
  html: '<svg width="28" height="36" viewBox="0 0 28 36" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M14 1.5c-6.6 0-12 5.3-12 11.9C2 21.9 14 34.5 14 34.5S26 21.9 26 13.4C26 6.8 20.6 1.5 14 1.5z" fill="#0f7777" stroke="#fff" stroke-width="2.5"/><circle cx="14" cy="13.2" r="4.4" fill="#fff"/></svg>',
  iconSize: [28, 36],
  iconAnchor: [14, 35]
})
let mapInstance = null
let mapMarker = null
let mapRequestToken = 0
let mapSyncTimer = null
const statusOptions = [
  { label: '草稿', value: 'Draft' },
  { label: '待审核', value: 'PendingReview' },
  { label: '营业中', value: 'Active' },
  { label: '已下架', value: 'Offline' },
  { label: '已禁用', value: 'Disabled' }
]
const fallbackRegionOptions = toRegionCascaderOptions()

function emptyForm() {
  return {
    id: '', name: '', code: '', description: '', province: '', city: '', district: '', regionPath: [], address: '', phone: '',
    longitude: null, latitude: null, status: 'Draft', sortOrder: 0, rating: 0,
    ownerUsername: '', ownerNickname: '', ownerAvatarUrl: '', ownerName: '', ownerPhone: '',
    idCardFrontUrl: '', idCardBackUrl: '', propertyCertificateUrl: '', businessLicenseUrl: '', doorImageUrl: ''
  }
}
const roomPreviewList = computed(() => roomImages.value.map((image) => resolveAssetUrl(image)).filter(Boolean))
const filteredStores = computed(() => stores.value.filter((row) => {
  const keyword = filters.keyword.trim().toLowerCase()
  const matchesKeyword = !keyword || [row.name, row.address, row.phone].join(' ').toLowerCase().includes(keyword)
  const matchesMap = !filters.map || (filters.map === 'configured' ? row.hasMapLocation : !row.hasMapLocation)
  return matchesKeyword && matchesTab(row) && (!filters.status || row.status === filters.status) && matchesMap
}))
const pagedStores = computed(() => filteredStores.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch(statusTab, () => { filters.status = ''; page.value = 1 })
watch([() => filters.keyword, () => filters.status, () => filters.map, pageSize], () => { page.value = 1 })
watch(filteredStores, () => {
  const maxPage = Math.max(1, Math.ceil(filteredStores.value.length / pageSize.value))
  if (page.value > maxPage) page.value = maxPage
})
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
function formatDateTime(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
// 历史图片地址里带的是上传时的后端域名（可能是 localhost 或某个内网 IP），
// 管理端换机器/换端口访问时会加载失败，这里统一转成站内相对路径交给代理处理。
function resolveAssetUrl(value) {
  const url = String(value || '').trim()
  if (!url) return ''
  if (/^(data:|blob:)/i.test(url)) return url
  try {
    const parsed = new URL(url, window.location.origin)
    if (parsed.pathname.startsWith('/uploads/') || parsed.pathname.startsWith('/static/')) {
      return `${parsed.pathname}${parsed.search}`
    }
  } catch {
    return url
  }
  return url
}
function assetUrl(row, field) {
  return resolveAssetUrl(row?.[field])
}
function roomImageListForRow(row) {
  return parseArray(row?.roomImageUrlsJson || row?.roomImageUrls || row?.roomImages)
    .map((image) => resolveAssetUrl(image))
    .filter(Boolean)
}
function assetPreview(field) {
  return resolveAssetUrl(uploadPreview[field] || form[field])
}
function normalizeRegionOptions(payload) {
  const source = Array.isArray(payload) ? payload : (payload?.items || payload?.data || payload?.rows || [])
  if (!Array.isArray(source)) return []
  const mapNode = (item, index = 0) => ({
    value: String(item.value ?? item.id ?? item.code ?? item.key ?? item.name ?? index),
    label: item.label ?? item.name ?? item.title ?? String(item.value ?? item.id ?? item.code ?? ''),
    children: Array.isArray(item.children) ? item.children.map((child, childIndex) => mapNode(child, childIndex)) : undefined
  })
  if (source.some((item) => Array.isArray(item?.children))) return source.map((item, index) => mapNode(item, index))

  const nodes = source.map((item, index) => ({
    ...item,
    value: String(item.value ?? item.id ?? item.code ?? item.key ?? item.name ?? index),
    label: item.label ?? item.name ?? item.title ?? String(item.value ?? item.id ?? item.code ?? ''),
    parentValue: item.parentValue ?? item.parentId ?? item.parentCode ?? item.parentKey ?? item.parent
  }))
  const byValue = new Map(nodes.map((item) => [String(item.value), item]))
  const byLabel = new Map(nodes.map((item) => [String(item.label), item]))
  const roots = []
  const childrenByParent = new Map()
  nodes.forEach((node) => {
    const parent = node.parentValue == null ? null : (byValue.get(String(node.parentValue)) || byLabel.get(String(node.parentValue)))
    if (!parent || String(node.parentValue) === '中国' || String(node.parentValue) === '全国') {
      roots.push(node)
      return
    }
    const key = String(parent.value)
    if (!childrenByParent.has(key)) childrenByParent.set(key, [])
    childrenByParent.get(key).push(node)
  })
  const build = (node) => {
    const children = (childrenByParent.get(String(node.value)) || []).map(build)
    return { value: String(node.value), label: node.label, ...(children.length ? { children } : {}) }
  }
  return roots.map(build)
}
function findRegionPathByLabels(province, city, district) {
  const wanted = [province, city, district].filter(Boolean).map(String)
  if (!wanted.length || !regionOptions.value.length) return []
  const path = []
  let level = regionOptions.value
  for (const label of wanted) {
    const match = level.find((item) => String(item.label) === label || String(item.value) === label)
    if (!match) break
    path.push(match.value)
    level = match.children || []
  }
  return path
}
function findRegionLabels(path) {
  const labels = []
  let level = regionOptions.value
  for (const value of path || []) {
    const match = level.find((item) => String(item.value) === String(value))
    if (!match) break
    labels.push(match.label)
    level = match.children || []
  }
  return labels
}
function handleRegionChange(path) {
  const labels = findRegionLabels(path)
  Object.assign(form, { province: labels[0] || '', city: labels[1] || '', district: labels[2] || '' })
}
async function loadRegions() {
  regionLoading.value = true
  try {
    let payload
    try {
      payload = await get('/admin/regions/tree')
    } catch {
      payload = await get('/admin/regions')
    }
    regionOptions.value = normalizeRegionOptions(payload)
    if (!regionOptions.value.length) regionOptions.value = fallbackRegionOptions
  } catch {
    try {
      const cached = JSON.parse(localStorage.getItem('tcpms.admin.demo.regions') || '[]')
      regionOptions.value = normalizeRegionOptions(cached)
    } catch {
      regionOptions.value = []
    }
    if (!regionOptions.value.length) regionOptions.value = fallbackRegionOptions
  } finally {
    regionLoading.value = false
  }
}
async function load() {
  loading.value = true
  try {
    const data = await get('/admin/stores')
    stores.value = Array.isArray(data) ? data : []
    // The summary endpoint intentionally omits credentials and image fields.
    // Hydrate visible stores so the table can render actual thumbnails.
    const needsDetails = stores.value.filter((row) => row.id)
    if (needsDetails.length && needsDetails.length <= 50) {
      const details = await Promise.all(needsDetails.map(async (row) => {
        try { return await get(`/admin/stores/${row.id}`) } catch { return null }
      }))
      const detailMap = new Map(details.filter(Boolean).map((row) => [String(row.id), row]))
      stores.value = stores.value.map((row) => ({ ...row, ...(detailMap.get(String(row.id)) || {}) }))
    }
    page.value = 1
  } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
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
    requestNotificationRefresh()
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
function clearAsset(field) {
  uploadPreview[field] = ''
  form[field] = ''
}
function handleRoomImageChange(uploadFile) {
  const file = uploadFile?.raw
  if (!file || !file.type?.startsWith('image/')) return ElMessage.warning('请选择图片文件')
  if (file.size > 5 * 1024 * 1024) return ElMessage.warning('图片不能超过 5MB')
  const reader = new FileReader()
  reader.onload = () => {
    const dataUrl = String(reader.result || '')
    if (dataUrl) roomImages.value = [...roomImages.value, dataUrl]
  }
  reader.readAsDataURL(file)
}
function removeRoomImage(index) {
  roomImages.value = roomImages.value.filter((_, itemIndex) => itemIndex !== index)
}
function openCreate() {
  Object.assign(form, emptyForm())
  roomImages.value = []
  resetUploadPreview()
  dialog.mode = 'create'
  dialog.visible = true
}
async function openEdit(row) {
  try {
    const detail = await get(`/admin/stores/${row.id}`)
    Object.assign(form, { ...emptyForm(), ...detail })
  } catch {
    Object.assign(form, { ...emptyForm(), ...row, code: row.code || row.id.slice(0, 8).toUpperCase() })
  }
  roomImages.value = parseArray(form.roomImageUrlsJson)
  form.regionPath = findRegionPathByLabels(form.province, form.city, form.district)
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
      roomImageUrlsJson: JSON.stringify(roomImages.value.filter(Boolean))
    }
    delete payload.regionPath
    if (dialog.mode === 'create') await post('/admin/stores', payload)
    else await put(`/admin/stores/${form.id}`, payload)
    ElMessage.success('门店已保存')
    requestNotificationRefresh()
    dialog.visible = false
    await load()
  } catch (error) { ElMessage.error(error.message) } finally { dialog.saving = false }
}
function parseArray(value) {
  if (Array.isArray(value)) return value
  const text = String(value ?? '').trim()
  if (!text) return []
  try {
    const parsed = JSON.parse(text)
    return Array.isArray(parsed) ? parsed.filter(Boolean) : []
  } catch {
    // 兼容历史上用换行或逗号拼接的图片地址。
    return text.split(/[\n,]+/).map((item) => item.trim()).filter(Boolean)
  }
}
function round6(value) {
  const number = Number(value)
  return Number.isFinite(number) ? Math.round(number * 1e6) / 1e6 : null
}
function openMapPicker() {
  const longitude = Number(form.longitude)
  const latitude = Number(form.latitude)
  Object.assign(mapForm, {
    keyword: form.address || '',
    searchCity: form.city || '',
    address: form.address || '',
    province: form.province || '',
    city: form.city || '',
    district: form.district || '',
    longitude: Number.isFinite(longitude) && longitude ? round6(longitude) : null,
    latitude: Number.isFinite(latitude) && latitude ? round6(latitude) : null
  })
  mapDialog.error = ''
  mapDialog.loading = false
  mapDialog.visible = true
}
function initMap() {
  nextTick(() => {
    if (mapInstance || !mapContainer.value) return
    const hasPoint = mapForm.latitude != null && mapForm.longitude != null
    const center = hasPoint
      ? [mapForm.latitude, mapForm.longitude]
      : [MAP_FALLBACK_CENTER.latitude, MAP_FALLBACK_CENTER.longitude]
    mapInstance = L.map(mapContainer.value, {
      center,
      zoom: hasPoint ? 16 : 12,
      minZoom: 3,
      maxZoom: 18,
      zoomControl: true,
      attributionControl: false
    })
    L.tileLayer('https://webrd0{s}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=8&x={x}&y={y}&z={z}', {
      subdomains: ['1', '2', '3', '4'],
      maxZoom: 18
    }).addTo(mapInstance)
    mapInstance.on('click', (event) => {
      applyPoint(event.latlng.lat, event.latlng.lng, { reverse: true })
    })
    if (hasPoint) placeMarker(mapForm.latitude, mapForm.longitude)
    window.setTimeout(() => mapInstance?.invalidateSize(), 180)
    if (hasPoint && !mapForm.address) {
      reverseLookup()
    } else if (!hasPoint && mapForm.keyword) {
      // 门店还没坐标时，用已有详细地址先定位，避免每次都从默认城市开始找。
      searchAddress({ silent: true })
    }
  })
}
function destroyMap() {
  if (mapSyncTimer) {
    window.clearTimeout(mapSyncTimer)
    mapSyncTimer = null
  }
  mapRequestToken += 1
  if (mapInstance) {
    mapInstance.remove()
    mapInstance = null
    mapMarker = null
  }
}
function placeMarker(latitude, longitude) {
  if (!mapInstance) return
  if (!mapMarker) {
    mapMarker = L.marker([latitude, longitude], { draggable: true, icon: mapPinIcon }).addTo(mapInstance)
    mapMarker.on('dragend', () => {
      const position = mapMarker.getLatLng()
      applyPoint(position.lat, position.lng, { reverse: true })
    })
    return
  }
  mapMarker.setLatLng([latitude, longitude])
}
function applyPoint(latitude, longitude, options = {}) {
  const lat = round6(latitude)
  const lng = round6(longitude)
  if (lat == null || lng == null) return
  mapForm.latitude = lat
  mapForm.longitude = lng
  placeMarker(lat, lng)
  if (options.pan && mapInstance) mapInstance.panTo([lat, lng])
  if (options.reverse) reverseLookup()
}
async function searchAddress(options = {}) {
  const keyword = mapForm.keyword.trim()
  if (!keyword) {
    if (!options.silent) ElMessage.warning('请输入要搜索的地址')
    return
  }
  mapDialog.searching = true
  mapDialog.error = ''
  try {
    const result = await post('/admin/geo/geocode', { address: keyword, city: mapForm.searchCity.trim() || null })
    if (result?.latitude == null || result?.longitude == null) throw new Error('未找到匹配地址，换个关键词再试试')
    mapForm.address = result.address || keyword
    mapForm.province = result.province || mapForm.province
    mapForm.city = result.city || mapForm.city
    mapForm.district = result.district || mapForm.district
    applyPoint(result.latitude, result.longitude)
    if (mapInstance) {
      mapInstance.setView([Number(result.latitude), Number(result.longitude)], 16)
    }
    if (!options.silent) ElMessage.success('已定位到搜索结果')
  } catch (error) {
    mapDialog.error = error.message || '地址解析失败'
  } finally {
    mapDialog.searching = false
  }
}
async function reverseLookup() {
  if (mapForm.latitude == null || mapForm.longitude == null) return
  const token = (mapRequestToken += 1)
  mapDialog.loading = true
  mapDialog.error = ''
  try {
    const result = await post('/admin/geo/reverse', { longitude: mapForm.longitude, latitude: mapForm.latitude })
    if (token !== mapRequestToken) return
    mapForm.address = result.address || mapForm.address
    mapForm.province = result.province || mapForm.province
    mapForm.city = result.city || mapForm.city
    mapForm.district = result.district || mapForm.district
  } catch (error) {
    if (token === mapRequestToken) mapDialog.error = error.message || '该位置解析失败，可以拖动图钉重试'
  } finally {
    if (token === mapRequestToken) mapDialog.loading = false
  }
}
function syncFromInputs() {
  if (mapForm.latitude == null || mapForm.longitude == null) return
  applyPoint(mapForm.latitude, mapForm.longitude, { pan: true })
  if (mapInstance) {
    mapInstance.setView([mapForm.latitude, mapForm.longitude], Math.max(mapInstance.getZoom(), 16))
  }
  if (mapSyncTimer) window.clearTimeout(mapSyncTimer)
  mapSyncTimer = window.setTimeout(() => {
    mapSyncTimer = null
    reverseLookup()
  }, 400)
}
function confirmMapPicker() {
  if (mapForm.latitude == null || mapForm.longitude == null) return ElMessage.warning('请先在地图上选择位置')
  form.longitude = round6(mapForm.longitude)
  form.latitude = round6(mapForm.latitude)
  if (mapForm.address) form.address = mapForm.address
  // 选点位置就是门店真实所在地，省市区跟随解析结果，避免地址和行政区打架。
  const regionChanged = Boolean(mapForm.province && mapForm.city && mapForm.district) &&
    (mapForm.province !== form.province || mapForm.city !== form.city || mapForm.district !== form.district)
  if (mapForm.province) form.province = mapForm.province
  if (mapForm.city) form.city = mapForm.city
  if (mapForm.district) form.district = mapForm.district
  if (regionChanged) form.regionPath = findRegionPathByLabels(form.province, form.city, form.district)
  mapDialog.visible = false
  ElMessage.success('已填入选点位置，保存门店后生效')
}
function openRooms(row) {
  const selection = { storeId: row.id, storeName: row.name }
  sessionStorage.setItem('tcpms.admin.room-store-filter', String(row.id))
  localStorage.setItem('tcpms.admin.room-filter', JSON.stringify(selection))
  window.dispatchEvent(new CustomEvent('tcpms:room-filter-request', { detail: selection }))
  emit('navigate', 'roomManagement')
}
async function changeStatus(row, status) {
  if (status === row.status) return
  await ElMessageBox.confirm(`确定将“${row.name}”设置为${statusLabel(status)}吗？`, '确认操作', { type: status === 'Disabled' ? 'warning' : 'info' })
    .then(async () => {
      await put(`/admin/stores/${row.id}`, { ...row, code: row.code || row.id.slice(0, 8), status })
      ElMessage.success('状态已更新')
      requestNotificationRefresh()
      load()
    })
    .catch(() => {})
}
onMounted(async () => {
  await Promise.all([load(), loadRegions()])
})
onBeforeUnmount(() => {
  destroyMap()
})
</script>

<style scoped>
.form-section-title{font-size:14px;font-weight:700;color:var(--text-primary);padding:12px 0 8px;border-top:1px solid var(--line);margin-top:4px}
.form-section-title:first-child{border-top:0;padding-top:0}
.store-form :deep(.el-form-item){margin-bottom:14px}
.store-form :deep(.el-input-number),.store-form :deep(.el-select),.store-form :deep(.el-cascader){width:100%}
.asset-editor{display:flex;flex-direction:column;gap:9px;width:100%}
.asset-preview{display:flex;width:132px;height:88px;align-items:center;justify-content:center;overflow:hidden;border:1px solid var(--admin-border);border-radius:7px;background:var(--admin-surface-soft)}
.asset-preview :deep(.el-image){display:block;width:100%;height:100%}
.asset-preview-grid{display:flex;flex-wrap:wrap;gap:10px}
.image-preview-item{position:relative;width:132px;height:88px}
.image-preview-item .image-remove{position:absolute;right:5px;bottom:5px;min-height:26px;width:26px;padding:0}
.image-uploader{display:block}
.image-add-tile{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;width:132px;height:88px;border:1px dashed var(--admin-border);border-radius:7px;background:var(--admin-surface-soft);color:var(--admin-muted);font-size:12px;line-height:1.2;cursor:pointer;transition:border-color .2s,color .2s}
.image-add-tile:hover{border-color:var(--el-color-primary);color:var(--el-color-primary)}
.image-add-tile .el-icon{font-size:20px}
.image-error{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:4px;width:100%;height:100%;color:var(--admin-muted);font-size:12px;background:var(--admin-surface-soft)}
.image-error .el-icon{font-size:18px}
.asset-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:18px}
.asset-grid :deep(.el-form-item:last-child){grid-column:1 / -1}
.region-cascader{width:100%}
.field-hint{margin-top:5px;color:var(--admin-muted);font-size:11px;line-height:1.5}
.address-editor{display:flex;gap:8px;width:100%}
.address-editor :deep(.el-input){flex:1}
.table-image-list{display:flex;align-items:center;gap:4px}
.credential-thumb,.room-thumb{width:50px;height:38px;border-radius:4px;border:1px solid var(--admin-border);background:var(--admin-surface-soft)}
.image-count{color:var(--admin-muted);font-size:11px}
.map-picker{display:flex;flex-direction:column;gap:12px}
.map-search{display:flex;gap:8px;align-items:center}
.map-search :deep(.el-input){flex:1}
.map-city-input{max-width:170px}
.map-canvas-wrap{position:relative;overflow:hidden;border:1px solid var(--admin-border);border-radius:10px;background:var(--admin-surface-soft)}
.map-canvas{width:100%;height:360px}
.map-canvas :deep(.leaflet-container){font:inherit}
.map-canvas :deep(.map-pin-marker){background:transparent;border:0}
.map-canvas-tip{position:absolute;left:50%;bottom:12px;z-index:500;transform:translateX(-50%);padding:5px 12px;border-radius:999px;background:rgba(15,23,32,.62);color:#fff;font-size:12px;line-height:1.4;white-space:nowrap;pointer-events:none}
.map-canvas-brand{position:absolute;right:8px;bottom:6px;z-index:500;padding:1px 6px;border-radius:4px;background:rgba(255,255,255,.82);color:#6b7785;font-size:11px;pointer-events:none}
.map-loading{position:absolute;inset:0;z-index:600;display:flex;align-items:center;justify-content:center;gap:8px;background:rgba(255,255,255,.66);color:var(--admin-muted);font-size:13px;pointer-events:none}
.map-loading-dot{width:9px;height:9px;border-radius:50%;background:var(--el-color-primary);animation:map-pulse 1s ease-in-out infinite}
@keyframes map-pulse{0%,100%{opacity:.25;transform:scale(.8)}50%{opacity:1;transform:scale(1)}}
.map-summary{display:flex;align-items:center;justify-content:space-between;gap:16px}
.map-summary-text{display:flex;flex-direction:column;gap:3px;min-width:0}
.map-summary-text span{color:var(--admin-muted);font-size:12px}
.map-summary-text strong{color:var(--text-primary);font-size:13px;line-height:1.5}
.map-coord-inputs{display:flex;gap:10px;flex:0 0 auto}
.map-coord-inputs label{display:flex;align-items:center;gap:6px;color:var(--admin-muted);font-size:12px;white-space:nowrap}
.map-coord-inputs :deep(.el-input-number){width:126px}
@media (max-width: 760px){.asset-grid{grid-template-columns:1fr}}
</style>
