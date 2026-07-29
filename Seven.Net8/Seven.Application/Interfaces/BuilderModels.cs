namespace Seven.Application.Interfaces;

/// <summary>加载表配置请求</summary>
public class LoadTableRequest
{
    public int ParentId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ColumnCNName { get; set; } = string.Empty;
    public string Namespace { get; set; } = "Seven.Domain.Entities.System";
    public string FolderName { get; set; } = "System";
    public int Table_Id { get; set; }
    public bool IsTreeLoad { get; set; }
}

/// <summary>生成业务类请求</summary>
public class CreateServicesRequest
{
    public string TableName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
}

/// <summary>生成 Vue 页面请求</summary>
public class CreateVuePageRequest
{
    public Domain.Entities.Core.Sys_TableInfo TableInfo { get; set; } = new();
    public string? VuePath { get; set; }
}

/// <summary>保存主子表关系请求</summary>
public class SaveTableDetailsRequest
{
    public string ParentTable { get; set; } = string.Empty;
    public List<Domain.Entities.Core.Sys_TableDetail> Details { get; set; } = [];
}

/// <summary>扫描外键请求</summary>
public class ScanForeignKeysRequest
{
    public string ParentTable { get; set; } = string.Empty;
}
