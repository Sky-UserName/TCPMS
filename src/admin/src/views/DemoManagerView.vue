<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <span class="eyebrow">{{ config.eyebrow }}</span>
        <h2>{{ config.title }}</h2>
        <p>{{ config.description }}</p>
      </div>
      <div class="heading-actions">
        <el-button :icon="Refresh" :loading="loading" @click="refresh">刷新</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">添加</el-button>
      </div>
    </section>

    <el-card class="panel-card list-card" shadow="never">
      <div class="status-tabs" role="tablist">
        <button
          v-for="tab in config.tabs"
          :key="tab.key"
          type="button"
          role="tab"
          class="status-tab"
          :class="{ active: activeTab === tab.key }"
          :aria-selected="activeTab === tab.key"
          @click="activeTab = tab.key"
        >
          {{ tab.label }}
          <span>{{ countForTab(tab.key) }}</span>
        </button>
      </div>

      <div class="list-toolbar">
        <div class="toolbar-left">
          <el-button :icon="Refresh" title="刷新" @click="refresh" />
          <el-button type="success" :icon="Plus" @click="openCreate">添加</el-button>
          <el-button type="primary" plain :icon="Edit" :disabled="selectedRows.length !== 1" @click="openEdit(selectedRows[0])">编辑</el-button>
          <el-button type="danger" plain :icon="Delete" :disabled="!selectedRows.length" @click="removeSelected">删除</el-button>
          <el-button v-if="config.approval" type="warning" plain :icon="CircleCheck" :disabled="!selectedRows.length" @click="approveSelected">审核</el-button>
        </div>
        <div class="toolbar-right">
          <el-input
            v-model="keyword"
            clearable
            class="table-search"
            placeholder="搜索关键字"
            :prefix-icon="Search"
            @keyup.enter="page = 1"
          />
          <el-button :icon="Search" title="搜索" @click="page = 1" />
          <el-dropdown trigger="click">
            <el-button :icon="MoreFilled" title="更多操作" />
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="exportRows">导出当前列表</el-dropdown-item>
                <el-dropdown-item @click="clearSelection">清空选择</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>

      <el-table
        v-loading="loading"
        :data="pagedRows"
        class="clean-table admin-data-table"
        row-key="id"
        @selection-change="selectedRows = $event"
      >
        <el-table-column type="selection" width="48" fixed="left" />
        <el-table-column
          v-for="column in config.columns"
          :key="column.key"
          :prop="column.key"
          :label="column.label"
          :width="column.width"
          :min-width="column.minWidth"
          :sortable="column.sortable ? 'custom' : false"
          show-overflow-tooltip
        >
          <template #default="{ row }">
            <template v-if="column.kind === 'status'">
              <span class="dot-status" :class="statusTone(row[column.key])"><i></i>{{ displayValue(column, row[column.key]) }}</span>
            </template>
            <template v-else-if="column.kind === 'tag'">
              <div class="tag-list">
                <el-tag v-for="tag in asTags(row[column.key])" :key="tag" size="small" effect="light">{{ tag }}</el-tag>
              </div>
            </template>
            <template v-else-if="column.kind === 'image'">
              <el-image
                v-if="row[column.key] && !isImageBroken(row, column)"
                class="table-thumb"
                :src="row[column.key]"
                :preview-src-list="[row[column.key]]"
                preview-teleported
                fit="cover"
                @error="markImageBroken(row, column)"
              />
              <span v-else class="table-image-placeholder"><el-icon><Picture /></el-icon></span>
            </template>
            <template v-else-if="column.kind === 'money'">
              <strong class="price-text">{{ money(row[column.key]) }}</strong>
            </template>
            <template v-else-if="column.kind === 'avatar'">
              <div class="avatar-cell">
                <span class="profile-avatar">{{ String(row.nickname || row[column.key] || '会').slice(0, 1) }}</span>
                <span>{{ row[column.key] || '-' }}</span>
              </div>
            </template>
            <template v-else>
              {{ displayValue(column, row[column.key]) }}
            </template>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="196" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <el-button v-if="config.approval && isPending(row)" type="success" link @click="approve(row)">通过</el-button>
              <el-button type="primary" link @click="openEdit(row)">编辑</el-button>
              <el-button type="danger" link @click="removeRows([row])">删除</el-button>
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

    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? `添加${config.title}` : `编辑${config.title}`" width="620px" destroy-on-close>
      <el-form :model="form" label-width="96px" class="dialog-form">
        <div class="form-grid">
          <el-form-item v-for="field in config.fields" :key="field.key" :class="{ 'field-wide': field.type === 'textarea' }" :label="field.label" :required="field.required">
            <el-select v-if="field.type === 'select'" v-model="form[field.key]" :placeholder="field.placeholder || `请选择${field.label}`" clearable>
              <el-option v-for="option in field.options" :key="option.value" :label="option.label" :value="option.value" />
            </el-select>
            <el-switch v-else-if="field.type === 'switch'" v-model="form[field.key]" active-text="启用" />
            <el-input-number v-else-if="field.type === 'number'" v-model="form[field.key]" :min="field.min ?? 0" controls-position="right" />
            <el-input v-else-if="field.type === 'textarea'" v-model="form[field.key]" type="textarea" :rows="3" :placeholder="field.placeholder" />
            <el-input v-else v-model="form[field.key]" :placeholder="field.placeholder" />
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
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CircleCheck, Delete, Edit, MoreFilled, Picture, Plus, Refresh, Search } from '@element-plus/icons-vue'

