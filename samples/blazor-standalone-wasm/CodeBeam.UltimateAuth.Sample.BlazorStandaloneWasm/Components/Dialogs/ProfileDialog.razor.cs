using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class ProfileDialog
{
    private MudForm? _form;
    private string? _firstName;
    private string? _lastName;
    private string? _displayName;
    private DateTime? _birthDate;
    private string? _gender;
    private string? _bio;
    private string? _language;
    private string? _timeZone;
    private string? _culture;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey? UserKey { get; set; }

    protected override async Task OnInitializedAsync()
    {
        UAuthResult<UserView> result;

        if (UserKey is null)
        {
            result = await UAuthClient.Users.GetMeAsync();
        }
        else
        {
            result = await UAuthClient.Users.GetProfileAsync(UserKey.Value);
        }

        if (result.IsSuccess && result.Value is not null)
        {
            var p = result.Value;

            _firstName = p.FirstName;
            _lastName = p.LastName;
            _displayName = p.DisplayName;

            _gender = p.Gender;
            _birthDate = p.BirthDate?.ToDateTime(TimeOnly.MinValue);
            _bio = p.Bio;

            _language = p.Language;
            _timeZone = p.TimeZone;
            _culture = p.Culture;
        }
    }

    private async Task SaveAsync()
    {
        if (AuthState is null || AuthState.Identity is null)
        {
            Snackbar.Add("No AuthState found.", Severity.Error);
            return;
        }

        if (_form is not null)
        {
            await _form.Validate();
            if (!_form.IsValid)
                return;
        }

        var request = new UpdateProfileRequest
        {
            FirstName = _firstName,
            LastName = _lastName,
            DisplayName = _displayName,
            BirthDate = _birthDate.HasValue ? DateOnly.FromDateTime(_birthDate.Value) : null,
            Gender = _gender,
            Bio = _bio,
            Language = _language,
            TimeZone = _timeZone,
            Culture = _culture
        };

        UAuthResult result;

        if (UserKey is null)
        {
            result = await UAuthClient.Users.UpdateMeAsync(request);
        }
        else
        {
            result = await UAuthClient.Users.UpdateProfileAsync(UserKey.Value, request);
        }

        if (result.IsSuccess)
        {
            Snackbar.Add("Profile updated", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.GetErrorText ?? "Failed to update profile", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
