import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { useRealtime } from '../lib/useRealtime'
import { PageHeader, StatusPill, Modal, useToast } from '../components/ui'
import type { Order, MaintenanceRequest } from '../lib/types'

interface MyRoom { reservationId: string; roomNumber: number; roomType: string; floor: number; status: string; checkInDate: string; checkOutDate: string; nights: number; billId?: string; total: number; paid: boolean }
interface Available { roomId: string; number: number; floor: number; type: string; nightlyRate: number }
interface MenuItem { name: string; price: number }

const today = () => new Date().toISOString().slice(0, 10)
const tomorrow = () => new Date(Date.now() + 86400000).toISOString().slice(0, 10)

export default function MyRoom() {
  const toast = useToast()
  const { user } = useAuth()
  const [room, setRoom] = useState<MyRoom | null>(null)
  const [available, setAvailable] = useState<Available[]>([])
  const [loading, setLoading] = useState(true)

  // booking modal
  const [picked, setPicked] = useState<Available | null>(null)
  const [form, setForm] = useState({ fullName: '', checkIn: today(), checkOut: tomorrow(), card: '' })
  const [paying, setPaying] = useState(false)

  // room service + maintenance
  const [menu, setMenu] = useState<MenuItem[]>([])
  const [qty, setQty] = useState<Record<string, number>>({})
  const [orders, setOrders] = useState<Order[]>([])
  const [issues, setIssues] = useState<MaintenanceRequest[]>([])
  const [issue, setIssue] = useState({ description: '', priority: 'Normal' })

  const loadServices = useCallback(async () => {
    try {
      const [o, i] = await Promise.all([api.get<Order[]>('/api/me/orders'), api.get<MaintenanceRequest[]>('/api/me/issues')])
      setOrders(o.data); setIssues(i.data)
    } catch { /* ignore */ }
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await api.get('/api/me/room')
      if (res.status === 204 || !res.data) {
        setRoom(null)
        const av = await api.get<Available[]>('/api/me/available-rooms')
        setAvailable(av.data)
      } else {
        setRoom(res.data)
        if (menu.length === 0) api.get<MenuItem[]>('/api/me/menu').then((m) => setMenu(m.data)).catch(() => {})
        loadServices()
      }
    } catch (e) { toast(errorMessage(e), 'err') } finally { setLoading(false) }
  }, [toast, menu.length, loadServices])
  useEffect(() => { load() }, [load])

  // Live: kitchen/maintenance updates refresh the guest's lists.
  useRealtime({ onActivity: () => { if (room) { loadServices(); api.get('/api/me/room').then((r) => r.data && setRoom(r.data)).catch(() => {}) } } })

  function openBooking(r: Available) {
    setPicked(r)
    setForm({ fullName: user?.fullName ?? '', checkIn: today(), checkOut: tomorrow(), card: '' })
  }
  const nights = Math.max(1, Math.round((new Date(form.checkOut).getTime() - new Date(form.checkIn).getTime()) / 86400000))
  const total = picked ? nights * picked.nightlyRate : 0

  async function payAndBook(e: React.FormEvent) {
    e.preventDefault(); if (!picked) return
    setPaying(true)
    try {
      await api.post('/api/me/book', { roomId: picked.roomId, checkInDate: form.checkIn, checkOutDate: form.checkOut, fullName: form.fullName, cardNumber: form.card })
      toast(`Paid £${total.toFixed(2)} — room ${picked.number} booked!`); setPicked(null); load()
    } catch (e) { toast(errorMessage(e), 'err') } finally { setPaying(false) }
  }
  async function leave() {
    try { await api.post('/api/me/leave'); toast('Checked out. Thank you!'); load() } catch (e) { toast(errorMessage(e), 'err') }
  }
  async function placeOrder() {
    const items = menu.filter((m) => (qty[m.name] ?? 0) > 0).map((m) => ({ name: m.name, quantity: qty[m.name] }))
    if (!items.length) return toast('Select at least one item', 'err')
    try { await api.post('/api/me/orders', { items }); toast('Order placed — on its way!'); setQty({}); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }
  async function reportIssue() {
    if (!issue.description.trim()) return toast('Describe the problem', 'err')
    try { await api.post('/api/me/issues', issue); toast('Reported — maintenance notified'); setIssue({ description: '', priority: 'Normal' }); loadServices() }
    catch (e) { toast(errorMessage(e), 'err') }
  }

  if (loading) return <div className="card text-slate-400">Loading…</div>

  // ===== The user already has a room =====
  if (room) {
    return (
      <div>
        <PageHeader title="My Room" subtitle="Your stay — order service or report a problem" />
        <div className="grid gap-5 lg:grid-cols-3">
          {/* Room + leave */}
          <div className="card">
            <div className="flex items-center justify-between">
              <span className="text-3xl font-bold text-slate-800">Room {room.roomNumber}</span>
              <StatusPill value={room.status} />
            </div>
            <div className="mt-1 text-slate-500">{room.roomType} · Floor {room.floor}</div>
            <div className="mt-3 space-y-1 text-sm text-slate-600">
              <div>{room.checkInDate.slice(0, 10)} → {room.checkOutDate.slice(0, 10)} · {room.nights} night(s)</div>
              <div>Total: <span className="font-semibold">£{room.total.toFixed(2)}</span> {room.paid && <span className="badge bg-emerald-100 text-emerald-700 ml-1">Paid</span>}</div>
            </div>
            <button className="btn btn-danger mt-4 w-full" onClick={leave}>Leave room (check out)</button>
          </div>

          {/* Room service */}
          <div className="card">
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">🛎️ Room Service</h3>
            <div className="space-y-2">
              {menu.map((m) => (
                <div key={m.name} className="flex items-center justify-between">
                  <span className="text-sm">{m.name} <span className="text-slate-400">£{m.price.toFixed(2)}</span></span>
                  <input className="input w-16 px-2 py-1" type="number" min={0} value={qty[m.name] ?? 0} onChange={(e) => setQty({ ...qty, [m.name]: Number(e.target.value) })} />
                </div>
              ))}
            </div>
            <button className="btn btn-primary mt-3 w-full" onClick={placeOrder}>Place order</button>
            <div className="mt-4">
              <div className="mb-1 text-xs font-semibold text-slate-400">YOUR ORDERS</div>
              {orders.length === 0 ? <p className="text-sm text-slate-400">No active orders.</p> : orders.map((o) => (
                <div key={o.id} className="mb-1 flex items-center justify-between text-sm">
                  <span>{o.items.map((i) => `${i.quantity}× ${i.name}`).join(', ')}</span>
                  <StatusPill value={o.status} />
                </div>
              ))}
            </div>
          </div>

          {/* Maintenance */}
          <div className="card">
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">🔧 Report a Problem</h3>
            <label className="label">What's wrong?</label>
            <input className="input" placeholder="e.g. Air conditioning not working" value={issue.description} onChange={(e) => setIssue({ ...issue, description: e.target.value })} />
            <label className="label mt-2">Urgency</label>
            <select className="input" value={issue.priority} onChange={(e) => setIssue({ ...issue, priority: e.target.value })}>
              {['Low', 'Normal', 'High', 'Critical'].map((p) => <option key={p}>{p}</option>)}
            </select>
            <button className="btn mt-3 w-full" style={{ background: '#e11d48', color: '#fff' }} onClick={reportIssue}>Send to maintenance</button>
            <div className="mt-4">
              <div className="mb-1 text-xs font-semibold text-slate-400">YOUR REPORTS</div>
              {issues.length === 0 ? <p className="text-sm text-slate-400">No reports yet.</p> : issues.map((m) => (
                <div key={m.id} className="mb-1 flex items-center justify-between text-sm">
                  <span>{m.description}</span>
                  <span className="flex gap-1"><StatusPill value={m.priority} /><StatusPill value={m.status} /></span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    )
  }

  // ===== No room yet: choose a room, then book + pay =====
  return (
    <div>
      <PageHeader title="Book a Room" subtitle="Pick a room, choose your dates, and pay to confirm" />
      {available.length === 0 ? (
        <div className="card text-slate-400">No rooms available right now. Please check back shortly.</div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {available.map((r) => (
            <div key={r.roomId} className="card">
              <div className="text-xl font-bold text-slate-800">Room {r.number}</div>
              <div className="text-xs text-slate-500">{r.type} · Floor {r.floor}</div>
              <div className="mt-2 text-lg font-semibold text-emerald-600">£{r.nightlyRate.toFixed(2)}<span className="text-xs font-normal text-slate-400">/night</span></div>
              <button className="btn btn-primary mt-3 w-full" onClick={() => openBooking(r)}>Book</button>
            </div>
          ))}
        </div>
      )}

      <Modal open={!!picked} onClose={() => setPicked(null)} title={`Book Room ${picked?.number}`}>
        {picked && (
          <form onSubmit={payAndBook} className="space-y-3">
            <div className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">{picked.type} · Floor {picked.floor} · £{picked.nightlyRate.toFixed(2)}/night</div>
            <div><label className="label">Full name (on card)</label><input className="input" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} placeholder="e.g. Alice Smith" /></div>
            <div className="grid grid-cols-2 gap-3">
              <div><label className="label">Check-in</label><input className="input" type="date" required min={today()} value={form.checkIn} onChange={(e) => setForm({ ...form, checkIn: e.target.value })} /></div>
              <div><label className="label">Check-out</label><input className="input" type="date" required min={form.checkIn} value={form.checkOut} onChange={(e) => setForm({ ...form, checkOut: e.target.value })} /></div>
            </div>
            <div><label className="label">Card number</label><input className="input" required inputMode="numeric" placeholder="4111 1111 1111 1111" value={form.card} onChange={(e) => setForm({ ...form, card: e.target.value })} /></div>
            <div className="flex items-center justify-between rounded-lg bg-indigo-50 px-3 py-2">
              <span className="text-sm text-slate-600">{nights} night(s) × £{picked.nightlyRate.toFixed(2)}</span>
              <span className="text-xl font-bold text-indigo-700">£{total.toFixed(2)}</span>
            </div>
            <button className="btn btn-success w-full" disabled={paying}>{paying ? 'Processing…' : `Pay £${total.toFixed(2)} & Book`}</button>
          </form>
        )}
      </Modal>
    </div>
  )
}
