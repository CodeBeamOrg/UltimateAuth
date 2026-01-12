using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultUAuthHubContextInitializer : IUAuthHubContextInitializer
    {
        private readonly NavigationManager _nav;
        private readonly IAuthStore _authStore;
        private readonly UAuthHubContextAccessor _accessor;
        private readonly IClock _clock;

        private bool _initialized;

        public DefaultUAuthHubContextInitializer(NavigationManager nav, IAuthStore authStore, UAuthHubContextAccessor accessor, IClock clock)
        {
            _nav = nav;
            _authStore = authStore;
            _accessor = accessor;
            _clock = clock;
        }

        public async Task EnsureInitializedAsync()
        {
            if (_initialized || _accessor.Current != null)
                return;

            var uri = _nav.ToAbsoluteUri(_nav.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (!query.TryGetValue("hub", out var hubKey))
                return;

            var artifact = await _authStore.GetAsync(new AuthArtifactKey(hubKey!));

            if (artifact is not HubFlowArtifact flow)
                return;

            var context = new UAuthHubContext(
                hubSessionId: flow.HubSessionId,
                flowType: flow.FlowType,
                clientProfile: flow.ClientProfile,
                tenantId: flow.TenantId,
                returnUrl: flow.ReturnUrl,
                payload: flow.Payload,
                createdAt: _clock.UtcNow);

            _accessor.Initialize(context);
            _initialized = true;

            //_nav.NavigateTo(uri.GetLeftPart(UriPartial.Path), replace: true);
            _nav.NavigateTo(_nav.Uri);
        }
    }
}
