using Seven.Domain.Common;

namespace Seven.Domain.Entities.News;

/// <summary>
/// 新闻/公告
/// </summary>
public class App_News : BaseEntity
{
    /// <summary>新闻 Id</summary>
    public int Id { get; set; }

    /// <summary>标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>内容</summary>
    public string? Content { get; set; }

    /// <summary>是否发布</summary>
    public byte? Enable { get; set; } = 1;
}
