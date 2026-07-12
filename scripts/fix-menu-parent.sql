-- 修复已有库中菜单 ParentId 扁平结构（仅当「系统管理」与子菜单均为 ParentId=0 时执行）
-- 执行前请备份 Sys_Menu 表

SET @systemId = (SELECT Menu_Id FROM Sys_Menu WHERE MenuName = '系统管理' AND ParentId = 0 LIMIT 1);

UPDATE Sys_Menu SET Url = NULL, TableName = NULL, Auth = NULL
WHERE Menu_Id = @systemId;

UPDATE Sys_Menu SET ParentId = @systemId
WHERE MenuName IN (
  '用户管理','角色管理','菜单管理','部门管理','字典管理',
  '日志管理','告警管理','代码生成','设备管理'
) AND ParentId = 0 AND Menu_Id <> @systemId;
