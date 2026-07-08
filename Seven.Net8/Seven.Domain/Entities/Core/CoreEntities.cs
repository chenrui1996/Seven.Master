using Seven.Domain.Common;

namespace Seven.Domain.Entities.Core;

/// <summary>
/// 代码生成器-表信息
/// </summary>
public class Sys_TableInfo : BaseEntity
{
    /// <summary>表 Id</summary>
    public int Table_Id { get; set; }

    /// <summary>表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>表中文名</summary>
    public string? ColumnCNName { get; set; }

    /// <summary>命名空间</summary>
    public string? Namespace { get; set; }

    /// <summary>列信息</summary>
    public ICollection<Sys_TableColumn> TableColumns { get; set; } = [];
}

/// <summary>
/// 代码生成器-列信息
/// </summary>
public class Sys_TableColumn : BaseEntity
{
    /// <summary>列 Id</summary>
    public int ColumnId { get; set; }

    /// <summary>表 Id</summary>
    public int Table_Id { get; set; }

    /// <summary>列名</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>列中文名</summary>
    public string? ColumnCNName { get; set; }

    /// <summary>数据库类型</summary>
    public string? ColumnType { get; set; }

    /// <summary>是否主键</summary>
    public bool IsKey { get; set; }

    /// <summary>是否可编辑</summary>
    public bool Editable { get; set; } = true;
}
