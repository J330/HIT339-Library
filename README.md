# HIT339 Library Management System

**Source code repository:** https://github.com/J330/HIT339-Library  
**Technology:** C# · ASP.NET Core MVC · .NET 10 · Entity Framework Core · ASP.NET Core Identity · SQL Server  
**Hosted demonstration:** https://library-management-demo-eaa4hbhbg3b8gug9.australiaeast-01.azurewebsites.net/

This repository contains the expanded Library Management System, including its kiosk and public catalogue API, multi-branch inventory, reservations and waitlists, simulated notifications, media imports, and manager analytics and exports. The instructions below explain how to download and run the **`.slnx` solution locally**, navigate the features, prepare multi-branch demonstration data and manually test the integrations.

> **Local vs cloud:** Opening the solution in Visual Studio runs a **local ASP.NET Core website**. By default it uses a separate SQL Server LocalDB database, **not** the hosted Azure SQL database. The optional Windows desktop client is a separate, cloud-connected application.

## 1. Requirements

On Windows, install:

- **Visual Studio 2026** with the **ASP.NET and web development** workload.
- **.NET 10 SDK** (check with `dotnet --version`).
- **SQL Server Express LocalDB** (available through the Visual Studio Installer or SQL Server Express installer).
- A web browser. Internet access is useful for NuGet restore and optional external metadata searches.

An Azure subscription is **not required** to run and test the web project locally.

## 2. Download the repository and open the solution

