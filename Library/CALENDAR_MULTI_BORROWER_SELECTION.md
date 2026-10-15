# Calendar events: multiple specific borrowers

## What's changed
- Under **Notifications → click a date → New calendar event**, staff can use **Specific borrowers** → **Add** multiple times. Names appear below the selector, each with a Remove button. Up to 200 borrowers can be selected.
- Adding a borrower automatically checks **Borrowers** under Visible to. Other checked roles (Admin, Manager, Reception) continue to see the same event regardless of the targeted borrower list.
- With the Borrowers role checked and **no names selected**, the event is visible to all borrowers. If specific names are selected, **only those borrowers** see it. Borrowers creating their own events still create private events.
- Existing targeted events are copied into the new recipient table, preserving their privacy. Fine payment and scheduled notification visibility is unchanged.

## Setup
This project includes migration `20261014233000_CalendarEventRecipients`. Back up the database, rebuild, and run `Update-Database` in Visual Studio Package Manager Console. Do **not** run `Add-Migration`.

## Quick test
1. Sign in as manager or reception staff. Open `/Notifications`, click a date and create an event. Check Borrowers and Manager, add two specific borrowers and remove one to test the list. Submit.
2. Sign in as the selected borrower; the event should be visible. Sign in as another borrower; it should not be visible. Managers should see it either way.
3. Create an event with Borrowers checked and no names selected. All borrowers should see it.
4. Create an event while signed in as a borrower. Other borrowers must not see it.
5. Previously created targeted events and fine payments should still follow their original visibility rules.

Compilation and SQL Server migration must be verified locally.
