namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public static class LabDemoPermissions
{
    public const string ProductsRead = "demo.products.read";
    public const string ProductsCreate = "demo.products.create";
    public const string ProductsUpdate = "demo.products.update";
    public const string ProductsDelete = "demo.products.delete";
    public const string OrdersCreate = "demo.orders.create";

    public static readonly IReadOnlyList<string> Manager =
    [
        ProductsRead,
        ProductsCreate,
        ProductsUpdate,
        ProductsDelete
    ];

    public static readonly IReadOnlyList<string> Customer =
    [
        ProductsRead,
        OrdersCreate
    ];
}
