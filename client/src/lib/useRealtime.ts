import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { tokens } from './api'

type Handlers = {
  onActivity?: (msg: { message: string; at: string }) => void
  onNotification?: (msg: { type: string; message: string; at: string }) => void
}

/**
 * Connects to the dashboard SignalR hub and invokes handlers on live events.
 * The access token is supplied via the query string (the hub reads it there).
 */
export function useRealtime(handlers: Handlers) {
  const ref = useRef(handlers)
  ref.current = handlers

  useEffect(() => {
    const access = tokens.access
    if (!access) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/dashboard?access_token=${access}`)
      .withAutomaticReconnect()
      .build()

    connection.on('activity', (m) => ref.current.onActivity?.(m))
    connection.on('notification', (m) => ref.current.onNotification?.(m))
    connection.start().catch(() => {})

    return () => { connection.stop().catch(() => {}) }
  }, [])
}
