using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public interface IPasswordUserSimilarityChecker
{
    bool IsSimilar(PasswordPolicyContext context, PasswordPolicyLevel level);
}
