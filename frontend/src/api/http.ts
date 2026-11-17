export const apiUrl = (import.meta.env.VITE_API_URL?.trim()
  || (import.meta.env.DEV ? `http://${window.location.hostname}:5080` : '')).replace(/\/$/, '')

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function send(path: string, init: RequestInit): Promise<Response> {
  try {
    return await fetch(`${apiUrl}${path}`, { ...init, credentials: 'include', cache: 'no-store' })
  } catch {
    throw new ApiError(0, 'API-ja nuk është e qasshme. Provoni përsëri pas pak.')
  }
}

async function reject(response: Response, path: string): Promise<never> {
  if (response.status === 401 && !path.startsWith('/api/auth/')) {
    window.dispatchEvent(new Event('mini-erp-session-expired'))
  }
  const problem = await response.json().catch(() => null)
  const errors = problem?.errors as Record<string, string[]> | undefined
  const fallback = response.status === 401 ? 'Hyni në llogari për të vazhduar.'
    : response.status === 403 ? 'Nuk keni leje për këtë veprim.'
    : response.status === 404 ? 'Regjistrimi nuk ekziston më. Rifreskoni listën.'
    : 'Kërkesa dështoi. Rifreskoni faqen dhe provoni përsëri.'
  throw new ApiError(response.status, errors ? Object.values(errors).flat().join(' ') : problem?.detail ?? fallback)
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  const method = (init.method ?? 'GET').toUpperCase()
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    const csrf = await send('/api/auth/csrf', {})
    if (!csrf.ok) return reject(csrf, path)
    const { token } = await csrf.json() as { token: string }
    headers.set('X-CSRF-TOKEN', token)
  }
  const response = await send(path, { ...init, headers })
  if (!response.ok) return reject(response, path)
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}
