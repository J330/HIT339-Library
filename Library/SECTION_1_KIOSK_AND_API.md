# Section 1 – Public Self-Service Kiosk and Developer API

This edition extends the original ASP.NET Core MVC / EF Core SQL Server library project. Staff item/borrowing screens and the existing public catalogue remain available.

## Start it

1. Open `S379658-HIT339-Assignment2-Library.slnx` in Visual Studio with the .NET **10 SDK** and SQL Server LocalDB installed.
2. Check the `DefaultConnection` in `Library/appsettings.json`. Back up existing data before schema changes.
3. Launch the `Library` web project using HTTPS. `DatabaseSeeder.SeedAsync` calls `Database.MigrateAsync()` and creates the new Borrower role. A new migration, `20261014000000_KioskAndPublicApi`, adds membership codes, borrower identity links, and required first-login password changes. You can also apply manually using `dotnet ef database update` from the `Library/Library` project folder.
4. Sign in as the seeded **reception** user (existing development credentials in `DatabaseSeeder.cs` — change all demo staff passwords outside local development).
5. Go to **Borrowers → Create New Borrower**. Creating a borrower now assigns a unique code (e.g. `LIB...`) and a one-time temporary Identity password. The borrower detail page displays that temporary password ONCE. Give it privately to the borrower; do not print it in public or send it in plain text.
6. For a borrower whose account existed before the migration, open **Borrowers → Details → Activate kiosk access**. This provisions the member login without replacing their borrowing history. Reception may also issue a new temporary password for an existing kiosk account.
7. Open **Self-Service Kiosk** from the navigation or home page (`/Kiosk`). Sign in with membership code + temporary password. The borrower must create their own password. Then open **My account** or **Scan item to borrow**. The kiosk writes to the original `Items`, `Borrowers` and `Borrowings` tables. Item barcodes are the existing `Item.LibraryCode`.
8. Open **Public API** from navigation (`/Developers`). This contains clickable JSON endpoints and sample query parameters.

## URLs

| Route | Access | Function |
| --- | --- | --- |
| `/Kiosk` | Public | Touch-friendly self-service kiosk landing |
| `/Kiosk/Login` | Public | Member-code + password sign in |
| `/Kiosk/Account` | Borrower | Loan list, due dates, recorded fines |
| `/Kiosk/Checkout` | Borrower | Check out an available item by scanning its item library code |
| `/Kiosk/ChangePassword` | Borrower | Mandatory temporary-password replacement and optional later changes |
| `/Developers` | Public | Developer documentation, examples and clickable links |
| `GET /api/catalogue/items` | Public | Paginated catalogue (`search`, `type`, `category`, `availability`, `page`, `pageSize`) |
| `GET /api/catalogue/items/{id}` | Public | Individual item |
| `GET /api/catalogue/categories` | Public | Book, music and toy categories |
| `GET /api/catalogue/status` | Public | API availability; reports branch hours not configured |

The API **does not** expose borrower names, membership codes, Identity credentials, lending histories, fines or raw EF entities. It has no write endpoints. API and kiosk login requests are rate-limited; failed member passwords trigger Identity lockout.

## Barcode/camera details

- A USB barcode scanner operates as a keyboard; select the membership/item input and scan.
- **Scan with camera** uses the browser's native `BarcodeDetector` API where available. It needs HTTPS (or localhost), camera permission and a supported browser. The input always remains usable manually when camera detection is not supported. No third-party CDN dependency is needed.
- Kiosk borrower pages carry `Cache-Control: no-store`. A borrower session is automatically signed out after two minutes of inactivity on kiosk pages, and it can be ended with **End session**. A deployment using shared physical kiosks should additionally use browser kiosk-mode restrictions, auto-clear browser data, and a short session timeout.

## Implementation notes and boundaries

- Existing staff borrowing/return workflow is preserved. Kiosk checkout follows the existing `Available` status and the filtered unique active-loan database index. It cannot check out `Borrowed`, `Damaged` or `Destroyed` items.
- Checkout uses a fixed 14-day due date; an additional service can later make loan durations configurable by item type/branch.
- `Recorded fines` represents current `Borrowing.FineAmount` sums; the original project does not yet have a payment ledger or real-time fine assessment for unreturned books.
- Physical branch opening hours have not been modelled yet. `/api/catalogue/status` clearly reports that these hours are **not configured** instead of claiming that a branch is open.
- Changing contact details on the reception Borrower record does not automatically send verification email or update Identity's email address. Email verification, self-service account recovery, barcode printing, scanner hardware integration and an actual branch-hours service are not included in this section.
- The `.NET SDK` and SQL Server LocalDB are not installed in this build environment; **compile and migration execution could not be performed here**. Please run Visual Studio Build and apply the migration against a backed-up development database before using these changes.

## Implementation references

| Component | Reference |
| --- | --- |
| QR/Barcode scanning library alternative | https://github.com/zxing-js/browser |
| Identity authentication | https://learn.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity |
| Touch-friendly buttons | https://getbootstrap.com/docs/5.3/components/buttons/ |
| EF Core indexes and membership uniqueness | https://learn.microsoft.com/en-us/ef/core/modeling/indexes |
| ASP.NET Core Web API | https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api |
| API controllers | https://learn.microsoft.com/en-us/aspnet/core/web-api/ |
| Role-based authorisation | https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles |
| Rate limiting | https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit |

## Quick manual acceptance checklist

- [ ] Reception can create borrower and one-time credentials are visible only on the reception detail page.
- [ ] Previously created borrower can be activated without losing their loans.
- [ ] An incorrect kiosk password is rejected; staff or another borrower's loans remain inaccessible.
- [ ] First kiosk sign-in requires a new password.
- [ ] Kiosk checkout with a valid available item creates one loan; repeated checkout is refused.
- [ ] Loan appears both in member kiosk and the reception borrowing list.
- [ ] Ending the session prevents viewing account and checkout screens; browser Back does not reveal cached account details.
- [ ] API returns catalogue JSON, public categories, and a status response; there are no borrower details in JSON.
- [ ] API endpoints reject unsupported write operations and throttle excessive read requests.
