<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
        <p>审核用户对房间的评价，门店、用户名称和房间信息不可修改，仅可调整星级、评价内容和图片。</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load(true)">刷新</el-button>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs" role="tablist">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          type="button"
          role="tab"
          class="status-tab"
          :class="{ active: activeTab === tab.key }"
          :aria-selected="activeTab === tab.key"
          @click="activeTab = tab.key"
        >
          {{ tab.label }}
          <span>{{ tab.count }}</span>
        </button>
      </div>

      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新" @click="load(true)" />
        </div>
        <div class="toolbar-right">
          <el-input v-model="keyword" clearable class="table-search" placeholder="搜索用户、门店、房间或评价内容" :prefix-icon="Search" />
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedRows" class="clean-table admin-data-table" row-key="id">
        <el-table-column label="用户" width="140">
          <template #default="{ row }">
            <div class="review-user-cell">
              <span class="review-user-avatar">{{ String(row.user || '微').slice(0, 1) }}</span>
              <div class="review-user-copy">
                <strong>{{ row.user }}</strong>
                <small class="table-sub">{{ formatDateTime(row.createdAt) }}</small>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="store" label="门店" min-width="100" show-overflow-tooltip />
        <el-table-column prop="room" label="房间" min-width="100" show-overflow-tooltip />
        <el-table-column label="星级" width="132">
          <template #default="{ row }">
            <div class="review-score-cell">
              <el-rate :model-value="Number(row.score) || 0" disabled />
              <span>{{ Number(row.score) || 0 }}.0</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="评价内容" min-width="160">
          <template #default="{ row }">
            <span class="review-content-text">{{ row.content || '用户未填写文字评价' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="图片" width="96">
          <template #default="{ row }">
            <div v-if="rowImages(row).length" class="review-thumbs">
              <el-image
                v-for="(image, index) in rowImages(row).slice(0, 2)"
                :key="`${row.id}-${index}`"
                class="review-thumb"
                :src="image"
                :preview-src-list="rowImages(row)"
                :initial-index="index"
                preview-teleported
                fit="cover"
              />
              <span v-if="rowImages(row).length > 2" class="review-thumb-more">+{{ rowImages(row).length - 2 }}</span>
            </div>
            <span v-else class="review-no-image">无图片</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="84">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)" effect="light">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="176" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <template v-if="canApprove">
                <el-button v-if="row.status !== 'published'" type="success" link @click="moderate(row, 'published')">通过</el-button>
                <el-button v-if="row.status !== 'hidden'" type="warning" link @click="moderate(row, 'hidden')">不通过</el-button>
              </template>
              <el-button v-if="canEdit" type="primary" link @click="openEdit(row)">编辑</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <div class="list-footer">
        <span>显示第 {{ filteredRows.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredRows.length) }} 条记录，共 {{ filteredRows.length }} 条记录</span>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50]"
          layout="sizes, prev, pager, next, jumper"
          :total="filteredRows.length"
          background
        />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" title="编辑评价" width="640px" destroy-on-close>
      <div class="review-locked">
        <div class="review-locked-head"><el-icon><Lock /></el-icon><span>门店、用户名称、房间由订单自动关联，不可修改</span></div>
        <div class="review-locked-grid">
          <div><small>用户名称</small><strong>{{ form.user || '-' }}</strong></div>
          <div><small>门店</small><strong>{{ form.store || '-' }}</strong></div>
          <div><small>房间</small><strong>{{ form.room || '-' }}</strong></div>
        </div>
      </div>

      <el-form label-width="92px" class="dialog-form review-edit-form">
        <el-form-item label="星级">
          <div class="review-rate-row">
            <el-rate v-model="form.score" :max="5" show-text :texts="['1 分', '2 分', '3 分', '4 分', '5 分']" />
          </div>
        </el-form-item>
        <el-form-item label="评价内容">
          <el-input v-model="form.content" type="textarea" :rows="4" maxlength="500" show-word-limit placeholder="请输入评价内容" />
        </el-form-item>
        <el-form-item label="评价图片">
          <div class="review-uploader">
            <div v-for="(image, index) in form.images" :key="image + index" class="review-upload-item">
              <el-image :src="image" fit="cover" :preview-src-list="form.images" :initial-index="index" preview-teleported />
              <button type="button" class="review-upload-remove" @click="removeImage(index)">×</button>
            </div>
            <el-upload
              v-if="form.images.length < 9"
              :show-file-list="false"
              accept="image/jpeg,image/png,image/webp"
              :disabled="uploading"
              :http-request="uploadImage"
            >
              <button type="button" class="review-upload-add" :disabled="uploading">
                <el-icon><Plus /></el-icon>
                <span>{{ uploading ? '上传中' : '上传图片' }}</span>
              </button>
            </el-upload>
          </div>
          <p class="review-upload-tip">最多 9 张，支持 JPG、PNG、WEBP，单张不超过 10MB</p>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Lock, Plus, Refresh, Search } from '@element-plus/icons-vue'
