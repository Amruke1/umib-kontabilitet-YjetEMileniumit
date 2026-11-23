
# Auditimi i Mini ERP — 17 nëntor 2026

Konteksti: projekt universitar në Kosovë, një biznes, një magazinë, EUR.

Skema e plotë në kompjuterin e shokut nuk është e disponueshme këtu.

## Ndryshimet nga versioni vetëm me klientë

| Fusha | Shtuar në këtë version |
| --- | --- |
| Qasja | Sesion me cookie, hash fjalëkalimi, katër role dhe kufizime në API |
| Furnitorë / produkte | Regjistra, SKU unik dhe stok me vlerë monetare |
| Blerjet | Draft, konfirmim sasish, postim stoku/detyrimi/ditari |
| Shitjet | Porosi, rezervim, anulim, konfirmim, postim faturimi/stoku/kostoje |
| Kontabiliteti | Plan kontabël, ditar i balancuar dhe periudha që mbyllen |
| Pagesat | Arkë/bankë, alokim në faturë dhe pagesa të pjesshme |
| Korrigjimet | Kundërregjistrim pagesash/ditarësh dhe kthim i plotë dokumenti |
| Raportet | Bilanc provues, lëvizje sipas llogarisë, rezultat dhe bilanc gjendjeje |
| Prezantimi | Ndërfaqe shqip, eksport CSV, printim pamjeje dhe demo klientësh |

## Plani kontabël

Llogaritë shembull janë:

- 1000 Arka
- 1010 Banka
- 1100 Klientët
- 1200 Inventari
- 1300 TVSH e zbritshme
- 2000 Furnitorët
- 2100 TVSH e pagueshme
- 3000 Kapitali
- 3100 Fitimi i bartur
- 4000 Shitjet
- 5000 Kostoja e shitjes
- 5100 Qiraja
- 5200 Shërbimet
- 5300 Shpenzime të tjera

Kodet janë zgjedhje pedagogjike, jo pretendim për numërim zyrtar të detyrueshëm në Kosovë.

Proceset automatike përdorin këto kode të rezervuara; llogari shtesë krijohen në ndërfaqe.

Llogaritë mund të çaktivizohen, por nuk fshihen ose riklasifikohen nga API-ja.

ATK liston normën standarde 18% dhe të reduktuar 8%:

https://www.atk-ks.org/en/portfolio/informata-te-pergjithshme-per-tatimet-ne-kosove/

Rreshtat lejojnë 18%, 8% ose 0% për demonstrim.

Përjashtimi, eksporti, mosregjistrimi në TVSH dhe furnizimi me normë zero nuk janë të njëjta juridikisht; ky version nuk i modelon si kategori të veçanta dhe nuk krijon deklaratë tatimore.

## Rregullat

- Çmimet dhe emrat ruhen si snapshot në dokument. Backend-i llogarit totalet me decimal dhe rrumbullakim të deklaruar në dy decimale për rresht.

- Stoku i disponueshëm është fizik minus rezervim. Kostoja mesatare e ponderuar përdor vlerën e inventarit; shitja e njësisë së fundit konsumon vlerën e mbetur për të shmangur mbetje nga rrumbullakimi.

- Postimi, stoku dhe ditari kryhen në transaksion serializable. Konfliktet kërkojnë rifreskim/riprovim dhe nuk prodhojnë ruajtje të pjesshme.

- Dokumenti postohet një herë. Pagesat dhe ditarët manualë përdorin UUID kërkese që të shmangin dyfishimin gjatë riprovimit.

- Ditari kërkon debit = kredit dhe të paktën dy rreshta; databaza e kontrollon përsëri në commit. Ditarët e postuar janë të pandryshueshëm.

- Magazina merr vetëm sasi, emra dhe identifikues; API-ja nuk i dërgon çmime.

- Historiku ruhet; klientët me dokumente nuk mund të fshihen.

## Çfarë nuk duhet të rëndojë versionin universitar

Nuk shtohen paga/personel, prodhim, shumë magazina/kompani, shumë monedha, CRM marketingu, parashikime me AI ose integrime bankare.

Asnjë nga këto nuk ekzistonte në kod për t'u hequr.

Demonstrimi i klientëve ruhet si ndihmë për prezantim.

Janë hequr nga navigimi real etiketat që i quanin të gjitha modulet e tjera thjesht të planifikuara; tani navigimi hap funksionet përkatëse.

## Kufizimet dhe faza tjetër

- Kthime/dorëzime të pjesshme, parapagime dhe kreditnota të pjesshme mungojnë.

- Printimi është i pamjes aktuale, jo model fature ligjërisht i plotë. Identifikuesit fiskalë, adresat dhe të dhënat e biznesit duhen zgjeruar para përdorimit real; fiskalizimi dhe deklarimi në ATK nuk implementohen këtu.

- Nuk ka hierarki llogarish ose konfigurim të hartës së kodeve të postimit.

- Ndryshimi i produktit/furnitorit, menaxhimi i roleve ekzistuese, resetimi i fjalëkalimit dhe çaktivizimi i përdoruesve mbeten për zgjerim.

- Ditari manual postohet drejtpërdrejt, pa workflow draft/aprovim të dytë.

- Numri i brendshëm i dokumentit është ID unik me boshllëqe të mundshme; nuk pretendohet seri fiskale e rregulluar. Dokumentet nuk ndryshohen pas krijimit; drafti anulohet dhe krijohet sërish kur kërkon korrigjim.

- Saldot ekzistuese të biznesit nuk importohen automatikisht. Aplikimi mbi databazën e shokut kërkon krahasim skeme dhe pajtim të saldove.

## Kontrolli i pranimit

Pa TVSH dhe me saldo zero:

- Blerje: 100 EUR
- Shitje: 150 EUR
- Arkëtim: 40 EUR
- Pagesë furnitori: 30 EUR

Priten rezultatet:

| Përshkrimi | Vlera (EUR) |
| --- | ---: |
| Klientë | 110 |
| Furnitorë | 70 |
| Bankë | 10 |
| Inventar | 0 |
| Të ardhura | 150 |
| Kosto | 100 |
| Fitim | 50 |

**Aktive 120 = detyrime 70 + rezultat 50.**

Provoni edhe:

- Rezervime konkurruese
- Mungesë stoku
- Ditar të pabalancuar
- Pagesë mbi mbetje
- Postim të përsëritur
- Kthim
- Rol të gabuar
- Periudhë të mbyllur
- Backup/restore në një databazë tjetër
