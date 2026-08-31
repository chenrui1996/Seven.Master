using Seven.Domain.Common;

namespace Seven.Domain.Entities.Flow;

/// <summary>工作流定义（对齐 Legrand：可视化 Node/Line、权重、审核中可编辑）</summary>
public class Sys_WorkFlow : BaseEntity
{
    public int WorkFlow_Id { get; set; }

    public string WorkName { get; set; } = string.Empty;

    /// <summary>关联业务表名</summary>
    public string? WorkTable { get; set; }

    /// <summary>功能菜单显示名</summary>
    public string? WorkTableName { get; set; }

    /// <summary>关联表主键字段</summary>
    public string? WorkTableKey { get; set; }

    /// <summary>权重（同表多流程时优先匹配大权重）</summary>
    public int? Weight { get; set; }

    public byte? Enable { get; set; } = 1;

    /// <summary>可视化节点 JSON</summary>
    public string? NodeConfig { get; set; }

    /// <summary>可视化连线 JSON</summary>
    public string? LineConfig { get; set; }

    public string? Remark { get; set; }

    /// <summary>审核中数据是否可编辑（1=可）</summary>
    public int? AuditingEdit { get; set; }

    public ICollection<Sys_WorkFlowStep> Steps { get; set; } = [];
}

/// <summary>工作流步骤（对齐 Legrand 节点属性）</summary>
public class Sys_WorkFlowStep : BaseEntity
{
    public int WorkStepFlow_Id { get; set; }

    public int WorkFlow_Id { get; set; }

    /// <summary>可视化节点 Id（稳定标识）</summary>
    public string? StepId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    /// <summary>审批人类型：1角色 2用户 3部门</summary>
    public int? StepType { get; set; }

    public string? StepValue { get; set; }

    /// <summary>节点属性：start / node / end</summary>
    public string? StepAttrType { get; set; }

    /// <summary>下一节点 StepId 列表（逗号分隔）</summary>
    public string? NextStepIds { get; set; }

    public string? ParentId { get; set; }

    public int? Weight { get; set; }

    /// <summary>条件过滤 JSON</summary>
    public string? Filters { get; set; }

    public int? AuditRefuse { get; set; }

    public int? AuditBack { get; set; }

    public int? AuditMethod { get; set; }

    public int? SendMail { get; set; }

    public string? Remark { get; set; }

    public Sys_WorkFlow? WorkFlow { get; set; }
}

/// <summary>工作流实例</summary>
public class Sys_WorkFlowTable : BaseEntity
{
    public int WorkFlowTable_Id { get; set; }

    public int WorkFlow_Id { get; set; }

    public string? WorkTable { get; set; }

    public string? WorkTableKey { get; set; }

    public int AuditStatus { get; set; }

    public int? CurrentStepId { get; set; }
}

/// <summary>工作流实例步骤</summary>
public class Sys_WorkFlowTableStep : BaseEntity
{
    public int Sys_WorkFlowTableStep_Id { get; set; }

    public int WorkFlowTable_Id { get; set; }

    public int WorkStepFlow_Id { get; set; }

    /// <summary>步骤名称快照</summary>
    public string? StepName { get; set; }

    public int AuditStatus { get; set; }

    public int? AuditUserId { get; set; }

    public string? Auditor { get; set; }

    public DateTime? AuditDate { get; set; }

    public string? Remark { get; set; }
}

/// <summary>工作流审批日志</summary>
public class Sys_WorkFlowTableAuditLog : BaseEntity
{
    public int Id { get; set; }

    public int WorkFlowTable_Id { get; set; }

    public int WorkStepFlow_Id { get; set; }

    public string? AuditUser { get; set; }

    public int AuditStatus { get; set; }

    public string? Remark { get; set; }
}
