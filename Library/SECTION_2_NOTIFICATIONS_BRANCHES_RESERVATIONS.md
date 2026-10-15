# Section 2 — Notifications, Multi-Branch Inventory and Reservations

This version **builds on** `SECTION_1_KIOSK_AND_API.md`: the self-service kiosk, ASP.NET Core Identity borrower accounts and public catalogue API remain intact.

## Run in Visual Studio / .NET 10

1. **Back up your existing SQL Server database.** Open `S379658-HIT339-Assignment2-Library.slnx` in Visual Studio, or enter the `Library/Library` project directory in a terminal.
2. Restore NuGet dependencies and build with `dotnet restore` and `dotnet build`.
3. Check the `DefaultConnection` connection string in `appsettings.json`, preferably overriding it with user secrets or environment variables. Do not commit production credentials.
4. Run `dotnet ef database update` from the project directory (`dotnet tool install --global dotnet-ef` if needed, with matching EF major version). Alternatively, the project migrates automatically when it starts via `DatabaseSeeder.SeedAsync`.
5. Run `dotnet run`. The migration `20261014060000_BranchesReservationsNotifications` creates the new tables and assigns existing copies/borrowings to **Main Library** (branch ID 1). Admin must edit its address and hours in **Branches**.
6. For deployment, replace the demonstration seeded staff passwords; protect the application with HTTPS, store secrets securely and consider disabling automatic schema upgrades in production.

**No real email or SMS is sent.** Every notification is stored as two *simulated* delivery records (when email and phone are present). All dates used for hold expiry are UTC; loan due-date reminders use the server's local date (align server timezone with library policy for production).

## Feature-by-feature walkthrough

| Feature | Pages and actions | Database entities | Key rules |
|---|---|---|---|
| Automated notifications | `/Notifications`, `POST /Notifications/RunChecks`; borrowers see only their own log | `LibraryNotifications` | Logs checkout confirmation, due-soon (within 2 days), overdue, daily estimated fine, return fine, hold ready and hold expiry. `LibraryNotificationWorker` scans hourly while the app is running, with deduplication via `ReferenceKey`. |
| Multi-branch inventory | `/Branches` (admin), `/Transfers` (admin/reception), branch selection at `/AdminItems/Create`, `/Borrowing/Borrow`, `/Borrowing/Return` | `LibraryBranches`, `ReceptionDesks`, `ItemTransfers`; `Item.BranchId`, `Borrowing.CheckoutBranchId` / `ReturnBranchId` | Each physical copy belongs to one branch. A pending transfer locks its copy from checkout. Receive a transfer to update `Item.BranchId`. Cross-branch returns update the holding branch and record the receiving branch. Transfer history remains in the database. |
| Reservations & waitlist | `/Reservations` (member/staff); reserve a borrowed/damaged/held copy from `/Catalogue/Details/{id}` | `Reservations` with `Pending`, `Ready`, `Collected`, `Cancelled`, `Expired` | First request wins. On item return or repair, first waiting hold is marked `Ready`; the borrower has 3 days to collect. Kiosk and reception checkout use one service to respect the queue. Cancel/expire promotes the next hold. |

### How to test manually

1. **Branches:** Admin → Branches → create a branch and add a reception desk. Admin → Manage Items → create a book and assign the branch. Verify details and the public `/api/catalogue/branches` endpoint.
2. **Transfer:** Reception → Transfers → request a transfer for an available unreserved copy to a different branch. Try to check it out while pending (must fail). Mark it received, then inspect Admin Items and the API for its new holding branch.
3. **Waitlist:** Reception borrows a copy for member A. Borrower B signs in at kiosk, opens the book's public catalogue details and reserves it. Borrower C reserves the same copy. Return A's copy. B should become `Ready` and see two simulator messages (email and SMS). C remains `Pending`. C must not be allowed to check out the book. Check it out to B; B becomes `Collected`.
4. **Cancellation and repair:** Put a copy in `Damaged` state and reserve it. Admin edits it to `Available`, which triggers the first hold. Cancel the ready hold and verify that the next borrower becomes `Ready` and is notified.
5. **Notifications:** Reception borrows an item with a due date one day ahead. Notifications → **Check overdue items and expiring holds now**. Verify `DueSoon` messages. Repeat the scan and confirm no duplicate reminders. Test an overdue item (date in the past) and verify `Overdue` and daily `Fine` simulator messages. Returning an overdue book records a charged fine event.
6. **Privacy:** Verify a borrower can view only their own reservation and notification records, cannot cancel another borrower's reservation, and cannot visit admin branch management. Confirm public APIs do not return borrower names, email, phone, loans or notification details.

### Developer notes / limitations

- **BackgroundService** is an in-process hourly check. On an app that sleeps when idle, the worker will not run while it is stopped. For production, use an external scheduler, a durable jobs service and business-specific time zone settings.
- The membership code plus password sign-in implemented in Section 1 remains required before member actions. No private account endpoints were added to the public API.
- For an assignment-sized demonstration, the reservation queue is **per physical copy**, not a title-level queue that automatically selects among interchangeable copies. An expansion could group copies by catalogue work/ISBN.
- The branch `OpeningHours` field is descriptive text, not an automatic open/closed computation.
- Notification `DeliveryStatus` is always `Simulated`. Real messaging would need consent management, secure provider credentials, opt-outs, delivery retries and audit controls.
- Payment processing and tracking fines *paid* versus *charged* belong to the later reporting deliverable; today's fine alerts report charge or estimates, not payment history.
- SQL Server `Serializable` transactions plus filtered unique indexes protect checkout, hold ownership and one-pending-transfer integrity. Consider automatic retry for transient SQL deadlocks in a production system.
- Before final submission, **run `dotnet build`, apply the migration to a copy of your database and perform the manual steps above**. This package has only been statically inspected here because the .NET SDK and a SQL Server test instance are unavailable.

## References followed

- [Microsoft Learn — Hosted services and BackgroundService](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0)
- [Microsoft Learn — EF Core one-to-many relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many)
- [Microsoft Learn — EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Microsoft Learn — EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Microsoft Learn — EF Core optimistic concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Microsoft Learn — Role-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles)

## Quick source map

- `Models/{LibraryBranch,ReceptionDesk,ItemTransfer,Reservation,LibraryNotification}.cs`
- `Models/Configuration/LibraryExtensionConfiguration.cs` and modified existing configurations
- `Services/{LibraryCheckoutService,ReservationService,TransferService,NotificationService,NotificationSweepService,LibraryNotificationWorker}.cs`
- `Controllers/{BranchesController,TransfersController,ReservationsController,NotificationsController}.cs`
- Modified `BorrowingController`, `AdminItemsController`, `BorrowersController`, `CatalogueController` and `CatalogueApiController`
- `Migrations/20261014060000_BranchesReservationsNotifications.cs` and updated model snapshot
