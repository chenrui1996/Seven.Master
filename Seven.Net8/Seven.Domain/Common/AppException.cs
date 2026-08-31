namespace Seven.Domain.Common;

/// <summary>
/// 带业务码的应用异常。中间件按 <see cref="Code"/> 返回；禁止裸中文 throw 无码。
/// 码段见 doc/22-异常码规范.md。
/// </summary>
public class AppException : Exception
{
    public string Code { get; }
    public int HttpStatus { get; }

    public AppException(string code, string message, int httpStatus = 400)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code) ? ExceptionCodes.Sys.Unhandled : code.Trim();
        HttpStatus = httpStatus is >= 400 and < 600 ? httpStatus : 400;
    }
}

/// <summary>常用异常码常量（首批）。完整码表见 doc/22。</summary>
public static class ExceptionCodes
{
    public static class Sys
    {
        public const string Unhandled = "SYS.UNHANDLED";
        public const string Validation = "SYS.VALIDATION";
    }

    public static class Auth
    {
        public const string Unauthorized = "AUTH.UNAUTHORIZED";
        public const string Forbidden = "AUTH.FORBIDDEN";
    }

    public static class Wms
    {
        public const string Domain = "WMS.DOMAIN";
        public const string StockInsufficient = "WMS.STOCK_INSUFFICIENT";
        public const string OrderStatusIllegal = "WMS.ORDER_STATUS_ILLEGAL";
        public const string OrderNotFound = "WMS.ORDER_NOT_FOUND";
        public const string QtyInvalid = "WMS.QTY_INVALID";
        public const string LocationRequired = "WMS.LOCATION_REQUIRED";
        public const string LocationNotFound = "WMS.LOCATION_NOT_FOUND";
        public const string LocationLocked = "WMS.LOCATION_LOCKED";
        public const string MaterialRequired = "WMS.MATERIAL_REQUIRED";
        public const string OrderNoRequired = "WMS.ORDER_NO_REQUIRED";
        public const string OrderNoExists = "WMS.ORDER_NO_EXISTS";
        public const string LinesRequired = "WMS.LINES_REQUIRED";
        public const string TransportPending = "WMS.TRANSPORT_PENDING";
        public const string PickingNotFound = "WMS.PICKING_NOT_FOUND";
        public const string PickingStatusIllegal = "WMS.PICKING_STATUS_ILLEGAL";
    }

    /// <summary>业务扩展（Seven.Business / Biz_）。见 doc/23。</summary>
    public static class Biz
    {
        public const string Domain = "BIZ.DOMAIN";
        public const string OrderNotFound = "BIZ.ORDER_NOT_FOUND";
        public const string OrderStatusIllegal = "BIZ.ORDER_STATUS_ILLEGAL";
        public const string OrderNoRequired = "BIZ.ORDER_NO_REQUIRED";
        public const string OrderNoExists = "BIZ.ORDER_NO_EXISTS";
        public const string LinesRequired = "BIZ.LINES_REQUIRED";
        public const string QtyInvalid = "BIZ.QTY_INVALID";
        public const string MaterialRequired = "BIZ.MATERIAL_REQUIRED";
        public const string LocationRequired = "BIZ.LOCATION_REQUIRED";
    }

    public static class Bus
    {
        public const string Domain = "BUS.DOMAIN";
    }

    public static class Stk
    {
        public const string Domain = "STK.DOMAIN";
    }

    public static class Fw
    {
        public const string Domain = "FW.DOMAIN";
        public const string PromoteLoopback = "FW.PROMOTE_LOOPBACK";
    }

    public static class Sim
    {
        public const string Domain = "SIM.DOMAIN";
        public const string PromoteLoopback = "SIM.PROMOTE_LOOPBACK";
    }
}
