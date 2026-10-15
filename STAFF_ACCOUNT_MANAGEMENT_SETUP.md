# Administrator-created staff accounts

## What changed
- New administrator-only pages: `/StaffAccounts` (directory) and `/StaffAccounts/Create` (new staff).
- Staff roles supported: **Admin**, **Manager** and **Reception**. Managers, receptionists, borrowers and guests cannot create staff accounts.
- New staff are created with ASP.NET Core Identity password hashing and are required to replace their temporary password at `/Account/ChangePassword` before accessing staff features.
- Role validation takes place on the server as well as in the form. Passwords are not shown in the staff directory or logged.
- The navigation entry **Management → Staff Account Management** only appears for admins.
- An administrator-issued account uses the existing `ApplicationUser.FullName`, `Email`, `MustChangePassword`, Identity Users/Roles tables. **No new EF migration is needed.**

## Updating your GitHub/Azure deployment
1. Copy the changed project files into your GitHub repository, preserving their paths (the patch ZIP contains only the changed files).
2. Open the solution, run `dotnet build` and resolve any errors before pushing.
3. Push to `main`; confirm the existing GitHub Actions workflow deploys successfully to App Service.
4. Open the Azure library website, sign in as the existing admin and choose **Management → Staff Account Management**.
5. Create a new staff user with a unique username and email, select the role, and enter a strong temporary password (12+ characters with mixed case, a number and a symbol).
6. Give the password to the person through a secure channel. Ask them to sign in via **Staff sign in**; they must change their password first.
7. Verify a receptionist/manager/borrower gets **Access Denied** when opening `/StaffAccounts/Create` directly.

## Existing staff and database
- Existing administrator, manager, reception and borrower accounts are unchanged.
- This feature does **not** reset or reveal existing passwords, manage branch assignments, or allow staff to grant themselves roles.
- Staff role management uses the same `AppRoles` as the rest of the system.
- The app download ZIP does **not** need rebuilding because only cloud-hosted ASP.NET pages changed.

## Security note
The admin must personally distribute temporary credentials; no real email/SMS dispatch is implemented. For real-world operations, a password-reset invitation and verified email delivery are preferable to sharing an initial password manually. Before production, configure stronger Identity password policy, email verification, auditing and account recovery. If a new staff member cannot sign in, review the Identity error and account role rather than changing the environment to Development.
