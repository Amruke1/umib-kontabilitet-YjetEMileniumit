# Implementation progress — 15 November 2026

There is no weekly assignment list. The order below is an implementation proposal
based on `project-description.md`, not a claim about agreed deadlines.

## Starting point and completed work

The repository had a React customer list/create form and an ASP.NET Core API
backed by the `customers` PostgreSQL table. Other ERP modules were absent.

Customer management now includes:

- GET `/api/customers/{id}` with 404 for missing records.
- PUT `/api/customers/{id}` with the same validation as creation.
- DELETE `/api/customers/{id}` with 204 on success, 404 if missing, and 409
  when PostgreSQL reports a foreign-key dependency.
- POST now returns a Location header pointing to the created resource.
- Server-side trimming before validation; blank optional contacts become null.
- A shared add/edit form with cancellation, search by ID/name/email/phone,
  deletion confirmation, and per-customer accessible action labels.
- UI changes happen only after successful API responses. A failed write keeps
  the form and existing list available for correction or retry.
- CORS permits the new PUT and DELETE methods for the existing local frontend origins.

The existing customer schema is unchanged. These endpoints remain unauthenticated,
as the original application was; role-based access is still outstanding.

## Why this order

Presentation update: the frontend now includes a responsive application shell,
dashboard with customer counts, contact preview, and customer management workspace.
An explicit demonstration mode (`?demo=1`) uses fictional in-memory contacts and
supports adding, editing, searching and deleting without a backend. Demo data
never writes to PostgreSQL; refreshing or switching modes resets it. Planned
modules and roles are labeled as planned. Real mode shows connection failures
and offers retry. The final frontend build and lint both pass without warnings.
Browser visual verification remains incomplete because computer use stopped when
it could not confidently identify the native browser URL.

Customers are prerequisites for orders. Completing one existing module establishes
request validation, HTTP status handling, and UI editing conventions before those
patterns are extended. No replacement database schema was invented for modules
whose database work is being handled by the team.

## Remaining implementation sequence

1. Authentication and authorization: administrator, salesperson, warehouse worker,
   accountant. Enforce permissions on the API; hiding UI buttons is insufficient.
   Warehouse API responses must omit prices, as required by the project description.
2. Products and suppliers: agree table/column contracts with the database team,
   then implement product catalog, supplier records and stock receipts.
3. Sales orders and reservations: validate customers and line items, calculate
   totals on the server, snapshot sale prices, reserve stock atomically, and reject
   insufficient availability. Decide cancellation and reservation-release rules.
4. Warehouse delivery: deliver an order once, reduce physical stock and release
   its reservation together, and record the stock movement in the same transaction.
5. Accounting: partial payments, outstanding customer/supplier balances, expenses,
   and balanced journal entries. Agree currency, rounding and posting rules first.
6. Connect role-specific screens and verify the complete receipt → order → delivery
   → partial/full payment flow, including simultaneous reservations and retries.

## Verification and limits

- Backend build passed with zero warnings and errors.
- Frontend TypeScript/production build and Oxlint passed.
- `git diff --check` passed before this report was added.
- Runtime API/database checks could not run: the local API has no configured
  `ConnectionStrings:DefaultConnection` and refuses to start. No credentials were
  invented or written to the repository.
- `scripts/Test-Customers.ps1` provides a PowerShell 7 integration smoke test for
  a configured development database. It checks validation, normalization, CRUD,
  rejected-update persistence and missing-record responses, and cleans up its own
  test customer. It has not yet been executed against PostgreSQL.
- Visual/browser interaction and database concurrency behavior are not verified.

After following `database-setup.md` and starting the API, run:

```powershell
pwsh -File scripts/Test-Customers.ps1
```

Manual UI check: add a customer, search by contact details, edit and cancel, save
an edit, cancel deletion, confirm deletion, and stop the API to check error feedback.
