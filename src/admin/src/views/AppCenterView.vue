<template>
  <div class="view-stack compact-view">
    <section class="page-heading">
      <div>
        <p>配置微信支付和高德地图服务。敏感凭证只在服务端读取，后台页面仅展示脱敏状态。</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新状态</el-button>
    </section>

    <el-alert
      title="配置来源：服务端 appsettings.Production.json 或环境变量"
      description="请将商户私钥、API v3 Key、平台证书（或微信支付公钥）和高德服务端 Key 配置在后端，不要放入管理后台前端或小程序包。"
      type="info"
      :closable="false"
      show-icon
    />

    <div class="integration-grid">
      <el-card class="panel-card integration-card" shadow="never">
        <template #header>
          <div class="panel-header">
            <div class="integration-title"><span class="integration-icon wechat"><el-icon><Wallet /></el-icon></span><div><strong>微信支付</strong><span>小程序 JSAPI 支付、回调和退款</span></div></div>
            <el-tag :type="wechatStatus.configured ? 'success' : 'warning'" effect="light">{{ wechatStatus.configured ? '已配置' : '待配置' }}</el-tag>
          </div>
        </template>

        <div class="integration-summary">
          <div><small>小程序 AppID</small><strong>{{ wechatStatus.appId || '未配置' }}</strong></div>
          <div><small>商户号</small><strong>{{ wechatStatus.merchantId || '未配置' }}</strong></div>
          <div><small>证书序列号</small><strong>{{ wechatStatus.merchantSerialNumber || '未配置' }}</strong></div>
          <div><small>回调地址</small><strong class="break-value">{{ wechatStatus.notifyUrl || '未配置' }}</strong></div>
        </div>

        <div class="check-list">
          <div v-for="item in wechatChecks" :key="item.label" class="check-item">
            <el-icon :class="item.ok ? 'check-ok' : 'check-pending'"><component :is="item.ok ? CircleCheck : Warning" /></el-icon>
            <span>{{ item.label }}</span>
            <small>{{ item.ok ? '已就绪' : '待配置' }}</small>
          </div>
        </div>

        <div class="integration-actions">
          <el-button type="primary" :loading="testing === 'wechat'" :icon="Connection" @click="test('wechat')">测试商户连接</el-button>
          <span class="action-note">测试会调用微信支付商户 API，不会创建订单。</span>
        </div>

        <div class="config-keys">
          <strong>需要填写的服务端配置</strong>
          <code>WeChat__MiniProgramAppId</code>
          <code>WeChat__MiniProgramSecret</code>
          <code>WeChat__MerchantId</code>
          <code>WeChat__MerchantSerialNumber</code>
          <code>WeChat__ApiV3Key</code>
          <code>WeChat__MerchantPrivateKeyPath</code>
          <code>WeChat__PlatformCertificatePath</code>
          <code>WeChat__NotifyBaseUrl</code>
        </div>
      </el-card>

      <el-card class="panel-card integration-card" shadow="never">
        <template #header>
          <div class="panel-header">
            <div class="integration-title"><span class="integration-icon amap"><el-icon><Location /></el-icon></span><div><strong>高德地图</strong><span>门店地理编码、逆地理编码和附近门店</span></div></div>
            <el-tag :type="amapStatus.configured ? 'success' : 'warning'" effect="light">{{ amapStatus.configured ? '已配置' : '待配置' }}</el-tag>
          </div>
        </template>

        <div class="integration-summary">
          <div><small>服务端 Key</small><strong>{{ amapStatus.hasServerKey ? '已配置并脱敏' : '未配置' }}</strong></div>
          <div><small>小程序 Key</small><strong>{{ amapStatus.hasMiniProgramKey ? '已配置' : '未配置' }}</strong></div>
          <div><small>附近搜索半径</small><strong>{{ amapStatus.defaultRadiusKm }} 公里</strong></div>
          <div><small>调用能力</small><strong>地址解析 / 逆解析</strong></div>
        </div>

        <div class="check-list">
          <div class="check-item">
            <el-icon :class="amapStatus.hasServerKey ? 'check-ok' : 'check-pending'"><component :is="amapStatus.hasServerKey ? CircleCheck : Warning" /></el-icon>
            <span>高德 Web 服务 API Key</span>
            <small>{{ amapStatus.hasServerKey ? '已就绪' : '待配置' }}</small>
          </div>
          <div class="check-item">
            <el-icon :class="amapStatus.hasMiniProgramKey ? 'check-ok' : 'check-pending'"><component :is="amapStatus.hasMiniProgramKey ? CircleCheck : Warning" /></el-icon>
            <span>微信小程序端 Key</span>
            <small>{{ amapStatus.hasMiniProgramKey ? '已就绪' : '待配置' }}</small>
          </div>
        </div>

        <div class="integration-actions">
          <el-input v-model="amapTestAddress" placeholder="测试地址，例如：上海市黄浦区人民广场" clearable />
          <el-button type="primary" :loading="testing === 'amap'" :icon="Connection" @click="test('amap')">测试地址解析</el-button>
        </div>

        <div class="config-keys">
          <strong>需要填写的服务端配置</strong>
          <code>Amap__ServerKey</code>
          <code>Amap__MiniProgramKey</code>
          <code>Amap__DefaultRadiusKm</code>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { CircleCheck, Connection, Location, Refresh, Wallet, Warning } from '@element-plus/icons-vue'
