using CodeBeam.UltimateAuth.Docs.Wasm;
using CodeBeam.UltimateAuth.Docs.Wasm.Client.Pages;
using CodeBeam.UltimateAuth.Docs.Wasm.Components;
using MudBlazor.Services;
using MudExtensions.Services;
using System.Text.Json;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddHttpClient();
builder.Services.AddScoped<DocsPageState>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CodeBeam.UltimateAuth.Docs.Wasm.Client._Imports).Assembly);

app.MapGet("/sitemap.xml", async (IWebHostEnvironment env) =>
{
    var file = env.WebRootFileProvider.GetFileInfo("docs/docs-index.json");

    if (!file.Exists)
        return Results.NotFound();

    using var stream = file.CreateReadStream();

    var pages = await JsonSerializer.DeserializeAsync<List<DocIndexEntry>>(
        stream,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

    const string baseUrl = "https://ultimateauth.com";

    XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    var urls = new List<string>
    {
        $"{baseUrl}/",
        $"{baseUrl}/docs"
    };

    urls.AddRange(pages.Where(x => !string.IsNullOrWhiteSpace(x.Slug)).Select(x => $"{baseUrl}/docs/{x.Slug.Trim('/')}"));

    var document = new XDocument(
        new XDeclaration("1.0", "UTF-8", null),
        new XElement(
            ns + "urlset",
            urls.Distinct().Select(url =>
                new XElement(
                    ns + "url",
                    new XElement(ns + "loc", url)
                )
            )
        )
    );

    return Results.Text(document.ToString(), "application/xml");
});

app.Run();
