import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, AuthUser } from '../lib/types'
import { ROLES } from '../lib/types'
import { PageHeader, Modal, StatusPill, useToast } from '../components/ui'

export default function Users() {
  const toast = useToast()
  const [users, setUsers] = useState<AuthUser[]>([])
  const [adding, setAdding] = useState(false)
  const [form, setForm] = useState({ username: '', email: '', password: '', fullName: '', roleName: ROLES.Receptionist as string })

  const load = useCallback(() => { api.get<Paged<AuthUser>>('/api/users?pageSize=100').then(({ data }) => setUsers(data.items)).catch(() => {}) }, [])
  useEffect(() => { load() }, [load])

  async function create(e: React.FormEvent) {
    e.preventDefault()
    try { await api.post('/api/users', form); toast('User created'); setAdding(false); setForm({ username: '', email: '', password: '', fullName: '', roleName: ROLES.Receptionist }); load() }
    catch (err) { toast(errorMessage(err), 'err') }
  }

  return (
    <div>
      <PageHeader title="Users" subtitle="Staff accounts and roles"
        action={<button className="btn btn-primary" onClick={() => setAdding(true)}>+ New user</button>} />
      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr><th className="th">Username</th><th className="th">Full name</th><th className="th">Email</th><th className="th">Role</th><th className="th">Active</th></tr></thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-slate-50">
                <td className="td font-medium">{u.username}</td><td className="td">{u.fullName}</td><td className="td">{u.email}</td>
                <td className="td">{u.role}</td>
                <td className="td"><StatusPill value={u.isActive ? 'Completed' : 'Cancelled'} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal open={adding} onClose={() => setAdding(false)} title="Create user">
        <form onSubmit={create} className="space-y-3">
          <div><label className="label">Username</label><input className="input" required value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} /></div>
          <div><label className="label">Full name</label><input className="input" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></div>
          <div><label className="label">Email</label><input className="input" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div><label className="label">Password</label><input className="input" type="password" required value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></div>
          <div><label className="label">Role</label>
            <select className="input" value={form.roleName} onChange={(e) => setForm({ ...form, roleName: e.target.value })}>
              {Object.values(ROLES).map((r) => <option key={r}>{r}</option>)}
            </select>
          </div>
          <button className="btn btn-primary w-full">Create</button>
        </form>
      </Modal>
    </div>
  )
}
