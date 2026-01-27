namespace CodeBeam.UltimateAuth.Policies
{
    public interface IConditionalPolicyBuilder
    {
        IPolicyScopeBuilder Then();
        IPolicyScopeBuilder Otherwise();
    }
}
