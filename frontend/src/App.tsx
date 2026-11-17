import { useEffect, useState } from 'react'
import { getSession } from './api/auth'
import { ApiError } from './api/http'
import Login from './pages/Login'
import Workspace from './Workspace'
import type { UserAccount } from './types/User'

const demonstration = new URLSearchParams(window.location.search).get('demo') === '1'

export default function App() {
  const [user, setUser] = useState<UserAccount | null>(null)
  const [status, setStatus] = useState<'loading' | 'anonymous' | 'ready' | 'offline'>('loading')
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    if (demonstration) return
    let active = true
    getSession().then(account => { if (active) { setUser(account); setStatus('ready') } })
      .catch(reason => {
        if (!active) return
        setUser(null); setStatus(reason instanceof ApiError && reason.status === 401 ? 'anonymous' : 'offline')
      })
    const expired = () => { setUser(null); setStatus('anonymous') }
    window.addEventListener('mini-erp-session-expired', expired)
    return () => { active = false; window.removeEventListener('mini-erp-session-expired', expired) }
  }, [attempt])

  if (demonstration) return <Workspace user={null} />
  if (status === 'loading') return <main className="auth-shell"><section className="auth-card"><h1>Mini ERP</h1><p role="status">Duke kontrolluar hyrjen…</p></section></main>
  if (status === 'offline') return <main className="auth-shell"><section className="auth-card"><h1>Lidhja po merr kohë.</h1><p>Nuk mund të lidhemi me shërbimin. Provoni përsëri pas pak.</p><button className="auth-link" onClick={() => { setStatus('loading'); setAttempt(value => value + 1) }}>Provo përsëri</button></section></main>
  if (status === 'anonymous' || !user) return <Login onAuthenticated={account => { setUser(account); setStatus('ready') }} />
  return <Workspace key={`${user.id}:${user.role}`} user={user} onSignedOut={() => { setUser(null); setStatus('anonymous') }} />
}
