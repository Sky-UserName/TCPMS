<template>
  <div class="view-stack compact-view subscription-page">
    <section class="page-heading">
      <div>
        <p>维护微信小程序订阅消息模板，统一配置模板标识、参数映射和启用状态。</p>
        <div class="page-meta">
          <el-tag size="small" effect="plain" :type="sourceMode === 'api' ? 'success' : 'warning'">
            {{ sourceMode === 'api' ? '接口数据' : '演示数据' }}
          </el-tag>
          <span>共 {{ filteredTemplates.length }} 个模板</span>
        </div>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">添加模板</el-button>
      </div>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="list-toolbar subscription-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新模板" @click="load" />
          <el-button type="primary" :icon="Plus" @click="openCreate">添加</el-button>
          <el-button type="danger" plain :icon="Delete" :disabled="!selectedIds.length" @click="removeSelected">删除</el-button>
        </div>
        <div class="toolbar-right">
          <el-input
            v-model="keyword"
            clearable
            class="table-search"
            placeholder="搜索模板名称、标识或模板 ID"
            :prefix-icon="Search"
            @keyup.enter="page = 1"
          />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
        </div>
      </div>

      <div v-loading="loading" class="template-grid">
        <el-checkbox-group v-model="selectedIds" class="template-selection-group">
          <el-card v-for="item in pagedTemplates" :key="item.id" class="template-card" shadow="never">
            <div class="template-card-head">
              <el-checkbox :value="item.id" aria-label="选择模板" />
              <div class="template-card-title">
                <strong>{{ item.name || '未命名模板' }}</strong>
                <span>{{ item.typeLabel || '小程序订阅消息' }}</span>
              </div>
              <el-tag :type="item.enabled ? 'success' : 'info'" effect="light" size="small">
                {{ item.enabled ? '启用' : '停用' }}
              </el-tag>
            </div>

            <div class="template-card-body">
              <div class="template-id-line">
                <span class="field-label">标识</span>
                <code>{{ item.identifier || '-' }}</code>
              </div>
              <div class="template-id-line">
                <span class="field-label">模板 ID</span>
                <code class="template-id">{{ item.templateId || '-' }}</code>
              </div>

              <div class="template-param-section">
                <div class="section-label">参数映射 <span>{{ item.params.length }}</span></div>
                <div v-if="item.params.length" class="template-param-list">
                  <div v-for="param in item.params" :key="param.id" class="template-param-item">
                    <span>{{ param.description || '-' }}</span>
                    <code>{{ param.field || '-' }}</code>
                    <small>{{ param.mapping || '-' }}</small>
                  </div>
                </div>
                <span v-else class="empty-param">未配置参数映射</span>
              </div>

              <div class="template-card-foot">
                <span>更新于 {{ formatDateTime(item.updatedAt) }}</span>
                <div class="row-actions">
                  <el-button link type="primary" :icon="Edit" @click="openEdit(item)">编辑</el-button>
                  <el-button link type="danger" :icon="Delete" @click="removeRows([item])">删除</el-button>
                </div>
              </div>
            </div>
          </el-card>
        </el-checkbox-group>

        <el-empty v-if="!pagedTemplates.length && !loading" description="暂无订阅消息模板" :image-size="90" />
      </div>

      <div class="list-footer">
        <span>显示第 {{ filteredTemplates.length ? (page - 1) * pageSize + 1 : 0 }} 到 {{ Math.min(page * pageSize, filteredTemplates.length) }} 条记录，共 {{ filteredTemplates.length }} 条记录</span>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :page-sizes="[8, 12, 24]"
          layout="sizes, prev, pager, next, jumper"
          :total="filteredTemplates.length"
          background
        />
      </div>
    </el-card>

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '添加订阅消息模板' : '编辑订阅消息模板'" width="860px" destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="rules" label-width="104px" class="dialog-form subscription-form">
        <el-form-item label="类型">
          <el-radio-group v-model="form.type">
            <el-radio value="miniProgram">小程序订阅消息</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="标识" prop="identifier">
          <el-input v-model="form.identifier" placeholder="例如：miniprogram_order_success" />
        </el-form-item>
        <el-form-item label="模板名称" prop="name">
          <el-input v-model="form.name" placeholder="例如：预定民宿成功" />
        </el-form-item>
        <el-form-item label="模板 ID" prop="templateId">
          <el-input v-model="form.templateId" placeholder="请输入微信公众平台提供的模板 ID" />
        </el-form-item>

        <el-form-item label="参数映射">
          <div class="params-editor">
            <div class="params-editor-head">
              <span>字段说明</span>
              <span>字段</span>
              <span>映射库字段</span>
              <span>排序</span>
              <span></span>
            </div>
            <div v-for="(param, index) in form.params" :key="param.id" class="params-editor-row">
              <el-input v-model="param.description" placeholder="例如：民宿名称" />
              <el-input v-model="param.field" placeholder="thing1" />
              <el-input v-model="param.mapping" placeholder="shop_name" />
              <div class="param-sort-actions">
                <el-button circle text :icon="ArrowUp" :disabled="index === 0" title="上移" @click="moveParam(index, -1)" />
                <el-button circle text :icon="ArrowDown" :disabled="index === form.params.length - 1" title="下移" @click="moveParam(index, 1)" />
              </div>
              <el-button circle text type="danger" :icon="Delete" title="删除参数" @click="removeParam(index)" />
            </div>
            <el-button class="add-param-button" type="success" plain :icon="Plus" @click="addParam">添加参数</el-button>
          </div>
        </el-form-item>

        <el-form-item label="状态">
          <el-switch v-model="form.enabled" active-text="启用" inactive-text="停用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialog.visible = false">取消</el-button>
        <el-button type="primary" :loading="dialog.saving" @click="save">保存模板</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, ArrowUp, Delete, Edit, Plus, Refresh, Search } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'

