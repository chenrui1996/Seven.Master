-- =============================================================================
-- LES2 → Seven.Master WMS 数据迁移（M6）
-- 前置：同实例可访问 LesDb / SevenDb；先跑 EF 迁移建好 Wms_*/Stk_* 表
-- 用法：按 00→05 顺序执行；每步可单独回滚本步写入（见各文件底部注释）
-- 详解：scripts/les-migrate/README.md 与 design/wms/06-les-data-migration.md
-- =============================================================================

-- >>> 使用前必改 <<<
USE SevenDb;
GO

IF OBJECT_ID(N'dbo.Mig_LesRunConfig', N'U') IS NOT NULL DROP TABLE dbo.Mig_LesRunConfig;
CREATE TABLE dbo.Mig_LesRunConfig
(
    LesDbName            sysname        NOT NULL,   -- 例：LesDb
    SevenDbName          sysname        NOT NULL,   -- 例：SevenDb
    WarehouseCode        nvarchar(64)   NOT NULL,   -- 只迁该仓（经 WarehouseZone）
    DenseMiniLoadPackId  nvarchar(32)   NOT NULL CONSTRAINT DF_Mig_DensePack DEFAULT (N'stacker'),
    -- stacker | fourway：DenseWarehouse(30)/MiniLoad(31) 映射目标 Pack
    DryRunOnly           bit            NOT NULL CONSTRAINT DF_Mig_DryRun DEFAULT (1),
    CreatedAt            datetime2      NOT NULL CONSTRAINT DF_Mig_CfgAt DEFAULT (SYSUTCDATETIME())
);

INSERT INTO dbo.Mig_LesRunConfig (LesDbName, SevenDbName, WarehouseCode, DenseMiniLoadPackId, DryRunOnly)
VALUES (N'LesDb', N'SevenDb', N'WH01', N'stacker', 1);
GO

PRINT N'[00] 配置已写入 Mig_LesRunConfig。确认 WarehouseCode / 库名 / DryRunOnly 后继续 01。';
