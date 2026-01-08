using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    internal sealed class DefaultUAuthStateManager : IUAuthStateManager
    {
        private readonly IUAuthClient _client;
        private readonly IClock _clock;

        public UAuthState State { get; } = UAuthState.Anonymous();

        public DefaultUAuthStateManager(IUAuthClient client, IClock clock)
        {
            _client = client;
            _clock = clock;
        }

        public async Task EnsureAsync(CancellationToken ct = default)
        {
            //if (!State.IsAuthenticated)
            //    return;

            //if (!State.IsStale)
            //    return;

            if (State.IsAuthenticated && !State.IsStale)
                return;

            var result = await _client.ValidateAsync();

            if (!result.IsValid)
            {
                State.Clear();
                return;
            }

            State.ApplySnapshot(result.Snapshot, _clock.UtcNow);
        }

        public async Task OnLoginAsync(CancellationToken ct = default)
        {
            var result = await _client.ValidateAsync();

            if (!result.IsValid || result.Snapshot is null)
            {
                State.Clear();
                return;
            }

            var now = _clock.UtcNow;

            State.ApplySnapshot(
                result.Snapshot,
                validatedAt: now);
        }


        public Task OnLogoutAsync()
        {
            State.Clear();
            return Task.CompletedTask;
        }

        public void MarkStale()
        {
            State.MarkStale();
        }
    }
}
