# Online loan duration, account HUD and calendar borrower search

## Features
- In the public catalogue and on each book details page, authenticated borrowers choose an integer loan period between **1 and 14 days** (default 14) before online checkout.
- Server validates the range with `OnlineBorrowViewModel` and recalculates the due date using the server's date. It does not trust a submitted due date. Checkout still goes through the existing `LibraryCheckoutService`, including hold priority, branch checks, notifications and transactional availability enforcement.
- Signed-in borrowers see a **My borrowed books** HUD in `/Catalogue` with total active books, due-soon count, overdue count, next five loans and a link to `/Kiosk/Account` for all their loans. Anonymous users and staff do not see borrower loan details.
- `/Kiosk/Account` shows the branch and days remaining / overdue for each active loan.
- In `/Notifications`, the staff-only **Specific borrowers** event audience control can now search by full/partial name, Identity username or membership code and build a removable multi-borrower list. Filtering runs client-side against the existing staff-only picker options. A maximum of 200 selected borrowers remains enforced by the server.

## Setup
No new models or database columns were added, so **no new EF Core migration is needed**. Open the `.slnx` in Visual Studio, restore and build, and start the application. Apply any earlier pending migrations if needed: `Update-Database`.

## Testing checklist
1. Log in as `borrower1`. Open `/Catalogue`, choose an available book, select 3 days and borrow. Verify the resulting due date is three calendar days after today in `/Kiosk/Account`, `/Catalogue` HUD, and the notification calendar.
2. Borrow another book for 14 days. Verify the catalogue HUD shows two books, dates and branch names.
3. Use browser dev tools to submit `LoanDays=0` or `LoanDays=15` to `/Catalogue/BorrowOnline`. Verify validation blocks it; neither loan is created.
4. Confirm that a staff user or a guest cannot see another user's loan HUD or borrow a book online.
5. Sign in as a manager, open `/Notifications`, create an event and search for `borrower1` then `borrower2`. Add both and verify they remain in the selected list, then remove one and save. Verify calendar visibility only matches the selected recipients and staff-role checkboxes.
6. Verify checkout conflicts, reservations and no-longer-available copies continue to be rejected by `LibraryCheckoutService`.

## Notes
- Online borrowing begins the physical item's loan immediately; staff still manage collection/return via the existing borrowing screens.
- The account HUD is for active **books**; the full account page continues to list all types of borrowed library items.
- Tested through source checks and archive validation only; compilation and EF/database execution must be verified on your Visual Studio/SQL Server installation.
