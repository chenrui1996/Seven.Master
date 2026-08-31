namespace Seven.Domain.Wcs;

/// <summary>
/// 数据库表前缀权威常量。已有 Wms_/Bus_/Stk_/Fw_/Sys_/Sim_/Scd_/Ext_/Ifc_/Ctl_ 不改名；
/// 无模块前缀表补齐为 Biz_/Dc_/Form_/Mq_。
/// 项目侧扩展单据/逻辑：实体放 Entities/Business、表一律 Biz_、服务放 Seven.Business（见 doc/23）。
/// </summary>
public static class TablePrefixes
{
    public const string Wms = "Wms_";
    public const string Bus = "Bus_";
    public const string Stacker = "Stk_";
    public const string FourWay = "Fw_";
    public const string External = "Ext_";
    public const string InterfaceLog = "Ifc_";
    public const string Control = "Ctl_";
    public const string Scada = "Scd_";
    public const string Simulator = "Sim_";

    /// <summary>
    /// 业务扩展与 Demo（Entities/Business）。
    /// 新出入库类型、在库处理单据等项目扩展表使用本前缀，勿占用 Wms_。
    /// </summary>
    public const string Biz = "Biz_";

    /// <summary>设备通讯 DeviceComm</summary>
    public const string DeviceComm = "Dc_";

    /// <summary>动态表单</summary>
    public const string Form = "Form_";

    /// <summary>消息队列 Outbox</summary>
    public const string Mq = "Mq_";
}
