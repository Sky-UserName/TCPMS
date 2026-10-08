<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
        <span class="eyebrow">ROOMS · RATES · INVENTORY</span>
        <h2>房间管理</h2>
        <p>审核用户发布的房间，也可以由后台直接新增房型、设置价格和库存。</p>
      </div>
      <el-button v-if="activeTab === 'rooms'" type="primary" :icon="Plus" @click="openRoomCreate">新增房间</el-button>
    </section>

    <el-card class="panel-card tabs-card" shadow="never">
      <div class="page-tabs" role="tablist">
        <button v-for="item in tabs" :key="item.key" type="button" role="tab" :aria-selected="activeTab === item.key" :class="{ active: activeTab === item.key }" @click="activeTab = item.key">
          {{ item.label }}<span>{{ item.count }}</span>
        </button>
      </div>
    </el-card>

    <template v-if="activeTab === 'rooms'">
      <el-card class="panel-card filter-card" shadow="never">
        <div class="room-status-tabs" role="tablist" aria-label="房间审核状态">
          <button v-for="tab in roomStatusTabs" :key="tab.key" type="button" :class="{ active: roomStatusTab === tab.key }" @click="roomStatusTab = tab.key">
            {{ tab.label }}<span>{{ roomStatusCount(tab.key) }}</span>
          </button>
        </div>
        <div class="filter-row">
          <el-select v-model="roomFilters.storeId" clearable placeholder="全部门店" @change="loadRooms">
            <el-option label="全部门店" value="" />
            <el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" />
          </el-select>
          <el-select v-model="roomFilters.kind" clearable placeholder="房型类型">
            <el-option label="全部类型" value="" />
            <el-option label="独立房间" value="PrivateRoom" />
            <el-option label="多人间床位" value="Bed" />
          </el-select>
          <el-select v-model="roomFilters.published" clearable placeholder="展示状态">
            <el-option label="全部状态" value="" />
            <el-option label="已上架" :value="true" />
            <el-option label="已下架" :value="false" />
          </el-select>
          <el-input v-model="roomFilters.keyword" clearable placeholder="搜索房间名称、门店或规则" :prefix-icon="Search" />
          <el-button type="primary" plain @click="loadRooms">筛选</el-button>
        </div>
      </el-card>

      <el-card class="panel-card" shadow="never">
        <div class="table-toolbar room-toolbar">
          <div class="toolbar-left">
            <el-button :icon="Refresh" title="刷新房间" :loading="loading" @click="loadRooms" />
            <el-button type="success" :icon="Plus" @click="openRoomCreate">添加</el-button>
            <el-button type="primary" plain :disabled="selectedRows.length !== 1" @click="openRoomEdit(selectedRows[0])">编辑</el-button>
            <el-button type="danger" plain :disabled="!selectedRows.length" @click="removeSelected">删除</el-button>
            <el-button type="warning" plain :disabled="!selectedRows.length" @click="approveSelected">审核</el-button>
            <strong>房间列表</strong><span class="toolbar-count">{{ filteredRooms.length }} 个房间</span>
          </div>
          <el-button text :icon="Refresh" title="刷新房间" :loading="loading" @click="loadRooms">刷新</el-button>
        </div>
        <el-table v-loading="loading" :data="filteredRooms" class="clean-table admin-data-table room-table" row-key="id" @selection-change="selectedRows = $event">
          <el-table-column type="selection" width="48" fixed="left" />
          <el-table-column label="Id" width="88" fixed="left"><template #default="{ row }">{{ shortId(row.id) }}</template></el-table-column>
          <el-table-column label="长名称" min-width="190" fixed="left"><template #default="{ row }">{{ row.longName || row.name || '—' }}</template></el-table-column>
          <el-table-column label="房间名" min-width="160"><template #default="{ row }">{{ row.name || '—' }}</template></el-table-column>
          <el-table-column label="门店ID" min-width="160"><template #default="{ row }">{{ row.storeName || shortId(row.storeId) }}</template></el-table-column>
          <el-table-column label="房间照片" min-width="150">
            <template #default="{ row }">
              <div class="room-cell">
                <div class="room-thumbs" v-if="imageList(row).length">
                  <img v-for="image in imageList(row).slice(0, 3)" :key="image" :src="image" alt="" />
                </div>
                <span v-else class="room-photo empty"><el-icon><House /></el-icon></span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="入住时间" min-width="120"><template #default="{ row }">{{ row.checkInTime || '—' }}</template></el-table-column>
          <el-table-column label="退房时间" min-width="120"><template #default="{ row }">{{ row.checkOutTime || '—' }}</template></el-table-column>
          <el-table-column label="退订规则" min-width="170"><template #default="{ row }">{{ row.cancellationRule || '—' }}</template></el-table-column>
          <el-table-column label="加入规则" min-width="150"><template #default="{ row }">{{ row.joinRule || '—' }}</template></el-table-column>
          <el-table-column label="房屋介绍" min-width="220"><template #default="{ row }"><span class="truncate-cell" :title="row.description || ''">{{ row.description || '—' }}</span></template></el-table-column>
          <el-table-column label="状态" width="95"><template #default="{ row }"><el-tag :type="row.isPublished ? 'success' : 'info'" effect="light">{{ row.isPublished ? '正常' : '隐藏' }}</el-tag></template></el-table-column>
          <el-table-column label="总预定数" width="95"><template #default="{ row }">{{ row.totalBookings ?? 0 }}</template></el-table-column>
          <el-table-column label="审核状态" width="110"><template #default="{ row }"><span class="dot-status" :class="approvalTone(row.approvalStatus)"><i></i>{{ approvalLabel(row.approvalStatus) }}</span></template></el-table-column>
          <el-table-column label="是否草稿" width="90"><template #default="{ row }">{{ row.isDraft ? '是' : '否' }}</template></el-table-column>
          <el-table-column label="拒绝理由" min-width="170"><template #default="{ row }">{{ row.rejectionReason || '—' }}</template></el-table-column>
          <el-table-column label="审核时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.reviewedAt) }}</template></el-table-column>
          <el-table-column label="排序" width="72"><template #default="{ row }">{{ row.sortOrder ?? 0 }}</template></el-table-column>
          <el-table-column label="创建时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column>
          <el-table-column label="更新时间" min-width="160"><template #default="{ row }">{{ formatDateTime(row.updatedAt) }}</template></el-table-column>
          <el-table-column label="操作" width="305" fixed="right">
            <template #default="{ row }">
              <el-button v-if="['Draft', 'PendingReview', 'Rejected'].includes(row.approvalStatus)" link type="success" @click="reviewRoom(row, true)">通过</el-button>
              <el-button v-if="['Draft', 'PendingReview'].includes(row.approvalStatus)" link type="danger" @click="reviewRoom(row, false)">未通过</el-button>
              <el-button type="primary" size="small" :icon="Money" @click="openPriceSettings(row)">价格设置</el-button>
              <el-button link type="primary" @click="openRoomEdit(row)">编辑</el-button>
              <el-button link type="danger" @click="removeRows([row])">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </template>

    <template v-else>
      <el-card class="panel-card filter-card" shadow="never">
        <div class="filter-row">
          <el-select v-model="selectedRoomId" placeholder="选择房型" style="min-width: 260px" @change="loadSchedule">
            <el-option v-for="room in rooms" :key="room.id" :label="`${room.storeName} · ${room.name}`" :value="room.id" />
          </el-select>
          <template v-if="activeTab === 'prices'">
            <el-button :icon="ArrowLeft" @click="shiftMonth(-1)">上个月</el-button>
            <strong class="month-picker-label">{{ calendarMonthLabel }}</strong>
            <el-button :icon="ArrowRight" @click="shiftMonth(1)">下个月</el-button>
            <el-button :icon="Refresh" @click="loadSchedule">刷新日历</el-button>
          </template>
          <template v-else>
            <el-date-picker v-model="scheduleRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" @change="loadSchedule" />
            <el-button :icon="Refresh" @click="loadSchedule">刷新库存</el-button>
          </template>
        </div>
      </el-card>

      <template v-if="activeTab === 'prices'">
        <el-card class="panel-card calendar-card" shadow="never">
          <template #header>
            <div class="panel-header">
              <div><strong>价格设置</strong><span>按日维护单价、总库存和剩余库存</span></div>
              <el-button type="primary" plain size="small" @click="priceDialog = true">批量调价</el-button>
            </div>
          </template>
          <div v-loading="scheduleLoading" class="calendar-wrap">
            <div class="calendar-week"><span v-for="day in weekLabels" :key="day">{{ day }}</span></div>
            <div class="calendar-grid">
              <div v-for="day in calendarDays" :key="day.key" class="calendar-day" :class="{ outside: !day.currentMonth, weekend: day.weekend, today: day.today }">
                <div class="calendar-day-head"><b>{{ day.number }}</b><small v-if="day.holiday">{{ day.holiday }}</small></div>
                <div class="calendar-metrics">
                  <div><span>单价</span><strong>{{ day.priceCents !== null ? `${centsToYuan(day.priceCents)} 元` : '—' }}</strong></div>
                  <div><span>总数</span><strong>{{ day.totalQuantity ?? '—' }}</strong></div>
                  <div><span>剩余</span><strong :class="{ 'danger-text': day.availableQuantity !== null && day.availableQuantity <= 1, 'success-text': day.availableQuantity !== null && day.availableQuantity > 1 }">{{ day.availableQuantity ?? '—' }}</strong></div>
                </div>
              </div>
            </div>
          </div>
        </el-card>
      </template>

      <template v-else>
        <el-card class="panel-card" shadow="never">
          <template #header><div class="panel-header"><div><strong>房态库存</strong><span>剩余 = 总库存 - 锁定 - 已售</span></div><el-button type="primary" plain size="small" @click="inventoryDialog = true">调整库存</el-button></div></template>
          <el-table v-loading="scheduleLoading" :data="inventoryItems" class="clean-table admin-data-table" size="small">
            <el-table-column prop="date" label="日期" width="150"><template #default="{ row }">{{ dateOnly(row.date) }}</template></el-table-column>
            <el-table-column prop="totalQuantity" label="总量" width="100" />
            <el-table-column prop="lockedQuantity" label="锁定" width="100" />
            <el-table-column prop="soldQuantity" label="已售" width="100" />
            <el-table-column label="剩余" width="100"><template #default="{ row }"><strong :class="row.availableQuantity <= 1 ? 'danger-text' : 'success-text'">{{ row.availableQuantity }}</strong></template></el-table-column>
            <el-table-column prop="updatedAt" label="更新时间" min-width="180" />
          </el-table>
        </el-card>
      </template>
    </template>

    <el-dialog v-model="roomDialog" :title="roomForm.id ? '编辑房间' : '新增房间'" width="820px" destroy-on-close>
      <el-form :model="roomForm" label-width="96px" class="dialog-form room-form">
        <div class="form-section-title">基础信息</div>
        <div class="form-grid">
          <el-form-item label="所属门店" required><el-select v-model="roomForm.storeId" :disabled="Boolean(roomForm.id)"><el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" /></el-select></el-form-item>
          <el-form-item label="长名称"><el-input v-model="roomForm.longName" placeholder="用于列表展示的完整标题" /></el-form-item>
          <el-form-item label="房间名称" required><el-input v-model="roomForm.name" placeholder="例如：西湖景观大床房" /></el-form-item>
          <el-form-item label="房间类型"><el-select v-model="roomForm.kind"><el-option label="独立房间" value="PrivateRoom" /><el-option label="多人间床位" value="Bed" /></el-select></el-form-item>
          <el-form-item label="房型分类"><el-input v-model="roomForm.roomCategory" placeholder="例如：小区、别墅、客栈" /></el-form-item>
          <el-form-item label="性别限制"><el-select v-model="roomForm.gender" clearable><el-option label="不限性别" value="Any" /><el-option label="女生专区" value="Female" /><el-option label="男生专区" value="Male" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">容量与价格</div>
        <div class="form-grid form-grid-4">
          <el-form-item label="面积"><el-input-number v-model="roomForm.area" :min="0" :precision="1" controls-position="right"><template #suffix>㎡</template></el-input-number></el-form-item>
          <el-form-item label="朝向"><el-input v-model="roomForm.orientation" placeholder="例如：南" /></el-form-item>
          <el-form-item label="房间数量"><el-input-number v-model="roomForm.roomCount" :min="1" controls-position="right" /></el-form-item>
          <el-form-item label="床位数"><el-input-number v-model="roomForm.bedCount" :min="1" controls-position="right" /></el-form-item>
          <el-form-item label="容纳人数"><div class="capacity-fields"><el-input-number v-model="roomForm.maxGuests" :min="1" controls-position="right" /><span>成人</span><el-input-number v-model="roomForm.childCapacity" :min="0" controls-position="right" /><span>儿童</span></div></el-form-item>
          <el-form-item label="户型"><el-input v-model="roomForm.layout" placeholder="例如：一室一厅" /></el-form-item>
          <el-form-item label="默认价格"><el-input-number v-model="roomForm.basePriceYuan" :min="0" :precision="2" :step="1" controls-position="right"><template #suffix>元</template></el-input-number></el-form-item>
          <el-form-item label="默认库存"><el-input-number v-model="roomForm.defaultInventory" :min="0" controls-position="right" /></el-form-item>
        </div>

        <div class="form-section-title">民宿标签与设施</div>
        <div class="form-grid">
          <el-form-item label="民宿标签"><el-select v-model="roomForm.tags" multiple filterable allow-create default-first-option placeholder="选择或输入标签"><el-option v-for="item in tagOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="民宿服务"><el-select v-model="roomForm.services" multiple filterable allow-create default-first-option placeholder="选择服务"><el-option v-for="item in serviceOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="基础设施"><el-select v-model="roomForm.facilities" multiple filterable allow-create default-first-option placeholder="选择设施"><el-option v-for="item in facilityOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="卫浴设施"><el-select v-model="roomForm.bathroomFacilities" multiple filterable allow-create default-first-option placeholder="选择卫浴设施"><el-option v-for="item in bathroomOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="周边设施"><el-select v-model="roomForm.nearbyFacilities" multiple filterable allow-create default-first-option placeholder="选择周边设施"><el-option v-for="item in nearbyOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">入住规则</div>
        <div class="form-grid">
          <el-form-item label="入住时间"><el-input v-model="roomForm.checkInTime" placeholder="例如：当天 14:00 后" /></el-form-item>
          <el-form-item label="退房时间"><el-input v-model="roomForm.checkOutTime" placeholder="例如：次日 12:00 前" /></el-form-item>
          <el-form-item label="退订规则"><el-input v-model="roomForm.cancellationRule" placeholder="例如：提前 24 小时可免费取消" /></el-form-item>
          <el-form-item label="加入规则"><el-input v-model="roomForm.joinRule" placeholder="例如：入住须知或费用说明" /></el-form-item>
        </div>
        <el-form-item label="房屋介绍"><el-input v-model="roomForm.description" type="textarea" :rows="3" placeholder="填写房间介绍、服务说明和注意事项" /></el-form-item>
        <el-form-item label="房间图片">
          <div class="room-image-editor">
            <div class="asset-field"><el-input v-model="roomForm.imageUrlsText" type="textarea" :rows="2" placeholder="每行一个图片地址，支持粘贴多个地址" /><el-upload :show-file-list="false" :auto-upload="false" accept="image/*" :on-change="handleRoomImageChange"><el-button type="primary" plain :icon="Upload">选择</el-button></el-upload></div>
            <div v-if="roomImageList.length" class="room-image-grid">
              <div v-for="(image, index) in roomImageList" :key="`${image}-${index}`" class="room-image-item">
                <img :src="image" alt="" />
                <button type="button" title="删除图片" @click="removeRoomImage(index)"><el-icon><Delete /></el-icon></button>
              </div>
            </div>
          </div>
        </el-form-item>

        <div class="form-section-title">审核与展示</div>
        <div class="form-grid">
          <el-form-item label="审核状态"><el-select v-model="roomForm.approvalStatus"><el-option label="审核中" value="PendingReview" /><el-option label="已通过" value="Approved" /><el-option label="已拒绝" value="Rejected" /><el-option label="草稿" value="Draft" /><el-option label="已下架" value="Offline" /></el-select></el-form-item>
          <el-form-item label="拒绝理由"><el-input v-model="roomForm.rejectionReason" placeholder="审核拒绝时填写" /></el-form-item>
          <el-form-item label="上架展示"><el-switch v-model="roomForm.isPublished" active-text="展示给用户" /></el-form-item>
          <el-form-item label="排序"><el-input-number v-model="roomForm.sortOrder" :min="0" controls-position="right" /></el-form-item>
        </div>
      </el-form>
      <template #footer><el-button @click="roomDialog = false">取消</el-button><el-button type="primary" :loading="roomSaving" @click="saveRoom">保存房间</el-button></template>
    </el-dialog>

    <el-dialog v-model="priceDialog" title="批量设置价格" width="480px">
      <el-form :model="priceForm" label-width="88px" class="dialog-form">
        <el-form-item label="日期范围"><el-date-picker v-model="priceForm.range" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" /></el-form-item>
        <el-form-item label="每日价格"><el-input-number v-model="priceForm.priceYuan" :min="0" :precision="2" :step="1" controls-position="right"><template #suffix>元</template></el-input-number></el-form-item>
        <el-form-item label="价格来源"><el-input v-model="priceForm.source" placeholder="例如：节假日、手工调价" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="priceDialog = false">取消</el-button><el-button type="primary" :loading="scheduleSaving" @click="savePrices">保存价格</el-button></template>
    </el-dialog>

    <el-dialog v-model="inventoryDialog" title="批量调整库存" width="480px">
      <el-form :model="inventoryForm" label-width="88px" class="dialog-form">
        <el-form-item label="日期范围"><el-date-picker v-model="inventoryForm.range" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" /></el-form-item>
        <el-form-item label="调整数量"><el-input-number v-model="inventoryForm.delta" :step="1" controls-position="right" /></el-form-item>
        <el-form-item label="调整原因"><el-input v-model="inventoryForm.reason" type="textarea" :rows="3" placeholder="例如：新增两间房源、维修关闭一间" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="inventoryDialog = false">取消</el-button><el-button type="primary" :loading="scheduleSaving" @click="saveInventory">提交调整</el-button></template>
    </el-dialog>

    <el-drawer v-model="unitsVisible" :title="`${unitRoom?.name || '房型'} · 房间/床位`" size="520px">
      <div class="drawer-toolbar"><span>具体资源用于入住时分配</span><el-button type="primary" size="small" :icon="Plus" @click="addUnit">新增资源</el-button></div>
      <div v-if="unitForm.visible" class="inline-form"><el-input v-model="unitForm.code" placeholder="房间号或床位号" /><el-select v-model="unitForm.status"><el-option label="可售" value="Available" /><el-option label="维修" value="Maintenance" /><el-option label="停用" value="Disabled" /></el-select><el-button type="primary" @click="saveUnit">保存</el-button></div>
      <div class="unit-list"><div v-for="unit in units" :key="unit.id" class="unit-row"><span class="unit-code">{{ unit.code }}</span><el-tag :type="unit.status === 'Available' ? 'success' : unit.status === 'Occupied' ? 'warning' : 'info'" effect="light">{{ unitStatus(unit.status) }}</el-tag><el-button link type="primary" @click="editUnit(unit)">编辑</el-button></div></div>
      <el-empty v-if="!units.length" description="暂无具体资源" />
    </el-drawer>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, ArrowRight, Delete, House, Money, Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'

