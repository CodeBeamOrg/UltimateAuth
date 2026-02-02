namespace CodeBeam.UltimateAuth.Policies;

public interface IPolicyBuilder
{
    IPolicyScopeBuilder For(string actionPrefix);
    IPolicyScopeBuilder Global();
}
