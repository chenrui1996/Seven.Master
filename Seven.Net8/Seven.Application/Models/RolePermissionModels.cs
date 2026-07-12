namespace Seven.Application.Models;

/// <summary>菜单按钮项</summary>
public class PermissionActionDto
{
    /// <summary>显示文本</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>权限值，如 Search、Add</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>权限分配树节点（扁平，前端按 pid 组树）</summary>
public class MenuPermissionNodeDto
{
    /// <summary>菜单 Id</summary>
    public int Id { get; set; }

    /// <summary>父级 Id</summary>
    public int Pid { get; set; }

    /// <summary>菜单名称</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>是否移动端菜单</summary>
    public bool IsApp { get; set; }

    /// <summary>可分配按钮</summary>
    public List<PermissionActionDto> Actions { get; set; } = [];
}

/// <summary>保存角色权限项</summary>
public class RolePermissionSaveItem
{
    /// <summary>菜单 Id</summary>
    public int Id { get; set; }

    /// <summary>已勾选按钮</summary>
    public List<PermissionActionDto> Actions { get; set; } = [];
}

/// <summary>保存角色权限请求（Legrand 兼容格式）</summary>
public class SaveRolePermissionRequest
{
    /// <summary>角色 Id</summary>
    public int RoleId { get; set; }

    /// <summary>菜单权限列表</summary>
    public List<RolePermissionSaveItem> Permissions { get; set; } = [];
}
