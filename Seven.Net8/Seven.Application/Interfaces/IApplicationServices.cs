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
    Task<WebResponseContent> SavePermissionAsync(int roleId, int[] menuIds, Dictionary<int, string> authValues, CancellationToken cancellationToken = default);
}

/// <summary>
/// 菜单管理服务
/// </summary>
public interface ISysMenuService
{
    Task<WebResponseContent> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetMenuByRoleAsync(int roleId, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetCurrentUserMenuAsync(CancellationToken cancellationToken = default);
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
    Task<WebResponseContent> LoadTableInfoAsync(CancellationToken cancellationToken = default);
    Task<WebResponseContent> SyncTableAsync(string tableName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 设备/大屏服务
/// </summary>
public interface IDeviceService
{
    Task<PageGridData<Domain.Entities.Board.Device>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default);
    Task<WebResponseContent> GetChartDataAsync(CancellationToken cancellationToken = default);
}
