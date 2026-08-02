using Microsoft.EntityFrameworkCore;
using Seven.Domain.Common;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Entities.Business;
using Seven.Domain.Entities.Core;
using Seven.Domain.Entities.Flow;
using Seven.Domain.Entities.Form;
using Seven.Domain.Entities.News;
using Seven.Domain.Entities.Quartz;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Messaging.Outbox;

namespace Seven.Infrastructure.Persistence;

/// <summary>
/// Seven 系统 EF Core 数据库上下文（CodeFirst）
/// </summary>
public class SevenDbContext : DbContext
{
    /// <summary>当前租户（0=不过滤/默认）</summary>
    public int CurrentTenantId { get; set; }

    /// <summary>是否启用租户过滤</summary>
    public bool TenantFilterEnabled { get; set; }

    /// <summary>构造函数</summary>
    public SevenDbContext(DbContextOptions<SevenDbContext> options) : base(options) { }

    /// <summary>租户</summary>
    public DbSet<Sys_Tenant> Sys_Tenants => Set<Sys_Tenant>();

    /// <summary>用户</summary>
    public DbSet<Sys_User> Sys_Users => Set<Sys_User>();

    /// <summary>角色</summary>
    public DbSet<Sys_Role> Sys_Roles => Set<Sys_Role>();

    /// <summary>角色权限</summary>
    public DbSet<Sys_RoleAuth> Sys_RoleAuths => Set<Sys_RoleAuth>();

    /// <summary>菜单</summary>
    public DbSet<Sys_Menu> Sys_Menus => Set<Sys_Menu>();

    /// <summary>部门</summary>
    public DbSet<Sys_Department> Sys_Departments => Set<Sys_Department>();

    /// <summary>用户部门</summary>
    public DbSet<Sys_UserDepartment> Sys_UserDepartments => Set<Sys_UserDepartment>();

    /// <summary>字典</summary>
    public DbSet<Sys_Dictionary> Sys_Dictionaries => Set<Sys_Dictionary>();

    /// <summary>字典明细</summary>
    public DbSet<Sys_DictionaryList> Sys_DictionaryLists => Set<Sys_DictionaryList>();

    /// <summary>日志</summary>
    public DbSet<Sys_Log> Sys_Logs => Set<Sys_Log>();

    /// <summary>工作流</summary>
    public DbSet<Sys_WorkFlow> Sys_WorkFlows => Set<Sys_WorkFlow>();

    /// <summary>工作流步骤</summary>
    public DbSet<Sys_WorkFlowStep> Sys_WorkFlowSteps => Set<Sys_WorkFlowStep>();

    /// <summary>工作流实例</summary>
    public DbSet<Sys_WorkFlowTable> Sys_WorkFlowTables => Set<Sys_WorkFlowTable>();

    /// <summary>工作流实例步骤</summary>
    public DbSet<Sys_WorkFlowTableStep> Sys_WorkFlowTableSteps => Set<Sys_WorkFlowTableStep>();

    /// <summary>工作流审批日志</summary>
    public DbSet<Sys_WorkFlowTableAuditLog> Sys_WorkFlowTableAuditLogs => Set<Sys_WorkFlowTableAuditLog>();

    /// <summary>表单设计</summary>
    public DbSet<FormDesignOptions> FormDesignOptions => Set<FormDesignOptions>();

    /// <summary>表单采集</summary>
    public DbSet<FormCollectionObject> FormCollectionObjects => Set<FormCollectionObject>();

    /// <summary>定时任务</summary>
    public DbSet<Sys_QuartzOptions> Sys_QuartzOptions => Set<Sys_QuartzOptions>();

    /// <summary>定时任务日志</summary>
    public DbSet<Sys_QuartzLog> Sys_QuartzLogs => Set<Sys_QuartzLog>();

    /// <summary>代码生成表</summary>
    public DbSet<Sys_TableInfo> Sys_TableInfos => Set<Sys_TableInfo>();

    /// <summary>代码生成列</summary>
    public DbSet<Sys_TableColumn> Sys_TableColumns => Set<Sys_TableColumn>();

    /// <summary>代码生成主子表关系</summary>
    public DbSet<Sys_TableDetail> Sys_TableDetails => Set<Sys_TableDetail>();

    /// <summary>设备</summary>
    public DbSet<Device> Devices => Set<Device>();

    /// <summary>子设备</summary>
    public DbSet<SubDevice> SubDevices => Set<SubDevice>();

    /// <summary>新闻</summary>
    public DbSet<App_News> App_News => Set<App_News>();

    /// <summary>告警码</summary>
    public DbSet<Sys_AlarmCode> Sys_AlarmCodes => Set<Sys_AlarmCode>();

    /// <summary>告警记录</summary>
    public DbSet<Sys_Alarm> Sys_Alarms => Set<Sys_Alarm>();

    /// <summary>Outbox 出站消息</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SevenDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;
            var clr = entityType.ClrType;
            var param = System.Linq.Expressions.Expression.Parameter(clr, "e");
            var isDeleted = System.Linq.Expressions.Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Equal(
                isDeleted, System.Linq.Expressions.Expression.Constant(false));

            var tenantIdProp = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.TenantId));
            var ctxConst = System.Linq.Expressions.Expression.Constant(this);
            var filterEnabled = System.Linq.Expressions.Expression.Property(ctxConst, nameof(TenantFilterEnabled));
            var currentTenant = System.Linq.Expressions.Expression.Property(ctxConst, nameof(CurrentTenantId));
            var tenantOk = System.Linq.Expressions.Expression.OrElse(
                System.Linq.Expressions.Expression.Not(filterEnabled),
                System.Linq.Expressions.Expression.OrElse(
                    System.Linq.Expressions.Expression.Equal(tenantIdProp, System.Linq.Expressions.Expression.Constant(0)),
                    System.Linq.Expressions.Expression.Equal(tenantIdProp, currentTenant)));

            var body = System.Linq.Expressions.Expression.AndAlso(notDeleted, tenantOk);
            var lambda = System.Linq.Expressions.Expression.Lambda(body, param);
            modelBuilder.Entity(clr).HasQueryFilter(lambda);
        }

        base.OnModelCreating(modelBuilder);
    }
}
