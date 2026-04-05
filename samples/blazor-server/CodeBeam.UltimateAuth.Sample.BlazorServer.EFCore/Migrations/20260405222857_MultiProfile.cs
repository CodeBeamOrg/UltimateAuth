using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class MultiProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey",
                table: "UAuth_UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ProfileKey",
                table: "UAuth_UserProfiles",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey_ProfileKey",
                table: "UAuth_UserProfiles",
                columns: new[] { "Tenant", "UserKey", "ProfileKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey_ProfileKey",
                table: "UAuth_UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileKey",
                table: "UAuth_UserProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey",
                table: "UAuth_UserProfiles",
                columns: new[] { "Tenant", "UserKey" });
        }
    }
}
