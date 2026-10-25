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
        <span class="eyebrow">{{ step === 'login' ? 'WELCOME BACK' : 'FIRST LOGIN' }}</span>
        <h2>{{ step === 'login' ? '登录管理后台' : '设置新密码' }}</h2>
        <p class="form-intro">
          {{ step === 'login' ? '使用你的运营账号继续工作' : '当前为初始密码，请先设置自己的密码后再进入后台' }}
        </p>

        <el-form v-if="step === 'login'" ref="formRef" :model="form" :rules="rules" @submit.prevent="submit">
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
          </div>
          <el-button class="login-submit" type="primary" size="large" :loading="loading" @click="submit">
            进入工作台
          </el-button>
        </el-form>

        <el-form v-else ref="changeFormRef" :model="changeForm" :rules="changeRules" @submit.prevent="submitChange">
          <el-form-item prop="newPassword" label="新密码">
            <el-input
              v-model="changeForm.newPassword"
              size="large"
              type="password"
              show-password
              placeholder="至少 8 位，需包含字母和数字"
              :prefix-icon="Lock"
            />
          </el-form-item>
          <el-form-item prop="confirmPassword" label="确认新密码">
            <el-input
              v-model="changeForm.confirmPassword"
              size="large"
              type="password"
              show-password
              placeholder="再次输入新密码"
              :prefix-icon="Lock"
              @keyup.enter="submitChange"
            />
          </el-form-item>
          <el-button class="login-submit" type="primary" size="large" :loading="changing" @click="submitChange">
            保存并进入
          </el-button>
        </el-form>
        <p class="login-footer">TCPMS 智选旅宿管理系统</p>
      </div>
    </section>
  </main>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Lock, User } from '@element-plus/icons-vue'
import { post, setToken, get } from '../api'

const emit = defineEmits(['success'])
const formRef = ref()
const changeFormRef = ref()
const loading = ref(false)
const changing = ref(false)
const step = ref('login')
const form = reactive({
  username: '',
  password: ''
})
const changeForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const rules = {
  username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}
const changeRules = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (!value || value.length < 8 || value.length > 64) {
          callback(new Error('密码长度需在 8 到 64 位之间'))
          return
        }
        if (!/[A-Za-z]/.test(value) || !/\d/.test(value)) {
          callback(new Error('密码需同时包含字母和数字'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value !== changeForm.newPassword) {
          callback(new Error('两次输入的新密码不一致'))
          return
        }
        callback()
      },
      trigger: 'blur'
    }
  ]
}

async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  loading.value = true
  try {
    const result = await post('/auth/login', form)
    setToken(result.token)
    if (result.mustChangePassword) {
      // 初始密码不允许直接进后台，先完成改密。
      changeForm.currentPassword = form.password
      changeForm.newPassword = ''
      changeForm.confirmPassword = ''
      step.value = 'change'
      ElMessage.warning('当前为初始密码，请先设置新密码')
      return
    }
    const user = await get('/admin/me')
    emit('success', { token: result.token, user })
  } catch (error) {
    ElMessage.error(error.message || '登录失败')
  } finally {
    loading.value = false
  }
}

async function submitChange() {
  const valid = await changeFormRef.value?.validate().catch(() => false)
  if (!valid) return
  changing.value = true
  try {
    const result = await post('/auth/change-password', {
      currentPassword: changeForm.currentPassword,
      newPassword: changeForm.newPassword
    })
    // 改密后后端换发新 token，旧 token 仍带「必须改密」标记。
    setToken(result.token)
    const user = await get('/admin/me')
    ElMessage.success('密码已更新')
    emit('success', { token: result.token, user })
  } catch (error) {
    ElMessage.error(error.message || '修改密码失败')
  } finally {
    changing.value = false
  }
}
</script>
