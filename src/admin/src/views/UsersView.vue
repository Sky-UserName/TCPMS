<template>
  <div class="view-stack">
    <section class="page-heading"><div><span class="eyebrow">ACCESS CONTROL</span><h2>账号与权限</h2><p>按角色和门店范围控制后台访问，服务端权限始终是最终控制点。</p></div><el-button type="primary" :icon="Plus" @click="openCreate">新增账号</el-button></section>
    <section class="split-grid">
      <el-card class="panel-card" shadow="never"><template #header><div class="panel-header"><div><strong>后台账号</strong><span>{{ users.length }} 个账号</span></div><el-button text :icon="Refresh" title="刷新账号" @click="load" /></div></template><el-table v-loading="loading" :data="pagedUsers" class="clean-table admin-data-table"><el-table-column label="账号" min-width="180"><template #default="{ row }"><div class="user-cell"><span class="profile-avatar">{{ row.displayName?.slice(0, 1) }}</span><div><strong>{{ row.displayName }}</strong><small>{{ row.username }}</small></div></div></template></el-table-column><el-table-column label="角色" min-width="150"><template #default="{ row }"><el-tag v-for="role in row.roles" :key="role" size="small" effect="light">{{ role }}</el-tag></template></el-table-column><el-table-column label="门店范围" min-width="140"><template #default="{ row }">{{ row.storeName || '全部门店' }}</template></el-table-column><el-table-column label="状态" width="82"><template #default="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'info'" effect="light">{{ row.isEnabled ? '启用' : '停用' }}</el-tag></template></el-table-column><el-table-column label="操作" width="80"><template #default="{ row }"><el-button link type="primary" @click="openEdit(row)">编辑</el-button></template></el-table-column></el-table><div class="list-footer"><span>共 {{ users.length }} 个账号</span><el-pagination v-model:current-page="page" v-model:page-size="pageSize" :page-sizes="[10, 20, 50]" layout="sizes, prev, pager, next, jumper" :total="users.length" background /></div></el-card>
      <el-card class="panel-card role-card" shadow="never"><template #header><div class="panel-header"><div><strong>预设角色</strong><span>首版使用固定角色矩阵</span></div></div></template><div v-for="role in roles" :key="role.id" class="role-item"><span class="role-symbol"><el-icon><UserFilled /></el-icon></span><div><strong>{{ role.name }}</strong><small>{{ role.code }} · {{ role.userCount }} 个账号</small></div><el-icon><ArrowRight /></el-icon></div><div class="permission-note"><el-icon><InfoFilled /></el-icon><span>自定义菜单、按钮和数据范围后续继续扩展；当前接口已按角色和门店范围校验。</span></div></el-card>
    </section>
    <el-dialog v-model="dialog.visible" :title="dialog.mode === 'create' ? '新增后台账号' : '编辑后台账号'" width="560px" destroy-on-close><el-form :model="form" label-width="92px" class="dialog-form"><el-form-item label="登录账号" required><el-input v-model="form.username" :disabled="dialog.mode === 'edit'" /></el-form-item><el-form-item label="显示姓名" required><el-input v-model="form.displayName" /></el-form-item><el-form-item label="初始密码" :required="dialog.mode === 'create'"><el-input v-model="form.password" type="password" show-password placeholder="编辑时留空表示不修改" /></el-form-item><el-form-item label="手机号"><el-input v-model="form.phoneNumber" /></el-form-item><el-form-item label="绑定门店"><el-select v-model="form.storeId" clearable><el-option label="全部门店" value="" /><el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" /></el-select></el-form-item><el-form-item label="角色"><el-checkbox-group v-model="form.roleIds"><el-checkbox v-for="role in roles" :key="role.id" :value="role.id">{{ role.name }}</el-checkbox></el-checkbox-group></el-form-item><el-form-item label="账号状态"><el-switch v-model="form.isEnabled" active-text="启用" /></el-form-item></el-form><template #footer><el-button @click="dialog.visible = false">取消</el-button><el-button type="primary" :loading="dialog.saving" @click="save">保存账号</el-button></template></el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { ArrowRight, InfoFilled, Plus, Refresh, UserFilled } from '@element-plus/icons-vue'
import { get, post, put } from '../api'

const users = ref([])
const roles = ref([])
const stores = ref([])
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const dialog = reactive({ visible: false, mode: 'create', saving: false })
const form = reactive(emptyForm())
const pagedUsers = computed(() => users.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([users, pageSize], () => { if ((page.value - 1) * pageSize.value >= users.value.length && page.value > 1) page.value = 1 })
function emptyForm() { return { id: '', username: '', displayName: '', password: '', phoneNumber: '', storeId: '', roleIds: [], isEnabled: true } }
async function load() { loading.value = true; try { [users.value, roles.value, stores.value] = await Promise.all([get('/admin/users'), get('/admin/roles'), get('/admin/stores')]) } catch (error) { ElMessage.error(error.message) } finally { loading.value = false } }
function openCreate() { Object.assign(form, emptyForm()); dialog.mode = 'create'; dialog.visible = true }
function openEdit(row) { Object.assign(form, emptyForm(), { ...row, roleIds: roles.value.filter((role) => row.roles?.includes(role.name)).map((role) => role.id) }); dialog.mode = 'edit'; dialog.visible = true }
async function save() { if (!form.username || !form.displayName || (dialog.mode === 'create' && !form.password)) return ElMessage.warning('请填写账号、姓名和密码'); dialog.saving = true; try { const payload = { ...form, storeId: form.storeId || null }; if (dialog.mode === 'create') await post('/admin/users', payload); else await put(`/admin/users/${form.id}`, payload); ElMessage.success('账号已保存'); dialog.visible = false; await load() } catch (error) { ElMessage.error(error.message) } finally { dialog.saving = false } }
onMounted(load)
</script>
