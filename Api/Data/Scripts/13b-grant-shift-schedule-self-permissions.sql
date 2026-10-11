/*
================================================================================
 13b　階段二：開放全員「個人排班」（2026-10-21 教育訓練後，開放排 11／12／1 月）
================================================================================

 正式站分階段開放四週彈性工時（2026-10-11 決議）：
   13a　　　　10 月中旬　activity-days:read / :write → 主管層
   13b（本檔）10/21　　　shift-schedule:read / :write → 全部角色
 出勤排休總覽表（reports-shift-schedule:read）與 shift-schedule:view-all 兩階段都不給；
 日後要開再跑完整版 13-grant-shift-schedule-permissions.sql（冪等，已指派者跳過）。

 ⚠ shift-schedule:read 是選單與路由的判準，只給 :write 等於白給，故兩碼一起給。
 ⚠ 〈改班申請〉沿用同一組權限碼，本檔跑完同仁即可送改班申請 —— 送之前須先跑 14 建好簽核流程。

 執行方式：先原樣執行（@Commit = 0）看空跑報表 → 確認後改 @Commit = 1 重跑。冪等可重跑。
 ⚠ 指派後須請**全員重新登入**（權限在 JWT 內）。
================================================================================
*/

DECLARE @Commit bit = 0;   -- 改成 1 才會真的寫入

BEGIN TRAN;

-- ── 指派矩陣（角色名稱 × 權限碼） ───────────────────────────────────────────
DECLARE @Grant TABLE (RoleName nvarchar(200), Code nvarchar(100));

-- 全員：排自己的班
INSERT INTO @Grant (RoleName, Code)
SELECT r.Name, c.Code
FROM Roles r
CROSS JOIN (VALUES ('shift-schedule:read'), ('shift-schedule:write')) AS c(Code);

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
