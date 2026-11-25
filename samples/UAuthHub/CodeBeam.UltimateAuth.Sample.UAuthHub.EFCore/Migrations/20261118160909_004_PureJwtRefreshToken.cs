using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class _004_PureJwtRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChainId",
                table: "UAuth_RefreshTokens",
                newName: "SessionChainId");

            migrationBuilder.RenameIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_ChainId",
                table: "UAuth_RefreshTokens",
                newName: "IX_UAuth_RefreshTokens_Tenant_SessionChainId");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "UAuth_RefreshTokens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthenticatedAt",
                table: "UAuth_RefreshTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "UAuth_RefreshTokens",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefreshChainId",
                table: "UAuth_RefreshTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_RefreshChainId",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "RefreshChainId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_UserKey_DeviceId",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "UserKey", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_RefreshChainId",
                table: "UAuth_RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_UserKey_DeviceId",
                table: "UAuth_RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AuthenticatedAt",
                table: "UAuth_RefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "UAuth_RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RefreshChainId",
                table: "UAuth_RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "SessionChainId",
                table: "UAuth_RefreshTokens",
                newName: "ChainId");

            migrationBuilder.RenameIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_SessionChainId",
                table: "UAuth_RefreshTokens",
                newName: "IX_UAuth_RefreshTokens_Tenant_ChainId");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "UAuth_RefreshTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
