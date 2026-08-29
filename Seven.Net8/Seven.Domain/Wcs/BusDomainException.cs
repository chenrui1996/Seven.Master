namespace Seven.Domain.Wcs;

/// <summary>编排总线领域规则违反（如规划失败）。</summary>
public class BusDomainException : Exception
{
    public BusDomainException(string message) : base(message) { }
}
