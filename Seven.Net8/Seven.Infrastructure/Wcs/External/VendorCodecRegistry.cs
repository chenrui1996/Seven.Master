namespace Seven.Infrastructure.Wcs.External;

/// <summary>按 Codec 名称解析已注册供应商编解码器。</summary>
public sealed class VendorCodecRegistry
{
    private readonly IReadOnlyDictionary<string, IVendorCodec> _codecs;

    public VendorCodecRegistry(IEnumerable<IVendorCodec> codecs)
        => _codecs = codecs.ToDictionary(c => c.CodecName, StringComparer.OrdinalIgnoreCase);

    public IVendorCodec Resolve(string codecName)
    {
        if (string.IsNullOrWhiteSpace(codecName))
            throw new InvalidOperationException("External WCS Codec name is required.");
        if (!_codecs.TryGetValue(codecName, out var codec))
            throw new InvalidOperationException($"External WCS Codec not registered: {codecName}");
        return codec;
    }
}
