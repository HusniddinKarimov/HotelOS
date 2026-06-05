import { Link, Navigate } from 'react-router-dom'
import { tokens } from '../lib/api'

const FEATURES = [
  { icon: '🛏️', title: 'Effortless booking', text: 'Browse available rooms, pick your dates, and confirm in seconds — pay securely online.' },
  { icon: '⚡', title: 'Real-time service', text: 'Order room service and report issues from your account; staff are notified instantly.' },
  { icon: '🧹', title: 'Always spotless', text: 'Rooms are cleaned and verified the moment you check out, so every stay is fresh.' },
  { icon: '🔒', title: 'Safe & secure', text: 'Your details are protected with encrypted authentication and card data is never stored.' },
]

export default function Landing() {
  // Already signed in? Skip the marketing page.
  if (tokens.access) return <Navigate to="/dashboard" replace />

  return (
    <div className="min-h-screen bg-slate-950 text-white">
      {/* Top bar */}
      <header className="absolute inset-x-0 top-0 z-20 flex items-center justify-between px-6 py-5 md:px-12">
        <div className="flex items-center gap-2 text-xl font-bold">🏨 <span>HotelOS</span></div>
        <nav className="flex items-center gap-3">
          <Link to="/login" className="rounded-lg px-4 py-2 text-sm font-semibold text-white/90 hover:bg-white/10">Log in</Link>
          <Link to="/signup" className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-500">Sign up</Link>
        </nav>
      </header>

      {/* Hero */}
      <section className="relative flex min-h-[88vh] items-center justify-center overflow-hidden">
        <img
          src="https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1920&q=80"
          alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-b from-slate-950/80 via-slate-950/60 to-slate-950" />
        <div className="relative z-10 mx-auto max-w-3xl px-6 text-center">
          <span className="mb-4 inline-block rounded-full border border-white/20 bg-white/10 px-4 py-1 text-xs font-medium tracking-wide backdrop-blur">
            ★★★★ · GrandStay Hotel
          </span>
          <h1 className="text-4xl font-extrabold leading-tight sm:text-6xl">
            Your perfect stay,<br /><span className="bg-gradient-to-r from-indigo-400 to-sky-400 bg-clip-text text-transparent">beautifully simple.</span>
          </h1>
          <p className="mx-auto mt-5 max-w-xl text-lg text-white/80">
            Book a room, order room service, and manage your whole stay from one elegant dashboard.
          </p>
          <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
            <Link to="/signup" className="w-full rounded-xl bg-indigo-600 px-7 py-3 text-base font-semibold shadow-lg shadow-indigo-900/40 hover:bg-indigo-500 sm:w-auto">
              Book your stay
            </Link>
            <Link to="/login" className="w-full rounded-xl border border-white/25 bg-white/5 px-7 py-3 text-base font-semibold backdrop-blur hover:bg-white/10 sm:w-auto">
              I have an account
            </Link>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="mx-auto max-w-6xl px-6 py-20">
        <h2 className="text-center text-3xl font-bold">Everything your stay needs</h2>
        <p className="mt-2 text-center text-white/60">One account — from check-in to check-out.</p>
        <div className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map((f) => (
            <div key={f.title} className="rounded-2xl border border-white/10 bg-white/5 p-6 transition hover:border-indigo-500/40 hover:bg-white/[0.07]">
              <div className="text-3xl">{f.icon}</div>
              <h3 className="mt-4 text-lg font-semibold">{f.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-white/60">{f.text}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA band */}
      <section className="border-y border-white/10 bg-gradient-to-r from-indigo-600 to-sky-600">
        <div className="mx-auto flex max-w-5xl flex-col items-center justify-between gap-4 px-6 py-12 text-center sm:flex-row sm:text-left">
          <div>
            <h2 className="text-2xl font-bold">Ready to check in?</h2>
            <p className="text-white/80">Create your account and book a room in under a minute.</p>
          </div>
          <Link to="/signup" className="rounded-xl bg-white px-7 py-3 font-semibold text-indigo-700 hover:bg-white/90">Get started</Link>
        </div>
      </section>

      <footer className="px-6 py-10 text-center text-sm text-white/40">
        © {new Date().getFullYear()} GrandStay Hotel · Powered by HotelOS
      </footer>
    </div>
  )
}
