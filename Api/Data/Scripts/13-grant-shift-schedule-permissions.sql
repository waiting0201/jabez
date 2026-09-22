/*
================================================================================
 指派四週彈性工時的 6 個權限碼給角色
================================================================================

 背景
 ----
 這 6 個權限碼上線時**刻意不回填既有角色**（比照 reports-overtime:amount 先例，預設全關），
 故部署完成後除了 Superadmin 以外**沒有任何人看得到**排班月曆、活動日管理、出勤排休總覽表 ——
 選單不顯示、路由被 permissionGuard 擋在 403。

 這支腳本把權限配給角色。**以角色名稱定位、不寫死 Id**（Roles.Id 是 nvarchar，
 測試站多為 GUID 字面量、正式站可能不同），故同一份可在測試站與正式站各跑一次。

 指派原則
 --------
 | 權限碼 | 給誰 | 理由 |
 |---|---|---|
 | shift-schedule:read / :write | **全部角色** | 每個人都要排自己的例假與休假日，這是全員功能 |
 | activity-days:read / :write  | 有帶人的主管層 | 排活動日與預定人力是主管職責 |
 | reports-shift-schedule:read  | 主管層 + 財務 / 人事 / 後端管理者 | 出勤排休總覽表是管理用報表 |
 | shift-schedule:view-all      | **僅 3 個** | 跨部門看全公司班表，繞過 ProjectAccessScope 的部門範圍 |

 ⚠ shift-schedule:view-all 刻意給最少人：它會**繞過部門可見性**。
   但要留意一個既有現象 —— 有 6 個部門的 Department.CanSeeAll = 1（共 15 人），
   那些人就算沒有 view-all 也本來就看得到全公司。這是規劃時已接受的取捨
   （排班不視為敏感資料），不要為了收斂它去動 Department 旗標 ——
   那會連帶影響專案、款項統計、出缺勤等 7 個既有消費端。

 ⚠ **指派後必須請受影響的同仁重新登入**：權限在 JWT 裡（access 60 分 / refresh 7 天），
   舊 token 沒有新碼，畫面不會因為這支腳本跑過就立刻變。

 執行方式
 --------
 1. 先原樣執行（@Commit = 0）看空跑報表，確認角色名稱都有命中
 2. 再把 @Commit 改成 1 重跑
 冪等：已指派者跳過，可安全重跑。
================================================================================
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @Commit bit = 0;   -- 改成 1 才會真的寫入

BEGIN TRAN;

-- ── 指派矩陣（角色名稱 × 權限碼） ───────────────────────────────────────────
DECLARE @Grant TABLE (RoleName nvarchar(200), Code nvarchar(100));

-- 全員：排自己的班
INSERT INTO @Grant (RoleName, Code)
SELECT r.Name, c.Code
FROM Roles r
CROSS JOIN (VALUES ('shift-schedule:read'), ('shift-schedule:write')) AS c(Code);

-- 主管層：排活動日 + 看總覽表
INSERT INTO @Grant (RoleName, Code)
SELECT r.RoleName, c.Code
FROM (VALUES (N'協理'), (N'總監室總監'), (N'營運管理部執行長'),
             (N'數位研發部經理'), (N'經理副理主管')) AS r(RoleName)
CROSS JOIN (VALUES ('activity-days:read'), ('activity-days:write'),
                   ('reports-shift-schedule:read')) AS c(Code);

-- 財務 / 人事 / 後端管理者：看總覽表
INSERT INTO @Grant (RoleName, Code)
SELECT r.RoleName, 'reports-shift-schedule:read'
FROM (VALUES (N'財務管理部'), (N'財務管理部會計室'), (N'後端管理者')) AS r(RoleName);

-- 跨部門看全公司（最少人）
INSERT INTO @Grant (RoleName, Code)
SELECT r.RoleName, 'shift-schedule:view-all'
FROM (VALUES (N'後端管理者'), (N'總監室總監'), (N'財務管理部')) AS r(RoleName);

-- ── 解析成實際的 (RoleId, PermissionId)，並排除已指派者 ─────────────────────
SELECT r.Id AS RoleId, r.Name AS RoleName, p.Id AS PermissionId, p.Code
INTO #Todo
FROM @Grant g
JOIN Roles       r ON r.Name = g.RoleName
JOIN Permissions p ON p.Code = g.Code
WHERE NOT EXISTS (SELECT 1 FROM RolePermissions rp
                  WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- ── 空跑報表 ────────────────────────────────────────────────────────────────
SELECT N'【將指派】' AS Section, RoleName, Code FROM #Todo ORDER BY RoleName, Code;
SELECT N'合計' AS Section, COUNT(*) AS 筆數 FROM #Todo;

-- 角色名稱打錯會靜默漏掉，故明列「指派矩陣中查無此角色」者
SELECT DISTINCT N'⚠ 查無此角色' AS Section, g.RoleName
FROM @Grant g WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Name = g.RoleName);

-- 同理，權限碼查無（migration 沒跑到）也要看得見
SELECT DISTINCT N'⚠ 查無此權限碼' AS Section, g.Code
FROM @Grant g WHERE NOT EXISTS (SELECT 1 FROM Permissions p WHERE p.Code = g.Code);

-- ── 寫入 ────────────────────────────────────────────────────────────────────
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT RoleId, PermissionId FROM #Todo;

-- ── 寫入後的完整分佈（供核對） ──────────────────────────────────────────────
SELECT p.Code,
       STRING_AGG(CAST(r.Name AS nvarchar(max)), N'、') WITHIN GROUP (ORDER BY r.Name) AS 已指派角色
FROM Permissions p
LEFT JOIN RolePermissions rp ON rp.PermissionId = p.Id
LEFT JOIN Roles r            ON r.Id = rp.RoleId
WHERE p.Code IN ('shift-schedule:read','shift-schedule:write','shift-schedule:view-all',
                 'reports-shift-schedule:read','activity-days:read','activity-days:write')
GROUP BY p.Code
ORDER BY p.Code;

DROP TABLE #Todo;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT N'== 已認可（COMMIT）。請通知受影響的同仁重新登入，權限才會生效 ==';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT N'== 空跑（ROLLBACK），把 @Commit 改成 1 才會真的寫入 ==';
END
