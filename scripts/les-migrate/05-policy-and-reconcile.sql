-- =============================================================================
-- 05：Stk_AssignmentPolicy + 抽样对账
-- =============================================================================
USE SevenDb;
GO

DECLARE @LesDb sysname, @WhCode nvarchar(64), @Dry bit;
SELECT TOP 1
    @LesDb = LesDbName,
    @WhCode = WarehouseCode,
    @Dry = DryRunOnly
FROM dbo.Mig_LesRunConfig
ORDER BY CreatedAt DESC;

DECLARE @sql nvarchar(max);

IF @Dry = 0
BEGIN
    SET @sql = N'
    INSERT INTO dbo.Stk_AssignmentPolicy (AisleCode, IsAvailable, MaxHeight, MaxWeight, DestinationPointCode, TenantId, CreateDate, IsDeleted)
    SELECT
        am.SevenCode,
        CASE WHEN p.StartSign = 1 THEN 1 ELSE 0 END,
        ISNULL(p.TopHight, 0),
        CAST(ISNULL(p.TopLoad, 0) AS int),
        COALESCE(
            NULLIF(LTRIM(RTRIM(a.AssignedTargetStation)), N''''),
            am.SevenCode + N''-EP''),
        0, SYSUTCDATETIME(), 0
    FROM ' + QUOTENAME(@LesDb) + N'.dbo.AssignmentPolicy p
    INNER JOIN dbo.Mig_LesCodeMap am ON am.EntityType = N''Aisle'' AND am.LesCode = p.AisleCode
    LEFT JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Aisle a ON a.Id = p.AisleId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Stk_AssignmentPolicy x WHERE x.AisleCode = am.SevenCode);';
    EXEC sp_executesql @sql;
    PRINT N'[05] Stk_AssignmentPolicy 已写入。';
END
ELSE
    PRINT N'[05] DryRun：跳过 AssignmentPolicy 写入。';

-- ---------- 抽样对账 ----------
PRINT N'========== 对账：行数 ==========';
SET @sql = N'
SELECT N''Zone'' AS Entity,
       (SELECT COUNT(*) FROM ' + QUOTENAME(@LesDb) + N'.dbo.WarehouseZone wz
        INNER JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Warehouse w ON w.Id = wz.WarehouseId AND w.Code = @WhCode) AS LesCnt,
       (SELECT COUNT(*) FROM dbo.Wms_Zone) AS SevenCnt
UNION ALL
SELECT N''Aisle'',
       (SELECT COUNT(*) FROM dbo.Mig_LesCodeMap WHERE EntityType = N''Aisle''),
       (SELECT COUNT(*) FROM dbo.Wms_Aisle)
UNION ALL
SELECT N''Location'',
       (SELECT COUNT(*) FROM dbo.Mig_LesCodeMap WHERE EntityType = N''Location''),
       (SELECT COUNT(*) FROM dbo.Wms_Location)
UNION ALL
SELECT N''StockQtySum'',
       (SELECT CAST(SUM(Quantity) AS int) FROM ' + QUOTENAME(@LesDb) + N'.dbo.Stock WHERE Quantity > 0),
       (SELECT CAST(SUM(Qty) AS int) FROM dbo.Wms_Stock);';
EXEC sp_executesql @sql, N'@WhCode nvarchar(64)', @WhCode;

PRINT N'========== 对账：库存无货位映射（应为空）==========';
SET @sql = N'
SELECT TOP 50 s.Id, s.MaterialCode, s.ContainerCode, s.Quantity, c.LocationCode AS LesLoc
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Stock s
LEFT JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Container c ON c.Code = s.ContainerCode
LEFT JOIN dbo.Mig_LesCodeMap lm ON lm.EntityType = N''Location'' AND lm.LesCode = c.LocationCode
WHERE s.Quantity > 0 AND lm.SevenCode IS NULL;';
EXEC sp_executesql @sql;

PRINT N'========== 对账：前缀抽样（各 10 条）==========';
SELECT TOP 10 LesCode, SevenCode, PackId FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Zone';
SELECT TOP 10 LesCode, SevenCode, PackId FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Aisle';
SELECT TOP 10 LesCode, SevenCode, PackId FROM dbo.Mig_LesCodeMap WHERE EntityType = N'Location';

PRINT N'========== 对账：Seven 侧无前缀码（应为空）==========';
SELECT Code FROM dbo.Wms_Zone WHERE Code NOT LIKE N'Stk.%' AND Code NOT LIKE N'Fw.%' AND Code NOT LIKE N'Bs.%';
SELECT Code FROM dbo.Wms_Aisle WHERE Code NOT LIKE N'Stk.%' AND Code NOT LIKE N'Fw.%' AND Code NOT LIKE N'Bs.%';
SELECT Code FROM dbo.Wms_Location WHERE Code NOT LIKE N'Stk.%' AND Code NOT LIKE N'Fw.%' AND Code NOT LIKE N'Bs.%';
SELECT LocationCode FROM dbo.Wms_Stock WHERE LocationCode = N'UNKNOWN' OR (LocationCode NOT LIKE N'Stk.%' AND LocationCode NOT LIKE N'Fw.%' AND LocationCode NOT LIKE N'Bs.%');

PRINT N'[05] 对账查询已输出。验收：行数接近、无 UNKNOWN 库存、无裸码。';
GO
