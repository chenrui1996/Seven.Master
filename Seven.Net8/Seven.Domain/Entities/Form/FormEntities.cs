using Seven.Domain.Common;

namespace Seven.Domain.Entities.Form;

/// <summary>
/// 表单设计配置
/// </summary>
public class FormDesignOptions : BaseEntity
{
    /// <summary>主键</summary>
    public int FormId { get; set; }

    /// <summary>表单名称</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>表单 JSON 配置</summary>
    public string? FormOptions { get; set; }

    /// <summary>表单 HTML</summary>
    public string? FormHtml { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;
}

/// <summary>
/// 表单采集数据
/// </summary>
public class FormCollectionObject : BaseEntity
{
    /// <summary>主键</summary>
    public int FormCollectionId { get; set; }

    /// <summary>表单 Id</summary>
    public int FormId { get; set; }

    /// <summary>采集数据 JSON</summary>
    public string? FormData { get; set; }

    /// <summary>提交人</summary>
    public string? Submitter { get; set; }
}
