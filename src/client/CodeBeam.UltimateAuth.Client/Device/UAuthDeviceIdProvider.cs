using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client;

internal sealed class UAuthDeviceIdProvider : IDeviceIdProvider
{
    private readonly IDeviceIdStorage _storage;
    private readonly IDeviceIdGenerator _generator;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DeviceId? _cached;

    public UAuthDeviceIdProvider(IDeviceIdStorage storage, IDeviceIdGenerator generator)
    {
        _storage = storage;
        _generator = generator;
    }

    public async ValueTask<DeviceId> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
            return _cached.Value;

        await _gate.WaitAsync(ct);
        try
        {
            var raw = await _storage.LoadAsync(ct);

            if (!string.IsNullOrWhiteSpace(raw))
            {
                _cached = DeviceId.Create(raw);
                return _cached.Value;
            }

            var generated = _generator.Generate();
            await _storage.SaveAsync(generated.Value, ct);

            _cached = generated;
            return generated;
        }
        finally
        { 
            _gate.Release(); 
        }
    }
}
