using HtmlAgilityPack;
using Markdig;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

Console.WriteLine("=== DocsBuilder START ===");

var solutionRootDir = TryGetSolutionDir(args) ?? FindSolutionRootFallback();

Console.WriteLine($"SolutionRoot: {solutionRootDir}");

var sourceDocsDir = Path.Combine(solutionRootDir, "docs", "content");
var groupsOrderPath = Path.Combine(sourceDocsDir, "_groups.json");

Dictionary<string, int> groupOrders = new(StringComparer.OrdinalIgnoreCase);

if (File.Exists(groupsOrderPath))
{
    var groupsOrderJson = await File.ReadAllTextAsync(groupsOrderPath);
    groupOrders = JsonSerializer.Deserialize<Dictionary<string, int>>(groupsOrderJson)
        ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

var websiteDocsOutputDir = Path.Combine(
    solutionRootDir,
    "docs",
    "website",
    "CodeBeam.UltimateAuth.Docs.Wasm",
    "CodeBeam.UltimateAuth.Docs.Wasm.Client",
    "wwwroot",
    "docs"
);

if (!Directory.Exists(sourceDocsDir))
{
    Console.Error.WriteLine($"Docs source directory not found: {sourceDocsDir}");
    return;
}

Directory.CreateDirectory(websiteDocsOutputDir);

var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseAutoIdentifiers()
    .Build();

var markdownFiles = Directory.GetFiles(sourceDocsDir, "*.md", SearchOption.AllDirectories);

var allDocs = new List<DocIndexItem>();

foreach (var file in markdownFiles)
{
    var relativePath = Path.GetRelativePath(sourceDocsDir, file);
    var outputRelativePath = Path.ChangeExtension(relativePath, ".json");
    var outputPath = Path.Combine(websiteDocsOutputDir, outputRelativePath);

    var markdown = await File.ReadAllTextAsync(file);

    var (meta, content) = ParseFrontmatter(markdown);

    var title = meta.TryGetValue("title", out var metaTitle) && !string.IsNullOrWhiteSpace(metaTitle)
        ? metaTitle
        : ExtractTitle(content) ?? Path.GetFileNameWithoutExtension(file);

    var order = meta.ContainsKey("order")
        ? int.Parse(meta["order"])
        : 999;

    var group = meta.ContainsKey("group")
        ? meta["group"]
        : "";

    var slug = NormalizeSlug(relativePath);

    var groupOrder = groupOrders.TryGetValue(group, out var resolvedGroupOrder)
        ? resolvedGroupOrder
        : 999;

    allDocs.Add(new DocIndexItem
    {
        Title = title,
        Slug = slug,
        Order = order,
        Group = group,
        GroupOrder = groupOrder
    });

    var inputLastWrite = File.GetLastWriteTimeUtc(file);

    if (File.Exists(outputPath))
    {
        var outputLastWrite = File.GetLastWriteTimeUtc(outputPath);

        if (outputLastWrite >= inputLastWrite)
        {
            Console.WriteLine($"⏩ Skipped: {relativePath}");
            continue;
        }
    }

    Console.WriteLine($"⚙ Processing: {relativePath}");

    //var headings = ExtractHeadings(content);
    var html = Markdown.ToHtml(content, pipeline);
    html = RemoveFirstH1(html);

    var headings = ExtractHeadingsFromHtml(html);

    html = Regex.Replace(html, "<h2", "<h2 class=\"mud-scrollspy-section\"");
    html = Regex.Replace(html, "<h3", "<h3 class=\"mud-scrollspy-section\"");

    var outputFolder = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(outputFolder))
        Directory.CreateDirectory(outputFolder);

    var doc = new DocOutput(slug, title, html, headings);

    var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    await File.WriteAllTextAsync(outputPath, json);
}

var ordered = allDocs
    .OrderBy(x => x.GroupOrder)
    .ThenBy(x => x.Group)
    .ThenBy(x => x.Order)
    .ToList();

var indexJson = JsonSerializer.Serialize(ordered, new JsonSerializerOptions
{
    WriteIndented = true
});

await File.WriteAllTextAsync(
    Path.Combine(websiteDocsOutputDir, "docs-index.json"),
    indexJson
);

Console.WriteLine("✅ Docs build completed.");


// ---------------- HELPERS ----------------

