# Mini ERP — Frontend

React + TypeScript me Vite. Kërkon Node.js 22.12+ ose 24+ dhe npm.

Nga rrënja e repository-t:

```powershell
cd frontend
npm ci
npm run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Hap http://127.0.0.1:5173 për faqen “Klientët”. Ndale serverin me Ctrl+C.

```powershell
npm run build
npm run preview -- --host 127.0.0.1 --port 4173 --strictPort
```

Build-i krijohet në `dist/`. Preview hapet në http://127.0.0.1:4173.

- `src/pages/Customers.tsx`: faqja dhe tabela responsive (me rrëshqitje horizontale në telefon).
- `src/types/Customer.ts`: tipi Customer; email dhe phone lejojnë null.
- `src/data/customers.ts`: tre klientë fiktivë lokalë.

Nuk ka lidhje me API apo databazë. `node_modules/` dhe `dist/` përjashtohen nga Git.
