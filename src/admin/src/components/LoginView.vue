<template>
  <main class="login-page">
    <section class="login-brand-panel">
      <div class="brand-mark">T</div>
      <span class="eyebrow">TCPMS · SERENE STAY</span>
      <h1>智选旅宿运营中台</h1>
      <p>统一管理门店、房源、库存、订单与退款，让每一次入住都清晰可控。</p>
      <div class="brand-points">
        <span><i></i> 多门店经营总览</span>
        <span><i></i> 房态库存实时协同</span>
        <span><i></i> 高德地图位置管理</span>
      </div>
    </section>
    <section class="login-form-panel">
      <div class="login-form-wrap">
        <span class="eyebrow">WELCOME BACK</span>
        <h2>登录管理后台</h2>
        <p class="form-intro">使用你的运营账号继续工作</p>
        <el-form ref="formRef" :model="form" :rules="rules" @submit.prevent="submit">
          <el-form-item prop="username" label="账号">
            <el-input v-model="form.username" size="large" placeholder="请输入账号" :prefix-icon="User" />
          </el-form-item>
          <el-form-item prop="password" label="密码">
            <el-input
              v-model="form.password"
              size="large"
              type="password"
              show-password
              placeholder="请输入密码"
              :prefix-icon="Lock"
              @keyup.enter="submit"
            />
          </el-form-item>
          <div class="login-meta">
            <span class="secure-note"><span class="status-dot"></span> 安全登录 · 权限按角色控制</span>
            <button type="button" class="text-button" @click="showDemo">查看演示账号</button>
          </div>
          <el-button class="login-submit" type="primary" size="large" :loading="loading" @click="submit">
            进入工作台
          </el-button>
        </el-form>
        <p class="login-footer">TCPMS 智选旅宿管理系统 · 开发环境</p>
      </div>
    </section>
  </main>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { ElMessage, ElNotification } from 'element-plus'
import { Lock, User } from '@element-plus/icons-vue'
import { post, setToken, get } from '../api'

const emit = defineEmits(['success'])
const formRef = ref()
const loading = ref(false)
const form = reactive({
  username: 'admin',
  password: 'Admin@123456'
})
const rules = {
  username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  loading.value = true
  try {
    const token = await post('/auth/login', form)
    setToken(token.token)
    const user = await get('/admin/me')
    emit('success', { token: token.token, user })
  } catch (error) {
    ElMessage.error(error.message || '登录失败')
  } finally {
    loading.value = false
  }
}

function showDemo() {
  ElNotification({
    title: '开发环境账号',
    message: '总部管理员：admin / Admin@123456。其他演示角色使用相同密码。',
    type: 'info',
    duration: 5000
  })
}
</script>
