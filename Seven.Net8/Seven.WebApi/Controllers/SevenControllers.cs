using Microsoft.AspNetCore.Mvc;
using Seven.Application.Interfaces;
using Seven.Application.Models;
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

    /// <summary>获取角色详情（含权限）</summary>
    [HttpGet("getDetail/{id}")]
    public Task<WebResponseContent> GetDetail(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

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
    public Task<WebResponseContent> SavePermission([FromBody] SaveRolePermissionRequest request, CancellationToken cancellationToken) =>
        _service.SavePermissionAsync(request, cancellationToken);

    /// <summary>获取当前用户可分配的菜单权限树</summary>
    [HttpPost("getCurrentTreePermission")]
    public Task<WebResponseContent> GetCurrentTreePermission(CancellationToken cancellationToken) =>
        _service.GetCurrentTreePermissionAsync(cancellationToken);

    /// <summary>获取指定角色已分配权限</summary>
    [HttpPost("getUserTreePermission")]
    public Task<WebResponseContent> GetUserTreePermission([FromBody] RolePermissionQueryRequest request, CancellationToken cancellationToken) =>
        _service.GetUserTreePermissionAsync(request.RoleId, cancellationToken);
}

/// <summary>角色权限查询</summary>
public class RolePermissionQueryRequest
{
    /// <summary>角色 Id</summary>
    public int RoleId { get; set; }
}

/// <summary>保存权限请求（兼容旧格式）</summary>
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

    /// <summary>菜单管理：获取全部菜单扁平列表</summary>
    [HttpGet("getMenuList")]
    public Task<WebResponseContent> GetMenuList(CancellationToken cancellationToken) =>
        _service.GetMenuListAsync(cancellationToken);

    /// <summary>菜单管理：获取单条菜单详情</summary>
    [HttpPost("getTreeItem")]
    public Task<WebResponseContent> GetTreeItem([FromQuery] int menuId, CancellationToken cancellationToken) =>
        _service.GetTreeItemAsync(menuId, cancellationToken);

    /// <summary>获取当前用户菜单（动态路由）</summary>
    [HttpGet("getMenu")]
    public Task<WebResponseContent> GetMenu(CancellationToken cancellationToken) =>
        _service.GetCurrentUserMenuAsync(cancellationToken);

    /// <summary>新建或编辑菜单</summary>
    [HttpPost("save")]
    public Task<WebResponseContent> Save([FromBody] Sys_Menu entity, CancellationToken cancellationToken) =>
        _service.SaveAsync(entity, cancellationToken);

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
    public Task<WebResponseContent> Delete([FromQuery] int menuId, CancellationToken cancellationToken) =>
        _service.DeleteAsync(menuId, cancellationToken);
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

    /// <summary>代码生成器字典</summary>
    [HttpPost("GetBuilderDictionary")]
    public Task<WebResponseContent> GetBuilderDictionary(CancellationToken cancellationToken) =>
        _service.GetBuilderDictionaryAsync(cancellationToken);

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

    /// <summary>获取配置树</summary>
    [HttpPost("GetTableTree")]
    public Task<WebResponseContent> GetTableTree(CancellationToken cancellationToken) =>
        _service.GetTableTreeAsync(cancellationToken);

    /// <summary>加载/新建单表配置</summary>
    [HttpPost("LoadTableInfo")]
    public Task<WebResponseContent> LoadTableInfo([FromBody] LoadTableRequest request, CancellationToken cancellationToken) =>
        _service.LoadTableAsync(request, cancellationToken);

    /// <summary>加载全部表信息</summary>
    [HttpGet("loadTableInfo")]
    public Task<WebResponseContent> LoadAllTableInfo(CancellationToken cancellationToken) =>
        _service.LoadTableInfoAsync(cancellationToken);

    /// <summary>保存配置</summary>
    [HttpPost("Save")]
    public Task<WebResponseContent> Save([FromBody] Domain.Entities.Core.Sys_TableInfo tableInfo, CancellationToken cancellationToken) =>
        _service.SaveAsync(tableInfo, cancellationToken);

    /// <summary>同步表结构</summary>
    [HttpPost("syncTable")]
    public Task<WebResponseContent> SyncTable([FromBody] string tableName, CancellationToken cancellationToken) =>
        _service.SyncTableAsync(tableName, cancellationToken);

    /// <summary>生成 Entity</summary>
    [HttpPost("CreateModel")]
    public Task<WebResponseContent> CreateModel([FromBody] Domain.Entities.Core.Sys_TableInfo tableInfo, CancellationToken cancellationToken) =>
        _service.CreateModelAsync(tableInfo, cancellationToken);

    /// <summary>生成业务类</summary>
    [HttpPost("CreateServices")]
    public Task<WebResponseContent> CreateServices([FromBody] CreateServicesRequest request, CancellationToken cancellationToken) =>
        _service.CreateServicesAsync(request, cancellationToken);

    /// <summary>生成 Vue 页面</summary>
    [HttpPost("CreateVuePage")]
    public Task<WebResponseContent> CreateVuePage([FromBody] CreateVuePageRequest request, CancellationToken cancellationToken) =>
        _service.CreateVuePageAsync(request, cancellationToken);

    /// <summary>删除空树节点</summary>
    [HttpPost("delTree")]
    public Task<WebResponseContent> DelTree([FromBody] int tableId, CancellationToken cancellationToken) =>
        _service.DelTreeAsync(tableId, cancellationToken);
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

