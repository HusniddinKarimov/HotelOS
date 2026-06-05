import { useEffect, useState, useCallback } from 'react'
import { api } from '../lib/api'
import type { Paged, Notification } from '../lib/types'
import { PageHeader } from '../components/ui'

export default function Notifications() {
  const [items, setItems] = useState<Notification[]>([])
  const load = useCallback(() => { api.get<Paged<Notification>>('/api/notifications?pageSize=50').then(({ data }) => setItems(data.items)).catch(() => {}) }, [])
  useEffect(() => { load() }, [load])

  async function markRead(id: string) { await api.post(`/api/notifications/${id}/read`); load() }

  return (
    <div>
      <PageHeader title="Notifications" subtitle="Your in-app notifications" />
      <div className="card divide-y divide-slate-100 p-0">
        {items.length === 0 && <div className="p-4 text-slate-400">No notifications.</div>}
        {items.map((n) => (
          <div key={n.id} className={`flex items-center justify-between px-4 py-3 ${n.isRead ? 'opacity-60' : ''}`}>
            <div>
              <div className="text-sm font-medium text-slate-700">{n.message}</div>
              <div className="text-xs text-slate-400">{n.type} · {new Date(n.createdAt).toLocaleString()}</div>
            </div>
            {!n.isRead && <button className="btn btn-ghost !px-2 !py-1 text-xs" onClick={() => markRead(n.id)}>Mark read</button>}
          </div>
        ))}
      </div>
    </div>
  )
}
