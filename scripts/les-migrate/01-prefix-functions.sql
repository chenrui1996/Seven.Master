-- =============================================================================
-- 01：前缀函数 + 编码映射表（不写业务表；DryRun 也可执行）
-- 规则对齐 Seven.Domain.Wms.LesMigrationCodes / design/wms/02 §7
-- =============================================================================
USE SevenDb;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_PackPrefix(@packId nvarchar(32))
RETURNS nvarchar(8)
AS
BEGIN
    DECLARE @p nvarchar(32) = LOWER(LTRIM(RTRIM(ISNULL(@packId, N''))));
    RETURN CASE @p
        WHEN N'stacker' THEN N'Stk.'
        WHEN N'fourway' THEN N'Fw.'
        WHEN N'boxsort' THEN N'Bs.'
        ELSE N''
    END;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_StripPack(@code nvarchar(128))
RETURNS nvarchar(128)
AS
BEGIN
    DECLARE @c nvarchar(128) = LTRIM(RTRIM(ISNULL(@code, N'')));
    IF @c LIKE N'Stk.%' SET @c = SUBSTRING(@c, 5, 200);
    ELSE IF @c LIKE N'Fw.%' SET @c = SUBSTRING(@c, 4, 200);
    ELSE IF @c LIKE N'Bs.%' SET @c = SUBSTRING(@c, 4, 200);
    RETURN @c;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_StripKind(@code nvarchar(128))
RETURNS nvarchar(128)
AS
BEGIN
    DECLARE @c nvarchar(128) = dbo.fn_Mig_StripPack(@code);
    IF @c LIKE N'Z-%' OR @c LIKE N'A-%' OR @c LIKE N'B-%' OR @c LIKE N'N-%' OR @c LIKE N'L-%'
        SET @c = SUBSTRING(@c, 3, 200);
    RETURN @c;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_WithKind(@lesCode nvarchar(128), @packId nvarchar(32), @kind nvarchar(4))
RETURNS nvarchar(128)
AS
BEGIN
    DECLARE @prefix nvarchar(8) = dbo.fn_Mig_PackPrefix(@packId);
    IF @prefix = N'' RETURN NULL;
    RETURN @prefix + @kind + dbo.fn_Mig_StripKind(@lesCode);
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_LocationCode(@lesCode nvarchar(128), @packId nvarchar(32))
RETURNS nvarchar(128)
AS
BEGIN
    IF LOWER(LTRIM(RTRIM(@packId))) = N'fourway'
    BEGIN
        DECLARE @c nvarchar(128) = dbo.fn_Mig_StripPack(@lesCode);
        IF @c LIKE N'N-%' SET @c = SUBSTRING(@c, 3, 200);
        ELSE IF LEN(@c) > 1 AND LEFT(@c, 1) IN (N'N', N'n') AND SUBSTRING(@c, 2, 1) LIKE N'[0-9]'
            SET @c = SUBSTRING(@c, 2, 200);
        RETURN dbo.fn_Mig_PackPrefix(@packId) + N'N-' + @c;
    END
    RETURN dbo.fn_Mig_WithKind(@lesCode, @packId, N'B-');
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_LayerCode(@lesCode nvarchar(128), @packId nvarchar(32))
RETURNS nvarchar(128)
AS
BEGIN
    DECLARE @prefix nvarchar(8) = dbo.fn_Mig_PackPrefix(@packId);
    DECLARE @c nvarchar(128) = dbo.fn_Mig_StripPack(@lesCode);
    DECLARE @num nvarchar(32);
    IF @c LIKE N'L-%' SET @c = SUBSTRING(@c, 3, 200);
    ELSE IF LEFT(@c, 1) IN (N'L', N'l') SET @c = SUBSTRING(@c, 2, 200);
    IF TRY_CAST(@c AS int) IS NOT NULL AND TRY_CAST(@c AS int) BETWEEN 0 AND 999
    BEGIN
        SET @num = RIGHT(N'00' + CAST(TRY_CAST(@c AS int) AS nvarchar(8)), 2);
        RETURN @prefix + N'L' + @num;
    END
    RETURN dbo.fn_Mig_WithKind(@lesCode, @packId, N'L-');
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_Mig_PackFromZoneType(@zoneType tinyint, @densePack nvarchar(32))
RETURNS nvarchar(32)
AS
BEGIN
    IF @zoneType IN (30, 31) RETURN ISNULL(NULLIF(LTRIM(RTRIM(@densePack)), N''), N'stacker');
    RETURN N'stacker';
END;
GO

IF OBJECT_ID(N'dbo.Mig_LesCodeMap', N'U') IS NOT NULL DROP TABLE dbo.Mig_LesCodeMap;
CREATE TABLE dbo.Mig_LesCodeMap
(
    EntityType   nvarchar(32)  NOT NULL, -- Warehouse/Zone/Aisle/Location/Container
    LesId        int           NULL,
    LesCode      nvarchar(128) NOT NULL,
    PackId       nvarchar(32)  NOT NULL,
    SevenCode    nvarchar(128) NOT NULL,
    WarehouseId  int           NULL,
    ZoneId       int           NULL,
    AisleId      int           NULL,
    ExtraJson    nvarchar(512) NULL,
    CONSTRAINT PK_Mig_LesCodeMap PRIMARY KEY (EntityType, LesCode, PackId)
);
GO

PRINT N'[01] 前缀函数与 Mig_LesCodeMap 已就绪。继续 02 填充映射。';
