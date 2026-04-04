using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public static class UserIdentifierMapper
{
    public static UserIdentifierInfo ToDto(UserIdentifier record)
        => new()
        {
            Id = record.Id,
            Type = record.Type,
            Value = record.Value,
            NormalizedValue = record.NormalizedValue,
            IsPrimary = record.IsPrimary,
            IsVerified = record.IsVerified,
            CreatedAt = record.CreatedAt,
            VerifiedAt = record.VerifiedAt,
            Version = record.Version
        };
}
