using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations.UAuthAuthenticationDb
{
    /// <inheritdoc />
    public partial class InitAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    CredentialType = table.Column<int>(type: "INTEGER", nullable: true),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastFailedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RequiresReauthentication = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResetRequestedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResetExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResetConsumedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResetTokenHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ResetAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_Authentication", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_LockedUntil",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "LockedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_ResetRequestedAt",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "ResetRequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "UserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "UserKey", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope_CredentialType",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "UserKey", "Scope", "CredentialType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_Authentication");
        }
    }
}
