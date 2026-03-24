using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeBeam.UltimateAuth.Sample.ResourceApi;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetAll()
    {
        return Ok(ProductStore.Items);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "products.read")]
    public IActionResult Get(int id)
    {
        var item = ProductStore.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "products.create")]
    public IActionResult Create(Product product)
    {
        product.Id = ProductStore.Items.Count + 1;
        ProductStore.Items.Add(product);

        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "products.delete")]
    public IActionResult Delete(int id)
    {
        var item = ProductStore.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();

        ProductStore.Items.Remove(item);
        return Ok();
    }
}
