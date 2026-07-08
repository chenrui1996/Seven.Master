using Microsoft.AspNetCore.Mvc;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.System;

namespace Seven.WebApi.Controllers;

/// <summary>
/// 用户认证 API
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>构造函数</summary>
    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>用户登录</summary>
    [HttpPost("login")]
    public Task<WebResponseContent> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        _authService.LoginAsync(request.UserName, request.Password, request.VerificationCode, request.Uuid, cancellationToken);

    /// <summary>刷新 Token</summary>
    [HttpPost("refresh")]
    public Task<WebResponseContent> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

    /// <summary>退出登录</summary>
    [HttpPost("logout")]
    public Task<WebResponseContent> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        _authService.LogoutAsync(request.RefreshToken, cancellationToken);
}

/// <summary>登录请求体</summary>
public class LoginRequest
{
    /// <summary>用户名</summary>
    public string UserName { get; set; } = string.Empty;
    /// <summary>密码</summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>验证码</summary>
    public string? VerificationCode { get; set; }
    /// <summary>验证码 Key</summary>
    public string? Uuid { get; set; }
}

/// <summary>Refresh Token 请求体</summary>
public class RefreshRequest
{
    /// <summary>Refresh Token</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 用户管理 API（兼容 Legrand 路由 Sys_User）
/// </summary>
[Route("api/Sys_User")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_UserController : ControllerBase
{
    private readonly ISysUserService _service;
    private readonly IAuthService _authService;

    /// <summary>构造函数</summary>
    public Sys_UserController(ISysUserService service, IAuthService authService)
    {
        _service = service;
        _authService = authService;
    }

    /// <summary>分页查询用户</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>获取用户详情</summary>
    [HttpGet("getDetail/{id}")]
    public Task<WebResponseContent> GetDetail(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    /// <summary>新增用户</summary>
    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Sys_User entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    /// <summary>更新用户</summary>
    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Sys_User entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除用户</summary>
    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAsync(ids, cancellationToken);

    /// <summary>修改密码</summary>
    [HttpPost("modifyPwd")]
    public Task<WebResponseContent> ModifyPwd([FromBody] ModifyPwdRequest request, CancellationToken cancellationToken) =>
        _authService.ChangePasswordAsync(request.UserId, request.OldPwd, request.NewPwd, cancellationToken);
}

/// <summary>改密请求</summary>
public class ModifyPwdRequest
{
    /// <summary>用户 Id</summary>
    public int UserId { get; set; }
    /// <summary>旧密码</summary>
    public string OldPwd { get; set; } = string.Empty;
    /// <summary>新密码</summary>
    public string NewPwd { get; set; } = string.Empty;
}

/// <summary>角色管理 API</summary>
[Route("api/Sys_Role")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_RoleController : ControllerBase
{
    private readonly ISysRoleService _service;

    /// <summary>构造函数</summary>
    public Sys_RoleController(ISysRoleService service) => _service = service;

    /// <summary>分页查询</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>新增</summary>
    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Sys_Role entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Sys_Role entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAsync(ids, cancellationToken);

    /// <summary>保存权限</summary>
    [HttpPost("savePermission")]
    public Task<WebResponseContent> SavePermission([FromBody] SavePermissionRequest request, CancellationToken cancellationToken) =>
        _service.SavePermissionAsync(request.RoleId, request.MenuIds, request.AuthValues, cancellationToken);
}

/// <summary>保存权限请求</summary>
public class SavePermissionRequest
{
    /// <summary>角色 Id</summary>
    public int RoleId { get; set; }
    /// <summary>菜单 Id 列表</summary>
    public int[] MenuIds { get; set; } = [];
    /// <summary>菜单按钮权限</summary>
    public Dictionary<int, string> AuthValues { get; set; } = [];
}

/// <summary>菜单管理 API</summary>
[Route("api/Sys_Menu")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_MenuController : ControllerBase
{
    private readonly ISysMenuService _service;

    /// <summary>构造函数</summary>
    public Sys_MenuController(ISysMenuService service) => _service = service;

    /// <summary>获取菜单树</summary>
    [HttpGet("getTree")]
    public Task<WebResponseContent> GetTree(CancellationToken cancellationToken) =>
        _service.GetTreeAsync(cancellationToken);

    /// <summary>获取当前用户菜单（动态路由）</summary>
    [HttpGet("getMenu")]
    public Task<WebResponseContent> GetMenu(CancellationToken cancellationToken) =>
        _service.GetCurrentUserMenuAsync(cancellationToken);

    /// <summary>新增菜单</summary>
    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Sys_Menu entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    /// <summary>更新菜单</summary>
    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Sys_Menu entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除菜单</summary>
    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}

/// <summary>部门管理 API</summary>
[Route("api/Sys_Department")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_DepartmentController : ControllerBase
{
    private readonly ISysDepartmentService _service;

    /// <summary>构造函数</summary>
    public Sys_DepartmentController(ISysDepartmentService service) => _service = service;

    /// <summary>获取部门树</summary>
    [HttpGet("getTree")]
    public Task<WebResponseContent> GetTree(CancellationToken cancellationToken) =>
        _service.GetTreeAsync(cancellationToken);

    /// <summary>新增</summary>
    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Sys_Department entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Sys_Department entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}

/// <summary>字典管理 API</summary>
[Route("api/Sys_Dictionary")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_DictionaryController : ControllerBase
{
    private readonly ISysDictionaryService _service;

    /// <summary>构造函数</summary>
    public Sys_DictionaryController(ISysDictionaryService service) => _service = service;

    /// <summary>分页查询</summary>
    [HttpPost("getPageData")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        _service.GetPageDataAsync(options, cancellationToken);

    /// <summary>获取前端字典数据</summary>
    [HttpPost("getVueDictionary")]
    public Task<WebResponseContent> GetVueDictionary([FromBody] string[] dicNos, CancellationToken cancellationToken) =>
        _service.GetVueDictionaryAsync(dicNos, cancellationToken);

    /// <summary>新增</summary>
    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Sys_Dictionary entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Sys_Dictionary entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAsync(ids, cancellationToken);
}

/// <summary>日志管理 API</summary>
[Route("api/Sys_Log")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_LogController : ControllerBase
{
    private readonly ISysLogService _service;

    /// <summary>构造函数</summary>
    public Sys_LogController(ISysLogService service) => _service = service;

    /// <summary>分页查询日志</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));
}

/// <summary>工作流 API</summary>
[Route("api/Sys_WorkFlow")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_WorkFlowController : ControllerBase
{
    private readonly IWorkFlowService _service;

    /// <summary>构造函数</summary>
    public Sys_WorkFlowController(IWorkFlowService service) => _service = service;

    /// <summary>提交审批</summary>
    [HttpPost("submit")]
    public Task<WebResponseContent> Submit([FromBody] WorkFlowSubmitRequest request, CancellationToken cancellationToken) =>
        _service.SubmitAsync(request.TableName, request.TableKey, cancellationToken);

    /// <summary>审批</summary>
    [HttpPost("audit")]
    public Task<WebResponseContent> Audit([FromBody] WorkFlowAuditRequest request, CancellationToken cancellationToken) =>
        _service.AuditAsync(request.WorkFlowTableId, request.AuditStatus, request.Remark, cancellationToken);

    /// <summary>分页查询实例</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));
}

/// <summary>提交审批请求</summary>
public class WorkFlowSubmitRequest
{
    /// <summary>业务表名</summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>业务主键</summary>
    public string TableKey { get; set; } = string.Empty;
}

/// <summary>审批请求</summary>
public class WorkFlowAuditRequest
{
    /// <summary>实例 Id</summary>
    public int WorkFlowTableId { get; set; }
    /// <summary>审批状态</summary>
    public int AuditStatus { get; set; }
    /// <summary>审批意见</summary>
    public string? Remark { get; set; }
}

/// <summary>代码生成器 API</summary>
[Route("api/Builder")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class BuilderController : ControllerBase
{
    private readonly IBuilderService _service;

    /// <summary>构造函数</summary>
    public BuilderController(IBuilderService service) => _service = service;

    /// <summary>加载表信息</summary>
    [HttpGet("loadTableInfo")]
    public Task<WebResponseContent> LoadTableInfo(CancellationToken cancellationToken) =>
        _service.LoadTableInfoAsync(cancellationToken);

    /// <summary>同步表结构</summary>
    [HttpPost("syncTable")]
    public Task<WebResponseContent> SyncTable([FromBody] string tableName, CancellationToken cancellationToken) =>
        _service.SyncTableAsync(tableName, cancellationToken);
}

/// <summary>设备/大屏 API</summary>
[Route("api/Device")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _service;

    /// <summary>构造函数</summary>
    public DeviceController(IDeviceService service) => _service = service;

    /// <summary>分页查询设备</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>获取图表数据</summary>
    [HttpGet("getChartData")]
    public Task<WebResponseContent> GetChartData(CancellationToken cancellationToken) =>
        _service.GetChartDataAsync(cancellationToken);
}

/// <summary>文件上传 API</summary>
[Route("api/File")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class FileController : ControllerBase
{
    private readonly IFileStorageService _storage;

    /// <summary>构造函数</summary>
    public FileController(IFileStorageService storage) => _storage = storage;

    /// <summary>上传文件</summary>
    [HttpPost("upload")]
    public async Task<WebResponseContent> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return WebResponseContent.Error("文件不能为空");
        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return WebResponseContent.Ok("上传成功", new { url });
    }
}

/// <summary>健康检查</summary>
[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>健康状态</summary>
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public WebResponseContent Get() => WebResponseContent.Ok("Seven API is running");
}