const TEMPLATE_ENDPOINT = '/admin/subscription-templates'

const loading = ref(false)
const sourceMode = ref('api')
const keyword = ref('')
const page = ref(1)
const pageSize = ref(8)
const templates = ref([])
const selectedIds = ref([])
const formRef = ref()
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive(emptyForm())

const rules = {
  identifier: [{ required: true, message: '请输入模板标识', trigger: 'blur' }],
  name: [{ required: true, message: '请输入模板名称', trigger: 'blur' }],
  templateId: [{ required: true, message: '请输入模板 ID', trigger: 'blur' }]
}

const filteredTemplates = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  if (!query) return templates.value
  return templates.value.filter((item) => [item.name, item.identifier, item.templateId, item.typeLabel]
    .concat(item.params.flatMap((param) => [param.description, param.field, param.mapping]))
    .some((value) => String(value || '').toLowerCase().includes(query)))
})
const pagedTemplates = computed(() => filteredTemplates.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch([keyword, pageSize], () => { page.value = 1 })

function emptyForm() {
  return {
    id: null,
    type: 'miniProgram',
    identifier: '',
    name: '',
    templateId: '',
    enabled: true,
    params: []
  }
}

function normalizeParam(param = {}, index = 0) {
  return {
    id: param.id || `param-${Date.now()}-${index}-${Math.random().toString(16).slice(2)}`,
    description: param.description || param.fieldDescription || param.label || '',
    field: param.field || param.key || '',
    mapping: param.mapping || param.mappingField || param.source || ''
  }
}

function normalizeTemplate(item = {}, index = 0) {
  const params = item.params || item.parameters || item.fields || []
  return {
    id: item.id ?? item.templateId ?? item.templateID ?? `demo-${index + 1}`,
    type: item.type || 'miniProgram',
    typeLabel: item.typeLabel || '小程序订阅消息',
    identifier: item.identifier || item.code || item.mark || '',
    name: item.name || item.templateName || item.title || '',
    templateId: item.templateId || item.templateID || item.wxTemplateId || '',
    enabled: item.enabled ?? item.isEnabled ?? (item.status ? item.status === 'active' : true),
    params: Array.isArray(params) ? params.map(normalizeParam) : [],
    createdAt: item.createdAt || item.createTime || '',
    updatedAt: item.updatedAt || item.updateTime || item.createdAt || ''
  }
}

function normalizeList(payload) {
  const list = Array.isArray(payload) ? payload : payload?.items || payload?.data || payload?.rows || []
  return Array.isArray(list) ? list.map(normalizeTemplate) : []
}

function demoTemplates() {
  return [
    {
      id: 1,
      type: 'miniProgram',
      identifier: 'miniprogram_order_success',
      name: '预定民宿成功',
      templateId: 'CObfez5LQVt07Cs-T3YSaf80e5fU8BFz_9w-LWV0QbE',
      enabled: true,
      params: [
        { description: '民宿名称', field: 'thing6', mapping: 'shop_name' },
        { description: '民宿地址', field: 'thing7', mapping: 'shop_address' },
        { description: '入离日期', field: 'thing8', mapping: 'start_end_date' },
        { description: '住宿姓名', field: 'thing9', mapping: 'username' },
        { description: '房型名称', field: 'thing13', mapping: 'room_name' }
      ],
      createdAt: '2026-01-30 16:05:14',
      updatedAt: '2026-10-08 09:20:00'
    },
    {
      id: 2,
      type: 'miniProgram',
      identifier: 'miniprogram_cancel',
      name: '预订取消成功通知',
      templateId: 'kCcmOB4H5xyDWCqfW57fFbLW-pH-pnqQCGIM9gSDcY84',
      enabled: true,
      params: [
        { description: '订单单号', field: 'character_string8', mapping: 'order_sn' },
        { description: '酒店名称', field: 'thing1', mapping: 'shop_name' },
        { description: '入住房型', field: 'thing7', mapping: 'room_name' },
        { description: '入住日期', field: 'date3', mapping: 'start_date' }
      ],
      createdAt: '2026-02-03 10:45:51',
      updatedAt: '2026-10-08 09:20:00'
    },
    {
      id: 3,
      type: 'miniProgram',
      identifier: 'miniprogram_leave_alert',
      name: '客人退房提醒',
      templateId: '***********************HIfzZUNNNNE',
      enabled: true,
      params: [
        { description: '民宿名称', field: 'thing1', mapping: 'shop_name' },
        { description: '入住人', field: 'name3', mapping: 'username' },
        { description: '入住日期', field: 'date4', mapping: 'start_date' },
        { description: '到期日期', field: 'time7', mapping: 'end_date' }
      ],
      createdAt: '2026-02-03 10:48:32',
      updatedAt: '2026-10-08 09:20:00'
    },
    {
      id: 4,
      type: 'miniProgram',
      identifier: 'miniprogram_enter_alert',
      name: '预订入住提醒',
      templateId: 'ig94dS3oiATi0-z3BjktRts6dHAdYEwc6pDdDFRFfs',
      enabled: true,
      params: [
        { description: '民宿名称', field: 'thing1', mapping: 'shop_name' },
        { description: '入住时间', field: 'time5', mapping: 'start_time' },
        { description: '备注', field: 'thing4', mapping: 'enter_remark' }
      ],
      createdAt: '2026-02-04 10:48:55',
      updatedAt: '2026-10-08 09:20:00'
    },
    {
      id: 5,
      type: 'miniProgram',
      identifier: 'miniprogram_withdraw_success',
      name: '提现审核通过',
      templateId: '************************ifTPilylIHA',
      enabled: false,
      params: [
        { description: '申请时间', field: 'time1', mapping: 'createtime' },
        { description: '提现金额', field: 'amount3', mapping: 'money' },
        { description: '处理时间', field: 'time2', mapping: 'checktime' },
        { description: '提现状态', field: 'phrase4', mapping: 'state' },
        { description: '备注', field: 'thing5', mapping: 'reason' }
      ],
      createdAt: '2026-05-09 11:03:06',
      updatedAt: '2026-10-08 09:20:00'
    }
  ].map(normalizeTemplate)
}

async function requestList() {
  return normalizeList(await get(`${TEMPLATE_ENDPOINT}?includeDisabled=true`))
}

async function load() {
  loading.value = true
  try {
    const result = await requestList()
    templates.value = result
    sourceMode.value = 'api'
    selectedIds.value = []
  } catch (error) {
    ElMessage.error(error.message || '订阅消息模板加载失败')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  Object.assign(form, emptyForm())
  form.params = []
  dialog.mode = 'create'
  dialog.visible = true
}

function openEdit(item) {
  Object.assign(form, {
    id: item.id,
    type: item.type || 'miniProgram',
    identifier: item.identifier || '',
    name: item.name || '',
    templateId: item.templateId || '',
    enabled: item.enabled !== false,
    params: item.params.map((param, index) => normalizeParam(param, index))
  })
  dialog.mode = 'edit'
  dialog.visible = true
}

function addParam() {
  form.params.push(normalizeParam({}, form.params.length))
}

function removeParam(index) {
  form.params.splice(index, 1)
}

function moveParam(index, offset) {
  const target = index + offset
  if (target < 0 || target >= form.params.length) return
  const [item] = form.params.splice(index, 1)
  form.params.splice(target, 0, item)
}

function formPayload() {
  return {
    type: form.type,
    identifier: form.identifier.trim(),
    name: form.name.trim(),
    templateId: form.templateId.trim(),
    isEnabled: Boolean(form.enabled),
    parameters: form.params.map(({ description, field, mapping }) => ({
      description,
      field,
      mappingField: mapping
    }))
  }
}

function updateLocal(item) {
  const index = templates.value.findIndex((entry) => entry.id === item.id)
  if (index === -1) templates.value.unshift(item)
  else templates.value[index] = item
}

async function save() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  dialog.saving = true
  const payload = formPayload()
  try {
    const result = dialog.mode === 'create'
      ? await post(TEMPLATE_ENDPOINT, payload)
      : await put(`${TEMPLATE_ENDPOINT}/${encodeURIComponent(form.id)}`, payload)
    const saved = normalizeTemplate(result?.item || result?.data || result)
    updateLocal(saved)
    sourceMode.value = 'api'
    ElMessage.success('模板已保存')
    dialog.visible = false
  } catch (error) {
    ElMessage.error(error.message || '模板保存失败')
  } finally {
    dialog.saving = false
  }
}

async function removeRows(items) {
  if (!items.length) return
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 个模板吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }

  try {
    await Promise.all(items.map((item) => del(`${TEMPLATE_ENDPOINT}/${encodeURIComponent(item.id)}`)))
  } catch (error) {
    ElMessage.error(error.message || '模板删除失败')
    return
  }
  const ids = new Set(items.map((item) => item.id))
  templates.value = templates.value.filter((item) => !ids.has(item.id))
  selectedIds.value = selectedIds.value.filter((id) => !ids.has(id))
  ElMessage.success('模板已删除')
}

