-- =============================================================================
-- 04：容器类型 / 容器 / 库存（LocationCode 经映射表转前缀码）
-- LES Stock 无 LocationCode，经 Container.LocationCode → Mig_LesCodeMap
-- =============================================================================
USE SevenDb;
GO

DECLARE @LesDb sysname, @Dry bit;
SELECT TOP 1 @LesDb = LesDbName, @Dry = DryRunOnly
FROM dbo.Mig_LesRunConfig
ORDER BY CreatedAt DESC;

DECLARE @sql nvarchar(max);

IF @Dry = 1
BEGIN
    SET @sql = N'
    SELECT N''ContainerType'' AS Step, COUNT(*) AS WouldInsert FROM ' + QUOTENAME(@LesDb) + N'.dbo.ContainerType
    UNION ALL
    SELECT N''Container'', COUNT(*) FROM ' + QUOTENAME(@LesDb) + N'.dbo.Container
    UNION ALL
    SELECT N''Stock'', COUNT(*) FROM ' + QUOTENAME(@LesDb) + N'.dbo.Stock s
    WHERE s.Quantity > 0;';
    EXEC sp_executesql @sql;
    PRINT N'[04] DryRun：未写入。';
END
ELSE
BEGIN
-- ContainerType（Comments 作 Name；无 Comments 用 Code）
SET @sql = N'
INSERT INTO dbo.Wms_ContainerType (Code, Name, TenantId, CreateDate, IsDeleted)
SELECT ct.Code, ISNULL(NULLIF(LTRIM(RTRIM(ct.Comments)), N''''), ct.Code), 0, SYSUTCDATETIME(), 0
FROM ' + QUOTENAME(@LesDb) + N'.dbo.ContainerType ct
WHERE NOT EXISTS (SELECT 1 FROM dbo.Wms_ContainerType x WHERE x.Code = ct.Code);';
EXEC sp_executesql @sql;

-- Container：LocationCode 换成 Seven 前缀码
SET IDENTITY_INSERT dbo.Wms_Container ON;
SET @sql = N'
INSERT INTO dbo.Wms_Container (Id, Code, ContainerTypeId, LocationCode, Status, TenantId, CreateDate, IsDeleted)
SELECT
    c.Id,
    c.Code,
    ct.Id,
    NULLIF(lm.SevenCode, N''''),
    CASE WHEN c.Status = 0 THEN 0 ELSE 1 END,
    0, SYSUTCDATETIME(), 0
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Container c
LEFT JOIN dbo.Wms_ContainerType ct ON ct.Code = c.ContainerTypeCode
LEFT JOIN dbo.Mig_LesCodeMap lm
       ON lm.EntityType = N''Location'' AND lm.LesCode = c.LocationCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.Wms_Container x WHERE x.Code = c.Code);';
EXEC sp_executesql @sql;
SET IDENTITY_INSERT dbo.Wms_Container OFF;

-- 回写货位当前容器
UPDATE loc SET
    loc.CurrentContainerCode = c.Code,
    loc.IsOccupied = 1
FROM dbo.Wms_Location loc
INNER JOIN dbo.Wms_Container c ON c.LocationCode = loc.Code
WHERE c.LocationCode IS NOT NULL;

-- Stock
SET @sql = N'
INSERT INTO dbo.Wms_Stock (LocationCode, ContainerCode, MaterialCode, Qty, AvailableQty, Lot, TenantId, CreateDate, IsDeleted)
SELECT
    ISNULL(lm.SevenCode, N''UNKNOWN''),
    NULLIF(s.ContainerCode, N''''),
    s.MaterialCode,
    CAST(s.Quantity AS decimal(18,4)),
    CAST(s.AvailableQuantity AS decimal(18,4)),
    NULLIF(s.Batch, N''''),
    0, SYSUTCDATETIME(), 0
FROM ' + QUOTENAME(@LesDb) + N'.dbo.Stock s
LEFT JOIN ' + QUOTENAME(@LesDb) + N'.dbo.Container c ON c.Code = s.ContainerCode
LEFT JOIN dbo.Mig_LesCodeMap lm
       ON lm.EntityType = N''Location'' AND lm.LesCode = c.LocationCode
WHERE s.Quantity > 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Wms_Stock x
      WHERE x.MaterialCode = s.MaterialCode
        AND ISNULL(x.ContainerCode, N'''') = ISNULL(s.ContainerCode, N'''')
        AND x.LocationCode = ISNULL(lm.SevenCode, N''UNKNOWN'')
        AND ISNULL(x.Lot, N'''') = ISNULL(s.Batch, N'''')
  );';
EXEC sp_executesql @sql;

PRINT N'[04] 容器与库存写入完成。继续 05 策略与对账。';
END
GO
