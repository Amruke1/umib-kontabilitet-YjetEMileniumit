# Publikimi falas: Render dhe Neon

React dhe API-ja ASP.NET Core publikohen si një aplikacion në Render Free.
PostgreSQL ruhet veçmas në Neon Free. Të dhënat nuk varen nga kompjuteri lokal
ose nga disku i përkohshëm i Render-it.

## Gjendja e konfigurimit më 17 nëntor 2026

- Projekti Neon **mini-erp-yjet-e-mileniumit** është krijuar në planin Free,
  në AWS Frankfurt, me databazën `mini_erp` dhe branch-in `production`.
- `Database/customers.sql` është ekzekutuar me sukses. Një kontroll te
  `information_schema.columns` konfirmoi kolonat `id`, `name`, `email`, `phone`,
  gjatësitë, NULL dhe identity sipas modelit ekzistues.
- [Hap SQL Editor të projektit](https://console.neon.tech/app/projects/red-shape-41284864/branches/br-falling-heart-b9bh6v8u/sql-editor?database=mini_erp).
  Kjo adresë kërkon hyrje në llogarinë përkatëse dhe nuk përmban kredenciale.
- Aplikacioni është publikuar në **Render Free**:
  [Hap Mini ERP](https://mini-erp-yjet-e-mileniumit.onrender.com/).
  Frontend-i dhe API-ja përdorin të njëjtën adresë; të dhënat ruhen në Neon.
- GitHub App i Render-it ka qasje te repository privat. Blueprint-i
  `mini-erp-yjet-e-mileniumit` ndjek branch-in `main` dhe publikon ndryshimet
  pas bashkimit të Pull Request-eve, pavarësisht autorit të commit-it.
- Connection string-u real ruhet vetëm te konfigurimi privat i Render-it.
  `/health` dhe `/health/ready` kthyen 200; rrugët API që nuk ekzistojnë kthyen 404.
  Për kontrollin e plotë të klientëve dhe ruajtjes, ndiq seksionin 3 më poshtë.

## Si punon ekipi

Çdo anëtar vazhdon me branch, commit, push dhe Pull Request në GitHub.
Një llogari hostimi me qasje në repository e lidh Render-in me branch-in `main`.
Pas bashkimit në `main`, Render ndërton dhe publikon ndryshimet automatikisht.
Kontribuesit e kodit nuk kanë nevojë të shtohen si përdorues të dashboard-it
të Render-it. Nuk ndryshohen emrat ose autorët e commit-eve.

## 1. Krijo databazën në Neon Free

1. Krijo një projekt në planin **Free**, afër Render-it, p.sh. Frankfurt.
2. Krijo ose zgjidh databazën `mini_erp`.
3. Merr connection string-un e formatit **Npgsql/.NET** nga Connect.
4. Ruaje në Render si `ConnectionStrings__DefaultConnection`.

Formati i shembullit është:

```text
Host=YOUR_NEON_HOST;Port=5432;Database=mini_erp;Username=YOUR_NEON_USER;Password=YOUR_NEON_PASSWORD;SSL Mode=VerifyFull
```

Përdor host-in, username-in dhe parametrat TLS që jep Neon. Mos përdor
fjalëkalimin e PostgreSQL lokal. Mos vendos connection string-un real në Git,
README, frontend, `VITE_*` ose argumentet e build-it të Docker-it.

Databaza online fillon bosh. Regjistrimet lokale mbeten në databazën lokale;
kopjimi i tyre në Neon bëhet veçmas kur kërkohet, me të dhëna fiktive për projektin.

## 2. Publiko aplikacionin në Render Free

Përdor **New → Blueprint** me këtë repository dhe `render.yaml`, ose krijo
një **Web Service** me cilësimet më poshtë:

| Cilësimi | Vlera |
| --- | --- |
| Branch | `main` |
| Root Directory | bosh, rrënja e repository-t |
| Runtime/Language | Docker |
| Dockerfile Path | `./Dockerfile` |
| Docker Build Context | `.` |
| Region | Frankfurt |
| Instance Type | **Free** |
| Health Check Path | `/health/ready` |
| Auto-deploy | çdo commit në `main` |

Variablat e serverit:

| Çelësi | Vlera |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `PORT` | `10000` |
| `Database__InitializeSchema` | `true` |
| `ConnectionStrings__DefaultConnection` | connection string-u sekret i Neon |

`render.yaml` e kërkon sekretin gjatë konfigurimit; nuk përmban kredenciale.
Zgjidh vetëm planin Free. Ky konfigurim nuk krijon databazë Render dhe nuk
kërkon disk të paguar, domain të blerë ose përdorues të tjerë të paguar.

Dockerfile ndërton frontend-in dhe API-në. React kopjohet në `wwwroot`, ndërsa
ASP.NET Core shërben faqen dhe `/api/customers` në të njëjtën adresë HTTPS.
Në build-in e Render-it, lëre **`VITE_API_URL` të pavendosur**. Kjo shmang
thirrjet nga faqja online drejt `localhost` dhe konfigurimin e veçantë CORS.

Gjatë nisjes, opsioni `Database__InitializeSchema=true` ekzekuton skriptin
`Database/customers.sql` me `CREATE TABLE IF NOT EXISTS`. Nuk fshin klientë dhe
nuk ndryshon kolonat e një tabele ekzistuese. Ndryshimet e ardhshme të skemës
duhet të bëhen me migrime ose skripte të dakorduara nga ekipi.

## 3. Kontrollo publikimin

Në adresën HTTPS që kthen Render:

1. `/health` duhet të kthejë 200 me `status: ok`.
2. `/health/ready` duhet të kthejë 200 me `status: ready`.
3. `/api/customers` duhet të kthejë listën nga Neon.
4. Në faqen kryesore, përdor modalitetin real, shto një klient fiktiv dhe rifresko.
5. Ristarto ose ripubliko shërbimin dhe kontrollo që klienti mbetet në listë.

Nga PowerShell 7 mund të ekzekutosh edhe kontrollin ekzistues CRUD:

```powershell
pwsh -File scripts/Test-Customers.ps1 -BaseUrl 'https://YOUR_SERVICE.onrender.com'
```

Ky skript krijon dhe fshin vetëm klientin e vet fiktiv të testit.
Një rrugë API që nuk ekziston duhet të kthejë 404, jo faqen HTML të React-it.

## Kufijtë falas dhe të dhënat

- Render Free pushon pas 15 minutash pa trafik; hapja tjetër mund të zgjasë rreth
  një minutë. Kjo nuk i fshin regjistrimet në Neon.
- Planet falas kanë kufij mujorë të trafikut, build-eve, compute dhe storage.
  Mbaji shërbimet në planin Free dhe kontrollo përdorimin në dashboard.
- Neon e ndal compute-in pas pasivitetit dhe e nis automatikisht kur vjen një kërkesë.
- Ruaj kopje të të dhënave të rëndësishme me mjetet e PostgreSQL; push në GitHub
  ruan kodin dhe skemën, jo regjistrimet e databazës.
- API-ja aktuale nuk ka ende autentikim; përdor vetëm të dhëna fiktive të projektit.
  Kontrolli i përdoruesve dhe roleve është detyrë më vete.

Modaliteti Demonstrim i ndërfaqes përdor vetëm shembuj në memorie dhe nuk ruan në Neon.

## Nëse mbahet edhe frontend-i i Vercel-it

Adresa e re e Render-it përmban tashmë të gjithë aplikacionin. Nëse ekipi dëshiron
të mbajë edhe frontend-in e Vercel-it, pronari i atij projekti vendos
`VITE_API_URL=https://YOUR_SERVICE.onrender.com` dhe e ripublikon frontend-in.
Në Render vendos gjithashtu `Cors__AllowedOrigins__0=https://YOUR_SITE.vercel.app`.
Mos përdor wildcard për origjinat. Kjo është vetëm alternativë; versioni i plotë
në Render nuk varet nga Vercel-i.

## Burimet

- https://render.com/docs/free
- https://render.com/docs/docker
- https://render.com/docs/git-provider
- https://render.com/docs/blueprint-spec
- https://neon.com/pricing
- https://neon.com/docs/guides/dotnet-npgsql
