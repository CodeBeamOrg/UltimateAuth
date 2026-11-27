using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client;

public sealed record UAuthTokenSnapshot(
    AccessToken? Token,
    long Generation,
    long Epoch);
