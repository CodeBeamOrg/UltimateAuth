using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations.UAuthTokenDb
{
    /// <inheritdoc />
    public partial class InitTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TokenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_ChainId",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "ChainId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_ExpiresAt",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_ExpiresAt_RevokedAt",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_ReplacedByTokenHash",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "ReplacedByTokenHash" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_SessionId",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_TokenHash",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "TokenHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_TokenHash_RevokedAt",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "TokenHash", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_TokenId",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "TokenId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RefreshTokens_Tenant_UserKey",
                table: "UAuth_RefreshTokens",
                columns: new[] { "Tenant", "UserKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_RefreshTokens");
        }
    }
}
