<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
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
        <el-table v-loading="loading" :data="pagedRooms" class="clean-table admin-data-table room-table" row-key="id" @selection-change="selectedRows = $event">
          <el-table-column type="selection" width="48" fixed="left" />
          <el-table-column label="长名称" min-width="190" fixed="left"><template #default="{ row }">{{ row.longName || row.name || '—' }}</template></el-table-column>
          <el-table-column label="房间名" min-width="160"><template #default="{ row }">{{ row.name || '—' }}</template></el-table-column>
          <el-table-column label="门店" min-width="160"><template #default="{ row }">{{ row.storeName || '—' }}</template></el-table-column>
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
        <div class="list-footer">
          <span>显示第 {{ filteredRooms.length ? (roomPage - 1) * roomPageSize + 1 : 0 }} 到 {{ Math.min(roomPage * roomPageSize, filteredRooms.length) }} 个房间，共 {{ filteredRooms.length }} 个</span>
          <el-pagination v-model:current-page="roomPage" v-model:page-size="roomPageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="filteredRooms.length" background />
        </div>
      </el-card>
    </template>

    <template v-else>
      <el-card class="panel-card filter-card" shadow="never">
        <div class="filter-row">
          <el-select v-model="selectedRoomId" placeholder="选择房型" style="min-width: 260px" @change="loadSchedule">
            <el-option v-for="room in rooms" :key="room.id" :label="`${room.storeName} · ${room.name}`" :value="room.id" />
          </el-select>
          <el-date-picker v-model="scheduleRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" @change="loadSchedule" />
          <el-button :icon="Refresh" @click="loadSchedule">刷新库存</el-button>
        </div>
      </el-card>

      <el-card class="panel-card" shadow="never">
        <template #header><div class="panel-header"><div><strong>房态库存</strong><span>剩余 = 总库存 - 锁定 - 已售</span></div><el-button type="primary" plain size="small" @click="inventoryDialog = true">调整库存</el-button></div></template>
        <el-table v-loading="scheduleLoading" :data="pagedInventoryItems" class="clean-table admin-data-table" size="small">
          <el-table-column prop="date" label="日期" width="150"><template #default="{ row }">{{ dateOnly(row.date) }}</template></el-table-column>
          <el-table-column prop="totalQuantity" label="总量" width="100" />
          <el-table-column prop="lockedQuantity" label="锁定" width="100" />
          <el-table-column prop="soldQuantity" label="已售" width="100" />
          <el-table-column label="剩余" width="100"><template #default="{ row }"><strong :class="row.availableQuantity <= 1 ? 'danger-text' : 'success-text'">{{ row.availableQuantity }}</strong></template></el-table-column>
          <el-table-column prop="updatedAt" label="更新时间" min-width="180" />
        </el-table>
        <div class="list-footer">
          <span>显示第 {{ inventoryItems.length ? (inventoryPage - 1) * inventoryPageSize + 1 : 0 }} 到 {{ Math.min(inventoryPage * inventoryPageSize, inventoryItems.length) }} 条库存记录，共 {{ inventoryItems.length }} 条</span>
          <el-pagination v-model:current-page="inventoryPage" v-model:page-size="inventoryPageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="inventoryItems.length" background />
        </div>
      </el-card>
    </template>

    <el-dialog v-model="roomDialog" :title="roomForm.id ? '编辑房间' : '新增房间'" width="820px" destroy-on-close>
      <p v-if="roomForm.id" class="dialog-data-tip">下面显示的就是这条房型的全部数据；灰色文字是填写示例，空白说明该项还没有填写过。</p>
      <el-form :model="roomForm" label-width="96px" class="dialog-form room-form">
        <div class="form-section-title">基础信息</div>
        <div class="form-grid">
          <el-form-item label="所属门店" required><el-select v-model="roomForm.storeId" :disabled="Boolean(roomForm.id)"><el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" /></el-select></el-form-item>
          <el-form-item label="长名称"><el-input v-model="roomForm.longName" placeholder="用于列表展示的完整标题" /></el-form-item>
          <el-form-item label="房间名称" required><el-input v-model="roomForm.name" placeholder="例如：西湖景观大床房" /></el-form-item>
          <el-form-item label="房间类型"><el-select v-model="roomForm.kind" @change="handleRoomKindChange"><el-option label="独立房间" value="PrivateRoom" /><el-option label="多人间床位" value="Bed" /></el-select></el-form-item>
          <el-form-item label="房型分类"><el-select v-model="roomForm.roomCategory" filterable allow-create default-first-option placeholder="选择或输入房型分类" @change="syncKindFromCategory"><el-option v-for="item in categoryOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item v-if="roomForm.kind === 'Bed'" label="性别限制"><el-select v-model="roomForm.gender" clearable><el-option label="不限性别" value="Any" /><el-option label="女生专区" value="Female" /><el-option label="男生专区" value="Male" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">容量与价格</div>
        <div class="form-grid">
          <el-form-item label="面积"><el-input-number v-model="roomForm.area" :min="0" :precision="1" controls-position="right" placeholder="例如：25"><template #suffix>㎡</template></el-input-number></el-form-item>
          <el-form-item label="朝向"><el-input v-model="roomForm.orientation" placeholder="例如：南" /></el-form-item>
          <el-form-item v-if="roomForm.kind !== 'Bed'" label="房间数量"><el-input-number v-model="roomForm.roomCount" :min="1" controls-position="right" @change="handleRoomCountChange" /></el-form-item>
          <el-form-item v-if="roomForm.kind === 'Bed'" label="床铺数量"><el-input-number v-model="roomForm.bedCount" :min="1" controls-position="right" @change="handleBedCountChange" /></el-form-item>
          <el-form-item label="容纳人数">
            <div class="capacity-fields">
              <el-input-number v-model="roomForm.maxGuests" :min="1" controls-position="right"><template #suffix>成人</template></el-input-number>
              <el-input-number v-model="roomForm.childCapacity" :min="0" controls-position="right"><template #suffix>儿童</template></el-input-number>
            </div>
          </el-form-item>
          <el-form-item label="户型"><el-input v-model="roomForm.layout" placeholder="例如：一室一厅" /></el-form-item>
          <el-form-item label="默认价格"><el-input-number v-model="roomForm.basePriceYuan" :min="0" :precision="2" :step="1" controls-position="right"><template #suffix>元</template></el-input-number></el-form-item>
        </div>
        <p class="form-hint">{{ quantityHint }}</p>

        <div class="form-section-title">民宿标签与设施</div>
        <div class="form-grid">
          <el-form-item label="民宿标签"><el-select v-model="roomForm.tags" multiple filterable default-first-option placeholder="选择或输入标签"><el-option v-for="item in tagOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="民宿服务"><el-select v-model="roomForm.services" multiple filterable default-first-option placeholder="选择服务"><el-option v-for="item in serviceOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="基础设施"><el-select v-model="roomForm.facilities" multiple filterable default-first-option placeholder="选择设施"><el-option v-for="item in facilityOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="卫浴设施"><el-select v-model="roomForm.bathroomFacilities" multiple filterable default-first-option placeholder="选择卫浴设施"><el-option v-for="item in bathroomOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
          <el-form-item label="周边设施"><el-select v-model="roomForm.nearbyFacilities" multiple filterable default-first-option placeholder="选择周边设施"><el-option v-for="item in nearbyOptions" :key="item" :label="item" :value="item" /></el-select></el-form-item>
        </div>

        <div class="form-section-title">入住规则</div>
        <div class="form-grid">
          <el-form-item label="入住时间"><el-input v-model="roomForm.checkInTime" placeholder="例如：14:00" /></el-form-item>
          <el-form-item label="退房时间"><el-input v-model="roomForm.checkOutTime" placeholder="例如：12:00" /></el-form-item>
        </div>

        <el-form-item label="房屋介绍"><el-input v-model="roomForm.description" type="textarea" :rows="3" placeholder="填写房间介绍、服务说明和注意事项" /></el-form-item>
        <el-form-item label="房间图片">
          <div class="room-image-editor">
            <div class="room-image-grid">
              <div v-for="(image, index) in roomImages" :key="`${image}-${index}`" class="room-image-item">
                <img :src="image" alt="" />
                <span v-if="index === 0" class="room-image-cover">封面</span>
                <button type="button" title="删除图片" @click="removeRoomImage(index)"><el-icon><Delete /></el-icon></button>
              </div>
              <el-upload
                v-if="roomImages.length < 9"
                class="room-image-upload"
                :show-file-list="false"
                :auto-upload="false"
                accept="image/*"
                multiple
                :on-change="handleRoomImageChange"
              >
                <div class="room-image-add"><el-icon><Plus /></el-icon><span>上传图片</span></div>
              </el-upload>
            </div>
            <div class="form-help">最多 9 张，第一张为封面，点击右上角可删除；支持一次选择多张图片。</div>
          </div>
        </el-form-item>

        <div class="form-section-title">审核与展示</div>
        <div class="form-grid">
          <el-form-item label="审核状态"><el-select v-model="roomForm.approvalStatus"><el-option label="审核中" value="PendingReview" /><el-option label="已通过" value="Approved" /><el-option label="已拒绝" value="Rejected" /><el-option label="草稿" value="Draft" /><el-option label="已下架" value="Offline" /></el-select></el-form-item>
          <el-form-item label="拒绝理由"><el-input v-model="roomForm.rejectionReason" placeholder="审核拒绝时填写" /></el-form-item>
          <el-form-item label="上架展示"><el-switch v-model="roomForm.isPublished" active-text="展示给用户" /></el-form-item>
          <el-form-item label="排序"><el-input-number v-model="roomForm.sortOrder" :min="0" controls-position="right" /></el-form-item>
        </div>
        <div v-if="roomForm.id" class="dialog-meta">
          <span>总预定数：{{ roomForm.totalBookings ?? 0 }}</span>
          <span>是否草稿：{{ roomForm.isDraft ? '是' : '否' }}</span>
          <span>审核时间：{{ formatDateTime(roomForm.reviewedAt) }}</span>
          <span>创建时间：{{ formatDateTime(roomForm.createdAt) }}</span>
          <span>更新时间：{{ formatDateTime(roomForm.updatedAt) }}</span>
        </div>
      </el-form>
      <template #footer><el-button @click="roomDialog = false">取消</el-button><el-button type="primary" :loading="roomSaving" @click="saveRoom">保存房间</el-button></template>
    </el-dialog>

    <el-dialog v-model="priceSettingsDialog" :title="`${selectedRoom?.name || '房间'} · 价格设置`" width="1120px" top="4vh" destroy-on-close @close="resetCalendarEdit" @closed="resetCalendarEdit">
      <div class="price-settings-dialog">
        <div class="price-settings-summary">
          <div class="price-settings-summary-copy">
            <strong>{{ selectedRoom?.storeName || '当前房间' }}</strong>
            <span>默认价格 ¥{{ selectedRoom ? centsToYuan(selectedRoom.basePriceCents) : '—' }} · {{ selectedRoom && selectedRoom.kind === 'Bed' ? '床铺数量' : '房间数量' }} {{ selectedRoom ? defaultInventoryForRoom(selectedRoom) : '—' }}{{ selectedRoom && selectedRoom.kind === 'Bed' ? ' 床' : ' 间' }}</span>
            <span class="price-settings-hint">改过单价的日期按日历里的总数/剩余展示，其余日期跟房型数量一致</span>
          </div>
          <div class="price-settings-controls">
            <el-button :icon="ArrowLeft" title="上个月" @click="shiftMonth(-1)" />
            <strong class="month-picker-label">{{ calendarMonthLabel }}</strong>
            <el-button :icon="ArrowRight" title="下个月" @click="shiftMonth(1)" />
            <el-button type="primary" plain @click="priceDialog = true">批量改价</el-button>
          </div>
        </div>
        <div v-loading="scheduleLoading" class="calendar-wrap">
          <div class="calendar-week"><span v-for="day in weekLabels" :key="day">{{ day }}</span></div>
          <div class="calendar-grid">
            <div v-for="day in calendarDays" :key="day.key" class="calendar-day" :class="{ outside: !day.currentMonth, weekend: day.weekend, today: day.today, holiday: day.holiday, customized: day.customized }" :title="day.customized ? '该日期在价格设置里单独改过，按日历里的库存展示' : ''">
              <div class="calendar-day-head"><b>{{ day.number }}</b><small v-if="day.holiday">{{ day.holiday }}</small></div>
              <div v-if="calendarEdit.dayKey === day.key" class="calendar-edit">
                <span class="calendar-edit-label">{{ calendarFieldLabel(calendarEdit.field) }}</span>
                <el-input
                  :ref="setCalendarEditInput"
                  v-model="calendarEdit.value"
                  class="calendar-edit-input"
                  type="number"
                  size="small"
                  @input="handleCalendarInput"
                  @blur="finishCalendarEdit($event)"
                  @keyup.enter="finishCalendarEdit($event)"
                  @keydown.esc.stop="finishCalendarEdit($event)"
                />
                <span class="calendar-edit-status" :class="{ saving: calendarCellSavingKey === calendarEdit.key }">{{ calendarCellSavingKey === calendarEdit.key ? '保存中…' : '输入即自动保存' }}</span>
              </div>
              <div v-else class="calendar-metrics">
                <div class="calendar-metric">
                  <span>单价</span>
                  <button type="button" class="calendar-edit-value" :disabled="!day.currentMonth || scheduleLoading" @click="startCalendarEdit(day, 'price')">{{ day.priceCents !== null ? `${centsToYuan(day.priceCents)} 元` : '—' }}</button>
                </div>
                <div class="calendar-metric">
                  <span>总数</span>
                  <button type="button" class="calendar-edit-value" :disabled="!day.currentMonth || scheduleLoading" @click="startCalendarEdit(day, 'total')">{{ day.totalQuantity ?? '—' }}</button>
                </div>
                <div class="calendar-metric">
                  <span>剩余</span>
                  <button type="button" class="calendar-edit-value" :disabled="!day.currentMonth || scheduleLoading" :class="{ 'danger-text': day.availableQuantity !== null && day.availableQuantity <= 1, 'success-text': day.availableQuantity !== null && day.availableQuantity > 1 }" @click="startCalendarEdit(day, 'available')">{{ day.availableQuantity ?? '—' }}</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </el-dialog>

    <el-dialog v-model="priceDialog" title="批量改价" width="480px">
      <el-form :model="priceForm" label-width="88px" class="dialog-form">
        <el-form-item label="日期范围"><el-date-picker v-model="priceForm.range" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" /></el-form-item>
        <el-form-item label="每日价格"><el-input-number v-model="priceForm.priceYuan" :min="0" :precision="2" :step="1" controls-position="right"><template #suffix>元</template></el-input-number></el-form-item>
        <el-form-item label="每日库存"><el-input-number v-model="priceForm.inventoryQuantity" :min="0" :precision="0" :step="1" controls-position="right"><template #suffix>{{ selectedRoom && selectedRoom.kind === 'Bed' ? '床' : '间' }}</template></el-input-number></el-form-item>
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
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, ArrowRight, Delete, House, Money, Plus, Refresh, Search } from '@element-plus/icons-vue'
import { del, get, post, put } from '../api'
import { requestNotificationRefresh } from '../notifications'

