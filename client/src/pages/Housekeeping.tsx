import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { HousekeepingTask } from '../lib/types'
import { PageHeader, StatusPill, useToast } from '../components/ui'
import { useRealtime } from '../lib/useRealtime'

export default function Housekeeping() {
  const toast = useToast()
  const [tasks, setTasks] = useState<HousekeepingTask[]>([])

  const load = useCallback(() => { api.get<HousekeepingTask[]>('/api/housekeeping/queue').then(({ data }) => setTasks(data)).catch(() => {}) }, [])
  useEffect(() => { load() }, [load])
  useRealtime({ onActivity: () => load() })

  async function act(id: string, action: 'start' | 'complete') {
    try { await api.post(`/api/housekeeping/${id}/${action}`); toast(action === 'start' ? 'Cleaning started' : 'Room cleaned'); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Housekeeping" subtitle="Cleaning queue — Dirty → Cleaning → Clean" />
      {tasks.length === 0 ? <div className="card text-slate-400">Nothing to clean. 🎉</div> : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {tasks.map((t) => (
            <div key={t.id} className="card">
              <div className="flex items-center justify-between">
                <span className="text-xl font-bold">Room {t.roomNumber}</span>
                <StatusPill value={t.status} />
              </div>
              <div className="mt-3">
                {t.status === 'Pending' && <button className="btn btn-primary w-full" onClick={() => act(t.id, 'start')}>Start cleaning</button>}
                {t.status === 'InProgress' && <button className="btn btn-success w-full" onClick={() => act(t.id, 'complete')}>Mark clean</button>}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
