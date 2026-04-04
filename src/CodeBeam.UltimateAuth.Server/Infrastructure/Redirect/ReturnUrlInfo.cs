namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed record ReturnUrlInfo
{
    public ReturnUrlKind Kind { get; }
    public string? RelativePath { get; }
    public Uri? AbsoluteUri { get; }

    private ReturnUrlInfo(ReturnUrlKind kind, string? relative, Uri? absolute)
    {
        Kind = kind;
        RelativePath = relative;
        AbsoluteUri = absolute;
    }

    public static ReturnUrlInfo None()
        => new(ReturnUrlKind.None, null, null);

    public static ReturnUrlInfo Relative(string path)
        => new(ReturnUrlKind.Relative, path, null);

    public static ReturnUrlInfo Absolute(Uri uri)
        => new(ReturnUrlKind.Absolute, null, uri);

    public bool IsNone => Kind == ReturnUrlKind.None;
    public bool IsRelative => Kind == ReturnUrlKind.Relative;
    public bool IsAbsolute => Kind == ReturnUrlKind.Absolute;
}
