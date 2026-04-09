namespace CodeBeam.UltimateAuth.Docs.Wasm.Client.Pages;

public sealed class DocsPageState
{
    public string? CurrentSlug { get; private set; }
    public string? CurrentTitle { get; private set; }
    public IReadOnlyList<DocsHeadingItem> Headings { get; private set; } = [];

    public string? ActiveHeadingId { get; private set; }

    public event Action? Changed;

    public void SetPage(string? slug, string? title, IReadOnlyList<DocsHeadingItem> headings)
    {
        CurrentSlug = slug;
        CurrentTitle = title;
        Headings = headings;
        ActiveHeadingId = headings.Count > 0 ? headings[0].Id : null;
        Changed?.Invoke();
    }

    public void SetActiveHeading(string? id)
    {
        if (string.Equals(ActiveHeadingId, id, StringComparison.Ordinal))
            return;

        ActiveHeadingId = id;
        Changed?.Invoke();
    }

    public void Clear()
    {
        CurrentSlug = null;
        CurrentTitle = null;
        Headings = [];
        ActiveHeadingId = null;
        Changed?.Invoke();
    }
}

public sealed class DocsHeadingItem
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int Level { get; set; }
}
