namespace Seven.Domain.Wms;

/// <summary>WMS 领域规则违反（如库存不足）。</summary>
public class WmsDomainException : Exception
{
    public WmsDomainException(string message) : base(message) { }
}
