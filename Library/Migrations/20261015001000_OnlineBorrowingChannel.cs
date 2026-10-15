using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;

#nullable disable
namespace S379658_HIT339_Assignment2_Library.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015001000_OnlineBorrowingChannel")]
public partial class OnlineBorrowingChannel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Channel", table: "Borrowings", type: "int",
            nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Channel", table: "Borrowings");
    }
}
