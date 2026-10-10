using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.ORM.Migrations
{
    /// <inheritdoc />
    public partial class RegisterPartyPlatformApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "Id", "ClientId", "CreatedAt", "IsActive", "Name" },
                values: new object[] { new Guid("3e9a6c5f-4d77-4c6b-af40-9d8c1b5e0f43"), "festa-facil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Festa Fácil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: new Guid("3e9a6c5f-4d77-4c6b-af40-9d8c1b5e0f43"));
        }
    }
}
