import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, MaintenanceRequest } from '../lib/types'
import { PageHeader, StatusPill, Modal, useToast } from '../components/ui'
import { useRealtime } from '../lib/useRealtime'

export default function Maintenance() {
  const toast = useToast()
  const [items, setItems] = useState<MaintenanceRequest[]>([])
  const [techs, setTechs] = useState<{ id: string; fullName: string }[]>([])
  const [adding, setAdding] = useState(false)
  const [form, setForm] = useState({ roomNumber: '', description: '', priority: 'Normal' })
  const [assignFor, setAssignFor] = useState<MaintenanceRequest | null>(null)
  const [techId, setTechId] = useState('')

  const load = useCallback(() => {
    api.get<Paged<MaintenanceRequest>>('/api/maintenance?pageSize=50').then(({ data }) => setItems(data.items)).catch(() => {})
  }, [])
  useEffect(() => { load(); api.get('/api/maintenance/technicians').then(({ data }) => setTechs(data)).catch(() => {}) }, [load])
  useRealtime({ onActivity: () => load(), onNotification: () => load() })

  async function create(e: React.FormEvent) {
    e.preventDefault()
    try { await api.post('/api/maintenance', { ...form, roomNumber: Number(form.roomNumber) }); toast('Request logged'); setAdding(false); setForm({ roomNumber: '', description: '', priority: 'Normal' }); load() }
    catch (err) { toast(errorMessage(err), 'err') }
  }
  async function assign() {
    if (!assignFor || !techId) return
    try { await api.post(`/api/maintenance/${assignFor.id}/assign`, { technicianUserId: techId }); toast('Assigned'); setAssignFor(null); setTechId(''); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }
  async function resolve(id: string) {
    try { await api.post(`/api/maintenance/${id}/resolve`); toast('Resolved'); load() } catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Maintenance" subtitle="Priority queue — Critical first"
        action={<button className="btn btn-primary" onClick={() => setAdding(true)}>+ Report issue</button>} />

      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr><th className="th">Priority</th><th className="th">Room</th><th className="th">Description</th><th className="th">Status</th><th className="th">Technician</th><th className="th">Actions</th></tr></thead>
          <tbody>
            {items.map((m) => (
              <tr key={m.id} className="hover:bg-slate-50">
                <td className="td"><StatusPill value={m.priority} /></td>
                <td className="td font-medium">{m.roomNumber}</td>
                <td className="td">{m.description}</td>
                <td className="td"><StatusPill value={m.status} /></td>
                <td className="td">{m.assignedToName ?? '—'}</td>
                <td className="td">
                  <div className="flex gap-2">
                    {m.status !== 'Completed' && m.status === 'Open' && <button className="btn btn-ghost !px-2 !py-1 text-xs" onClick={() => setAssignFor(m)}>Assign</button>}
                    {m.status !== 'Completed' && <button className="btn btn-success !px-2 !py-1 text-xs" onClick={() => resolve(m.id)}>Resolve</button>}
                  </div>
                </td>
              </tr>
            ))}
            {items.length === 0 && <tr><td className="td text-slate-400" colSpan={6}>No open requests. 🎉</td></tr>}
          </tbody>
        </table>
      </div>

      <Modal open={adding} onClose={() => setAdding(false)} title="Report maintenance issue">
        <form onSubmit={create} className="space-y-3">
          <div><label className="label">Room number</label><input className="input" type="number" required value={form.roomNumber} onChange={(e) => setForm({ ...form, roomNumber: e.target.value })} /></div>
          <div><label className="label">Description</label><input className="input" required value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
          <div><label className="label">Priority</label>
            <select className="input" value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}>
              {['Critical', 'High', 'Normal', 'Low'].map((p) => <option key={p}>{p}</option>)}
            </select>
          </div>
          <button className="btn btn-primary w-full">Submit</button>
        </form>
      </Modal>

      <Modal open={!!assignFor} onClose={() => setAssignFor(null)} title={`Assign room ${assignFor?.roomNumber}`}>
        <label className="label">Technician</label>
        <select className="input" value={techId} onChange={(e) => setTechId(e.target.value)}>
          <option value="">Select technician…</option>
          {techs.map((t) => <option key={t.id} value={t.id}>{t.fullName}</option>)}
        </select>
        <button className="btn btn-primary mt-4 w-full" onClick={assign}>Assign</button>
      </Modal>
    </div>
  )
}