const props = defineProps({
  storeId: { type: [String, Number], default: '' }
})

const activeTab = ref('rooms')
const tabs = computed(() => [
  { key: 'rooms', label: '房间审核', count: rooms.value.length },
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
const roomOptions = reactive({
  categories: [],
  tags: [],
  services: [],
  facilities: [],
  bathroomFacilities: [],
  nearbyFacilities: []
})
const mergeOptionValues = (items, selected) => [...new Set([...(items || []), ...((selected || []).filter(Boolean))])]
const categoryOptions = computed(() => mergeOptionValues((roomOptions.categories || []).map((item) => item.name), roomForm.roomCategory ? [roomForm.roomCategory] : []))
const tagOptions = computed(() => mergeOptionValues(roomOptions.tags, roomForm.tags))
const serviceOptions = computed(() => mergeOptionValues(roomOptions.services, roomForm.services))
const facilityOptions = computed(() => mergeOptionValues(roomOptions.facilities, roomForm.facilities))
const bathroomOptions = computed(() => mergeOptionValues(roomOptions.bathroomFacilities, roomForm.bathroomFacilities))
const nearbyOptions = computed(() => mergeOptionValues(roomOptions.nearbyFacilities, roomForm.nearbyFacilities))

const stores = ref([])
const rooms = ref([])
const loading = ref(false)
const scheduleLoading = ref(false)
const scheduleSaving = ref(false)
const roomSaving = ref(false)
const roomDialog = ref(false)
const priceSettingsDialog = ref(false)
const priceDialog = ref(false)
const inventoryDialog = ref(false)
const unitsVisible = ref(false)
const selectedRoomId = ref('')
const selectedRows = ref([])
const roomPage = ref(1)
const roomPageSize = ref(10)
const inventoryPage = ref(1)
const inventoryPageSize = ref(10)
const unitRoom = ref(null)
const units = ref([])
const roomStatusTab = ref('all')
const calendarDate = ref(new Date())
const roomFilters = reactive({ storeId: '', kind: '', published: '', keyword: '' })
const scheduleRange = ref([dateOnly(new Date()), dateOnly(addDays(new Date(), 7))])
const priceItems = ref([])
const inventoryItems = ref([])
const roomForm = reactive(emptyRoom())
const roomImages = ref([])
const priceForm = reactive({ range: [dateOnly(new Date()), dateOnly(addDays(new Date(), 7))], priceYuan: 268, inventoryQuantity: 1 })
const inventoryForm = reactive({ range: [dateOnly(new Date()), dateOnly(addDays(new Date(), 7))], delta: 1, reason: '' })
const unitForm = reactive({ visible: false, id: '', code: '', status: 'Available' })
const calendarEdit = reactive({ key: '', dayKey: '', field: '', value: '' })
const calendarCellSavingKey = ref('')
// 日历小格里放三个数字，宽度只有 ~40px；编辑时改成整格宽的输入面板，这里拿它的实例做自动聚焦。
let calendarEditInput = null
// 输入即保存：停止输入 500ms 就写库，输入过程中的半成品不提示、不打断。
const CALENDAR_AUTO_SAVE_DELAY = 500
let calendarCommitTimer = null
let calendarCommitPending = false
let calendarPendingClose = false
const CALENDAR_FIELD_LABELS = { price: '单价（元）', total: '总库存', available: '剩余库存' }
function setCalendarEditInput(instance) { calendarEditInput = instance }

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
    (!text || `${row.name} ${row.longName || ''} ${row.storeName || ''}`.toLowerCase().includes(text))
}))
const pagedRooms = computed(() => filteredRooms.value.slice((roomPage.value - 1) * roomPageSize.value, roomPage.value * roomPageSize.value))
const pagedInventoryItems = computed(() => inventoryItems.value.slice((inventoryPage.value - 1) * inventoryPageSize.value, inventoryPage.value * inventoryPageSize.value))

