# Cloud deployment + staff-only Windows application

This is a source-code update to the existing ASP.NET Core MVC library. No borrowing/catalogue/notification database schema changes are needed for this feature.

## What changed

- **Staff download page:** `/Downloads` and `/Downloads/Windows` both require an authenticated **Admin, Manager or Reception** role. Borrowers/visitors cannot retrieve the file by typing its URL. Only staff get the header link. The ZIP must be stored outside `wwwroot`.
- **Cloud SQL:** the website uses its existing `DefaultConnection` with cloud settings from environment variables; production explicitly rejects a LocalDB connection.
- **Demo users:** the default `admin`, `manager`, `reception`, `borrower1`, and `borrower2` accounts are now created **only in Development**. Production can create one initial admin with a secret password from configuration.
- **Security:** production cookies are Secure-only; HTTPS and HSTS remain enabled; staff login failures count toward lockout; forwarded headers only trust configured proxies.
- **Notification jobs:** a configuration option controls the hourly notification worker, useful when hosting multiple instances.
- **Windows desktop client source:** a separate **WPF + WebView2** project under `LibraryDesktopClient` that embeds the cloud site. This is a *portable Windows client*, not a local copy of SQL Server or the ASP.NET backend.

## A. Deploy the website and database (example: Azure)

1. Create **Azure App Service** configured for ASP.NET Core / .NET 10 and **Azure SQL Database**. Check pricing and region first.
2. Set the cloud SQL connection string in App Service **Environment variables / App settings** using the exact key `ConnectionStrings__DefaultConnection`. Example SQL authentication connection (replace placeholders; do NOT commit this value):

   `Server=tcp:YOURSERVER.database.windows.net,1433;Initial Catalog=LibraryManagementDB;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;User ID=YOURUSER;Password=YOUR_SECRET;`

   Configure Azure SQL firewall / networking to permit the app and deployment machine, or use an appropriate managed-identity connection. Ensure the SQL database and login exist first.
3. Apply your existing EF Core migrations to the *cloud* database from your workstation before the first production launch. Example from the `Library/Library` folder in PowerShell:

   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Production"
   $env:ConnectionStrings__DefaultConnection = "<YOUR_CLOUD_SQL_CONNECTION_STRING>"
   dotnet ef database update
   Remove-Item Env:ConnectionStrings__DefaultConnection
   Remove-Item Env:ASPNETCORE_ENVIRONMENT
   ```

   Use secret storage, avoid shell history and avoid sharing connection strings/screenshots. Back up any existing production database first.
4. To create **one initial production admin**, temporarily configure these App Service environment settings:
   - `LibraryBootstrap__AdminUsername` = a unique admin username
   - `LibraryBootstrap__AdminPassword` = a **strong, 12+ character** password
   - `LibraryBootstrap__AdminEmail` = email (optional)

   Restart the app and sign in as this new admin; then remove the bootstrap **password** setting and restart again. The bootstrap procedure does not reset an existing admin password. Production does **not** create the known demonstration accounts. **Rotate any demo passwords if you migrate an old demo database into production.**
5. Deploy the website project `Library/Library/S379658-HIT339-Assignment2-Library.csproj` to App Service via Visual Studio Publish, GitHub Actions, or `dotnet publish`. Configure the app hostname with `AllowedHosts` if you are restricting hosts. Verify `/Catalogue` over HTTPS and sign-in works.
6. For reliable scheduled notifications, host **one** instance with Always On enabled (supported App Service tier); otherwise run the worker separately. Configure `LibraryHosting__RunNotificationWorker=false` on web replicas when using a dedicated job runner. Background workers in several app replicas can process the same records twice without distributed locking.
7. If deploying behind a reverse proxy other than IIS/Azure App Service integration, configure `ReverseProxy__KnownProxies__0` etc. with the proxy's IP before trusting `X-Forwarded-*`. Do not trust forwarded headers from any arbitrary internet client.
8. For multi-instance deployments, configure a shared, protected ASP.NET Core Data Protection key ring so authentication cookies work between replicas. Consider Azure Blob Storage + Key Vault key protection. Do not deploy with ephemeral keys when scaling out.

## B. Build the staff Windows client on a Windows PC

Requires Windows, the **.NET 10 SDK**, and internet to restore the `Microsoft.Web.WebView2` NuGet package. The virtual machine needs the Microsoft Edge **WebView2 Runtime** (typically present on Windows 11; install separately if missing).

From `Library/LibraryDesktopClient`, run:

```powershell
.\Publish-WindowsClient.ps1 -LibraryUrl "https://YOUR-LIBRARY.azurewebsites.net/"
```

The script builds `LibraryDesktopClient.exe` and a portable **`published/LibraryDesktop-Windows-x64.zip`** containing the app files and `library-client.json` with the hosted HTTPS site URL.

> `LibraryDesktopClient` intentionally is a standalone project, not added to the existing `.slnx`, so rebuilding the ASP.NET solution does not need WPF Windows targeting or WebView2 NuGet restore. The WPF project is the downloadable front-end; no database is installed on the VM.

Test locally with `http://localhost:5014` (loopback HTTP only) if the hosted site is not ready; rebuild the package with its final HTTPS address before distribution.

