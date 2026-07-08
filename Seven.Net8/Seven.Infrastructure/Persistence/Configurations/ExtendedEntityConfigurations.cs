using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Board;
using Seven.Domain.Entities.Core;
using Seven.Domain.Entities.Flow;
using Seven.Domain.Entities.Form;
using Seven.Domain.Entities.News;
using Seven.Domain.Entities.Quartz;

namespace Seven.Infrastructure.Persistence.Configurations;

/// <summary>工作流定义配置</summary>
public class SysWorkFlowConfiguration : IEntityTypeConfiguration<Sys_WorkFlow>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_WorkFlow> builder)
    {
        builder.ToTable("Sys_WorkFlow");
        builder.HasKey(x => x.WorkFlow_Id);
        builder.HasMany(x => x.Steps).WithOne(x => x.WorkFlow).HasForeignKey(x => x.WorkFlow_Id);
    }
}

/// <summary>工作流步骤配置</summary>
public class SysWorkFlowStepConfiguration : IEntityTypeConfiguration<Sys_WorkFlowStep>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_WorkFlowStep> builder)
    {
        builder.ToTable("Sys_WorkFlowStep");
        builder.HasKey(x => x.WorkStepFlow_Id);
    }
}

/// <summary>工作流实例配置</summary>
public class SysWorkFlowTableConfiguration : IEntityTypeConfiguration<Sys_WorkFlowTable>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_WorkFlowTable> builder)
    {
        builder.ToTable("Sys_WorkFlowTable");
        builder.HasKey(x => x.WorkFlowTable_Id);
    }
}

/// <summary>工作流实例步骤配置</summary>
public class SysWorkFlowTableStepConfiguration : IEntityTypeConfiguration<Sys_WorkFlowTableStep>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_WorkFlowTableStep> builder)
    {
        builder.ToTable("Sys_WorkFlowTableStep");
        builder.HasKey(x => x.Sys_WorkFlowTableStep_Id);
    }
}

/// <summary>工作流审批日志配置</summary>
public class SysWorkFlowTableAuditLogConfiguration : IEntityTypeConfiguration<Sys_WorkFlowTableAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_WorkFlowTableAuditLog> builder)
    {
        builder.ToTable("Sys_WorkFlowTableAuditLog");
        builder.HasKey(x => x.Id);
    }
}

/// <summary>表单设计配置</summary>
public class FormDesignOptionsConfiguration : IEntityTypeConfiguration<FormDesignOptions>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FormDesignOptions> builder)
    {
        builder.ToTable("FormDesignOptions");
        builder.HasKey(x => x.FormId);
    }
}

/// <summary>表单采集配置</summary>
public class FormCollectionObjectConfiguration : IEntityTypeConfiguration<FormCollectionObject>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FormCollectionObject> builder)
    {
        builder.ToTable("FormCollectionObject");
        builder.HasKey(x => x.FormCollectionId);
    }
}

/// <summary>定时任务配置</summary>
public class SysQuartzOptionsConfiguration : IEntityTypeConfiguration<Sys_QuartzOptions>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_QuartzOptions> builder)
    {
        builder.ToTable("Sys_QuartzOptions");
        builder.HasKey(x => x.Id);
    }
}

/// <summary>定时任务日志配置</summary>
public class SysQuartzLogConfiguration : IEntityTypeConfiguration<Sys_QuartzLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_QuartzLog> builder)
    {
        builder.ToTable("Sys_QuartzLog");
        builder.HasKey(x => x.LogId);
    }
}

/// <summary>代码生成表配置</summary>
public class SysTableInfoConfiguration : IEntityTypeConfiguration<Sys_TableInfo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_TableInfo> builder)
    {
        builder.ToTable("Sys_TableInfo");
        builder.HasKey(x => x.Table_Id);
        builder.HasMany(x => x.TableColumns).WithOne().HasForeignKey(x => x.Table_Id);
    }
}

/// <summary>代码生成列配置</summary>
public class SysTableColumnConfiguration : IEntityTypeConfiguration<Sys_TableColumn>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_TableColumn> builder)
    {
        builder.ToTable("Sys_TableColumn");
        builder.HasKey(x => x.ColumnId);
    }
}

/// <summary>设备配置</summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Device");
        builder.HasKey(x => x.DeviceId);
    }
}

/// <summary>新闻配置</summary>
public class AppNewsConfiguration : IEntityTypeConfiguration<App_News>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<App_News> builder)
    {
        builder.ToTable("App_News");
        builder.HasKey(x => x.Id);
    }
}
