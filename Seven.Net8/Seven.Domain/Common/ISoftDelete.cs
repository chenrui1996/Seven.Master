namespace Seven.Domain.Common;

/// <summary>软删除标记接口（配合全局查询过滤器）</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