## C. Place the downloaded app on the server

Copy `LibraryDesktop-Windows-x64.zip` into the ASP.NET app's **private** directory `App_Data/Downloads/` alongside (NOT inside) `wwwroot`. Alternatively set:

- `Downloads__WindowsPackagePath` = an **absolute server-side path** outside `wwwroot`, e.g. a persistent and access-controlled volume used by Azure App Service.

Use persistent storage for this package; deployment may overwrite files in the app's content directory. **Never** put the downloadable ZIP under `wwwroot`, since files there are served without controller authorization.

Log into the hosted site as Admin/Manager/Reception. The navigation now shows **Staff app download** and `/Downloads` offers the package if present. Borrowers and anonymous users do not see the navigation link and cannot retrieve `/Downloads/Windows` directly (authentication/authorization is enforced on the server).

## D. Test on the VM

1. Start a clean Windows virtual machine with internet access.
2. Browse to the hosted library URL and sign in **as staff**.
3. Open **Staff app download**, download the ZIP, extract it, and run `LibraryDesktopClient.exe`.
4. Sign in to the library *within* the desktop client. The same hosted data (borrowers, loans, reservations and notifications) appears for authorised roles.
5. Test the security boundary: sign out and try `/Downloads/Windows` as an anonymous visitor or borrower; it should not return the archive. Test manager and reception staff positive access.

## Security and limitations

- The client is a website wrapper; it **requires network access**, and its users authenticate against the server using the normal ASP.NET Core Identity accounts. It does not include an offline copy of the database.
- The Windows package is **not precompiled in this ZIP** because building WPF/WebView2 is a Windows-specific build step. The website will show an informational message until you publish/upload the package.
- The script produces a portable ZIP of an EXE and dependencies, **not an MSI/MSIX setup installer**. You can add MSIX/Inno Setup later if you need an actual installation wizard.
- Restricting the download to staff does not make copies of the binary revocable once shared. Login/permissions remain enforced by the hosted website regardless of how someone obtains the client.
- Cloud SQL, Azure App Service, deployment, DNS, TLS configuration and the live installer upload require your own credentials and must be completed on your infrastructure.
- This project has not been compiled or exercised against Azure/SQL Server in this environment. Run `dotnet build`, `dotnet ef database update`, publish the WPF project on Windows and perform the security tests before submission.

## References

- [Azure App Service: ASP.NET Core with Azure SQL](https://learn.microsoft.com/en-us/azure/app-service/tutorial-dotnetcore-sqldb-app)
- [ASP.NET Core authorization by role](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles)
- [Microsoft WebView2 WPF tutorial](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf)
- [Publish .NET applications](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
