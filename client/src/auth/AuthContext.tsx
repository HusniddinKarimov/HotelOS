import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, tokens } from '../lib/api'
import type { AuthResponse, AuthUser } from '../lib/types'

interface AuthState {
  user: AuthUser | null
  login: (username: string, password: string) => Promise<void>
  logout: () => void
  hasRole: (...roles: string[]) => boolean
}

const AuthContext = createContext<AuthState>(null!)
const USER_KEY = 'hotelos.user'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as AuthUser) : null
  })

  // If we have a token but no user (e.g. refresh), fetch identity.
  useEffect(() => {
    if (tokens.access && !user) {
      api.get('/api/auth/me')
        .then(({ data }) => setUser({ id: data.id, username: data.username, role: data.role, email: '', fullName: data.username, isActive: true }))
        .catch(() => {})
    }
  }, [user])

  async function login(username: string, password: string) {
    const { data } = await api.post<AuthResponse>('/api/auth/login', { username, password })
    tokens.set(data.accessToken, data.refreshToken)
    localStorage.setItem(USER_KEY, JSON.stringify(data.user))
    setUser(data.user)
  }

  function logout() {
    tokens.clear()
    localStorage.removeItem(USER_KEY)
    setUser(null)
    location.href = '/login'
  }

  const hasRole = (...roles: string[]) => !!user && roles.includes(user.role)

  return <AuthContext.Provider value={{ user, login, logout, hasRole }}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
