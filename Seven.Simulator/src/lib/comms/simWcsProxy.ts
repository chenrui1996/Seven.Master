import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { ref, shallowRef } from 'vue'
import { AUTH_TOKEN_KEY } from '@/api/http'

export interface WcsProxyMessage {
  connectionId: string
  payload: string
  receivedAt: number
}

const connected = ref(false)
const lastMessage = shallowRef<WcsProxyMessage | null>(null)

let connection: HubConnection | null = null
const messageHandlers = new Set<(msg: WcsProxyMessage) => void>()

function hubUrl(): string {
  const base = import.meta.env.VITE_API_BASE_URL || ''
  return `${base}/hubs/sim-wcs-proxy`
}

function notifyMessage(connectionId: string, payload: string) {
  const msg: WcsProxyMessage = {
    connectionId,
    payload,
    receivedAt: Date.now(),
  }
  lastMessage.value = msg
  for (const handler of messageHandlers) {
    handler(msg)
  }
}

function buildConnection(): HubConnection {
  const token = localStorage.getItem(AUTH_TOKEN_KEY)
  return new HubConnectionBuilder()
    .withUrl(hubUrl(), {
      accessTokenFactory: () => token ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build()
}

/** Subscribe to inbound WCS messages from the proxy hub. */
export function onWcsMessageReceived(handler: (msg: WcsProxyMessage) => void): () => void {
  messageHandlers.add(handler)
  return () => messageHandlers.delete(handler)
}

/** Connect to `/hubs/sim-wcs-proxy`. Hub is AllowAnonymous; token is optional. */
export async function connectSimWcsProxy(): Promise<void> {
  if (connection?.state === 'Connected') return

  if (!connection) {
    connection = buildConnection()
    connection.on('OnWcsMessageReceived', (connectionId: string, payload: string) => {
      notifyMessage(connectionId, payload)
    })
    connection.onreconnected(() => {
      connected.value = true
    })
    connection.onclose(() => {
      connected.value = false
    })
  }

  await connection.start()
  connected.value = true
}

/** Disconnect from the proxy hub. */
export async function disconnectSimWcsProxy(): Promise<void> {
  if (!connection) {
    connected.value = false
    return
  }
  await connection.stop()
  connected.value = false
}

/** Invoke hub `SendWcsMessage`; subscribers receive `OnWcsMessageReceived`. */
export async function sendWcsMessage(connectionId: string, payload: string): Promise<void> {
  if (!connection || connection.state !== 'Connected') {
    throw new Error('Gateway hub 未连接')
  }
  await connection.invoke('SendWcsMessage', connectionId, payload)
}

/** Reactive hub connection state and last inbound message (minimal emulator hook). */
export function useSimWcsProxy() {
  return {
    connected,
    lastMessage,
    connect: connectSimWcsProxy,
    disconnect: disconnectSimWcsProxy,
    sendWcsMessage,
    onWcsMessageReceived,
  }
}
