using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeBeam.UltimateAuth.Sample.ResourceApi;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    //[Authorize(Policy = UAuthActions.Authorization.Roles.GetAdmin)] // You can use UAuthActions as permission in ASP.NET Core policy.
    public IActionResult GetAll()
    {
        return Ok(ProductStore.Items);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    //[Authorize(Policy = UAuthActions.Authorization.Roles.GetAdmin)]
    public IActionResult Get(int id)
    {
        var item = ProductStore.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    //[Authorize(Policy = UAuthActions.Authorization.Roles.GetAdmin)]
    public IActionResult Create(SampleProduct product)
    {
        var nextId = ProductStore.Items.Any()
            ? ProductStore.Items.Max(x => x.Id) + 1
            : 1;

        product.Id = nextId;
        ProductStore.Items.Add(product);

        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    //[Authorize(Policy = UAuthActions.Authorization.Roles.GetAdmin)]
    public IActionResult Update(int id, SampleProduct product)
    {
        var item = ProductStore.Items.FirstOrDefault(x => x.Id == id);

        if (item == null)
        {
            throw new UAuthNotFoundException("No product found.");
        }

        item.Name = product.Name;
        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    //[Authorize(Policy = UAuthActions.Authorization.Roles.GetAdmin)]
    public IActionResult Delete(int id)
    {
        var item = ProductStore.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();

        ProductStore.Items.Remove(item);
        return Ok(item);
    }
}
