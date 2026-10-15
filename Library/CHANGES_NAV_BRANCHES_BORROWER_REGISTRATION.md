# Library site changes: manager branches, navigation and borrower registration

## Where to find the changes
- **Management → Library branches → Create new branch** (`/Branches/Create`): Manager **and** Admin roles can create, edit and manage branches and reception desks. Branch codes stay unique.
- **Navigation:** Bootstrap collapsible header, with Explore / My library / Staff operations / Management dropdown groups. On smaller screens choose **Open navigation menu**. Staff do not see the public kiosk button on the header or home page.
- **Create account** (`/MemberAccount/Register`): an anonymous borrower can register with their name, email, phone, address and a password. The database creates a Borrower record and an ASP.NET Identity login (role: Borrower), and assigns a generated membership code. The borrower is signed in automatically and can see their code on **My account**.
- **My library → Contact details** (`/MemberAccount/Details`): borrowers can view and update their own account information. Borrowers are selected via their authenticated Identity user ID, not through a user-supplied ID.
- Existing reception-created borrower records **cannot** be claimed just by typing someone else's email. Those users should ask reception to activate their existing account.

## Setup
1. Back up the database and replace project files with this ZIP.
2. Run `dotnet restore` then `dotnet build` in the `Library/Library` directory (or build in Visual Studio).
3. Run `Update-Database` in Package Manager Console if previous migrations have not already been applied.
4. Run the site. **No new migration is required for these four requested changes**, since borrower contact data, membership code and branch information were already present in the existing tables.

## Manual tests
1. Sign in as **manager**. Under Management choose **Create a branch**; create a unique branch code and check it appears in the list.
2. Sign in as **admin** and repeat. Try duplicating a code (should show a validation message).
3. Sign in as **reception**, **manager**, or **admin**; verify no Self-service kiosk link/button appears in the site header or home.
4. Sign out. Check that the navigation collapses on a narrow window and that public pages, borrower login and registration are accessible.
5. Register a borrower from `/MemberAccount/Register`; confirm that membership code appears and the account can view its own contact details, reservations and checkout pages.
6. Sign out and log back in from the kiosk using the assigned membership code and password.
7. Try registering with an email already used by a reception-created borrower; the UI should tell you to contact reception instead of linking to someone else's account.
8. Edit borrower contact details and confirm staff see the updates in borrower records.

## Notes
- Password hashes are stored by ASP.NET Identity, not in the Borrower record.
- Account registration includes a per-IP rate limit and anti-forgery protection, but **email addresses are not verified** in this demonstration project. Add email confirmation before using public registration in production.
- The Borrower model already contains contact information, so these changes do not introduce new database columns.
- C# compilation and a running SQL Server instance could not be tested in this environment. Validate the build and migrations in Visual Studio before submission.
