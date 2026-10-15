using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;

#nullable disable
namespace S379658_HIT339_Assignment2_Library.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014230000_CalendarAudienceRoles")]
public partial class CalendarAudienceRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Audience", table: "CalendarEntries", type: "int",
            nullable: false, defaultValue: 0);

        // Preserve existing calendar-event visibility during upgrade.
        // Previously staff could see all events, and borrowers could see their own
        // events or any event without a borrower assigned.
        migrationBuilder.Sql("UPDATE CalendarEntries SET Audience = 15 WHERE Kind = 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Audience", table: "CalendarEntries");
    }
}
