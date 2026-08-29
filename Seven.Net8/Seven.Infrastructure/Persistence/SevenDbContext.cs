using Microsoft.EntityFrameworkCore;
using Seven.Domain.Common;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Entities.Business;
using Seven.Domain.Entities.Core;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Entities.Wcs.External;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Entities.Simulator;
using Seven.Domain.Entities.Wms;
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

    /// <summary>设备通讯连接</summary>
    public DbSet<CommConnection> CommConnections => Set<CommConnection>();

    /// <summary>设备通讯点位</summary>
    public DbSet<CommPoint> CommPoints => Set<CommPoint>();

    /// <summary>设备通讯规则</summary>
    public DbSet<CommRule> CommRules => Set<CommRule>();

    /// <summary>设备通讯事件日志</summary>
    public DbSet<CommEventLog> CommEventLogs => Set<CommEventLog>();

    /// <summary>WMS 仓库</summary>
    public DbSet<WmsWarehouse> WmsWarehouses => Set<WmsWarehouse>();

    /// <summary>WMS 库区</summary>
    public DbSet<WmsZone> WmsZones => Set<WmsZone>();

    /// <summary>WMS 楼层（四向等）</summary>
    public DbSet<WmsLayer> WmsLayers => Set<WmsLayer>();

    /// <summary>WMS 巷道</summary>
    public DbSet<WmsAisle> WmsAisles => Set<WmsAisle>();

    /// <summary>WMS 库位</summary>
    public DbSet<WmsLocation> WmsLocations => Set<WmsLocation>();

    /// <summary>WMS 容器类型</summary>
    public DbSet<WmsContainerType> WmsContainerTypes => Set<WmsContainerType>();

    /// <summary>WMS 容器</summary>
    public DbSet<WmsContainer> WmsContainers => Set<WmsContainer>();

    /// <summary>WMS 库存</summary>
    public DbSet<WmsStock> WmsStocks => Set<WmsStock>();

    /// <summary>WMS 库存流水</summary>
    public DbSet<WmsStockLedger> WmsStockLedgers => Set<WmsStockLedger>();

    /// <summary>WMS 入库单</summary>
    public DbSet<WmsInboundOrder> WmsInboundOrders => Set<WmsInboundOrder>();

    /// <summary>WMS 入库单行</summary>
    public DbSet<WmsInboundOrderLine> WmsInboundOrderLines => Set<WmsInboundOrderLine>();

    /// <summary>WMS 入库组盘明细</summary>
    public DbSet<WmsInboundDetail> WmsInboundDetails => Set<WmsInboundDetail>();

    /// <summary>WMS 出库单</summary>
    public DbSet<WmsOutboundOrder> WmsOutboundOrders => Set<WmsOutboundOrder>();

    /// <summary>WMS 出库单行</summary>
    public DbSet<WmsOutboundOrderLine> WmsOutboundOrderLines => Set<WmsOutboundOrderLine>();

    /// <summary>WMS 盘点单</summary>
    public DbSet<WmsCycleCount> WmsCycleCounts => Set<WmsCycleCount>();

    /// <summary>WMS 盘点单行</summary>
    public DbSet<WmsCycleCountLine> WmsCycleCountLines => Set<WmsCycleCountLine>();

    /// <summary>WMS 跨包交接链</summary>
    public DbSet<WmsHandoverLink> WmsHandoverLinks => Set<WmsHandoverLink>();

    /// <summary>编排总线运输单</summary>
    public DbSet<BusTransportOrder> BusTransportOrders => Set<BusTransportOrder>();

    /// <summary>编排总线运输段</summary>
    public DbSet<BusTransportLeg> BusTransportLegs => Set<BusTransportLeg>();

    /// <summary>堆垛机申请点</summary>
    public DbSet<StkRequestPoint> StkRequestPoints => Set<StkRequestPoint>();

    /// <summary>堆垛机巷道分配策略</summary>
    public DbSet<StkAssignmentPolicy> StkAssignmentPolicies => Set<StkAssignmentPolicy>();

    /// <summary>堆垛机巷道轮转记录</summary>
    public DbSet<StkAssignmentRecord> StkAssignmentRecords => Set<StkAssignmentRecord>();

    /// <summary>堆垛机上架任务</summary>
    public DbSet<StkPutAwayTask> StkPutAwayTasks => Set<StkPutAwayTask>();

    /// <summary>堆垛机出库取货任务</summary>
    public DbSet<StkRetrievalTask> StkRetrievalTasks => Set<StkRetrievalTask>();

    /// <summary>堆垛机设备段任务</summary>
    public DbSet<StkDeviceTask> StkDeviceTasks => Set<StkDeviceTask>();

    /// <summary>堆垛机路网边</summary>
    public DbSet<StkRoute> StkRoutes => Set<StkRoute>();

    /// <summary>堆垛机边占用</summary>
    public DbSet<StkRouteFlow> StkRouteFlows => Set<StkRouteFlow>();

    /// <summary>堆垛机设备点编码映射</summary>
    public DbSet<StkDeviceCoder> StkDeviceCoders => Set<StkDeviceCoder>();

    /// <summary>堆垛机货位扩展（双深组 / LockBin）</summary>
    public DbSet<StkLocationProfile> StkLocationProfiles => Set<StkLocationProfile>();

    /// <summary>四向车地图版本</summary>
    public DbSet<FwMapVersion> FwMapVersions => Set<FwMapVersion>();

    /// <summary>四向车路网节点</summary>
    public DbSet<FwNode> FwNodes => Set<FwNode>();

    /// <summary>四向车路网边分组</summary>
    public DbSet<FwRouteGroup> FwRouteGroups => Set<FwRouteGroup>();

    /// <summary>四向车路网边</summary>
    public DbSet<FwRoute> FwRoutes => Set<FwRoute>();

    /// <summary>四向车穿梭任务</summary>
    public DbSet<FwShuttleTask> FwShuttleTasks => Set<FwShuttleTask>();

    /// <summary>四向车任务路径</summary>
    public DbSet<FwShuttleTaskPath> FwShuttleTaskPaths => Set<FwShuttleTaskPath>();

    /// <summary>四向层分配策略</summary>
    public DbSet<FwLayerPolicy> FwLayerPolicies => Set<FwLayerPolicy>();

    /// <summary>四向巷分配策略</summary>
    public DbSet<FwAislePolicy> FwAislePolicies => Set<FwAislePolicy>();

    /// <summary>四向层/巷轮转记录</summary>
    public DbSet<FwAssignmentRecord> FwAssignmentRecords => Set<FwAssignmentRecord>();

    /// <summary>四向入库上架任务</summary>
    public DbSet<FwPutAwayTask> FwPutAwayTasks => Set<FwPutAwayTask>();

    /// <summary>四向目的地申请点</summary>
    public DbSet<FwRequestPoint> FwRequestPoints => Set<FwRequestPoint>();

    /// <summary>四向出库取货任务</summary>
    public DbSet<FwRetrievalTask> FwRetrievalTasks => Set<FwRetrievalTask>();

    /// <summary>四向停车账本</summary>
    public DbSet<FwParkingLedger> FwParkingLedgers => Set<FwParkingLedger>();

    /// <summary>四向提升机台账</summary>
    public DbSet<FwHoistDevice> FwHoistDevices => Set<FwHoistDevice>();

    /// <summary>四向提升机层口</summary>
    public DbSet<FwHoistLayerPoint> FwHoistLayerPoints => Set<FwHoistLayerPoint>();

    /// <summary>四向提升机业务任务</summary>
    public DbSet<FwHoistTask> FwHoistTasks => Set<FwHoistTask>();

    /// <summary>四向提升机执行任务</summary>
    public DbSet<FwHoistExecTask> FwHoistExecTasks => Set<FwHoistExecTask>();

    /// <summary>外部 WCS 系统实例</summary>
    public DbSet<ExtSystem> ExtSystems => Set<ExtSystem>();

    /// <summary>外部 WCS 报文日志</summary>
    public DbSet<ExtMessageLog> ExtMessageLogs => Set<ExtMessageLog>();

    /// <summary>新闻</summary>
    public DbSet<App_News> App_News => Set<App_News>();

    /// <summary>告警码</summary>
    public DbSet<Sys_AlarmCode> Sys_AlarmCodes => Set<Sys_AlarmCode>();

    /// <summary>告警记录</summary>
    public DbSet<Sys_Alarm> Sys_Alarms => Set<Sys_Alarm>();

    /// <summary>Outbox 出站消息</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>接口调用日志</summary>
    public DbSet<IfcApiLog> IfcApiLogs => Set<IfcApiLog>();

    /// <summary>联锁/运行模式</summary>
    public DbSet<CtlMode> CtlModes => Set<CtlMode>();

    /// <summary>2D SCADA 视图</summary>
    public DbSet<ScdView> ScdViews => Set<ScdView>();

    /// <summary>2D SCADA 节点绑定</summary>
    public DbSet<ScdNodeBind> ScdNodeBinds => Set<ScdNodeBind>();

    /// <summary>仿真部署记录</summary>
    public DbSet<SimDeployment> SimDeployments => Set<SimDeployment>();

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
