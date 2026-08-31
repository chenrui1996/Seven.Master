using Seven.Domain.Common;

namespace Seven.Domain.Wcs;

/// <summary>编排总线领域异常。</summary>
public class BusDomainException : AppException
{
    public BusDomainException(string message)
        : base(ExceptionCodes.Bus.Domain, message) { }

    public BusDomainException(string code, string message, int httpStatus = 400)
        : base(code, message, httpStatus) { }
}
