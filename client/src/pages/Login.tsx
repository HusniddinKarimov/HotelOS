import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../lib/api'

const DEMO = [
  ['admin', 'Admin@123'], ['manager', 'Password@123'], ['reception', 'Password@123'],
  ['housekeeping', 'Password@123'], ['kitchen', 'Password@123'], ['roomservice', 'Password@123'],
  ['maintenance', 'Password@123'],
]

export default function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('admin')
  const [password, setPassword] = useState('Admin@123')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true); setError('')
    try {
      await login(username, password)
      navigate('/')
    } catch (err) {
      setError(errorMessage(err))
    } finally { setBusy(false) }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-900 p-4">
      <div className="w-full max-w-sm rounded-2xl bg-white p-8 shadow-xl">
        <div className="mb-6 text-center">
          <div className="text-3xl">🏨</div>
          <h1 className="text-2xl font-bold text-slate-800">HotelOS</h1>
          <p className="text-sm text-slate-500">GrandStay Hotel Management</p>
        </div>
        <form onSubmit={submit} className="space-y-4">
          <div>
            <label className="label">Username</label>
            <input className="input" value={username} onChange={(e) => setUsername(e.target.value)} autoFocus />
          </div>
          <div>
            <label className="label">Password</label>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          {error && <p className="text-sm text-rose-600">{error}</p>}
          <button className="btn btn-primary w-full" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
        </form>
        <p className="mt-5 text-center text-sm text-slate-500">
          New here? <Link to="/signup" className="font-semibold text-indigo-600 hover:underline">Create an account</Link>
        </p>
        <div className="mt-6 text-xs text-slate-400">
          <p className="mb-1 font-semibold">Staff demo accounts (click to fill):</p>
          <div className="flex flex-wrap gap-1">
            {DEMO.map(([u, p]) => (
              <button key={u} onClick={() => { setUsername(u); setPassword(p) }}
                className="rounded bg-slate-100 px-2 py-0.5 hover:bg-slate-200">{u}</button>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
