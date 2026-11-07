using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class _003_PasswordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_PasswordHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CredentialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecretHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ReplacedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_PasswordHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordHistory_Tenant_CredentialId_ReplacedAt_Id",
                table: "UAuth_PasswordHistory",
                columns: new[] { "Tenant", "CredentialId", "ReplacedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordHistory_Tenant_UserKey",
                table: "UAuth_PasswordHistory",
                columns: new[] { "Tenant", "UserKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_PasswordHistory");
        }
    }
}
