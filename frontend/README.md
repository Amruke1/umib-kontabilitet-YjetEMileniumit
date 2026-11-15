# Mini ERP — Frontend

React + TypeScript me Vite. Kërkon Node.js 22.12+ ose 24+ dhe npm.

## Demonstrimi pa databazë

Nis frontend-in me `npm run dev` dhe hap `http://localhost:5173/?demo=1`.
Parametri `demo=1` hap panelin me kontakte ilustruese pa kërkuar backend.
Shtimi, ndryshimi, kërkimi dhe fshirja e kontakteve funksionojnë në memorien
e sesionit. Navigimi ruan ndryshimet; rifreskimi ose ndërrimi i modalitetit
i rivendos ato. Butoni në krye kalon mes demonstrimit dhe API-së reale.
Produktet, porositë, magazina, financat dhe rolet janë të shënuara si të
planifikuara dhe ende nuk janë implementuar.

Nga rrënja e repository-t, nis backend-in në një terminal:

Së pari përgatit PostgreSQL dhe connection string-un lokal sipas
[udhëzimit të databazës](../docs/database-setup.md).

```powershell
dotnet run --project backend/MiniERP.Api.csproj --launch-profile http
```

Në një terminal tjetër:

```powershell
cd frontend
npm ci
Copy-Item .env.example .env.local
npm run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Hap http://127.0.0.1:5173. `VITE_API_URL` në `.env.local` përcakton adresën e
backend-it (parazgjedhje http://localhost:5080). Rinis Vite pas ndryshimit të saj.
CORS i backend-it lejon localhost dhe 127.0.0.1 në portin 5173.

```powershell
npm run build
```

Faqja `src/pages/Customers.tsx` merr listën nga API-ja dhe dërgon formularin me POST.
Tipi është në `src/types/Customer.ts`; thirrjet HTTP janë në `src/api/customers.ts`.
Email dhe phone lejojnë null. Nuk përdoret localStorage.

Të dhënat ruhen në PostgreSQL dhe mbeten edhe pas ristartimit të backend-it.
Rifreskimi i frontend-it e merr sërish listën nga backend-i aktiv.
Për të kontrolluar gabimin e lidhjes, ndal backend-in dhe rifresko faqen.
Ndal secilin server me Ctrl+C. `node_modules/`, `dist/` dhe `.env.local` nuk përfshihen në Git.
