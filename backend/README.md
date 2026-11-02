# Mini ERP API

Kërkon .NET SDK 10. Projekti përdor ASP.NET Core Web API me Controllers.

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

## Klientët në memorie

Nis frontend-in në portin 5173 (shih `frontend/README.md`). CORS lejon
`http://localhost:5173` dhe `http://127.0.0.1:5173`.

- `GET /api/customers`: lista, HTTP 200.
- `POST /api/customers`: JSON me `name`, `email` dhe `phone`; HTTP 201 me klientin e krijuar.
- Email dhe phone janë opsionalë dhe lejojnë null; ID caktohet vetëm nga serveri.
- Të dhënat e pavlefshme kthejnë HTTP 400 me mesazhe validimi.

CustomerService është singleton dhe mbron listën/ID-të me lock. Të dhënat ndahen
ndërmjet kërkesave brenda të njëjtit proces; humbin kur backend-i ristartohet.
Nuk ka databazë apo ruajtje në disk.
