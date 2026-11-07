using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Policies;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class UAuthPasswordUserSimilarityCheckerTests
{
    private readonly UAuthPasswordUserSimilarityChecker _checker = new();

    private static PasswordPolicyContext Context(string password, string? userName = null,
        params string[] identifiers) => new()
        {
            Password = password,
            UserName = userName,
            UserIdentifiers = identifiers
        };

    // ============================================================
    // BASIC
    // ============================================================

    [Theory]
    [InlineData("Alice", "alice")]
    [InlineData("ALICE", "alice")]
    [InlineData("alice", "ALICE")]
    public void Basic_RejectsExactUsernameIgnoringCase(string password, string userName)
    {
        Assert.True(_checker.IsSimilar(Context(password, userName), PasswordPolicyLevel.Basic));
    }

    [Fact]
    public void Basic_RejectsExactIdentifier()
    {
        Assert.True(_checker.IsSimilar(
            Context("alice@example.com", null, "alice@example.com"),
            PasswordPolicyLevel.Basic));
    }

    [Fact]
    public void Basic_AllowsUsernameEmbeddedInPassword()
    {
        Assert.False(_checker.IsSimilar(
            Context("SecureAlice123!", "alice"),
            PasswordPolicyLevel.Basic));
    }

    [Fact]
    public void Basic_AllowsDifferentPassword()
    {
        Assert.False(_checker.IsSimilar(
            Context("CompletelyDifferent123!", "alice"),
            PasswordPolicyLevel.Basic));
    }

    // ============================================================
    // STANDARD
    // ============================================================

    [Theory]
    [InlineData("alice", "SecureAlice123!")]
    [InlineData("owner", "Owner-test-password-8!")]
    [InlineData("member", "Member-test-password-8!")]
    public void Standard_RejectsEmbeddedUsername(
        string userName,
        string password)
    {
        Assert.True(_checker.IsSimilar(
            Context(password, userName),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void Standard_RejectsEmbeddedEmailLocalPart()
    {
        Assert.True(_checker.IsSimilar(
            Context("SecureAlice123!", null, "alice@example.com"),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void Standard_RejectsEmbeddedSecondaryIdentifier()
    {
        Assert.True(_checker.IsSimilar(
            Context(
                "SecureMember123!",
                "alice",
                "alice@example.com",
                "member@example.com"),
            PasswordPolicyLevel.Standard));
    }

    [Theory]
    [InlineData("abc", "SecureAbc123!")]
    [InlineData("ab", "SecureAb123!")]
    [InlineData("a", "SecureA123!")]
    public void Standard_DoesNotRejectShortEmbeddedUsername(
        string userName,
        string password)
    {
        Assert.False(_checker.IsSimilar(
            Context(password, userName),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void Standard_RejectsFourCharacterUsername()
    {
        Assert.True(_checker.IsSimilar(
            Context("SecureJohn123!", "john"),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void Standard_DoesNotApplyStrictNormalization()
    {
        Assert.False(_checker.IsSimilar(
            Context("Secure4l1ce123!", "alice"),
            PasswordPolicyLevel.Standard));
    }

    // ============================================================
    // STRICT
    // ============================================================

    [Theory]
    [InlineData("alice", "Secure4l1ce123!")]
    [InlineData("password", "SecureP@ssw0rd123!")]
    [InlineData("master", "SecureM4$ter123!")]
    [InlineData("test", "Secure7e$7123!")]
    public void Strict_RejectsNormalizedUsername(
        string userName,
        string password)
    {
        Assert.True(_checker.IsSimilar(
            Context(password, userName),
            PasswordPolicyLevel.Strict));
    }

    [Theory]
    [InlineData("john.doe", "SecureJohnDoe123!")]
    [InlineData("john_doe", "SecureJohnDoe123!")]
    [InlineData("john-doe", "SecureJohnDoe123!")]
    public void Strict_IgnoresSeparators(
        string userName,
        string password)
    {
        Assert.True(_checker.IsSimilar(
            Context(password, userName),
            PasswordPolicyLevel.Strict));
    }

    [Fact]
    public void Strict_NormalizesEmailLocalPart()
    {
        Assert.True(_checker.IsSimilar(
            Context(
                "SecureJ0hnD0e123!",
                null,
                "john.doe@example.com"),
            PasswordPolicyLevel.Strict));
    }

    [Fact]
    public void Strict_DoesNotMatchUnrelatedPassword()
    {
        Assert.False(_checker.IsSimilar(
            Context("UnrelatedSecure123!", "alice"),
            PasswordPolicyLevel.Strict));
    }

    [Fact]
    public void Strict_DoesNotMatchShortNormalizedCandidate()
    {
        Assert.False(_checker.IsSimilar(
            Context("Secure4l1123!", "ali"),
            PasswordPolicyLevel.Strict));
    }

    // ============================================================
    // INPUT / IDENTIFIERS
    // ============================================================

    [Theory]
    [InlineData(PasswordPolicyLevel.Basic)]
    [InlineData(PasswordPolicyLevel.Standard)]
    [InlineData(PasswordPolicyLevel.Strict)]
    public void EmptyPassword_IsNotSimilar(
        PasswordPolicyLevel level)
    {
        Assert.False(_checker.IsSimilar(
            Context("", "alice"),
            level));
    }

    [Fact]
    public void MissingIdentifiers_ReturnsFalse()
    {
        Assert.False(_checker.IsSimilar(
            Context("SecurePassword123!"),
            PasswordPolicyLevel.Strict));
    }

    [Fact]
    public void WhitespaceIdentifiers_AreIgnored()
    {
        Assert.False(_checker.IsSimilar(
            Context(
                "SecurePassword123!",
                "   ",
                " ",
                "\t"),
            PasswordPolicyLevel.Strict));
    }

    [Fact]
    public void Identifiers_AreTrimmed()
    {
        Assert.True(_checker.IsSimilar(
            Context("SecureAlice123!", "  alice  "),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void DuplicateIdentifiers_DoNotChangeResult()
    {
        Assert.True(_checker.IsSimilar(
            Context(
                "SecureAlice123!",
                null,
                "alice",
                "ALICE",
                "alice"),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void IdentifierWithoutEmailLocalPart_IsHandled()
    {
        Assert.False(_checker.IsSimilar(
            Context("SecureAlice123!", null, "@example.com"),
            PasswordPolicyLevel.Standard));
    }

    [Fact]
    public void Disabled_AlwaysReturnsFalse()
    {
        Assert.False(_checker.IsSimilar(
            Context("alice", "alice"),
            PasswordPolicyLevel.Disabled));
    }

    [Fact]
    public void NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _checker.IsSimilar(
                null!,
                PasswordPolicyLevel.Standard));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void InvalidLevel_Throws(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _checker.IsSimilar(
                Context("SecurePassword123!", "alice"),
                (PasswordPolicyLevel)level));
    }
}
