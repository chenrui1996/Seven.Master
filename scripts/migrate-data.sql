-- Legrand.Master → Seven.Master 数据迁移脚本
-- 注意：密码字段无法从 DES 直接迁移到 BCrypt，迁移后用户需首次登录重置密码

-- ========== 1. 用户迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_User (User_Id, UserName, UserTrueName, PasswordHash, MustResetPassword, Role_Id, RoleName, Enable, CreateDate, PhoneNo, Email)
-- SELECT User_Id, UserName, UserTrueName, '', 1, Role_Id, RoleName, Enable, CreateDate, PhoneNo, Email
-- FROM LegrandDb.dbo.Sys_User WHERE Enable = 1;

-- ========== 2. 角色迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_Role (Role_Id, ParentId, RoleName, Enable, OrderNo, CreateDate)
-- SELECT Role_Id, ParentId, RoleName, Enable, OrderNo, CreateDate
-- FROM LegrandDb.dbo.Sys_Role;

-- ========== 3. 菜单迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_Menu (Menu_Id, ParentId, MenuName, TableName, Url, Auth, Icon, OrderNo, Enable, MenuType, CreateDate)
-- SELECT Menu_Id, ParentId, MenuName, TableName, Url, Auth, Icon, OrderNo, Enable, MenuType, CreateDate
-- FROM LegrandDb.dbo.Sys_Menu;

-- ========== 4. 角色权限迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_RoleAuth (Auth_Id, Role_Id, Menu_Id, AuthValue)
-- SELECT Auth_Id, Role_Id, Menu_Id, AuthValue FROM LegrandDb.dbo.Sys_RoleAuth;

-- ========== 5. 部门迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_Department (DepartmentId, DepartmentName, ParentId, Enable, OrderNo, CreateDate)
-- SELECT DepartmentId, DepartmentName, ParentId, Enable, OrderNo, CreateDate
-- FROM LegrandDb.dbo.Sys_Department;

-- ========== 6. 字典迁移 ==========
-- INSERT INTO SevenDb.dbo.Sys_Dictionary (Dic_ID, DicNo, DicName, ParentId, Enable, OrderNo, CreateDate)
-- SELECT Dic_ID, DicNo, DicName, ParentId, Enable, OrderNo, CreateDate
-- FROM LegrandDb.dbo.Sys_Dictionary;

-- INSERT INTO SevenDb.dbo.Sys_DictionaryList (DicList_ID, Dic_ID, DicName, DicValue, Enable, OrderNo)
-- SELECT DicList_ID, Dic_ID, DicName, DicValue, Enable, OrderNo
-- FROM LegrandDb.dbo.Sys_DictionaryList;

-- ========== 7. 工作流迁移 ==========
-- 按需迁移 Sys_WorkFlow, Sys_WorkFlowStep, Sys_WorkFlowTable 等表

-- ========== 密码重置说明 ==========
-- 迁移完成后，所有用户的 MustResetPassword = 1
-- 管理员可通过 Seven API 重置密码，或用户首次登录时强制改密
-- 新密码将使用 BCrypt (workFactor=12) 存储

-- ========== 接口对比测试 ==========
-- 使用以下 curl 命令验证 Seven API 与 Legrand 响应结构兼容：
-- curl -X POST http://localhost:5000/api/Auth/login -H "Content-Type: application/json" -d "{\"userName\":\"admin\",\"password\":\"123456\"}"
-- 期望响应: {"status":true,"message":"登录成功","data":{"token":"...","refreshToken":"...","permissions":[...]}}
