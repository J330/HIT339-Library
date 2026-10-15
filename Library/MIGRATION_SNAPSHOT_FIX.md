# Fix for `Update-Database` navigation exception

## Why the migration failed

The generated `Migrations/AppDbContextModelSnapshot.cs` in the earlier project archive tried to configure `Borrower.Reservations` and `Borrower.Notifications` *before* declaring the corresponding `HasOne(...).WithMany(...)` relationships. EF Core fails while constructing the snapshot model. It is a problem in the supplied snapshot, not an error caused by your borrower data.

## What changed

Only the order of `b.Navigation(...)` calls inside `AppDbContextModelSnapshot.cs` was changed: they now run after all relationships are declared. No entities, schema definitions or migration `Up`/`Down` operations were changed. The existing library functionality is retained.

## In Visual Studio

1. Back up your SQL Server database.
2. Open the solution in the patched ZIP (or copy its `Migrations/AppDbContextModelSnapshot.cs` over the original).
3. Set the Library project as the Default Project in Package Manager Console.
4. Run `Get-Migration`, then `Update-Database`.
5. If you see `PendingModelChangesWarning`, do not suppress it. Run `dotnet ef migrations has-pending-model-changes` to verify the snapshot/model, and create a correctly scaffolded follow-up migration if required.

The ZIP was checked structurally but was **not** compiled or tested against SQL Server here because the .NET SDK is not available in this environment.
