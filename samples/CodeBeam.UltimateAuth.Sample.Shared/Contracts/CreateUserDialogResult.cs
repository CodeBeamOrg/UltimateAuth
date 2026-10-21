namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public sealed record CreateUserDialogResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static CreateUserDialogResult Success() => new(true);

    public static CreateUserDialogResult Failure(string message) => new(false, message);
}
