import axios from 'axios'

// Base URL is empty: Vite proxies /api and /hubs to the backend in dev.
export const api = axios.create({ baseURL: '' })

const TOKEN_KEY = 'hotelos.token'
const REFRESH_KEY = 'hotelos.refresh'

export const tokens = {
  get access() { return localStorage.getItem(TOKEN_KEY) },
  get refresh() { return localStorage.getItem(REFRESH_KEY) },
  set(access: string, refresh: string) {
    localStorage.setItem(TOKEN_KEY, access)
    localStorage.setItem(REFRESH_KEY, refresh)
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
  },
}

// Attach the bearer token to every request.
api.interceptors.request.use((config) => {
  const t = tokens.access
  if (t) config.headers.Authorization = `Bearer ${t}`
  return config
})

// On 401, clear the session and bounce to login.
api.interceptors.response.use(
  (r) => r,
  (error) => {
    if (error?.response?.status === 401 && !location.pathname.startsWith('/login')) {
      tokens.clear()
      location.href = '/login'
    }
    return Promise.reject(error)
  },
)

/** Extracts a human-readable message from an API error response. */
export function errorMessage(e: unknown): string {
  const anyE = e as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } }
  const data = anyE?.response?.data
  if (data?.errors) return Object.values(data.errors).flat().join(' ')
  return data?.message ?? 'Something went wrong.'
}
