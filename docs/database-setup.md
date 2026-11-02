# Konfigurimi lokal i PostgreSQL

Backend-i përdor .NET 10, EF Core 10 dhe paketën
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3. Çdo anëtar konfiguron
databazën dhe kredencialet në kompjuterin e vet.

## 1. Përgatit databazën

Instalo PostgreSQL dhe, sipas dëshirës, pgAdmin. Sigurohu që serveri PostgreSQL
është nisur. Porta e zakonshme është `5432`.

Në pgAdmin, krijo databazën `mini_erp`. Hap Query Tool të kësaj databaze dhe
ekzekuto `Database/customers.sql` nga repository. Nëse `public.customers`
ekziston nga detyra e parë, përdor tabelën ekzistuese pa e rikrijuar.

Përputhja e modelit është:

| Modeli C# | Kolona PostgreSQL | Tipi dhe kufizimet |
| --- | --- | --- |
| `Customer.Id` | `id` | `integer`, primary key, identity by default |
| `Customer.Name` | `name` | `varchar(150)`, NOT NULL, jo bosh ose vetëm hapësira |
| `Customer.Email` | `email` | `varchar(254)`, NULL lejohet |
| `Customer.Phone` | `phone` | `varchar(30)`, NULL lejohet |

`AppDbContext` i lidh shprehimisht këta emra me tabelën `public.customers`.
Email-i kontrollohet nga validimi i modelit në `CustomerService` kur plotësohet.

## 2. Ruaj connection string-un lokalisht

Nga folderi kryesor i repository-t, ekzekuto komandën më poshtë pasi ta
zëvendësosh `YOUR_LOCAL_PASSWORD` me fjalëkalimin tënd lokal. Përshtat host-in,
portën dhe username-in nëse instalimi yt përdor vlera të tjera.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=mini_erp;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project backend/MiniERP.Api.csproj
```

Projekti e ka tashmë `UserSecretsId`; nuk nevojitet `dotnet user-secrets init`.
User Secrets ruhet jashtë repository-t dhe ngarkohet në ambientin Development,
të cilin e aktivizon launch profile `http`.

Ruaj në GitHub vetëm kodin dhe shembujt me placeholder. Mos vendos
fjalëkalime reale në `Program.cs`, `appsettings.json`, dokumentim ose commit-e.
User Secrets është mjet për zhvillim lokal dhe nuk i enkripton vlerat.

Alternativë për sesionin aktual të PowerShell-it:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=mini_erp;Username=postgres;Password=YOUR_LOCAL_PASSWORD'
```

Kjo variabël ka përparësi ndaj User Secrets. Nëse kthehesh te User Secrets,
hiqe nga ai sesion me:

```powershell
Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
```

## 3. Ndërto dhe nis backend-in

```powershell
dotnet build backend/MiniERP.Api.csproj
dotnet run --project backend/MiniERP.Api.csproj --launch-profile http
```

API-ja niset në `http://localhost:5080`. Nëse konfigurimi mungon, aplikacioni
ndalet me mesazh që kërkon `ConnectionStrings:DefaultConnection`.

## 4. Provo leximin dhe ruajtjen

Në një terminal tjetër:

```powershell
Invoke-RestMethod -Uri 'http://localhost:5080/api/customers'

$customerBody = @{
    name = 'Klient Test PostgreSQL'
    email = 'klient@example.com'
    phone = '+383 44 123 456'
} | ConvertTo-Json

Invoke-RestMethod -Uri 'http://localhost:5080/api/customers' -Method Post -ContentType 'application/json' -Body $customerBody
```

POST duhet të kthejë HTTP 201 me klientin dhe ID-në e gjeneruar nga PostgreSQL.
Në pgAdmin, në `mini_erp`, kontrollo:

```sql
SELECT id, name, email, phone
FROM public.customers
ORDER BY id;
```

Ndal backend-in me `Ctrl+C`, nise përsëri dhe përsërit GET. Klienti duhet të
jetë ende në listë. Rifreskimi i faqes së React duhet të shfaqë të njëjtat të
dhëna. Çdo POST i testit shton një regjistrim të ri fiktiv.

Provo gjithashtu emër bosh ose vetëm hapësira dhe email të pavlefshëm: API-ja
duhet të kthejë HTTP 400 dhe të mos ruajë klientin. Email-i dhe telefoni mund
të mungojnë; këto vlera ruhen si NULL.

## Bashkëpunimi dhe ndryshimet e skemës

- Emri i kontekstit: `MiniERP.Data.AppDbContext`.
- Koleksioni i klientëve: `AppDbContext.Customers`.
- Çelësi i konfigurimit: `ConnectionStrings:DefaultConnection`.
- Metodat ekzistuese: `CustomerService.GetAll()` dhe `CustomerService.Add(Customer input)`.
- Konteksti dhe shërbimi regjistrohen si scoped në `Program.cs`.
- Frontend-i dhe kontrata JSON e GET/POST mbeten me `id`, `name`, `email`, `phone`.

Për këtë fazë, skema krijohet me `Database/customers.sql`. Aplikacioni nuk
krijon ose ndryshon automatikisht tabela gjatë nisjes. Para përdorimit të
migrimeve EF Core, Amra dhe Th duhet të dakordojnë si do të trajtohet tabela
ekzistuese; një migrim fillestar që krijon `customers` nuk aplikohet mbi të
njëjtën tabelë që ekziston tashmë.

## Zgjidhja e problemeve

- Connection string mungon: konfiguro User Secrets dhe përdor profile `http`,
  ose vendos variablën e ambientit.
- Connection refused: kontrollo që PostgreSQL është nisur dhe porta është e saktë.
- Password authentication failed: kontrollo kredencialet lokale.
- Database does not exist: krijo `mini_erp` ose korrigjo emrin në connection string.
- Relation customers does not exist: ekzekuto skriptin SQL në databazën e saktë.
- `GET /health` kthen 200, por klientët dështojnë: health endpoint provon vetëm API-në;
  kontrollo lidhjen me databazën përmes `GET /api/customers`.
