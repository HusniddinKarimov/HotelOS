import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Room } from '../lib/types'
import { useAuth } from '../auth/AuthContext'
import { ROLES } from '../lib/types'
import { PageHeader, StatusPill, useToast } from '../components/ui'
import { useRealtime } from '../lib/useRealtime'

const STATUSES = ['', 'Clean', 'Occupied', 'Dirty', 'Cleaning', 'Maintenance']

export default function Rooms() {
  const { hasRole } = useAuth()
  const toast = useToast()
  const [rooms, setRooms] = useState<Room[]>([])
  const [status, setStatus] = useState('')
  const canManage = hasRole(ROLES.Administrator, ROLES.HotelManager)

  const load = useCallback(() => {
    api.get<Paged<Room>>(`/api/rooms?pageSize=100&sortBy=number${status ? `&status=${status}` : ''}`)
      .then(({ data }) => setRooms(data.items)).catch(() => {})
  }, [status])
  useEffect(() => { load() }, [load])
  useRealtime({ onActivity: () => load() })

  async function setRoomStatus(id: string, newStatus: string) {
    try { await api.put(`/api/rooms/${id}/status`, { status: newStatus }); toast('Room updated'); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Rooms" subtitle="Live room inventory and status"
        action={
          <select className="input w-44" value={status} onChange={(e) => setStatus(e.target.value)}>
            {STATUSES.map((s) => <option key={s} value={s}>{s || 'All statuses'}</option>)}
          </select>
        } />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5">
        {rooms.map((r) => (
          <div key={r.id} className="card">
            <div className="flex items-center justify-between">
              <span className="text-xl font-bold text-slate-800">{r.number}</span>
              <StatusPill value={r.status} />
            </div>
            <div className="mt-1 text-xs text-slate-500">{r.type} · Floor {r.floor}{r.nearElevator ? ' · 🛗' : ''}</div>
            <div className="mt-2 text-sm font-medium text-slate-700">{r.currentGuest ? `👤 ${r.currentGuest}` : '—'}</div>
            {canManage && (
              <select className="input mt-3 text-xs" value={r.status} onChange={(e) => setRoomStatus(r.id, e.target.value)}>
                {['Clean', 'Available', 'Occupied', 'Dirty', 'Cleaning', 'Maintenance', 'Reserved'].map((s) => <option key={s}>{s}</option>)}
              </select>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
