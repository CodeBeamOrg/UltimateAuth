using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class PkceService : IPkceService
{
    private readonly IAuthStore _authStore;
    private readonly IPkceAuthorizationValidator _validator;
    private readonly IUAuthFlowService _flow;
    private readonly IClock _clock;
    private readonly UAuthServerOptions _options;

    public PkceService(IAuthStore authStore, IPkceAuthorizationValidator validator, IUAuthFlowService flow, IClock clock, IOptions<UAuthServerOptions> options)
    {
        _authStore = authStore;
        _validator = validator;
        _flow = flow;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<PkceAuthorizeResponse> AuthorizeAsync(PkceAuthorizeCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.CodeChallenge))
            throw new InvalidOperationException("code_challenge is required.");

        if (!string.Equals(command.ChallengeMethod, "S256", StringComparison.Ordinal))
            throw new InvalidOperationException("Only S256 supported.");

        var authorizationCode = AuthArtifactKey.New();

        var snapshot = new PkceContextSnapshot(
            clientProfile: command.ClientProfile,
            tenant: command.Tenant,
            redirectUri: command.RedirectUri,
            device: command.Device
        );

        var expiresAt = _clock.UtcNow.AddSeconds(_options.Pkce.AuthorizationCodeLifetimeSeconds);

        var artifact = new PkceAuthorizationArtifact(
            authorizationCode: authorizationCode,
            codeChallenge: command.CodeChallenge,
            challengeMethod: PkceChallengeMethod.S256,
            expiresAt: expiresAt,
            context: snapshot
        );

        await _authStore.StoreAsync(authorizationCode, artifact, ct);

        return new PkceAuthorizeResponse
        {
            AuthorizationCode = authorizationCode.Value,
            ExpiresIn = _options.Pkce.AuthorizationCodeLifetimeSeconds
        };
    }

    public async Task<PkceCompleteResult> CompleteAsync(AuthFlowContext auth, PkceCompleteRequest request, CancellationToken ct = default)
    {
        var key = new AuthArtifactKey(request.AuthorizationCode);

        var artifact = await _authStore.ConsumeAsync(key, ct) as PkceAuthorizationArtifact;

        if (artifact is null)
        {
            return new PkceCompleteResult
            {
                InvalidPkce = true
            };
        }

        var validation = _validator.Validate(
            artifact,
            request.CodeVerifier,
            new PkceContextSnapshot(
                clientProfile: artifact.Context.ClientProfile,
                tenant: artifact.Context.Tenant,
                redirectUri: artifact.Context.RedirectUri,
                device: artifact.Context.Device),
            _clock.UtcNow);

        if (!validation.Success)
        {
            artifact.RegisterAttempt();

            return new PkceCompleteResult
            {
                Success = false,
                FailureReason = AuthFailureReason.InvalidCredentials
            };
        }

        var loginRequest = new LoginRequest
        {
            Identifier = request.Identifier!,
            Secret = request.Secret!,
            RequestTokens = auth.AllowsTokenIssuance
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = artifact.Context.ClientProfile,
            Device = artifact.Context.Device
        };

        var result = await _flow.LoginAsync(auth, execution, loginRequest, ct);

        return new PkceCompleteResult
        {
            Success = result.IsSuccess,
            FailureReason = result.FailureReason,
            LoginResult = result
        };
    }

    public async Task<HubCredentials> RefreshAsync(HubFlowArtifact hub, CancellationToken ct = default)
    {
        if (hub.Payload.TryGet<string>("authorization_code", out var oldCode) && !string.IsNullOrWhiteSpace(oldCode))
        {
            await _authStore.ConsumeAsync(new AuthArtifactKey(oldCode), ct);
        }

        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);
        var device = hub.Device;
        var authorizationCode = AuthArtifactKey.New();

        var snapshot = new PkceContextSnapshot(
            clientProfile: hub.ClientProfile,
            tenant: hub.Tenant,
            redirectUri: hub.ReturnUrl,
            device: device
        );

        var expiresAt = _clock.UtcNow.AddSeconds(_options.Pkce.AuthorizationCodeLifetimeSeconds);

        var artifact = new PkceAuthorizationArtifact(
            authorizationCode,
            challenge,
            PkceChallengeMethod.S256,
            expiresAt,
            snapshot
        );

        await _authStore.StoreAsync(authorizationCode, artifact, ct);

        return new HubCredentials
        {
            AuthorizationCode = authorizationCode.Value,
            CodeVerifier = verifier
        };
    }

    private static string CreateVerifier()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CreateChallenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier));

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
