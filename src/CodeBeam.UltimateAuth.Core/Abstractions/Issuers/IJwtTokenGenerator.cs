using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Low-level JWT creation abstraction.
    /// Can be replaced for asymmetric keys, external KMS, etc.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        string CreateToken(UAuthJwtTokenDescriptor descriptor);
    }
}
