# Updates: notification calendar, standard kiosk header, member usernames

## Notifications (`/Notifications`)
- Month-view calendar with previous/next month navigation and day selection. Due dates, planned two-day reminders, ready-hold collection deadlines, and simulated email/SMS log timestamps appear as colour-coded events. Select a date to read its full entries.
- Staff see events for all borrowers; a signed-in borrower only sees their own events and log entries. The log keeps the latest 100 simulated notifications; it is not an actual email/SMS gateway.
- The existing hourly background worker and staff-only manual notification check are retained. Planned reminder markers do not mean a message has already been sent. Deadlines use library-server local dates; hold and log UTC timestamps are converted for display.
- Upcoming events are computed from current loans and ready holds rather than stored as separate scheduled-notification rows. There is no new table or database migration.

## Self-service interface (`/Kiosk`)
- All kiosk pages now use the standard light library navigation (`_Layout.cshtml`), **not** the separate blue header.
- The touch-sized kiosk tiles/forms and camera barcode scanning continue to use `kiosk.css` and `kiosk.js`, loaded only on kiosk routes.
- The kiosk inactivity logout logic is retained through the existing sign-out form id on kiosk pages.

## Username / member sign-in
- Public registration requires a unique username (3–32 letters, digits, dots, underscores, hyphens). It is saved as `AspNetUsers.UserName`; the printed permanent membership code remains in `Borrowers.MembershipCode`.
- Members can sign in via **username, membership barcode, or email** at `/Kiosk/Login`, using the same existing password. Only Identity users with the Borrower role linked to a Borrower record may use this login.
- Existing members can continue using their membership code, and can set their own username at `/MemberAccount/Username` after confirming their current password. No new migration is needed.
- Usernames belong to the shared ASP.NET Identity user table and must be unique across staff and borrower accounts.

## Setup and checks
1. Open the `.slnx` in Visual Studio; restore packages and build.
2. No new migration is needed for these changes, but ensure **existing** migrations have been applied: `Update-Database`.
3. Register a new borrower with a username and sign in by username, email, and scanned membership code.
4. On an existing borrower, visit Contact details > Change username. Confirm the old membership code continues working.
5. Visit the self-service route and confirm the normal header appears, camera scan can be opened, and inactivity still signs out.
6. Visit `/Notifications`, browse months and select dates with due loans, ready holds, or simulated notifications. Verify borrower-only privacy and staff Run checks.

> Source-level changes were checked, but compile/runtime and SQL Server tests require Visual Studio and the project's database.
