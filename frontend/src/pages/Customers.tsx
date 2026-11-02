import { customers } from '../data/customers'

export default function Customers() {
  return (
    <>
      <header className="app-header"><span className="brand-mark" aria-hidden="true">M</span> Mini ERP</header>
      <main>
        <div className="page-heading">
          <div><p className="eyebrow">REGJISTRI I KLIENTËVE</p><h1>Klientët</h1><p>Të dhënat e kontaktit të klientëve tuaj.</p></div>
          <span className="count">{customers.length} klientë</span>
        </div>
        <section className="table-panel" aria-label="Lista e klientëve">
          <div className="table-scroll" tabIndex={0} role="region" aria-label="Tabela e klientëve; rrëshqitni horizontalisht në ekran të vogël">
            <table>
              <caption className="visually-hidden">Klientët dhe të dhënat e kontaktit</caption>
              <thead><tr><th scope="col">ID</th><th scope="col">Emri</th><th scope="col">Email</th><th scope="col">Telefoni</th></tr></thead>
              <tbody>{customers.map((customer) => (
                <tr key={customer.id}>
                  <td className="customer-id">{String(customer.id).padStart(2, '0')}</td>
                  <th scope="row">{customer.name}</th>
                  <td>{customer.email ?? <span className="missing">Nuk është vendosur</span>}</td>
                  <td>{customer.phone ?? <span className="missing">Nuk është vendosur</span>}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
          <p className="table-note">Të dhëna fiktive për demonstrim.</p>
        </section>
      </main>
    </>
  )
}
