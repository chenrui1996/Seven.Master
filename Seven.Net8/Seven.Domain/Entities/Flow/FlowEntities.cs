using Seven.Domain.Common;

namespace Seven.Domain.Entities.Flow;

/// <summary>
/// 工作流定义
/// </summary>
public class Sys_WorkFlow : BaseEntity
{
    /// <summary>流程 Id</summary>
    public int WorkFlow_Id { get; set; }

    /// <summary>流程名称</summary>
    public string WorkName { get; set; } = string.Empty;

    /// <summary>关联业务表名</summary>
    public string? WorkTable { get; set; }

    /// <summary>关联表主键字段</summary>
    public string? WorkTableKey { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>流程步骤</summary>
    public ICollection<Sys_WorkFlowStep> Steps { get; set; } = [];
}

/// <summary>
/// 工作流步骤
/// </summary>
public class Sys_WorkFlowStep : BaseEntity
{
    /// <summary>步骤 Id</summary>
    public int WorkStepFlow_Id { get; set; }

    /// <summary>流程 Id</summary>
    public int WorkFlow_Id { get; set; }

    /// <summary>步骤名称</summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>步骤顺序</summary>
    public int StepOrder { get; set; }

    /// <summary>审批人类型</summary>
    public int? StepType { get; set; }

    /// <summary>审批人值（角色/用户 Id）</summary>
    public string? StepValue { get; set; }

    /// <summary>关联流程</summary>
    public Sys_WorkFlow? WorkFlow { get; set; }
}

/// <summary>
/// 工作流实例
/// </summary>
public class Sys_WorkFlowTable : BaseEntity
{
    /// <summary>实例 Id</summary>
    public int WorkFlowTable_Id { get; set; }

    /// <summary>流程 Id</summary>
    public int WorkFlow_Id { get; set; }

    /// <summary>业务表名</summary>
    public string? WorkTable { get; set; }

    /// <summary>业务主键值</summary>
    public string? WorkTableKey { get; set; }

    /// <summary>当前审批状态</summary>
    public int AuditStatus { get; set; }

    /// <summary>当前步骤 Id</summary>
    public int? CurrentStepId { get; set; }
}

/// <summary>
/// 工作流实例步骤
/// </summary>
public class Sys_WorkFlowTableStep : BaseEntity
{
    /// <summary>主键</summary>
    public int Sys_WorkFlowTableStep_Id { get; set; }

    /// <summary>实例 Id</summary>
    public int WorkFlowTable_Id { get; set; }

    /// <summary>步骤 Id</summary>
    public int WorkStepFlow_Id { get; set; }

    /// <summary>审批状态</summary>
    public int AuditStatus { get; set; }

    /// <summary>审批人 Id</summary>
    public int? AuditUserId { get; set; }

    /// <summary>审批时间</summary>
    public DateTime? AuditDate { get; set; }
}

/// <summary>
/// 工作流审批日志
/// </summary>
public class Sys_WorkFlowTableAuditLog : BaseEntity
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>实例 Id</summary>
    public int WorkFlowTable_Id { get; set; }

    /// <summary>步骤 Id</summary>
    public int WorkStepFlow_Id { get; set; }

    /// <summary>审批人</summary>
    public string? AuditUser { get; set; }

    /// <summary>审批结果</summary>
    public int AuditStatus { get; set; }

    /// <summary>审批意见</summary>
    public string? Remark { get; set; }
}
