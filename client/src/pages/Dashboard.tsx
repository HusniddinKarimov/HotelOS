import { useEffect, useState, useCallback } from 'react'
import { api } from '../lib/api'
import type { Dashboard as Dash } from '../lib/types'
import { useRealtime } from '../lib/useRealtime'
import { PageHeader } from '../components/ui'

function Stat({ label, value, color }: { label: string; value: string | number; color: string }) {
  return (
    <div className="card">
      <div className={`text-3xl font-bold ${color}`}>{value}</div>
      <div className="mt-1 text-sm text-slate-500">{label}</div>
    </div>
  )
}

export default function Dashboard() {
  const [d, setD] = useState<Dash | null>(null)
  const [feed, setFeed] = useState<string[]>([])

  const load = useCallback(() => { api.get<Dash>('/api/dashboard').then(({ data }) => setD(data)).catch(() => {}) }, [])
  useEffect(() => { load() }, [load])

  // Live: any activity refreshes the metrics and prepends to the feed.
  useRealtime({
    onActivity: (m) => { setFeed((f) => [m.message, ...f].slice(0, 12)); load() },
    onNotification: () => load(),
  })

  return (
    <div>
      <PageHeader title="Operations Dashboard" subtitle="Live hotel status — updates in real time" />
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Stat label="Available" value={d?.availableRooms ?? '–'} color="text-emerald-600" />
        <Stat label="Occupied" value={d?.occupiedRooms ?? '–'} color="text-blue-600" />
        <Stat label="Dirty" value={d?.dirtyRooms ?? '–'} color="text-orange-600" />
        <Stat label="Cleaning" value={d?.cleaningRooms ?? '–'} color="text-violet-600" />
        <Stat label="Maintenance" value={d?.maintenanceRooms ?? '–'} color="text-rose-600" />
        <Stat label="Active guests" value={d?.activeGuests ?? '–'} color="text-slate-700" />
        <Stat label="Active orders" value={d?.activeOrders ?? '–'} color="text-amber-600" />
        <Stat label="Open maintenance" value={d?.openMaintenanceRequests ?? '–'} color="text-rose-600" />
      </div>

      <div className="mt-6 grid gap-4 md:grid-cols-3">
        <div className="card md:col-span-1">
          <div className="text-sm text-slate-500">Total revenue</div>
          <div className="text-4xl font-bold text-emerald-600">£{(d?.revenue ?? 0).toFixed(2)}</div>
          <div className="mt-1 text-xs text-slate-400">{d?.totalRooms ?? 0} rooms total</div>
        </div>
        <div className="card md:col-span-2">
          <div className="mb-2 text-sm font-semibold text-slate-600">Live activity feed</div>
          {feed.length === 0 ? (
            <p className="text-sm text-slate-400">Waiting for events… (try a check-in or order)</p>
          ) : (
            <ul className="space-y-1 text-sm text-slate-600">
              {feed.map((m, i) => <li key={i} className="border-b border-slate-100 py-1">• {m}</li>)}
            </ul>
          )}
        </div>
      </div>
    </div>
  )
}
