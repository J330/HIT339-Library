# Staff password reset (admin-only)

This update adds a **Reset password** action for each *other* staff member on `Management > Staff Account Management` (`/StaffAccounts`). It removes the Change password link from the global navigation but retains `/Account/ChangePassword` for first-login and self-service password changes.

## What to do
1. Apply the patch to your `HIT339-Library` repository, preserving folder paths.
2. Build the ASP.NET Core website with `dotnet build ./Library/S379658-HIT339-Assignment2-Library.csproj`.
3. Commit and push. Azure's GitHub Actions deployment updates the hosted website.
4. Sign in as an administrator and go to `/StaffAccounts`; click **Reset password** beside a different admin, manager or receptionist.
5. Enter a strong temporary password twice. Give it to the employee securely. They must change it on their next staff login.

## Important safeguards
- Both reset endpoints require the `Admin` role; the POST requires a CSRF token.
- Accounts are checked against actual staff roles, so borrowers cannot be reset through this interface.
- Administrators cannot reset their own password from this page. The existing `/Account/ChangePassword` route remains available.
- ASP.NET Core Identity's `GeneratePasswordResetTokenAsync` and `ResetPasswordAsync` update password hashes/security stamps; no plaintext password is stored or displayed.
- The new temporary password requires 12+ characters, upper/lowercase, number and symbol.
- Password resets set `MustChangePassword = true`, unlock the target account, and reset failed attempts.
- Password-reset audit entries are written to application logs without including the password.
- Existing auth cookies may remain usable until Identity's security-stamp validation next runs. For stricter instant session revocation, use session management or adjust Identity's security-stamp validation policy.

No new EF Core migration is required. The desktop WebView2 client does not need to be republished; the change is server-side.

**Build and Azure runtime testing required.** The source changes were validated structurally, but not executed against Azure SQL.
