using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.ResourceApi;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class ResourceApiDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    List<Product> _products = new List<Product>();

    private async Task GetProducts()
    {
        _products = await Api.GetAllAsync();
    }
}
