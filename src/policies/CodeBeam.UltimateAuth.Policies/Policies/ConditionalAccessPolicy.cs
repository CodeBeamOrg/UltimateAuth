using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies
{
    internal sealed class ConditionalAccessPolicy : IAccessPolicy
    {
        private readonly Func<AccessContext, bool> _condition;
        private readonly bool _expected;
        private readonly IAccessPolicy _inner;

        public ConditionalAccessPolicy(Func<AccessContext, bool> condition, bool expected, IAccessPolicy inner)
        {
            _condition = condition;
            _expected = expected;
            _inner = inner;
        }

        public bool AppliesTo(AccessContext context) => _condition(context) == _expected;

        public AccessDecision Decide(AccessContext context) => _inner.Decide(context);
    }
}
