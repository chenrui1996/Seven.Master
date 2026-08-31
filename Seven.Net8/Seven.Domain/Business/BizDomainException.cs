using Seven.Domain.Common;

namespace Seven.Domain.Business;

/// <summary>业务扩展领域异常。须带 ExceptionCodes.Biz / doc/22。</summary>
public class BizDomainException : AppException
{
    public BizDomainException(string message)
        : base(ExceptionCodes.Biz.Domain, message) { }

    public BizDomainException(string code, string message, int httpStatus = 400)
        : base(code, message, httpStatus) { }
}
