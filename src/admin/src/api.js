const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5180/api/v1'
const TOKEN_KEY = 'tcpms.admin.token'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY) || ''
}

export function setToken(token) {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY)
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
    const message = body?.message || body?.title || `请求失败（${response.status}）`
    const error = new Error(message)
    error.status = response.status
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
