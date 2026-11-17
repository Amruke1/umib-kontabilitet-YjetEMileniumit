import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { getSetupStatus, login, setupAdministrator } from '../api/auth'
import type { UserAccount } from '../types/User'

export default function Login({ onAuthenticated }: { onAuthenticated: (user: UserAccount) => void }) {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [setupToken, setSetupToken] = useState('')
  const [setup, setSetup] = useState(false)
  const [setupStatus, setSetupStatus] = useState<{ required: boolean; available: boolean } | null>(null)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    getSetupStatus().then(status => { if (active) setSetupStatus(status) })
      .catch(reason => { if (active) setError(reason instanceof Error ? reason.message : 'Kontrolli i lidhjes dështoi.') })
    return () => { active = false }
  }, [])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    setSaving(true); setError('')
    try {
      const user = setup
        ? await setupAdministrator({ userName, displayName, password, setupToken })
        : await login(userName, password)
      setPassword(''); setSetupToken('')
      onAuthenticated(user)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Hyrja dështoi.')
    } finally { setSaving(false) }
  }

  return <main className="auth-shell">
    <section className="auth-card" aria-labelledby="login-heading">
      <div className="brand"><span className="brand-mark">m.</span><span>mini<span className="brand-light">erp</span><small>YJET E MILENIUMIT</small></span></div>
      <p className="eyebrow">HAPËSIRA E EKIPIT</p>
      <h1 id="login-heading">{setup ? 'Administratori i parë' : 'Mirë se erdhe.'}</h1>
      <p>{setup ? 'Krijoni llogarinë që do të menaxhojë përdoruesit dhe rolet.' : 'Hyni në llogari për të vazhduar punën.'}</p>
      <form onSubmit={submit} className="auth-form">
        {setup && <label>Emri i plotë<input required maxLength={100} value={displayName} onChange={event => setDisplayName(event.target.value)} autoComplete="name" disabled={saving} /></label>}
        <label>Emri i përdoruesit<input required minLength={setup ? 3 : undefined} maxLength={100} value={userName} onChange={event => setUserName(event.target.value)} autoComplete="username" disabled={saving} /></label>
        <label>Fjalëkalimi<input required type="password" minLength={setup ? 12 : undefined} maxLength={128} value={password} onChange={event => setPassword(event.target.value)} autoComplete={setup ? 'new-password' : 'current-password'} disabled={saving} /></label>
        {setup && <><small>Të paktën 12 karaktere. Mund të përdorni një frazë.</small><label>Kodi privat i konfigurimit<input required type="password" maxLength={256} value={setupToken} onChange={event => setSetupToken(event.target.value)} autoComplete="off" disabled={saving} /></label></>}
        {error && <p className="message error" role="alert">{error}</p>}
        <button disabled={saving}>{saving ? 'Ju lutem prisni…' : setup ? 'Krijo administratorin' : 'Hyr'}</button>
      </form>
      {setupStatus?.required && (setupStatus.available
        ? <button className="text-button auth-link" disabled={saving} onClick={() => { setSetup(!setup); setError(''); setPassword(''); setSetupToken('') }}>{setup ? 'Kthehu te hyrja' : 'Konfiguro administratorin e parë'}</button>
        : <p className="auth-note">Llogaria e parë ende nuk është konfiguruar. Kontaktoni përgjegjësin e projektit.</p>)}
      <a className="demo-link" href="/?demo=1">Shiko demonstrimin me të dhëna fiktive ↗</a>
    </section>
  </main>
}
