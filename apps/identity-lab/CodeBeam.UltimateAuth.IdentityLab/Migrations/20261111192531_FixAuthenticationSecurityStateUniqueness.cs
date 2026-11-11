using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.IdentityLab.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthenticationSecurityStateUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope_CredentialType",
                table: "UAuth_Authentication");

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope_CredentialType",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "UserKey", "Scope", "CredentialType" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope_CredentialType",
                table: "UAuth_Authentication");

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Authentication_Tenant_UserKey_Scope_CredentialType",
                table: "UAuth_Authentication",
                columns: new[] { "Tenant", "UserKey", "Scope", "CredentialType" },
                unique: true);
        }
    }
}