const props = defineProps({
  viewKey: { type: String, required: true }
})

const configs = {
  decoration: {
    eyebrow: 'PAGE BUILDER',
    title: '装修管理',
    description: '配置小程序首页、门店详情和发现页的展示内容。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'published', label: '已发布' }, { key: 'draft', label: '草稿' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '页面名称', minWidth: 190 }, { key: 'page', label: '页面位置', minWidth: 140 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '页面名称', required: true }, { key: 'page', label: '页面位置', required: true }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已发布', value: 'published' }, { label: '草稿', value: 'draft' }] }, { key: 'description', label: '页面说明', type: 'textarea' }]
  },
  roomTypes: {
    eyebrow: 'ROOM TAXONOMY',
    title: '房间类型',
    description: '维护房间类型字典，供门店房源和搜索筛选使用。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '名称', minWidth: 220 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'sort', label: '排序', width: 90 }, { key: 'createdAt', label: '创建时间', minWidth: 170 }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '类型名称', required: true }, { key: 'sort', label: '排序', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  levels: {
    eyebrow: 'PROPERTY LEVELS',
    title: '民宿等级',
    description: '维护门店等级和推荐标识，统一展示品质分层。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '等级名称', minWidth: 220 }, { key: 'description', label: '等级说明', minWidth: 280 }, { key: 'sort', label: '排序', width: 90 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '等级名称', required: true }, { key: 'description', label: '等级说明', type: 'textarea' }, { key: 'sort', label: '排序', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  facilities: {
    eyebrow: 'FACILITIES',
    title: '服务设施',
    description: '维护房源可选设施，统一门店展示和筛选口径。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '设施名称', minWidth: 220 }, { key: 'category', label: '分类', minWidth: 140 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'sort', label: '排序', width: 90 }],
    fields: [{ key: 'name', label: '设施名称', required: true }, { key: 'category', label: '设施分类', type: 'select', options: [{ label: '基础设施', value: '基础设施' }, { label: '卫浴设施', value: '卫浴设施' }, { label: '公共服务', value: '公共服务' }] }, { key: 'sort', label: '排序', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  tags: {
    eyebrow: 'PROPERTY TAGS',
    title: '民宿标签',
    description: '维护门店和攻略使用的标签体系。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '标签名称', minWidth: 200 }, { key: 'color', label: '颜色', width: 110 }, { key: 'usage', label: '使用数量', width: 110 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '标签名称', required: true }, { key: 'color', label: '颜色', placeholder: '例如：#16b79a' }, { key: 'usage', label: '使用数量', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  regions: {
    eyebrow: 'REGION DICTIONARY',
    title: '地区管理',
    description: '维护省市区和商圈层级，支撑门店定位和检索。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '地区名称', minWidth: 220 }, { key: 'parent', label: '上级地区', minWidth: 180 }, { key: 'level', label: '层级', width: 90 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '地区名称', required: true }, { key: 'parent', label: '上级地区' }, { key: 'level', label: '层级', type: 'select', options: [{ label: '省/直辖市', value: '省/直辖市' }, { label: '城市', value: '城市' }, { label: '区县', value: '区县' }, { label: '商圈', value: '商圈' }] }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  commonConfig: {
    eyebrow: 'SYSTEM SETTINGS',
    title: '通用配置',
    description: '集中管理平台名称、订单规则、客服信息和运营开关。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'enabled', label: '已启用' }, { key: 'disabled', label: '已停用' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '配置名称', minWidth: 220 }, { key: 'key', label: '配置键', minWidth: 220 }, { key: 'value', label: '配置值', minWidth: 200 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '配置名称', required: true }, { key: 'key', label: '配置键', required: true }, { key: 'value', label: '配置值', required: true }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已启用', value: 'enabled' }, { label: '已停用', value: 'disabled' }] }]
  },
  pages: {
    eyebrow: 'CONTENT PAGES',
    title: '单页文章',
    description: '编辑关于我们、入住须知、隐私协议等平台内容。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'published', label: '已发布' }, { key: 'draft', label: '草稿' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '文章标题', minWidth: 260 }, { key: 'slug', label: '页面标识', minWidth: 180 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '文章标题', required: true }, { key: 'slug', label: '页面标识', required: true }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已发布', value: 'published' }, { label: '草稿', value: 'draft' }] }, { key: 'content', label: '文章内容', type: 'textarea' }]
  },
  reviews: {
    eyebrow: 'REVIEW CENTER',
    title: '评价管理',
    description: '查看住客评价、关联门店和房间，维护上架内容与评分。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'hidden', label: '隐藏' }, { key: 'published', label: '上架' }],
    columns: [
      { key: 'id', label: 'ID', width: 72 },
      { key: 'user', label: '用户', minWidth: 150, kind: 'avatar' },
      { key: 'store', label: '门店', minWidth: 180 },
      { key: 'room', label: '房间', minWidth: 180 },
      { key: 'image', label: '图片', width: 90, kind: 'image' },
      { key: 'content', label: '评价内容', minWidth: 300 },
      { key: 'status', label: '状态', width: 100, kind: 'status' },
      { key: 'score', label: '星级', width: 80 },
      { key: 'createdAt', label: '评价时间', minWidth: 170 }
    ],
    fields: [
      { key: 'user', label: '用户', required: true },
      { key: 'store', label: '门店', required: true },
      { key: 'room', label: '房间', required: true },
      { key: 'content', label: '评价内容', type: 'textarea', required: true },
      { key: 'score', label: '星级', type: 'number', min: 1 },
      { key: 'status', label: '状态', type: 'select', options: [{ label: '上架', value: 'published' }, { label: '隐藏', value: 'hidden' }] }
    ]
  },
  roomManagement: {
    eyebrow: 'ROOM MANAGEMENT',
    title: '房间管理',
    description: '维护房屋介绍、加入规则、审核状态和房源展示状态。',
    approval: true,
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [
      { key: 'rule', label: '加入规则', minWidth: 170 },
      { key: 'description', label: '房屋介绍', minWidth: 260 },
      { key: 'status', label: '状态', width: 90, kind: 'status' },
      { key: 'reservations', label: '总预定数', width: 100 },
      { key: 'approval', label: '审核状态', width: 100, kind: 'status' },
      { key: 'draft', label: '是否草稿', width: 90 },
      { key: 'rejectionReason', label: '拒绝理由', minWidth: 180 },
      { key: 'approvalAt', label: '审核时间', minWidth: 170 },
      { key: 'sort', label: '排序', width: 80 },
      { key: 'createdAt', label: '创建时间', minWidth: 170 },
      { key: 'updatedAt', label: '更新时间', minWidth: 170 }
    ],
    fields: [
      { key: 'rule', label: '加入规则', required: true },
      { key: 'description', label: '房屋介绍', type: 'textarea', required: true },
      { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] },
      { key: 'approval', label: '审核状态', type: 'select', options: [{ label: '通过', value: 'approved' }, { label: '审核中', value: 'pending' }, { label: '未通过', value: 'rejected' }] },
      { key: 'draft', label: '是否草稿', type: 'select', options: [{ label: '是', value: '是' }, { label: '否', value: '否' }] },
      { key: 'rejectionReason', label: '拒绝理由' },
      { key: 'sort', label: '排序', type: 'number' }
    ]
  },
  guideTypes: {
    eyebrow: 'DISCOVERY',
    title: '攻略类型',
    description: '维护发现页攻略的分类、标签和排序。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '正常' }, { key: 'hidden', label: '隐藏' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '类型名称', minWidth: 220 }, { key: 'color', label: '标签色', width: 110 }, { key: 'sort', label: '排序', width: 90 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '类型名称', required: true }, { key: 'color', label: '标签色' }, { key: 'sort', label: '排序', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  guides: {
    eyebrow: 'DISCOVERY',
    title: '攻略管理',
    description: '审核和维护用户投稿的旅行攻略内容。',
    approval: true,
    tabs: [{ key: 'all', label: '全部' }, { key: 'pending', label: '待审核' }, { key: 'approved', label: '已通过' }, { key: 'rejected', label: '已拒绝' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '标题', minWidth: 260 }, { key: 'user', label: '用户', width: 120 }, { key: 'image', label: '图片', width: 90, kind: 'image' }, { key: 'store', label: '关联民宿', minWidth: 160 }, { key: 'type', label: '攻略类型', width: 160, kind: 'tag' }, { key: 'status', label: '状态', width: 100, kind: 'status' }, { key: 'approval', label: '审核状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '攻略标题', required: true }, { key: 'user', label: '用户' }, { key: 'store', label: '关联民宿' }, { key: 'type', label: '攻略类型', type: 'select', options: [{ label: '亲子', value: '亲子' }, { label: '人文', value: '人文' }, { label: '自然风景', value: '自然风景' }, { label: '沙滩', value: '沙滩' }] }, { key: 'approval', label: '审核状态', type: 'select', options: [{ label: '待审核', value: 'pending' }, { label: '已通过', value: 'approved' }, { label: '已拒绝', value: 'rejected' }] }, { key: 'status', label: '状态', type: 'select', options: [{ label: '正常', value: 'active' }, { label: '隐藏', value: 'hidden' }] }]
  },
  search: {
    eyebrow: 'SEARCH INSIGHTS',
    title: '搜索记录',
    description: '查看用户搜索词和热门目的地，辅助内容与房源运营。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'today', label: '今日' }, { key: 'hot', label: '热门' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'keyword', label: '搜索词', minWidth: 240 }, { key: 'user', label: '用户', width: 140 }, { key: 'count', label: '搜索次数', width: 110 }, { key: 'result', label: '结果数', width: 100 }, { key: 'createdAt', label: '最近搜索时间', minWidth: 180 }],
    fields: [{ key: 'keyword', label: '搜索词', required: true }, { key: 'user', label: '用户' }, { key: 'count', label: '搜索次数', type: 'number' }, { key: 'result', label: '结果数', type: 'number' }]
  },
  withdrawals: {
    eyebrow: 'FINANCE',
    title: '门店提现',
    description: '审核门店提现申请，查看账户、金额和到账状态。',
    approval: true,
    tabs: [{ key: 'all', label: '全部' }, { key: 'pending', label: '待审核' }, { key: 'approved', label: '已通过' }, { key: 'rejected', label: '已拒绝' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'store', label: '门店', minWidth: 220 }, { key: 'account', label: '收款账户', minWidth: 180 }, { key: 'amount', label: '提现金额', width: 120, kind: 'money' }, { key: 'status', label: '审核状态', width: 110, kind: 'status' }, { key: 'createdAt', label: '申请时间', minWidth: 170 }],
    fields: [{ key: 'store', label: '门店', required: true }, { key: 'account', label: '收款账户', required: true }, { key: 'amount', label: '提现金额（分）', type: 'number', min: 1 }, { key: 'status', label: '审核状态', type: 'select', options: [{ label: '待审核', value: 'pending' }, { label: '已通过', value: 'approved' }, { label: '已拒绝', value: 'rejected' }] }]
  },
  memberWithdrawals: {
    eyebrow: 'FINANCE',
    title: '会员提现',
    description: '审核会员佣金提现申请和处理记录。',
    approval: true,
    tabs: [{ key: 'all', label: '全部' }, { key: 'pending', label: '待审核' }, { key: 'approved', label: '已通过' }, { key: 'rejected', label: '已拒绝' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'user', label: '会员', minWidth: 180 }, { key: 'account', label: '收款账户', minWidth: 180 }, { key: 'amount', label: '提现金额', width: 120, kind: 'money' }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'createdAt', label: '申请时间', minWidth: 170 }],
    fields: [{ key: 'user', label: '会员', required: true }, { key: 'account', label: '收款账户', required: true }, { key: 'amount', label: '提现金额（分）', type: 'number', min: 1 }, { key: 'status', label: '状态', type: 'select', options: [{ label: '待审核', value: 'pending' }, { label: '已通过', value: 'approved' }, { label: '已拒绝', value: 'rejected' }] }]
  },
  distribution: {
    eyebrow: 'FINANCE',
    title: '分销日志',
    description: '查看会员推荐关系、订单分佣和结算明细。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'settled', label: '已结算' }, { key: 'pending', label: '待结算' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'user', label: '会员', minWidth: 170 }, { key: 'order', label: '关联订单', minWidth: 180 }, { key: 'amount', label: '佣金', width: 110, kind: 'money' }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'createdAt', label: '记录时间', minWidth: 170 }],
    fields: [{ key: 'user', label: '会员', required: true }, { key: 'order', label: '关联订单' }, { key: 'amount', label: '佣金（分）', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已结算', value: 'settled' }, { label: '待结算', value: 'pending' }] }]
  },
  income: {
    eyebrow: 'FINANCE',
    title: '门店收入',
    description: '按门店和日期查看订单、实收、退款与可结算金额。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'today', label: '今日' }, { key: 'month', label: '本月' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'store', label: '门店', minWidth: 220 }, { key: 'orders', label: '订单数', width: 100 }, { key: 'income', label: '实收金额', width: 120, kind: 'money' }, { key: 'refund', label: '退款金额', width: 120, kind: 'money' }, { key: 'settlement', label: '可结算', width: 120, kind: 'money' }, { key: 'date', label: '统计日期', minWidth: 160 }],
    fields: [{ key: 'store', label: '门店', required: true }, { key: 'orders', label: '订单数', type: 'number' }, { key: 'income', label: '实收金额（分）', type: 'number' }, { key: 'refund', label: '退款金额（分）', type: 'number' }, { key: 'settlement', label: '可结算（分）', type: 'number' }]
  },
  messages: {
    eyebrow: 'MESSAGE CENTER',
    title: '消息管理',
    description: '管理系统通知、审核提醒和订阅消息发送记录。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'draft', label: '草稿' }, { key: 'sent', label: '已发送' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '消息标题', minWidth: 260 }, { key: 'type', label: '消息类型', width: 140 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'audience', label: '接收人数', width: 110 }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '消息标题', required: true }, { key: 'type', label: '消息类型', type: 'select', options: [{ label: '系统通知', value: '系统通知' }, { label: '订阅消息', value: '订阅消息' }, { label: '运营公告', value: '运营公告' }] }, { key: 'audience', label: '接收人数', type: 'number' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '草稿', value: 'draft' }, { label: '已发送', value: 'sent' }] }, { key: 'content', label: '消息内容', type: 'textarea' }]
  },
  plugins: {
    eyebrow: 'EXTENSIONS',
    title: '插件管理',
    description: '查看已安装插件、扩展能力和运行状态。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '运行中' }, { key: 'disabled', label: '已停用' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '插件名称', minWidth: 240 }, { key: 'version', label: '版本', width: 100 }, { key: 'developer', label: '开发者', minWidth: 150 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '插件名称', required: true }, { key: 'version', label: '版本' }, { key: 'developer', label: '开发者' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '运行中', value: 'active' }, { label: '已停用', value: 'disabled' }] }]
  },
  appCenter: {
    eyebrow: 'APP CENTER',
    title: '应用中心',
    description: '管理平台应用、服务连接和可选能力。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'active', label: '已启用' }, { key: 'disabled', label: '未启用' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '应用名称', minWidth: 240 }, { key: 'description', label: '应用说明', minWidth: 280 }, { key: 'status', label: '状态', width: 110, kind: 'status' }, { key: 'updatedAt', label: '更新时间', minWidth: 170 }],
    fields: [{ key: 'name', label: '应用名称', required: true }, { key: 'description', label: '应用说明', type: 'textarea' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已启用', value: 'active' }, { label: '未启用', value: 'disabled' }] }]
  },
  storePermissions: {
    eyebrow: 'STORE ACCESS',
    title: '门店权限',
    description: '配置门店账号的数据范围和可操作模块。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'enabled', label: '已启用' }, { key: 'disabled', label: '已停用' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'user', label: '账号', minWidth: 180 }, { key: 'store', label: '门店', minWidth: 220 }, { key: 'scope', label: '数据范围', minWidth: 180 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'user', label: '账号', required: true }, { key: 'store', label: '门店', required: true }, { key: 'scope', label: '数据范围' }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已启用', value: 'enabled' }, { label: '已停用', value: 'disabled' }] }]
  },
  common: {
    eyebrow: 'CONFIGURATION',
    title: '常规管理',
    description: '集中查看后台运行配置和基础运营参数。',
    tabs: [{ key: 'all', label: '全部' }, { key: 'enabled', label: '已启用' }, { key: 'disabled', label: '已停用' }],
    columns: [{ key: 'id', label: 'ID', width: 72 }, { key: 'name', label: '配置项', minWidth: 240 }, { key: 'key', label: '配置键', minWidth: 220 }, { key: 'value', label: '当前值', minWidth: 220 }, { key: 'status', label: '状态', width: 110, kind: 'status' }],
    fields: [{ key: 'name', label: '配置项', required: true }, { key: 'key', label: '配置键', required: true }, { key: 'value', label: '当前值', required: true }, { key: 'status', label: '状态', type: 'select', options: [{ label: '已启用', value: 'enabled' }, { label: '已停用', value: 'disabled' }] }]
  }
}

