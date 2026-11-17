import { useState } from 'react'
import type { FormEvent } from 'react'
import { changePassword } from '../api/auth'

export default function Password() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    setError(''); setSuccess(false)
    if (next !== confirmation) { setError('Fjalëkalimet e reja nuk përputhen.'); return }
    setSaving(true)
    try {
      await changePassword(current, next)
      setCurrent(''); setNext(''); setConfirmation(''); setSuccess(true)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Ndryshimi dështoi.') }
    finally { setSaving(false) }
  }
  return <>
    <div className="page-heading"><div><p className="eyebrow">LLOGARIA JOTE</p><h1>Ndrysho fjalëkalimin</h1></div></div>
    <form className="customer-form" onSubmit={submit}><div className="form-fields">
      <label>Fjalëkalimi aktual<input required type="password" maxLength={128} autoComplete="current-password" value={current} onChange={event => setCurrent(event.target.value)} disabled={saving} /></label>
      <label>Fjalëkalimi i ri<input required type="password" minLength={12} maxLength={128} autoComplete="new-password" value={next} onChange={event => setNext(event.target.value)} disabled={saving} /></label>
      <label>Përsërit fjalëkalimin e ri<input required type="password" minLength={12} maxLength={128} autoComplete="new-password" value={confirmation} onChange={event => setConfirmation(event.target.value)} disabled={saving} /></label>
    </div><button disabled={saving}>{saving ? 'Duke ruajtur…' : 'Ruaj fjalëkalimin'}</button></form>
    {error && <p className="message error" role="alert">{error}</p>}
    {success && <p className="message success" role="status">Fjalëkalimi u ndryshua me sukses.</p>}
  </>
}
