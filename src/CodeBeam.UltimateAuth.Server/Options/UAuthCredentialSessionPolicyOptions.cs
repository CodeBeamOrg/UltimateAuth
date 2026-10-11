using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCredentialSessionPolicyOptions
{
    /// <summary>
    /// Session behavior when a user changes their own credential.
    /// By default, the current session chain is preserved.
    /// </summary>
    public CredentialSessionBehavior OnSelfChange { get; set; } = CredentialSessionBehavior.PreserveCurrent;

    /// <summary>
    /// Session behavior when an administrator changes a user's credential.
    /// By default, all session chains are revoked.
    /// </summary>
    public CredentialSessionBehavior OnAdminChange { get; set; } = CredentialSessionBehavior.RevokeAll;

    /// <summary>
    /// Session behavior after successful credential recovery.
    /// By default, all session chains are revoked.
    /// <br /> <br />
    /// Preserving the current session don't work well with recovery flows, because the current session is usually the one that was lost and is being recovered.
    /// In this case, RevokeAll option is using instead of PreserveCurrent automatically.
    /// </summary>
    public CredentialSessionBehavior OnRecovery { get; set; } = CredentialSessionBehavior.RevokeAll;

    internal UAuthCredentialSessionPolicyOptions Clone()
    {
        return new UAuthCredentialSessionPolicyOptions
        {
            OnSelfChange = OnSelfChange,
            OnAdminChange = OnAdminChange,
            OnRecovery = OnRecovery
        };
    }
}
