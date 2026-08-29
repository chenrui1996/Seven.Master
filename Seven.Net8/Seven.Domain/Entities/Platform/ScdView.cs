using Seven.Domain.Common;

namespace Seven.Domain.Entities.Platform;

/// <summary>2D SCADA 视图（画布尺寸与标识）</summary>
public class ScdView : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}
