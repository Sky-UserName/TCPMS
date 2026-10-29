<template>
  <div class="view-stack">
    <section class="page-heading">
      <div>
        <p>房间类型来自「房间管理」中的实际房源，按房型分类 + 房源形态聚合，不再单独维护字典。</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
    </section>

    <el-card class="panel-card" shadow="never">
      <div class="room-type-summary">
        <strong>房型分类</strong>
        <span>共 {{ rows.length }} 种，{{ totalRooms }} 个房间</span>
      </div>
      <el-table v-loading="loading" :data="rows" class="clean-table admin-data-table">
        <el-table-column prop="name" label="房型分类" min-width="240">
          <template #default="{ row }">{{ row.name || '未分类' }}</template>
        </el-table-column>
        <el-table-column label="房源形态" width="150">
          <template #default="{ row }">
            <el-tag :type="row.kind === 'Bed' ? 'warning' : 'success'" effect="light">{{ kindLabel(row.kind) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="count" label="房间数量" width="120" />
      </el-table>
      <p class="view-note">新增或修改房型分类，请到「房间管理」新增/编辑房间；修改后会立即出现在这里和发布页。</p>
    </el-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { get } from '../api'

const rows = ref([])
const loading = ref(false)
const totalRooms = computed(() => rows.value.reduce((total, item) => total + Number(item.count || 0), 0))

function kindLabel(value) {
  return value === 'Bed' ? '多人间床位' : '独立房间'
}

async function load() {
  loading.value = true
  try {
    const data = await get('/admin/room-options')
    rows.value = Array.isArray(data.categories) ? data.categories : []
  } catch (error) {
    ElMessage.error(error.message || '房间类型加载失败')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.room-type-summary {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 16px;
  padding: 0 2px 14px;
  color: #6f8085;
  font-size: 13px;
}

.room-type-summary strong {
  color: #23393d;
  font-size: 15px;
}

.view-note {
  margin: 14px 2px 0;
  color: #6f8085;
  font-size: 13px;
}
</style>
