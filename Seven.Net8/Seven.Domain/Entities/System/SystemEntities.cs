using Seven.Domain.Common;

namespace Seven.Domain.Entities.System;

/// <summary>
/// 系统用户
/// </summary>
public class Sys_User : BaseEntity
{
    /// <summary>用户主键</summary>
    public int User_Id { get; set; }

    /// <summary>登录账号</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>真实姓名</summary>
    public string UserTrueName { get; set; } = string.Empty;

    /// <summary>BCrypt 密码哈希</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>是否必须重置密码（旧库迁移标记）</summary>
    public bool MustResetPassword { get; set; }

    /// <summary>角色 Id</summary>
    public int Role_Id { get; set; }

    /// <summary>角色名称（冗余）</summary>
    public string? RoleName { get; set; }

    /// <summary>部门 Id 列表，逗号分隔</summary>
    public string? DeptIds { get; set; }

    /// <summary>性别</summary>
    public int? Gender { get; set; }

    /// <summary>头像 URL</summary>
    public string? HeadImageUrl { get; set; }

    /// <summary>手机号</summary>
    public string? PhoneNo { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>是否启用</summary>
    public byte Enable { get; set; } = 1;

    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>最后改密时间</summary>
    public DateTime? LastModifyPwdDate { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }
}

/// <summary>
/// 系统角色
/// </summary>
public class Sys_Role : BaseEntity
{
    /// <summary>角色主键</summary>
    public int Role_Id { get; set; }

    /// <summary>父级角色 Id</summary>
    public int ParentId { get; set; }

    /// <summary>角色名称</summary>
    public string? RoleName { get; set; }

    /// <summary>部门 Id</summary>
    public int? Dept_Id { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }

    /// <summary>
    /// 数据权限范围：0=全部 1=本部门 2=本部门及下级 3=仅本人
    /// </summary>
    public int DataScope { get; set; }

    /// <summary>角色权限关联</summary>
    public ICollection<Sys_RoleAuth> RoleAuths { get; set; } = [];
}

/// <summary>
/// 角色菜单/按钮权限
/// </summary>
public class Sys_RoleAuth : BaseEntity
{
    /// <summary>主键</summary>
    public int Auth_Id { get; set; }

    /// <summary>角色 Id</summary>
    public int Role_Id { get; set; }

    /// <summary>菜单 Id</summary>
    public int Menu_Id { get; set; }

    /// <summary>按钮权限，逗号分隔</summary>
    public string? AuthValue { get; set; }

    /// <summary>关联角色</summary>
    public Sys_Role? Role { get; set; }
}

/// <summary>
/// 系统菜单
/// </summary>
public class Sys_Menu : BaseEntity
{
    /// <summary>菜单主键</summary>
    public int Menu_Id { get; set; }

    /// <summary>父级 Id</summary>
    public int ParentId { get; set; }

    /// <summary>菜单名称</summary>
    public string MenuName { get; set; } = string.Empty;

    /// <summary>关联表名</summary>
    public string? TableName { get; set; }

    /// <summary>路由 URL</summary>
    public string? Url { get; set; }

    /// <summary>按钮权限定义</summary>
    public string? Auth { get; set; }

    /// <summary>图标</summary>
    public string? Icon { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>菜单类型：0 PC，1 移动端</summary>
    public int? MenuType { get; set; }

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }
}

/// <summary>
/// 部门组织
/// </summary>
public class Sys_Department : BaseEntity
{
    /// <summary>部门主键</summary>
    public int DepartmentId { get; set; }

    /// <summary>部门名称</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>父级 Id</summary>
    public int ParentId { get; set; }

    /// <summary>部门编码</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }
}

/// <summary>
/// 用户部门关联
/// </summary>
public class Sys_UserDepartment : BaseEntity
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>用户 Id</summary>
    public int UserId { get; set; }

    /// <summary>部门 Id</summary>
    public int DepartmentId { get; set; }

    /// <summary>是否主部门</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// 数据字典主表
/// </summary>
public class Sys_Dictionary : BaseEntity
{
    /// <summary>字典主键</summary>
    public int Dic_ID { get; set; }

    /// <summary>字典编号</summary>
    public string DicNo { get; set; } = string.Empty;

    /// <summary>字典名称</summary>
    public string DicName { get; set; } = string.Empty;

    /// <summary>父级 Id</summary>
    public int ParentId { get; set; }

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }

    /// <summary>字典明细</summary>
    public ICollection<Sys_DictionaryList> DictionaryLists { get; set; } = [];
}

/// <summary>
/// 数据字典明细
/// </summary>
public class Sys_DictionaryList : BaseEntity
{
    /// <summary>明细主键</summary>
    public int DicList_ID { get; set; }

    /// <summary>字典主表 Id</summary>
    public int Dic_ID { get; set; }

    /// <summary>显示文本</summary>
    public string DicName { get; set; } = string.Empty;

    /// <summary>存储值</summary>
    public string DicValue { get; set; } = string.Empty;

    /// <summary>是否启用</summary>
    public byte? Enable { get; set; } = 1;

    /// <summary>排序号</summary>
    public int? OrderNo { get; set; }

    /// <summary>关联字典</summary>
    public Sys_Dictionary? Dictionary { get; set; }
}

/// <summary>
/// 系统操作/登录日志
/// </summary>
public class Sys_Log : BaseEntity
{
    /// <summary>日志主键</summary>
    public int Id { get; set; }

    /// <summary>日志类型</summary>
    public string? LogType { get; set; }

    /// <summary>请求 URL</summary>
    public string? Url { get; set; }

    /// <summary>用户 Id</summary>
    public int? User_Id { get; set; }

    /// <summary>用户名</summary>
    public string? UserName { get; set; }

    /// <summary>IP 地址</summary>
    public string? IPAddress { get; set; }

    /// <summary>请求参数</summary>
    public string? RequestParameter { get; set; }

    /// <summary>响应内容</summary>
    public string? ResponseParameter { get; set; }

    /// <summary>异常信息</summary>
    public string? ExceptionInfo { get; set; }

    /// <summary>耗时毫秒</summary>
    public int? ServiceTime { get; set; }
}

/// <summary>
/// 租户主数据（不继承 BaseEntity，避免租户全局过滤器作用于租户表本身）
/// </summary>
public class Sys_Tenant
{
    /// <summary>租户主键</summary>
    public int TenantId { get; set; }

    /// <summary>租户编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>租户名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用</summary>
    public byte Enable { get; set; } = 1;

    /// <summary>创建时间</summary>
    public DateTime? CreateDate { get; set; }
}
