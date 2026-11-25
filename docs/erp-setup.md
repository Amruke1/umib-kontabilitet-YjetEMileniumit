
# Konfigurimi i moduleve ERP

## Databaza e ekipit

Merrni një backup nga shoku dhe përdorni një kopje lokale për prova. Mos aplikoni ndryshime në databazën e tij pa krahasuar skemën dhe pa backup.

`public.customers` duhet të përputhet me `docs/database-setup.md`.

Hapni pgAdmin → databaza lokale → Query Tool.

Në një databazë të re ekzekutoni fillimisht `Database/customers.sql`; nëse tabela ekziston, mos e rikrijoni.

Pastaj ekzekutoni `Database/erp.sql` një herë.

Skripti krijon skemën e ndarë `erp`, tabelat dhe llogaritë shembull. Ai nuk fshin tabelat ekzistuese dhe nuk ekzekutohet automatikisht nga backend-i.

Nëse skema `erp` ekziston, skripti ndalet: krahasoni strukturën; mos e fshini për të anashkaluar gabimin.

Tabelat e reja përdorin emra kolonash me shkronja të mëdha në thonjëza, sipas modelit EF Core; `public.customers` ruan emrat e vet ekzistues me të vogla.

## Administratori i parë

Pas konfigurimit të connection string-ut, vendosni kredencialet fillestare në .NET User Secrets.

Zëvendësoni placeholder-in me një fjalëkalim të ri me të paktën 12 karaktere; mos përdorni shembullin si fjalëkalim real.

```powershell
dotnet user-secrets set "Bootstrap:Username" "admin" --project backend/MiniERP.Api.csproj

dotnet user-secrets set "Bootstrap:Password" "YOUR_NEW_PASSWORD_12_OR_MORE_CHARS" --project backend/MiniERP.Api.csproj

dotnet run --project backend/MiniERP.Api.csproj --launch-profile http
```

Hyni nga frontend-i me këto kredenciale.

Vetëm nëse tabela e përdoruesve është bosh, hyrja e saktë krijon administratorin dhe ruan hash-in e fjalëkalimit.

Pas hyrjes së parë hiqni sekretet fillestare:

```powershell
dotnet user-secrets remove "Bootstrap:Username" --project backend/MiniERP.Api.csproj

dotnet user-secrets remove "Bootstrap:Password" --project backend/MiniERP.Api.csproj
```

Te **Periudhat / Ekipi**, krijoni përdorues për shitësin, magazinierin dhe kontabilistin.

Pastaj hapni periudhën kontabël që përfshin datat e dokumenteve.

Mos hapni periudha që mbivendosen. Mbyllja e periudhës ndalon postimet e reja.

## Provimi i procesit

1. Shtoni furnitor, produkt dhe klient. Produkti fillon me stok zero.
2. Krijoni blerjen me sasi, kosto pa TVSH dhe trajtimin tatimor të zgjedhur.
3. Magazina konfirmon sasitë; kontabilisti poston pranimin/faturën e blerjes.
4. Shitësi krijon porosinë dhe e rezervon. Magazina konfirmon dorëzimin.
5. Kontabilisti poston shitjen: krijohen fatura, dalja dhe kostoja e mallit.
6. Regjistroni pagesë të pjesshme. Kontrolloni mbetjen dhe librin e madh.
7. Gjeneroni raportet për intervalin përkatës dhe kontrolloni debit = kredit dhe aktive = detyrime + kapital/rezultat.

Postimi është i plotë: konfirmimi i magazinës është përgatitje/aprovim sasish; dorëzimi/pranimi fizik regjistrohet kur kontabilisti përfundon postimin atomik.

Dokumentet e postuara nuk fshihen.

Kthimet janë të plota; korrigjoni pagesat para kthimit të dokumentit.

Kthimet e pjesshme janë jashtë këtij versioni.

Shpenzimi regjistrohet me ditar manual: debit shpenzim, kredit bankë/arkë.

Kapitali fillestar: debit bankë/arkë, kredit kapital.

Inventari dhe borxhet fillestare duhen futur përmes dokumenteve; ditari manual nuk lejon llogaritë kontrolluese 1100/1200/1300/2000/2100 që të shmangë mospajtimin me nënregjistrat.

## Hosting dhe sesioni

Kërkesat frontend → backend përfshijnë cookie-n e sesionit.

Përdorni `localhost` për të dy shërbimet në zhvillim; mos përzieni `localhost` dhe `127.0.0.1`.

Për publikim përdorni HTTPS dhe frontend/backend në të njëjtin site (idealisht përmes reverse proxy); cookie-ja është SameSite=Lax.

Vendosni `VITE_API_URL` para build-it të frontend-it dhe origjinat e lejuara në backend me `FrontendOrigins__0`, `FrontendOrigins__1` sipas nevojës.

Mos vendosni kredencialet e PostgreSQL në frontend.

Në prodhim ruani çelësat ASP.NET Data Protection në disk/ruajtje persistente të mbrojtur për të ruajtur sesionet pas rinisjeve dhe përdorni user databaze me privilegjet minimale.

API-ja ka limit hyrjesh; administrimi i fjalëkalimeve dhe rikuperimi i llogarive mbeten për një fazë tjetër.

## Testi i integrimit

`scripts/Test-Erp.mjs` kërkon një databazë provë të re me skemat e krijuara, pa periudha ose dokumente ekzistuese.

Mos e përdorni mbi databazën e ekipit: ai krijon histori kontabël dhe mbyll periudhën 2026.

Nisni një backend të veçantë kundrejt databazës provë dhe vendosni `ERP_TEST_URL`, `ERP_TEST_USERNAME`, `ERP_TEST_PASSWORD` në ambientin e terminalit.

Pastaj ekzekutoni:

```powershell
node scripts/Test-Erp.mjs
```

Kredencialet nuk printohen nga testi.

## Backup dhe rikthimi

Në pgAdmin përdorni Backup me format Custom.

Provoni Restore në një databazë tjetër dhe kontrolloni klientët, skemën `erp` dhe raportet.

Ruani backup-in privatisht; ai përmban të dhëna biznesi dhe hash-e fjalëkalimesh.

Kopjet lokale nuk sinkronizohen automatikisht mes anëtarëve të ekipit.
