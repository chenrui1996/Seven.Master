namespace Seven.Domain.Attributes;

/// <summary>
/// 标记字段在代码生成表单中使用指定枚举下拉（属性可为 int / 枚举）。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FormEnumAttribute : Attribute
{
    /// <summary>枚举类型</summary>
    public Type EnumType { get; }

    /// <summary>构造函数</summary>
    public FormEnumAttribute(Type enumType)
    {
        if (enumType is null || !enumType.IsEnum)
            throw new ArgumentException("FormEnum 必须指向枚举类型", nameof(enumType));
        EnumType = enumType;
    }
}
