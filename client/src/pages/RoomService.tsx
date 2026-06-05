import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Order } from '../lib/types'
import { PageHeader, StatusPill, Modal, useToast } from '../components/ui'
import { useRealtime } from '../lib/useRealtime'

const NEXT: Record<string, string> = { Received: 'Preparing', Preparing: 'Ready', Ready: 'Delivering', Delivering: 'Delivered' }
const MENU = [
  { name: 'Coffee', price: 3.5 }, { name: 'Tea', price: 3.0 }, { name: 'Bottled Water', price: 2.0 },
  { name: 'Club Sandwich', price: 9.0 }, { name: 'Cheeseburger', price: 12.5 }, { name: 'Chocolate Cake', price: 5.5 },
]

export default function RoomService() {
  const toast = useToast()
  const [orders, setOrders] = useState<Order[]>([])
  const [adding, setAdding] = useState(false)
  const [room, setRoom] = useState('')
  const [qty, setQty] = useState<Record<string, number>>({})

  const load = useCallback(() => {
    api.get<Paged<Order>>('/api/roomservice/orders?pageSize=50').then(({ data }) => setOrders(data.items)).catch(() => {})
  }, [])
  useEffect(() => { load() }, [load])
  useRealtime({ onActivity: () => load() })

  async function create(e: React.FormEvent) {
    e.preventDefault()
    const items = MENU.filter((m) => (qty[m.name] ?? 0) > 0).map((m) => ({ name: m.name, quantity: qty[m.name], unitPrice: m.price }))
    if (!items.length) return toast('Select at least one item', 'err')
    try {
      await api.post('/api/roomservice/orders', { roomNumber: Number(room), items })
      toast('Order placed'); setAdding(false); setQty({}); setRoom(''); load()
    } catch (err) { toast(errorMessage(err), 'err') }
  }

  async function advance(id: string) {
    try { await api.post(`/api/roomservice/orders/${id}/advance`); load() } catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Room Service" subtitle="Orders and delivery tracking"
        action={<button className="btn btn-primary" onClick={() => setAdding(true)}>+ New order</button>} />

      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr><th className="th">Order</th><th className="th">Room</th><th className="th">Items</th><th className="th">Total</th><th className="th">Status</th><th className="th"></th></tr></thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id} className="hover:bg-slate-50">
                <td className="td font-mono text-xs">{o.orderNumber}</td>
                <td className="td">{o.roomNumber}</td>
                <td className="td">{o.items.map((i) => `${i.quantity}× ${i.name}`).join(', ')}</td>
                <td className="td">£{o.total.toFixed(2)}</td>
                <td className="td"><StatusPill value={o.status} /></td>
                <td className="td">{NEXT[o.status] && <button className="btn btn-primary !px-2 !py-1 text-xs" onClick={() => advance(o.id)}>→ {NEXT[o.status]}</button>}</td>
              </tr>
            ))}
            {orders.length === 0 && <tr><td className="td text-slate-400" colSpan={6}>No orders.</td></tr>}
          </tbody>
        </table>
      </div>

      <Modal open={adding} onClose={() => setAdding(false)} title="New room-service order">
        <form onSubmit={create} className="space-y-3">
          <div><label className="label">Room number</label><input className="input" type="number" required value={room} onChange={(e) => setRoom(e.target.value)} /></div>
          <div className="space-y-2">
            {MENU.map((m) => (
              <div key={m.name} className="flex items-center justify-between">
                <span className="text-sm">{m.name} <span className="text-slate-400">£{m.price.toFixed(2)}</span></span>
                <input className="input w-20" type="number" min={0} value={qty[m.name] ?? 0} onChange={(e) => setQty({ ...qty, [m.name]: Number(e.target.value) })} />
              </div>
            ))}
          </div>
          <button className="btn btn-primary w-full">Place order</button>
        </form>
      </Modal>
    </div>
  )
}
