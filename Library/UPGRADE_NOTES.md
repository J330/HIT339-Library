# Architecture, EF Core and ASP.NET Core Identity upgrade notes

This version keeps the existing Admin, Reception, Manager and public catalogue features while strengthening both the application architecture and the authentication model.

## Identity upgrade based on the supplied Microsoft guides

Microsoft references:

- Customize the Identity model: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-7.0#customize-the-model
- Add custom user data: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/add-user-data?view=aspnetcore-7.0&tabs=visual-studio

Changed files:

- `Users/Domain/ApplicationUser.cs`
  - Replaces the custom `User` class with `ApplicationUser : IdentityUser<Guid>`.
  - Adds `FullName` as application-specific user data using `[PersonalData]`.
- `Users/Infrastructure/ApplicationUserConfiguration.cs`
  - Only configures the application-specific `FullName` field; Identity configures its own user schema.
- `Roles/AppRoles.cs`
  - Keeps Admin/Reception/Manager role-name constants without maintaining a custom Role database entity.
- `Database/AppDbContext.cs`
  - Now derives from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
- `Controllers/AccountController.cs`
  - Replaces manual password verification, claims creation and cookie sign-in with `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`.
- `Database/DatabaseSeeder.cs`
  - Uses `RoleManager<IdentityRole<Guid>>` and `UserManager<ApplicationUser>` to create starter staff roles/accounts.
- `Program.cs`
  - Registers ASP.NET Core Identity and configures the existing `/Account/Login` and `/Account/AccessDenied` pages as Identity cookie destinations.
- `S379658-HIT339-Assignment2-Library.csproj`
  - Adds `Microsoft.AspNetCore.Identity.EntityFrameworkCore` version `10.0.11`, matching the project's EF Core 10.0.11 packages.
- `Migrations/202609122330_UseAspNetCoreIdentity.cs`
  - Replaces the old custom `Users`/`Roles` schema with the standard Identity user/role/claim/login/token tables.

The existing `Views/Account/Login.cshtml`, `Models/LoginViewModel.cs`, role-protected controllers and portal UI remain custom to this project.

## Other architecture improvements retained from the previous version

- Completed the `Item` to `Borrowing` inverse navigation relationship.
- Added EF Core configuration classes for items, borrowers and borrowings.
- Added a filtered unique index so one physical item cannot have two active loans at once.
- Centralised late-fine calculation in `FineService`.
- Moved manager statistics queries into `ManagerStatisticsService` and changed most totals to database-side aggregate queries.
- Added `BorrowerViewModel` so create/edit forms no longer post the EF entity directly.
- Replaced Admin item-type strings with the `LibraryItemType` enum.
- Prevented Admin from manually setting an item to `Borrowed`, or changing an actively borrowed item to another status.

## Starter staff accounts

The same starter credentials are recreated through ASP.NET Core Identity:

- Admin: `admin` / `Admin123!`
- Reception: `reception` / `Reception123!`
- Manager: `manager` / `Manager123!`

## Database upgrade

The included migrations can update a database that was created from the immediately previous project version. The Identity migration removes only the old custom authentication tables (`Users` and `Roles`) and recreates the starter staff accounts through Identity. Library items, borrowers and borrowing records are not dropped by that Identity migration.

If your local database came from a much older checkpoint without EF migration history, reset the development database once:

Visual Studio Package Manager Console:

```powershell
Drop-Database
Update-Database
```

Or from the `Library` folder:

```powershell
dotnet ef database drop --force
dotnet ef database update
```

## Verification commands

From the folder containing the `.slnx` file:

```powershell
dotnet restore
dotnet build
cd Library
dotnet run --launch-profile https
```
