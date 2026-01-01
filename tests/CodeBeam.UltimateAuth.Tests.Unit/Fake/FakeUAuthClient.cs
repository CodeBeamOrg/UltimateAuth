using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit
{
    internal sealed class FakeUAuthClient : IUAuthClient
    {
        private readonly Queue<RefreshOutcome> _outcomes;

        public FakeUAuthClient(params RefreshOutcome[] outcomes)
        {
            _outcomes = new Queue<RefreshOutcome>(outcomes);
        }

        public Task LoginAsync(LoginRequest request)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync()
        {
            throw new NotImplementedException();
        }

        public Task ReauthAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RefreshResult> RefreshAsync(bool isAuto = false)
        {
            var outcome = _outcomes.Count > 0
                ? _outcomes.Dequeue()
                : RefreshOutcome.None;

            return Task.FromResult(new RefreshResult
            {
                Ok = true,
                Outcome = outcome
            });
        }

        public Task<AuthValidationResult> ValidateAsync()
        {
            throw new NotImplementedException();
        }
    }

}