import { get, post } from '../api'

const loading = ref(false)
const testing = ref('')
const amapTestAddress = ref('上海市黄浦区人民广场')
const wechatStatus = reactive({
  configured: false,
  miniProgramConfigured: false,
  appId: '',
  merchantId: '',
  merchantSerialNumber: '',
  notifyUrl: '',
  hasApiV3Key: false,
  hasMerchantPrivateKey: false,
  hasPlatformCertificate: false,
  hasWeChatPayPublicKey: false
})
const amapStatus = reactive({
  configured: false,
  hasServerKey: false,
  hasMiniProgramKey: false,
  defaultRadiusKm: 50
})
const wechatChecks = computed(() => [
  { label: '小程序登录配置', ok: wechatStatus.miniProgramConfigured },
  { label: 'API v3 Key', ok: wechatStatus.hasApiV3Key },
  { label: '商户私钥', ok: wechatStatus.hasMerchantPrivateKey },
  { label: '平台证书 / 微信支付公钥', ok: wechatStatus.hasPlatformCertificate || wechatStatus.hasWeChatPayPublicKey },
  { label: '支付回调地址', ok: Boolean(wechatStatus.notifyUrl) }
])

async function load() {
  loading.value = true
  try {
    const data = await get('/admin/integrations')
    Object.assign(wechatStatus, data?.wechatPay || {})
    Object.assign(amapStatus, data?.amap || {})
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    loading.value = false
  }
}

async function test(provider) {
  testing.value = provider
  try {
    const query = provider === 'amap' && amapTestAddress.value
      ? `?address=${encodeURIComponent(amapTestAddress.value)}`
      : ''
    const result = await post(`/admin/integrations/${provider}/test${query}`)
    ElMessage.success(result?.message || '测试成功')
  } catch (error) {
    ElMessage.error(error.message)
  } finally {
    testing.value = ''
  }
}

onMounted(load)
</script>

<style scoped>
.integration-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}
.integration-card{min-width:0}
.integration-title{display:flex;align-items:center;gap:10px;min-width:0}
.integration-title>div{display:grid;gap:3px;min-width:0}
.integration-title strong{color:var(--admin-text)}
.integration-title span:not(.integration-icon){color:var(--admin-muted);font-size:12px}
.integration-icon{display:grid;width:38px;height:38px;flex:0 0 38px;place-items:center;border-radius:10px;font-size:19px}
.integration-icon.wechat{background:#e9f8ef;color:#12a05b}
.integration-icon.amap{background:#fff2e7;color:#e67d2e}
.integration-summary{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;padding:14px 0;border-bottom:1px solid var(--admin-border)}
.integration-summary>div{display:grid;gap:4px;min-width:0}
.integration-summary small,.check-item small{color:var(--admin-muted);font-size:11px}
.integration-summary strong{overflow:hidden;color:var(--admin-text);font-size:13px;text-overflow:ellipsis;white-space:nowrap}
.break-value{white-space:normal!important;word-break:break-all}
.check-list{display:grid;gap:9px;padding:14px 0}
.check-item{display:flex;align-items:center;gap:8px;color:var(--admin-text);font-size:13px}
.check-item span{flex:1}
.check-ok{color:#16a26b}
.check-pending{color:#d88a24}
.integration-actions{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.integration-actions :deep(.el-input){min-width:240px;flex:1}
.action-note{color:var(--admin-muted);font-size:11px}
.config-keys{display:grid;gap:6px;margin-top:18px;padding-top:14px;border-top:1px solid var(--admin-border)}
.config-keys strong{color:var(--admin-text);font-size:12px}
.config-keys code{display:block;overflow:auto;padding:5px 7px;border-radius:4px;background:var(--admin-surface-soft);color:var(--admin-muted);font-size:11px;white-space:nowrap}
@media (max-width: 900px){.integration-grid{grid-template-columns:1fr}}
@media (max-width: 560px){.integration-summary{grid-template-columns:1fr}}
</style>
