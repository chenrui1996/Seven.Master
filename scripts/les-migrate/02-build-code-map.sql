-- =============================================================================
-- 02：从 LES 填充 Mig_LesCodeMap（主数据编码预览 / DryRun）
-- 依赖：00-config、01-prefix-functions；LES 表名默认与实体同名（Warehouse/Zone/…）
-- =============================================================================
USE SevenDb;
GO

DECLARE @LesDb sysname, @WhCode nvarchar(64), @DensePack nvarchar(32);
SELECT TOP 1
    @LesDb = LesDbName,
    @WhCode = WarehouseCode,
    @DensePack = DenseMiniLoadPackId
FROM dbo.Mig_LesRunConfig
ORDER BY CreatedAt DESC;

IF @LesDb IS NULL THROW 50001, N'请先执行 00-config.sql', 1;

TRUNCATE TABLE dbo.Mig_LesCodeMap;

DECLARE @sql nvarchar(max);

-- Warehouse
SET @sql = N'
INSERT INTO dbo.Mig_LesCodeMap (EntityType, LesId, LesCode, PackId, SevenCode, WarehouseId)
SELECT N''Warehouse'', w.Id, w.Code, N'''', w.Code, w.Id
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Warehouse w
WHERE w.Code = @WhCode;';
EXEC sp_executesql @sql, N'@WhCode nvarchar(64)', @WhCode;

-- Zone（经 WarehouseZone）
SET @sql = N'
INSERT INTO dbo.Mig_LesCodeMap (EntityType, LesId, LesCode, PackId, SevenCode, WarehouseId, ZoneId)
SELECT N''Zone'', z.Id, z.Code,
       dbo.fn_Mig_PackFromZoneType(z.Type, @DensePack),
       dbo.fn_Mig_WithKind(z.Code, dbo.fn_Mig_PackFromZoneType(z.Type, @DensePack), N''Z-''),
       wz.WarehouseId, z.Id
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Zone z
INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.WarehouseZone wz ON wz.ZoneId = z.Id
INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Warehouse w ON w.Id = wz.WarehouseId AND w.Code = @WhCode;';
EXEC sp_executesql @sql, N'@WhCode nvarchar(64), @DensePack nvarchar(32)', @WhCode, @DensePack;

-- Aisle
SET @sql = N'
INSERT INTO dbo.Mig_LesCodeMap (EntityType, LesId, LesCode, PackId, SevenCode, WarehouseId, ZoneId, AisleId)
SELECT N''Aisle'', a.Id, a.Code, zm.PackId,
       dbo.fn_Mig_WithKind(a.Code, zm.PackId, N''A-''),
       zm.WarehouseId, a.ZoneId, a.Id
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Aisle a
INNER JOIN dbo.Mig_LesCodeMap zm ON zm.EntityType = N''Zone'' AND zm.ZoneId = a.ZoneId;';
EXEC sp_executesql @sql;

-- Location
SET @sql = N'
INSERT INTO dbo.Mig_LesCodeMap (EntityType, LesId, LesCode, PackId, SevenCode, WarehouseId, ZoneId, AisleId, ExtraJson)
SELECT N''Location'', l.Id, l.Code,
       COALESCE(am.PackId, zm.PackId, N''stacker''),
       dbo.fn_Mig_LocationCode(l.Code, COALESCE(am.PackId, zm.PackId, N''stacker'')),
       COALESCE(am.WarehouseId, zm.WarehouseId),
       zm.ZoneId,
       l.AisleId,
       CONCAT(N''{"row":'', ISNULL(CAST(l.Row AS nvarchar(16)), N''null''),
              N'',"col":'', ISNULL(CAST(l.Col AS nvarchar(16)), N''null''),
              N'',"layer":'', ISNULL(CAST(l.Layer AS nvarchar(16)), N''null''),
              N'',"depth":'', ISNULL(CAST(l.Deepth AS nvarchar(16)), N''null''),
              N'',"type":'', CAST(l.Type AS nvarchar(8)), N''}'')
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Location l
INNER JOIN dbo.Mig_LesCodeMap zm ON zm.EntityType = N''Zone'' AND zm.LesCode = l.ZoneCode
LEFT JOIN dbo.Mig_LesCodeMap am ON am.EntityType = N''Aisle'' AND am.AisleId = l.AisleId;';
EXEC sp_executesql @sql;

-- Container（位置码经 Location 映射；无货位则仍记入，SevenCode=原码）
SET @sql = N'
INSERT INTO dbo.Mig_LesCodeMap (EntityType, LesId, LesCode, PackId, SevenCode, ExtraJson)
SELECT N''Container'', c.Id, c.Code,
       ISNULL(lm.PackId, N''stacker''),
       c.Code,
       CONCAT(N''{"lesLocation":"'', ISNULL(REPLACE(c.LocationCode, N''"'', N''''''''), N''''),
              N''","sevenLocation":"'', ISNULL(lm.SevenCode, N''''), N''"}'')
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Container c
LEFT JOIN dbo.Mig_LesCodeMap lm
       ON lm.EntityType = N''Location'' AND lm.LesCode = c.LocationCode;';
EXEC sp_executesql @sql;

SELECT EntityType, COUNT(*) AS Cnt FROM dbo.Mig_LesCodeMap GROUP BY EntityType ORDER BY EntityType;
SELECT TOP 20 * FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Location' ORDER BY LesId;

PRINT N'[02] 映射表已填充。抽样核对后执行 03（DryRunOnly=0 时才会写业务表）。';
GO