/// <summary>告警管理 API</summary>
[Route("api/Sys_Alarm")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class Sys_AlarmController : ControllerBase
{
    private readonly IAlarmService _service;

    /// <summary>构造函数</summary>
    public Sys_AlarmController(IAlarmService service) => _service = service;

    /// <summary>分页查询告警</summary>
    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>活跃告警数量</summary>
    [HttpGet("getActiveCount")]
    public Task<WebResponseContent> GetActiveCount(CancellationToken cancellationToken) =>
        _service.GetActiveCountAsync(cancellationToken);

    /// <summary>抛出告警（业务/测试调用）</summary>
    [HttpPost("raise")]
    public Task<WebResponseContent> Raise([FromBody] RaiseAlarmRequest request, CancellationToken cancellationToken) =>
        _service.RaiseAsync(request, cancellationToken);

    /// <summary>确认告警</summary>
    [HttpPost("acknowledge")]
    public Task<WebResponseContent> Acknowledge([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.AcknowledgeAsync(ids, cancellationToken);

    /// <summary>清除告警</summary>
    [HttpPost("clear")]
    public Task<WebResponseContent> Clear([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.ClearAsync(ids, cancellationToken);

    /// <summary>分页查询报警码配置</summary>
    [HttpPost("getAlarmCodes")]
    public async Task<WebResponseContent> GetAlarmCodes([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetAlarmCodesAsync(options, cancellationToken));

    /// <summary>保存报警码</summary>
    [HttpPost("saveAlarmCode")]
    public Task<WebResponseContent> SaveAlarmCode([FromBody] Domain.Entities.Alarm.Sys_AlarmCode entity, CancellationToken cancellationToken) =>
        _service.SaveAlarmCodeAsync(entity, cancellationToken);

    /// <summary>删除报警码</summary>
    [HttpPost("delAlarmCode")]
    public Task<WebResponseContent> DeleteAlarmCode([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAlarmCodeAsync(ids, cancellationToken);
}

/// <summary>消息队列 API</summary>
[Route("api/MessageQueue")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class MessageQueueController : ControllerBase
{
    private readonly IMessageQueueService _service;

    /// <summary>构造函数</summary>
    public MessageQueueController(IMessageQueueService service) => _service = service;

    /// <summary>获取 MQ 状态</summary>
    [HttpGet("status")]
    public Task<WebResponseContent> GetStatus(CancellationToken cancellationToken) =>
        _service.GetStatusAsync(cancellationToken);

    /// <summary>发布抛警命令到队列（WCS 入站测试）</summary>
    [HttpPost("publishRaiseAlarm")]
    public Task<WebResponseContent> PublishRaiseAlarm([FromBody] Seven.Application.Messaging.RaiseAlarmCommand command, CancellationToken cancellationToken) =>
        _service.PublishRaiseAlarmAsync(command, cancellationToken);
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
