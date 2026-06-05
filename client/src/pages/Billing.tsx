import { useEffect, useState, useCallback } from 'react'
import { api, errorMessage } from '../lib/api'
import type { Paged, Bill } from '../lib/types'
import { PageHeader, StatusPill, Modal, useToast } from '../components/ui'

export default function Billing() {
  const toast = useToast()
  const [bills, setBills] = useState<Bill[]>([])
  const [status, setStatus] = useState('')
  const [view, setView] = useState<Bill | null>(null)
  const [pay, setPay] = useState({ method: 'Card', amount: 0 })

  const load = useCallback(() => {
    api.get<Paged<Bill>>(`/api/bills?pageSize=50${status ? `&status=${status}` : ''}`).then(({ data }) => setBills(data.items)).catch(() => {})
  }, [status])
  useEffect(() => { load() }, [load])

  async function takePayment() {
    if (!view) return
    try {
      const { data } = await api.post<Bill>('/api/payments', { billId: view.id, method: pay.method, amount: Number(pay.amount), reference: null })
      setView(data); toast('Payment recorded'); load()
    } catch (e) { toast(errorMessage(e), 'err') }
  }

  return (
    <div>
      <PageHeader title="Billing" subtitle="Invoices and payments"
        action={
          <select className="input w-40" value={status} onChange={(e) => setStatus(e.target.value)}>
            {['', 'Open', 'Paid', 'Cancelled'].map((s) => <option key={s} value={s}>{s || 'All'}</option>)}
          </select>
        } />
      <div className="card overflow-x-auto p-0">
        <table className="w-full">
          <thead><tr><th className="th">Total</th><th className="th">Paid</th><th className="th">Balance</th><th className="th">Status</th><th className="th"></th></tr></thead>
          <tbody>
            {bills.map((b) => (
              <tr key={b.id} className="hover:bg-slate-50">
                <td className="td font-medium">£{b.total.toFixed(2)}</td>
                <td className="td">£{b.paid.toFixed(2)}</td>
                <td className="td">£{b.balance.toFixed(2)}</td>
                <td className="td"><StatusPill value={b.status} /></td>
                <td className="td text-right"><button className="text-indigo-600 hover:underline" onClick={() => { setView(b); setPay({ method: 'Card', amount: b.balance }) }}>View</button></td>
              </tr>
            ))}
            {bills.length === 0 && <tr><td className="td text-slate-400" colSpan={5}>No bills.</td></tr>}
          </tbody>
        </table>
      </div>

      <Modal open={!!view} onClose={() => setView(null)} title="Invoice">
        {view && (
          <div className="space-y-2 text-sm">
            {view.items.map((i) => <div key={i.id} className="flex justify-between border-b border-slate-100 py-1"><span>{i.description}</span><span>£{i.amount.toFixed(2)}</span></div>)}
            <div className="flex justify-between pt-2 font-semibold"><span>Total</span><span>£{view.total.toFixed(2)}</span></div>
            <div className="flex justify-between text-lg font-bold"><span>Balance</span><span>£{view.balance.toFixed(2)}</span></div>
            <div><StatusPill value={view.status} /></div>
            {view.balance > 0 && (
              <div className="mt-3 flex items-end gap-2 border-t border-slate-100 pt-3">
                <div className="flex-1"><label className="label">Method</label>
                  <select className="input" value={pay.method} onChange={(e) => setPay({ ...pay, method: e.target.value })}><option>Card</option><option>Cash</option><option>BankTransfer</option></select>
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
