using Seven.Application.Models;
using Seven.Domain.Common;

namespace Seven.Application.Interfaces;

/// <summary>
/// 认证服务
/// </summary>
public interface IAuthService
{
    /// <summary>用户登录</summary>
    Task<WebResponseContent> LoginAsync(string userName, string password, string? captchaCode, string? captchaKey, CancellationToken cancellationToken = default);

    /// <summary>刷新 Token</summary>
    Task<WebResponseContent> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>退出登录</summary>
    Task<WebResponseContent> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>修改密码</summary>
    Task<WebResponseContent> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>获取当前登录用户权限码列表（用于改权后刷新前端）</summary>
    Task<WebResponseContent> GetMyPermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户管理服务
/// </summary>
public interface ISysUserService
{
    Task<PageGridData<Domain.Entities.System.Sys_User>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(Domain.Entities.System.Sys_User entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(Domain.Entities.System.Sys_User entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// 角色管理服务
/// </summary>
public interface ISysRoleService
{
    Task<PageGridData<Domain.Entities.System.Sys_Role>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(Domain.Entities.System.Sys_Role entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(Domain.Entities.System.Sys_Role entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetCurrentTreePermissionAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetUserTreePermissionAsync(int roleId, CancellationToken cancellationToken = default);
    Task<WebResponseContent> SavePermissionAsync(SaveRolePermissionRequest request, CancellationToken cancellationToken = default);
    Task<WebResponseContent> SavePermissionAsync(int roleId, int[] menuIds, Dictionary<int, string> authValues, CancellationToken cancellationToken = default);
}

/// <summary>
/// 菜单管理服务
/// </summary>
public interface ISysMenuService
{
    Task<WebResponseContent> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetMenuListAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetTreeItemAsync(int menuId, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetMenuByRoleAsync(int roleId, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetCurrentUserMenuAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> SaveAsync(Domain.Entities.System.Sys_Menu entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(Domain.Entities.System.Sys_Menu entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(Domain.Entities.System.Sys_Menu entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 部门管理服务
/// </summary>
public interface ISysDepartmentService
{
    Task<WebResponseContent> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(Domain.Entities.System.Sys_Department entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(Domain.Entities.System.Sys_Department entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 字典管理服务
/// </summary>
public interface ISysDictionaryService
{
    Task<WebResponseContent> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetVueDictionaryAsync(string[] dicNos, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetBuilderDictionaryAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> AddAsync(Domain.Entities.System.Sys_Dictionary entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> UpdateAsync(Domain.Entities.System.Sys_Dictionary entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// 日志查询服务
/// </summary>
public interface ISysLogService
{
    Task<PageGridData<Domain.Entities.System.Sys_Log>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// 工作流服务
/// </summary>
public interface IWorkFlowService
{
    Task<WebResponseContent> SubmitAsync(string tableName, string tableKey, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AuditAsync(int workFlowTableId, int auditStatus, string? remark, CancellationToken cancellationToken = default);
    Task<PageGridData<Domain.Entities.Flow.Sys_WorkFlowTable>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成器服务
/// </summary>
public interface IBuilderService
{
    Task<WebResponseContent> GetTableTreeAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> LoadTableAsync(LoadTableRequest request, CancellationToken cancellationToken = default);
    Task<WebResponseContent> SaveAsync(Domain.Entities.Core.Sys_TableInfo tableInfo, CancellationToken cancellationToken = default);
    Task<WebResponseContent> LoadTableInfoAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> SyncTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task<WebResponseContent> CreateModelAsync(Domain.Entities.Core.Sys_TableInfo tableInfo, CancellationToken cancellationToken = default);
    Task<WebResponseContent> CreateServicesAsync(CreateServicesRequest request, CancellationToken cancellationToken = default);
    Task<WebResponseContent> CreateVuePageAsync(CreateVuePageRequest request, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DelTreeAsync(int tableId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 设备/大屏服务
/// </summary>
public interface IDeviceService
{
    Task<PageGridData<Domain.Entities.Board.Device>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetChartDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 抛出告警请求（业务代码调用 IAlarmService.RaiseAsync）
/// </summary>
public class RaiseAlarmRequest
{
    /// <summary>报警码，需在 Sys_AlarmCode 或 appsettings Alarm:Codes 中配置</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>来源模块/服务名</summary>
    public string? Source { get; set; }

    /// <summary>关联设备名</summary>
    public string? DeviceName { get; set; }

    /// <summary>模板占位符，如 DeviceName → 堆垛机-01</summary>
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>扩展 JSON</summary>
    public string? ExtraData { get; set; }
}

/// <summary>
/// 告警服务：报警码解析、持久化、SignalR 推送
/// </summary>
public interface IAlarmService
{
    Task<PageGridData<Domain.Entities.Alarm.Sys_Alarm>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetActiveCountAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> RaiseAsync(RaiseAlarmRequest request, CancellationToken cancellationToken = default);
    Task<WebResponseContent> AcknowledgeAsync(int[] ids, CancellationToken cancellationToken = default);
    Task<WebResponseContent> ClearAsync(int[] ids, CancellationToken cancellationToken = default);
    Task<PageGridData<Domain.Entities.Alarm.Sys_AlarmCode>> GetAlarmCodesAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> SaveAlarmCodeAsync(Domain.Entities.Alarm.Sys_AlarmCode entity, CancellationToken cancellationToken = default);
    Task<WebResponseContent> DeleteAlarmCodeAsync(int[] ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// SignalR 推送的告警 DTO（前后端共用结构）
/// </summary>
public class AlarmPushDto
{
    /// <summary>告警 Id</summary>
    public int AlarmId { get; set; }

    /// <summary>报警码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>报警信息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>级别</summary>
    public int Level { get; set; }

    /// <summary>分类</summary>
    public string? Category { get; set; }

    /// <summary>来源</summary>
    public string? Source { get; set; }

    /// <summary>设备</summary>
    public string? DeviceName { get; set; }

    /// <summary>状态</summary>
    public int Status { get; set; }

    /// <summary>确认人</summary>
    public string? AckUserName { get; set; }

    /// <summary>确认时间</summary>
    public DateTime? AckDate { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateDate { get; set; }
}

/// <summary>
/// 告警 SignalR 推送接口
/// </summary>
public interface IAlarmPushService
{
    /// <summary>推送新告警</summary>
    Task PushNewAlarmAsync(AlarmPushDto alarm);

    /// <summary>推送告警状态变更</summary>
    Task PushAlarmUpdatedAsync(AlarmPushDto alarm);
}
