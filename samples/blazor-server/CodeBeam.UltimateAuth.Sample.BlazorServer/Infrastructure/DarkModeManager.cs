using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Infrastructure;

public sealed class DarkModeManager
{
    private const string StorageKey = "uauth:theme:dark";

    private readonly IBrowserStorage _storage;

    public DarkModeManager(IBrowserStorage storage)
    {
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        var value = await _storage.GetAsync(StorageScope.Local, StorageKey);

        if (bool.TryParse(value, out var parsed))
            IsDarkMode = parsed;
    }

    public bool IsDarkMode { get; set; }

    public event Action? Changed;

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;

        await _storage.SetAsync(StorageScope.Local, StorageKey, IsDarkMode.ToString());
        Changed?.Invoke();
    }

    public void Set(bool value)
    {
        if (IsDarkMode == value)
            return;

        IsDarkMode = value;
        Changed?.Invoke();
    }
}
