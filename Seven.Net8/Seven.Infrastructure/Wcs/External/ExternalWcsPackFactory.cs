using Seven.Application.Wcs;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.External;

/// <summary>按配置项构造 ExternalWcsPack 实例。</summary>
public sealed class ExternalWcsPackFactory
{
    private readonly ExternalTransportFactory _transportFactory;
    private readonly VendorCodecRegistry _codecRegistry;
    private readonly SevenDbContext _db;
    private readonly IOrchestrationBus? _bus;

    public ExternalWcsPackFactory(
        ExternalTransportFactory transportFactory,
        VendorCodecRegistry codecRegistry,
        SevenDbContext db,
        IOrchestrationBus? bus = null)
    {
        _transportFactory = transportFactory;
        _codecRegistry = codecRegistry;
        _db = db;
        _bus = bus;
    }

    public ExternalWcsPack Create(ExternalWcsEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var transport = _transportFactory.Create(options);
        var codec = _codecRegistry.Resolve(options.Codec);
        return new ExternalWcsPack(options, transport, codec, _db, _bus);
    }
}