const activeTab = ref('rooms')
const tabs = computed(() => [
  { key: 'rooms', label: '房间审核', count: rooms.value.length },
  { key: 'prices', label: '价格设置', count: selectedRoomId.value ? priceItems.value.length : 0 },
  { key: 'inventory', label: '房态库存', count: selectedRoomId.value ? inventoryItems.value.length : 0 }
])
const roomStatusTabs = [
  { key: 'all', label: '全部' },
  { key: 'pending', label: '审核中' },
  { key: 'approved', label: '已通过' },
  { key: 'rejected', label: '已拒绝' },
  { key: 'hidden', label: '已隐藏' }
]
const weekLabels = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六']
const tagOptions = ['超赞房东', '交通方便', '近景区', '亲子友好']
const serviceOptions = ['免费停车', '国内长途', '行李寄存', '早餐服务']
const facilityOptions = ['无线网络', '窗户', '地暖', '网络电视', '零食、冰箱', '空调']
const bathroomOptions = ['独立卫浴', '吹风机', '热水淋浴', '浴缸']
const nearbyOptions = ['餐厅', '商场', '公交站', '景区入口', '便利店']

const stores = ref([])
const rooms = ref([])
const loading = ref(false)
const scheduleLoading = ref(false)
const scheduleSaving = ref(false)
const roomSaving = ref(false)
const roomDialog = ref(false)
const priceDialog = ref(false)
const inventoryDialog = ref(false)
const unitsVisible = ref(false)
const selectedRoomId = ref('')
const selectedRows = ref([])
const unitRoom = ref(null)
const units = ref([])
const roomStatusTab = ref('all')
const calendarDate = ref(new Date())
const roomFilters = reactive({ storeId: '', kind: '', published: '', keyword: '' })
const scheduleRange = ref([dateOnly(new Date()), dateOnly(addDays(new Date(), 7))])
const priceItems = ref([])
const inventoryItems = ref([])
const roomForm = reactive(emptyRoom())
const priceForm = reactive({ range: [dateOnly(new Date()), dateOnly(addDays(new Date(), 7))], priceYuan: 268, source: 'Manual' })
const inventoryForm = reactive({ range: [dateOnly(new Date()), dateOnly(addDays(new Date(), 7))], delta: 1, reason: '' })
const unitForm = reactive({ visible: false, id: '', code: '', status: 'Available' })

