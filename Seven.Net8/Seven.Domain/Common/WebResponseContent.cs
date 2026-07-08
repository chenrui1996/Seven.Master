namespace Seven.Domain.Common;

/// <summary>
/// 统一 API 响应体，与 Legrand WebResponseContent 兼容。
/// </summary>
public class WebResponseContent
{
    /// <summary>操作是否成功</summary>
    public bool Status { get; set; }

    /// <summary>业务状态码</summary>
    public string? Code { get; set; }

    /// <summary>提示消息</summary>
    public string? Message { get; set; }

    /// <summary>响应数据</summary>
    public object? Data { get; set; }

    /// <summary>创建成功响应</summary>
    public static WebResponseContent Ok(string? message = null, object? data = null) =>
        new() { Status = true, Message = message, Data = data };

    /// <summary>创建失败响应</summary>
    public static WebResponseContent Error(string? message = null) =>
        new() { Status = false, Message = message };
}

/// <summary>
/// 分页查询结果
/// </summary>
public class PageGridData<T>
{
    /// <summary>总行数</summary>
    public int Total { get; set; }

    /// <summary>当前页数据</summary>
    public List<T> Rows { get; set; } = [];
}

/// <summary>
/// 分页查询参数
/// </summary>
public class PageDataOptions
{
    /// <summary>页码，从 1 开始</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数</summary>
    public int Rows { get; set; } = 30;

    /// <summary>排序字段</summary>
    public string? Sort { get; set; }

    /// <summary>排序方向 asc/desc</summary>
    public string? Order { get; set; }

    /// <summary>搜索关键字 JSON</summary>
    public string? Wheres { get; set; }
}
