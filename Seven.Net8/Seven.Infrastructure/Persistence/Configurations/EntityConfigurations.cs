using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.System;

namespace Seven.Infrastructure.Persistence.Configurations;

/// <summary>
/// 系统模块 EF 实体配置
/// </summary>
public class SysUserConfiguration : IEntityTypeConfiguration<Sys_User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_User> builder)
    {
        builder.ToTable("Sys_User");
        builder.HasKey(x => x.User_Id);
        builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserTrueName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.UserName).IsUnique();
    }
}

/// <summary>角色配置</summary>
public class SysRoleConfiguration : IEntityTypeConfiguration<Sys_Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Role> builder)
    {
        builder.ToTable("Sys_Role");
        builder.HasKey(x => x.Role_Id);
        builder.Property(x => x.RoleName).HasMaxLength(50);
        builder.HasMany(x => x.RoleAuths).WithOne(x => x.Role).HasForeignKey(x => x.Role_Id);
    }
}

/// <summary>角色权限配置</summary>
public class SysRoleAuthConfiguration : IEntityTypeConfiguration<Sys_RoleAuth>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_RoleAuth> builder)
    {
        builder.ToTable("Sys_RoleAuth");
        builder.HasKey(x => x.Auth_Id);
    }
}

/// <summary>菜单配置</summary>
public class SysMenuConfiguration : IEntityTypeConfiguration<Sys_Menu>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Menu> builder)
    {
        builder.ToTable("Sys_Menu");
        builder.HasKey(x => x.Menu_Id);
        builder.Property(x => x.MenuName).HasMaxLength(50).IsRequired();
    }
}

/// <summary>部门配置</summary>
public class SysDepartmentConfiguration : IEntityTypeConfiguration<Sys_Department>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Department> builder)
    {
        builder.ToTable("Sys_Department");
        builder.HasKey(x => x.DepartmentId);
        builder.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
    }
}

/// <summary>用户部门配置</summary>
public class SysUserDepartmentConfiguration : IEntityTypeConfiguration<Sys_UserDepartment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_UserDepartment> builder)
    {
        builder.ToTable("Sys_UserDepartment");
        builder.HasKey(x => x.Id);
    }
}

/// <summary>字典配置</summary>
public class SysDictionaryConfiguration : IEntityTypeConfiguration<Sys_Dictionary>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Dictionary> builder)
    {
        builder.ToTable("Sys_Dictionary");
        builder.HasKey(x => x.Dic_ID);
        builder.Property(x => x.DicNo).HasMaxLength(100).IsRequired();
        builder.HasMany(x => x.DictionaryLists).WithOne(x => x.Dictionary).HasForeignKey(x => x.Dic_ID);
    }
}

/// <summary>字典明细配置</summary>
public class SysDictionaryListConfiguration : IEntityTypeConfiguration<Sys_DictionaryList>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_DictionaryList> builder)
    {
        builder.ToTable("Sys_DictionaryList");
        builder.HasKey(x => x.DicList_ID);
    }
}

/// <summary>日志配置</summary>
public class SysLogConfiguration : IEntityTypeConfiguration<Sys_Log>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Log> builder)
    {
        builder.ToTable("Sys_Log");
        builder.HasKey(x => x.Id);
    }
}

/// <summary>租户配置</summary>
public class SysTenantConfiguration : IEntityTypeConfiguration<Sys_Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Tenant> builder)
    {
        builder.ToTable("Sys_Tenant");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
