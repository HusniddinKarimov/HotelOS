import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import ProtectedRoute from './auth/ProtectedRoute'
import { ToastProvider } from './components/ui'
import Layout from './components/Layout'
import Landing from './pages/Landing'
import Login from './pages/Login'
import SignUp from './pages/SignUp'
import Dashboard from './pages/Dashboard'
import Guests from './pages/Guests'
import Reservations from './pages/Reservations'
import Rooms from './pages/Rooms'
import Housekeeping from './pages/Housekeeping'
import Kitchen from './pages/Kitchen'
import RoomService from './pages/RoomService'
import Maintenance from './pages/Maintenance'
import Billing from './pages/Billing'
import Reports from './pages/Reports'
import Users from './pages/Users'
import Notifications from './pages/Notifications'
import MyRoom from './pages/MyRoom'

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<Landing />} />
            <Route path="/login" element={<Login />} />
            <Route path="/signup" element={<SignUp />} />
            <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/my-room" element={<MyRoom />} />
              <Route path="/reservations" element={<Reservations />} />
              <Route path="/guests" element={<Guests />} />
              <Route path="/rooms" element={<Rooms />} />
              <Route path="/housekeeping" element={<Housekeeping />} />
              <Route path="/kitchen" element={<Kitchen />} />
              <Route path="/roomservice" element={<RoomService />} />
              <Route path="/maintenance" element={<Maintenance />} />
              <Route path="/billing" element={<Billing />} />
              <Route path="/reports" element={<Reports />} />
              <Route path="/users" element={<Users />} />
              <Route path="/notifications" element={<Notifications />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
