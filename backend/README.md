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
