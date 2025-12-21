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

        private async Task LoginAsync()
        {

            try
            {
                //var result = await FlowService.LoginAsync(new LoginRequest
                //{
                //    Identifier = _username!,
                //    Secret = _password!
                //});
                var client = Http.CreateClient();
                var result = await client.PostAsJsonAsync(
                    "https://localhost:7213/auth/login",
                    new LoginRequest
                    {
                        Identifier = _username!,
                        Secret = _password!
                    });


                if (!result.IsSuccessStatusCode)
                {
                    Snackbar.Add("Login failed.", Severity.Info);
                    return;
                }

                Snackbar.Add("Successfully logged in!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.ToString(), Severity.Error);
            }
        }
    }
}
