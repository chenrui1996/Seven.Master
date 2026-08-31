using Seven.Domain.Common;

namespace Seven.Domain.Wms;

/// <summary>WMS 领域规则违反。须带业务码（见 ExceptionCodes.Wms / doc/22）。</summary>
public class WmsDomainException : AppException
{
    public WmsDomainException(string message)
        : base(ExceptionCodes.Wms.Domain, message) { }

    public WmsDomainException(string code, string message, int httpStatus = 400)
        : base(code, message, httpStatus) { }
}
