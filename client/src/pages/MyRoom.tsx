import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { useRealtime } from '../lib/useRealtime'
import { PageHeader, StatusPill, Modal, useToast } from '../components/ui'
import type { Order, MaintenanceRequest } from '../lib/types'

interface MyRoom { reservationId: string; roomNumber: number; roomType: string; floor: number; status: string; checkInDate: string; checkOutDate: string; nights: number; billId?: string; total: number; paid: boolean }
interface Available { roomId: string; number: number; floor: number; type: string; nightlyRate: number; nights: number; total: number }
interface Booking { reservationId: string; referenceCode: string; roomNumber: number | null; roomType: string; checkInDate: string; checkOutDate: string; nights: number; status: string; total: number; paid: boolean; canCheckIn: boolean }
interface MenuItem { name: string; price: number }

const today = () => new Date().toISOString().slice(0, 10)
const plus = (n: number) => new Date(Date.now() + n * 86400000).toISOString().slice(0, 10)
const fmt = (s: string) => s.slice(0, 10)

export default function MyRoom() {
  const toast = useToast()
  const { user } = useAuth()
  const [room, setRoom] = useState<MyRoom | null>(null)
  const [bookings, setBookings] = useState<Booking[]>([])
  const [loading, setLoading] = useState(true)

  // search + booking
  const [dates, setDates] = useState({ checkIn: today(), checkOut: plus(1) })
  const [available, setAvailable] = useState<Available[] | null>(null)
  const [searching, setSearching] = useState(false)
  const [picked, setPicked] = useState<Available | null>(null)
  const [card, setCard] = useState('')
  const [fullName, setFullName] = useState('')
  const [paying, setPaying] = useState(false)

  // current-stay services
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
        const b = await api.get<Booking[]>('/api/me/bookings'); setBookings(b.data)
      } else {
        setRoom(res.data)
        if (menu.length === 0) api.get<MenuItem[]>('/api/me/menu').then((m) => setMenu(m.data)).catch(() => {})
        loadServices()
      }
    } catch (e) { toast(errorMessage(e), 'err') } finally { setLoading(false) }
  }, [toast, menu.length, loadServices])
  useEffect(() => { load() }, [load])
  useRealtime({ onActivity: () => { if (room) { loadServices(); api.get('/api/me/room').then((r) => r.data && setRoom(r.data)).catch(() => {}) } } })

  async function search() {
    setSearching(true); setAvailable(null)
    try {
      const { data } = await api.get<Available[]>(`/api/me/available-rooms?checkIn=${dates.checkIn}&checkOut=${dates.checkOut}`)
      setAvailable(data)
    } catch (e) { toast(errorMessage(e), 'err') } finally { setSearching(false) }
  }
  function openBook(r: Available) { setPicked(r); setFullName(user?.fullName ?? ''); setCard('') }
  async function payAndBook(e: React.FormEvent) {
    e.preventDefault(); if (!picked) return
    setPaying(true)
    try {
      await api.post('/api/me/book', { roomId: picked.roomId, checkIn: dates.checkIn, checkOut: dates.checkOut, fullName, cardNumber: card })
      toast(`Booked room ${picked.number} — £${picked.total.toFixed(2)} paid`); setPicked(null); setAvailable(null); load()
    } catch (e) { toast(errorMessage(e), 'err') } finally { setPaying(false) }
  }
  async function checkIn(id: string) { try { await api.post(`/api/me/bookings/${id}/checkin`); toast('Checked in — enjoy your stay!'); load() } catch (e) { toast(errorMessage(e), 'err') } }
  async function cancel(id: string) { if (!confirm('Cancel this booking?')) return; try { await api.post(`/api/me/bookings/${id}/cancel`); toast('Booking cancelled'); load() } catch (e) { toast(errorMessage(e), 'err') } }
  async function leave() { try { await api.post('/api/me/leave'); toast('Checked out. Thank you!'); load() } catch (e) { toast(errorMessage(e), 'err') } }
  async function placeOrder() {
    const items = menu.filter((m) => (qty[m.name] ?? 0) > 0).map((m) => ({ name: m.name, quantity: qty[m.name] }))
    if (!items.length) return toast('Select at least one item', 'err')
    try { await api.post('/api/me/orders', { items }); toast('Order placed!'); setQty({}); load() } catch (e) { toast(errorMessage(e), 'err') }
  }
  async function reportIssue() {
    if (!issue.description.trim()) return toast('Describe the problem', 'err')
    try { await api.post('/api/me/issues', issue); toast('Reported — maintenance notified'); setIssue({ description: '', priority: 'Normal' }); loadServices() } catch (e) { toast(errorMessage(e), 'err') }
  }

  if (loading) return <div className="card text-slate-400">Loading…</div>

  // ===== Currently checked in =====
  if (room) {
    return (
      <div>
        <PageHeader title="My Stay" subtitle="You're checked in — order service or report a problem" />
        <div className="grid gap-5 lg:grid-cols-3">
          <div className="card">
            <div className="flex items-center justify-between">
              <span className="text-3xl font-bold text-slate-800">Room {room.roomNumber}</span>
              <StatusPill value={room.status} />
            </div>
            <div className="mt-1 text-slate-500">{room.roomType} · Floor {room.floor}</div>
            <div className="mt-3 space-y-1 text-sm text-slate-600">
              <div>{fmt(room.checkInDate)} → {fmt(room.checkOutDate)} · {room.nights} night(s)</div>
              <div>Total: <span className="font-semibold">£{room.total.toFixed(2)}</span> {room.paid && <span className="badge bg-emerald-100 text-emerald-700 ml-1">Paid</span>}</div>
            </div>
            <button className="btn btn-danger mt-4 w-full" onClick={leave}>Check out</button>
          </div>

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
            <div className="mt-4"><div className="mb-1 text-xs font-semibold text-slate-400">YOUR ORDERS</div>
              {orders.length === 0 ? <p className="text-sm text-slate-400">No active orders.</p> : orders.map((o) => (
                <div key={o.id} className="mb-1 flex items-center justify-between text-sm"><span>{o.items.map((i) => `${i.quantity}× ${i.name}`).join(', ')}</span><StatusPill value={o.status} /></div>
              ))}
            </div>
          </div>

          <div className="card">
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">🔧 Report a Problem</h3>
            <label className="label">What's wrong?</label>
            <input className="input" placeholder="e.g. Air conditioning not working" value={issue.description} onChange={(e) => setIssue({ ...issue, description: e.target.value })} />
            <label className="label mt-2">Urgency</label>
            <select className="input" value={issue.priority} onChange={(e) => setIssue({ ...issue, priority: e.target.value })}>{['Low', 'Normal', 'High', 'Critical'].map((p) => <option key={p}>{p}</option>)}</select>
            <button className="btn mt-3 w-full" style={{ background: '#e11d48', color: '#fff' }} onClick={reportIssue}>Send to maintenance</button>
            <div className="mt-4"><div className="mb-1 text-xs font-semibold text-slate-400">YOUR REPORTS</div>
              {issues.length === 0 ? <p className="text-sm text-slate-400">No reports yet.</p> : issues.map((m) => (
                <div key={m.id} className="mb-1 flex items-center justify-between text-sm"><span>{m.description}</span><span className="flex gap-1"><StatusPill value={m.priority} /><StatusPill value={m.status} /></span></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    )
  }

  // ===== No current stay: search + book, and manage bookings =====
  return (
    <div>
      <PageHeader title="Book a Stay" subtitle="Choose your dates and find an available room" />

      {/* Date search */}
      <div className="card mb-5">
        <div className="flex flex-wrap items-end gap-3">
          <div><label className="label">Check-in</label><input className="input" type="date" min={today()} value={dates.checkIn} onChange={(e) => setDates({ ...dates, checkIn: e.target.value })} /></div>
          <div><label className="label">Check-out</label><input className="input" type="date" min={dates.checkIn} value={dates.checkOut} onChange={(e) => setDates({ ...dates, checkOut: e.target.value })} /></div>
          <button className="btn btn-primary" onClick={search} disabled={searching}>{searching ? 'Searching…' : 'Search rooms'}</button>
        </div>
      </div>

      {/* Results */}
      {available && (
        available.length === 0 ? <div className="card text-slate-400">No rooms available for those dates. Try different dates.</div> : (
          <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
            {available.map((r) => (
              <div key={r.roomId} className="card">
                <div className="text-xl font-bold text-slate-800">Room {r.number}</div>
                <div className="text-xs text-slate-500">{r.type} · Floor {r.floor}</div>
                <div className="mt-2 text-lg font-semibold text-emerald-600">£{r.total.toFixed(2)}</div>
                <div className="text-xs text-slate-400">{r.nights} night(s) · £{r.nightlyRate.toFixed(2)}/night</div>
                <button className="btn btn-primary mt-3 w-full" onClick={() => openBook(r)}>Book</button>
              </div>
            ))}
          </div>
        )
      )}

      {/* My bookings */}
      <h2 className="mb-2 mt-2 text-lg font-bold text-slate-700">My bookings</h2>
      {bookings.length === 0 ? <div className="card text-slate-400">You have no bookings yet.</div> : (
        <div className="card overflow-x-auto p-0">
          <table className="w-full">
            <thead><tr><th className="th">Ref</th><th className="th">Room</th><th className="th">Dates</th><th className="th">Total</th><th className="th">Status</th><th className="th"></th></tr></thead>
            <tbody>
              {bookings.map((b) => (
                <tr key={b.reservationId} className="hover:bg-slate-50">
                  <td className="td font-mono text-xs">{b.referenceCode}</td>
                  <td className="td">{b.roomNumber ?? '—'} · {b.roomType}</td>
                  <td className="td text-xs">{fmt(b.checkInDate)} → {fmt(b.checkOutDate)} ({b.nights}n)</td>
                  <td className="td">£{b.total.toFixed(2)}</td>
                  <td className="td"><StatusPill value={b.status} /></td>
                  <td className="td">
                    <div className="flex gap-2">
                      {b.canCheckIn && <button className="btn btn-success !px-2 !py-1 text-xs" onClick={() => checkIn(b.reservationId)}>Check in</button>}
                      {b.status === 'Confirmed' && <button className="btn btn-danger !px-2 !py-1 text-xs" onClick={() => cancel(b.reservationId)}>Cancel</button>}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Booking modal */}
      <Modal open={!!picked} onClose={() => setPicked(null)} title={`Book Room ${picked?.number}`}>
        {picked && (
          <form onSubmit={payAndBook} className="space-y-3">
            <div className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">{picked.type} · Floor {picked.floor} · {fmt(dates.checkIn)} → {fmt(dates.checkOut)}</div>
            <div><label className="label">Full name (on card)</label><input className="input" required value={fullName} onChange={(e) => setFullName(e.target.value)} /></div>
            <div><label className="label">Card number</label><input className="input" required inputMode="numeric" placeholder="4111 1111 1111 1111" value={card} onChange={(e) => setCard(e.target.value)} /></div>
            <div className="flex items-center justify-between rounded-lg bg-indigo-50 px-3 py-2">
              <span className="text-sm text-slate-600">{picked.nights} night(s) × £{picked.nightlyRate.toFixed(2)}</span>
              <span className="text-xl font-bold text-indigo-700">£{picked.total.toFixed(2)}</span>
            </div>
            <button className="btn btn-success w-full" disabled={paying}>{paying ? 'Processing…' : `Pay £${picked.total.toFixed(2)} & Book`}</button>
          </form>
        )}
      </Modal>
    </div>
  )
}
