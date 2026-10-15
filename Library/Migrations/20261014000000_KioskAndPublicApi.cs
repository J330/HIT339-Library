using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace S379658_HIT339_Assignment2_Library.Migrations
{
    public partial class KioskAndPublicApi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityUserId",
                table: "Borrowers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipCode",
                table: "Borrowers",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            // Existing borrower rows must have unique member codes before the unique index is created.
            migrationBuilder.Sql(
                "UPDATE [Borrowers] SET [MembershipCode] = 'LIB' + RIGHT('000000000000' + CONVERT(varchar(12), [Id]), 12) WHERE [MembershipCode] = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_IdentityUserId",
                table: "Borrowers",
                column: "IdentityUserId",
                unique: true,
                filter: "[IdentityUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_MembershipCode",
                table: "Borrowers",
                column: "MembershipCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrowers_AspNetUsers_IdentityUserId",
                table: "Borrowers",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrowers_AspNetUsers_IdentityUserId", table: "Borrowers");
            migrationBuilder.DropIndex(name: "IX_Borrowers_IdentityUserId", table: "Borrowers");
            migrationBuilder.DropIndex(name: "IX_Borrowers_MembershipCode", table: "Borrowers");
            migrationBuilder.DropColumn(name: "IdentityUserId", table: "Borrowers");
            migrationBuilder.DropColumn(name: "MembershipCode", table: "Borrowers");
            migrationBuilder.DropColumn(name: "MustChangePassword", table: "AspNetUsers");
        }
    }
}
