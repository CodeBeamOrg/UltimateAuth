using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Sample.Shared.ResourceApi;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;

public partial class ResourceApiDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState? AuthState { get; set; }

    [Parameter]
    public bool PreviewMode { get; set; }

    private List<SampleProduct> _products = [];
    private string? _newName;

    private SampleProduct? _editingProduct;
    private string? _editingName;

    private bool _isLoading;
    private bool _isBusy;
    private string? _loadError;

    private readonly List<PreviewProduct> _previewProducts =
    [
        new(1, "Sample Laptop"),
        new(2, "Wireless Keyboard"),
        new(3, "USB-C Hub"),
        new(4, "External Monitor")
    ];

    protected override async Task OnInitializedAsync()
    {
        if (PreviewMode)
            return;

        await GetProductsAsync();
    }

    private async Task GetProductsAsync()
    {
        if (PreviewMode || _isBusy)
            return;

        _isLoading = true;
        _loadError = null;

        try
        {
            var result = await Api.GetAllAsync();

            if (!result.IsSuccess)
            {
                _loadError = result.ErrorText ??
                    "Failed to load products.";
                return;
            }

            _products = result.Value ?? [];
        }
        catch (Exception)
        {
            _loadError = "Unable to load resource data.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BeginEdit(SampleProduct product)
    {
        if (PreviewMode || _isBusy)
            return;

        _editingProduct = product;
        _editingName = product.Name;
    }

    private void CancelEdit()
    {
        _editingProduct = null;
        _editingName = null;
    }

    private async Task SaveProductAsync()
    {
        if (PreviewMode || _isBusy || _editingProduct is null ||
            string.IsNullOrWhiteSpace(_editingName))
            return;

        _isBusy = true;
        bool success = false;

        try
        {
            var product = new SampleProduct
            {
                Id = _editingProduct.Id,
                Name = _editingName.Trim()
            };

            var result = await Api.UpdateAsync(product.Id, product);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to update product.",
                    Severity.Error);
                return;
            }

            success = true;
            Snackbar.Add(
                "Product updated successfully.",
                Severity.Success);
            CancelEdit();
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to update product.", Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }

        if (success)
            await GetProductsAsync();
    }

    private async Task CreateProductAsync()
    {
        if (PreviewMode || _isBusy ||
            string.IsNullOrWhiteSpace(_newName))
            return;

        _isBusy = true;
        bool success = false;

        try
        {
            var product = new SampleProduct
            {
                Name = _newName.Trim()
            };

            var result = await Api.CreateAsync(product);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to create product.",
                    Severity.Error);
                return;
            }

            success = true;
            _newName = null;

            Snackbar.Add(
                "Product created successfully.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to create product.", Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }

        if (success)
            await GetProductsAsync();
    }

    private async Task DeleteProductAsync(int id)
    {
        if (PreviewMode || _isBusy)
            return;

        _isBusy = true;
        bool success = false;

        try
        {
            var result = await Api.DeleteAsync(id);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.ErrorText ?? "Failed to delete product.",
                    Severity.Error);
                return;
            }

            success = true;
            Snackbar.Add(
                "Product deleted successfully.",
                Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add("Unable to delete product.", Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }

        if (success)
            await GetProductsAsync();
    }

    private void Close()
    {
        if (!_isBusy)
            MudDialog.Close();
    }

    private sealed record PreviewProduct(int Id, string Name);
}