static string? ExtractTitle(string markdown)
{
    using var reader = new StringReader(markdown);

    while (reader.ReadLine() is { } line)
    {
        if (line.StartsWith("# "))
            return line[2..].Trim();
    }

    return null;
}

static string NormalizeSlug(string relativePath)
{
    return Path.ChangeExtension(relativePath, null)!
        .Replace('\\', '/');
}

static string? TryGetSolutionDir(string[] args)
{
    var index = Array.IndexOf(args, "--solutionDir");

    if (index >= 0 && index < args.Length - 1)
    {
        var path = args[index + 1];

        if (Directory.Exists(path))
            return Path.GetFullPath(path);
    }

    return null;
}

static string FindSolutionRootFallback()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);

    for (int i = 0; i < 10; i++)
    {
        if (dir.GetDirectories("docs").Any())
            return dir.FullName;

        dir = dir.Parent!;
        if (dir == null) break;
    }

    throw new Exception("Solution root not found");
}

static (Dictionary<string, string> meta, string content) ParseFrontmatter(string markdown)
{
    if (string.IsNullOrWhiteSpace(markdown))
        return (new(), string.Empty);

    markdown = markdown.TrimStart('\uFEFF');
    markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");
    markdown = markdown.TrimStart();

    if (!markdown.StartsWith("---"))
        return (new(), markdown);

    var firstLineEnd = markdown.IndexOf('\n');
    if (firstLineEnd == -1)
        return (new(), markdown);

    var firstLine = markdown.Substring(0, firstLineEnd).Trim();
    if (firstLine != "---")
        return (new(), markdown);

    var end = markdown.IndexOf("\n---\n", firstLineEnd + 1);
    if (end == -1)
        return (new(), markdown);

    var rawMeta = markdown.Substring(firstLineEnd + 1, end - firstLineEnd - 1);
    var content = markdown[(end + 5)..];

    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var line in rawMeta.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);

        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
        {
            dict[parts[0]] = parts[1];
        }
    }

    return (dict, content);
}

static string RemoveFirstH1(string html)
{
    var start = html.IndexOf("<h1");
    if (start == -1) return html;

    var end = html.IndexOf("</h1>");
    if (end == -1) return html;

    return html.Remove(start, end + 5 - start);
}

static List<DocsHeadingItem> ExtractHeadings(string markdown)
{
    var result = new List<DocsHeadingItem>();
    using var reader = new StringReader(markdown);

    string? line;
    while ((line = reader.ReadLine()) is not null)
    {
        var trimmed = line.Trim();

        if (trimmed.StartsWith("## "))
        {
            var text = trimmed[3..].Trim();
            result.Add(new DocsHeadingItem
            {
                Text = text,
                Id = Slugify(text),
                Level = 0
            });
        }
        else if (trimmed.StartsWith("### "))
        {
            var text = trimmed[4..].Trim();
            result.Add(new DocsHeadingItem
            {
                Text = text,
                Id = Slugify(text),
                Level = 1
            });
        }
    }

    return result;
}

static List<DocsHeadingItem> ExtractHeadingsFromHtml(string html)
{
    var result = new List<DocsHeadingItem>();

    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    var nodes = doc.DocumentNode.SelectNodes("//h2 | //h3");
    if (nodes == null)
        return result;

    foreach (var node in nodes)
    {
        var id = node.GetAttributeValue("id", null);

        if (string.IsNullOrWhiteSpace(id))
            continue;

        result.Add(new DocsHeadingItem
        {
            Id = id,
            Text = HtmlEntity.DeEntitize(node.InnerText),
            Level = node.Name == "h2" ? 0 : 1
        });
    }

    return result;
}

static string Slugify(string text)
{
    var chars = text
        .ToLowerInvariant()
        .Select(c => char.IsLetterOrDigit(c) ? c : '-')
        .ToArray();

    var slug = new string(chars);
    while (slug.Contains("--"))
        slug = slug.Replace("--", "-");

    return slug.Trim('-');
}

internal sealed record DocOutput(string Slug, string Title, string Html, List<DocsHeadingItem> Headings);

internal sealed class DocIndexItem
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Order { get; set; }
    public string Group { get; set; } = "";
    public int GroupOrder { get; set; } = 999;
}

internal sealed class DocsHeadingItem
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int Level { get; set; }
}