const filteredRooms = computed(() => rooms.value.filter((row) => {
  const text = roomFilters.keyword.trim().toLowerCase()
  const matchesStatus = roomStatusTab.value === 'all' ||
    (roomStatusTab.value === 'pending' && ['Draft', 'PendingReview'].includes(row.approvalStatus)) ||
    (roomStatusTab.value === 'approved' && row.approvalStatus === 'Approved') ||
    (roomStatusTab.value === 'rejected' && row.approvalStatus === 'Rejected') ||
    (roomStatusTab.value === 'hidden' && !row.isPublished)
  return matchesStatus &&
    (!roomFilters.storeId || row.storeId === roomFilters.storeId) &&
    (!roomFilters.kind || row.kind === roomFilters.kind) &&
    (roomFilters.published === '' || row.isPublished === roomFilters.published) &&
    (!text || `${row.name} ${row.longName || ''} ${row.storeName || ''} ${row.cancellationRule || ''}`.toLowerCase().includes(text))
}))

const priceByDate = computed(() => new Map(priceItems.value.map((item) => [dateOnly(item.date), item])))
const inventoryByDate = computed(() => new Map(inventoryItems.value.map((item) => [dateOnly(item.date), item])))
const selectedRoom = computed(() => rooms.value.find((room) => room.id === selectedRoomId.value))
const calendarMonthLabel = computed(() => new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: 'long' }).format(calendarDate.value))
const calendarDays = computed(() => {
  const monthStart = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth(), 1)
  const monthEnd = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth() + 1, 0)
  const total = Math.ceil((monthStart.getDay() + monthEnd.getDate()) / 7) * 7
  return Array.from({ length: total }, (_, index) => {
    const date = new Date(monthStart)
    date.setDate(index - monthStart.getDay() + 1)
    const key = dateOnly(date)
    const price = priceByDate.value.get(key)
    const inventory = inventoryByDate.value.get(key)
    const currentMonth = date.getFullYear() === monthStart.getFullYear() && date.getMonth() === monthStart.getMonth()
    return {
      key,
      number: date.getDate(),
      currentMonth,
      weekend: date.getDay() === 0 || date.getDay() === 6,
      today: key === dateOnly(new Date()),
      holiday: date.getDay() === 0 ? '休息日' : date.getDay() === 6 ? '周末' : '',
      priceCents: currentMonth ? (price?.priceCents ?? (selectedRoom.value?.basePriceCents || 0)) : null,
      totalQuantity: currentMonth ? (inventory?.totalQuantity ?? (selectedRoom.value?.defaultInventory || null)) : null,
      availableQuantity: currentMonth ? (inventory ? inventory.availableQuantity : (selectedRoom.value?.defaultInventory || null)) : null
    }
  })
})

