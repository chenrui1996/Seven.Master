using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Entities.Business;
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

/// <summary>代码生成主子表关系</summary>
public class SysTableDetailConfiguration : IEntityTypeConfiguration<Sys_TableDetail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_TableDetail> builder)
    {
        builder.ToTable("Sys_TableDetail");
        builder.HasKey(x => x.DetailId);
        builder.Property(x => x.ParentTable).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChildTable).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ForeignKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MasterKey).HasMaxLength(100);
        builder.Property(x => x.DisplayMode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CnName).HasMaxLength(100);
        builder.HasIndex(x => new { x.ParentTable, x.ChildTable, x.ForeignKey }).IsUnique();
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
        builder.Property(x => x.DeviceName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DeviceCode).HasMaxLength(64);
        builder.Property(x => x.Location).HasMaxLength(256);
    }
}

/// <summary>子设备配置（Device 一对多 Demo）</summary>
public class SubDeviceConfiguration : IEntityTypeConfiguration<SubDevice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SubDevice> builder)
    {
        builder.ToTable("SubDevice");
        builder.HasKey(x => x.SubDeviceId);
        builder.Property(x => x.SubDeviceName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SubDeviceCode).HasMaxLength(64);
        builder.Property(x => x.Remark).HasMaxLength(256);
        builder.HasIndex(x => x.DeviceId);
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
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

/// <summary>告警码配置</summary>
public class SysAlarmCodeConfiguration : IEntityTypeConfiguration<Sys_AlarmCode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_AlarmCode> builder)
    {
        builder.ToTable("Sys_AlarmCode");
        builder.HasKey(x => x.AlarmCode_Id);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(32);
    }
}

/// <summary>告警记录</summary>
public class SysAlarmConfiguration : IEntityTypeConfiguration<Sys_Alarm>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sys_Alarm> builder)
    {
        builder.ToTable("Sys_Alarm");
        builder.HasKey(x => x.Alarm_Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(32);
        builder.Property(x => x.Source).HasMaxLength(128);
        builder.Property(x => x.DeviceName).HasMaxLength(128);
    }
}
