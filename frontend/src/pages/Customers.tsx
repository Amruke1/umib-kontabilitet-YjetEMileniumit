import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { addCustomer, getCustomers } from '../api/customers'
import type { Customer } from '../types/Customer'

export default function Customers() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const submitting = useRef(false)

  useEffect(() => {
    let active = true
    getCustomers().then((data) => { if (active) setCustomers(data) })
      .catch((reason: Error) => { if (active) setError(reason.message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting.current || loading) return
    setError('')
    setSuccess('')
    if (!name.trim()) { setError('Emri është i detyrueshëm.'); return }
    submitting.current = true
    setSaving(true)
    try {
      const customer = await addCustomer({ name, email: email || null, phone: phone || null })
      setCustomers((current) => [...current, customer])
      setName(''); setEmail(''); setPhone('')
      setSuccess('Klienti u shtua me sukses.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Ruajtja dështoi.')
    } finally {
      submitting.current = false
      setSaving(false)
    }
  }

  return (
    <>
      <header className="app-header"><span className="brand-mark" aria-hidden="true">M</span> Mini ERP</header>
      <main>
        <div className="page-heading">
          <div><p className="eyebrow">REGJISTRI I KLIENTËVE</p><h1>Klientët</h1><p>Të dhënat e kontaktit të klientëve tuaj.</p></div>
          <span className="count">{customers.length} klientë</span>
        </div>
        <form className="customer-form" onSubmit={submit}>
          <h2>Shto klient</h2>
          <div className="form-fields">
            <label><span>Emri <span aria-hidden="true">*</span></span><input name="name" required maxLength={150} value={name} onChange={(event) => setName(event.target.value)} disabled={saving} autoComplete="name" /></label>
            <label>Email<input name="email" type="email" maxLength={254} value={email} onChange={(event) => setEmail(event.target.value)} disabled={saving} autoComplete="email" /></label>
            <label>Telefoni<input name="phone" type="tel" maxLength={30} value={phone} onChange={(event) => setPhone(event.target.value)} disabled={saving} autoComplete="tel" /></label>
          </div>
          <button type="submit" disabled={saving || loading}>{saving ? 'Duke ruajtur…' : 'Shto klientin'}</button>
        </form>
        {error && <p className="message error" role="alert">{error}</p>}
        {success && <p className="message success" role="status">{success}</p>}
        {loading ? <p role="status">Duke ngarkuar klientët…</p> : customers.length === 0 ? (
          <p className="empty">{error ? 'Lista nuk u ngarkua. Rifreskoni faqen për të provuar përsëri.' : 'Nuk ka klientë. Shtoni klientin e parë.'}</p>
        ) : (
          <section className="table-panel" aria-label="Lista e klientëve">
            <div className="table-scroll" tabIndex={0} role="region" aria-label="Tabela e klientëve; rrëshqitni horizontalisht në ekran të vogël">
              <table>
                <caption className="visually-hidden">Klientët dhe të dhënat e kontaktit</caption>
                <thead><tr><th scope="col">ID</th><th scope="col">Emri</th><th scope="col">Email</th><th scope="col">Telefoni</th></tr></thead>
                <tbody>{customers.map((customer) => (
                  <tr key={customer.id}>
                    <td className="customer-id">{customer.id}</td><th scope="row">{customer.name}</th>
                    <td>{customer.email ?? <span className="missing">Nuk është vendosur</span>}</td>
                    <td>{customer.phone ?? <span className="missing">Nuk është vendosur</span>}</td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          </section>
        )}
        <p className="storage-note">Të dhënat ruhen në databazë dhe mbeten edhe pas ristartimit të backend-it.</p>
      </main>
    </>
  )
}
