import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { createUser, getUsers, updateUser } from '../api/auth'
import { roleLabels } from '../types/User'
import type { Role, UserAccount } from '../types/User'

export default function Users({ currentUser }: { currentUser: UserAccount }) {
  const [users, setUsers] = useState<UserAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [editing, setEditing] = useState<number | null>(null)
  const [userName, setUserName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<Role>('Salesperson')
  const [isActive, setIsActive] = useState(true)
  const [refresh, setRefresh] = useState(0)
  const formRef = useRef<HTMLFormElement>(null)

  useEffect(() => {
    let active = true
    getUsers().then(data => { if (active) { setUsers(data); setError('') } })
      .catch(reason => { if (active) setError(reason instanceof Error ? reason.message : 'Lista nuk u ngarkua.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [refresh])

  function reset() {
    setEditing(null); setUserName(''); setDisplayName(''); setPassword(''); setRole('Salesperson'); setIsActive(true)
  }

  function edit(user: UserAccount) {
    setEditing(user.id); setUserName(user.userName); setDisplayName(user.displayName)
    setRole(user.role); setIsActive(user.isActive); setPassword(''); setSuccess(''); setError('')
    formRef.current?.querySelector<HTMLInputElement>('input[name="displayName"]')?.focus()
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    setSaving(true); setError(''); setSuccess('')
    try {
      const user = editing === null ? await createUser({ userName, displayName, password, role })
        : await updateUser(editing, { displayName, role, isActive })
      setUsers(current => editing === null ? [...current, user] : current.map(item => item.id === editing ? user : item))
      setSuccess(editing === null ? 'Përdoruesi u krijua me sukses.' : 'Lejet u ruajtën. Përdoruesi duhet të hyjë përsëri.')
      reset()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Ruajtja dështoi.') }
    finally { setSaving(false) }
  }

  return <>
    <div className="page-heading"><div><p className="eyebrow">ADMINISTRIMI</p><h1>Përdoruesit dhe rolet</h1><p>Caktoni qasjen sipas punës së secilit anëtar.</p></div></div>
    <form className="customer-form" onSubmit={submit} ref={formRef}>
      <h2>{editing === null ? 'Shto përdorues' : `Ndrysho ${userName}`}</h2>
      <div className="form-fields">
        <label>Emri i plotë<input name="displayName" required maxLength={100} value={displayName} onChange={event => setDisplayName(event.target.value)} disabled={saving} autoComplete="off" /></label>
        <label>Emri i përdoruesit<input required minLength={3} maxLength={100} value={userName} onChange={event => setUserName(event.target.value)} disabled={saving || editing !== null} autoComplete="off" /></label>
        <label>Roli<select value={role} onChange={event => setRole(event.target.value as Role)} disabled={saving}>{Object.entries(roleLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        {editing === null && <label>Fjalëkalimi fillestar<input required type="password" minLength={12} maxLength={128} value={password} onChange={event => setPassword(event.target.value)} disabled={saving} autoComplete="new-password" /><small>Të paktën 12 karaktere.</small></label>}
      </div>
      {editing !== null && <label className="checkbox-field"><input type="checkbox" checked={isActive} onChange={event => setIsActive(event.target.checked)} disabled={saving} />Llogaria aktive</label>}
      <button disabled={saving}>{saving ? 'Duke ruajtur…' : editing === null ? 'Shto përdoruesin' : 'Ruaj lejet'}</button>
      {editing !== null && <button type="button" className="secondary" disabled={saving} onClick={reset}>Anulo</button>}
    </form>
    {error && <div className="message error" role="alert">{error} <button className="text-button" onClick={() => { setLoading(true); setRefresh(value => value + 1) }}>Rifresko listën</button></div>}
    {success && <p className="message success" role="status">{success}</p>}
    {loading ? <p role="status">Duke ngarkuar përdoruesit…</p> : <section className="table-panel"><div className="table-scroll" tabIndex={0} role="region" aria-label="Lista e përdoruesve"><table>
      <caption className="visually-hidden">Përdoruesit dhe rolet e tyre</caption>
      <thead><tr><th scope="col">Përdoruesi</th><th scope="col">Emri</th><th scope="col">Roli</th><th scope="col">Statusi</th><th scope="col">Veprimet</th></tr></thead>
      <tbody>{users.map(user => <tr key={user.id}><th scope="row">{user.userName}</th><td>{user.displayName}</td><td>{roleLabels[user.role]}</td><td>{user.isActive ? 'Aktiv' : 'Joaktiv'}</td><td className="row-actions">{user.id === currentUser.id ? <span>Llogaria jote</span> : <button disabled={saving} onClick={() => edit(user)} aria-label={`Ndrysho lejet e ${user.userName}`}>Ndrysho lejet</button>}</td></tr>)}</tbody>
    </table></div></section>}
  </>
}