function removeSelected() {
  removeRows(templates.value.filter((item) => selectedIds.value.includes(item.id)))
}

function formatDateTime(value) {
  if (!value) return '未记录'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

onMounted(load)
</script>

<style scoped>
.subscription-page .page-meta{display:flex;align-items:center;gap:8px;margin-top:9px;color:var(--admin-muted);font-size:11px}
.subscription-toolbar{margin-bottom:18px}
.template-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px;min-height:220px}
.template-selection-group{display:contents}
.template-card{min-width:0;border-color:var(--admin-border)!important;background:var(--admin-surface-soft)!important}
.template-card :deep(.el-card__body){padding:16px}
.template-card-head{display:flex;align-items:flex-start;gap:8px;min-width:0}
.template-card-title{display:grid;min-width:0;flex:1;gap:5px}
.template-card-title strong{overflow:hidden;color:var(--admin-text);font-size:15px;text-overflow:ellipsis;white-space:nowrap}
.template-card-title span{color:var(--admin-muted);font-size:11px}
.template-card-body{display:grid;gap:12px;margin-top:16px}
.template-id-line{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:8px}
.field-label,.section-label{color:var(--admin-muted);font-size:11px}
.template-id-line code{overflow:hidden;color:var(--admin-text);font-family:Consolas,"SFMono-Regular",monospace;font-size:11px;text-overflow:ellipsis;white-space:nowrap}
.template-id-line code.template-id{color:var(--admin-primary-dark)}
.template-param-section{padding-top:11px;border-top:1px solid var(--admin-border)}
.section-label{display:flex;align-items:center;justify-content:space-between;margin-bottom:8px}
.section-label span{color:var(--admin-primary);font-weight:700}
.template-param-list{display:grid;gap:6px}
.template-param-item{display:grid;grid-template-columns:minmax(0,1fr) 78px minmax(0,1fr);align-items:center;gap:6px;padding:6px 7px;border-radius:5px;background:var(--admin-surface)}
.template-param-item span,.template-param-item small,.template-param-item code{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.template-param-item span{color:var(--admin-text);font-size:11px}
.template-param-item code,.template-param-item small{color:var(--admin-muted);font-family:Consolas,"SFMono-Regular",monospace;font-size:10px}
.template-param-item small{font-family:inherit}
.empty-param{color:var(--admin-muted);font-size:11px}
.template-card-foot{display:flex;align-items:center;justify-content:space-between;gap:8px;padding-top:9px;border-top:1px solid var(--admin-border);color:var(--admin-muted);font-size:10px}
.template-card-foot .row-actions{gap:2px}
.params-editor{width:100%;overflow:auto}
.params-editor-head,.params-editor-row{display:grid;grid-template-columns:minmax(130px,1fr) minmax(110px,1fr) minmax(130px,1fr) 76px 36px;align-items:center;gap:8px;min-width:660px}
.params-editor-head{padding:0 8px 8px;color:var(--admin-muted);font-size:11px}
.params-editor-row{padding:8px;border-top:1px solid var(--admin-border)}
.params-editor-row .el-input{min-width:0}
.param-sort-actions{display:flex;gap:0}
.param-sort-actions .el-button{margin-left:0}
.add-param-button{margin-top:10px}
@media (max-width:1180px){.template-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media (max-width:720px){.template-grid{grid-template-columns:1fr}.subscription-toolbar{align-items:flex-start;flex-direction:column}.subscription-toolbar .toolbar-right{width:100%}.subscription-toolbar .table-search{width:100%}.template-card-foot{align-items:flex-start;flex-direction:column}}
</style>
