using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UAuth.Sample.IntWasm.Client.ResourceApi;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UAuth.Sample.IntWasm.Client.Components.Dialogs;

public partial class ResourceApiDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    private List<SampleProduct> _products = new List<SampleProduct>();
    private string? _newName = null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _products = (await Api.GetAllAsync()).Value ?? new();
            StateHasChanged();
        }
    }

    private async Task<DataGridEditFormAction> CommittedItemChanges(SampleProduct item)
    {
        var result = await Api.UpdateAsync(item.Id, item);

        if (result.IsSuccess)
        {
            Snackbar.Add("Product updated successfully", Severity.Success);
        }
        else
        {
            Snackbar.Add(result?.ErrorText ?? "Failed to update product.", Severity.Error);
        }

        return DataGridEditFormAction.Close;
    }

    private async Task GetProducts()
    {
        var result = await Api.GetAllAsync();

        if (result.IsSuccess)
        {
            _products = result.Value ?? new();
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Process failed.", Severity.Error);
        }
    }

    private async Task CreateProduct()
    {
        var product = new SampleProduct
        {
            Name = _newName
        };

        var result = await Api.CreateAsync(product);

        if (result.IsSuccess)
        {
            Snackbar.Add("New product created.");
            _products = (await Api.GetAllAsync()).Value ?? new();
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Process failed.", Severity.Error);
        }
    }

    private async Task DeleteProduct(int id)
    {
        var result = await Api.DeleteAsync(id);

        if (result.IsSuccess)
        {
            Snackbar.Add("Product deleted succesfully.", Severity.Success);
            _products = (await Api.GetAllAsync()).Value ?? new();
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Process failed.", Severity.Error);
        }
    }
}
