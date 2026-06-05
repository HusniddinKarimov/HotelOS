import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Order } from '../lib/types'
import { PageHeader, StatusPill, useToast } from '../components/ui'
import { useRealtime } from '../lib/useRealtime'

const NEXT: Record<string, string> = { Received: 'Preparing', Preparing: 'Ready', Ready: 'Delivering', Delivering: 'Delivered' }

export default function Kitchen() {
  const toast = useToast()
  const [orders, setOrders] = useState<Order[]>([])

  const load = useCallback(() => {
    api.get<Paged<Order>>('/api/kitchen/orders?pageSize=50').then(({ data }) => setOrders(data.items)).catch(() => {})
  }, [])
  useEffect(() => { load() }, [load])
  useRealtime({ onActivity: () => load(), onNotification: () => load() })

  async function advance(id: string) {
    try { await api.post(`/api/kitchen/orders/${id}/advance`); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Kitchen" subtitle="Incoming orders — Received → Preparing → Ready" />
      {orders.length === 0 ? <div className="card text-slate-400">No active orders.</div> : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {orders.map((o) => (
            <div key={o.id} className="card">
              <div className="flex items-center justify-between">
                <span className="font-bold">#{o.orderNumber} · Room {o.roomNumber}</span>
                <StatusPill value={o.status} />
              </div>
              <ul className="mt-2 text-sm text-slate-600">
                {o.items.map((i, idx) => <li key={idx}>{i.quantity}× {i.name}</li>)}
              </ul>
              <div className="mt-2 text-sm font-semibold">£{o.total.toFixed(2)}</div>
              {NEXT[o.status] && <button className="btn btn-primary mt-3 w-full" onClick={() => advance(o.id)}>→ {NEXT[o.status]}</button>}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
