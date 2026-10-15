using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;

#nullable disable
namespace S379658_HIT339_Assignment2_Library.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014233000_CalendarEventRecipients")]
public partial class CalendarEventRecipients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CalendarEntryRecipients",
            columns: table => new
            {
                CalendarEntryId = table.Column<int>(type: "int", nullable: false),
                BorrowerId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarEntryRecipients", x => new { x.CalendarEntryId, x.BorrowerId });
                table.ForeignKey("FK_CalendarEntryRecipients_CalendarEntries_CalendarEntryId", x => x.CalendarEntryId,
                    "CalendarEntries", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_CalendarEntryRecipients_Borrowers_BorrowerId", x => x.BorrowerId,
                    "Borrowers", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(
            name: "IX_CalendarEntryRecipients_BorrowerId", table: "CalendarEntryRecipients", column: "BorrowerId");

        // Preserve all previously created one-borrower events, including private member events.
        migrationBuilder.Sql(@"INSERT INTO [CalendarEntryRecipients] ([CalendarEntryId], [BorrowerId])
                               SELECT [Id], [BorrowerId] FROM [CalendarEntries]
                               WHERE [Kind] = 0 AND [BorrowerId] IS NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retain one selected recipient in the legacy BorrowerId when rolling back.
        migrationBuilder.Sql(@"UPDATE c SET c.[BorrowerId] =
                                   (SELECT MIN(r.[BorrowerId]) FROM [CalendarEntryRecipients] r
                                    WHERE r.[CalendarEntryId] = c.[Id])
                               FROM [CalendarEntries] c
                               WHERE c.[Kind] = 0 AND c.[BorrowerId] IS NULL
                               AND EXISTS (SELECT 1 FROM [CalendarEntryRecipients] r WHERE r.[CalendarEntryId] = c.[Id]);");
        migrationBuilder.DropTable(name: "CalendarEntryRecipients");
    }
}
