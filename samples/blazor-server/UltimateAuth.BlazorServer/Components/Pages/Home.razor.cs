using CodeBeam.UltimateAuth.Client.Components;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using MudBlazor;

namespace UltimateAuth.BlazorServer.Components.Pages
{
    public partial class Home
    {
        private string? _username;
        private string? _password;

        private UALoginForm _form;

        private async Task HandleLogin()
        {
            await _form.SubmitAsync();
        }

    }
}
