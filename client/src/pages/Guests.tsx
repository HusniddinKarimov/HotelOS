import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Guest } from '../lib/types'
import { ROLES } from '../lib/types'
import { useAuth } from '../auth/AuthContext'
import { PageHeader, Modal, StatusPill, useToast } from '../components/ui'

interface GuestDetail extends Guest {
  history: { id: string; referenceCode: string; roomType: string; roomNumber: number | null; checkInDate: string; checkOutDate: string; status: string }[]
}

export default function Guests() {
  const toast = useToast()
  const { hasRole } = useAuth()
  const isAdmin = hasRole(ROLES.Administrator)
  const [guests, setGuests] = useState<Guest[]>([])
  const [search, setSearch] = useState('')
  const [adding, setAdding] = useState(false)
  const [detail, setDetail] = useState<GuestDetail | null>(null)
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', nationality: '', passportNumber: '' })

  const load = useCallback(() => {
    api.get<Paged<Guest>>(`/api/guests?pageSize=50${search ? `&search=${encodeURIComponent(search)}` : ''}`)
      .then(({ data }) => setGuests(data.items)).catch(() => {})
  }, [search])
  useEffect(() => { const t = setTimeout(load, 250); return () => clearTimeout(t) }, [load])

  async function create(e: React.FormEvent) {
    e.preventDefault()
    try {
      await api.post('/api/guests', form)
      toast('Guest registered'); setAdding(false)
      setForm({ fullName: '', email: '', phone: '', nationality: '', passportNumber: '' }); load()
    } catch (err) { toast(errorMessage(err), 'err') }
  }

  async function remove(g: Guest) {
    if (!confirm(`Delete guest "${g.fullName}"? This cannot be undone.`)) return
    try { await api.delete(`/api/guests/${g.id}`); toast('Guest deleted'); load() }
    catch (err) { toast(errorMessage(err), 'err') }
  }

  return (
    <div>
      <PageHeader title="Guests" subtitle="Register and search guests"
        action={<button className="btn btn-primary" onClick={() => setAdding(true)}>+ New guest</button>} />

      <input className="input mb-4 max-w-sm" placeholder="Search name, email or phone…" value={search} onChange={(e) => setSearch(e.target.value)} />

      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr><th className="th">Name</th><th className="th">Email</th><th className="th">Phone</th><th className="th">Nationality</th><th className="th"></th></tr></thead>
          <tbody>
            {guests.map((g) => (
              <tr key={g.id} className="hover:bg-slate-50">
                <td className="td font-medium">{g.fullName}</td>
                <td className="td">{g.email}</td>
                <td className="td">{g.phone}</td>
                <td className="td">{g.nationality ?? '—'}</td>
                <td className="td text-right">
                  <button className="text-indigo-600 hover:underline" onClick={() => api.get(`/api/guests/${g.id}`).then(({ data }) => setDetail(data))}>History</button>
                  {isAdmin && <button className="ml-3 text-rose-600 hover:underline" onClick={() => remove(g)}>Delete</button>}
                </td>
              </tr>
            ))}
            {guests.length === 0 && <tr><td className="td text-slate-400" colSpan={5}>No guests found.</td></tr>}
          </tbody>
        </table>
      </div>

      <Modal open={adding} onClose={() => setAdding(false)} title="Register guest">
        <form onSubmit={create} className="space-y-3">
          <div><label className="label">Full name</label><input className="input" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></div>
          <div><label className="label">Email</label><input className="input" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div><label className="label">Phone</label><input className="input" required value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="label">Nationality</label><input className="input" value={form.nationality} onChange={(e) => setForm({ ...form, nationality: e.target.value })} /></div>
            <div><label className="label">Passport</label><input className="input" value={form.passportNumber} onChange={(e) => setForm({ ...form, passportNumber: e.target.value })} /></div>
          </div>
          <button className="btn btn-primary w-full">Register</button>
        </form>
      </Modal>

      <Modal open={!!detail} onClose={() => setDetail(null)} title={detail?.fullName ?? ''}>
        <div className="space-y-1 text-sm text-slate-600">
          <div>{detail?.email} · {detail?.phone}</div>
          <div className="mt-3 font-semibold text-slate-700">Reservation history</div>
          {detail?.history.length ? detail.history.map((h) => (
            <div key={h.id} className="flex items-center justify-between border-b border-slate-100 py-1">
              <span>{h.referenceCode} · {h.roomType}{h.roomNumber ? ` (#${h.roomNumber})` : ''}</span>
              <StatusPill value={h.status} />
            </div>
          )) : <p className="text-slate-400">No reservations yet.</p>}
        </div>
      </Modal>
    </div>
  )
}