import { get, put, uploadFile } from '../api'
import { requestNotificationRefresh } from '../notifications'

const reviews = ref([])
const loading = ref(false)
const activeTab = ref('all')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(10)
const canApprove = ref(true)
const canEdit = ref(true)

const dialogVisible = ref(false)
const saving = ref(false)
const uploading = ref(false)
const form = reactive({ id: '', user: '', store: '', room: '', score: 5, content: '', images: [] })

function statusLabel(value) {
  return ({ pending: '待审核', published: '已通过', hidden: '未通过' })[value] || value
}
function statusType(value) {
  return ({ pending: 'warning', published: 'success', hidden: 'info' })[value] || 'info'
}
function resolveAssetUrl(value) {
  const url = String(value || '').trim()
  if (!url) return ''
  if (/^(data:|blob:)/i.test(url)) return url
  try {
    const parsed = new URL(url, window.location.origin)
    if (parsed.pathname.startsWith('/uploads/') || parsed.pathname.startsWith('/static/')) {
      return `${parsed.pathname}${parsed.search}`
    }
    return url
  } catch {
    return url
  }
}
function rowImages(row) {
  const images = Array.isArray(row.images) ? row.images : (row.image ? [row.image] : [])
  return images.map(resolveAssetUrl).filter(Boolean)
}
function formatDateTime(value) {
  if (!value) return '-'
  let text = String(value)
  if (/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}/.test(text) && !/(z|[+-]\d{2}:?\d{2})$/i.test(text)) {
    text = `${text.replace(' ', 'T')}Z`
  }
  const date = new Date(text)
  if (Number.isNaN(date.getTime())) return text.replace('T', ' ').slice(0, 16)
  const pad = (num) => String(num).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const tabs = computed(() => {
  const count = (status) => reviews.value.filter((review) => review.status === status).length
  return [
    { key: 'all', label: '全部', count: reviews.value.length },
    { key: 'pending', label: '待审核', count: count('pending') },
    { key: 'published', label: '已通过', count: count('published') },
    { key: 'hidden', label: '未通过', count: count('hidden') }
  ]
})
const filteredRows = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  return reviews.value.filter((review) => {
    const matchesTab = activeTab.value === 'all' || review.status === activeTab.value
    const matchesQuery = !query ||
      [review.user, review.store, review.room, review.content]
        .some((value) => String(value || '').toLowerCase().includes(query))
    return matchesTab && matchesQuery
  })
})
const pagedRows = computed(() => filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([activeTab, keyword, pageSize], () => { page.value = 1 })

async function load(notify = false) {
  loading.value = true
  try {
    const data = await get('/admin/reviews')
    reviews.value = Array.isArray(data) ? data : []
    page.value = 1
    if (notify) ElMessage.success('列表已刷新')
  } catch (error) {
    ElMessage.error(error.message || '评价加载失败')
  } finally {
    loading.value = false
  }
}
function replaceRow(saved) {
  const index = reviews.value.findIndex((review) => review.id === saved.id)
  if (index >= 0) reviews.value[index] = saved
}
async function moderate(row, status) {
  const approving = status === 'published'
  const label = approving ? '通过' : '不通过'
  try {
    await ElMessageBox.confirm(
      approving ? '通过后该评价会展示在房型评价列表，确定通过吗？' : '不通过后该评价不会展示给用户，确定不通过吗？',
      `${label}评价`,
      { type: approving ? 'success' : 'warning', confirmButtonText: label, cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  try {
    const saved = await put(`/admin/reviews/${row.id}`, { status })
    replaceRow(saved)
    requestNotificationRefresh()
    ElMessage.success(approving ? '评价已通过，用户端可见' : '评价已设为不通过')
  } catch (error) {
    ElMessage.error(error.message || '审核失败')
  }
}
function openEdit(row) {
  form.id = row.id
  form.user = row.user
  form.store = row.store
  form.room = row.room
  form.score = Number(row.score) || 5
  form.content = row.content || ''
  form.images = rowImages(row)
  dialogVisible.value = true
}
async function uploadImage(options) {
  uploading.value = true
  try {
    const result = await uploadFile('/admin/uploads/review-image', options.file)
    const url = resolveAssetUrl(result?.url)
    if (!url) throw new Error('图片上传失败')
    if (!form.images.includes(url) && form.images.length < 9) form.images.push(url)
    options.onSuccess?.(result)
  } catch (error) {
    options.onError?.(error)
    ElMessage.error(error.message || '图片上传失败')
  } finally {
    uploading.value = false
  }
}
function removeImage(index) {
  form.images.splice(index, 1)
}
async function save() {
  if (!String(form.content).trim()) return ElMessage.warning('评价内容不能为空')
  saving.value = true
  try {
    const saved = await put(`/admin/reviews/${form.id}`, {
      content: String(form.content).trim(),
      score: Number(form.score) || 5,
      images: form.images
    })
    replaceRow(saved)
    dialogVisible.value = false
    ElMessage.success('评价已保存')
  } catch (error) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    saving.value = false
  }
}
onMounted(() => load())
</script>

<style scoped>
.review-user-cell {
  display: flex;
  align-items: center;
  gap: 9px;
}

.review-user-avatar {
  display: grid;
  width: 32px;
  height: 32px;
  flex: 0 0 32px;
  place-items: center;
  color: #fff;
  border-radius: 50%;
  background: #2aa9a9;
  font-size: 13px;
  font-weight: 700;
}

.review-user-copy {
  min-width: 0;
}

.review-user-cell strong {
  display: block;
  overflow: hidden;
  color: var(--admin-text);
  font-size: 13px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.review-score-cell {
  display: flex;
  align-items: center;
  gap: 6px;
}

.review-score-cell :deep(.el-rate) {
  height: 18px;
}

.review-score-cell :deep(.el-rate__item) {
  margin-right: 1px;
}

.review-score-cell span {
  color: var(--admin-muted);
  font-size: 11px;
}

.row-actions :deep(.el-button + .el-button) {
  margin-left: 8px;
}

.review-content-text {
  display: -webkit-box;
  overflow: hidden;
  color: var(--admin-text);
  line-height: 1.6;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.review-thumbs {
  display: flex;
  align-items: center;
  gap: 5px;
}

.review-thumb {
  width: 42px;
  height: 42px;
  overflow: hidden;
  border: 1px solid var(--admin-border);
  border-radius: 5px;
}

.review-thumb-more {
  color: var(--admin-muted);
  font-size: 11px;
}

.review-no-image {
  color: var(--admin-muted);
  font-size: 11px;
}

.review-locked {
  margin-bottom: 18px;
  padding: 12px 14px;
  border: 1px dashed #d6e4e8;
  border-radius: 8px;
  background: #f6fbfb;
}

.review-locked-head {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #5d7b80;
  font-size: 12px;
}

.review-locked-grid {
  display: grid;
  margin-top: 10px;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}

.review-locked-grid small {
  display: block;
  color: var(--admin-muted);
  font-size: 11px;
}

.review-locked-grid strong {
  display: block;
  margin-top: 4px;
  overflow: hidden;
  color: var(--admin-text);
  font-size: 13px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.review-rate-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.review-uploader {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.review-upload-item {
  position: relative;
  width: 84px;
  height: 84px;
  overflow: hidden;
  border: 1px solid var(--admin-border);
  border-radius: 8px;
}

.review-upload-item :deep(.el-image) {
  width: 100%;
  height: 100%;
}

.review-upload-remove {
  position: absolute;
  top: 4px;
  right: 4px;
  display: grid;
  width: 20px;
  height: 20px;
  padding: 0;
  place-items: center;
  color: #fff;
  border: 0;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.55);
  cursor: pointer;
  font-size: 14px;
  line-height: 1;
}

.review-upload-add {
  display: flex;
  width: 84px;
  height: 84px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 5px;
  color: #5f7b80;
  border: 1px dashed #c9dade;
  border-radius: 8px;
  background: #f7fbfb;
  cursor: pointer;
  font-size: 11px;
}

.review-upload-add:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.review-upload-tip {
  width: 100%;
  margin: 8px 0 0;
  color: var(--admin-muted);
  font-size: 11px;
}

.review-edit-form :deep(.el-form-item__content) {
  min-width: 0;
}
</style>