const priceByDate = computed(() => new Map(priceItems.value.map((item) => [dateOnly(item.date), item])))
const inventoryByDate = computed(() => new Map(inventoryItems.value.map((item) => [dateOnly(item.date), item])))
const selectedRoom = computed(() => rooms.value.find((room) => room.id === selectedRoomId.value))
const calendarMonthLabel = computed(() => new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: 'long' }).format(calendarDate.value))
// 价格日历一天的展示口径：
// - 在价格设置里手动改过的日期（Manual 价格记录，含只调库存自动补的标记）
//   → 总数/剩余按日历里这一天的记录展示；
// - 没改过的日期（含历史遗留的脏库存）→ 回落到房型当前数量，也就是「正常显示」。
// 种子数据自动铺的 Seed / Weekend 价格不算手动设置。
function resolveCalendarDay({ currentMonth, price, inventory, defaultInventory, basePriceCents }) {
  const inventoryTotal = inventory?.totalQuantity ?? null
  const lockedQuantity = inventory?.lockedQuantity ?? 0
  const soldQuantity = inventory?.soldQuantity ?? 0
  const customized = Boolean(price) && price.source !== 'Seed' && price.source !== 'Weekend'
  const totalQuantity = defaultInventory === null
    ? inventoryTotal
    : customized
      ? (inventoryTotal ?? defaultInventory)
      : defaultInventory
  return {
    priceCents: currentMonth ? (price?.priceCents ?? basePriceCents ?? null) : null,
    totalQuantity: currentMonth ? totalQuantity : null,
    inventoryTotal: currentMonth ? inventoryTotal : null,
    lockedQuantity: currentMonth ? lockedQuantity : null,
    soldQuantity: currentMonth ? soldQuantity : null,
    availableQuantity: currentMonth && totalQuantity !== null
      ? Math.max(0, totalQuantity - lockedQuantity - soldQuantity)
      : null,
    customized: currentMonth ? customized : false
  }
}
const calendarDays = computed(() => {
  const monthStart = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth(), 1)
  const monthEnd = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth() + 1, 0)
  const total = Math.ceil((monthStart.getDay() + monthEnd.getDate()) / 7) * 7
  const todayKey = dateOnly(new Date())
  const defaultInventory = selectedRoom.value ? defaultInventoryForRoom(selectedRoom.value) : null
  return Array.from({ length: total }, (_, index) => {
    const date = new Date(monthStart)
    date.setDate(index - monthStart.getDay() + 1)
    const key = dateOnly(date)
    const currentMonth = date.getFullYear() === monthStart.getFullYear() && date.getMonth() === monthStart.getMonth()
    const metrics = resolveCalendarDay({
      currentMonth,
      price: priceByDate.value.get(key),
      inventory: inventoryByDate.value.get(key),
      defaultInventory,
      basePriceCents: selectedRoom.value?.basePriceCents ?? null
    })
    return {
      key,
      number: date.getDate(),
      currentMonth,
      weekend: date.getDay() === 0 || date.getDay() === 6,
      today: key === todayKey,
      holiday: festivalLabel(date),
      ...metrics
    }
  })
})

