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
            var adapted = _adapter.Adapt(template, ctx);

            return new EffectiveAuthResponse(
                adapted.SessionIdDelivery,
                adapted.AccessTokenDelivery,
                adapted.RefreshTokenDelivery,

                new EffectiveLoginRedirectResponse(
                    adapted.Login.RedirectEnabled,
                    adapted.Login.SuccessRedirect,
                    adapted.Login.FailureRedirect,
                    adapted.Login.FailureQueryKey,
                    adapted.Login.CodeQueryKey,
                    adapted.Login.FailureCodes
                ),

                new EffectiveLogoutRedirectResponse(
                    adapted.Logout.RedirectEnabled,
                    adapted.Logout.RedirectUrl,
                    adapted.Logout.AllowReturnUrlOverride
                )
            );
        }
    }
}
