using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Identity.ORM.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationRolesAndUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "Id", "ClientId", "CreatedAt", "IsActive", "Name" },
                values: new object[] { new Guid("0b6d3f2c-1a44-4f3e-9c1d-6a5f8e2b7c10"), "developer-store", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Developer Store" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ApplicationId", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("1c7e4a3d-2b55-4a4f-8d2e-7b6a9f3c8d21"), new Guid("0b6d3f2c-1a44-4f3e-9c1d-6a5f8e2b7c10"), "Full access to the sample application", "Admin" },
                    { new Guid("2d8f5b4e-3c66-4b5a-9e3f-8c7b0a4d9e32"), new Guid("0b6d3f2c-1a44-4f3e-9c1d-6a5f8e2b7c10"), "Regular customer of the sample application", "Customer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ApplicationId",
                table: "RefreshTokens",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ClientId",
                table: "Applications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ApplicationId_Name",
                table: "Roles",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Applications_ApplicationId",
                table: "RefreshTokens",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Applications_ApplicationId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ApplicationId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "RefreshTokens");
        }
    }
}
