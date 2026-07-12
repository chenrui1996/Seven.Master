using Seven.Domain.Common;

namespace Seven.Domain.Entities.Core;

/// <summary>
/// 代码生成器-表信息
/// </summary>
public class Sys_TableInfo : BaseEntity
{
    /// <summary>表 Id</summary>
    public int Table_Id { get; set; }

    /// <summary>父级 Id（配置树）</summary>
    public int? ParentId { get; set; }

    /// <summary>表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>数据库真实表名</summary>
    public string? TableTrueName { get; set; }

    /// <summary>表中文名</summary>
    public string? ColumnCNName { get; set; }

    /// <summary>命名空间</summary>
    public string? Namespace { get; set; }

    /// <summary>文件夹名</summary>
    public string? FolderName { get; set; }

    /// <summary>列表类型</summary>
    public string? DataTableType { get; set; }

    /// <summary>编辑类型</summary>
    public string? EditorType { get; set; }

    /// <summary>排序</summary>
    public int? OrderNo { get; set; }

    /// <summary>上传字段</summary>
    public string? UploadField { get; set; }

    /// <summary>快捷编辑字段</summary>
    public string? ExpressField { get; set; }

    /// <summary>明细表名</summary>
    public string? DetailName { get; set; }

    /// <summary>排序字段</summary>
    public string? SortName { get; set; }

    /// <summary>数据库连接标识</summary>
    public string? DBServer { get; set; }

    /// <summary>是否启用</summary>
    public int? Enable { get; set; } = 1;

    /// <summary>中文名（显示）</summary>
    public string? CnName { get; set; }

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

    /// <summary>表名</summary>
    public string? TableName { get; set; }

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

    /// <summary>最大长度</summary>
    public int? Maxlength { get; set; }

    /// <summary>是否可空</summary>
    public int? IsNull { get; set; }

    /// <summary>是否显示</summary>
    public int? IsDisplay { get; set; } = 1;

    /// <summary>是否列数据</summary>
    public int? IsColumnData { get; set; } = 1;

    /// <summary>列宽</summary>
    public int? ColumnWidth { get; set; }

    /// <summary>查询行号</summary>
    public int? SearchRowNo { get; set; }

    /// <summary>查询列号</summary>
    public int? SearchColNo { get; set; }

    /// <summary>查询类型</summary>
    public string? SearchType { get; set; }

    /// <summary>编辑行号</summary>
    public int? EditRowNo { get; set; }

    /// <summary>编辑列号</summary>
    public int? EditColNo { get; set; }

    /// <summary>编辑类型</summary>
    public string? EditType { get; set; }

    /// <summary>字典编号</summary>
    public string? DropNo { get; set; }

    /// <summary>列占用</summary>
    public int? ColSize { get; set; }

    /// <summary>是否只读数据集</summary>
    public int? IsReadDataset { get; set; }

    /// <summary>是否可排序</summary>
    public int? Sortable { get; set; }

    /// <summary>图片类型</summary>
    public int? IsImage { get; set; }

    /// <summary>格式化</summary>
    public string? Columnformat { get; set; }

    /// <summary>脚本</summary>
    public string? Script { get; set; }

    /// <summary>启用：1显示/查询/编辑</summary>
    public int? Enable { get; set; } = 1;

    /// <summary>排序</summary>
    public int? OrderNo { get; set; }
}
