import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../lib/api'

export default function SignUp() {
  const { signup } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ fullName: '', username: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true); setError('')
    try {
      await signup(form)
      navigate('/my-room')   // new users are basic Users -> their room/booking page
    } catch (err) {
      setError(errorMessage(err))
    } finally { setBusy(false) }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-900 p-4">
      <div className="w-full max-w-sm rounded-2xl bg-white p-8 shadow-xl">
        <div className="mb-6 text-center">
          <div className="text-3xl">🏨</div>
          <h1 className="text-2xl font-bold text-slate-800">Create your account</h1>
          <p className="text-sm text-slate-500">Sign up to book a room at GrandStay</p>
        </div>
        <form onSubmit={submit} className="space-y-4">
          <div><label className="label">Full name</label><input className="input" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} autoFocus /></div>
          <div><label className="label">Username</label><input className="input" required value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} /></div>
          <div><label className="label">Email</label><input className="input" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div><label className="label">Password</label><input className="input" type="password" required value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></div>
          {error && <p className="text-sm text-rose-600">{error}</p>}
          <button className="btn btn-primary w-full" disabled={busy}>{busy ? 'Creating…' : 'Sign up'}</button>
        </form>
        <p className="mt-6 text-center text-sm text-slate-500">
          Already have an account? <Link to="/login" className="font-semibold text-indigo-600 hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
