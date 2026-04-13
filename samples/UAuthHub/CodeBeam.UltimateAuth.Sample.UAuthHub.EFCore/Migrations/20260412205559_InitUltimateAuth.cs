using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitUltimateAuth : Migration
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
                    LastFailedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresReauthentication = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResetRequestedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetTokenHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ResetAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_Authentication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_PasswordCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SecretHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_PasswordCredentials", x => x.Id);
                });

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
                    ReplacedByTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_RolePermissions",
                columns: table => new
                {
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_RolePermissions", x => new { x.Tenant, x.RoleId, x.Permission });
                });

            migrationBuilder.CreateTable(
                name: "UAuth_Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_SessionRoots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootId = table.Column<Guid>(type: "TEXT", maxLength: 128, nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_SessionRoots", x => x.Id);
                    table.UniqueConstraint("AK_UAuth_SessionRoots_Tenant_RootId", x => new { x.Tenant, x.RootId });
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserIdentifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserLifecycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserLifecycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProfileKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: true),
                    Bio = table.Column<string>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", nullable: true),
                    Culture = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UAuth_UserRoles",
                columns: table => new
                {
                    Tenant = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UAuth_UserRoles", x => new { x.Tenant, x.UserKey, x.RoleId });
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbsoluteExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Device = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimsSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ActiveSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    RotationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TouchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityVersionAtCreation = table.Column<long>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RolePermissions_Tenant_Permission",
                table: "UAuth_RolePermissions",
                columns: new[] { "Tenant", "Permission" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_RolePermissions_Tenant_RoleId",
                table: "UAuth_RolePermissions",
                columns: new[] { "Tenant", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Roles_Tenant_Id",
                table: "UAuth_Roles",
                columns: new[] { "Tenant", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_Roles_Tenant_NormalizedName",
                table: "UAuth_Roles",
                columns: new[] { "Tenant", "NormalizedName" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_NormalizedValue",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "NormalizedValue" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_Type_NormalizedValue",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "Type", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey_IsPrimary",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserIdentifiers_Tenant_UserKey_Type_IsPrimary",
                table: "UAuth_UserIdentifiers",
                columns: new[] { "Tenant", "UserKey", "Type", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserLifecycles_Tenant_UserKey",
                table: "UAuth_UserLifecycles",
                columns: new[] { "Tenant", "UserKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserProfiles_Tenant_UserKey_ProfileKey",
                table: "UAuth_UserProfiles",
                columns: new[] { "Tenant", "UserKey", "ProfileKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserRoles_Tenant_RoleId",
                table: "UAuth_UserRoles",
                columns: new[] { "Tenant", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UAuth_UserRoles_Tenant_UserKey",
                table: "UAuth_UserRoles",
                columns: new[] { "Tenant", "UserKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UAuth_Authentication");

            migrationBuilder.DropTable(
                name: "UAuth_PasswordCredentials");

            migrationBuilder.DropTable(
                name: "UAuth_RefreshTokens");

            migrationBuilder.DropTable(
                name: "UAuth_RolePermissions");

            migrationBuilder.DropTable(
                name: "UAuth_Roles");

            migrationBuilder.DropTable(
                name: "UAuth_Sessions");

            migrationBuilder.DropTable(
                name: "UAuth_UserIdentifiers");

            migrationBuilder.DropTable(
                name: "UAuth_UserLifecycles");

            migrationBuilder.DropTable(
                name: "UAuth_UserProfiles");

            migrationBuilder.DropTable(
                name: "UAuth_UserRoles");

            migrationBuilder.DropTable(
                name: "UAuth_SessionChains");

            migrationBuilder.DropTable(
                name: "UAuth_SessionRoots");
        }
    }
}
