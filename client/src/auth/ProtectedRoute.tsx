import { Navigate } from 'react-router-dom'
import { tokens } from '../lib/api'
import type { ReactNode } from 'react'

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  if (!tokens.access) return <Navigate to="/login" replace />
  return <>{children}</>
}
