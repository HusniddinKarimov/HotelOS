import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'

/** Colour-coded pill for a status string (rooms, orders, reservations…). */
export function StatusPill({ value }: { value: string }) {
  const map: Record<string, string> = {
    Clean: 'bg-emerald-100 text-emerald-700', Available: 'bg-emerald-100 text-emerald-700',
    Occupied: 'bg-blue-100 text-blue-700', Reserved: 'bg-sky-100 text-sky-700',
    Dirty: 'bg-orange-100 text-orange-700', Cleaning: 'bg-violet-100 text-violet-700',
    Maintenance: 'bg-rose-100 text-rose-700',
    Confirmed: 'bg-sky-100 text-sky-700', Pending: 'bg-slate-100 text-slate-600',
    CheckedIn: 'bg-blue-100 text-blue-700', CheckedOut: 'bg-slate-100 text-slate-600',
    Cancelled: 'bg-rose-100 text-rose-700',
    Received: 'bg-slate-100 text-slate-600', Preparing: 'bg-amber-100 text-amber-700',
    Ready: 'bg-sky-100 text-sky-700', Delivering: 'bg-blue-100 text-blue-700', Delivered: 'bg-emerald-100 text-emerald-700',
    Critical: 'bg-rose-100 text-rose-700', High: 'bg-orange-100 text-orange-700',
    Normal: 'bg-sky-100 text-sky-700', Low: 'bg-slate-100 text-slate-600',
    Open: 'bg-orange-100 text-orange-700', Assigned: 'bg-sky-100 text-sky-700',
    InProgress: 'bg-violet-100 text-violet-700', Completed: 'bg-emerald-100 text-emerald-700',
    Paid: 'bg-emerald-100 text-emerald-700',
  }
  return <span className={`badge ${map[value] ?? 'bg-slate-100 text-slate-600'}`}>{value}</span>
}

export function PageHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return (
    <div className="mb-5 flex items-end justify-between">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">{title}</h1>
        {subtitle && <p className="text-sm text-slate-500">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

export function Modal({ open, onClose, title, children }: { open: boolean; onClose: () => void; title: string; children: ReactNode }) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onClick={onClose}>
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button className="text-slate-400 hover:text-slate-600" onClick={onClose}>✕</button>
        </div>
        {children}
      </div>
    </div>
  )
}

/* --- toast --- */
type Toast = { id: number; text: string; kind: 'ok' | 'err' }
const ToastCtx = createContext<(text: string, kind?: 'ok' | 'err') => void>(() => {})
export const useToast = () => useContext(ToastCtx)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<Toast[]>([])
  const push = useCallback((text: string, kind: 'ok' | 'err' = 'ok') => {
    const id = Date.now() + Math.random()
    setItems((s) => [...s, { id, text, kind }])
    setTimeout(() => setItems((s) => s.filter((t) => t.id !== id)), 3500)
  }, [])
  return (
    <ToastCtx.Provider value={push}>
      {children}
      <div className="fixed bottom-4 right-4 z-[60] flex flex-col gap-2">
        {items.map((t) => (
          <div key={t.id} className={`rounded-lg px-4 py-2 text-sm text-white shadow-lg ${t.kind === 'ok' ? 'bg-emerald-600' : 'bg-rose-600'}`}>
            {t.text}
          </div>
        ))}
      </div>
    </ToastCtx.Provider>
  )
}
