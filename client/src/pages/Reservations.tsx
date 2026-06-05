import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Reservation, Guest, Room, Bill } from '../lib/types'
import { PageHeader, Modal, StatusPill, useToast } from '../components/ui'

const STATUSES = ['', 'Confirmed', 'CheckedIn', 'CheckedOut', 'Cancelled']

export default function Reservations() {
  const toast = useToast()
  const [items, setItems] = useState<Reservation[]>([])
  const [status, setStatus] = useState('')
  const [guests, setGuests] = useState<Guest[]>([])
  const [types, setTypes] = useState<{ id: number; name: string }[]>([])
  const [adding, setAdding] = useState(false)
  const [bill, setBill] = useState<Bill | null>(null)
  const [pay, setPay] = useState({ method: 'Card', amount: 0 })
  const [form, setForm] = useState({ guestId: '', roomTypeId: 0, checkInDate: '', checkOutDate: '', floorPreference: '', proximityPreference: '' })

  const load = useCallback(() => {
    api.get<Paged<Reservation>>(`/api/reservations?pageSize=50&sortDir=desc${status ? `&status=${status}` : ''}`)
      .then(({ data }) => setItems(data.items)).catch(() => {})
  }, [status])
  useEffect(() => { load() }, [load])

  useEffect(() => {
    api.get<Paged<Guest>>('/api/guests?pageSize=100').then(({ data }) => setGuests(data.items)).catch(() => {})
    api.get<Paged<Room>>('/api/rooms?pageSize=100').then(({ data }) => {
      const map = new Map<number, string>()
      data.items.forEach((r) => map.set(r.roomTypeId, r.type))
      setTypes([...map.entries()].map(([id, name]) => ({ id, name })).sort((a, b) => a.id - b.id))
    }).catch(() => {})
  }, [])

  async function create(e: React.FormEvent) {
    e.preventDefault()
    try {
      await api.post('/api/reservations', {
        guestId: form.guestId, roomTypeId: Number(form.roomTypeId),
        checkInDate: form.checkInDate, checkOutDate: form.checkOutDate,
        floorPreference: form.floorPreference ? Number(form.floorPreference) : null,
        proximityPreference: form.proximityPreference || null,
      })
      toast('Reservation created'); setAdding(false); load()
    } catch (err) { toast(errorMessage(err), 'err') }
  }

  async function checkIn(id: string) {
    try { const { data } = await api.post(`/api/reservations/${id}/checkin`); toast(`Checked in to room ${data.roomNumber}`); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }
  async function checkOut(id: string) {
    try {
      const { data } = await api.post<Bill>(`/api/reservations/${id}/checkout`, { lateCheckout: false, discountPercent: 0 })
      setBill(data); setPay({ method: 'Card', amount: data.balance }); load()
    } catch (e) { toast(errorMessage(e), 'err') }
  }
  async function cancel(id: string) {
    try { await api.post(`/api/reservations/${id}/cancel`); toast('Reservation cancelled'); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }
  async function takePayment() {
    if (!bill) return
    try {
      const { data } = await api.post<Bill>('/api/payments', { billId: bill.id, method: pay.method, amount: Number(pay.amount), reference: null })
      setBill(data); toast('Payment recorded')
    } catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Reservations" subtitle="Bookings, check-in and check-out"
        action={<button className="btn btn-primary" onClick={() => setAdding(true)}>+ New reservation</button>} />

      <select className="input mb-4 w-48" value={status} onChange={(e) => setStatus(e.target.value)}>
        {STATUSES.map((s) => <option key={s} value={s}>{s || 'All statuses'}</option>)}
      </select>

      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr>
            <th className="th">Ref</th><th className="th">Guest</th><th className="th">Type</th><th className="th">Room</th>
            <th className="th">Dates</th><th className="th">Status</th><th className="th">Actions</th>
          </tr></thead>
          <tbody>
            {items.map((r) => (
              <tr key={r.id} className="hover:bg-slate-50">
                <td className="td font-mono text-xs">{r.referenceCode}</td>
                <td className="td font-medium">{r.guestName}</td>
                <td className="td">{r.roomType}</td>
                <td className="td">{r.roomNumber ?? '—'}</td>
                <td className="td text-xs">{r.checkInDate.slice(0, 10)} → {r.checkOutDate.slice(0, 10)}</td>
                <td className="td"><StatusPill value={r.status} /></td>
                <td className="td">
                  <div className="flex gap-2">
                    {(r.status === 'Confirmed' || r.status === 'Pending') && <button className="btn btn-success !px-2 !py-1 text-xs" onClick={() => checkIn(r.id)}>Check in</button>}
                    {r.status === 'CheckedIn' && <button className="btn btn-primary !px-2 !py-1 text-xs" onClick={() => checkOut(r.id)}>Check out</button>}
                    {(r.status === 'Confirmed' || r.status === 'Pending') && <button className="btn btn-danger !px-2 !py-1 text-xs" onClick={() => cancel(r.id)}>Cancel</button>}
                  </div>
                </td>
              </tr>
            ))}
            {items.length === 0 && <tr><td className="td text-slate-400" colSpan={7}>No reservations.</td></tr>}
          </tbody>
        </table>
      </div>

      <Modal open={adding} onClose={() => setAdding(false)} title="New reservation">
        <form onSubmit={create} className="space-y-3">
          <div><label className="label">Guest</label>
            <select className="input" required value={form.guestId} onChange={(e) => setForm({ ...form, guestId: e.target.value })}>
              <option value="">Select guest…</option>
              {guests.map((g) => <option key={g.id} value={g.id}>{g.fullName}</option>)}
            </select>
          </div>
          <div><label className="label">Room type</label>
            <select className="input" required value={form.roomTypeId} onChange={(e) => setForm({ ...form, roomTypeId: Number(e.target.value) })}>
              <option value={0}>Select type…</option>
              {types.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="label">Check-in</label><input className="input" type="date" required value={form.checkInDate} onChange={(e) => setForm({ ...form, checkInDate: e.target.value })} /></div>
            <div><label className="label">Check-out</label><input className="input" type="date" required value={form.checkOutDate} onChange={(e) => setForm({ ...form, checkOutDate: e.target.value })} /></div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="label">Floor pref</label><input className="input" type="number" value={form.floorPreference} onChange={(e) => setForm({ ...form, floorPreference: e.target.value })} /></div>
            <div><label className="label">Proximity</label>
              <select className="input" value={form.proximityPreference} onChange={(e) => setForm({ ...form, proximityPreference: e.target.value })}>
                <option value="">Any</option><option value="elevator">Elevator</option><option value="stairs">Stairs</option>
              </select>
            </div>
          </div>
          <button className="btn btn-primary w-full">Create reservation</button>
        </form>
      </Modal>

      <Modal open={!!bill} onClose={() => setBill(null)} title="Invoice">
        {bill && (
          <div className="space-y-2 text-sm">
            {bill.items.map((i) => (
              <div key={i.id} className="flex justify-between border-b border-slate-100 py-1">
                <span>{i.description}</span><span>£{i.amount.toFixed(2)}</span>
              </div>
            ))}
            <div className="flex justify-between pt-2 font-semibold"><span>Total</span><span>£{bill.total.toFixed(2)}</span></div>
            <div className="flex justify-between"><span>Paid</span><span>£{bill.paid.toFixed(2)}</span></div>
            <div className="flex justify-between text-lg font-bold"><span>Balance</span><span>£{bill.balance.toFixed(2)}</span></div>
            <div className="mt-1"><StatusPill value={bill.status} /></div>
            {bill.balance > 0 && (
              <div className="mt-3 flex items-end gap-2 border-t border-slate-100 pt-3">
                <div className="flex-1"><label className="label">Method</label>
                  <select className="input" value={pay.method} onChange={(e) => setPay({ ...pay, method: e.target.value })}>
                    <option>Card</option><option>Cash</option><option>BankTransfer</option>
                  </select>
                </div>
                <div className="w-28"><label className="label">Amount</label><input className="input" type="number" value={pay.amount} onChange={(e) => setPay({ ...pay, amount: Number(e.target.value) })} /></div>
                <button className="btn btn-success" onClick={takePayment}>Pay</button>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  )
}
