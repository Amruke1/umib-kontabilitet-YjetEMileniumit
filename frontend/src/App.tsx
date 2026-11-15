import { useEffect, useState } from 'react'
import Customers from './pages/Customers'
import { getCustomers } from './api/customers'
import { demoCustomers } from './demo'
import type { Customer } from './types/Customer'

const startInDemo = new URLSearchParams(window.location.search).get('demo') === '1'

export default function App() {
  const [page, setPage] = useState<'overview' | 'customers'>('overview')
  const [demo, setDemo] = useState(startInDemo)
  const [customers, setCustomers] = useState<Customer[]>(() => startInDemo ? demoCustomers.map(c => ({ ...c })) : [])
  const [status, setStatus] = useState<'loading' | 'connected' | 'offline'>(startInDemo ? 'connected' : 'loading')
  const [refresh, setRefresh] = useState(0)
  useEffect(() => {
    let active = true
    if (demo) return
    getCustomers().then(data => { if (active) { setCustomers(data); setStatus('connected') } })
      .catch(() => { if (active) setStatus('offline') })
    return () => { active = false }
  }, [demo, refresh])
  function retry() {
    setStatus('loading'); setCustomers([]); setRefresh(v => v + 1)
  }
  function toggleMode() {
    setCustomers(demo ? [] : demoCustomers.map(c => ({ ...c })))
    setStatus(demo ? 'loading' : 'connected')
    setDemo(!demo); setPage('overview')
  }
  const available = status === 'connected'
  return <div className="app-shell">
    <a className="skip-link" href="#workspace">Kalo te përmbajtja</a>
    <aside className="sidebar">
      <div className="brand"><span className="brand-mark">m.</span><span>mini<span className="brand-light">erp</span><small>YJET E MILENIUMIT</small></span></div>
      <p className="nav-label">HAPËSIRA E PUNËS</p>
      <nav aria-label="Navigimi kryesor">
        <button className={`nav-item ${page === 'overview' ? 'active' : ''}`} onClick={() => setPage('overview')} aria-current={page === 'overview' ? 'page' : undefined}><span aria-hidden="true">◫</span> Përmbledhja</button>
        <button className={`nav-item ${page === 'customers' ? 'active' : ''}`} onClick={() => setPage('customers')} aria-current={page === 'customers' ? 'page' : undefined}><span aria-hidden="true">♧</span> Klientët <span className="nav-number">{available ? customers.length : '—'}</span></button>
      </nav>
      <p className="nav-label">NË PLANIFIKIM</p>
      <div className="planned-nav"><span>◇ &nbsp; Produktet</span><span>▤ &nbsp; Porositë</span><span>▦ &nbsp; Magazina</span><span>◷ &nbsp; Financat</span></div>
      <div className="sidebar-note"><span className="small-spark">✦</span><strong>Një sistem. Katër role.</strong><p>Shitjet, magazina dhe financat në një hapësirë të përbashkët.</p></div>
      <div className="team-label"><span className="avatar">YM</span><div>Yjet e Mileniumit<small>Mini ERP · Projekti ekipor</small></div></div>
    </aside>
    <div className="workspace">
      <header className="topbar"><div>Hapësira e punës <span>/</span> <strong>{page === 'overview' ? 'Përmbledhja' : 'Klientët'}</strong></div><span className={`connection ${status}`}><i />{demo ? 'Demonstrim' : available ? 'API e lidhur' : status === 'loading' ? 'Duke u lidhur' : 'API e palidhur'}</span></header>
      <div className="mode-bar"><p>{demo ? 'Të dhëna ilustruese. Ndryshimet vlejnë vetëm gjatë demonstrimit.' : 'Modaliteti real · Të dhënat lexohen nga API-ja.'}</p><button className="text-button" onClick={toggleMode}>{demo ? 'Kalo te të dhënat reale' : 'Hap demonstrimin'} ↗</button></div>
      <main id="workspace" tabIndex={-1}>
        {page === 'customers' ? <Customers key={String(demo)} customers={customers} setCustomers={setCustomers} demo={demo} loading={status === 'loading'} connected={available} retry={retry} /> : <>
          <div className="page-heading"><div><p className="eyebrow">BIZNESI JUAJ, MË I ORGANIZUAR</p><h1>Gjithçka fillon këtu<span className="accent">.</span></h1><p>Një pamje e qartë e klientëve dhe proceseve të biznesit.</p></div><button onClick={() => setPage('customers')}>Menaxho klientët ↗</button></div>
          {status === 'offline' && <div className="message error" role="alert">Nuk mund të lidhemi me API-në. Nisni backend-in ose hapni demonstrimin. <button className="text-button" onClick={retry}>Provo përsëri</button></div>}
          <section className="stats-grid" aria-label="Përmbledhja e klientëve">{[['Klientë gjithsej', customers.length, 'Regjistri i kontakteve', '♧'], ['Me adresë email', customers.filter(c => c.email).length, 'Kontakt për komunikim', '↗'], ['Me numër telefoni', customers.filter(c => c.phone).length, 'Kontakt i drejtpërdrejtë', '⌁']].map(([label, value, note, icon]) => <article className="stat-card" key={label}><div><span>{label}</span><span className="stat-icon" aria-hidden="true">{icon}</span></div><strong>{available ? String(value).padStart(2, '0') : '—'}</strong><p>{note}</p></article>)}</section>
          <div className="overview-grid"><section className="panel"><div className="panel-heading"><div><p className="eyebrow">MARRËDHËNIET ME KLIENTËT</p><h2>Regjistri i klientëve</h2></div><button className="text-button" onClick={() => setPage('customers')}>Shiko të gjithë ↗</button></div>
            {!available ? <div className="empty">{status === 'loading' ? 'Duke ngarkuar kontaktet…' : 'Kontaktet do të shfaqen kur API-ja të lidhet.'}</div> : customers.length === 0 ? <div className="empty">Regjistri është bosh. Shtoni klientin e parë.</div> : <div className="contact-list">{customers.slice(0, 5).map(c => <div className="contact-row" key={c.id}><span className="contact-avatar">{c.name.split(' ').map(p => p[0]).slice(0, 2).join('')}</span><div><strong>{c.name}</strong><small>{c.email ?? c.phone ?? 'Pa të dhëna kontakti'}</small></div><span className="record-id">#{String(c.id).padStart(3, '0')}</span></div>)}</div>}
            <div className="panel-footer">{demo ? 'Kontakte ilustruese për prezantim' : 'Burimi: regjistri i klientëve në API'}</div>
          </section><section className="flow-panel"><span className="pill">VIZIONI I PROJEKTIT</span><h2>Nga produkti te<br />pagesa.</h2><p>Procesi që lidh ekipin tuaj.</p><ol>{[['01', 'Pranimi', 'Produktet hyjnë në magazinë.'], ['02', 'Porosia', 'Shitësi regjistron kërkesën.'], ['03', 'Dorëzimi', 'Magazinieri përgatit produktet.'], ['04', 'Pagesa', 'Kontabilisti përcjell bilancin.']].map(([n, title, description]) => <li key={n}><span>{n}</span><div><strong>{title}</strong><small>{description}</small></div></li>)}</ol><p className="flow-note">Ky proces është në planifikim.</p></section></div>
          <section className="roles-section"><div className="section-title"><h2>Katër role, një ekip</h2><span>Rolet e planifikuara të sistemit</span></div><div className="roles-grid">{[['AD', 'Administratori', 'Përdoruesit dhe qasja'], ['SH', 'Shitësi', 'Klientët dhe porositë'], ['MG', 'Magazinieri', 'Stoku dhe dorëzimet'], ['KO', 'Kontabilisti', 'Pagesat dhe financat']].map(([initials, title, description]) => <article className="role-card" key={title}><span>{initials}</span><div><h3>{title}</h3><p>{description}</p></div></article>)}</div></section>
        </>}
        <footer className="workspace-footer"><span>miniERP / Yjet e Mileniumit</span><span>{demo ? 'Demonstrim · Pa ruajtje në databazë' : 'Version zhvillimi'}</span></footer>
      </main>
    </div>
  </div>
}
