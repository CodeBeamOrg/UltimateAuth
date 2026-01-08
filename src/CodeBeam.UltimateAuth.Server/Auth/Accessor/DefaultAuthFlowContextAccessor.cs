namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultAuthFlowContextAccessor : IAuthFlowContextAccessor
    {
        private static readonly AsyncLocal<AuthFlowContext?> _current = new();

        public AuthFlowContext Current => _current.Value ?? throw new InvalidOperationException("AuthFlowContext is not available for this request.");

        internal void Set(AuthFlowContext context)
        {
            _current.Value = context;
        }
    }
}
