using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

[ApiController]
[Route("__tests/resource")]
public sealed class TestResourceController : ControllerBase
{
    public const string ProductsRead = "products.read.self";
    public const string ProductsUpdate = "products.update.admin";

    [HttpGet("anonymous")]
    [AllowAnonymous]
    public IActionResult Anonymous()
    {
        return Ok();
    }

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult Authenticated()
    {
        return Ok();
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return Ok();
    }

    [HttpGet("identity")]
    [Authorize]
    public IActionResult Identity()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Roles = User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .ToArray(),
            Permissions = User.FindAll("uauth:permission")
                .Select(x => x.Value)
                .ToArray()
        });
    }

    [HttpGet("products/read")]
    [Authorize(Policy = ProductsRead)]
    public IActionResult ReadProducts()
    {
        return Ok();
    }

    [HttpPut("products/{id:int}")]
    [Authorize(Policy = ProductsUpdate)]
    public IActionResult UpdateProduct(int id)
    {
        return Ok(new { Id = id });
    }
}