function dateOnly(value) {
  const date = new Date(value)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
function addDays(value, count) { const date = new Date(value); date.setDate(date.getDate() + count); return date }
function addDateString(value, count) { return dateOnly(addDays(new Date(`${value}T00:00:00`), count)) }
function monthRange(value) {
  const start = new Date(value.getFullYear(), value.getMonth(), 1)
  const end = new Date(value.getFullYear(), value.getMonth() + 1, 1)
  return [dateOnly(start), dateOnly(end)]
}
function emptyRoom() {
  return {
    id: '', storeId: '', name: '', longName: '', kind: 'PrivateRoom', roomCategory: '', bedCount: 1, gender: 'Any',
    maxGuests: 2, childCapacity: 0, area: 15, orientation: '', roomCount: 1, layout: '', basePriceCents: 26800, basePriceYuan: 268, defaultInventory: 1,
    tags: [], services: [], facilities: [], bathroomFacilities: [], nearbyFacilities: [], checkInTime: '当天 14:00 后',
    checkOutTime: '次日 12:00 前', cancellationRule: '', joinRule: '', description: '', imageUrlsText: '',
    imageUrlsJson: '', facilitiesJson: '', tagsJson: '', servicesJson: '', bathroomFacilitiesJson: '', nearbyFacilitiesJson: '',
    isPublished: false, approvalStatus: 'PendingReview', isDraft: true, rejectionReason: '', sortOrder: 0
  }
}
function parseArray(value) {
  if (Array.isArray(value)) return value
  try {
    const parsed = JSON.parse(value || '[]')
    return Array.isArray(parsed) ? parsed : []
  } catch {
    const text = String(value || '').trim()
    if (!text) return []
    if (text.includes('\n')) return text.split(/\r?\n/).map((item) => item.trim()).filter(Boolean)
    return text.startsWith('data:') ? [text] : text.split(',').map((item) => item.trim()).filter(Boolean)
  }
}
function jsonArray(value) { return JSON.stringify((value || []).filter(Boolean)) }
function yuanToCents(value) { return Math.max(0, Math.round((Number(value) || 0) * 100)) }
function centsToYuan(value) { return Number(((Number(value) || 0) / 100).toFixed(2)) }
function shortId(value) { return value ? String(value).slice(0, 8) : '—' }
function formatDateTime(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
function kindLabel(value) { return value === 'Bed' ? '多人间床位' : '独立房间' }
function approvalLabel(value) { return ({ Approved: '已通过', PendingReview: '审核中', Draft: '草稿', Rejected: '已拒绝', Offline: '已下架' })[value] || value || '审核中' }
function approvalTone(value) { return ({ Approved: 'success', PendingReview: 'warning', Draft: 'warning', Rejected: 'danger', Offline: 'muted' })[value] || 'muted' }
function roomStatusCount(key) {
  return rooms.value.filter((row) => key === 'all' ||
    (key === 'pending' && ['Draft', 'PendingReview'].includes(row.approvalStatus)) ||
    (key === 'approved' && row.approvalStatus === 'Approved') ||
    (key === 'rejected' && row.approvalStatus === 'Rejected') ||
    (key === 'hidden' && !row.isPublished)).length
}
function unitStatus(value) { return ({ Available: '可售', Occupied: '已占用', Maintenance: '维修', Disabled: '停用' })[value] || value }
function imageList(row) { return parseArray(row.imageUrlsJson) }
function firstImage(row) { return imageList(row)[0] || '' }
function imageCount(row) { return imageList(row).length }
const roomImageList = computed(() => parseArray(roomForm.imageUrlsText))
function handleRoomImageChange(uploadFile) {
  const raw = uploadFile?.raw
  if (!raw) return
  const reader = new FileReader()
  reader.onload = () => {
    roomForm.imageUrlsText = [...roomImageList.value, String(reader.result)].join('\n')
  }
  reader.readAsDataURL(raw)
}
function removeRoomImage(index) {
  roomForm.imageUrlsText = roomImageList.value.filter((_, itemIndex) => itemIndex !== index).join('\n')
}
async function loadStores() { stores.value = await get('/admin/stores') }
async function loadRooms() {
  loading.value = true
  try {
    rooms.value = await get('/admin/room-types?includeUnpublished=true')
    if (!selectedRoomId.value || !rooms.value.some((room) => room.id === selectedRoomId.value)) selectedRoomId.value = rooms.value[0]?.id || ''
    if (selectedRoom.value) priceForm.priceYuan = centsToYuan(selectedRoom.value.basePriceCents)
  } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
}
async function loadSchedule() {
  if (!selectedRoomId.value) return
  const [from, to] = activeTab.value === 'prices' ? monthRange(calendarDate.value) : (scheduleRange.value || [])
  if (!from || !to) return
  scheduleLoading.value = true
  try {
    const query = `roomTypeId=${selectedRoomId.value}&from=${from}&to=${to}`
    const [prices, inventory] = await Promise.all([get(`/admin/prices?${query}`), get(`/admin/inventory?${query}`)])
    priceItems.value = prices
    inventoryItems.value = inventory
  } catch (error) { ElMessage.error(error.message) } finally { scheduleLoading.value = false }
}
function shiftMonth(delta) {
  calendarDate.value = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth() + delta, 1)
  loadSchedule()
}
function openRoomCreate() { Object.assign(roomForm, emptyRoom(), { storeId: stores.value[0]?.id || '' }); roomDialog.value = true }
function openRoomEdit(row) {
  const base = emptyRoom()
  Object.assign(base, row, {
    tags: parseArray(row.tagsJson),
    services: parseArray(row.servicesJson),
    facilities: parseArray(row.facilitiesJson),
    bathroomFacilities: parseArray(row.bathroomFacilitiesJson),
    nearbyFacilities: parseArray(row.nearbyFacilitiesJson),
    imageUrlsText: parseArray(row.imageUrlsJson).join('\n'),
    basePriceYuan: centsToYuan(row.basePriceCents),
    childCapacity: row.childCapacity || 0
  })
  Object.assign(roomForm, base)
  roomDialog.value = true
}
async function saveRoom() {
  if (!roomForm.storeId || !roomForm.name) return ElMessage.warning('请填写所属门店和房间名称')
  roomSaving.value = true
  try {
    const payload = {
      ...roomForm,
      basePriceCents: yuanToCents(roomForm.basePriceYuan),
      childCapacity: Number(roomForm.childCapacity) || 0,
      facilitiesJson: jsonArray(roomForm.facilities),
      tagsJson: jsonArray(roomForm.tags),
      servicesJson: jsonArray(roomForm.services),
      bathroomFacilitiesJson: jsonArray(roomForm.bathroomFacilities),
      nearbyFacilitiesJson: jsonArray(roomForm.nearbyFacilities),
      imageUrlsJson: jsonArray(parseArray(roomForm.imageUrlsText))
    }
    delete payload.tags
    delete payload.services
    delete payload.facilities
    delete payload.bathroomFacilities
    delete payload.nearbyFacilities
    delete payload.imageUrlsText
    delete payload.basePriceYuan
    if (roomForm.id) await put(`/admin/room-types/${roomForm.id}`, payload)
    else await post('/admin/room-types', payload)
    ElMessage.success('房间已保存'); roomDialog.value = false; await loadRooms()
  } catch (error) { ElMessage.error(error.message) } finally { roomSaving.value = false }
}
async function reviewRoom(row, approved) {
  let rejectionReason = ''
  if (!approved) {
    try {
      const result = await ElMessageBox.prompt('请填写拒绝原因，方便发布者修改房间资料。', '拒绝房间', { inputPlaceholder: '例如：房间图片不清晰或地址资料不完整', inputValidator: (value) => Boolean(value?.trim()) || '拒绝原因不能为空' })
      rejectionReason = result.value
    } catch { return }
  } else {
    try { await ElMessageBox.confirm(`确定通过“${row.name}”的审核并允许上架吗？`, '审核通过', { type: 'success' }) } catch { return }
  }
  try {
    await post(`/admin/room-types/${row.id}/review`, { approved, rejectionReason })
    ElMessage.success(approved ? '房间审核已通过' : '房间已拒绝')
    await loadRooms()
  } catch (error) { ElMessage.error(error.message) }
}
async function togglePublish(row) {
  try { await post(`/admin/room-types/${row.id}/publish?published=${!row.isPublished}`); ElMessage.success(row.isPublished ? '房间已隐藏' : '房间已上架'); await loadRooms() } catch (error) { ElMessage.error(error.message) }
}
function openPriceSettings(row) {
  selectedRoomId.value = row.id
  activeTab.value = 'prices'
  calendarDate.value = new Date()
  priceForm.range = monthRange(calendarDate.value)
  priceForm.priceYuan = centsToYuan(row.basePriceCents)
}
async function approveSelected() {
  const rows = selectedRows.value.filter((row) => ['Draft', 'PendingReview', 'Rejected'].includes(row.approvalStatus))
  if (!rows.length) return ElMessage.warning('请选择待审核或已拒绝的房间')
  try {
    await ElMessageBox.confirm(`确定通过选中的 ${rows.length} 个房间并允许上架吗？`, '批量审核通过', { type: 'success' })
    for (const row of rows) await post(`/admin/room-types/${row.id}/review`, { approved: true, rejectionReason: null })
    ElMessage.success(`已通过 ${rows.length} 个房间`)
    selectedRows.value = []
    await loadRooms()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error(error.message)
  }
}
async function removeRows(rows) {
  if (!rows.length) return
  try {
    await ElMessageBox.confirm(`删除后将无法恢复，确定删除选中的 ${rows.length} 个房间吗？`, '删除房间', { type: 'warning' })
    for (const row of rows) await del(`/admin/room-types/${row.id}`)
    ElMessage.success(`已删除 ${rows.length} 个房间`)
    selectedRows.value = []
    await loadRooms()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error(error.message)
  }
}
function removeSelected() { return removeRows(selectedRows.value) }
async function savePrices() {
  const [from, to] = priceForm.range || []
  if (!selectedRoomId.value || !from || !to || priceForm.priceYuan === null || priceForm.priceYuan === undefined) return ElMessage.warning('请填写完整的价格范围和金额')
  scheduleSaving.value = true
  try { await put('/admin/prices/bulk', { roomTypeId: selectedRoomId.value, from, to: addDateString(to, 1), priceCents: yuanToCents(priceForm.priceYuan), source: priceForm.source }); ElMessage.success('价格日历已更新'); priceDialog.value = false; await loadSchedule() } catch (error) { ElMessage.error(error.message) } finally { scheduleSaving.value = false }
}
async function saveInventory() {
  const [from, to] = inventoryForm.range || []
  if (!selectedRoomId.value || !from || !to || !inventoryForm.delta || !inventoryForm.reason) return ElMessage.warning('请填写调整数量和原因')
  scheduleSaving.value = true
  try { await post('/admin/inventory/adjust', { roomTypeId: selectedRoomId.value, from, to: addDateString(to, 1), delta: inventoryForm.delta, reason: inventoryForm.reason }); ElMessage.success('库存已调整'); inventoryDialog.value = false; await loadSchedule() } catch (error) { ElMessage.error(error.message) } finally { scheduleSaving.value = false }
}
async function openUnits(row) { unitRoom.value = row; unitsVisible.value = true; unitForm.visible = false; try { units.value = await get(`/admin/room-types/${row.id}/units`) } catch (error) { ElMessage.error(error.message) } }
function addUnit() { Object.assign(unitForm, { visible: true, id: '', code: '', status: 'Available' }) }
function editUnit(unit) { Object.assign(unitForm, { visible: true, id: unit.id, code: unit.code, status: unit.status }) }
async function saveUnit() {
  if (!unitForm.code) return ElMessage.warning('请输入资源编码')
  try {
    const payload = { roomTypeId: unitRoom.value.id, code: unitForm.code, kind: unitRoom.value.kind === 'Bed' ? 'Bed' : 'Room', gender: unitRoom.value.gender, status: unitForm.status, note: '' }
    if (unitForm.id) await put(`/admin/room-units/${unitForm.id}`, payload); else await post(`/admin/room-types/${unitRoom.value.id}/units`, payload)
    ElMessage.success('资源已保存'); unitForm.visible = false; units.value = await get(`/admin/room-types/${unitRoom.value.id}/units`)
  } catch (error) { ElMessage.error(error.message) }
}
watch(activeTab, (value) => { if (value !== 'rooms') loadSchedule() })
watch(selectedRoomId, () => { if (activeTab.value !== 'rooms') loadSchedule() })
onMounted(async () => { try { await Promise.all([loadStores(), loadRooms()]) } catch (error) { ElMessage.error(error.message) } })
</script>

<style scoped>
.room-toolbar{align-items:center;flex-wrap:wrap;margin-bottom:16px}
.toolbar-left{display:flex;align-items:center;gap:8px;min-width:0;flex-wrap:wrap}
.room-table{min-width:100%}
.room-table :deep(.el-table__header-wrapper),.room-table :deep(.el-table__body-wrapper){overflow-x:auto}
.room-table :deep(.el-table__cell){white-space:nowrap}
.room-table :deep(.el-button+.el-button){margin-left:5px}
.room-thumbs{display:flex;gap:4px;align-items:center}
.room-thumbs img{width:42px;height:36px;object-fit:cover;border-radius:5px;border:1px solid var(--admin-border)}
.truncate-cell{display:block;max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.dot-status{display:inline-flex;align-items:center;gap:5px;white-space:nowrap}
.dot-status i{width:7px;height:7px;border-radius:50%;background:var(--admin-muted)}
.dot-status.success{color:#14a887}.dot-status.success i{background:#14b88f}
.dot-status.warning{color:#ba7915}.dot-status.warning i{background:#e9a11c}
.dot-status.danger{color:#c85a63}.dot-status.danger i{background:#d96872}
.dot-status.muted{color:var(--admin-muted)}
.capacity-fields{display:flex;align-items:center;gap:5px;min-width:0}
.capacity-fields :deep(.el-input-number){min-width:88px}
.capacity-fields span{font-size:12px;color:var(--admin-muted);white-space:nowrap}
.asset-field{display:flex;align-items:flex-start;gap:10px;width:100%}
.asset-field :deep(.el-textarea){flex:1}
.room-image-editor{width:100%}
.room-image-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin-top:10px}
.room-image-item{position:relative;min-height:92px;overflow:hidden;border:1px solid var(--admin-border);border-radius:7px;background:var(--admin-surface-soft)}
.room-image-item img{display:block;width:100%;height:92px;object-fit:cover}
.room-image-item button{position:absolute;top:5px;right:5px;display:grid;width:25px;height:25px;place-items:center;border:0;border-radius:50%;background:rgba(201,91,98,.92);color:#fff;cursor:pointer}
.room-image-item button:hover{background:#b74953}
.room-status-tabs{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:16px}
.room-status-tabs button{border:1px solid var(--admin-border);background:var(--admin-surface-soft);color:var(--admin-muted);border-radius:7px;padding:8px 14px;cursor:pointer;font:inherit}
.room-status-tabs button.active{background:var(--admin-primary-soft);border-color:var(--admin-primary);color:var(--admin-primary);font-weight:700}
.room-status-tabs span{margin-left:6px;font-size:12px;opacity:.7}
.room-photo{width:46px;height:40px;border-radius:7px;overflow:hidden;background:var(--admin-surface-soft);display:inline-flex;align-items:center;justify-content:center;color:var(--admin-muted);flex:0 0 auto}
.room-photo img{width:100%;height:100%;object-fit:cover}
.room-photo.empty{border:1px dashed var(--admin-border)}
.table-subline{display:block;color:var(--admin-muted);font-size:12px;margin-top:4px}
.month-picker-label{min-width:128px;text-align:center;color:var(--admin-text)}
.calendar-wrap{overflow:auto}
.calendar-week,.calendar-grid{display:grid;grid-template-columns:repeat(7,minmax(130px,1fr));min-width:910px}
.calendar-week span{padding:10px 12px;text-align:center;color:var(--admin-muted);font-size:12px;border-bottom:1px solid var(--admin-border)}
.calendar-day{min-height:126px;padding:10px;border:1px solid var(--admin-border);border-top:0;border-left:0;background:var(--admin-surface);transition:background .18s ease}
.calendar-day:nth-child(7n+1){border-left:1px solid var(--admin-border)}
.calendar-day:hover{background:var(--admin-primary-soft)}
.calendar-day.outside{opacity:.45;background:var(--admin-surface-soft)}
.calendar-day.weekend{background:#f5fbfa}
.calendar-day.today{box-shadow:inset 0 0 0 2px var(--primary)}
.calendar-day-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}
.calendar-day-head b{font-size:16px}
.calendar-day-head small{font-size:11px;color:var(--admin-muted)}
.calendar-metrics{display:grid;grid-template-columns:repeat(3,1fr);gap:5px}
.calendar-metrics div{display:flex;flex-direction:column;gap:5px;text-align:center}
.calendar-metrics span{font-size:11px;color:var(--admin-muted)}
.calendar-metrics strong{font-size:13px;color:var(--admin-text)}
.calendar-metrics .success-text,.calendar-metrics .danger-text{font-weight:700}
.form-section-title{font-size:14px;font-weight:700;color:var(--admin-text);padding:12px 0 8px;border-top:1px solid var(--admin-border);margin-top:4px}
.form-section-title:first-child{border-top:0;padding-top:0}
.room-form :deep(.el-form-item){margin-bottom:14px}
.room-form :deep(.el-select),.room-form :deep(.el-input-number){width:100%}
.form-grid-4{grid-template-columns:repeat(4,minmax(0,1fr))}
@media (max-width: 900px){.form-grid-4{grid-template-columns:repeat(2,minmax(0,1fr))}.calendar-day{min-height:112px}}
@media (max-width: 640px){.room-image-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.asset-field{align-items:stretch;flex-direction:column}.toolbar-left{width:100%}}
</style>
