// 默认走同源 /api/v1，开发和预览时由 vite 代理转发到后端；
// 管理后台和 API 分域名部署时用 VITE_API_BASE_URL 覆盖。
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/+$/, '')
const TOKEN_KEY = 'tcpms.admin.token'

export const getVerificationQrUrl = (code) => code
  ? `${API_BASE_URL}/verification-qr/${encodeURIComponent(code)}`
  : ''

const decodeJwtPayload = (token) => {
  try {
    const part = String(token || '').split('.')[1]
    if (!part) return null
    const normalized = part.replace(/-/g, '+').replace(/_/g, '/')
    const base64 = normalized + '='.repeat((4 - (normalized.length % 4)) % 4)
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
        .join('')
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}

// 本地令牌过期后继续发请求只会拿到一串 401，进页面前先判断掉。
export function isTokenExpired(token, skewSeconds = 30) {
  const payload = decodeJwtPayload(token)
  if (!payload || typeof payload.exp !== 'number') return false
  return payload.exp * 1000 <= Date.now() + skewSeconds * 1000
}

export function getToken() {
  const token = localStorage.getItem(TOKEN_KEY) || ''
  // 历史上若有代码把 undefined 写进存储，会变成字面量 "undefined"。清掉，别再带着发请求。
  if (!token || token === 'undefined' || token === 'null') {
    if (token) localStorage.removeItem(TOKEN_KEY)
    return ''
  }
  if (isTokenExpired(token)) {
    localStorage.removeItem(TOKEN_KEY)
    return ''
  }
  return token
}

export function setToken(token) {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY)
}

let unauthorizedTimer = null

function notifyUnauthorized() {
  // 并发请求同时 401 时只广播一次，避免重复清理令牌和重复跳登录页。
  if (unauthorizedTimer) return
  window.dispatchEvent(new CustomEvent('tcpms:unauthorized'))
  unauthorizedTimer = window.setTimeout(() => {
    unauthorizedTimer = null
  }, 1000)
}

export async function request(path, options = {}) {
  const headers = new Headers(options.headers || {})
  headers.set('Content-Type', 'application/json')
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers
  })
  const text = await response.text()
  let body = null
  try {
    body = text ? JSON.parse(text) : null
  } catch {
    body = text
  }

  if (!response.ok) {
    // 令牌失效统一清理，交给 App 层弹出登录页，避免各页面各自处理。
    // 登录接口的 401 表示账号或密码错误，不按“登录失效”处理。
    if (response.status === 401 && !path.startsWith('/auth/login')) {
      clearToken()
      notifyUnauthorized()
    }
    const message = body?.message || body?.title || `请求失败（${response.status}）`
    const error = new Error(message)
    error.status = response.status
    error.code = body?.code
    error.body = body
    throw error
  }
  return body
}

export async function uploadFile(path, file, fieldName = 'file') {
  const formData = new FormData()
  formData.append(fieldName, file)
  const headers = new Headers()
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers,
    body: formData
  })
  const text = await response.text()
  let body = null
  try {
    body = text ? JSON.parse(text) : null
  } catch {
    body = text
  }

  if (!response.ok) {
    if (response.status === 401) {
      clearToken()
      notifyUnauthorized()
    }
    const message = body?.message || body?.title || `上传失败（${response.status}）`
    const error = new Error(message)
    error.status = response.status
    error.code = body?.code
    error.body = body
    throw error
  }
  return body
}

export function get(path) {
  return request(path)
}

export function post(path, body) {
  return request(path, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body)
  })
}

export function put(path, body) {
  return request(path, {
    method: 'PUT',
    body: JSON.stringify(body)
  })
}

export function del(path) {
  return request(path, { method: 'DELETE' })
}

export function money(cents) {
  return `¥${((Number(cents) || 0) / 100).toFixed(2)}`
}

export function formatDate(value) {
  if (!value) return '-'
  return new Date(value).toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}
