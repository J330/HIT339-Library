# Branch Management, Borrower Seeds & Online Book Borrowing

## What's changed

- Management navigation and branch-list heading now say **Branch Management**; `/Branches` URLs remain unchanged.
- In **Development** only, the startup seeder creates two borrowers: `borrower1` and `borrower2` with the password `Borrower123!` (unless the users already exist). Their membership codes are `LIBDEMO000101` and `LIBDEMO000102`, and emails are `demo.borrower1@example.test` and `demo.borrower2@example.test`. The original `borrower` account, if present in an existing DB, is intentionally not deleted.
- Books (not music or toys) that are available and not held now have **Borrow online** on public catalogue cards and details. An authenticated borrower can click it, subject to the shared checkout availability, active-loan, transfer and queue rules. Loan due date is 14 days from checkout, branch is determined by the physical copy. The physical book still requires collection; online action *records* checkout rather than arranging delivery.
- Staff can open **Borrowing and returns**, view checkout method (`Reception`, `Kiosk`, `Online`), view borrower and branch details, edit the due date of active loans, and process returns. Admin, Manager and Reception roles have access to loan management. Staff can view borrower details; reception handles editing borrower profiles.

## Upgrade existing database

1. Back up your SQL Server database.
2. Open the Visual Studio solution and rebuild.
3. Select the library project in Package Manager Console and run `Update-Database`.
   This applies `20261015001000_OnlineBorrowingChannel` to add the `Channel` column to `Borrowings` (old rows default to `Reception`). Do *not* run `Add-Migration` because migration is supplied.
4. Start in the **Development** environment to seed `borrower1` and `borrower2`. Seeder does not reset passwords or overwrite real borrowers.
5. The project now directly references patched `System.Security.Cryptography.Pkcs` version `9.0.21` to resolve the previous NuGet `9.0.11` vulnerability warnings. Restore NuGet packages after opening.

## Testing

1. Login as `borrower1` / `Borrower123!`; open `/Catalogue`, select an available **Book**, click Borrow online. Confirm the item becomes Borrowed and a loan shows in `/Kiosk/Account` with 14-day due date.
2. Log in as `manager` / `Manager123!`, go to Management -> Borrowing and returns, find that loan with method `Online`, edit its due date and check notification calendar. Use Return Item to close the loan.
3. Log in as `borrower2` / `Borrower123!` and confirm that unavailable copies cannot be checked out; reserve the book when it is borrowed.
4. Verify only borrowers can POST to `/Catalogue/BorrowOnline` and the CSRF token is required.
5. Verify manager can use Branch Management to create branches.

## Implementation

Uses existing `LibraryCheckoutService` serializable transaction for online borrowing, preserving reservation priority, and EF Core migration for checkout source; no separate borrowing store.

**Validation note:** Sources were statically checked but the .NET SDK / SQL Server are unavailable in the container; compile and apply migrations locally in Visual Studio.
