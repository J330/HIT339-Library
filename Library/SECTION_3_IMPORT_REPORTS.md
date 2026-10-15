# Section 3 – External Media Importer and Manager Reporting

This continues Sections 1 and 2 of the existing ASP.NET Core MVC (.NET 10) library project. Previous kiosk, API, notification, multi-branch and reservation modules are retained.

## 1. Prerequisites and first run

1. Open `S379658-HIT339-Assignment2-Library.slnx` in Visual Studio with the .NET 10 SDK and a reachable SQL Server (see `appsettings.json`).
2. Back up your existing database **before** running migrations.
3. Restore packages: `dotnet restore` (adds CsvHelper 33.1.0 and QuestPDF 2026.9.1).
4. Compile: `dotnet build`.
5. Apply schema: `dotnet ef database update` (new migration `20261014120000_ImportReportingFinePayments`). The app also migrates on startup via `DatabaseSeeder`.
6. Start: `dotnet run`. Use the seeded Admin, Manager, and Reception roles from the existing application. Replace development passwords before real deployment.

**Testing status:** Source was reviewed and packaging validated. No .NET SDK or SQL Server was available in the authoring environment, so compilation and live schema/route tests must be done locally.

## 2. External Book/Media Data Importer

- **Admin-only**: `/Imports` and the "Import Items" menu link. Added `ImportsController`, `MediaImportService`, `Views/Imports/Index.cshtml`.
- CSV template at `/Imports/SampleCsv` or `Resources/sample-import.csv`. Column headers are required exactly (case-insensitive): `Type,LibraryCode,Name,Description,BranchCode,Author,Genre,Artist,Year,ToyType,MinimumAge`.
- Each CSV row represents **one physical copy**, not one unique title/ISBN. Only Book, Music and Toy types use the existing database inheritance mapping; each copy is assigned to an **active branch** by its `BranchCode` (e.g. `MAIN`). New items start as Available.
- Validation: file extension, size (2MB), count (500 rows), required fields, length, numeric/year/age bounds, active branch, and duplicate `LibraryCode` both within file and database. A unique index also rejects duplicate codes on concurrent requests.
- If any row fails validation, **nothing is imported**. The screen lists row-specific failures. CSV bytes are parsed from the upload stream and are not persisted to the server's file system.
- The second form imports **mock external metadata** from `Resources/mock-media.json`, applying a selected active branch. Demo codes begin `DEMO-`; re-importing identical codes is rejected, not duplicated. No external network service or API key is required.
- To add a real book provider later, replace the mock JSON source with an `HttpClient` adapter and map the provider's records to `MediaImportRow`. Do **not** bypass validation or import a remote-provided primary key.

## 3. Manager Analytics and CSV/PDF Export

- **Manager-only**: `/Managers` dashboard now adds per-branch inventory, current holds, transfer counts, fine amounts charged, payments received, and outstanding balance.
- **Manager-only**: `/Reports` shows selectable reports: `Borrowing`, `Fines`, `Inventory`, and `PaymentAudit`.
- Filters: branch, start and end dates (when applicable). `Inventory` is a **current snapshot** regardless of date filter. `Borrowing` filters by borrow date, `Fines` charges by **return date** and payments by **payment UTC date**, and `PaymentAudit` by payment UTC date.
- `/Reports/Export?type=Fines&format=csv` and `&format=pdf` return downloadable reports. Export preserves the filters currently shown. CSV uses CsvHelper including formula-injection escaping; PDF uses QuestPDF (evaluation license mode).
- All exports require a Manager login. Do not expose these records through the public catalogue API.

## 4. Fine receipts and audit separation

- **Manager and Reception-only**: `/Payments` lists recorded fine charges per borrowing, existing receipts and remaining balance. Post a positive payment and a reference to record a receipt.
- New `FinePayment` model and `FinePayments` table: `BorrowingId`, `Amount`, `PaidAtUtc`, `Reference`.
- Charges remain in existing `Borrowing.FineAmount`; payments are **separate** recorded transactions. A serializable database transaction prevents recording payments in excess of the charged balance when two staff users act concurrently.
- Previous fine records are **not** treated as paid. Old records begin with zero receipts unless someone records payments. There is no integrated bank/payment gateway, refund, or automated collection.
- The `PaymentAudit` report lists the timestamp, receipt reference, borrowing ID, title, branch and amount for every recorded receipt in the selection.

## 5. Acceptance checks

| Test | Expected outcome |
|---|---|
| Import `sample-import.csv` to a clean system containing branch `MAIN` | Three available records created; count increases on the correct branch |
| Import the same CSV again | Duplicate code errors; zero new rows |
| Change one CSV row to a missing branch or invalid year | All rows rejected; no partial import |
| Sign in as Borrower and visit `/Imports`, `/Reports`, `/Payments` | Access denied |
| Import mock records to another active branch | Four new copies appear at that branch; rerun rejects duplicates |
| Return an item late and open Manager | Charged fine increases but payments remain unchanged |
| Record a partial payment and a second payment | Payment total increases, outstanding falls; no overpayment accepted |
| Download borrowing, fine, inventory and payment-audit CSV/PDF | Exports match filtered on-screen figures; correct MIME type |
| Open `/api/catalogue/items` | Public catalogue still works; neither receipts nor private borrowing records are exposed |

## 6. References / techniques used

- [Microsoft – Upload files in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads): file upload handling, length limits, validation and untrusted file names.
- [CsvHelper – Reading CSV files](https://joshclose.github.io/CsvHelper/examples/reading/): CSV reader, headers, and typed fields.
- [CsvHelper – Writing CSV files and injection protection](https://joshclose.github.io/CsvHelper/examples/writing/): CSV generation and `InjectionOptions.Escape`.
- [EF Core – Querying / complex queries](https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators): aggregate/group-by query patterns.
- [EF Core – Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions): serializable transaction used to avoid excess simultaneous receipts.
- [EF Core – Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/): add FinePayments to current database without replacing previous data.
- [QuestPDF – Quick Start](https://www.questpdf.com/quick-start.html): report tables and PDF generation.
- [QuestPDF – License selection](https://www.questpdf.com/license/guide.html): evaluation setting used for this coursework. Verify eligibility or select an appropriate paid license before production use.

> No ISBN field exists in the present Item/Book schema. Duplicate detection therefore uses **physical-copy LibraryCode**. Introducing title-level ISBN and edition metadata would require a subsequent model/migration change; do not treat identical book titles as duplicate physical copies.
