# Calendar scheduling and demo borrower (October 2026)

## New demo borrower (Development environment only)

Once the application starts in the ASP.NET Core **Development** environment, `DatabaseSeeder` creates:

| Field | Seeded value |
| --- | --- |
| Username | `borrower` |
| Password | `Borrower123!` |
| Membership code | `LIBDEMO000001` |
| Email | `demo.borrower@example.test` |
| Name | Demo Borrower |
| Phone | 0400000000 |
| Address | 1 Example Street |

Only new accounts receive the starter password. If `borrower` exists, the seeder does not reset it. The password is deliberately never seeded in a non-Development environment. Change the password before distributing a demo database.

## Calendar

- Go to `/Notifications`; select a calendar day. The three creation forms appear beneath the selected day details.
- **Event:** borrowers create private notes; staff may create library-wide events (no borrower selected) or events assigned to a particular borrower.
- **Reservation:** select an item and, for staff, a borrower. The existing first-come/first-served reservation service places a hold immediately. An entry on the chosen calendar day reminds the member to check the hold status; this date is not a guaranteed collection date. Existing availability/duplicate/reservation constraints still apply. Cancelling a reservation removes its follow-up reminder.
- **Notification:** choose subject, message, and email/SMS/both; staff must select a recipient. A borrower can schedule only for their own account. The hourly worker (or staff **Run notification checks now**) logs simulated deliveries when the selected date arrives; it never sends actual messages.
- Members only see their own events, queue follow-ups, and messages, plus library-wide events. Staff can see everything.
- All dates are interpreted using the server's local calendar date; scheduled notifications are date-based (not time-of-day appointments). The worker runs at startup and once per hour. For newly scheduled same-day messages, staff can run checks immediately.

## Database update

New migration: `20261014220000_CalendarScheduling` creates `CalendarEntries`. The latest `AppDbContextModelSnapshot` was updated; other migrations are unchanged.

In **Visual Studio Package Manager Console**, select the web project and run:

```powershell
Update-Database
```

Then build and run the project. The application also calls `MigrateAsync()` at startup, but applying changes deliberately in the console is recommended.

## Quick tests

1. Open the website in Development; sign in at `/Kiosk/Login` using `borrower` / `Borrower123!`, and visit `/Notifications`.
2. Click tomorrow and create a private event. Confirm only that member or staff can see it.
3. Schedule an email reminder for today and check the notification log after the scheduled worker runs, or press **Run notification checks now** as staff. Check that repeated sweeps don't duplicate the log.
4. As staff, create a library-wide event and verify it appears for the seeded borrower.
5. Place a reservation for a borrowed or damaged item from a selected future day; confirm the queue entry is created immediately and the calendar follow-up appears on the chosen day. Cancel it and check the reminder disappears.
6. Verify a borrower cannot schedule another borrower's notification by tampering with the form.

## References used

- EF Core relationships: https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
- EF Core migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- EF Core transactions: https://learn.microsoft.com/en-us/ef/core/saving/transactions
- Background hosted services: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- ASP.NET Core Identity: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity

This was reviewed statically; a full build and a SQL Server migration test could not be run in this environment (no .NET SDK/SQL Server).
