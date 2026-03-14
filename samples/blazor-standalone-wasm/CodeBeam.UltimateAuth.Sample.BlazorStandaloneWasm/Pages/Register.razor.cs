using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Users.Contracts;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Pages;

public partial class Register
{
    private string? _username;
    private string? _password;
    private string? _passwordCheck;
    private string? _email;
    private UAuthClientProductInfo? _productInfo;
    private MudForm _form = null!;

    protected override async Task OnInitializedAsync()
    {
        _productInfo = ClientProductInfoProvider.Get();
    }

    private async Task HandleRegisterAsync()
    {
        await _form.Validate();
        
        if (!_form.IsValid)
            return;
        
        var request = new CreateUserRequest
        {
            UserName = _username,
            Password = _password,
            Email = _email,
        };

        var result = await UAuthClient.Users.CreateAsync(request);
        if (result.IsSuccess)
        {
            Snackbar.Add("User created succesfully.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.GetErrorText ?? "Failed to create user.", Severity.Error);
        }
    }
}
