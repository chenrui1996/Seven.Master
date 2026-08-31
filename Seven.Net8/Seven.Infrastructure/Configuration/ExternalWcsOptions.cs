namespace Seven.Infrastructure.Configuration;

/// <summary>外部 WCS 配置项（appsettings ExternalWcs 数组元素）。</summary>
public class ExternalWcsEntryOptions
{
    public const string SectionName = "ExternalWcs";

    public string PackId { get; set; } = string.Empty;
    public string Transport { get; set; } = "Http";
    public string Codec { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool Enabled { get; set; } = true;
}
