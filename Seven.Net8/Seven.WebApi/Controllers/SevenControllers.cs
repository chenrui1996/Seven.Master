using Microsoft.AspNetCore.Mvc;
using Seven.Application.Interfaces;
using Seven.Application.Models;
using Seven.Domain.Common;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Security;
using Seven.WebApi.Hubs;

namespace Seven.WebApi.Controllers;

/// <summary>
/// 用户认证 API
/// </summary>
[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "system")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>构造函数</summary>
    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>用户登录</summary>
    [HttpPost("login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public Task<WebResponseContent> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    ) =>
        _authService.LoginAsync(
            request.UserName,
            request.Password,
            request.VerificationCode,
            request.Uuid,
            cancellationToken
        );

    /// <summary>刷新 Token</summary>
    [HttpPost("refresh")]
    public Task<WebResponseContent> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken
    ) => _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

    /// <summary>退出登录</summary>
    [HttpPost("logout")]
    public Task<WebResponseContent> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken
    ) => _authService.LogoutAsync(request.RefreshToken, cancellationToken);

    /// <summary>当前用户权限码（改角色授权后可刷新前端）</summary>
    [HttpGet("permissions")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public Task<WebResponseContent> Permissions(CancellationToken cancellationToken) =>
        _authService.GetMyPermissionsAsync(cancellationToken);
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
[ApiExplorerSettings(GroupName = "system")]
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
    [Permission("Sys_User.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>获取用户详情</summary>
    [HttpGet("getDetail/{id}")]
    [Permission("Sys_User.Search")]
    public Task<WebResponseContent> GetDetail(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    /// <summary>新增用户</summary>
    [HttpPost("add")]
    [Permission("Sys_User.Add")]
    public Task<WebResponseContent> Add(
        [FromBody] Sys_User entity,
        CancellationToken cancellationToken
    ) => _service.AddAsync(entity, cancellationToken);

    /// <summary>更新用户</summary>
    [HttpPost("update")]
    [Permission("Sys_User.Update")]
    public Task<WebResponseContent> Update(
        [FromBody] Sys_User entity,
        CancellationToken cancellationToken
    ) => _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除用户</summary>
    [HttpPost("del")]
    [Permission("Sys_User.Delete")]
    public Task<WebResponseContent> Delete(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.DeleteAsync(ids, cancellationToken);

    /// <summary>修改密码</summary>
    [HttpPost("modifyPwd")]
    [Permission("Sys_User.ModifyPwd")]
    public Task<WebResponseContent> ModifyPwd(
        [FromBody] ModifyPwdRequest request,
        CancellationToken cancellationToken
    ) =>
        _authService.ChangePasswordAsync(
            request.UserId,
            request.OldPwd,
            request.NewPwd,
            cancellationToken
        );
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
[ApiExplorerSettings(GroupName = "system")]
public class Sys_RoleController : ControllerBase
{
    private readonly ISysRoleService _service;

    /// <summary>构造函数</summary>
    public Sys_RoleController(ISysRoleService service) => _service = service;

    /// <summary>分页查询</summary>
    [HttpPost("getPageData")]
    [Permission("Sys_Role.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>获取角色详情（含权限）</summary>
    [HttpGet("getDetail/{id}")]
    [Permission("Sys_Role.Search")]
    public Task<WebResponseContent> GetDetail(int id, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(id, cancellationToken);

    /// <summary>新增</summary>
    [HttpPost("add")]
    [Permission("Sys_Role.Add")]
    public Task<WebResponseContent> Add(
        [FromBody] Sys_Role entity,
        CancellationToken cancellationToken
    ) => _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    [Permission("Sys_Role.Update")]
    public Task<WebResponseContent> Update(
        [FromBody] Sys_Role entity,
        CancellationToken cancellationToken
    ) => _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    [Permission("Sys_Role.Delete")]
    public Task<WebResponseContent> Delete(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.DeleteAsync(ids, cancellationToken);

    /// <summary>保存权限</summary>
    [HttpPost("savePermission")]
    [Permission("Sys_Role.SavePermission")]
    public Task<WebResponseContent> SavePermission(
        [FromBody] SaveRolePermissionRequest request,
        CancellationToken cancellationToken
    ) => _service.SavePermissionAsync(request, cancellationToken);

    /// <summary>获取当前用户可分配的菜单权限树</summary>
    [HttpPost("getCurrentTreePermission")]
    [Permission("Sys_Role.Search")]
    public Task<WebResponseContent> GetCurrentTreePermission(CancellationToken cancellationToken) =>
        _service.GetCurrentTreePermissionAsync(cancellationToken);

    /// <summary>获取指定角色已分配权限</summary>
    [HttpPost("getUserTreePermission")]
    [Permission("Sys_Role.Search")]
    public Task<WebResponseContent> GetUserTreePermission(
        [FromBody] RolePermissionQueryRequest request,
        CancellationToken cancellationToken
    ) => _service.GetUserTreePermissionAsync(request.RoleId, cancellationToken);
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
[ApiExplorerSettings(GroupName = "system")]
public class Sys_MenuController : ControllerBase
{
    private readonly ISysMenuService _service;

    /// <summary>构造函数</summary>
    public Sys_MenuController(ISysMenuService service) => _service = service;

    /// <summary>获取菜单树</summary>
    [HttpGet("getTree")]
    [Permission("Sys_Menu.Search")]
    public Task<WebResponseContent> GetTree(CancellationToken cancellationToken) =>
        _service.GetTreeAsync(cancellationToken);

    /// <summary>菜单管理：获取全部菜单扁平列表</summary>
    [HttpGet("getMenuList")]
    [Permission("Sys_Menu.Search")]
    public Task<WebResponseContent> GetMenuList(CancellationToken cancellationToken) =>
        _service.GetMenuListAsync(cancellationToken);

    /// <summary>菜单管理：获取单条菜单详情</summary>
    [HttpPost("getTreeItem")]
    [Permission("Sys_Menu.Search")]
    public Task<WebResponseContent> GetTreeItem(
        [FromQuery] int menuId,
        CancellationToken cancellationToken
    ) => _service.GetTreeItemAsync(menuId, cancellationToken);

    /// <summary>获取当前用户菜单（动态路由）</summary>
    [HttpGet("getMenu")]
    public Task<WebResponseContent> GetMenu(CancellationToken cancellationToken) =>
        _service.GetCurrentUserMenuAsync(cancellationToken);

    /// <summary>新建或编辑菜单</summary>
    [HttpPost("save")]
    [Permission("Sys_Menu.Update")]
    public Task<WebResponseContent> Save(
        [FromBody] Sys_Menu entity,
        CancellationToken cancellationToken
    ) => _service.SaveAsync(entity, cancellationToken);

    /// <summary>新增菜单</summary>
    [HttpPost("add")]
    [Permission("Sys_Menu.Add")]
    public Task<WebResponseContent> Add(
        [FromBody] Sys_Menu entity,
        CancellationToken cancellationToken
    ) => _service.AddAsync(entity, cancellationToken);

    /// <summary>更新菜单</summary>
    [HttpPost("update")]
    [Permission("Sys_Menu.Update")]
    public Task<WebResponseContent> Update(
        [FromBody] Sys_Menu entity,
        CancellationToken cancellationToken
    ) => _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除菜单</summary>
    [HttpPost("del")]
    [Permission("Sys_Menu.Delete")]
    public Task<WebResponseContent> Delete(
        [FromQuery] int menuId,
        CancellationToken cancellationToken
    ) => _service.DeleteAsync(menuId, cancellationToken);
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
    [Permission("Sys_Department.Search")]
    public Task<WebResponseContent> GetTree(CancellationToken cancellationToken) =>
        _service.GetTreeAsync(cancellationToken);

    /// <summary>新增</summary>
    [HttpPost("add")]
    [Permission("Sys_Department.Add")]
    public Task<WebResponseContent> Add(
        [FromBody] Sys_Department entity,
        CancellationToken cancellationToken
    ) => _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    [Permission("Sys_Department.Update")]
    public Task<WebResponseContent> Update(
        [FromBody] Sys_Department entity,
        CancellationToken cancellationToken
    ) => _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    [Permission("Sys_Department.Delete")]
    public Task<WebResponseContent> Delete(
        [FromBody] int id,
        CancellationToken cancellationToken
    ) => _service.DeleteAsync(id, cancellationToken);
}

/// <summary>字典管理 API</summary>
[Route("api/Sys_Dictionary")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[ApiExplorerSettings(GroupName = "system")]
public class Sys_DictionaryController : ControllerBase
{
    private readonly ISysDictionaryService _service;

    /// <summary>构造函数</summary>
    public Sys_DictionaryController(ISysDictionaryService service) => _service = service;

    /// <summary>分页查询</summary>
    [HttpPost("getPageData")]
    [Permission("Sys_Dictionary.Search")]
    public Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => _service.GetPageDataAsync(options, cancellationToken);

    /// <summary>获取前端字典数据</summary>
    [HttpPost("getVueDictionary")]
    [Permission("Sys_Dictionary.Search")]
    public Task<WebResponseContent> GetVueDictionary(
        [FromBody] string[] dicNos,
        CancellationToken cancellationToken
    ) => _service.GetVueDictionaryAsync(dicNos, cancellationToken);

    /// <summary>代码生成器字典</summary>
    [HttpPost("GetBuilderDictionary")]
    [Permission("Builder.Search")]
    public Task<WebResponseContent> GetBuilderDictionary(CancellationToken cancellationToken) =>
        _service.GetBuilderDictionaryAsync(cancellationToken);

    /// <summary>新增</summary>
    [HttpPost("add")]
    [Permission("Sys_Dictionary.Add")]
    public Task<WebResponseContent> Add(
        [FromBody] Sys_Dictionary entity,
        CancellationToken cancellationToken
    ) => _service.AddAsync(entity, cancellationToken);

    /// <summary>更新</summary>
    [HttpPost("update")]
    [Permission("Sys_Dictionary.Update")]
    public Task<WebResponseContent> Update(
        [FromBody] Sys_Dictionary entity,
        CancellationToken cancellationToken
    ) => _service.UpdateAsync(entity, cancellationToken);

    /// <summary>删除</summary>
    [HttpPost("del")]
    [Permission("Sys_Dictionary.Delete")]
    public Task<WebResponseContent> Delete(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.DeleteAsync(ids, cancellationToken);
}

/// <summary>日志管理 API</summary>
[Route("api/Sys_Log")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[ApiExplorerSettings(GroupName = "system")]
public class Sys_LogController : ControllerBase
{
    private readonly ISysLogService _service;

    /// <summary>构造函数</summary>
    public Sys_LogController(ISysLogService service) => _service = service;

    /// <summary>分页查询日志</summary>
    [HttpPost("getPageData")]
    [Permission("Sys_Log.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));
}

/// <summary>工作流 API</summary>
[Route("api/Sys_WorkFlow")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("WorkFlow")]
[ApiExplorerSettings(GroupName = "workflow")]
public class Sys_WorkFlowController : ControllerBase
{
    private readonly IWorkFlowService _service;

    /// <summary>构造函数</summary>
    public Sys_WorkFlowController(IWorkFlowService service) => _service = service;

    [HttpPost("listDefinitions")]
    [Permission("Sys_WorkFlow.Search")]
    public Task<WebResponseContent> ListDefinitions(CancellationToken cancellationToken) =>
        _service.ListDefinitionsAsync(cancellationToken);

    [HttpPost("getDefinition")]
    [Permission("Sys_WorkFlow.Search")]
    public Task<WebResponseContent> GetDefinition(
        [FromBody] WorkFlowIdRequest request,
        CancellationToken cancellationToken
    ) => _service.GetDefinitionAsync(request.WorkFlowId, cancellationToken);

    [HttpPost("saveDefinition")]
    [Permission("Sys_WorkFlow.Update")]
    public Task<WebResponseContent> SaveDefinition(
        [FromBody] WorkFlowDefinitionRequest request,
        CancellationToken cancellationToken
    ) => _service.SaveDefinitionAsync(request, cancellationToken);

    [HttpPost("delete")]
    [Permission("Sys_WorkFlow.Delete")]
    public Task<WebResponseContent> Delete(
        [FromBody] IdsRequest request,
        CancellationToken cancellationToken
    ) => _service.DeleteDefinitionsAsync(request.Ids ?? [], cancellationToken);

    /// <summary>提交审批</summary>
    [HttpPost("submit")]
    [Permission("Sys_WorkFlow.Search")]
    public Task<WebResponseContent> Submit(
        [FromBody] WorkFlowSubmitRequest request,
        CancellationToken cancellationToken
    ) => _service.SubmitAsync(request.TableName, request.TableKey, cancellationToken);

    /// <summary>审批</summary>
    [HttpPost("audit")]
    [Permission("Sys_WorkFlow.Audit")]
    public Task<WebResponseContent> Audit(
        [FromBody] WorkFlowAuditRequest request,
        CancellationToken cancellationToken
    ) =>
        _service.AuditAsync(
            request.WorkFlowTableId,
            request.AuditStatus,
            request.Remark,
            cancellationToken
        );

    /// <summary>分页查询实例</summary>
    [HttpPost("getPageData")]
    [Permission("Sys_WorkFlow.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));
}

/// <summary>工作流实例 API（待办/已办）</summary>
[Route("api/Sys_WorkFlowTable")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("WorkFlow")]
[ApiExplorerSettings(GroupName = "workflow")]
public class Sys_WorkFlowTableController : ControllerBase
{
    private readonly IWorkFlowService _service;

    public Sys_WorkFlowTableController(IWorkFlowService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("Sys_WorkFlowTable.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));
}

public class WorkFlowIdRequest
{
    public int WorkFlowId { get; set; }
}

public class IdsRequest
{
    public int[]? Ids { get; set; }
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
[RequiresFeature("Builder")]
[ApiExplorerSettings(GroupName = "builder")]
public class BuilderController : ControllerBase
{
    private readonly IBuilderService _service;

    /// <summary>构造函数</summary>
    public BuilderController(IBuilderService service) => _service = service;

    /// <summary>获取配置树</summary>
    [HttpPost("GetTableTree")]
    [Permission("Builder.Search")]
    public Task<WebResponseContent> GetTableTree(CancellationToken cancellationToken) =>
        _service.GetTableTreeAsync(cancellationToken);

    /// <summary>加载/新建单表配置</summary>
    [HttpPost("LoadTableInfo")]
    [Permission("Builder.Search")]
    public Task<WebResponseContent> LoadTableInfo(
        [FromBody] LoadTableRequest request,
        CancellationToken cancellationToken
    ) => _service.LoadTableAsync(request, cancellationToken);

    /// <summary>加载全部表信息</summary>
    [HttpGet("loadTableInfo")]
    [Permission("Builder.Search")]
    public Task<WebResponseContent> LoadAllTableInfo(CancellationToken cancellationToken) =>
        _service.LoadTableInfoAsync(cancellationToken);

    /// <summary>保存配置</summary>
    [HttpPost("Save")]
    [Permission("Builder.Update")]
    public Task<WebResponseContent> Save(
        [FromBody] Domain.Entities.Core.Sys_TableInfo tableInfo,
        CancellationToken cancellationToken
    ) => _service.SaveAsync(tableInfo, cancellationToken);

    /// <summary>同步表结构</summary>
    [HttpPost("syncTable")]
    [Permission("Builder.Update")]
    public Task<WebResponseContent> SyncTable(
        [FromQuery] string tableName,
        CancellationToken cancellationToken
    ) => _service.SyncTableAsync(tableName, cancellationToken);

    /// <summary>生成 Entity</summary>
    [HttpPost("CreateModel")]
    [Permission("Builder.Update")]
    public Task<WebResponseContent> CreateModel(
        [FromBody] Domain.Entities.Core.Sys_TableInfo tableInfo,
        CancellationToken cancellationToken
    ) => _service.CreateModelAsync(tableInfo, cancellationToken);

    /// <summary>生成业务类</summary>
    [HttpPost("CreateServices")]
    [Permission("Builder.Update")]
    public Task<WebResponseContent> CreateServices(
        [FromBody] CreateServicesRequest request,
        CancellationToken cancellationToken
    ) => _service.CreateServicesAsync(request, cancellationToken);

    /// <summary>生成 Vue 页面</summary>
    [HttpPost("CreateVuePage")]
    [Permission("Builder.Update")]
    public Task<WebResponseContent> CreateVuePage(
        [FromBody] CreateVuePageRequest request,
        CancellationToken cancellationToken
    ) => _service.CreateVuePageAsync(request, cancellationToken);

    /// <summary>删除空树节点</summary>
    [HttpPost("delTree")]
    public Task<WebResponseContent> DelTree(
        [FromQuery] int tableId,
        CancellationToken cancellationToken
    ) => _service.DelTreeAsync(tableId, cancellationToken);

    /// <summary>获取主子表关系</summary>
    [HttpGet("GetTableDetails")]
    public Task<WebResponseContent> GetTableDetails(
        [FromQuery] string parentTable,
        CancellationToken cancellationToken
    ) => _service.GetTableDetailsAsync(parentTable, cancellationToken);

    /// <summary>保存主子表关系</summary>
    [HttpPost("SaveTableDetails")]
    public Task<WebResponseContent> SaveTableDetails(
        [FromBody] SaveTableDetailsRequest request,
        CancellationToken cancellationToken
    ) => _service.SaveTableDetailsAsync(request, cancellationToken);

    /// <summary>扫描外键候选并写入 Sys_TableDetail</summary>
    [HttpPost("ScanForeignKeys")]
    public Task<WebResponseContent> ScanForeignKeys(
        [FromBody] ScanForeignKeysRequest request,
        CancellationToken cancellationToken
    ) => _service.ScanForeignKeysAsync(request, cancellationToken);
}

/// <summary>告警管理 API</summary>
[Route("api/Sys_Alarm")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("Alarm")]
[ApiExplorerSettings(GroupName = "ops")]
public class Sys_AlarmController : ControllerBase
{
    private readonly IAlarmService _service;

    /// <summary>构造函数</summary>
    public Sys_AlarmController(IAlarmService service) => _service = service;

    /// <summary>分页查询告警</summary>
    [HttpPost("getPageData")]
    [Permission("Sys_Alarm.Search")]
    public async Task<WebResponseContent> GetPageData(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    /// <summary>活跃告警数量</summary>
    [HttpGet("getActiveCount")]
    public Task<WebResponseContent> GetActiveCount(CancellationToken cancellationToken) =>
        _service.GetActiveCountAsync(cancellationToken);

    /// <summary>抛出告警（业务/测试调用）</summary>
    [HttpPost("raise")]
    public Task<WebResponseContent> Raise(
        [FromBody] RaiseAlarmRequest request,
        CancellationToken cancellationToken
    ) => _service.RaiseAsync(request, cancellationToken);

    /// <summary>确认告警</summary>
    [HttpPost("acknowledge")]
    public Task<WebResponseContent> Acknowledge(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.AcknowledgeAsync(ids, cancellationToken);

    /// <summary>清除告警</summary>
    [HttpPost("clear")]
    public Task<WebResponseContent> Clear(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.ClearAsync(ids, cancellationToken);

    /// <summary>分页查询报警码配置</summary>
    [HttpPost("getAlarmCodes")]
    public async Task<WebResponseContent> GetAlarmCodes(
        [FromBody] PageDataOptions options,
        CancellationToken cancellationToken
    ) => WebResponseContent.Ok(data: await _service.GetAlarmCodesAsync(options, cancellationToken));

    /// <summary>保存报警码</summary>
    [HttpPost("saveAlarmCode")]
    public Task<WebResponseContent> SaveAlarmCode(
        [FromBody] Domain.Entities.Alarm.Sys_AlarmCode entity,
        CancellationToken cancellationToken
    ) => _service.SaveAlarmCodeAsync(entity, cancellationToken);

    /// <summary>删除报警码</summary>
    [HttpPost("delAlarmCode")]
    public Task<WebResponseContent> DeleteAlarmCode(
        [FromBody] int[] ids,
        CancellationToken cancellationToken
    ) => _service.DeleteAlarmCodeAsync(ids, cancellationToken);
}

/// <summary>消息队列 API</summary>
[Route("api/MessageQueue")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[ApiExplorerSettings(GroupName = "ops")]
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
    public Task<WebResponseContent> PublishRaiseAlarm(
        [FromBody] Seven.Application.Messaging.RaiseAlarmCommand command,
        CancellationToken cancellationToken
    ) => _service.PublishRaiseAlarmAsync(command, cancellationToken);
}

/// <summary>文件上传 API</summary>
[Route("api/File")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[ApiExplorerSettings(GroupName = "ops")]
public class FileController : ControllerBase
{
    private readonly IFileStorageService _storage;

    /// <summary>构造函数</summary>
    public FileController(IFileStorageService storage) => _storage = storage;

    /// <summary>上传文件</summary>
    [HttpPost("upload")]
    public async Task<WebResponseContent> Upload(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        if (file == null || file.Length == 0)
            return WebResponseContent.Error("文件不能为空");
        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken
        );
        return WebResponseContent.Ok("上传成功", new { url });
    }
}

/// <summary>健康检查</summary>
[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "ops")]
public class HealthController : ControllerBase
{
    /// <summary>健康状态</summary>
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public WebResponseContent Get() => WebResponseContent.Ok("Seven API is running");
}

/// <summary>验证码</summary>
[Route("api/Captcha")]
[ApiController]
[RequiresFeature("Captcha")]
[ApiExplorerSettings(GroupName = "system")]
public class CaptchaController : ControllerBase
{
    private readonly Seven.Infrastructure.Security.ICaptchaService _captcha;

    public CaptchaController(Seven.Infrastructure.Security.ICaptchaService captcha) => _captcha = captcha;

    [HttpGet("create")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public Task<WebResponseContent> Create(CancellationToken cancellationToken) =>
        _captcha.CreateAsync(cancellationToken);
}

/// <summary>定时任务</summary>
[Route("api/Sys_QuartzOptions")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("Quartz")]
[ApiExplorerSettings(GroupName = "system")]
public class Sys_QuartzOptionsController : ControllerBase
{
    private readonly Seven.Infrastructure.Quartz.IQuartzJobService _service;

    public Sys_QuartzOptionsController(Seven.Infrastructure.Quartz.IQuartzJobService service) => _service = service;

    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, ct));

    [HttpPost("save")]
    public Task<WebResponseContent> Save([FromBody] Seven.Domain.Entities.Quartz.Sys_QuartzOptions entity, CancellationToken ct) =>
        _service.SaveAsync(entity, ct);

    [HttpPost("delete")]
    public Task<WebResponseContent> Delete([FromBody] IdsRequest request, CancellationToken ct) =>
        _service.DeleteAsync(request.Ids ?? [], ct);

    [HttpPost("setEnable")]
    public Task<WebResponseContent> SetEnable([FromBody] QuartzEnableRequest request, CancellationToken ct) =>
        _service.SetEnableAsync(request.Id, request.Enable, ct);

    [HttpPost("enable")]
    public Task<WebResponseContent> Enable([FromBody] QuartzIdRequest request, CancellationToken ct) =>
        _service.SetEnableAsync(request.Id, true, ct);

    [HttpPost("disable")]
    public Task<WebResponseContent> Disable([FromBody] QuartzIdRequest request, CancellationToken ct) =>
        _service.SetEnableAsync(request.Id, false, ct);

    [HttpPost("runNow")]
    public Task<WebResponseContent> RunNow([FromBody] QuartzIdRequest request, CancellationToken ct) =>
        _service.RunNowAsync(request.Id, ct);
}

/// <summary>定时任务执行日志</summary>
[Route("api/Sys_QuartzLog")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("Quartz")]
[ApiExplorerSettings(GroupName = "system")]
public class Sys_QuartzLogController : ControllerBase
{
    private readonly Seven.Infrastructure.Quartz.IQuartzJobService _service;

    public Sys_QuartzLogController(Seven.Infrastructure.Quartz.IQuartzJobService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("Sys_QuartzLog.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetLogPageDataAsync(options, ct));
}

public class QuartzEnableRequest
{
    public int Id { get; set; }
    public bool Enable { get; set; }
}

public class QuartzIdRequest
{
    public int Id { get; set; }
}

/// <summary>邮件</summary>
[Route("api/Mail")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("Mail")]
[ApiExplorerSettings(GroupName = "ops")]
public class MailController : ControllerBase
{
    private readonly Seven.Infrastructure.Mail.IEmailService _mail;

    public MailController(Seven.Infrastructure.Mail.IEmailService mail) => _mail = mail;

    [HttpPost("send")]
    public Task<WebResponseContent> Send([FromBody] MailSendRequest request, CancellationToken ct) =>
        _mail.SendAsync(request.To, request.Subject, request.HtmlBody, ct);
}

public class MailSendRequest
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
}

/// <summary>系统通知 / 在线人数</summary>
[Route("api/Notify")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequiresFeature("SignalR")]
[ApiExplorerSettings(GroupName = "ops")]
public class NotifyController : ControllerBase
{
    private readonly IMessagePushService _push;

    public NotifyController(IMessagePushService push) => _push = push;

    [HttpGet("online")]
    public WebResponseContent Online() =>
        WebResponseContent.Ok(data: new { count = Seven.WebApi.Hubs.MessageHub.OnlineCount });

    [HttpPost("broadcast")]
    public async Task<WebResponseContent> Broadcast([FromBody] NotifyRequest request)
    {
        await _push.PushSystemNotifyAsync(request.Title, request.Content);
        return WebResponseContent.Ok("已推送");
    }
}

public class NotifyRequest
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}

/// <summary>运行时功能开关（供前端隐藏菜单/入口）</summary>
[Route("api/config")]
[ApiController]
[ApiExplorerSettings(GroupName = "system")]
public class ConfigController : ControllerBase
{
    private readonly Microsoft.Extensions.Options.IOptions<Seven.Infrastructure.Configuration.FeatureOptions> _features;

    public ConfigController(
        Microsoft.Extensions.Options.IOptions<Seven.Infrastructure.Configuration.FeatureOptions> features
    ) => _features = features;

    /// <summary>获取功能开关（匿名）</summary>
    [HttpGet("features")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public WebResponseContent GetFeatures() => WebResponseContent.Ok(data: _features.Value);
}
