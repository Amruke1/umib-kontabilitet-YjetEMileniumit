import { useRef, useState } from 'react'
import type { Dispatch, FormEvent, SetStateAction } from 'react'
import { addCustomer, deleteCustomer, updateCustomer } from '../api/customers'
import type { Customer } from '../types/Customer'

type Props = {
  customers: Customer[]
  setCustomers: Dispatch<SetStateAction<Customer[]>>
  demo: boolean
  loading: boolean
  connected: boolean
  retry: () => void
  canManage: boolean
}

export default function Customers({ customers, setCustomers, demo, loading, connected, retry, canManage }: Props) {
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const submitting = useRef(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [query, setQuery] = useState('')
  const [pendingDelete, setPendingDelete] = useState<Customer | null>(null)
  const visibleCustomers = customers.filter(customer =>
    [String(customer.id), customer.name, customer.email ?? '', customer.phone ?? '']
      .some(value => value.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase())))

  function resetForm() {
    setEditingId(null); setName(''); setEmail(''); setPhone('')
  }

  function edit(customer: Customer) {
    if (!canManage) return
    setEditingId(customer.id); setName(customer.name)
    setEmail(customer.email ?? ''); setPhone(customer.phone ?? '')
    setError(''); setSuccess(''); setPendingDelete(null)
    document.querySelector<HTMLInputElement>('input[name="name"]')?.focus()
  }

  async function confirmDelete() {
    if (!canManage || !pendingDelete || submitting.current) return
    submitting.current = true; setSaving(true); setError(''); setSuccess('')
    try {
      if (!demo) await deleteCustomer(pendingDelete.id)
      setCustomers(current => current.filter(customer => customer.id !== pendingDelete.id))
      if (editingId === pendingDelete.id) resetForm()
      setPendingDelete(null); setSuccess('Klienti u fshi me sukses.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Fshirja dështoi.')
    } finally { submitting.current = false; setSaving(false) }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canManage || submitting.current || loading || !connected) return
    setError('')
    setSuccess('')
    if (!name.trim()) { setError('Emri është i detyrueshëm.'); return }
    submitting.current = true
    setSaving(true)
    try {
      const input = { name: name.trim(), email: email.trim() || null, phone: phone.trim() || null }
      const customer = demo
        ? { ...input, id: editingId ?? Math.max(0, ...customers.map(item => item.id)) + 1 }
        : editingId === null ? await addCustomer(input) : await updateCustomer(editingId, input)
      setCustomers((current) => editingId === null ? [...current, customer] : current.map(item => item.id === editingId ? customer : item))
      resetForm()
      setSuccess(editingId === null ? 'Klienti u shtua me sukses.' : 'Klienti u përditësua me sukses.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Ruajtja dështoi.')
    } finally {
      submitting.current = false
      setSaving(false)
    }
  }

  return (
    <>
        <div className="page-heading">
          <div><p className="eyebrow">REGJISTRI I KLIENTËVE</p><h1>Klientët</h1><p>Të dhënat e kontaktit të klientëve tuaj.</p></div>
          <span className="count">{connected ? customers.length : '—'} klientë</span>
        </div>
        {!connected && !loading && <div className="message error" role="alert">API-ja nuk është e qasshme. Hapni demonstrimin ose provoni lidhjen përsëri. <button type="button" className="text-button" onClick={retry}>Provo përsëri</button></div>}
        {!canManage && <p className="message" role="status">Keni qasje vetëm për leximin e klientëve.</p>}
        {canManage && <form className="customer-form" onSubmit={submit}>
          <h2>{editingId === null ? 'Shto klient' : `Ndrysho klientin #${editingId}`}</h2>
          <div className="form-fields">
            <label><span>Emri <span aria-hidden="true">*</span></span><input name="name" required maxLength={150} value={name} onChange={(event) => setName(event.target.value)} disabled={saving} autoComplete="name" /></label>
            <label>Email<input name="email" type="email" maxLength={254} value={email} onChange={(event) => setEmail(event.target.value)} disabled={saving} autoComplete="email" /></label>
            <label>Telefoni<input name="phone" type="tel" maxLength={30} value={phone} onChange={(event) => setPhone(event.target.value)} disabled={saving} autoComplete="tel" /></label>
          </div>
          <button type="submit" disabled={saving || loading || !connected}>{saving ? 'Duke ruajtur…' : editingId === null ? '+ Shto klientin' : 'Ruaj ndryshimet'}</button>
          {editingId !== null && <button className="secondary" type="button" disabled={saving} onClick={resetForm}>Anulo ndryshimin</button>}
        </form>}
        <label className="search-field">Kërko klient<input type="search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Emri, email, telefoni ose ID" /></label>
        {canManage && pendingDelete && <section className="delete-confirmation" aria-label="Konfirmo fshirjen">
          <p>Fshi klientin <strong>{pendingDelete.name}</strong>? Ky veprim nuk mund të kthehet.</p>
          <button className="danger" type="button" disabled={saving} onClick={confirmDelete}>Konfirmo fshirjen</button>
          <button className="secondary" type="button" disabled={saving} onClick={() => setPendingDelete(null)}>Anulo</button>
        </section>}
        {error && <p className="message error" role="alert">{error}</p>}
        {success && <p className="message success" role="status">{success}</p>}
        {loading ? <p role="status">Duke ngarkuar klientët…</p> : customers.length === 0 ? (
          <p className="empty">{error ? 'Lista nuk u ngarkua. Rifreskoni faqen për të provuar përsëri.' : 'Nuk ka klientë. Shtoni klientin e parë.'}</p>
        ) : (
          <section className="table-panel" aria-label="Lista e klientëve">
            <div className="table-scroll" tabIndex={0} role="region" aria-label="Tabela e klientëve; rrëshqitni horizontalisht në ekran të vogël">
              <table>
                <caption className="visually-hidden">Klientët dhe të dhënat e kontaktit</caption>
                <thead><tr><th scope="col">ID</th><th scope="col">Emri</th><th scope="col">Email</th><th scope="col">Telefoni</th>{canManage && <th scope="col">Veprimet</th>}</tr></thead>
                <tbody>{visibleCustomers.map((customer) => (
                  <tr key={customer.id}>
                    <td className="customer-id">{customer.id}</td><th scope="row">{customer.name}</th>
                    <td>{customer.email ?? <span className="missing">Nuk është vendosur</span>}</td>
                    <td>{customer.phone ?? <span className="missing">Nuk është vendosur</span>}</td>
                    {canManage && <td className="row-actions"><button type="button" disabled={saving} onClick={() => edit(customer)} aria-label={`Ndrysho ${customer.name}`}>Ndrysho</button><button className="danger" type="button" disabled={saving} onClick={() => setPendingDelete(customer)} aria-label={`Fshi ${customer.name}`}>Fshi</button></td>}
                  </tr>
                ))}</tbody>
              </table>
              {visibleCustomers.length === 0 && <p className="empty" role="status">Nuk u gjet asnjë klient.</p>}
            </div>
          </section>
        )}
        <p className="storage-note">{demo ? 'Demonstrim: ndryshimet mbeten gjatë navigimit dhe humbin kur rifreskoni faqen ose ndërroni modalitetin.' : 'Të dhënat ruhen përmes API-së në databazë.'}</p>
    </>
  )
}
