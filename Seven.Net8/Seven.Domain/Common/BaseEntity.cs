namespace Seven.Domain.Common;

/// <summary>
/// 实体基类，包含审计字段与软删除标记。
/// </summary>
public abstract class BaseEntity : ISoftDelete
{
    /// <summary>多租户 Id（0 表示默认/未启用租户）</summary>
    public int TenantId { get; set; }

    /// <summary>创建人 Id</summary>
    public int? CreateId { get; set; }

    /// <summary>创建人姓名</summary>
    public string? Creator { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateDate { get; set; }

    /// <summary>修改人 Id</summary>
    public int? ModifyId { get; set; }

    /// <summary>修改人姓名</summary>
    public string? Modifier { get; set; }

    /// <summary>修改时间</summary>
    public DateTime? ModifyDate { get; set; }

    /// <summary>是否已软删除</summary>
    public bool IsDeleted { get; set; }
}