1. Visit **[J330/HIT339-Library](https://github.com/J330/HIT339-Library)**.
2. Choose **Code → Download ZIP**, then extract the archive to a normal folder. **Do not open the solution inside the ZIP.**
3. Alternatively, use Git:

   ```powershell
   git clone https://github.com/J330/HIT339-Library.git
   cd HIT339-Library
   ```

4. Open **Visual Studio 2026 → Open a project or solution** and select the file at the repository root:

   ```text
   S379658-HIT339-Assignment2-Library.slnx
   ```

5. In Solution Explorer, set the **Library** project as the startup project. Wait for NuGet restore to complete.
6. Select the **https** launch profile and press **F5** (or **Ctrl+F5** to start without debugging).
7. Accept the local HTTPS developer-certificate prompt if displayed. The development site normally opens at **https://localhost:7278**.

**Alternative CLI commands** (run in the extracted repository root):

```powershell
dotnet restore .\S379658-HIT339-Assignment2-Library.slnx
dotnet build .\S379658-HIT339-Assignment2-Library.slnx
dotnet run --project .\Library\S379658-HIT339-Assignment2-Library.csproj --launch-profile https
```

### Local database and migrations

`Library/appsettings.json` contains the **development** connection string for `(localdb)\MSSQLLocalDB`, database `LibraryManagementDB`. With `ASPNETCORE_ENVIRONMENT=Development` (set by the included launch profiles), startup **automatically applies the included EF Core migrations**, creates Identity roles and seeds demonstration accounts. The branch migration also creates a **Main Library** branch (`MAIN`) and assigns pre-existing items to it.

If starting with an existing database, **back it up before migrations**. If startup fails, check that LocalDB is installed and the connection string points to the correct instance. You can also apply the already-included migrations manually from the `Library` project directory with `dotnet ef database update` (requires the `dotnet-ef` tool). **Do not create new migrations simply to set up the existing project.**

**Production/Azure:** Do not use the Development environment or the sample passwords on Azure. Production database migrations and demo account bootstrap are configured separately; see `PRODUCTION_DEMO_ACCOUNTS_SETUP.md` if working on the hosted demonstration. Never commit SQL credentials or real passwords to the repository.

## 3. Local demonstration accounts

The following accounts are created **only in Development**, if they do not already exist. Restarting the application does **not** reset passwords on existing accounts.

| Role | Username | Local development password | Typical permissions |
| --- | --- | --- | --- |
| Administrator | `admin` | `Admin123!` | Manage inventory, branches, transfers and staff accounts |
| Manager | `manager` | `Manager123!` | Branch management, dashboard and report exports |
| Receptionist | `reception` | `Reception123!` | Borrow/return, manage transfers and import records |
| Borrower 1 | `borrower1` | `Borrower123!` | Kiosk, online borrowing and reservations |
| Borrower 2 | `borrower2` | `Borrower123!` | Kiosk, online borrowing and reservations |

Development borrower membership codes: `LIBDEMO000101` and `LIBDEMO000102`. These are demonstration credentials only; do not reuse them in production.

## 4. Navigate the new modules

Once the local website is running, use the role-appropriate navigation menus or enter these paths after `https://localhost:7278`:

| Module / deliverable | Route | Who uses it | What to try |
| --- | --- | --- | --- |
| Public catalogue | `/Catalogue` | Public | Search and inspect books, music and toys; see branch and availability |
| **Self-service kiosk** | `/Kiosk` | Borrower | Sign in with a member code, see loans and scan an item library code |
| **Public read-only API** | `/Developers` | Public / developers | Browse JSON examples and available endpoints |
| API catalogue | `/api/catalogue/items` | Public | Retrieve public item information as JSON |
| API categories | `/api/catalogue/categories` | Public | Retrieve category information |
| API branches | `/api/catalogue/branches` | Public | Retrieve library branch information |
| API status | `/api/catalogue/status` | Public | Retrieve API operating/status information |
| **Branch management** | `/Branches` | Admin, Manager | Create/edit branches and add reception desks |
| **Inter-branch transfers** | `/Transfers` | Admin, Reception | Request, complete or cancel a physical-copy transfer |
| Inventory management | `/AdminItems` | Admin | Create/edit inventory and assign each copy to a branch |
| Borrowing and returns | `/Borrowing` | Admin, Manager, Reception | Check out and return physical items |
| **Reservations and waitlist** | `/Reservations` | Borrower, Admin, Reception | Place/view/cancel holds; check queue progression |
| **Notifications and calendar** | `/Notifications` | Signed-in roles | Review simulated notifications, reminders and calendar entries |
| **CSV / mock imports** | `/Imports` | Admin, Manager, Reception | Upload validated CSV or import mock media |
| **External metadata lookup** | `/Imports/Search` | Admin, Manager, Reception | Search/preview supported external media metadata |
| **Manager dashboard** | `/Managers` | Manager | Review library operating statistics |
| **CSV / PDF reports** | `/Reports` | Manager | Filter and export borrowing, fines, inventory and payment-audit reports |
| Fine payments | `/Payments` | Manager, Reception | Record and review fine receipts |
| Staff account management | `/StaffAccounts` | Admin | Create staff accounts and reset staff passwords |

These modules share the same Entity Framework Core database when running inside one application instance. The **public API is read-only** and does not expose private borrower data, loans or passwords. The kiosk and account pages require borrower sign-in for personal information or checkout.

## 5. Seed multi-branch demonstration data

**Important:** The included migration seeds **one** branch, `Main Library` (`MAIN`). It does **not** automatically create multiple example branches. Use the steps below to create a reproducible multi-branch demonstration **through the application's UI**, without editing tables directly.

### A. Create two additional branches

1. Sign in as `admin` or `manager` and open **Management → Branch Management** (`/Branches`).
2. Edit **Main Library** and replace its placeholder address if desired.
3. Select **Create a branch** and add the following example branches, keeping **IsActive** checked:

| Name | Code | Example address | Opening hours |
| --- | --- | --- | --- |
| North Library | `NORTH` | `10 North Street` | `Mon–Fri 09:00–17:00` |
| East Library | `EAST` | `20 East Street` | `Mon–Sat 09:00–16:00` |

4. In the branch list, add reception desks such as **North Reception** and **East Reception** to their respective branches.
5. Reopen `/Branches` to confirm that **MAIN**, **NORTH** and **EAST** appear. Branch codes must be unique.

### B. Seed inventory in different branches using CSV

The repository includes an import example at **`Library/Resources/sample-import.csv`**; it can also be downloaded from **`/Imports/SampleCsv`**. The importer treats **each row as a distinct physical copy** identified by a unique `LibraryCode`.

To populate both new branches, copy the following content into a **new** plain-text file named `multi-branch-demo.csv` and save it as **UTF-8 CSV**:

```csv
Type,LibraryCode,Name,Description,BranchCode,Author,Genre,Artist,Year,ToyType,MinimumAge
Book,NORTH-DEMO-101,North Branch Novel,Example novel for transfer testing,NORTH,A. Example,Fiction,,,,
Book,EAST-DEMO-201,East Branch Guide,Example guide for reservation testing,EAST,B. Example,Travel,,,,
Music,NORTH-DEMO-102,North Demo Album,Example music record,NORTH,,Instrumental,Demo Orchestra,2024,,
Toy,EAST-DEMO-202,East Building Set,Example toy inventory,EAST,,,,,Construction,5
```

1. Sign in as `admin`, `manager` or `reception` and open **`/Imports`**.
2. Choose the **CSV upload** option, select `multi-branch-demo.csv`, then submit it.
3. Check the result message and view `/Catalogue` (or `/AdminItems` as Admin).
4. Confirm there are **two items at NORTH** and **two at EAST**.
5. To test duplicate validation, upload the **same file again**. It should report duplicate library codes rather than silently create duplicate copies.

**Tip:** If you have already used these example codes, change the `LibraryCode` values before the first import. The importer validates the complete upload and rejects the batch if any row is invalid. Importing a CSV does not create branches automatically: each `BranchCode` must already exist and be active.

### C. Set up borrower test data

The Development seeder creates `borrower1` and `borrower2` with membership codes listed above. To test **two separate people waiting for the same item**, create a **third** borrower account through the existing borrower registration/reception workflow, because a two-person hold queue requires **two borrowers other than the person who currently has the item**.

## 6. Test the integrations (manual acceptance checklist)

Follow these scenarios after completing Sections 2–5. Use a **local development database** so tests do not alter the live library.

| Feature | Test procedure | Expected result |
| --- | --- | --- |
| **Kiosk borrowing** | Sign in at `/Kiosk` as `borrower1`, using `LIBDEMO000101` and the local demo password. Scan or type an **available** item code. | A borrowing record is created; the item becomes unavailable; the account displays its loan and due date. |
| **Public API** | Open `/api/catalogue/items`, `/api/catalogue/categories`, `/api/catalogue/branches` and `/api/catalogue/status` in a browser without signing in. | JSON is returned; the added branches and catalogue data are visible, but private member records are not. |
| **Read-only API** | Attempt a write operation such as POST to a catalogue API endpoint. | No public write endpoint exists; the request must not modify library records. |
| **Branch ownership** | Open `/Branches` and view an item created via the multi-branch CSV. | The item's assigned physical holding branch matches `NORTH` or `EAST`. |
| **Inter-branch transfer** | Sign in as `reception` or `admin`; request transfer of an **available, unreserved** copy at `/Transfers`, e.g. NORTH → EAST; then complete it. | Item is restricted while transfer is pending and shows EAST as its branch after receipt. |
| **Reservation queue** | Borrow a copy for borrower 1 via `/Borrowing`. Sign in as borrower 2 and place a hold from `/Catalogue/Details/{id}`; optionally add a third borrower to test ordering. Return the copy through reception. | First eligible reservation becomes ready; later holds stay queued; checkout respects reservation priority. |
| **Simulated notifications** | With an active loan, set a due date within two days via staff borrowing tools, then open `/Notifications` as staff and select **Run checks**. | Due-soon notification events are recorded; repeating the check should not create duplicate events for the same reference. **No real SMS/email is sent.** |
| **Hold-ready alert** | With a reservation pending, return or repair the relevant item so it becomes available. | The next eligible borrower receives a **simulated** email/SMS record in the notification log (depending on configured contact details). |
| **CSV import validation** | Import `multi-branch-demo.csv`, then try it again or change one branch code to an unknown value. | First valid batch creates copies; duplicates/invalid branch prevent unsafe or partial import. |
| **Mock/external import** | Visit `/Imports` for mock data or `/Imports/Search` for provider lookup; preview and confirm a record with a branch selected. | A validated catalogue copy is created in the selected branch. External lookups require connectivity/provider availability. |
| **Manager analytics** | Sign in as `manager`; open `/Managers` and `/Reports`, then select Borrowing, Fines, Inventory or Payment Audit. | Results reflect the current local database and selected filters. |
| **Export integration** | At `/Reports`, export the same report to **CSV** and **PDF**. | Both files download and contain the report data; public/borrower accounts cannot access manager-only exports. |
| **Role security** | Attempt `/Branches`, `/Transfers`, `/StaffAccounts` and `/Reports` while logged in as a borrower. | Restricted management pages deny access. |

### Recommended demonstration sequence

**Create branches → Import inventory → Borrow an item → Place a reservation → Return the item → Verify a notification → Transfer another item → Generate a manager report → Export CSV/PDF.**

This sequence demonstrates that the new features are **integrated**, rather than just independent pages. Check `/Catalogue` and the public catalogue API between steps to verify that changes are reflected across channels.

## 7. Where the feature code lives

| Area | Repository location |
| --- | --- |
| Main MVC web project | `Library/S379658-HIT339-Assignment2-Library.csproj` |
| Application startup, services and seeding | `Library/Program.cs`, `Library/Database/DatabaseSeeder.cs` |
| Database context and migrations | `Library/Database/AppDbContext.cs`, `Library/Migrations/` |
| Kiosk and public API | `Library/Controllers/KioskController.cs`, `CatalogueApiController.cs` |
| Branches, transfers and reservations | `Library/Controllers/BranchesController.cs`, `TransfersController.cs`, `ReservationsController.cs` |
| Notification and reminder logic | `Library/Controllers/NotificationsController.cs`, `Library/Services/Notification*` |
| Importers and example data | `Library/Controllers/ImportsController.cs`, `Library/Resources/` |
| Analytics / exports | `Library/Controllers/ManagersController.cs`, `ReportsController.cs`, `Library/Services/` |
| Staff Identity management | `Library/Controllers/StaffAccountsController.cs`, `AccountController.cs` |
| HTML/Razor screens | `Library/Views/` |
| Windows desktop client (separate project) | `LibraryDesktopClient/` |

Additional background notes exist in `Library/SECTION_1_KIOSK_AND_API.md`, `Library/SECTION_2_NOTIFICATIONS_BRANCHES_RESERVATIONS.md` and `Library/SECTION_3_IMPORT_REPORTS.md`.

## 8. Troubleshooting

- **`.slnx` cannot open:** Extract the whole ZIP, use a recent Visual Studio version with .NET 10 support, and confirm the solution and `Library/` project folders are both present.
- **NuGet or compilation errors:** Run `dotnet restore` and `dotnet build`; check the installed SDK with `dotnet --list-sdks`.
- **SQL/LocalDB connection error:** Install/start SQL Server Express LocalDB; check `ConnectionStrings:DefaultConnection` in `Library/appsettings.json`.
- **Migration fails:** Back up the database, inspect the detailed error and ensure the supplied migrations have been applied in order; avoid deleting a database that contains important data.
- **Unable to log in:** Confirm you're using the local Development instance. Existing passwords are not overwritten when seeding runs again.
- **`/Reports` or `/Branches` says Access Denied:** Check your account role; not all staff roles have the same permissions.
- **Nothing to transfer or reserve:** Create inventory at multiple branches, then make an item unavailable by borrowing it before testing holds.
- **Notifications not appearing:** Verify the borrower's contact details, run the manual check from `/Notifications` and inspect the log. The hosted background worker may pause when the application is not running.
- **External metadata search fails:** Test the built-in mock import instead; live provider connectivity and rate limits may vary.

## 9. What is simulated vs. live

- **Email and SMS:** delivery is **simulated and recorded** in the application. No actual messages are sent.
- **Branch opening hours:** stored as descriptive text; they do not provide a live open/closed clock calculation.
- **Reservation queue:** operates on individual physical copies, not a title-wide shared waiting list.
- **Local development:** uses LocalDB, so its test records do **not** appear automatically in the Azure deployment.
- **Windows desktop client:** presents the Azure-hosted library through WebView2. Staff using that packaged `.exe` normally connect to the cloud; the `.slnx` solution is for developing the web application.

---

**Assignment deliverable:** The Git repository linked above contains the expanded .NET 10 MVC solution. This README explains how to **download and launch the `.slnx`, navigate the new modules, seed multi-branch sample data and verify the integrations** through reproducible manual tests.
