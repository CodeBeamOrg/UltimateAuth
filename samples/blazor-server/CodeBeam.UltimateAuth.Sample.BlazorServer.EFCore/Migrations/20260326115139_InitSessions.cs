using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UAuth_SessionRoots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootId = table.Column<Guid>(type: "TEXT", maxLength: 128, nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_SessionRoots", x => x.Id);
                    table.UniqueConstraint("AK_UAuth_SessionRoots_Tenant_RootId", x => new { x.Tenant, x.RootId });
                });

            migrationBuilder.CreateTable(
                name: "UAuth_SessionChains",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AbsoluteExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Device = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimsSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ActiveSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    RotationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TouchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersionAtCreation = table.Column<long>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_SessionChains", x => x.Id);
                    table.UniqueConstraint("AK_UAuth_SessionChains_Tenant_ChainId", x => new { x.Tenant, x.ChainId });
                    table.ForeignKey(
                        name: "FK_UAuth_SessionChains_UAuth_SessionRoots_Tenant_RootId",
                        columns: x => new { x.Tenant, x.RootId },
                        principalTable: "UAuth_SessionRoots",
                        principalColumns: new[] { "Tenant", "RootId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_Sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SecurityVersionAtCreation = table.Column<long>(type: "INTEGER", nullable: false),
                    Device = table.Column<string>(type: "TEXT", nullable: false),
                    Claims = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UAuth_Sessions_UAuth_SessionChains_Tenant_ChainId",
                        columns: x => new { x.Tenant, x.ChainId },
                        principalTable: "UAuth_SessionChains",
                        principalColumns: new[] { "Tenant", "ChainId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionChains_Tenant_ChainId",
                table: "UAuth_SessionChains",
                columns: new[] { "Tenant", "ChainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionChains_Tenant_RootId",
                table: "UAuth_SessionChains",
                columns: new[] { "Tenant", "RootId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionChains_Tenant_UserKey",
                table: "UAuth_SessionChains",
                columns: new[] { "Tenant", "UserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionChains_Tenant_UserKey_DeviceId",
                table: "UAuth_SessionChains",
                columns: new[] { "Tenant", "UserKey", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionRoots_Tenant_RootId",
                table: "UAuth_SessionRoots",
                columns: new[] { "Tenant", "RootId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_SessionRoots_Tenant_UserKey",
                table: "UAuth_SessionRoots",
                columns: new[] { "Tenant", "UserKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_ChainId",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "ChainId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_ChainId_RevokedAt",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "ChainId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_ExpiresAt",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_RevokedAt",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_SessionId",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Sessions_Tenant_UserKey_RevokedAt",
                table: "UAuth_Sessions",
                columns: new[] { "Tenant", "UserKey", "RevokedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_Sessions");

            migrationBuilder.DropTable(
                name: "UAuth_SessionChains");

            migrationBuilder.DropTable(
                name: "UAuth_SessionRoots");
        }
    }
}
