import { useEffect, useState } from 'react'
import Customers from './pages/Customers'
import { getCustomers } from './api/customers'
import { demoCustomers } from './demo'
import type { Customer } from './types/Customer'
import Users from './pages/Users'
import Password from './pages/Password'
import { logout } from './api/auth'
import { canReadCustomers, canWriteCustomers, roleLabels } from './types/User'
import type { UserAccount } from './types/User'

const startInDemo = new URLSearchParams(window.location.search).get('demo') === '1'

export default function Workspace({ user, onSignedOut }: { user: UserAccount | null; onSignedOut?: () => void }) {
  const [page, setPage] = useState<'overview' | 'customers' | 'users' | 'password'>('overview')
  const [demo, setDemo] = useState(startInDemo)
  const canView = demo || (user !== null && canReadCustomers(user))
  const canManage = demo || (user !== null && canWriteCustomers(user))
  const [signingOut, setSigningOut] = useState(false)
  const [accountError, setAccountError] = useState('')
  const [customers, setCustomers] = useState<Customer[]>(() => startInDemo ? demoCustomers.map(c => ({ ...c })) : [])
  const [status, setStatus] = useState<'loading' | 'connected' | 'offline'>(startInDemo || user?.role === 'Warehouse' ? 'connected' : 'loading')
  const [refresh, setRefresh] = useState(0)
  useEffect(() => {
    let active = true
    if (demo || !canView) return
    getCustomers().then(data => { if (active) { setCustomers(data); setStatus('connected') } })
      .catch(() => { if (active) setStatus('offline') })
    return () => { active = false }
  }, [demo, refresh, canView])
  function retry() {
    setStatus('loading'); setCustomers([]); setRefresh(v => v + 1)
  }
  function toggleMode() {
    if (demo && !user) { window.location.assign('/'); return }
    setCustomers(demo ? [] : demoCustomers.map(c => ({ ...c })))
    setStatus(demo && user?.role !== 'Warehouse' ? 'loading' : 'connected')
    setDemo(!demo); setPage('overview')
  }
  async function signOut() {
    if (signingOut) return
    setSigningOut(true); setAccountError('')
    try { await logout(); onSignedOut?.() }
    catch (reason) { setAccountError(reason instanceof Error ? reason.message : 'Dalja dështoi.') }
    finally { setSigningOut(false) }
  }
  const pageTitle = { overview: 'Përmbledhja', customers: 'Klientët', users: 'Përdoruesit', password: 'Fjalëkalimi' }[page]
  const available = status === 'connected'
  return <div className="app-shell">
    <a className="skip-link" href="#workspace">Kalo te përmbajtja</a>
    <aside className="sidebar">
      <div className="brand"><span className="brand-mark">m.</span><span>mini<span className="brand-light">erp</span><small>YJET E MILENIUMIT</small></span></div>
      <p className="nav-label">HAPËSIRA E PUNËS</p>
      <nav aria-label="Navigimi kryesor">
        <button className={`nav-item ${page === 'overview' ? 'active' : ''}`} onClick={() => setPage('overview')} aria-current={page === 'overview' ? 'page' : undefined}><span aria-hidden="true">◫</span> Përmbledhja</button>
        {canView && <button className={`nav-item ${page === 'customers' ? 'active' : ''}`} onClick={() => setPage('customers')} aria-current={page === 'customers' ? 'page' : undefined}><span aria-hidden="true">♧</span> Klientët <span className="nav-number">{available ? customers.length : '—'}</span></button>}
        {!demo && user?.role === 'Administrator' && <button className={`nav-item ${page === 'users' ? 'active' : ''}`} onClick={() => setPage('users')} aria-current={page === 'users' ? 'page' : undefined}><span aria-hidden="true">◉</span> Përdoruesit</button>}
      </nav>
      <p className="nav-label">NË PLANIFIKIM</p>
      <div className="planned-nav"><span>◇ &nbsp; Produktet</span><span>▤ &nbsp; Porositë</span><span>▦ &nbsp; Magazina</span><span>◷ &nbsp; Financat</span></div>
      <div className="sidebar-note"><span className="small-spark">✦</span><strong>Një sistem. Katër role.</strong><p>Shitjet, magazina dhe financat në një hapësirë të përbashkët.</p></div>
      <div className="team-label"><span className="avatar">{user?.displayName.split(/\s+/).map(part => part[0]).slice(0, 2).join('') ?? 'YM'}</span><div>{user?.displayName ?? 'Demonstrim'}<small>{user ? roleLabels[user.role] : 'Vetëm të dhëna fiktive'}</small></div></div>
    </aside>
    <div className="workspace">
      <header className="topbar"><div>Hapësira e punës <span>/</span> <strong>{pageTitle}</strong></div><div className="account-actions"><span className={`connection ${status}`}><i />{demo ? 'Demonstrim' : available ? 'API e lidhur' : status === 'loading' ? 'Duke u lidhur' : 'API e palidhur'}</span>{user && !demo && <><span className="role-pill">{roleLabels[user.role]}</span><button className="text-button" onClick={() => setPage('password')}>Fjalëkalimi</button><button className="text-button" disabled={signingOut} onClick={signOut}>{signingOut ? 'Duke dalë…' : 'Dil'}</button></>}</div></header>
      <div className="mode-bar"><p>{demo ? 'Të dhëna ilustruese. Ndryshimet vlejnë vetëm gjatë demonstrimit.' : 'Modaliteti real · Të dhënat lexohen nga API-ja.'}</p><button className="text-button" onClick={toggleMode}>{demo ? 'Kalo te të dhënat reale' : 'Hap demonstrimin'} ↗</button></div>
      <main id="workspace" tabIndex={-1}>
        {accountError && <p className="message error" role="alert">{accountError}</p>}
        {page === 'customers' && canView ? <Customers key={String(demo)} customers={customers} setCustomers={setCustomers} demo={demo} loading={status === 'loading'} connected={available} retry={retry} canManage={canManage} /> : page === 'users' && user?.role === 'Administrator' && !demo ? <Users currentUser={user} /> : page === 'password' && !demo ? <Password /> : <>
          <div className="page-heading"><div><p className="eyebrow">BIZNESI JUAJ, MË I ORGANIZUAR</p><h1>Gjithçka fillon këtu<span className="accent">.</span></h1><p>Një pamje e qartë e klientëve dhe proceseve të biznesit.</p></div>{canView && <button onClick={() => setPage('customers')}>{canManage ? 'Menaxho klientët ↗' : 'Shiko klientët ↗'}</button>}</div>
          {!canView && <section className="customer-form"><h2>Hapësira e magazinierit</h2><p>Këtu do të punoni me stokun, pranimet dhe dorëzimet. Moduli i magazinës është në përgatitje.</p></section>}
          {status === 'offline' && <div className="message error" role="alert">Nuk mund të lidhemi me API-në. Nisni backend-in ose hapni demonstrimin. <button className="text-button" onClick={retry}>Provo përsëri</button></div>}
          {canView && <section className="stats-grid" aria-label="Përmbledhja e klientëve">{[['Klientë gjithsej', customers.length, 'Regjistri i kontakteve', '♧'], ['Me adresë email', customers.filter(c => c.email).length, 'Kontakt për komunikim', '↗'], ['Me numër telefoni', customers.filter(c => c.phone).length, 'Kontakt i drejtpërdrejtë', '⌁']].map(([label, value, note, icon]) => <article className="stat-card" key={label}><div><span>{label}</span><span className="stat-icon" aria-hidden="true">{icon}</span></div><strong>{available ? String(value).padStart(2, '0') : '—'}</strong><p>{note}</p></article>)}</section>}
          <div className={`overview-grid ${!canView ? 'single-column' : ''}`}>{canView && <section className="panel"><div className="panel-heading"><div><p className="eyebrow">MARRËDHËNIET ME KLIENTËT</p><h2>Regjistri i klientëve</h2></div><button className="text-button" onClick={() => setPage('customers')}>Shiko të gjithë ↗</button></div>
            {!available ? <div className="empty">{status === 'loading' ? 'Duke ngarkuar kontaktet…' : 'Kontaktet do të shfaqen kur API-ja të lidhet.'}</div> : customers.length === 0 ? <div className="empty">Regjistri është bosh. Shtoni klientin e parë.</div> : <div className="contact-list">{customers.slice(0, 5).map(c => <div className="contact-row" key={c.id}><span className="contact-avatar">{c.name.split(' ').map(p => p[0]).slice(0, 2).join('')}</span><div><strong>{c.name}</strong><small>{c.email ?? c.phone ?? 'Pa të dhëna kontakti'}</small></div><span className="record-id">#{String(c.id).padStart(3, '0')}</span></div>)}</div>}
            <div className="panel-footer">{demo ? 'Kontakte ilustruese për prezantim' : 'Burimi: regjistri i klientëve në API'}</div>
          </section>}<section className="flow-panel"><span className="pill">VIZIONI I PROJEKTIT</span><h2>Nga produkti te<br />pagesa.</h2><p>Procesi që lidh ekipin tuaj.</p><ol>{[['01', 'Pranimi', 'Produktet hyjnë në magazinë.'], ['02', 'Porosia', 'Shitësi regjistron kërkesën.'], ['03', 'Dorëzimi', 'Magazinieri përgatit produktet.'], ['04', 'Pagesa', 'Kontabilisti përcjell bilancin.']].map(([n, title, description]) => <li key={n}><span>{n}</span><div><strong>{title}</strong><small>{description}</small></div></li>)}</ol><p className="flow-note">Ky proces është në planifikim.</p></section></div>
          <section className="roles-section"><div className="section-title"><h2>Katër role, një ekip</h2><span>Rolet e sistemit</span></div><div className="roles-grid">{[['AD', 'Administratori', 'Përdoruesit dhe qasja'], ['SH', 'Shitësi', 'Klientët dhe porositë'], ['MG', 'Magazinieri', 'Stoku dhe dorëzimet'], ['KO', 'Kontabilisti', 'Pagesat dhe financat']].map(([initials, title, description]) => <article className="role-card" key={title}><span>{initials}</span><div><h3>{title}</h3><p>{description}</p></div></article>)}</div></section>
        </>}
        <footer className="workspace-footer"><span>miniERP / Yjet e Mileniumit</span><span>{demo ? 'Demonstrim · Pa ruajtje në databazë' : 'Version zhvillimi'}</span></footer>
      </main>
    </div>
  </div>
}
