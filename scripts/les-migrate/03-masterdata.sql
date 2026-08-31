-- =============================================================================
-- 03：写入主数据 Warehouse / Zone / Aisle / Location
-- DryRunOnly=1 时仅打印将写入行数，不 INSERT
-- =============================================================================
USE SevenDb;
GO

DECLARE @LesDb sysname, @WhCode nvarchar(64), @DensePack nvarchar(32), @Dry bit;
SELECT TOP 1
    @LesDb = LesDbName,
    @WhCode = WarehouseCode,
    @DensePack = DenseMiniLoadPackId,
    @Dry = DryRunOnly
FROM dbo.Mig_LesRunConfig
ORDER BY CreatedAt DESC;

IF NOT EXISTS (SELECT 1 FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Warehouse')
    THROW 50002, N'请先执行 02-build-code-map.sql', 1;

DECLARE @sql nvarchar(max);
DECLARE @enabled nvarchar(256);

-- EnabledPackIds = 本仓涉及 Pack 去重
;WITH p AS (
    SELECT DISTINCT PackId FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Zone' AND PackId <> N''
)
SELECT @enabled = STUFF((
    SELECT N',' + PackId FROM p FOR XML PATH(N''), TYPE
).value(N'.', N'nvarchar(256)'), 1, 1, N'');
IF @enabled IS NULL OR @enabled = N'' SET @enabled = N'stacker';

PRINT N'EnabledPackIds => ' + @enabled;

IF @Dry = 1
BEGIN
    SELECT N'Warehouse' AS Step, COUNT(*) AS WouldInsert FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Warehouse'
    UNION ALL SELECT N'Zone', COUNT(*) FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Zone'
    UNION ALL SELECT N'Aisle', COUNT(*) FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Aisle'
    UNION ALL SELECT N'Location', COUNT(*) FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Location';
    PRINT N'[03] DryRun：未写入。将 Mig_LesRunConfig.DryRunOnly 改为 0 后重跑本脚本。';
END
ELSE
BEGIN
-- Warehouse
SET IDENTITY_INSERT dbo.Wms_Warehouse ON;
SET @sql = N'
INSERT INTO dbo.Wms_Warehouse (Id, Code, Name, EnabledPackIds, TenantId, CreateDate, IsDeleted)
SELECT w.Id, w.Code, w.Name, @enabled, 0, SYSUTCDATETIME(), 0
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Warehouse w
WHERE w.Code = @WhCode
  AND NOT EXISTS (SELECT 1 FROM dbo.Wms_Warehouse x WHERE x.Code = w.Code);';
EXEC sp_executesql @sql, N'@WhCode nvarchar(64), @enabled nvarchar(256)', @WhCode, @enabled;
SET IDENTITY_INSERT dbo.Wms_Warehouse OFF;

-- Zone
SET IDENTITY_INSERT dbo.Wms_Zone ON;
SET @sql = N'
INSERT INTO dbo.Wms_Zone (Id, WarehouseId, PackId, Code, Name, TenantId, CreateDate, IsDeleted)
SELECT zm.ZoneId, zm.WarehouseId, zm.PackId, zm.SevenCode, ISNULL(z.Name, zm.LesCode), 0, SYSUTCDATETIME(), 0
FROM dbo.Mig_LesCodeMap zm
INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Zone z ON z.Id = zm.ZoneId
WHERE zm.EntityType = N''Zone''
  AND NOT EXISTS (SELECT 1 FROM dbo.Wms_Zone x WHERE x.Code = zm.SevenCode);';
EXEC sp_executesql @sql;
SET IDENTITY_INSERT dbo.Wms_Zone OFF;

-- Aisle
SET IDENTITY_INSERT dbo.Wms_Aisle ON;
SET @sql = N'
INSERT INTO dbo.Wms_Aisle (
    Id, WarehouseId, ZoneId, LayerId, PackId, Code, Name,
    EpPointCode, IsAvailable, AllocationWeight, MaxDepth,
    TenantId, CreateDate, IsDeleted)
SELECT
    am.AisleId, am.WarehouseId, am.ZoneId, NULL, am.PackId, am.SevenCode, ISNULL(a.Name, a.Code),
    NULLIF(LTRIM(RTRIM(a.AssignedTargetStation)), N''''),
    CASE WHEN a.LockFlag = 1 THEN 0 ELSE 1 END,
    ISNULL(NULLIF(a.AllcationWeight, 0), 1),
    NULLIF(a.MaxDeepth, 0),
    0, SYSUTCDATETIME(), 0
FROM dbo.Mig_LesCodeMap am
INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Aisle a ON a.Id = am.AisleId
WHERE am.EntityType = N''Aisle''
  AND NOT EXISTS (SELECT 1 FROM dbo.Wms_Aisle x WHERE x.Code = am.SevenCode);';
EXEC sp_executesql @sql;
SET IDENTITY_INSERT dbo.Wms_Aisle OFF;

-- Location
SET IDENTITY_INSERT dbo.Wms_Location ON;
SET @sql = N'
INSERT INTO dbo.Wms_Location (
    Id, WarehouseId, ZoneId, LayerId, AisleId, PackId, Code,
    Aisle, Row, [Column], Layer, Depth,
    IsOccupied, IsLocked, IsBooked, IsHandover, CurrentContainerCode,
    TenantId, CreateDate, IsDeleted)
SELECT
    lm.LesId,
    lm.WarehouseId,
    lm.ZoneId,
    NULL,
    lm.AisleId,
    lm.PackId,
    lm.SevenCode,
    aisleMap.SevenCode,
    CAST(l.Row AS nvarchar(32)),
    CAST(l.Col AS nvarchar(32)),
    CAST(l.Layer AS nvarchar(32)),
    CAST(l.Deepth AS nvarchar(32)),
    ISNULL(l.OccupyFlag, 0),
    ISNULL(l.LockFlag, 0),
    ISNULL(l.BookingFlag, 0),
    CASE WHEN l.Type IN (30, 31, 50, 51, 52, 53, 54, 55, 56) THEN 1 ELSE 0 END,
    NULL,
    0, SYSUTCDATETIME(), 0
FROM dbo.Mig_LesCodeMap lm
INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Location l ON l.Id = lm.LesId
LEFT JOIN dbo.Mig_LesCodeMap aisleMap ON aisleMap.EntityType = N''Aisle'' AND aisleMap.AisleId = l.AisleId
WHERE lm.EntityType = N''Location''
  AND NOT EXISTS (SELECT 1 FROM dbo.Wms_Location x WHERE x.Code = lm.SevenCode);';
EXEC sp_executesql @sql;
SET IDENTITY_INSERT dbo.Wms_Location OFF;

PRINT N'[03] 主数据写入完成。继续 04 容器与库存。';
END
GO
