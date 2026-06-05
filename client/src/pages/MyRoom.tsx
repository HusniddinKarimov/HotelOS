import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import { PageHeader, StatusPill, useToast } from '../components/ui'

interface MyRoom { reservationId: string; roomNumber: number; roomType: string; floor: number; status: string; checkInDate: string; checkOutDate: string; billId?: string; total: number }
interface Available { roomId: string; number: number; floor: number; type: string; nightlyRate: number }

export default function MyRoom() {
  const toast = useToast()
  const [room, setRoom] = useState<MyRoom | null>(null)
  const [available, setAvailable] = useState<Available[]>([])
  const [loading, setLoading] = useState(true)

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
      }
    } catch (e) { toast(errorMessage(e), 'err') } finally { setLoading(false) }
  }, [toast])
  useEffect(() => { load() }, [load])

  async function book(roomId: string) {
    try { await api.post('/api/me/book', { roomId }); toast('Room booked — enjoy your stay!'); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }
  async function leave() {
    try { await api.post('/api/me/leave'); toast('Checked out. Thank you!'); load() }
    catch (e) { toast(errorMessage(e), 'err') }
  }

  if (loading) return <div className="card text-slate-400">Loading…</div>

  // --- The user already has a room ---
  if (room) {
    return (
      <div>
        <PageHeader title="My Room" subtitle="Your current stay" />
        <div className="card max-w-md">
          <div className="flex items-center justify-between">
            <span className="text-4xl font-bold text-slate-800">Room {room.roomNumber}</span>
            <StatusPill value={room.status} />
          </div>
          <div className="mt-2 text-slate-500">{room.roomType} · Floor {room.floor}</div>
          <div className="mt-4 space-y-1 text-sm text-slate-600">
            <div>Checked in: {new Date(room.checkInDate).toLocaleDateString()}</div>
            <div>Current bill: <span className="font-semibold">£{room.total.toFixed(2)}</span></div>
          </div>
          <button className="btn btn-danger mt-5 w-full" onClick={leave}>Leave room (check out)</button>
          <p className="mt-2 text-center text-xs text-slate-400">Your room becomes available for cleaning when you leave.</p>
        </div>
      </div>
    )
  }

  // --- No room yet: book one ---
  return (
    <div>
      <PageHeader title="Book a Room" subtitle="You don't have a room yet — pick an available one" />
      {available.length === 0 ? (
        <div className="card text-slate-400">No rooms available right now. Please check back shortly.</div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {available.map((r) => (
            <div key={r.roomId} className="card">
              <div className="text-xl font-bold text-slate-800">Room {r.number}</div>
              <div className="text-xs text-slate-500">{r.type} · Floor {r.floor}</div>
              <div className="mt-2 text-lg font-semibold text-emerald-600">£{r.nightlyRate.toFixed(2)}<span className="text-xs font-normal text-slate-400">/night</span></div>
              <button className="btn btn-primary mt-3 w-full" onClick={() => book(r.roomId)}>Book</button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
