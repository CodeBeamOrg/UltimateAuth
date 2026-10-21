using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

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

    private bool _isLoading = true;
    private bool _isSaving;
    private string? _loadError;

    private readonly IReadOnlyList<TimeZoneInfo> _timeZones =
        TimeZoneInfo.GetSystemTimeZones();

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public UserKey? UserKey { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    [Parameter]
    public string? PreviewDisplayName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (PreviewMode)
        {
            LoadPreviewData();
            _isLoading = false;
            return;
        }

        try
        {
            UAuthResult<UserView> result = UserKey is null
                ? await UAuthClient.Users.GetMeAsync()
                : await UAuthClient.Users.GetUserAsync(UserKey.Value);

            if (!result.IsSuccess || result.Value is null)
            {
                _loadError = result.ErrorText
                    ?? "Unable to load profile information.";
                return;
            }

            var profile = result.Value;

            _firstName = profile.FirstName;
            _lastName = profile.LastName;
            _displayName = profile.DisplayName;
            _birthDate = profile.BirthDate?
                .ToDateTime(TimeOnly.MinValue);
            _gender = profile.Gender;
            _bio = profile.Bio;
            _language = profile.Language;
            _timeZone = profile.TimeZone;
            _culture = profile.Culture;
        }
        catch (Exception)
        {
            _loadError = "An unexpected error occurred while loading the profile.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadPreviewData()
    {
        _displayName = PreviewDisplayName ?? "Alex Morgan";
        _firstName = "Alex";
        _lastName = "Morgan";
        _birthDate = null;
        _gender = null;
        _bio = "Exploring identity management with UltimateAuth.";
        _language = "en";
        _culture = "en-US";
        _timeZone = "UTC";
    }

    private async Task SaveAsync()
    {
        if (PreviewMode || _isLoading || _isSaving || _loadError is not null)
            return;

        if (_form is null)
            return;

        await _form.ValidateAsync();

        if (!_form.IsValid)
            return;

        var request = new UpdateProfileRequest
        {
            FirstName = _firstName?.Trim(),
            LastName = _lastName?.Trim(),
            DisplayName = _displayName?.Trim(),
            BirthDate = _birthDate.HasValue
                ? DateOnly.FromDateTime(_birthDate.Value)
                : null,
            Gender = _gender?.Trim(),
            Bio = _bio,
            Language = _language?.Trim(),
            TimeZone = _timeZone,
            Culture = _culture?.Trim()
        };

        _isSaving = true;

        try
        {
            UAuthResult result = UserKey is null
                ? await UAuthClient.Users.UpdateMyProfileAsync(request)
                : await UAuthClient.Users.UpdateUserProfileAsync(
                    UserKey.Value, request);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to update profile.",
                    Severity.Error);
                return;
            }

            Snackbar.Add("Profile updated successfully.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception)
        {
            Snackbar.Add(
                "An unexpected error occurred while saving the profile.",
                Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void Cancel()
    {
        if (!_isSaving)
            MudDialog.Cancel();
    }
}
