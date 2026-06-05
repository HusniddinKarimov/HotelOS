import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { ROLES } from '../lib/types'
import { useRealtime } from '../lib/useRealtime'
import { api } from '../lib/api'
import { useToast } from './ui'

// Every staff role (i.e. everyone except a basic User).
const STAFF = [
  ROLES.Administrator, ROLES.HotelManager, ROLES.Receptionist, ROLES.Housekeeping,
  ROLES.KitchenStaff, ROLES.RoomServiceStaff, ROLES.MaintenanceStaff,
]

const NAV: { to: string; label: string; icon: string; roles?: string[] }[] = [
  { to: '/my-room', label: 'My Room', icon: '🛏️', roles: [ROLES.User] },
  { to: '/', label: 'Dashboard', icon: '📊', roles: STAFF },
  { to: '/reservations', label: 'Reservations', icon: '📅', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.Receptionist] },
  { to: '/guests', label: 'Guests', icon: '🧑', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.Receptionist] },
  { to: '/rooms', label: 'Rooms', icon: '🚪', roles: STAFF },
  { to: '/housekeeping', label: 'Housekeeping', icon: '🧹', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.Housekeeping] },
  { to: '/kitchen', label: 'Kitchen', icon: '🍳', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.KitchenStaff] },
  { to: '/roomservice', label: 'Room Service', icon: '🛎️', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.RoomServiceStaff, ROLES.Receptionist] },
  { to: '/maintenance', label: 'Maintenance', icon: '🔧', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.MaintenanceStaff, ROLES.Receptionist] },
  { to: '/billing', label: 'Billing', icon: '💳', roles: [ROLES.Administrator, ROLES.HotelManager, ROLES.Receptionist] },
  { to: '/reports', label: 'Reports', icon: '📈', roles: [ROLES.Administrator, ROLES.HotelManager] },
  { to: '/users', label: 'Users', icon: '⚙️', roles: [ROLES.Administrator] },
]

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const [unread, setUnread] = useState(0)

  const loadUnread = () =>
    api.get('/api/notifications?unreadOnly=true&pageSize=1').then(({ data }) => setUnread(data.totalCount)).catch(() => {})

  useEffect(() => { loadUnread() }, [])
  useRealtime({
    onNotification: (m) => { toast(m.message); loadUnread() },
  })

  const items = NAV.filter((n) => !n.roles || (user && n.roles.includes(user.role)))

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-60 flex-col bg-slate-900 text-slate-200">
        <div className="px-5 py-5 text-lg font-bold text-white">🏨 HotelOS</div>
        <nav className="flex-1 space-y-1 px-3">
          {items.map((n) => (
            <NavLink key={n.to} to={n.to} end={n.to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm ${isActive ? 'bg-indigo-600 text-white' : 'hover:bg-slate-800'}`}>
              <span>{n.icon}</span> {n.label}
            </NavLink>
          ))}
        </nav>
        <div className="border-t border-slate-800 p-4 text-xs text-slate-400">
          <div className="font-semibold text-slate-200">{user?.fullName ?? user?.username}</div>
          <div>{user?.role}</div>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
          <div className="text-sm text-slate-500">GrandStay Hotel — Operations</div>
          <div className="flex items-center gap-4">
            <button className="relative" onClick={() => navigate('/notifications')} title="Notifications">
              <span className="text-xl">🔔</span>
              {unread > 0 && <span className="absolute -right-2 -top-1 rounded-full bg-rose-600 px-1.5 text-xs font-bold text-white">{unread}</span>}
            </button>
            <button className="btn btn-ghost" onClick={logout}>Logout</button>
          </div>
        </header>
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
