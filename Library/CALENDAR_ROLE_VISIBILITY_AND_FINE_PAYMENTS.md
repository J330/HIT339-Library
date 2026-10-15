# Calendar update: role visibility and fine payments

This update extends the existing `/Notifications` calendar without replacing its borrowing, reservation, or notification functionality.

## Role-based event audiences

Staff-created calendar events now use a **Visible to** group of checkboxes. You can select any combination of:

- Administrators
- Managers (all users with the Manager role)
- Reception staff
- Borrowers

You must select at least one role. If Borrowers is selected, the **Specific borrower (optional)** dropdown restricts the borrower audience to that member. Leaving it blank allows all borrowers to see the event. Staff role selections still apply separately.

Borrower-created events remain private to the signed-in borrower. A borrower cannot assign staff roles through a forged request: the server ignores all role checkbox values for borrower-created events. Events are filtered on the server so users cannot see events excluded by their roles.

Existing manually created events retain their previous visibility on migration: staff could previously see all events, and borrowers could see their own plus unassigned events. The new migration gives these historical events all four audience role flags. Newly created borrower events are private by default.

## Recorded fine payments in calendar

Each receipt in `FinePayments` appears as a **Fine payment** on its `PaidAtUtc` date, converted to the server's local time. Details show amount, related item, and time. Staff also see the receipt reference. Borrowers see only their own payments; staff continue to see library-wide payment activity.

Only *recorded payments* are displayed: a fine being charged or becoming overdue is not treated as proof of payment. Payments appear automatically once reception/manager records a receipt through `/Payments` and refreshes `/Notifications`. No extra payment table or separate migration for payments is required.

## Apply the migration

In Visual Studio Package Manager Console, with the application project selected as Default Project:

```powershell
Update-Database
```

The included migration is `20261014230000_CalendarAudienceRoles`. **Do not run Add-Migration** for this update. Keep all existing migration files; do not delete or reset the database.

## Manual test plan

1. Sign in as a manager, open `/Notifications`, select a date and create an event visible only to **Managers** and **Reception**.
2. Confirm a reception account can see it, while a borrower account cannot.
3. Create a second event visible to **Borrowers** and **Managers**, choosing one specific borrower. Verify other borrowers cannot see it; managers still can.
4. Sign in as a borrower and create a personal event. Verify it is not shared with staff or other borrowers.
5. As reception/manager, record a real fine payment in `/Payments`. Return to `/Notifications` on the payment date and confirm the new green **Fine payment received** entry, amount, and receipt reference.
6. Verify the borrower associated with the paid loan sees the payment without its staff-only receipt reference and that a different borrower does not see it.
7. Check a month containing an older event; it should remain visible to the same audiences as before migration.

## Reference concepts

- EF Core migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- ASP.NET Core role-based authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
- EF Core relationships and filtering: https://learn.microsoft.com/en-us/ef/core/querying/

## Testing limitations

The ZIP was inspected for required source files, migration order, valid structure and archive integrity. The .NET SDK and SQL Server are unavailable in the packaging environment, so please compile and execute the migration in Visual Studio before deploying.
