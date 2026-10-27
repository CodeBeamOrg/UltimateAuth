namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabPartialProvisioningException : Exception
{
    public string CreatedUserKey { get; }

    public LabPartialProvisioningException(string createdUserKey, Exception innerException)
        : base("The user was created but sandbox provisioning was incomplete.", innerException)
    {
        CreatedUserKey = createdUserKey;
    }
}
