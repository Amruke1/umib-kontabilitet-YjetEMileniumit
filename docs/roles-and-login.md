# Hyrja dhe rolet

Përdoruesit hyjnë me emër përdoruesi dhe fjalëkalim. Llogaritë, rolet dhe çelësat
e sesioneve ruhen në PostgreSQL, në skemën `auth`. Tabela ekzistuese
`public.customers` vazhdon të përdoret pa ndryshime.

## Lejet e klientëve

| Roli në faqe | Vlera në API | Lista dhe leximi | Shtimi, ndryshimi, fshirja | Përdoruesit dhe rolet |
| --- | --- | --- | --- | --- |
| Administratori | `Administrator` | Po | Po | Po |
| Shitësi | `Salesperson` | Po | Po | Jo |
| Kontabilisti | `Accountant` | Po | Jo | Jo |
| Magazinieri | `Warehouse` | Jo | Jo | Jo |

API-ja i kontrollon këto leje edhe kur kërkesa dërgohet jashtë faqes. Pa hyrje
kthen 401; kur roli nuk ka leje kthen 403. Modulet e porosive, magazinës dhe
financave mbeten për t'u ndërtuar. Magazinieri ka pamjen e vet fillestare dhe
nuk merr të dhëna klientësh nga API-ja.

## Administratori i parë

1. Në Render, Blueprint-i gjeneron `Auth__BootstrapToken` si variabël private.
   Mos e shkruaj në Git, Trello ose chat.
2. Hap faqen dhe zgjidh **Konfiguro administratorin e parë**.
3. Plotëso emrin, përdoruesin, një fjalëkalim të ri dhe kodin privat të konfigurimit.

Ky konfigurim lejohet vetëm kur databaza nuk ka ende përdorues. Nuk ka regjistrim
publik dhe roli i administratorit fillestar vendoset vetëm nga serveri.
Ndryshimet e administratorëve kryhen brenda një transaksioni të serializuar;
administratori nuk mund ta çaktivizojë ose t'ia heqë rolin llogarisë së vet.

## Konfigurimi lokal

Nga rrënja e projektit:

```powershell
dotnet tool restore
dotnet user-secrets set 'Auth:BootstrapToken' 'YOUR_PRIVATE_RANDOM_TOKEN' --project backend/MiniERP.Api.csproj
dotnet ef database update --context AuthDbContext --project backend/MiniERP.Api.csproj
```

Përdor një token të rastësishëm me të paktën 32 karaktere. Konfiguro paraprakisht
`ConnectionStrings:DefaultConnection` sipas `database-setup.md`. Fjalëkalimet reale
dhe token-i qëndrojnë te User Secrets, jo në repository.

Migrimi `AddIdentityAndRoles` krijon vetëm tabelat në skemën `auth`. Për rishikim,
SQL-ja përkatëse është te `Database/auth-roles.sql`. Përdor migrimet si mënyrën
e menaxhimit; mos i trajto SQL-në e gjeneruar dhe migrimin si dy ndryshime të ndara.
Në Render, `Database__InitializeSchema=true` aplikon migrimet gjatë nisjes.

Faqja dhe API-ja duhet të jenë në të njëjtin site për cookie-t Strict. Në zhvillim,
përdor të njëjtin host për të dyja, p.sh. `localhost:5173` dhe `localhost:5080`.
Kur `VITE_API_URL` nuk është caktuar, frontend-i përdor të njëjtin hostname lokal.

## Sesioni dhe kërkesat

- Fjalëkalimet ruhen me hash-in e ASP.NET Core Identity.
- Cookie-t e hyrjes janë HttpOnly, SameSite Strict dhe Secure online. CookiePolicy
  e ruan Secure edhe kur Render e përfundon lidhjen HTTPS para backend-it.
- POST, PUT dhe DELETE kërkojnë `X-CSRF-TOKEN`, të marrë nga `GET /api/auth/csrf`.
  Frontend-i e merr para çdo ndryshimi.
- Pesë fjalëkalime të gabuara e bllokojnë hyrjen për 15 minuta. Hyrja dhe
  konfigurimi fillestar kanë edhe kufizim kërkesash.
- Ndryshimi i rolit ose çaktivizimi i llogarisë i refuzon sesionet e vjetra në
  kërkesën tjetër. Përdoruesi hyn përsëri për lejet e reja.
- Dalja dhe ndryshimi i fjalëkalimit i zhvlerësojnë sesionet e vjetra. Pas ndryshimit
  të fjalëkalimit, shfletuesi aktual merr sesion të ri.
- Çelësat e mbrojtjes së cookie-ve ruhen në databazën private, që sesionet të
  vazhdojnë pas ripublikimit në Render.
- Demonstrimi `?demo=1` përdor vetëm të dhëna fiktive në memorie.

## Testimi

Kontrolli në GitHub përdor PostgreSQL 18 të përkohshëm dhe të dhëna fiktive.
Nuk përdor kredencialet e databazës së ekipit. Testet kontrollojnë katër rolet,
401/403, CSRF, konfigurimin e administratorit, revokimin e qasjes, daljen,
ndryshimin e fjalëkalimit dhe sesionin në një instancë të re të aplikacionit.

Lokalisht përdor vetëm një databazë të veçantë `mini_erp_tests` dhe vendos
`MINIERP_TEST_DATABASE_CONNECTION` jashtë Git-it. Testet e pastrojnë skemën `auth`
dhe tabelën e klientëve në këtë databazë. Refuzojnë host-e ose emra të tjerë.
Pa variablën, testet e integrimit shënohen skipped.

```powershell
dotnet test MiniERP.sln
```

Testi CRUD kërkon një llogari Administrator ose Shitës:

```powershell
$credential = Get-Credential -Message 'Llogaria e aplikacionit Mini ERP'
./scripts/Test-Customers.ps1 -BaseUrl 'https://mini-erp-yjet-e-mileniumit.onrender.com' -Credential $credential
```

Fjalëkalimi plotësohet në dritaren e PowerShell-it dhe nuk shkruhet në komandë.

## Referencat

- https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0