const config = computed(() => configs[props.viewKey] || configs.common)
const storageKey = computed(() => {
  const versionedViews = ['reviews', 'roomManagement']
  return `tcpms.admin.demo${versionedViews.includes(props.viewKey) ? '.v2' : ''}.${props.viewKey}`
})
const rows = ref([])
const selectedRows = ref([])
const keyword = ref('')
const activeTab = ref('all')
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const brokenImages = ref(new Set())
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive({})

const filteredRows = computed(() => {
  const query = keyword.value.trim().toLowerCase()
  return rows.value.filter((row) => {
    const matchesTab = rowMatchesTab(row, activeTab.value)
    const matchesQuery = !query || config.value.columns.some((column) => String(row[column.key] ?? '').toLowerCase().includes(query))
    return matchesTab && matchesQuery
  })
})
const pagedRows = computed(() => filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))

watch(() => props.viewKey, () => {
  activeTab.value = 'all'
  keyword.value = ''
  page.value = 1
  brokenImages.value = new Set()
  loadData()
}, { immediate: true })
watch([activeTab, keyword, pageSize], () => { page.value = 1 })

function seedFor(key) {
  const now = new Date().toLocaleString('zh-CN')
  const base = {
    decoration: [
      { id: 1, name: '小程序首页', page: '首页', status: 'published', updatedAt: now, description: '首页运营布局' },
      { id: 2, name: '发现页', page: '发现', status: 'draft', updatedAt: now, description: '旅行攻略内容流' }
    ],
    roomTypes: ['电竞房', '阁楼', '江景房', '山景房', '海景房', '小区', '花园洋房', '老式公寓', '别墅'].map((name, index) => ({ id: index + 1, name, status: 'active', sort: index + 1, createdAt: '2026-10-01 10:20:00', updatedAt: '2026-10-07 09:12:00' })),
    facilities: [
      { id: 1, name: '独立卫浴', category: '卫浴设施', status: 'active', sort: 1 },
      { id: 2, name: '无线网络', category: '基础设施', status: 'active', sort: 2 },
      { id: 3, name: '智能电视', category: '基础设施', status: 'active', sort: 3 },
      { id: 4, name: '行李寄存', category: '公共服务', status: 'hidden', sort: 4 }
    ],
    tags: [
      { id: 1, name: '亲子友好', color: '#16b79a', usage: 12, status: 'active' },
      { id: 2, name: '近地铁', color: '#4387d6', usage: 8, status: 'active' },
      { id: 3, name: '可带宠物', color: '#e29a3c', usage: 3, status: 'hidden' }
    ],
    regions: [
      { id: 1, name: '上海市', parent: '中国', level: '省/直辖市', status: 'active' },
      { id: 2, name: '黄浦区', parent: '上海市', level: '区县', status: 'active' },
      { id: 3, name: '人民广场商圈', parent: '黄浦区', level: '商圈', status: 'active' }
    ],
    levels: [
      { id: 1, name: '无等级', description: '普通民宿', sort: 1, status: 'active' },
      { id: 2, name: '优质民宿', description: '服务和房源质量稳定', sort: 2, status: 'active' },
      { id: 3, name: '品牌推荐', description: '平台重点推荐门店', sort: 3, status: 'active' }
    ],
    commonConfig: [
      { id: 1, name: '平台名称', key: 'platform.name', value: 'TCPMS 智选旅宿', status: 'enabled' },
      { id: 2, name: '订单超时分钟', key: 'order.payment_timeout', value: '15', status: 'enabled' },
      { id: 3, name: '客服热线', key: 'service.phone', value: '400-800-1234', status: 'disabled' }
    ],
    common: [
      { id: 1, name: '默认入住时间', key: 'checkin.time', value: '14:00', status: 'enabled' },
      { id: 2, name: '默认退房时间', key: 'checkout.time', value: '12:00', status: 'enabled' }
    ],
    pages: [
      { id: 1, name: '关于我们', slug: 'about', status: 'published', updatedAt: now, content: '平台介绍' },
      { id: 2, name: '入住须知', slug: 'check-in-notice', status: 'draft', updatedAt: now, content: '入住规则' }
    ],
    reviews: [
      { id: 6, user: '小钟哥', store: '好人民宿', room: '江景生态环境优美宜人', image: 'https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?w=120&h=72&fit=crop', content: '扇啊', score: 5, status: 'published', createdAt: '2026-10-07 09:20:00' },
      { id: 5, user: '星星', store: '一方幽静', room: '一方幽静·茶香', image: 'https://images.unsplash.com/photo-1566073771259-6a8506099945?w=120&h=72&fit=crop', content: '非常不错的环境，很舒适自在，周围也很安静。', score: 4, status: 'published', createdAt: '2026-10-06 16:40:00' },
      { id: 4, user: '鑫', store: '夕霞小筑', room: '夕霞小筑·夕霞房', image: 'https://images.unsplash.com/photo-1540541338287-41700207dee6?w=120&h=72&fit=crop', content: '房子很好，可以来。', score: 5, status: 'published', createdAt: '2026-10-05 13:15:00' },
      { id: 3, user: '心', store: '夕霞小筑', room: '夕霞小筑·夕霞房', image: '', content: '房子环境很好，周围设施很全面。', score: 5, status: 'hidden', createdAt: '2026-10-04 18:02:00' },
      { id: 2, user: '鑫', store: '夕霞小筑', room: '小筑房', image: 'https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?w=120&h=72&fit=crop', content: '房子非常漂亮啊，可以来看看体验下。', score: 5, status: 'published', createdAt: '2026-10-03 11:45:00' },
      { id: 1, user: '心', store: '夕霞小筑', room: '小筑房', image: 'https://images.unsplash.com/photo-1445019980597-93fa8acb246c?w=120&h=72&fit=crop', content: '房子非常不错OK，干净整洁卫生，服务也很好。', score: 4, status: 'published', createdAt: '2026-10-02 10:20:00' }
    ],
    roomManagement: [
      { id: 16, rule: '无', description: '欢迎入住', status: 'active', reservations: 1, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-07-29 18:02:06', sort: 0, createdAt: '2026-05-20 18:31:54', updatedAt: '2026-10-07 17:54:56' },
      { id: 15, rule: '这是费用须知', description: '这里是房屋介绍', status: 'active', reservations: 0, approval: 'pending', draft: '否', rejectionReason: '', approvalAt: '', sort: 0, createdAt: '2026-09-16 10:07:56', updatedAt: '2026-09-16 10:07:56' },
      { id: 14, rule: '加入规则说明', description: '房屋介绍说明', status: 'active', reservations: 0, approval: 'pending', draft: '否', rejectionReason: '', approvalAt: '', sort: 0, createdAt: '2026-08-29 07:52:51', updatedAt: '2026-08-29 07:52:51' },
      { id: 13, rule: '55899', description: '56585', status: 'active', reservations: 0, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-07-15 21:31:46', sort: 0, createdAt: '2026-06-04 10:21:24', updatedAt: '2026-07-15 21:31:46' },
      { id: 12, rule: '不含停车费', description: '这是一个房屋介绍，很长，很多字', status: 'active', reservations: 0, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '', sort: 0, createdAt: '2026-06-26 11:06:19', updatedAt: '2026-06-26 11:07:19' },
      { id: 11, rule: '不含停车费', description: '民你农民他mood，哇麒麟我', status: 'active', reservations: 2, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '', sort: 0, createdAt: '2026-06-19 11:23:31', updatedAt: '2026-06-19 11:26:40' },
      { id: 10, rule: '不能', description: '哈哈哈', status: 'hidden', reservations: 0, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-06-11 15:27:23', sort: 0, createdAt: '2026-06-11 15:27:23', updatedAt: '2026-06-11 15:27:23' },
      { id: 9, rule: '不取消', description: '周边好玩', status: 'active', reservations: 0, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-05-25 14:18:44', sort: 0, createdAt: '2026-05-25 14:18:44', updatedAt: '2026-05-25 14:21:47' },
      { id: 8, rule: '不可加入', description: '南北通透，明亮宽敞，干净整洁', status: 'active', reservations: 1, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-05-07 11:25:15', sort: 0, createdAt: '2026-04-21 17:02:15', updatedAt: '2026-05-07 11:25:15' },
      { id: 7, rule: '不可加入', description: '南北通透，干净整洁', status: 'active', reservations: 3, approval: 'approved', draft: '否', rejectionReason: '', approvalAt: '2026-04-29 18:08:30', sort: 0, createdAt: '2026-04-21 11:43:31', updatedAt: '2026-04-29 18:08:30' }
    ],
    guideTypes: [
      { id: 1, name: '亲子', color: '#16b79a', sort: 1, status: 'active' },
      { id: 2, name: '人文', color: '#4387d6', sort: 2, status: 'active' },
      { id: 3, name: '自然风景', color: '#e29a3c', sort: 3, status: 'active' },
      { id: 4, name: '沙滩', color: '#ef766a', sort: 4, status: 'hidden' }
    ],
    guides: [
      { id: 4, name: '住过一次就念念不忘', user: '星星', image: 'https://images.unsplash.com/photo-1500534623283-312aade485b7?w=120&h=72&fit=crop', store: '上海人民广场店', type: ['亲子', '人文', '自然风景'], status: 'active', approval: 'approved' },
      { id: 3, name: '很温馨的度假房间', user: '星星', image: 'https://images.unsplash.com/photo-1566073771259-6a8506099945?w=120&h=72&fit=crop', store: '杭州西湖店', type: ['亲子', '人文'], status: 'active', approval: 'pending' },
      { id: 2, name: '海边度假游', user: '新', image: 'https://images.unsplash.com/photo-1507520781678-1c2b1a1a1c1b?w=120&h=72&fit=crop', store: '杭州西湖店', type: ['亲子', '沙滩'], status: 'active', approval: 'approved' }
    ],
    search: [
      { id: 1, keyword: '西湖民宿', user: '星星', count: 38, result: 24, createdAt: now },
      { id: 2, keyword: '上海亲子房', user: '新', count: 22, result: 12, createdAt: now },
      { id: 3, keyword: '近地铁', user: '匿名用户', count: 16, result: 18, createdAt: now }
    ],
    withdrawals: [
      { id: 1, store: '上海人民广场店', account: '微信商户号 · 0042', amount: 128000, status: 'pending', createdAt: now },
      { id: 2, store: '杭州西湖店', account: '对公账户 · 8821', amount: 86000, status: 'approved', createdAt: '2026-10-06 14:12:00' }
    ],
    memberWithdrawals: [
      { id: 1, user: '星星', account: '微信 · 138****0000', amount: 6800, status: 'pending', createdAt: now },
      { id: 2, user: '新', account: '微信 · 139****0001', amount: 12000, status: 'approved', createdAt: '2026-10-05 11:00:00' }
    ],
    distribution: [
      { id: 1, user: '星星', order: 'TCP202610010001', amount: 5360, status: 'settled', createdAt: now },
      { id: 2, user: '新', order: 'TCP202610010002', amount: 1760, status: 'pending', createdAt: now }
    ],
    income: [
      { id: 1, store: '上海人民广场店', orders: 18, income: 486000, refund: 32000, settlement: 454000, date: '2026-10-07' },
      { id: 2, store: '杭州西湖店', orders: 11, income: 368000, refund: 0, settlement: 368000, date: '2026-10-07' }
    ],
    messages: [
      { id: 1, name: '新订单待确认提醒', type: '系统通知', status: 'sent', audience: 12, updatedAt: now },
      { id: 2, name: '国庆入住须知', type: '运营公告', status: 'draft', audience: 0, updatedAt: now }
    ],
    plugins: [
      { id: 1, name: '高德地图', version: '1.2.0', developer: 'TCPMS', status: 'active', updatedAt: now },
      { id: 2, name: '微信支付', version: '0.9.3', developer: 'TCPMS', status: 'disabled', updatedAt: now }
    ],
    appCenter: [
      { id: 1, name: '微信支付', description: '订单支付、退款与对账', status: 'active', updatedAt: now },
      { id: 2, name: '高德地图', description: '门店定位和地址解析', status: 'active', updatedAt: now },
      { id: 3, name: '订阅消息', description: '入住、退款和审核通知', status: 'disabled', updatedAt: now }
    ],
    storePermissions: [
      { id: 1, user: 'shanghai_admin', store: '上海人民广场店', scope: '本店订单、房源与收入', status: 'enabled' },
      { id: 2, user: 'hangzhou_staff', store: '杭州西湖店', scope: '本店入住与核销', status: 'enabled' }
    ]
  }
  return base[key] || base.common
}

function loadData() {
  const saved = localStorage.getItem(storageKey.value)
  rows.value = saved ? JSON.parse(saved) : seedFor(props.viewKey)
  if (!saved) persist()
}
function persist() { localStorage.setItem(storageKey.value, JSON.stringify(rows.value)) }
function refresh() {
  loading.value = true
  window.setTimeout(() => { loading.value = false; ElMessage.success('列表已刷新') }, 240)
}
function countForTab(key) {
  if (key === 'all') return rows.value.length
  return rows.value.filter((row) => rowMatchesTab(row, key)).length
}
function rowMatchesTab(row, key) {
  if (key === 'all') return true
  if (props.viewKey === 'search') {
    if (key === 'today') return isDateToday(row.createdAt)
    if (key === 'hot') return Number(row.count) >= 20
  }
  if (props.viewKey === 'income') {
    if (key === 'today') return isDateToday(row.date)
    if (key === 'month') return isDateThisMonth(row.date)
  }
  return Object.values(row).includes(key)
}
function isDateToday(value) {
  if (!value) return false
  const date = new Date(value)
  const now = new Date()
  return date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth() && date.getDate() === now.getDate()
}
function isDateThisMonth(value) {
  if (!value) return false
  const date = new Date(value)
  const now = new Date()
  return date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth()
}
function statusTone(value) {
  return ({ active: 'success', approved: 'success', published: 'success', enabled: 'success', settled: 'success', sent: 'success', pending: 'warning', draft: 'warning', hidden: 'muted', disabled: 'muted', rejected: 'danger' })[value] || 'muted'
}
function displayValue(column, value) {
  if (value === null || value === undefined || value === '') return '-'
  if (props.viewKey === 'reviews' && column.key === 'status') {
    return ({ published: '上架', hidden: '隐藏' })[value] || value
  }
  if (props.viewKey === 'roomManagement' && column.key === 'approval') {
    return ({ approved: '通过', pending: '审核中', rejected: '未通过' })[value] || value
  }
  if (column.key === 'status' || column.key === 'approval') {
    return ({ active: '正常', approved: '已通过', published: '已发布', enabled: '已启用', settled: '已结算', sent: '已发送', pending: '待审核', draft: '草稿', hidden: '隐藏', disabled: '已停用', rejected: '已拒绝' })[value] || value
  }
  return value
}
function asTags(value) { return Array.isArray(value) ? value : [value] }
function money(cents) { return `¥${((Number(cents) || 0) / 100).toFixed(2)}` }
function isPending(row) { return Object.values(row).includes('pending') }
function imageKey(row, column) { return `${row.id}:${column.key}` }
function isImageBroken(row, column) { return brokenImages.value.has(imageKey(row, column)) }
function markImageBroken(row, column) {
  const next = new Set(brokenImages.value)
  next.add(imageKey(row, column))
  brokenImages.value = next
}
function clearSelection() { selectedRows.value = [] }
function newForm(row = {}) {
  const result = {}
  config.value.fields.forEach((field) => { result[field.key] = row[field.key] ?? (field.type === 'number' ? 0 : field.type === 'switch' ? true : field.options?.[0]?.value || '') })
  return result
}
function openCreate() { Object.assign(form, newForm()); dialog.mode = 'create'; dialog.visible = true }
function openEdit(row) { if (!row) return; Object.assign(form, newForm(row), { id: row.id }); dialog.mode = 'edit'; dialog.visible = true }
async function save() {
  const required = config.value.fields.filter((field) => field.required)
  if (required.some((field) => !String(form[field.key] ?? '').trim())) return ElMessage.warning('请补齐必填项')
  dialog.saving = true
  try {
    if (dialog.mode === 'create') {
      rows.value.unshift({ ...newForm(form), id: Date.now(), createdAt: new Date().toLocaleString('zh-CN'), updatedAt: new Date().toLocaleString('zh-CN') })
    } else {
      const index = rows.value.findIndex((row) => row.id === form.id)
      if (index >= 0) rows.value[index] = { ...rows.value[index], ...form, updatedAt: new Date().toLocaleString('zh-CN') }
    }
    persist()
    dialog.visible = false
    ElMessage.success('保存成功')
  } finally {
    dialog.saving = false
  }
}
async function removeRows(items) {
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${items.length} 条记录吗？`, '删除确认', { type: 'warning' })
    const ids = new Set(items.map((row) => row.id))
    rows.value = rows.value.filter((row) => !ids.has(row.id))
    persist()
    clearSelection()
    ElMessage.success('已删除')
  } catch {}
}
function removeSelected() { removeRows(selectedRows.value) }
async function approve(row) {
  row.approval = 'approved'
  row.status = row.status === 'pending' ? 'active' : row.status
  persist()
  ElMessage.success('审核已通过')
}
async function approveSelected() {
  selectedRows.value.filter(isPending).forEach((row) => { row.approval = 'approved'; row.status = row.status === 'pending' ? 'active' : row.status })
  persist()
  ElMessage.success('审核操作已提交')
}
function exportRows() {
  const header = config.value.columns.map((column) => column.label).join(',')
  const body = filteredRows.value.map((row) => config.value.columns.map((column) => JSON.stringify(displayValue(column, row[column.key]))).join(',')).join('\n')
  const blob = new Blob([`\ufeff${header}\n${body}`], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${config.value.title}.csv`
  link.click()
  URL.revokeObjectURL(url)
  ElMessage.success('导出已开始')
}
</script>
