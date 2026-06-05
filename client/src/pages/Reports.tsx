import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { PageHeader } from '../components/ui'

interface Summary {
  revenueTotal: number
  revenueByMethod: Record<string, number>
  totalReservations: number
  checkedInNow: number
  totalRooms: number
  occupiedRooms: number
  occupancyRate: number
  roomsByStatus: Record<string, number>
}

export default function Reports() {
  const [s, setS] = useState<Summary | null>(null)
  useEffect(() => { api.get<Summary>('/api/reports/summary').then(({ data }) => setS(data)).catch(() => {}) }, [])

  return (
    <div>
      <PageHeader title="Reports" subtitle="Revenue and occupancy summary" />
      {!s ? <div className="card text-slate-400">Loading…</div> : (
        <div className="grid gap-4 md:grid-cols-3">
          <div className="card"><div className="text-sm text-slate-500">Total revenue</div><div className="text-3xl font-bold text-emerald-600">£{s.revenueTotal.toFixed(2)}</div></div>
          <div className="card"><div className="text-sm text-slate-500">Occupancy rate</div><div className="text-3xl font-bold text-blue-600">{s.occupancyRate}%</div><div className="text-xs text-slate-400">{s.occupiedRooms}/{s.totalRooms} rooms</div></div>
          <div className="card"><div className="text-sm text-slate-500">Reservations</div><div className="text-3xl font-bold text-slate-700">{s.totalReservations}</div><div className="text-xs text-slate-400">{s.checkedInNow} currently in-house</div></div>

          <div className="card md:col-span-1">
            <div className="mb-2 text-sm font-semibold text-slate-600">Revenue by method</div>
            {Object.keys(s.revenueByMethod).length === 0 ? <p className="text-sm text-slate-400">No payments yet.</p> :
              Object.entries(s.revenueByMethod).map(([k, v]) => <div key={k} className="flex justify-between border-b border-slate-100 py-1 text-sm"><span>{k}</span><span>£{v.toFixed(2)}</span></div>)}
          </div>
          <div className="card md:col-span-2">
            <div className="mb-2 text-sm font-semibold text-slate-600">Rooms by status</div>
            <div className="flex flex-wrap gap-3">
              {Object.entries(s.roomsByStatus).map(([k, v]) => (
                <div key={k} className="rounded-lg bg-slate-100 px-4 py-2 text-center"><div className="text-2xl font-bold">{v}</div><div className="text-xs text-slate-500">{k}</div></div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