// The admin calendar has no holiday API yet. Keep the visual calendar useful
// with recurring statutory holidays plus the lunar dates used by the current
// demo year. The API can replace this map later without changing the view.
const recurringFestivals = {
  '01-01': '元旦',
  '04-05': '清明节',
  '05-01': '劳动节',
  '10-01': '国庆节'
}
const yearFestivals = {
  2025: { '01-29': '春节', '05-31': '端午节', '10-06': '中秋节' },
  2026: { '02-17': '春节', '06-19': '端午节', '09-25': '中秋节', '10-08': '寒露', '10-23': '霜降' },
  2027: { '02-06': '春节', '06-09': '端午节', '09-15': '中秋节' }
}

function festivalLabel(date) {
  const key = dateOnly(date)
  const yearMap = yearFestivals[date.getFullYear()] || {}
  return yearMap[key.slice(5)] || recurringFestivals[key.slice(5)] || ''
}

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
function monthPickerRange(value) {
  const start = new Date(value.getFullYear(), value.getMonth(), 1)
  const end = new Date(value.getFullYear(), value.getMonth() + 1, 0)
  return [dateOnly(start), dateOnly(end)]
}
function emptyRoom() {
  return {
    id: '', storeId: '', name: '', longName: '', kind: 'PrivateRoom', roomCategory: '', bedCount: 1, gender: 'Any',
    maxGuests: 2, childCapacity: 0, area: 15, orientation: '', roomCount: 1, layout: '', basePriceCents: 26800, basePriceYuan: 268, defaultInventory: 1,
    checkInTime: '14:00', checkOutTime: '12:00',
    tags: [], services: [], facilities: [], bathroomFacilities: [], nearbyFacilities: [], description: '',
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
// 房型数量是唯一口径：多人间看床铺数量，独立房型看房间数量；老数据缺字段时才退回默认库存。
function defaultInventoryForRoom(room) {
  if (!room) return null
  const quantity = room.kind === 'Bed' ? Number(room.bedCount) : Number(room.roomCount)
  if (Number(quantity) > 0) return Math.max(Number(quantity), 1)
  if (Number(room.defaultInventory) > 0) return Math.max(Number(room.defaultInventory), 1)
  return 1
}
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
function handleRoomImageChange(uploadFile) {
  const raw = uploadFile?.raw
  if (!raw) return
  if (!raw.type?.startsWith('image/')) return ElMessage.warning('请选择图片文件')
  const reader = new FileReader()
  reader.onload = () => {
    roomImages.value = [...roomImages.value, String(reader.result)].slice(0, 9)
  }
  reader.readAsDataURL(raw)
}
function removeRoomImage(index) {
  roomImages.value = roomImages.value.filter((_, itemIndex) => itemIndex !== index)
}
async function loadStores() { stores.value = await get('/admin/stores') }
async function loadRoomOptions() {
  try {
    const data = await get('/admin/room-options')
    roomOptions.categories = Array.isArray(data.categories) ? data.categories : []
    roomOptions.tags = Array.isArray(data.tags) ? data.tags : []
    roomOptions.services = Array.isArray(data.services) ? data.services : []
    roomOptions.facilities = Array.isArray(data.facilities) ? data.facilities : []
    roomOptions.bathroomFacilities = Array.isArray(data.bathroomFacilities) ? data.bathroomFacilities : []
    roomOptions.nearbyFacilities = Array.isArray(data.nearbyFacilities) ? data.nearbyFacilities : []
  } catch (error) {
    ElMessage.error(error.message || '房间选项加载失败')
  }
}
async function loadRooms() {
  loading.value = true
  try {
    const query = new URLSearchParams({ includeUnpublished: 'true' })
    if (roomFilters.storeId) query.set('storeId', roomFilters.storeId)
    rooms.value = await get(`/admin/room-types?${query.toString()}`)
    const availableRooms = filteredRooms.value
    if (!selectedRoomId.value || !availableRooms.some((room) => room.id === selectedRoomId.value)) {
      selectedRoomId.value = availableRooms[0]?.id || (!roomFilters.storeId ? rooms.value[0]?.id || '' : '')
    }
    roomPage.value = 1
    if (selectedRoom.value) {
      priceForm.priceYuan = centsToYuan(selectedRoom.value.basePriceCents)
      priceForm.inventoryQuantity = defaultInventoryForRoom(selectedRoom.value)
    }
  } catch (error) { ElMessage.error(error.message) } finally { loading.value = false }
}
async function loadSchedule(mode = activeTab.value, { silent = false } = {}) {
  if (!selectedRoomId.value) return
  const [from, to] = mode === 'prices' ? monthRange(calendarDate.value) : (scheduleRange.value || [])
  if (!from || !to) return
  if (!silent) scheduleLoading.value = true
  try {
    const query = `roomTypeId=${selectedRoomId.value}&from=${from}&to=${to}`
    const [prices, inventory] = await Promise.all([get(`/admin/prices?${query}`), get(`/admin/inventory?${query}`)])
    priceItems.value = prices
    inventoryItems.value = inventory
    inventoryPage.value = 1
  } catch (error) { if (!silent) ElMessage.error(error.message) } finally { if (!silent) scheduleLoading.value = false }
}
function shiftMonth(delta) {
  calendarDate.value = new Date(calendarDate.value.getFullYear(), calendarDate.value.getMonth() + delta, 1)
  loadSchedule('prices')
}
// 分类名里带「床位/多人间」按多人间处理，带「独立/单间/房间」按独立房型处理；
// 认不出来才退回分类表里的 kind。避免历史脏数据把「独立房间」记成 Bed 后一直传染。
function inferKindFromCategory(name) {
  const text = String(name || '').trim()
  if (!text) return ''
  if (/(床位|多人间|宿舍|胶囊|铺位|青旅)/.test(text)) return 'Bed'
  if (/(独立|单间|标间|大床|双床|套房|家庭房|房间)/.test(text)) return 'PrivateRoom'
  return ''
}
function syncKindFromCategory(value) {
  const match = (roomOptions.categories || []).find((item) => item.name === value)
  const inferred = inferKindFromCategory(value) || (match && match.kind) || roomForm.kind
  if (!inferred) return
  roomForm.kind = inferred === 'Bed' ? 'Bed' : 'PrivateRoom'
  handleRoomKindChange(roomForm.kind)
}
// 数量只有一个口径：独立房型用「房间数量」，多人间用「床铺数量」，默认库存始终跟着它走。
// 之前表单里还有一个独立的「默认库存」，导致房间管理和价格设置各存一个数字、互不同步。
const quantityHint = computed(() => roomForm.kind === 'Bed'
  ? `每日库存按床铺数量生成：${Math.max(Number(roomForm.bedCount) || 1, 1)} 个床位；保存后自动同步到「价格设置」未来 180 天`
  : `每日库存按房间数量生成：${Math.max(Number(roomForm.roomCount) || 1, 1)} 间；保存后自动同步到「价格设置」未来 180 天`)
function handleRoomKindChange(value) {
  if (value === 'Bed') {
    roomForm.roomCount = 1
    roomForm.bedCount = Math.max(Number(roomForm.bedCount) || 1, 1)
    roomForm.defaultInventory = roomForm.bedCount
  } else {
    roomForm.bedCount = 1
    roomForm.gender = 'Any'
    roomForm.roomCount = Math.max(Number(roomForm.roomCount) || 1, 1)
    roomForm.defaultInventory = roomForm.roomCount
  }
}
function handleRoomCountChange(value) {
  const count = Math.max(Number(value) || 1, 1)
  roomForm.roomCount = count
  roomForm.defaultInventory = count
}
function handleBedCountChange(value) {
  const count = Math.max(Number(value) || 1, 1)
  roomForm.bedCount = count
  roomForm.defaultInventory = count
}
function openRoomCreate() { Object.assign(roomForm, emptyRoom(), { storeId: stores.value[0]?.id || '' }); roomImages.value = []; roomDialog.value = true }
function openRoomEdit(row) {
  const base = emptyRoom()
  Object.assign(base, row, {
    // 列表的「长名称」列在留空时展示的是房间名称，编辑时用同样的值回填，
    // 避免出现「列表里有名字、点开编辑却是空」的错觉。
    longName: row.longName || row.name || '',
    tags: parseArray(row.tagsJson),
    services: parseArray(row.servicesJson),
    facilities: parseArray(row.facilitiesJson),
    bathroomFacilities: parseArray(row.bathroomFacilitiesJson),
    nearbyFacilities: parseArray(row.nearbyFacilitiesJson),
    basePriceYuan: centsToYuan(row.basePriceCents),
    childCapacity: row.childCapacity || 0,
    checkInTime: row.checkInTime || '',
    checkOutTime: row.checkOutTime || '',
    rejectionReason: row.rejectionReason || ''
  })
  Object.assign(roomForm, base)
  // 房型分类决定类型：打开编辑时按分类名校准一次，避免「分类写着独立房间、却在填床铺数量」。
  syncKindFromCategory(roomForm.roomCategory)
  roomImages.value = parseArray(row.imageUrlsJson)
  roomDialog.value = true
}
async function saveRoom() {
  if (!roomForm.storeId || !roomForm.name) return ElMessage.warning('请填写所属门店和房间名称')
  if (!String(roomForm.roomCategory || '').trim()) return ElMessage.warning('请选择或填写房型分类')
  roomSaving.value = true
  try {
    const kind = roomForm.kind === 'Bed' ? 'Bed' : 'PrivateRoom'
    const bedCount = kind === 'Bed' ? Math.max(Number(roomForm.bedCount) || 1, 1) : 1
    const roomCount = kind === 'Bed' ? 1 : Math.max(Number(roomForm.roomCount) || 1, 1)
    // 数量即库存：保存时把「房间数量/床铺数量」写进 defaultInventory，后端据此同步每日库存。
    const defaultInventory = kind === 'Bed' ? bedCount : roomCount
    const payload = {
      ...roomForm,
      kind,
      bedCount,
      roomCount,
      defaultInventory,
      gender: kind === 'Bed' ? roomForm.gender : 'Any',
      roomCategory: String(roomForm.roomCategory || '').trim(),
      basePriceCents: yuanToCents(roomForm.basePriceYuan),
      childCapacity: Number(roomForm.childCapacity) || 0,
      facilitiesJson: jsonArray(roomForm.facilities),
      tagsJson: jsonArray(roomForm.tags),
      servicesJson: jsonArray(roomForm.services),
      bathroomFacilitiesJson: jsonArray(roomForm.bathroomFacilities),
      nearbyFacilitiesJson: jsonArray(roomForm.nearbyFacilities),
      imageUrlsJson: jsonArray(roomImages.value)
    }
    delete payload.tags
    delete payload.services
    delete payload.facilities
    delete payload.bathroomFacilities
    delete payload.nearbyFacilities
    delete payload.basePriceYuan
    // 退订规则/加入规则已不再维护：不提交这两个字段，
    // 后端 PUT 用 `?? 原值` 保留库里已有内容，不会因为界面移除而被清空。
    delete payload.cancellationRule
    delete payload.joinRule
    if (roomForm.id) await put(`/admin/room-types/${roomForm.id}`, payload)
    else await post('/admin/room-types', payload)
    ElMessage.success('房间已保存'); requestNotificationRefresh(); roomDialog.value = false; await Promise.all([loadRooms(), loadRoomOptions()])
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
    requestNotificationRefresh()
    await loadRooms()
  } catch (error) { ElMessage.error(error.message) }
}
async function togglePublish(row) {
  try { await post(`/admin/room-types/${row.id}/publish?published=${!row.isPublished}`); ElMessage.success(row.isPublished ? '房间已隐藏' : '房间已上架'); await loadRooms() } catch (error) { ElMessage.error(error.message) }
}
function openPriceSettings(row) {
  selectedRoomId.value = row.id
  calendarDate.value = new Date()
  priceForm.range = monthPickerRange(calendarDate.value)
  priceForm.priceYuan = centsToYuan(row.basePriceCents)
  priceForm.inventoryQuantity = defaultInventoryForRoom(row)
  priceSettingsDialog.value = true
  loadSchedule('prices')
}
async function approveSelected() {
  const rows = selectedRows.value.filter((row) => ['Draft', 'PendingReview', 'Rejected'].includes(row.approvalStatus))
  if (!rows.length) return ElMessage.warning('请选择待审核或已拒绝的房间')
  try {
    await ElMessageBox.confirm(`确定通过选中的 ${rows.length} 个房间并允许上架吗？`, '批量审核通过', { type: 'success' })
    for (const row of rows) await post(`/admin/room-types/${row.id}/review`, { approved: true, rejectionReason: null })
    ElMessage.success(`已通过 ${rows.length} 个房间`)
    requestNotificationRefresh()
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
function calendarEditKey(day, field) { return `${day.key}:${field}` }
function calendarFieldLabel(field) { return CALENDAR_FIELD_LABELS[field] || '' }
// 编辑框的初始值（也是「没改动就关掉」的判断依据）。
function calendarOriginalValue(day, field) {
  const value = field === 'price'
    ? centsToYuan(day.priceCents ?? selectedRoom.value?.basePriceCents ?? 0)
    : field === 'total'
      ? day.totalQuantity ?? defaultInventoryForRoom(selectedRoom.value)
      : day.availableQuantity ?? defaultInventoryForRoom(selectedRoom.value)
  return String(value ?? '')
}
function clearCalendarCommitTimer() {
  if (!calendarCommitTimer) return
  clearTimeout(calendarCommitTimer)
  calendarCommitTimer = null
}
function resetCalendarEdit() {
  clearCalendarCommitTimer()
  calendarCommitPending = false
  calendarPendingClose = false
  Object.assign(calendarEdit, { key: '', dayKey: '', field: '', value: '' })
  calendarCellSavingKey.value = ''
}
// 写库后日历会静默刷新，算库存差要用最新的一格数据，不能拿闭包里那份旧对象。
function currentCalendarDay() {
  return calendarDays.value.find((day) => day.key === calendarEdit.dayKey) || null
}
function startCalendarEdit(day, field) {
  if (!day.currentMonth || !selectedRoomId.value) return
  clearCalendarCommitTimer()
  calendarCommitPending = false
  Object.assign(calendarEdit, {
    key: calendarEditKey(day, field),
    dayKey: day.key,
    field,
    value: calendarOriginalValue(day, field)
  })
  // 等编辑面板渲染出来再聚焦，点一下就能直接改数字。
  nextTick(() => {
    calendarEditInput?.focus?.()
    calendarEditInput?.select?.()
  })
}
// input 事件：停止输入 500ms 就自动写库，面板保持打开，不弹提示打断输入。
function handleCalendarInput() {
  if (!calendarEdit.key) return
  clearCalendarCommitTimer()
  calendarCommitTimer = setTimeout(() => {
    calendarCommitTimer = null
    commitCalendarEdit({ silent: true, keepOpen: true })
  }, CALENDAR_AUTO_SAVE_DELAY)
}
// 失焦（点其他任何地方）/ 回车 / Esc：把还没写库的内容立刻写掉，然后收起面板。
function finishCalendarEdit(event) {
  const field = calendarEdit.field
  if (!field) return
  clearCalendarCommitTimer()
  const related = event && event.relatedTarget
  // 点弹窗右上角 X 是「退出」，焦点落到关闭按钮上时丢弃草稿，别把它当成普通失焦写进日历。
  if (related && typeof related.closest === 'function' && related.closest('.el-dialog__headerbtn')) {
    resetCalendarEdit()
    return
  }
  const day = currentCalendarDay()
  if (!day) {
    resetCalendarEdit()
    return
  }
  if (String(calendarEdit.value ?? '').trim() === calendarOriginalValue(day, field)) {
    resetCalendarEdit()
    return
  }
  commitCalendarEdit({ keepOpen: false, closeOnInvalid: true })
}
async function commitCalendarEdit({ silent = false, keepOpen = false, closeOnInvalid = false } = {}) {
  const field = calendarEdit.field
  if (!field) return
  const editKey = calendarEdit.key
  const day = currentCalendarDay()
  if (!day) return
  const rawValue = Number(calendarEdit.value)
  const invalid = !Number.isFinite(rawValue)
    || (field === 'price' ? rawValue <= 0 : rawValue < 0 || !Number.isInteger(rawValue))
  if (invalid) {
    // 输入过程中的半成品（清空、只输了个小数点）先不提示，等失焦/回车再说。
    if (silent) return
    ElMessage.warning(field === 'price' ? '请输入有效的价格' : '请输入大于等于 0 的整数')
    if (closeOnInvalid) resetCalendarEdit()
    return
  }
  if (calendarCellSavingKey.value === editKey) {
    // 上一次保存还没回来：标记一下，等它结束后再补写最后一次输入（失焦的还要记得收面板）。
    calendarCommitPending = true
    if (!keepOpen) calendarPendingClose = true
    return
  }
  if (String(calendarEdit.value ?? '').trim() === calendarOriginalValue(day, field)) {
    if (!keepOpen) resetCalendarEdit()
    return
  }

  calendarCellSavingKey.value = editKey
  let saved = false
  try {
    if (field === 'price') {
      await put('/admin/prices/bulk', {
        roomTypeId: selectedRoomId.value,
        from: day.key,
        to: addDateString(day.key, 1),
        priceCents: yuanToCents(rawValue)
      })
    } else {
      const lockedQuantity = Number(day.lockedQuantity) || 0
      const soldQuantity = Number(day.soldQuantity) || 0
      // 改库存要基于服务端的真实总数算差值：没单独设置过的日期，展示值可能已回落到房型数量，
      // 但库存行里仍是旧数字，直接拿展示值相减会让调整量偏小/偏大。
      const currentTotal = Number(day.inventoryTotal ?? day.totalQuantity ?? defaultInventoryForRoom(selectedRoom.value) ?? 0)
      const nextTotal = field === 'total' ? rawValue : rawValue + lockedQuantity + soldQuantity
      if (nextTotal < lockedQuantity + soldQuantity) {
        ElMessage.warning('库存不能低于锁定和已售数量')
        if (closeOnInvalid) resetCalendarEdit()
        return
      }
      const delta = nextTotal - currentTotal
      if (delta !== 0) {
        await post('/admin/inventory/adjust', {
          roomTypeId: selectedRoomId.value,
          from: day.key,
          to: addDateString(day.key, 1),
          delta,
          reason: field === 'total' ? '日历修改总库存' : '日历修改剩余库存'
        })
      }
    }
    saved = true
    // 面板还开着时不能转圈，否则输入会被打断；静默刷新让格子数据跟上服务端。
    await loadSchedule('prices', { silent: true })
    // 保存期间用户可能已经点开了另一格，别把新的编辑面板一起清掉。
    if (!keepOpen && calendarEdit.key === editKey) {
      resetCalendarEdit()
      if (!silent) ElMessage.success(field === 'price' ? '单价已更新' : '库存已更新')
    }
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    if (calendarCellSavingKey.value === editKey) calendarCellSavingKey.value = ''
    // 保存期间又输入/又点了别处：这里补一次，别丢掉最后一次输入，也别让面板挂着不消失。
    const stillEditing = calendarEdit.key === editKey
    const pending = calendarCommitPending && stillEditing
    const shouldClose = calendarPendingClose
    calendarCommitPending = false
    calendarPendingClose = false
    if (saved && pending) {
      const latest = currentCalendarDay()
      const dirty = latest && String(calendarEdit.value ?? '').trim() !== calendarOriginalValue(latest, field)
      if (dirty) {
        commitCalendarEdit(shouldClose ? { keepOpen: false, closeOnInvalid: true } : { silent: true, keepOpen: true })
      } else if (shouldClose) {
        resetCalendarEdit()
      }
    }
  }
}
async function savePrices() {
  const [from, to] = priceForm.range || []
  const inventoryQuantity = Number(priceForm.inventoryQuantity)
  if (!selectedRoomId.value || !from || !to || Number(priceForm.priceYuan) <= 0 || !Number.isInteger(inventoryQuantity) || inventoryQuantity < 0) return ElMessage.warning('请填写完整的价格范围、金额和库存')
  scheduleSaving.value = true
  try {
    await put('/admin/prices/bulk', {
      roomTypeId: selectedRoomId.value,
      from,
      to: addDateString(to, 1),
      priceCents: yuanToCents(priceForm.priceYuan),
      totalQuantity: inventoryQuantity
    })
    ElMessage.success('价格和库存已更新')
    priceDialog.value = false
    await loadSchedule('prices')
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    scheduleSaving.value = false
  }
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
watch(() => props.storeId, (value) => {
  if (value !== undefined && value !== null) roomFilters.storeId = String(value)
  if (activeTab.value !== 'rooms') loadSchedule()
})
watch(() => roomFilters.storeId, () => {
  const availableRooms = filteredRooms.value
  if (availableRooms.length && !availableRooms.some((room) => room.id === selectedRoomId.value)) selectedRoomId.value = availableRooms[0].id
})
watch([roomStatusTab, () => roomFilters.storeId, () => roomFilters.kind, () => roomFilters.published, () => roomFilters.keyword, roomPageSize], () => { roomPage.value = 1 })
watch(filteredRooms, () => {
  const maxPage = Math.max(1, Math.ceil(filteredRooms.value.length / roomPageSize.value))
  if (roomPage.value > maxPage) roomPage.value = maxPage
})
watch([inventoryItems, inventoryPageSize], () => {
  const maxPage = Math.max(1, Math.ceil(inventoryItems.value.length / inventoryPageSize.value))
  if (inventoryPage.value > maxPage) inventoryPage.value = maxPage
})
watch(activeTab, (value) => { if (value !== 'rooms') loadSchedule() })
watch(selectedRoomId, () => { if (activeTab.value !== 'rooms') loadSchedule() })
onMounted(async () => {
  try {
    const storedStoreId = sessionStorage.getItem('tcpms.admin.room-store-filter')
    roomFilters.storeId = String(props.storeId || storedStoreId || '')
    sessionStorage.removeItem('tcpms.admin.room-store-filter')
    await Promise.all([loadStores(), loadRooms(), loadRoomOptions()])
  } catch (error) { ElMessage.error(error.message) }
})
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
.capacity-fields{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;width:100%;min-width:0}
.dialog-data-tip{margin:0 0 14px;padding:9px 12px;border:1px solid var(--admin-border);border-radius:8px;background:var(--admin-surface-soft);color:var(--admin-muted);font-size:12px;line-height:1.6}
.dialog-meta{display:flex;flex-wrap:wrap;gap:6px 18px;margin-top:6px;padding:10px 12px;border-radius:8px;background:var(--admin-surface-soft);color:var(--admin-muted);font-size:12px;line-height:1.6}
.capacity-fields :deep(.el-input-number){width:100%;min-width:0}
.room-image-editor{width:100%}
.room-image-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px}
.room-image-item,.room-image-upload{position:relative;height:92px;border-radius:7px}
.room-image-item{overflow:hidden;border:1px solid var(--admin-border);background:var(--admin-surface-soft)}
.room-image-item img{display:block;width:100%;height:100%;object-fit:cover}
.room-image-cover{position:absolute;top:0;left:0;padding:2px 7px;border-bottom-right-radius:7px;background:rgba(15,119,119,.86);color:#fff;font-size:11px;line-height:1.6}
.room-image-item button{position:absolute;top:5px;right:5px;display:grid;width:25px;height:25px;place-items:center;border:0;border-radius:50%;background:rgba(201,91,98,.92);color:#fff;cursor:pointer}
.room-image-item button:hover{background:#b74953}
.room-image-upload{display:block;width:100%}
.room-image-upload :deep(.el-upload){display:block;width:100%;height:100%}
.room-image-add{display:flex;height:100%;align-items:center;justify-content:center;flex-direction:column;gap:5px;border:1px dashed var(--admin-border);border-radius:7px;background:var(--admin-surface-soft);color:var(--admin-primary-dark);font-size:12px;cursor:pointer;transition:border-color .18s ease,background .18s ease}
.room-image-add:hover{border-color:var(--admin-primary);background:var(--admin-primary-soft)}
.room-image-add .el-icon{font-size:18px}
.form-help{margin-top:8px;color:var(--admin-muted);font-size:12px;line-height:1.5}
.room-status-tabs{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:16px}
.room-status-tabs button{border:1px solid var(--admin-border);background:var(--admin-surface-soft);color:var(--admin-muted);border-radius:7px;padding:8px 14px;cursor:pointer;font:inherit}
.room-status-tabs button.active{background:var(--admin-primary-soft);border-color:var(--admin-primary);color:var(--admin-primary);font-weight:700}
.room-status-tabs span{margin-left:6px;font-size:12px;opacity:.7}
.room-photo{width:46px;height:40px;border-radius:7px;overflow:hidden;background:var(--admin-surface-soft);display:inline-flex;align-items:center;justify-content:center;color:var(--admin-muted);flex:0 0 auto}
.room-photo img{width:100%;height:100%;object-fit:cover}
.room-photo.empty{border:1px dashed var(--admin-border)}
.table-subline{display:block;color:var(--admin-muted);font-size:12px;margin-top:4px}
.price-settings-dialog{display:grid;gap:16px}
.price-settings-summary{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:12px 14px;border:1px solid var(--admin-border);border-radius:8px;background:var(--admin-surface-soft)}
.price-settings-summary-copy{display:grid;gap:5px;min-width:0}
.price-settings-summary-copy strong{overflow:hidden;color:var(--admin-text);font-size:14px;text-overflow:ellipsis;white-space:nowrap}
.price-settings-summary-copy span{color:var(--admin-muted);font-size:12px}
.price-settings-controls{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.month-picker-label{min-width:128px;text-align:center;color:var(--admin-text)}
.calendar-wrap{overflow:auto}
.calendar-week,.calendar-grid{display:grid;grid-template-columns:repeat(7,minmax(130px,1fr));min-width:910px}
.calendar-week span{padding:10px 12px;text-align:center;color:var(--admin-muted);font-size:12px;border-bottom:1px solid var(--admin-border)}
.calendar-day{min-height:126px;padding:10px;border:1px solid var(--admin-border);border-top:0;border-left:0;background:var(--admin-surface);transition:background .18s ease}
.calendar-day:nth-child(7n+1){border-left:1px solid var(--admin-border)}
.calendar-day:hover{background:var(--admin-primary-soft)}
.calendar-day.outside{opacity:.45;background:var(--admin-surface-soft)}
.calendar-day.weekend{background:#f5fbfa}
.calendar-day.customized .calendar-day-head b::after{content:'';display:inline-block;width:5px;height:5px;margin-left:4px;border-radius:50%;background:var(--admin-primary,#0f766e);vertical-align:super}
.calendar-day.holiday{background:#fff2f2}
.calendar-day.holiday .calendar-day-head b,
.calendar-day.holiday .calendar-day-head small{color:#e5535d}
.calendar-day.today{box-shadow:inset 0 0 0 2px var(--admin-primary)}
.calendar-day-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}
.calendar-day-head b{font-size:16px}
.calendar-day-head small{font-size:11px;color:var(--admin-muted)}
.calendar-metrics{display:grid;grid-template-columns:repeat(3,1fr);gap:5px}
.calendar-metrics div{display:flex;flex-direction:column;gap:5px;text-align:center}
.calendar-metric{min-width:0}
.calendar-metrics span{font-size:11px;color:var(--admin-muted)}
.calendar-metrics strong{font-size:13px;color:var(--admin-text)}
.calendar-metrics .success-text,.calendar-metrics .danger-text{font-weight:700}
.calendar-edit-value{width:100%;min-height:24px;padding:2px 1px;border:1px solid transparent;border-radius:5px;background:transparent;color:var(--admin-text);cursor:pointer;font:inherit;font-weight:700;line-height:1.3;text-align:center}
.calendar-edit-value:hover:not(:disabled){border-color:var(--admin-primary);background:#f7fffd}
.calendar-edit-value:disabled{cursor:default}
/* 日历格只有 ~130px 宽，三列指标各占 1/3；编辑时整格换成一块输入面板，避免输入框被压到看不清。 */
.calendar-edit{display:flex;flex-direction:column;gap:6px}
.calendar-edit-label{font-size:11px;color:var(--admin-muted);line-height:1;text-align:center}
.calendar-edit-input{width:100%}
.calendar-edit-input :deep(.el-input__wrapper){padding:1px 8px}
.calendar-edit-input :deep(.el-input__inner){padding:0;text-align:center;font-weight:700}
.calendar-edit-input :deep(.el-input__inner)::-webkit-outer-spin-button,
.calendar-edit-input :deep(.el-input__inner)::-webkit-inner-spin-button{-webkit-appearance:none;margin:0}
.calendar-edit-status{font-size:11px;line-height:1.3;color:var(--admin-muted);text-align:center}
.calendar-edit-status.saving{color:var(--admin-primary);font-weight:700}
.form-section-title{font-size:14px;font-weight:700;color:var(--admin-text);padding:12px 0 8px;border-top:1px solid var(--admin-border);margin-top:4px}
.form-section-title:first-child{border-top:0;padding-top:0}
.form-hint{margin:-2px 0 10px;color:var(--admin-muted,#6f8085);font-size:12px}
.room-form :deep(.el-form-item){margin-bottom:14px}
.room-form :deep(.el-select),.room-form :deep(.el-input-number){width:100%}
@media (max-width: 900px){.calendar-day{min-height:112px}}
@media (max-width: 640px){.room-image-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.toolbar-left{width:100%}.price-settings-summary{align-items:stretch;flex-direction:column}.price-settings-controls{justify-content:space-between}.month-picker-label{min-width:0;flex:1}}
</style>
