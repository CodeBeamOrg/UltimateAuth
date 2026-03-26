using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations.UAuthCredentialDb
{
    /// <inheritdoc />
    public partial class InitCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_PasswordCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SecretHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_PasswordCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordCredentials_Tenant_ExpiresAt",
                table: "UAuth_PasswordCredentials",
                columns: new[] { "Tenant", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordCredentials_Tenant_Id",
                table: "UAuth_PasswordCredentials",
                columns: new[] { "Tenant", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordCredentials_Tenant_RevokedAt",
                table: "UAuth_PasswordCredentials",
                columns: new[] { "Tenant", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordCredentials_Tenant_UserKey",
                table: "UAuth_PasswordCredentials",
                columns: new[] { "Tenant", "UserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_PasswordCredentials_Tenant_UserKey_DeletedAt",
                table: "UAuth_PasswordCredentials",
                columns: new[] { "Tenant", "UserKey", "DeletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_PasswordCredentials");
        }
    }
}
