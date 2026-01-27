using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public static class UserIdentifierMapper
    {
        public static UserIdentifierDto ToDto(UserIdentifier record)
            => new()
            {
                Type = record.Type,
                Value = record.Value,
                IsPrimary = record.IsPrimary,
                IsVerified = record.IsVerified,
                CreatedAt = record.CreatedAt,
                VerifiedAt = record.VerifiedAt
            };
    }
}
