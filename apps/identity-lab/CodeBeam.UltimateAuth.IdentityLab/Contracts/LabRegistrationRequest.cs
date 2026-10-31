namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record LabRegistrationRequest(
    string Username,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    DateOnly? BirthDate = null);
