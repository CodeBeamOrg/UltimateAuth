namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultAuthResponseResolver : IAuthResponseResolver
    {
        private readonly ModeAuthResponseTemplateResolver _template;
        private readonly ClientProfileAuthResponseAdapter _adapter;

        public DefaultAuthResponseResolver(ModeAuthResponseTemplateResolver template, ClientProfileAuthResponseAdapter adapter)
        {
            _template = template;
            _adapter = adapter;
        }

        public EffectiveAuthResponse Resolve(AuthFlowContext ctx)
        {
            var template = _template.Resolve(ctx.EffectiveMode, ctx.FlowType);
            return _adapter.Adapt(template, ctx);
        }
    }
}
