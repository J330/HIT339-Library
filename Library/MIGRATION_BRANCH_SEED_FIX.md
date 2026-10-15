# Branch seed migration fix

`20261014060000_BranchesReservationsNotifications.cs` was updated to specify SQL Server `columnTypes` explicitly on `migrationBuilder.InsertData`. EF Core needs these types because this manually authored migration has no generated TargetModel.

After backing up the database, build the project and run `Update-Database` in Visual Studio Package Manager Console. There is no need to add a new migration or delete the existing database.

This fix was checked at source level and the ZIP was integrity-tested; it has not been compiled or run against SQL Server in this environment.
