# Seed five demo accounts on Azure (one-time, explicit opt-in)

The production version of the Library Management application can create `admin`, `manager`, `reception`, `borrower1` and `borrower2` **without enabling Development mode**. Their passwords are not checked into GitHub; the seeder uses five separate values from Azure App Service **Environment variables / App settings**. There is no database schema change and no new EF migration.

## 1. Deploy this version from GitHub

Commit and push the updated `Library/Database/DatabaseSeeder.cs`, `Library/Program.cs`, and `Library/appsettings.Production.json` (within the web application project). Wait for GitHub Actions to publish this code successfully to the `library-management-demo` Azure App Service. Its Azure SQL connection and existing migrations must already work before seeding.

## 2. Configure secrets in App Service

Go to **Azure Portal → App Services → library-management-demo → Settings → Environment variables → App settings** and add these entries. Choose **five different, strong passwords** (at least 12 characters, each containing uppercase, lowercase, a digit and a symbol). Do not use the local Development sample passwords.

| App setting name | Value |
|---|---|
| `LibraryDemoSeed__AdminPassword` | *new unique admin password* |
| `LibraryDemoSeed__ManagerPassword` | *new unique manager password* |
| `LibraryDemoSeed__ReceptionPassword` | *new unique reception password* |
| `LibraryDemoSeed__Borrower1Password` | *new unique borrower1 password* |
| `LibraryDemoSeed__Borrower2Password` | *new unique borrower2 password* |
| `LibraryDemoSeed__AdminEmail` | *optional, your real administrator email* |
| `LibraryDemoSeed__Enabled` | `true` **only after all five passwords are set** |

**Do not place any of these values in `appsettings.json`, source code, GitHub Actions logs, or README files.** If you previously configured `LibraryBootstrap__AdminUsername`, `LibraryBootstrap__AdminPassword` and `LibraryBootstrap__AdminEmail`, note that an already-existing admin account will NOT have its password replaced; the new seeder skips existing accounts with the expected role.

Choose **Apply** and **Save**. Restart the App Service if required. On startup the application validates all five passwords before inserting any records. If there is a username/role or membership-code collision, it stops without granting an unexpected account extra permissions. Writes for the five accounts are done in a single database transaction.

## 3. Check creation in Azure SQL

Open **SQL databases → LibraryManagementDB → Query editor** and run:

```sql
SELECT UserName FROM dbo.AspNetUsers
WHERE NormalizedUserName IN ('ADMIN','MANAGER','RECEPTION','BORROWER1','BORROWER2')
ORDER BY UserName;

SELECT MembershipCode, FirstName, LastName
FROM dbo.Borrowers
WHERE MembershipCode IN ('LIBDEMO000101','LIBDEMO000102');
```

Expected usernames: `admin`, `manager`, `reception`, `borrower1`, `borrower2` (unless an account with the same name already existed and had the correct role). The two demo borrower membership codes are `LIBDEMO000101` and `LIBDEMO000102`.

Try signing in as each account with **its configured password**. Development passwords `Admin123!`, `Manager123!`, `Reception123!`, `Borrower123!` are **not** used in Azure. New accounts are assigned to the corresponding Identity role. Existing users keep their original passwords and profile information; seeding never resets them.

## 4. Disable the seeder and delete the secrets

Once the five accounts exist and you have successfully signed in, change `LibraryDemoSeed__Enabled` to `false` and **delete all five `LibraryDemoSeed__*Password` values** from App Service. Save and restart. The accounts remain in Azure SQL after the switch is turned off, and GitHub redeployments do not delete them.

The optional admin email setting can be removed too. You do not need to keep the demo-seeding passwords available after setup. The seeder is idempotent; it never overwrites an existing user's password or promotes an unrelated user that has a conflicting username.

## Troubleshooting

- `Invalid object name 'AspNetUsers'` or `AspNetRoles`: Apply existing EF Core migrations to the **Azure** database first.
- Missing `LibraryDemoSeed:...Password`: Configure **all five** passwords in Azure, then enable the switch; otherwise startup fails intentionally, and transactions prevent partially seeded users.
- Existing username with the wrong role: Do not manually change roles to bypass the error. Check account ownership/identity, then choose an appropriate correction.
- Existing admin with a different password: The seeder deliberately leaves it unchanged. Use a proper password reset procedure instead of expecting redeployment to overwrite it.
- `HTTP 500` after enabling: Open **App Service → Monitoring → Log stream** and check the latest startup exception. Never enable the `Development` environment on the public site.

**Security:** This is a demo/staging convenience only. A publicly reachable server containing test accounts is still a target for guessing and misuse. Configure unique passwords, limit account permissions, and remove the secrets as soon as setup is completed. Prefer normal invitation and account provisioning for a real deployment.
