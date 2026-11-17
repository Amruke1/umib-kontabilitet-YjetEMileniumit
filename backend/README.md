# Mini ERP API

Për hyrjen, katër rolet dhe konfigurimin e administratorit të parë, shih
[Hyrja dhe rolet](../docs/roles-and-login.md).

Për publikimin falas të React + API në Render me PostgreSQL në Neon, ndiq
[konfigurimin online](../docs/online-deployment.md).

Kërkon .NET SDK 10 dhe PostgreSQL. Projekti përdor ASP.NET Core Web API me
Controllers, EF Core dhe Npgsql.

Para nisjes, përgatit databazën lokale `mini_erp`, tabelën `public.customers`
dhe connection string-un sipas [udhëzimit të databazës](../docs/database-setup.md).
Fjalëkalimi ruhet në .NET User Secrets ose në variabla të ambientit, jashtë Git-it.

Nga folderi kryesor i repository-t:

```powershell
dotnet build backend/MiniERP.Api.csproj
dotnet run --project backend/MiniERP.Api.csproj --launch-profile http
```

Adresa lokale: http://localhost:5080 (HTTP).

Në një terminal tjetër, kontrollo endpoint-in:

```powershell
curl.exe -i http://localhost:5080/health
```

Përgjigjja e pritur: HTTP 200 dhe JSON `{"status":"ok"}`.
Për të ndalur API-në, shtyp `Ctrl+C` në terminalin ku po ekzekutohet.

## Klientët në PostgreSQL

Nis frontend-in në portin 5173 (shih `frontend/README.md`). CORS lejon
`http://localhost:5173` dhe `http://127.0.0.1:5173`.

- `GET /api/customers`: lista, HTTP 200.
- `POST /api/customers`: JSON me `name`, `email` dhe `phone`; HTTP 201 me klientin e krijuar.
- Email dhe phone janë opsionalë dhe lejojnë null; ID gjenerohet nga PostgreSQL.
- Të dhënat e pavlefshme kthejnë HTTP 400 me mesazhe validimi.

`CustomerService` dhe `AppDbContext` kanë lifetime scoped. Metodat ekzistuese
`GetAll()` dhe `Add(Customer input)` ruhen, ndërsa të dhënat lexohen dhe ruhen
në `public.customers`. Regjistrimet mbeten edhe pas ristartimit të backend-it.

`GET /health` kontrollon nëse API-ja është nisur. `GET /health/ready` kontrollon
edhe nëse tabela `customers` mund të lexohet nga PostgreSQL; kthen 503 nëse
lidhja ose tabela nuk është në dispozicion.
